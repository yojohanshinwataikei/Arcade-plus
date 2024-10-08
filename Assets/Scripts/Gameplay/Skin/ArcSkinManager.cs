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
		public Image DifficultyEternal;

		[Header("Dialog")]
		public List<AdeSingleDialog> SingleDialogs;
		public List<AdeDualDialog> DualDialogs;

		[Header("Shutter")]
		public Image ShutterLeft;
		public Image ShutterRight;
		[Header("SkyInput")]
		public SpriteRenderer SkyInputLabel;
		public SpriteRenderer SkyInputLine;
		[Header("Track")]
		public SpriteRenderer[] TrackComponents;
		public SpriteRenderer[] TrackExtraComponents;
		public SpriteRenderer[] CriticalLines;
		public SpriteRenderer[] CriticalLineExtras;
		public SpriteRenderer[] TrackLaneDividers;
		public SpriteRenderer[] LaneHits;

		[Header("Misc")]
		public Text ComboText;

		public Image TutorialBanner;
		public Image Toast;

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
			DifficultyEternal.sprite = skinData.DifficultyEternal.value;

			ArcadeComposeManager.Instance.SetPauseSprite(skinData.Pause.value);
			ArcadeComposeManager.Instance.SetPausePressedSprite(skinData.PausePressed.value);
			ArcadeComposeManager.Instance.SetPlaySprite(skinData.Play.value);
			ArcadeComposeManager.Instance.SetPlayPressedSprite(skinData.PlayPressed.value);

			Toast.sprite = skinData.DialogBackground.value;
			foreach (AdeSingleDialog singleDialog in SingleDialogs)
			{
				singleDialog.SetDialogSkin(
					skinData.DialogTop.value,
					skinData.DialogBackground.value,
					skinData.ButtonSingle.value,
					skinData.ButtonSingleDisabled.value,
					skinData.ButtonSinglePressed.value
				);
			}
			foreach (AdeDualDialog dualDialog in DualDialogs)
			{
				dualDialog.SetDialogSkin(
					skinData.DialogTop.value,
					skinData.DialogBackground.value,
					skinData.ButtonDualLeft.value,
					skinData.ButtonDualLeftDisabled.value,
					skinData.ButtonDualLeftPressed.value,
					skinData.ButtonDualRight.value,
					skinData.ButtonDualRightDisabled.value,
					skinData.ButtonDualRightPressed.value
				);
			}

			ShutterLeft.sprite = skinData.ShutterLeft.value;
			ShutterRight.sprite = skinData.ShutterRight.value;
			AdeShutterManager.Instance.OpenAudio = skinData.ShutterOpen.value;
			AdeShutterManager.Instance.CloseAudio = skinData.ShutterClose.value;
			ArcEffectManager.Instance.TapAudio = skinData.TapSound.value;
			ArcEffectManager.Instance.ArcAudio = skinData.ArcSound.value;

			TutorialBanner.sprite = skinData.TutorialBanner.value;
			BackgroundDarken.sprite = skinData.BackgroundDarken.value;

			SkyInputLabel.sprite = skinData.SkyInputLabel.value;
			SkyInputLine.sprite = skinData.SkyInputLine.value;

			foreach (SpriteRenderer trackLaneDivider in TrackLaneDividers)
			{
				trackLaneDivider.sprite = skinData.TrackLaneDivider.value;
			}
			foreach (SpriteRenderer laneHit in LaneHits)
			{
				laneHit.sprite = skinData.LaneHit.value;
			}

			ArcEffectManager.Instance.SetSfxTapEffectTexture(skinData.ParticleSfxTap.value);
			ArcEffectManager.Instance.SetParticleArcTexture(skinData.ParticleArc.value);

			ArcArcManager.Instance.SetArcCapSkin(skinData.ArcCap.value);

			ArcArcManager.Instance.SetArcTapShadowSkin(skinData.ArcTapShadow.value);

			ArcArcManager.Instance.SetHeightIndicatorSkin(skinData.HeightIndicator.value);

			ArcArcManager.Instance.SetArcBodySkin(skinData.ArcBody.value, skinData.ArcBodyHighlight.value);

			ArcArcManager.Instance.SetSfxArcTapModel(skinData.SfxArcTapModel.value);
			AdeCursorManager.Instance.SfxArcTapCursorRenderer.GetComponent<MeshFilter>().mesh = skinData.SfxArcTapModel.value;
		}
		public void SetNoteSideSkin(AdeSkinHost.NoteSideData noteSideData)
		{
			ArcTapNoteManager.Instance.SetTapNoteSkin(noteSideData.TapNote.value);
			ArcHoldNoteManager.Instance.SetHoldNoteSkin(noteSideData.HoldNote.value, noteSideData.HoldNoteHighlight.value);
			ArcArcManager.Instance.SetArcTapSkin(noteSideData.ArcTap.value);
			ArcArcManager.Instance.SetSfxArcTapSkin(noteSideData.SfxArcTapNote.value, noteSideData.SfxArcTapCore.value);
			ArcTapNoteManager.Instance.SetConnectionLineColor(noteSideData.ConnectionLineColor);
			ArcArcManager.Instance.SetArcColors(
				noteSideData.ArcRedLow, noteSideData.ArcBlueLow, noteSideData.ArcGreenLow, noteSideData.ArcUnknownLow,
				noteSideData.ArcRedHigh, noteSideData.ArcBlueHigh, noteSideData.ArcGreenHigh, noteSideData.ArcUnknownHigh,
				noteSideData.ArcVoid);
		}
		public void SetThemeSideSkin(AdeSkinHost.ThemeSideData themeSideData)
		{
			foreach (SpriteRenderer trackComponent in TrackComponents)
			{
				trackComponent.sprite = themeSideData.Track.value;
			}
			foreach (SpriteRenderer trackExtraComponent in TrackExtraComponents)
			{
				trackExtraComponent.sprite = themeSideData.TrackExtra.value;
			}
			foreach (SpriteRenderer criticalLine in CriticalLines)
			{
				criticalLine.sprite = themeSideData.CriticalLine.value;
			}
			foreach (SpriteRenderer criticalLineExtra in CriticalLineExtras)
			{
				criticalLineExtra.sprite = themeSideData.CriticalLineExtra.value;
			}
			Color ComboTextColor = themeSideData.ComboTextColor;
			ComboTextColor.a = 0.75f;
			ComboText.color = ComboTextColor;
			ArcEffectManager.Instance.SetTapEffectTexture(themeSideData.ParticleNote.value);
			ArcEffectManager.Instance.SetParticleArcColor(themeSideData.ParticleArcStartColor, themeSideData.ParticleArcEndColor);
		}

		public void SetBackground(Sprite background)
		{
			Background.sprite = background;
		}
	}
}

