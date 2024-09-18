using System;
using UnityEngine;

namespace Arcade.Compose
{

    public abstract class AdeDialogContent<TDialog> : MonoBehaviour where TDialog:AdeDialog{
        public TDialog Dialog;
        public void OpenDialog(){
            Dialog.Open();
        }
        public void CloseDialog(){
            Dialog.Close();
        }
    }

    public abstract class AdeDialog : MonoBehaviour{
        public GameObject View;
        public void Open(){
            AdeDialogManager.Instance.Open(this);
        }
        public void Close(){
            AdeDialogManager.Instance.Close(this);
        }
        public void SwitchOpenState(){
            AdeDialogManager.Instance.SwitchOpenState(this);
        }
    }

    public class AdeDialogManager : MonoBehaviour
    {
		public static AdeDialogManager Instance { get; private set; }

        public Transform Opening,Closed;

        private void Awake()
		{
			Instance = this;
		}

        public void Open(AdeDialog dialog)
        {
            dialog.transform.SetParent(Opening);
            dialog.View.SetActive(true);
        }

        public void Close(AdeDialog dialog)
        {
            dialog.View.SetActive(false);
            dialog.transform.SetParent(Closed);
        }

        public void SwitchOpenState(AdeDialog dialog)
        {
            if(dialog.View.activeSelf){
                Close(dialog);
            }else{
                Open(dialog);
            }
        }
    }
}