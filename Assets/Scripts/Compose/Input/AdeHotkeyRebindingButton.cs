using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose
{

	public class AdeHotkeyRebindingButton : MonoBehaviour
	{
		public string HotkeyName;
		public Text Text;

		public void StartRebinding()
		{
			AdeInputManager.Instance.SetHotkeyRebindingButton(this);
		}
	}
}