using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GyroController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private TMP_Text debugText;

    private AttitudeSensor attitudeSensor;

    private Quaternion neutralRotation;

    private void Awake()
    {
        attitudeSensor = AttitudeSensor.current;

        if (attitudeSensor == null)
        {
            Debug.LogWarning("No Attitude Sensor detected.");
            return;
        }

        InputSystem.EnableDevice(attitudeSensor);

        // Current phone orientation becomes neutral
        neutralRotation = attitudeSensor.attitude.ReadValue();
    }

    private void Update()
    {
        if (attitudeSensor == null)
            return;

        Quaternion currentRotation = attitudeSensor.attitude.ReadValue();

        // Calculate rotation relative to the starting position
        Quaternion relativeRotation =
            Quaternion.Inverse(neutralRotation) * currentRotation;

        // Convert relative rotation to Euler angles
        Vector3 relativeAngles = relativeRotation.eulerAngles;

        // Convert 0-360 range into -180 to +180
        float x = NormalizeAngle(relativeAngles.x);
        float y = NormalizeAngle(relativeAngles.y);
        float z = NormalizeAngle(relativeAngles.z);

        if (debugText != null)
        {
            debugText.text =
                "RELATIVE TILT\n\n" +
                "X: " + x.ToString("F1") + "°\n" +
                "Y: " + y.ToString("F1") + "°\n" +
                "Z: " + z.ToString("F1") + "°";
        }
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    public Vector2 GetInput()
    {
        return Vector2.zero;
    }
}