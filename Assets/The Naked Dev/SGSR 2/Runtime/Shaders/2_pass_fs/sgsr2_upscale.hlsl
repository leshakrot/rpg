#include "../sgsr2_common.hlsl"

//============================================================================================================
//
//
//                  Copyright (c) 2024, Qualcomm Innovation Center, Inc. All rights reserved.
//                              SPDX-License-Identifier: BSD-3-Clause
//
//============================================================================================================

//precision mediump float;
//precision highp int;

TEXTURE2D_X_HALF(PrevHistoryOutput);
TEXTURE2D_X_HALF(MotionDepthClipAlphaBuffer);
TEXTURE2D_X_HALF(InputColor);

SAMPLER(samplerPrevHistoryOutput);
SAMPLER(samplerMotionDepthClipAlphaBuffer);
SAMPLER(samplerInputColor);

void sgsr2_upscale(const half2 texCoord, out half4 Output)
{
    half Biasmax_viewportXScale = scaleRatio.x;
    half scalefactor = scaleRatio.y;

    float2 Hruv = texCoord;

    float2 Jitteruv;
    Jitteruv.x = clamp(Hruv.x + (jitterOffset.x * renderSizeRcp.x), 0.0, 1.0);
    Jitteruv.y = clamp(Hruv.y + (jitterOffset.y * renderSizeRcp.y), 0.0, 1.0);

    int2 InputPos = int2(Jitteruv * renderSize);

    float3 mda = SAMPLE_TEXTURE2D_X_LOD(MotionDepthClipAlphaBuffer, S_LINEAR_CLAMP, Jitteruv, 0.0).xyz;
    float2 Motion = mda.xy;

    float2 PrevUV;
    PrevUV.x = clamp(-0.5 * Motion.x + Hruv.x, 0.0, 1.0);
#ifdef REQUEST_NDC_Y_UP
    PrevUV.y = clamp(0.5 * Motion.y + Hruv.y, 0.0, 1.0);
#else
    PrevUV.y = clamp(-0.5 * Motion.y + Hruv.y, 0.0, 1.0);
#endif

    half depthfactor = mda.z;

    half3 HistoryColor = SAMPLE_TEXTURE2D_X_LOD(PrevHistoryOutput, S_LINEAR_CLAMP, PrevUV, 0.0).xyz;

    /////upsample and compute box
    half4 Upsampledcw = 0.0f;
    half biasmax = Biasmax_viewportXScale ;
    half biasmin = max(1.0f, 0.3 + 0.3 * biasmax);
    half biasfactor = 0.25f * depthfactor;
    half kernelbias = lerp(biasmax, biasmin, biasfactor);
    half motion_viewport_len = length(Motion * displaySize);
    half curvebias = lerp(-2.0, -3.0, clamp(motion_viewport_len * 0.02, 0.0, 1.0));

    half3 rectboxcenter = 0.0f;
    half3 rectboxvar = 0.0f;
    half rectboxweight = 0.0;
    float2 srcpos = half2(InputPos) + 0.5f - jitterOffset;

    kernelbias *= 0.5f;
    half kernelbias2 = kernelbias * kernelbias;
    half2 srcpos_srcOutputPos = srcpos - Hruv * renderSize;  //srcOutputPos = Hruv * params.renderSize;
    half3 rectboxmin;
    half3 rectboxmax;
    half3 topMid = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(0, 1)).xyz;
    {

        half3 samplecolor = topMid;
        half2 baseoffset = srcpos_srcOutputPos + half2(0.0, 1.0);
        half baseoffset_dot = dot(baseoffset, baseoffset);
        half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        half weight = FastLanczos(base);
        Upsampledcw += half4(samplecolor * weight, weight);
        half boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = samplecolor;
        rectboxmax = samplecolor;
        half3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    half3 rightMid = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(1, 0)).xyz;
    {

        half3 samplecolor = rightMid;
        half2 baseoffset = srcpos_srcOutputPos + half2(1.0, 0.0);
        half baseoffset_dot = dot(baseoffset, baseoffset);
        half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        half weight = FastLanczos(base);
        Upsampledcw += half4(samplecolor * weight, weight);
        half boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        half3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    half3 leftMid = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(-1, 0)).xyz;
    {

        half3 samplecolor = leftMid;
        half2 baseoffset = srcpos_srcOutputPos + half2(-1.0, 0.0);
        half baseoffset_dot = dot(baseoffset, baseoffset);
        half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        half weight = FastLanczos(base);
        Upsampledcw += half4(samplecolor * weight, weight);
        half boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        half3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    half3 centerMid = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(0, 0)).xyz;
    {

        half3 samplecolor = centerMid;
        half2 baseoffset = srcpos_srcOutputPos;
        half baseoffset_dot = dot(baseoffset, baseoffset);
        half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        half weight = FastLanczos(base);
        Upsampledcw += half4(samplecolor * weight, weight);
        half boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        half3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    half3 btmMid = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(0, -1)).xyz;
    {

        half3 samplecolor = btmMid;
        half2 baseoffset = srcpos_srcOutputPos + half2(0.0, -1.0);
        half baseoffset_dot = dot(baseoffset, baseoffset);
        half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        half weight = FastLanczos(base);
        Upsampledcw += half4(samplecolor * weight, weight);
        half boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        half3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }

    //if (params.sameCameraFrmNum!=0u)  //maybe disable this for ultra performance
    //if (false)  //maybe disable this for ultra performance, true could generate more realistic output
    {
        {
            half3 topRight = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(1, 1)).xyz;
            half3 samplecolor = topRight;
            half2 baseoffset = srcpos_srcOutputPos + half2(1.0, 1.0);
            half baseoffset_dot = dot(baseoffset, baseoffset);
            half base = clamp(baseoffset_dot * kernelbias2, 0.0, 1.0);
            half weight = FastLanczos(base);
            Upsampledcw += half4(samplecolor * weight, weight);
            half boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            half3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }
        {
            half3 topLeft = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(-1, 1)).xyz;
            half3 samplecolor = topLeft;
            half2 baseoffset = srcpos_srcOutputPos + half2(-1.0, 1.0);
            half baseoffset_dot = dot(baseoffset, baseoffset);
            half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
            half weight = FastLanczos(base);
            Upsampledcw += half4(samplecolor * weight, weight);
            half boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            half3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }
        {
            half3 btmRight = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(1, -1)).xyz;
            half3 samplecolor = btmRight;
            half2 baseoffset = srcpos_srcOutputPos + half2(1.0, -1.0);
            half baseoffset_dot = dot(baseoffset, baseoffset);
            half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
            half weight = FastLanczos(base);
            Upsampledcw += half4(samplecolor * weight, weight);
            half boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            half3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }

        {
            half3 btmLeft = LOAD_TEXTURE2D_X(InputColor, InputPos + int2(-1, -1)).xyz;
            half3 samplecolor = btmLeft;
            half2 baseoffset = srcpos_srcOutputPos + half2(-1.0, -1.0);
            half baseoffset_dot = dot(baseoffset, baseoffset);
            half base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
            half weight = FastLanczos(base);
            Upsampledcw += half4(samplecolor * weight, weight);
            half boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            half3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }
    }

    rectboxweight = 1.0 / rectboxweight;
    rectboxcenter *= rectboxweight;
    rectboxvar *= rectboxweight;
    rectboxvar = sqrt(abs(rectboxvar - rectboxcenter * rectboxcenter));

    Upsampledcw.xyz =  clamp(Upsampledcw.xyz / Upsampledcw.w, rectboxmin-0.075f, rectboxmax+0.075f);
    Upsampledcw.w = Upsampledcw.w * (1.0f / 3.0f) ;

    half baseupdate = 1.0f - depthfactor;
    baseupdate = min(baseupdate, lerp(baseupdate, Upsampledcw.w *10.0f, clamp(10.0f* motion_viewport_len, 0.0, 1.0)));
    baseupdate = min(baseupdate, lerp(baseupdate, Upsampledcw.w, clamp(motion_viewport_len *0.05f, 0.0, 1.0)));
    half basealpha = baseupdate;

    half boxscale = max(depthfactor, clamp(motion_viewport_len * 0.05f, 0.0, 1.0));
    half boxsize = lerp(scalefactor, 1.0f, boxscale);
    half3 sboxvar = rectboxvar * boxsize;
    half3 boxmin = rectboxcenter - sboxvar;
    half3 boxmax = rectboxcenter + sboxvar;
    rectboxmax = min(rectboxmax, boxmax);
    rectboxmin = max(rectboxmin, boxmin);

    half3 clampedcolor = clamp(HistoryColor, rectboxmin, rectboxmax);
    half startLerpValue = minLerpContribution;
    if ((abs(mda.x) + abs(mda.y)) > 0.000001) startLerpValue = 0.0;
    half lerpcontribution = (any(rectboxmin > HistoryColor) || any(HistoryColor > rectboxmax)) ? startLerpValue : 1.0f;

    HistoryColor = lerp(clampedcolor, HistoryColor, clamp(lerpcontribution, 0.0, 1.0));
    half basemin = min(basealpha, 0.1f);
    basealpha = lerp(basemin, basealpha, clamp(lerpcontribution, 0.0, 1.0));

    ////blend color
    half alphasum = max(EPSILON, basealpha + Upsampledcw.w);
    half alpha = clamp(Upsampledcw.w / alphasum + reset, 0.0, 1.0);

    Upsampledcw.xyz = lerp(HistoryColor, Upsampledcw.xyz, alpha);

    Output = half4(Upsampledcw.xyz, 0.0);
}
