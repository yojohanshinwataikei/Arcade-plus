Shader "Arcade/Arc"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}
		[PerRendererData] _HighColor ("HighColor", Color) = (1,1,1,1)
		[PerRendererData] _LowColor ("LowColor", Color) = (1,1,1,1)
		[PerRendererData] _From ("From", Float) = 0
		[PerRendererData] _To ("To", Float) = 1
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "CanUseSpriteAtlas"="true" "PreviewType" = "Plane"  }

        Cull Off
        Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
				half4 color    : COLOR;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			float _From,_To;
			float4 _HighColor;
			float4 _LowColor;
            float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				o.uv2 = v.uv2;
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
			    if(i.uv.y < _From || i.uv.y > _To) return 0;
				float4 c = tex2D(_MainTex,i.uv) ;
				float4 inColor = lerp(_LowColor,_HighColor,clamp(i.uv2.x,0,1));
				c *= inColor;
				return c;
			}
			ENDHLSL
		}
	}
}

