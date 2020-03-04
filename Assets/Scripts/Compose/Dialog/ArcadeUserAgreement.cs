using UnityEngine;

namespace Arcade.Compose.Dialog
{
	public class ArcadeUserAgreement : MonoBehaviour
	{
		public static ArcadeUserAgreement Instance { get; private set; }
		public const int CurrentUserAgreementVersion = 1;
		private void Awake()
		{
			Instance = this;
		}
		public RectTransform View;

		public void Show()
		{
			View.gameObject.SetActive(true);
		}
		public void Agree()
		{
			ArcadeComposeManager.Instance.ArcadePreference.AgreedUserAgreementVersion = CurrentUserAgreementVersion;
			View.gameObject.SetActive(false);
		}
		public void Disagree()
		{
			Application.Quit();
		}
	}
}