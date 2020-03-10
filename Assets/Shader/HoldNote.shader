Shader "Arcade/HoldNote"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}
		[PerRendererData] _From ("From", Float) = 0
		[PerRendererData] _To ("To",Float) = 1
		[PerRendererData] _Alpha ("Alpha", Float) = 1
		[PerRendererData] _Highlight("Highlight", Int) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType"="Transparent" "CanUseSpriteAtlas"="true"  }
		ZWrite Off
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "ColorSpace.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			int _Highlight;
			float _From,_To,_Alpha;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half4 Highlight(half4 c)
			{
				fixed3 hsv = rgb2hsv(c.rgb);
				hsv.r += 0.4f;
				hsv.b += 0.25f;
				return half4(hsv2rgb(hsv),c.a);
			}

			half4 frag (v2f i) : SV_Target
			{
			    if(i.uv.y > _To || i.uv.y < _From) return 0;
				half4 c = half4(tex2D(_MainTex,i.uv).rgb, _Alpha);
				if(_Highlight == 1)
				{
					c = Highlight(c);
				};
				return c;
			}
			ENDCG
		}
	}
}
