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
	public SpriteRenderer[] BaseTrackComponentsRenderer;
	public SpriteRenderer[] DividerRenderers;
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
		foreach (var TrackRenderer in BaseTrackComponentsRenderer)
		{
			TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
		}
		trackVisible = true;
	}

	private void Update()
	{
		bool newTrackVisible = true;
		float enwidenCameraPrecent = 0;
		foreach (ArcTimingGroup tg in ArcTimingManager.Instance.timingGroups)
		{
			tg.GroupHide = false;
		}
		foreach (ArcSceneControl sc in SceneControls.OrderBy(sc => sc.Timing))
		{
			if (sc.Timing + ArcAudioManager.Instance.AudioOffset > ArcGameplayManager.Instance.Timing)
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
					int offset = ArcGameplayManager.Instance.Timing - ArcAudioManager.Instance.AudioOffset - sc.Timing;
					if (offset <= 0)
					{
						enwidenCameraPrecent = sc.Enable ? 0 : 1;
					}
					else if (offset >= sc.Duration)
					{
						enwidenCameraPrecent = sc.Enable ? 1 : 0;
					}
					else
					{
						float precent = (ArcGameplayManager.Instance.Timing - ArcAudioManager.Instance.AudioOffset - sc.Timing) / sc.Duration;
						enwidenCameraPrecent = sc.Enable ? precent : 1 - precent;
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

		Vector3 SkyInputPosition = SkyInput.localPosition;
		SkyInputPosition.y = 5.5f + enwidenCameraPrecent * 2.745f;
		SkyInput.localPosition = SkyInputPosition;
		ArcCameraManager.Instance.EnwidenPrecent = enwidenCameraPrecent;
	}

	private void HideTrack()
	{
		foreach (SpriteRenderer r in DividerRenderers)
		{
			r.DOKill();
		};
		foreach (var TrackRenderer in BaseTrackComponentsRenderer)
		{
			TrackRenderer.sharedMaterial.DOKill();
		}
		BackgroundDarkenLayer.DOKill();
		if (ArcGameplayManager.Instance.IsPlaying)
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.DOFade(0, trackAnimationTime).SetEase(Ease.InCubic);
			};
			foreach (var TrackRenderer in BaseTrackComponentsRenderer)
			{
				TrackRenderer.sharedMaterial.DOColor(Color.clear, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
			}
			BackgroundDarkenLayer.DOColor(Color.black, trackAnimationTime).SetEase(Ease.InCubic);
		}
		else
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.color = Color.clear;
			};
			foreach (var TrackRenderer in BaseTrackComponentsRenderer)
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
			r.DOKill();
		};
		foreach (var TrackRenderer in BaseTrackComponentsRenderer)
		{
			TrackRenderer.sharedMaterial.DOKill();
		}
		if (ArcGameplayManager.Instance.IsPlaying)
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.DOFade(1, trackAnimationTime).SetEase(Ease.InCubic);
			};
			foreach (var TrackRenderer in BaseTrackComponentsRenderer)
			{
				TrackRenderer.sharedMaterial.DOColor(Color.white, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
			}
			BackgroundDarkenLayer.DOColor(Color.clear, trackAnimationTime).SetEase(Ease.OutCubic);
		}
		else
		{
			foreach (SpriteRenderer r in DividerRenderers)
			{
				r.color = Color.white;
			};
			foreach (var TrackRenderer in BaseTrackComponentsRenderer)
			{
				TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
			}
			BackgroundDarkenLayer.color = Color.clear;
		}
	}
}

