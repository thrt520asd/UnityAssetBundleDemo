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

	static string abPath =  "";
	static Dictionary<string , string[]> abAssetDic  = new Dictionary<string, string[]>() ;
	static Dictionary<string , HashSet<Object>> abDepDic  = new Dictionary<string, HashSet<Object>>();
	static Dictionary<Object , List<string>> depObjDic = new Dictionary<Object, List<string>>() ;
	static List<Object> dupResList = new List<Object>();
	static List<Object> dupBuiltInResList = new List<Object>();
	static Dictionary<string , GUIIDAndFileId> builtInExtraDic = new Dictionary<string, GUIIDAndFileId> ();
	static Dictionary<Object ,List<string>> replacedResAssetDic = new Dictionary<Object, List<string>> ();

	/// <summary>
	/// 快速打包 沿用上次打包配置 ， 版本号默认+1
	/// </summary>
	public static void BuildByLastSet(){
		BuildBundle (++BundleBuildConfig.VersionNum , BundleBuildConfig.outputPath, BundleBuildConfig.compressOption, BundleBuildConfig.BuildTarget, BundleBuildConfig.ForceBuild);
	}

	public static void BuildBundle(int version ,  string path , CompressOption compressOption , BuildTarget target , bool isReBuild ){
		EditorUtility.DisplayProgressBar ("开始打包ab", "开始打包ab", 1f);
		BeforeBuild ();
		BundleBuildConfig.outputPath = path;
		BundleBuildConfig.VersionNum = version;

		abPath = Application.dataPath + "/" + path + "/" + target.ToString()+"/Bundles";
		if (isReBuild && Directory.Exists(abPath)) {
			Directory.Delete (abPath, true);
		}
		if (!Directory.Exists (abPath)) {
			Directory.CreateDirectory (abPath);
		}
		BuildAssetBundleOptions options = 0;
		switch (compressOption)
		{
			case CompressOption.Uncompressed: options = options | BuildAssetBundleOptions.UncompressedAssetBundle; break;
			case CompressOption.ChunkBasedCompression: options = options | BuildAssetBundleOptions.ChunkBasedCompression; break;
		}
		BuildPipeline.BuildAssetBundles (abPath, options, target);
		AfterBuild ();
		AssetDatabase.Refresh ();
		EditorUtility.ClearProgressBar ();
		EditorUtility.DisplayDialog ("打包完成", "打包完成" , "ok");
	}

	private static void BeforeBuild(){
		ResetBundleNameByRule ();
		AnalysisPackAssets ();
		if (BundleBuildConfig.isReplaceBuiltInRes) {
			CheckBuiltInRes ();
		}
		if (BundleBuildConfig.isReplaceBuiltInRes) {
			CopyNeedExtractFile() ;
			AnalysisExtractRes();
			ReplaceBuiltInRes ();
		}
		ResetDupResABName ();
		SetShaderAssetBundle ();//shader单独打包 做预加载和warm
	}


	public static void ResetBundleNameByRule(){
		EditorUtility.DisplayProgressBar ("重置abName", "重置abName", 1f);
		string[] assetPaths = GetAllPackAssetPath ();
		for (int i = 0; i < assetPaths.Length; i++) {
			AssetImporter ai = AssetImporter.GetAtPath (assetPaths [i]);
			ai.assetBundleName = null;
		}
		AssetDatabase.RemoveUnusedAssetBundleNames ();
		AutoABNamePostprocessor.PackAll ();
	}

	public static void AnalysisPackAssets(){
		EditorUtility.DisplayProgressBar ("检测重复AB", "检测重复AB", 0.1f);
		abAssetDic.Clear ();
		abDepDic.Clear ();
		depObjDic.Clear ();
		dupResList.Clear ();
		dupBuiltInResList.Clear ();

		string[] abNames = AssetDatabase.GetAllAssetBundleNames ();
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
		EditorUtility.DisplayProgressBar ("检测重复AB", "检测重复AB", 0.3f);
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
		EditorUtility.DisplayProgressBar ("检测重复AB", "检测重复AB", 0.6f);
		foreach (var pair in depObjDic) {
			if (!(pair.Value.Count > 1)) {
				continue;
			}
			string depPath = AssetDatabase.GetAssetPath (pair.Key);
			if (IsBuildIn (depPath)) {
				dupBuiltInResList.Add (pair.Key);
			} else {
				dupResList.Add (pair.Key);
			}
		}
		EditorUtility.DisplayProgressBar ("检测重复AB", "检测重复AB", 0.9f);
		EditorUtility.ClearProgressBar ();
	}

	private static void CheckBuiltInRes(){
		if (!Directory.Exists (BundleBuildConfig.BuiltExtraPath)) {
			BundleBuildConfig.isReplaceBuiltInRes = false;
			EditorUtility.DisplayDialog ("Error", "无内置资源文件，请检查路径，或更换文件 ， tips:当前路径" + BundleBuildConfig.BuiltExtraPath , "ok");
		}
	}

	private static void CopyNeedExtractFile(){
		string shaderNameFilePath = BundleBuildConfig.BuiltExtraPath + "/shaderName.txt";
		if (!File.Exists (shaderNameFilePath)) {
			GenShaderNameFile ();
		}
		Dictionary<string , string > shaderFileNameDic = new Dictionary<string, string> ();
		using (FileStream fs = new FileStream (shaderNameFilePath , FileMode.Open)) {
			using (StreamReader sr = new StreamReader (fs)) {
				string content = sr.ReadLine ();
				while (content != null) {
					string[] strs = content.Split (':');
					shaderFileNameDic [strs [0]] = strs [1];
					content = sr.ReadLine ();
				}
			}
		}
		foreach (var obj in dupBuiltInResList) {
			string extName = "";
			string filePath = "";
			if (obj is Shader ) {
				if (shaderFileNameDic.ContainsKey (obj.name)) {
					filePath = BundleBuildConfig.BuiltExtraPath + "/" + shaderFileNameDic [obj.name];
				} else {
					Debug.Log ("not shader " + obj.name);
				}
			} else if (obj is Material) {
				extName = ".mat";
				filePath = BundleBuildConfig.BuiltExtraPath + "/" + obj.name + extName;
			} else if (obj is Mesh) {
				extName = ".asset";
				filePath = BundleBuildConfig.BuiltExtraPath + "/" + obj.name + extName;
			} else {
				extName = ".asset";
				filePath = BundleBuildConfig.BuiltExtraPath + "/" + obj.name + extName;
				Debug.Log ("现在不支持除了shader mesh ， material以外的内置资源");
			}

			if (File.Exists (filePath)) {
				string parentPath =	 BundleBuildConfig.BuiltExtraAssetPath;
				if (!Directory.Exists (parentPath)) {
					Directory.CreateDirectory (parentPath);
				}
				File.Copy (filePath, parentPath +"/"+ Path.GetFileName (filePath));
			} else {
				Debug.Log ("没有该文件" + filePath);
			}
		}
		AssetDatabase.Refresh ();
	}
		
	public static void GenShaderNameFile(){
		DirectoryInfo dirInfo = new DirectoryInfo (BundleBuildConfig.BuiltExtraPath);
		Debug.Log (dirInfo.FullName);
		FileInfo[] files = dirInfo.GetFiles ("*.shader", SearchOption.AllDirectories);
		Dictionary<string , string> shaderFileNameDic = new Dictionary<string, string> ();
		foreach (var file in files) {
			FileStream fs = new FileStream(file.FullName , FileMode.Open);
			StreamReader sr = new StreamReader (fs);
			string content = sr.ReadLine ();
			while (content != null) {
				if (content.Contains ("Shader")) {
					string[] strs = content.Split ('"');
					string name = strs [1];
					shaderFileNameDic [name] = file.Name;
					break;
				} else {
					content = sr.ReadLine ();
				}
			}
			sr.Close ();
		}
		using (FileStream fs = new FileStream (BundleBuildConfig.BuiltExtraPath + "/shaderName.txt" , FileMode.OpenOrCreate)) {
			using (StreamWriter sw = new StreamWriter (fs)) {
				foreach (var pair in shaderFileNameDic) {
					sw.WriteLine (string.Format ("{0}:{1}", pair.Key, pair.Value));
				}
			}
		}
	}

	private static void AnalysisExtractRes(){
		EditorUtility.DisplayProgressBar ("解析内置资源", "解析内置资源", 1f);
		builtInExtraDic.Clear ();
		if (!Directory.Exists (BundleBuildConfig.BuiltExtraAssetPath))
			return;
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

	private static void ReplaceBuiltInRes(){
		float count = dupBuiltInResList.Count;
		float i = 0;
		replacedResAssetDic.Clear ();
		foreach (var builtInRes in dupBuiltInResList) {
			EditorUtility.DisplayProgressBar ("替换guiid和fileid", "替换guiid和fileid", ++i / count);
			if (!builtInExtraDic.ContainsKey (builtInRes.name)) {
				Debug.Log ("无该资源" + builtInRes.name);
				continue;
			}
			List<string> abNameList = depObjDic [builtInRes];
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
					if (deps.Contains (builtInRes)) {
						ReplaceGUIAndFileId (builtInRes, path);
						RecordReplaceAssetPath (builtInRes, path);
						isPack = true;
					}
				}
			}
			if (isPack) {
				GUIIDAndFileId ids = builtInExtraDic [builtInRes.name];
				string path = AssetDatabase.GUIDToAssetPath (ids.guid);
				dupResList.AddRange(AssetDatabase.LoadAllAssetsAtPath(path)) ;
			}
		}
		EditorUtility.ClearProgressBar ();
	}

	private static void ReplaceGUIAndFileId(Object builInRes , string targetAssetPath){
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

	private static void RecordReplaceAssetPath(Object builtInRes , string path){
		if (replacedResAssetDic.ContainsKey (builtInRes)) {
			replacedResAssetDic [builtInRes].Add (path);
		} else {
			replacedResAssetDic [builtInRes] = new List<string>{ path };
		}
	}

	private static void ResetDupResABName(){
		float count = dupResList.Count;
		for (int i = 0; i < dupResList.Count; i++) {
			string path = AssetDatabase.GetAssetPath (dupResList[i]);
			AssetImporter ai = AssetImporter.GetAtPath (path);
			ai.assetBundleName = BundleBuildConfig.CommonBundleName;
			EditorUtility.DisplayProgressBar ("修改abName", path + "  "+BundleBuildConfig.CommonBundleName, i / count);
		}
	}

	static void SetShaderAssetBundle ()
	{
		HashSet<Object> alldepSet = GetAllDeps() ;
		foreach (var depobj in alldepSet) {
			if (depobj is Shader) {
				string path = AssetDatabase.GetAssetPath (depobj);
				Debug.Log (path);
				AssetImporter ai = AssetImporter.GetAtPath (path);
				if (ai == null) {
					Debug.Log (" ai null exort  " + AssetDatabase.GetAssetPath (depobj) + depobj.name);
				} else {
					ai.assetBundleName = BundleBuildConfig.ShaderBundleName;
				}
			}
		}
	}

	private static void AfterBuild(){
		CreateMD5File ();
		if (BundleBuildConfig.isReplaceBuiltInRes) {
			ReverReplaceBuiltInRes ();
			DeleteCopyBuiltInRes ();
		}
		ComparerBundleFile ();
	}

	public static void CreateMD5File ()
	{
		EditorUtility.DisplayProgressBar ("生成MD5文件", "生成MD5文件", 1f);
		FileStream fs = new FileStream (abPath + "/../file.txt", FileMode.OpenOrCreate);
		StreamWriter sw = new StreamWriter (fs);
		string[] files = Directory.GetFiles (abPath, "*.*", SearchOption.AllDirectories).Where (s => {return !(s.EndsWith(".meta") || s.EndsWith(".manifest"));}).ToArray();
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

	private static void AnalysisBuildedBundles(){
		
	}

	private static void ReverReplaceBuiltInRes(){
		foreach (var pair in replacedResAssetDic) {
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
	}

	private static void DeleteCopyBuiltInRes(){
		Directory.Delete (BundleBuildConfig.BuiltExtraAssetPath , true);
	}

	public static void ComparerBundleFile ()
	{
		
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
		for (int i = 0; i < dupResList.Count; i++) {
			sw.WriteLine(AssetDatabase.GetAssetPath(dupResList[i]));
		}
		sw.WriteLine ("内置资源");
		for (int i = 0; i < dupBuiltInResList.Count; i++) {
			sw.WriteLine(AssetDatabase.GetAssetPath(dupBuiltInResList[i])+dupBuiltInResList[i].name) ;
		}
		sw.Close ();
		fs.Close ();
	}



	/// <summary>
	/// 获取指定path的所有依赖
	/// </summary>
	/// <returns>The asset deps.</returns>
	/// <param name="path">Path.</param>
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

	/// <summary>
	/// 获取所有依赖 无重复的
	/// </summary>
	private static HashSet<Object> GetAllDeps(){
		string[] abNames = AssetDatabase.GetAllAssetBundleNames();
		HashSet<Object> allDepSet = new HashSet<Object> ();
		foreach (var abName in abNames) {
			string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle (abName);
			List<Object> assetObjList = new List<Object>() ;
			foreach (var assetPath in assetPaths) {
				if (assetPath.EndsWith (".unity")) {
					assetObjList.Add (AssetDatabase.LoadMainAssetAtPath (assetPath));
				} else {
					assetObjList.AddRange (AssetDatabase.LoadAllAssetsAtPath (assetPath));
				}
			}
			HashSet<Object> depObjSet = new HashSet<Object>(EditorUtility.CollectDependencies(assetObjList.ToArray()).Where(x => !(x is MonoScript)));
			allDepSet.UnionWith (depObjSet);
		}
		return allDepSet;
	}

}

public class GUIIDAndFileId{
	public  string guid ;
	public long fileid ;
}
