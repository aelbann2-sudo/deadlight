using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Enemy
{
    public class SimpleEnemyAI : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.4f;
        [SerializeField] private float chaseSpeed = 2.45f;
        [SerializeField] private float detectionRange = 25f;
        [SerializeField] private float attackRange = 1f;
        
        [Header("Combat")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackWindup = 0.12f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.8f, 0.25f, 1f);
        
        [Header("Behavior")]
        [SerializeField] private bool alwaysAggressive = false;
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float wanderInterval = 1.5f;

        [Header("Phase Aggression")]
        [SerializeField] private float dayDetectionRangeMultiplier = 0.55f;
        [SerializeField] private float dayChaseSpeedMultiplier = 0.82f;
        [SerializeField] private float dayAttackCooldownMultiplier = 1.35f;
        [SerializeField] private float nightDetectionRangeMultiplier = 1.15f;
        [SerializeField] private float nightChaseSpeedMultiplier = 1.08f;
        [SerializeField] private float nightAttackCooldownMultiplier = 0.9f;
        [SerializeField] private float dayPursuitMemoryDuration = 5f;
        [SerializeField] private float attackCommitRangeMultiplier = 1.15f;
        
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
        private Vector3 startPosition;
        private float speedMultiplier = 1f;
        private float damageMultiplier = 1f;
        private float baseMoveSpeed;
        private float baseChaseSpeed;
        private bool isAttackWindingUp;
        private Color baseColor = Color.white;
        private EnemyAggressionPhase aggressionPhase;

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
            SetNewWanderTarget();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                RefreshAggressionPhase(GameManager.Instance.CurrentState);
            }
            else
            {
                aggressionPhase = alwaysAggressive ? EnemyAggressionPhase.NightHunt : EnemyAggressionPhase.DayStalk;
                UpdateAudioAggression();
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
            RefreshAggressionPhase(newState);
        }

        private void FindPlayer()
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

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

            if (aggressionPhase == EnemyAggressionPhase.Dormant)
            {
                rb.linearVelocity = Vector2.zero;
                UpdateVisuals();
                return;
            }

            if (aggressionPhase == EnemyAggressionPhase.DayStalk && target == null)
            {
                Wander();
                UpdateVisuals();
                return;
            }

            FindPlayer();

            if (target == null)
            {
                if (Time.time - lastSeenPlayerTime < GetEffectivePursuitMemoryDuration() && lastKnownPlayerPosition != Vector3.zero)
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
            float effectiveDetectionRange = GetEffectiveDetectionRange();

            if (distanceToPlayer > effectiveDetectionRange)
            {
                if (Time.time - lastSeenPlayerTime < GetEffectivePursuitMemoryDuration() && lastKnownPlayerPosition != Vector3.zero)
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
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = ((Vector2)lastKnownPlayerPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * (GetEffectiveChaseSpeed() * 0.85f);
        }

        private void ChasePlayer()
        {
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * GetEffectiveChaseSpeed();
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
            
            if (Time.time - lastAttackTime < GetEffectiveAttackCooldown()) return;
            
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
                float distanceToTarget = Vector2.Distance(transform.position, target.position);
                if (distanceToTarget > attackRange * attackCommitRangeMultiplier)
                {
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = baseColor;
                    }

                    isAttackWindingUp = false;
                    yield break;
                }

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
            aggressionPhase = aggressive ? EnemyAggressionPhase.NightHunt : EnemyAggressionPhase.Dormant;
            UpdateAudioAggression();
        }

        public void ApplySpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void MultiplySpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, speedMultiplier * Mathf.Max(0.1f, multiplier));
        }

        public void ApplyDamageMultiplier(float multiplier)
        {
            damageMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void MultiplyDamageMultiplier(float multiplier)
        {
            damageMultiplier = Mathf.Max(0.1f, damageMultiplier * Mathf.Max(0.1f, multiplier));
        }

        private void RefreshAggressionPhase(GameState state)
        {
            aggressionPhase = EnemyAggressionResolver.Resolve(state, alwaysAggressive);
            UpdateAudioAggression();
        }

        private void UpdateAudioAggression()
        {
            var zombieSounds = GetComponent<Audio.ZombieSounds>();
            if (zombieSounds != null)
            {
                zombieSounds.SetAggressive(aggressionPhase == EnemyAggressionPhase.NightHunt);
            }
        }

        private float GetEffectiveDetectionRange()
        {
            return aggressionPhase switch
            {
                EnemyAggressionPhase.NightHunt => detectionRange * nightDetectionRangeMultiplier,
                EnemyAggressionPhase.DayStalk => detectionRange * dayDetectionRangeMultiplier,
                _ => 0f
            };
        }

        private float GetEffectiveChaseSpeed()
        {
            float phaseMultiplier = aggressionPhase switch
            {
                EnemyAggressionPhase.NightHunt => nightChaseSpeedMultiplier,
                EnemyAggressionPhase.DayStalk => dayChaseSpeedMultiplier,
                _ => 0f
            };

            return baseChaseSpeed * speedMultiplier * phaseMultiplier;
        }

        private float GetEffectiveAttackCooldown()
        {
            float phaseMultiplier = aggressionPhase switch
            {
                EnemyAggressionPhase.NightHunt => nightAttackCooldownMultiplier,
                EnemyAggressionPhase.DayStalk => dayAttackCooldownMultiplier,
                _ => float.PositiveInfinity
            };

            return attackCooldown * phaseMultiplier;
        }

        private float GetEffectivePursuitMemoryDuration()
        {
            return aggressionPhase switch
            {
                EnemyAggressionPhase.NightHunt => pursuitMemoryDuration,
                EnemyAggressionPhase.DayStalk => dayPursuitMemoryDuration,
                _ => 0f
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? GetEffectiveDetectionRange() : detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, wanderRadius);
        }
    }
}
