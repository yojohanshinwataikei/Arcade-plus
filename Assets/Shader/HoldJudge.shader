Shader "Arcade/HoldJudge"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType" = "Plane" }
		ZWrite Off
		Cull Off
		Blend SrcAlpha One

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half4 color    : COLOR;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				o.color.a += 0.5;
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				float4 c = tex2D(_MainTex,i.uv) * i.color;
				c.rgb *= c.a;
				return c;
			}
			ENDHLSL
		}
	}
}

