using UnityEngine;

public class DestroyOnInteract : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        // Pooled objects go back to the pool; anything else is destroyed as before.
        if (!ObjectPool.Despawn(other.gameObject))
            Destroy(other.gameObject);
    }
}
