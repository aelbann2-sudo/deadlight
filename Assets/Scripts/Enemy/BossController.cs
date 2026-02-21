using UnityEngine;
using System.Collections;
using Deadlight.Core;
using Deadlight.Visuals;

namespace Deadlight.Enemy
{
    public enum BossPhase { Phase1, Phase2, Phase3, Dead }
    
    public class BossController : MonoBehaviour
    {
        private EnemyHealth health;
        private SimpleEnemyAI ai;
        private SpriteRenderer spriteRenderer;
        private Transform player;
        private BossPhase currentPhase = BossPhase.Phase1;
        
        [Header("Boss Stats")]
        private float chargeSpeed = 12f;
        private float chargeDamage = 40f;
        private float slamRadius = 4f;
        private float slamDamage = 30f;
        
        private float spawnTimer;
        private float attackTimer;
        private bool isCharging;
        private bool isSlamming;
        private float phaseCheckTimer;
        
        void Start()
        {
            health = GetComponent<EnemyHealth>();
            ai = GetComponent<SimpleEnemyAI>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            var playerObj = GameObject.Find("Player");
            if (playerObj != null) player = playerObj.transform;
            
            if (health != null)
            {
                health.OnDamageTaken += OnDamaged;
                health.OnEnemyDeath += OnDeath;
            }
            
            transform.localScale = Vector3.one * 2f;
            
            StartCoroutine(DramaticEntrance());
        }
        
        private IEnumerator DramaticEntrance()
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.TriggerScreenShake(1f, 0.8f);
            }
            
            if (GameEffects.Instance != null)
            {
                GameEffects.Instance.FlashScreen(new Color(0.5f, 0f, 0f, 0.5f), 0.3f);
            }
            
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(0.5f);
            
            if (RadioTransmissions.Instance != null)
                RadioTransmissions.Instance.ShowSubject23Warning();
            
            yield return new WaitForSecondsRealtime(2f);
            Time.timeScale = 1f;
            
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.TriggerScreenShake(0.5f, 0.3f);
            }
        }
        
        void Update()
        {
            if (currentPhase == BossPhase.Dead || health == null || !health.IsAlive) return;
            
            CheckPhaseTransition();
            HandlePhase();
        }
        
        void CheckPhaseTransition()
        {
            phaseCheckTimer += Time.deltaTime;
            if (phaseCheckTimer < 0.5f) return;
            phaseCheckTimer = 0f;
            
            float hpPercent = health.HealthPercentage;
            BossPhase newPhase = currentPhase;
            
            if (hpPercent <= 0.3f) newPhase = BossPhase.Phase3;
            else if (hpPercent <= 0.6f) newPhase = BossPhase.Phase2;
            
            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged();
            }
        }
        
        void OnPhaseChanged()
        {
            string msg = currentPhase switch
            {
                BossPhase.Phase2 => "SUBJECT 23 IS ADAPTING! DON'T LET UP!",
                BossPhase.Phase3 => "IT'S ENRAGED! EVERYTHING OR NOTHING!",
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(msg) && RadioTransmissions.Instance != null)
                RadioTransmissions.Instance.ShowMessage(msg, 3f);
            
            if (currentPhase == BossPhase.Phase2 && ai != null)
                ai.ApplySpeedMultiplier(1.25f);
            
            if (currentPhase == BossPhase.Phase3)
            {
                if (ai != null) ai.ApplySpeedMultiplier(1.5f);
                if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0.2f);
            }
        }
        
        void HandlePhase()
        {
            spawnTimer += Time.deltaTime;
            attackTimer += Time.deltaTime;
            
            float spawnInterval = currentPhase switch
            {
                BossPhase.Phase1 => 15f,
                BossPhase.Phase2 => 10f,
                BossPhase.Phase3 => 8f,
                _ => 999f
            };
            
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnMinions();
            }
            
            if (currentPhase >= BossPhase.Phase2 && attackTimer >= 5f && !isCharging && !isSlamming)
            {
                attackTimer = 0f;
                if (currentPhase == BossPhase.Phase3 && Random.value > 0.5f)
                    StartCoroutine(SlamAttack());
                else
                    StartCoroutine(ChargeAttack());
            }
        }
        
        void SpawnMinions()
        {
            int count = currentPhase switch
            {
                BossPhase.Phase1 => 3,
                BossPhase.Phase2 => 4,
                BossPhase.Phase3 => 3,
                _ => 0
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = (Vector3)(Random.insideUnitCircle.normalized * 3f);
                var minion = new GameObject("BossMinion");
                minion.transform.position = transform.position + offset;
                
                var sr = minion.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateZombieSprite(
                    currentPhase >= BossPhase.Phase2 ? ProceduralSpriteGenerator.ZombieType.Runner : ProceduralSpriteGenerator.ZombieType.Basic, 0, 0);
                sr.sortingOrder = 9;
                
                var rb = minion.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                
                minion.AddComponent<CircleCollider2D>().radius = 0.35f;
                
                var hp = minion.AddComponent<EnemyHealth>();
                hp.SetMaxHealth(currentPhase >= BossPhase.Phase2 ? 20f : 30f);
                hp.SetPointsOnDeath(10);
                
                minion.AddComponent<SimpleEnemyAI>();
            }
        }
        
        IEnumerator ChargeAttack()
        {
            if (player == null) yield break;
            isCharging = true;
            
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                for (float t = 0; t < 0.5f; t += Time.deltaTime)
                {
                    spriteRenderer.color = Color.Lerp(c, Color.yellow, Mathf.PingPong(t * 8f, 1f));
                    yield return null;
                }
                spriteRenderer.color = c;
            }
            
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            float elapsed = 0f;
            var rb = GetComponent<Rigidbody2D>();
            
            while (elapsed < 0.6f && rb != null)
            {
                rb.linearVelocity = dir * chargeSpeed;
                elapsed += Time.deltaTime;
                
                if (player != null && Vector2.Distance(transform.position, player.position) < 1.5f)
                {
                    var ph = player.GetComponent<Player.PlayerHealth>();
                    if (ph != null) ph.TakeDamage(chargeDamage);
                    
                    if (VFXManager.Instance != null)
                        VFXManager.Instance.TriggerScreenShake(0.5f, 0.3f);
                    break;
                }
                
                yield return null;
            }
            
            if (rb != null) rb.linearVelocity = Vector2.zero;
            isCharging = false;
        }
        
        IEnumerator SlamAttack()
        {
            if (player == null) yield break;
            isSlamming = true;
            
            Vector3 startPos = transform.position;
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * (2f + t * 2f);
                yield return null;
            }
            
            transform.localScale = Vector3.one * 2f;
            
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.TriggerScreenShake(0.8f, 0.5f);
                VFXManager.Instance.PlayDeathExplosion(transform.position, false);
            }
            
            var hits = Physics2D.OverlapCircleAll(transform.position, slamRadius);
            foreach (var hit in hits)
            {
                var ph = hit.GetComponent<Player.PlayerHealth>();
                if (ph != null)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float falloff = 1f - (dist / slamRadius);
                    ph.TakeDamage(slamDamage * falloff);
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            isSlamming = false;
        }
        
        void OnDamaged(float dmg)
        {
            if (health != null && health.HealthPercentage <= 0.1f)
            {
                if (RadioTransmissions.Instance != null)
                    RadioTransmissions.Instance.ShowMessage("FINISH IT! END THIS NIGHTMARE!", 2f);
            }
        }
        
        void OnDeath()
        {
            currentPhase = BossPhase.Dead;
            if (RadioTransmissions.Instance != null)
                RadioTransmissions.Instance.ShowMessage("SUBJECT 23 IS DOWN! HELICOPTER INBOUND!", 4f);
            
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeathExplosion(transform.position, false);
                VFXManager.Instance.TriggerScreenShake(1f, 0.6f);
            }
        }
        
        void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamageTaken -= OnDamaged;
                health.OnEnemyDeath -= OnDeath;
            }
        }
    }
}
