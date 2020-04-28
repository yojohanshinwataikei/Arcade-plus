using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose.Dialog
{
	public class AdeSingleDialog : MonoBehaviour
    {
        public static AdeSingleDialog Instance { get; private set; }
        private void Awake()
        {
            Instance = this;
        }

        public RectTransform Dialog, View;
        public Text Title, Button1;
        public Text Content;

        private Action callback;

        public void Show(string Content, string Title = null, string Button1 = null, Action Callback = null)
        {
            this.Content.text = Content;
            this.Title.text = Title ?? "提示";
            this.Button1.text = Button1 ?? "确认";
            callback = Callback;
            View.gameObject.SetActive(true);
        }
        public void Hide()
        {
            View.gameObject.SetActive(false);
            callback?.Invoke();
            callback = null;
        }
    }
}
