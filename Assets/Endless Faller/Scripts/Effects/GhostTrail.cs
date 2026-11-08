using System.Collections.Generic;
using UnityEngine;

// Echoes of the player, left behind only when it moves. Each one stays where it was
// dropped, glitches, and slowly fades away. Use it with the Custom/GhostEcho material.
// Real time, so the game's 3x speed doesn't rush them, and they hold still while paused.
public class GhostTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer[] sourceRenderers;
    [Tooltip("A Custom/GhostEcho material.")]
    [SerializeField] private Material ghostMaterial;

    [Header("Echoes")]
    [Tooltip("An echo drops each time the player moves this far. Standing still leaves none.")]
    [SerializeField, Min(0.1f)] private float moveSpacing = 6f;
    [Tooltip("Seconds an echo takes to fade away.")]
    [SerializeField, Min(0.1f)] private float lifetime = 1.3f;
    [Tooltip("Most echoes at once. When it runs out, the oldest one is reused.")]
    [SerializeField, Min(1)] private int maxEchoes = 40;
    [Tooltip("Size at the end of its life, against the player. 1 keeps it the same.")]
    [SerializeField, Min(0.1f)] private float endScale = 1f;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");
    private static readonly int SeedID = Shader.PropertyToID("_Seed");

    private class Echo
    {
        public GameObject root;
        public readonly List<Transform> parts = new();
        public readonly List<Vector3> sizes = new();
        public readonly List<MeshRenderer> renderers = new();
        public float seed;
        public float age;
    }

    private readonly Queue<Echo> free = new();
    private readonly List<Echo> active = new();
    private MaterialPropertyBlock block;
    private Vector3 lastPosition;
    private float moved;

    private void Awake()
    {
        if (sourceRenderers == null || sourceRenderers.Length == 0)
            sourceRenderers = GetComponentsInChildren<Renderer>();

        block = new MaterialPropertyBlock();

        if (ghostMaterial == null) return;

        for (int i = 0; i < maxEchoes; i++) free.Enqueue(CreateEcho());

        lastPosition = transform.position;
    }

    private Echo CreateEcho()
    {
        var echo = new Echo { root = new GameObject("Echo") };
        echo.root.SetActive(false);

        foreach (Renderer source in sourceRenderers)
        {
            if (source == null || !source.TryGetComponent(out MeshFilter sourceFilter)) continue;

            var part = new GameObject(source.name);
            part.transform.SetParent(echo.root.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;

            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ghostMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            echo.parts.Add(part.transform);
            echo.sizes.Add(Vector3.one);
            echo.renderers.Add(renderer);
        }

        return echo;
    }

    private void Update()
    {
        if (ghostMaterial == null) return;

        GameManager game = GameManager.Instance;

        // No echoes on the menus, in the intro, or once the run has ended.
        if (game == null || !game.IsPlaying)
        {
            ClearAll();
            lastPosition = transform.position;
            return;
        }

        if (game.IsPaused) return;

        float deltaTime = Time.unscaledDeltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Echo echo = active[i];
            echo.age += deltaTime;

            if (echo.age >= lifetime)
            {
                Release(i);
                continue;
            }

            Animate(echo, deltaTime);
        }

        moved += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (moved >= moveSpacing)
        {
            moved = 0f;
            Spawn();
        }
    }

    private void Spawn()
    {
        // When the pool runs out, the oldest echo is reused.
        Echo echo = free.Count > 0 ? free.Dequeue() : TakeOldest();
        if (echo == null) return;

        int part = 0;

        foreach (Renderer source in sourceRenderers)
        {
            if (source == null || part >= echo.parts.Count) continue;

            echo.parts[part].SetPositionAndRotation(source.transform.position, source.transform.rotation);
            echo.sizes[part] = source.transform.lossyScale;
            echo.parts[part].localScale = echo.sizes[part];
            part++;
        }

        echo.seed = Random.Range(0f, 100f);
        echo.age = 0f;
        echo.root.SetActive(true);
        active.Add(echo);

        Animate(echo, 0f);
    }

    private void Animate(Echo echo, float deltaTime)
    {
        float life = echo.age / lifetime;
        float fade = 1f - life * life;
        float size = Mathf.Lerp(1f, endScale, life);

        block.SetFloat(FadeID, fade);
        block.SetFloat(SeedID, echo.seed);

        for (int i = 0; i < echo.parts.Count; i++)
        {
            echo.parts[i].localScale = echo.sizes[i] * size;
            echo.renderers[i].SetPropertyBlock(block);
        }
    }

    private Echo TakeOldest()
    {
        if (active.Count == 0) return null;

        Echo oldest = active[0];
        active.RemoveAt(0);
        return oldest;
    }

    private void Release(int index)
    {
        Echo echo = active[index];
        active.RemoveAt(index);
        echo.root.SetActive(false);
        free.Enqueue(echo);
    }

    private void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) Release(i);
    }

    private void OnDestroy()
    {
        foreach (Echo echo in active) if (echo.root != null) Destroy(echo.root);
        foreach (Echo echo in free) if (echo.root != null) Destroy(echo.root);
    }
}
