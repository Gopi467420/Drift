using Enums;
using UnityEngine;

// All the numbers that tune a run, in one asset, so they change without touching code.
// Create one with Assets > Create > Drift > Game Settings.
[CreateAssetMenu(fileName = "GameSettings", menuName = "Drift/Game Settings")]
public class GameSettings : ScriptableObject
{
    [System.Serializable]
    public struct RewardValues
    {
        public float stamina;
        public float score;
        public float scoreMultiplier;

        public RewardValues(float stamina, float score, float scoreMultiplier)
        {
            this.stamina = stamina;
            this.score = score;
            this.scoreMultiplier = scoreMultiplier;
        }

        public Rewards ToRewards(float scale = 1f)
        {
            return new Rewards(stamina * scale, score * scale, scoreMultiplier * scale);
        }
    }

    [System.Serializable]
    public struct CollectableReward
    {
        public CollectableType type;
        public RewardValues reward;

        public CollectableReward(CollectableType type, RewardValues reward)
        {
            this.type = type;
            this.reward = reward;
        }
    }

    [System.Serializable]
    public struct Difficulty
    {
        public string name;
        [Tooltip("How packed each zone is. Tighter zones leave smaller gaps.")]
        public ZoneType zone;
        [Min(1)] public int obstaclesPerZone;
        [Tooltip("How many coin paths each zone gets.")]
        [Min(0)] public int coinPathsPerZone;
        [Min(1)] public int coinsPerPath;
        [Tooltip("How much the score multiplier drops per second. Harder modes keep it longer, as their reward.")]
        [Min(0f)] public float multiplierDrain;
    }

    [Header("Difficulty")]
    public Difficulty[] difficulties =
    {
        new Difficulty { name = "Easy", zone = ZoneType.Open, obstaclesPerZone = 200, coinPathsPerZone = 1, coinsPerPath = 10, multiplierDrain = 0.15f },
        new Difficulty { name = "Normal", zone = ZoneType.Loose, obstaclesPerZone = 350, coinPathsPerZone = 1, coinsPerPath = 10, multiplierDrain = 0.1f },
        new Difficulty { name = "Hard", zone = ZoneType.Busy, obstaclesPerZone = 500, coinPathsPerZone = 2, coinsPerPath = 8, multiplierDrain = 0.06f },
        new Difficulty { name = "Expert", zone = ZoneType.Crowded, obstaclesPerZone = 650, coinPathsPerZone = 2, coinsPerPath = 8, multiplierDrain = 0.03f },
        new Difficulty { name = "Insane", zone = ZoneType.Choked, obstaclesPerZone = 800, coinPathsPerZone = 2, coinsPerPath = 8, multiplierDrain = 0.01f }
    };

    [Header("Game Speed")]
    [Tooltip("Speed of the whole game in normal play.")]
    [Min(0.01f)] public float normalSpeed = 3f;
    [Tooltip("Speed of the whole game during slow motion.")]
    [Min(0.01f)] public float slowMoSpeed = 0.5f;

    [Header("Score Multiplier")]
    public float scoreMultiplierMax = 10f;

    [Header("Power Ups")]
    [Tooltip("How many zones a multiplier or invincibility pickup lasts.")]
    [Min(0.1f)] public float powerUpZones = 1f;
    [Tooltip("What each multiplier pickup adds to the boost: x2, then x4, then x6.")]
    [Min(1f)] public float multiplierBoostPerPickup = 2f;
    [Min(1f)] public float multiplierBoostMax = 10f;

    [Header("Rewards")]
    public CollectableReward[] collectables =
    {
        new CollectableReward(CollectableType.Coin, new RewardValues(2f, 50f, 0f)),
        new CollectableReward(CollectableType.Multiplier, new RewardValues(5f, 100f, 0.5f)),
        new CollectableReward(CollectableType.Invincibility, new RewardValues(0f, 0f, 0f)),
        new CollectableReward(CollectableType.Attraction, new RewardValues(25f, 500f, 1f))
    };

    [Tooltip("Multiplied by the zone's level: Open is 1, Loose is 2, up to Choked at 5.")]
    public RewardValues zoneReward = new RewardValues(6f, 300f, 0.25f);

    public Rewards GetCollectableReward(CollectableType type)
    {
        foreach (CollectableReward entry in collectables)
            if (entry.type == type) return entry.reward.ToRewards();

        return new Rewards(0f, 0f, 0f);
    }

    public Rewards GetZoneReward(ZoneType zone)
    {
        return zoneReward.ToRewards((int)zone);
    }
}
