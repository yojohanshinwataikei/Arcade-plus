using System.Collections;
using System.Collections.Generic;
using Arcade.Compose.Command;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Arcade.Util.UnityExtension;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Compose
{
	public class AdeSelectionManager : MonoBehaviour, IMarkingMenuItemProvider
	{
		public static AdeSelectionManager Instance { get; private set; }

		public MarkingMenuItem DeleteItem;
		public MarkingMenuItem[] Items
		{
			get
			{
				return SelectedNotes.Count == 0 ? new MarkingMenuItem[] { } : new MarkingMenuItem[] { DeleteItem };
			}
		}

		public bool IsOnly
		{
			get
			{
				return false;
			}
		}

		public List<ArcNote> SelectedNotes = new List<ArcNote>();

		public List<INoteSelectEvent> NoteEventListeners = new List<INoteSelectEvent>();

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			AdeMarkingMenuManager.Instance.Providers.Add(this);
			ArcGameplayManager.Instance.OnChartLoad.AddListener(this.DeselectAllNotes);
		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
			ArcGameplayManager.Instance.OnChartLoad.RemoveListener(this.DeselectAllNotes);
		}

		private void LagecyUpdate()
		{
			Selecting();
			DeleteListener();
		}

		private float? rangeSelectPosition = null;
		public float? RangeSelectPosition{get=>rangeSelectPosition;}
		private void Selecting()
		{
			if (!AdeInputManager.Instance.Inputs.RangeSelection.IsPressed())
			{
				rangeSelectPosition = null;
			}

			if (Mouse.current.leftButton.wasPressedThisFrame)
			{
				//range selection shortcut
				if (AdeInputManager.Instance.Inputs.RangeSelection.IsPressed())
				{
					if (rangeSelectPosition == null)
					{
						rangeSelectPosition = AdeCursorManager.Instance.AttachedTiming;
					}
					else
					{
						if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
						{
							DeselectAllNotes();
						}
						RangeSelectNote(rangeSelectPosition.Value, AdeCursorManager.Instance.AttachedTiming);
						rangeSelectPosition = null;
					}
					return;
				}

				Ray ray = AdeCursorManager.Instance.GameplayCamera.MousePositionToRay();

				RaycastHit[] hits = Physics.RaycastAll(ray, 120, 1 << 9);
				ArcNote n = null;
				float distance = float.MaxValue;
				foreach (var h in hits)
				{
					ArcNote t = ArcGameplayManager.Instance.FindNoteByRaycastHit(h);
					if (t != null)
					{
						if (h.distance < distance)
						{
							distance = h.distance;
							n = t;
						}
					}
				}
				if (n != null)
				{
					if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
					{
						DeselectAllNotes();
						SelectNote(n);
					}
					else
					{
						if (SelectedNotes.Contains(n)) DeselectNote(n);
						else SelectNote(n);
					}
				}
				else
				{
					if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed() && AdeCursorManager.Instance.IsHorizontalHit)
					{
						DeselectAllNotes();
					}
				}
			}
		}
		private void DeleteListener()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.Delete))
			{
				DeleteSelectedNotes();
			}
		}

		public void RangeSelectNote(float from, float to)
		{
			float start = Mathf.Min(from, to);
			float end = Mathf.Max(from, to);
			List<ArcNote> list = new List<ArcNote>();
			foreach (var tap in ArcTapNoteManager.Instance.Taps)
			{
				if (tap.Timing >= start && tap.Timing <= end)
				{
					SelectNote(tap);
				}
			}
			foreach (var hold in ArcHoldNoteManager.Instance.Holds)
			{
				if (hold.Timing >= start && hold.EndTiming <= end)
				{
					SelectNote(hold);
				}
			}
			foreach (var arc in ArcArcManager.Instance.Arcs)
			{
				if (arc.Timing >= start && arc.EndTiming <= end)
				{
					SelectNote(arc);
				}
				if (arc.Timing <= end && arc.EndTiming >= start)
				{
					foreach (var arctap in arc.ArcTaps)
					{
						if (arctap.Timing >= start && arctap.Timing <= end)
						{
							SelectNote(arctap);
						}
					}
				}
			}
		}
		public void SelectNote(ArcNote note)
		{
			if (note.Instance != null) note.Selected = true;
			if (!SelectedNotes.Contains(note))
			{
				SelectedNotes.Add(note);
				foreach (var l in NoteEventListeners) l.OnNoteSelect(note);
			}
		}
		public void DeselectNote(ArcNote note)
		{
			if (note.Instance != null) note.Selected = false;
			if (SelectedNotes.Contains(note))
			{
				SelectedNotes.Remove(note);
				foreach (var l in NoteEventListeners) l.OnNoteDeselect(note);
			}
		}
		public void DeselectAllNotes()
		{
			foreach (var note in SelectedNotes) if (note.Instance != null) note.Selected = false;
			SelectedNotes.Clear();
			foreach (var l in NoteEventListeners) l.OnNoteDeselectAll();
		}
		public void DeleteSelectedNotes()
		{
			List<ICommand> deleteCommands = new List<ICommand>();
			foreach (var s in SelectedNotes)
			{
				if (s is ArcArcTap) deleteCommands.Add(new RemoveArcTapCommand((s as ArcArcTap).Arc, s as ArcArcTap));
				else deleteCommands.Add(new RemoveArcEventCommand(s));
			}
			if (deleteCommands.Count != 0) CommandManager.Instance.Add(new BatchCommand(deleteCommands.ToArray(), "删除"));
			SelectedNotes.Clear();
		}
	}
}