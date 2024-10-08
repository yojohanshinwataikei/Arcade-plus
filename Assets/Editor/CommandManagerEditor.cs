using UnityEngine;
using UnityEditor;
using Arcade.Compose.Command;

[CustomEditor(typeof(AdeCommandManager))]
public class CommandManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Undo")) AdeCommandManager.Instance.Undo();
		if (GUILayout.Button("Redo")) AdeCommandManager.Instance.Redo();
		EditorGUILayout.EndHorizontal();
	}
}

