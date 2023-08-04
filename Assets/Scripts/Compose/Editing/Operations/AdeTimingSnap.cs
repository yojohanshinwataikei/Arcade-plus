using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;
using Arcade.Compose;
using Arcade.Compose.MarkingMenu;
using Arcade.Compose.Command;
using Arcade.Gameplay;

public class AdeTimingSnap : MonoBehaviour, IMarkingMenuItemProvider
{
	public static AdeTimingSnap Instance { get; private set; }
	public MarkingMenuItem Entry;

	public bool IsOnly => false;
	public MarkingMenuItem[] Items
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
	private void Start()
	{
		AdeMarkingMenuManager.Instance.Providers.Add(this);
	}
	private void OnDestroy()
	{
		AdeMarkingMenuManager.Instance.Providers.Remove(this);
	}

	public void SnapSelectedNotesTiming()
	{
		// We hide the menu when grid is not open, so we should not check here
		var selected = AdeSelectionManager.Instance.SelectedNotes;
		List<ICommand> commands = new List<ICommand>();
		foreach (var note in selected)
		{
			if (note is ArcArcTap)
			{
				if (selected.Contains((note as ArcArcTap).Arc))
				{
					continue;
				}
				var newNote = note.Clone();
				var arc = (note as ArcArcTap).Arc;
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
				newNote.Timing = newTiming;
				if (note is ArcLongNote)
				{
					int newEndTiming = GetSnappedTiming((note as ArcLongNote).EndTiming);
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
						if (note is ArcArc)
						{
							var arc = note as ArcArc;
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
							newEndTiming = GetNextSnapedTiming(newTiming);
						}
					}
					(newNote as ArcLongNote).EndTiming = newEndTiming;
				}
				commands.Add(new EditArcEventCommand(note, newNote));
				if (note is ArcArc)
				{
					ArcArc newArc = newNote as ArcArc;
					foreach (var arcTap in (note as ArcArc).ArcTaps)
					{
						var newArcTap = arcTap.Clone();
						int newArcTapTiming = GetSnappedTiming(arcTap.Timing);
						if (newArcTapTiming < newArc.Timing)
						{
							newArcTapTiming = newArc.Timing;
						}
						if (newArcTapTiming > newArc.EndTiming)
						{
							newArcTapTiming = newArc.EndTiming;
						}
						newArcTap.Timing = newArcTapTiming;
						commands.Add(new EditArcEventCommand(arcTap, newArcTap));
					}
				}
			}
		}
		CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "时间对齐"));
	}

	public int GetSnappedTiming(int timing)
	{
		int newTiming = Mathf.FloorToInt(AdeGridManager.Instance.AttachTiming(timing));
		return newTiming;
	}
	public int GetNextSnapedTiming(int timing)
	{
		int newTiming = Mathf.FloorToInt(AdeGridManager.Instance.AttachScroll(timing, 1));
		return newTiming;
	}
	private void Update()
	{
		if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.TimingSnap))
		{
			SnapSelectedNotesTiming();
		}
	}
}