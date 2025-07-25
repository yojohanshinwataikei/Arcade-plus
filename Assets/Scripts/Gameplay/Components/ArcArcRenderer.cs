using System;
using System.Collections.Generic;
using System.Linq;
using Arcade.Gameplay.Chart;
using Arcade.Util.Misc;
using UnityEngine;
using UnityEngine.VFX;

namespace Arcade.Gameplay
{
	public class ArcArcRenderer : MonoBehaviour
	{
		public const float OffsetNormal = 0.9f;
		public const float OffsetVoid = 0.15f;

		public Material arcMaterial;
		public Material shadowMaterial;
		public GameObject SegmentPrefab;

		public MeshCollider ArcCollider, HeadCollider;
		public MeshFilter HeadFilter;
		public MeshRenderer HeadRenderer;
		public Transform Head;
		public SpriteRenderer HeightIndicatorRenderer;
		public Transform ArcCap;
		public SpriteRenderer ArcCapRenderer;
		public VisualEffect JudgeEffect;
		public Transform JudgeEffectTransform;
		public Texture2D DefaultTexture, HighlightTexture;

		private MaterialPropertyBlock headPropertyBlock;

		private Color ArcRedHigh
		{
			get { return ArcArcManager.Instance.ArcRedHigh; }
		}
		private Color ArcBlueHigh
		{
			get { return ArcArcManager.Instance.ArcBlueHigh; }
		}
		private Color ArcGreenHigh
		{
			get { return ArcArcManager.Instance.ArcGreenHigh; }
		}
		private Color ArcUnknownHigh
		{
			get { return ArcArcManager.Instance.ArcUnknownHigh; }
		}
		private Color ArcRedLow
		{
			get { return ArcArcManager.Instance.ArcRedLow; }
		}
		private Color ArcBlueLow
		{
			get { return ArcArcManager.Instance.ArcBlueLow; }
		}
		private Color ArcGreenLow
		{
			get { return ArcArcManager.Instance.ArcGreenLow; }
		}
		private Color ArcUnknownLow
		{
			get { return ArcArcManager.Instance.ArcUnknownLow; }
		}
		private Color ArcVoid
		{
			get { return ArcArcManager.Instance.ArcVoid; }
		}
		private Color ArcDesignant
		{
			get { return ArcArcManager.Instance.ArcDesignant; }
		}



		public ArcArc Arc
		{
			get
			{
				return arc;
			}
			set
			{
				arc = value;
				Build();
			}
		}
		public Color HighColor
		{
			get
			{
				return currentHighColor;
			}
			set
			{
				if (currentHighColor != value)
				{
					currentHighColor = value;
					HeadRenderer.GetPropertyBlock(headPropertyBlock);
					headPropertyBlock.SetColor(highColorShaderId, currentHighColor);
					HeadRenderer.SetPropertyBlock(headPropertyBlock);
					foreach (var s in segments)
					{
						s.HighColor = currentHighColor;
					}
				}
			}
		}
		public Color LowColor
		{
			get
			{
				return currentLowColor;
			}
			set
			{
				if (currentLowColor != value)
				{
					currentLowColor = value;
					HeadRenderer.GetPropertyBlock(headPropertyBlock);
					headPropertyBlock.SetColor(lowColorShaderId, currentLowColor);
					HeadRenderer.SetPropertyBlock(headPropertyBlock);
					foreach (var s in segments)
					{
						s.LowColor = currentLowColor;
					}
				}
			}
		}
		public float Alpha
		{
			get
			{
				return currentHighColor.a;
			}
			set
			{
				if (currentHighColor.a != value)
				{
					currentHighColor.a = value;
					HeadRenderer.GetPropertyBlock(headPropertyBlock);
					headPropertyBlock.SetColor(highColorShaderId, currentHighColor);
					HeadRenderer.SetPropertyBlock(headPropertyBlock);
					foreach (var s in segments)
					{
						s.Alpha = value;
					}
				}
				if (currentLowColor.a != value)
				{
					currentLowColor.a = value;
					headPropertyBlock.SetColor(lowColorShaderId, currentLowColor);
				}
			}
		}
		public bool Enable
		{
			get
			{
				return enable;
			}
			set
			{
				if (enable != value)
				{
					enable = value;
					EnableHead = value;
					EnableHeightIndicator = value;
					foreach (ArcArcSegmentComponent s in segments) s.Enable = value;
					EnableArcCap = value;
					if (!value) EnableEffect = false;
					ArcCollider.enabled = value;
				}
			}
		}
		public bool EnableHead
		{
			get
			{
				return headEnable;
			}
			set
			{
				if (headEnable != value)
				{
					headEnable = value;
					HeadRenderer.enabled = value;
					HeadCollider.enabled = value;
				}
			}
		}
		public bool EnableHeightIndicator
		{
			get
			{
				return heightIndicatorEnable;
			}
			set
			{
				if (heightIndicatorEnable != value)
				{
					heightIndicatorEnable = value;
					HeightIndicatorRenderer.enabled = value;
				}
			}
		}
		public bool EnableArcCap
		{
			get
			{
				return arcCapEnable;
			}
			set
			{
				if (arcCapEnable != value)
				{
					arcCapEnable = value;
					ArcCapRenderer.enabled = value;
				}
			}
		}
		public bool Highlight
		{
			get
			{
				return highlighted;
			}
			set
			{
				if (highlighted != value)
				{
					highlighted = value;
					HeadRenderer.GetPropertyBlock(headPropertyBlock);
					headPropertyBlock.SetTexture(mainTexShaderId, highlighted ? HighlightTexture : DefaultTexture);
					HeadRenderer.SetPropertyBlock(headPropertyBlock);
					foreach (var s in segments) s.Highlight = value;
				}
			}
		}
		public void ReloadSkin()
		{
			if (headPropertyBlock != null)
			{
				HeadRenderer.GetPropertyBlock(headPropertyBlock);
				headPropertyBlock.SetTexture(mainTexShaderId, highlighted ? HighlightTexture : DefaultTexture);
				HeadRenderer.SetPropertyBlock(headPropertyBlock);
			}
			foreach (ArcArcSegmentComponent s in segments)
			{
				s.DefaultTexture = DefaultTexture;
				s.HighlightTexture = HighlightTexture;
				s.ReloadSkin();
			}
		}

		public void ReloadColor()
		{
			float alpha = Alpha;
			if (arc != null)
			{
				HighColor = GetColor(true);
				LowColor = GetColor(false);
			}
			Alpha = alpha;
		}
		public bool EnableEffect
		{
			get
			{
				return effect;
			}
			set
			{
				if (effect != value)
				{
					effect = value;
					if (value)
					{
						JudgeEffect.Play();
						JudgeEffect.Simulate(1f / 60f, 60);
					}
					else
					{
						JudgeEffect.Stop();
						JudgeEffect.Simulate(1f / 60f, 60);
					}
				}
			}
		}
		public bool Selected
		{
			get
			{
				return selected;
			}
			set
			{
				if (selected != value)
				{
					HeadRenderer.renderingLayerMask = MaskUtil.SetMask(HeadRenderer.renderingLayerMask, ArcGameplayManager.Instance.SelectionLayerMask, value);

					foreach (var s in segments) s.Selected = value;
					selected = value;
				}
			}
		}
		public bool IsHead
		{
			get
			{
				return arc.RenderHead;
			}
		}

		private void Awake()
		{
			headPropertyBlock = new MaterialPropertyBlock();
			HeadRenderer.sortingLayerName = "Arc";
			HeadRenderer.sortingOrder = 1;
			highColorShaderId = Shader.PropertyToID("_HighColor");
			lowColorShaderId = Shader.PropertyToID("_LowColor");
			mainTexShaderId = Shader.PropertyToID("_MainTex");
		}
		private void OnDestroy()
		{
			Destroy(ArcCollider.sharedMesh);
			Destroy(HeadCollider.sharedMesh);
			Destroy(HeadFilter.sharedMesh);
		}

		private int highColorShaderId;
		private int lowColorShaderId;
		private int mainTexShaderId;
		private int segmentCount = 0;
		private bool enable;
		private bool selected;
		private bool headEnable;
		private bool heightIndicatorEnable;
		private bool arcCapEnable;
		private bool highlighted;
		private bool effect;
		private ArcArc arc;
		private Color currentHighColor;
		private Color currentLowColor;
		private List<ArcArcSegmentComponent> segments = new List<ArcArcSegmentComponent>();
		private int zeroLengthVoidArcDisappearTime = int.MaxValue;

		private Color GetColor(bool high)
		{
			if (arc.LineType == ArcLineType.TrueIsVoid)
			{
				return ArcVoid;
			}
			else if (arc.LineType == ArcLineType.Designant)
			{
				return ArcDesignant;
			}
			else
			{
				if (arc.Color == 0)
				{
					return high ? ArcBlueHigh : ArcBlueLow;
				}
				else if (arc.Color == 1)
				{
					return high ? ArcRedHigh : ArcRedLow;
				}
				else if (arc.Color == 2)
				{
					return high ? ArcGreenHigh : ArcGreenLow;
				}
				else
				{
					return high ? ArcUnknownHigh : ArcUnknownLow;
				}
			}
		}

		private void InstantiateSegment(int quantity)
		{
			int count = segments.Count;
			if (count == quantity) return;
			else if (count < quantity)
			{
				for (int i = 0; i < quantity - count; ++i)
				{
					GameObject g = Instantiate(SegmentPrefab, transform);
					ArcArcSegmentComponent component = g.GetComponent<ArcArcSegmentComponent>();
					component.Enable = Enable;
					component.Alpha = Alpha;
					component.Highlight = Highlight;
					component.Selected = Selected;
					segments.Add(component);
				}
			}
			else if (count > quantity)
			{
				for (int i = 0; i < count - quantity; ++i)
				{
					Destroy(segments.Last().gameObject);
					segments.RemoveAt(segments.Count - 1);
				}
			}
			foreach (ArcArcSegmentComponent s in segments)
			{
				s.transform.SetAsLastSibling();
			}
		}

		public void Build()
		{
			BuildHeightIndicator();
			BuildSegments();
			BuildHead();
			BuildCollider();
		}
		public void BuildHeightIndicator()
		{
			if (arc.IsVoid || arc.IsVariousSizedArctap)
			{
				EnableHeightIndicator = false;
				return;
			}
			HeightIndicatorRenderer.transform.localPosition = new Vector3(ArcAlgorithm.ArcXToWorld(arc.XStart), 0, 0);
			HeightIndicatorRenderer.transform.localScale = new Vector3(2.34f, 100 * (ArcAlgorithm.ArcYToWorld(arc.YStart) - OffsetNormal / 2), 1);
			HeightIndicatorRenderer.color = Color.Lerp(
				arc.Color == 0 ? ArcBlueLow : arc.Color == 1 ? ArcRedLow : arc.Color == 2 ? ArcGreenLow : ArcUnknownLow,
				arc.Color == 0 ? ArcBlueHigh : arc.Color == 1 ? ArcRedHigh : arc.Color == 2 ? ArcGreenHigh : ArcUnknownHigh,
				arc.YStart);
		}
		public void BuildSegments()
		{
			if (arc == null) return;


			ArcTimingManager timingManager = ArcTimingManager.Instance;
			int duration = arc.EndTiming - arc.Timing;

			if (duration == 0 && (arc.IsVoid || arc.NoInput()))
			{
				zeroLengthVoidArcDisappearTime = timingManager.CalculateZeroLengthVoidArcDisappearTime(arc.Timing, arc.TimingGroup);
			}
			else
			{
				zeroLengthVoidArcDisappearTime = int.MaxValue;
			}

			int v1 = duration < 1000 ? 14 : 7;
			float v2 = 1f / (v1 * duration / 1000f);
			int segSize = (int)(duration * v2);
			segmentCount = 0;
			if (segSize != 0)
			{
				segmentCount = duration / segSize + (duration % segSize == 0 ? 0 : 1);
			}
			if (segmentCount == 0 && (arc.XStart != arc.XEnd || arc.YStart != arc.YEnd))
			{
				segmentCount = 1;
			}
			InstantiateSegment(segmentCount);

			float startHeight = 0;
			float endHeight = arc.YStart;
			Vector3 start = new Vector3();
			Vector3 end = new Vector3(ArcAlgorithm.ArcXToWorld(arc.XStart),
										ArcAlgorithm.ArcYToWorld(arc.YStart));
			for (int i = 0; i < segmentCount - 1; ++i)
			{
				startHeight = endHeight;
				start = end;
				endHeight = ArcAlgorithm.Y(arc.YStart, arc.YEnd, (i + 1f) * segSize / duration, arc.CurveType);
				end = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(arc.XStart, arc.XEnd, (i + 1f) * segSize / duration, arc.CurveType)),
								  ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(arc.YStart, arc.YEnd, (i + 1f) * segSize / duration, arc.CurveType)),
								  -timingManager.CalculatePositionByTimingAndStart(arc.Timing, arc.Timing + segSize * (i + 1), arc.TimingGroup) / 1000f);
				segments[i].BuildSegment(start, end, arc.IsVoid ? OffsetVoid : OffsetNormal, arc.Timing + segSize * i, arc.Timing + segSize * (i + 1), startHeight, endHeight);
			}

			if (segmentCount > 0)
			{
				startHeight = endHeight;
				start = end;
				endHeight = arc.YEnd;
				end = new Vector3(ArcAlgorithm.ArcXToWorld(arc.XEnd),
								  ArcAlgorithm.ArcYToWorld(arc.YEnd),
								  -timingManager.CalculatePositionByTimingAndStart(arc.Timing, arc.EndTiming, arc.TimingGroup) / 1000f);
				segments[segmentCount - 1].BuildSegment(start, end, arc.IsVoid ? OffsetVoid : OffsetNormal, arc.Timing + segSize * (segmentCount - 1), arc.EndTiming, startHeight, endHeight);
			}
			HighColor = GetColor(true);
			LowColor = GetColor(false);
		}

		public void BuildHead()
		{
			Vector3 pos = new Vector3(ArcAlgorithm.ArcXToWorld(arc.XStart), ArcAlgorithm.ArcYToWorld(arc.YStart));
			float offset = arc.IsVoid ? OffsetVoid : OffsetNormal;

			Vector3[] vertices = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			Vector2[] uv2 = new Vector2[4];
			int[] triangles = new int[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };

			vertices[0] = pos + new Vector3(0, offset / 2, 0);
			uv[0] = new Vector2(0, 0);
			uv2[0] = new Vector2(arc.YStart, 0);
			vertices[1] = pos + new Vector3(offset, -offset / 2, 0);
			uv[1] = new Vector2(1, 0);
			uv2[1] = new Vector2(arc.YStart, 0);
			vertices[2] = pos + new Vector3(0, -offset / 2, offset);
			uv[2] = new Vector2(1, 1);
			uv2[2] = new Vector2(arc.YStart, 0);
			vertices[3] = pos + new Vector3(-offset, -offset / 2, 0);
			uv[3] = new Vector2(1, 1);
			uv2[3] = new Vector2(arc.YStart, 0);

			Destroy(HeadFilter.sharedMesh);
			HeadFilter.sharedMesh = new Mesh()
			{
				vertices = vertices,
				uv = uv,
				uv2 = uv2,
				triangles = triangles.Take(6).ToArray()
			};

			Destroy(HeadCollider.sharedMesh);
			HeadCollider.sharedMesh = new Mesh()
			{
				vertices = vertices,
				uv = uv,
				triangles = triangles
			};

			HeadRenderer.sharedMaterial = arcMaterial;
		}
		public void BuildCollider()
		{
			if (arc.Timing > arc.EndTiming || segments.Count == 0 || arc.IsVariousSizedArctap)
			{
				if (ArcCollider.sharedMesh)
				{
					Destroy(ArcCollider.sharedMesh);
				}
				return;
			}

			List<Vector3> vert = new List<Vector3>();
			List<int> tri = new List<int>();
			List<Vector2> uv = new List<Vector2>();

			float offset = arc.IsVoid ? OffsetVoid : OffsetNormal;

			Vector3 pos = segments[0].FromPos;
			vert.Add(pos + new Vector3(-offset, -offset / 2, 0));
			vert.Add(pos + new Vector3(0, offset / 2, 0));
			vert.Add(pos + new Vector3(offset, -offset / 2, 0));
			uv.Add(new Vector2(0, 0));
			uv.Add(new Vector2(0, 0));
			uv.Add(new Vector2(0, 0));

			int t = 0;
			foreach (var seg in segments)
			{
				if (seg.FromTiming > seg.ToTiming) break;
				float ratio = 1;
				if (seg.FromTiming == seg.ToTiming)
				{
					if (seg.FromPos.Equals(seg.ToPos))
						break;
				}
				else
				{
					ratio = ((float)(seg.ToTiming - arc.Timing)) / ((float)(arc.EndTiming - arc.Timing));
				}
				pos = seg.ToPos;
				vert.Add(pos + new Vector3(-offset, -offset / 2, 0));
				vert.Add(pos + new Vector3(0, offset / 2, 0));
				vert.Add(pos + new Vector3(offset, -offset / 2, 0));
				uv.Add(new Vector2(ratio, 0));
				uv.Add(new Vector2(ratio, 0));
				uv.Add(new Vector2(ratio, 0));

				tri.AddRange(new int[] { t + 1, t, t + 3, t + 1, t + 3, t, t + 1, t + 3, t + 4, t + 1, t + 4, t + 3,
					t + 1, t + 2, t + 5, t + 1, t + 5, t + 2, t + 1, t + 5, t + 4, t + 1, t + 4, t + 5 });
				t += 3;
			}

			Destroy(ArcCollider.sharedMesh);
			ArcCollider.sharedMesh = new Mesh()
			{
				vertices = vert.ToArray(),
				triangles = tri.ToArray(),
				uv = uv.ToArray(),
			};
		}

		public void UpdateArc()
		{
			if (!enable) return;
			UpdateHead();
			UpdateSegments();
			UpdateHeightIndicator();
			UpdateArcCap();
		}
		private void UpdateSegments()
		{
			int currentTiming = ArcGameplayManager.Instance.ChartTiming;
			float z = arc.transform.localPosition.z;

			foreach (ArcArcSegmentComponent s in segments)
			{
				if (arc.IsVariousSizedArctap)
				{
					s.Enable = false;
					continue;
				}
				if (s.ToTiming == s.FromTiming && currentTiming > zeroLengthVoidArcDisappearTime)
				{
					s.Enable = false;
					continue;
				}
				float pos = -(z + s.FromPos.z);
				float endPos = -(z + s.ToPos.z);
				if (endPos > 100 && pos < 100)
				{
					s.To = (100 - pos) / (endPos - pos);
				}
				else
				{
					s.To = 1;
				}

				if ((s.ToTiming < currentTiming && s.ToTiming != s.FromTiming) || (Mathf.Max(pos, endPos) < 0 && s.ToTiming == s.FromTiming))
				{
					if (arc.Judging || arc.IsVoid || arc.NoInput())
					{
						s.Enable = false;
						continue;
					}
					else
					{
						s.Enable = true;
						continue;
					}
				}
				if (s.FromTiming < currentTiming && s.ToTiming >= currentTiming)
				{
					s.Enable = true;
					s.CurrentArcMaterial = null;
					s.CurrentShadowMaterial = null;
					s.Alpha = currentHighColor.a;
					if (arc.Judging || arc.IsVoid || arc.NoInput())
					{
						s.From = (z + s.FromPos.z) / (-s.ToPos.z + s.FromPos.z);
					}
					else
					{
						s.From = 0;
					}
					continue;
				}
				if (pos > 90 && pos < 100 && !arc.IsVoid)
				{
					s.Enable = true;
					s.CurrentArcMaterial = null;
					s.CurrentShadowMaterial = null;
					s.Alpha = currentHighColor.a * (100 - pos) / 10f;
					s.From = 0;
				}
				else if (Mathf.Min(pos, endPos) > 100 || Mathf.Max(pos, endPos) < -100)
				{
					s.Enable = false;
				}
				else
				{
					s.Enable = true;
					s.Alpha = currentHighColor.a;
					s.From = 0;
					s.CurrentArcMaterial = arcMaterial;
					s.CurrentShadowMaterial = shadowMaterial;
				}
			}
		}
		private void UpdateHead()
		{
			if (!IsHead || arc.IsVariousSizedArctap)
			{
				EnableHead = false;
				return;
			}

			int currentTiming = ArcGameplayManager.Instance.ChartTiming;
			if (arc.Timing == arc.EndTiming && currentTiming > zeroLengthVoidArcDisappearTime)
			{
				EnableHead = false;
				return;
			}


			if (arc.Position > 100000 || arc.Position < -100000)
			{
				EnableHead = false;
				return;
			}
			EnableHead = true;
			if (arc.Position > 90000 && arc.Position <= 100000)
			{
				Head.localPosition = new Vector3();
				HeadRenderer.sharedMaterial = arcMaterial;
				Color highC = currentHighColor;
				highC.a = currentHighColor.a * (100000 - arc.Position) / 100000;
				Color lowC = currentLowColor;
				lowC.a = currentLowColor.a * (100000 - arc.Position) / 100000;
				HeadRenderer.GetPropertyBlock(headPropertyBlock);
				headPropertyBlock.SetColor(highColorShaderId, highC);
				headPropertyBlock.SetColor(lowColorShaderId, lowC);
				HeadRenderer.SetPropertyBlock(headPropertyBlock);
			}
			if ((arc.Timing < currentTiming && arc.Timing != arc.EndTiming) || (Arc.Position < 0 && arc.Timing == arc.EndTiming))
			{
				HeadRenderer.GetPropertyBlock(headPropertyBlock);
				headPropertyBlock.SetColor(highColorShaderId, currentHighColor);
				headPropertyBlock.SetColor(lowColorShaderId, currentLowColor);
				HeadRenderer.SetPropertyBlock(headPropertyBlock);
				if (arc.Judging || arc.IsVoid || arc.NoInput())
				{
					if (segmentCount >= 1)
					{
						ArcArcSegmentComponent s = segments[0];
						int duration = s.ToTiming - s.FromTiming;
						if (duration == 0)
						{
							EnableHead = false;
							return;
						}
						if (s.ToTiming < currentTiming)
						{
							EnableHead = false;
							return;
						}
						float t = duration == 0 ? 0 : ((-arc.Position / 1000f) / (-s.ToPos.z));
						if (float.IsNaN(t))
						{
							t = duration == 0 ? 0 : ((float)(currentTiming - s.FromTiming)) / ((float)(duration));
						}
						if (t > 1)
						{
							EnableHead = false;
							return;
						}
						else if (t < 0) t = 0;
						Head.localPosition = (s.ToPos - s.FromPos) * t;
					}
				}
				else
				{
					Head.localPosition = new Vector3();
				}
			}
			else
			{
				HeadRenderer.GetPropertyBlock(headPropertyBlock);
				headPropertyBlock.SetColor(highColorShaderId, currentHighColor);
				headPropertyBlock.SetColor(lowColorShaderId, currentLowColor);
				HeadRenderer.SetPropertyBlock(headPropertyBlock);
				Head.localPosition = new Vector3();
			}
		}
		private void UpdateHeightIndicator()
		{
			if (arc.IsVoid || (arc.YEnd == arc.YStart && !IsHead) || arc.IsVariousSizedArctap)
			{
				EnableHeightIndicator = false;
				return;
			}

			float pos = transform.position.z;
			if (pos < -90 && pos > -100)
			{
				Color c = Color.Lerp(currentLowColor, currentHighColor, arc.YStart);
				c.a = currentHighColor.a * (pos + 100) / 10;
				EnableHeightIndicator = true;
				HeightIndicatorRenderer.color = c;
			}
			else if (pos < -100 || pos > 100)
			{
				EnableHeightIndicator = false;
			}
			else
			{
				int currentTiming = ArcGameplayManager.Instance.ChartTiming;
				if ((arc.Judging || arc.NoInput()) && arc.Timing < currentTiming) EnableHeightIndicator = false;
				else EnableHeightIndicator = true;
				HeightIndicatorRenderer.color = Color.Lerp(currentLowColor, currentHighColor, arc.YStart);
			}
		}
		private void UpdateArcCap()
		{
			int currentTiming = ArcGameplayManager.Instance.ChartTiming;
			int duration = arc.EndTiming - arc.Timing;

			if (duration == 0 || arc.IsVariousSizedArctap)
			{
				EnableArcCap = false;
				return;
			}

			Vector3 arcCapScaleFactor = new Vector3(256 / ArcCapRenderer.sprite.rect.width, 256 / ArcCapRenderer.sprite.rect.height, 1);

			if (arc.Timing < currentTiming && arc.EndTiming >= currentTiming)
			{
				EnableArcCap = true;
				ArcCapRenderer.color = new Color(1, 1, 1, arc.IsVoid ? 0.5f : 1f);
				ArcCap.localScale = Vector3.Scale(new Vector3(arc.IsVoid ? 0.21f : 0.35f, arc.IsVoid ? 0.21f : 0.35f), arcCapScaleFactor);

				foreach (var s in segments)
				{
					if (s.FromTiming < currentTiming && s.ToTiming >= currentTiming)
					{
						float t = (s.FromPos.z - arc.Position / 1000f) / (s.FromPos.z - s.ToPos.z);
						if (float.IsNaN(t))
						{
							t = ((float)(currentTiming - s.FromTiming)) / ((float)(s.ToTiming - s.FromTiming));
						}
						ArcCap.position = new Vector3(s.FromPos.x + (s.ToPos.x - s.FromPos.x) * t,
													  s.FromPos.y + (s.ToPos.y - s.FromPos.y) * t);
						JudgeEffectTransform.position = ArcEffectManager.Instance.EffectPlane.GetPositionOnPlane(ArcCap.position);
						if (!arc.IsVoid) ArcArcManager.Instance.ArcJudgePos += ArcCap.position.x;
						break;
					}
				}
			}
			else if (arc.Timing >= currentTiming && IsHead && !arc.IsVoid)
			{
				float p = 1 - Mathf.Abs(arc.Position) / 100000;
				float scale = 0.35f + 0.5f * (1 - p);
				EnableArcCap = true;
				ArcCapRenderer.color = new Color(1, 1, 1, p);
				ArcCap.localScale = Vector3.Scale(new Vector3(scale, scale), arcCapScaleFactor);
				ArcCap.position = new Vector3(ArcAlgorithm.ArcXToWorld(arc.XStart), ArcAlgorithm.ArcYToWorld(arc.YStart));
				JudgeEffectTransform.position = ArcCap.position;
			}
			else
			{
				EnableArcCap = false;
			}
		}

		public bool IsHitMyself(RaycastHit h)
		{
			if (HeadCollider.gameObject.Equals(h.transform.gameObject))
			{
				return true;
			}
			else if (ArcCollider.gameObject.Equals(h.transform.gameObject))
			{
				if (arc.Judging || arc.IsVoid)
				{
					double timing = ((double)h.textureCoord.x) * (arc.EndTiming - arc.Timing) + arc.Timing;
					return timing + double.Epsilon >= ArcGameplayManager.Instance.ChartTiming;
				}
				return true;
			}
			return false;
		}
	}
}
