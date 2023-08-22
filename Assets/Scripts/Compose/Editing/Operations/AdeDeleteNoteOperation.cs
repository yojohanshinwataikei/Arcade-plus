using System.Collections;
using System.Collections.Generic;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Arcade.Util.UnityExtension;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Compose.Operation
{
	public class AdeDeleteNoteOperation : AdeOperation
	{
		public override AdeOperationResult TryExecuteOperation()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.Delete))
			{
				AdeSelectionManager.Instance.DeleteSelectedNotes();
				return true;
			}
			return false;
		}
	}
}