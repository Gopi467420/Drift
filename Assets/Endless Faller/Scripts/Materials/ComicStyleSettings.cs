
using UnityEngine;

// One place to tune the comic look of every Custom/ComicLit material.
// Edit the ComicStyle asset (Assets/Endless Faller/Materials) - changes show live.
// Materials only choose a Base Color and whether Halftone / Glitch is on.
[CreateAssetMenu(fileName = "ComicStyle", menuName = "Drift/Comic Style Settings")]
public class ComicStyleSettings : ScriptableObject
{
    [System.Serializable]
    public class Hatching
    {
        public Color color = Color.black;
        [Range(0, 1)] public float strength = 0.403f;
        [Tooltip("Distance between lines, in screen pixels.")]
        [Range(2, 40)] public float spacing = 8.4f;
        [Range(0.01f, 0.5f)] public float width = 0.196f;
        [Range(0, 180)] public float angle = 165f;

        [Header("Where it appears")]
        [Tooltip("How dark a spot must be before hatching starts.")]
        [Range(0, 1)] public float shadowThreshold = 0.718f;
        [Range(0.01f, 1)] public float shadowSoftness = 0.01f;
        [Tooltip("How much cast shadows count.")]
        [Range(0, 1)] public float shadowContribution = 0.766f;
        [Tooltip("How much surfaces facing away from the light count.")]
        [Range(0, 1)] public float unlitContribution = 1f;

        [Header("Cross Hatching")]
        public bool crossHatching = true;
        [Range(0, 1)] public float crossHatchThreshold = 0.411f;
        [Range(0, 1)] public float crossHatchStrength = 0.048f;
    }

    [System.Serializable]
    public class Halftone
    {
        public Color dotColor = new Color(0.05f, 0.03f, 0.08f, 1f);
        public Color paperColor = Color.white;
        [Tooltip("Size of one dot cell, in screen pixels.")]
        [Range(1, 100)] public float dotScale = 15.1f;
        [Range(0, 1)] public float strength = 0.995f;
        [Range(0, 1)] public float dotDarkness = 1f;
        [Range(0.5f, 2)] public float contrast = 0.5f;
        [Range(0, 1)] public float shadowThreshold = 1f;
        [Range(0.01f, 1)] public float shadowSoftness = 0.95f;
    }

    [System.Serializable]
    public class Glitch
    {
        [Tooltip("How many times per second the glitch pattern jumps. 12 reads like animation on twos.")]
        [Range(0, 30)] public float speed = 12f;

        [Header("How often")]
        [Tooltip("Each object gets one chance to glitch every this many seconds.")]
        [Range(1, 30)] public float interval = 8f;
        [Tooltip("How long one burst lasts, in seconds.")]
        [Range(0.05f, 1f)] public float duration = 0.35f;
        [Tooltip("Chance that an object really glitches when its turn comes.")]
        [Range(0, 1)] public float chance = 0.4f;

        [Header("Colors")]
        public Color colorA = new Color(0f, 0.85f, 1f, 1f);
        public Color colorB = new Color(1f, 0.05f, 0.5f, 1f);

        [Header("Shards (comic print on the object)")]
        [Tooltip("Share of shards filled at the start of a burst.")]
        [Range(0, 1)] public float shardAmount = 0.4f;
        [Tooltip("Shard height, as a share of the screen height.")]
        [Range(0.005f, 0.2f)] public float shardHeight = 0.035f;
        [Range(0, 1)] public float shardStrength = 0.95f;
        [Tooltip("Size of the print dots, as a share of the screen height.")]
        [Range(0.002f, 0.05f)] public float dotScale = 0.011f;

        [Header("Slices (mesh snaps sideways)")]
        [Tooltip("Share of screen bands that snap during a burst.")]
        [Range(0, 1)] public float sliceAmount = 0.35f;
        [Tooltip("More bands = thinner bands.")]
        [Range(2, 80)] public float sliceCount = 24f;
        [Tooltip("How far a band snaps, in world units.")]
        [Range(0, 50)] public float sliceShift = 6f;

        [Header("Ghost (offset color layer)")]
        [Tooltip("How far the color layer sits from the object, as a share of the screen height.")]
        [Range(0, 0.05f)] public float ghostOffset = 0.012f;
    }

    [System.Serializable]
    public class Spawn
    {
        [Tooltip("Objects appear at about this distance from the camera. Keep it near RandomSpawner's Spawn Distance, so everything glitches in as it spawns. 0 = always visible.")]
        [Range(0, 3000)] public float distance = 1700f;
        [Tooltip("Depth of the glitch shapes phase, in world units. At 120 units per second and timeScale 3, 220 is about 0.6 seconds.")]
        [Range(10, 500)] public float band = 220f;
    }

    [System.Serializable]
    public class Rim
    {
        [ColorUsage(false, true)] public Color colorA = new Color(0f, 0.8f, 1f);
        [ColorUsage(false, true)] public Color colorB = new Color(1f, 0.05f, 0.6f);
        [Range(0, 4)] public float strength = 1.1f;
        [Tooltip("Higher = a thinner line of light along the edges.")]
        [Range(0.5f, 8)] public float power = 3f;
    }

    [System.Serializable]
    public class Outline
    {
        [Tooltip("Width as a share of the screen height. 0 turns it off.")]
        [Range(0, 0.01f)] public float width = 0.0022f;
        [Tooltip("Black inside this distance from the screen center (where the sky is bright), white outside it. In screen heights.")]
        [Range(0, 1)] public float switchRadius = 0.2f;
        [Tooltip("Objects closer to the camera than this lose their outline.")]
        [Range(0, 2000)] public float nearDistance = 60f;
    }

    [System.Serializable]
    public class Surface
    {
        [Tooltip("A test texture laid over every Custom/ComicLit object. Any texture works here.")]
        public Texture2D texture;
        [Tooltip("0 turns it off completely.")]
        [Range(0, 1)] public float strength = 0.85f;
        [Range(0.1f, 10)] public float tiling = 1f;
    }

    public Hatching hatching = new Hatching();
    public Halftone halftone = new Halftone();
    public Glitch glitch = new Glitch();
    public Spawn spawn = new Spawn();
    public Rim rim = new Rim();
    public Outline outline = new Outline();
    public Surface surface = new Surface();

    // Hatching
    private static readonly int HatchColorID = Shader.PropertyToID("_Comic_HatchColor");
    private static readonly int HatchStrengthID = Shader.PropertyToID("_Comic_HatchStrength");
    private static readonly int HatchSpacingID = Shader.PropertyToID("_Comic_HatchSpacing");
    private static readonly int HatchWidthID = Shader.PropertyToID("_Comic_HatchWidth");
    private static readonly int HatchAngleID = Shader.PropertyToID("_Comic_HatchAngle");
    private static readonly int HatchShadowThresholdID = Shader.PropertyToID("_Comic_HatchShadowThreshold");
    private static readonly int HatchShadowSoftnessID = Shader.PropertyToID("_Comic_HatchShadowSoftness");
    private static readonly int EnableCrossHatchingID = Shader.PropertyToID("_Comic_EnableCrossHatching");
    private static readonly int CrossHatchThresholdID = Shader.PropertyToID("_Comic_CrossHatchThreshold");
    private static readonly int CrossHatchStrengthID = Shader.PropertyToID("_Comic_CrossHatchStrength");
    private static readonly int ShadowContributionID = Shader.PropertyToID("_Comic_ShadowContribution");
    private static readonly int UnlitContributionID = Shader.PropertyToID("_Comic_UnlitContribution");

    // Halftone
    private static readonly int DotScaleID = Shader.PropertyToID("_Comic_DotScale");
    private static readonly int HalftoneStrengthID = Shader.PropertyToID("_Comic_HalftoneStrength");
    private static readonly int HalftoneShadowThresholdID = Shader.PropertyToID("_Comic_HalftoneShadowThreshold");
    private static readonly int HalftoneShadowSoftnessID = Shader.PropertyToID("_Comic_HalftoneShadowSoftness");
    private static readonly int ContrastID = Shader.PropertyToID("_Comic_Contrast");
    private static readonly int DotDarknessID = Shader.PropertyToID("_Comic_DotDarkness");
    private static readonly int DotColorID = Shader.PropertyToID("_Comic_DotColor");
    private static readonly int PaperColorID = Shader.PropertyToID("_Comic_PaperColor");

    // Glitch
    private static readonly int GlitchSpeedID = Shader.PropertyToID("_Comic_GlitchSpeed");
    private static readonly int GlitchIntervalID = Shader.PropertyToID("_Comic_GlitchInterval");
    private static readonly int GlitchDurationID = Shader.PropertyToID("_Comic_GlitchDuration");
    private static readonly int GlitchChanceID = Shader.PropertyToID("_Comic_GlitchChance");
    private static readonly int GlitchColorAID = Shader.PropertyToID("_Comic_GlitchColorA");
    private static readonly int GlitchColorBID = Shader.PropertyToID("_Comic_GlitchColorB");
    private static readonly int GlitchShardAmountID = Shader.PropertyToID("_Comic_GlitchShardAmount");
    private static readonly int GlitchShardHeightID = Shader.PropertyToID("_Comic_GlitchShardHeight");
    private static readonly int GlitchShardStrengthID = Shader.PropertyToID("_Comic_GlitchShardStrength");
    private static readonly int GlitchDotScaleID = Shader.PropertyToID("_Comic_GlitchDotScale");
    private static readonly int GlitchSliceAmountID = Shader.PropertyToID("_Comic_GlitchSliceAmount");
    private static readonly int GlitchSliceCountID = Shader.PropertyToID("_Comic_GlitchSliceCount");
    private static readonly int GlitchSliceShiftID = Shader.PropertyToID("_Comic_GlitchSliceShift");
    private static readonly int GlitchGhostOffsetID = Shader.PropertyToID("_Comic_GlitchGhostOffset");

    private static readonly int SpawnDistanceID = Shader.PropertyToID("_Comic_SpawnDistance");
    private static readonly int SpawnBandID = Shader.PropertyToID("_Comic_SpawnBand");
    private static readonly int RimColorAID = Shader.PropertyToID("_Comic_RimColorA");
    private static readonly int RimColorBID = Shader.PropertyToID("_Comic_RimColorB");
    private static readonly int RimStrengthID = Shader.PropertyToID("_Comic_RimStrength");
    private static readonly int RimPowerID = Shader.PropertyToID("_Comic_RimPower");

    private static readonly int OutlineWidthID = Shader.PropertyToID("_Comic_OutlineWidth");
    private static readonly int OutlineSwitchID = Shader.PropertyToID("_Comic_OutlineSwitch");
    private static readonly int OutlineNearID = Shader.PropertyToID("_Comic_OutlineNear");

    private static readonly int SurfaceMapID = Shader.PropertyToID("_Comic_SurfaceMap");
    private static readonly int SurfaceStrengthID = Shader.PropertyToID("_Comic_SurfaceStrength");
    private static readonly int SurfaceTilingID = Shader.PropertyToID("_Comic_SurfaceTiling");

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    // Sends every value to the shader as a global, shared by all ComicLit materials.
    public void Apply()
    {
        Shader.SetGlobalColor(HatchColorID, hatching.color);
        Shader.SetGlobalFloat(HatchStrengthID, hatching.strength);
        Shader.SetGlobalFloat(HatchSpacingID, hatching.spacing);
        Shader.SetGlobalFloat(HatchWidthID, hatching.width);
        Shader.SetGlobalFloat(HatchAngleID, hatching.angle);
        Shader.SetGlobalFloat(HatchShadowThresholdID, hatching.shadowThreshold);
        Shader.SetGlobalFloat(HatchShadowSoftnessID, hatching.shadowSoftness);
        Shader.SetGlobalFloat(EnableCrossHatchingID, hatching.crossHatching ? 1f : 0f);
        Shader.SetGlobalFloat(CrossHatchThresholdID, hatching.crossHatchThreshold);
        Shader.SetGlobalFloat(CrossHatchStrengthID, hatching.crossHatchStrength);
        Shader.SetGlobalFloat(ShadowContributionID, hatching.shadowContribution);
        Shader.SetGlobalFloat(UnlitContributionID, hatching.unlitContribution);

        Shader.SetGlobalFloat(DotScaleID, halftone.dotScale);
        Shader.SetGlobalFloat(HalftoneStrengthID, halftone.strength);
        Shader.SetGlobalFloat(HalftoneShadowThresholdID, halftone.shadowThreshold);
        Shader.SetGlobalFloat(HalftoneShadowSoftnessID, halftone.shadowSoftness);
        Shader.SetGlobalFloat(ContrastID, halftone.contrast);
        Shader.SetGlobalFloat(DotDarknessID, halftone.dotDarkness);
        Shader.SetGlobalColor(DotColorID, halftone.dotColor);
        Shader.SetGlobalColor(PaperColorID, halftone.paperColor);

        Shader.SetGlobalFloat(GlitchSpeedID, glitch.speed);
        Shader.SetGlobalFloat(GlitchIntervalID, glitch.interval);
        Shader.SetGlobalFloat(GlitchDurationID, glitch.duration);
        Shader.SetGlobalFloat(GlitchChanceID, glitch.chance);
        Shader.SetGlobalColor(GlitchColorAID, glitch.colorA);
        Shader.SetGlobalColor(GlitchColorBID, glitch.colorB);
        Shader.SetGlobalFloat(GlitchShardAmountID, glitch.shardAmount);
        Shader.SetGlobalFloat(GlitchShardHeightID, glitch.shardHeight);
        Shader.SetGlobalFloat(GlitchShardStrengthID, glitch.shardStrength);
        Shader.SetGlobalFloat(GlitchDotScaleID, glitch.dotScale);
        Shader.SetGlobalFloat(GlitchSliceAmountID, glitch.sliceAmount);
        Shader.SetGlobalFloat(GlitchSliceCountID, glitch.sliceCount);
        Shader.SetGlobalFloat(GlitchSliceShiftID, glitch.sliceShift);
        Shader.SetGlobalFloat(GlitchGhostOffsetID, glitch.ghostOffset);

        Shader.SetGlobalFloat(SpawnDistanceID, spawn.distance);
        Shader.SetGlobalFloat(SpawnBandID, spawn.band);

        Shader.SetGlobalColor(RimColorAID, rim.colorA);
        Shader.SetGlobalColor(RimColorBID, rim.colorB);
        Shader.SetGlobalFloat(RimStrengthID, rim.strength);
        Shader.SetGlobalFloat(RimPowerID, rim.power);

        Shader.SetGlobalFloat(OutlineWidthID, outline.width);
        Shader.SetGlobalFloat(OutlineSwitchID, outline.switchRadius);
        Shader.SetGlobalFloat(OutlineNearID, outline.nearDistance);

        if (surface.texture != null) Shader.SetGlobalTexture(SurfaceMapID, surface.texture);
        Shader.SetGlobalFloat(SurfaceStrengthID, surface.texture != null ? surface.strength : 0f);
        Shader.SetGlobalFloat(SurfaceTilingID, surface.tiling);
    }
}
