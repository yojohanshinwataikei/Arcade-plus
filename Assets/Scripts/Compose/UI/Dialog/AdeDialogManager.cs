using System;
using UnityEngine;

namespace Arcade.Compose
{

    public abstract class AdeDialogContent<TDialog> : MonoBehaviour where TDialog:AdeDialog{
        public TDialog dialog;
        public void OpenDialog(){
            dialog.Open();
        }
        public void Close(){
            dialog.Close();
        }
    }

    public abstract class AdeDialog : MonoBehaviour{
        public void Open(){
            AdeDialogManager.Instance.Open(this);
        }
        public void Close(){
            AdeDialogManager.Instance.Close(this);
        }
    }

    public class AdeDialogManager : MonoBehaviour
    {
		public static AdeDialogManager Instance { get; private set; }

        private void Awake()
		{
			Instance = this;
		}

        public void Open(AdeDialog adeDialog)
        {
            throw new NotImplementedException();
        }

        public void Close(AdeDialog adeDialog)
        {
            throw new NotImplementedException();
        }
    }
}