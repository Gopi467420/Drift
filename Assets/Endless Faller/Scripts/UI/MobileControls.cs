using UnityEngine;
using UnityEngine.InputSystem;

// Shows the on-screen controls only on touch devices, and reads them directly.
// The stick and the slow-mo button both talk to a virtual gamepad. PlayerInput would
// otherwise treat that as a different device from the touchscreen and switch control
// schemes mid-game, which cuts the input off, so this skips PlayerInput for them.
public class MobileControls : MonoBehaviour
{
    [Tooltip("Stick, stick base, slow-mo and pause. Hidden when there is no touchscreen.")]
    [SerializeField] private GameObject[] touchOnlyObjects;

    private bool isTouchDevice;

    private void Awake()
    {
        isTouchDevice = Application.isMobilePlatform || Touchscreen.current != null;

        foreach (GameObject control in touchOnlyObjects)
            if (control != null) control.SetActive(isTouchDevice);
    }

    private void Update()
    {
        if (!isTouchDevice) return;

        Gamepad pad = Gamepad.current;
        if (pad == null || Player.Instance == null) return;

        Player.Instance.SetMoveInput(pad.leftStick.ReadValue());

        // Not paused, so a tap on slow-mo can't change speed behind the pause menu.
        if (pad.leftTrigger.wasPressedThisFrame && !GameManager.Instance.IsPaused)
            InputManager.Instance.ToggleSlowMo();
    }
}
