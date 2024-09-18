using UnityEngine;

namespace Arcade.Compose
{
    public class AdeHotkeyDialogContent : AdeDialogContent<AdeSingleDialog>
    {
        public AdeHotkeyRebindingButton[] RebindingButtons;

        public void Awake(){
            Dialog.OnOpen+=OnOpen;
            Dialog.OnClose+=OnClose;
        }

        public void OnDestroy(){
            Dialog.OnOpen-=OnOpen;
            Dialog.OnClose-=OnClose;
        }
        public void OnOpen()
        {
            foreach (var button in RebindingButtons)
            {
                AdeInputManager.Instance.UpdateTextForHotkeyButton(button);
            }
        }

        public void OnClose()
        {
            AdeInputManager.Instance.SetHotkeyRebindingButton(null);
        }
    }
}