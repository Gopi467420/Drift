using UnityEngine;

public class MoveToPoint : MonoBehaviour
{
    public enum MovementMode
    {
        Slow,
        Fast
    }

    [Header("Speed")]
    [SerializeField] private float slowSpeed = 5f;
    [SerializeField] private float fastSpeed = 20f;

    [Header("Stopping")]
    [SerializeField] private float stoppingDistance = 0.05f;

    private Vector3 targetPosition;
    private MovementMode movementMode;

    private bool hasTarget;

    private void Update()
    {
        if (!hasTarget)
            return;

        float speed = movementMode == MovementMode.Fast
            ? fastSpeed
            : slowSpeed;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime 
        );

        if (Vector3.Distance(transform.position, targetPosition) <= stoppingDistance)
        {
            transform.position = targetPosition;

            // For a tap, this stops automatically.
            if (movementMode == MovementMode.Fast)
            {
                hasTarget = false;
            }
        }
    }

    public void SetTarget(Vector3 newTarget, MovementMode mode)
    {
        // Always replace the previous target.
        targetPosition = newTarget;
        movementMode = mode;
        hasTarget = true;
    }

    public void Stop()
    {
        hasTarget = false;
    }

    public Vector3 GetTarget()
    {
        return targetPosition;
    }
}