using Arcade.Gameplay.Chart;
using Arcade.Gameplay;

namespace Arcade.Compose.Command
{
	public class AddArcEventCommand : ICommand
	{
		private readonly ArcEvent @event = null;
		public AddArcEventCommand(ArcEvent note)
		{
			this.@event = note;
		}
		public string Name
		{
			get
			{
				return "添加 Note";
			}
		}
		public void Do()
		{
			switch (@event)
			{
				case ArcTap note:
					ArcTapNoteManager.Instance.Add(note);
					break;
				case ArcHold note:
					ArcHoldNoteManager.Instance.Add(note);
					break;
				case ArcArc note:
					ArcArcManager.Instance.Add(note);
					break;
			}
		}
		public void Undo()
		{
			if (@event is ArcNote note)
			{
				AdeSelectionManager.Instance.DeselectNote(note);
			}
			if (@event is ArcTap tap)
			{
				ArcTapNoteManager.Instance.Remove(tap);
			}
			else if (@event is ArcHold hold)
			{
				ArcHoldNoteManager.Instance.Remove(hold);
			}
			else if (@event is ArcArc arc)
			{
				foreach (var arctap in arc.ArcTaps)
				{
					AdeSelectionManager.Instance.DeselectNote(arctap);
				}
				ArcArcManager.Instance.Remove(arc);
			}
		}
	}
	public class RemoveArcEventCommand : ICommand
	{
		private readonly ArcEvent @event = null;
		public RemoveArcEventCommand(ArcEvent note)
		{
			this.@event = note;
		}
		public string Name
		{
			get
			{
				return "删除 Note";
			}
		}
		public void Do()
		{
			if (@event is ArcNote note)
			{
				AdeSelectionManager.Instance.DeselectNote(note);
			}
			if (@event is ArcTap tap)
			{
				ArcTapNoteManager.Instance.Remove(tap);
			}
			else if (@event is ArcHold hold)
			{
				ArcHoldNoteManager.Instance.Remove(hold);
			}
			else if (@event is ArcArc arc)
			{
				foreach (var arctap in arc.ArcTaps)
				{
					AdeSelectionManager.Instance.DeselectNote(arctap);
				}
				ArcArcManager.Instance.Remove(arc);
			}
		}
		public void Undo()
		{
			if (@event is ArcTap tap)
			{
				ArcTapNoteManager.Instance.Add(tap);
			}
			else if (@event is ArcHold hold)
			{
				ArcHoldNoteManager.Instance.Add(hold);
			}
			else if (@event is ArcArc arc)
			{
				ArcArcManager.Instance.Add(arc);
			}
		}
	}
	public class EditArcEventCommand : ICommand
	{
		private readonly ArcEvent note = null;
		private readonly ArcEvent oldValues, newValues;
		public EditArcEventCommand(ArcEvent note, ArcEvent newValues)
		{
			this.note = note;
			oldValues = note.Clone();
			this.newValues = newValues;
		}
		public string Name
		{
			get
			{
				return "修改 Note";
			}
		}
		public void Do()
		{
			(note as ArcArcTap)?.RemoveArcTapConnection();
			note.Assign(newValues);
			(note as ArcArcTap)?.Relocate();
			(note as ArcArc)?.Rebuild();
			(note as ArcTap)?.SetupArcTapConnection();
			if (note is ArcArc) ArcArcManager.Instance.CalculateArcRelationship();
			ArcGameplayManager.Instance.ResetJudge();
		}
		public void Undo()
		{
			(note as ArcArcTap)?.RemoveArcTapConnection();
			note.Assign(oldValues);
			(note as ArcArcTap)?.Relocate();
			(note as ArcArc)?.Rebuild();
			(note as ArcTap)?.SetupArcTapConnection();
			if (note is ArcArc) ArcArcManager.Instance.CalculateArcRelationship();
			ArcGameplayManager.Instance.ResetJudge();
		}
	}

	public class AddArcTapCommand : ICommand
	{
		private readonly ArcArc arc;
		private readonly ArcArcTap arctap;
		public AddArcTapCommand(ArcArc arc, ArcArcTap arctap)
		{
			this.arc = arc;
			this.arctap = arctap;
		}
		public string Name
		{
			get
			{
				return "添加 ArcTap";
			}
		}
		public void Do()
		{
			arc.AddArcTap(arctap);
		}
		public void Undo()
		{
			AdeSelectionManager.Instance.DeselectNote(arctap);
			arc.RemoveArcTap(arctap);
		}
	}
	public class RemoveArcTapCommand : ICommand
	{
		private readonly ArcArc arc;
		private readonly ArcArcTap arctap;
		public RemoveArcTapCommand(ArcArc arc, ArcArcTap arctap)
		{
			this.arc = arc;
			this.arctap = arctap;
		}
		public string Name
		{
			get
			{
				return "删除 ArcTap";
			}
		}
		public void Do()
		{
			AdeSelectionManager.Instance.DeselectNote(arctap);
			arc.RemoveArcTap(arctap);
		}
		public void Undo()
		{
			arc.AddArcTap(arctap);
		}
	}
}
