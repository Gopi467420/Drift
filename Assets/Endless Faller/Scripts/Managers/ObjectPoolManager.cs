using System.Collections.Generic;
using UnityEngine;

public enum PoolCategory
{
    Collectable,        // the base coin
    SpecialCollectable,
    Obstacle,           // critical zone obstacles
    SpecialObstacle,
    EndZoneAsset,
    ZoneEnd,
    ZoneCloud           // reward clouds around each zone end
}

// Owns every prefab list and every pool. Creates one ObjectPool per prefab
// (prewarmed at start), and hands out copies. RandomSpawner only asks it for things.
//
//   GameObject prefab = pools.PickRandom(PoolCategory.Obstacle);   // WHAT
//   pools.Spawn(prefab, position);                                   // a free copy of it
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Collectables")]
    [SerializeField] private GameObject baseCollectable;
    [Tooltip("One replaces a coin on each path, and one goes in every special obstacle slot.")]
    [SerializeField] private GameObject[] specialCollectables;

    [Header("Obstacles")]
    [Tooltip("Critical zone obstacles.")]
    [SerializeField] private GameObject[] obstacles;
    [Tooltip("Obstacles with a SpecialObstacle component and reward slots.")]
    [SerializeField] private GameObject[] specialObstacles;
    [Tooltip("Low-detail scenery placed around each zone end (EndZoneAsset component).")]
    [SerializeField] private GameObject[] endZoneAssets;
    [Tooltip("Reward clouds around each zone end. No collider, the player passes through.")]
    [SerializeField] private GameObject[] zoneClouds;
    [SerializeField] private ZoneEnd zoneEnd;

    [Header("Prewarm (copies created at start, per prefab)")]
    [SerializeField, Min(0)] private int collectablePrewarm = 10;
    [SerializeField, Min(0)] private int obstaclePrewarm = 30;
    [SerializeField, Min(0)] private int specialPrewarm = 3;
    [SerializeField, Min(0)] private int endZonePrewarm = 12;
    [SerializeField, Min(0)] private int zoneCloudPrewarm = 32;

    [Tooltip("Keep free copies under a child object per pool, so the Hierarchy stays tidy.")]
    [SerializeField] private bool groupFreeCopies = true;

    private readonly Dictionary<GameObject, ObjectPool> poolByPrefab = new();
    private readonly List<ObjectPool> pools = new();

    // Every pool, for Locomotion to loop over the active copies.
    public List<ObjectPool> Pools => pools;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Prewarm(baseCollectable, collectablePrewarm);
        Prewarm(specialCollectables, collectablePrewarm);
        Prewarm(obstacles, obstaclePrewarm);
        Prewarm(specialObstacles, specialPrewarm);
        Prewarm(endZoneAssets, endZonePrewarm);
        Prewarm(zoneClouds, zoneCloudPrewarm);
        if (zoneEnd != null) Prewarm(zoneEnd.gameObject, 2);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------ Asking for prefabs

    public bool Has(PoolCategory category)
    {
        return PickRandom(category) != null;
    }

    // A random prefab from the category's list (null if the list is empty).
    public GameObject PickRandom(PoolCategory category)
    {
        switch (category)
        {
            case PoolCategory.Collectable: return baseCollectable;
            case PoolCategory.ZoneEnd: return zoneEnd != null ? zoneEnd.gameObject : null;
            default: return PickFrom(GetList(category));
        }
    }

    // ------------------------------------------------------------ Spawning

    public GameObject Spawn(GameObject prefab, Vector3 position)
    {
        return Spawn(prefab, position, Quaternion.identity);
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        return GetPool(prefab).Spawn(position, rotation);
    }

    public GameObject SpawnRandom(PoolCategory category, Vector3 position)
    {
        return Spawn(PickRandom(category), position);
    }

    public static bool Despawn(GameObject target)
    {
        return ObjectPool.Despawn(target);
    }

    // ------------------------------------------------------------ Internals

    private GameObject[] GetList(PoolCategory category)
    {
        switch (category)
        {
            case PoolCategory.SpecialCollectable: return specialCollectables;
            case PoolCategory.Obstacle: return obstacles;
            case PoolCategory.SpecialObstacle: return specialObstacles;
            case PoolCategory.EndZoneAsset: return endZoneAssets;
            case PoolCategory.ZoneCloud: return zoneClouds;
            default: return null;
        }
    }

    private static GameObject PickFrom(GameObject[] list)
    {
        if (list == null || list.Length == 0) return null;

        // Skip empty slots in the list.
        for (int tries = 0; tries < 4; tries++)
        {
            GameObject prefab = list[Random.Range(0, list.Length)];
            if (prefab != null) return prefab;
        }

        foreach (GameObject prefab in list)
            if (prefab != null) return prefab;

        return null;
    }

    // One pool per prefab, created the first time it is needed.
    private ObjectPool GetPool(GameObject prefab)
    {
        if (!poolByPrefab.TryGetValue(prefab, out ObjectPool pool))
        {
            Transform container = null;
            if (groupFreeCopies)
            {
                container = new GameObject(prefab.name + " Pool").transform;
                container.SetParent(transform, false);
            }

            pool = new ObjectPool(prefab, 0, container);
            poolByPrefab.Add(prefab, pool);
            pools.Add(pool);
        }
        return pool;
    }

    private void Prewarm(GameObject prefab, int count)
    {
        if (prefab != null) GetPool(prefab).Prewarm(count);
    }

    private void Prewarm(GameObject[] prefabs, int count)
    {
        if (prefabs == null) return;
        foreach (GameObject prefab in prefabs) Prewarm(prefab, count);
    }
}
