using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool
{
    private readonly List<PooledObject> items = new();
    private readonly Transform container;

    public GameObject Prefab { get; }
    public int ActiveCount { get; private set; }
    public int Count => items.Count;
    public PooledObject this[int index] => items[index];

    // container: optional parent for FREE copies, to keep the Hierarchy tidy.
    // Active copies are moved to the scene root (moving objects are cheaper there).
    public ObjectPool(GameObject prefab, int prewarm = 0, Transform container = null)
    {
        Prefab = prefab;
        this.container = container;
        Prewarm(prewarm);
    }

    public void Prewarm(int count)
    {
        while (items.Count < count) CreateFree();
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        // Drop copies that were Destroy()ed while they were free.
        while (ActiveCount < items.Count && items[ActiveCount] == null)
            RemoveFreeAt(ActiveCount);

        if (ActiveCount == items.Count) CreateFree();

        // The first free copy sits right after the active ones.
        PooledObject item = items[ActiveCount];
        ActiveCount++;

        Transform t = item.CachedTransform;
        if (container != null) t.SetParent(null, false);
        t.SetPositionAndRotation(position, rotation);
        item.gameObject.SetActive(true);
        item.NotifySpawn();
        return item.gameObject;
    }

    public void Despawn(PooledObject item)
    {
        if (item == null || item.IsDespawning || !IsActive(item)) return;

        item.IsDespawning = true;
        item.NotifyDespawn();
        item.gameObject.SetActive(false);
        item.IsDespawning = false;

        // If a callback may moved it then read the index again.
        if (!IsActive(item)) return;

        // Swap with the last active copy, then shrink the active part.
        Swap(item.Index, ActiveCount - 1);
        ActiveCount--;

        if (container != null) item.CachedTransform.SetParent(container, false);
    }

    public bool IsActive(PooledObject item)
    {
        int index = item.Index;
        return index >= 0 && index < ActiveCount && items[index] == item;
    }

    // Gives any object back to the pool that made it (works from a child collider too).
    // Returns false if the object did not come from a pool.
    public static bool Despawn(GameObject target)
    {
        if (target == null) return false;

        PooledObject pooled = target.GetComponentInParent<PooledObject>();
        if (pooled == null || pooled.Owner == null) return false;

        pooled.Owner.Despawn(pooled);
        return true;
    }

    private void CreateFree()
    {
        GameObject created = Object.Instantiate(Prefab, container);
        created.name = Prefab.name;
        created.SetActive(false);

        PooledObject item = created.AddComponent<PooledObject>();
        item.Init(this, items.Count);
        items.Add(item);
    }

    private void Swap(int a, int b)
    {
        if (a == b) return;

        PooledObject itemA = items[a];
        PooledObject itemB = items[b];
        items[a] = itemB;
        items[b] = itemA;
        if (itemB != null) itemB.Index = a;
        if (itemA != null) itemA.Index = b;
    }

    private void RemoveFreeAt(int index)
    {
        int last = items.Count - 1;
        Swap(index, last);
        items.RemoveAt(last);
    }
}
