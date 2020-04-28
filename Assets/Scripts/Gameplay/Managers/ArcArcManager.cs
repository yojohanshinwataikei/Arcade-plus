using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;
using System;

namespace Arcade.Gameplay
{
	public class ArcArcManager : MonoBehaviour
	{
		public static ArcArcManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}

		[HideInInspector]
		public List<ArcArc> Arcs = new List<ArcArc>();

		public GameObject ArcNotePrefab, ArcTapPrefab, ConnectionPrefab;
		public Transform ArcLayer;
		public Color ConnectionColor;
		public Texture2D ArcTapSkin;
		public Material ArcTapMaterial;
		public Material ArcMaterial;

		public Color ArcRedLow;
		public Color ArcBlueLow;
		public Color ArcGreenLow;
		public Color ArcRedHigh;
		public Color ArcBlueHigh;
		public Color ArcGreenHigh;
		public Color ArcVoid;
		public Color ShadowColor;
		public readonly float[] Lanes = { 6.375f, 2.125f, -2.125f, -6.375f };

		[HideInInspector]
		public float ArcJudgePos;

		public void Clean()
		{
			foreach (var t in Arcs)
			{
				t.Destroy();
			};
			Arcs.Clear();
		}
		public void Load(List<ArcArc> arcs)
		{
			Arcs = arcs;
			foreach (var t in Arcs) t.Instantiate();
			CalculateArcRelationship();
		}

		public void CalculateArcRelationship()
		{
			ArcTimingManager timing = ArcTimingManager.Instance;
			foreach (ArcArc arc in Arcs)
			{
				arc.ArcGroup = null;
				arc.RenderHead = true;
			}
			foreach (ArcArc a in Arcs)
			{
				foreach (ArcArc b in Arcs)
				{
					if (a == b) continue;
					if (Mathf.Abs(a.XEnd - b.XStart) < 0.1f && Mathf.Abs(a.EndTiming - b.Timing) <= 9 && a.YEnd == b.YStart)
					{
						if (a.Color == b.Color && a.IsVoid == b.IsVoid)
						{
							if (a.ArcGroup == null && b.ArcGroup != null)
							{
								a.ArcGroup = b.ArcGroup;
							}
							else if (a.ArcGroup != null && b.ArcGroup == null)
							{
								b.ArcGroup = a.ArcGroup;
							}
							else if (a.ArcGroup != null && b.ArcGroup != null)
							{
								foreach (var t in b.ArcGroup)
								{
									if (!a.ArcGroup.Contains(t)) a.ArcGroup.Add(t);
								}
								b.ArcGroup = a.ArcGroup;
							}
							else if (a.ArcGroup == null && b.ArcGroup == null)
							{
								a.ArcGroup = b.ArcGroup = new List<ArcArc> { a };
							}
							if (!a.ArcGroup.Contains(b)) a.ArcGroup.Add(b);
						}
						if (a.IsVoid == b.IsVoid)
						{
							b.RenderHead = false;
						}
					}
				}
			}
			foreach (ArcArc arc in Arcs)
			{
				if (arc.ArcGroup == null) arc.ArcGroup = new List<ArcArc> { arc };
				arc.ArcGroup.Sort((ArcArc a, ArcArc b) => a.Timing.CompareTo(b.Timing));
			}
			foreach (ArcArc arc in Arcs)
			{
				arc.CalculateJudgeTimings();
			}
		}

		public void Rebuild()
		{
			foreach (var t in Arcs) t.Rebuild();
			CalculateArcRelationship();
		}

		public void Add(ArcArc arc)
		{
			arc.Instantiate();
			Arcs.Add(arc);
			CalculateArcRelationship();
		}
		public void Remove(ArcArc arc)
		{
			Arcs.Remove(arc);
			arc.Destroy();
			CalculateArcRelationship();
		}

		private void Update()
		{
			if (Arcs == null) return;
			if (ArcGameplayManager.Instance.Auto) JudgeArcs();
			ArcJudgePos = 0;
			RenderArcs();
		}

		private void RenderArcs()
		{
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			int currentTiming = ArcGameplayManager.Instance.Timing;
			int offset = ArcAudioManager.Instance.AudioOffset;

			foreach (var t in Arcs)
			{
				RenderArcTaps(t);
				int duration = t.EndTiming - t.Timing;
				if (!timingManager.ShouldTryRender(t.Timing + offset, duration + (t.IsVoid ? 50 : 120)) || t.Judged)
				{
					t.Enable = false;
					continue;
				}
				t.Position = timingManager.CalculatePositionByTiming(t.Timing + offset);
				t.EndPosition = timingManager.CalculatePositionByTiming(t.EndTiming + offset);
				if (t.Position > 100000 || t.EndPosition < -20000)
				{
					t.Enable = false;
					continue;
				}
				t.Enable = true;
				t.transform.localPosition = new Vector3(0, 0, -t.Position / 1000f);
				if (!t.IsVoid)
				{
					t.arcRenderer.EnableEffect = currentTiming > t.Timing + offset && currentTiming < t.EndTiming + offset && !t.IsVoid && t.Judging;
					foreach (var a in t.ArcGroup)
					{
						if (!a.Flag)
						{
							a.Flag = true;
							float alpha = 1;
							if (a.Judging)
							{
								a.FlashCount = (a.FlashCount + 1) % 5;
								if (a.FlashCount == 0) alpha = 0.85f;
								a.arcRenderer.Highlight = true;
							}
							else
							{
								alpha = 0.65f;
								a.arcRenderer.Highlight = false;
							}
							alpha *= 0.8823592f;
							a.arcRenderer.Alpha = alpha;
						}
					}
				}
				else
				{
					t.arcRenderer.EnableEffect = false;
					t.arcRenderer.Highlight = false;
					t.arcRenderer.Alpha = 0.318627f;
				}
				t.arcRenderer.UpdateArc();
			}
			foreach (var t in Arcs)
			{
				t.Flag = false;
			}
		}

		private void RenderArcTaps(ArcArc arc)
		{
			int timing = ArcGameplayManager.Instance.Timing;
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;

			foreach (ArcArcTap t in arc.ArcTaps)
			{
				if (!timingManager.ShouldTryRender(t.Timing + offset, 50) || t.Judged)
				{
					t.Enable = false;
					continue;
				}
				if (timing > t.Timing + offset + 50)
				{
					t.Enable = false;
					continue;
				}
				float pos = timingManager.CalculatePositionByTiming(t.Timing + offset) / 1000f;
				if (pos > -10 && pos <= 90)
				{
					t.Alpha = 1;
					t.Enable = true;
				}
				else if (pos > 90 && pos <= 100)
				{
					t.Enable = true;
					t.Alpha = (100 - pos) / 10f;
				}
				else
				{
					t.Enable = false;
				}
			}
		}

		private void JudgeArcs()
		{
			int currentTiming = ArcGameplayManager.Instance.Timing;
			int offset = ArcAudioManager.Instance.AudioOffset;
			foreach (ArcArc arc in Arcs)
			{
				JudgeArcTaps(arc);
				if (arc.Judged) continue;
				if (currentTiming > arc.EndTiming + offset)
				{
					arc.Judged = true;
				}
				else if (currentTiming > arc.Timing + offset && currentTiming <= arc.EndTiming + offset)
				{
					if (!arc.IsVoid)
					{
						if (!arc.AudioPlayed)
						{
							if (ArcGameplayManager.Instance.IsPlaying && arc.ShouldPlayAudio) ArcEffectManager.Instance.PlayArcSound();
							arc.AudioPlayed = true;
						}
					}
					foreach (var a in arc.ArcGroup) a.Judging = true;
				}
				else
				{
					arc.ShouldPlayAudio = true;
				}
			}
		}
		private void JudgeArcTaps(ArcArc arc)
		{
			int currentTiming = ArcGameplayManager.Instance.Timing;
			int offset = ArcAudioManager.Instance.AudioOffset;
			foreach (ArcArcTap t in arc.ArcTaps)
			{
				if (t.Judged) continue;
				if (currentTiming > t.Timing + offset && currentTiming <= t.Timing + offset + 150)
				{
					t.Judged = true;
					if (ArcGameplayManager.Instance.IsPlaying) ArcEffectManager.Instance.PlayTapNoteEffectAt(new Vector2(t.LocalPosition.x, t.LocalPosition.y + 0.5f), true);
				}
				else if (currentTiming > t.Timing + offset + 150)
				{
					t.Judged = true;
				}
			}
		}

		public void SetArcTapShadowSkin(Sprite sprite)
		{
			ArcTapPrefab.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
			foreach (ArcArc arc in Arcs)
			{
				foreach (ArcArcTap t in arc.ArcTaps)
				{
					t.ShadowRenderer.sprite = sprite;
				}
			}
		}

		public void SetArcCapSkin(Sprite sprite)
		{
			ArcNotePrefab.GetComponent<ArcArcRenderer>().ArcCapRenderer.sprite = sprite;
			foreach (ArcArc arc in Arcs)
			{
				arc.arcRenderer.ArcCapRenderer.sprite = sprite;
			}
		}
		public void SetHeightIndicatorSkin(Sprite sprite)
		{
			ArcNotePrefab.GetComponent<ArcArcRenderer>().HeightIndicatorRenderer.sprite = sprite;
			foreach (ArcArc arc in Arcs)
			{
				arc.arcRenderer.HeightIndicatorRenderer.sprite = sprite;
			}
		}

		public void SetArcColors(Color arcRedLow, Color arcBlueLow, Color arcGreenLow, Color arcRedHigh, Color arcBlueHigh, Color arcGreenHigh,Color arcVoid)
		{
			ArcRedLow=arcRedLow;
			ArcBlueLow=arcBlueLow;
			ArcGreenLow=arcGreenLow;
			ArcRedHigh=arcRedHigh;
			ArcBlueHigh=arcBlueHigh;
			ArcGreenHigh=arcGreenHigh;
			ArcVoid=arcVoid;
			//TODO: arc has slight diifferent color at different hight
			ArcArcRenderer prefabRenderer = ArcNotePrefab.GetComponent<ArcArcRenderer>();
			prefabRenderer.ReloadColor();
			foreach (ArcArc arc in Arcs)
			{
				arc.arcRenderer.ReloadColor();
			}
		}

		public void SetArcBodySkin(Texture2D normal, Texture2D highlight)
		{
			ArcMaterial.mainTexture=normal;
			ArcArcRenderer prefabRenderer = ArcNotePrefab.GetComponent<ArcArcRenderer>();
			prefabRenderer.HighlightTexture = highlight;
			prefabRenderer.DefaultTexture = normal;
			prefabRenderer.ReloadSkin();
			ArcArcSegmentComponent prefabSegmentComponent = prefabRenderer.SegmentPrefab.GetComponent<ArcArcSegmentComponent>();
			prefabSegmentComponent.HighlightTexture = highlight;
			prefabSegmentComponent.DefaultTexture = normal;
			prefabSegmentComponent.ReloadSkin();
			foreach (ArcArc arc in Arcs)
			{
				arc.arcRenderer.HighlightTexture = highlight;
				arc.arcRenderer.DefaultTexture = normal;
				arc.arcRenderer.ReloadSkin();
			}
		}

		public void SetParticleArcColor(ParticleSystem.MinMaxGradient startColor, ParticleSystem.MinMaxGradient overTimeColor)
		{
			ArcArcRenderer prefabRenderer = ArcNotePrefab.GetComponent<ArcArcRenderer>();
			ParticleSystem.MainModule prefabMain = prefabRenderer.JudgeEffect.main;
			prefabMain.startColor = startColor;
			ParticleSystem.ColorOverLifetimeModule prefabColorOverLifetime = prefabRenderer.JudgeEffect.colorOverLifetime;
			prefabColorOverLifetime.color = overTimeColor;
			foreach (ArcArc arc in Arcs)
			{
				ParticleSystem.MainModule main = arc.arcRenderer.JudgeEffect.main;
				main.startColor = startColor;
				ParticleSystem.ColorOverLifetimeModule colorOverLifetime = arc.arcRenderer.JudgeEffect.colorOverLifetime;
				colorOverLifetime.color = overTimeColor;
			}
		}

		public void SetArcTapSkin(Texture2D texture){
			ArcTapSkin = texture;
			ArcTapMaterial.mainTexture = texture;
		}
	}
}
