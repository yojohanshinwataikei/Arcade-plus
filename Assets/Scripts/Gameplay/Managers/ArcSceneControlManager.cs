using System.Collections.Generic;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using Arcade.Aff;
using System;

public class ArcSceneControlManager : MonoBehaviour
{
	public static ArcSceneControlManager Instance { get; private set; }
	private void Awake()
	{
		Instance = this;
	}
	public Image BackgroundDarkenLayer;
	public SpriteRenderer[] TrackComponentsRenderers;
	public SpriteRenderer[] DividerRenderers;
	public SpriteRenderer[] ExtraLaneRenderers;
	public SpriteRenderer[] TrackBorderRenderers;
	public SpriteRenderer[] ExtraTrackBorderRenderers;
	public SpriteRenderer[] ExtraLaneDividerRenderers;
	public SpriteRenderer[] ExtraLaneCriticalLineRenderers;
	public Transform SkyInput;
	[HideInInspector]
	public List<ArcSceneControl> SceneControls = new List<ArcSceneControl>();
	private bool trackVisible = true;
	private const float trackAnimationTime = 0.3f;

	public void Load(List<ArcSceneControl> sceneControls)
	{
		SceneControls = sceneControls;
		ResetScene();
	}
	public void Clean()
	{
		SceneControls.Clear();
	}
	public void ResetScene()
	{
		foreach (var r in DividerRenderers)
		{
			r.color = Color.white;
		}
		foreach (var TrackRenderer in TrackComponentsRenderers)
		{
			TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
		}
		trackVisible = true;
		SetEnwidenCameraRatio(0);
		SetEnwidenLaneRatio(0);
	}

	private void Update()
	{
		bool newTrackVisible = true;
		float enwidenCameraRatio = 0;
		float enwidenLaneRatio = 0;
		foreach (ArcTimingGroup tg in ArcTimingManager.Instance.timingGroups)
		{
			tg.GroupHide = false;
		}
		foreach (ArcSceneControl sc in SceneControls.OrderBy(sc => sc.Timing))
		{
			if (sc.Timing + ArcAudioManager.Instance.AudioOffset > ArcGameplayManager.Instance.AudioTiming)
			{
				break;
			}
			switch (sc.Type)
			{
				case SceneControlType.TrackHide:
					newTrackVisible = false;
					break;
				case SceneControlType.TrackShow:
					newTrackVisible = true;
					break;
				case SceneControlType.HideGroup:
					var timingGroup = sc.TimingGroup;
					if (timingGroup != null)
					{
						timingGroup.GroupHide = sc.Enable;
					}
					break;
				case SceneControlType.EnwidenCamera:
					{
						int offset = ArcGameplayManager.Instance.AudioTiming - ArcAudioManager.Instance.AudioOffset - sc.Timing;
						if (offset <= 0)
						{
							enwidenCameraRatio = sc.Enable ? 0 : 1;
						}
						else if (offset >= sc.Duration)
						{
							enwidenCameraRatio = sc.Enable ? 1 : 0;
						}
						else
						{
							float precent = (ArcGameplayManager.Instance.AudioTiming - ArcAudioManager.Instance.AudioOffset - sc.Timing) / sc.Duration;
							enwidenCameraRatio = sc.Enable ? precent : 1 - precent;
						}
					}
					break;
				case SceneControlType.EnwidenLanes:
					{
						int offset = ArcGameplayManager.Instance.AudioTiming - ArcAudioManager.Instance.AudioOffset - sc.Timing;
						if (offset <= 0)
						{
							enwidenLaneRatio = sc.Enable ? 0 : 1;
						}
						else if (offset >= sc.Duration)
						{
							enwidenLaneRatio = sc.Enable ? 1 : 0;
						}
						else
						{
							float precent = (ArcGameplayManager.Instance.AudioTiming - ArcAudioManager.Instance.AudioOffset - sc.Timing) / sc.Duration;
							enwidenLaneRatio = sc.Enable ? precent : 1 - precent;
						}
					}
					break;
			}
		}

		if (newTrackVisible != trackVisible)
		{
			if (newTrackVisible)
			{
				ShowTrack();
			}
			else
			{
				HideTrack();
			}
			trackVisible = newTrackVisible;
		}

		SetEnwidenCameraRatio(enwidenCameraRatio);
		SetEnwidenLaneRatio(enwidenLaneRatio);
	}

	private void SetEnwidenCameraRatio(float enwidenCameraRatio)
	{
		Vector3 SkyInputPosition = SkyInput.localPosition;
		SkyInputPosition.y = 5.5f + enwidenCameraRatio * 2.745f;
		SkyInput.localPosition = SkyInputPosition;
		ArcCameraManager.Instance.EnwidenRatio = enwidenCameraRatio;
	}

	private void SetEnwidenLaneRatio(float enwidenLaneRatio)
	{
		foreach (var ExtraLaneRenderer in ExtraLaneRenderers)
		{
			ExtraLaneRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraLaneRenderer.size = new Vector2(ExtraLaneRenderer.size.x, 53.5f + 100 * enwidenLaneRatio);
			ExtraLaneRenderer.color = new Color(1, 1, 1, enwidenLaneRatio);
		}
		foreach (var TrackBorderRenderer in TrackBorderRenderers)
		{
			TrackBorderRenderer.gameObject.SetActive(enwidenLaneRatio < 1);
			TrackBorderRenderer.color = new Color(1, 1, 1, 1 - enwidenLaneRatio);
		}
		foreach (var ExtraTrackBorderRenderer in ExtraTrackBorderRenderers)
		{
			ExtraTrackBorderRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraTrackBorderRenderer.color = new Color(1, 1, 1, enwidenLaneRatio);
		}
		foreach (var ExtraLaneDividerRenderer in ExtraLaneDividerRenderers)
		{
			ExtraLaneDividerRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraLaneDividerRenderer.color = new Color(1, 1, 1, enwidenLaneRatio);
		}
		foreach (var ExtraLaneCriticalLineRenderer in ExtraLaneCriticalLineRenderers)
		{
			ExtraLaneCriticalLineRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraLaneCriticalLineRenderer.color = new Color(1, 1, 1, enwidenLaneRatio);
		}
		ArcTimingManager.Instance.BeatlineEnwidenRatio=enwidenLaneRatio;
	}

	private void HideTrack()
	{
		foreach (SpriteRenderer r in DividerRenderers)
		{
			r.sharedMaterial.DOKill();
		};
		foreach (var TrackRenderer in TrackComponentsRenderers)
		{
			TrackRenderer.sharedMaterial.DOKill();
		}
		BackgroundDarkenLayer.DOKill();
		if (ArcGameplayManager.Instance.IsPlaying)
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.sharedMaterial.DOColor(Color.clear, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
			};
			foreach (var TrackRenderer in TrackComponentsRenderers)
			{
				TrackRenderer.sharedMaterial.DOColor(Color.clear, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
			}
			BackgroundDarkenLayer.DOColor(Color.black, trackAnimationTime).SetEase(Ease.InCubic);
		}
		else
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.sharedMaterial.SetColor("_Color", Color.clear);
			};
			foreach (var TrackRenderer in TrackComponentsRenderers)
			{
				TrackRenderer.sharedMaterial.SetColor("_Color", Color.clear);
			}
			BackgroundDarkenLayer.color = Color.black;
		}
	}

	private void ShowTrack()
	{
		foreach (SpriteRenderer r in DividerRenderers)
		{
			r.sharedMaterial.DOKill();
		};
		foreach (var TrackRenderer in TrackComponentsRenderers)
		{
			TrackRenderer.sharedMaterial.DOKill();
		}
		BackgroundDarkenLayer.DOKill();
		if (ArcGameplayManager.Instance.IsPlaying)
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.sharedMaterial.DOColor(Color.white, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
			};
			foreach (var TrackRenderer in TrackComponentsRenderers)
			{
				TrackRenderer.sharedMaterial.DOColor(Color.white, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
			}
			BackgroundDarkenLayer.DOColor(Color.clear, trackAnimationTime).SetEase(Ease.OutCubic);
		}
		else
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.sharedMaterial.SetColor("_Color", Color.white);
			};
			foreach (var TrackRenderer in TrackComponentsRenderers)
			{
				TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
			}
			BackgroundDarkenLayer.color = Color.clear;
		}
	}
}

