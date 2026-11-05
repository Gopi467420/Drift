using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    public static InputManager Instance;

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
