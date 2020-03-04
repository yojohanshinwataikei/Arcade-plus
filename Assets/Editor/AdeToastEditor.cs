using UnityEngine;
using UnityEditor;
using Arcade.Compose;

[CustomEditor(typeof(AdeToast))]
public class AdeToastEditor : Editor
{
    private string text;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        text = GUILayout.TextArea(text);
        if (GUILayout.Button("Send")) AdeToast.Instance.Show(text);
    }
}
