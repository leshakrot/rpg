#include "../sgsr2_common.hlsl"

//============================================================================================================
//
//
//                  Copyright (c) 2024, Qualcomm Innovation Center, Inc. All rights reserved.
//                              SPDX-License-Identifier: BSD-3-Clause
//
//============================================================================================================

TEXTURE2D_X_UINT(PrevLumaHistory)                   : register(t0);
TEXTURE2D_X(MotionDepthAlphaBuffer)                 : register(t1);
TEXTURE2D_X_UINT(YCoCgColor)                        : register(t2);
RW_TEXTURE2D_X(float4, MotionDepthClipAlphaBuffer)  : register(u0);
RW_TEXTURE2D_X(uint, LumaHistory)                   : register(u1);

[numthreads(8, 8, 1)]
void CS_Activate(uint3 gl_GlobalInvocationID : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(gl_GlobalInvocationID.z);
    
    int2 sampleOffset[4] = {
        int2(-1, -1),
        int2(-1, +0),
        int2(+0, -1),
        int2(+0, +0)
    };

    uint2 InputPos = gl_GlobalInvocationID.xy;

    float2 ViewportUV = (float2(gl_GlobalInvocationID.xy) + 0.5f) * renderSizeRcp;
    float2 gatherCoord = ViewportUV + 0.5f * renderSizeRcp;
    uint luma_reference32 = GATHER_RED_TEXTURE2D_X(YCoCgColor, S_POINT_CLAMP, gatherCoord).w;
    float luma_reference = DecodeColorY(luma_reference32);

    float4 mda = LOAD_TEXTURE2D_X(MotionDepthAlphaBuffer, gl_GlobalInvocationID.xy).xyzw; //motion depth alpha
    float depth = mda.z;
    float alphamask = mda.w;
    float2 motion = mda.xy;

 #ifdef REQUEST_NDC_Y_UP
    float2 PrevUV = float2(-0.5f * motion.x + ViewportUV.x, 0.5f * motion.y + ViewportUV.y);
 #else
    float2 PrevUV = float2(-0.5f * motion.x + ViewportUV.x, -0.5f * motion.y + ViewportUV.y);
 #endif
    float depthclip = 0.0;

    if (DEPTH_CLIP(depth)) {
        float2 Prevf_sample = PrevUV * float2(renderSize) - 0.5f;
        float2 Prevfrac = Prevf_sample - floor(Prevf_sample);
        float OneMinusPrevfacx = 1.0 - Prevfrac.x;

        float Bilinweights[4] = {
            OneMinusPrevfacx - OneMinusPrevfacx * Prevfrac.y,
            Prevfrac.x - Prevfrac.x * Prevfrac.y,
            OneMinusPrevfacx * Prevfrac.y,
            Prevfrac.x * Prevfrac.y
        };

        float diagonal_length = length(float2(renderSize));
        float Wdepth = 0.0;
        float Wsum = 0.0;
        float Ksep = 1.37e-05f;
        float Kfov = cameraFovAngleHor;
        float Ksep_Kfov_diagonal = Ksep * Kfov * diagonal_length;
        for (int index = 0; index < 4; index+=2){
            float4 gPrevdepth = GATHER_BLUE_TEXTURE2D_X_OFFSET(MotionDepthAlphaBuffer, S_POINT_CLAMP, PrevUV, sampleOffset[index]);
            float tdepth1 = min(gPrevdepth.x, gPrevdepth.y);
            float tdepth2 = min(gPrevdepth.z, gPrevdepth.w);
            float fPrevdepth = min(tdepth1, tdepth2);

            float Depthsep = Ksep_Kfov_diagonal * (1.0 - min(fPrevdepth, depth));
            float weight = Bilinweights[index];
            Wdepth += clamp(Depthsep / (abs(fPrevdepth - depth) + EPSILON), 0.0, 1.0) * weight;

            float2 gPrevdepth2 = GATHER_BLUE_TEXTURE2D_X_OFFSET(MotionDepthAlphaBuffer, S_POINT_CLAMP, PrevUV, sampleOffset[index + int(1)]).zw;
            fPrevdepth = min(min(gPrevdepth2.x, gPrevdepth2.y), tdepth2);
            Depthsep = Ksep_Kfov_diagonal * (1.0 - min(fPrevdepth, depth));
            weight = Bilinweights[index + int(1)];
            Wdepth += clamp(Depthsep / (abs(fPrevdepth - depth) + EPSILON), 0.0, 1.0) * weight;
        }
        depthclip = clamp(1.0f - Wdepth, 0.0, 1.0);
    }

    float2 current_luma_diff;
    uint prev_luma_diff_pack = GATHER_RED_TEXTURE2D_X(PrevLumaHistory, S_POINT_CLAMP, PrevUV).w;
    float2 prev_luma_diff;
    prev_luma_diff.x = unpackHalf2x16(prev_luma_diff_pack >> 16u).x;
    prev_luma_diff.y = unpackHalf2x16((prev_luma_diff_pack & uint(0xFFFF))).x;

    bool enable = false;
    if (depthclip + float(reset) < 0.1)
    {
        enable = (all(PrevUV >= 0.0f) && all(PrevUV <= 1.0f));
    }
    float luma_diff = luma_reference - prev_luma_diff.x;
    if (!enable)
    {
        current_luma_diff.x = 0.0;
        current_luma_diff.y = 0.0;
    }else{
        current_luma_diff.x = luma_reference;
        current_luma_diff.y = (prev_luma_diff.y != 0.0f) ? ((sign(luma_diff) == sign(prev_luma_diff.y)) ? (sign(luma_diff) * min(abs(prev_luma_diff.y), abs(luma_diff))) : prev_luma_diff.y) : luma_diff;
    }

    alphamask = floor(alphamask) + 0.5f * float((current_luma_diff.x != 0.0f) && (abs(current_luma_diff.y) != abs(luma_diff)));
    LumaHistory[COORD_TEXTURE2D_X(InputPos)] = (packHalf2x16(float2(current_luma_diff.x, 0.0)) << 16u) | packHalf2x16(float2(current_luma_diff.y, 0.0));
    MotionDepthClipAlphaBuffer[COORD_TEXTURE2D_X(InputPos)] = float4(motion, depthclip, alphamask);
}
