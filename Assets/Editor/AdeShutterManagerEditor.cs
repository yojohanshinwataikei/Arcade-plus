using UnityEngine;
using UnityEditor;
using Arcade.Compose;

[CustomEditor(typeof(AdeShutterManager))]
public class AdeShutterManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!Application.isPlaying) return;
        GUILayout.Label("Editor");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open"))
        {
            AdeShutterManager.Instance.Open();
        }
        if (GUILayout.Button("Close"))
        {
            AdeShutterManager.Instance.Close();
        }
        GUILayout.EndHorizontal();
    }
}

