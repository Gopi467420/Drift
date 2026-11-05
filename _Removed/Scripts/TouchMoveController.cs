using UnityEngine;
using UnityEngine.InputSystem;

public class TouchMoveController : MonoBehaviour
{
    public enum MovementPlane
    {
        XY,
        XZ,
        YZ
    }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MoveToPoint moveToPoint;

    [Header("Movement Plane")]
    [SerializeField] private MovementPlane movementPlane = MovementPlane.XZ;

    [Header("Touch Movement")]
    [SerializeField] private float holdThreshold = 0.2f;

    // The plane is created once and reused.
    private Plane touchPlane;

    private Vector2 currentTouchPosition;

    private float touchStartTime;

    private bool touchPressed;
    private bool holdStarted;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (moveToPoint == null)
            moveToPoint = GetComponent<MoveToPoint>();

        CreateTouchPlane();
    }

    private void Update()
    {
        if (!touchPressed)
            return;

        float touchDuration = Time.time - touchStartTime;

        // TapOnly can be added later if required.
        if (!holdStarted && touchDuration >= holdThreshold)
        {
            holdStarted = true;

          //  Debug.Log("HOLD STARTED");
        }

        if (holdStarted)
        {
            MoveSlowlyToTouch();
        }
    }

    // =========================================================
    // TOUCH PRESS
    // =========================================================

    public void TouchPress(InputAction.CallbackContext ctx)
    {
        if (Touchscreen.current == null)
            return;

        // Finger touched the screen.
        if (ctx.started)
        {
            touchPressed = true;
            holdStarted = false;

            touchStartTime = Time.time;

            currentTouchPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();

           // Debug.Log("TOUCH STARTED");
        }

        // Finger released.
        if (ctx.canceled)
        {
            if (!touchPressed)
                return;

            float touchDuration = Time.time - touchStartTime;

            bool wasTap = touchDuration < holdThreshold;

            if (wasTap)
            {
                MoveQuicklyToTouch();
            }

            touchPressed = false;
            holdStarted = false;

          //  Debug.Log("TOUCH ENDED");
        }
    }

    // =========================================================
    // TOUCH POSITION
    // =========================================================

    public void TouchPosition(InputAction.CallbackContext ctx)
    {
        currentTouchPosition = ctx.ReadValue<Vector2>();
    }

    // =========================================================
    // TAP
    // =========================================================

    private void MoveQuicklyToTouch()
    {
        if (!TryGetWorldPosition(
                currentTouchPosition,
                out Vector3 targetPosition))
        {
            return;
        }

        moveToPoint.SetTarget(
            targetPosition,
            MoveToPoint.MovementMode.Fast
        );

       // Debug.Log("FAST TARGET: " + targetPosition);
    }

    // =========================================================
    // HOLD
    // =========================================================

    private void MoveSlowlyToTouch()
    {
        if (!TryGetWorldPosition(
                currentTouchPosition,
                out Vector3 targetPosition))
        {
            return;
        }

        moveToPoint.SetTarget(
            targetPosition,
            MoveToPoint.MovementMode.Slow
        );
    }

    // =========================================================
    // CREATE PLANE
    // =========================================================

    private void CreateTouchPlane()
    {
        Vector3 currentPosition = transform.position;

        switch (movementPlane)
        {
            case MovementPlane.XY:

                // X and Y can change.
                // Z is fixed.

                touchPlane = new Plane(
                    Vector3.forward,
                    new Vector3(
                        0f,
                        0f,
                        currentPosition.z
                    )
                );

                break;

            case MovementPlane.XZ:

                // X and Z can change.
                // Y is fixed.

                touchPlane = new Plane(
                    Vector3.up,
                    new Vector3(
                        0f,
                        currentPosition.y,
                        0f
                    )
                );

                break;

            case MovementPlane.YZ:

                // Y and Z can change.
                // X is fixed.

                touchPlane = new Plane(
                    Vector3.right,
                    new Vector3(
                        currentPosition.x,
                        0f,
                        0f
                    )
                );

                break;
        }
    }

    // =========================================================
    // SCREEN → WORLD
    // =========================================================

    private bool TryGetWorldPosition(
        Vector2 screenPosition,
        out Vector3 targetPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (!touchPlane.Raycast(ray, out float distance))
        {
            targetPosition = transform.position;
            return false;
        }

        Vector3 worldPoint = ray.GetPoint(distance);

        targetPosition = KeepFixedAxis(worldPoint);

        return true;
    }

    // =========================================================
    // KEEP FIXED AXIS
    // =========================================================

    private Vector3 KeepFixedAxis(Vector3 worldPoint)
    {
        Vector3 currentPosition = transform.position;

        switch (movementPlane)
        {
            case MovementPlane.XY:

                return new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    currentPosition.z
                );

            case MovementPlane.XZ:

                return new Vector3(
                    worldPoint.x,
                    currentPosition.y,
                    worldPoint.z
                );

            case MovementPlane.YZ:

                return new Vector3(
                    currentPosition.x,
                    worldPoint.y,
                    worldPoint.z
                );

            default:

                return currentPosition;
        }
    }
}