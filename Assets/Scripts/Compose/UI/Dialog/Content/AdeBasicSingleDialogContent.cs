using System;
using Arcade.Util.I18n;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose.Dialog
{
	public class AdeBasicSingleDialogContent : AdeDialogContent<AdeSingleDialog>
	{
		public static AdeBasicSingleDialogContent Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
			Dialog.OnClose += OnClose;
		}
		private void OnDestroy()
		{
			Dialog.OnClose -= OnClose;
		}

		public Text Content;

		private Action callback;

		public void Show(string Content, string Title = null, string Button1 = null, Action Callback = null)
		{
			this.Content.text = Content;
			this.Dialog.Title.text = Title ?? I18n.Translate("dialogs.title");
			this.Dialog.ButtonText.text = Button1 ?? I18n.Translate("dialogs.button.confirm");
			callback = Callback;
			OpenDialog();
		}
		public void OnClose()
		{
			callback?.Invoke();
			callback = null;
		}
	}
}
