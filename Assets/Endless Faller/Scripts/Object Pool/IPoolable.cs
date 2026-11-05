// Anything on a pooled object that needs to reset or clean up when it is reused.
// ObjectPool calls these on every component of the object that implements it.
public interface IPoolable
{
    // Called right after the object is taken from the pool and switched on.
    void OnSpawn();

    // Called right before the object is switched off and given back to the pool.
    void OnDespawn();
}
