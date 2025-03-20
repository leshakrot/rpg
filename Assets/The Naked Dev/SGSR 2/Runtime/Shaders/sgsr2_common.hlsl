#ifndef SGSR2_COMMON_HLSL
#define SGSR2_COMMON_HLSL

#define EPSILON 1.192e-07f

#if UNITY_UV_STARTS_AT_TOP
#define REQUEST_NDC_Y_UP
#endif

#if UNITY_REVERSED_Z
#define DEPTH_NEAREST(a, b)     max((a), (b))
#define DEPTH_CLIP(depth)       ((depth) > 1.0e-05f)
#else
#define DEPTH_NEAREST(a, b)     min((a), (b))
#define DEPTH_CLIP(depth)       ((depth) < 1.0f - 1.0e-05f)
#endif

// Fallback for shader targets that don't support texture gather
#if SHADER_TARGET < 45
#undef GATHER_RED_TEXTURE2D_X
#define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2) \
    float4( LOAD_TEXTURE2D_X(textureName, int2(saturate((coord2) + renderSizeRcp * float2(0, 1)) * (renderSize - 1))).r, \
            LOAD_TEXTURE2D_X(textureName, int2(saturate((coord2) + renderSizeRcp * float2(1, 1)) * (renderSize - 1))).r, \
            LOAD_TEXTURE2D_X(textureName, int2(saturate((coord2) + renderSizeRcp * float2(1, 0)) * (renderSize - 1))).r, \
            LOAD_TEXTURE2D_X(textureName, int2(saturate((coord2) + renderSizeRcp * float2(0, 0)) * (renderSize - 1))).r )
#endif

cbuffer cbSGSR2 : register(b0)
{
    uint2   renderSize;
    uint2   displaySize;
    float2  renderSizeRcp;
    float2  displaySizeRcp;
    float2  jitterOffset;
    float2  jitterCancellation;
    float4  clipToPrevClip[4];
    float   preExposure;
    float   cameraFovAngleHor;
    float   cameraNear;
    float   minLerpContribution;
    float2  scaleRatio;
    uint    bSameCamera;
    uint    reset;
};

float FastLanczos(float base)
{
    float y = base - 1.0f;
    float y2 = y * y;
    float y_temp = 0.75f * y + y2;
    return y_temp * y2;
}

float3 DecodeColor(uint sample32)
{
    uint x11 = sample32 >> 21u;
    uint y11 = sample32 & (2047u << 10u);
    uint z10 = sample32 & 1023u;
    float3 samplecolor;
    samplecolor.x = (float(x11) * (1.0 / 2047.5));
    samplecolor.y = (float(y11) * (4.76953602e-7)) - 0.5;
    samplecolor.z = (float(z10) * (1.0 / 1023.5)) - 0.5;

    return samplecolor;
}

float DecodeColorY(uint sample32)
{
    uint x11 = sample32 >> 21u;
    return float(x11) * (1.0 / 2047.5);
}

uint packHalf2x16(float2 value)
{
    return f32tof16(value.x) | (f32tof16(value.y) << 16);
}

float2 unpackHalf2x16(uint x)
{
    return f16tof32(uint2(x & 0xFFFF, x >> 16));
}
#endif  // SGSR2_COMMON_HLSL
