Shader "Arcade/Shadow"
{
	Properties
	{
		[PerRendererData] _Color ("Color", Color) = (1,1,1,1)
		[PerRendererData] _From ("From", Float) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType"="Transparent" "CanUseSpriteAtlas"="true"  }

        Cull Off
        Lighting Off
		ZWrite Off
        Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

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

			float _From;
			float4 _Color;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
			    if(i.uv.y < _From) return 0;
				float4 c = _Color;
				c.rgb *= c.a;
				return c;
			}
			ENDCG
		}
	}
}
