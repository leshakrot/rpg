#include "../sgsr2_common.hlsl"

//============================================================================================================
//
//
//                  Copyright (c) 2024, Qualcomm Innovation Center, Inc. All rights reserved.
//                              SPDX-License-Identifier: BSD-3-Clause
//
//============================================================================================================

// precision highp float;
// precision highp int;

TEXTURE2D_X_HALF(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D_X_HALF(_CameraMotionVectorsTexture);
SAMPLER(sampler_CameraMotionVectorsTexture);
#define InputDepth      _CameraDepthTexture
#define InputVelocity   _CameraMotionVectorsTexture

void sgsr2_convert(const half2 texCoord, out float4 MotionDepthClipAlphaBuffer)
{
    uint2 InputPos = uint2(texCoord * renderSize);
    float2 gatherCoord = texCoord - 0.5f * renderSizeRcp;
    
    // texture gather to find nearest depth
    //      a  b  c  d
    //      e  f  g  h
    //      i  j  k  l
    //      m  n  o  p
    //btmLeft mnji
    //btmRight oplk
    //topLeft  efba
    //topRight ghdc

    float4 btmLeft = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, gatherCoord);
    float2 v10 = float2(renderSizeRcp.x * 2.0f, 0.0);
    float4 btmRight = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord+v10));
    float2 v12 = float2(0.0, renderSizeRcp.y * 2.0f);
    float4 topLeft = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord+v12));
    float2 v14 = float2(renderSizeRcp.x * 2.0f, renderSizeRcp.y * 2.0f);
    float4 topRight = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord+v14));
    float maxC = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(btmLeft.z,btmRight.w),topLeft.y),topRight.x);
    float btmLeft4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(btmLeft.y,btmLeft.x),btmLeft.z),btmLeft.w);
    float btmLeftMax9 = DEPTH_NEAREST(topLeft.x,DEPTH_NEAREST(DEPTH_NEAREST(maxC,btmLeft4),btmRight.x));

    float depthclip = 0.0;
    if (DEPTH_CLIP(maxC))
    {
        float btmRight4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(btmRight.y,btmRight.x),btmRight.z),btmRight.w);
        float topLeft4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(topLeft.y,topLeft.x),topLeft.z),topLeft.w);
        float topRight4 = DEPTH_NEAREST(DEPTH_NEAREST(DEPTH_NEAREST(topRight.y,topRight.x),topRight.z),topRight.w);

        float Wdepth = 0.0;
        float Ksep = 1.37e-05f;
        float Kfov = cameraFovAngleHor;
        float diagonal_length = length(renderSize);
        float Ksep_Kfov_diagonal = Ksep * Kfov * diagonal_length;

        float Depthsep = Ksep_Kfov_diagonal * (1.0 - maxC);
        Wdepth += clamp((Depthsep / (abs(maxC - btmLeft4) + EPSILON)), 0.0, 1.0);
        Wdepth += clamp((Depthsep / (abs(maxC - btmRight4) + EPSILON)), 0.0, 1.0);
        Wdepth += clamp((Depthsep / (abs(maxC - topLeft4) + EPSILON)), 0.0, 1.0);
        Wdepth += clamp((Depthsep / (abs(maxC - topRight4) + EPSILON)), 0.0, 1.0);
        depthclip = clamp(1.0f - Wdepth * 0.25, 0.0, 1.0);
    }

    //refer to ue/fsr2 PostProcessFFX_FSR2ConvertVelocity.usf, and using nearest depth for dilated motion

    float2 EncodedVelocity = LOAD_TEXTURE2D_X(InputVelocity, int2(InputPos)).xy;

    float2 motion;
    if (any(abs(EncodedVelocity)) > 0.0)
    {
        motion = decodeVelocityFromTexture(EncodedVelocity.xy + jitterCancellation);
    }
    else
    {
#ifdef REQUEST_NDC_Y_UP
        float2 ScreenPos = float2(2.0f * texCoord.x - 1.0f, 1.0f - 2.0f * texCoord.y);
#else
        float2 ScreenPos = float2(2.0f * texCoord - 1.0f);
#endif
        float3 Position = float3(ScreenPos, btmLeftMax9);    //this_clip
        float4 PreClip = clipToPrevClip[3] + ((clipToPrevClip[2] * Position.z) + ((clipToPrevClip[1] * ScreenPos.y) + (clipToPrevClip[0] * ScreenPos.x)));
        float2 PreScreen = PreClip.xy / PreClip.w;
        motion = Position.xy - PreScreen;
    }
    MotionDepthClipAlphaBuffer = float4(motion, depthclip, 0.0);
}
