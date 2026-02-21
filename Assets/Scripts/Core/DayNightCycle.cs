using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Deadlight.Visuals;

namespace Deadlight.Core
{
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Time Settings")]
        [SerializeField] private float dayDuration = 180f;
        [SerializeField] private float nightDuration = 210f;
        [SerializeField] private float transitionDuration = 3f;

        [Header("Lighting")]
        [SerializeField] private Light globalLight;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private Color nightColor = new Color(0.1f, 0.1f, 0.2f);
        [SerializeField] private Color daySkyColor = new Color(0.55f, 0.75f, 0.95f);
        [SerializeField] private Color nightSkyColor = new Color(0.03f, 0.04f, 0.08f);
        [SerializeField] private float dayIntensity = 1f;
        [SerializeField] private float nightIntensity = 0.3f;

        [Header("Sky Gradient Colors")]
        [SerializeField] private Color sunsetColor = new Color(0.95f, 0.5f, 0.3f);
        [SerializeField] private Color twilightColor = new Color(0.4f, 0.3f, 0.5f);
        [SerializeField] private Color dawnColor = new Color(0.85f, 0.6f, 0.5f);

        [Header("Environment Effects")]
        [SerializeField] private float nightFogDensity = 0.03f;
        [SerializeField] private Color nightFogColor = new Color(0.1f, 0.12f, 0.2f);
        [SerializeField] private bool enableStars = true;
        [SerializeField] private bool enableMoonGlow = true;

        [Header("Current State")]
        [SerializeField] private float currentTime;
        [SerializeField] private bool isDay = true;
        [SerializeField] private bool isTransitioning = false;

        public float CurrentTime => currentTime;
        public float TimeRemaining => Mathf.Max(0f, isDay ? dayDuration - currentTime : nightDuration - currentTime);
        public float TotalPhaseTime => isDay ? dayDuration : nightDuration;
        public float NormalizedTime => TotalPhaseTime <= 0f ? 0f : currentTime / TotalPhaseTime;
        public bool IsDay => isDay;
        public bool IsNight => !isDay && !isTransitioning;
        public float DayDuration => dayDuration;
        public float NightDuration => nightDuration;

        public event Action OnDayStart;
        public event Action OnNightStart;
        public event Action OnTransitionStart;
        public event Action<float> OnTimeUpdate;
        public event Action<float> OnSkyColorChanged;

        private bool isPaused = false;
        private ParticleSystem starsParticleSystem;
        private GameObject moonGlow;
        private GameObject sunIndicator;
        private Canvas skyCanvas;
        private Image skyGradientImage;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            InitializeLighting();
            CreateSkySystem();
            CreateEnvironmentalEffects();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void Update()
        {
            if (isPaused || isTransitioning) return;

            if (GameManager.Instance?.CurrentState == GameState.DayPhase ||
                GameManager.Instance?.CurrentState == GameState.NightPhase)
            {
                UpdateTime();
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.DayPhase:
                    StartDay();
                    break;
                case GameState.NightPhase:
                    StartNight();
                    break;
                case GameState.DawnPhase:
                case GameState.GameOver:
                case GameState.Victory:
                case GameState.MainMenu:
                    isPaused = true;
                    break;
            }
        }

        private void InitializeLighting()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (globalLight == null)
            {
                var existingLight = FindObjectOfType<Light>();
                if (existingLight != null)
                {
                    globalLight = existingLight;
                }
            }

            SetDayLighting();
        }

        private void UpdateTime()
        {
            currentTime += Time.deltaTime;
            OnTimeUpdate?.Invoke(TimeRemaining);

            float targetDuration = isDay ? dayDuration : nightDuration;

            if (currentTime >= targetDuration)
            {
                OnPhaseComplete();
            }
        }

        private void OnPhaseComplete()
        {
            if (isDay)
            {
                StartTransitionToNight();
            }
            else
            {
                GameManager.Instance?.OnNightSurvived();
            }
        }

        public void StartDay()
        {
            isDay = true;
            currentTime = 0f;
            isPaused = false;

            SetDayLighting();
            OnDayStart?.Invoke();

            Debug.Log("[DayNightCycle] Day phase started");
        }

        public void StartNight()
        {
            isDay = false;
            currentTime = 0f;
            isPaused = false;

            SetNightLighting();
            OnNightStart?.Invoke();

            Debug.Log("[DayNightCycle] Night phase started");
        }

        private void StartTransitionToNight()
        {
            isTransitioning = true;
            OnTransitionStart?.Invoke();
            StartCoroutine(TransitionToNightCoroutineEnhanced());
        }

        private void SetDayLighting()
        {
            if (globalLight != null)
            {
                globalLight.color = dayColor;
                globalLight.intensity = dayIntensity;
            }

            if (mainCamera != null)
            {
                mainCamera.backgroundColor = daySkyColor;
            }
        }

        private void SetNightLighting()
        {
            if (globalLight != null)
            {
                globalLight.color = nightColor;
                globalLight.intensity = nightIntensity;
            }

            if (mainCamera != null)
            {
                mainCamera.backgroundColor = nightSkyColor;
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void AdjustDayDuration(float multiplier)
        {
            dayDuration *= multiplier;
        }

        public void AdjustNightDuration(float multiplier)
        {
            nightDuration *= multiplier;
        }

        public void SetDayDuration(float seconds)
        {
            dayDuration = Mathf.Max(5f, seconds);
        }

        public void SetNightDuration(float seconds)
        {
            nightDuration = Mathf.Max(5f, seconds);
        }

        #region Sky System

        private void CreateSkySystem()
        {
            var canvasGO = new GameObject("SkyCanvas");
            canvasGO.transform.SetParent(transform);
            
            skyCanvas = canvasGO.AddComponent<Canvas>();
            skyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            skyCanvas.sortingOrder = -100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            CreateSkyGradient();
            
            if (enableStars) CreateStars();
            if (enableMoonGlow) CreateMoon();
            CreateSun();
        }

        private void CreateSkyGradient()
        {
            var go = new GameObject("SkyGradient");
            go.transform.SetParent(skyCanvas.transform);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            skyGradientImage = go.AddComponent<Image>();
            skyGradientImage.sprite = CreateGradientSprite(daySkyColor, Color.Lerp(daySkyColor, Color.white, 0.3f));
            skyGradientImage.type = Image.Type.Simple;
            skyGradientImage.raycastTarget = false;
        }

        private Sprite CreateGradientSprite(Color topColor, Color bottomColor)
        {
            int width = 4;
            int height = 64;
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                Color col = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, col);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private void CreateStars()
        {
            var go = new GameObject("Stars");
            go.transform.SetParent(transform);
            
            starsParticleSystem = go.AddComponent<ParticleSystem>();
            var main = starsParticleSystem.main;
            main.loop = true;
            main.startLifetime = 999f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(1f, 1f, 0.95f, 0f);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = starsParticleSystem.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 100) });

            var shape = starsParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50, 30, 1);
            shape.position = new Vector3(0, 10, 10);

            var colorOverLifetime = starsParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.95f), 0f) },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.3f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0.3f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.white;
            renderer.material = mat;
            renderer.sortingOrder = -99;
        }

        private void CreateMoon()
        {
            moonGlow = new GameObject("MoonGlow");
            moonGlow.transform.SetParent(transform);
            moonGlow.transform.position = new Vector3(15, 12, 5);

            var sr = moonGlow.AddComponent<SpriteRenderer>();
            sr.sprite = CreateMoonSprite();
            sr.sortingOrder = -98;
            sr.color = new Color(0.9f, 0.92f, 1f, 0f);
        }

        private Sprite CreateMoonSprite()
        {
            int size = 64;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.35f;
            float glowRadius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (dist < radius)
                    {
                        float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                        float brightness = 0.85f + noise * 0.15f;
                        texture.SetPixel(x, y, new Color(brightness, brightness * 0.98f, brightness, 1f));
                    }
                    else if (dist < glowRadius)
                    {
                        float alpha = 1f - (dist - radius) / (glowRadius - radius);
                        alpha = Mathf.Pow(alpha, 2f) * 0.4f;
                        texture.SetPixel(x, y, new Color(0.8f, 0.85f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        private void CreateSun()
        {
            sunIndicator = new GameObject("SunIndicator");
            sunIndicator.transform.SetParent(transform);
            sunIndicator.transform.position = new Vector3(-15, 10, 5);

            var sr = sunIndicator.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSunSprite();
            sr.sortingOrder = -98;
            sr.color = new Color(1f, 0.95f, 0.7f, 0.8f);
        }

        private Sprite CreateSunSprite()
        {
            int size = 48;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.25f;
            float glowRadius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (dist < radius)
                    {
                        texture.SetPixel(x, y, new Color(1f, 0.98f, 0.85f, 1f));
                    }
                    else if (dist < glowRadius)
                    {
                        float alpha = 1f - (dist - radius) / (glowRadius - radius);
                        alpha = Mathf.Pow(alpha, 1.5f) * 0.6f;
                        texture.SetPixel(x, y, new Color(1f, 0.9f, 0.5f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        #endregion

        #region Environmental Effects

        private void CreateEnvironmentalEffects()
        {
            RenderSettings.fog = false;
        }

        private void UpdateEnvironmentalEffects(float dayNightBlend)
        {
            if (starsParticleSystem != null)
            {
                var main = starsParticleSystem.main;
                main.startColor = new Color(1f, 1f, 0.95f, dayNightBlend * 0.8f);
            }

            if (moonGlow != null)
            {
                var sr = moonGlow.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(0.9f, 0.92f, 1f, dayNightBlend * 0.9f);
                }
            }

            if (sunIndicator != null)
            {
                var sr = sunIndicator.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(1f, 0.95f, 0.7f, (1f - dayNightBlend) * 0.8f);
                }
            }

            if (skyGradientImage != null)
            {
                Color topColor = Color.Lerp(daySkyColor, nightSkyColor, dayNightBlend);
                Color bottomColor = Color.Lerp(
                    Color.Lerp(daySkyColor, Color.white, 0.3f),
                    Color.Lerp(nightSkyColor, Color.black, 0.3f),
                    dayNightBlend
                );
                skyGradientImage.sprite = CreateGradientSprite(topColor, bottomColor);
            }

            if (AtmosphereController.Instance != null)
            {
                if (dayNightBlend > 0.5f)
                {
                    AtmosphereController.Instance.TransitionToNight(0.1f);
                }
                else
                {
                    AtmosphereController.Instance.TransitionToDay(0.1f);
                }
            }

            OnSkyColorChanged?.Invoke(dayNightBlend);
        }

        public void SetZombieNightGlow(GameObject zombie, bool glowing)
        {
            if (zombie == null) return;

            var sr = zombie.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            if (glowing)
            {
                var glowGO = zombie.transform.Find("EyeGlow");
                if (glowGO == null)
                {
                    glowGO = new GameObject("EyeGlow").transform;
                    glowGO.SetParent(zombie.transform);
                    glowGO.localPosition = new Vector3(0, 0.3f, -0.1f);

                    var glowSR = glowGO.gameObject.AddComponent<SpriteRenderer>();
                    glowSR.sprite = CreateEyeGlowSprite();
                    glowSR.sortingOrder = sr.sortingOrder + 1;
                    glowSR.color = new Color(0.9f, 0.2f, 0.15f, 0.8f);
                }
                else
                {
                    glowGO.gameObject.SetActive(true);
                }
            }
            else
            {
                var glowGO = zombie.transform.Find("EyeGlow");
                if (glowGO != null)
                {
                    glowGO.gameObject.SetActive(false);
                }
            }
        }

        private Sprite CreateEyeGlowSprite()
        {
            int size = 16;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float leftDist = Vector2.Distance(new Vector2(x, y), new Vector2(4, 8));
                    float rightDist = Vector2.Distance(new Vector2(x, y), new Vector2(12, 8));
                    float minDist = Mathf.Min(leftDist, rightDist);
                    
                    if (minDist < 3)
                    {
                        float alpha = 1f - (minDist / 3f);
                        texture.SetPixel(x, y, new Color(1f, 0.3f, 0.2f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        #endregion

        #region Enhanced Transitions

        private IEnumerator TransitionToNightCoroutineEnhanced()
        {
            float elapsed = 0f;

            if (enableStars && starsParticleSystem != null && !starsParticleSystem.isPlaying)
            {
                starsParticleSystem.Play();
            }

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                float smoothT = t * t * (3f - 2f * t);

                Color currentSkyColor;
                if (t < 0.3f)
                {
                    currentSkyColor = Color.Lerp(daySkyColor, sunsetColor, t / 0.3f);
                }
                else if (t < 0.6f)
                {
                    currentSkyColor = Color.Lerp(sunsetColor, twilightColor, (t - 0.3f) / 0.3f);
                }
                else
                {
                    currentSkyColor = Color.Lerp(twilightColor, nightSkyColor, (t - 0.6f) / 0.4f);
                }

                if (globalLight != null)
                {
                    globalLight.color = Color.Lerp(dayColor, nightColor, smoothT);
                    globalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, smoothT);
                }

                if (mainCamera != null)
                {
                    mainCamera.backgroundColor = currentSkyColor;
                }

                UpdateEnvironmentalEffects(smoothT);

                yield return null;
            }

            isTransitioning = false;
            GameManager.Instance?.StartNightPhase();
        }

        #endregion
    }
}
