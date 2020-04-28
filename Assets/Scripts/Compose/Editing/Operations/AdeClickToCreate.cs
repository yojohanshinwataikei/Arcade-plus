using System.Collections;
using System.Collections.Generic;
using Arcade.Compose.Command;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using UnityEngine;

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
		public MarkingMenuItem[] SelectingValueItems;

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
				}
				if (enable != value)
				{
					AdeCursorManager.Instance.Mode = value ? CursorMode.Horizontal : CursorMode.Idle;
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
				if (AdeTimingSelector.Instance.Enable || AdePositionSelector.Instance.Enable) return SelectingValueItems;
				if (!enable) return Entry;
				else
				{
					List<MarkingMenuItem> items = new List<MarkingMenuItem>();
					items.AddRange(ClickToCreateItems);
					if (AdeCursorManager.Instance.SelectedNotes.Count != 0)
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
				if (AdeTimingSelector.Instance.Enable) return ClickToCreateMode.Idle;
				if (AdePositionSelector.Instance.Enable) return ClickToCreateMode.Idle;
				return mode;
			}
			set
			{
				if (mode != value)
				{
					mode = value;
				}
				if (mode == ClickToCreateMode.Idle)
				{
					AdeCursorManager.Instance.EnableArcTapCursor = false;
					AdeCursorManager.Instance.EnableVerticalPanel = false;
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

		private bool enable;
		private bool skipNextClick;
		private bool canAddArcTap;
		private ArcLongNote pendingNote;
		private ArcArc selectedArc;
		private int currentArcColor;
		private bool currentArcIsVoid;
		private ArcLineType currentArcType;

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			AdeMarkingMenuManager.Instance.Providers.Add(this);
			AdeCursorManager.Instance.NoteEventListeners.Add(this);
		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
		}
		private void Update()
		{
			if (!enable) return;
			if (Mode == ClickToCreateMode.Idle) return;
			UpdateArcTapCursor();
			if (Input.GetMouseButtonDown(0))
			{
				AddNoteHandler();
			}
		}

		private void UpdateArcTapCursor()
		{
			if (selectedArc != null && selectedArc.Instance == null)
			{
				selectedArc = null;
				if (Mode == ClickToCreateMode.ArcTap) Mode = ClickToCreateMode.Idle;
				return;
			}
			if (Mode != ClickToCreateMode.ArcTap || selectedArc == null)
			{
				AdeCursorManager.Instance.EnableArcTapCursor = false;
				AdeCursorManager.Instance.EnableVerticalPanel = false;
				return;
			}
			Vector3 pos = AdeCursorManager.Instance.AttachedHorizontalPoint;
			int timing = ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000) - ArcAudioManager.Instance.AudioOffset;
			canAddArcTap = selectedArc.Timing <= timing && selectedArc.EndTiming >= timing;
			AdeCursorManager.Instance.EnableArcTapCursor = canAddArcTap;
			AdeCursorManager.Instance.EnableVerticalPanel = canAddArcTap;
			if (!canAddArcTap) return;
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			float t = 1f * (timing - selectedArc.Timing) / (selectedArc.EndTiming - selectedArc.Timing);
			Vector2 gizmo = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(selectedArc.XStart, selectedArc.XEnd, t, selectedArc.LineType)),
									   ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(selectedArc.YStart, selectedArc.YEnd, t, selectedArc.LineType)) - 0.5f);
			AdeCursorManager.Instance.ArcTapCursorPosition = gizmo;
		}
		private void AddNoteHandler()
		{
			if (!AdeCursorManager.Instance.IsHorizontalHit) return;
			if (skipNextClick)
			{
				skipNextClick = false;
				return;
			}
			Vector3 pos = AdeCursorManager.Instance.AttachedHorizontalPoint;
			int timing = ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000) - ArcAudioManager.Instance.AudioOffset;
			ArcNote note = null;
			switch (Mode)
			{
				case ClickToCreateMode.Tap:
					note = new ArcTap() { Timing = timing, Track = PositionToTrack(pos.x) };
					break;
				case ClickToCreateMode.Hold:
					note = new ArcHold() { Timing = timing, Track = PositionToTrack(pos.x), EndTiming = timing };
					break;
				case ClickToCreateMode.Arc:
					note = new ArcArc() { Timing = timing, EndTiming = timing, Color = currentArcColor, IsVoid = currentArcIsVoid, LineType = currentArcType };
					break;
				case ClickToCreateMode.ArcTap:
					note = new ArcArcTap() { Timing = timing };
					break;
			}
			if (note == null) return;
			switch (Mode)
			{
				case ClickToCreateMode.Tap:
				case ClickToCreateMode.Hold:
				case ClickToCreateMode.Arc:
					CommandManager.Instance.Add(new AddArcEventCommand(note));
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
			return Mathf.Clamp((int)(position / -4.25f + 2) + 1, 1, 4);
		}

		public void CancelAddLongNote()
		{
			if (postHoldCoroutine != null || postArcCoroutine != null)
			{
				StopCoroutine(postHoldCoroutine ?? postArcCoroutine);
				postHoldCoroutine = postArcCoroutine = null;
				AdePositionSelector.Instance.EndModify();
				AdeTimingSelector.Instance.EndModify();
				CommandManager.Instance.Add(new RemoveArcEventCommand(pendingNote));
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
		public void SetClickToCreateMode(int mode)
		{
			ClickToCreateMode newMode = (ClickToCreateMode)mode;
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
			AdeTimingSelector.Instance.ModifyNote(note, (a) => { note.EndTiming = a; });
			while (AdeTimingSelector.Instance.Enable) yield return null;
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
			AdePositionSelector.Instance.ModifyNote(arc, (a) => { arc.XStart = arc.XEnd = a.x; arc.YStart = arc.YEnd = a.y; arc.Rebuild(); });
			while (AdePositionSelector.Instance.Enable) yield return null;
			AdeTimingSelector.Instance.ModifyNote(arc, (a) => { arc.EndTiming = a; arc.Rebuild(); });
			while (AdeTimingSelector.Instance.Enable) yield return null;
			AdePositionSelector.Instance.ModifyNote(arc, (a) => { arc.XEnd = a.x; arc.YEnd = a.y; arc.Rebuild(); });
			while (AdePositionSelector.Instance.Enable) yield return null;
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
