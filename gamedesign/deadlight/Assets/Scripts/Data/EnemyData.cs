using UnityEngine;

namespace Deadlight.Data
{
    public enum EnemyType
    {
        Basic,
        Runner,
        Tank,
        Exploder,
        Spitter,
        Boss
    }

    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Deadlight/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName = "Zombie";
        [TextArea] public string description;
        public EnemyType enemyType;
        public Sprite icon;
        public GameObject prefab;

        [Header("Health")]
        public float maxHealth = 50f;

        [Header("Combat")]
        [Tooltip("Damage dealt to player per attack")]
        public float damage = 10f;
        
        [Tooltip("Time between attacks in seconds")]
        public float attackCooldown = 1f;
        
        [Tooltip("Range at which enemy can attack")]
        public float attackRange = 1.5f;

        [Header("Movement")]
        [Tooltip("Base movement speed")]
        public float moveSpeed = 3f;
        
        [Tooltip("Speed when chasing player")]
        public float chaseSpeed = 4f;
        
        [Tooltip("Range at which enemy detects player")]
        public float detectionRange = 15f;

        [Header("Rewards")]
        [Tooltip("Points awarded when killed")]
        public int pointsOnKill = 10;
        
        [Tooltip("Chance to drop loot (0-1)")]
        [Range(0f, 1f)]
        public float dropChance = 0.15f;

        [Header("Audio")]
        public AudioClip idleSound;
        public AudioClip aggroSound;
        public AudioClip attackSound;
        public AudioClip hurtSound;
        public AudioClip deathSound;

        [Header("Visual")]
        public RuntimeAnimatorController animatorController;
        public Color tintColor = Color.white;

        [Header("Spawn Info")]
        [Tooltip("First night this enemy can appear")]
        public int minNight = 1;
        
        [Tooltip("Spawn weight (higher = more common)")]
        public float spawnWeight = 1f;

        public static EnemyData CreateBasicZombie()
        {
            var enemy = CreateInstance<EnemyData>();
            enemy.enemyName = "Zombie";
            enemy.description = "Standard undead. Slow but relentless.";
            enemy.enemyType = EnemyType.Basic;
            enemy.maxHealth = 50f;
            enemy.damage = 10f;
            enemy.attackCooldown = 1f;
            enemy.attackRange = 1.5f;
            enemy.moveSpeed = 2f;
            enemy.chaseSpeed = 3f;
            enemy.detectionRange = 12f;
            enemy.pointsOnKill = 10;
            enemy.dropChance = 0.15f;
            enemy.minNight = 1;
            enemy.spawnWeight = 1f;
            return enemy;
        }

        public static EnemyData CreateRunner()
        {
            var enemy = CreateInstance<EnemyData>();
            enemy.enemyName = "Runner";
            enemy.description = "Fast but fragile. Attacks quickly.";
            enemy.enemyType = EnemyType.Runner;
            enemy.maxHealth = 30f;
            enemy.damage = 8f;
            enemy.attackCooldown = 0.6f;
            enemy.attackRange = 1.2f;
            enemy.moveSpeed = 4f;
            enemy.chaseSpeed = 6f;
            enemy.detectionRange = 18f;
            enemy.pointsOnKill = 15;
            enemy.dropChance = 0.1f;
            enemy.minNight = 3;
            enemy.spawnWeight = 0.6f;
            return enemy;
        }

        public static EnemyData CreateTank()
        {
            var enemy = CreateInstance<EnemyData>();
            enemy.enemyName = "Tank";
            enemy.description = "Heavily armored. Slow but extremely dangerous.";
            enemy.enemyType = EnemyType.Tank;
            enemy.maxHealth = 200f;
            enemy.damage = 25f;
            enemy.attackCooldown = 1.5f;
            enemy.attackRange = 2f;
            enemy.moveSpeed = 1.5f;
            enemy.chaseSpeed = 2f;
            enemy.detectionRange = 10f;
            enemy.pointsOnKill = 50;
            enemy.dropChance = 0.4f;
            enemy.minNight = 4;
            enemy.spawnWeight = 0.3f;
            return enemy;
        }

        public static EnemyData CreateExploder()
        {
            var enemy = CreateInstance<EnemyData>();
            enemy.enemyName = "Exploder";
            enemy.description = "Explodes on death. Keep your distance!";
            enemy.enemyType = EnemyType.Exploder;
            enemy.maxHealth = 40f;
            enemy.damage = 30f;
            enemy.attackCooldown = 0f;
            enemy.attackRange = 3f;
            enemy.moveSpeed = 3f;
            enemy.chaseSpeed = 4.5f;
            enemy.detectionRange = 15f;
            enemy.pointsOnKill = 20;
            enemy.dropChance = 0.05f;
            enemy.minNight = 3;
            enemy.spawnWeight = 0.4f;
            return enemy;
        }

        public static EnemyData CreateBoss()
        {
            var enemy = CreateInstance<EnemyData>();
            enemy.enemyName = "Abomination";
            enemy.description = "The ultimate threat. Final boss of Night 5.";
            enemy.enemyType = EnemyType.Boss;
            enemy.maxHealth = 1000f;
            enemy.damage = 40f;
            enemy.attackCooldown = 2f;
            enemy.attackRange = 3f;
            enemy.moveSpeed = 2f;
            enemy.chaseSpeed = 3.5f;
            enemy.detectionRange = 30f;
            enemy.pointsOnKill = 500;
            enemy.dropChance = 1f;
            enemy.minNight = 5;
            enemy.spawnWeight = 0f;
            return enemy;
        }
    }
}
