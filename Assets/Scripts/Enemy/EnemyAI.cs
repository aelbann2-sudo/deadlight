using UnityEngine;
using UnityEngine.AI;
using Deadlight.Core;

namespace Deadlight.Enemy
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float damage = 10f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float patrolSpeed = 0.75f;
        [SerializeField] private float chaseSpeed = 2f;

        [Header("Telegraph")]
        [SerializeField] private float attackWindup = 0.15f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.8f, 0.25f, 1f);

        [Header("Phase Aggression")]
        [SerializeField] private float dayDetectionRangeMultiplier = 0.5f;
        [SerializeField] private float dayChaseSpeedMultiplier = 0.85f;
        [SerializeField] private float dayAttackCooldownMultiplier = 1.4f;
        [SerializeField] private float nightDetectionRangeMultiplier = 1.15f;
        [SerializeField] private float nightChaseSpeedMultiplier = 1.08f;
        [SerializeField] private float nightAttackCooldownMultiplier = 0.9f;
        [SerializeField] private float attackCommitRangeMultiplier = 1.15f;

        [Header("State")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;

        [Header("References")]
        [SerializeField] private Transform target;

        private NavMeshAgent agent;
        private float lastAttackTime;
        private float damageMultiplier = 1f;
        private float speedMultiplier = 1f;
        private Animator animator;
        private float baseMoveSpeed;
        private float basePatrolSpeed;
        private float baseChaseSpeed;
        private DayNightCycle dayNightCycle;
        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private bool isWindingUpAttack;
        private EnemyAggressionPhase aggressionPhase;

        public EnemyState CurrentState => currentState;
        public float Damage => damage * damageMultiplier;
        public bool IsAggressive => aggressionPhase != EnemyAggressionPhase.Dormant;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            agent.updateRotation = false;
            agent.updateUpAxis = false;

            baseMoveSpeed = moveSpeed;
            basePatrolSpeed = patrolSpeed;
            baseChaseSpeed = chaseSpeed;
        }

        private void Start()
        {
            SetupNavMeshAgent();
            FindTarget();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                RefreshAggressionPhase(GameManager.Instance.CurrentState);
            }
            else
            {
                aggressionPhase = EnemyAggressionPhase.DayStalk;
            }

            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.OnNightStart += OnNightStart;
                dayNightCycle.OnDayStart += OnDayStart;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            if (dayNightCycle != null)
            {
                dayNightCycle.OnNightStart -= OnNightStart;
                dayNightCycle.OnDayStart -= OnDayStart;
            }
        }

        private void SetupNavMeshAgent()
        {
            agent.speed = baseMoveSpeed * speedMultiplier;
            agent.stoppingDistance = attackRange * 0.8f;
            agent.acceleration = 8f;
            agent.angularSpeed = 360f;
        }

        private void FindTarget()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void Update()
        {
            if (currentState == EnemyState.Dead) return;

            FindTarget();

            if (target == null)
            {
                isWindingUpAttack = false;
                return;
            }

            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            UpdateState(distanceToTarget);
            ExecuteStateBehavior(distanceToTarget);
            UpdateRotation();
        }

        private void HandleGameStateChanged(GameState newState)
        {
            RefreshAggressionPhase(newState);

            if (aggressionPhase == EnemyAggressionPhase.Dormant)
            {
                ChangeState(EnemyState.Idle);
            }
        }

        private void OnNightStart()
        {
            RefreshAggressionPhase(GameState.NightPhase);
        }

        private void OnDayStart()
        {
            RefreshAggressionPhase(GameState.DayPhase);
        }

        private void UpdateState(float distanceToTarget)
        {
            if (aggressionPhase == EnemyAggressionPhase.Dormant)
            {
                if (currentState != EnemyState.Idle && currentState != EnemyState.Patrol)
                {
                    ChangeState(EnemyState.Idle);
                }
                return;
            }

            float effectiveDetectionRange = GetEffectiveDetectionRange();

            if (distanceToTarget <= attackRange)
            {
                ChangeState(EnemyState.Attack);
            }
            else if (distanceToTarget <= effectiveDetectionRange)
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                ChangeState(EnemyState.Patrol);
            }
        }

        private void ExecuteStateBehavior(float distanceToTarget)
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    HandleIdle();
                    break;
                case EnemyState.Patrol:
                    HandlePatrol();
                    break;
                case EnemyState.Chase:
                    HandleChase();
                    break;
                case EnemyState.Attack:
                    HandleAttack();
                    break;
            }
        }

        private void HandleIdle()
        {
            agent.isStopped = true;
            agent.speed = 0;
        }

        private void HandlePatrol()
        {
            agent.isStopped = false;
            agent.speed = basePatrolSpeed * speedMultiplier;
        }

        private void HandleChase()
        {
            agent.isStopped = false;
            agent.speed = GetEffectiveChaseSpeed();

            if (target != null)
            {
                agent.SetDestination(target.position);
            }
        }

        private void HandleAttack()
        {
            agent.isStopped = true;

            if (Time.time >= lastAttackTime + GetEffectiveAttackCooldown())
            {
                StartCoroutine(PerformAttack());
            }
        }

        private System.Collections.IEnumerator PerformAttack()
        {
            if (isWindingUpAttack)
            {
                yield break;
            }

            isWindingUpAttack = true;
            lastAttackTime = Time.time;

            if (target == null)
            {
                isWindingUpAttack = false;
                yield break;
            }

            var worldUi = GetComponent<Deadlight.UI.EnemyWorldUI>();
            worldUi?.ShowTelegraph(attackWindup + 0.1f);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = telegraphColor;
            }

            yield return new WaitForSeconds(attackWindup);

            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (distanceToTarget <= attackRange * attackCommitRangeMultiplier)
            {
                var playerHealth = target.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(Damage);
                    Debug.Log($"[EnemyAI] Attacked player for {Damage} damage");
                }

                if (animator != null)
                {
                    animator.SetTrigger("Attack");
                }
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            isWindingUpAttack = false;
        }

        private void UpdateRotation()
        {
            if (agent.velocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(agent.velocity.y, agent.velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
            else if (target != null && currentState == EnemyState.Attack)
            {
                Vector2 direction = (target.position - transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        private void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;

            currentState = newState;

            if (animator != null)
            {
                animator.SetInteger("State", (int)newState);
            }
        }

        public void ApplyDamageMultiplier(float multiplier)
        {
            damageMultiplier = multiplier;
        }

        public void SetAggressive(bool aggressive)
        {
            aggressionPhase = aggressive ? EnemyAggressionPhase.NightHunt : EnemyAggressionPhase.Dormant;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetSpeed(float speed)
        {
            baseMoveSpeed = speed;
            basePatrolSpeed = speed * 0.5f;
            baseChaseSpeed = speed * 1.33f;
        }

        public void ApplySpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void MultiplySpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, speedMultiplier * Mathf.Max(0.1f, multiplier));
        }

        public void MultiplyDamageMultiplier(float multiplier)
        {
            damageMultiplier = Mathf.Max(0.1f, damageMultiplier * Mathf.Max(0.1f, multiplier));
        }

        public void OnDeath()
        {
            ChangeState(EnemyState.Dead);
            agent.isStopped = true;
            agent.enabled = false;
        }

        private void RefreshAggressionPhase(GameState state)
        {
            aggressionPhase = EnemyAggressionResolver.Resolve(state);

            if (agent != null)
            {
                agent.stoppingDistance = attackRange * (aggressionPhase == EnemyAggressionPhase.NightHunt ? 0.9f : 0.75f);
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? GetEffectiveDetectionRange() : detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
