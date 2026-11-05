using UnityEngine;
public abstract class Interactable : MonoBehaviour
{

    protected bool canInteract;

    // OnEnable (not Start) so pooled objects reset every time they are reused.
    protected virtual void OnEnable()
    {
        canInteract = true;
    }
    private void OnTriggerEnter(Collider other)
    {

       
        if (other != null && canInteract) { Interact(other); }
    }
        
    protected abstract void Interact( Collider other);
}


   