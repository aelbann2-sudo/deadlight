using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Deadlight.Core
{
    public class GameEffects : MonoBehaviour
    {
        public static GameEffects Instance { get; private set; }

        private Image damageOverlay;
        private Image fadeOverlay;
        private CameraController cameraController;

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

        public void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
        {
            var flash = new GameObject("MuzzleFlash");
            flash.transform.position = position;
            flash.transform.rotation = rotation;

            var sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFlashSprite();
            sr.sortingOrder = 15;
            sr.color = new Color(1f, 0.9f, 0.5f, 0.9f);
            flash.transform.localScale = Vector3.one * 0.4f;

            Destroy(flash, 0.05f);
        }

        public void SpawnHitEffect(Vector3 position)
        {
            for (int i = 0; i < 4; i++)
            {
                var particle = new GameObject("HitParticle");
                particle.transform.position = position;

                var sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSmallSquareSprite();
                sr.sortingOrder = 14;
                sr.color = new Color(1f, 0.4f, 0.2f);
                particle.transform.localScale = Vector3.one * 0.15f;

                var rb = particle.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0.5f;
                Vector2 dir = Random.insideUnitCircle.normalized;
                rb.linearVelocity = dir * Random.Range(3f, 6f);

                Destroy(particle, 0.3f);
            }
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
    }
}
