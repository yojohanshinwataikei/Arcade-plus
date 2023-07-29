Shader "Arcade/Track"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}
		[PerRendererData] _Phase ("Phase", Float) = 0
		[PerRendererData] _Color ("Color", Color) = (1,1,1,1)
		_TextureWidth ("TextureWidth", Float) = 1024
		_TextureHeight ("TextureHeight", Float) = 256
		_XStart ("XStart", Float) = 36
		_XEnd ("XEnd", Float) = 988
		_TrackLength ("TrackLength", Float) = 153.5
		_TrackRepeat ("TrackRepeat", Float) = 4.57142857
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

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color    : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color    : COLOR;
				float2 uv : TEXCOORD0;
			};

			float _Phase;
			sampler2D _MainTex;
			float4 _Color;
			float _TextureWidth;
			float _TextureHeight;
			float _XStart;
			float _XEnd;
			float _TrackLength;
			float _TrackRepeat;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
				o.color = v.color*_Color;
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
			    float2 p = i.uv;
				p.x = lerp(_XStart/_TextureWidth,_XEnd/_TextureWidth,p.x);
				p.y = (p.y*_TrackLength/_TrackRepeat + _Phase);
				float4 c= tex2D(_MainTex,p);
				c.a=1.0;
				c*=i.color;
				c.rgb *= c.a;
				return c;
			}
			ENDHLSL
		}
	}
}

