using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public static class EditorTools  {
//	public void CopyFileOrDir(string resPath , string parentPath){
//		if (Directory.Exists (resPath)) {
//			
//		}
//	}

	public static void CopyDir(string dir ,string parentDir , bool isAppend = false){
		if (!Directory.Exists (dir)) {
			Debug.Log ("dir not exit :" + dir + "   please check your input");
			return;
		}

		if (!Directory.Exists (parentDir)) {
			Directory.CreateDirectory (parentDir);
		}
		DirectoryInfo di = new DirectoryInfo (dir);
		string pathName = di.Name;
		string newDirFullPath = parentDir + "/" + pathName;
		if (!isAppend && Directory.Exists(newDirFullPath)) {
			Directory.Delete (newDirFullPath);
		}
		if (!Directory.Exists (newDirFullPath)) {
			Directory.CreateDirectory (newDirFullPath);
		}
		DirectoryInfo[] dirInfos = di.GetDirectories ();
		foreach (var dirInfo in dirInfos) {
			string newSubDirFullPath = dirInfo.FullName.Replace (dir, parentDir);
			if (!Directory.Exists (newSubDirFullPath)) {		
				Directory.CreateDirectory (newSubDirFullPath);
			}
		}

		FileInfo[] fileInfos = di.GetFiles ("*.*", SearchOption.AllDirectories);
		foreach (var fileInfo in fileInfos) {
			if (fileInfo.FullName.EndsWith (".meta")) {
				continue;
			}
			string newFileFullPath = fileInfo.FullName.Replace (dir, parentDir);

			File.Copy (fileInfo.FullName, newFileFullPath, true);
		}
	}

	public  static string GetWindowsPath(string path){
		return Application.dataPath.Replace ("Assets", path);
	}

	public static  string GetUnityPath(string path){
		return "Assets" + path.Replace (Application.dataPath, "");
	}
}
