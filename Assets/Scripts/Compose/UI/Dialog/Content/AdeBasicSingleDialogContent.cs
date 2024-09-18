using System;
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
        }

        public Text Content;

        private Action callback;

        public void Show(string Content, string Title = null, string Button1 = null, Action Callback = null)
        {
            this.Content.text = Content;
            this.Dialog.Title.text = Title ?? "提示";
            this.Dialog.ButtonText.text = Button1 ?? "确认";
            callback = Callback;
            OpenDialog();
        }
        public void Hide()
        {
            CloseDialog();
            callback?.Invoke();
            callback = null;
        }
    }
}
