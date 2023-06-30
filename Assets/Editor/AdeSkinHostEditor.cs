
using Arcade.Compose;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static Arcade.Compose.AdeSkinHost;
using System;

[CustomEditor(typeof(AdeSkinHost))]
public class AdeSkinHostEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (!Application.isPlaying) return;
		ShowSkinData(AdeSkinHost.Instance.skinData);
		GUILayout.Label("External backgrounds:");
		foreach(KeyValuePair<string, Labelled<Sprite>> backgroundData in AdeSkinHost.Instance.ExternalBackgrounds){
			GUILayout.Label($"  {backgroundData.Key}:{backgroundData.Value.label}");
		}
	}

	private void ShowSkinData(AdeSkinHost.SkinDatas data)
	{
		if (data == null)
		{
			GUILayout.Label("Skin data not loaded");
			return;
		}
		GUILayout.Label("Simple skin-able elements:");
		GUILayout.Label($"  SongInfo:{data.SongInfo.label}");
		GUILayout.Label($"  ProgressGlow:{data.ProgressGlow.label}");
		GUILayout.Label($"  UnknownCover:{data.UnknownCover.label}");
		GUILayout.Label($"  DifficultyPast:{data.DifficultyPast.label}");
		GUILayout.Label($"  DifficultyPresent:{data.DifficultyPresent.label}");
		GUILayout.Label($"  DifficultyFuture:{data.DifficultyFuture.label}");
		GUILayout.Label($"  DifficultyBeyond:{data.DifficultyBeyond.label}");
		GUILayout.Label($"  Pause:{data.Pause.label}");
		GUILayout.Label($"  PausePressed:{data.PausePressed.label}");
		GUILayout.Label($"  Play:{data.Play.label}");
		GUILayout.Label($"  PlayPressed:{data.PlayPressed.label}");
		GUILayout.Label($"  DialogTop:{data.DialogTop.label}");
		GUILayout.Label($"  DialogBackground:{data.DialogBackground.label}");
		GUILayout.Label($"  ButtonSingle:{data.ButtonSingle.label}");
		GUILayout.Label($"  ButtonSinglePressed:{data.ButtonSinglePressed.label}");
		GUILayout.Label($"  ButtonSingleDisabled:{data.ButtonSingleDisabled.label}");
		GUILayout.Label($"  ButtonDualLeft:{data.ButtonDualLeft.label}");
		GUILayout.Label($"  ButtonDualLeftPressed:{data.ButtonDualLeftPressed.label}");
		GUILayout.Label($"  ButtonDualLeftDisabled:{data.ButtonDualLeftDisabled.label}");
		GUILayout.Label($"  ButtonDualRight:{data.ButtonDualRight.label}");
		GUILayout.Label($"  ButtonDualRightPressed:{data.ButtonDualRightPressed.label}");
		GUILayout.Label($"  ButtonDualRightDisabled:{data.ButtonDualRightDisabled.label}");
		GUILayout.Label($"  ShutterLeft:{data.ShutterLeft.label}");
		GUILayout.Label($"  ShutterRight:{data.ShutterRight.label}");
		GUILayout.Label($"  ShutterOpen:{data.ShutterOpen.label}");
		GUILayout.Label($"  ShutterClose:{data.ShutterClose.label}");
		GUILayout.Label($"  TapSound:{data.TapSound.label}");
		GUILayout.Label($"  ArcSound:{data.ArcSound.label}");
		GUILayout.Label($"  TutorialBanner:{data.TutorialBanner.label}");
		GUILayout.Label($"  BackgroundDarken:{data.BackgroundDarken.label}");
		GUILayout.Label($"  SkyInputLabel:{data.SkyInputLabel.label}");
		GUILayout.Label($"  SkyInputLine:{data.SkyInputLine.label}");
		GUILayout.Label($"  SkyInputLabel:{data.SkyInputLabel.label}");
		GUILayout.Label($"  TrackLaneDivider:{data.TrackLaneDivider.label}");
		GUILayout.Label($"  LaneHit:{data.LaneHit.label}");
		GUILayout.Label($"  ParticleArc:{data.ParticleArc.label}");
		GUILayout.Label($"  ParticleArc:{data.ParticleSfxTap.label}");
		GUILayout.Label($"  ArcCap:{data.ArcCap.label}");
		GUILayout.Label($"  ArcTapShadow:{data.ArcTapShadow.label}");
		GUILayout.Label($"  HeightIndicator:{data.HeightIndicator.label}");
		GUILayout.Label($"  ArcBody:{data.ArcBody.label}");
		GUILayout.Label($"  ArcBodyHighlight:{data.ArcBodyHighlight.label}");
		GUILayout.Label($"  SfxArcTapModel:{data.SfxArcTapModel.label}");
		GUILayout.Label($"Default note type:{data.DefaultNoteData}");
		GUILayout.Label("Note datas:");
		foreach(KeyValuePair<string, WithSideData<NoteSideData>> noteData in data.NoteDatas){
			GUILayout.Label($"  {noteData.Key}:");
			ShowWithSideData<NoteSideData>(noteData.Value,ShowNoteSideData);
		}
		GUILayout.Label($"Default theme type:{data.DefaultThemeData}");
		GUILayout.Label("Theme datas:");
		foreach(KeyValuePair<string, WithSideData<ThemeSideData>> themeData in data.ThemeDatas){
			GUILayout.Label($"  {themeData.Key}:");
			ShowWithSideData<ThemeSideData>(themeData.Value,ShowThemeSideData);
		}
		GUILayout.Label($"Default background:{data.DefaultBackground}");
		GUILayout.Label("Background datas:");
		foreach(KeyValuePair<string, BackgroundData> backgroundData in data.BackgroundDatas){
			GUILayout.Label($"  {backgroundData.Key}:");
			ShowBackground(backgroundData.Value);
		}
	}

	private void ShowBackground(BackgroundData data)
	{
		GUILayout.Label($"    Background:{data.background.label}");
		GUILayout.Label($"    Side:{data.side}");
		GUILayout.Label($"    Theme:{data.theme}");
	}

	private delegate void dataShower<T>(T data);

	private void ShowWithSideData<T>(WithSideData<T> data,dataShower<T> shower)
	{
		GUILayout.Label("    Light:");
		shower(data.Light);
		GUILayout.Label("    Conflict:");
		shower(data.Conflict);
	}

	private void ShowNoteSideData(NoteSideData data)
	{
		GUILayout.Label($"      TapNote:{data.TapNote.label}");
		GUILayout.Label($"      HoldNote:{data.HoldNote.label}");
		GUILayout.Label($"      HoldNoteHighlight:{data.HoldNoteHighlight.label}");
		GUILayout.Label($"      ArcTap:{data.ArcTap.label}");
		GUILayout.Label($"      ConnectionLineColor:{data.ConnectionLineColor}");
		GUILayout.Label($"      ArcRedLow:{data.ArcRedLow}");
		GUILayout.Label($"      ArcBlueLow:{data.ArcBlueLow}");
		GUILayout.Label($"      ArcGreenLow:{data.ArcGreenLow}");
		GUILayout.Label($"      ArcRedHigh:{data.ArcRedHigh}");
		GUILayout.Label($"      ArcBlueHigh:{data.ArcBlueHigh}");
		GUILayout.Label($"      ArcGreenHigh:{data.ArcGreenHigh}");
		GUILayout.Label($"      ArcVoid:{data.ArcVoid}");
	}

	private void ShowThemeSideData(ThemeSideData data)
	{
		GUILayout.Label($"      Track:{data.Track.label}");
		GUILayout.Label($"      TrackExtra:{data.TrackExtra.label}");
		GUILayout.Label($"      CriticalLine:{data.CriticalLine.label}");
		GUILayout.Label($"      CriticalLineExtra:{data.CriticalLineExtra.label}");
		GUILayout.Label($"      ComboTextColor:{data.ComboTextColor}");
		GUILayout.Label($"      ParticleNote:{data.ParticleNote.label}");
		GUILayout.Label($"      ParticleArcStartColor:{data.ParticleArcStartColor}");
		GUILayout.Label($"      ParticleArcEndColor:{data.ParticleArcEndColor}");
	}
}
