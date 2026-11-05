using UnityEngine;

// LEGACY: does nothing any more. Movement settings now live on
// Obstacle / Collectable / ZoneEnd and Locomotion applies them.
// Kept only so "Endless Faller > 1. Migrate Locomotion Settings" can read the old values.
// Delete this file after running the migration.
[AddComponentMenu("")]
public class MoveInLine : MonoBehaviour
{
    [SerializeField] private float speedRangeMin;
    [SerializeField] private float speedRangeMax;
    [SerializeField] private MoveAxis movementAxis;
}
