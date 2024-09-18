using UnityEngine;

namespace Arcade.Compose
{
    public class AdeHotkeyDialog : MonoBehaviour
    {
        public GameObject View;
        public AdeHotkeyRebindingButton[] RebindingButtons;
        public void Show()
        {
            foreach (var button in RebindingButtons)
            {
                AdeInputManager.Instance.UpdateTextForHotkeyButton(button);
            }
            View.SetActive(true);
        }

        public void Hide()
        {
            AdeInputManager.Instance.SetHotkeyRebindingButton(null);
            View.SetActive(false);
        }
    }
}