using UnityEngine;
using UnityEngine.UI;
using Arcade.Util.UnityExtension;
using Arcade.Compose;
using System.Collections.Generic;
using System;

namespace Arcade.Gameplay
{
	public class ArcSkinManager : MonoBehaviour
	{
		public static ArcSkinManager Instance { get; private set; }

		[Header("Background")]
		public Image Background;
		public Image BackgroundDarken;

		[Header("Combo")]
		public Color[] ComboTextColors = new Color[3];
		[Header("InGame")]
		public Image SongInfo;
		public Image DifficultyPast;
		public Image DifficultyPresent;
		public Image DifficultyFuture;
		public Image DifficultyBeyond;

		[Header("Dialog")]
		//TODO: We should use a Dialog Component to handle dialogs
		public List<Image> DialogTops;
		public List<Image> DialogBackgrounds;
		public List<Button> DialogSingleButtons;
		public List<Button> DialogDualLeftButtons;
		public List<Button> DialogDualRightButtons;

		[Header("Shutter")]
		public Image ShutterLeft;
		public Image ShutterRight;
		[Header("SkyInput")]
		public SpriteRenderer SkyInputLabel;
		public SpriteRenderer SkyInputLine;
		[Header("Track")]
		public SpriteRenderer[] TrackComponents;
		public SpriteRenderer[] CriticalLines;
		public SpriteRenderer[] TrackLaneDividers;
		public SpriteRenderer[] LaneHits;
		[Header("Particle")]
		public Material TapJudge;
		public Material HoldJudge;

		[Header("Misc")]
		public Text ComboText;

		public Image TutorialBanner;

		private void Awake()
		{
			Instance = this;
		}

		public void SetSimpleSkin(AdeSkinHost.SkinDatas skinData)
		{
			SongInfo.sprite = skinData.SongInfo.value;
			AdeProjectManager.Instance.SetDefaultCover(skinData.UnknownCover.value);
			ArcadeComposeManager.Instance.SetGlowSliderKnob(skinData.ProgressGlow.value);

			DifficultyPast.sprite = skinData.DifficultyPast.value;
			DifficultyPresent.sprite = skinData.DifficultyPresent.value;
			DifficultyFuture.sprite = skinData.DifficultyFuture.value;
			DifficultyBeyond.sprite = skinData.DifficultyBeyond.value;

			ArcadeComposeManager.Instance.SetPauseSprite(skinData.Pause.value);
			ArcadeComposeManager.Instance.SetPausePressedSprite(skinData.PausePressed.value);
			ArcadeComposeManager.Instance.SetPlaySprite(skinData.Play.value);
			ArcadeComposeManager.Instance.SetPlayPressedSprite(skinData.PlayPressed.value);

			foreach (Image dialogTop in DialogTops)
			{
				dialogTop.sprite = skinData.DialogTop.value;
			}
			foreach (Image dialogBackground in DialogBackgrounds)
			{
				dialogBackground.sprite = skinData.DialogBackground.value;
			}
			foreach (Button dialogSingleButton in DialogSingleButtons)
			{
				dialogSingleButton.image.sprite = skinData.ButtonSingle.value;
				dialogSingleButton.spriteState = new SpriteState
				{
					disabledSprite = skinData.ButtonSingleDisabled.value,
					pressedSprite = skinData.ButtonSinglePressed.value
				};
			}
			foreach (Button dialogDualLeftButton in DialogDualLeftButtons)
			{
				dialogDualLeftButton.image.sprite = skinData.ButtonDualLeft.value;
				dialogDualLeftButton.spriteState = new SpriteState
				{
					disabledSprite = skinData.ButtonDualLeftDisabled.value,
					pressedSprite = skinData.ButtonDualLeftPressed.value
				};
			}
			foreach (Button dialogDualRightButton in DialogDualRightButtons)
			{
				dialogDualRightButton.image.sprite = skinData.ButtonDualRight.value;
				dialogDualRightButton.spriteState = new SpriteState
				{
					disabledSprite = skinData.ButtonDualRightDisabled.value,
					pressedSprite = skinData.ButtonDualRightPressed.value
				};
			}

			ShutterLeft.sprite = skinData.ShutterLeft.value;
			ShutterRight.sprite = skinData.ShutterRight.value;
			AdeShutterManager.Instance.OpenAudio = skinData.ShutterOpen.value;
			AdeShutterManager.Instance.CloseAudio = skinData.ShutterClose.value;
			ArcEffectManager.Instance.TapAudio = skinData.TapSound.value;
			ArcEffectManager.Instance.ArcAudio = skinData.ArcSound.value;

			TutorialBanner.sprite = skinData.TutorialBanner.value;
			BackgroundDarken.sprite = skinData.BackgroundDarken.value;

			SkyInputLabel.sprite=skinData.SkyInputLabel.value;
			SkyInputLine.sprite=skinData.SkyInputLine.value;

			foreach(SpriteRenderer trackLaneDivider in TrackLaneDividers){
				trackLaneDivider.sprite=skinData.TrackLaneDivider.value;
			}
			foreach(SpriteRenderer laneHit in LaneHits){
				laneHit.sprite=skinData.LaneHit.value;
			}

			HoldJudge.SetTexture(Shader.PropertyToID("_MainTex"),skinData.ParticleArc.value);

			ArcArcManager.Instance.SetArcCapSkin(skinData.ArcCap.value);

			ArcArcManager.Instance.SetArcTapShadowSkin(skinData.ArcTapShadow.value);

			ArcArcManager.Instance.SetHeightIndicatorSkin(skinData.HeightIndicator.value);

			ArcArcManager.Instance.SetArcBodySkin(skinData.ArcBody.value,skinData.ArcBodyHighlight.value);
		}
		public void SetNoteSideSkin(AdeSkinHost.NoteSideData noteSideData){
			ArcTapNoteManager.Instance.SetTapNoteSkin(noteSideData.TapNote.value);
			ArcHoldNoteManager.Instance.SetHoldNoteSkin(noteSideData.HoldNote.value,noteSideData.HoldNoteHighlight.value);
			ArcArcManager.Instance.SetArcTapSkin(noteSideData.ArcTap.value);
			ArcTapNoteManager.Instance.SetConnectionLineColor(noteSideData.ConnectionLineColor);
			ArcArcManager.Instance.SetArcColors(noteSideData.ArcRedLow,noteSideData.ArcBlueLow,noteSideData.ArcGreenLow,noteSideData.ArcRedHigh,noteSideData.ArcBlueHigh,noteSideData.ArcGreenHigh,noteSideData.ArcVoid);
		}
		public void SetThemeSideSkin(AdeSkinHost.ThemeSideData themeSideData){
			foreach(SpriteRenderer trackComponent in TrackComponents){
				trackComponent.sprite=themeSideData.Track.value;
			}
			foreach(SpriteRenderer criticalLine in CriticalLines){
				criticalLine.sprite=themeSideData.CriticalLine.value;
			}
			Color ComboTextColor=themeSideData.ComboTextColor;
			ComboTextColor.a=0.75f;
			ComboText.color = ComboTextColor;
			TapJudge.mainTexture=themeSideData.ParticleNote.value;
			ArcEffectManager.Instance.SetParticleArcColor(themeSideData.ParticleArcStartColor,themeSideData.ParticleArcEndColor);
		}

		public void SetBackground(Sprite background)
		{
			Background.sprite=background;
		}
	}
}

