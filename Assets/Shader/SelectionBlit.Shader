Shader "SelectionRendering/SelectionBlit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            #define SAMPLE_COUNT 16

            uniform float _SampleDistance;
            uniform float4 _OutlineColor;
            uniform float4 _SelectionColor;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);

                if(color.a>0.1f){
                    return _SelectionColor;
                }

                int insideCount=0;

                float2 texelSize=1.0/_BlitTextureSize;
                for(int i=0;i<SAMPLE_COUNT;i++){
                    float s;
                    float c;
                    sincos(radians(360.0f/((float)SAMPLE_COUNT)*((float)i)),s,c);
                    float2 uv=input.texcoord+float2(s,c)*texelSize*_SampleDistance;
                    float4 sampleColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                    if(sampleColor.a>0.1f){
                        insideCount+=1;
                    }
                }

                if(insideCount>=1){
                    return _OutlineColor;
                }
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}