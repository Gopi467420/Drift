using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class RotateDialWithTouch : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float rotationClampMin = 0f;
    [SerializeField] private float rotationClampMax = 90f;

    public UnityEvent<float> OnUIDialRotation;

    private float currentRotation;
    private RectTransform rectTransform;

    private bool isDragging;
    private float previousAngle;

    private TouchControl activeTouch;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (Touchscreen.current == null)
            return;

        if (!isDragging)
        {
            FindDialTouch();
        }
        else
        {
            UpdateDialTouch();
        }
    }

    private void FindDialTouch()
    {
        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.wasPressedThisFrame)
                continue;

            Vector2 touchPosition = touch.position.ReadValue();

            if (RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform,
                touchPosition))
            {
                StartRotation(touchPosition);

                activeTouch = touch;
                break;
            }
        }
    }

    private void UpdateDialTouch()
    {
        if (activeTouch == null)
        {
            isDragging = false;
            return;
        }

        if (activeTouch.press.wasReleasedThisFrame)
        {
            isDragging = false;
            activeTouch = null;
            return;
        }

        if (activeTouch.press.isPressed)
        {
            Rotate(activeTouch.position.ReadValue());
        }
    }

    private void StartRotation(Vector2 touchPosition)
    {
        Vector2 dialCenter =
            RectTransformUtility.WorldToScreenPoint(
                null,
                rectTransform.position);

        Vector2 direction = touchPosition - dialCenter;

        previousAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        isDragging = true;
    }

    private void Rotate(Vector2 touchPosition)
    {
        Vector2 dialCenter =
            RectTransformUtility.WorldToScreenPoint(
                null,
                rectTransform.position);

        Vector2 direction = touchPosition - dialCenter;

        float currentAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float deltaAngle =
            Mathf.DeltaAngle(previousAngle, currentAngle);

        currentRotation += deltaAngle * rotationSpeed;

        currentRotation = Mathf.Clamp(
            currentRotation,
            rotationClampMin,
            rotationClampMax
        );

        rectTransform.localRotation =
            Quaternion.Euler(0f, 0f, currentRotation);

        previousAngle = currentAngle;

        OnUIDialRotation?.Invoke(currentRotation);
    }
}