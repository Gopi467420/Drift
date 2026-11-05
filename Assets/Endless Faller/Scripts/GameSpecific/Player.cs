using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    [SerializeField] private float moveSpeed = 5f;
    private Vector2 moveInput;
    private float knockbackStrength = 2f;
    private float knockbackDecay = 8f;
    private Vector3 knockbackVelocity;


    private PA_Stamina staminaComponent;

    public UnityEvent OnStaminaReset;
    public UnityEvent OnStaminaGain;
    public UnityEvent OnStaminaDrain;

    // What can be seen of the player. The camera sits under the player too, so skip it.
    public MeshRenderer[] VisibleParts { get; private set; }



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        var parts = new System.Collections.Generic.List<MeshRenderer>();

        foreach (MeshRenderer part in GetComponentsInChildren<MeshRenderer>(true))
            if (part.GetComponentInParent<Camera>(true) == null) parts.Add(part);

        VisibleParts = parts.ToArray();

        // Hidden until the entrance finishes.
        SetVisible(false);

        knockbackStrength = 0;
        knockbackDecay = 0;

        staminaComponent = gameObject.GetComponent<PA_Stamina>();

    }

   
    // Movement used to be 0.3 per frame, so it ran slower on low-FPS phones.
    // Scaling by frame time keeps the same feel as 60 FPS on every device.
    // Unscaled time, because the player ignored timeScale (slow-mo) before too.
    private const float ReferenceFrameRate = 60f;

    public void SetVisible(bool isVisible)
    {
        foreach (MeshRenderer part in VisibleParts) part.enabled = isVisible;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying || GameManager.Instance.IsPaused) return;

        float step = 0.3f * ReferenceFrameRate * Time.unscaledDeltaTime;

        Vector3 movement = new Vector3(moveInput.x,0f,moveInput.y) * moveSpeed;
        transform.position += (movement + knockbackVelocity) * step;
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity,Vector3.zero,knockbackDecay * step);
    }

   public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Obstacle")) return;
        Vector3 direction = transform.position - other.transform.position;
        direction = other.transform.InverseTransformDirection(direction);
        direction = other.transform.TransformDirection(direction).normalized;
        direction.y = 0f;
        knockbackVelocity = direction * knockbackStrength;
    }

    //Stamina


    public void HandleStaminaDraining()
    {
        if (!staminaComponent.GetIsDraining()) { staminaComponent.StartStaminaDrain(true); }
        else { staminaComponent.StartStaminaDrain(false); }
    }
    public PA_Stamina GetStaminaComponent() { return staminaComponent; }



}