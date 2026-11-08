using UnityEngine;
using Enums;

public class Collectable : PooledInteractable
{
    [SerializeField] private CollectableType type;

    private Vector3 fullScale;

    // True while AttractionEffect carries it to the player.
    public bool IsFlying { get; private set; }

    // Awake runs once per copy, so this is the size from the prefab.
    private void Awake()
    {
        fullScale = transform.localScale;
    }

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

            ReturnToPool();
        }
    }

    public void BeginFlight()
    {
        IsFlying = true;
        canInteract = false;
        Velocity = Vector3.zero;
    }

    public void SetFlightSize(float size)
    {
        transform.localScale = fullScale * size;
    }

    public void Land()
    {
        GameManager.Instance.OnPlayerInteractCollectable.Invoke(type);
        ReturnToPool();
    }

    public override void OnDespawn()
    {
        base.OnDespawn();

        IsFlying = false;
        transform.localScale = fullScale;
    }

    // Back to the pool for reuse (plain hide if it wasn't pooled).
    private void ReturnToPool()
    {
        if (!ObjectPool.Despawn(gameObject))
            gameObject.SetActive(false);
    }
}
