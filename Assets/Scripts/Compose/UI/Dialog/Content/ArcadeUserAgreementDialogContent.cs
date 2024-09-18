using UnityEngine;

namespace Arcade.Compose.Dialog
{
	public class ArcadeUserAgreementDialogContent : AdeDialogContent<AdeDualDialog>
	{
		public static ArcadeUserAgreementDialogContent Instance { get; private set; }
		public const int CurrentUserAgreementVersion = 1;
		private void Awake()
		{
			Instance = this;
		}
		public void Agree()
		{
			ArcadeComposeManager.Instance.ArcadePreference.AgreedUserAgreementVersion = CurrentUserAgreementVersion;
			CloseDialog();
		}
		public void Disagree()
		{
			Application.Quit();
		}
	}
}
