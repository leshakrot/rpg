#include "../sgsr2_common.hlsl"

//============================================================================================================
//
//
//                  Copyright (c) 2024, Qualcomm Innovation Center, Inc. All rights reserved.
//                              SPDX-License-Identifier: BSD-3-Clause
//
//============================================================================================================

TEXTURE2D_X(InputColor)                             : register(t0);
TEXTURE2D_X(InputDepth)                             : register(t1);
TEXTURE2D_X(InputVelocity)                          : register(t2);
RW_TEXTURE2D_X(float4, MotionDepthClipAlphaBuffer)  : register(u0);
RW_TEXTURE2D_X(uint, YCoCgColor)                    : register(u1);

[numthreads(8, 8, 1)]
void CS_Convert(uint3 gl_GlobalInvocationID : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(gl_GlobalInvocationID.z);
    
    half Exposure_co_rcp = preExposure;
    float2 ViewportSizeInverse = displaySizeRcp.xy;
    uint2 InputPos = gl_GlobalInvocationID.xy;

    float2 gatherCoord = float2(gl_GlobalInvocationID.xy) * ViewportSizeInverse;
    float2 ViewportUV = gatherCoord + 0.5f * ViewportSizeInverse;

    //derived from ffx_fsr2_reconstruct_dilated_velocity_and_previous_depth.h
    //FindNearestDepth

    float4 topleft = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, gatherCoord);
    float2 v10 = float2(ViewportSizeInverse.x*2.0, 0.0);
    float4 topRight = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord+v10));
    float2 v12 = float2(0.0, ViewportSizeInverse.y*2.0);
    float4 bottomLeft = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord+v12));
    float2 v14 = float2(ViewportSizeInverse.x*2.0, ViewportSizeInverse.y*2.0);
    float4 bottomRight = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord+v14));
    float maxC = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(topleft.y,topRight.x),bottomLeft.z),bottomRight.w);
    float topleft4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(topleft.y,topleft.x),topleft.z),topleft.w);
    float topLeftMax9 = DEPTH_NEAREST(bottomLeft.w,DEPTH_NEAREST(DEPTH_NEAREST(maxC,topleft4),topRight.w));

    float depthclip = 0.0;
    if (DEPTH_CLIP(maxC))
    {
        float topRight4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(topRight.y,topRight.x),topRight.z),topRight.w);
        float bottomLeft4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(bottomLeft.y,bottomLeft.x),bottomLeft.z),bottomLeft.w);
        float bottomRight4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(bottomRight.y,bottomRight.x),bottomRight.z),bottomRight.w);

        float Wdepth = 0.0;
        float Ksep = 1.37e-05f;
        float Kfov = cameraFovAngleHor;
        float diagonal_length = length(float2(renderSize));
        float Ksep_Kfov_diagonal = Ksep * Kfov * diagonal_length;

        float Depthsep = Ksep_Kfov_diagonal * (1.0 - maxC);
        Wdepth += clamp((Depthsep / (abs(maxC - topleft4) + EPSILON)), 0.0, 1.0);
        Wdepth += clamp((Depthsep / (abs(maxC - topRight4) + EPSILON)), 0.0, 1.0);
        Wdepth += clamp((Depthsep / (abs(maxC - bottomLeft4) + EPSILON)), 0.0, 1.0);
        Wdepth += clamp((Depthsep / (abs(maxC - bottomRight4) + EPSILON)), 0.0, 1.0);
        depthclip = clamp(1.0f - Wdepth*0.25, 0.0, 1.0);
    }

    //refer to ue/fsr2 PostProcessFFX_FSR2ConvertVelocity.usf, and using nearest depth for dilated motion

    float2 EncodedVelocity = LOAD_TEXTURE2D_X(InputVelocity, InputPos).xy;

    float2 motion;
    if (any(abs(EncodedVelocity) > 0.0))
    {
        motion = decodeVelocityFromTexture(EncodedVelocity.xy + jitterCancellation);
    }
    else
    {
#ifdef REQUEST_NDC_Y_UP
        float2 ScreenPos = float2(2.0f * ViewportUV.x - 1.0f, 1.0f - 2.0f * ViewportUV.y);
#else
         float2 ScreenPos = float2(2.0f * ViewportUV - 1.0f);
#endif
        float3 Position = float3(ScreenPos, topLeftMax9);    //this_clip
        float4 PreClip = clipToPrevClip[3] + ((clipToPrevClip[2] * Position.z) + ((clipToPrevClip[1] * ScreenPos.y) + (clipToPrevClip[0] * ScreenPos.x)));
        float2 PreScreen = PreClip.xy / PreClip.w;
        motion = Position.xy - PreScreen;
    }

    ////////////compute luma
    half3 Colorrgb = LOAD_TEXTURE2D_X(InputColor, InputPos).xyz;

    ///simple tonemap
    float ColorMax = max(max(Colorrgb.x, Colorrgb.y), Colorrgb.z) + Exposure_co_rcp;
    Colorrgb /= ColorMax;

    float3 Colorycocg;
    Colorycocg.x = 0.25 * (Colorrgb.x + 2.0 * Colorrgb.y + Colorrgb.z);
    Colorycocg.y = clamp(0.5 * Colorrgb.x + 0.5 - 0.5 * Colorrgb.z, 0.0, 1.0);
    Colorycocg.z = clamp(Colorycocg.x + Colorycocg.y - Colorrgb.x, 0.0, 1.0);

    //now color YCoCG all in the range of [0,1]
    uint x11 = uint(Colorycocg.x * 2047.5);
    uint y11 = uint(Colorycocg.y * 2047.5);
    uint z10 = uint(Colorycocg.z * 1023.5);

    YCoCgColor[COORD_TEXTURE2D_X(InputPos)] = ((x11 << 21u) | (y11 << 10u)) | z10;

    half4 v29 = half4(motion, depthclip, ColorMax);
    MotionDepthClipAlphaBuffer[COORD_TEXTURE2D_X(InputPos)] = v29;
}
