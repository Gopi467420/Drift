using UnityEngine;

// Owns the dream background's colors. Sends the current palette to the sky,
// cloud and sparkle shaders, flashes on every cleared zone, and moves to the
// next palette every few zones. Its particle layers follow the player.
[ExecuteAlways]
public class DreamSky : MonoBehaviour
{
    [System.Serializable]
    public class Palette
    {
        public string name;
        [Header("Sky")]
        public Color center = Color.white;
        public Color mid = Color.magenta;
        public Color edge = Color.blue;
        public Color swirl = Color.white;
        public Color accent = Color.cyan;
        [Header("Clouds")]
        public Color cloudLight = Color.white;
        public Color cloudShadow = Color.gray;
        public Color rewardCloud = Color.yellow;
    }

    [SerializeField] private Palette[] palettes =
    {
        new Palette
        {
            name = "Solar Burst",
            center = new Color(1f, 0.96f, 0.72f), mid = new Color(1f, 0.55f, 0.08f), edge = new Color(0.012f, 0.01f, 0.02f),
            swirl = new Color(0.92f, 0.13f, 0.12f), accent = new Color(0.2f, 0.62f, 0.66f),
            cloudLight = new Color(0.36f, 0.22f, 0.52f), cloudShadow = new Color(0.05f, 0.05f, 0.12f), rewardCloud = new Color(1f, 0.35f, 0.3f)
        },
        new Palette
        {
            name = "Toxic Poster",
            center = new Color(0.92f, 1f, 0.7f), mid = new Color(0.3f, 0.95f, 0.6f), edge = new Color(0.008f, 0.02f, 0.02f),
            swirl = new Color(0.95f, 0.15f, 0.6f), accent = new Color(0.3f, 0.2f, 0.75f),
            cloudLight = new Color(0.25f, 0.55f, 0.52f), cloudShadow = new Color(0.03f, 0.1f, 0.12f), rewardCloud = new Color(1f, 0.85f, 0.2f)
        },
        new Palette
        {
            name = "Ultra Pop",
            center = new Color(1f, 0.86f, 1f), mid = new Color(1f, 0.3f, 0.7f), edge = new Color(0.015f, 0.008f, 0.03f),
            swirl = new Color(0.55f, 0.1f, 0.9f), accent = new Color(0.1f, 0.75f, 0.85f),
            cloudLight = new Color(0.4f, 0.2f, 0.6f), cloudShadow = new Color(0.05f, 0.03f, 0.12f), rewardCloud = new Color(0.3f, 0.95f, 0.95f)
        },
        new Palette
        {
            name = "Ice Flare",
            center = new Color(0.9f, 1f, 1f), mid = new Color(0.2f, 0.75f, 1f), edge = new Color(0.008f, 0.012f, 0.03f),
            swirl = new Color(0.1f, 0.25f, 0.9f), accent = new Color(1f, 0.45f, 0.15f),
            cloudLight = new Color(0.2f, 0.35f, 0.6f), cloudShadow = new Color(0.02f, 0.05f, 0.12f), rewardCloud = new Color(1f, 0.6f, 0.2f)
        }
    };

    [Tooltip("Palette shown at the start of a run.")]
    [SerializeField, Min(0)] private int startPalette;
    [Tooltip("The sky changes after this many cleared zones.")]
    [SerializeField, Min(1)] private int zonesPerPalette = 3;
    [Tooltip("Seconds to blend from one palette to the next.")]
    [SerializeField, Min(0.1f)] private float blendTime = 6f;

    [Header("Zone reward")]
    [Tooltip("Seconds the bright flash takes to fade after clearing a zone.")]
    [SerializeField, Min(0.05f)] private float flashTime = 1.1f;

    [Header("Look")]
    [Tooltip("Size of the sky and cloud print dots, as a share of the screen height.")]
    [Range(0.002f, 0.03f)] [SerializeField] private float dotScale = 0.008f;

    private static readonly int CenterID = Shader.PropertyToID("_Dream_Center");
    private static readonly int MidID = Shader.PropertyToID("_Dream_Mid");
    private static readonly int EdgeID = Shader.PropertyToID("_Dream_Edge");
    private static readonly int SwirlID = Shader.PropertyToID("_Dream_Swirl");
    private static readonly int AccentID = Shader.PropertyToID("_Dream_Accent");
    private static readonly int CloudLightID = Shader.PropertyToID("_Dream_CloudLight");
    private static readonly int CloudShadowID = Shader.PropertyToID("_Dream_CloudShadow");
    private static readonly int RewardCloudID = Shader.PropertyToID("_Dream_RewardCloud");
    private static readonly int FlashID = Shader.PropertyToID("_Dream_Flash");
    private static readonly int DotScaleID = Shader.PropertyToID("_Dream_DotScale");

    private int fromIndex, toIndex;
    private float blend = 1f;
    private float flash;
    private int zonesCleared;

    private void OnEnable()
    {
        ResetColors();
    }

    private void OnValidate()
    {
        ResetColors();
    }

    private void Start()
    {
        if (Application.isPlaying && GameManager.Instance != null)
            GameManager.Instance.OnZoneCleared.AddListener(OnZoneCleared);
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && GameManager.Instance != null)
            GameManager.Instance.OnZoneCleared.RemoveListener(OnZoneCleared);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (Player.Instance != null)
        {
            Vector3 position = Player.Instance.transform.position;
            transform.position = new Vector3(position.x, 0f, position.z);
        }

        if (blend < 1f)
        {
            blend = Mathf.Min(blend + Time.deltaTime / blendTime, 1f);
            SendColors();
        }

        if (flash > 0f)
        {
            flash = Mathf.Max(flash - Time.deltaTime / flashTime, 0f);
            Shader.SetGlobalFloat(FlashID, flash * flash);
        }
    }

    private void OnZoneCleared()
    {
        flash = 1f;

        zonesCleared++;
        if (zonesCleared % zonesPerPalette != 0 || palettes.Length < 2) return;

        fromIndex = toIndex;
        toIndex = (toIndex + 1) % palettes.Length;
        blend = 0f;
    }

    private void ResetColors()
    {
        if (palettes == null || palettes.Length == 0) return;

        fromIndex = toIndex = Mathf.Min(startPalette, palettes.Length - 1);
        blend = 1f;
        flash = 0f;

        Shader.SetGlobalFloat(FlashID, 0f);
        Shader.SetGlobalFloat(DotScaleID, dotScale);
        SendColors();
    }

    private void SendColors()
    {
        Palette from = palettes[fromIndex];
        Palette to = palettes[toIndex];

        Send(CenterID, from.center, to.center);
        Send(MidID, from.mid, to.mid);
        Send(EdgeID, from.edge, to.edge);
        Send(SwirlID, from.swirl, to.swirl);
        Send(AccentID, from.accent, to.accent);
        Send(CloudLightID, from.cloudLight, to.cloudLight);
        Send(CloudShadowID, from.cloudShadow, to.cloudShadow);
        Send(RewardCloudID, from.rewardCloud, to.rewardCloud);
    }

    // SetGlobalVector sends the numbers as they are, so convert to linear here.
    private void Send(int id, Color from, Color to)
    {
        Shader.SetGlobalVector(id, Color.Lerp(from, to, blend).linear);
    }
}
