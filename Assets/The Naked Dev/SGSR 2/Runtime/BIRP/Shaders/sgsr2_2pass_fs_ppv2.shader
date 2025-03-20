Shader "Hidden/TND/PPV2/sgsr2_2pass_fs"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Convert"
            
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragConvert
            #pragma target 4.5
            //#pragma enable_d3d11_debug_symbols

            #define UNITY_CG_INCLUDED
            #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
            #undef EPSILON
            #include "sgsr2_birp.hlsl"
            #include "../../Shaders/2_pass_fs/sgsr2_convert.hlsl"

            void FragConvert(VaryingsDefault i, out float4 MotionDepthClipAlphaBuffer: SV_Target)
            {
                sgsr2_convert(i.texcoord, MotionDepthClipAlphaBuffer);
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "Upscale"
            
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpscale
            #pragma target 4.5
            //#pragma enable_d3d11_debug_symbols

            #define UNITY_CG_INCLUDED
            #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
            #undef EPSILON
            #include "sgsr2_birp.hlsl"
            #include "../../Shaders/2_pass_fs/sgsr2_upscale.hlsl"

            void FragUpscale(VaryingsDefault i, out half4 OutputColor: SV_Target0, out half4 HistoryOutput: SV_Target1)
            {
                sgsr2_upscale(i.texcoord, OutputColor);
                HistoryOutput = OutputColor;
            }
            
            ENDHLSL
        }
    }

    Fallback "Hidden/TND/PPV2/sgsr2_2pass_fs_low"
}
