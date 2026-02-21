using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Enemy
{
    public class SimpleEnemyAI : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float detectionRange = 12f;
        [SerializeField] private float attackRange = 1f;
        
        [Header("Combat")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackCooldown = 1f;
        
        [Header("Behavior")]
        [SerializeField] private bool alwaysAggressive = true;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderInterval = 3f;
        
        private Transform target;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private EnemyHealth health;
        
        private Vector2 wanderTarget;
        private float lastWanderTime;
        private float lastAttackTime;
        private bool isAggressive;
        private Vector3 startPosition;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            health = GetComponent<EnemyHealth>();
            startPosition = transform.position;
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
            if (health != null && health.IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            
            FindPlayer();
            
            if (target == null)
            {
                Wander();
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, target.position);

            if (isAggressive && distanceToPlayer <= detectionRange)
            {
                if (distanceToPlayer <= attackRange)
                {
                    Attack();
                }
                else
                {
                    ChasePlayer();
                }
            }
            else
            {
                Wander();
            }

            UpdateVisuals();
        }

        private void ChasePlayer()
        {
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * chaseSpeed;
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
            rb.linearVelocity = direction * moveSpeed;
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
            
            var playerHealth = target.GetComponent<Player.PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
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
