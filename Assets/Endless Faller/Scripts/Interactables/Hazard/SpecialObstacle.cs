using System.Collections.Generic;
using UnityEngine;

// An obstacle with holes or gaps (donut, cashew, door frame...).
// Add empty child objects inside the holes and drag them into Reward Slots.
// RandomSpawner puts a random special collectable in every slot; the reward
// sticks to its slot (even if the obstacle spins) until it is collected.
public class SpecialObstacle : Obstacle
{
    [Header("Rewards")]
    [Tooltip("Empty child objects placed inside the holes / gaps. One reward spawns at each.")]
    [SerializeField] private Transform[] rewardSlots;

    private readonly List<Collectable> rewards = new();

    public Transform[] RewardSlots => rewardSlots;

    public void AttachReward(Collectable reward, Transform slot)
    {
        reward.FollowTarget = slot;
        reward.transform.position = slot.position;
        rewards.Add(reward);
    }

    public override void OnDespawn()
    {
        base.OnDespawn();

        // Rewards nobody collected leave together with the obstacle.
        // (A reward that was collected and reused elsewhere follows something else, so it is left alone.)
        foreach (Collectable reward in rewards)
        {
            if (reward != null && reward.FollowTarget != null && reward.FollowTarget.IsChildOf(transform))
                ObjectPool.Despawn(reward.gameObject);
        }

        rewards.Clear();
    }

    // Yellow spheres in the Scene view show where the rewards will appear.
    private void OnDrawGizmosSelected()
    {
        if (rewardSlots == null) return;

        Gizmos.color = Color.yellow;
        foreach (Transform slot in rewardSlots)
        {
            if (slot != null) Gizmos.DrawWireSphere(slot.position, 10f);
        }
    }
}
