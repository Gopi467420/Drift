
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class ComicOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material outlineMaterial;

        public LayerMask outlineLayer;

        public RenderPassEvent injectionPoint =
            RenderPassEvent.AfterRenderingOpaques;
    }

    [SerializeField] private Settings settings = new Settings();

    private ComicOutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new ComicOutlinePass
        {
            renderPassEvent = settings.injectionPoint
        };
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        outlinePass.Setup(
            settings.outlineMaterial,
            settings.outlineLayer
        );

        renderer.EnqueuePass(outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        outlinePass = null;
    }

    private class ComicOutlinePass : ScriptableRenderPass
    {
        private Material outlineMaterial;
        private LayerMask outlineLayer;

        private static readonly ShaderTagId ShaderTag =
            new ShaderTagId("UniversalForward");

        public void Setup(Material material, LayerMask layer)
        {
            outlineMaterial = material;
            outlineLayer = layer;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            if (outlineMaterial == null)
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

            TextureHandle cameraColor =
                resourceData.activeColorTexture;

            TextureHandle cameraDepth =
                resourceData.activeDepthTexture;

            DrawingSettings drawingSettings =
                RenderingUtils.CreateDrawingSettings(
                    ShaderTag,
                    renderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags
                );

            drawingSettings.overrideMaterial = outlineMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;

            FilteringSettings filteringSettings =
                new FilteringSettings(
                    RenderQueueRange.opaque,
                    outlineLayer
                );

            RendererListParams rendererListParams =
                new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings
                );

            RendererListHandle rendererList =
                renderGraph.CreateRendererList(rendererListParams);

            using (var builder =
                renderGraph.AddRasterRenderPass<OutlinePassData>(
                    "Comic Outline Pass",
                    out var passData))
            {
                passData.rendererList = rendererList;

                builder.UseRendererList(rendererList);

                builder.SetRenderAttachment(
                    cameraColor,
                    0,
                    AccessFlags.ReadWrite
                );

                builder.SetRenderAttachmentDepth(
                    cameraDepth,
                    AccessFlags.Read
                );

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(
                    (OutlinePassData data, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(data.rendererList);
                    }
                );
            }
        }

        private class OutlinePassData
        {
            public RendererListHandle rendererList;
        }
    }
}
