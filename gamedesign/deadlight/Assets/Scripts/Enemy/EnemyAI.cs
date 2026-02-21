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
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float chaseSpeed = 4f;

        [Header("State")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;
        [SerializeField] private bool isAggressive = false;

        [Header("References")]
        [SerializeField] private Transform target;

        private NavMeshAgent agent;
        private float lastAttackTime;
        private float damageMultiplier = 1f;
        private Animator animator;

        public EnemyState CurrentState => currentState;
        public float Damage => damage * damageMultiplier;
        public bool IsAggressive => isAggressive;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        private void Start()
        {
            SetupNavMeshAgent();
            FindTarget();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                
                if (GameManager.Instance.CurrentState == GameState.NightPhase)
                {
                    isAggressive = true;
                }
            }

            var dayNightCycle = FindObjectOfType<DayNightCycle>();
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
        }

        private void SetupNavMeshAgent()
        {
            agent.speed = moveSpeed;
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

            if (target == null) return;

            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            UpdateState(distanceToTarget);
            ExecuteStateBehavior(distanceToTarget);
            UpdateRotation();
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.NightPhase:
                    isAggressive = true;
                    break;
                case GameState.DayPhase:
                    isAggressive = false;
                    ChangeState(EnemyState.Idle);
                    break;
            }
        }

        private void OnNightStart()
        {
            isAggressive = true;
        }

        private void OnDayStart()
        {
            isAggressive = false;
            ChangeState(EnemyState.Idle);
        }

        private void UpdateState(float distanceToTarget)
        {
            if (!isAggressive)
            {
                if (currentState != EnemyState.Idle && currentState != EnemyState.Patrol)
                {
                    ChangeState(EnemyState.Idle);
                }
                return;
            }

            if (distanceToTarget <= attackRange)
            {
                ChangeState(EnemyState.Attack);
            }
            else if (distanceToTarget <= detectionRange)
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
            agent.speed = patrolSpeed;
        }

        private void HandleChase()
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;

            if (target != null)
            {
                agent.SetDestination(target.position);
            }
        }

        private void HandleAttack()
        {
            agent.isStopped = true;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            lastAttackTime = Time.time;

            if (target == null) return;

            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (distanceToTarget > attackRange * 1.2f) return;

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
            isAggressive = aggressive;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetSpeed(float speed)
        {
            moveSpeed = speed;
            chaseSpeed = speed * 1.33f;
        }

        public void OnDeath()
        {
            ChangeState(EnemyState.Dead);
            agent.isStopped = true;
            agent.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
