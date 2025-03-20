#include "../sgsr2_common.hlsl"

//============================================================================================================
//
//
//                  Copyright (c) 2024, Qualcomm Innovation Center, Inc. All rights reserved.
//                              SPDX-License-Identifier: BSD-3-Clause
//
//============================================================================================================

TEXTURE2D_X(InputOpaqueColor)                   : register(t0);
TEXTURE2D_X(InputColor)                         : register(t1);
TEXTURE2D_X(InputDepth)                         : register(t2);
TEXTURE2D_X(InputVelocity)                      : register(t3);
RW_TEXTURE2D_X(float4, MotionDepthAlphaBuffer)  : register(u0);
RW_TEXTURE2D_X(uint, YCoCgColor)                : register(u1);

[numthreads(8, 8, 1)]
void CS_Convert(uint3 gl_GlobalInvocationID : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(gl_GlobalInvocationID.z);
    
    half h0 = preExposure;
    uint2 InputPos = gl_GlobalInvocationID.xy;
    
    float2 gatherCoord = float2(gl_GlobalInvocationID.xy) * renderSizeRcp;
    float2 ViewportUV = gatherCoord + 0.5f * renderSizeRcp;

    //derived from ffx_fsr2_reconstruct_dilated_velocity_and_previous_depth.h
    //FindNearestDepth

    int2 InputPosBtmRight = int2(1, 1) + int2(InputPos);
    float NearestZ = LOAD_TEXTURE2D_X(InputDepth, InputPosBtmRight).x;
    
    float4 topleft = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, gatherCoord);

    NearestZ = DEPTH_NEAREST(topleft.x, NearestZ);
    NearestZ = DEPTH_NEAREST(topleft.y, NearestZ);
    NearestZ = DEPTH_NEAREST(topleft.z, NearestZ);
    NearestZ = DEPTH_NEAREST(topleft.w, NearestZ);

    float2 v11 = float2(renderSizeRcp.x, 0.0);
    float2 topRight = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord + v11)).yz;

    NearestZ = DEPTH_NEAREST(topRight.x, NearestZ);
    NearestZ = DEPTH_NEAREST(topRight.y, NearestZ);

    float2 v13 = float2(0.0, renderSizeRcp.y);
    float2 bottomLeft = GATHER_RED_TEXTURE2D_X(InputDepth, S_POINT_CLAMP, (gatherCoord + v13)).xy;

    NearestZ = DEPTH_NEAREST(bottomLeft.x, NearestZ);
    NearestZ = DEPTH_NEAREST(bottomLeft.y, NearestZ);

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
        float3 Position = float3(ScreenPos, NearestZ);    //this_clip
        float4 PreClip = clipToPrevClip[3] + ((clipToPrevClip[2] * Position.z) + ((clipToPrevClip[1] * ScreenPos.y) + (clipToPrevClip[0] * ScreenPos.x)));
        float2 PreScreen = PreClip.xy / PreClip.w;
        motion = Position.xy - PreScreen;
    }

    ////////////compute luma
    half3 Colorrgb = LOAD_TEXTURE2D_X(InputColor, InputPos).xyz;

    ///simple tonemap
    Colorrgb /= max(max(Colorrgb.x, Colorrgb.y), Colorrgb.z) + h0;

    float3 Colorycocg;
    Colorycocg.x = 0.25 * (Colorrgb.x + 2.0 * Colorrgb.y + Colorrgb.z);
    Colorycocg.y = clamp(0.5 * Colorrgb.x + 0.5 - 0.5 * Colorrgb.z, 0.0, 1.0);
    Colorycocg.z = clamp(Colorycocg.x + Colorycocg.y - Colorrgb.x, 0.0, 1.0);

    //now color YCoCG all in the range of [0,1]
    uint x11 = uint(Colorycocg.x * 2047.5);
    uint y11 = uint(Colorycocg.y * 2047.5);
    uint z10 = uint(Colorycocg.z * 1023.5);

    half3 Colorprergb = LOAD_TEXTURE2D_X(InputOpaqueColor, InputPos).xyz;

    ///simple tonemap
    Colorprergb /= max(max(Colorprergb.x, Colorprergb.y), Colorprergb.z) + h0;
    half3 delta = abs(Colorrgb - Colorprergb);
    half alpha_mask = max(delta.x, max(delta.y, delta.z));
    alpha_mask = (0.35f * 1000.0f) * alpha_mask;

    YCoCgColor[COORD_TEXTURE2D_X(InputPos)] = ((x11 << 21u) | (y11 << 10u)) | z10;
    MotionDepthAlphaBuffer[COORD_TEXTURE2D_X(InputPos)] = float4(motion, NearestZ, alpha_mask);
}
