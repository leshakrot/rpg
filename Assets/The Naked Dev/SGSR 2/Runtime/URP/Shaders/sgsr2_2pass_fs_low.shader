Shader "Hidden/TND/URP/sgsr2_2pass_fs_low"
{
    // Fallback version of SGSR2 2-pass fragment for low-end shader model support
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Convert"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag_convert
            #pragma target 3.5
            //#pragma enable_d3d11_debug_symbols

            #include "sgsr2_urp.hlsl"
            #include "../../Shaders/2_pass_fs/sgsr2_convert.hlsl"

            void frag_convert(Varyings i, out float4 MotionDepthClipAlphaBuffer: SV_Target)
            {
                sgsr2_convert(i.texcoord, MotionDepthClipAlphaBuffer);
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "Upscale"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag_upscale
            #pragma target 3.5
            //#pragma enable_d3d11_debug_symbols

            #include "sgsr2_urp.hlsl"
            #include "../../Shaders/2_pass_fs/sgsr2_upscale.hlsl"

            void frag_upscale(Varyings i, out half4 OutputColor: SV_Target0, out half4 HistoryOutput: SV_Target1)
            {
                sgsr2_upscale(i.texcoord, OutputColor);
                HistoryOutput = OutputColor;
            }
            
            ENDHLSL
        }
    }
}
