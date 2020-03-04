using UnityEngine;
using UnityEditor;
using Arcade.Gameplay;

[CustomEditor(typeof(ArcGameplayManager))]
public class ArcGameplayManagerEditor : Editor
{
	private AudioClip clip;
	private string affPath;
	private int setTiming = 0;
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (!Application.isPlaying) return;
		GUILayout.Label("Editor", new GUIStyle() { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
		if (GUILayout.Button("Init"))
		{
			ArcCameraManager.Instance.ResetCamera();
		}
		clip = EditorGUILayout.ObjectField("Clip", clip, typeof(AudioClip), true) as AudioClip;
		affPath = EditorGUILayout.TextField("Aff", affPath);
		if (GUILayout.Button("Load"))
		{
			ArcGameplayManager.Instance.Load(new Arcade.Gameplay.Chart.ArcChart(new Arcade.Aff.ArcaeaAffReader(affPath)), clip);
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Play"))
		{
			ArcGameplayManager.Instance.Play();
		}
		if (GUILayout.Button("Pause"))
		{
			ArcGameplayManager.Instance.Pause();
		}
		if (GUILayout.Button("Stop"))
		{
			ArcGameplayManager.Instance.Stop();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Timing");
		GUILayout.Label(ArcGameplayManager.Instance.Timing.ToString());
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Set Timing");
		setTiming = EditorGUILayout.IntField(setTiming);
		if (GUILayout.Button("Set")) ArcGameplayManager.Instance.Timing = setTiming;
		GUILayout.EndHorizontal();
		Repaint();
	}
}
