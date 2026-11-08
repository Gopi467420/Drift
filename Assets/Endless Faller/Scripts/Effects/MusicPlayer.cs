using UnityEngine;

// Plays the game's music: the calm loop on the start, pause and game over screens, and
// the run loop from the moment the player falls. The run loop sinks in slow motion, holds while paused and
// winds down like a tape when the player dies.
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [Header("Run")]
    [SerializeField] private AudioClip runLoop;
    [Range(0f, 1f)][SerializeField] private float volume = 0.7f;

    [Header("Start, Pause and Game Over")]
    [SerializeField] private AudioClip menuLoop;
    [Range(0f, 1f)][SerializeField] private float menuVolume = 0.6f;
    [Tooltip("Seconds the menu music takes to fade in or out.")]
    [SerializeField, Min(0.01f)] private float menuFadeTime = 0.5f;

    [Header("Pitch")]
    [Tooltip("Pitch while the game is in slow motion. 1 is normal.")]
    [Range(0.1f, 1f)][SerializeField] private float slowMotionPitch = 0.6f;
    [Tooltip("Seconds to slide in or out of slow motion.")]
    [SerializeField, Min(0.01f)] private float pitchSlideTime = 0.25f;
    [Tooltip("Seconds the music takes to wind down on death.")]
    [SerializeField, Min(0.01f)] private float deathStopTime = 0.5f;

    private AudioSource source;
    private AudioSource menuSource;
    private bool hasStarted;
    private bool wasPaused;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.clip = runLoop;
        source.loop = true;
        source.playOnAwake = false;

        menuSource = gameObject.AddComponent<AudioSource>();
        menuSource.clip = menuLoop;
        menuSource.loop = true;
        menuSource.playOnAwake = false;
        menuSource.volume = 0f;
    }

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        bool isOnMenu = game.State == GameManager.GameState.Menu;
        bool isOnGameOver = MenuScreens.Instance != null && MenuScreens.Instance.IsShowingGameOver;

        FadeMenuMusic((isOnMenu && !SplashScreen.IsShowing) || game.IsPaused || isOnGameOver);

        if (isOnMenu) return;

        source.volume = volume;

        if (game.State == GameManager.GameState.Dead)
        {
            WindDown();
            return;
        }

        if (!hasStarted)
        {
            hasStarted = true;
            source.Play();
        }

        if (game.IsPaused != wasPaused)
        {
            wasPaused = game.IsPaused;

            if (wasPaused) source.Pause();
            else source.UnPause();
        }

        float pitch = game.IsSlowMotion ? slowMotionPitch : 1f;
        source.pitch = Mathf.MoveTowards(source.pitch, pitch, Time.unscaledDeltaTime / pitchSlideTime);
    }

    private void FadeMenuMusic(bool isWanted)
    {
        if (isWanted && !menuSource.isPlaying) menuSource.Play();

        float target = isWanted ? menuVolume : 0f;
        menuSource.volume = Mathf.MoveTowards(menuSource.volume, target, menuVolume * Time.unscaledDeltaTime / menuFadeTime);

        if (!isWanted && menuSource.volume <= 0f && menuSource.isPlaying) menuSource.Stop();
    }

    private void WindDown()
    {
        if (!source.isPlaying) return;

        source.pitch = Mathf.MoveTowards(source.pitch, 0f, Time.unscaledDeltaTime / deathStopTime);

        if (source.pitch <= 0f) source.Stop();
    }
}
