using UnityEngine;
using UnityEditor;
using Arcade.Compose.Command;

[CustomEditor(typeof(CommandManager))]
public class CommandManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Undo")) CommandManager.Instance.Undo();
        if (GUILayout.Button("Redo")) CommandManager.Instance.Redo();
        EditorGUILayout.EndHorizontal();
    }
}
