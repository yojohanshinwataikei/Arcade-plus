using System;
using System.IO;
using UnityEditor;
using Arcade.Compose;
using UnityEngine;

public class ArcadeBuild
{
	[MenuItem("Arcade/Test build")]
	public static void TestBuild()
	{
		UnityEngine.Debug.Log(BuildPipeline.BuildPlayer(
			 new BuildPlayerOptions()
			 {
				 locationPathName = "Build/x64/Arcade-plus.exe",
				 scenes = new string[] { "Assets/_Scenes/ArcEditor.unity" },
				 target = BuildTarget.StandaloneWindows64,
				 options = BuildOptions.None,
			 }).summary.result.ToString());
	}
	[MenuItem("Arcade/UpdateBuildTime")]
	public static void UpdateBuildTime()
	{
		DateTime buildTime = DateTime.Now;
		//Note: seems pwd is project folder, however this is not documented
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Misc/BuildTimestamp.txt"), $"{buildTime.Ticks}");
		AssetDatabase.Refresh();
	}
	[MenuItem("Arcade/Build")]
	public static void Build()
	{
		BuildArcade(false);
	}
	[MenuItem("Arcade/Build and zip")]
	public static void BuildAndZip()
	{
		BuildArcade(true);
	}
	public static void BuildArcade(bool createZipPackage)
	{
		UpdateBuildTime();
		FileUtil.DeleteFileOrDirectory("Build");
		UnityEngine.Debug.Log(BuildPipeline.BuildPlayer(
			 new BuildPlayerOptions()
			 {
				 locationPathName = "Build/x86/Arcade-plus.exe",
				 scenes = new string[] { "Assets/_Scenes/ArcEditor.unity" },
				 target = BuildTarget.StandaloneWindows,
				 options = BuildOptions.None,
			 }).summary.result.ToString());
		UnityEngine.Debug.Log(BuildPipeline.BuildPlayer(
			 new BuildPlayerOptions()
			 {
				 locationPathName = "Build/x64/Arcade-plus.exe",
				 scenes = new string[] { "Assets/_Scenes/ArcEditor.unity" },
				 target = BuildTarget.StandaloneWindows64,
				 options = BuildOptions.None,
			 }).summary.result.ToString());
		UnityEngine.Debug.Log(BuildPipeline.BuildPlayer(
			 new BuildPlayerOptions()
			 {
				 locationPathName = "Build/mac/Arcade-plus.app",
				 scenes = new string[] { "Assets/_Scenes/ArcEditor.unity" },
				 target = BuildTarget.StandaloneOSX,
				 options = BuildOptions.None,
			 }).summary.result.ToString());
		UnityEngine.Debug.Log(BuildPipeline.BuildPlayer(
			 new BuildPlayerOptions()
			 {
				 locationPathName = "Build/linux/Arcade-plus",
				 scenes = new string[] { "Assets/_Scenes/ArcEditor.unity" },
				 target = BuildTarget.StandaloneLinux64,
				 options = BuildOptions.None,
			 }).summary.result.ToString());
		// TODO: Find a way to use the System.IO.Compression.ZipFile without breaking things
		FileUtil.CopyFileOrDirectory("Skin", "Build/x64/Skin");
		FileUtil.CopyFileOrDirectory("Background", "Build/x64/Background");
	}
}

