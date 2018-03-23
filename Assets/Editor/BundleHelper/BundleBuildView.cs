using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleBuildView  {

	string outputPath = "" ;
//	BuildTarget buildTarget ;
//	CompressOption compressOption ;
//	bool forceBuild  ; 
//	bool replaceBuiltInRes;
	int version ;
	public void Draw(Rect r){
		GUILayout.BeginHorizontal (TableStyles.Toolbar);
		{
			outputPath = EditorGUILayout.TextField( "outputPath" , outputPath  ,TableStyles.TextField , GUILayout.MinWidth(250));
			if (GUILayout.Button("Brower" , TableStyles.ToolbarButton , GUILayout.MaxWidth(120) )){
				string result = EditorUtility.OpenFolderPanel("", "选择目录", "");
				if (result != null)
				{
					outputPath = GetAssetPath(result);
					GUI.FocusControl(null);
				}
			}
			version = int.Parse (EditorGUILayout.TextField ("version", version.ToString(), TableStyles.TextField, GUILayout.Width (250)));
			if (GUILayout.Button("Build", TableStyles.ToolbarButton , GUILayout.MaxWidth(120))){
				BundleBuilder.BuildBundle(version , outputPath , BundleBuildConfig.compressOption , BundleBuildConfig.BuildTarget , BundleBuildConfig.ForceBuild);
			}
		}
		GUILayout.EndHorizontal ();
		GUILayout.BeginHorizontal (TableStyles.Toolbar);
		{
			BundleBuildConfig.BuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", BundleBuildConfig.BuildTarget );
			BundleBuildConfig.compressOption = (CompressOption)EditorGUILayout.EnumPopup ("CompressOption", BundleBuildConfig.compressOption);
		}
		GUILayout.EndHorizontal ();
		GUILayout.BeginHorizontal (TableStyles.Toolbar);
		{
			BundleBuildConfig.ForceBuild = (bool)EditorGUILayout.Toggle ("ForceRebuild", BundleBuildConfig.ForceBuild );
			BundleBuildConfig.isReplaceBuiltInRes = (bool)EditorGUILayout.Toggle ("替换内置资源", BundleBuildConfig.isReplaceBuiltInRes );
			
		}
		GUILayout.EndHorizontal ();
	}

	public BundleBuildView(EditorWindow  hostWindow){
		outputPath = BundleBuildConfig.outputPath;
//		compressOption = BundleBuildConfig.compressOption;
//		buildTarget = BundleBuildConfig.BuildTarget;
//		forceBuild = BundleBuildConfig.ForceBuild;
		version = BundleBuildConfig.VersionNum;
	}
		
	private string GetAssetPath(string result)
	{
		if (result.StartsWith(Application.dataPath))
			return result == Application.dataPath ? "" : result.Substring(Application.dataPath.Length + 1);
		else if (result.StartsWith("Assets"))
			return result == "Assets" ? "" : result.Substring("Assets/".Length);
		return null;
	}
}
