using UnityEngine;
using UnityEditor;
using Arcade.Gameplay;

[CustomEditor(typeof(ArcEffectManager))]
public class ArcEffectManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (!Application.isPlaying) return;
		GUILayout.Label("Loaded special effect audios:");
		foreach (var key in ArcEffectManager.Instance.SpecialEffectAudios.Keys)
		{
			GUILayout.Label($"    {key}");
		}
	}
}

