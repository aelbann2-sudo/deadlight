using Deadlight.Enemy;
using UnityEngine;

namespace Deadlight.UI
{
    public class EnemyWorldUI : MonoBehaviour
    {
        [SerializeField] private Vector3 uiOffset = new Vector3(0f, 0.75f, 0f);
        [SerializeField] private float visibleDuration = 1.3f;

        private EnemyHealth health;
        private Camera cam;
        private float visibleUntil;
        private float telegraphUntil;

        private Texture2D barBg;
        private Texture2D barFill;
        private Texture2D telegraphTex;

        public void Bind(EnemyHealth enemyHealth)
        {
            if (health != null)
            {
                health.OnDamageTaken -= HandleDamage;
                health.OnEnemyDeath -= HandleDeath;
            }

            health = enemyHealth;
            if (health != null)
            {
                health.OnDamageTaken += HandleDamage;
                health.OnEnemyDeath += HandleDeath;
            }
        }

        private void Awake()
        {
            cam = Camera.main;
            barBg = MakeTex(new Color(0f, 0f, 0f, 0.65f));
            barFill = MakeTex(new Color(0.85f, 0.2f, 0.2f, 0.95f));
            telegraphTex = MakeTex(new Color(1f, 0.85f, 0.15f, 0.95f));

            if (health == null)
            {
                Bind(GetComponent<EnemyHealth>());
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamageTaken -= HandleDamage;
                health.OnEnemyDeath -= HandleDeath;
            }
        }

        public void Show(float seconds)
        {
            visibleUntil = Mathf.Max(visibleUntil, Time.time + Mathf.Max(0.1f, seconds));
        }

        public void ShowTelegraph(float seconds = 0.2f)
        {
            telegraphUntil = Mathf.Max(telegraphUntil, Time.time + Mathf.Max(0.05f, seconds));
            Show(seconds + 0.35f);
        }

        private void HandleDamage(float _)
        {
            Show(visibleDuration);
        }

        private void HandleDeath()
        {
            visibleUntil = 0f;
        }

        private void OnGUI()
        {
            if (health == null || cam == null || !health.IsAlive)
            {
                return;
            }

            bool isVisible = Time.time <= visibleUntil;
            if (!isVisible)
            {
                return;
            }

            Vector3 world = transform.position + uiOffset;
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                return;
            }

            float w = 46f;
            float h = 6f;
            float x = screen.x - w * 0.5f;
            float y = Screen.height - screen.y;

            GUI.DrawTexture(new Rect(x, y, w, h), barBg);
            GUI.DrawTexture(new Rect(x + 1f, y + 1f, (w - 2f) * Mathf.Clamp01(health.HealthPercentage), h - 2f), barFill);

            if (Time.time <= telegraphUntil)
            {
                GUI.DrawTexture(new Rect(x - 2f, y - 8f, w + 4f, 3f), telegraphTex);
            }
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
