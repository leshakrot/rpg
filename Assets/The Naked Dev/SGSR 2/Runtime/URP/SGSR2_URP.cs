using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.XR;
using static TND.SGSR2.SGSR2_UTILS;

namespace TND.SGSR2
{
    [RequireComponent(typeof(Camera))]
    public class SGSR2_URP : SGSR2_Base
    {
        public override SGSR2_Variant ActiveVariant => m_contexts[0] != null && m_contexts[0].Initialized ? m_contexts[0].Variant : variant;

        //Rendertextures
        public RTHandle m_opaqueOnlyColorBuffer;
        public RTHandle m_upscalerOutput;

        private List<SGSR2ScriptableRenderFeature> sgsr2ScriptableRenderFeature;
        private bool containsRenderFeature = false;
        private bool m_usePhysicalProperties;

        //UniversalRenderPipelineAsset
        private UniversalRenderPipelineAsset UniversalRenderPipelineAsset;
        private UniversalAdditionalCameraData m_cameraData;

        private GraphicsFormat m_graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
        public GraphicsFormat m_prevGraphicsFormat;

        private readonly SGSR2_Context[] m_contexts = new SGSR2_Context[2];
        private SGSR2_Assets m_assets;
        private Material m_fragmentMaterial;
        private MaterialPropertyBlock m_fragmentPropertyBlock;

        public bool m_cameraStacking = false;
        public Camera m_topCamera;
        private int m_prevCameraStackCount;
        private bool m_isBaseCamera;
        private List<SGSR2_URP> m_prevCameraStack = new List<SGSR2_URP>();
        private SGSR2_Quality m_prevStackQuality = (SGSR2_Quality)(-1);

        private int prevDisplayWidth, prevDisplayHeight;
        private float m_previousScaleFactor;
        private RenderingPath m_previousRenderingPath;

        private Vector2 _jitterOffset;

        protected override void InitializeSGSR2()
        {
            base.InitializeSGSR2();

            m_mainCamera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            if (m_assets == null)
            {
                m_assets = Resources.Load<SGSR2_Assets>("SGSR2/SGSR2_URP");
                m_fragmentMaterial = new Material(m_assets.shaders.twoPassFragment);
                if (m_fragmentPropertyBlock == null)
                {
                    m_fragmentPropertyBlock = new MaterialPropertyBlock();
                    m_fragmentPropertyBlock.SetVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                }
            }

            SetupResolution();

            if (!m_initialized)
            {
                RenderPipelineManager.beginCameraRendering += PreRenderCamera;
                RenderPipelineManager.endCameraRendering += PostRenderCamera;
            }

            if (m_cameraData == null)
            {
                m_cameraData = m_mainCamera.GetUniversalAdditionalCameraData();
                if (m_cameraData != null)
                {
                    if (m_cameraData.renderType == CameraRenderType.Base)
                    {
                        m_isBaseCamera = true;
                        SetupCameraStacking();
                    }
                }
            }
        }


        /// <summary>
        /// Sets up the buffers, initializes the SGSR2 context, and sets up the command buffer
        /// Must be recalled whenever the display resolution changes
        /// </summary>
        private void SetupCommandBuffer()
        {
            if (m_upscalerOutput != null)
            {
                m_upscalerOutput.Release();
                m_upscalerOutput = null;

                if (m_opaqueOnlyColorBuffer != null)
                {
                    m_opaqueOnlyColorBuffer.Release();
                    m_opaqueOnlyColorBuffer = null;
                }
            }

            if (sgsr2ScriptableRenderFeature != null)
            {
                for (int i = 0; i < sgsr2ScriptableRenderFeature.Count; i++)
                {
                    sgsr2ScriptableRenderFeature[i].OnDispose();
                }
            }
            else
            {
                containsRenderFeature = GetRenderFeature();
            }

            SetDynamicResolution(m_scaleFactor);

            m_renderWidth = (int)(m_mainCamera.pixelWidth / m_scaleFactor);
            m_renderHeight = (int)(m_mainCamera.pixelHeight / m_scaleFactor);

            if (m_mainCamera.stereoEnabled)
            {
                m_renderWidth = XRSettings.eyeTextureWidth;
                m_renderHeight = XRSettings.eyeTextureHeight;
                m_displayWidth = (int)(XRSettings.eyeTextureWidth / XRSettings.eyeTextureResolutionScale);
                m_displayHeight = (int)(XRSettings.eyeTextureHeight / XRSettings.eyeTextureResolutionScale);
            }

            m_upscalerOutput = RTHandles.Alloc(m_displayWidth, m_displayHeight, enableRandomWrite: variant != SGSR2_Variant.TwoPassFragment, colorFormat: m_graphicsFormat, msaaSamples: MSAASamples.None, name: "SGSR2 OUTPUT");

            if (variant == SGSR2_Variant.ThreePassCompute)
            {
                m_opaqueOnlyColorBuffer = RTHandles.Alloc(m_renderWidth, m_renderHeight, enableRandomWrite: false, colorFormat: m_graphicsFormat, msaaSamples: MSAASamples.None, name: "OPAQUE ONLY BUFFER");
            }

            if (!containsRenderFeature)
            {
                Debug.LogError("Current Universal Render Data is missing the 'SGSR 2 Scriptable Render Pass URP' Rendering Feature");
                return;
            }

            for (int i = 0; i < sgsr2ScriptableRenderFeature.Count; i++)
            {
                sgsr2ScriptableRenderFeature[i].OnSetReference(this);
            }

            for (int i = 0; i < sgsr2ScriptableRenderFeature.Count; i++)
            {
                sgsr2ScriptableRenderFeature[i].IsEnabled = true;
            }
        }


        private bool GetRenderFeature()
        {
            sgsr2ScriptableRenderFeature = new List<SGSR2ScriptableRenderFeature>();

            UniversalRenderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            bool scriptableRenderFeatureFound = false;
            if (UniversalRenderPipelineAsset == null)
            {
                Debug.LogError("SGSR 2: Can't find UniversalRenderPipelineAsset");
                return false;
            }

            UniversalRenderPipelineAsset.upscalingFilter = UpscalingFilterSelection.Linear;

            var type = UniversalRenderPipelineAsset.GetType();
            var propertyInfo = type.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                return false;
            }

            var scriptableRenderData = (ScriptableRendererData[])propertyInfo.GetValue(UniversalRenderPipelineAsset);
            if (scriptableRenderData == null || scriptableRenderData.Length <= 0)
            {
                return false;
            }

            foreach (var renderData in scriptableRenderData)
            {
                foreach (var renderFeature in renderData.rendererFeatures)
                {
                    var sgsr2Feature = renderFeature as SGSR2ScriptableRenderFeature;
                    if (sgsr2Feature == null)
                    {
                        continue;
                    }

                    sgsr2ScriptableRenderFeature.Add(sgsr2Feature);
                    scriptableRenderFeatureFound = true;

                    //Stop looping the current renderer, we only allow 1 instance per renderer
                    break;
                }
            }

            return scriptableRenderFeatureFound;
        }

        void PreRenderCamera(ScriptableRenderContext context, Camera cameras)
        {
            if (cameras != m_mainCamera)
            {
                return;
            }

            m_displayWidth = m_mainCamera.pixelWidth;
            m_displayHeight = m_mainCamera.pixelHeight;
            if (cameras.stereoEnabled)
            {
                m_displayWidth = (int)(XRSettings.eyeTextureWidth / XRSettings.eyeTextureResolutionScale);
                m_displayHeight = (int)(XRSettings.eyeTextureHeight / XRSettings.eyeTextureResolutionScale);
            }

            JitterCameraMatrix(context);

            if (UniversalRenderPipelineAsset != GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset)
            {
                if (sgsr2ScriptableRenderFeature != null)
                {
                    for (int i = 0; i < sgsr2ScriptableRenderFeature.Count; i++)
                    {
                        sgsr2ScriptableRenderFeature[i].OnDispose();
                    }
                }
                sgsr2ScriptableRenderFeature = null;
                OnSetQuality(variant, quality);
                SetupCommandBuffer();
            }

            //Check if display resolution has changed
            if (m_displayWidth != prevDisplayWidth || m_displayHeight != prevDisplayHeight || m_prevGraphicsFormat != m_graphicsFormat)
            {
                SetupResolution();
            }

            if (!Mathf.Approximately(m_previousScaleFactor, m_scaleFactor) || m_previousRenderingPath != m_mainCamera.actualRenderingPath)
            {
                SetupFrameBuffers();
            }

            //Camera Stacking
            if (m_isBaseCamera)
            {
                if (m_cameraData != null)
                {
                    if (m_cameraStacking)
                    {
                        try
                        {
                            if (m_topCamera != m_cameraData.cameraStack[^1] || m_prevCameraStackCount != m_cameraData.cameraStack.Count || m_prevStackQuality != quality)
                            {
                                SetupCameraStacking();
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        public void Execute(CommandBuffer cmd, DispatchParameters parameters, int multiPassId)
        {
            var context = m_contexts[multiPassId];
            if (context == null || !context.Initialized)
            {
                cmd.Blit(parameters.inputColor, m_upscalerOutput);
                return;
            }

            Vector2Int renderSize = new Vector2Int(m_renderWidth, m_renderHeight);
            context.UpdateFrameData(cmd, m_mainCamera, renderSize, _jitterOffset, 1.0f, m_resetCamera, true);
            m_resetCamera = false;

            parameters.inputOpaqueOnly = m_opaqueOnlyColorBuffer;
            parameters.outputColor = m_upscalerOutput;
            parameters.renderSize = renderSize;

            switch (context.Variant)
            {
                case SGSR2_Variant.TwoPassFragment:
                    RenderTwoPassFragmentProcedural(cmd, context, parameters, m_fragmentMaterial, m_fragmentPropertyBlock);
                    break;
                case SGSR2_Variant.TwoPassCompute:
                    RenderTwoPassCompute(cmd, context, m_assets.shaders.twoPassCompute, parameters);
                    break;
                case SGSR2_Variant.ThreePassCompute:
                    RenderThreePassCompute(cmd, context, m_assets.shaders.threePassCompute, parameters);
                    break;
            }
        }

        void PostRenderCamera(ScriptableRenderContext context, Camera cameras)
        {
            if (cameras != m_mainCamera)
            {
                return;
            }

            m_mainCamera.usePhysicalProperties = m_usePhysicalProperties;
            if (!m_mainCamera.usePhysicalProperties)
                m_mainCamera.ResetProjectionMatrix();
        }

        /// <summary>
        /// SGSR2 TAA Jitter
        /// </summary>
        private void JitterCameraMatrix(ScriptableRenderContext context)
        {
            if (sgsr2ScriptableRenderFeature == null || sgsr2ScriptableRenderFeature.Count == 0 || !sgsr2ScriptableRenderFeature[0].IsEnabled)
            {
                return;
            }

            int jitterPhaseCount = GetJitterPhaseCount(m_renderWidth, (int)(m_renderWidth * m_scaleFactor));
            GetJitterOffset(out float jitterX, out float jitterY, Time.frameCount, jitterPhaseCount);
            _jitterOffset = new Vector2(jitterX, jitterY);

            jitterX = 2.0f * jitterX / m_renderWidth;
            jitterY = 2.0f * jitterY / m_renderHeight;

            jitterX += Random.Range(-0.0001f * antiGhosting, 0.0001f * antiGhosting);
            jitterY += Random.Range(-0.0001f * antiGhosting, 0.0001f * antiGhosting);

            m_usePhysicalProperties = m_mainCamera.usePhysicalProperties;

            if (m_mainCamera.stereoEnabled)
            {
                // We only need to configure all of this once for stereo, during OnPreCull
                ConfigureStereoJitteredProjectionMatrices(context, m_mainCamera, jitterX, jitterY);
            }
            else
            {
                ConfigureJitteredProjectionMatrix(m_mainCamera, jitterX, jitterY);
            }
        }

        /// <summary>
        /// Prepares the jittered and non jittered projection matrices.
        /// </summary>
        /// <param name="context">The current post-processing context.</param>
        public void ConfigureJitteredProjectionMatrix(Camera camera, float jitterX, float jitterY)
        {
            var jitterTranslationMatrix = Matrix4x4.Translate(new Vector3(jitterX, jitterY, 0));
            var projectionMatrix = camera.projectionMatrix;
            camera.nonJitteredProjectionMatrix = projectionMatrix;
            camera.projectionMatrix = jitterTranslationMatrix * camera.nonJitteredProjectionMatrix;
            camera.useJitteredProjectionMatrixForTransparentRendering = true;
        }

        /// <summary>
        /// Prepares the jittered and non jittered projection matrices for stereo rendering.
        /// </summary>
        /// <param name="context">The current post-processing context.</param>
        // TODO: We'll probably need to isolate most of this for SRPs
        public void ConfigureStereoJitteredProjectionMatrices(ScriptableRenderContext context, Camera camera, float jitterX, float jitterY)
        {
            for (var eye = Camera.StereoscopicEye.Left; eye <= Camera.StereoscopicEye.Right; eye++)
            {
                // This saves off the device generated projection matrices as non-jittered
                camera.CopyStereoDeviceProjectionMatrixToNonJittered(eye);
                var originalProj = camera.GetStereoNonJitteredProjectionMatrix(eye);
                // Currently no support for custom jitter func, as VR devices would need to provide
                // original projection matrix as input along with jitter
                var jitteredMatrix = GenerateJitteredProjectionMatrixFromOriginal(camera, originalProj, jitterX, jitterY);
                camera.SetStereoProjectionMatrix(eye, jitteredMatrix);
            }

            // jitter has to be scaled for the actual eye texture size, not just the intermediate texture size
            // which could be double-wide in certain stereo rendering scenarios
            //jitter = new Vector2(jitter.x / context.screenWidth, jitter.y / context.screenHeight);
            camera.useJitteredProjectionMatrixForTransparentRendering = true;
        }

        /// <summary>
        /// Gets a jittered perspective projection matrix from an original projection matrix.
        /// </summary>
        /// <param name="context">The current render context</param>
        /// <param name="origProj">The original projection matrix</param>
        /// <param name="jitter">The jitter offset</param>
        /// <returns>A jittered projection matrix</returns>
        public static Matrix4x4 GenerateJitteredProjectionMatrixFromOriginal(Camera context, Matrix4x4 origProj, float jitterX, float jitterY)
        {
            var planes = origProj.decomposeProjection;

            float vertFov = Mathf.Abs(planes.top) + Mathf.Abs(planes.bottom);
            float horizFov = Mathf.Abs(planes.left) + Mathf.Abs(planes.right);

            var planeJitter = new Vector2(jitterX * horizFov / context.pixelWidth,
                jitterY * vertFov / context.pixelHeight);

            planes.left += planeJitter.x;
            planes.right += planeJitter.x;
            planes.top += planeJitter.y;
            planes.bottom += planeJitter.y;

            var jitteredMatrix = Matrix4x4.Frustum(planes);

            return jitteredMatrix;
        }

        /// <summary>
        /// Handle Dynamic Scaling
        /// </summary>
        /// <param name="value"></param>
        public void SetDynamicResolution(float value)
        {
            if (UniversalRenderPipelineAsset != null)
            {
                UniversalRenderPipelineAsset.renderScale = 1 / value;
            }
        }

        /// <summary>
        /// Creates new buffers and sends them to the plugin
        /// </summary>
        private void SetupFrameBuffers()
        {
            m_previousScaleFactor = m_scaleFactor;

            SetupCommandBuffer();

            m_previousRenderingPath = m_mainCamera.actualRenderingPath;
        }

        /// <summary>
        /// Creates new buffers, sends them to the plugin, and re-initializes SGSR2 to adjust the display size
        /// </summary>
        private void SetupResolution()
        {
            m_displayWidth = m_mainCamera.pixelWidth;
            m_displayHeight = m_mainCamera.pixelHeight;
            m_renderWidth = (int)(m_mainCamera.pixelWidth / m_scaleFactor);
            m_renderHeight = (int)(m_mainCamera.pixelHeight / m_scaleFactor);

            if (m_mainCamera.stereoEnabled)
            {
                m_renderWidth = XRSettings.eyeTextureWidth;
                m_renderHeight = XRSettings.eyeTextureHeight;
                m_displayWidth = (int)(XRSettings.eyeTextureWidth / XRSettings.eyeTextureResolutionScale);
                m_displayHeight = (int)(XRSettings.eyeTextureHeight / XRSettings.eyeTextureResolutionScale);

                if (m_displayWidth == 0)
                {
                    return;
                }
            }
            prevDisplayWidth = m_displayWidth;
            prevDisplayHeight = m_displayHeight;

            m_prevGraphicsFormat = m_graphicsFormat;

            DestroyContexts();

            int numContexts = m_mainCamera.stereoEnabled ? 2 : 1;
            for (int i = 0; i < numContexts; i++)
            {
                Vector2Int maxRenderSize = new Vector2Int(m_renderWidth, m_renderHeight);
                Vector2Int displaySize = new Vector2Int(m_displayWidth, m_displayHeight);

                m_contexts[i] = new SGSR2_Context();
                switch (variant)
                {
                    case SGSR2_Variant.TwoPassFragment:
                        m_contexts[i].InitTwoPassFragment(maxRenderSize, displaySize, m_graphicsFormat);
                        break;
                    case SGSR2_Variant.TwoPassCompute:
                        m_contexts[i].InitTwoPassCompute(maxRenderSize, displaySize, m_graphicsFormat);
                        break;
                    case SGSR2_Variant.ThreePassCompute:
                        m_contexts[i].InitThreePassCompute(maxRenderSize, displaySize, m_graphicsFormat);
                        break;
                }
            }

            SetupFrameBuffers();
        }

        /// <summary>
        /// Automatically Setup camera stacking
        /// </summary>
        private void SetupCameraStacking()
        {
            m_prevCameraStackCount = m_cameraData.cameraStack.Count;
            if (m_cameraData.renderType == CameraRenderType.Base)
            {
                m_isBaseCamera = true;

                m_cameraStacking = m_cameraData.cameraStack.Count > 0;
                if (m_cameraStacking)
                {
                    CleanupOverlayCameras();
                    m_prevStackQuality = quality;

                    m_topCamera = m_cameraData.cameraStack[^1];

                    for (int i = 0; i < m_cameraData.cameraStack.Count; i++)
                    {
                        SGSR2_URP stackedCamera = m_cameraData.cameraStack[i].gameObject.GetComponent<SGSR2_URP>();
                        if (stackedCamera == null)
                        {
                            stackedCamera = m_cameraData.cameraStack[i].gameObject.AddComponent<SGSR2_URP>();
                        }
                        m_prevCameraStack.Add(m_cameraData.cameraStack[i].gameObject.GetComponent<SGSR2_URP>());

                        //stackedCamera.hideFlags = HideFlags.HideInInspector;
                        stackedCamera.m_cameraStacking = true;
                        stackedCamera.m_topCamera = m_topCamera;

                        stackedCamera.OnSetQuality(variant, quality);
                    }
                }
            }
        }

        private void CleanupOverlayCameras()
        {
            for (int i = 0; i < m_prevCameraStack.Count; i++)
            {
                if (!m_prevCameraStack[i].m_isBaseCamera)
                    DestroyImmediate(m_prevCameraStack[i]);
            }
            m_prevCameraStack = new List<SGSR2_URP>();
        }

        protected override void DisableSGSR2()
        {
            RenderPipelineManager.beginCameraRendering -= PreRenderCamera;
            RenderPipelineManager.endCameraRendering -= PostRenderCamera;

            SetDynamicResolution(1);
            if (sgsr2ScriptableRenderFeature != null)
            {
                for (int i = 0; i < sgsr2ScriptableRenderFeature.Count; i++)
                {
                    sgsr2ScriptableRenderFeature[i].IsEnabled = false;
                }
            }
            CleanupOverlayCameras();
            m_previousScaleFactor = -1;
            m_prevStackQuality = (SGSR2_Quality)(-1);

            if (m_upscalerOutput != null)
            {
                m_upscalerOutput.Release();
                m_upscalerOutput = null;

                if (m_opaqueOnlyColorBuffer != null)
                {
                    m_opaqueOnlyColorBuffer.Release();
                    m_opaqueOnlyColorBuffer = null;
                }
            }

            if (m_fragmentMaterial != null)
            {
                Destroy(m_fragmentMaterial);
                m_fragmentMaterial = null;
            }

            if (m_assets != null)
            {
                Resources.UnloadAsset(m_assets);
                m_assets = null;
            }

            DestroyContexts();
        }

        private void DestroyContexts()
        {
            for (int i = 0; i < m_contexts.Length; i++)
            {
                if (m_contexts[i] != null)
                {
                    m_contexts[i].Destroy();
                    m_contexts[i] = null;
                }
            }
        }
    }
}
