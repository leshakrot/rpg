using System;
using UnityEngine;

namespace TND.SGSR2
{
    public static class SGSR2_UTILS
    {
        public static readonly int idInputOpaqueColor = Shader.PropertyToID("InputOpaqueColor");
        public static readonly int idInputColor = Shader.PropertyToID("InputColor");
        public static readonly int idInputDepth = Shader.PropertyToID("InputDepth");
        public static readonly int idInputVelocity = Shader.PropertyToID("InputVelocity");
        public static readonly int idCameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int idCameraMotionVectorsTexture = Shader.PropertyToID("_CameraMotionVectorsTexture");
        public static readonly int idMotionDepthAlphaBuffer = Shader.PropertyToID("MotionDepthAlphaBuffer");
        public static readonly int idMotionDepthClipAlphaBuffer = Shader.PropertyToID("MotionDepthClipAlphaBuffer");
        public static readonly int idYCoCgColor = Shader.PropertyToID("YCoCgColor");
        public static readonly int idLumaHistory = Shader.PropertyToID("LumaHistory");
        public static readonly int idPrevLumaHistory = Shader.PropertyToID("PrevLumaHistory");
        public static readonly int idHistoryOutput = Shader.PropertyToID("HistoryOutput");
        public static readonly int idPrevHistoryOutput = Shader.PropertyToID("PrevHistoryOutput");
        public static readonly int idSceneColorOutput = Shader.PropertyToID("SceneColorOutput");
        public static readonly int idParams = Shader.PropertyToID("cbSGSR2");

        [Serializable]
        public struct Params
        {
            public Vector2Int renderSize;
            public Vector2Int displaySize;

            public Vector2 renderSizeRcp;
            public Vector2 displaySizeRcp;

            public Vector2 jitterOffset;
            public Vector2 jitterCancellation;

            public Matrix4x4 clipToPrevClip;

            public float preExposure;
            public float cameraFovAngleHor;
            public float cameraNear;
            public float minLerpContribution;

            public Vector2 scaleRatio;
            public uint bSameCamera;
            public uint reset;
        }

        [Serializable]
        public class Shaders
        {
            public Shader twoPassFragment;
            public ComputeShader twoPassCompute;
            public ComputeShader threePassCompute;
        }

        public static int GetJitterPhaseCount(int renderWidth, int displayWidth)
        {
            const float basePhaseCount = 8.0f;
            int jitterPhaseCount = (int)(basePhaseCount * Mathf.Pow((float)displayWidth / renderWidth, 2.0f));
            return jitterPhaseCount;
        }

        public static void GetJitterOffset(out float outX, out float outY, int index, int phaseCount)
        {
            outX = Halton((index % phaseCount) + 1, 2) - 0.5f;
            outY = Halton((index % phaseCount) + 1, 3) - 0.5f;
        }

        private static float Halton(int index, int @base)
        {
            float f = 1.0f, result = 0.0f;

            for (int currentIndex = index; currentIndex > 0;)
            {

                f /= @base;
                result += f * (currentIndex % @base);
                currentIndex = (int)Mathf.Floor((float)currentIndex / @base);
            }

            return result;
        }
    }
}
