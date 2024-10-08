using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose
{
	public class AdeSingleDialog : AdeDialog
	{
		public Button CompleteButton;
		public Text Title, ButtonText;
		public Image DialogTop, DialogBackground;

		public void SetDialogSkin(
			Sprite dialogTop, Sprite dialogBackground,
			Sprite buttonSingle, Sprite buttonSingleDisabled, Sprite buttonSinglePressed
		)
		{
			DialogTop.sprite = dialogTop;
			DialogBackground.sprite = dialogBackground;
			CompleteButton.image.sprite = buttonSingle;
			CompleteButton.spriteState = new SpriteState
			{
				disabledSprite = buttonSingleDisabled,
				pressedSprite = buttonSinglePressed
			};
		}
	}
}