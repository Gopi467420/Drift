using UnityEngine;

// Base for everything the spawner puts in a pool: Collectable, Obstacle, SpecialObstacle, ZoneEnd.
// Holds the movement settings (shown in each one's Inspector under "Locomotion"),
// picks the speed / spin on every spawn, and Locomotion does the actual moving.
public abstract class PooledInteractable : Interactable, IPoolable, IMovable
{
    [Header("Locomotion")]
    [SerializeField] private bool moveInLine;
    [SerializeField] private MoveInLineSettings moveInLineSettings = new();

    [SerializeField] private bool randomRotate;
    [SerializeField] private RandomRotateSettings randomRotateSettings = new();

    public Vector3 Velocity { get; set; }
    public Vector3 Spin { get; set; }
    public Transform FollowTarget { get; set; }

    public virtual void OnSpawn()
    {
        Velocity = moveInLine ? moveInLineSettings.PickVelocity() : Vector3.zero;
        Spin = randomRotate ? randomRotateSettings.PickSpin() : Vector3.zero;
        FollowTarget = null;
    }

    public virtual void OnDespawn()
    {
        FollowTarget = null;
    }
}
