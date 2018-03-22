using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO ;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System;
using Object = UnityEngine.Object ;

public class BundleBuilder  {

	public static bool isLog = true;
	static Dictionary<string , string[]> abAssetDic  = new Dictionary<string, string[]>() ;
	static Dictionary<string , HashSet<Object>> abDepDic  = new Dictionary<string, HashSet<Object>>();
	static Dictionary<Object , List<string>> depObjDic = new Dictionary<Object, List<string>>() ;
	static List<Object> dupList = new List<Object>();
	static List<Object> buildInList = new List<Object>();
	static Dictionary<string , GUIIDAndFileId> builtInExtraDic = new Dictionary<string, GUIIDAndFileId> ();
	static Dictionary<Object ,List<string>> replaceBuiltInResDic = new Dictionary<Object, List<string>> ();

	public static void BuildBundle(string path , CompressOption compressOption , BuildTarget target , bool isReBuild ){
		EditorUtility.DisplayProgressBar ("开始打包ab", "开始打包ab", 1f);

		BundleBuildConfig.outputPath = path;
		string abPath = Application.dataPath + "/" + path + "/" + target.ToString();
		if (isReBuild && Directory.Exists(abPath)) {
			Directory.Delete (abPath, true);
		}
		if (!Directory.Exists (abPath)) {
			Directory.CreateDirectory (abPath);
		}
		ResetBundleName ();
		CheckDuplicateRes ();
		SetShaderAssetBundle ();
		BuildAssetBundleOptions options = 0;
		switch (compressOption)
		{
			case CompressOption.Uncompressed: options = options | BuildAssetBundleOptions.UncompressedAssetBundle; break;
			case CompressOption.ChunkBasedCompression: options = options | BuildAssetBundleOptions.ChunkBasedCompression; break;
		}
		BuildPipeline.BuildAssetBundles (abPath, options, target);
		CreateMD5File (abPath);
		EditorUtility.ClearProgressBar ();
		EditorUtility.DisplayDialog ("打包完成", "打包完成" , "ok");

		ReverReplaceBuiltInRes ();
		AssetDatabase.Refresh ();
	}

	private static void ReverReplaceBuiltInRes(){
		foreach (var pair in replaceBuiltInResDic) {
			Object defaultObj = pair.Key;
			List<string> replaceAsset = pair.Value;
			string defaultObjPath = AssetDatabase.GetAssetPath (defaultObj);
			GUIIDAndFileId ids = builtInExtraDic [defaultObj.name];
			foreach (var path in replaceAsset) {
				string windowsPath = EditorTools.GetWindowsPath (path);
				StreamReader sr = new StreamReader (windowsPath);
				string content = sr.ReadToEnd ();
				sr.Close ();
				content = content.Replace ("#修改标记", "");
				content = content.Replace (ids.guid, AssetDatabase.AssetPathToGUID (defaultObjPath));
				content = content.Replace (ids.fileid.ToString (), defaultObj.GetFileID ().ToString());
				StreamWriter sw = new StreamWriter (windowsPath);
				sw.Write (content);
				sw.Close ();
			}
		}
		Directory.Delete (BundleBuildConfig.BuiltExtraAssetPath , true);
	}

	/// <summary>
	/// 快速打包 沿用上次打包配置 ， 版本号默认+1
	/// </summary>
	public static void BuildByLastSet(){
		BuildBundle (BundleBuildConfig.outputPath, BundleBuildConfig.compressOption, BundleBuildConfig.buildTarget, BundleBuildConfig.forceBuild);
	}

	public static void ResetBundleName(){
		EditorUtility.DisplayProgressBar ("重置abName", "重置abName", 1f);
		ClearBundleName ();
		AssetDatabase.RemoveUnusedAssetBundleNames ();
		AutoABNamePostprocessor.PackAll ();
	}

	public static void CreateMD5File (string abPath)
	{
		EditorUtility.DisplayProgressBar ("生成MD5文件", "生成MD5文件", 1f);

		FileStream fs = new FileStream (abPath + "/../file.txt", FileMode.OpenOrCreate);
		StreamWriter sw = new StreamWriter (fs);
		string[] files = Directory.GetFiles (abPath, "*.*", SearchOption.AllDirectories).Where (s => {
			return !(s.EndsWith(".meta") || s.EndsWith(".manifest"));
		}).ToArray();
		StringBuilder stringBuilder = new StringBuilder ();
		string parentPath = abPath.Substring(abPath.LastIndexOf("/"));
		foreach (var filePath in files) {
			string md5 = GetMD5HashFromFile (filePath);
			string fileName = filePath.Substring (filePath.LastIndexOf( parentPath + "\\")+ (parentPath + "\\").Length);
			stringBuilder.AppendLine (string.Format ("{0}:{1}", fileName, md5));
		}
		sw.Write (stringBuilder.ToString ());
		sw.Close ();
		fs.Close ();
	}

	static void SetShaderAssetBundle ()
	{
		foreach (var dep in depObjDic.Keys) {
			if (dep is Shader) {
				string path = AssetDatabase.GetAssetPath (dep);
				if (IsBuildIn (path)) {
//					Debug.Log ("IsBuildIn shader" + path);
				}
				AssetImporter ai = AssetImporter.GetAtPath (path);
				if (ai != null) {
					ai.assetBundleName = BundleBuildConfig.ShaderBundleName;
				}
			}
		}
	}

	public static void CheckDuplicateRes(){
		EditorUtility.DisplayProgressBar ("解析依赖", "解析依赖", 1f);

		abAssetDic.Clear ();
		abDepDic.Clear ();
		depObjDic.Clear ();
		dupList.Clear ();
		buildInList.Clear ();

		string[] abNames = AssetDatabase.GetAllAssetBundleNames ();
		abDepDic = new Dictionary<string, HashSet<Object>> ();
		abAssetDic = new Dictionary<string, string[]> ();
		foreach (var abName in abNames) {
			string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle (abName);
			abAssetDic [abName] = assetPaths;
			List<Object> assetObjList = new List<Object>() ;
			foreach (var assetPath in assetPaths) {
				if (assetPath.EndsWith (".unity")) {
					assetObjList.Add (AssetDatabase.LoadMainAssetAtPath (assetPath));
				} else {
					assetObjList.AddRange (AssetDatabase.LoadAllAssetsAtPath (assetPath));
				}
			}
			HashSet<Object> depObjSet = new HashSet<Object>(EditorUtility.CollectDependencies(assetObjList.ToArray()).Where(x => !(x is MonoScript)));
			abDepDic [abName] = depObjSet;
		}
		depObjDic = new Dictionary<Object, List<string>> ();
		foreach (var pair in abDepDic) {
			string abName = pair.Key;
			HashSet<Object> depAsset = pair.Value;
			foreach (var depObj in depAsset) {
				if (depObjDic.ContainsKey (depObj)) {
					depObjDic [depObj].Add (abName); 
				} else {
					depObjDic [depObj] = new List<string> {abName};
				}
			}
		}
		dupList = new List<Object> ();
		buildInList = new List<Object> ();
		foreach (var pair in depObjDic) {
			if (!(pair.Value.Count > 1)) {
				continue;
			}
			string depPath = AssetDatabase.GetAssetPath (pair.Key);
			if (IsBuildIn (depPath)) {
				buildInList.Add (pair.Key);
				Debug.Log ("build list add " + depPath);
			} else {
				AssetImporter ai = AssetImporter.GetAtPath (depPath);
				ai.assetBundleName = BundleBuildConfig.CommonBundleName;
				EditorUtility.DisplayProgressBar ("修改abName", depPath + "  "+BundleBuildConfig.CommonBundleName, 1f);
				dupList.Add (pair.Key);
			}
		}
		ReplaceBuiltInRes (buildInList);
//		if (isLog) {
//			SaveBuildInfo();
//		}
	}

	private static void SaveBuildInfo(){
		int abCount = 0, assetCount = 0, depCount = 0, dupCount = 0 , buildInCount = 0;
		FileStream fs = new FileStream (Application.dataPath + "/pack.txt" , FileMode.OpenOrCreate , FileAccess.Write );
		StreamWriter sw = new StreamWriter (fs);
		sw.WriteLine (System.DateTime.Now.ToString ());
		sw.WriteLine ("==============abAssets==================" );
		foreach (var pair in abAssetDic) {
			sw.WriteLine ("--------------"+pair.Key+"--------------" );
			foreach (var asset in pair.Value) {
				sw.WriteLine (asset);
				assetCount++;
			}
		}
		sw.WriteLine ("==============ab对应依赖==================" );
		foreach (var pair in abDepDic) {
			abCount++;
			sw.WriteLine ("--------------"+pair.Key+"--------------" );
			foreach (var dep in pair.Value) {
				depCount++;
				sw.WriteLine (AssetDatabase.GetAssetPath (dep) +string.Format("({0})" , dep.GetType()));
			}
		}
		sw.WriteLine ("==============依赖对应ab==================" );
		depObjDic.OrderBy(x => x.Value.Count) ;
		foreach (var pair in depObjDic) {
			Object depObj = pair.Key;
			string path = AssetDatabase.GetAssetPath (depObj) +string.Format("({0})" , depObj.GetType()) ;
			if (IsBuildIn (path)) {
				buildInCount++;
			}
			sw.Write (path+ ": ");
			List<string> abNamesList = depObjDic [depObj];
			for (int i = 0; i < abNamesList.Count; i++) {
				sw.Write ( abNamesList [i]+" , ");
			}
			sw.WriteLine ();
		}
		sw.WriteLine ("==============总结==================" );
		sw.WriteLine (string.Format ("ab包：{0}个 ， asset：{1}个 ， dep{2}个， 重复asset：{3}个， 内置asset{4}个", abCount, assetCount, depCount, dupCount, buildInCount));
		sw.WriteLine ("打入common包");
		for (int i = 0; i < dupList.Count; i++) {
			sw.WriteLine(AssetDatabase.GetAssetPath(dupList[i]));
		}
		sw.WriteLine ("内置资源");
		for (int i = 0; i < buildInList.Count; i++) {
			sw.WriteLine(AssetDatabase.GetAssetPath(buildInList[i])+buildInList[i].name) ;
		}
		sw.Close ();
		fs.Close ();
	}



	private static void ReplaceBuiltInRes(List<Object> builtInDepList){
		CopyNeedExtractFile(builtInDepList) ;
		AssetDatabase.Refresh();
		AnalysisReplaceRes();

		EditorUtility.DisplayProgressBar ("替换guiid和fileid", "替换guiid和fileid", 1f);

		replaceBuiltInResDic.Clear ();
		foreach (var builtInDep in builtInDepList) {
			if (!builtInExtraDic.ContainsKey (builtInDep.name)) {
				Debug.Log ("无该资源" + builtInDep.name);
				continue;
			} else {
				Debug.Log ("try to replace " + builtInDep.name);
			}
			List<string> abNameList = depObjDic [builtInDep];
			bool isPack = false;
			foreach (var abName in abNameList) {
				string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle (abName);
				foreach (var path in assetPaths) {
					List<Object> assetObjectList = new List<Object> ();
					if (path.EndsWith (".unity")) {
						assetObjectList.Add (AssetDatabase.LoadMainAssetAtPath (path));
					} else {
						assetObjectList.AddRange (AssetDatabase.LoadAllAssetsAtPath (path));
					}
					Object[] deps = EditorUtility.CollectDependencies (assetObjectList.ToArray ());
					if (deps.Contains (builtInDep)) {
						ReplaceGUIAndFileId (builtInDep, path);
						if (replaceBuiltInResDic.ContainsKey (builtInDep)) {
							replaceBuiltInResDic [builtInDep].Add (path);
						} else {
							replaceBuiltInResDic [builtInDep] = new List<string>{ path };
						}
						isPack = true;
					}
				}
			}
			if (isPack) {
				GUIIDAndFileId ids = builtInExtraDic [builtInDep.name];
				string path = AssetDatabase.GUIDToAssetPath (ids.guid);
				AssetImporter ai = AssetImporter.GetAtPath (path);
				ai.assetBundleName = BundleBuildConfig.CommonBundleName;  //临时这么写
			}
		}
//		DirectoryInfo dir2 = new DirectoryInfo(Application.dataPath + "/Editor/builtInExtra");
//		dir2.MoveTo (Application.dataPath + "/../builtInExtra");
	}

	private static void CopyBuiltInResInAsset(){
		DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/../builtInExtra");
		dir.MoveTo (Application.dataPath + "/Editor/builtInExtra");
	}

	private static void ReplaceGUIAndFileId(Object builInRes , string targetAssetPath){
//		string windowsPath = GetWindowsPath (path);
		Debug.Log("ReplaceGUIAndFileId" + builInRes.name + "  targetPath" + targetAssetPath );
		try{
			long defaultFileId = builInRes.GetFileID() ;
			string defaultGUIId = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(builInRes)) ;
			GUIIDAndFileId ids = builtInExtraDic[builInRes.name] ;
			StreamReader sr = new StreamReader(EditorTools.GetWindowsPath(targetAssetPath)) ;
			string content = sr.ReadToEnd() ;
			sr.Close() ;
			content = content.Replace(defaultGUIId , ids.guid) ;
			content = content.Replace(defaultFileId.ToString() , ids.fileid.ToString()) ;
			FileStream fs = new FileStream(EditorTools.GetWindowsPath(targetAssetPath) , FileMode.OpenOrCreate) ;
			StreamWriter sw = new StreamWriter(fs) ;
			sw.Write(content) ;
			sw.WriteLine("#修改标记");
			sw.Close();
			fs.Close();

		}catch{
			throw new UnityException ("builnt in extrac dic 数据错误");
		}

	}

	private static void AnalysisReplaceRes(){
		EditorUtility.DisplayProgressBar ("解析内置资源", "解析内置资源", 1f);
		builtInExtraDic.Clear ();

//		string extraWindowsPath = BundleBuildConfig.BuiltExtraAssetPath;
//		string builtInUnityPath = EditorTools.GetUnityPath (extraWindowsPath);
		string[] filePaths = Directory.GetFiles(BundleBuildConfig.BuiltExtraAssetPath , "*.*" , SearchOption.AllDirectories) ;
		foreach (var filePath in filePaths) {
			if (filePath.EndsWith (".meta"))
				continue;
			string builtInUnityPath = EditorTools.GetUnityPath (filePath);
			Object builtInObj = AssetDatabase.LoadMainAssetAtPath (builtInUnityPath);
			builtInExtraDic.Add (builtInObj.name, new GUIIDAndFileId{
				guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(builtInObj)),
				fileid = builtInObj.GetFileID()
			});
		}
	}

	private static void CopyNeedExtractFile(List<Object> objList){
		foreach (var obj in objList) {
			string extName = "";
			if (obj is Shader) {
				extName = ".shader";
			} else if (obj is Material) {
				extName = ".mat";
			} else if (obj is Mesh) {
				extName = ".asset";
			} else {
				extName = ".asset";
				Debug.Log ("现在不支持除了shader mesh ， material以外的内置资源");
			}
			string filePath = BundleBuildConfig.BuiltExtraPath + "/" + obj.name + extName;
			if (File.Exists (filePath)) {
				string parentPath =	 BundleBuildConfig.BuiltExtraAssetPath;
				if (!Directory.Exists (parentPath)) {
					Directory.CreateDirectory (parentPath);
				}
				File.Copy (filePath, parentPath +"/"+ Path.GetFileName (filePath));
			} else {
				Debug.Log ("没有改文件" + filePath);
				
			}
		}
	}

	private Object[] GetAssetDeps(string path){
		List<Object> assetObjectList = new List<Object> ();
		if (path.EndsWith (".unity")) {
			assetObjectList.Add (AssetDatabase.LoadMainAssetAtPath (path));
		} else {
			assetObjectList.AddRange (AssetDatabase.LoadAllAssetsAtPath (path));
		}
		Object[] deps = EditorUtility.CollectDependencies (assetObjectList.ToArray ());
		return deps;
	}

	/// <summary>
	/// 获取所有被标记ab名的asset的路径
	/// </summary>
	/// <returns>The all pack asset path.</returns>
	private static string[] GetAllPackAssetPath(){
		string[] allABName = AssetDatabase.GetAllAssetBundleNames();

		List<string> packAssetPaths = new List<string> ();
		for (int i = 0; i < allABName.Length; i++) {
			packAssetPaths.AddRange (AssetDatabase.GetAssetPathsFromAssetBundle (allABName [i]));
		}
		return packAssetPaths.ToArray();
	}

	private static bool IsBuildIn(string path){
		return path.StartsWith("Resources/unity_builtin_extra") || path == "Library/unity default resources";
	}

	public static void ClearBundleName(){
		string[] assetPaths = GetAllPackAssetPath ();
		for (int i = 0; i < assetPaths.Length; i++) {
			AssetImporter ai = AssetImporter.GetAtPath (assetPaths [i]);
			ai.assetBundleName = null;
		}
	}

	public static string GetMD5HashFromFile(string filePath){
		try  
		{  
			FileStream file = new FileStream(filePath, System.IO.FileMode.Open);  
			MD5 md5 = new MD5CryptoServiceProvider();  
			byte[] retVal = md5.ComputeHash(file);  
			file.Close();  
			StringBuilder sb = new StringBuilder();  
			for (int i = 0; i < retVal.Length; i++)  
			{  
				sb.Append(retVal[i].ToString("x2"));  
			}  
			return sb.ToString();  
		}  
		catch (Exception ex)  
		{  
			throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);  
		}  
	}


}

public class GUIIDAndFileId{
	public  string guid ;
	public long fileid ;
}
