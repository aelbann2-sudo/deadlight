using UnityEngine;

namespace Deadlight.Enemy
{
    public enum AffixType { None, Berserker, Splitter, Regenerator, Vampiric }
    
    public class EnemyAffix : MonoBehaviour
    {
        [SerializeField] private AffixType affixType = AffixType.None;
        private EnemyHealth health;
        private SimpleEnemyAI ai;
        private SpriteRenderer spriteRenderer;
        private GameObject auraObject;
        private bool berserkerActive;
        
        public AffixType CurrentAffix => affixType;
        
        public void SetAffix(AffixType type) { affixType = type; }
        
        void Start()
        {
            health = GetComponent<EnemyHealth>();
            ai = GetComponent<SimpleEnemyAI>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (affixType == AffixType.None) return;
            CreateAura();
            
            if (health != null)
            {
                health.OnDamageTaken += OnDamaged;
                health.OnEnemyDeath += OnDeath;
            }
        }
        
        void Update()
        {
            if (affixType == AffixType.None || health == null || !health.IsAlive) return;
            
            switch (affixType)
            {
                case AffixType.Berserker:
                    if (!berserkerActive && health.HealthPercentage <= 0.3f)
                    {
                        berserkerActive = true;
                        if (ai != null) ai.ApplySpeedMultiplier(1.5f);
                        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0.3f);
                    }
                    break;
                case AffixType.Regenerator:
                    health.Heal(5f * Time.deltaTime);
                    break;
            }
            
            if (auraObject != null)
            {
                float pulse = 0.8f + Mathf.Sin(Time.time * 3f) * 0.2f;
                auraObject.transform.localScale = Vector3.one * pulse;
            }
        }
        
        void OnDamaged(float dmg)
        {
            if (affixType == AffixType.Vampiric)
            {
                // Vampiric heals a portion of damage dealt to player
            }
        }
        
        void OnDeath()
        {
            if (affixType == AffixType.Splitter)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 offset = Random.insideUnitCircle * 0.5f;
                    var mini = new GameObject("MiniZombie");
                    mini.transform.position = transform.position + offset;
                    mini.transform.localScale = Vector3.one * 0.6f;
                    
                    var sr = mini.AddComponent<SpriteRenderer>();
                    sr.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
                    sr.sortingOrder = 9;
                    sr.color = new Color(0.7f, 0.5f, 0.8f);
                    
                    var rb = mini.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 0;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    
                    var col = mini.AddComponent<CircleCollider2D>();
                    col.radius = 0.25f;
                    
                    var hp = mini.AddComponent<EnemyHealth>();
                    hp.SetMaxHealth(20f);
                    hp.SetPointsOnDeath(5);
                    
                    var aiComp = mini.AddComponent<SimpleEnemyAI>();
                    aiComp.ApplySpeedMultiplier(1.3f);
                }
            }
        }
        
        void CreateAura()
        {
            Color auraColor = affixType switch
            {
                AffixType.Berserker => new Color(1f, 0.2f, 0.2f, 0.3f),
                AffixType.Splitter => new Color(0.7f, 0.3f, 1f, 0.3f),
                AffixType.Regenerator => new Color(0.2f, 1f, 0.3f, 0.3f),
                AffixType.Vampiric => new Color(0.6f, 0f, 0f, 0.3f),
                _ => Color.clear
            };
            
            auraObject = new GameObject("Aura");
            auraObject.transform.SetParent(transform);
            auraObject.transform.localPosition = Vector3.zero;
            
            var sr = auraObject.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            Vector2 center = new Vector2(16, 16);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / 16f;
                    pixels[y * 32 + x] = dist < 1f ? new Color(auraColor.r, auraColor.g, auraColor.b, auraColor.a * (1f - dist)) : Color.clear;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
            sr.sortingOrder = 8;
        }
        
        void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamageTaken -= OnDamaged;
                health.OnEnemyDeath -= OnDeath;
            }
        }
        
        public static AffixType GetRandomAffix()
        {
            float roll = Random.value;
            if (roll < 0.25f) return AffixType.Berserker;
            if (roll < 0.5f) return AffixType.Splitter;
            if (roll < 0.75f) return AffixType.Regenerator;
            return AffixType.Vampiric;
        }
    }
}
