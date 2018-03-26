using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class BundleBuildConfig  {

	public static BuildTarget BuildTarget
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

	public static bool ForceBuild
	{
		get { return EditorPrefs.GetBool("ABBuild.forceBuild"); }
		set { EditorPrefs.SetBool("ABBuild.forceBuild", value); }
	}

	public const string CommonBundleName = "common" ;
	public const string ShaderBundleName = "shader" ;
	/// <summary>
	/// 内置资源解压系统目录
	/// </summary>
	/// <value>The built extra path.</value>
	public static string BuiltExtraSysPath {
		get {return Application.dataPath +"/../" + BuiltExtraFolderName;  ;}
	}
	/// <summary>
	/// 内置资源解压Unity目录
	/// </summary>
	/// <value>The built extra asset path.</value>
	public static string BuiltExtraAssetPath {
		get { return Application.dataPath + "/" + BuiltExtraFolderName; }
	}

	public static bool isReplaceBuiltInRes {
		get{
			return EditorPrefs.GetBool ("BundleBuild.isReplaceBuiltInRes" , true);
		}
		set{
			EditorPrefs.SetBool ("BundleBuild.isReplaceBuiltInRes", value);
		}
	}

	public const string BuiltExtraFolderName = "builtInExtra";

	public static int VersionNum{
		get{ return EditorPrefs.GetInt ("BundleBuild.Version", 1);}
		set{ EditorPrefs.SetInt ("BundleBuild.Version", value);}
	}
}

public enum CompressOption
{
	Uncompressed,
	StandardCompression,
	ChunkBasedCompression
}
