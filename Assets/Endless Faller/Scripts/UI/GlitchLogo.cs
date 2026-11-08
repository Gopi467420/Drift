using UnityEngine;
using UnityEngine.UI;

// The game logo: it floats gently like something in a dream, and every few seconds it
// breaks up in a short glitch. Put it on an Image using the Custom/GlitchLogo shader.
// Uses unscaled time, so it keeps moving on the pause menu.
public class GlitchLogo : MonoBehaviour
{
    [Header("Glitch")]
    [Tooltip("Fewest and most seconds of calm between two glitches.")]
    [SerializeField] private Vector2 calmTime = new Vector2(1.5f, 4f);
    [SerializeField, Min(0.05f)] private float burstTime = 0.3f;
    [Tooltip("How many times per second the pattern changes during a glitch.")]
    [SerializeField, Min(1f)] private float flickerRate = 18f;

    [Header("Float")]
    [Tooltip("How far it drifts up and down, in canvas units.")]
    [SerializeField] private float floatHeight = 10f;
    [Tooltip("Largest tilt, in degrees.")]
    [SerializeField] private float floatTilt = 1.2f;
    [Tooltip("Seconds for one full drift.")]
    [SerializeField, Min(0.1f)] private float floatTime = 6f;

    private static readonly int GlitchID = Shader.PropertyToID("_Glitch");
    private static readonly int SeedID = Shader.PropertyToID("_Seed");

    private RectTransform rect;
    private Material material;
    private Vector2 home;
    private float burstLeft;
    private float nextFlicker;
    private float nextBurst;

    private void Awake()
    {
        rect = (RectTransform)transform;
        home = rect.anchoredPosition;

        // Its own copy, so each logo on screen can glitch at its own moment.
        Image image = GetComponent<Image>();
        material = new Material(image.material);
        image.material = material;
    }

    private void OnEnable()
    {
        // Opens with a glitch, so a new screen lands with a bit of life.
        PlayBurst();
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    public void PlayBurst()
    {
        burstLeft = burstTime;
        nextFlicker = 0f;
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float wave = Mathf.Sin(time * Mathf.PI * 2f / floatTime);

        rect.anchoredPosition = home + Vector2.up * (wave * floatHeight);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Cos(time * Mathf.PI * 2f / floatTime) * floatTilt);

        UpdateGlitch(time);
    }

    private void UpdateGlitch(float time)
    {
        if (burstLeft > 0f)
        {
            burstLeft -= Time.unscaledDeltaTime;

            if (burstLeft <= 0f)
            {
                material.SetFloat(GlitchID, 0f);
                nextBurst = time + Random.Range(calmTime.x, calmTime.y);
                return;
            }

            if (time >= nextFlicker)
            {
                nextFlicker = time + 1f / flickerRate;
                material.SetFloat(GlitchID, Random.Range(0.35f, 1f));
                material.SetFloat(SeedID, Random.Range(0f, 100f));
            }

            return;
        }

        if (time >= nextBurst) PlayBurst();
    }
}
