using UnityEngine;

// Added by ObjectPool to every copy it creates.
// Remembers which pool and slot the copy belongs to, and caches components,
// so nothing has to call GetComponent every frame.
public class PooledObject : MonoBehaviour
{
    public ObjectPool Owner { get; private set; }

    // Position in the owner's list. Index < ActiveCount means "in use".
    public int Index { get; internal set; }

    public Transform CachedTransform { get; private set; }
    public bool IsActive => Owner != null && Owner.IsActive(this);

    internal bool IsDespawning;

    private IPoolable[] poolables;

    // Small cache for Get<T>(): a few (type, component) pairs per object.
    private System.Type[] cachedTypes = new System.Type[4];
    private object[] cachedValues = new object[4];
    private int cachedCount;

    internal void Init(ObjectPool owner, int index)
    {
        Owner = owner;
        Index = index;
        CachedTransform = transform;
        poolables = GetComponentsInChildren<IPoolable>(true);
    }

    // GetComponent<T>() that is looked up once, then remembered.
    // Works with interfaces too (e.g. Get<IMovable>()). Returns null if missing.
    public T Get<T>() where T : class
    {
        System.Type type = typeof(T);

        for (int i = 0; i < cachedCount; i++)
            if (cachedTypes[i] == type) return (T)cachedValues[i];

        T value = GetComponent<T>();
        if (value is Object unityObject && unityObject == null) value = null;

        if (cachedCount == cachedTypes.Length)
        {
            System.Array.Resize(ref cachedTypes, cachedCount * 2);
            System.Array.Resize(ref cachedValues, cachedCount * 2);
        }

        cachedTypes[cachedCount] = type;
        cachedValues[cachedCount] = value;
        cachedCount++;
        return value;
    }

    internal void NotifySpawn()
    {
        for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawn();
    }

    internal void NotifyDespawn()
    {
        for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawn();
    }
}
