#include "../sgsr2_common.hlsl"

//============================================================================================================
//
//
//                  Copyright (c) 2024, Qualcomm Innovation Center, Inc. All rights reserved.
//                              SPDX-License-Identifier: BSD-3-Clause
//
//============================================================================================================

TEXTURE2D_X(PrevHistoryOutput)              : register(t0);
TEXTURE2D_X(MotionDepthClipAlphaBuffer)     : register(t1);
TEXTURE2D_X_UINT(YCoCgColor)                : register(t2);
RW_TEXTURE2D_X(float4, SceneColorOutput)    : register(u0);
RW_TEXTURE2D_X(float4, HistoryOutput)       : register(u1);

[numthreads(8, 8, 1)]
void CS_Upscale(uint3 gl_GlobalInvocationID : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(gl_GlobalInvocationID.z);
    
    float Biasmax_viewportXScale = min(float(displaySize.x) / float(renderSize.x), 1.99);  //Biasmax_viewportXScale
    float scalefactor = min(20.0, pow((float(displaySize.x) / float(renderSize.x)) * (float(displaySize.y) / float(renderSize.y)), 3.0));
    float f2 = preExposure;            //1.0;   //preExposure
    float2 HistoryInfoViewportSizeInverse = displaySizeRcp;
    float2 HistoryInfoViewportSize = float2(displaySize);
    float2 InputJitter = jitterOffset;
    float2 InputInfoViewportSize = float2(renderSize);
    float2 Hruv = (float2(gl_GlobalInvocationID.xy) + 0.5f) * HistoryInfoViewportSizeInverse;
    float2 Jitteruv;
    Jitteruv.x = clamp(Hruv.x + (InputJitter.x * renderSizeRcp.x), 0.0, 1.0);
    Jitteruv.y = clamp(Hruv.y + (InputJitter.y * renderSizeRcp.y), 0.0, 1.0);

    int2 InputPos = int2(Jitteruv * InputInfoViewportSize);
    float4 mda = SAMPLE_TEXTURE2D_X_LOD(MotionDepthClipAlphaBuffer, S_LINEAR_CLAMP, Jitteruv, 0).xyzw;
    float2 Motion = mda.xy;

    ///ScreenPosToViewportScale&Bias
    float2 PrevUV;
    PrevUV.x = clamp(-0.5 * Motion.x + Hruv.x, 0.0, 1.0);
#ifdef REQUEST_NDC_Y_UP
    PrevUV.y = clamp(0.5 * Motion.y + Hruv.y, 0.0, 1.0);
#else
    PrevUV.y = clamp(-0.5 * Motion.y + Hruv.y, 0.0, 1.0);
#endif

    float depthfactor = mda.z;
    float ColorMax = mda.w;

    float4 History = SAMPLE_TEXTURE2D_X_LOD(PrevHistoryOutput, S_LINEAR_CLAMP, PrevUV, 0);
    float3 HistoryColor = History.xyz;
    float Historyw = History.w;
    float Wfactor = clamp(abs(Historyw), 0.0, 1.0);

    /////upsample and compute box
    float4 Upsampledcw = 0.0f;
    float kernelfactor = clamp(Wfactor + float(reset), 0.0, 1.0);
    float biasmax = Biasmax_viewportXScale - Biasmax_viewportXScale * kernelfactor;
    float biasmin = max(1.0f, 0.3 + 0.3 * biasmax);
    float biasfactor = max(0.25f * depthfactor, kernelfactor);
    float kernelbias = lerp(biasmax, biasmin, biasfactor);
    float motion_viewport_len = length(Motion * HistoryInfoViewportSize);
    float curvebias = lerp(-2.0, -3.0, clamp(motion_viewport_len * 0.02, 0.0, 1.0));

    float3 rectboxcenter = 0.0f;
    float3 rectboxvar = 0.0f;
    float rectboxweight = 0.0f;
    float2 srcpos = float2(InputPos) + 0.5f - InputJitter;
    float2 srcOutputPos = Hruv * InputInfoViewportSize;

    kernelbias *= 0.5f;
    float kernelbias2 = kernelbias * kernelbias;
    float2 srcpos_srcOutputPos = srcpos - srcOutputPos;

    int2 InputPosBtmRight = 1 + InputPos;
    float2 gatherCoord = float2(InputPos) * renderSizeRcp;
    uint btmRight = LOAD_TEXTURE2D_X(YCoCgColor, InputPosBtmRight).x;
    uint4 topleft = GATHER_RED_TEXTURE2D_X(YCoCgColor, S_POINT_CLAMP, gatherCoord);
    uint2 topRight = 0;
    uint2 bottomLeft = 0;

    uint sameCameraFrmNum = bSameCamera;

    if (sameCameraFrmNum!=0u)
    {
        topRight = GATHER_RED_TEXTURE2D_X(YCoCgColor, S_POINT_CLAMP, gatherCoord + float2(renderSizeRcp.x, 0.0)).yz;
        bottomLeft = GATHER_RED_TEXTURE2D_X(YCoCgColor, S_POINT_CLAMP, gatherCoord + float2(0.0, renderSizeRcp.y)).xy;
    }
    else
    {
        uint2 btmRight = GATHER_RED_TEXTURE2D_X(YCoCgColor, S_POINT_CLAMP, gatherCoord + float2(renderSizeRcp.x, renderSizeRcp.y)).xz;
        bottomLeft.y = btmRight.x;
        topRight.x = btmRight.y;
    }

    float3 rectboxmin;
    float3 rectboxmax;
    {
        float3 samplecolor = DecodeColor(bottomLeft.y);
        float2 baseoffset = srcpos_srcOutputPos + float2(0.0, 1.0);
        float baseoffset_dot = dot(baseoffset, baseoffset);
        float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        float weight = FastLanczos(base);
        Upsampledcw += float4(samplecolor * weight, weight);
        float boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = samplecolor;
        rectboxmax = samplecolor;
        float3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    {
        float3 samplecolor = DecodeColor(topRight.x);
        float2 baseoffset = srcpos_srcOutputPos + float2(1.0, 0.0);
        float baseoffset_dot = dot(baseoffset, baseoffset);
        float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        float weight = FastLanczos(base);
        Upsampledcw += float4(samplecolor * weight, weight);
        float boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        float3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    {
        float3 samplecolor = DecodeColor(topleft.x);
        float2 baseoffset = srcpos_srcOutputPos + float2(-1.0, 0.0);
        float baseoffset_dot = dot(baseoffset, baseoffset);
        float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        float weight = FastLanczos(base);
        Upsampledcw += float4(samplecolor * weight, weight);
        float boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        float3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    {
        float3 samplecolor = DecodeColor(topleft.y);
        float2 baseoffset = srcpos_srcOutputPos;
        float baseoffset_dot = dot(baseoffset, baseoffset);
        float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        float weight = FastLanczos(base);
        Upsampledcw += float4(samplecolor * weight, weight);
        float boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        float3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }
    {
        float3 samplecolor = DecodeColor(topleft.z);
        float2 baseoffset = srcpos_srcOutputPos + float2(0.0, -1.0);
        float baseoffset_dot = dot(baseoffset, baseoffset);
        float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
        float weight = FastLanczos(base);
        Upsampledcw += float4(samplecolor * weight, weight);
        float boxweight = exp(baseoffset_dot * curvebias);
        rectboxmin = min(rectboxmin, samplecolor);
        rectboxmax = max(rectboxmax, samplecolor);
        float3 wsample = samplecolor * boxweight;
        rectboxcenter += wsample;
        rectboxvar += (samplecolor * wsample);
        rectboxweight += boxweight;
    }

    if (sameCameraFrmNum!=0u)
    {
        {
            float3 samplecolor = DecodeColor(btmRight);
            float2 baseoffset = srcpos_srcOutputPos + float2(1.0, 1.0);
            float baseoffset_dot = dot(baseoffset, baseoffset);
            float base = clamp(baseoffset_dot * kernelbias2, 0.0, 1.0);
            float weight = FastLanczos(base);
            Upsampledcw += float4(samplecolor * weight, weight);
            float boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            float3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }
        {
            float3 samplecolor = DecodeColor(bottomLeft.x);
            float2 baseoffset = srcpos_srcOutputPos + float2(-1.0, 1.0);
            float baseoffset_dot = dot(baseoffset, baseoffset);
            float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
            float weight = FastLanczos(base);
            Upsampledcw += float4(samplecolor * weight, weight);
            float boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            float3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }
        {
            float3 samplecolor = DecodeColor(topRight.y);
            float2 baseoffset = srcpos_srcOutputPos + float2(1.0, -1.0);
            float baseoffset_dot = dot(baseoffset, baseoffset);
            float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
            float weight = FastLanczos(base);
            Upsampledcw += float4(samplecolor * weight, weight);
            float boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            float3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }

        {
            float3 samplecolor = DecodeColor(topleft.w);
            float2 baseoffset = srcpos_srcOutputPos + float2(-1.0, -1.0);
            float baseoffset_dot = dot(baseoffset, baseoffset);
            float base = clamp(baseoffset_dot * kernelbias2, 0.0f, 1.0f);
            float weight = FastLanczos(base);
            Upsampledcw += float4(samplecolor * weight, weight);
            float boxweight = exp(baseoffset_dot * curvebias);
            rectboxmin = min(rectboxmin, samplecolor);
            rectboxmax = max(rectboxmax, samplecolor);
            float3 wsample = samplecolor * boxweight;
            rectboxcenter += wsample;
            rectboxvar += (samplecolor * wsample);
            rectboxweight += boxweight;
        }
    }

    rectboxweight = 1.0 / rectboxweight;
    rectboxcenter *= rectboxweight;
    rectboxvar *= rectboxweight;
    rectboxvar = sqrt(abs(rectboxvar - rectboxcenter * rectboxcenter));

    Upsampledcw.xyz =  clamp(Upsampledcw.xyz / Upsampledcw.w, rectboxmin-0.05f, rectboxmax+0.05f);
    Upsampledcw.w = Upsampledcw.w * (1.0f / 3.0f) ;

    float OneMinusWfactor = 1.0f - Wfactor;

    float baseupdate = OneMinusWfactor - OneMinusWfactor * depthfactor;
    baseupdate = min(baseupdate, lerp(baseupdate, Upsampledcw.w *10.0f, clamp(10.0f* motion_viewport_len, 0.0, 1.0)));
    baseupdate = min(baseupdate, lerp(baseupdate, Upsampledcw.w, clamp(motion_viewport_len *0.05f, 0.0, 1.0)));
    float basealpha = baseupdate;

    float boxscale = max(depthfactor, clamp(motion_viewport_len * 0.05f, 0.0, 1.0));
    float boxsize = lerp(scalefactor, 1.0f, boxscale);
    float3 sboxvar = rectboxvar * boxsize;
    float3 boxmin = rectboxcenter - sboxvar;
    float3 boxmax = rectboxcenter + sboxvar;
    rectboxmax = min(rectboxmax, boxmax);
    rectboxmin = max(rectboxmin, boxmin);

    float3 clampedcolor = clamp(HistoryColor, rectboxmin, rectboxmax);
    float startLerpValue = minLerpContribution; //MinLerpContribution; //MinLerpContribution;
    if ((abs(mda.x) + abs(mda.y)) > 0.000001) startLerpValue = 0.0;
    float lerpcontribution = (any(rectboxmin > HistoryColor) || any(HistoryColor > rectboxmax)) ? startLerpValue : 1.0f;

    HistoryColor = lerp(clampedcolor, HistoryColor, clamp(lerpcontribution, 0.0, 1.0));
    float basemin = min(basealpha, 0.1f);
    basealpha = lerp(basemin, basealpha, clamp(lerpcontribution, 0.0, 1.0));

    ////blend color
    float alphasum = max(EPSILON, basealpha + Upsampledcw.w);
    float alpha = clamp(Upsampledcw.w / alphasum + float(reset), 0.0, 1.0);
    Upsampledcw.xyz = lerp(HistoryColor, Upsampledcw.xyz, alpha);
    
    HistoryOutput[COORD_TEXTURE2D_X(gl_GlobalInvocationID.xy)] = float4(Upsampledcw.xyz, Wfactor);

    ////ycocg to rgb
    float x_z = Upsampledcw.x - Upsampledcw.z;
    Upsampledcw.xyz = float3(
        clamp(x_z + Upsampledcw.y, 0.0, 1.0),
        clamp(Upsampledcw.x + Upsampledcw.z, 0.0, 1.0),
        clamp(x_z - Upsampledcw.y, 0.0, 1.0));

    float compMax = max(Upsampledcw.x, Upsampledcw.y);
    compMax = clamp(max(compMax, Upsampledcw.z), 0.0f, 1.0f);
    float scale = preExposure /  ((1.0f + 600.0f / 65504.0f) - compMax);

    if (ColorMax > 4000.0f) scale = ColorMax;
    Upsampledcw.xyz = Upsampledcw.xyz * scale;
    SceneColorOutput[COORD_TEXTURE2D_X(gl_GlobalInvocationID.xy)] = Upsampledcw;
}
