using Deadlight.Level;
using Deadlight.Visuals;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public static class TownCenterLandmarks
    {
        public static readonly Vector3 CrashSitePosition = new Vector3(0f, 29f, 0f);
        public static readonly Vector3 MilitaryCheckpointPosition = new Vector3(-27f, 0f, 0f);
        public static readonly Vector3 GasStationPosition = new Vector3(25f, -20f, 0f);
        public static readonly Vector3 DinerPosition = new Vector3(-20f, 25f, 0f);
        public static readonly Vector3 SchoolPosition = new Vector3(25f, 25f, 0f);
        public static readonly Vector3 HospitalPosition = new Vector3(-15f, 5f, 0f);

        public static readonly Vector3[] StreetlightPositions =
        {
            new Vector3(-15f, 15f, 0f),
            new Vector3(15f, 15f, 0f),
            new Vector3(-15f, -15f, 0f),
            new Vector3(15f, -15f, 0f),
            new Vector3(-30f, 0f, 0f),
            new Vector3(30f, 0f, 0f),
            new Vector3(8f, 30f, 0f),
            new Vector3(0f, -30f, 0f),
            new Vector3(-15f, 30f, 0f),
            new Vector3(15f, -30f, 0f),
            new Vector3(-25f, 15f, 0f),
            new Vector3(25f, -15f, 0f)
        };

        public static void Create(Transform parent)
        {
            var root = new GameObject("TownCenterLandmarks").transform;
            root.SetParent(parent);

            CreateCrashedHelicopter(root, CrashSitePosition);
            CreateMilitaryCheckpoint(root, MilitaryCheckpointPosition);
            CreateGasStation(root, GasStationPosition);
            CreateDiner(root, DinerPosition);
            CreateSchool(root, SchoolPosition);
            CreateHospital(root, HospitalPosition);
            CreateStreetlights(root, StreetlightPositions);
        }

        private static void CreateCrashedHelicopter(Transform parent, Vector3 position)
        {
            var crashSite = new GameObject("CrashSite").transform;
            crashSite.SetParent(parent);
            crashSite.position = position;

            var heliBody = CreateSpriteObject(crashSite, "HelicopterBody", CreateHelicopterSprite(), Vector3.zero, 5);
            heliBody.transform.rotation = Quaternion.Euler(0f, 0f, 25f);
            var bodyCollider = heliBody.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                bodyCollider,
                heliBody.GetComponent<SpriteRenderer>().sprite,
                heliBody.transform.localScale,
                new Vector2(3f, 1.2f));

            var fireEffect = new GameObject("Fire");
            fireEffect.transform.SetParent(crashSite);
            fireEffect.transform.localPosition = new Vector3(-1f, 0.3f, 0f);
            fireEffect.AddComponent<FireEffect>();

            var smokeEffect = new GameObject("Smoke");
            smokeEffect.transform.SetParent(crashSite);
            smokeEffect.transform.localPosition = new Vector3(0.5f, 0.8f, 0f);
            smokeEffect.AddComponent<SmokeEffect>();

            for (int i = 0; i < 3; i++)
            {
                Vector3 debrisPos = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), 0f);
                CreateSpriteObject(crashSite, $"Debris_{i}", CreateDebrisSprite(), debrisPos, 4)
                    .GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.3f, 0.3f);
            }

            var crate = CreateSpriteObject(crashSite, "SupplyCrate", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(2f, -0.5f, 0f), 5);
            crate.GetComponent<SpriteRenderer>().color = new Color(0.4f, 0.5f, 0.3f);
        }

        private static void CreateMilitaryCheckpoint(Transform parent, Vector3 position)
        {
            var checkpoint = new GameObject("MilitaryCheckpoint").transform;
            checkpoint.SetParent(parent);
            checkpoint.position = position;

            var searchlight = new GameObject("Searchlight");
            searchlight.transform.SetParent(checkpoint);
            searchlight.transform.localPosition = new Vector3(1.8f, 0.7f, 0f);
            searchlight.AddComponent<SearchlightEffect>();

            var ammo = CreateSpriteObject(checkpoint, "AmmoCase", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(-2f, -1f, 0f), 4);
            ammo.GetComponent<SpriteRenderer>().color = new Color(0.38f, 0.48f, 0.3f);

            CreateSpriteObject(checkpoint, "CheckpointPost", CreateCheckpointPostSprite(), new Vector3(2.4f, -0.2f, 0f), 4);
        }

        private static void CreateGasStation(Transform parent, Vector3 position)
        {
            var station = new GameObject("GasStation").transform;
            station.SetParent(parent);
            station.position = position;

            var canopy = CreateSpriteObject(station, "Canopy", CreateCanopySprite(), Vector3.zero, 6);
            var canopyCollider = canopy.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCenteredFootprint(canopyCollider, new Vector2(3f, 1.5f), 0.82f, 0.22f, 0.02f, 0.28f);

            var sign = CreateSpriteObject(station, "NeonSign", CreateNeonSignSprite(), new Vector3(0f, 1.5f, 0f), 7);
            sign.AddComponent<FlickeringLight>();

            CreateSpriteObject(station, "FuelPump", CreateFuelPumpSprite(), new Vector3(-1f, -0.8f, 0f), 5);
        }

        private static void CreateDiner(Transform parent, Vector3 position)
        {
            var diner = new GameObject("Diner").transform;
            diner.SetParent(parent);
            diner.position = position;

            var building = CreateSpriteObject(diner, "Building", CreateDinerSprite(), Vector3.zero, 5);
            var buildingCollider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(buildingCollider, building.GetComponent<SpriteRenderer>().sprite, building.transform.localScale, 0.92f, 0.9f);

            var sign = CreateSpriteObject(diner, "NeonSign", CreateDinerSignSprite(), new Vector3(0f, 1.2f, 0f), 6);
            sign.AddComponent<FlickeringLight>();
        }

        private static void CreateSchool(Transform parent, Vector3 position)
        {
            var school = new GameObject("School").transform;
            school.SetParent(parent);
            school.position = position;

            var building = CreateSpriteObject(school, "Building", CreateSchoolSprite(), Vector3.zero, 5);
            var buildingCollider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(buildingCollider, building.GetComponent<SpriteRenderer>().sprite, building.transform.localScale, 0.92f, 0.9f);

            var bus = CreateSpriteObject(school, "SchoolBus", CreateSchoolBusSprite(), new Vector3(-2.2f, -1.1f, 0f), 4);
            bus.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            CreateSpriteObject(school, "SchoolSign", CreateSchoolSignSprite(), new Vector3(2.4f, -1f, 0f), 5);
        }

        private static void CreateHospital(Transform parent, Vector3 position)
        {
            var hospital = new GameObject("Hospital").transform;
            hospital.SetParent(parent);
            hospital.position = position;

            var building = CreateSpriteObject(hospital, "Building", CreateHospitalSprite(), Vector3.zero, 5);
            var buildingCollider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(buildingCollider, building.GetComponent<SpriteRenderer>().sprite, building.transform.localScale, 0.92f, 0.9f);

            CreateSpriteObject(hospital, "Ambulance", CreateAmbulanceSprite(), new Vector3(-2.2f, -1.1f, 0f), 4);
            CreateSpriteObject(hospital, "EmergencySign", CreateHospitalSignSprite(), new Vector3(2.5f, -1f, 0f), 5);
        }

        private static void CreateStreetlights(Transform parent, Vector3[] positions)
        {
            var lightsParent = new GameObject("Streetlights").transform;
            lightsParent.SetParent(parent);

            foreach (Vector3 pos in positions)
            {
                var light = new GameObject("Streetlight").transform;
                light.SetParent(lightsParent);
                light.position = pos;

                CreateSpriteObject(light, "Pole", CreateStreetlightSprite(), Vector3.zero, 10);

                var glow = CreateSpriteObject(light, "LightGlow", CreateGlowSprite(new Color(1f, 0.9f, 0.6f, 0.25f)), new Vector3(0f, 1.2f, 0f), -100);
                glow.transform.localScale = Vector3.one * 3f;
                if (Random.value < 0.3f)
                {
                    glow.AddComponent<FlickeringLight>();
                }
            }
        }

        private static GameObject CreateSpriteObject(Transform parent, string name, Sprite sprite, Vector3 localPosition, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = LandmarkSpriteUtility.ResolveSortingOrder(parent, localPosition, sortingOrder);
            return go;
        }

        private static Sprite CreateDinerSprite()
        {
            const int w = 48;
            const int h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color wallColor = new Color(0.9f, 0.7f, 0.2f);
            Color roofColor = new Color(0.7f, 0.2f, 0.2f);

            for (int y = 4; y < h - 4; y++)
            {
                for (int x = 4; x < w - 4; x++)
                {
                    pixels[y * w + x] = wallColor;
                }
            }

            for (int y = h - 6; y < h; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = roofColor;
                }
            }

            for (int wx = 8; wx < w - 8; wx += 8)
            {
                for (int y = h / 2; y < h - 8; y++)
                {
                    for (int dx = 0; dx < 4 && wx + dx < w; dx++)
                    {
                        pixels[y * w + wx + dx] = new Color(0.7f, 0.9f, 1f, 0.8f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateDinerSignSprite()
        {
            const int w = 32;
            const int h = 16;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color signColor = new Color(0.9f, 0.2f, 0.2f);
            Color textColor = new Color(1f, 1f, 0.8f);

            for (int y = 2; y < h - 2; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = signColor;
                }
            }

            for (int i = 6; i < 12; i++)
            {
                pixels[8 * w + i] = textColor;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateSchoolSprite()
        {
            const int w = 72;
            const int h = 40;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color wall = new Color(0.82f, 0.72f, 0.46f);
            Color roof = new Color(0.62f, 0.2f, 0.16f);
            Color window = new Color(0.65f, 0.83f, 0.95f);

            for (int y = 3; y < h - 8; y++)
            {
                for (int x = 3; x < w - 3; x++)
                {
                    pixels[y * w + x] = wall;
                }
            }

            for (int y = h - 8; y < h - 2; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = roof;
                }
            }

            for (int wy = 12; wy <= 22; wy += 10)
            {
                for (int wx = 8; wx < w - 12; wx += 12)
                {
                    for (int y = wy; y < wy + 6; y++)
                    {
                        for (int x = wx; x < wx + 6; x++)
                        {
                            pixels[y * w + x] = window;
                        }
                    }
                }
            }

            for (int y = 3; y < 12; y++)
            {
                for (int x = 28; x < 44; x++)
                {
                    pixels[y * w + x] = new Color(0.45f, 0.28f, 0.14f);
                }
            }

            for (int x = 20; x < 52; x++)
            {
                pixels[(h - 14) * w + x] = new Color(0.95f, 0.95f, 0.82f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 16f);
        }

        private static Sprite CreateSchoolSignSprite()
        {
            const int w = 16;
            const int h = 20;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color post = new Color(0.45f, 0.45f, 0.48f);
            Color sign = new Color(0.96f, 0.88f, 0.32f);

            for (int y = 0; y < 10; y++)
            {
                for (int x = 7; x < 9; x++)
                {
                    pixels[y * w + x] = post;
                }
            }

            for (int y = 10; y < 18; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = sign;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 16f);
        }

        private static Sprite CreateHospitalSprite()
        {
            const int w = 80;
            const int h = 40;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color wall = new Color(0.9f, 0.92f, 0.95f);
            Color roof = new Color(0.65f, 0.7f, 0.74f);
            Color window = new Color(0.62f, 0.84f, 0.96f);
            Color cross = new Color(0.82f, 0.12f, 0.12f);

            for (int y = 3; y < h - 8; y++)
            {
                for (int x = 3; x < w - 3; x++)
                {
                    pixels[y * w + x] = wall;
                }
            }

            for (int y = h - 8; y < h - 2; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = roof;
                }
            }

            for (int wy = 13; wy <= 23; wy += 10)
            {
                for (int wx = 8; wx < w - 12; wx += 12)
                {
                    for (int y = wy; y < wy + 6; y++)
                    {
                        for (int x = wx; x < wx + 6; x++)
                        {
                            pixels[y * w + x] = window;
                        }
                    }
                }
            }

            for (int y = 5; y < 15; y++)
            {
                for (int x = 36; x < 44; x++)
                {
                    pixels[y * w + x] = cross;
                }
            }

            for (int y = 8; y < 12; y++)
            {
                for (int x = 32; x < 48; x++)
                {
                    pixels[y * w + x] = cross;
                }
            }

            for (int y = 3; y < 12; y++)
            {
                for (int x = 34; x < 46; x++)
                {
                    pixels[y * w + x] = new Color(0.52f, 0.58f, 0.62f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 16f);
        }

        private static Sprite CreateHospitalSignSprite()
        {
            const int w = 16;
            const int h = 20;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color post = new Color(0.45f, 0.45f, 0.48f);
            Color sign = new Color(0.95f, 0.95f, 0.95f);
            Color cross = new Color(0.82f, 0.12f, 0.12f);

            for (int y = 0; y < 10; y++)
            {
                for (int x = 7; x < 9; x++)
                {
                    pixels[y * w + x] = post;
                }
            }

            for (int y = 10; y < 18; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = sign;
                }
            }

            for (int y = 12; y < 16; y++)
            {
                for (int x = 6; x < 10; x++)
                {
                    pixels[y * w + x] = cross;
                }
            }

            for (int y = 13; y < 15; y++)
            {
                for (int x = 4; x < 12; x++)
                {
                    pixels[y * w + x] = cross;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 16f);
        }

        private static Sprite CreateAmbulanceSprite()
        {
            const int w = 28;
            const int h = 14;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color body = new Color(0.94f, 0.94f, 0.96f);
            Color stripe = new Color(0.85f, 0.18f, 0.16f);
            Color window = new Color(0.6f, 0.8f, 0.94f);

            for (int y = 2; y < h - 2; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = body;
                }
            }

            for (int y = 5; y < 8; y++)
            {
                for (int x = 3; x < w - 3; x++)
                {
                    pixels[y * w + x] = stripe;
                }
            }

            for (int y = 8; y < h - 3; y++)
            {
                for (int x = 6; x < 12; x++)
                {
                    pixels[y * w + x] = window;
                }
            }

            for (int y = 8; y < h - 3; y++)
            {
                for (int x = 14; x < 20; x++)
                {
                    pixels[y * w + x] = window;
                }
            }

            for (int y = 1; y < 4; y++)
            {
                for (int x = 20; x < 24; x++)
                {
                    pixels[y * w + x] = stripe;
                }
            }

            for (int y = 0; y < 3; y++)
            {
                for (int x = 6; x < 10; x++)
                {
                    pixels[y * w + x] = Color.black;
                }
            }

            for (int y = 0; y < 3; y++)
            {
                for (int x = 18; x < 22; x++)
                {
                    pixels[y * w + x] = Color.black;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 16f);
        }

        private static Sprite CreateSchoolBusSprite()
        {
            const int w = 48;
            const int h = 20;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color bodyColor = new Color(1f, 0.8f, 0.2f);
            Color detailColor = new Color(0.2f, 0.2f, 0.8f);

            for (int y = 3; y < h - 3; y++)
            {
                for (int x = 3; x < w - 3; x++)
                {
                    pixels[y * w + x] = bodyColor;
                }
            }

            for (int i = 12; i < 24; i++)
            {
                pixels[(h / 2) * w + i] = detailColor;
            }

            for (int y = 0; y < 4; y++)
            {
                for (int x = 8; x < 14; x++)
                {
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.1f);
                }
            }

            for (int y = 0; y < 4; y++)
            {
                for (int x = w - 14; x < w - 8; x++)
                {
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateHelicopterSprite()
        {
            const int w = 64;
            const int h = 32;
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

        private static Sprite CreateDebrisSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color metalColor = new Color(0.4f, 0.42f, 0.4f);

            for (int i = 0; i < 20; i++)
            {
                int x = Random.Range(2, size - 2);
                int y = Random.Range(2, size - 2);
                pixels[y * size + x] = metalColor;
                if (x + 1 < size)
                {
                    pixels[y * size + x + 1] = metalColor;
                }

                if (y + 1 < size)
                {
                    pixels[(y + 1) * size + x] = metalColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateCheckpointPostSprite()
        {
            const int w = 10;
            const int h = 28;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color pole = new Color(0.35f, 0.35f, 0.38f);
            Color lamp = new Color(0.9f, 0.85f, 0.65f);

            for (int y = 0; y < h - 6; y++)
            {
                for (int x = 4; x < 6; x++)
                {
                    pixels[y * w + x] = pole;
                }
            }

            for (int y = h - 6; y < h - 2; y++)
            {
                for (int x = 2; x < 8; x++)
                {
                    pixels[y * w + x] = lamp;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 16f);
        }

        private static Sprite CreateCanopySprite()
        {
            const int w = 48;
            const int h = 24;
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
                for (int x = 4; x < 8; x++)
                {
                    pixels[y * w + x] = new Color(0.3f, 0.3f, 0.3f);
                }

                for (int x = w - 8; x < w - 4; x++)
                {
                    pixels[y * w + x] = new Color(0.3f, 0.3f, 0.3f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateNeonSignSprite()
        {
            const int w = 32;
            const int h = 16;
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

        private static Sprite CreateFuelPumpSprite()
        {
            const int w = 12;
            const int h = 24;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 2; x < w - 2; x++)
                {
                    pixels[y * w + x] = new Color(0.8f, 0.2f, 0.2f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateStreetlightSprite()
        {
            const int w = 8;
            const int h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color poleColor = new Color(0.3f, 0.3f, 0.35f);
            Color lampColor = new Color(0.4f, 0.4f, 0.3f);

            for (int y = 0; y < h - 4; y++)
            {
                pixels[y * w + 3] = poleColor;
                pixels[y * w + 4] = poleColor;
            }

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
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 16f);
        }

        private static Sprite CreateGlowSprite(Color glowColor)
        {
            const int size = 32;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    pixels[y * size + x] = dist < 1f
                        ? new Color(glowColor.r, glowColor.g, glowColor.b, (1f - dist) * glowColor.a)
                        : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
