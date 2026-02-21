using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Deadlight.Core
{
    public class GameEffects : MonoBehaviour
    {
        public static GameEffects Instance { get; private set; }

        private Image damageOverlay;
        private Image fadeOverlay;
        private CameraController cameraController;
        private Coroutine hitStopRoutine;
        
        private Canvas worldCanvas;
        private Font damageFont;
        private List<GameObject> activeDamageNumbers = new List<GameObject>();
        private const int MAX_DAMAGE_NUMBERS = 20;

        public event System.Action OnHitConfirmed;
        public event System.Action<int> OnDamageDealt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetupEffects(Image dmgOverlay, Image fdOverlay, CameraController cam)
        {
            damageOverlay = dmgOverlay;
            fadeOverlay = fdOverlay;
            cameraController = cam;
        }

        public void ScreenShake(float duration = 0.15f, float intensity = 0.2f)
        {
            if (cameraController != null)
            {
                cameraController.Shake(duration, intensity);
            }
        }

        public void DamageFlash()
        {
            if (damageOverlay != null)
            {
                StartCoroutine(DamageFlashRoutine());
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            damageOverlay.color = new Color(0.8f, 0, 0, 0.35f);
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.35f, 0f, elapsed / duration);
                damageOverlay.color = new Color(0.8f, 0, 0, alpha);
                yield return null;
            }
            damageOverlay.color = Color.clear;
        }
        
        public void FlashScreen(Color color, float duration)
        {
            if (damageOverlay != null)
            {
                StartCoroutine(FlashScreenRoutine(color, duration));
            }
        }
        
        private IEnumerator FlashScreenRoutine(Color color, float duration)
        {
            damageOverlay.color = color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
                damageOverlay.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
            damageOverlay.color = Color.clear;
        }

        public void SpawnMuzzleFlash(Vector3 position, Quaternion rotation, float scale = 0.4f, Color? tint = null)
        {
            var flash = new GameObject("MuzzleFlash");
            flash.transform.position = position;
            flash.transform.rotation = rotation;

            var sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFlashSprite();
            sr.sortingOrder = 15;
            sr.color = tint ?? new Color(1f, 0.9f, 0.5f, 0.9f);
            flash.transform.localScale = Vector3.one * Mathf.Max(0.15f, scale);

            Destroy(flash, 0.05f);
        }

        public void SpawnHitEffect(Vector3 position, bool heavyHit = false)
        {
            int particles = heavyHit ? 7 : 4;
            for (int i = 0; i < particles; i++)
            {
                var particle = new GameObject("HitParticle");
                particle.transform.position = position;

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSmallSquareSprite();
                sr.sortingOrder = 14;
                sr.color = heavyHit ? new Color(0.92f, 0.2f, 0.2f) : new Color(1f, 0.4f, 0.2f);
                particle.transform.localScale = Vector3.one * (heavyHit ? 0.2f : 0.15f);

                var rb = particle.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0.5f;
                Vector2 dir = Random.insideUnitCircle.normalized;
                rb.linearVelocity = dir * Random.Range(3f, 6f);

                Destroy(particle, 0.3f);
            }

            OnHitConfirmed?.Invoke();
        }

        public void TriggerHitStop(float duration = 0.04f)
        {
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
            }

            hitStopRoutine = StartCoroutine(HitStopRoutine(Mathf.Clamp(duration, 0.01f, 0.08f)));
        }

        public void SpawnDeathEffect(Vector3 position, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                var particle = new GameObject("DeathParticle");
                particle.transform.position = position;

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSmallSquareSprite();
                sr.sortingOrder = 14;
                sr.color = color;
                particle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.25f);

                var rb = particle.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0.8f;
                Vector2 dir = Random.insideUnitCircle.normalized;
                rb.linearVelocity = dir * Random.Range(2f, 5f);
                rb.angularVelocity = Random.Range(-360f, 360f);

                Destroy(particle, 0.6f);
            }
        }

        public void FadeScreen(bool fadeIn, float duration = 1f)
        {
            if (fadeOverlay != null)
            {
                StartCoroutine(FadeRoutine(fadeIn, duration));
            }
        }

        private IEnumerator FadeRoutine(bool fadeIn, float duration)
        {
            float startAlpha = fadeIn ? 1f : 0f;
            float endAlpha = fadeIn ? 0f : 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeOverlay.color = new Color(0, 0, 0, endAlpha);
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            float originalScale = Time.timeScale;
            Time.timeScale = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = originalScale <= 0f ? 1f : originalScale;
            hitStopRoutine = null;
        }

        private Sprite CreateFlashSprite()
        {
            int size = 16;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Max(0, 1f - dist * dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateSmallSquareSprite()
        {
            var texture = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        #region Damage Numbers

        private void InitializeDamageNumberSystem()
        {
            if (worldCanvas != null) return;

            var canvasGO = new GameObject("WorldCanvas_DamageNumbers");
            canvasGO.transform.SetParent(transform);
            
            worldCanvas = canvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 200;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            damageFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (damageFont == null)
            {
                damageFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        public void SpawnDamageNumber(Vector3 position, int damage, bool critical = false, bool heal = false)
        {
            InitializeDamageNumberSystem();
            if (worldCanvas == null) return;

            while (activeDamageNumbers.Count >= MAX_DAMAGE_NUMBERS)
            {
                var oldest = activeDamageNumbers[0];
                activeDamageNumbers.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }

            var numberGO = new GameObject("DamageNumber");
            numberGO.transform.SetParent(worldCanvas.transform);
            numberGO.transform.position = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.5f, 0);
            numberGO.transform.localScale = Vector3.one * 0.02f;

            var text = numberGO.AddComponent<Text>();
            text.text = heal ? $"+{damage}" : damage.ToString();
            text.font = damageFont;
            text.fontSize = critical ? 48 : 36;
            text.fontStyle = critical ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (heal)
            {
                text.color = new Color(0.3f, 1f, 0.4f);
            }
            else if (critical)
            {
                text.color = new Color(1f, 0.9f, 0.2f);
            }
            else
            {
                text.color = Color.white;
            }

            var outline = numberGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            activeDamageNumbers.Add(numberGO);
            StartCoroutine(AnimateDamageNumber(numberGO, critical));

            OnDamageDealt?.Invoke(damage);
        }

        private IEnumerator AnimateDamageNumber(GameObject numberGO, bool critical)
        {
            if (numberGO == null) yield break;

            var text = numberGO.GetComponent<Text>();
            if (text == null) yield break;

            float duration = critical ? 1.2f : 0.8f;
            float elapsed = 0f;
            Vector3 startPos = numberGO.transform.position;
            Vector3 endPos = startPos + new Vector3(0, 1f, 0);
            float startScale = critical ? 0.035f : 0.02f;
            Color startColor = text.color;

            if (critical)
            {
                float punchDuration = 0.1f;
                float punchElapsed = 0f;
                while (punchElapsed < punchDuration && numberGO != null)
                {
                    punchElapsed += Time.deltaTime;
                    float punch = 1f + Mathf.Sin(punchElapsed / punchDuration * Mathf.PI) * 0.3f;
                    numberGO.transform.localScale = Vector3.one * startScale * punch;
                    yield return null;
                }
            }

            while (elapsed < duration && numberGO != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                numberGO.transform.position = Vector3.Lerp(startPos, endPos, easeT);

                float scale = Mathf.Lerp(startScale, startScale * 0.7f, t);
                numberGO.transform.localScale = Vector3.one * scale;

                float alpha = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                yield return null;
            }

            if (numberGO != null)
            {
                activeDamageNumbers.Remove(numberGO);
                Destroy(numberGO);
            }
        }

        #endregion

        #region Enemy Visual States

        public void ApplyDamageFlash(GameObject enemy, float duration = 0.1f)
        {
            if (enemy == null) return;
            StartCoroutine(DamageFlashRoutine(enemy, duration));
        }

        private IEnumerator DamageFlashRoutine(GameObject enemy, float duration)
        {
            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr == null) yield break;

            Color originalColor = sr.color;
            sr.color = Color.white;

            yield return new WaitForSeconds(duration);

            if (sr != null)
            {
                sr.color = originalColor;
            }
        }

        public void ApplyStaggerEffect(GameObject enemy, Vector3 hitDirection, float force = 0.3f)
        {
            if (enemy == null) return;
            StartCoroutine(StaggerRoutine(enemy, hitDirection, force));
        }

        private IEnumerator StaggerRoutine(GameObject enemy, Vector3 hitDirection, float force)
        {
            if (enemy == null) yield break;

            Vector3 startPos = enemy.transform.position;
            Vector3 knockbackPos = startPos + hitDirection.normalized * force;

            float duration = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration && enemy != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                enemy.transform.position = Vector3.Lerp(startPos, knockbackPos, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration && enemy != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                enemy.transform.position = Vector3.Lerp(knockbackPos, startPos, t);
                yield return null;
            }
        }

        public void ApplyDeathDissolve(GameObject enemy, float duration = 0.5f)
        {
            if (enemy == null) return;
            StartCoroutine(DeathDissolveRoutine(enemy, duration));
        }

        private IEnumerator DeathDissolveRoutine(GameObject enemy, float duration)
        {
            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr == null) yield break;

            Color startColor = sr.color;
            float elapsed = 0f;

            while (elapsed < duration && enemy != null && sr != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                sr.color = new Color(
                    Mathf.Lerp(startColor.r, 0.2f, t),
                    Mathf.Lerp(startColor.g, 0.1f, t),
                    Mathf.Lerp(startColor.b, 0.1f, t),
                    Mathf.Lerp(startColor.a, 0f, t * t)
                );

                if (enemy != null)
                {
                    enemy.transform.localScale *= 0.99f;
                }

                yield return null;
            }
        }

        public void ApplyRageState(GameObject enemy, bool isRaging)
        {
            if (enemy == null) return;

            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            var existingGlow = enemy.transform.Find("RageGlow");
            
            if (isRaging)
            {
                if (existingGlow == null)
                {
                    var glow = new GameObject("RageGlow");
                    glow.transform.SetParent(enemy.transform);
                    glow.transform.localPosition = Vector3.zero;
                    glow.transform.localScale = Vector3.one * 1.3f;

                    var glowSR = glow.AddComponent<SpriteRenderer>();
                    glowSR.sprite = CreateGlowSprite();
                    glowSR.color = new Color(1f, 0.2f, 0.1f, 0.4f);
                    glowSR.sortingOrder = sr.sortingOrder - 1;
                }

                StartCoroutine(RagePulse(enemy));
            }
            else
            {
                if (existingGlow != null)
                {
                    Destroy(existingGlow.gameObject);
                }
            }
        }

        private IEnumerator RagePulse(GameObject enemy)
        {
            var glowTransform = enemy?.transform.Find("RageGlow");
            if (glowTransform == null) yield break;

            var glowSR = glowTransform.GetComponent<SpriteRenderer>();
            if (glowSR == null) yield break;

            while (enemy != null && glowTransform != null)
            {
                float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                glowTransform.localScale = Vector3.one * (1.2f + pulse * 0.2f);
                glowSR.color = new Color(1f, 0.2f, 0.1f, 0.3f + pulse * 0.2f);
                yield return null;
            }
        }

        private Sprite CreateGlowSprite()
        {
            int size = 32;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Max(0, 1f - dist);
                    alpha = Mathf.Pow(alpha, 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        #endregion

        #region Screen Effects

        public void ChromaticAberrationPulse(float intensity = 0.5f)
        {
            var atmosphere = Visuals.AtmosphereController.Instance;
            if (atmosphere != null)
            {
                atmosphere.TriggerChromaticPulse(intensity, 0.3f);
            }
        }

        public void TriggerSlowMotion(float duration = 0.5f, float timeScale = 0.3f)
        {
            StartCoroutine(SlowMotionRoutine(duration, timeScale));
        }

        private IEnumerator SlowMotionRoutine(float duration, float targetTimeScale)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = 0.02f * targetTimeScale;

            yield return new WaitForSecondsRealtime(duration);

            float lerpDuration = 0.2f;
            float elapsed = 0f;
            while (elapsed < lerpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(targetTimeScale, originalTimeScale, elapsed / lerpDuration);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                yield return null;
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = 0.02f;
        }

        public void SpawnBulletTrail(Vector3 start, Vector3 end, float duration = 0.1f)
        {
            var trail = new GameObject("BulletTrail");
            var lr = trail.AddComponent<LineRenderer>();
            
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            
            lr.startWidth = 0.05f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.9f, 0.5f, 0.8f);
            lr.endColor = new Color(1f, 0.7f, 0.3f, 0.3f);
            lr.sortingOrder = 100;

            StartCoroutine(FadeTrail(trail, lr, duration));
        }

        private IEnumerator FadeTrail(GameObject trail, LineRenderer lr, float duration)
        {
            float elapsed = 0f;
            Color startColorA = lr.startColor;
            Color endColorA = lr.endColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                lr.startColor = new Color(startColorA.r, startColorA.g, startColorA.b, startColorA.a * (1f - t));
                lr.endColor = new Color(endColorA.r, endColorA.g, endColorA.b, endColorA.a * (1f - t));
                
                yield return null;
            }

            Destroy(trail);
        }

        #endregion
    }
}
