using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace TND.SGSR2
{
    public class SGSR2_Context
    {
        private SGSR2_Variant _variant;
        private Vector2Int _upscaleSize;
        private Vector2 _prevJitterOffset;
        private uint _frameCount = 0;

        private RenderTexture _colorLuma;
        private RenderTexture _motionDepthAlpha;
        private RenderTexture _motionDepthClipAlpha;
        private readonly RenderTexture[] _lumaHistory = new RenderTexture[2];
        private readonly RenderTexture[] _upscaleHistory = new RenderTexture[2];

        private ComputeBuffer _computeBuffer;
        private readonly SGSR2_UTILS.Params[] _paramsArray = { new SGSR2_UTILS.Params() };
        private ref SGSR2_UTILS.Params Params => ref _paramsArray[0];

        public bool Initialized { get; private set; }
        public SGSR2_Variant Variant => _variant;

        public RenderTexture ColorLuma => _colorLuma;
        public RenderTexture MotionDepthAlpha => _motionDepthAlpha;
        public RenderTexture MotionDepthClipAlpha => _motionDepthClipAlpha;
        public RenderTexture PrevLumaHistory => _lumaHistory[(_frameCount & 1) ^ 1];
        public RenderTexture NextLumaHistory => _lumaHistory[_frameCount & 1];
        public RenderTexture PrevUpscaleHistory => _upscaleHistory[(_frameCount & 1) ^ 1];
        public RenderTexture NextUpscaleHistory => _upscaleHistory[_frameCount & 1];
        public ComputeBuffer ConstantBuffer => _computeBuffer;

        public void InitTwoPassFragment(in Vector2Int maxRenderSize, in Vector2Int upscaleSize, GraphicsFormat outputFormat)
        {
            InitTwoPassFragment(maxRenderSize, upscaleSize);
            CreateHistoryTextures(upscaleSize, outputFormat);
            Initialized = true;
        }

        public void InitTwoPassCompute(in Vector2Int maxRenderSize, in Vector2Int upscaleSize, GraphicsFormat outputFormat)
        {
            InitTwoPassCompute(maxRenderSize, upscaleSize);
            CreateHistoryTextures(upscaleSize, outputFormat, true);
            Initialized = true;
        }

        public void InitThreePassCompute(in Vector2Int maxRenderSize, in Vector2Int upscaleSize, GraphicsFormat outputFormat)
        {
            InitThreePassCompute(maxRenderSize, upscaleSize);
            CreateHistoryTextures(upscaleSize, outputFormat, true);
            Initialized = true;
        }

        public void InitTwoPassFragment(in Vector2Int maxRenderSize, in Vector2Int upscaleSize, RenderTextureFormat outputFormat)
        {
            InitTwoPassFragment(maxRenderSize, upscaleSize);
            CreateHistoryTextures(upscaleSize, outputFormat);
            Initialized = true;
        }

        public void InitTwoPassCompute(in Vector2Int maxRenderSize, in Vector2Int upscaleSize, RenderTextureFormat outputFormat)
        {
            InitTwoPassCompute(maxRenderSize, upscaleSize);
            CreateHistoryTextures(upscaleSize, outputFormat, true);
            Initialized = true;
        }

        public void InitThreePassCompute(in Vector2Int maxRenderSize, in Vector2Int upscaleSize, RenderTextureFormat outputFormat)
        {
            InitThreePassCompute(maxRenderSize, upscaleSize);
            CreateHistoryTextures(upscaleSize, outputFormat, true);
            Initialized = true;
        }

        private void InitTwoPassFragment(in Vector2Int maxRenderSize, in Vector2Int upscaleSize)
        {
            _variant = SGSR2_Variant.TwoPassFragment;
            _upscaleSize = upscaleSize;
            _frameCount = 0;

            CreateConstantBuffer();
            CreateRenderTexture(ref _motionDepthClipAlpha, "MotionDepthClipAlpha", maxRenderSize, GraphicsFormat.R16G16B16A16_SFloat);
        }

        private void InitTwoPassCompute(in Vector2Int maxRenderSize, in Vector2Int upscaleSize)
        {
            _variant = SGSR2_Variant.TwoPassCompute;
            _upscaleSize = upscaleSize;
            _frameCount = 0;

            CreateConstantBuffer();
            CreateRenderTexture(ref _colorLuma, "ColorLuma", maxRenderSize, GraphicsFormat.R32_UInt, true);
            CreateRenderTexture(ref _motionDepthClipAlpha, "MotionDepthClipAlpha", maxRenderSize, GraphicsFormat.R16G16B16A16_SFloat, true);
        }

        private void InitThreePassCompute(in Vector2Int maxRenderSize, in Vector2Int upscaleSize)
        {
            _variant = SGSR2_Variant.ThreePassCompute;
            _upscaleSize = upscaleSize;
            _frameCount = 0;

            CreateConstantBuffer();
            CreateRenderTexture(ref _colorLuma, "ColorLuma", maxRenderSize, GraphicsFormat.R32_UInt, true);
            CreateRenderTexture(ref _motionDepthAlpha, "MotionDepthAlpha", maxRenderSize, GraphicsFormat.R16G16B16A16_SFloat, true);
            CreateRenderTexture(ref _motionDepthClipAlpha, "MotionDepthClipAlpha", maxRenderSize, GraphicsFormat.R16G16B16A16_SFloat, true);
            CreateRenderTextureArray(_lumaHistory, "LumaHistory", maxRenderSize, GraphicsFormat.R32_UInt, true);
        }

        public void Destroy()
        {
            Initialized = false;

            DestroyRenderTextureArray(_upscaleHistory);
            DestroyRenderTextureArray(_lumaHistory);
            DestroyRenderTexture(ref _motionDepthClipAlpha);
            DestroyRenderTexture(ref _motionDepthAlpha);
            DestroyRenderTexture(ref _colorLuma);

            DestroyConstantBuffer();
        }

        public void UpdateFrameData(CommandBuffer cmd, Camera cam, in Vector2Int renderSize, in Vector2 jitterOffset, float preExposure, bool reset, bool jitterCancellation = false)
        {
            Matrix4x4 clipToPrevClip = Matrix4x4.identity;
            bool isCameraStill = false;
            if (_frameCount > 0 && !reset)
            {
                // We need to use the projection matrix as it is used on the GPU to match what Unity keeps in Camera.previousViewProjectionMatrix
                Matrix4x4 viewProj = GL.GetGPUProjectionMatrix(cam.nonJitteredProjectionMatrix, true) * cam.worldToCameraMatrix;
                clipToPrevClip = cam.previousViewProjectionMatrix * viewProj.inverse;
                isCameraStill = IsCameraStill(viewProj, cam.previousViewProjectionMatrix);
            }

            ref var p = ref Params;
            p.renderSize = renderSize;
            p.displaySize = _upscaleSize;
            p.renderSizeRcp = new Vector2(1.0f / p.renderSize.x, 1.0f / p.renderSize.y);
            p.displaySizeRcp = new Vector2(1.0f / p.displaySize.x, 1.0f / p.displaySize.y);
            p.jitterOffset = jitterOffset;
            p.jitterCancellation = jitterCancellation ? (_prevJitterOffset - jitterOffset) * p.renderSizeRcp : Vector2.zero;
            p.clipToPrevClip = clipToPrevClip;
            p.preExposure = preExposure;
            p.cameraFovAngleHor = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * p.renderSize.x * p.renderSizeRcp.y;
            p.cameraNear = cam.nearClipPlane;
            p.minLerpContribution = 0f;
            p.scaleRatio = new Vector2(p.displaySize.x * p.renderSizeRcp.x, p.displaySize.y * p.renderSizeRcp.y);
            p.bSameCamera = isCameraStill ? 1u : 0u;
            p.reset = reset ? 1u : 0u;

            cmd.SetBufferData(_computeBuffer, _paramsArray);

            if (_frameCount == 0 || reset)
            {
                if (_variant == SGSR2_Variant.ThreePassCompute)
                {
                    cmd.SetRenderTarget(_lumaHistory[0]);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                    cmd.SetRenderTarget(_lumaHistory[1]);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                }

                cmd.SetRenderTarget(_upscaleHistory[0]);
                cmd.ClearRenderTarget(false, true, Color.clear);
                cmd.SetRenderTarget(_upscaleHistory[1]);
                cmd.ClearRenderTarget(false, true, Color.clear);
            }

            _prevJitterOffset = jitterOffset;
            _frameCount++;
        }

        private void CreateHistoryTextures(in Vector2Int upscaleSize, GraphicsFormat format, bool enableRandomWrite = false)
        {
            CreateRenderTextureArray(_upscaleHistory, "History", upscaleSize, format, enableRandomWrite);
        }

        private void CreateHistoryTextures(in Vector2Int upscaleSize, RenderTextureFormat format, bool enableRandomWrite = false)
        {
            CreateRenderTextureArray(_upscaleHistory, "History", upscaleSize, format, enableRandomWrite);
        }

        private void CreateConstantBuffer()
        {
            if (_computeBuffer == null)
            {
                _computeBuffer = new ComputeBuffer(1, Marshal.SizeOf<SGSR2_UTILS.Params>(), ComputeBufferType.Constant);
            }
        }

        private void DestroyConstantBuffer()
        {
            if (_computeBuffer != null)
            {
                _computeBuffer.Release();
                _computeBuffer = null;
            }
        }

        private static bool CreateRenderTexture(ref RenderTexture rt, string name, in Vector2Int size, GraphicsFormat format, bool enableRandomWrite = false)
        {
            rt = new RenderTexture(size.x, size.y, 0, format) { name = name, enableRandomWrite = enableRandomWrite };
            return rt.Create();
        }

        private static void CreateRenderTextureArray(RenderTexture[] rts, string name, in Vector2Int size, GraphicsFormat format, bool enableRandomWrite = false)
        {
            for (int i = 0; i < rts.Length; ++i)
            {
                CreateRenderTexture(ref rts[i], $"{name}_{i + 1}", size, format, enableRandomWrite);
            }
        }

        private static bool CreateRenderTexture(ref RenderTexture rt, string name, in Vector2Int size, RenderTextureFormat format, bool enableRandomWrite = false)
        {
            rt = new RenderTexture(size.x, size.y, 0, format) { name = name, enableRandomWrite = enableRandomWrite };
            return rt.Create();
        }

        private static void CreateRenderTextureArray(RenderTexture[] rts, string name, in Vector2Int size, RenderTextureFormat format, bool enableRandomWrite = false)
        {
            for (int i = 0; i < rts.Length; ++i)
            {
                CreateRenderTexture(ref rts[i], $"{name}_{i + 1}", size, format, enableRandomWrite);
            }
        }

        private static void DestroyRenderTexture(ref RenderTexture rt)
        {
            if (rt == null)
                return;

            rt.Release();
            rt = null;
        }

        private static void DestroyRenderTextureArray(RenderTexture[] rts)
        {
            for (int i = 0; i < rts.Length; ++i)
            {
                DestroyRenderTexture(ref rts[i]);
            }
        }

        private static bool IsCameraStill(in Matrix4x4 currViewProj, in Matrix4x4 prevViewProj, float threshold = 1e-5f)
        {
            float vpDiff = 0f;
            for (int i = 0; i < 16; i++)
            {
                vpDiff += Mathf.Abs(currViewProj[i] - prevViewProj[i]);
            }

            return vpDiff < threshold;
        }
    }
}
