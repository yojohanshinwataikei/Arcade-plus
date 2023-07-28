Shader "SelectionRendering/Selection"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
		Cull Off
        LOD 200

		Pass
		{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return float4(1,1,1,1);
            }
            ENDHLSL
		}
    }
}
