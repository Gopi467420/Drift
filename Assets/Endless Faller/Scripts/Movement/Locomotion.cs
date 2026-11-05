using UnityEngine;

// Moves and spins every ACTIVE pooled object in one Update, and gives objects
// back to their pool once they are far enough above the player
// (this replaces the old "DestroyAll" trigger).
// Put it next to ObjectPoolManager. Because each pool keeps its active copies
// packed at the front, this only loops over objects that are actually in use.
public class Locomotion : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager poolManager;

    [Tooltip("Objects higher than this above the player go back to the pool.")]
    [SerializeField] private float despawnHeightAbovePlayer = 700f;

    private void Awake()
    {
        if (poolManager == null) poolManager = GetComponent<ObjectPoolManager>();
    }

    private void Update()
    {
        ObjectPoolManager manager = poolManager != null ? poolManager : ObjectPoolManager.Instance;
        if (manager == null) return;

        float deltaTime = Time.deltaTime;

        bool hasPlayer = GameManager.Instance != null && Player.Instance != null;
        Vector3 playerPosition = hasPlayer ? GameManager.Instance.GetPlayerPosition() : Vector3.zero;
        float despawnY = playerPosition.y + despawnHeightAbovePlayer;

        var pools = manager.Pools;

        // Pass 1: move, spin, near-miss check, despawn when out of sight above.
        for (int p = 0; p < pools.Count; p++)
        {
            ObjectPool pool = pools[p];

            // Backwards, so a copy despawned here can't make us skip one.
            for (int i = pool.ActiveCount - 1; i >= 0; i--)
            {
                PooledObject item = pool[i];
                if (item == null) continue;

                Transform t = item.CachedTransform;
                IMovable movable = item.Get<IMovable>();

                if (movable != null)
                {
                    if (movable.FollowTarget == null)
                    {
                        Vector3 velocity = movable.Velocity;
                        if (velocity != Vector3.zero) t.position += velocity * deltaTime;
                    }

                    Vector3 spin = movable.Spin;
                    if (spin != Vector3.zero) t.Rotate(spin * deltaTime, Space.Self);
                }

                if (!hasPlayer) continue;

                if (t.position.y > despawnY)
                {
                    pool.Despawn(item);
                    continue;
                }

                item.Get<IPlayerProximity>()?.CheckPlayer(playerPosition);
            }
        }

        // Pass 2: followers, after everything has moved, so they sit exactly in their slot.
        for (int p = 0; p < pools.Count; p++)
        {
            ObjectPool pool = pools[p];

            for (int i = pool.ActiveCount - 1; i >= 0; i--)
            {
                PooledObject item = pool[i];
                if (item == null) continue;

                IMovable movable = item.Get<IMovable>();
                if (movable == null) continue;

                Transform target = movable.FollowTarget;
                if (target != null) item.CachedTransform.position = target.position;
            }
        }
    }
}
