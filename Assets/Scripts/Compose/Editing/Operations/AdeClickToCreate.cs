using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Arcade.Compose.Command;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Cysharp.Threading.Tasks;
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
	public class AdeClickToCreate : AdeOperation
	{
		public static AdeClickToCreate Instance { get; private set; }

		public MarkingMenuItem Delete;
		public MarkingMenuItem[] Entry;
		public MarkingMenuItem[] ClickToCreateItems;

		public override bool IsOnlyMarkingMenu => enable;
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
					AdeCursorManager.Instance.VisibleWhenIdle = value;
					Mode = ClickToCreateMode.Idle;
					enable = value;
				}
			}
		}
		public override MarkingMenuItem[] MarkingMenuItems
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

		private bool enable = false;
		private bool skipNextClick;
		private ArcLongNote pendingNote;
		private int currentArcColor;
		private bool currentArcIsVoid;
		private ArcLineType currentArcType = ArcLineType.S;

		private void Awake()
		{
			Instance = this;
		}
		private void Update()
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
		}

		private void UpdateArcTapCursor()
		{
			ArcArc currentArc = GetCurrentArc();
			if (Mode != ClickToCreateMode.ArcTap || currentArc == null)
			{
				AdeCursorManager.Instance.ArcTapCursorEnabled = false;
				return;
			}
			int timing = AdeCursorManager.Instance.AttachedTiming;
			bool canAddArcTap = currentArc.Timing <= timing && currentArc.EndTiming >= timing && currentArc.Timing < currentArc.EndTiming;
			AdeCursorManager.Instance.ArcTapCursorEnabled = canAddArcTap;
			AdeCursorManager.Instance.ArcTapCursorIsSfx = currentArc.IsSfx;
			if (!canAddArcTap) return;
			float t = 1f * (timing - currentArc.Timing) / (currentArc.EndTiming - currentArc.Timing);
			Vector2 gizmo = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(currentArc.XStart, currentArc.XEnd, t, currentArc.LineType)),
									   ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(currentArc.YStart, currentArc.YEnd, t, currentArc.LineType)) - 0.5f);
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
					// note = new ArcTap() { Timing = timing, Track = PositionToTrack(pos.x), TimingGroup = timingGroup };
					break;
				case ClickToCreateMode.Hold:
					// note = new ArcHold() { Timing = timing, Track = PositionToTrack(pos.x), EndTiming = timing, TimingGroup = timingGroup };
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
					// if (canAddArcTap)
					// {
					// 	CommandManager.Instance.Add(new AddArcTapCommand(selectedArc, note as ArcArcTap));
					// }
					// else
					// {
					// 	if (selectedArc != null)
					// 	{
					// 		selectedArc = null;
					// 		Mode = ClickToCreateMode.Idle;
					// 	}
					// }
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

		public void CancelAddLongNote()
		{
			if (postHoldCoroutine != null || postArcCoroutine != null)
			{
				StopCoroutine(postHoldCoroutine ?? postArcCoroutine);
				postHoldCoroutine = postArcCoroutine = null;
				// AdePositionSelector.Instance.EndModify();
				// AdeTimingSelector.Instance.EndModify();
				CommandManager.Instance.Cancel();
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
			Mode = newMode;
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

		private ArcArc GetCurrentArc()
		{
			var notes = AdeSelectionManager.Instance.SelectedNotes;
			if (notes.Count != 1)
			{
				return null;
			}
			var note = notes[0];
			if (note is ArcArc)
			{
				var arc = note as ArcArc;
				if (arc.IsVoid)
				{
					return arc;
				}
			}
			return null;
		}

		public bool MayAddArctap()
		{
			ArcArc currentArc = GetCurrentArc();
			if (currentArc != null)
			{
				int timing = AdeCursorManager.Instance.AttachedTiming;
				if (currentArc.Timing <= timing && currentArc.EndTiming >= timing && currentArc.Timing < currentArc.EndTiming)
				{
					return true;
				}
			}
			return false;
		}

		private async UniTask ExecuteAddHold(CancellationToken cancellationToken)
		{
			int track = AdeCursorManager.Instance.AttachedTrack;
			int timing = AdeCursorManager.Instance.AttachedTiming;
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			ArcHold note = new ArcHold() { Timing = timing, Track = track, EndTiming = timing, TimingGroup = timingGroup };
			CommandManager.Instance.Prepare(new AddArcEventCommand(note));

			try
			{
				Action<int> updateEndTiming = (int timing) =>
				{
					if (timing > note.Timing)
					{
						note.EndTiming = timing;
					}
				};
				while (true)
				{
					var endTiming = await AdeCursorManager.Instance.SelectTiming(Progress.Create(updateEndTiming), cancellationToken);
					if (endTiming > note.Timing)
					{
						updateEndTiming(endTiming);
						break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				CommandManager.Instance.Cancel();
				throw ex;
			}
			CommandManager.Instance.Commit();
		}

		public override AdeOperationResult TryExecuteOperation()
		{
			if (!Enable)
			{
				return false;
			}
			if (!AdeGameplayContentInputHandler.InputActive)
			{
				return false;
			}
			if (Mouse.current.leftButton.wasPressedThisFrame)
			{
				if (mode == ClickToCreateMode.Tap)
				{
					int track = AdeCursorManager.Instance.AttachedTrack;
					int timing = AdeCursorManager.Instance.AttachedTiming;
					var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
					ArcTap note = new ArcTap() { Timing = timing, Track = track, TimingGroup = timingGroup };
					CommandManager.Instance.Add(new AddArcEventCommand(note));
					return true;
				}
				else if (mode == ClickToCreateMode.ArcTap)
				{
					ArcArc currentArc = GetCurrentArc();
					if (currentArc != null)
					{
						int timing = AdeCursorManager.Instance.AttachedTiming;
						if (currentArc.Timing <= timing && currentArc.EndTiming >= timing && currentArc.Timing < currentArc.EndTiming)
						{
							ArcArcTap note = new ArcArcTap() { Timing = timing };
							CommandManager.Instance.Add(new AddArcTapCommand(currentArc, note));
							return true;
						}
					}
				}
				else if (mode == ClickToCreateMode.Hold)
				{
					var cancellation = new CancellationTokenSource();
					return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
					{
						task = ExecuteAddHold(cancellation.Token),
						cancellation = cancellation,
					});
				}
			}
			return false;
		}
	}
}
