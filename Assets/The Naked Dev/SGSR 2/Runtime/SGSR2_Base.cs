using System;
using UnityEngine;
using UnityEngine.Rendering;
using static TND.SGSR2.SGSR2_UTILS;

namespace TND.SGSR2
{
    public enum SGSR2_Variant
    {
        [InspectorName("2-Pass Fragment (Faster)")] TwoPassFragment,
        [InspectorName("2-Pass Compute (Fast)")] TwoPassCompute,
        [InspectorName("3-Pass Compute (Normal)")] ThreePassCompute,
    }

    public enum SGSR2_Quality
    {
        Off = 0,
        NativeAA = 1,
        UltraQuality = 2,
        Quality = 3,
        Balanced = 4,
        Performance = 5,
        UltraPerformance = 6,
    }

    /// <summary>
    /// Base script for SGSR 2
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public abstract class SGSR2_Base : MonoBehaviour
    {
        public SGSR2_Variant variant = SGSR2_Variant.TwoPassFragment;
        public SGSR2_Quality quality = SGSR2_Quality.Quality;

        [Range(0, 1)] public float antiGhosting = 0.0f;

        public float mipmapBiasOverride = 1.0f;
        public bool autoTextureUpdate = true;
        public float mipMapUpdateFrequency = 2f;

        protected bool m_initialized = false;
        protected Camera m_mainCamera;
        public Camera MainCamera => m_mainCamera;

        public abstract SGSR2_Variant ActiveVariant { get; }

        protected float m_scaleFactor = 1.5f;
        protected int m_renderWidth, m_renderHeight;
        public int m_displayWidth, m_displayHeight;

        protected SGSR2_Variant m_previousVariant;
        protected SGSR2_Quality m_previousQuality;

        protected bool m_resetCamera = false;

        //Mipmap variables
        protected Texture[] m_allTextures;
        protected ulong m_previousLength;
        protected float m_mipMapBias;
        protected float m_prevMipMapBias;
        protected float m_mipMapTimer = float.MaxValue;

        private readonly RenderTargetIdentifier[] m_mrt = new RenderTargetIdentifier[2];

        #region Public API
        /// <summary>
        /// Set SGSR 2 Quality settings.
        /// Quality = 1.5, Balanced = 1.7, Performance = 2, Ultra Performance = 3
        /// </summary>
        public void OnSetQuality(SGSR2_Quality newQuality)
        {
            OnSetQuality(variant, newQuality);
        }

        /// <summary>
        /// Set SGSR 2 Quality settings.
        /// Quality = 1.5, Balanced = 1.7, Performance = 2, Ultra Performance = 3
        /// </summary>
        public void OnSetQuality(SGSR2_Variant newVariant, SGSR2_Quality newQuality)
        {
            m_previousVariant = newVariant;
            m_previousQuality = newQuality;
            variant = newVariant;
            quality = newQuality;

            if (quality == SGSR2_Quality.Off)
            {
                Initialize();
                Disable();
                m_scaleFactor = 1;
            }
            else
            {
                switch (quality)
                {
                    case SGSR2_Quality.NativeAA:
                        m_scaleFactor = 1.0f;
                        break;
                    case SGSR2_Quality.UltraQuality:
                        m_scaleFactor = 1.2f;
                        break;
                    case SGSR2_Quality.Quality:
                        m_scaleFactor = 1.5f;
                        break;
                    case SGSR2_Quality.Balanced:
                        m_scaleFactor = 1.7f;
                        break;
                    case SGSR2_Quality.Performance:
                        m_scaleFactor = 2.0f;
                        break;
                    case SGSR2_Quality.UltraPerformance:
                        m_scaleFactor = 3.0f;
                        break;
                }

                Initialize();
            }
        }

        public void OnSetAdaptiveQuality(float value)
        {
            m_scaleFactor = value;
        }

        /// <summary>
        /// Checks whether the currently selected SGSR 2 variant is compatible using the current build settings
        /// </summary>
        public bool OnIsSupported() => OnIsSupported(variant);

        /// <summary>
        /// Checks whether the given SGSR 2 variant is compatible using the current build settings
        /// </summary>
        public bool OnIsSupported(SGSR2_Variant desiredVariant)
        {
            switch (desiredVariant)
            {
                case SGSR2_Variant.TwoPassFragment:
                    return SystemInfo.graphicsShaderLevel >= 35;
                default:
                    return SystemInfo.graphicsShaderLevel >= 45 && SystemInfo.supportsComputeShaders;
            }
        }

        /// <summary>
        /// Resets the camera for the next frame, clearing all the buffers saved from previous frames in order to prevent artifacts.
        /// Should be called in or before PreRender on the frame where the camera makes a jump cut.
        /// Is automatically disabled the frame after.
        /// </summary>
        public void OnResetCamera()
        {
            m_resetCamera = true;
        }

        /// <summary>
        /// Updates a single texture to the set MipMap Bias.
        /// Should be called when an object is instantiated, or when the ScaleFactor is changed.
        /// </summary>
        public void OnMipmapSingleTexture(Texture texture)
        {
            texture.mipMapBias = m_mipMapBias;
        }

        /// <summary>
        /// Updates all textures currently loaded to the set MipMap Bias.
        /// Should be called when a lot of new textures are loaded, or when the ScaleFactor is changed.
        /// </summary>
        public void OnMipMapAllTextures()
        {
            m_allTextures = Resources.FindObjectsOfTypeAll<Texture>();
            for (int i = 0; i < m_allTextures.Length; i++)
            {
                m_allTextures[i].mipMapBias = m_mipMapBias;
            }
        }

        /// <summary>
        /// Resets all currently loaded textures to the default mipmap bias.
        /// </summary>
        public void OnResetAllMipMaps()
        {
            m_prevMipMapBias = -1;

            m_allTextures = Resources.FindObjectsOfTypeAll<Texture>();
            for (int i = 0; i < m_allTextures.Length; i++)
            {
                m_allTextures[i].mipMapBias = 0;
            }

            m_allTextures = null;
        }
        #endregion

        protected virtual void OnEnable()
        {
            OnSetQuality(variant, quality);
        }

        protected virtual void Update()
        {
            if (m_previousVariant != variant || m_previousQuality != quality)
            {
                OnDisable();
                OnEnable();
            }

#if UNITY_BIRP
            if(m_initialized && autoTextureUpdate)
            {
                UpdateMipMaps();
            }
#endif
        }

        protected virtual void OnDisable()
        {
            Disable();
        }

        private void Initialize()
        {
            // Reset mipmap timer so mipmap are instantly updated if automatic mip map is turned on
            m_mipMapTimer = float.MaxValue;

            if (m_initialized || !Application.isPlaying)
            {
                return;
            }

            if (OnIsSupported())
            {
                InitializeSGSR2();
                m_initialized = true;
            }
            else
            {
                Debug.LogWarning("SGSR2 is not supported");
                enabled = false;
            }
        }

        private void Disable()
        {
            m_initialized = false;
            DisableSGSR2();
        }

        protected virtual void InitializeSGSR2()
        {
            m_mainCamera = GetComponent<Camera>();
        }

        protected abstract void DisableSGSR2();

#if UNITY_BIRP
        /// <summary>
        /// Automatically updates the mipmap of all loaded textures
        /// </summary>
        protected void UpdateMipMaps()
        {
            m_mipMapTimer += Time.deltaTime;

            if (m_mipMapTimer < mipMapUpdateFrequency)
            {
                return;
            }

            m_mipMapTimer = 0;
            m_mipMapBias = (Mathf.Log((float)m_renderWidth / m_displayWidth, 2f) - 1) * mipmapBiasOverride;

            if (m_previousLength != Texture.currentTextureMemory || !Mathf.Approximately(m_prevMipMapBias, m_mipMapBias))
            {
                m_prevMipMapBias = m_mipMapBias;
                m_previousLength = Texture.currentTextureMemory;

                OnMipMapAllTextures();
            }
        }
#endif

        public struct DispatchParameters
        {
            public RenderTargetIdentifier inputColor;
            public RenderTargetIdentifier inputDepth;
            public RenderTargetIdentifier inputMotionVectors;
            public RenderTargetIdentifier inputOpaqueOnly;

            public RenderTargetIdentifier outputColor;

            public Vector2Int renderSize;
        }

        protected void RenderTwoPassFragmentBlit(CommandBuffer cmd, SGSR2_Context context, in DispatchParameters parameters, Material material)
        {
            var constantBuffer = context.ConstantBuffer;
            cmd.SetGlobalTexture(idInputColor, parameters.inputColor);
            cmd.SetGlobalTexture(idMotionDepthClipAlphaBuffer, context.MotionDepthClipAlpha);
            cmd.SetGlobalTexture(idPrevHistoryOutput, context.PrevUpscaleHistory);
            cmd.SetGlobalTexture(idCameraDepthTexture, parameters.inputDepth);
            cmd.SetGlobalTexture(idCameraMotionVectorsTexture, parameters.inputMotionVectors);
            cmd.SetGlobalConstantBuffer(constantBuffer, idParams, 0, constantBuffer.stride);

            // Convert pass
            cmd.Blit(BuiltinRenderTextureType.None, context.MotionDepthClipAlpha, material, 0);

            // Upscale pass
            cmd.Blit(BuiltinRenderTextureType.None, parameters.outputColor, material, 1);
            cmd.CopyTexture(parameters.outputColor, context.NextUpscaleHistory);
        }

        protected void RenderTwoPassFragmentProcedural(CommandBuffer cmd, SGSR2_Context context, in DispatchParameters parameters, Material material, MaterialPropertyBlock properties)
        {
            var constantBuffer = context.ConstantBuffer;
            cmd.SetGlobalTexture(idInputColor, parameters.inputColor);
            cmd.SetGlobalTexture(idMotionDepthClipAlphaBuffer, context.MotionDepthClipAlpha);
            cmd.SetGlobalTexture(idPrevHistoryOutput, context.PrevUpscaleHistory);
            cmd.SetGlobalTexture(idCameraDepthTexture, parameters.inputDepth);
            cmd.SetGlobalTexture(idCameraMotionVectorsTexture, parameters.inputMotionVectors);
            cmd.SetGlobalConstantBuffer(constantBuffer, idParams, 0, constantBuffer.stride);

            // Convert pass
            cmd.SetRenderTarget(context.MotionDepthClipAlpha);
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, properties);

            // Upscale pass
            m_mrt[0] = parameters.outputColor;
            m_mrt[1] = context.NextUpscaleHistory;
            cmd.SetRenderTarget(m_mrt, BuiltinRenderTextureType.None);
            cmd.DrawProcedural(Matrix4x4.identity, material, 1, MeshTopology.Triangles, 3, 1, properties);
        }

        protected void RenderTwoPassCompute(CommandBuffer cmd, SGSR2_Context context, ComputeShader shader, in DispatchParameters parameters)
        {
            var constantBuffer = context.ConstantBuffer;

            const int threadGroupWorkRegionDim = 8;
            int dispatchSrcX = (parameters.renderSize.x + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchSrcY = (parameters.renderSize.y + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchDstX = (m_displayWidth + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchDstY = (m_displayHeight + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;

            // Convert pass
            {
                int kernelIndex = shader.FindKernel("CS_Convert");

                cmd.SetComputeConstantBufferParam(shader, idParams, constantBuffer, 0, constantBuffer.stride);
                cmd.SetComputeTextureParam(shader, kernelIndex, idInputColor, parameters.inputColor);
                cmd.SetComputeTextureParam(shader, kernelIndex, idInputDepth, parameters.inputDepth);
                cmd.SetComputeTextureParam(shader, kernelIndex, idInputVelocity, parameters.inputMotionVectors);
                cmd.SetComputeTextureParam(shader, kernelIndex, idMotionDepthClipAlphaBuffer, context.MotionDepthClipAlpha);
                cmd.SetComputeTextureParam(shader, kernelIndex, idYCoCgColor, context.ColorLuma);

                cmd.DispatchCompute(shader, kernelIndex, dispatchSrcX, dispatchSrcY, 1);
            }

            // Upscale pass
            {
                int kernelIndex = shader.FindKernel("CS_Upscale");

                cmd.SetComputeConstantBufferParam(shader, idParams, constantBuffer, 0, constantBuffer.stride);
                cmd.SetComputeTextureParam(shader, kernelIndex, idPrevHistoryOutput, context.PrevUpscaleHistory);
                cmd.SetComputeTextureParam(shader, kernelIndex, idMotionDepthClipAlphaBuffer, context.MotionDepthClipAlpha);
                cmd.SetComputeTextureParam(shader, kernelIndex, idYCoCgColor, context.ColorLuma);
                cmd.SetComputeTextureParam(shader, kernelIndex, idSceneColorOutput, parameters.outputColor);
                cmd.SetComputeTextureParam(shader, kernelIndex, idHistoryOutput, context.NextUpscaleHistory);

                cmd.DispatchCompute(shader, kernelIndex, dispatchDstX, dispatchDstY, 1);
            }
        }

        protected void RenderThreePassCompute(CommandBuffer cmd, SGSR2_Context context, ComputeShader shader, in DispatchParameters parameters)
        {
            var constantBuffer = context.ConstantBuffer;

            const int threadGroupWorkRegionDim = 8;
            int dispatchSrcX = (parameters.renderSize.x + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchSrcY = (parameters.renderSize.y + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchDstX = (m_displayWidth + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchDstY = (m_displayHeight + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;

            // Convert pass
            {
                int kernelIndex = shader.FindKernel("CS_Convert");

                cmd.SetComputeConstantBufferParam(shader, idParams, constantBuffer, 0, constantBuffer.stride);
                cmd.SetComputeTextureParam(shader, kernelIndex, idInputOpaqueColor, parameters.inputOpaqueOnly);
                cmd.SetComputeTextureParam(shader, kernelIndex, idInputColor, parameters.inputColor);
                cmd.SetComputeTextureParam(shader, kernelIndex, idInputDepth, parameters.inputDepth);
                cmd.SetComputeTextureParam(shader, kernelIndex, idInputVelocity, parameters.inputMotionVectors);
                cmd.SetComputeTextureParam(shader, kernelIndex, idMotionDepthAlphaBuffer, context.MotionDepthAlpha);
                cmd.SetComputeTextureParam(shader, kernelIndex, idYCoCgColor, context.ColorLuma);

                cmd.DispatchCompute(shader, kernelIndex, dispatchSrcX, dispatchSrcY, 1);
            }

            // Activate pass
            {
                int kernelIndex = shader.FindKernel("CS_Activate");

                cmd.SetComputeConstantBufferParam(shader, idParams, constantBuffer, 0, constantBuffer.stride);
                cmd.SetComputeTextureParam(shader, kernelIndex, idPrevLumaHistory, context.PrevLumaHistory);
                cmd.SetComputeTextureParam(shader, kernelIndex, idMotionDepthAlphaBuffer, context.MotionDepthAlpha);
                cmd.SetComputeTextureParam(shader, kernelIndex, idYCoCgColor, context.ColorLuma);
                cmd.SetComputeTextureParam(shader, kernelIndex, idMotionDepthClipAlphaBuffer, context.MotionDepthClipAlpha);
                cmd.SetComputeTextureParam(shader, kernelIndex, idLumaHistory, context.NextLumaHistory);

                cmd.DispatchCompute(shader, kernelIndex, dispatchSrcX, dispatchSrcY, 1);
            }

            // Upscale pass
            {
                int kernelIndex = shader.FindKernel("CS_Upscale");

                cmd.SetComputeConstantBufferParam(shader, idParams, constantBuffer, 0, constantBuffer.stride);
                cmd.SetComputeTextureParam(shader, kernelIndex, idPrevHistoryOutput, context.PrevUpscaleHistory);
                cmd.SetComputeTextureParam(shader, kernelIndex, idMotionDepthClipAlphaBuffer, context.MotionDepthClipAlpha);
                cmd.SetComputeTextureParam(shader, kernelIndex, idYCoCgColor, context.ColorLuma);
                cmd.SetComputeTextureParam(shader, kernelIndex, idSceneColorOutput, parameters.outputColor);
                cmd.SetComputeTextureParam(shader, kernelIndex, idHistoryOutput, context.NextUpscaleHistory);

                cmd.DispatchCompute(shader, kernelIndex, dispatchDstX, dispatchDstY, 1);
            }
        }
    }
}
