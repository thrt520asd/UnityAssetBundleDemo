using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class BundleBuildConfig  {

	public static BuildTarget buildTarget
	{
		get { return EditorPrefs.HasKey("ABBuild.buildTarget") ? (BuildTarget)EditorPrefs.GetInt("ABBuild.buildTarget") : BuildTarget.StandaloneWindows; }
		set { EditorPrefs.SetInt("ABBuild.buildTarget", (int)value); }
	}

	public static string outputPath
	{
		get { return EditorPrefs.HasKey("ABBuild.outputPath") ? EditorPrefs.GetString("ABBuild.outputPath") : "AssetBundles"; }
		set { EditorPrefs.SetString("ABBuild.outputPath", value); }
	}

	public static CompressOption compressOption
	{
		get { return EditorPrefs.HasKey("ABBuild.compressOption") ? (CompressOption)EditorPrefs.GetInt("ABBuild.compressOption") : CompressOption.ChunkBasedCompression; }
		set { EditorPrefs.SetInt("ABBuild.compressOption", (int)value); }
	}

	public static bool forceBuild
	{
		get { return EditorPrefs.GetBool("ABBuild.forceBuild"); }
		set { EditorPrefs.SetBool("ABBuild.forceBuild", value); }
	}

	public const string CommonBundleName = "common" ;
	public const string ShaderBundleName = "shader" ;
	public static string BuiltExtraPath {
		get {return Application.dataPath +"/../" + BuiltExtraFolderName;  ;}
	}

	public static string BuiltExtraAssetPath {
		get { return Application.dataPath + "/" + BuiltExtraFolderName; }
	}

	public const string BuiltExtraFolderName = "builtInExtra";
}

public enum CompressOption
{
	Uncompressed,
	StandardCompression,
	ChunkBasedCompression
}
