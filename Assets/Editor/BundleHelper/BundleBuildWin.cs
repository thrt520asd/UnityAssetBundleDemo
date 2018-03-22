using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor ;
using System.IO;
using System.Text;

public class BundleBuildWin : EditorWindow {
	
	[MenuItem("BundleBuild/快速打包AB" , false  , 0)]
	static void QuickPack(){
		if (EditorUtility.DisplayDialog ("确认打包AB", "确认打包AB ,确认不可撤回", "yes")) {
			BundleBuilder.BuildByLastSet ();
		}
	}

	[MenuItem("BundleBuild/AB打包Win" , false  , 1)]
	static void Open(){
		BundleBuildWin win = EditorWindow.GetWindow<BundleBuildWin> ();
		win.Show ();
	}

	[MenuItem("BundleBuild/Clear")]
	static void Clear(){
		BundleBuilder.ClearBundleName ();
	}
	[MenuItem("BundleBuild/CopyFIle")]
	static void DeZip(){
//		EditorUtility.DisplayProgressBar ("复制内置资源", "复制内置资源", 1f);
//		string path = Application.dataPath + "/../builtInExtra";
//		EditorTools.CopyDir (path, Application.dataPath + "/Editor" );
//		AssetDatabase.Refresh ();
		DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/../builtInExtra");
		dir.MoveTo (Application.dataPath + "/Editor/builtInExtra");
		AssetDatabase.Refresh ();
	}
//	[MenuItem("BundleBuild/MD5")]
//	static void MD5(){
//		BundleBuilder.CreateMD5File ();
//	}

	void OnEnable(){
		if (m_bundleBuildView == null) {
			m_bundleBuildView = new BundleBuildView (this);
			m_bundleRuleView = new BundleRuleView (this);
		}
	}

	void OnGUI(){
		if (m_bundleBuildView != null) {
			m_bundleBuildView.Draw (new Rect (0, 10, position.width, position.height - 10));
			m_bundleRuleView.Draw (new Rect (0 , 40, position.width, position.height - 40));
		}
	}

	void OnDisable(){
		
	}

	void OnDestroy(){
		if (onWinDisable != null) {
			onWinDisable ();
		}
	}

	private BundleBuildView m_bundleBuildView = null;
	private BundleRuleView m_bundleRuleView = null ;
	public System.Action onWinDisable = null ;


//	[MenuItem("test/testReplacBuiltIn")]
//	public static void ReplaceBuiltIn(){
//		string unityPath = "Assets/prefab/Cube.prefab";
//		string windowsPath = GetWindowsPath (unityPath);
//
//		StreamReader sr = new StreamReader( windowsPath , Encoding.Default);
//		string content = sr.ReadToEnd();
//		sr.Close ();
//
//		Object obj = AssetDatabase.LoadMainAssetAtPath (unityPath);
//		Material mat = (obj as GameObject).GetComponent<MeshRenderer> ().sharedMaterial;
//		long fileId = (mat as Object).GetFileID ();
//		string guid = AssetDatabase.AssetPathToGUID (AssetDatabase.GetAssetPath (mat as Object));
//		Debug.Log ("file id " + fileId + "  guidi" + guid);
//
//
//		Dictionary<string , GUIIDAndFileId> tempDic = new Dictionary<string, GUIIDAndFileId> ();
//		string extraUnityPath = "Assets/builtInExtra";
//		string extraWindowsPath = GetWindowsPath (extraUnityPath);
//		string[] filePaths = Directory.GetFiles(extraWindowsPath , "*.*" , SearchOption.AllDirectories) ;
//		foreach (var filePath in filePaths) {
//			if (filePath.EndsWith (".meta"))
//				continue;
//			string builtInUnityPath = GetUnityPath (filePath);
//			Object builtInObj = AssetDatabase.LoadMainAssetAtPath (builtInUnityPath);
//			tempDic.Add (builtInObj.name, new GUIIDAndFileId{
//				guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(builtInObj)),
//				fileid = builtInObj.GetFileID()
//			});
//		}
//
//		if (tempDic.ContainsKey (mat.name)) {
//			GUIIDAndFileId ids = tempDic [mat.name];
//			Debug.Log (ids.guid + "   " + ids.fileid);
//			content = content.Replace (fileId.ToString(), ids.fileid.ToString());
////			Debug.Log ("str " + str);
//			content = content.Replace (guid, ids.guid);
//			FileStream fs = File.Open (windowsPath, FileMode.OpenOrCreate);
//			StreamWriter sw = new StreamWriter (fs);
//			sw.Write (content);
//			sw.WriteLine ("aaaaaaaaaaaaaaaaaa");
//			sw.Close ();
//			fs.Close ();
//		} else {
//			Debug.Log ("temp dic" + tempDic.Count);
//			Debug.Log ("not contains : " + mat.name);
//		}
//		AssetDatabase.SaveAssets ();
//		StreamReader sr2 = new StreamReader (windowsPath);
//		Debug.Log (sr2.ReadToEnd ());
//		sr2.Close ();
//	}
	private  static string GetWindowsPath(string path){
		return Application.dataPath.Replace ("Assets", path);
	}

	private static  string GetUnityPath(string path){
		return "Assets" + path.Replace (Application.dataPath, "");
	}
}

public static class TableStyles
{
	public static GUIStyle Toolbar = "Toolbar";
	public static GUIStyle ToolbarButton = "ToolbarButton";
	
	public static GUIStyle TextField = "TextField";
}

public class TestScriptable : ScriptableObject{
	public int i = 0;
}