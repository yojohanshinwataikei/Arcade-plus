using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose
{
	public class AdeDualDialog : AdeDialog
	{
		public Button LeftButton, RightButton;
		public Text Title, LeftButtonText, RightButtonText;
		public Image DialogTop, DialogBackground;

		public void SetDialogSkin(
			Sprite dialogTop, Sprite dialogBackground,
			Sprite buttonLeft, Sprite buttonLeftDisabled, Sprite buttonLeftPressed,
			Sprite buttonRight, Sprite buttonRightDisabled, Sprite buttonRightPressed
		)
		{
			DialogTop.sprite=dialogTop;
			DialogBackground.sprite=dialogBackground;
			LeftButton.image.sprite = buttonLeft;
			LeftButton.spriteState = new SpriteState
			{
				disabledSprite = buttonLeftDisabled,
				pressedSprite = buttonLeftPressed
			};
			RightButton.image.sprite = buttonRight;
			RightButton.spriteState = new SpriteState
			{
				disabledSprite = buttonRightDisabled,
				pressedSprite = buttonRightPressed
			};
		}
	}
}