using System;
using System.IO;
using UnityEditor;
using Arcade.Compose;
using UnityEngine;

public class ArcadeBuild
{
	[MenuItem("Arcade/UpdateBuildTime")]
	public static void UpdateBuildTime(){
		DateTime buildTime = DateTime.Now;
		//Note: seems pwd is project folder, however this is not documented
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Misc/BuildTimestamp.txt"), $"{buildTime.Ticks}");
		AssetDatabase.Refresh();
    }
	[MenuItem("Arcade/Build")]
	public static void BuildArcade()
	{
        UpdateBuildTime();
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
	}
}
