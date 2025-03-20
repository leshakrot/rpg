#include "UnityCG.cginc"

#define TEXTURE2D_X(textureName)                Texture2D textureName
#define TEXTURE2D_X_HALF(textureName)           Texture2D<half4> textureName
#define TEXTURE2D_X_UINT(textureName)           Texture2D<uint> textureName
#define RW_TEXTURE2D_X(type, textureName)       RWTexture2D<type> textureName

#define COORD_TEXTURE2D_X(pixelCoord)           pixelCoord

#define LOAD_TEXTURE2D_X(textureName, unCoord2)                                         textureName[unCoord2]
#define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)                   textureName.SampleLevel(samplerName, coord2, lod)
#define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)                        textureName.GatherRed(samplerName, coord2)
#define GATHER_BLUE_TEXTURE2D_X_OFFSET(textureName, samplerName, coord2, offset)        textureName.GatherBlue(samplerName, coord2, offset)

#define SAMPLER(samplerName)    SamplerState samplerName

SamplerState s_PointClamp   : register(s0);
SamplerState s_LinearClamp  : register(s1);

#define S_POINT_CLAMP       s_PointClamp
#define S_LINEAR_CLAMP      s_LinearClamp

#define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex)

inline float2 decodeVelocityFromTexture(float2 ev)
{
#if UNITY_UV_STARTS_AT_TOP
    return float2(ev.x, -ev.y) * 2.0f;
#else
    return ev * 2.0f;
#endif
}
