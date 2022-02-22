using System;
using UnityEngine;
using Arcade.Gameplay.Chart;
using Arcade.Gameplay;
using UnityEngine.InputSystem;

namespace Arcade.Compose.Editing
{
	public class AdeTimingSelector : MonoBehaviour
	{
		public static AdeTimingSelector Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}

		public bool Enable { get; private set; }

		private ArcNote note;
		private Action<int> currentSetter;

		private void Update()
		{
			if (!Enable) return;
			UpdateTiming();
		}
		public void ModifyNote(ArcNote note, Action<int> setter)
		{
			Enable = true;
			this.note = note;
			currentSetter = setter;
			if (note is ArcArc) AdeCursorManager.Instance.EnableVerticalPanel = true;
		}
		private void UpdateTiming()
		{
			if (!AdeCursorManager.Instance.IsHorizontalHit) return;
			Vector3 pos = AdeCursorManager.Instance.AttachedHorizontalPoint;
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			int timing = ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000, timingGroup) - ArcAudioManager.Instance.AudioOffset;
			if (note is ArcLongNote)
			{
				var longNote = note as ArcLongNote;
				if (timing < longNote.Timing)
				{
					return;
				}
			}
			currentSetter?.Invoke(timing);
			if (Mouse.current.leftButton.wasPressedThisFrame) EndModify();
		}
		public void EndModify()
		{
			EndOfFrame.Instance.Listeners.AddListener(() =>
			{
				Enable = false;
				note = null;
				currentSetter = null;
				AdeCursorManager.Instance.EnableVerticalPanel = false;
			});
		}
	}
}
