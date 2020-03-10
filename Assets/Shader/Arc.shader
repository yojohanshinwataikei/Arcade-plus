Shader "Arcade/Arc"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}
		[PerRendererData] _HighColor ("HighColor", Color) = (1,1,1,1)
		[PerRendererData] _LowColor ("LowColor", Color) = (1,1,1,1)
		[PerRendererData] _From ("From", Float) = 0
		[PerRendererData] _To ("To", Float) = 1
		[PerRendererData] _Highlight("Highlight", Int) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "CanUseSpriteAtlas"="true"  }

        Cull Off
        Lighting Off
		ZWrite Off
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
				float4 color    : COLOR;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color    : COLOR;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			int _Highlight;
			float _From,_To;
			float4 _HighColor;
			float4 _LowColor;
            float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				o.uv2 = v.uv2;
				return o;
			}

			half4 Highlight(half4 c)
			{
				// TODO: highlight by outline might be better
				fixed3 hsv = rgb2hsv(c.rgb);
				float rate=1.75;
				float sv=hsv.y*hsv.z*rate;
				hsv.z+=sv*(1-rate)/2;
				hsv.y=sv/hsv.z;
				return half4(hsv2rgb(hsv),c.a);
			}

			half4 frag (v2f i) : SV_Target
			{
			    if(i.uv.y < _From || i.uv.y > _To) return 0;
				float4 c = tex2D(_MainTex,i.uv) ;
				float4 inColor = lerp(_LowColor,_HighColor,i.uv2.x);
				if(_Highlight == 1)
				{
					inColor = Highlight(inColor);
				}
				c *= inColor;
				return c;
			}
			ENDCG
		}
	}
}
