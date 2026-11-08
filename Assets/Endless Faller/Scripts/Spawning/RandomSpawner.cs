using System.Collections;
using System.Collections.Generic;
using Enums;
using UnityEngine;

// Only cares about WHERE and WHEN. Every spawnInterval seconds it fills the next zone below the player:
//   1. special obstacles (yes / no roll) + a random special collectable in each reward slot
//   2. a collectable path (coins + one special collectable)
//   3. critical zone obstacles, placed so they don't overlap anything on blockingMask
//      (spread over a few frames, so there is no hitch)
//   4. the ZoneEnd trigger at the bottom of the zone
//   5. fast edge obstacles in a ring around the whole zone, so going wide is no way around it
//   6. reward clouds around the zone end
// WHAT to spawn comes from ObjectPoolManager (it owns every prefab list and pool).
public class RandomSpawner : MonoBehaviour
{
    [Header("Pools")]
    [Tooltip("Leave empty to use ObjectPoolManager.Instance.")]
    [SerializeField] private ObjectPoolManager poolManager;

    [Header("Special Obstacles")]
    [Tooltip("Chance (per try) that a special obstacle spawns in a zone.")]
    [Range(0f, 1f)][SerializeField] private float specialObstacleChance = 0.5f;
    [Tooltip("How many times per zone we roll the chance above.")]
    [Range(0, 10)][SerializeField] private int maxSpecialObstaclesPerZone = 2;
    [Tooltip("Extra empty space kept around a special obstacle, so the holes are reachable.")]
    [Range(0, 300)][SerializeField] private float specialObstacleClearance = 120f;

    [Header("End Zone Assets")]
    [Range(0, 300)][SerializeField] private int endZoneAssetsPerZone = 12;
    [Tooltip("The ring starts at Spawn Radius (outside the play area) and is this wide.")]
    [SerializeField] private float endZoneRingWidth = 400f;
    [Tooltip("How far the ring reaches below the zone end. It also covers the whole zone above it.")]
    [SerializeField] private float endZoneHeight = 300f;

    [Header("Reward Clouds")]
    [Range(0, 40)][SerializeField] private int zoneCloudsPerZone = 14;
    [Tooltip("Clouds stay at least this far from the player's line, so a gap opens to fall through.")]
    [SerializeField] private float zoneCloudMinRadius = 320f;
    [SerializeField] private float zoneCloudMaxRadius = 900f;
    [Tooltip("Vertical spread of the clouds around the zone end.")]
    [SerializeField] private float zoneCloudHeight = 220f;

    [Header("Zone Settings")]
    [SerializeField] private LayerMask blockingMask;
    [Tooltip("Tries, not a promise: a try is skipped when it finds no free spot.")]
    [Range(1, 1000)][SerializeField] private int obstaclesPerZone = 300;
    [Tooltip("Copies of every obstacle placed first, biggest list entries first. Without this, small ones fill the zone and big ones find no room.")]
    [Range(0, 10)][SerializeField] private int minOfEachObstacle = 2;
    [SerializeField] private float spawnInterval = 15f;
    [Range(1, 3000)][SerializeField] private float spawnRadius = 450f;
    [Range(1000, 5000)][SerializeField] private float zoneDepth = 1000f;
    [Tooltip("How far below the player the zone starts.")]
    [Range(1000, 5000)][SerializeField] private float spawnDistance = 1708f;
    [SerializeField, Min(1)] private int maxPlacementAttempts = 10;
    [Tooltip("Obstacles placed per frame. Lower = smoother, but the zone takes a few more frames to fill. 0 = all in one frame.")]
    [SerializeField, Min(0)] private int obstaclesPerFrame = 30;

    [Header("Collectable Path Settings")]
    [Range(1, 30)][SerializeField] private int collectablesPerPath = 6;
    [Range(0, 12)][SerializeField] private int coinPathsPerZone = 1;
    [Tooltip("Size of cluster / spiral shapes.")]
    [SerializeField] private float pathShapeRadius = 100f;

    [Header("Debug")]
    [SerializeField] private bool spawnObstacle = true;
    [SerializeField] private bool spawnCollectable = true;
    [SerializeField] private bool spawnSpecialObstacle = true;
    [SerializeField] private bool spawnEndZoneAssets = true;
    [SerializeField] private bool spawnZoneClouds = true;
    [SerializeField] private ZoneType nextZone;
    [Tooltip("Shows the last path that was spawned.")]
    [SerializeField] private CollectableSpawnPathType nextCollectableSpawnPath;

    // Gap between the zone top and the first obstacles / ZoneEnd below the zone.
    private const float ObstacleTopMargin = 100f;
    private const float ZoneEndOffset = 200f;

    private float spawnTimer;
    private bool isSpawning;
    private bool hasWarnedMissingManager;

    // Vertical range of the zone being filled (world Y), set at the start of every spawn.
    private Vector3 playerPosition;
    private float zoneTop;
    private float zoneBottom;

    // Prefab shape is the same every time, so it is measured once per prefab.
    private readonly Dictionary<GameObject, Bounds> spawnBoundsCache = new();
    private readonly List<Vector3> pathPoints = new();

    private ObjectPoolManager Pools => poolManager != null ? poolManager : ObjectPoolManager.Instance;

    // Game time between one zone and the next.
    public float ZoneTime => spawnInterval;

    // The chosen difficulty sets these before a run starts.
    public void ApplyDifficulty(GameSettings.Difficulty difficulty)
    {
        nextZone = difficulty.zone;
        obstaclesPerZone = difficulty.obstaclesPerZone;
        coinPathsPerZone = difficulty.coinPathsPerZone;
        collectablesPerPath = difficulty.coinsPerPath;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval && !isSpawning)
        {
            spawnTimer = 0f;
            StartCoroutine(SpawnZone());
        }
    }

    private IEnumerator SpawnZone()
    {
        if (GameManager.Instance == null || !HasPools()) yield break;

        isSpawning = true;

        playerPosition = GameManager.Instance.GetPlayerPosition();
        zoneTop = playerPosition.y - spawnDistance;
        zoneBottom = zoneTop - zoneDepth;

        // Special ones go first so they get the free space; regular obstacles avoid them.
        if (spawnSpecialObstacle) SpawnSpecialObstacles();

        if (spawnCollectable)
            for (int i = 0; i < coinPathsPerZone; i++) SpawnCollectablePath();

        if (spawnObstacle)
        {
            float clearance = GetZoneClearance(nextZone);
            int thisFrame = 0;

            foreach (GameObject prefab in Pools.GetList(PoolCategory.Obstacle))
                for (int i = 0; i < minOfEachObstacle; i++) PlaceObstacle(prefab, clearance);

            for (int i = 0; i < obstaclesPerZone; i++)
            {
                SpawnOneObstacle(clearance);

                if (obstaclesPerFrame > 0 && ++thisFrame >= obstaclesPerFrame)
                {
                    thisFrame = 0;
                    yield return null; // continue next frame
                }
            }
        }

        SpawnZoneEnd();
        if (spawnEndZoneAssets) SpawnEndZoneAssets();
        if (spawnZoneClouds) SpawnZoneClouds();

        isSpawning = false;
    }

    private bool HasPools()
    {
        if (Pools != null) return true;

        if (!hasWarnedMissingManager)
        {
            Debug.LogError("RandomSpawner: no ObjectPoolManager in the scene.", this);
            hasWarnedMissingManager = true;
        }
        return false;
    }

    // ---------------------------------------------------------------- Obstacles

    private void SpawnOneObstacle(float clearance)
    {
        PlaceObstacle(Pools.PickRandom(PoolCategory.Obstacle), clearance);
    }

    private void PlaceObstacle(GameObject prefab, float clearance)
    {
        if (prefab == null) return;

        if (TryFindFreeSpot(GetSpawnBounds(prefab), clearance, ObstacleTopMargin, out Vector3 position))
            SpawnAndSync(prefab, position);
    }

    private void SpawnSpecialObstacles()
    {
        for (int i = 0; i < maxSpecialObstaclesPerZone; i++)
        {
            // The yes / no roll.
            if (Random.value > specialObstacleChance) continue;

            GameObject prefab = Pools.PickRandom(PoolCategory.SpecialObstacle);
            if (prefab == null) return;

            if (!TryFindFreeSpot(GetSpawnBounds(prefab), specialObstacleClearance, ObstacleTopMargin, out Vector3 position)) continue;

            GameObject obstacle = SpawnAndSync(prefab, position);
            if (obstacle != null) SpawnRewardsInside(obstacle);
        }
    }

    // Puts a random special collectable in every reward slot.
    // The reward then sticks to its slot (Locomotion handles that).
    private void SpawnRewardsInside(GameObject obstacle)
    {
        if (!obstacle.TryGetComponent(out SpecialObstacle special))
        {
            Debug.LogWarning($"{obstacle.name} is in Special Obstacles but has no SpecialObstacle component.", obstacle);
            return;
        }

        if (special.RewardSlots == null) return;

        foreach (Transform slot in special.RewardSlots)
        {
            if (slot == null) continue;

            GameObject reward = Pools.SpawnRandom(PoolCategory.SpecialCollectable, slot.position);

            if (reward != null && reward.TryGetComponent(out Collectable collectable))
                special.AttachReward(collectable, slot);
        }
    }

    // Scatters fast obstacles in a ring just outside the play area, over the whole zone
    // height, so moving far out to the side is no way around the zone.
    private void SpawnEndZoneAssets()
    {
        float lowest = zoneBottom - ZoneEndOffset - endZoneHeight * 0.5f;

        for (int i = 0; i < endZoneAssetsPerZone; i++)
        {
            GameObject prefab = Pools.PickRandom(PoolCategory.EndZoneAsset);
            if (prefab == null) return;

            Bounds shape = GetSpawnBounds(prefab);

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(spawnRadius, spawnRadius + endZoneRingWidth);

                Vector3 position = new Vector3(
                    playerPosition.x + Mathf.Cos(angle) * distance,
                    Random.Range(lowest, zoneTop),
                    playerPosition.z + Mathf.Sin(angle) * distance);

                if (!IsFree(position, shape, 0f)) continue;

                SpawnAndSync(prefab, position);
                break;
            }
        }
    }

    // Clouds have no collider, so they need no free-spot check and may overlap anything.
    private void SpawnZoneClouds()
    {
        float centerY = zoneBottom - ZoneEndOffset;

        for (int i = 0; i < zoneCloudsPerZone; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(zoneCloudMinRadius, zoneCloudMaxRadius);

            Vector3 position = new Vector3(
                playerPosition.x + Mathf.Cos(angle) * distance,
                centerY + Random.Range(-zoneCloudHeight, zoneCloudHeight) * 0.5f,
                playerPosition.z + Mathf.Sin(angle) * distance);

            Pools.SpawnRandom(PoolCategory.ZoneCloud, position);
        }
    }

    private void SpawnZoneEnd()
    {
        Vector3 position = playerPosition;
        position.y = zoneBottom - ZoneEndOffset;

        GameObject spawned = Pools.SpawnRandom(PoolCategory.ZoneEnd, position);
        if (spawned != null && spawned.TryGetComponent(out ZoneEnd zoneEnd))
            zoneEnd.SetZoneType(nextZone);
    }

    // Picks random spots in the zone until one has no blocking collider inside the shape.
    private bool TryFindFreeSpot(Bounds shape, float clearance, float topMargin, out Vector3 position)
    {
        float upperY = zoneTop - topMargin;
        float lowerY = upperY - zoneDepth;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            position = RandomPointOnDisc(playerPosition, spawnRadius);
            position.y = Random.Range(lowerY, upperY);

            if (IsFree(position, shape, clearance)) return true;
        }

        position = default;
        return false;
    }

    private bool IsFree(Vector3 position, Bounds shape, float clearance)
    {
        Vector3 halfExtents = shape.extents + Vector3.one * clearance;

        return !Physics.CheckBox(position + shape.center, halfExtents, Quaternion.identity, blockingMask, QueryTriggerInteraction.Collide);
    }

    private static float GetZoneClearance(ZoneType zone)
    {
        switch (zone)
        {
            case ZoneType.Open: return 60f;
            case ZoneType.Loose: return 45f;
            case ZoneType.Busy: return 30f;
            case ZoneType.Crowded: return 20f;
            case ZoneType.Choked: return 10f;
            default: return 10f;
        }
    }

    // The space a prefab needs, relative to its pivot, measured from all its colliders
    // (or meshes if it has none). Colliders can be anywhere, on the root or on children.
    // A spinning prefab also needs the space its far corners sweep through.
    private Bounds GetSpawnBounds(GameObject prefab)
    {
        if (spawnBoundsCache.TryGetValue(prefab, out Bounds cached)) return cached;

        Transform root = prefab.transform;
        Matrix4x4 toRoot = root.worldToLocalMatrix;
        Bounds bounds = default;
        bool hasBounds = false;
        float reach = 0f;
        float sideReach = 0f;

        void Add(Transform part, Bounds local)
        {
            Matrix4x4 m = toRoot * part.localToWorldMatrix;
            Vector3 c = local.center, e = local.extents;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 p = Vector3.Scale(m.MultiplyPoint3x4(corner), root.localScale);

                reach = Mathf.Max(reach, p.magnitude);
                sideReach = Mathf.Max(sideReach, new Vector2(p.x, p.z).magnitude);

                if (!hasBounds) { bounds = new Bounds(p, Vector3.zero); hasBounds = true; }
                else bounds.Encapsulate(p);
            }
        }

        foreach (Collider col in prefab.GetComponentsInChildren<Collider>(true))
        {
            if (col.isTrigger && col.GetComponent<Collectable>() != null) continue;

            switch (col)
            {
                case BoxCollider box: Add(box.transform, new Bounds(box.center, box.size)); break;
                case SphereCollider s: Add(s.transform, new Bounds(s.center, Vector3.one * s.radius * 2f)); break;
                case CapsuleCollider cap:
                    Vector3 size = Vector3.one * cap.radius * 2f;
                    size[cap.direction] = Mathf.Max(cap.height, cap.radius * 2f);
                    Add(cap.transform, new Bounds(cap.center, size));
                    break;
                case MeshCollider mc when mc.sharedMesh != null: Add(mc.transform, mc.sharedMesh.bounds); break;
            }
        }

        if (!hasBounds)
        {
            foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) Add(mf.transform, mf.sharedMesh.bounds);
        }

        if (!hasBounds) bounds = new Bounds(Vector3.zero, Vector3.one * 100f);

        if (prefab.TryGetComponent(out PooledInteractable item))
        {
            if (item.SpinsFlat)
                bounds = new Bounds(Vector3.up * bounds.center.y, new Vector3(sideReach * 2f, bounds.size.y, sideReach * 2f));
            else if (item.SpinsAnyWay)
                bounds = new Bounds(Vector3.zero, Vector3.one * reach * 2f);
        }

        spawnBoundsCache[prefab] = bounds;
        return bounds;
    }

    // ---------------------------------------------------------------- Collectables

    private void SpawnCollectablePath()
    {
        GameObject coin = Pools.PickRandom(PoolCategory.Collectable);
        if (coin == null) return;

        // Skips None and Single, like before.
        var path = (CollectableSpawnPathType)Random.Range(2, System.Enum.GetValues(typeof(CollectableSpawnPathType)).Length);
        nextCollectableSpawnPath = path;

        BuildPathPoints(path, collectablesPerPath, pathPoints);
        if (pathPoints.Count == 0) return;

        // One random point on the path gets a special collectable instead of a coin.
        int specialIndex = Pools.Has(PoolCategory.SpecialCollectable) ? Random.Range(0, pathPoints.Count) : -1;

        for (int i = 0; i < pathPoints.Count; i++)
        {
            GameObject prefab = i == specialIndex ? Pools.PickRandom(PoolCategory.SpecialCollectable) : coin;
            Pools.Spawn(prefab, pathPoints[i]);
        }
    }

    // Fills `points` with the positions for one path shape, top to bottom.
    private void BuildPathPoints(CollectableSpawnPathType path, int count, List<Vector3> points)
    {
        points.Clear();
        float step = zoneDepth / count;
        Vector3 start;

        switch (path)
        {
            case CollectableSpawnPathType.Single:
                start = RandomPointOnDisc(playerPosition, spawnRadius);
                start.y = Random.Range(zoneBottom, zoneTop);
                points.Add(start);
                break;

            case CollectableSpawnPathType.Random:
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = RandomPointOnDisc(playerPosition, spawnRadius);
                    p.y = Random.Range(zoneBottom, zoneTop);
                    points.Add(p);
                }
                break;

            case CollectableSpawnPathType.Inline:
                start = RandomPointOnDisc(playerPosition, spawnRadius);
                AddLine(start, Vector3.zero, step, count, points);
                break;

            case CollectableSpawnPathType.TwoPaths:
                // Two parallel lines on opposite sides; the player has to pick one.
                start = RandomPointOnDisc(playerPosition, spawnRadius * 0.5f);
                Vector3 side = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.right * pathShapeRadius * 1.5f;
                int half = Mathf.Max(1, count / 2);
                AddLine(start - side, Vector3.zero, zoneDepth / half, half, points);
                AddLine(start + side, Vector3.zero, zoneDepth / half, half, points);
                break;

            case CollectableSpawnPathType.Cluster:
                start = RandomPointOnDisc(playerPosition, spawnRadius);
                start.y = (zoneTop + zoneBottom) * 0.5f;
                for (int i = 0; i < count; i++)
                    points.Add(Utility.FindRandominSphere(start, pathShapeRadius));
                break;

            case CollectableSpawnPathType.Spiral:
                start = RandomPointOnDisc(playerPosition, spawnRadius * 0.25f);
                const float totalAngle = 1080f; // 3 full turns
                for (int i = 1; i <= count; i++)
                {
                    Vector3 p = Utility.GetPointOnCircle(start, pathShapeRadius, totalAngle * i / count);
                    p.y = zoneTop - step * i;
                    points.Add(p);
                }
                break;

            case CollectableSpawnPathType.SlantedRight:
            case CollectableSpawnPathType.SlantedLeft:
                start = RandomPointOnDisc(playerPosition, spawnRadius * 0.25f);
                float sideStep = spawnRadius * 0.5f / count;
                if (path == CollectableSpawnPathType.SlantedLeft) sideStep = -sideStep;
                AddLine(start, new Vector3(sideStep, 0f, 0f), step, count, points);
                break;
        }
    }

    // Points going down from the zone top, each one `sideStep` further sideways.
    private void AddLine(Vector3 start, Vector3 sideStep, float downStep, int count, List<Vector3> points)
    {
        for (int i = 1; i <= count; i++)
        {
            Vector3 p = start + sideStep * i;
            p.y = zoneTop - downStep * i;
            points.Add(p);
        }
    }

    // ---------------------------------------------------------------- Helpers

    // Random point in a flat circle around `center` (X/Z only, keeps center.y).
    private static Vector3 RandomPointOnDisc(Vector3 center, float radius)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        return new Vector3(center.x + offset.x, center.y, center.z + offset.y);
    }

    // Obstacles need their collider registered right away so the next
    // placement check in the same frame can see it.
    private GameObject SpawnAndSync(GameObject prefab, Vector3 position)
    {
        GameObject spawned = Pools.Spawn(prefab, position);
        if (spawned != null) Physics.SyncTransforms();
        return spawned;
    }

    // Temp
    public void IncreaseDifficulty()
    {
        nextZone = (ZoneType)Mathf.Min((int)nextZone + 1, (int)ZoneType.Choked);
    }

    public void DecreaseDifficulty()
    {
        nextZone = (ZoneType)Mathf.Max((int)nextZone - 1, (int)ZoneType.Loose);
    }
}

/* Design notes (kept from the old version)

Make sure it is visible the crtical zones and the normal free zones  to the player
sometime hiding the reward

Keep the multiplier alive (my pick)
Pets are rare and the multiplier decays on a timer, so there are long stretches where it would just drain.Let each small pickup add a little time to the decay timer.Pets raise the multiplier, and the common pickups keep it from dropping while you hunt for the next pet.That solves the “decay vs pet frequency” worry from before without adding a new system.

Refill stamina
Each pickup restores a bit of slow - fall stamina, which gives you a reason to leave the safe line.If stamina already regenerates by itself, slow the passive regen down, or the pickups won’t matter.

Charge a pet call
Collect 10 and a pet appears in clear space ahead.This links the frequent collectible to the rare one, so a player who is missing pets can earn one.

Charge a shield or dash
Fill a small meter to earn one shield hit or a dash recharge.It’s a simple way to give the shield idea a reason to exist, since it comes from play instead of random pickups.

Chain bonus
Pickups placed in trails are worth more the longer you collect without missing one, so they reward clean flying along the path.

You can keep the visuals themed(raindrops or treats from earlier) and make some a bigger “rare drop” version placed near danger, so one object type covers safe and risky value.

multiplier glowes red when full capped
firedlies and lantern
*/
