using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The start, settings, how to play, leaderboard and game over screens. While one shows, the in-game HUD is hidden.
// Buttons call the public methods here.
public class MenuScreens : MonoBehaviour
{
    public static MenuScreens Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Settings")]
    [Tooltip("Shows the chosen mode.")]
    [SerializeField] private TextMeshProUGUI difficultyLabel;
    [Tooltip("Shows the chosen mode on the pause menu.")]
    [SerializeField] private TextMeshProUGUI pauseDifficultyLabel;

    [Header("Start")]
    [SerializeField] private TextMeshProUGUI bestScoreText;

    [Header("How To Play")]
    [SerializeField] private TextMeshProUGUI howToPlayText;
    [Tooltip("Says FALL when the screen leads into a run, BACK when it leads to the menu.")]
    [SerializeField] private TextMeshProUGUI howToPlayCloseLabel;
    [TextArea(6, 12)][SerializeField] private string keyboardGuide;
    [TextArea(6, 12)][SerializeField] private string touchGuide;
    [TextArea(6, 12)][SerializeField] private string gamepadGuide;

    [Header("Leaderboard")]
    [SerializeField] private TextMeshProUGUI leaderboardModeLabel;
    [SerializeField] private TextMeshProUGUI leaderboardList;
    [Tooltip("Where the player stands after a run, on the game over screen.")]
    [SerializeField] private TextMeshProUGUI rankText;
    [Tooltip("Only shows when the run made the board.")]
    [SerializeField] private TMP_InputField nameField;
    [Tooltip("The ADD YOUR NAME line, shown with the name field.")]
    [SerializeField] private GameObject namePrompt;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI statsText;

    [SerializeField] private PlayerIntro playerIntro;
    [SerializeField] private SplashScreen splashScreen;

    private const string SeenHowToPlayKey = "SeenHowToPlay";

    private readonly List<GameObject> hudObjects = new();
    private bool isHowToPlayBeforeRun;
    private GameObject leaderboardReturnPanel;
    private int leaderboardPage;
    private int worldFallbackPage;
    private string lastRunMode;
    private int lastRunRank = -1;

    public bool IsShowingGameOver => gameOverPanel.activeSelf;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        nameField.onEndEdit.AddListener(SaveRunName);

        // Signed in early, so opening the board or ending a run has no wait.
        OnlineLeaderboard.Connect();

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

        if (splashScreen.HasPlayed)
        {
            ShowOnly(startPanel);
            return;
        }

        ShowOnly(null);
        splashScreen.Play(() => ShowOnly(startPanel));
    }

    // Keyboards get Escape and gamepads get Start, as the on-screen pause button only shows on touch devices.
    private void Update()
    {
        bool isPausePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.startButton.wasPressedThisFrame) isPausePressed = true;
            if (Gamepad.current.buttonEast.wasPressedThisFrame) GoBack();
        }

        if (isPausePressed) UIManager.Instance.TogglePauseMenu();

        KeepGamepadFocus();
    }

    // A gamepad has no pointer, so some button on the open screen always has to be selected.
    private void KeepGamepadFocus()
    {
        if (EventSystem.current == null || !InputManager.HasGamepad) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.activeInHierarchy) return;

        GameObject panel = OpenPanel();
        if (panel == null) return;

        Button first = panel.GetComponentInChildren<Button>();
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    private GameObject OpenPanel()
    {
        if (UIManager.Instance.PauseMenu.activeSelf) return UIManager.Instance.PauseMenu;
        if (startPanel.activeSelf) return startPanel;
        if (settingsPanel.activeSelf) return settingsPanel;
        if (howToPlayPanel.activeSelf) return howToPlayPanel;
        if (leaderboardPanel.activeSelf) return leaderboardPanel;
        if (gameOverPanel.activeSelf) return gameOverPanel;
        return null;
    }

    // The B button.
    private void GoBack()
    {
        if (UIManager.Instance.PauseMenu.activeSelf) Resume();
        else if (settingsPanel.activeSelf) CloseSettings();
        else if (leaderboardPanel.activeSelf) CloseLeaderboard();
        else if (howToPlayPanel.activeSelf && !isHowToPlayBeforeRun) CloseHowToPlay();
    }

    // ------------------------------------------------------------ Buttons

    public void Fall()
    {
        // The first run opens with the guide, and closing it starts the fall.
        if (PlayerPrefs.GetInt(SeenHowToPlayKey, 0) == 0)
        {
            ShowHowToPlay(true);
            return;
        }

        StartIntro();
    }

    private void StartIntro()
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

    public void OpenHowToPlay()
    {
        ShowHowToPlay(false);
    }

    public void CloseHowToPlay()
    {
        PlayerPrefs.SetInt(SeenHowToPlayKey, 1);
        PlayerPrefs.Save();

        if (isHowToPlayBeforeRun) StartIntro();
        else ShowOnly(startPanel);
    }

    private void ShowHowToPlay(bool isBeforeRun)
    {
        isHowToPlayBeforeRun = isBeforeRun;

        howToPlayText.text = InputManager.HasGamepad ? gamepadGuide
            : MobileControls.IsTouchDevice ? touchGuide
            : keyboardGuide;
        howToPlayCloseLabel.text = isBeforeRun ? "FALL" : "BACK";

        ShowOnly(howToPlayPanel);
    }

    public void OpenLeaderboard()
    {
        leaderboardReturnPanel = gameOverPanel.activeSelf ? gameOverPanel : startPanel;
        leaderboardPage = WorldPage;
        worldFallbackPage = GameManager.Instance.CurrentDifficultyIndex;

        ShowOnly(leaderboardPanel);
        ShowLeaderboard();
    }

    public void CloseLeaderboard()
    {
        ShowOnly(leaderboardReturnPanel);
    }

    public void NextLeaderboardMode() { ChangeLeaderboardMode(1); }
    public void PreviousLeaderboardMode() { ChangeLeaderboardMode(-1); }

    // One page per mode for the scores on this device, then the world board after them.
    private int WorldPage => GameManager.Instance.DifficultyCount;

    private void ChangeLeaderboardMode(int step)
    {
        int count = WorldPage + 1;

        leaderboardPage = (leaderboardPage + step + count) % count;

        // With no connection the world page is skipped, carrying on in the same direction.
        worldFallbackPage = step > 0 ? 0 : WorldPage - 1;

        ShowLeaderboard();
    }

    public void SetDifficulty(int index)
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
        WebPage.Quit();
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

        ShowRank(stats);
        ShowOnly(gameOverPanel);
    }

    // The run goes on the board at once, so nothing is lost if the name is never typed.
    private void ShowRank(GameManager.RunStats stats)
    {
        int finalScore = Mathf.RoundToInt(stats.score);
        string mode = stats.modeName.ToUpperInvariant();

        lastRunMode = stats.modeName;
        lastRunRank = Leaderboard.Add(stats.modeName, Leaderboard.PlayerName, finalScore);

        if (finalScore > 0) OnlineLeaderboard.Submit(stats.modeName, finalScore);

        nameField.gameObject.SetActive(lastRunRank >= 0);
        namePrompt.SetActive(lastRunRank >= 0);
        nameField.SetTextWithoutNotify(Leaderboard.PlayerName);

        if (lastRunRank >= 0)
            rankText.text = "RANK " + (lastRunRank + 1) + " ON " + mode;
        else
            rankText.text = (Leaderboard.GetScoreToBeat(stats.modeName) + 1 - finalScore) + " MORE FOR THE " + mode + " BOARD";
    }

    private void SaveRunName(string playerName)
    {
        Leaderboard.PlayerName = playerName;
        nameField.SetTextWithoutNotify(Leaderboard.PlayerName);

        if (lastRunRank >= 0) Leaderboard.Rename(lastRunMode, lastRunRank, Leaderboard.PlayerName);

        OnlineLeaderboard.UpdateName();
    }

    private void ShowLeaderboard()
    {
        if (leaderboardPage == WorldPage)
        {
            leaderboardModeLabel.text = "WORLD";
            leaderboardList.text = "LOADING";
            FetchWorldBoard();
            return;
        }

        string mode = GameManager.Instance.GetDifficultyName(leaderboardPage);

        leaderboardModeLabel.text = mode.ToUpperInvariant() + " LOCAL";
        ShowRows(mode, Leaderboard.GetTop(mode), false);
    }

    private async void FetchWorldBoard()
    {
        List<Leaderboard.Entry> world = await OnlineLeaderboard.GetTop();

        // By now the scene may have reloaded, or the player may have moved on.
        if (this == null || !leaderboardPanel.activeSelf || leaderboardPage != WorldPage) return;

        if (world != null)
        {
            ShowRows(null, world, true);
            return;
        }

        // No connection, so fall back to the scores on this device.
        leaderboardPage = worldFallbackPage;
        ShowLeaderboard();
    }

    private void ShowRows(string mode, List<Leaderboard.Entry> entries, bool isWorld)
    {
        var rows = new System.Text.StringBuilder();

        for (int i = 0; i < Leaderboard.MaxEntries; i++)
        {
            bool isHighlighted = isWorld
                ? i < entries.Count && entries[i].isPlayer
                : mode == lastRunMode && i == lastRunRank;

            if (isHighlighted) rows.Append("<color=#FFE21A>");

            rows.Append(i + 1).Append("<pos=10%>");

            if (i >= entries.Count) rows.Append("---");
            else if (isWorld) rows.Append(entries[i].name).Append("<pos=46%>").Append(entries[i].mode).Append("<pos=74%>").Append(entries[i].score);
            else rows.Append(entries[i].name).Append("<pos=70%>").Append(entries[i].score);

            if (isHighlighted) rows.Append("</color>");

            rows.Append('\n');
        }

        leaderboardList.text = rows.ToString();
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
        howToPlayPanel.SetActive(panel == howToPlayPanel);
        leaderboardPanel.SetActive(panel == leaderboardPanel);
        gameOverPanel.SetActive(panel == gameOverPanel);
    }
}
