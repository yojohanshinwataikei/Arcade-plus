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

		public GameObject ArcNotePrefab, ArcTapPrefab, SfxArcTapPrefab, ConnectionPrefab;
		public Transform ArcLayer;
		public Color ConnectionColor;
		public Texture2D ArcTapSkin;
		public Material ArcTapMaterial;
		public Texture2D SfxArcTapNoteSkin;
		public Material SfxArcTapNoteMaterial;
		public Texture2D SfxArcTapCoreSkin;
		public Material SfxArcTapCoreMaterial;
		public Material ArcMaterial;

		public Color ArcRedLow;
		public Color ArcBlueLow;
		public Color ArcGreenLow;
		public Color ArcUnknownLow;
		public Color ArcRedHigh;
		public Color ArcBlueHigh;
		public Color ArcGreenHigh;
		public Color ArcUnknownHigh;
		public Color ArcVoid;
		public Color ShadowColor;
		public readonly float[] Lanes = { 10.625f, 6.375f, 2.125f, -2.125f, -6.375f, -10.625f };

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
			foreach (var t in Arcs) t.Rebuild();
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
					if (Mathf.Abs(a.XEnd - b.XStart) < 0.25f && Mathf.Abs(a.EndTiming - b.Timing) <= 9 && Mathf.Abs(a.YEnd - b.YStart) < 0.01f)
					{
						if (a.Color == b.Color && a.IsVoid == b.IsVoid)
						{
							if (b.ArcGroup != null && a.ArcGroup == null)
							{
								b.ArcGroup.Insert(0, a);
								a.ArcGroup = b.ArcGroup;
							}
							else if (a.ArcGroup != null && b.ArcGroup == null)
							{
								a.ArcGroup.Add(b);
								b.ArcGroup = a.ArcGroup;
							}
							else if (a.ArcGroup == null && b.ArcGroup == null)
							{
								a.ArcGroup = b.ArcGroup = new List<ArcArc> { a, b };
							}
							else if (a.ArcGroup != null && b.ArcGroup != null)
							{
								if (a.ArcGroup != b.ArcGroup)
								{
									a.ArcGroup.AddRange(b.ArcGroup);
									foreach (ArcArc arc in b.ArcGroup)
									{
										arc.ArcGroup = a.ArcGroup;
									}
								}
							}
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
				if (arc.ArcGroup == null)
				{
					arc.ArcGroup = new List<ArcArc> { arc };
				}
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
				foreach(var arctap in t.ArcTaps){
					RenderArcTap(arctap);
				}
				if (t.ConvertedVariousSizedArctap!=null){
					RenderArcTap(t.ConvertedVariousSizedArctap);
				}
				int duration = t.EndTiming - t.Timing;
				if (!timingManager.ShouldTryRender(t.Timing + offset, t.TimingGroup, duration, !t.IsVoid) || (t.Judged && !t.IsVoid) || t.GroupHide())
				{
					t.Enable = false;
					continue;
				}
				t.Position = timingManager.CalculatePositionByTiming(t.Timing + offset, t.TimingGroup);
				t.EndPosition = timingManager.CalculatePositionByTiming(t.EndTiming + offset, t.TimingGroup);
				if (Mathf.Min(t.Position, t.EndPosition) > 100000 || Mathf.Max(t.Position, t.EndPosition) < -100000)
				{
					t.Enable = false;
					continue;
				}
				t.Enable = true;
				t.transform.localPosition = new Vector3(0, 0, -t.Position / 1000f);
				if (!t.IsVoid)
				{
					t.arcRenderer.EnableEffect = currentTiming > t.Timing + offset && currentTiming <= t.EndTiming + offset && !t.IsVoid && t.Judging;
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

		private void RenderArcTap(ArcArcTap t)
		{
			int timing = ArcGameplayManager.Instance.Timing;
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;

			if (!timingManager.ShouldTryRender(t.Timing + offset, t.TimingGroup) || t.Judged || t.GroupHide())
			{
				t.Enable = false;
				return;
			}
			float pos = timingManager.CalculatePositionByTiming(t.Timing + offset, t.TimingGroup) / 1000f;
			if (pos > -100 && pos <= 90)
			{
				t.Alpha = 1;
				t.Enable = true;
				t.UpdatePosition();
			}
			else if (pos > 90 && pos <= 100)
			{
				t.Enable = true;
				t.Alpha = (100 - pos) / 10f;
				t.UpdatePosition();
			}
			else
			{
				t.Enable = false;
			}
		}

		private void JudgeArcs()
		{
			int currentTiming = ArcGameplayManager.Instance.Timing;
			int offset = ArcAudioManager.Instance.AudioOffset;
			foreach (ArcArc arc in Arcs)
			{
				if (arc.NoInput())
				{
					continue;
				}
				foreach(var arcTap in arc.ArcTaps){
					JudgeArcTap(arcTap);
				}
				if(arc.ConvertedVariousSizedArctap!=null){
					JudgeArcTap(arc.ConvertedVariousSizedArctap);
				}
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
		private void JudgeArcTap(ArcArcTap t)
		{
			int currentTiming = ArcGameplayManager.Instance.Timing;
			int offset = ArcAudioManager.Instance.AudioOffset;
			if (t.Judged) return;
			if (currentTiming > t.Timing + offset && currentTiming <= t.Timing + offset + 150)
			{
				t.Judged = true;
				if (ArcGameplayManager.Instance.IsPlaying) ArcEffectManager.Instance.PlayTapNoteEffectAt(new Vector2(t.LocalPosition.x, t.LocalPosition.y + 0.5f), true, t.Arc.Effect);
			}
			else if (currentTiming > t.Timing + offset + 150)
			{
				t.Judged = true;
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

		public void SetArcColors(
			Color arcRedLow, Color arcBlueLow, Color arcGreenLow, Color arcUnknownLow,
			Color arcRedHigh, Color arcBlueHigh, Color arcGreenHigh, Color arcUnknownHigh,
			Color arcVoid)
		{
			ArcRedLow = arcRedLow;
			ArcBlueLow = arcBlueLow;
			ArcGreenLow = arcGreenLow;
			ArcUnknownLow = arcUnknownLow;
			ArcRedHigh = arcRedHigh;
			ArcBlueHigh = arcBlueHigh;
			ArcGreenHigh = arcGreenHigh;
			ArcUnknownHigh = arcUnknownHigh;
			ArcVoid = arcVoid;
			ArcArcRenderer prefabRenderer = ArcNotePrefab.GetComponent<ArcArcRenderer>();
			prefabRenderer.ReloadColor();
			foreach (ArcArc arc in Arcs)
			{
				arc.arcRenderer.ReloadColor();
			}
		}

		public void SetArcBodySkin(Texture2D normal, Texture2D highlight)
		{
			ArcMaterial.mainTexture = normal;
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

		public void SetParticleArcColor(Color particleArcStartColor, Color particleArcEndColor)
		{
			ArcArcRenderer prefabRenderer = ArcNotePrefab.GetComponent<ArcArcRenderer>();
			prefabRenderer.JudgeEffect.SetVector4("StartColor", particleArcStartColor);
			prefabRenderer.JudgeEffect.SetVector4("EndColor", particleArcEndColor);
			foreach (ArcArc arc in Arcs)
			{
				arc.arcRenderer.JudgeEffect.SetVector4("StartColor", particleArcStartColor);
				arc.arcRenderer.JudgeEffect.SetVector4("EndColor", particleArcEndColor);
			}
		}

		public void SetArcTapSkin(Texture2D texture)
		{
			ArcTapSkin = texture;
			ArcTapMaterial.mainTexture = texture;
		}

		public void SetSfxArcTapModel(Mesh value)
		{
			SfxArcTapPrefab.GetComponentInChildren<MeshFilter>().mesh = value;
			foreach (ArcArc arc in Arcs)
			{
				if (arc.IsSfx)
				{
					foreach (ArcArcTap arcTap in arc.ArcTaps)
					{
						if (arcTap.Instance != null)
						{
							arcTap.ModelRenderer.GetComponent<MeshFilter>().mesh = value;
						}
					}
				}
			}
		}

		public void SetSfxArcTapSkin(Texture2D noteTexture, Texture2D coreTexture)
		{
			SfxArcTapNoteSkin = noteTexture;
			SfxArcTapNoteMaterial.mainTexture = noteTexture;
			SfxArcTapCoreSkin = coreTexture;
			SfxArcTapCoreMaterial.mainTexture = coreTexture;
		}

		internal void SetParticleArcTexture(Texture2D texture)
		{
			ArcArcRenderer prefabRenderer = ArcNotePrefab.GetComponent<ArcArcRenderer>();
			prefabRenderer.JudgeEffect.SetTexture("Texture", texture);
			foreach (ArcArc arc in Arcs)
			{
				arc.arcRenderer.JudgeEffect.SetTexture("Texture", texture);
			}
		}
	}
}
