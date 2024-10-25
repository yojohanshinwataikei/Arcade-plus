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
	private const float trackAnimationDefaultTime = 1f;

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
		SetEnwidenCameraRatio(0);
		UpdateLane(1, 0);
	}

	private void Update()
	{
		float startLaneOpacity = 1;
		float laneOpacity = 1;
		float backgroundDarkenProgress = 0;
		float enwidenCameraRatio = 0;
		float enwidenLaneRatio = 0;
		foreach (ArcTimingGroup tg in ArcTimingManager.Instance.timingGroups)
		{
			tg.GroupHide = false;
		}

		Debug.Log("====");
		foreach (ArcSceneControl sc in SceneControls.OrderBy(sc => sc.Timing))
		{
			if (sc.Timing > ArcGameplayManager.Instance.ChartTiming)
			{
				break;
			}
			switch (sc.Type)
			{
				case SceneControlType.TrackHide:
					{
						float animationProgress = Mathf.Clamp01((ArcGameplayManager.Instance.ChartTiming - sc.Timing) / (trackAnimationDefaultTime * 1000));
						laneOpacity = Mathf.Lerp(startLaneOpacity, 0, animationProgress);
						Debug.Log($"TrackHide {sc.Timing} {startLaneOpacity} -> {laneOpacity}");
						if (ArcGameplayManager.Instance.ChartTiming - sc.Timing > trackAnimationDefaultTime * 1000)
						{
							startLaneOpacity = 0;
						}
					}
					break;
				case SceneControlType.TrackShow:
					{
						float animationProgress = Mathf.Clamp01((ArcGameplayManager.Instance.ChartTiming - sc.Timing) / (trackAnimationDefaultTime * 1000));
						laneOpacity = Mathf.Lerp(startLaneOpacity, 1, animationProgress);
						Debug.Log($"TrackShow {sc.Timing} {startLaneOpacity} -> {laneOpacity}");
						if (ArcGameplayManager.Instance.ChartTiming - sc.Timing > trackAnimationDefaultTime * 1000)
						{
							startLaneOpacity = 1;
						}
					}
					break;
				case SceneControlType.TrackDisplay:
					{
						float animationProgress = Mathf.Clamp01((ArcGameplayManager.Instance.ChartTiming - sc.Timing) / (sc.Duration * 1000));
						float targetLaneOpacity = ((float)(sc.TrackDisplayValue % 256)) / 255;
						laneOpacity = Mathf.Lerp(startLaneOpacity, targetLaneOpacity, animationProgress);
						Debug.Log($"TrackDisplay {sc.Timing}+{sc.Duration}->{sc.TrackDisplayValue} {startLaneOpacity} {laneOpacity}");
						if (ArcGameplayManager.Instance.ChartTiming - sc.Timing > sc.Duration * 1000)
						{
							startLaneOpacity = targetLaneOpacity;
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
		SetEnwidenCameraRatio(enwidenCameraRatio);
		UpdateLane(laneOpacity, enwidenLaneRatio);
	}

	private void SetEnwidenCameraRatio(float enwidenCameraRatio)
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

	// private void HideTrack()
	// {
	// 	foreach (SpriteRenderer r in DividerRenderers)
	// 	{
	// 		r.sharedMaterial.DOKill();
	// 	};
	// 	foreach (var TrackRenderer in TrackComponentsRenderers)
	// 	{
	// 		TrackRenderer.sharedMaterial.DOKill();
	// 	}
	// 	BackgroundDarkenLayer.DOKill();
	// 	if (ArcGameplayManager.Instance.IsPlaying)
	// 	{
	// 		foreach (SpriteRenderer r in DividerRenderers)
	// 		{
	// 			r.sharedMaterial.DOColor(Color.clear, "_Color", trackAnimationDefaultTime).SetEase(Ease.InCubic);
	// 		};
	// 		foreach (var TrackRenderer in TrackComponentsRenderers)
	// 		{
	// 			TrackRenderer.sharedMaterial.DOColor(Color.clear, "_Color", trackAnimationDefaultTime).SetEase(Ease.InCubic);
	// 		}
	// 		BackgroundDarkenLayer.DOColor(Color.black, trackAnimationDefaultTime).SetEase(Ease.InCubic);
	// 	}
	// 	else
	// 	{
	// 		foreach (SpriteRenderer r in DividerRenderers)
	// 		{
	// 			r.sharedMaterial.SetColor("_Color", Color.clear);
	// 		};
	// 		foreach (var TrackRenderer in TrackComponentsRenderers)
	// 		{
	// 			TrackRenderer.sharedMaterial.SetColor("_Color", Color.clear);
	// 		}
	// 		BackgroundDarkenLayer.color = Color.black;
	// 	}
	// }

	// private void ShowTrack()
	// {
	// 	foreach (SpriteRenderer r in DividerRenderers)
	// 	{
	// 		r.sharedMaterial.DOKill();
	// 	};
	// 	foreach (var TrackRenderer in TrackComponentsRenderers)
	// 	{
	// 		TrackRenderer.sharedMaterial.DOKill();
	// 	}
	// 	BackgroundDarkenLayer.DOKill();
	// 	if (ArcGameplayManager.Instance.IsPlaying)
	// 	{
	// 		foreach (SpriteRenderer r in DividerRenderers)
	// 		{
	// 			r.sharedMaterial.DOColor(Color.white, "_Color", trackAnimationDefaultTime).SetEase(Ease.InCubic);
	// 		};
	// 		foreach (var TrackRenderer in TrackComponentsRenderers)
	// 		{
	// 			TrackRenderer.sharedMaterial.DOColor(Color.white, "_Color", trackAnimationDefaultTime).SetEase(Ease.InCubic);
	// 		}
	// 		BackgroundDarkenLayer.DOColor(Color.clear, trackAnimationDefaultTime).SetEase(Ease.OutCubic);
	// 	}
	// 	else
	// 	{
	// 		foreach (SpriteRenderer r in DividerRenderers)
	// 		{
	// 			r.sharedMaterial.SetColor("_Color", Color.white);
	// 		};
	// 		foreach (var TrackRenderer in TrackComponentsRenderers)
	// 		{
	// 			TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
	// 		}
	// 		BackgroundDarkenLayer.color = Color.clear;
	// 	}
	// }
}

