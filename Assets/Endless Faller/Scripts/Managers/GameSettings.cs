using Enums;
using UnityEngine;

// All the numbers that tune a run, in one asset, so they change without touching code.
// Create one with Assets > Create > Endless Faller > Game Settings.
[CreateAssetMenu(fileName = "GameSettings", menuName = "Endless Faller/Game Settings")]
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
    }

    [Header("Difficulty")]
    public Difficulty[] difficulties =
    {
        new Difficulty { name = "Easy", zone = ZoneType.Open, obstaclesPerZone = 180, coinPathsPerZone = 6, coinsPerPath = 10 },
        new Difficulty { name = "Medium", zone = ZoneType.Busy, obstaclesPerZone = 240, coinPathsPerZone = 5, coinsPerPath = 10 },
        new Difficulty { name = "Hard", zone = ZoneType.Choked, obstaclesPerZone = 300, coinPathsPerZone = 4, coinsPerPath = 8 }
    };

    [Header("Game Speed")]
    [Tooltip("Speed of the whole game in normal play.")]
    [Min(0.01f)] public float normalSpeed = 3f;
    [Tooltip("Speed of the whole game during slow motion.")]
    [Min(0.01f)] public float slowMoSpeed = 0.5f;

    [Header("Score Multiplier")]
    public float scoreMultiplierMax = 10f;
    [Tooltip("How much the multiplier drops per second.")]
    public float scoreMultiplierDrain = 0.15f;

    [Header("Rewards")]
    public CollectableReward[] collectables =
    {
        new CollectableReward(CollectableType.C1, new RewardValues(2f, 50f, 0.05f)),
        new CollectableReward(CollectableType.C2, new RewardValues(0f, 100f, 0.5f)),
        new CollectableReward(CollectableType.C3, new RewardValues(10f, 250f, 1f)),
        new CollectableReward(CollectableType.C4, new RewardValues(25f, 500f, 1f))
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
