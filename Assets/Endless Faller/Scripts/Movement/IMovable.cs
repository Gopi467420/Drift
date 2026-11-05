using UnityEngine;

// Something Locomotion moves. Values are set when the object spawns.
public interface IMovable
{
    // Units per second, world space.
    Vector3 Velocity { get; set; }

    // Degrees per second, local space.
    Vector3 Spin { get; set; }

    // When set, the object sticks to this transform instead of using Velocity
    // (rewards inside a special obstacle's hole).
    Transform FollowTarget { get; set; }
}

// Something that wants the player position every frame (near-miss checks).
// Locomotion calls it in the same loop, so there are no per-object Update()s.
public interface IPlayerProximity
{
    void CheckPlayer(Vector3 playerPosition);
}
