using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The reward feed under the player. Sits on the RewardDisplay text, which acts as the
// template and marks where the newest line goes. Every new reward enters there and
// pushes the older ones up. Lines never time out: the list stays on screen, and the
// oldest line only leaves when a new one needs its place.
// Gains of the same kind landing right after each other add up on one line, so a run
// of coins counts up rather than flooding the list.
// Uses unscaled time, because the game runs at timeScale 3 and pauses at 0.
public class RewardPopups : MonoBehaviour
{
    public enum Kind { Score, Multiplier, Stamina }

    [SerializeField] private Color scoreColor = new Color(1f, 0.93f, 0.1f);
    [SerializeField] private Color multiplierColor = new Color(1f, 0.25f, 0.65f);
    [SerializeField] private Color staminaColor = new Color(0.4f, 1f, 0.35f);

    [Header("Feed")]
    [Tooltip("Most lines on screen at once. A new line pushes out the oldest.")]
    [SerializeField, Range(1, 8)] private int maxLines = 4;
    [Tooltip("A gain this soon after the newest line, of the same kind, adds to it.")]
    [SerializeField, Min(0f)] private float mergeWindow = 0.5f;
    [Tooltip("Distance between the lines, in canvas units.")]
    [SerializeField] private float lineSpacing = 50f;
    [Tooltip("How quickly lines slide to their new place.")]
    [SerializeField, Min(1f)] private float scrollSpeed = 14f;

    [Header("Line animation")]
    [SerializeField, Min(0.01f)] private float popTime = 0.2f;
    [SerializeField, Min(1f)] private float popScale = 1.5f;
    [SerializeField] private float maxTilt = 3f;

    [Header("HUD punch")]
    [Tooltip("How much the score or multiplier value grows on a gain.")]
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField, Min(0.01f)] private float punchTime = 0.25f;

    private class Line
    {
        public TextMeshProUGUI text;
        public Kind kind;
        public float value;
        public float age;
        public float tilt;
        public float height;
    }

    private class Punch
    {
        public RectTransform target;
        public Vector3 baseScale;
        public float age = -1f;
    }

    private readonly List<Line> shown = new();   // newest first
    private readonly Stack<Line> free = new();
    private readonly Punch[] punches = { new Punch(), new Punch(), new Punch() };
    private Vector2 home;
    private bool isReady;

    public void Show(Kind kind, float amount, RectTransform hudValue = null)
    {
        if (amount <= 0f) return;

        Prepare();

        Line line;

        if (shown.Count > 0 && shown[0].kind == kind && shown[0].age < mergeWindow)
        {
            line = shown[0];
            line.value += amount;
            line.age = popTime * 0.35f;
        }
        else
        {
            line = Take();
            line.kind = kind;
            line.value = amount;
            line.age = 0f;
            line.height = -lineSpacing;
            line.text.color = ColorOf(kind);
            shown.Insert(0, line);
        }

        line.tilt = Random.Range(-maxTilt, maxTilt);

        switch (kind)
        {
            case Kind.Score: line.text.SetText("SCORE +{0:0}", line.value); break;
            case Kind.Multiplier: line.text.SetText("MULTI +{0:1}", line.value); break;
            default: line.text.SetText("STAMINA +{0:0}", line.value); break;
        }

        if (hudValue != null) StartPunch((int)kind, hudValue);

        Tick(0f);
    }

    public void Clear()
    {
        for (int i = shown.Count - 1; i >= 0; i--) Release(i);
    }

    private void Update()
    {
        Tick(Time.unscaledDeltaTime);
    }

    public void Tick(float deltaTime)
    {
        for (int i = shown.Count - 1; i >= 0; i--)
        {
            Line line = shown[i];
            line.age += deltaTime;

            float pop = Mathf.Clamp01(line.age / popTime);

            // Older lines sit higher, and slide up when a new one arrives.
            line.height = Mathf.Lerp(line.height, i * lineSpacing, 1f - Mathf.Exp(-scrollSpeed * deltaTime));

            RectTransform rect = line.text.rectTransform;
            rect.anchoredPosition = home + Vector2.up * line.height;
            rect.localScale = Vector3.one * Mathf.LerpUnclamped(popScale, 1f, BackOut(pop));
            rect.localRotation = Quaternion.Euler(0f, 0f, line.tilt * (1f + (1f - pop) * 1.5f));
        }

        foreach (Punch punch in punches) Animate(punch, deltaTime);
    }

    private Line Take()
    {
        if (shown.Count >= maxLines) Release(shown.Count - 1);

        Line line = free.Pop();
        line.text.gameObject.SetActive(true);
        return line;
    }

    private void Release(int index)
    {
        Line line = shown[index];
        shown.RemoveAt(index);
        line.text.gameObject.SetActive(false);
        free.Push(line);
    }

    private Color ColorOf(Kind kind)
    {
        switch (kind)
        {
            case Kind.Score: return scoreColor;
            case Kind.Multiplier: return multiplierColor;
            default: return staminaColor;
        }
    }

    private void StartPunch(int index, RectTransform target)
    {
        Punch punch = punches[index];

        if (punch.target != target)
        {
            punch.target = target;
            punch.baseScale = target.localScale;
        }

        punch.age = 0f;
    }

    private void Animate(Punch punch, float deltaTime)
    {
        if (punch.age < 0f || punch.target == null) return;

        punch.age += deltaTime;
        float t = Mathf.Clamp01(punch.age / punchTime);

        punch.target.localScale = punch.baseScale * Mathf.Lerp(punchScale, 1f, BackOut(t));

        if (t >= 1f) punch.age = -1f;
    }

    // Runs past 1 and settles back, which gives the slam its bounce.
    private static float BackOut(float t)
    {
        const float overshoot = 2.2f;
        t -= 1f;
        return 1f + t * t * ((overshoot + 1f) * t + overshoot);
    }

    private void Prepare()
    {
        if (isReady) return;
        isReady = true;

        var template = GetComponent<TextMeshProUGUI>();
        template.enabled = false;
        home = template.rectTransform.anchoredPosition;

        for (int i = 0; i < maxLines; i++)
        {
            TextMeshProUGUI clone = Instantiate(template, transform.parent);
            clone.name = "Reward Line " + i;
            clone.enabled = true;
            Destroy(clone.GetComponent<RewardPopups>());
            clone.gameObject.SetActive(false);

            free.Push(new Line { text = clone });
        }
    }
}
