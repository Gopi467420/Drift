using UnityEngine;
using Enums;

public class Collectable : PooledInteractable
{
    [SerializeField] private CollectableType type;

    protected override void Interact(Collider other)
    {
        if (other.CompareTag("Player") && GameManager.Instance.IsPlaying)
        {
            // Sitting in a special obstacle's slot is what makes this the big reward.
            bool wasInSpecialObstacle = FollowTarget != null;

            GameManager.Instance.OnPlayerInteractCollectable.Invoke(type);

            if (ComicEffects.Instance != null)
                ComicEffects.Instance.PlayPickup(transform.position);

            if (wasInSpecialObstacle && ComicEffects.Instance != null)
                ComicEffects.Instance.PlayImpact();

            // Back to the pool for reuse (plain hide if it wasn't pooled).
            if (!ObjectPool.Despawn(gameObject))
                gameObject.SetActive(false);
        }
    }
}
