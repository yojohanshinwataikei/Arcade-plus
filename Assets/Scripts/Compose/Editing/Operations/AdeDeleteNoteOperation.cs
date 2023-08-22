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
	public class AdeDeleteNoteOperation : AdeOperation, IMarkingMenuItemProvider
	{
		public MarkingMenuItem DeleteItem;
		public MarkingMenuItem[] Items
		{
			get
			{
				return AdeSelectionManager.Instance.SelectedNotes.Count == 0 ? new MarkingMenuItem[] { } : new MarkingMenuItem[] { DeleteItem };
			}
		}

		public bool IsOnly
		{
			get
			{
				return false;
			}
		}
		private void Start()
		{
			AdeMarkingMenuManager.Instance.Providers.Add(this);

		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
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