using UnityEngine;

namespace Arcade.Gameplay
{
	public class ArcArcSegmentComponent : MonoBehaviour
	{
		public Color ShadowColor;
		public Material ArcMaterial, ShadowMaterial;
		public MeshRenderer SegmentRenderer, ShadowRenderer;
		public MeshFilter SegmentFilter, ShadowFilter;
		public Texture2D DefaultTexture, HighlightTexture;
		[HideInInspector] public int FromTiming, ToTiming;
		[HideInInspector] public Vector3 FromPos, ToPos;
		private MaterialPropertyBlock bodyPropertyBlock;
		private MaterialPropertyBlock shadowPropertyBlock;

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
					SegmentRenderer.enabled = value;
					ShadowRenderer.enabled = value;
				}
			}
		}
		public float From
		{
			get
			{
				return currentFrom;
			}
			set
			{
				if (currentFrom != value)
				{
					currentFrom = value;
					bodyPropertyBlock.SetFloat(fromShaderId, value);
					SegmentRenderer.SetPropertyBlock(bodyPropertyBlock);
					shadowPropertyBlock.SetFloat(fromShaderId, value);
					ShadowRenderer.SetPropertyBlock(shadowPropertyBlock);
				}
			}
		}
		public Color Color
		{
			get
			{
				return currentColor;
			}
			set
			{
				if (currentColor != value)
				{
					currentColor = value;
					bodyPropertyBlock.SetColor(colorShaderId, value);
					SegmentRenderer.SetPropertyBlock(bodyPropertyBlock);
					Color c = ShadowColor;
					c.a = value.a * 0.3f;
					shadowPropertyBlock.SetColor(colorShaderId, c);
					ShadowRenderer.SetPropertyBlock(shadowPropertyBlock);
				}
			}
		}
		public float Alpha
		{
			get
			{
				return currentColor.a;
			}
			set
			{
				if (currentColor.a != value)
				{
					currentColor.a = value;
					bodyPropertyBlock.SetColor(colorShaderId, currentColor);
					SegmentRenderer.SetPropertyBlock(bodyPropertyBlock);
					Color c = ShadowColor;
					c.a = value * 0.3f;
					shadowPropertyBlock.SetColor(colorShaderId, c);
					ShadowRenderer.SetPropertyBlock(shadowPropertyBlock);
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
					bodyPropertyBlock.SetTexture(mainTexShaderId, highlighted ? HighlightTexture : DefaultTexture);
					SegmentRenderer.SetPropertyBlock(bodyPropertyBlock);
				}
			}
		}

		public void ReloadSkin()
		{
			if (bodyPropertyBlock != null)
			{
				bodyPropertyBlock.SetTexture(mainTexShaderId, highlighted ? HighlightTexture : DefaultTexture);
				SegmentRenderer.SetPropertyBlock(bodyPropertyBlock);
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
					bodyPropertyBlock.SetInt(highlightShaderId, value ? 1 : 0);
					SegmentRenderer.SetPropertyBlock(bodyPropertyBlock);
					selected = value;
				}
			}
		}

		public Material CurrentArcMaterial
		{
			set
			{
				if (value == null)
				{
					if (usingArcMaterial == false)
					{
						SegmentRenderer.sharedMaterial = ArcMaterial;
						usingArcMaterial = true;
					}
				}
				else
				{
					if (usingArcMaterial == true)
					{
						SegmentRenderer.sharedMaterial = value;
						usingArcMaterial = false;
					}
				}
			}
		}
		public Material CurrentShadowMaterial
		{
			set
			{
				if (value == null)
				{
					if (usingShadowInstanceMaterial == false)
					{
						ShadowRenderer.sharedMaterial = ShadowMaterial;
						usingShadowInstanceMaterial = true;
					}
				}
				else
				{
					if (usingShadowInstanceMaterial == true)
					{
						ShadowRenderer.sharedMaterial = value;
						usingShadowInstanceMaterial = false;
					}
				}
			}
		}

		private bool enable = false;
		private bool selected = false;
		private bool usingArcMaterial = true;
		private bool usingShadowInstanceMaterial = true;
		private bool highlighted = false;
		private int fromShaderId;
		private int colorShaderId;
		private int highlightShaderId;
		private int mainTexShaderId;
		private float currentFrom = 0;
		private Color currentColor;

		private void Awake()
		{
			SegmentRenderer.sharedMaterial = ArcMaterial;
			ShadowRenderer.sharedMaterial = ShadowMaterial;
			SegmentRenderer.sortingLayerName = "Arc";
			SegmentRenderer.sortingOrder = 1;
			ShadowRenderer.sortingLayerName = "Arc";
			ShadowRenderer.sortingOrder = 0;
			bodyPropertyBlock = new MaterialPropertyBlock();
			shadowPropertyBlock = new MaterialPropertyBlock();
			fromShaderId = Shader.PropertyToID("_From");
			colorShaderId = Shader.PropertyToID("_Color");
			highlightShaderId = Shader.PropertyToID("_Highlight");
			mainTexShaderId = Shader.PropertyToID("_MainTex");
		}
		private void OnDestroy()
		{
			Destroy(SegmentFilter.sharedMesh);
			Destroy(ShadowFilter.sharedMesh);
		}

		public void BuildSegment(Vector3 fromPos, Vector3 toPos, float offset, int from, int to)
		{
			FromTiming = from;
			ToTiming = to;
			FromPos = fromPos;
			ToPos = toPos;

			if (fromPos == toPos) return;

			Vector3[] vertices = new Vector3[6];
			Vector2[] uv = new Vector2[6];
			int[] triangles = new int[] { 0, 3, 2, 0, 2, 1, 0, 5, 4, 0, 4, 1 };

			vertices[0] = fromPos + new Vector3(0, offset / 2, 0);
			uv[0] = new Vector2();
			vertices[1] = toPos + new Vector3(0, offset / 2, 0);
			uv[1] = new Vector2(0, 1);
			vertices[2] = toPos + new Vector3(offset, -offset / 2, 0);
			uv[2] = new Vector2(1, 1);
			vertices[3] = fromPos + new Vector3(offset, -offset / 2, 0);
			uv[3] = new Vector2(1, 0);
			vertices[4] = toPos + new Vector3(-offset, -offset / 2, 0);
			uv[4] = new Vector2(1, 1);
			vertices[5] = fromPos + new Vector3(-offset, -offset / 2, 0);
			uv[5] = new Vector2(1, 0);

			Destroy(SegmentFilter.sharedMesh);
			SegmentFilter.sharedMesh = new Mesh()
			{
				vertices = vertices,
				uv = uv,
				triangles = triangles
			};

			Vector3[] shadowvertices = new Vector3[4];
			Vector2[] shadowuv = new Vector2[4];
			int[] shadowtriangles = new int[6];

			shadowvertices[0] = fromPos + new Vector3(-offset, -fromPos.y, 0);
			shadowuv[0] = new Vector2();
			shadowvertices[1] = toPos + new Vector3(-offset, -toPos.y, 0); ;
			shadowuv[1] = new Vector2(0, 1);
			shadowvertices[2] = toPos + new Vector3(offset, -toPos.y, 0);
			shadowuv[2] = new Vector2(1, 1);
			shadowvertices[3] = fromPos + new Vector3(offset, -fromPos.y, 0);
			shadowuv[3] = new Vector2(1, 0);

			shadowtriangles[0] = 0;
			shadowtriangles[1] = 1;
			shadowtriangles[2] = 2;
			shadowtriangles[3] = 0;
			shadowtriangles[4] = 2;
			shadowtriangles[5] = 3;

			Destroy(ShadowFilter.sharedMesh);
			ShadowFilter.sharedMesh = new Mesh()
			{
				vertices = shadowvertices,
				uv = shadowuv,
				triangles = shadowtriangles
			};
		}
	}
}