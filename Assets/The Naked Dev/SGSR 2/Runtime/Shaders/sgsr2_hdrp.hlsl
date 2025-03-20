#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

// Using renderSizeRcp here is a bit of a hack, but the SRPs don't offer any macros for gather with offset, and we know which texture the GatherBlue will be used for
#define GATHER_BLUE_TEXTURE2D_X_OFFSET(textureName, samplerName, coord2, offset)    GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2 + offset * renderSizeRcp)

#define S_POINT_CLAMP       s_point_clamp_sampler
#define S_LINEAR_CLAMP      s_linear_clamp_sampler

inline float2 decodeVelocityFromTexture(float2 ev)
{
#if UNITY_UV_STARTS_AT_TOP
    return float2(ev.x, -ev.y) * 2.0f;
#else
    return ev * 2.0f;
#endif
}
