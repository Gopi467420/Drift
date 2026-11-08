using Enums;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("ObjectRef")]
    [SerializeField] private Volume Vignette;
    [SerializeField] private GameObject pauseMenu;

    [Header("StatsRef")]
    [SerializeField] private TextMeshProUGUI scoreUI;
    [SerializeField] private TextMeshProUGUI scoreMultiplierUI;
    [SerializeField] private TextMeshProUGUI staminaUI;
    [SerializeField] private TextMeshProUGUI rewardsAchievements;
    [Tooltip("Countdown of the running power ups.")]
    [SerializeField] private TextMeshProUGUI powerUpsUI;
    [Tooltip("The big line in the middle when a power up is picked up.")]
    [SerializeField] private PowerPopup powerPopup;

    [Tooltip("Seconds the red vignette takes to close in.")]
    public float damageFadeDuration = 0.35f;
    [Range(0f, 1f)] public float damageVignetteStrength = 0.6f;
    public Color damageColor = new Color(0.85f, 0f, 0.05f);

    public UnityEvent<float> OnUpdateStaminaUI;
    public UnityEvent<Rewards> OnUpdateStats;
    public UnityEvent<float> OnUpdateScoreMultUI;

    private RewardPopups rewardPopups;
    private int shownInvincibleSeconds, shownBoost, shownBoostSeconds;

    public GameObject PauseMenu => pauseMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rewardPopups = rewardsAchievements.GetComponent<RewardPopups>();
    }

    private void Start()
    {
        scoreUI.SetText("0");

        OnUpdateStaminaUI.AddListener(UpdateStamina);
        OnUpdateStats.AddListener(UpdateScreenUI);
        OnUpdateScoreMultUI.AddListener(UpdateScoreMult);
    }

    private void UpdateScreenUI(Rewards stats)
    {
        scoreUI.text = ManageTextExtend(stats.GetScore());
        UpdateScoreMult(stats.GetScoreMult());
    }

    private string ManageTextExtend(float value)
    {
        if (value >= 1_000_000_000_000f) return (value / 1_000_000_000_000f).ToString("0.#") + "T";
        if (value >= 1_000_000_000f) return (value / 1_000_000_000f).ToString("0.#") + "B";
        if (value >= 1_000_000f) return (value / 1_000_000f).ToString("0.#") + "M";
        if (value >= 1_000f) return (value / 1_000f).ToString("0.#") + "k";
        return value.ToString("0.#");
    }

    public void ShowPowerPopup(string message, Color color)
    {
        powerPopup.Show(message, color);
    }

    // Runs every frame, so the text only changes when a whole second passes.
    public void ShowPowerUps(float invincibleSeconds, float boost, float boostSeconds)
    {
        int invincible = Mathf.CeilToInt(invincibleSeconds);
        int boostLeft = Mathf.CeilToInt(boostSeconds);
        int boostSize = Mathf.RoundToInt(boost);

        if (invincible == shownInvincibleSeconds && boostLeft == shownBoostSeconds && boostSize == shownBoost) return;

        shownInvincibleSeconds = invincible;
        shownBoostSeconds = boostLeft;
        shownBoost = boostSize;

        string text = invincible > 0 ? "INVINCIBLE " + invincible : "";

        if (boostLeft > 0)
        {
            if (text.Length > 0) text += "\n";
            text += "SCORE X" + boostSize + "  " + boostLeft;
        }

        powerUpsUI.text = text;
    }

    public void PlayDamageSequence()
    {
        StartCoroutine(FadeVignette());
    }

    public void ClearRewards()
    {
        rewardPopups.Clear();
    }

    public void ClearDamage()
    {
        StopAllCoroutines();

        if (Vignette.profile.TryGet(out Vignette effect)) effect.intensity.value = 0f;
    }

    private IEnumerator FadeVignette()
    {
        if (!Vignette.profile.TryGet(out Vignette effect)) yield break;

        effect.active = true;
        effect.color.Override(damageColor);
        effect.intensity.overrideState = true;

        float elapsedTime = 0f;

        while (elapsedTime < damageFadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            effect.intensity.value = Mathf.Lerp(0f, damageVignetteStrength, elapsedTime / damageFadeDuration);

            yield return null;
        }
    }

    private void UpdateStamina(float newvalue) { staminaUI.SetText("{0:0}", newvalue); }
    private void UpdateScoreMult(float newvalue) { scoreMultiplierUI.SetText("x{0:2}", newvalue); }

    // Shows every gain in a reward at once. Amounts of zero are skipped.
    public void ShowRewardAchievement(Rewards gained)
    {
        ShowScoreIncrease(gained.GetScore());
        ShowMultiplierIncrease(gained.GetScoreMult());
        ShowStaminaIncrease(gained.GetStamina());
    }

    public void ShowScoreIncrease(float amount)
    {
        rewardPopups.Show(RewardPopups.Kind.Score, amount, scoreUI.rectTransform);
    }

    public void ShowMultiplierIncrease(float amount)
    {
        rewardPopups.Show(RewardPopups.Kind.Multiplier, amount, scoreMultiplierUI.rectTransform);
    }

    public void ShowStaminaIncrease(float amount)
    {
        rewardPopups.Show(RewardPopups.Kind.Stamina, amount, staminaUI.rectTransform);
    }

    public void TogglePauseMenu()
    {
        // Only a live run can pause: not the menus, the intro or the death sequence.
        if (!GameManager.Instance.IsPlaying) return;

        bool isPausing = !pauseMenu.activeSelf;

        pauseMenu.SetActive(isPausing);
        GameManager.Instance.SetPaused(isPausing);

        if (isPausing) InputManager.Instance.SwitchToUI();
        else InputManager.Instance.SwitchToPlayer();
    }

    public void QuitGame()
    {
        WebPage.Quit();
    }
}
