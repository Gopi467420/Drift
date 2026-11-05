
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

public class ComicHalftoneFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("Halftone")]
        public Material halftoneMaterial;

        [Header("Hazard Mask")]
        public Material hazardMaskMaterial;
        public LayerMask hazardLayer;

        public RenderPassEvent injectionPoint =
            RenderPassEvent.BeforeRenderingPostProcessing;
    }

    [SerializeField] private Settings settings = new Settings();

    private ComicHalftonePass halftonePass;

    public override void Create()
    {
        halftonePass = new ComicHalftonePass
        {
            renderPassEvent = settings.injectionPoint
        };
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (settings.halftoneMaterial == null ||
            settings.hazardMaskMaterial == null ||
            settings.hazardLayer.value == 0)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        halftonePass.Setup(
            settings.halftoneMaterial,
            settings.hazardMaskMaterial,
            settings.hazardLayer
        );

        renderer.EnqueuePass(halftonePass);
    }

    protected override void Dispose(bool disposing)
    {
        halftonePass = null;
    }

    private class ComicHalftonePass : ScriptableRenderPass
    {
        private Material halftoneMaterial;
        private Material maskMaterial;
        private LayerMask hazardLayer;

        private static readonly int HazardMaskID =
            Shader.PropertyToID("_HazardMask");
        private static readonly ShaderTagId ShaderTag =
    new ShaderTagId("UniversalForward");
       
        private class MaskPassData
        {
            public RendererListHandle rendererList;
        }

        public ComicHalftonePass()
        {
            requiresIntermediateTexture = true;

            ConfigureInput(
                ScriptableRenderPassInput.Depth
            );
        }

        public void Setup(
            Material newHalftoneMaterial,
            Material newMaskMaterial,
            LayerMask newHazardLayer)
        {
            halftoneMaterial = newHalftoneMaterial;
            maskMaterial = newMaskMaterial;
            hazardLayer = newHazardLayer;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            if (halftoneMaterial == null || maskMaterial == null)
                return;

            UniversalResourceData resourceData =
                frameData.Get<UniversalResourceData>();

            UniversalRenderingData renderingData =
                frameData.Get<UniversalRenderingData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            UniversalLightData lightData =
                frameData.Get<UniversalLightData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source =
                resourceData.activeColorTexture;

            TextureHandle cameraDepth =
                resourceData.activeDepthTexture;

            // Create the hazard mask texture.
            TextureDesc maskDescriptor =
                renderGraph.GetTextureDesc(source);

            maskDescriptor.name = "Hazard Mask";
            maskDescriptor.colorFormat = GraphicsFormat.R8_UNorm;
            maskDescriptor.depthBufferBits = DepthBits.None;
            maskDescriptor.clearBuffer = true;
            maskDescriptor.clearColor = Color.black;

            TextureHandle hazardMask =
                renderGraph.CreateTexture(maskDescriptor);

            // Create the renderer list for Hazard-layer objects.
           
            DrawingSettings drawingSettings =
    RenderingUtils.CreateDrawingSettings(
        ShaderTag,
        renderingData,
        cameraData,
        lightData,
        cameraData.defaultOpaqueSortFlags
    );
            drawingSettings.overrideMaterial = maskMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;

            FilteringSettings filteringSettings =
                new FilteringSettings(
                    RenderQueueRange.opaque,
                    hazardLayer
                );

            RendererListParams rendererListParams =
                new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings
                );

            RendererListHandle rendererList =
                renderGraph.CreateRendererList(rendererListParams);

            // Render the mask.
            using (var builder =
                renderGraph.AddRasterRenderPass<MaskPassData>(
                    "Render Hazard Mask",
                    out var passData))
            {
                passData.rendererList = rendererList;

                builder.UseRendererList(rendererList);

                builder.SetRenderAttachment(
                    hazardMask,
                    0,
                    AccessFlags.Write
                );

                // Use the camera depth to hide occluded hazards.
                builder.SetRenderAttachmentDepth(
                    cameraDepth,
                    AccessFlags.Read
                );

                // Make the mask available to the following pass.
                builder.SetGlobalTextureAfterPass(
                    hazardMask,
                    HazardMaskID
                );

                builder.SetRenderFunc(
                    (MaskPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(data.rendererList);
                    }
                );
            }

            // Apply the halftone effect.
            TextureDesc outputDescriptor =
                renderGraph.GetTextureDesc(source);

            outputDescriptor.name = "Comic Halftone Output";

            TextureHandle destination =
                renderGraph.CreateTexture(outputDescriptor);

            var blitParameters = new BlitMaterialParameters(
                source,
                destination,
                halftoneMaterial,
                0
            );

            renderGraph.AddBlitPass(
                blitParameters,
                "Apply Hazard Halftone"
            );

            resourceData.cameraColor = destination;
        }
    }
}
