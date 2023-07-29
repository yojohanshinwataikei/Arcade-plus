Shader "Arcade/HoldNote"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}
		[PerRendererData] _From ("From", Float) = 0
		[PerRendererData] _To ("To",Float) = 1
		[PerRendererData] _Alpha ("Alpha", Float) = 1
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType" = "Plane" }
		ZWrite Off
		Cull Off
		Blend One OneMinusSrcAlpha

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "ColorSpace.hlsl"

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

			float _From,_To,_Alpha;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
			    if(i.uv.y > _To || i.uv.y < _From) return 0;
				float v = 1.-(i.uv.y-_To)/(_From-_To);
				half4 c = half4(tex2D(_MainTex,float2(i.uv.x,v)).rgb, _Alpha);
				c.rgb *= c.a;
				return c;
			}
			ENDHLSL
		}
	}
}

