using System.Collections;
using UnityEngine;

// Runs when the player is hit. The world freezes and goes black, leaving the player and
// the obstacle that hit them with their real shading, under the red vignette, with a
// comic caption. Both then glitch out, the sky breaks back in, and game over shows.
public class DeathSequence : MonoBehaviour
{
    [Tooltip("The yellow OUCH caption.")]
    [SerializeField] private RectTransform ouchCaption;
    [Tooltip("The obstacle spawn glitch, as a material.")]
    [SerializeField] private Material glitchMaterial;
    [Tooltip("The sky's cloud and streak layers, hidden during the black.")]
    [SerializeField] private GameObject[] backgroundLayers;

    [Header("Timing (seconds of real time)")]
    [SerializeField, Min(0.1f)] private float blackTime = 1.2f;
    [SerializeField, Min(0.05f)] private float ouchPopTime = 0.16f;
    [SerializeField, Min(0.05f)] private float glitchOutTime = 0.4f;
    [SerializeField, Min(0.05f)] private float glitchBackTime = 0.6f;
    [Tooltip("Largest tilt of the caption, in degrees.")]
    [SerializeField] private float ouchTilt = 5f;
    [Tooltip("How many times per second the glitch flickers.")]
    [SerializeField] private float flickerRate = 24f;

    private static readonly int BlackoutID = Shader.PropertyToID("_Comic_Blackout");

    private void Awake()
    {
        Shader.SetGlobalFloat(BlackoutID, 0f);
        ouchCaption.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(BlackoutID, 0f);
    }

    public void Play(GameObject hitObject, GameManager.RunStats stats)
    {
        StartCoroutine(Run(hitObject, stats));
    }

    private IEnumerator Run(GameObject hitObject, GameManager.RunStats stats)
    {
        Renderer[] hitParts = hitObject != null ? hitObject.GetComponentsInChildren<Renderer>() : new Renderer[0];

        HideEverythingExcept(hitObject);
        foreach (GameObject layer in backgroundLayers) layer.SetActive(false);

        Shader.SetGlobalFloat(BlackoutID, 1f);
        MenuScreens.Instance.SetHudVisible(false);
        UIManager.Instance.ClearRewards();
        UIManager.Instance.PlayDamageSequence();
        GameManager.Instance.SetHitStop(0f);

        ouchCaption.gameObject.SetActive(true);
        ouchCaption.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-ouchTilt, ouchTilt));

        float age = 0f;

        while (age < blackTime)
        {
            age += Time.unscaledDeltaTime;

            float pop = Mathf.Clamp01(age / ouchPopTime);
            ouchCaption.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, pop * pop);

            yield return null;
        }

        ouchCaption.gameObject.SetActive(false);

        // Time runs again so the glitch animates, while the black stays up.
        GameManager.Instance.SetHitStop(1f);

        yield return GlitchOut(hitParts);

        UIManager.Instance.ClearDamage();

        foreach (GameObject layer in backgroundLayers) layer.SetActive(true);

        age = 0f;

        while (age < glitchBackTime)
        {
            age += Time.unscaledDeltaTime;
            Shader.SetGlobalFloat(BlackoutID, 1f - Mathf.Clamp01(age / glitchBackTime));
            yield return null;
        }

        Shader.SetGlobalFloat(BlackoutID, 0f);
        MenuScreens.Instance.ShowGameOver(stats);
    }

    // The player and the obstacle break into the spawn glitch, flicker and vanish.
    private IEnumerator GlitchOut(Renderer[] hitParts)
    {
        MeshRenderer[] playerParts = Player.Instance.VisibleParts;

        SetGlitch(hitParts);
        SetGlitch(playerParts);

        float age = 0f;

        while (age < glitchOutTime)
        {
            age += Time.unscaledDeltaTime;

            bool isShown = Mathf.FloorToInt(age * flickerRate) % 3 != 0;

            foreach (Renderer part in hitParts) part.enabled = isShown;
            foreach (Renderer part in playerParts) part.enabled = isShown;

            yield return null;
        }

        foreach (Renderer part in hitParts) part.enabled = false;
        Player.Instance.SetVisible(false);
    }

    private void SetGlitch(Renderer[] parts)
    {
        foreach (Renderer part in parts)
        {
            var glitch = new Material[part.sharedMaterials.Length];
            for (int i = 0; i < glitch.Length; i++) glitch[i] = glitchMaterial;
            part.sharedMaterials = glitch;
        }
    }

    private static void HideEverythingExcept(GameObject hitObject)
    {
        foreach (ObjectPool pool in ObjectPoolManager.Instance.Pools)
        {
            for (int i = 0; i < pool.ActiveCount; i++)
            {
                PooledObject item = pool[i];
                if (item == null || item.gameObject == hitObject) continue;

                foreach (Renderer part in item.GetComponentsInChildren<Renderer>())
                    part.enabled = false;
            }
        }
    }
}
