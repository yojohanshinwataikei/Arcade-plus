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
    public SpriteRenderer TrackRenderer;
    public SpriteRenderer[] DividerRenderers;
    [HideInInspector]
    public List<ArcSceneControl> SceneControls = new List<ArcSceneControl>();
    private bool trackVisible = true;
    private const float trackAnimationTime=0.3f;

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
        TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
        trackVisible = true;
    }

    private void Update()
    {
        bool newTrackVisible = true;
        foreach (ArcSceneControl sc in SceneControls.OrderBy(sc=>sc.Timing))
        {
            if(sc.Timing>ArcGameplayManager.Instance.Timing+ArcAudioManager.Instance.AudioOffset){
                break;
            }
            switch (sc.Type){
                case SceneControlType.TrackHide:
                    newTrackVisible = false;
                    break;
                case SceneControlType.TrackShow:
                    newTrackVisible = true;
                    break;
            }
        }
        if(newTrackVisible!=trackVisible){
            if(newTrackVisible){
                ShowTrack();
            }else{
                HideTrack();
            }
            trackVisible=newTrackVisible;
        }
        // int timing = ArcGameplayManager.Instance.Timing;
        // int offset = ArcAudioManager.Instance.AudioOffset;
        // bool playText = false, playTrack = false, track = true;
        // string text = null;
        // foreach (var s in SceneControls)
        // {
        //     if (timing < s.Timing + offset) break;
        //     switch (s.Type)
        //     {
        //         case Arcade.Aff.SceneControlType.TextArea:
        //             if (s.Param1 == "in")
        //             {
        //                 if (!s.Played)
        //                 {
        //                     s.Played = true;
        //                     playText = true;
        //                 }
        //                 text = s.Param2;
        //             }
        //             else if (s.Param1 == "out")
        //             {
        //                 if (!s.Played)
        //                 {
        //                     s.Played = true;
        //                     playText = true;
        //                 }
        //                 text = null;
        //             }
        //             break;
        //         case Arcade.Aff.SceneControlType.Fade:
        //             if (s.Param1 == "in")
        //             {
        //                 if (!s.Played)
        //                 {
        //                     s.Played = true;
        //                     playTrack = true;
        //                 }
        //                 track = true;
        //             }
        //             else if (s.Param1 == "out")
        //             {
        //                 if (!s.Played)
        //                 {
        //                     s.Played = true;
        //                     playTrack = true;
        //                 }
        //                 track = false;
        //             }
        //             break;
        //     }
        // }
        // if (dispTrack != track)
        // {
        //     dispTrack = track;
        //     if (playTrack)
        //     {
        //         foreach (SpriteRenderer r in DividerRenderers) r.DOFade(track ? 1 : 0, 0.5f);
        //         TrackRenderer.sharedMaterial.DOColor(track ? Color.white : Color.clear, "_Color", 0.5f);
        //     }
        // }
        // if (text != null) TextAreaText.text = text;
        // bool dText = text != null;
        // if (dispText != dText)
        // {
        //     dispText = dText;
        //     if (playText)
        //     {
        //         TextArea.DOFade(dText ? 1 : 0, 0.5f);
        //     }
        // }
    }

	private void HideTrack()
	{
        Debug.Log("HideTrack");
        foreach (SpriteRenderer r in DividerRenderers){
            r.DOKill();
        };
        TrackRenderer.sharedMaterial.DOKill();
        if(ArcGameplayManager.Instance.IsPlaying){
            foreach (SpriteRenderer r in DividerRenderers){
                r.DOFade(0, trackAnimationTime).SetEase(Ease.InCubic);
            };
            TrackRenderer.sharedMaterial.DOColor(Color.clear, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
        }else{
            foreach (SpriteRenderer r in DividerRenderers){
                r.color = Color.clear;
            };
            TrackRenderer.sharedMaterial.SetColor("_Color", Color.clear);
        }
	}

	private void ShowTrack()
	{
        Debug.Log("ShowTrack"); foreach (SpriteRenderer r in DividerRenderers){
            r.DOKill();
        };
        TrackRenderer.sharedMaterial.DOKill();
        if(ArcGameplayManager.Instance.IsPlaying){
            foreach (SpriteRenderer r in DividerRenderers){
                r.DOFade(1, trackAnimationTime).SetEase(Ease.InCubic);
            };
            TrackRenderer.sharedMaterial.DOColor(Color.white, "_Color", trackAnimationTime).SetEase(Ease.InCubic);
        }else{
            foreach (SpriteRenderer r in DividerRenderers){
                r.color = Color.white;
            };
            TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
        }
	}
}
