Shader "Hidden/TND/BIRP/sgsr2_2pass_fs"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Convert"
            
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_convert
            #pragma target 4.5
            //#pragma enable_d3d11_debug_symbols

            #include "sgsr2_birp.hlsl"
            #include "../../Shaders/2_pass_fs/sgsr2_convert.hlsl"

            void frag_convert(v2f_img i, out float4 MotionDepthClipAlphaBuffer: SV_Target)
            {
                sgsr2_convert(i.uv, MotionDepthClipAlphaBuffer);
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "Upscale"
            
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_upscale
            #pragma target 4.5
            //#pragma enable_d3d11_debug_symbols

            #include "sgsr2_birp.hlsl"
            #include "../../Shaders/2_pass_fs/sgsr2_upscale.hlsl"

            void frag_upscale(v2f_img i, out half4 OutputColor: SV_Target)
            {
                sgsr2_upscale(i.uv, OutputColor);
            }
            
            ENDHLSL
        }
    }

    Fallback "Hidden/TND/BIRP/sgsr2_2pass_fs_low"
}
