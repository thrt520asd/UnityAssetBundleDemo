using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor ;
using System.IO;
using System ;
using Object = UnityEngine.Object;
public class ExportBuiltInResTools {

	const string BUILT_IN_RES_PATH = "Assets/builtInExtra";

	[MenuItem("BundleBuild/导出内置资源")]
	static void ExportBuildInRes(){
		List<Object> buildInAssets = new List<Object> ();
		Object[] resAssets = AssetDatabase.LoadAllAssetsAtPath ("Resources/unity_builtin_extra");
		Object[] libAssets = AssetDatabase.LoadAllAssetsAtPath ("Library/unity default resources");
		buildInAssets.AddRange (resAssets);
		buildInAssets.AddRange (libAssets);
		Dictionary<Type , int> typeDic = new Dictionary<Type, int> ();
		Dictionary<Type , ExportHandle> exportHandleDic = new Dictionary<Type, ExportHandle> ();
		exportHandleDic [typeof(Material)] = ExportMat;
		exportHandleDic [typeof(Texture2D)] = ExportTexture;
		exportHandleDic [typeof(Sprite)] = ExportSprite;
		exportHandleDic [typeof(Mesh)] = ExportMesh;

		foreach (var item in buildInAssets) {
			int count = 0;
			Type t = item.GetType ();
			typeDic .TryGetValue(item.GetType() , out count);
			typeDic [item.GetType ()] = ++count;
			ExportHandle handle;
			if (exportHandleDic.TryGetValue (t, out handle)) {
				handle (item);
			}
		}
//		foreach (var item in typeDic) {
//			Debug.Log (item.Key.ToString () + "/" + item.Value);
//		}
//		Texture2D b =  AssetDatabase.GetBuiltinExtraResource<Texture>();

		AssetDatabase.Refresh ();
	}

	public delegate void ExportHandle(Object obj);

	private static void ExportMat(Object obj){
		string parentFolderPath = BUILT_IN_RES_PATH + "/Mat";
		if (!Directory.Exists (parentFolderPath)) {
			Directory.CreateDirectory (parentFolderPath);
		}
		Material mat = Material.Instantiate (obj as Material);
		if (mat != null) {
			string path = parentFolderPath + "/" + obj.name + ".mat";
			Material asset =  AssetDatabase.LoadAssetAtPath <Material>(path);
			if (asset == null) {
				AssetDatabase.CreateAsset (mat, parentFolderPath + "/" + obj.name + ".mat");
			}
		}
	}

	private static void ExportTexture(Object obj){
		Debug.Log ("目前不支持texture 和 sprite , 建议不要使用内置texture 和sprite");//其实是没有解析才出来 日了狗了 
		return;
//		string parentFolderPath = BUILT_IN_RES_PATH + "/Tex";
//		if (!Directory.Exists (parentFolderPath)) {
//			Directory.CreateDirectory (parentFolderPath);
//		}
	}

	private static void ExportMesh(Object obj){
		string parentFolderPath = BUILT_IN_RES_PATH + "/Mesh";
		if (!Directory.Exists (parentFolderPath)) {
			Directory.CreateDirectory (parentFolderPath);
		}
		Mesh mesh = Mesh.Instantiate (obj as Mesh);
		AssetDatabase.CreateAsset (mesh, parentFolderPath + "/" + obj.name + ".asset");
	}

	private static void ExportSprite(Object obj){
		Debug.Log ("目前不支持texture 和 sprite , 建议不要使用内置texture 和sprite");
		return;
//		string parentFolderPath = BUILT_IN_RES_PATH + "/Sprite";
//		if (!Directory.Exists (parentFolderPath)) {
//			Directory.CreateDirectory (parentFolderPath);
//		}
	}
}

