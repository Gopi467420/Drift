using UnityEngine;
using UnityEngine.UI;

// The stamina bar. It sits in one fixed place, so a quick glance is enough.
// The fill glides toward the real value, and turns red when stamina is low.
public class StaminaHud : MonoBehaviour
{
    [SerializeField] private Image fill;

    [SerializeField] private Color fullColor = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color lowColor = new Color(1f, 0.12f, 0.1f);

    [Tooltip("Below this share of stamina the bar is red.")]
    [Range(0f, 1f)][SerializeField] private float lowThreshold = 0.25f;
    [Tooltip("Higher catches up faster. Lower glides more.")]
    [SerializeField, Min(0.1f)] private float followSpeed = 7f;
    [Tooltip("A gain at least this big flashes the bar. Small coin gains don't.")]
    [Range(0f, 1f)][SerializeField] private float flashGain = 0.08f;
    [Tooltip("Seconds the refill flash takes to fade.")]
    [SerializeField, Min(0.05f)] private float flashTime = 0.35f;

    private float shown;
    private float last;
    private float flash;
    private float lowBlend;

    private void Update()
    {
        if (Player.Instance == null) return;

        PA_Stamina stamina = Player.Instance.GetStaminaComponent();
        if (stamina == null || stamina.GetMaxStamina() <= 0f) return;

        float now = stamina.GetStamina() / stamina.GetMaxStamina();

        if (now >= last + flashGain) flash = 1f;
        last = now;

        float deltaTime = Time.unscaledDeltaTime;

        // Eases in, so no change ever lands as a jump.
        shown = Mathf.Lerp(shown, now, 1f - Mathf.Exp(-followSpeed * deltaTime));
        flash = Mathf.Max(flash - deltaTime / flashTime, 0f);
        lowBlend = Mathf.MoveTowards(lowBlend, now < lowThreshold ? 1f : 0f, deltaTime * 5f);

        fill.fillAmount = shown;
        fill.color = Color.Lerp(Color.Lerp(fullColor, lowColor, lowBlend), Color.white, flash);
    }
}
