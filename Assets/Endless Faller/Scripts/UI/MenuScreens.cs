using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// The start, settings and game over screens. While one shows, the in-game HUD is hidden.
// Buttons call the public methods here.
public class MenuScreens : MonoBehaviour
{
    public static MenuScreens Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Settings")]
    [Tooltip("Shows the chosen mode.")]
    [SerializeField] private TextMeshProUGUI difficultyLabel;
    [Tooltip("Shows the chosen mode on the pause menu.")]
    [SerializeField] private TextMeshProUGUI pauseDifficultyLabel;

    [Header("Start")]
    [SerializeField] private TextMeshProUGUI bestScoreText;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI statsText;

    [SerializeField] private PlayerIntro playerIntro;

    private readonly List<GameObject> hudObjects = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Runs after MobileControls has chosen which controls this device shows.
        foreach (Transform child in transform.parent)
        {
            GameObject item = child.gameObject;

            if (item != gameObject && item.activeSelf)
                hudObjects.Add(item);
        }

        ShowDifficulty();
        bestScoreText.text = "BEST " + Mathf.RoundToInt(GameManager.Instance.HighScore);

        if (GameManager.SkipStartScreen)
        {
            GameManager.SkipStartScreen = false;
            Fall();
            return;
        }

        SetHudVisible(false);
        ShowOnly(startPanel);
    }

    // Keyboards get Escape, as the on-screen pause button only shows on touch devices.
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            UIManager.Instance.TogglePauseMenu();
    }

    // ------------------------------------------------------------ Buttons

    public void Fall()
    {
        ShowOnly(null);
        GameManager.Instance.BeginIntro();
        playerIntro.Play(OnIntroFinished);
    }

    private void OnIntroFinished()
    {
        SetHudVisible(true);
        GameManager.Instance.BeginRun();
    }

    public void OpenSettings()
    {
        ShowOnly(settingsPanel);
    }

    public void CloseSettings()
    {
        ShowOnly(startPanel);
    }

    public void SetEasy() { SetDifficulty(0); }
    public void SetMedium() { SetDifficulty(1); }
    public void SetHard() { SetDifficulty(2); }

    private void SetDifficulty(int index)
    {
        GameManager.Instance.SetDifficulty(index);
        ShowDifficulty();
    }

    public void FallAgain()
    {
        GameManager.Instance.RestartRun();
    }

    public void Quit()
    {
        Application.Quit();
    }

    // ------------------------------------------------------------ Showing

    public void SetHudVisible(bool isVisible)
    {
        foreach (GameObject item in hudObjects)
            if (item != null) item.SetActive(isVisible);
    }

    public void ShowGameOver(GameManager.RunStats stats)
    {
        scoreText.text = "SCORE " + Mathf.RoundToInt(stats.score);
        highScoreText.text = (stats.isNewHighScore ? "NEW HIGH SCORE " : "HIGH SCORE ") + Mathf.RoundToInt(stats.highScore);

        statsText.text =
            "TIME " + Mathf.RoundToInt(stats.seconds) + "S\n" +
            "ZONES " + stats.zonesCleared + "\n" +
            "PICKUPS " + stats.pickups + "\n" +
            "BEST MULTI X" + stats.bestMultiplier.ToString("0.0");

        ShowOnly(gameOverPanel);
    }

    public void Resume()
    {
        UIManager.Instance.TogglePauseMenu();
    }

    private void ShowDifficulty()
    {
        string mode = "MODE: " + GameManager.Instance.CurrentDifficulty.name.ToUpperInvariant();

        difficultyLabel.text = mode;
        pauseDifficultyLabel.text = mode;
    }

    private void ShowOnly(GameObject panel)
    {
        startPanel.SetActive(panel == startPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        gameOverPanel.SetActive(panel == gameOverPanel);
    }
}
