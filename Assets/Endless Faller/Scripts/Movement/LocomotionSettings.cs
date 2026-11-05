using UnityEngine;

// Obstacles, collectables and ZoneEnd hold these, and Locomotion applies them.

public enum MoveAxis { X, NegativeX, Y, NegativeY, Z, NegativeZ }
public enum SpinAxis { X, Y, Z }

[System.Serializable]
public class MoveInLineSettings
{
    public MoveAxis axis = MoveAxis.Y;
    [Tooltip("A random speed between Min and Max is picked on every spawn (units per second).")]
    public float speedMin = 100f;
    public float speedMax = 150f;

    public Vector3 PickVelocity()
    {
        return AxisToVector(axis) * Random.Range(speedMin, speedMax);
    }

    private static Vector3 AxisToVector(MoveAxis moveAxis)
    {
        switch (moveAxis)
        {
            case MoveAxis.X: return Vector3.right;
            case MoveAxis.NegativeX: return Vector3.left;
            case MoveAxis.Y: return Vector3.up;
            case MoveAxis.NegativeY: return Vector3.down;
            case MoveAxis.Z: return Vector3.forward;
            case MoveAxis.NegativeZ: return Vector3.back;
            default: return Vector3.zero;
        }
    }
}

[System.Serializable]
public class RandomRotateSettings
{
    [Tooltip("Spin around a random direction instead of one axis.")]
    public bool freeRotation;
    public SpinAxis axis = SpinAxis.Y;
    [Tooltip("Degrees per second.")]
    public float speed = 10f;

    public Vector3 PickSpin()
    {
        Vector3 direction;

        if (freeRotation)
        {
            direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }
        else
        {
            switch (axis)
            {
                case SpinAxis.X: direction = Vector3.right; break;
                case SpinAxis.Z: direction = Vector3.forward; break;
                default: direction = Vector3.up; break;
            }
        }

        return direction * speed;
    }
}
