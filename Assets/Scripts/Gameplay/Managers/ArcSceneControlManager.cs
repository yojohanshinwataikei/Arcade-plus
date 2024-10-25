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
	public SpriteRenderer[] TrackMainRenderers;
	public SpriteRenderer[] TrackBorderRenderers;
	public SpriteRenderer[] ExtraLaneRenderers;
	public SpriteRenderer[] ExtraTrackBorderRenderers;
	public SpriteRenderer[] MainLaneDividerRenderers;
	public SpriteRenderer[] ExtraLaneDividerRenderers;
	public SpriteRenderer[] ExtraLaneCriticalLineRenderers;
	public Transform SkyInput;
	[HideInInspector]
	public List<ArcSceneControl> SceneControls = new List<ArcSceneControl>();
	private const float trackAnimationDefaultDuration = 1f;
	private const float backgroundDarkenDuration = 0.2f;

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
		UpdateEnwidenCameraRatio(0);
		UpdateLane(1, 0);
	}

	private void Update()
	{
		float startLaneOpacity = 1;
		float laneOpacity = 1;
		bool startBackgroundDarken = false;
		float backgroundDarkenProgress = 0;
		float enwidenCameraRatio = 0;
		float enwidenLaneRatio = 0;
		foreach (ArcTimingGroup tg in ArcTimingManager.Instance.timingGroups)
		{
			tg.GroupHide = false;
		}

		foreach (ArcSceneControl sc in SceneControls.OrderBy(sc => sc.Timing))
		{
			if (sc.Timing > ArcGameplayManager.Instance.ChartTiming)
			{
				break;
			}
			switch (sc.Type)
			{
				case SceneControlType.TrackHide:
				case SceneControlType.TrackShow:
				case SceneControlType.TrackDisplay:
					{
						float animationDuration = trackAnimationDefaultDuration;
						if (sc.Type == SceneControlType.TrackDisplay)
						{
							animationDuration = sc.Duration;
						}
						float targetLaneOpacity = 0;
						if (sc.Type == SceneControlType.TrackShow)
						{
							targetLaneOpacity = 1;
						}
						else if (sc.Type == SceneControlType.TrackHide)
						{
							targetLaneOpacity = 0;
						}
						else if (sc.Type == SceneControlType.TrackDisplay)
						{
							animationDuration = sc.Duration;
							targetLaneOpacity = ((float)(sc.TrackDisplayValue % 256)) / 255;
						}
						float animationProgress = Mathf.Clamp01((ArcGameplayManager.Instance.ChartTiming - sc.Timing) / (animationDuration * 1000));
						laneOpacity = Mathf.Lerp(startLaneOpacity, targetLaneOpacity, animationProgress);
						if (ArcGameplayManager.Instance.ChartTiming - sc.Timing > animationDuration * 1000)
						{
							startLaneOpacity = targetLaneOpacity;
						}

						bool backgroundDarken = true;
						if (sc.Type == SceneControlType.TrackShow)
						{
							backgroundDarken = false;
						}
						else if (sc.Type == SceneControlType.TrackHide)
						{
							backgroundDarken = true;
						}
						else if (sc.Type == SceneControlType.TrackDisplay)
						{
							backgroundDarken = sc.TrackDisplayValue < 255;
						}
						if (startBackgroundDarken == backgroundDarken)
						{
							backgroundDarkenProgress = backgroundDarken ? 1 : 0;
						}
						else
						{
							float darkenProgress = Mathf.Clamp01((ArcGameplayManager.Instance.ChartTiming - sc.Timing) / (backgroundDarkenDuration * 1000));
							float curvedDarkenProgress = (1 - darkenProgress) * (1 - darkenProgress) * (1 - darkenProgress);
							backgroundDarkenProgress = backgroundDarken ? 1 - curvedDarkenProgress : curvedDarkenProgress;
						}
						if (ArcGameplayManager.Instance.ChartTiming - sc.Timing > backgroundDarkenDuration * 1000)
						{
							startBackgroundDarken = backgroundDarken;
						}
					}
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
						int offset = ArcGameplayManager.Instance.ChartTiming - sc.Timing;
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
							float precent = (ArcGameplayManager.Instance.ChartTiming - sc.Timing) / sc.Duration;
							enwidenCameraRatio = sc.Enable ? precent : 1 - precent;
						}
					}
					break;
				case SceneControlType.EnwidenLanes:
					{
						int offset = ArcGameplayManager.Instance.ChartTiming - sc.Timing;
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
							float precent = (ArcGameplayManager.Instance.ChartTiming - sc.Timing) / sc.Duration;
							enwidenLaneRatio = sc.Enable ? precent : 1 - precent;
						}
					}
					break;
			}
		}
		UpdateEnwidenCameraRatio(enwidenCameraRatio);
		UpdateLane(laneOpacity, enwidenLaneRatio);
		UpdateBackgroundDarkenLayer(backgroundDarkenProgress);
	}

	private void UpdateEnwidenCameraRatio(float enwidenCameraRatio)
	{
		Vector3 SkyInputPosition = SkyInput.localPosition;
		SkyInputPosition.y = 5.5f + enwidenCameraRatio * 2.745f;
		SkyInput.localPosition = SkyInputPosition;
		ArcCameraManager.Instance.EnwidenRatio = enwidenCameraRatio;
	}

	private void UpdateLane(float laneOpacity, float enwidenLaneRatio)
	{
		foreach (var ExtraLaneRenderer in ExtraLaneRenderers)
		{
			ExtraLaneRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraLaneRenderer.size = new Vector2(ExtraLaneRenderer.size.x, 53.5f + 100 * enwidenLaneRatio);
			ExtraLaneRenderer.color = new Color(1, 1, 1, enwidenLaneRatio * laneOpacity);
		}
		foreach (var TrackMainRenderer in TrackMainRenderers)
		{
			TrackMainRenderer.color = new Color(1, 1, 1, laneOpacity);
		}
		foreach (var TrackBorderRenderer in TrackBorderRenderers)
		{
			TrackBorderRenderer.gameObject.SetActive(enwidenLaneRatio < 1);
			TrackBorderRenderer.color = new Color(1, 1, 1, (1 - enwidenLaneRatio) * laneOpacity);
		}
		foreach (var ExtraTrackBorderRenderer in ExtraTrackBorderRenderers)
		{
			ExtraTrackBorderRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraTrackBorderRenderer.color = new Color(1, 1, 1, enwidenLaneRatio * laneOpacity);
		}
		foreach (var MainLaneDividerRenderer in MainLaneDividerRenderers)
		{
			MainLaneDividerRenderer.color = new Color(1, 1, 1, laneOpacity);
		}
		foreach (var ExtraLaneDividerRenderer in ExtraLaneDividerRenderers)
		{
			ExtraLaneDividerRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraLaneDividerRenderer.color = new Color(1, 1, 1, enwidenLaneRatio * laneOpacity);
		}
		foreach (var ExtraLaneCriticalLineRenderer in ExtraLaneCriticalLineRenderers)
		{
			ExtraLaneCriticalLineRenderer.gameObject.SetActive(enwidenLaneRatio > 0);
			ExtraLaneCriticalLineRenderer.color = new Color(1, 1, 1, enwidenLaneRatio);
		}
		ArcTimingManager.Instance.BeatlineEnwidenRatio = enwidenLaneRatio;
	}

	private void UpdateBackgroundDarkenLayer(float backgroundDarkenProgress)
	{
		BackgroundDarkenLayer.color = new Color(0, 0, 0, backgroundDarkenProgress);
	}
}

