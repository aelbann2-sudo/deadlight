using UnityEngine;
using Deadlight.Visuals;

namespace Deadlight.Level
{
    public class MapLandmarks : MonoBehaviour
    {
        public static MapLandmarks Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void CreateAllLandmarks(Transform parent)
        {
            CreateCrashedHelicopter(parent, new Vector3(0, 10, 0));
            CreateMilitaryCheckpoint(parent, new Vector3(-9, 0, 0));
            CreateResearchLabEntrance(parent, new Vector3(0, -10, 0));
            CreateGasStation(parent, new Vector3(8, -5, 0));
            CreateStreetlights(parent);
        }

        public void CreateCrashedHelicopter(Transform parent, Vector3 position)
        {
            var crashSite = new GameObject("CrashSite");
            crashSite.transform.SetParent(parent);
            crashSite.transform.position = position;

            var heliBody = new GameObject("HelicopterBody");
            heliBody.transform.SetParent(crashSite.transform);
            heliBody.transform.localPosition = Vector3.zero;
            heliBody.transform.rotation = Quaternion.Euler(0, 0, 25f);

            var sr = heliBody.AddComponent<SpriteRenderer>();
            sr.sprite = CreateHelicopterSprite();
            sr.sortingOrder = 5;
            sr.color = new Color(0.4f, 0.4f, 0.4f);

            var col = heliBody.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 1.2f);

            var fireEffect = new GameObject("Fire");
            fireEffect.transform.SetParent(crashSite.transform);
            fireEffect.transform.localPosition = new Vector3(-1f, 0.3f, 0);
            fireEffect.AddComponent<FireEffect>();

            var smokeEffect = new GameObject("Smoke");
            smokeEffect.transform.SetParent(crashSite.transform);
            smokeEffect.transform.localPosition = new Vector3(0.5f, 0.8f, 0);
            smokeEffect.AddComponent<SmokeEffect>();

            for (int i = 0; i < 3; i++)
            {
                var debris = new GameObject($"Debris_{i}");
                debris.transform.SetParent(crashSite.transform);
                debris.transform.localPosition = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), 0);
                var dsr = debris.AddComponent<SpriteRenderer>();
                dsr.sprite = CreateDebrisSprite();
                dsr.sortingOrder = 4;
                dsr.color = new Color(0.3f, 0.3f, 0.3f);
            }

            var supplyCrate = new GameObject("SupplyCrate");
            supplyCrate.transform.SetParent(crashSite.transform);
            supplyCrate.transform.localPosition = new Vector3(2f, -0.5f, 0);
            var scsr = supplyCrate.AddComponent<SpriteRenderer>();
            scsr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
            scsr.sortingOrder = 5;
            scsr.color = new Color(0.4f, 0.5f, 0.3f);
        }

        public void CreateMilitaryCheckpoint(Transform parent, Vector3 position)
        {
            var checkpoint = new GameObject("MilitaryCheckpoint");
            checkpoint.transform.SetParent(parent);
            checkpoint.transform.position = position;

            Vector3[] sandbagPositions = {
                new Vector3(-1f, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1f, 0.5f, 0),
                new Vector3(-1f, -0.5f, 0), new Vector3(1f, -0.5f, 0)
            };

            foreach (var pos in sandbagPositions)
            {
                var sandbag = new GameObject("Sandbag");
                sandbag.transform.SetParent(checkpoint.transform);
                sandbag.transform.localPosition = pos;
                var sr = sandbag.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSandbagSprite();
                sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 5;
                var col = sandbag.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 0.4f);
            }

            var barricade = new GameObject("Barricade");
            barricade.transform.SetParent(checkpoint.transform);
            barricade.transform.localPosition = new Vector3(0, 1.5f, 0);
            var bsr = barricade.AddComponent<SpriteRenderer>();
            bsr.sprite = CreateBarricadeSprite();
            bsr.sortingOrder = 4;
            var bcol = barricade.AddComponent<BoxCollider2D>();
            bcol.size = new Vector2(2.5f, 0.5f);

            var searchlight = new GameObject("Searchlight");
            searchlight.transform.SetParent(checkpoint.transform);
            searchlight.transform.localPosition = new Vector3(1.5f, 1.5f, 0);
            searchlight.AddComponent<SearchlightEffect>();

            for (int i = 0; i < 2; i++)
            {
                var bodyBag = new GameObject($"BodyBag_{i}");
                bodyBag.transform.SetParent(checkpoint.transform);
                bodyBag.transform.localPosition = new Vector3(-2f + i * 0.8f, -1f, 0);
                var bbsr = bodyBag.AddComponent<SpriteRenderer>();
                bbsr.sprite = CreateBodyBagSprite();
                bbsr.sortingOrder = 3;
            }
        }

        public void CreateResearchLabEntrance(Transform parent, Vector3 position)
        {
            var labEntrance = new GameObject("ResearchLabEntrance");
            labEntrance.transform.SetParent(parent);
            labEntrance.transform.position = position;

            var building = new GameObject("LabBuilding");
            building.transform.SetParent(labEntrance.transform);
            building.transform.localPosition = Vector3.zero;
            var sr = building.AddComponent<SpriteRenderer>();
            sr.sprite = CreateLabBuildingSprite();
            sr.sortingOrder = 5;
            var col = building.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 2f);
            col.offset = new Vector2(0, 0.5f);

            var glow = new GameObject("EerieGlow");
            glow.transform.SetParent(labEntrance.transform);
            glow.transform.localPosition = new Vector3(0, -0.3f, 0);
            var glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite = CreateGlowSprite(new Color(0.2f, 0.8f, 0.3f, 0.3f));
            glowSr.sortingOrder = 4;
            glow.AddComponent<GlowPulse>();

            var hazardSign = new GameObject("HazardSign");
            hazardSign.transform.SetParent(labEntrance.transform);
            hazardSign.transform.localPosition = new Vector3(2.5f, 0.5f, 0);
            var hsr = hazardSign.AddComponent<SpriteRenderer>();
            hsr.sprite = CreateHazardSignSprite();
            hsr.sortingOrder = 6;
        }

        public void CreateGasStation(Transform parent, Vector3 position)
        {
            var station = new GameObject("GasStation");
            station.transform.SetParent(parent);
            station.transform.position = position;

            var canopy = new GameObject("Canopy");
            canopy.transform.SetParent(station.transform);
            canopy.transform.localPosition = Vector3.zero;
            var sr = canopy.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCanopySprite();
            sr.sortingOrder = 6;
            var col = canopy.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 1.5f);

            var sign = new GameObject("NeonSign");
            sign.transform.SetParent(station.transform);
            sign.transform.localPosition = new Vector3(0, 1.5f, 0);
            sign.AddComponent<FlickeringLight>();
            var signSr = sign.AddComponent<SpriteRenderer>();
            signSr.sprite = CreateNeonSignSprite();
            signSr.sortingOrder = 7;

            var pump = new GameObject("FuelPump");
            pump.transform.SetParent(station.transform);
            pump.transform.localPosition = new Vector3(-1f, -0.8f, 0);
            var psr = pump.AddComponent<SpriteRenderer>();
            psr.sprite = CreateFuelPumpSprite();
            psr.sortingOrder = 5;
        }

        public void CreateStreetlights(Transform parent)
        {
            Vector3[] lightPositions = {
                new Vector3(-5, 5, 0), new Vector3(5, 5, 0),
                new Vector3(-5, -5, 0), new Vector3(5, -5, 0),
                new Vector3(0, 3, 0), new Vector3(0, -3, 0),
            };

            var lightsParent = new GameObject("Streetlights");
            lightsParent.transform.SetParent(parent);

            foreach (var pos in lightPositions)
            {
                var light = new GameObject("Streetlight");
                light.transform.SetParent(lightsParent.transform);
                light.transform.position = pos;

                var pole = new GameObject("Pole");
                pole.transform.SetParent(light.transform);
                pole.transform.localPosition = Vector3.zero;
                var sr = pole.AddComponent<SpriteRenderer>();
                sr.sprite = CreateStreetlightSprite();
                sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 10;

                var col = pole.AddComponent<CircleCollider2D>();
                col.radius = 0.15f;

                var glowObj = new GameObject("LightGlow");
                glowObj.transform.SetParent(light.transform);
                glowObj.transform.localPosition = new Vector3(0, 1.2f, 0);
                var glowSr = glowObj.AddComponent<SpriteRenderer>();
                glowSr.sprite = CreateGlowSprite(new Color(1f, 0.9f, 0.6f, 0.25f));
                glowSr.sortingOrder = -100;
                glowObj.transform.localScale = Vector3.one * 3f;
                
                if (Random.value < 0.3f)
                {
                    glowObj.AddComponent<FlickeringLight>();
                }
            }
        }

        private Sprite CreateHelicopterSprite()
        {
            int w = 64, h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            for (int y = 8; y < 24; y++)
            {
                for (int x = 8; x < 56; x++)
                {
                    pixels[y * w + x] = new Color(0.3f, 0.35f, 0.3f);
                }
            }

            for (int y = 12; y < 20; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    pixels[y * w + x] = new Color(0.25f, 0.3f, 0.25f);
                }
            }

            for (int x = 20; x < 52; x++)
            {
                pixels[24 * w + x] = new Color(0.2f, 0.25f, 0.2f);
                pixels[25 * w + x] = new Color(0.2f, 0.25f, 0.2f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateDebrisSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color metalColor = new Color(0.4f, 0.42f, 0.4f);

            for (int i = 0; i < 20; i++)
            {
                int x = Random.Range(2, size - 2);
                int y = Random.Range(2, size - 2);
                pixels[y * size + x] = metalColor;
                if (x + 1 < size) pixels[y * size + x + 1] = metalColor;
                if (y + 1 < size) pixels[(y + 1) * size + x] = metalColor;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateSandbagSprite()
        {
            int w = 16, h = 8;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color sandColor = new Color(0.7f, 0.6f, 0.4f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float shade = 0.9f + Random.Range(-0.1f, 0.1f);
                    pixels[y * w + x] = sandColor * shade;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateBarricadeSprite()
        {
            int w = 48, h = 16;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if ((x / 6) % 2 == 0)
                        pixels[y * w + x] = new Color(0.9f, 0.2f, 0.1f);
                    else
                        pixels[y * w + x] = new Color(0.9f, 0.9f, 0.9f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateBodyBagSprite()
        {
            int w = 24, h = 12;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color bagColor = new Color(0.15f, 0.15f, 0.15f);

            for (int y = 2; y < h - 2; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = bagColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateLabBuildingSprite()
        {
            int w = 64, h = 48;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color wallColor = new Color(0.5f, 0.5f, 0.55f);
            Color darkColor = new Color(0.2f, 0.2f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y < 8 || x < 4 || x >= w - 4)
                        pixels[y * w + x] = darkColor;
                    else
                        pixels[y * w + x] = wallColor;
                }
            }

            for (int y = 16; y < 32; y++)
            {
                for (int x = 24; x < 40; x++)
                {
                    pixels[y * w + x] = new Color(0.1f, 0.3f, 0.15f, 0.8f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateHazardSignSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) < size / 2f)
                    {
                        pixels[y * size + x] = new Color(1f, 0.8f, 0f);
                    }
                }
            }

            for (int i = 4; i < 12; i++)
            {
                pixels[8 * size + i] = Color.black;
            }
            for (int i = 5; i < 11; i++)
            {
                pixels[i * size + 8] = Color.black;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateCanopySprite()
        {
            int w = 48, h = 24;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color roofColor = new Color(0.6f, 0.1f, 0.1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = roofColor;
                }
            }

            for (int y = 0; y < 4; y++)
            {
                for (int x = 4; x < 8; x++) pixels[y * w + x] = new Color(0.3f, 0.3f, 0.3f);
                for (int x = w - 8; x < w - 4; x++) pixels[y * w + x] = new Color(0.3f, 0.3f, 0.3f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateNeonSignSprite()
        {
            int w = 32, h = 16;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            for (int y = 2; y < h - 2; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.15f);
                }
            }

            Color neonColor = new Color(1f, 0.3f, 0.3f);
            for (int i = 4; i < 12; i++)
            {
                pixels[8 * w + i] = neonColor;
                pixels[8 * w + (w - i)] = neonColor;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateFuelPumpSprite()
        {
            int w = 12, h = 24;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color pumpColor = new Color(0.8f, 0.2f, 0.2f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = pumpColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateStreetlightSprite()
        {
            int w = 8, h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color poleColor = new Color(0.3f, 0.3f, 0.35f);

            for (int y = 0; y < h - 4; y++)
            {
                pixels[y * w + 3] = poleColor;
                pixels[y * w + 4] = poleColor;
            }

            Color lampColor = new Color(0.4f, 0.4f, 0.3f);
            for (int y = h - 6; y < h; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    pixels[y * w + x] = lampColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateGlowSprite(Color glowColor)
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    if (dist < 1f)
                    {
                        float alpha = (1f - dist) * glowColor.a;
                        pixels[y * size + x] = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }

    public class FireEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float flickerTimer;

        void Start()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFireSprite();
            sr.sortingOrder = 20;
        }

        void Update()
        {
            flickerTimer += Time.deltaTime * 8f;
            float scale = 1f + Mathf.Sin(flickerTimer) * 0.2f;
            transform.localScale = new Vector3(scale, scale * 1.2f, 1f);
            sr.color = new Color(1f, 0.6f + Mathf.Sin(flickerTimer * 1.3f) * 0.2f, 0.2f, 0.8f);
        }

        Sprite CreateFireSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 4f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = y - center.y;
                    if (dy > 0 && dx < (size / 2f) * (1f - dy / size))
                    {
                        float t = dy / size;
                        pixels[y * size + x] = Color.Lerp(new Color(1f, 0.6f, 0.1f), new Color(1f, 0.2f, 0f, 0f), t);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), 16f);
        }
    }

    public class SmokeEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float driftTimer;

        void Start()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSmokeSprite();
            sr.sortingOrder = 19;
            sr.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        }

        void Update()
        {
            driftTimer += Time.deltaTime;
            float drift = Mathf.Sin(driftTimer * 0.5f) * 0.3f;
            transform.localPosition = new Vector3(0.5f + drift, 0.8f + driftTimer % 2f * 0.5f, 0);
            float alpha = 0.4f * (1f - (driftTimer % 2f) / 2f);
            sr.color = new Color(0.3f, 0.3f, 0.3f, alpha);
            transform.localScale = Vector3.one * (1f + (driftTimer % 2f) * 0.5f);
        }

        Sprite CreateSmokeSprite()
        {
            int size = 24;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    if (dist < 1f)
                    {
                        pixels[y * size + x] = new Color(0.5f, 0.5f, 0.5f, (1f - dist) * 0.6f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }

    public class SearchlightEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float rotationTimer;

        void Start()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBeamSprite();
            sr.sortingOrder = -50;
            transform.localScale = new Vector3(1f, 4f, 1f);
        }

        void Update()
        {
            rotationTimer += Time.deltaTime * 20f;
            transform.rotation = Quaternion.Euler(0, 0, rotationTimer);
        }

        Sprite CreateBeamSprite()
        {
            int w = 16, h = 64;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w / 2f) / (w / 2f);
                    float dy = (float)y / h;
                    float alpha = (1f - dx) * (1f - dy) * 0.3f;
                    pixels[y * w + x] = new Color(1f, 1f, 0.8f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }
    }

    public class FlickeringLight : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float flickerTimer;
        private float nextFlicker;
        private bool isOn = true;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
            nextFlicker = Random.Range(0.5f, 3f);
        }

        void Update()
        {
            flickerTimer += Time.deltaTime;
            if (flickerTimer >= nextFlicker)
            {
                flickerTimer = 0f;
                nextFlicker = Random.Range(0.1f, 2f);
                isOn = !isOn || Random.value > 0.3f;
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = isOn ? 0.25f : 0.05f;
                    sr.color = c;
                }
            }
        }
    }

    public class GlowPulse : MonoBehaviour
    {
        void Update()
        {
            float pulse = 0.8f + Mathf.Sin(Time.time * 2f) * 0.2f;
            transform.localScale = Vector3.one * pulse * 2f;
        }
    }
}
