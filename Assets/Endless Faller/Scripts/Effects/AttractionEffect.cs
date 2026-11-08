using System.Collections.Generic;
using UnityEngine;

// The Attraction pickup: every coin in play flies to the player, along with two fresh
// streams of coins. They all swing out to the side and arrive level with the player,
// never through the space below, so they don't cover the obstacles coming up.
// Times are game time, so pause stops them and slow motion slows them.
public class AttractionEffect : MonoBehaviour
{
    [Tooltip("Game seconds a coin takes to reach the player. The game runs at 3x, so 3 is 1 real second.")]
    [SerializeField, Min(0.1f)] private float flightTime = 3f;
    [Tooltip("How far to the side the coins swing out. Should be past the edge of the screen.")]
    [SerializeField, Min(1f)] private float sideDistance = 140f;
    [Tooltip("How many fresh coins each of the two streams brings.")]
    [SerializeField, Min(0)] private int coinsPerStream = 10;
    [Tooltip("Game seconds between the coins of a stream.")]
    [SerializeField, Min(0f)] private float streamSpacing = 0.3f;

    [Header("Size")]
    [Tooltip("Coins start shrinking this close to the player, or they would fill the screen.")]
    [SerializeField, Min(1f)] private float shrinkDistance = 150f;
    [Tooltip("Size on arrival, as a share of the full coin.")]
    [Range(0.01f, 1f)][SerializeField] private float smallestSize = 0.03f;

    // Bends the path, so the streams curl in rather than arrive in a straight line.
    private const float CurlAngle = 50f;
    private const float StreamStartDistance = 2.5f;

    private class Flight
    {
        public Collectable coin;
        public Vector3 start;
        public Vector3 side;
        public float age;
    }

    private readonly List<Flight> flights = new();

    public void Play()
    {
        ObjectPoolManager pools = ObjectPoolManager.Instance;
        GameObject coinPrefab = pools.PickRandom(PoolCategory.Collectable);
        if (coinPrefab == null) return;

        Vector3 player = GameManager.Instance.GetPlayerPosition();
        Vector3 side = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.right;

        ObjectPool pool = pools.GetPool(coinPrefab);

        // Each coin in play takes the side it is already on.
        for (int i = 0; i < pool.ActiveCount; i++)
        {
            Collectable coin = pool[i].Get<Collectable>();

            if (coin != null && !coin.IsFlying)
            {
                bool isOnFarSide = Vector3.Dot(coin.transform.position - player, side) < 0f;
                Launch(coin, isOnFarSide ? -side : side, 0f);
            }
        }

        for (int i = 0; i < coinsPerStream; i++)
        {
            SpawnStreamCoin(pools, coinPrefab, player, side, i);
            SpawnStreamCoin(pools, coinPrefab, player, -side, i);
        }
    }

    private void SpawnStreamCoin(ObjectPoolManager pools, GameObject prefab, Vector3 player, Vector3 side, int index)
    {
        Vector3 start = player + side * (sideDistance * StreamStartDistance);
        GameObject spawned = pools.Spawn(prefab, start);

        if (spawned != null && spawned.TryGetComponent(out Collectable coin))
            Launch(coin, side, index * streamSpacing);
    }

    // A delayed coin waits at its start, which is off screen.
    private void Launch(Collectable coin, Vector3 side, float delay)
    {
        coin.BeginFlight();

        flights.Add(new Flight
        {
            coin = coin,
            start = coin.transform.position,
            side = side,
            age = -delay
        });
    }

    private void Update()
    {
        if (flights.Count == 0 || !GameManager.Instance.IsPlaying) return;

        Vector3 player = GameManager.Instance.GetPlayerPosition();

        for (int i = flights.Count - 1; i >= 0; i--)
        {
            Flight flight = flights[i];

            // Something else took the coin back, like the pool clearing what passed the player.
            if (!flight.coin.IsFlying)
            {
                flights.RemoveAt(i);
                continue;
            }

            flight.age += Time.deltaTime;

            if (flight.age < 0f) continue;

            float t = Mathf.Clamp01(flight.age / flightTime);

            if (t >= 1f)
            {
                flights.RemoveAt(i);
                flight.coin.Land();
                continue;
            }

            Move(flight, player, t);
        }
    }

    // t runs 0 to 1 over the flight. Squared, so coins start slowly and rush in like a magnet.
    private void Move(Flight flight, Vector3 player, float t)
    {
        float along = t * t;
        Vector3 swing = player + Quaternion.Euler(0f, CurlAngle, 0f) * flight.side * sideDistance;

        // A curve from the start, past the side point, to the player.
        Vector3 position = Vector3.Lerp(
            Vector3.Lerp(flight.start, swing, along),
            Vector3.Lerp(swing, player, along),
            along);

        // Coins from below shrink as they go, or they would pass in front of the obstacles.
        float closeness = Vector3.Distance(position, player) / shrinkDistance;
        float remaining = 1f - t;
        float size = Mathf.Min(closeness * closeness, remaining * remaining);

        flight.coin.transform.position = position;
        flight.coin.SetFlightSize(Mathf.Clamp(size, smallestSize, 1f));
    }
}
