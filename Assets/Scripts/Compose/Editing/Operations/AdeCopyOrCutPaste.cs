using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Compose.Command;
using System.Linq;
using UnityEngine.EventSystems;
using Arcade.Compose.Editing;

namespace Arcade.Compose
{
	public class AdeCopyOrCutPaste : MonoBehaviour, IMarkingMenuItemProvider
	{
		public static AdeCopyOrCutPaste Instance { get; private set; }

		public MarkingMenuItem CopyItem;
		public MarkingMenuItem CutItem;
		public MarkingMenuItem[] CopyingItems;

		public bool IsOnly => enable;
		public MarkingMenuItem[] Items
		{
			get
			{
				if (!enable)
				{
					if (!ArcGameplayManager.Instance.IsLoaded) return null;
					if (AdeCursorManager.Instance == null) return null;
					if (AdeCursorManager.Instance.SelectedNotes.Count == 0) return null;
					return new MarkingMenuItem[] { CopyItem, CutItem };
				}
				else return CopyingItems;
			}
		}

		private bool enable;
		private ArcNote[] notes = null;
		private CursorMode cursorMode = CursorMode.Idle;

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			AdeMarkingMenuManager.Instance.Providers.Add(this);
		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
		}
		private void Update()
		{
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C) && EventSystem.current.currentSelectedGameObject == null)
			{
				CopySelectedNotes();
			}
			else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.X) && EventSystem.current.currentSelectedGameObject == null)
			{
				CutSelectedNotes();
			}
			if (!enable) return;
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
			{
				CancelPaste();
				return;
			}
			UpdateTiming();
		}
		public void CopySelectedNotes()
		{
			if (enable) return;
			CopyOrCut(AdeCursorManager.Instance.SelectedNotes.ToArray(), false);
			AdeCursorManager.Instance.DeselectAllNotes();
		}
		public void CutSelectedNotes()
		{
			if (enable) return;
			CopyOrCut(AdeCursorManager.Instance.SelectedNotes.ToArray(), true);
			AdeCursorManager.Instance.DeselectAllNotes();
		}
		public void CopyOrCut(ArcNote[] notes, bool cut)
		{
			if (notes.Length == 0) return;
			List<ICommand> commands = new List<ICommand>();
			List<ArcNote> newNotes = new List<ArcNote>();
			foreach (var n in notes)
			{
				ArcEvent ne = n.Clone();
				if (ne is ArcArcTap)
				{
					if (notes.Contains((n as ArcArcTap).Arc))
					{
						continue;
					}
					commands.Add(new AddArcTapCommand((n as ArcArcTap).Arc, ne as ArcArcTap));
					if (cut)
					{
						commands.Add(new RemoveArcTapCommand((n as ArcArcTap).Arc, (n as ArcArcTap)));
					}
				}
				else
				{
					commands.Add(new AddArcEventCommand(ne));
					if (cut)
					{
						commands.Add(new RemoveArcEventCommand(n));
					}
					if (ne is ArcArc)
					{
						foreach (var at in (ne as ArcArc).ArcTaps)
							newNotes.Add(at);
					}
				}
				newNotes.Add(ne as ArcNote);
			}
			CommandManager.Instance.Prepare(new BatchCommand(commands.ToArray(), "复制"));
			this.notes = newNotes.ToArray();
			enable = true;
			cursorMode = AdeCursorManager.Instance.Mode;
			AdeCursorManager.Instance.Mode = CursorMode.Horizontal;
		}
		private void UpdateTiming()
		{
			if (!AdeCursorManager.Instance.IsHorizontalHit) return;
			Vector3 pos = AdeCursorManager.Instance.AttachedHorizontalPoint;
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			int timing = ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000, timingGroup) - ArcAudioManager.Instance.AudioOffset;
			bool hasIllegalArcTap = true;
			int beginTiming = notes.Min((n) => n.Timing);

			foreach (var n in notes)
			{
				n.Judged = false;
				int dif = n.Timing - beginTiming;
				switch (n)
				{
					case ArcLongNote note:
						int duration = note.EndTiming - note.Timing;
						note.Timing = timing + dif;
						note.EndTiming = timing + duration + dif;
						(note as ArcArc)?.Rebuild();
						if (note is ArcArc)
						{
							ArcArcManager.Instance.CalculateArcRelationship();
						}
						break;
					case ArcArcTap note:
						if (note.Arc.Timing > timing + dif || note.Arc.EndTiming < timing + dif)
						{
							hasIllegalArcTap = false;
						}
						note.RemoveArcTapConnection();
						note.Timing = timing + dif;
						note.Relocate();
						note.SetupArcTapConnection();
						break;
					case ArcTap note:
						note.Timing = timing + dif;
						note.SetupArcTapConnection();
						break;
					default:
						n.Timing = timing + dif;
						break;
				}
			}

			int offset = ArcAudioManager.Instance.AudioOffset;

			foreach (var t in ArcTapNoteManager.Instance.Taps)
			{
				if (ArcTimingManager.Instance.ShouldTryRender(t.Timing + offset, timingGroup))
				{
					t.SetupArcTapConnection();
				}
			}

			if (Input.GetMouseButtonDown(0))
			{
				if (hasIllegalArcTap) Paste();
				else
				{
					AdeToast.Instance.Show("粘贴的 Arctap 中有一部分超出了所在 Arc 的时间范围，无法粘贴");
				}
			}
		}
		public void CancelPaste()
		{
			CommandManager.Instance.Cancel();
			Cleanup();
		}
		private void Paste()
		{
			CommandManager.Instance.Commit();
			if (Input.GetKey(KeyCode.LeftControl))
			{
				foreach (var note in notes)
				{
					AdeCursorManager.Instance.SelectNote(note);
				}
			}
			Cleanup();
		}
		private void Cleanup()
		{
			EndOfFrame.Instance.Listeners.AddListener(() =>
			{
				enable = false;
				notes = null;
				AdeCursorManager.Instance.Mode = cursorMode;
			});

		}
	}
}
