using UnityEngine;

public class LaneMoveController : MonoBehaviour
{
    [Header("Lane Settings")]
    [SerializeField] private float laneDistance = 50f;

    [Header("Movement")]
    [SerializeField] private float laneChangeSpeed = 10f;

    // -1 = Left
    //  0 = Center
    // +1 = Right
    private int currentLane = 0;

    private Vector3 targetPosition;

    private void Awake()
    {
        // Start from the object's current position.
        targetPosition = transform.position;

        // Determine which of the 3 lanes the object is currently closest to.
        currentLane = GetClosestLane(transform.position.x);

        // Make sure target matches the current lane.
        UpdateTargetPosition();
    }

    private void Update()
    {
        // Smoothly move toward the selected lane.
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            laneChangeSpeed * Time.deltaTime
        );
    }

    public void MoveLeft()
    {
        ChangeLane(-1);
    }

    public void MoveRight()
    {
        ChangeLane(1);
    }

    public void ChangeLane(int direction)
    {
        // Move only one lane at a time.
        currentLane += direction;

        // There are ONLY three lanes:
        // -1 = Left
        //  0 = Center
        // +1 = Right
        currentLane = Mathf.Clamp(currentLane, -1, 1);

        UpdateTargetPosition();

        Debug.Log(
            "Current Lane: " + currentLane +
            " | Target Position: " + targetPosition
        );
    }

    private void UpdateTargetPosition()
    {
        // Calculate X from the lane.
        float targetX = currentLane * laneDistance;

        // Keep Y and Z exactly where the object currently is.
        targetPosition = new Vector3(
            targetX,
            transform.position.y,
            transform.position.z
        );
    }

    private int GetClosestLane(float xPosition)
    {
        if (laneDistance <= 0f)
            return 0;

        float lane = xPosition / laneDistance;

        return Mathf.Clamp(
            Mathf.RoundToInt(lane),
            -1,
            1
        );
    }

    public int GetCurrentLane()
    {
        return currentLane;
    }

    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
}