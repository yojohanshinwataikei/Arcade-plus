Shader "Arcade/Arc"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_From ("From", Float) = 0
		_To ("To", Float) = 1
		_Highlight("Highlight", Int) = 0
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
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color    : COLOR;
				float2 uv : TEXCOORD0;
			};

			int _Highlight;
			float _From,_To;
			float4 _Color;
            float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color * _Color;
				return o;
			}

			half4 Highlight(half4 c)
			{
				// TODO: the highlight thing should be more generic
				fixed3 hsv = rgb2hsv(c.rgb);
				if(c.r<0.5) {if(c.b>0.66){hsv.x += 0.1f;}else{hsv.x-=0.1f;}}
				else hsv.y += 1.2f;
				return half4(hsv2rgb(hsv),c.a);
			}

			half4 frag (v2f i) : SV_Target
			{
			    if(i.uv.y < _From || i.uv.y > _To) return 0;
				float4 c = tex2D(_MainTex,i.uv) ;
				float4 inColor = i.color;
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
