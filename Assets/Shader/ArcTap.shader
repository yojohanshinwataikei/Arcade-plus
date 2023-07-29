Shader "Arcade/ArcTap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[PerRendererData] _Alpha ("Alpha", Float) = 1
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType"="Transparent" "CanUseSpriteAtlas"="true"  }
		Cull Off
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
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float _Alpha;
			sampler2D _MainTex;
            float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				half4 c = half4(tex2D(_MainTex,i.uv).rgb, _Alpha);
				return c;
			}
			ENDHLSL
		}
	}
}

