using System.Collections;
using System.Collections.Generic;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Arcade.Util.UnityExtension;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Compose.Operation
{
	public class AdeDeleteNoteOperation : AdeOperation
	{
		public MarkingMenuItem DeleteItem;
		public override MarkingMenuItem[] MarkingMenuItems
		{
			get
			{
				return AdeSelectionManager.Instance.SelectedNotes.Count == 0 ? new MarkingMenuItem[] { } : new MarkingMenuItem[] { DeleteItem };
			}
		}

		public override bool IsOnlyMarkingMenu
		{
			get
			{
				return false;
			}
		}
		public override AdeOperationResult TryExecuteOperation()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.Delete))
			{
				AdeSelectionManager.Instance.DeleteSelectedNotes();
				return true;
			}
			return false;
		}

		public void ManuallyExecuteOperation()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				AdeSelectionManager.Instance.DeleteSelectedNotes();
				return null;
			});
		}
	}
}