using System.Collections.Generic;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class ArcSpecialManager : MonoBehaviour
{
    public static ArcSpecialManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    public Text TextAreaText;
    public CanvasGroup TextArea;
    public SpriteRenderer TrackRenderer;
    public SpriteRenderer[] DividerRenderers;
    [HideInInspector]
    public List<ArcSpecial> Specials = new List<ArcSpecial>();
    [HideInInspector]
    public bool dispTrack = true, dispText = false;

    public void Load(List<ArcSpecial> specials)
    {
        specials.Sort((ArcSpecial a, ArcSpecial b) => a.Timing.CompareTo(b.Timing));
        Specials = specials;
    }
    public void Clean()
    {
        Specials.Clear();
    }
    public void ResetJudge()
    {
        foreach (var s in Specials) s.Played = false;
        TextArea.alpha = 0;
        dispText = false;
        foreach (var r in DividerRenderers)
        {
            r.color = Color.white;
        }
        TrackRenderer.sharedMaterial.SetColor("_Color", Color.white);
        dispTrack = true;
    }

    private void Update()
    {
        int timing = ArcGameplayManager.Instance.Timing;
        int offset = ArcAudioManager.Instance.AudioOffset;
        bool playText = false, playTrack = false, track = true;
        string text = null;
        foreach (var s in Specials)
        {
            if (timing < s.Timing + offset) break;
            switch (s.Type)
            {
                case Arcade.Aff.Advanced.SpecialType.TextArea:
                    if (s.Param1 == "in")
                    {
                        if (!s.Played)
                        {
                            s.Played = true;
                            playText = true;
                        }
                        text = s.Param2;
                    }
                    else if (s.Param1 == "out")
                    {
                        if (!s.Played)
                        {
                            s.Played = true;
                            playText = true;
                        }
                        text = null;
                    }
                    break;
                case Arcade.Aff.Advanced.SpecialType.Fade:
                    if (s.Param1 == "in")
                    {
                        if (!s.Played)
                        {
                            s.Played = true;
                            playTrack = true;
                        }
                        track = true;
                    }
                    else if (s.Param1 == "out")
                    {
                        if (!s.Played)
                        {
                            s.Played = true;
                            playTrack = true;
                        }
                        track = false;
                    }
                    break;
            }
        }
        if (dispTrack != track)
        {
            dispTrack = track;
            if (playTrack)
            {
                foreach (SpriteRenderer r in DividerRenderers) r.DOFade(track ? 1 : 0, 0.5f);
                TrackRenderer.sharedMaterial.DOColor(track ? Color.white : Color.clear, "_Color", 0.5f);
            }
        }
        if (text != null) TextAreaText.text = text;
        bool dText = text != null;
        if (dispText != dText)
        {
            dispText = dText;
            if (playText)
            {
                TextArea.DOFade(dText ? 1 : 0, 0.5f);
            }
        }
    }
}
