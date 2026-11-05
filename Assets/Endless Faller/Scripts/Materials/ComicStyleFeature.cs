
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Keeps the ComicStyle settings applied while rendering (also in builds).
// It adds no render pass: hatching, halftone and glitch are drawn inside
// each Custom/ComicLit material. Tune them on the ComicStyle asset.
public class ComicStyleFeature : ScriptableRendererFeature
{
    [SerializeField] private ComicStyleSettings style;

    public override void Create()
    {
        if (style != null)
            style.Apply();
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        // Re-sent every frame so edits show up live.
        if (style != null)
            style.Apply();
    }
}
