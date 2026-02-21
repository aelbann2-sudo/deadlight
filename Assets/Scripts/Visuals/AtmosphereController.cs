using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Deadlight.Visuals
{
    public class AtmosphereController : MonoBehaviour
    {
        public static AtmosphereController Instance { get; private set; }

        [Header("Day Settings")]
        [SerializeField] private Color dayAmbientColor = new Color(0.95f, 0.92f, 0.85f);
        [SerializeField] private Color daySkyColor = new Color(0.55f, 0.75f, 0.95f);
        [SerializeField] private float dayVignetteIntensity = 0.15f;
        [SerializeField] private float dayDesaturation = 0f;
        [SerializeField] private float dayGrainIntensity = 0.02f;

        [Header("Night Settings")]
        [SerializeField] private Color nightAmbientColor = new Color(0.15f, 0.18f, 0.35f);
        [SerializeField] private Color nightSkyColor = new Color(0.03f, 0.04f, 0.08f);
        [SerializeField] private float nightVignetteIntensity = 0.45f;
        [SerializeField] private float nightDesaturation = 0.35f;
        [SerializeField] private float nightGrainIntensity = 0.08f;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 3f;

        [Header("Low Health Effects")]
        [SerializeField] private float lowHealthVignetteBoost = 0.3f;
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private Color lowHealthTint = new Color(0.8f, 0.2f, 0.15f, 0.15f);

        private Canvas effectsCanvas;
        private Image vignetteImage;
        private Image colorOverlayImage;
        private Image grainImage;
        private Image chromaticImage;
        private Image lowHealthOverlay;
        private Image damageFlashImage;

        private float currentVignetteIntensity;
        private float currentDesaturation;
        private float currentGrainIntensity;
        private Color currentAmbientColor;
        private bool isNight = false;
        private float healthPercent = 1f;

        private Coroutine transitionCoroutine;
        private Coroutine lowHealthPulseCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateEffectsCanvas();
            SetDayAtmosphere();
        }

        private void CreateEffectsCanvas()
        {
            var canvasGO = new GameObject("AtmosphereEffectsCanvas");
            canvasGO.transform.SetParent(transform);
            
            effectsCanvas = canvasGO.AddComponent<Canvas>();
            effectsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            effectsCanvas.sortingOrder = 150;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            CreateVignette();
            CreateColorOverlay();
            CreateGrainEffect();
            CreateChromaticAberration();
            CreateLowHealthOverlay();
            CreateDamageFlash();
        }

        private void CreateVignette()
        {
            var go = new GameObject("Vignette");
            go.transform.SetParent(effectsCanvas.transform);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            vignetteImage = go.AddComponent<Image>();
            vignetteImage.sprite = CreateVignetteSprite();
            vignetteImage.color = new Color(0, 0, 0, dayVignetteIntensity);
            vignetteImage.raycastTarget = false;
        }

        private Sprite CreateVignetteSprite()
        {
            int size = 512;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size * 0.7f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normalizedDist = dist / maxDist;
                    float alpha = Mathf.Clamp01(Mathf.Pow(normalizedDist, 2f));
                    texture.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void CreateColorOverlay()
        {
            var go = new GameObject("ColorOverlay");
            go.transform.SetParent(effectsCanvas.transform);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            colorOverlayImage = go.AddComponent<Image>();
            colorOverlayImage.color = Color.clear;
            colorOverlayImage.raycastTarget = false;
        }

        private void CreateGrainEffect()
        {
            var go = new GameObject("FilmGrain");
            go.transform.SetParent(effectsCanvas.transform);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            grainImage = go.AddComponent<Image>();
            grainImage.sprite = CreateGrainSprite();
            grainImage.color = new Color(1, 1, 1, dayGrainIntensity);
            grainImage.raycastTarget = false;
            grainImage.type = Image.Type.Tiled;

            StartCoroutine(AnimateGrain());
        }

        private Sprite CreateGrainSprite()
        {
            int size = 128;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Random.Range(0f, 1f);
                    float gray = noise > 0.5f ? 0.6f : 0.4f;
                    texture.SetPixel(x, y, new Color(gray, gray, gray, noise > 0.7f ? 0.3f : 0f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1);
        }

        private IEnumerator AnimateGrain()
        {
            while (true)
            {
                if (grainImage != null && grainImage.gameObject.activeInHierarchy)
                {
                    var rect = grainImage.rectTransform;
                    rect.anchoredPosition = new Vector2(
                        Random.Range(-10f, 10f),
                        Random.Range(-10f, 10f)
                    );
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void CreateChromaticAberration()
        {
            var go = new GameObject("ChromaticAberration");
            go.transform.SetParent(effectsCanvas.transform);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            chromaticImage = go.AddComponent<Image>();
            chromaticImage.sprite = CreateChromaticSprite();
            chromaticImage.color = new Color(1, 1, 1, 0);
            chromaticImage.raycastTarget = false;
        }

        private Sprite CreateChromaticSprite()
        {
            int size = 256;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float r = Mathf.Clamp01(dist * 0.3f);
                    float b = Mathf.Clamp01(dist * 0.2f);
                    texture.SetPixel(x, y, new Color(r, 0, b, dist * 0.15f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void CreateLowHealthOverlay()
        {
            var go = new GameObject("LowHealthOverlay");
            go.transform.SetParent(effectsCanvas.transform);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            lowHealthOverlay = go.AddComponent<Image>();
            lowHealthOverlay.sprite = CreateVignetteSprite();
            lowHealthOverlay.color = Color.clear;
            lowHealthOverlay.raycastTarget = false;
        }

        private void CreateDamageFlash()
        {
            var go = new GameObject("DamageFlash");
            go.transform.SetParent(effectsCanvas.transform);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            damageFlashImage = go.AddComponent<Image>();
            damageFlashImage.color = Color.clear;
            damageFlashImage.raycastTarget = false;
        }

        #region Public Methods

        public void SetDayAtmosphere()
        {
            isNight = false;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionAtmosphere(
                dayVignetteIntensity, dayDesaturation, dayGrainIntensity, dayAmbientColor, daySkyColor
            ));
        }

        public void SetNightAtmosphere()
        {
            isNight = true;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionAtmosphere(
                nightVignetteIntensity, nightDesaturation, nightGrainIntensity, nightAmbientColor, nightSkyColor
            ));
        }

        public void TransitionToNight(float duration)
        {
            isNight = true;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionAtmosphere(
                nightVignetteIntensity, nightDesaturation, nightGrainIntensity, nightAmbientColor, nightSkyColor, duration
            ));
        }

        public void TransitionToDay(float duration)
        {
            isNight = false;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionAtmosphere(
                dayVignetteIntensity, dayDesaturation, dayGrainIntensity, dayAmbientColor, daySkyColor, duration
            ));
        }

        public void UpdateHealthState(float healthPercentage)
        {
            healthPercent = healthPercentage;
            
            if (healthPercent <= lowHealthThreshold)
            {
                if (lowHealthPulseCoroutine == null)
                {
                    lowHealthPulseCoroutine = StartCoroutine(LowHealthPulse());
                }
            }
            else
            {
                if (lowHealthPulseCoroutine != null)
                {
                    StopCoroutine(lowHealthPulseCoroutine);
                    lowHealthPulseCoroutine = null;
                }
                if (lowHealthOverlay != null)
                    lowHealthOverlay.color = Color.clear;
            }

            UpdateVignette();
        }

        public void TriggerDamageFlash(float intensity = 0.4f)
        {
            StartCoroutine(DamageFlashRoutine(intensity));
        }

        public void TriggerChromaticPulse(float intensity = 0.5f, float duration = 0.2f)
        {
            StartCoroutine(ChromaticPulseRoutine(intensity, duration));
        }

        #endregion

        #region Coroutines

        private IEnumerator TransitionAtmosphere(float targetVignette, float targetDesat, 
            float targetGrain, Color targetAmbient, Color targetSky, float duration = -1f)
        {
            if (duration < 0) duration = transitionDuration;

            float startVignette = currentVignetteIntensity;
            float startDesat = currentDesaturation;
            float startGrain = currentGrainIntensity;
            Color startAmbient = currentAmbientColor;
            Color startSky = Camera.main != null ? Camera.main.backgroundColor : Color.black;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t);

                currentVignetteIntensity = Mathf.Lerp(startVignette, targetVignette, t);
                currentDesaturation = Mathf.Lerp(startDesat, targetDesat, t);
                currentGrainIntensity = Mathf.Lerp(startGrain, targetGrain, t);
                currentAmbientColor = Color.Lerp(startAmbient, targetAmbient, t);

                ApplyAtmosphereSettings();

                if (Camera.main != null)
                {
                    Camera.main.backgroundColor = Color.Lerp(startSky, targetSky, t);
                }

                yield return null;
            }

            currentVignetteIntensity = targetVignette;
            currentDesaturation = targetDesat;
            currentGrainIntensity = targetGrain;
            currentAmbientColor = targetAmbient;
            ApplyAtmosphereSettings();

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = targetSky;
            }
        }

        private IEnumerator LowHealthPulse()
        {
            while (healthPercent <= lowHealthThreshold)
            {
                float pulseSpeed = 2f + (1f - healthPercent / lowHealthThreshold) * 3f;
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                
                float intensity = lowHealthTint.a * pulse * (1f - healthPercent / lowHealthThreshold);
                if (lowHealthOverlay != null)
                {
                    lowHealthOverlay.color = new Color(lowHealthTint.r, lowHealthTint.g, lowHealthTint.b, intensity);
                }

                yield return null;
            }

            if (lowHealthOverlay != null)
                lowHealthOverlay.color = Color.clear;
            
            lowHealthPulseCoroutine = null;
        }

        private IEnumerator DamageFlashRoutine(float intensity)
        {
            if (damageFlashImage == null) yield break;

            damageFlashImage.color = new Color(0.8f, 0.1f, 0.05f, intensity);
            
            float elapsed = 0f;
            float duration = 0.15f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(intensity, 0f, elapsed / duration);
                damageFlashImage.color = new Color(0.8f, 0.1f, 0.05f, alpha);
                yield return null;
            }

            damageFlashImage.color = Color.clear;
        }

        private IEnumerator ChromaticPulseRoutine(float intensity, float duration)
        {
            if (chromaticImage == null) yield break;

            float halfDuration = duration / 2f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, intensity, elapsed / halfDuration);
                chromaticImage.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(intensity, 0f, elapsed / halfDuration);
                chromaticImage.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            chromaticImage.color = Color.clear;
        }

        #endregion

        #region Private Methods

        private void ApplyAtmosphereSettings()
        {
            UpdateVignette();
            UpdateGrain();
            UpdateDesaturation();
        }

        private void UpdateVignette()
        {
            if (vignetteImage == null) return;

            float healthBoost = 0f;
            if (healthPercent < lowHealthThreshold)
            {
                healthBoost = lowHealthVignetteBoost * (1f - healthPercent / lowHealthThreshold);
            }

            vignetteImage.color = new Color(0, 0, 0, currentVignetteIntensity + healthBoost);
        }

        private void UpdateGrain()
        {
            if (grainImage == null) return;
            grainImage.color = new Color(1, 1, 1, currentGrainIntensity);
        }

        private void UpdateDesaturation()
        {
            if (colorOverlayImage == null) return;
            float grayValue = 0.5f;
            colorOverlayImage.color = new Color(grayValue, grayValue, grayValue, currentDesaturation * 0.3f);
        }

        #endregion

        #region Ambient Effects

        public void CreateAmbientParticles(bool nightMode)
        {
            if (nightMode)
            {
                CreateFireflies();
            }
            else
            {
                CreateDustMotes();
            }
        }

        private void CreateFireflies()
        {
            var go = new GameObject("Fireflies");
            go.transform.SetParent(transform);
            
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(0.8f, 0.9f, 0.3f, 0.8f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30, 20, 1);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.8f, 0.9f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.7f, 0.2f), 0.5f),
                    new GradientColorKey(new Color(0.8f, 0.9f, 0.3f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0.8f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.5f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.8f, 0.9f, 0.3f);
            renderer.material = mat;
            renderer.sortingOrder = 5;
        }

        private void CreateDustMotes()
        {
            var go = new GameObject("DustMotes");
            go.transform.SetParent(transform);
            
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new Color(0.9f, 0.85f, 0.7f, 0.3f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20, 15, 1);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.3f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.9f, 0.85f, 0.7f, 0.3f);
            renderer.material = mat;
            renderer.sortingOrder = 3;
        }

        #endregion
    }
}
