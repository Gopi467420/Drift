using Enums;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Intro, Playing, Dead }

    public struct RunStats
    {
        public float score;
        public float highScore;
        public bool isNewHighScore;
        public float seconds;
        public int zonesCleared;
        public int pickups;
        public float bestMultiplier;
    }

    private const string DifficultyKey = "Difficulty";
    private const string HighScoreKey = "HighScore";

    public static GameManager Instance { get; private set; }

    // Set by Fall Again, so the restarted scene goes straight into a run.
    public static bool SkipStartScreen { get; set; }

    [Tooltip("Speeds, multiplier, reward and difficulty values.")]
    [SerializeField] private GameSettings settings;
    [SerializeField] private DeathSequence deathSequence;
    [SerializeField] private RandomSpawner spawner;

    [Header("Camera refrences")]
    [SerializeField] private CinemachineBasicMultiChannelPerlin cameraShakeComponent;

    //Events
    public UnityEvent<CollectableType> OnPlayerInteractCollectable;
    public UnityEvent OnPlayerTakeDamage;
    public UnityEvent OnPlayerPassingTooCloselyToObstacle;
    public UnityEvent OnZoneCleared;

    //Debug
    [SerializeField] private bool debugStats;

    private float score, scoreMultiplier;
    private float hitStopFactor = 1f;
    private float runStartTime;
    private float bestMultiplier;
    private int zonesCleared;
    private int pickups;
    private GameObject lastHitObject;

    public GameState State { get; private set; }
    public bool IsPlaying => State == GameState.Playing;
    public GameSettings.Difficulty CurrentDifficulty => settings.difficulties[PlayerPrefs.GetInt(DifficultyKey, 1)];
    public float HighScore => PlayerPrefs.GetFloat(HighScoreKey, 0f);

    // The one place game speed lives. Nothing else writes to Time.timeScale.
    public float GameSpeed { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsSlowMotion => GameSpeed < settings.normalSpeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        GameSpeed = settings.normalSpeed;
        ApplyGameSpeed();
    }

    private void Start()
    {
        OnPlayerInteractCollectable.AddListener(GrantCollectableRewards);
        OnPlayerTakeDamage.AddListener(PlayerTakeDamage);
        OnPlayerPassingTooCloselyToObstacle.AddListener(StartCameraShake);
        Player.Instance.OnStaminaReset.AddListener(ResetStamina);
        Player.Instance.OnStaminaDrain.AddListener(DrainStamina);
        Player.Instance.OnStaminaGain.AddListener(GainStamina);
    }

    private void Update()
    {
        if (IsPlaying) DrainScoreMultiplier();
    }

    // ------------------------------------------------------------ Run flow

    public void SetDifficulty(int index)
    {
        PlayerPrefs.SetInt(DifficultyKey, Mathf.Clamp(index, 0, settings.difficulties.Length - 1));
        PlayerPrefs.Save();

        // Changed from the pause menu, it applies from the next zone.
        if (State != GameState.Menu) spawner.ApplyDifficulty(CurrentDifficulty);
    }

    // A phone can close the game at any moment, so a run in progress counts too.
    private void OnApplicationPause(bool isGoingToBackground)
    {
        if (isGoingToBackground) SaveHighScore();
    }

    private void OnApplicationQuit()
    {
        SaveHighScore();
    }

    private bool SaveHighScore()
    {
        if (score <= HighScore) return false;

        PlayerPrefs.SetFloat(HighScoreKey, score);
        PlayerPrefs.Save();
        return true;
    }

    public void BeginIntro()
    {
        State = GameState.Intro;
        spawner.ApplyDifficulty(CurrentDifficulty);
    }

    public void BeginRun()
    {
        State = GameState.Playing;
        runStartTime = Time.unscaledTime;
    }

    public void RestartRun()
    {
        SkipStartScreen = true;
        hitStopFactor = 1f;
        IsPaused = false;
        ApplyGameSpeed();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Called by anything that kills the player on touch.
    public void PlayerHitBy(GameObject hitObject)
    {
        if (!IsPlaying) return;

        // The power surge is an escape window: nothing can hurt the player during it.
        if (ComicEffects.Instance != null && ComicEffects.Instance.IsSurging) return;

        lastHitObject = hitObject;
        OnPlayerTakeDamage.Invoke();
    }

    private RunStats FinishRun()
    {
        bool isNewBest = SaveHighScore();

        return new RunStats
        {
            score = score,
            highScore = HighScore,
            isNewHighScore = isNewBest,
            seconds = Time.unscaledTime - runStartTime,
            zonesCleared = zonesCleared,
            pickups = pickups,
            bestMultiplier = bestMultiplier
        };
    }

    // ------------------------------------------------------------ Game speed

    // Pause and hit-stop sit on top of the speed, so ending either one
    // always lands back on the right value.
    private void ApplyGameSpeed()
    {
        Time.timeScale = IsPaused ? 0f : GameSpeed * hitStopFactor;
    }

    public void SwitchTimeSpeed()
    {
        GameSpeed = Mathf.Approximately(GameSpeed, settings.normalSpeed) ? settings.slowMoSpeed : settings.normalSpeed;
        ApplyGameSpeed();
    }

    public void SetPaused(bool isPaused)
    {
        IsPaused = isPaused;
        ApplyGameSpeed();
    }

    // factor is a share of the current speed. 1 means no hit-stop.
    public void SetHitStop(float factor)
    {
        hitStopFactor = factor;
        ApplyGameSpeed();
    }

    // ------------------------------------------------------------ Rewards

    public void GrantCollectableRewards(CollectableType type)
    {
        if (!IsPlaying) return;

        pickups++;
        UpdateStats(settings.GetCollectableReward(type));
    }

    public void GrantZoneRewards(ZoneType zoneType)
    {
        if (!IsPlaying) return;

        zonesCleared++;
        UpdateStats(settings.GetZoneReward(zoneType));
        OnZoneCleared.Invoke();
    }

    public void UpdateStats(Rewards rewards)
    {
        float staminaBefore = Player.Instance.GetStaminaComponent().GetStamina();
        float scoreBefore = score;

        scoreMultiplier = Mathf.Min(scoreMultiplier + rewards.GetScoreMult(), settings.scoreMultiplierMax);
        bestMultiplier = Mathf.Max(bestMultiplier, scoreMultiplier);

        // The multiplier only ever adds, so points still count at x0.
        score += Mathf.Max(scoreMultiplier, 1f) * rewards.GetScore();
        Player.Instance.GetStaminaComponent().GainStamina(rewards.GetStamina());

        float staminaNow = Player.Instance.GetStaminaComponent().GetStamina();
        Rewards updatedStats = new Rewards(staminaNow, score, scoreMultiplier);

        //Update ui
        UIManager.Instance.OnUpdateStats.Invoke(updatedStats);

        // What was really gained: the multiplier scales score, and stamina stops at its maximum.
        UIManager.Instance.ShowRewardAchievement(
            new Rewards(staminaNow - staminaBefore, score - scoreBefore, rewards.GetScoreMult()));
    }

    private void DrainScoreMultiplier()
    {
        scoreMultiplier = Mathf.Clamp(scoreMultiplier - Time.deltaTime * settings.scoreMultiplierDrain, 0f, settings.scoreMultiplierMax);
        UIManager.Instance.OnUpdateScoreMultUI.Invoke(scoreMultiplier);
    }

    // ------------------------------------------------------------ Player events

    public void ResetStamina()
    {
        GameSpeed = settings.normalSpeed;
        ApplyGameSpeed();
    }

    public void DrainStamina()
    {
        UIManager.Instance.OnUpdateStaminaUI.Invoke(Player.Instance.GetStaminaComponent().GetStamina());
    }

    public void GainStamina()
    {
        UIManager.Instance.OnUpdateStaminaUI.Invoke(Player.Instance.GetStaminaComponent().GetStamina());
    }

    public void PlayerTakeDamage()
    {
        State = GameState.Dead;
        deathSequence.Play(lastHitObject, FinishRun());
    }

    public void StartCameraShake()
    {
        StartCoroutine(AddCameraShake());
    }

    private IEnumerator AddCameraShake()
    {
        float duration = 3f;
        float elapsed = 0f;

        float startFrequency = 10f;
        float targetFrequency = 0.5f;

        cameraShakeComponent.FrequencyGain = startFrequency;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            cameraShakeComponent.FrequencyGain = Mathf.Lerp(
                startFrequency,
                targetFrequency,
                elapsed / duration
            );

            yield return null;
        }

        cameraShakeComponent.FrequencyGain = targetFrequency;
    }

    public float GetScoreMultiplier() { return scoreMultiplier; }
    public Vector3 GetPlayerPosition() { return Player.Instance.transform.position; }
    public bool CanUSeSlowMo() { return Player.Instance.GetStaminaComponent().GetStamina() > 0; }

    // Debug
    private void OnGUI()
    {
        if (!debugStats) return;

        GUIStyle debugStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);

        GUILayout.BeginArea(new Rect(10, 10, 1000, 800), boxStyle);

        GUILayout.Label("=== DEBUG STATS ===", debugStyle);
        GUILayout.Label($"FPS: {1f / Time.unscaledDeltaTime:F0}", debugStyle);
        GUILayout.Label($"Game Speed: {GameSpeed:F1}", debugStyle);
        GUILayout.Label($"Score: {score}", debugStyle);
        GUILayout.Label($"ScoreMult: {scoreMultiplier}", debugStyle);
        GUILayout.Label($"Stamina: {Player.Instance.GetStaminaComponent().GetStamina()}", debugStyle);
        GUILayout.Label($"Player Position: {Player.Instance.transform.position:F1}", debugStyle);

        GUILayout.EndArea();
    }
}
