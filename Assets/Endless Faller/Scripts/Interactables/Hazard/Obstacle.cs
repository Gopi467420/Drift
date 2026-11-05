using UnityEngine;

// A regular obstacle: hurts the player on touch, and shakes the camera on a near miss.
// (This was HazardObstacle; the file was renamed so prefabs keep their settings.)
public class Obstacle : PooledInteractable, IPlayerProximity
{
    [Header("Near Miss")]
    [SerializeField] private float shakeDistance = 500;

    // Every collider of the obstacle (one on the root, or several on children like a ring).
    private Collider[] obstacleColliders;
    private bool hasTriggeredShake;

    private void Awake()
    {
        obstacleColliders = GetComponentsInChildren<Collider>(true);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        hasTriggeredShake = false;
    }

    // Called by Locomotion once per frame while this obstacle is active.
    public void CheckPlayer(Vector3 playerPosition)
    {
        if (IsPlayerPassingClosely(playerPosition))
            GameManager.Instance.OnPlayerPassingTooCloselyToObstacle.Invoke();
    }

    private bool IsPlayerPassingClosely(Vector3 playerPosition)
    {
        if (hasTriggeredShake || obstacleColliders == null)
            return false;

        float shakeDistanceSqr = shakeDistance * shakeDistance;

        foreach (Collider obstacleCollider in obstacleColliders)
        {
            if (obstacleCollider == null) continue;

            // Cheap box check first; the exact ClosestPoint only runs when the player is near.
            if (obstacleCollider.bounds.SqrDistance(playerPosition) >= shakeDistanceSqr)
                continue;

            Vector3 closestPoint = obstacleCollider.ClosestPoint(playerPosition);

            if (Utility.IsInRange(playerPosition, closestPoint, shakeDistance))
            {
                hasTriggeredShake = true;
                return true;
            }
        }

        return false;
    }

    protected override void Interact(Collider other)
    {
        if (other.CompareTag("Player"))
            GameManager.Instance.PlayerHitBy(gameObject);
    }

    public void ResetObstacle()
    {
        hasTriggeredShake = false;
    }
}
