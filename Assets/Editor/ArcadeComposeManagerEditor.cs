using UnityEngine;
using UnityEditor;
using Arcade.Compose;

[CustomEditor(typeof(ArcadeComposeManager))]
public class ArcadeComposeManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (!Application.isPlaying) return;
		GUILayout.Label("Editor");
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Play"))
		{
			ArcadeComposeManager.Instance.Play();
		}
		if (GUILayout.Button("Pause"))
		{
			ArcadeComposeManager.Instance.Pause();
		}
		GUILayout.EndHorizontal();
	}
}

