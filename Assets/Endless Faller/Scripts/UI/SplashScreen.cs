using System;
using System.Collections;
using TMPro;
using UnityEngine;

// The line shown once when the game opens, on the black and red look of the death
// screen. It stays away when a run restarts, as that reloads the scene.
public class SplashScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI line;
    [SerializeField] private DeathSequence deathSequence;

    [Header("Timing (seconds of real time)")]
    [Tooltip("Black and red alone, before the line appears.")]
    [SerializeField, Min(0f)] private float leadTime = 0.45f;
    [SerializeField, Min(0.05f)] private float popTime = 0.16f;
    [Tooltip("How long the line stays on screen.")]
    [SerializeField, Min(0.1f)] private float holdTime = 1.8f;
    [SerializeField, Min(0.05f)] private float flickerTime = 0.3f;
    [Tooltip("How many times per second the line flickers on its way out.")]
    [SerializeField] private float flickerRate = 24f;
    [Tooltip("Largest tilt of the line, in degrees.")]
    [SerializeField] private float maxTilt = 2f;

    private static bool hasPlayed;

    public static bool IsShowing { get; private set; }
    public bool HasPlayed => hasPlayed;

    public void Play(Action onFinished)
    {
        hasPlayed = true;
        IsShowing = true;
        StartCoroutine(Run(onFinished));
    }

    private void OnDisable()
    {
        IsShowing = false;
    }

    private IEnumerator Run(Action onFinished)
    {
        RectTransform rect = line.rectTransform;

        deathSequence.EnterBlack();

        yield return new WaitForSecondsRealtime(leadTime);

        line.gameObject.SetActive(true);
        rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-maxTilt, maxTilt));

        float age = 0f;

        while (age < holdTime)
        {
            age += Time.unscaledDeltaTime;

            float pop = Mathf.Clamp01(age / popTime);
            rect.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, pop * pop);

            yield return null;
        }

        age = 0f;

        while (age < flickerTime)
        {
            age += Time.unscaledDeltaTime;
            line.enabled = Mathf.FloorToInt(age * flickerRate) % 3 != 0;
            yield return null;
        }

        line.gameObject.SetActive(false);

        yield return deathSequence.LeaveBlack();

        IsShowing = false;
        onFinished?.Invoke();
    }
}
