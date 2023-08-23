using System.Collections;
using System.Collections.Generic;
using Arcade.Compose.Command;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Compose.Editing
{
	public enum ClickToCreateMode
	{
		Idle = 0,
		Tap = 1,
		Hold = 2,
		Arc = 3,
		ArcTap = 4
	}
	public class AdeClickToCreate : MonoBehaviour, IMarkingMenuItemProvider, INoteSelectEvent
	{
		public static AdeClickToCreate Instance { get; private set; }

		public MarkingMenuItem Delete;
		public MarkingMenuItem[] Entry;
		public MarkingMenuItem[] ClickToCreateItems;

		public bool IsOnly => enable;
		public bool Enable
		{
			get
			{
				return enable;
			}
			set
			{
				if (value && !ArcGameplayManager.Instance.IsLoaded)
				{
					AdeToast.Instance.Show("请先加载谱面");
					return;
				}
				if (enable != value)
				{
					CancelAddLongNote();
					// AdeCursorManager.Instance.Mode = value ? CursorMode.Track : CursorMode.Idle;
					if (!value)
					{
						selectedArc = null;
					}
					Mode = ClickToCreateMode.Idle;
					enable = value;
				}
			}
		}
		public MarkingMenuItem[] Items
		{
			get
			{
				// if (!AdeOperationManager.Instance.HasOngoingOperation) return SelectingValueItems;
				if (!enable) return Entry;
				else
				{
					List<MarkingMenuItem> items = new List<MarkingMenuItem>();
					items.AddRange(ClickToCreateItems);
					if (AdeSelectionManager.Instance.SelectedNotes.Count != 0)
					{
						items.Add(Delete);
					}
					return items.ToArray();
				}
			}
		}
		private ClickToCreateMode mode;
		public ClickToCreateMode Mode
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return ClickToCreateMode.Idle;
				// if (AdeTimingSelector.Instance.Enable) return ClickToCreateMode.Idle;
				// if (AdePositionSelector.Instance.Enable) return ClickToCreateMode.Idle;
				return mode;
			}
			set
			{
				if (mode != value)
				{
					CancelAddLongNote();
					mode = value;
				}
				if (mode == ClickToCreateMode.Idle)
				{
					AdeCursorManager.Instance.ArcTapCursorEnabled = false;
					// AdeCursorManager.Instance.EnableWallPanel = false;
				}
			}
		}

		public string CurrentArcColor
		{
			get
			{
				return currentArcColor == 0 ? "蓝" : "红";
			}
		}
		public string CurrentArcIsVoid
		{
			get
			{
				return currentArcIsVoid ? "虚" : "实";
			}
		}
		public string CurrentArcType
		{
			get
			{
				return currentArcType.ToString();
			}
		}

		private bool enable=false;
		private bool skipNextClick;
		private bool canAddArcTap;
		private ArcLongNote pendingNote;
		private ArcArc selectedArc;
		private int currentArcColor;
		private bool currentArcIsVoid;
		private ArcLineType currentArcType = ArcLineType.S;

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			AdeMarkingMenuManager.Instance.Providers.Add(this);
			AdeSelectionManager.Instance.NoteEventListeners.Add(this);
		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
		}
		private void LagecyUpdate()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ToggleClickToCreate))
			{
				Enable = !Enable;
			}
			if (!enable) return;

			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateIdle))
			{
				SetClickToCreateMode(ClickToCreateMode.Idle);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateTap))
			{
				SetClickToCreateMode(ClickToCreateMode.Tap);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateHold))
			{
				SetClickToCreateMode(ClickToCreateMode.Hold);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArc))
			{
				SetClickToCreateMode(ClickToCreateMode.Arc);
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArctap))
			{
				SetClickToCreateMode(ClickToCreateMode.ArcTap);
			}

			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Inputs.Cancel))
			{
				CancelAddLongNote();
			}

			if (Mode == ClickToCreateMode.Idle) return;

			if (Mode == ClickToCreateMode.Arc && postArcCoroutine == null)
			{
				if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArcVoid))
				{
					SwitchIsVoid();
				}
				if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArcColor))
				{
					SwitchColor();
				}
				if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ClickToCreateArcType))
				{
					SwitchType();
				}
			}

			UpdateArcTapCursor();
			if (Mouse.current.leftButton.wasPressedThisFrame)
			{
				AddNoteHandler();
			}
		}

		private void UpdateArcTapCursor()
		{
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			if (selectedArc != null && selectedArc.Instance == null)
			{
				selectedArc = null;
				if (Mode == ClickToCreateMode.ArcTap) Mode = ClickToCreateMode.Idle;
				return;
			}
			if (Mode != ClickToCreateMode.ArcTap || selectedArc == null)
			{
				AdeCursorManager.Instance.ArcTapCursorEnabled = false;
				// AdeCursorManager.Instance.EnableWallPanel = false;
				return;
			}
			Vector3 pos = AdeCursorManager.Instance.AttachedTrackPoint;
			int timing = ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000, timingGroup) - ArcAudioManager.Instance.AudioOffset;
			canAddArcTap = selectedArc.Timing <= timing && selectedArc.EndTiming >= timing;
			AdeCursorManager.Instance.ArcTapCursorEnabled = canAddArcTap;
			AdeCursorManager.Instance.ArcTapCursorIsSfx = selectedArc.IsSfx;
			// AdeCursorManager.Instance.EnableWallPanel = canAddArcTap;
			if (!canAddArcTap) return;
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			float t = 1f * (timing - selectedArc.Timing) / (selectedArc.EndTiming - selectedArc.Timing);
			Vector2 gizmo = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(selectedArc.XStart, selectedArc.XEnd, t, selectedArc.LineType)),
									   ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(selectedArc.YStart, selectedArc.YEnd, t, selectedArc.LineType)) - 0.5f);
			AdeCursorManager.Instance.ArcTapCursorPosition = gizmo;
		}
		private void AddNoteHandler()
		{
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			if (!AdeCursorManager.Instance.IsTrackHit) return;
			if (skipNextClick)
			{
				skipNextClick = false;
				return;
			}
			Vector3 pos = AdeCursorManager.Instance.AttachedTrackPoint;
			int timing = ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000, timingGroup) - ArcAudioManager.Instance.AudioOffset;
			ArcNote note = null;
			switch (Mode)
			{
				case ClickToCreateMode.Tap:
					note = new ArcTap() { Timing = timing, Track = PositionToTrack(pos.x), TimingGroup = timingGroup };
					break;
				case ClickToCreateMode.Hold:
					note = new ArcHold() { Timing = timing, Track = PositionToTrack(pos.x), EndTiming = timing, TimingGroup = timingGroup };
					break;
				case ClickToCreateMode.Arc:
					note = new ArcArc() { Timing = timing, EndTiming = timing, Color = currentArcColor, Effect = "none", IsVoid = currentArcIsVoid, LineType = currentArcType, TimingGroup = timingGroup };
					break;
				case ClickToCreateMode.ArcTap:
					note = new ArcArcTap() { Timing = timing };
					break;
			}
			if (note == null) return;
			switch (Mode)
			{
				case ClickToCreateMode.Tap:
					CommandManager.Instance.Add(new AddArcEventCommand(note));
					break;
				case ClickToCreateMode.Hold:
				case ClickToCreateMode.Arc:
					CommandManager.Instance.Prepare(new AddArcEventCommand(note));
					break;
				case ClickToCreateMode.ArcTap:
					if (canAddArcTap)
					{
						CommandManager.Instance.Add(new AddArcTapCommand(selectedArc, note as ArcArcTap));
					}
					else
					{
						if (selectedArc != null)
						{
							selectedArc = null;
							Mode = ClickToCreateMode.Idle;
						}
					}
					break;
			}
			if (note is ArcLongNote) pendingNote = note as ArcLongNote;
			switch (Mode)
			{
				case ClickToCreateMode.Hold:
					PostCreateHoldNote(note as ArcHold);
					break;
				case ClickToCreateMode.Arc:
					PostCreateArcNote(note as ArcArc);
					break;
			}
		}
		private int PositionToTrack(float position)
		{
			return Mathf.Clamp((int)(position / -4.25f + 3), 0, 5);
		}

		public void CancelAddLongNote()
		{
			if (postHoldCoroutine != null || postArcCoroutine != null)
			{
				StopCoroutine(postHoldCoroutine ?? postArcCoroutine);
				postHoldCoroutine = postArcCoroutine = null;
				// AdePositionSelector.Instance.EndModify();
				// AdeTimingSelector.Instance.EndModify();
				CommandManager.Instance.Cancel();
				selectedArc = null;
			}
		}
		public void SwitchColor()
		{
			currentArcColor = currentArcColor == 0 ? 1 : 0;
		}
		public void SwitchIsVoid()
		{
			currentArcIsVoid = !currentArcIsVoid;
		}
		public void SwitchType()
		{
			switch (currentArcType)
			{
				case ArcLineType.S:
					currentArcType = ArcLineType.B;
					break;
				case ArcLineType.B:
					currentArcType = ArcLineType.Si;
					break;
				case ArcLineType.Si:
					currentArcType = ArcLineType.So;
					break;
				case ArcLineType.So:
					currentArcType = ArcLineType.SiSi;
					break;
				case ArcLineType.SiSi:
					currentArcType = ArcLineType.SoSo;
					break;
				case ArcLineType.SoSo:
					currentArcType = ArcLineType.SiSo;
					break;
				case ArcLineType.SiSo:
					currentArcType = ArcLineType.SoSi;
					break;
				case ArcLineType.SoSi:
					currentArcType = ArcLineType.S;
					break;
			}
		}
		public void SetClickToCreateMode(int mode)
		{
			ClickToCreateMode newMode = (ClickToCreateMode)mode;
			SetClickToCreateMode(newMode);
		}
		public void SetClickToCreateMode(ClickToCreateMode newMode)
		{
			if (newMode == ClickToCreateMode.ArcTap && selectedArc == null)
			{
				AdeToast.Instance.Show("请选中一条 Arc");
				Mode = ClickToCreateMode.Idle;
				return;
			}
			Mode = newMode;
			if (newMode != ClickToCreateMode.ArcTap)
			{
				selectedArc = null;
			}
			skipNextClick = false;
		}
		public void SetArcTypeMode(int type)
		{
			currentArcType = (ArcLineType)type;
		}

		private Coroutine postHoldCoroutine = null;
		private void PostCreateHoldNote(ArcHold note)
		{
			if (postHoldCoroutine != null) StopCoroutine(postHoldCoroutine);
			postHoldCoroutine = StartCoroutine(PostCreateHoldNoteCoroutine(note));
		}
		private IEnumerator PostCreateHoldNoteCoroutine(ArcHold note)
		{
			// AdeTimingSelector.Instance.ModifyNote(note, (a) =>
			// {
			// 	note.EndTiming = a;
			// 	note.CalculateJudgeTimings();
			// });
			// while (AdeTimingSelector.Instance.Enable) yield return null;
			yield return null;
			CommandManager.Instance.Commit();
			pendingNote = null;
			postHoldCoroutine = null;
		}

		private Coroutine postArcCoroutine = null;
		private void PostCreateArcNote(ArcArc arc)
		{
			if (postArcCoroutine != null) StopCoroutine(postArcCoroutine);
			postArcCoroutine = StartCoroutine(PostCreateArcNoteCoroutine(arc));
		}
		private IEnumerator PostCreateArcNoteCoroutine(ArcArc arc)
		{
			// AdePositionSelector.Instance.ModifyNote(arc, (a) =>
			// {
			// 	arc.XStart = arc.XEnd = a.x;
			// 	arc.YStart = arc.YEnd = a.y;
			// 	arc.Rebuild();
			// 	ArcArcManager.Instance.CalculateArcRelationship();
			// });
			// while (AdePositionSelector.Instance.Enable) yield return null;
			// AdeTimingSelector.Instance.ModifyNote(arc, (a) =>
			// {
			// 	arc.EndTiming = a;
			// 	arc.Rebuild();
			// 	arc.CalculateJudgeTimings();
			// 	ArcArcManager.Instance.CalculateArcRelationship();
			// });
			// while (AdeTimingSelector.Instance.Enable) yield return null;
			// AdePositionSelector.Instance.ModifyNote(arc, (a) =>
			// {
			// 	arc.XEnd = a.x;
			// 	arc.YEnd = a.y;
			// 	arc.Rebuild();
			// 	ArcArcManager.Instance.CalculateArcRelationship();
			// });
			// while (AdePositionSelector.Instance.Enable) yield return null;
			yield return null;
			CommandManager.Instance.Commit();
			pendingNote = null;
			postArcCoroutine = null;
		}

		public void OnNoteSelect(ArcNote note)
		{
			if (note is ArcArc)
			{
				if (Mode == ClickToCreateMode.ArcTap)
				{
					if (selectedArc != null)
					{
						skipNextClick = true;
					}
					selectedArc = note as ArcArc;
				}
				else
				{
					selectedArc = note as ArcArc;
					skipNextClick = true;
				}
			}
		}
		public void OnNoteDeselect(ArcNote note)
		{
			return;
		}
		public void OnNoteDeselectAll()
		{
			return;
		}
	}
}
