using Deadlight.Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.UI
{
    /// <summary>
    /// Per-enemy world-space Canvas health bar.  Replaces the old OnGUI()
    /// immediate-mode approach with a lightweight Canvas that only activates
    /// after the enemy takes damage.
    /// </summary>
    public class EnemyWorldUI : MonoBehaviour
    {
        [SerializeField] private Vector3 uiOffset = new Vector3(0f, 0.75f, 0f);
        [SerializeField] private float visibleDuration = 1.3f;

        private EnemyHealth health;
        private float visibleUntil;
        private float telegraphUntil;

        // Canvas elements
        private Canvas _canvas;
        private GameObject _barRoot;
        private RectTransform _fillRt;
        private Image _fillImage;
        private Image _telegraphImage;

        private static readonly Color BarBgColor = new Color(0.05f, 0.06f, 0.08f, 0.72f);
        private static readonly Color BarFillLowColor = new Color(0.92f, 0.2f, 0.18f, 0.98f);
        private static readonly Color BarFillHighColor = new Color(0.24f, 0.84f, 0.34f, 0.98f);
        private static readonly Color TelegraphColor = new Color(1f, 0.85f, 0.15f, 0.95f);

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
            BuildWorldCanvas();

            if (health == null)
                Bind(GetComponent<EnemyHealth>());

            SetBarVisible(false);
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

        private void HandleDamage(float _) => Show(visibleDuration);

        private void HandleDeath()
        {
            visibleUntil = 0f;
            SetBarVisible(false);
        }

        private void LateUpdate()
        {
            if (health == null || !health.IsAlive) { SetBarVisible(false); return; }

            bool shouldShow = Time.time <= visibleUntil;
            SetBarVisible(shouldShow);
            if (!shouldShow) return;

            // Update fill
            float pct = Mathf.Clamp01(health.HealthPercentage);
            _fillRt.localScale = new Vector3(pct, 1f, 1f);
            _fillImage.color = Color.Lerp(BarFillLowColor, BarFillHighColor, pct);

            bool showTelegraph = Time.time <= telegraphUntil;
            if (_telegraphImage != null)
                _telegraphImage.gameObject.SetActive(showTelegraph);
        }

        private void SetBarVisible(bool vis)
        {
            if (_barRoot != null && _barRoot.activeSelf != vis)
                _barRoot.SetActive(vis);
        }

        // ── Build ────────────────────────────────────────────

        private void BuildWorldCanvas()
        {
            _barRoot = new GameObject("EnemyHPBar");
            _barRoot.transform.SetParent(transform, false);
            _barRoot.transform.localPosition = uiOffset;
            _barRoot.layer = gameObject.layer;

            _canvas = _barRoot.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 50;

            var rt = _barRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(36f, 6f);
            rt.localScale = Vector3.one * 0.02f;

            // Background
            var bg = new GameObject("Bg");
            bg.transform.SetParent(_barRoot.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = BarBgColor;
            bgImg.raycastTarget = false;

            // Fill
            var fill = new GameObject("Fill");
            fill.transform.SetParent(_barRoot.transform, false);
            _fillRt = fill.AddComponent<RectTransform>();
            _fillRt.anchorMin = Vector2.zero;
            _fillRt.anchorMax = Vector2.one;
            _fillRt.pivot = new Vector2(0f, 0.5f);
            _fillRt.offsetMin = new Vector2(1f, 1f);
            _fillRt.offsetMax = new Vector2(-1f, -1f);
            _fillImage = fill.AddComponent<Image>();
            _fillImage.color = BarFillHighColor;
            _fillImage.raycastTarget = false;

            // Telegraph stripe (above bar)
            var tel = new GameObject("Telegraph");
            tel.transform.SetParent(_barRoot.transform, false);
            var telRt = tel.AddComponent<RectTransform>();
            telRt.anchorMin = new Vector2(0f, 1f);
            telRt.anchorMax = new Vector2(1f, 1f);
            telRt.pivot = new Vector2(0.5f, 0f);
            telRt.anchoredPosition = new Vector2(0f, 2f);
            telRt.sizeDelta = new Vector2(0f, 0.04f);
            _telegraphImage = tel.AddComponent<Image>();
            _telegraphImage.color = TelegraphColor;
            _telegraphImage.raycastTarget = false;
            tel.SetActive(false);
        }
    }
}
