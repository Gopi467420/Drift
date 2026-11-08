using TMPro;
using UnityEngine;

// Breaks a text up for a moment every few seconds, like the DRIFT logo: some letters
// jump sideways and flash cyan or pink. Each text picks its own moments, so a screen
// full of them never glitches all at once. Uses unscaled time, so it runs while paused.
[RequireComponent(typeof(TMP_Text))]
public class TextGlitch : MonoBehaviour
{
    [Tooltip("Fewest and most seconds of calm between two glitches.")]
    [SerializeField] private Vector2 calmTime = new Vector2(2.5f, 7f);
    [SerializeField, Min(0.05f)] private float burstTime = 0.25f;
    [Tooltip("How many times per second the letters jump during a glitch.")]
    [SerializeField, Min(1f)] private float flickerRate = 18f;
    [Tooltip("Largest jump, as a share of the font size.")]
    [SerializeField, Range(0f, 0.5f)] private float jump = 0.12f;
    [SerializeField] private Color splitColorA = new Color(0f, 0.95f, 1f);
    [SerializeField] private Color splitColorB = new Color(1f, 0.15f, 0.65f);

    private TMP_Text text;
    private float burstLeft;
    private float nextFlicker;
    private float nextBurst;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        burstLeft = 0f;
        nextBurst = Time.unscaledTime + Random.Range(0.3f, calmTime.y);
    }

    private void OnDisable()
    {
        // Leaves the letters back in place for the next time it shows.
        if (text != null) text.ForceMeshUpdate();
    }

    public void PlayBurst()
    {
        burstLeft = burstTime;
        nextFlicker = 0f;
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        if (burstLeft <= 0f)
        {
            if (time >= nextBurst) PlayBurst();
            return;
        }

        burstLeft -= Time.unscaledDeltaTime;

        if (burstLeft <= 0f)
        {
            text.ForceMeshUpdate();
            nextBurst = time + Random.Range(calmTime.x, calmTime.y);
            return;
        }

        if (time >= nextFlicker)
        {
            nextFlicker = time + 1f / flickerRate;
            Scramble();
        }
    }

    private void Scramble()
    {
        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        float largestJump = jump * text.fontSize;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo letter = info.characterInfo[i];
            if (!letter.isVisible || Random.value > 0.35f) continue;

            Vector3[] vertices = info.meshInfo[letter.materialReferenceIndex].vertices;
            Color32[] colors = info.meshInfo[letter.materialReferenceIndex].colors32;
            Vector3 shift = new Vector3(Random.Range(-largestJump, largestJump), 0f, 0f);
            bool isTinted = Random.value < 0.5f;
            Color32 tint = Random.value < 0.5f ? splitColorA : splitColorB;

            for (int v = 0; v < 4; v++)
            {
                vertices[letter.vertexIndex + v] += shift;
                if (isTinted) colors[letter.vertexIndex + v] = tint;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
}
