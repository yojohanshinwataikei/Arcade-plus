using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;
using Arcade.Compose;
using Arcade.Compose.MarkingMenu;
using Arcade.Compose.Command;
using Arcade.Gameplay;

namespace Arcade.Compose.Operation
{
	public class AdeTimingSnapOperation : AdeOperation
	{
		public static AdeTimingSnapOperation Instance { get; private set; }
		public MarkingMenuItem Entry;

		public override bool IsOnlyMarkingMenu => false;
		public override MarkingMenuItem[] MarkingMenuItems
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return null;
				if (AdeCursorManager.Instance == null) return null;
				if (AdeSelectionManager.Instance.SelectedNotes.Count == 0) return null;
				if (!AdeGridManager.Instance.Enable) return null;
				return new MarkingMenuItem[] { Entry };
			}
		}
		private void Awake()
		{
			Instance = this;
		}

		private void SnapSelectedNotesTiming()
		{
			// We hide the menu when grid is not open, so we should not check here
			var selected = AdeSelectionManager.Instance.SelectedNotes;
			List<ICommand> commands = new List<ICommand>();
			foreach (var note in selected)
			{
				if (note is ArcArcTap arcTap)
				{
					if (selected.Contains(arcTap.Arc))
					{
						continue;
					}
					var newNote = note.Clone();
					var arc = arcTap.Arc;
					int newTiming = GetSnappedTiming(note.Timing);
					if (newTiming < arc.Timing)
					{
						newTiming = arc.Timing;
					}
					if (newTiming > arc.EndTiming)
					{
						newTiming = arc.EndTiming;
					}
					newNote.Timing = newTiming;
					commands.Add(new EditArcEventCommand(note, newNote));
				}
				else
				{
					var newNote = note.Clone();
					int newTiming = GetSnappedTiming(note.Timing);
					bool isSingleArctapArc = false;
					if (note is ArcArc arcArc)
					{
						isSingleArctapArc = (arcArc.EndTiming - note.Timing == 1) && arcArc.ArcTaps.Count > 0;
					}
					newNote.Timing = newTiming;
					if (note is ArcLongNote longNote)
					{
						if (isSingleArctapArc)
						{
							(newNote as ArcLongNote).EndTiming = newTiming + 1;
						}
						else
						{
							int newEndTiming = GetSnappedTiming(longNote.EndTiming);
							if (newEndTiming < newTiming)
							{
								newEndTiming = newTiming;
							}
							if (newEndTiming == newTiming)
							{
								bool needAvoidZeroLength = false;
								if (note is ArcHold)
								{
									needAvoidZeroLength = true;
								}
								if (note is ArcArc arc)
								{
									if (arc.ArcTaps.Count > 0)
									{
										needAvoidZeroLength = true;
									}
									if (Mathf.Approximately(arc.XStart, arc.XEnd) && Mathf.Approximately(arc.YStart, arc.YEnd))
									{
										needAvoidZeroLength = true;
									}
								}
								if (needAvoidZeroLength)
								{
									newEndTiming = GetNextSnappedTiming(newTiming);
								}
							}
							(newNote as ArcLongNote).EndTiming = newEndTiming;
						}
					}
					commands.Add(new EditArcEventCommand(note, newNote));
					if (note is ArcArc)
					{
						ArcArc newArc = newNote as ArcArc;
						foreach (var nestedArcTap in (note as ArcArc).ArcTaps)
						{
							var newArcTap = nestedArcTap.Clone();
							int newArcTapTiming = GetSnappedTiming(nestedArcTap.Timing);
							if (newArcTapTiming < newArc.Timing)
							{
								newArcTapTiming = newArc.Timing;
							}
							if (newArcTapTiming > newArc.EndTiming)
							{
								newArcTapTiming = newArc.EndTiming;
							}
							newArcTap.Timing = newArcTapTiming;
							commands.Add(new EditArcEventCommand(nestedArcTap, newArcTap));
						}
					}
				}
			}
			AdeCommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "时间对齐"));
		}

		public int GetSnappedTiming(int timing)
		{
			int newTiming = AdeGridManager.Instance.AttachTiming(timing);
			return newTiming;
		}
		public int GetNextSnappedTiming(int timing)
		{
			int newTiming = AdeGridManager.Instance.AttachScroll(timing, 1);
			return newTiming;
		}

		public override AdeOperationResult TryExecuteOperation()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.TimingSnap))
			{
				SnapSelectedNotesTiming();
				return true;
			}
			return false;
		}

		public void ManuallyExecuteOperation()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				SnapSelectedNotesTiming();
				return null;
			});
		}
	}
}