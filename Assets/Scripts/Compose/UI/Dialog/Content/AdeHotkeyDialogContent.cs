using UnityEngine;

namespace Arcade.Compose
{
    public class AdeHotkeyDialogContent : AdeDialogContent<AdeSingleDialog>
    {
        public AdeHotkeyRebindingButton[] RebindingButtons;
        public void Show()
        {
            foreach (var button in RebindingButtons)
            {
                AdeInputManager.Instance.UpdateTextForHotkeyButton(button);
            }
            OpenDialog();
        }

        public void Hide()
        {
            AdeInputManager.Instance.SetHotkeyRebindingButton(null);
            CloseDialog();
        }

        public void SwitchOpenState()
        {
            if(Dialog.View.activeSelf){
                Hide();
            }else{
                Show();
            }
        }
    }
}