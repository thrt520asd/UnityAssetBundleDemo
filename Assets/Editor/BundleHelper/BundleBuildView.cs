using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleBuildView  {

	private BundleRuleView m_ruleView = null;

	string outputPath = "" ;
	int version ;
	bool isUseNowVersion = false ;
	public void Draw(Rect r){
		GUILayout.BeginHorizontal (TableStyles.Toolbar);
		{
			GUILayout.Label ("当前版本号 : " + version, TableStyles.TextField , GUILayout.Width(200));
			version = int.Parse (EditorGUILayout.TextField ("version", version.ToString(), TableStyles.TextField, GUILayout.Width (250)));
			isUseNowVersion = GUILayout.Toggle (isUseNowVersion , "是否重用当前版本" );

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
		m_ruleView.Draw (new Rect (0, 0, 0, 0));
		GUILayout.BeginHorizontal ();
		{
			if (GUILayout.Button("Build" , GUILayout.Height(50))){
				if (BundleBuildConfig.VersionNum == version && !isUseNowVersion) {
					EditorUtility.DisplayDialog ("版本号重复， 打包失败", "版本号重复， 打包失败", "ok");
					return;
				}
				BundleBuilder.BuildBundle(version , outputPath , BundleBuildConfig.compressOption , BundleBuildConfig.BuildTarget , BundleBuildConfig.ForceBuild);
			}

		}
		GUILayout.EndHorizontal ();
	}

	public BundleBuildView(EditorWindow  hostWindow){
		outputPath = BundleBuildConfig.outputPath;
		version = BundleBuildConfig.VersionNum;
		m_ruleView = new BundleRuleView (hostWindow);
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
