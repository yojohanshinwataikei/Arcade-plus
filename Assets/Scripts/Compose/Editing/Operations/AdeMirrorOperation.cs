using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;
using Arcade.Compose;
using Arcade.Compose.MarkingMenu;
using Arcade.Compose.Command;
using Arcade.Gameplay;

namespace Arcade.Compose.Operation
{
	public class AdeMirrorOperation : AdeOperation
	{
		public static AdeMirrorOperation Instance { get; private set; }

		public MarkingMenuItem Entry;

		public override bool IsOnlyMarkingMenu => false;
		public override MarkingMenuItem[] MarkingMenuItems
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return null;
				if (AdeCursorManager.Instance == null) return null;
				if (AdeSelectionManager.Instance.SelectedNotes.Count == 0) return null;
				return new MarkingMenuItem[] { Entry };
			}
		}

		private void Awake()
		{
			Instance = this;
		}

		private void MirrorSelectedNotes()
		{
			var selected = AdeSelectionManager.Instance.SelectedNotes;
			List<ICommand> commands = new List<ICommand>();
			foreach (var n in selected)
			{
				switch (n)
				{
					case ArcTap tap:
						ArcTap newtap = tap.Clone() as ArcTap;
						newtap.Track = 5 - newtap.Track;
						commands.Add(new EditArcEventCommand(tap, newtap));
						break;
					case ArcHold hold:
						ArcHold newhold = hold.Clone() as ArcHold;
						newhold.Track = 5 - newhold.Track;
						commands.Add(new EditArcEventCommand(hold, newhold));
						break;
					case ArcArc arc:
						ArcArc newarc = arc.Clone() as ArcArc;
						newarc.XStart = 1 - newarc.XStart;
						newarc.XEnd = 1 - newarc.XEnd;
						if (newarc.Color < 2)
						{
							newarc.Color = 1 - newarc.Color;
						}
						commands.Add(new EditArcEventCommand(arc, newarc));
						break;
				}
			}
			AdeCommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "镜像"));
		}

		public override AdeOperationResult TryExecuteOperation()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.Mirror))
			{
				MirrorSelectedNotes();
				return true;
			}
			return false;
		}
		public void ManuallyExecuteOperation()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				MirrorSelectedNotes();
				return null;
			});
		}
	}
}
