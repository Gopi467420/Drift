using Unity.Cinemachine;
using UnityEngine;

// Game-feel switches.
//   Hype: rises with the score multiplier, and brightens the neon edges and glitches.
//   Power surge: dots burst from a small circle over the normal game view, with a
//   short hit-stop, a camera shake and a field of view kick as it lands.
public class ComicEffects : MonoBehaviour
{
    public static ComicEffects Instance { get; private set; }

    [Header("Hype")]
    [Tooltip("Multiplier that counts as full hype.")]
    [SerializeField, Min(0.1f)] private float fullHypeMultiplier = 8f;
    [Tooltip("How fast hype follows the multiplier, per second.")]
    [SerializeField, Min(0.1f)] private float hypeSpeed = 2.5f;

    [Header("Power surge (seconds of real time)")]
    [Tooltip("How long the burst takes to open.")]
    [SerializeField, Min(0.01f)] private float openTime = 0.16f;
    [Tooltip("How long it stays fully open. It covers the player, so keep this short.")]
    [SerializeField, Min(0f)] private float holdTime = 0.2f;
    [Tooltip("How long the drops take to shrink away.")]
    [SerializeField, Min(0.05f)] private float fadeTime = 0.55f;

    [Tooltip("How long the game nearly stops as it lands.")]
    [SerializeField, Min(0f)] private float hitStopTime = 0.12f;
    [Tooltip("Game speed during the hit-stop, as a share of normal.")]
    [Range(0.01f, 1f)][SerializeField] private float hitStop = 0.05f;

    [Header("Field of view kick")]
    [Tooltip("Degrees added to the field of view at the peak of the kick.")]
    [SerializeField] private float fovKick = 16f;
    [Tooltip("Seconds for the whole kick, out and back.")]
    [SerializeField, Min(0.05f)] private float fovKickTime = 0.4f;

    [Header("Coin pickup")]
    [Tooltip("Sparks thrown out by every pickup.")]
    [SerializeField] private ParticleSystem pickupBurst;
    [SerializeField, Min(1)] private int pickupSparks = 12;

    [Tooltip("Quad on the camera using the Custom/ImpactDots shader.")]
    [SerializeField] private Renderer surgeRenderer;

    private static readonly int HypeID = Shader.PropertyToID("_Comic_Hype");
    private static readonly int GrowID = Shader.PropertyToID("_Grow");
    private static readonly int ProgressID = Shader.PropertyToID("_Progress");

    private float hype;
    private float surgeAge = -1f;
    private bool isHitStopped;

    // True for the whole burst. GameManager makes the player untouchable while it plays.
    public bool IsSurging => surgeAge >= 0f;
    private Material dotsMaterial;
    private CinemachineCamera virtualCamera;
    private float baseFov;
    private float fovAge = -1f;

    private void Awake()
    {
        Instance = this;
        Shader.SetGlobalFloat(HypeID, 0f);

        if (surgeRenderer != null)
        {
            dotsMaterial = surgeRenderer.material;
            surgeRenderer.enabled = false;
        }

        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null) baseFov = virtualCamera.Lens.FieldOfView;
    }

    private void OnDisable()
    {
        EndHitStop();
        SetFov(baseFov);
        Shader.SetGlobalFloat(HypeID, 0f);
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        if (dotsMaterial != null) Destroy(dotsMaterial);
    }

    public void PlayImpact()
    {
        surgeAge = 0f;
        KickFov();

        if (hitStopTime > 0f && GameManager.Instance != null)
        {
            GameManager.Instance.SetHitStop(hitStop);
            isHitStopped = true;
        }

        if (surgeRenderer != null) surgeRenderer.enabled = true;
        if (GameManager.Instance != null) GameManager.Instance.StartCameraShake();

        UpdateSurge();
    }

    public void PlayPickup(Vector3 position)
    {
        if (pickupBurst == null) return;

        pickupBurst.transform.position = position;
        pickupBurst.Emit(pickupSparks);
    }

    public void KickFov()
    {
        fovAge = 0f;
    }

    private void Update()
    {
        float target = GameManager.Instance != null
            ? Mathf.Clamp01(GameManager.Instance.GetScoreMultiplier() / fullHypeMultiplier)
            : 0f;

        hype = Mathf.MoveTowards(hype, target, hypeSpeed * Time.unscaledDeltaTime);
        Shader.SetGlobalFloat(HypeID, hype);

        UpdateFovKick();

        if (surgeAge < 0f) return;

        surgeAge += Time.unscaledDeltaTime;
        UpdateSurge();
    }

    private void UpdateSurge()
    {
        if (isHitStopped && surgeAge >= hitStopTime) EndHitStop();

        if (dotsMaterial == null) return;

        float open = Mathf.Clamp01(surgeAge / openTime);
        float fade = Mathf.Clamp01((surgeAge - openTime - holdTime) / fadeTime);

        // Fast at first, then settling, so it punches open.
        dotsMaterial.SetFloat(GrowID, 1f - (1f - open) * (1f - open) * (1f - open));
        dotsMaterial.SetFloat(ProgressID, fade);

        if (fade >= 1f)
        {
            surgeAge = -1f;
            surgeRenderer.enabled = false;
        }
    }

    // Snaps out fast and eases back, which reads as a jolt of speed.
    private void UpdateFovKick()
    {
        if (fovAge < 0f) return;

        fovAge += Time.unscaledDeltaTime;
        float t = fovAge / fovKickTime;

        if (t >= 1f)
        {
            fovAge = -1f;
            SetFov(baseFov);
            return;
        }

        const float Peak = 0.15f;
        float back = 1f - (t - Peak) / (1f - Peak);

        SetFov(baseFov + fovKick * (t < Peak ? t / Peak : back * back));
    }

    private void SetFov(float value)
    {
        if (virtualCamera == null) return;

        LensSettings lens = virtualCamera.Lens;
        lens.FieldOfView = value;
        virtualCamera.Lens = lens;
    }

    private void EndHitStop()
    {
        if (!isHitStopped) return;
        isHitStopped = false;

        if (GameManager.Instance != null) GameManager.Instance.SetHitStop(1f);
    }
}
