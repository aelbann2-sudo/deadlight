using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Enemy
{
    public class SimpleEnemyAI : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.8f;
        [SerializeField] private float chaseSpeed = 4.9f;
        [SerializeField] private float detectionRange = 25f;
        [SerializeField] private float attackRange = 1f;
        
        [Header("Combat")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackWindup = 0.12f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.8f, 0.25f, 1f);
        
        [Header("Behavior")]
        [SerializeField] private bool alwaysAggressive = true;
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float wanderInterval = 1.5f;
        
        [Header("Pursuit Memory")]
        [SerializeField] private float pursuitMemoryDuration = 30f;
        private float lastSeenPlayerTime;
        private Vector3 lastKnownPlayerPosition;
        
        private Transform target;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private EnemyHealth health;
        
        private Vector2 wanderTarget;
        private float lastWanderTime;
        private float lastAttackTime;
        private bool isAggressive;
        private Vector3 startPosition;
        private float speedMultiplier = 1f;
        private float damageMultiplier = 1f;
        private float baseMoveSpeed;
        private float baseChaseSpeed;
        private bool isAttackWindingUp;
        private Color baseColor = Color.white;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            health = GetComponent<EnemyHealth>();
            startPosition = transform.position;
            baseMoveSpeed = moveSpeed;
            baseChaseSpeed = chaseSpeed;
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }
        }

        private void Start()
        {
            FindPlayer();
            isAggressive = alwaysAggressive;
            SetNewWanderTarget();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                isAggressive = GameManager.Instance.CurrentState == GameState.NightPhase || alwaysAggressive;
            }

            var zombieSounds = GetComponent<Audio.ZombieSounds>();
            if (zombieSounds != null) zombieSounds.SetAggressive(isAggressive);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (alwaysAggressive) return;
            
            isAggressive = newState == GameState.NightPhase;
            var zombieSounds = GetComponent<Audio.ZombieSounds>();
            if (zombieSounds != null) zombieSounds.SetAggressive(isAggressive);
        }

        private void FindPlayer()
        {
            if (target != null) return;
            
            var player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            
            FindPlayer();
            
            if (target == null)
            {
                if (Time.time - lastSeenPlayerTime < pursuitMemoryDuration && lastKnownPlayerPosition != Vector3.zero)
                {
                    ChaseLastKnownPosition();
                }
                else
                {
                    Wander();
                }
                UpdateVisuals();
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, target.position);
            
            lastSeenPlayerTime = Time.time;
            lastKnownPlayerPosition = target.position;

            if (distanceToPlayer <= attackRange)
            {
                Attack();
            }
            else
            {
                ChasePlayer();
            }

            UpdateVisuals();
        }
        
        private void ChaseLastKnownPosition()
        {
            float dist = Vector2.Distance(transform.position, lastKnownPlayerPosition);
            if (dist < 1f)
            {
                lastKnownPlayerPosition = Vector3.zero;
                return;
            }
            Vector2 direction = ((Vector2)lastKnownPlayerPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * (baseChaseSpeed * speedMultiplier * 0.8f);
        }

        private void ChasePlayer()
        {
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * (baseChaseSpeed * speedMultiplier);
        }

        private void Wander()
        {
            if (Time.time - lastWanderTime > wanderInterval)
            {
                SetNewWanderTarget();
            }

            float distToWander = Vector2.Distance(transform.position, wanderTarget);
            
            if (distToWander < 0.5f)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * (baseMoveSpeed * speedMultiplier);
        }

        private void SetNewWanderTarget()
        {
            lastWanderTime = Time.time;
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = (Vector2)startPosition + randomOffset;
        }

        private void Attack()
        {
            rb.linearVelocity = Vector2.zero;
            
            if (Time.time - lastAttackTime < attackCooldown) return;
            
            lastAttackTime = Time.time;
            StartCoroutine(AttackRoutine());
        }

        private System.Collections.IEnumerator AttackRoutine()
        {
            if (isAttackWindingUp || target == null)
            {
                yield break;
            }

            isAttackWindingUp = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = telegraphColor;
            }

            var worldUi = GetComponent<Deadlight.UI.EnemyWorldUI>();
            worldUi?.ShowTelegraph(attackWindup + 0.1f);
            yield return new WaitForSeconds(attackWindup);

            if (target != null)
            {
                var playerHealth = target.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage * damageMultiplier);
                }
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            isAttackWindingUp = false;
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer == null) return;
            
            if (rb.linearVelocity.x < -0.1f)
            {
                spriteRenderer.flipX = true;
            }
            else if (rb.linearVelocity.x > 0.1f)
            {
                spriteRenderer.flipX = false;
            }
        }

        public void SetAggressive(bool aggressive)
        {
            isAggressive = aggressive;
        }

        public void ApplySpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void ApplyDamageMultiplier(float multiplier)
        {
            damageMultiplier = Mathf.Max(0.1f, multiplier);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, wanderRadius);
        }
    }
}
