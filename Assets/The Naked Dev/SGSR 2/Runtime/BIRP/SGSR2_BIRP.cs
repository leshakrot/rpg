#if UNITY_BIRP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Experimental.Rendering;
#endif
using static TND.SGSR2.SGSR2_UTILS;

namespace TND.SGSR2
{
    /// <summary>
    /// SGSR2 implementation for the Built-in Render Pipeline
    /// </summary>
    public class SGSR2_BIRP : SGSR2_Base
    {
        public override SGSR2_Variant ActiveVariant => m_context != null && m_context.Initialized ? m_context.Variant : variant;

        // Commandbuffers
        private CommandBuffer m_colorGrabPass;
        private CommandBuffer m_sgsr2ComputePass;
        private CommandBuffer m_opaqueOnlyGrabPass;
        private CommandBuffer m_blitToCamPass;

        // Rendertextures
        private RenderTexture m_opaqueOnlyColorBuffer;
        private RenderTexture m_colorBuffer;
        private RenderTexture m_sgsr2Output;

        // Commandbuffer events
        private const CameraEvent m_OPAQUE_ONLY_EVENT = CameraEvent.BeforeForwardAlpha;
        private const CameraEvent m_COLOR_EVENT = CameraEvent.BeforeImageEffects;
        private const CameraEvent m_UPSCALE_EVENT = CameraEvent.BeforeImageEffects;
        private const CameraEvent m_BlIT_EVENT = CameraEvent.AfterImageEffects;

        private Matrix4x4 m_jitterMatrix;
        private Matrix4x4 m_projectionMatrix;
        private Vector2 m_jitterOffset;

        private SGSR2_Context m_context;
        private SGSR2_Assets m_assets;

        private Material m_fragmentMaterial;

        protected override void InitializeSGSR2()
        {
            base.InitializeSGSR2();

            if (m_assets == null)
            {
                m_assets = Resources.Load<SGSR2_Assets>("SGSR2/SGSR2_BIRP");
            }

            if (variant == SGSR2_Variant.TwoPassFragment)
            {
                m_fragmentMaterial = new Material(m_assets.shaders.twoPassFragment);
            }

            m_mainCamera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;

            m_colorGrabPass = new CommandBuffer { name = "SGSR2: Color Grab Pass" };
            m_opaqueOnlyGrabPass = new CommandBuffer { name = "SGSR2: Opaque Only Grab Pass" };
            m_sgsr2ComputePass = new CommandBuffer { name = "SGSR2: Compute Pass" };
            m_blitToCamPass = new CommandBuffer { name = "SGSR2: Blit to Camera" };

            SendMessage("RemovePPV2CommandBuffers", SendMessageOptions.DontRequireReceiver);
            SetupResolution();

            if (!m_initialized)
            {
                Camera.onPreRender += OnPreRenderCamera;
                Camera.onPostRender += OnPostRenderCamera;
            }
        }

        /// <summary>
        /// Sets up the buffers, initializes the SGSR2 context, and sets up the command buffer
        /// Must be recalled whenever the display resolution changes
        /// </summary>
        private void SetupCommandBuffer()
        {
            ClearCommandBufferCoroutine();

            if (m_colorBuffer)
            {
                if (m_opaqueOnlyColorBuffer)
                {
                    m_opaqueOnlyColorBuffer.Release();
                }

                m_colorBuffer.Release();
                m_sgsr2Output.Release();
            }

            m_renderWidth = (int)(m_displayWidth / m_scaleFactor);
            m_renderHeight = (int)(m_displayHeight / m_scaleFactor);

            m_colorBuffer = new RenderTexture(m_renderWidth, m_renderHeight, 0, RenderTextureFormat.Default);
            m_colorBuffer.Create();
            m_sgsr2Output = new RenderTexture(m_displayWidth, m_displayHeight, 0,
                m_mainCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            m_sgsr2Output.enableRandomWrite = variant != SGSR2_Variant.TwoPassFragment;
            m_sgsr2Output.Create();

            if (m_sgsr2ComputePass != null)
            {
                m_mainCamera.RemoveCommandBuffer(m_COLOR_EVENT, m_colorGrabPass);
                m_mainCamera.RemoveCommandBuffer(m_UPSCALE_EVENT, m_sgsr2ComputePass);
                m_mainCamera.RemoveCommandBuffer(m_BlIT_EVENT, m_blitToCamPass);

                if (m_opaqueOnlyGrabPass != null)
                {
                    m_mainCamera.RemoveCommandBuffer(m_OPAQUE_ONLY_EVENT, m_opaqueOnlyGrabPass);
                }
            }

            m_colorGrabPass.Clear();
            m_sgsr2ComputePass.Clear();
            m_blitToCamPass.Clear();
            m_opaqueOnlyGrabPass.Clear();

            m_colorGrabPass.Blit(BuiltinRenderTextureType.CameraTarget, m_colorBuffer);

            if (variant == SGSR2_Variant.ThreePassCompute)
            {
                m_opaqueOnlyColorBuffer = new RenderTexture(m_colorBuffer);
                m_opaqueOnlyColorBuffer.Create();

                m_opaqueOnlyGrabPass.Blit(BuiltinRenderTextureType.CameraTarget, m_opaqueOnlyColorBuffer);
            }

            m_blitToCamPass.Blit(m_sgsr2Output, BuiltinRenderTextureType.None);

            SendMessage("OverridePPV2TargetTexture", m_colorBuffer, SendMessageOptions.DontRequireReceiver);
            buildCommandBuffers = StartCoroutine(BuildCommandBuffer());
        }

        /// <summary>
        /// Built-in has no way to properly order command buffers, so we have to add them in the order we want ourselves.
        /// </summary>
        private Coroutine buildCommandBuffers;

        private IEnumerator BuildCommandBuffer()
        {
            SendMessage("RemovePPV2CommandBuffers", SendMessageOptions.DontRequireReceiver);

            yield return null;

            if (variant == SGSR2_Variant.ThreePassCompute)
            {
                if (m_opaqueOnlyGrabPass != null)
                {
                    m_mainCamera.AddCommandBuffer(m_OPAQUE_ONLY_EVENT, m_opaqueOnlyGrabPass);
                }
            }

            yield return null;

            SendMessage("AddPPV2CommandBuffer", SendMessageOptions.DontRequireReceiver);

            yield return null;

            if (m_sgsr2ComputePass != null)
            {
                m_mainCamera.AddCommandBuffer(m_COLOR_EVENT, m_colorGrabPass);
                m_mainCamera.AddCommandBuffer(m_UPSCALE_EVENT, m_sgsr2ComputePass);
                m_mainCamera.AddCommandBuffer(m_BlIT_EVENT, m_blitToCamPass);
            }

            buildCommandBuffers = null;
        }

        private void ClearCommandBufferCoroutine()
        {
            if (buildCommandBuffers != null)
            {
                StopCoroutine(buildCommandBuffers);
            }
        }

        private void OnPreRenderCamera(Camera camera)
        {
            if (camera != m_mainCamera)
            {
                return;
            }

            JitterTAA();

            m_mainCamera.targetTexture = m_colorBuffer;

            //Check if display resolution has changed
            if (m_displayWidth != Display.main.renderingWidth || m_displayHeight != Display.main.renderingHeight)
            {
                SetupResolution();
            }

            UpdateDispatch();
        }

        private void OnPostRenderCamera(Camera camera)
        {
            if (camera != m_mainCamera)
            {
                return;
            }

            m_mainCamera.targetTexture = null;
            m_mainCamera.ResetProjectionMatrix();
        }

        /// <summary>
        ///  TAA Jitter
        /// </summary>
        private void JitterTAA()
        {
            int jitterPhaseCount = GetJitterPhaseCount(m_renderWidth, (int)(m_renderWidth * m_scaleFactor));

            GetJitterOffset(out float jitterX, out float jitterY, Time.frameCount, jitterPhaseCount);
            m_jitterOffset = new Vector2(jitterX, jitterY);

            jitterX = 2.0f * jitterX / (float)m_renderWidth;
            jitterY = 2.0f * jitterY / (float)m_renderHeight;

            jitterX += UnityEngine.Random.Range(-0.001f * antiGhosting, 0.001f * antiGhosting);
            jitterY += UnityEngine.Random.Range(-0.001f * antiGhosting, 0.001f * antiGhosting);

            m_jitterMatrix = Matrix4x4.Translate(new Vector2(jitterX, jitterY));
            m_projectionMatrix = m_mainCamera.projectionMatrix;
            m_mainCamera.nonJitteredProjectionMatrix = m_projectionMatrix;
            m_mainCamera.projectionMatrix = m_jitterMatrix * m_projectionMatrix;
            m_mainCamera.useJitteredProjectionMatrixForTransparentRendering = true;
        }

        /// <summary>
        /// Creates new buffers, sends them to the plugin, and reinitializes SGSR2 to adjust the display size
        /// </summary>
        private void SetupResolution()
        {
            m_displayWidth = Display.main.renderingWidth;
            m_displayHeight = Display.main.renderingHeight;
            m_renderWidth = (int)(m_displayWidth / m_scaleFactor);
            m_renderHeight = (int)(m_displayHeight / m_scaleFactor);

            if (m_context != null)
            {
                m_context.Destroy();
                m_context = null;
            }

            Vector2Int maxRenderSize = new Vector2Int(m_renderWidth, m_renderHeight);
            Vector2Int displaySize = new Vector2Int(m_displayWidth, m_displayHeight);
            RenderTextureFormat format = m_mainCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            m_context = new SGSR2_Context();
            switch (variant)
            {
                case SGSR2_Variant.TwoPassFragment:
                    m_context.InitTwoPassFragment(maxRenderSize, displaySize, format);
                    break;
                case SGSR2_Variant.TwoPassCompute:
                    m_context.InitTwoPassCompute(maxRenderSize, displaySize, format);
                    break;
                case SGSR2_Variant.ThreePassCompute:
                    m_context.InitThreePassCompute(maxRenderSize, displaySize, format);
                    break;
            }

            SetupCommandBuffer();
        }

        private void UpdateDispatch()
        {
            if (m_sgsr2ComputePass == null || m_context == null)
            {
                return;
            }

            var cmd = m_sgsr2ComputePass;
            cmd.Clear();

            Vector2Int renderSize = new Vector2Int(m_renderWidth, m_renderHeight);
            m_context.UpdateFrameData(cmd, m_mainCamera, renderSize, m_jitterOffset, 1.0f, m_resetCamera);
            m_resetCamera = false;

            var parameters = new DispatchParameters
            {
                inputColor = m_colorBuffer,
                inputDepth = m_mainCamera.actualRenderingPath is RenderingPath.Forward or RenderingPath.VertexLit ? BuiltinRenderTextureType.Depth : BuiltinRenderTextureType.ResolvedDepth,
                inputMotionVectors = BuiltinRenderTextureType.MotionVectors,
                inputOpaqueOnly = m_opaqueOnlyColorBuffer,
                outputColor = m_sgsr2Output,
                renderSize = renderSize,
            };

            switch (ActiveVariant)
            {
                case SGSR2_Variant.TwoPassFragment:
                    RenderTwoPassFragmentBlit(cmd, m_context, parameters, m_fragmentMaterial);
                    break;
                case SGSR2_Variant.TwoPassCompute:
                    RenderTwoPassCompute(cmd, m_context, m_assets.shaders.twoPassCompute, parameters);
                    break;
                case SGSR2_Variant.ThreePassCompute:
                    RenderThreePassCompute(cmd, m_context, m_assets.shaders.threePassCompute, parameters);
                    break;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void DisableSGSR2()
        {
            Camera.onPreRender -= OnPreRenderCamera;
            Camera.onPostRender -= OnPostRenderCamera;

            ClearCommandBufferCoroutine();
            SendMessage("ResetPPV2CommandBuffer", SendMessageOptions.DontRequireReceiver);
            SendMessage("ResetPPV2TargetTexture", SendMessageOptions.DontRequireReceiver);

            OnResetAllMipMaps();

            if (m_mainCamera != null)
            {
                m_mainCamera.targetTexture = null;
                m_mainCamera.ResetProjectionMatrix();

                if (m_opaqueOnlyGrabPass != null)
                {
                    m_mainCamera.RemoveCommandBuffer(m_OPAQUE_ONLY_EVENT, m_opaqueOnlyGrabPass);
                }

                if (m_sgsr2ComputePass != null)
                {
                    m_mainCamera.RemoveCommandBuffer(m_COLOR_EVENT, m_colorGrabPass);
                    m_mainCamera.RemoveCommandBuffer(m_UPSCALE_EVENT, m_sgsr2ComputePass);
                    m_mainCamera.RemoveCommandBuffer(m_BlIT_EVENT, m_blitToCamPass);
                }
            }

            m_sgsr2ComputePass = m_colorGrabPass = m_opaqueOnlyGrabPass = m_blitToCamPass = null;

            if (m_colorBuffer)
            {
                if (m_opaqueOnlyColorBuffer)
                {
                    m_opaqueOnlyColorBuffer.Release();
                }

                m_colorBuffer.Release();
                m_sgsr2Output.Release();
            }

            if (m_fragmentMaterial != null)
            {
                Destroy(m_fragmentMaterial);
                m_fragmentMaterial = null;
            }

            if (m_context != null)
            {
                m_context.Destroy();
                m_context = null;
            }
        }
    }
}
#endif
