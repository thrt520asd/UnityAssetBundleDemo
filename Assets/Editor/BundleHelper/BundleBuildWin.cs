using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor ;
using System.IO;
using System.Text;

public class BundleBuildWin : EditorWindow {
	
	[MenuItem("BundleBuild/快捷打包打包AB" , false  , 0)]
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

	void OnEnable(){
		if (m_bundleBuildView == null) {
			m_bundleBuildView = new BundleBuildView (this);
//			m_bundleRuleView = new BundleRuleView (this);
		}
	}

	void OnGUI(){
		if (m_bundleBuildView != null) {
			m_bundleBuildView.Draw (new Rect (0, 10, position.width, position.height - 10));
//			m_bundleRuleView.Draw (new Rect (0 , 40, position.width, position.height - 40));
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