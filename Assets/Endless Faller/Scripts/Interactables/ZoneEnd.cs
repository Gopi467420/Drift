using UnityEngine;
using Enums;

public class ZoneEnd : PooledInteractable
{
    private ZoneType Zonetype;
    public void SetZoneType(ZoneType zone) { Zonetype = zone; }

    protected override void Interact(Collider other)
    {
        // Obstacles with a Rigidbody also touch this trigger, and only the player earns the reward.
        if (other.GetComponentInParent<Player>() == null) return;

        // The player has several colliders, and each one would count otherwise.
        canInteract = false;

        GameManager.Instance.GrantZoneRewards(Zonetype);
    }
}
