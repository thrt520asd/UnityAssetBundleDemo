using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleBuildView  {

	string outputPath = "" ;
	BuildTarget buildTarget ;
	CompressOption compressOption ;
	bool forceBuild  ; 
	public void Draw(Rect r){
		GUILayout.BeginHorizontal (TableStyles.Toolbar);
		{
			outputPath = EditorGUILayout.TextField( "outputPath" , outputPath  ,TableStyles.TextField , GUILayout.Width(250));
			if (GUILayout.Button("Brower" , TableStyles.ToolbarButton , GUILayout.MaxWidth(120) )){
				string result = EditorUtility.OpenFolderPanel("", "选择目录", "");
				if (result != null)
				{
					outputPath = GetAssetPath(result);
					GUI.FocusControl(null);
				}
			}
			compressOption = (CompressOption)EditorGUILayout.EnumPopup ("CompressOption", compressOption, GUILayout.Width (300));
//			BuildAssetBundleOptions.ForceRebuildAssetBundle
			buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", buildTarget , GUILayout.Width(250));
			forceBuild = (bool)EditorGUILayout.Toggle ("ForceRebuildAssetBundle", forceBuild);

			if (GUILayout.Button("Build", TableStyles.ToolbarButton , GUILayout.MinWidth(120))){
				BundleBuilder.BuildBundle(outputPath , compressOption , buildTarget , forceBuild);
			}
		}
		GUILayout.EndHorizontal ();
	}

	public BundleBuildView(EditorWindow  hostWindow){
		outputPath = BundleBuildConfig.outputPath;
		compressOption = BundleBuildConfig.compressOption;
		buildTarget = BundleBuildConfig.buildTarget;
		forceBuild = BundleBuildConfig.forceBuild;
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
