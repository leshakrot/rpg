#define SHADER_AVAILABLE_RANDOMWRITE 1
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

// Using renderSizeRcp here is a bit of a hack, but the SRPs don't offer any macros for gather with offset, and we know which texture the GatherBlue will be used for
#define GATHER_BLUE_TEXTURE2D_X_OFFSET(textureName, samplerName, coord2, offset)    GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2 + offset * renderSizeRcp)

#ifndef TEXTURE2D_X
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define TEXTURE2D_X(textureName)                                         Texture2DArray<type> textureName
#else
    #define TEXTURE2D_X(textureName)                                         Texture2D<type> textureName
#endif
#endif

#ifndef RW_TEXTURE2D_X
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define RW_TEXTURE2D_X(type, textureName)                                RWTexture2DArray<type> textureName
#else
    #define RW_TEXTURE2D_X(type, textureName)                                RWTexture2D<type> textureName
#endif
#endif

#ifndef TEXTURE2D_X_UINT
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define TEXTURE2D_X_UINT(textureName)                                    Texture2DArray<uint> textureName
#else
    #define TEXTURE2D_X_UINT(textureName)                                    Texture2D<uint> textureName
#endif
#endif

#ifndef COORD_TEXTURE2D_X
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define COORD_TEXTURE2D_X(pixelCoord)                                    uint3(pixelCoord, SLICE_ARRAY_INDEX)
#else
    #define COORD_TEXTURE2D_X(pixelCoord)                                    pixelCoord
#endif
#endif

#define S_POINT_CLAMP       sampler_PointClamp
#define S_LINEAR_CLAMP      sampler_LinearClamp

#define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex)

inline float2 decodeVelocityFromTexture(float2 ev)
{
#if UNITY_UV_STARTS_AT_TOP
    return float2(ev.x, -ev.y) * 2.0f;
#else
    return ev * 2.0f;
#endif
}
