using UnityEngine;

// LEGACY: does nothing any more. Rotation settings now live on
// Obstacle / Collectable / ZoneEnd and Locomotion applies them.
// Kept only so "Endless Faller > 1. Migrate Locomotion Settings" can read the old values.
// Delete this file after running the migration.
[AddComponentMenu("")]
public class RandomRotator : MonoBehaviour
{
    [SerializeField] private bool freeRotation;
    [SerializeField] private SpinAxis axis = SpinAxis.Y;
    [SerializeField] private float speed = 10;
}
