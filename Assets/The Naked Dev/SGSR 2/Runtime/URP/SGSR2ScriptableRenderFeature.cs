using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TND.SGSR2
{
    public class SGSR2ScriptableRenderFeature : ScriptableRendererFeature
    {
        [HideInInspector]
        public bool IsEnabled = false;
        private bool usingRenderGraph = false;

        private SGSR2_URP m_upscaler;

        private SGSR2BufferPass _bufferPass;
        private SGSR2RenderPass _renderPass;
        private SGSR2OpaqueOnlyPass _opaqueBufferPass;

        private CameraData cameraData;

        public override void Create()
        {
            name = "SGSR2RenderFeature";
            SetupPasses();
        }

        public void OnSetReference(SGSR2_URP upscaler)
        {
            m_upscaler = upscaler;
            SetupPasses();
        }

        private void SetupPasses()
        {
#if UNITY_2023_3_OR_NEWER
            var renderGraphSettings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            usingRenderGraph = !renderGraphSettings.enableRenderCompatibilityMode;
#endif

            if (!usingRenderGraph)
            {
                _bufferPass = new SGSR2BufferPass(m_upscaler);
                _bufferPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);
            }

            _renderPass = new SGSR2RenderPass(m_upscaler, usingRenderGraph);
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);

            _opaqueBufferPass = new SGSR2OpaqueOnlyPass(m_upscaler);
        }

        public void OnDispose()
        {
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (!IsEnabled)
            {
                return;
            }

            cameraData = renderingData.cameraData;
            if (cameraData.camera != m_upscaler.MainCamera)
            {
                return;
            }
            if (!cameraData.resolveFinalTarget)
            {
                return;
            }

            // Here you can queue up multiple passes after each other.
            if (!usingRenderGraph)
            {
                renderer.EnqueuePass(_bufferPass);
            }

            renderer.EnqueuePass(_renderPass);
            
            if (m_upscaler.ActiveVariant == SGSR2_Variant.ThreePassCompute)
            {
                renderer.EnqueuePass(_opaqueBufferPass);
            }
        }
    }
}
