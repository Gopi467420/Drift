using UnityEngine;

// Anything MovementSystem keeps in a list remembers its slot, so it can be removed fast.
public interface IMovementEntry
{
    int SystemIndex { get; set; }
}

// Moves, spins and near-miss-checks every active object in ONE Update,
// instead of hundreds of separate Update() calls.
// MoveInLine, RandomRotator and HazardObstacle only register their data here.
// Created automatically the first time something registers.
public class MovementSystem : MonoBehaviour
{
    private static MovementSystem instance;

    private readonly PackedList<MoveInLine> movers = new();
    private readonly PackedList<RandomRotator> spinners = new();
    private readonly PackedList<HazardObstacle> hazards = new();

    private static MovementSystem Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("MovementSystem").AddComponent<MovementSystem>();

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    // value = velocity (units per second)
    public static void AddMover(MoveInLine mover, Vector3 velocity) { Instance.movers.Add(mover, velocity); }
    public static void RemoveMover(MoveInLine mover) { if (instance != null) instance.movers.Remove(mover); }

    // Read / change the velocity of a mover that is already registered.
    // Used to make a collectable travel at exactly the same speed as its special obstacle.
    public static bool TryGetMoverVelocity(MoveInLine mover, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        return instance != null && instance.movers.TryGet(mover, out velocity);
    }

    public static void SetMoverVelocity(MoveInLine mover, Vector3 velocity)
    {
        if (instance != null) instance.movers.Set(mover, velocity);
    }

    // value = rotation (degrees per second, local space)
    public static void AddSpinner(RandomRotator spinner, Vector3 rotation) { Instance.spinners.Add(spinner, rotation); }
    public static void RemoveSpinner(RandomRotator spinner) { if (instance != null) instance.spinners.Remove(spinner); }

    public static void AddHazard(HazardObstacle hazard) { Instance.hazards.Add(hazard, Vector3.zero); }
    public static void RemoveHazard(HazardObstacle hazard) { if (instance != null) instance.hazards.Remove(hazard); }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        Transform[] moverTransforms = movers.transforms;
        Vector3[] velocities = movers.values;

        for (int i = 0; i < movers.count; i++)
            moverTransforms[i].position += velocities[i] * deltaTime;

        Transform[] spinnerTransforms = spinners.transforms;
        Vector3[] rotations = spinners.values;

        for (int i = 0; i < spinners.count; i++)
            spinnerTransforms[i].Rotate(rotations[i] * deltaTime, Space.Self);

        if (hazards.count > 0 && GameManager.Instance != null && Player.Instance != null)
        {
            Vector3 playerPosition = GameManager.Instance.GetPlayerPosition();

            // Backwards, so a hazard that despawns during its check can't skip another.
            for (int i = hazards.count - 1; i >= 0; i--)
                hazards.owners[i].CheckNearMiss(playerPosition);
        }
    }

    // Flat arrays with all active entries packed at the front.
    // Removing swaps the last entry into the freed slot.
    private class PackedList<T> where T : Component, IMovementEntry
    {
        public T[] owners = new T[256];
        public Transform[] transforms = new Transform[256];
        public Vector3[] values = new Vector3[256];
        public int count;

        public void Add(T owner, Vector3 value)
        {
            if (owner.SystemIndex >= 0)
                return;

            if (count == owners.Length)
            {
                int newSize = owners.Length * 2;
                System.Array.Resize(ref owners, newSize);
                System.Array.Resize(ref transforms, newSize);
                System.Array.Resize(ref values, newSize);
            }

            owners[count] = owner;
            transforms[count] = owner.transform;
            values[count] = value;
            owner.SystemIndex = count;
            count++;
        }

        private bool IsRegistered(T owner)
        {
            int index = owner.SystemIndex;
            return index >= 0 && index < count && owners[index] == owner;
        }

        public bool TryGet(T owner, out Vector3 value)
        {
            value = Vector3.zero;
            if (!IsRegistered(owner)) return false;

            value = values[owner.SystemIndex];
            return true;
        }

        public void Set(T owner, Vector3 value)
        {
            if (IsRegistered(owner)) values[owner.SystemIndex] = value;
        }

        public void Remove(T owner)
        {
            int index = owner.SystemIndex;

            if (index < 0 || index >= count || owners[index] != owner)
                return;

            int last = count - 1;

            if (index != last)
            {
                owners[index] = owners[last];
                transforms[index] = transforms[last];
                values[index] = values[last];
                owners[index].SystemIndex = index;
            }

            owners[last] = null;
            transforms[last] = null;
            owner.SystemIndex = -1;
            count--;
        }
    }
}
