
using System.Collections.Generic;
using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer[] sourceRenderers;
    [SerializeField] private Material ghostMaterial;

    [Header("Trail Settings")]
    [Min(0.01f)]
    [SerializeField] private float trailLength = 10f;

    [Min(0.01f)]
    [SerializeField] private float ghostSpacing = 5f;

    [Header("Appearance")]
    [Range(0f, 1f)]
    [SerializeField] private float startOpacity = 1f;

    [Header("Fade Settings")]
    [Min(0.01f)]
    [SerializeField] private float ghostFadeDuration = 2f;

    private class Ghost
    {
        public GameObject root;
        public readonly List<Material> materials = new();
        public readonly List<Color> originalColors = new();
        public readonly List<Mesh> bakedMeshes = new();

        public float age;
    }

    private readonly Queue<Ghost> availableGhosts = new();
    private readonly List<Ghost> activeGhosts = new();

    private Vector3 lastPosition;
    private float distanceAccumulator;

    private float cachedTrailLength;
    private float cachedSpacing;

    private static readonly int BaseColor =
        Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (sourceRenderers == null || sourceRenderers.Length == 0)
            sourceRenderers = GetComponentsInChildren<Renderer>();

        RebuildPool();
        lastPosition = transform.position;
    }

    private void Update()
    {
        CheckSettings();
        UpdateTrail();
    }

    private void CheckSettings()
    {
        if (Mathf.Approximately(trailLength, cachedTrailLength) &&
            Mathf.Approximately(ghostSpacing, cachedSpacing))
            return;

        RebuildPool();
        lastPosition = transform.position;
    }

    private void RebuildPool()
    {
        ClearPool();

        trailLength = Mathf.Max(0.01f, trailLength);
        ghostSpacing = Mathf.Max(0.01f, ghostSpacing);
        ghostFadeDuration = Mathf.Max(0.01f, ghostFadeDuration);

        cachedTrailLength = trailLength;
        cachedSpacing = ghostSpacing;

        if (ghostMaterial == null || sourceRenderers == null)
            return;

        int poolSize = Mathf.Max(
            1,
            Mathf.CeilToInt(trailLength / ghostSpacing)
        );

        for (int i = 0; i < poolSize; i++)
            availableGhosts.Enqueue(CreateGhost());

        distanceAccumulator = 0f;
    }

    private Ghost CreateGhost()
    {
        Ghost ghost = new Ghost();

        ghost.root = new GameObject("Ghost");
        ghost.root.SetActive(false);

        foreach (Renderer source in sourceRenderers)
        {
            if (source == null)
                continue;

            GameObject ghostObject = new GameObject(source.name);
            ghostObject.transform.SetParent(ghost.root.transform);

            MeshFilter filter = ghostObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = ghostObject.AddComponent<MeshRenderer>();

            Material material = new Material(ghostMaterial);

            ghost.materials.Add(material);
            ghost.originalColors.Add(
                material.HasProperty(BaseColor)
                    ? material.GetColor(BaseColor)
                    : Color.white
            );

            renderer.sharedMaterial = material;

            if (source is SkinnedMeshRenderer)
            {
                Mesh mesh = new Mesh();
                filter.sharedMesh = mesh;
                ghost.bakedMeshes.Add(mesh);
            }
            else
            {
                MeshFilter sourceFilter = source.GetComponent<MeshFilter>();

                filter.sharedMesh = sourceFilter != null
                    ? sourceFilter.sharedMesh
                    : null;

                ghost.bakedMeshes.Add(null);
            }
        }

        return ghost;
    }

    private void UpdateTrail()
    {
        Vector3 currentPosition = transform.position;

        float movementDistance = Vector3.Distance(
            currentPosition,
            lastPosition
        );

        lastPosition = currentPosition;

        distanceAccumulator += movementDistance;

        // Fade each ghost independently over time.
        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            Ghost ghost = activeGhosts[i];

            ghost.age += Time.deltaTime;

            float fadeProgress = Mathf.Clamp01(
                ghost.age / ghostFadeDuration
            );

            float opacity = Mathf.Lerp(
                startOpacity,
                0f,
                fadeProgress
            );

            SetGhostOpacity(ghost, opacity);

            if (ghost.age >= ghostFadeDuration)
            {
                ghost.root.SetActive(false);

                activeGhosts.RemoveAt(i);
                availableGhosts.Enqueue(ghost);
            }
        }

        // Spawn at consistent distance intervals.
        while (distanceAccumulator >= ghostSpacing)
        {
            distanceAccumulator -= ghostSpacing;
            SpawnGhost();
        }
    }

    private void SpawnGhost()
    {
        if (availableGhosts.Count == 0)
            return;

        Ghost ghost = availableGhosts.Dequeue();

        CaptureGhost(ghost);

        ghost.age = 0f;
        ghost.root.SetActive(true);

        SetGhostOpacity(ghost, startOpacity);

        activeGhosts.Add(ghost);
    }

    private void CaptureGhost(Ghost ghost)
    {
        int childIndex = 0;

        foreach (Renderer source in sourceRenderers)
        {
            if (source == null)
                continue;

            Transform ghostTransform =
                ghost.root.transform.GetChild(childIndex);

            ghostTransform.SetPositionAndRotation(
                source.transform.position,
                source.transform.rotation
            );

            ghostTransform.localScale = source.transform.lossyScale;

            if (source is SkinnedMeshRenderer skinnedSource)
                skinnedSource.BakeMesh(ghost.bakedMeshes[childIndex]);

            childIndex++;
        }
    }

    private void SetGhostOpacity(Ghost ghost, float opacity)
    {
        for (int i = 0; i < ghost.materials.Count; i++)
        {
            Material material = ghost.materials[i];

            if (!material.HasProperty(BaseColor))
                continue;

            Color color = ghost.originalColors[i];
            color.a *= opacity;

            material.SetColor(BaseColor, color);
        }
    }

    private void ClearPool()
    {
        foreach (Ghost ghost in availableGhosts)
            DestroyGhost(ghost);

        foreach (Ghost ghost in activeGhosts)
            DestroyGhost(ghost);

        availableGhosts.Clear();
        activeGhosts.Clear();
    }

    private void DestroyGhost(Ghost ghost)
    {
        foreach (Material material in ghost.materials)
            Destroy(material);

        foreach (Mesh mesh in ghost.bakedMeshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }

        if (ghost.root != null)
            Destroy(ghost.root);
    }

    private void OnDestroy()
    {
        ClearPool();
    }
}
