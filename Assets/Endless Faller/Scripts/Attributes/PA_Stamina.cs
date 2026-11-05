
using UnityEngine;
using UnityEngine.Events;


public class PA_Stamina : MonoBehaviour
{
    [Header("Stamina Variables")]

    [SerializeField] private float maxStamina;
    [SerializeField] private float staminaDrainSpeed ;


    private float stamina;   
    private bool draingradually;

    private void Start()
    {
        draingradually = false;
       

    }

    void Update()
    {
        DrainStamina();
    }

    private void DrainStamina()
    {
        if (draingradually)
        {           
            stamina = Mathf.Clamp(stamina - staminaDrainSpeed * Time.deltaTime, 0, maxStamina);
            Player.Instance.OnStaminaDrain.Invoke();
            if (stamina <= 0) { ResetStamina(); }

        }
    }

    public void StartStaminaDrain(bool value)
    {
        draingradually = value;
       

    }
    
    public void GainStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina + amount, 0, maxStamina);
        Player.Instance.OnStaminaGain.Invoke();

    }

    public void ResetStamina()
    {
        draingradually = false;
        Player.Instance.OnStaminaReset.Invoke(); 

    }

    public float GetStamina() { return stamina; }
    public float GetMaxStamina() { return maxStamina; }
    public bool GetIsDraining() { return draingradually; }
}
