using UnityEngine;

namespace Deadlight.Core
{
    [CreateAssetMenu(fileName = "CampaignBalanceProfile", menuName = "Deadlight/Campaign Balance Profile")]
    public class CampaignBalanceProfile : ScriptableObject
    {
        [Header("Profile Info")]
        public string displayName = "Campaign Standard";
        [TextArea] public string description = "Standard balance tuning for the level-based campaign.";

        [Header("Player Modifiers")]
        [Tooltip("Multiplier for player's starting health")]
        public float playerHealthMultiplier = 1f;
        
        [Tooltip("Multiplier for damage taken by player")]
        public float playerDamageTakenMultiplier = 1f;

        [Header("Enemy Modifiers")]
        [Tooltip("Multiplier for enemy health")]
        public float enemyHealthMultiplier = 1f;
        
        [Tooltip("Multiplier for enemy damage dealt")]
        public float enemyDamageMultiplier = 1f;
        
        [Tooltip("Multiplier for enemy movement speed")]
        public float enemySpeedMultiplier = 1f;

        [Header("Wave Modifiers")]
        [Tooltip("Multiplier for number of enemies per wave")]
        public float waveEnemyCountMultiplier = 1f;
        
        [Tooltip("Multiplier for time between spawns (lower = faster)")]
        public float spawnIntervalMultiplier = 1f;

        [Header("Resource Modifiers")]
        [Tooltip("Multiplier for resource spawn rates")]
        public float resourceSpawnMultiplier = 1f;
        
        [Tooltip("Multiplier for ammo found in pickups")]
        public float ammoDropMultiplier = 1f;
        
        [Tooltip("Multiplier for health pickup effectiveness")]
        public float healthPickupMultiplier = 1f;

        [Header("Scoring")]
        [Tooltip("Score multiplier for leaderboard")]
        public float scoreMultiplier = 1f;

        public static CampaignBalanceProfile CreateDefaultProfile()
        {
            var settings = CreateInstance<CampaignBalanceProfile>();
            return settings;
        }
    }
}
