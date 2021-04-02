using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;

namespace Arcade.Gameplay
{
	public class ArcTapNoteManager : MonoBehaviour
	{
		public static ArcTapNoteManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}

		[HideInInspector]
		public List<ArcTap> Taps = new List<ArcTap>();
		[HideInInspector]
		public readonly float[] Lanes = { 6.375f, 2.125f, -2.125f, -6.375f };
		public GameObject TapNotePrefab;
		public Transform NoteLayer;
		public Material ShaderdMaterial;

		public void Clean()
		{
			foreach (var t in Taps) t.Destroy();
			Taps.Clear();
		}
		public void Load(List<ArcTap> taps)
		{
			Taps = taps;
			foreach (var t in Taps)
			{
				t.Instantiate();
			}
		}

		public void Add(ArcTap tap)
		{
			tap.Instantiate();
			Taps.Add(tap);
			tap.SetupArcTapConnection();
		}
		public void Remove(ArcTap tap)
		{
			tap.Destroy();
			Taps.Remove(tap);
		}

		private void Update()
		{
			if (Taps == null) return;
			if (ArcGameplayManager.Instance.Auto) JudgeTapNotes();
			RenderTapNotes();
		}

		private void RenderTapNotes()
		{
			ArcTimingManager timing = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;

			foreach (var t in Taps)
			{
				if (!timing.ShouldTryRender(t.Timing + offset, t.TimingGroup) || t.Judged || t.GroupHide())
				{
					t.Enable = false;
					continue;
				}
				t.Position = timing.CalculatePositionByTiming(t.Timing + offset, t.TimingGroup);
				if (t.Position > 100000 || t.Position < -100000)
				{
					t.Enable = false;
					continue;
				}
				t.Enable = true;
				float pos = t.Position / 1000f;
				t.transform.localPosition = new Vector3(Lanes[t.Track - 1], pos, 0);
				if (ArcCameraManager.Instance.EditorCamera)
					t.transform.localScale = new Vector3(1.53f, 2, 1);
				else
					t.transform.localScale = new Vector3(1.53f, 2f + 5.1f * pos / 100f, 1);
				t.Alpha = pos < 90 ? 1 : (100 - pos) / 10f;
			}
		}
		private void JudgeTapNotes()
		{
			ArcTimingManager timing = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;
			int currentTiming = ArcGameplayManager.Instance.Timing;
			foreach (var t in Taps)
			{
				if (t.NoInput())
				{
					continue;
				}
				if (t.Judged) continue;
				if (currentTiming > t.Timing + offset && currentTiming <= t.Timing + offset + 150)
				{
					t.Judged = true;
					if (ArcGameplayManager.Instance.IsPlaying) ArcEffectManager.Instance.PlayTapNoteEffectAt(new Vector2(Lanes[t.Track - 1], 0));
				}
				else if (currentTiming > t.Timing + offset + 150)
				{
					t.Judged = true;
				}
			}
		}
		public void SetTapNoteSkin(Sprite sprite)
		{
			//Note: I have no idea why spriterenderer do not update when I set the sprite,
			// as a workaround, we set the texture
			// I do not know if this bug disappear in the build
			ShaderdMaterial.mainTexture = sprite.texture;
			TapNotePrefab.GetComponent<SpriteRenderer>().sprite = sprite;
			foreach (var t in Taps) t.spriteRenderer.sprite = sprite;
		}
		public void SetConnectionLineColor(Color color)
		{
			ArcArcManager.Instance.ConnectionColor = color;
			foreach (var t in Taps)
				foreach (var l in t.ConnectionLines.Values)
					l.startColor = l.endColor = color;
		}
	}
}
