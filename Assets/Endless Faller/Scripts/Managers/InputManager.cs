using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    public static InputManager Instance;

    // Touch devices run a pretend gamepad for the on-screen stick, so only a real one counts there.
    public static bool HasGamepad
    {
        get
        {
            foreach (Gamepad pad in Gamepad.all)
                if (pad.native || !MobileControls.IsTouchDevice) return true;

            return false;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        ToggleSlowMo();
    }

    public void ToggleSlowMo()
    {
        // The A button also presses menu buttons, so slow motion waits for a live run.
        if (!GameManager.Instance.IsPlaying || GameManager.Instance.IsPaused) return;

        Player.Instance.HandleStaminaDraining();

        if (GameManager.Instance.CanUSeSlowMo())
        {
            GameManager.Instance.SwitchTimeSpeed();
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        Player.Instance.SetMoveInput(context.ReadValue<Vector2>());
    }

    public void SwitchToUI()
    {
        playerInput.SwitchCurrentActionMap("UI");
    }

    public void SwitchToPlayer()
    {
        playerInput.SwitchCurrentActionMap("Player");
    }
}
