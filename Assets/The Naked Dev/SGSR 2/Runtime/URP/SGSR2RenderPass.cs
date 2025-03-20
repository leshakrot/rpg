using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.UIElements;

#if UNITY_2023_3_OR_NEWER
using System;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace TND.SGSR2
{
    public class SGSR2RenderPass : ScriptableRenderPass
    {
        private SGSR2_URP m_upscaler;
        private const string blitPass = "[SGSR 2] Upscaler";

        //Legacy
        private Vector4 _scaleBias;

        public SGSR2RenderPass(SGSR2_URP upscaler, bool usingRenderGraph)
        {
            m_upscaler = upscaler;
            renderPassEvent = usingRenderGraph ? RenderPassEvent.AfterRenderingPostProcessing : RenderPassEvent.AfterRendering + 2;

            _scaleBias = SystemInfo.graphicsUVStartsAtTop ? new Vector4(1, -1, 0, 1) : Vector4.one;
        }

        #region Unity 6

#if UNITY_2023_3_OR_NEWER
        private class PassData
        {
            public TextureHandle Source;
            public TextureHandle Depth;
            public TextureHandle MotionVector;
            public TextureHandle Destination;
            public Rect PixelRect;
        }

        private int multipassId = 0;
        private const string _upscaledTextureName = "_SGSR2_UpscaledTexture";

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(blitPass, out var passData))
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                RenderTextureDescriptor upscaledDesc = cameraData.cameraTargetDescriptor;
                upscaledDesc.depthBufferBits = 0;
                upscaledDesc.width = m_upscaler.m_displayWidth;
                upscaledDesc.height = m_upscaler.m_displayHeight;

                TextureHandle upscaled = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    upscaledDesc,
                    _upscaledTextureName,
                    false
                );

                passData.Source = resourceData.activeColorTexture;
                passData.Depth = resourceData.activeDepthTexture;
                passData.MotionVector = resourceData.motionVectorColor;
                passData.Destination = upscaled;
                passData.PixelRect = cameraData.camera.pixelRect;

                builder.UseTexture(passData.Source, AccessFlags.Read);
                builder.UseTexture(passData.Depth, AccessFlags.Read);
                builder.UseTexture(passData.MotionVector, AccessFlags.Read);
                builder.UseTexture(passData.Destination, AccessFlags.Write);

                builder.AllowPassCulling(false);

                resourceData.cameraColor = upscaled;
                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            //Stereo
            if (XRSettings.enabled)
            {
                multipassId++;
                if (multipassId >= 2)
                {
                    multipassId = 0;
                }
            }

            SGSR2_URP.DispatchParameters parameters = new()
            {
                inputColor = data.Source,
                inputDepth = data.Depth,
                inputMotionVectors = data.MotionVector,
            };

            m_upscaler.Execute(unsafeCmd, parameters, multipassId);

            unsafeCmd.SetRenderTarget(data.Destination);
            unsafeCmd.SetViewport(data.PixelRect);

            Blitter.BlitTexture(unsafeCmd, m_upscaler.m_upscalerOutput, new Vector4(1, 1, 0, 0), 0, false);
        }

#endif
        #endregion

        #region Unity Legacy
#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            try
            {
                CommandBuffer cmd = CommandBufferPool.Get(blitPass);

                CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
                cmd.SetViewport(renderingData.cameraData.camera.pixelRect);
                if (renderingData.cameraData.camera.targetTexture != null)
                {
                    _scaleBias = Vector2.one;
                }
                Blitter.BlitTexture(cmd, m_upscaler.m_upscalerOutput, _scaleBias, 0, false);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            catch { }
        }
    }

    #endregion

    public class SGSR2BufferPass : ScriptableRenderPass
    {
        private SGSR2_URP m_upscaler;

        private int multipassId = 0;
        private const string blitPass = "[SGSR 2] Upscaler";

        private readonly int depthTexturePropertyID = Shader.PropertyToID("_CameraDepthTexture");
        private readonly int motionTexturePropertyID = Shader.PropertyToID("_MotionVectorTexture");

        public SGSR2BufferPass(SGSR2_URP upscaler)
        {
            m_upscaler = upscaler;

            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(blitPass);

            //Stereo
            if (XRSettings.enabled)
            {
                multipassId++;
                if (multipassId >= 2)
                {
                    multipassId = 0;
                }
            }

            SGSR2_URP.DispatchParameters parameters = new()
            {
#if UNITY_2022_1_OR_NEWER
                inputColor = renderingData.cameraData.renderer.cameraColorTargetHandle,
                inputDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle,
#else
                inputColor = renderingData.cameraData.renderer.cameraColorTarget,
                inputDepth = Shader.GetGlobalTexture(depthTexturePropertyID),
#endif
                inputMotionVectors = Shader.GetGlobalTexture(motionTexturePropertyID),
            };

            m_upscaler.Execute(cmd, parameters, multipassId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    // Only used for the 3-pass CS variant
    public class SGSR2OpaqueOnlyPass : ScriptableRenderPass
    {
        private SGSR2_URP m_upscaler;

        public SGSR2OpaqueOnlyPass(SGSR2_URP upscaler)
        {
            m_upscaler = upscaler;

            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        #region Unity 6
#if UNITY_2023_3_OR_NEWER

        private class PassData
        {
            public TextureHandle Source;
            public Rect PixelRect;
        }
        private const string blitPass = "[SGSR 2] Opaque Pass";

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(blitPass, out var passData))
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                passData.Source = resourceData.activeColorTexture;
                passData.PixelRect = cameraData.camera.pixelRect;

                builder.UseTexture(passData.Source);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            unsafeCmd.SetRenderTarget(m_upscaler.m_opaqueOnlyColorBuffer);
            Blitter.BlitTexture(unsafeCmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
        }
#endif

        #endregion

        #region Unity Legacy

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

#if UNITY_2022_1_OR_NEWER
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, m_upscaler.m_opaqueOnlyColorBuffer);
#else
            Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, m_upscaler.m_opaqueOnlyColorBuffer);
#endif

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        #endregion
    }
}
