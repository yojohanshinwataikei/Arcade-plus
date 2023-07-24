using System;
using System.Collections.Generic;
using Arcade.Compose;
using Arcade.Util.Pooling;
using UnityEngine;

namespace Arcade.Gameplay
{
	public class ArcEffectManager : MonoBehaviour
	{
		public static ArcEffectManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			tapNoteEffectPool = new GameObjectPool<ArcTapNoteEffectComponent>(TapNoteJudgeEffect, EffectLayer, 10);
			sfxTapNoteEffectPool = new GameObjectPool<ArcTapNoteEffectComponent>(SfxTapNoteJudgeEffect, EffectLayer, 10);
		}

		public GameObject TapNoteJudgeEffect;
		public GameObject SfxTapNoteJudgeEffect;
		public GameObject[] LaneHits = new GameObject[6];
		public ParticleSystem[] HoldNoteEffects = new ParticleSystem[6];
		public Transform EffectLayer;
		public AudioClip TapAudio, ArcAudio;
		public AudioSource Source;

		public Dictionary<string, AudioClip> SpecialEffectAudios = new Dictionary<string, AudioClip>();

		private bool[] holdEffectStatus = new bool[6];
		private GameObjectPool<ArcTapNoteEffectComponent> tapNoteEffectPool;
		private GameObjectPool<ArcTapNoteEffectComponent> sfxTapNoteEffectPool;
		public void SetHoldNoteEffect(int track, bool show)
		{
			if (holdEffectStatus[track] != show)
			{
				holdEffectStatus[track] = show;
				if (show)
				{
					HoldNoteEffects[track].Play();
				}
				else
				{
					HoldNoteEffects[track].Stop();
					HoldNoteEffects[track].Clear();
				}
				LaneHits[track].SetActive(show);
			}
		}

		public void AddSpecialEffectAudio(string effect, AudioClip audio)
		{
			SpecialEffectAudios.Add(effect, audio);
		}

		public void CleanSpecialEffectAudios()
		{
			foreach (var audio in SpecialEffectAudios.Values)
			{
				Destroy(audio);
			}
			SpecialEffectAudios.Clear();
		}

		public void PlayTapNoteEffectAt(Vector2 pos, bool isArc = false, string arcTapEffect = "none")
		{
			if (arcTapEffect.EndsWith("_wav"))
			{
				sfxTapNoteEffectPool.Get((effect) =>
				{
					effect.PlayAt(pos);
				});
			}
			else
			{
				tapNoteEffectPool.Get((effect) =>
				{
					effect.PlayAt(pos);
				});
			}
			if (isArc)
			{
				if (SpecialEffectAudios.ContainsKey(arcTapEffect))
				{
					ArcAudioManager.Instance.Source.PlayOneShot(SpecialEffectAudios[arcTapEffect]);
				}
				else
				{
					Source.PlayOneShot(ArcAudio);
				}
			}
			else
			{
				Source.PlayOneShot(TapAudio);
			}
		}
		public void PlayTapSound()
		{
			Source.PlayOneShot(TapAudio);
		}
		public void PlayArcSound()
		{
			Source.PlayOneShot(ArcAudio);
		}
		public void ResetHoldNoteEffect()
		{
			for (int i = 0; i < 6; ++i) SetHoldNoteEffect(i, false);
		}

		public void SetParticleArcColor(Color particleArcStartColor, Color particleArcEndColor)
		{
			Color startColorMin = particleArcStartColor - new Color(0.1f, 0.1f, 0.1f);
			startColorMin.a = 0.5f;
			Color startColorMax = particleArcStartColor + new Color(0.1f, 0.1f, 0.1f);
			Gradient colorOverTime = new Gradient();
			colorOverTime.SetKeys(new GradientColorKey[]{
				new GradientColorKey(new Color(1.0f,1.0f,1.0f),0.0f),
				new GradientColorKey(particleArcEndColor,1.0f),
			}, new GradientAlphaKey[]{
				new GradientAlphaKey(1.0f,0.0f),
				new GradientAlphaKey(1.0f,1.0f),
			});
			ParticleSystem.MinMaxGradient startColor = new ParticleSystem.MinMaxGradient
			{
				mode = ParticleSystemGradientMode.TwoColors,
				colorMin = startColorMin,
				colorMax = startColorMax
			};
			ParticleSystem.MinMaxGradient overTimeColor = new ParticleSystem.MinMaxGradient
			{
				mode = ParticleSystemGradientMode.Gradient,
				gradient = colorOverTime
			};
			foreach (ParticleSystem holdEffect in HoldNoteEffects)
			{
				ParticleSystem.MainModule main = holdEffect.main;
				main.startColor = startColor;
				ParticleSystem.ColorOverLifetimeModule colorOverLifetime = holdEffect.colorOverLifetime;
				colorOverLifetime.color = overTimeColor;
			}
			ArcArcManager.Instance.SetParticleArcColor(startColor, overTimeColor);
		}

		internal void SetTapEffectTexture(Texture2D particleTap)
		{
			tapNoteEffectPool.Modify(effect=>{
				effect.SetTexture(particleTap);
			});
		}

		internal void SetSfxTapEffectTexture(Texture2D particleSfxTap)
		{
			sfxTapNoteEffectPool.Modify(effect=>{
				effect.SetTexture(particleSfxTap);
			});
		}
	}
}
