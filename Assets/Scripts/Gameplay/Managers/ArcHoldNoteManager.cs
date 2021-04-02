using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;

namespace Arcade.Gameplay
{
	public class ArcHoldNoteManager : MonoBehaviour
	{
		public static ArcHoldNoteManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}

		[HideInInspector]
		public List<ArcHold> Holds = new List<ArcHold>();
		[HideInInspector]
		public readonly float[] Lanes = { 6.375f, 2.125f, -2.125f, -6.375f };
		public GameObject HoldNotePrefab;
		public Transform NoteLayer;
		public Sprite DefaultSprite, HighlightSprite;
		public Material HoldNoteMatrial;

		public void Clean()
		{
			foreach (var t in Holds) t.Destroy();
			Holds.Clear();
		}
		public void Load(List<ArcHold> holds)
		{
			Holds = holds;
			foreach (var t in Holds)
			{
				t.Instantiate();
			}
		}

		public void Add(ArcHold hold)
		{
			hold.Instantiate();
			Holds.Add(hold);
		}
		public void Remove(ArcHold hold)
		{
			hold.Destroy();
			Holds.Remove(hold);
		}

		private void Update()
		{
			if (Holds == null) return;
			if (ArcGameplayManager.Instance.Auto) JudgeHoldNotes();
			RenderHoldNotes();
		}

		private void RenderHoldNotes()
		{
			ArcTimingManager timing = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;

			foreach (var t in Holds)
			{
				int duration = t.EndTiming - t.Timing;
				if (!timing.ShouldTryRender(t.Timing + offset, t.TimingGroup, duration) || t.Judged || t.GroupHide())
				{
					t.Enable = false;
					continue;
				}
				t.Position = timing.CalculatePositionByTiming(t.Timing + offset, t.TimingGroup);
				float endPosition = timing.CalculatePositionByTiming(t.EndTiming + offset, t.TimingGroup);
				t.Enable = true;
				if (t.Judging)
				{
					t.Position = 0;
				}
				if (endPosition < t.Position)
				{
					var p = t.Position;
					t.Position = endPosition;
					endPosition = p;
				}
				if (t.Position > 100000 || endPosition < -100000)
				{
					t.Enable = false;
					continue;
				}
				if (endPosition > 100000)
				{
					endPosition = 100000;
				}
				if (t.Position < -100000)
				{
					t.Position = -100000;
				}
				float pos = t.Position / 1000f;
				float length = (endPosition - t.Position) / 1000f;
				t.transform.localPosition = new Vector3(Lanes[t.Track - 1], pos, 0);
				t.transform.localScale = new Vector3(1.53f, length / 3.79f, 1);
				t.boxCollider.center = new Vector3(0, t.boxCollider.size.y / 2);

				float alpha = 1;
				if (t.Judging)
				{
					t.FlashCount = (t.FlashCount + 1) % 4;
					if (t.FlashCount == 0) alpha = 0.85f;
					t.Highlight = true;
				}
				else
				{
					alpha = t.Timing + offset < ArcGameplayManager.Instance.Timing ? 0.5f : 1;
					t.Highlight = false;
				}
				t.Alpha = alpha * 0.8627451f;
			}
		}
		private void JudgeHoldNotes()
		{
			ArcTimingManager timing = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;
			int currentTiming = ArcGameplayManager.Instance.Timing;
			ArcEffectManager.Instance.ResetHoldNoteEffect();
			foreach (var t in Holds)
			{
				if (t.NoInput())
				{
					continue;
				}
				if (t.Judged) continue;
				if (currentTiming >= t.Timing + offset && currentTiming <= t.EndTiming + offset)
				{
					t.Judging = true;
					if (!t.AudioPlayed)
					{
						if (ArcGameplayManager.Instance.IsPlaying && t.ShouldPlayAudio) ArcEffectManager.Instance.PlayTapSound();
						t.AudioPlayed = true;
					}
					ArcEffectManager.Instance.SetHoldNoteEffect(t.Track, true);
				}
				else if (currentTiming > t.EndTiming + offset)
				{
					t.Judging = false;
					t.Judged = true;
					t.AudioPlayed = true;
				}
				else
				{
					t.ShouldPlayAudio = true;
				}
			}
		}
		public void SetHoldNoteSkin(Sprite normal, Sprite highlight)
		{
			HoldNoteMatrial.mainTexture = normal.texture;
			HoldNotePrefab.GetComponent<SpriteRenderer>().sprite = normal;
			DefaultSprite = normal;
			HighlightSprite = highlight;
			foreach (var h in Holds) h.ReloadSkin();
		}
	}
}
