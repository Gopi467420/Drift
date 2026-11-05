using UnityEngine;

// Cheap, low-detail scenery that RandomSpawner places in a ring around the end
// of every critical zone. Moves with the Locomotion settings like everything else.
public class EndZoneAsset : PooledInteractable
{
    [Tooltip("Off = pure scenery the player can pass through.")]
    [SerializeField] private bool damagesPlayer = true;

    protected override void Interact(Collider other)
    {
        if (damagesPlayer && other.CompareTag("Player"))
            GameManager.Instance.PlayerHitBy(gameObject);
    }
}
