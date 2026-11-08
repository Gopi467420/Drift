using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The big caption in the middle of the screen when a power up is picked up, like
// "INVINCIBLE 5S" or "MAGNET!": black text on a box in the power up's color, in the
// same style as the DRIFT title. It slams in, holds, then fades.
// Uses unscaled time, as the game runs at 3x speed.
public class PowerPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [Tooltip("The caption box behind the text. Tinted with the power up's color.")]
    [SerializeField] private Image backing;
    [SerializeField] private CanvasGroup group;
    [Tooltip("Room around the text inside the box.")]
    [SerializeField] private Vector2 padding = new Vector2(150f, 80f);

    [SerializeField, Min(0.01f)] private float popTime = 0.18f;
    [SerializeField, Min(0.1f)] private float holdTime = 1.1f;
    [SerializeField, Min(0.05f)] private float fadeTime = 0.35f;
    [SerializeField, Min(1f)] private float popScale = 1.8f;
    [Tooltip("Largest tilt, in degrees.")]
    [SerializeField] private float maxTilt = 4f;

    private RectTransform rect;
    private TextGlitch glitch;
    private float age = -1f;

    private void Awake()
    {
        rect = (RectTransform)transform;
        glitch = text.GetComponent<TextGlitch>();
        group.alpha = 0f;
    }

    public void Show(string message, Color color)
    {
        text.text = message;
        backing.color = color;
        rect.sizeDelta = text.GetPreferredValues(message) + padding;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-maxTilt, maxTilt));
        age = 0f;

        if (glitch != null) glitch.PlayBurst();
    }

    private void Update()
    {
        if (age < 0f) return;

        age += Time.unscaledDeltaTime;

        float pop = Mathf.Clamp01(age / popTime);
        float fade = Mathf.Clamp01((age - popTime - holdTime) / fadeTime);

        rect.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, 1f - (1f - pop) * (1f - pop));
        group.alpha = 1f - fade;

        if (fade >= 1f) age = -1f;
    }
}
