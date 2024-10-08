using System;
using System.Collections.Generic;
using Arcade.Compose;
using Arcade.Util.Pooling;
using UnityEngine;
using UnityEngine.VFX;

namespace Arcade.Gameplay
{
	public class ArcEffectManager : MonoBehaviour
	{
		public static ArcEffectManager Instance { get; private set; }

		public ArcEffectPlane EffectPlane;
		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			tapNoteEffectPool = new GameObjectPool<ArcTapNoteEffectComponent>(TapNoteJudgeEffect, EffectPlane.transform, 10);
			sfxTapNoteEffectPool = new GameObjectPool<ArcTapNoteEffectComponent>(SfxTapNoteJudgeEffect, EffectPlane.transform, 10);
			for (int i = 0; i < 6; ++i)
			{
				HoldNoteEffectPosition[i] = HoldNoteEffects[i].transform.position;
			};
		}

		public GameObject TapNoteJudgeEffect;
		public GameObject SfxTapNoteJudgeEffect;
		public GameObject[] LaneHits = new GameObject[6];
		public VisualEffect[] HoldNoteEffects = new VisualEffect[6];
		public Vector3[] HoldNoteEffectPosition = new Vector3[6];
		public Transform EffectLayer;
		public AudioClip TapAudio, ArcAudio;
		public AudioSource Source;

		public Dictionary<string, AudioClip> SpecialEffectAudios = new Dictionary<string, AudioClip>();

		private bool[] holdEffectStatus = new bool[6];
		private bool[] holdEffectPlaying = new bool[6];
		private GameObjectPool<ArcTapNoteEffectComponent> tapNoteEffectPool;
		private GameObjectPool<ArcTapNoteEffectComponent> sfxTapNoteEffectPool;

		void Update()
		{
			for (int track = 0; track < 6; track++)
			{
				HoldNoteEffects[track].transform.position = EffectPlane.GetPositionOnPlane(HoldNoteEffectPosition[track]);
				bool show = holdEffectStatus[track];
				if (show != holdEffectPlaying[track])
				{
					holdEffectPlaying[track] = show;
					if (show)
					{
						HoldNoteEffects[track].Play();
						HoldNoteEffects[track].Simulate(1f / 60f, 60);
					}
					else
					{
						HoldNoteEffects[track].Stop();
						HoldNoteEffects[track].Simulate(1f / 60f, 60);
					}
				}
			}
		}
		public void SetHoldNoteEffect(int track, bool show)
		{
			if (holdEffectStatus[track] != show)
			{
				holdEffectStatus[track] = show;
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
					effect.PlayAt(EffectPlane.GetPositionOnPlane(pos));
				});
			}
			else
			{
				tapNoteEffectPool.Get((effect) =>
				{
					effect.PlayAt(EffectPlane.GetPositionOnPlane(pos));
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

			foreach (VisualEffect holdEffect in HoldNoteEffects)
			{
				holdEffect.SetVector4("StartColor", particleArcStartColor);
				holdEffect.SetVector4("EndColor", particleArcEndColor);
			}
			ArcArcManager.Instance.SetParticleArcColor(particleArcStartColor, particleArcEndColor);
		}

		internal void SetTapEffectTexture(Texture2D particleTap)
		{
			tapNoteEffectPool.Modify(effect =>
			{
				effect.SetTexture(particleTap);
			});
		}

		internal void SetSfxTapEffectTexture(Texture2D particleSfxTap)
		{
			sfxTapNoteEffectPool.Modify(effect =>
			{
				effect.SetTexture(particleSfxTap);
			});
		}

		internal void SetParticleArcTexture(Texture2D texture)
		{

			foreach (VisualEffect holdEffect in HoldNoteEffects)
			{
				holdEffect.SetTexture("Texture", texture);
			}
			ArcArcManager.Instance.SetParticleArcTexture(texture);
		}
	}
}
