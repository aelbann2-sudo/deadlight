using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Deadlight.Visuals
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Effect Settings")]
        [SerializeField] private float bloodDecalDuration = 30f;
        [SerializeField] private int maxDecals = 50;

        private List<GameObject> activeDecals = new List<GameObject>();
        private ParticleSystem muzzleFlashPS;
        private ParticleSystem bloodSplatterPS;
        private ParticleSystem sparkPS;
        private ParticleSystem dustPS;
        private ParticleSystem deathExplosionPS;
        private ParticleSystem toxicExplosionPS;
        private ParticleSystem healingPS;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeParticleSystems();
        }

        private void InitializeParticleSystems()
        {
            CreateMuzzleFlashSystem();
            CreateBloodSplatterSystem();
            CreateSparkSystem();
            CreateDustSystem();
            CreateDeathExplosionSystem();
            CreateToxicExplosionSystem();
            CreateHealingSystem();
        }

        #region Particle System Creation

        private void CreateMuzzleFlashSystem()
        {
            var go = new GameObject("MuzzleFlash_PS");
            go.transform.SetParent(transform);
            muzzleFlashPS = go.AddComponent<ParticleSystem>();
            
            var main = muzzleFlashPS.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = 0.08f;
            main.startSpeed = 0f;
            main.startSize = 0.5f;
            main.startColor = new Color(1f, 0.9f, 0.4f, 1f);
            main.maxParticles = 10;
            main.playOnAwake = false;

            var emission = muzzleFlashPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 3) });

            var shape = muzzleFlashPS.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;

            var sizeOverLifetime = muzzleFlashPS.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));

            var colorOverLifetime = muzzleFlashPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 1f, 0.8f), 0f),
                    new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(1f, 0.9f, 0.4f));
            renderer.sortingOrder = 100;
        }

        private void CreateBloodSplatterSystem()
        {
            var go = new GameObject("BloodSplatter_PS");
            go.transform.SetParent(transform);
            bloodSplatterPS = go.AddComponent<ParticleSystem>();

            var main = bloodSplatterPS.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.startColor = new Color(0.6f, 0.08f, 0.05f, 1f);
            main.gravityModifier = 2f;
            main.maxParticles = 50;
            main.playOnAwake = false;

            var emission = bloodSplatterPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8, 15) });

            var shape = bloodSplatterPS.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.1f;

            var colorOverLifetime = bloodSplatterPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.7f, 0.1f, 0.08f), 0f),
                    new GradientColorKey(new Color(0.4f, 0.05f, 0.03f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(0.6f, 0.08f, 0.05f));
            renderer.sortingOrder = 50;
        }

        private void CreateSparkSystem()
        {
            var go = new GameObject("Spark_PS");
            go.transform.SetParent(transform);
            sparkPS = go.AddComponent<ParticleSystem>();

            var main = sparkPS.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new Color(1f, 0.8f, 0.3f, 1f);
            main.gravityModifier = 3f;
            main.maxParticles = 30;
            main.playOnAwake = false;

            var emission = sparkPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 5, 12) });

            var shape = sparkPS.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;

            var colorOverLifetime = sparkPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 1f, 0.5f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var trails = sparkPS.trails;
            trails.enabled = true;
            trails.lifetime = 0.1f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(1f, 0.8f, 0.3f));
            renderer.trailMaterial = CreateParticleMaterial(new Color(1f, 0.6f, 0.2f, 0.5f));
            renderer.sortingOrder = 55;
        }

        private void CreateDustSystem()
        {
            var go = new GameObject("Dust_PS");
            go.transform.SetParent(transform);
            dustPS = go.AddComponent<ParticleSystem>();

            var main = dustPS.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new Color(0.6f, 0.55f, 0.45f, 0.6f);
            main.gravityModifier = -0.1f;
            main.maxParticles = 20;
            main.playOnAwake = false;

            var emission = dustPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 5, 10) });

            var shape = dustPS.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;

            var sizeOverLifetime = dustPS.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 1.5f));

            var colorOverLifetime = dustPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.6f, 0.55f, 0.45f), 0f) },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.6f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(0.6f, 0.55f, 0.45f, 0.5f));
            renderer.sortingOrder = 25;
        }

        private void CreateDeathExplosionSystem()
        {
            var go = new GameObject("DeathExplosion_PS");
            go.transform.SetParent(transform);
            deathExplosionPS = go.AddComponent<ParticleSystem>();

            var main = deathExplosionPS.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = new Color(0.5f, 0.55f, 0.4f, 1f);
            main.gravityModifier = 1.5f;
            main.maxParticles = 30;
            main.playOnAwake = false;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);

            var emission = deathExplosionPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15, 25) });

            var shape = deathExplosionPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var rotationOverLifetime = deathExplosionPS.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            var colorOverLifetime = deathExplosionPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.5f, 0.55f, 0.4f), 0f),
                    new GradientColorKey(new Color(0.3f, 0.32f, 0.25f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(0.5f, 0.55f, 0.4f));
            renderer.sortingOrder = 60;
        }

        private void CreateToxicExplosionSystem()
        {
            var go = new GameObject("ToxicExplosion_PS");
            go.transform.SetParent(transform);
            toxicExplosionPS = go.AddComponent<ParticleSystem>();

            var main = toxicExplosionPS.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = new Color(0.5f, 0.75f, 0.2f, 0.8f);
            main.gravityModifier = -0.3f;
            main.maxParticles = 50;
            main.playOnAwake = false;

            var emission = toxicExplosionPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { 
                new ParticleSystem.Burst(0f, 20, 30),
                new ParticleSystem.Burst(0.1f, 10, 15)
            });

            var shape = toxicExplosionPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var sizeOverLifetime = toxicExplosionPS.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 2f));

            var colorOverLifetime = toxicExplosionPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.6f, 0.85f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.3f, 0.5f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.2f, 0.3f, 0.1f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(0.5f, 0.75f, 0.2f, 0.5f));
            renderer.sortingOrder = 65;
        }

        private void CreateHealingSystem()
        {
            var go = new GameObject("Healing_PS");
            go.transform.SetParent(transform);
            healingPS = go.AddComponent<ParticleSystem>();

            var main = healingPS.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);
            main.gravityModifier = -1f;
            main.maxParticles = 30;
            main.playOnAwake = false;

            var emission = healingPS.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10, 20) });

            var shape = healingPS.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;

            var colorOverLifetime = healingPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.3f, 1f, 0.4f), 0f),
                    new GradientColorKey(new Color(0.1f, 0.8f, 0.2f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(0.2f, 0.9f, 0.3f));
            renderer.sortingOrder = 70;
        }

        private Material CreateParticleMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            return mat;
        }

        #endregion

        #region Public VFX Methods

        public void PlayMuzzleFlash(Vector3 position, Vector3 direction)
        {
            if (muzzleFlashPS == null) return;

            muzzleFlashPS.transform.position = position;
            muzzleFlashPS.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            muzzleFlashPS.Play();

            CreateMuzzleLight(position);
        }

        public void PlayBloodSplatter(Vector3 position, Vector3 direction)
        {
            if (bloodSplatterPS == null) return;

            bloodSplatterPS.transform.position = position;
            bloodSplatterPS.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            bloodSplatterPS.Play();

            CreateBloodDecal(position);
        }

        public void PlaySparks(Vector3 position, Vector3 normal)
        {
            if (sparkPS == null) return;

            sparkPS.transform.position = position;
            sparkPS.transform.rotation = Quaternion.LookRotation(Vector3.forward, normal);
            sparkPS.Play();
        }

        public void PlayDust(Vector3 position)
        {
            if (dustPS == null) return;

            dustPS.transform.position = position;
            dustPS.Play();
        }

        public void PlayDeathExplosion(Vector3 position, bool isToxic = false)
        {
            if (isToxic && toxicExplosionPS != null)
            {
                toxicExplosionPS.transform.position = position;
                toxicExplosionPS.Play();
                CreateToxicDecal(position);
            }
            else if (deathExplosionPS != null)
            {
                deathExplosionPS.transform.position = position;
                deathExplosionPS.Play();
                CreateBloodDecal(position, 1.5f);
            }
        }

        public void PlayHealing(Vector3 position)
        {
            if (healingPS == null) return;

            healingPS.transform.position = position;
            healingPS.Play();
        }

        public void PlayBulletImpact(Vector3 position, Vector3 normal, bool hitEnemy)
        {
            if (hitEnemy)
            {
                PlayBloodSplatter(position, normal);
            }
            else
            {
                PlaySparks(position, normal);
                PlayDust(position);
            }
        }

        #endregion

        #region Decals and Lights

        private void CreateMuzzleLight(Vector3 position)
        {
            var lightGO = new GameObject("MuzzleLight");
            lightGO.transform.position = position + Vector3.back * 0.1f;
            
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.4f);
            light.intensity = 2f;
            light.range = 3f;

            StartCoroutine(FadeLight(lightGO, 0.1f));
        }

        private IEnumerator FadeLight(GameObject lightGO, float duration)
        {
            var light = lightGO.GetComponent<Light>();
            float startIntensity = light.intensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / duration);
                yield return null;
            }

            Destroy(lightGO);
        }

        private void CreateBloodDecal(Vector3 position, float scale = 1f)
        {
            var decal = new GameObject("BloodDecal");
            decal.transform.position = position + Vector3.back * 0.01f;
            decal.transform.localScale = Vector3.one * (0.3f + Random.Range(0f, 0.2f)) * scale;
            decal.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            var sr = decal.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDecalSprite(new Color(0.5f, 0.05f, 0.03f, 0.7f));
            sr.sortingOrder = -5;

            activeDecals.Add(decal);
            if (activeDecals.Count > maxDecals)
            {
                var oldest = activeDecals[0];
                activeDecals.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }

            StartCoroutine(FadeDecal(decal, bloodDecalDuration));
        }

        private void CreateToxicDecal(Vector3 position)
        {
            var decal = new GameObject("ToxicDecal");
            decal.transform.position = position + Vector3.back * 0.01f;
            decal.transform.localScale = Vector3.one * (0.8f + Random.Range(0f, 0.4f));
            decal.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            var sr = decal.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDecalSprite(new Color(0.4f, 0.6f, 0.15f, 0.6f));
            sr.sortingOrder = -5;

            activeDecals.Add(decal);
            StartCoroutine(FadeDecal(decal, bloodDecalDuration * 0.5f));
        }

        private Sprite CreateDecalSprite(Color color)
        {
            int size = 32;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxRadius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    float edgeFade = 1f - Mathf.Clamp01((dist - maxRadius * 0.5f) / (maxRadius * 0.5f));
                    float alpha = edgeFade * (0.5f + noise * 0.5f);

                    if (dist < maxRadius * (0.8f + noise * 0.4f))
                    {
                        texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
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

        private IEnumerator FadeDecal(GameObject decal, float duration)
        {
            yield return new WaitForSeconds(duration * 0.8f);

            if (decal == null) yield break;

            var sr = decal.GetComponent<SpriteRenderer>();
            if (sr == null) yield break;

            Color startColor = sr.color;
            float fadeDuration = duration * 0.2f;
            float elapsed = 0f;

            while (elapsed < fadeDuration && decal != null)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            if (decal != null)
            {
                activeDecals.Remove(decal);
                Destroy(decal);
            }
        }

        #endregion

        #region Screen Effects

        public void TriggerScreenShake(float intensity = 0.3f, float duration = 0.15f)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var controller = cam.GetComponent<Core.CameraController>();
                if (controller != null)
                {
                    controller.Shake(duration, intensity);
                }
            }
        }

        public void TriggerHitStop(float duration = 0.05f)
        {
            StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.02f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalTimeScale;
        }

        #endregion

        public void ClearAllDecals()
        {
            foreach (var decal in activeDecals)
            {
                if (decal != null) Destroy(decal);
            }
            activeDecals.Clear();
        }
    }
}
