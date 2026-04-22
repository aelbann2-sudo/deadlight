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
            var crateCollider = crate.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(crateCollider, crate.GetComponent<SpriteRenderer>().sprite, Vector3.one, 1f, 1f);
        }

        private static void CreateMilitaryCheckpoint(Transform parent, Vector3 position)
        {
            var checkpoint = new GameObject("MilitaryCheckpoint").transform;
            checkpoint.SetParent(parent);
            checkpoint.position = position;

            var booth = CreateSpriteObject(checkpoint, "GuardBooth", ProceduralSpriteGenerator.CreateGarageSprite(0), new Vector3(-2.3f, 0.3f, 0f), 5);
            booth.transform.localScale = new Vector3(0.95f, 0.9f, 1f);
            var boothCollider = booth.AddComponent<BoxCollider2D>();
            // Collider sits on the scaled booth transform — pass Vector3.one so the helper
            // keeps sizes in sprite-local units. Unity then multiplies by booth.localScale
            // exactly once, matching the visible sprite. Passing localScale here would
            // double-scale the collider and produce ~10% phantom padding around the booth.
            MapFootprintCollider.ApplySpriteFootprint(boothCollider, booth.GetComponent<SpriteRenderer>().sprite, Vector3.one, 0.9f, 0.9f);

            var searchlight = new GameObject("Searchlight");
            searchlight.transform.SetParent(checkpoint);
            searchlight.transform.localPosition = new Vector3(1.8f, 0.7f, 0f);
            searchlight.AddComponent<SearchlightEffect>();

            var ammo = CreateSpriteObject(checkpoint, "AmmoCase", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(-2f, -1f, 0f), 4);
            ammo.GetComponent<SpriteRenderer>().color = new Color(0.38f, 0.48f, 0.3f);
            var ammoCollider = ammo.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(ammoCollider, ammo.GetComponent<SpriteRenderer>().sprite, Vector3.one, 1f, 1f);

            var barrier = CreateSpriteObject(checkpoint, "Barrier", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(0.2f, -0.7f, 0f), 4);
            barrier.transform.localScale = new Vector3(1.25f, 0.45f, 1f);
            // Barriers are solid physical objects (knee-high concrete blocks) — the player
            // should bump into them, not walk through. Use Vector3.one so the collider stays
            // in sprite-local units and Unity scales it with the flattened barrier transform,
            // matching the visible body exactly.
            var barrierCollider = barrier.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(barrierCollider, barrier.GetComponent<SpriteRenderer>().sprite, Vector3.one, 1f, 1f);

            CreateSpriteObject(checkpoint, "CheckpointPost", CreateCheckpointPostSprite(), new Vector3(2.4f, -0.2f, 0f), 4);
        }

        private static void CreateGasStation(Transform parent, Vector3 position)
        {
            var station = new GameObject("GasStation").transform;
            station.SetParent(parent);
            station.position = position;

            var canopy = CreateSpriteObject(station, "Canopy", CreateCanopySprite(), new Vector3(-0.4f, 0.7f, 0f), 6);
            canopy.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var canopyCollider = canopy.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCenteredFootprint(canopyCollider, new Vector2(3f, 1.5f), 0.82f, 0.22f, 0.02f, 0.28f);

            var sign = CreateSpriteObject(station, "PricePylon", CreateNeonSignSprite(), new Vector3(-3.2f, 1.1f, 0f), 7);
            sign.AddComponent<FlickeringLight>();

            // Two pumps under the canopy, each with a distinct fuel-grade color
            // so the player can tell them apart. Each pump gets its own collider
            // so they block movement instead of being walked through.
            var pumpRegular = CreateSpriteObject(station, "FuelPump_Regular", ProceduralSpriteGenerator.CreateFuelPumpSprite(0), new Vector3(-1.3f, -0.8f, 0f), 5);
            var pumpRegularCollider = pumpRegular.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(pumpRegularCollider, pumpRegular.GetComponent<SpriteRenderer>().sprite, Vector3.one, 1f, 1f);

            var pumpPremium = CreateSpriteObject(station, "FuelPump_Premium", ProceduralSpriteGenerator.CreateFuelPumpSprite(2), new Vector3(-0.1f, -0.8f, 0f), 5);
            var pumpPremiumCollider = pumpPremium.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(pumpPremiumCollider, pumpPremium.GetComponent<SpriteRenderer>().sprite, Vector3.one, 1f, 1f);

            CreateSpriteObject(station, "ParkedCar", ProceduralSpriteGenerator.CreateCarSprite(1), new Vector3(2.1f, 0.9f, 0f), 5);
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
            var busCollider = bus.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(busCollider, bus.GetComponent<SpriteRenderer>().sprite, bus.transform.localScale, 0.95f, 0.9f);

            var flag = CreateSpriteObject(school, "FlagPole", CreateSchoolFlagPoleSprite(), new Vector3(3.2f, -1.3f, 0f), 6);
            var flagCollider = flag.AddComponent<BoxCollider2D>();
            flagCollider.size = new Vector2(0.18f, 0.35f);
            flagCollider.offset = new Vector2(0f, 0.12f);
        }

        private static void CreateHospital(Transform parent, Vector3 position)
        {
            var hospital = new GameObject("Hospital").transform;
            hospital.SetParent(parent);
            hospital.position = position;

            var building = CreateSpriteObject(hospital, "Building", CreateHospitalSprite(), Vector3.zero, 5);
            var buildingCollider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(buildingCollider, building.GetComponent<SpriteRenderer>().sprite, building.transform.localScale, 0.92f, 0.9f);

            var ambulance = CreateSpriteObject(hospital, "Ambulance", CreateAmbulanceSprite(), new Vector3(-2.2f, -1.1f, 0f), 4);
            var ambulanceCollider = ambulance.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(ambulanceCollider, ambulance.GetComponent<SpriteRenderer>().sprite, ambulance.transform.localScale, 0.95f, 0.9f);

            var flag = CreateSpriteObject(hospital, "FlagPole", CreateHospitalFlagPoleSprite(), new Vector3(3.2f, -1.3f, 0f), 6);
            var flagCollider = flag.AddComponent<BoxCollider2D>();
            flagCollider.size = new Vector2(0.18f, 0.35f);
            flagCollider.offset = new Vector2(0f, 0.12f);
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

        private static Sprite CreateSchoolFlagPoleSprite()
        {
            const int w = 14;
            const int h = 40;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            Color poleBase = new Color(0.62f, 0.64f, 0.68f);
            Color poleHighlight = new Color(0.82f, 0.84f, 0.88f);
            Color poleShadow = new Color(0.34f, 0.36f, 0.4f);
            Color baseColor = new Color(0.22f, 0.22f, 0.26f);
            Color finial = new Color(0.95f, 0.82f, 0.22f);
            Color flagYellow = new Color(0.98f, 0.84f, 0.22f);
            Color flagBlue = new Color(0.15f, 0.32f, 0.68f);
            Color flagShadow = new Color(0.72f, 0.6f, 0.14f);
            Color flagOutline = new Color(0.1f, 0.2f, 0.44f);

            FillRectPx(pixels, w, 5, 2, 4, 2, baseColor);
            FillRectPx(pixels, w, 4, 0, 6, 2, baseColor);

            FillRectPx(pixels, w, 6, 4, 2, h - 8, poleBase);
            FillRectPx(pixels, w, 6, 4, 1, h - 8, poleShadow);
            FillRectPx(pixels, w, 7, 4, 1, h - 8, poleHighlight);

            FillRectPx(pixels, w, 6, h - 4, 2, 2, finial);
            pixels[(h - 3) * w + 6] = finial;
            pixels[(h - 3) * w + 7] = finial;

            int flagBottom = h - 16;
            int flagTop = h - 6;
            int flagLeft = 8;
            int flagRight = w;
            FillRectPx(pixels, w, flagLeft, flagBottom, flagRight - flagLeft, flagTop - flagBottom, flagYellow);
            FillRectPx(pixels, w, flagLeft, flagBottom, flagRight - flagLeft, 1, flagShadow);
            FillRectPx(pixels, w, flagRight - 1, flagBottom, 1, flagTop - flagBottom, flagShadow);

            int stripeY = (flagBottom + flagTop) / 2;
            FillRectPx(pixels, w, flagLeft + 1, stripeY - 1, flagRight - flagLeft - 2, 1, flagBlue);
            FillRectPx(pixels, w, flagLeft + 1, stripeY, flagRight - flagLeft - 2, 1, flagOutline);

            int letterX = flagLeft + 2;
            int letterY = flagTop - 4;
            pixels[(letterY) * w + letterX] = flagBlue;
            pixels[(letterY) * w + letterX + 1] = flagBlue;
            pixels[(letterY - 1) * w + letterX] = flagBlue;
            pixels[(letterY - 2) * w + letterX] = flagBlue;
            pixels[(letterY - 2) * w + letterX + 1] = flagBlue;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 16f);
        }

        private static Sprite CreateHospitalFlagPoleSprite()
        {
            const int w = 14;
            const int h = 40;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            Color poleBase = new Color(0.62f, 0.64f, 0.68f);
            Color poleHighlight = new Color(0.82f, 0.84f, 0.88f);
            Color poleShadow = new Color(0.34f, 0.36f, 0.4f);
            Color baseColor = new Color(0.22f, 0.22f, 0.26f);
            Color finial = new Color(0.95f, 0.82f, 0.22f);
            Color flagWhite = new Color(0.96f, 0.96f, 0.96f);
            Color flagCross = new Color(0.86f, 0.14f, 0.14f);
            Color flagShadow = new Color(0.76f, 0.76f, 0.78f);

            FillRectPx(pixels, w, 5, 2, 4, 2, baseColor);
            FillRectPx(pixels, w, 4, 0, 6, 2, baseColor);

            FillRectPx(pixels, w, 6, 4, 2, h - 8, poleBase);
            FillRectPx(pixels, w, 6, 4, 1, h - 8, poleShadow);
            FillRectPx(pixels, w, 7, 4, 1, h - 8, poleHighlight);

            FillRectPx(pixels, w, 6, h - 4, 2, 2, finial);
            pixels[(h - 3) * w + 6] = finial;
            pixels[(h - 3) * w + 7] = finial;

            int flagBottom = h - 16;
            int flagTop = h - 6;
            int flagLeft = 8;
            int flagRight = w;
            FillRectPx(pixels, w, flagLeft, flagBottom, flagRight - flagLeft, flagTop - flagBottom, flagWhite);
            FillRectPx(pixels, w, flagLeft, flagBottom, flagRight - flagLeft, 1, flagShadow);
            FillRectPx(pixels, w, flagRight - 1, flagBottom, 1, flagTop - flagBottom, flagShadow);

            int crossCenterY = (flagBottom + flagTop) / 2;
            int crossCenterX = (flagLeft + flagRight) / 2;
            FillRectPx(pixels, w, flagLeft + 1, crossCenterY - 1, flagRight - flagLeft - 2, 2, flagCross);
            FillRectPx(pixels, w, crossCenterX - 1, flagBottom + 1, 2, flagTop - flagBottom - 2, flagCross);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 16f);
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

            Color roofRed = new Color(0.58f, 0.15f, 0.12f);
            Color roofLight = new Color(0.72f, 0.22f, 0.18f);
            Color roofDark = new Color(0.34f, 0.08f, 0.06f);
            Color beam = new Color(0.22f, 0.05f, 0.04f);
            Color support = new Color(0.22f, 0.22f, 0.26f);
            Color supportHighlight = new Color(0.42f, 0.42f, 0.48f);
            Color supportShadow = new Color(0.1f, 0.1f, 0.12f);
            Color light = new Color(1f, 0.92f, 0.55f);
            Color lightGlow = new Color(1f, 0.82f, 0.4f);
            Color brandBand = new Color(0.94f, 0.9f, 0.82f);

            FillRectPx(pixels, w, 0, 0, w, h, roofRed);

            FillRectPx(pixels, w, 2, h - 2, w - 4, 1, roofLight);
            FillRectPx(pixels, w, 2, 1, w - 4, 1, roofDark);

            FillRectPx(pixels, w, 0, 0, w, 1, roofDark);
            FillRectPx(pixels, w, 0, h - 1, w, 1, roofDark);
            FillRectPx(pixels, w, 0, 0, 1, h, roofDark);
            FillRectPx(pixels, w, w - 1, 0, 1, h, roofDark);

            int midX = w / 2;
            int midY = h / 2;
            FillRectPx(pixels, w, midX - 1, 2, 2, h - 4, beam);
            FillRectPx(pixels, w, 2, midY - 1, w - 4, 2, beam);

            FillRectPx(pixels, w, 3, midY + 1, midX - 4, midY - 4, roofLight);
            FillRectPx(pixels, w, midX + 1, midY + 1, midX - 4, midY - 4, roofLight);
            FillRectPx(pixels, w, 3, 3, midX - 4, midY - 4, roofLight);
            FillRectPx(pixels, w, midX + 1, 3, midX - 4, midY - 4, roofLight);

            FillRectPx(pixels, w, 4, midY - 1, w - 8, 2, brandBand);
            FillRectPx(pixels, w, 4, midY, w - 8, 1, Color.Lerp(brandBand, Color.black, 0.2f));

            FillRectPx(pixels, w, 2, 2, 4, 4, support);
            FillRectPx(pixels, w, w - 6, 2, 4, 4, support);
            FillRectPx(pixels, w, 2, h - 6, 4, 4, support);
            FillRectPx(pixels, w, w - 6, h - 6, 4, 4, support);
            FillRectPx(pixels, w, 2, 5, 4, 1, supportHighlight);
            FillRectPx(pixels, w, w - 6, 5, 4, 1, supportHighlight);
            FillRectPx(pixels, w, 2, h - 3, 4, 1, supportHighlight);
            FillRectPx(pixels, w, w - 6, h - 3, 4, 1, supportHighlight);
            FillRectPx(pixels, w, 2, 2, 4, 1, supportShadow);
            FillRectPx(pixels, w, w - 6, 2, 4, 1, supportShadow);

            int[] lightRows = { midY - 3, midY + 2 };
            int[] lightCols = { 8, w - 9 };
            foreach (int lr in lightRows)
            {
                foreach (int lc in lightCols)
                {
                    pixels[lr * w + lc] = light;
                    pixels[lr * w + lc - 1] = lightGlow;
                    pixels[lr * w + lc + 1] = lightGlow;
                    pixels[(lr - 1) * w + lc] = lightGlow;
                    pixels[(lr + 1) * w + lc] = lightGlow;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateNeonSignSprite()
        {
            const int w = 20;
            const int h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            Color pole = new Color(0.32f, 0.32f, 0.36f);
            Color poleShadow = new Color(0.18f, 0.18f, 0.22f);
            Color frame = new Color(0.14f, 0.14f, 0.18f);
            Color frameTrim = new Color(0.36f, 0.36f, 0.42f);
            Color brandRed = new Color(0.84f, 0.18f, 0.18f);
            Color brandRedDark = new Color(0.54f, 0.1f, 0.08f);
            Color priceBg = new Color(0.05f, 0.05f, 0.08f);
            Color lcdGreen = new Color(0.35f, 0.96f, 0.45f);
            Color lcdAmber = new Color(0.98f, 0.78f, 0.25f);
            Color glow = new Color(1f, 0.45f, 0.4f);

            FillRectPx(pixels, w, 8, 0, 4, 10, pole);
            FillRectPx(pixels, w, 8, 0, 1, 10, poleShadow);
            FillRectPx(pixels, w, 6, 1, 8, 1, poleShadow);

            FillRectPx(pixels, w, 1, 10, w - 2, 22, frame);
            FillRectPx(pixels, w, 2, 11, w - 4, 20, frameTrim);
            FillRectPx(pixels, w, 3, 12, w - 6, 18, frame);

            FillRectPx(pixels, w, 3, 24, w - 6, 6, brandRed);
            FillRectPx(pixels, w, 3, 24, w - 6, 1, brandRedDark);
            FillRectPx(pixels, w, 3, 29, w - 6, 1, brandRedDark);

            pixels[26 * w + 5] = glow;
            pixels[26 * w + 7] = glow;
            pixels[27 * w + 5] = glow;
            pixels[27 * w + 7] = glow;
            pixels[26 * w + 9] = glow;
            pixels[26 * w + 10] = glow;
            pixels[26 * w + 11] = glow;
            pixels[27 * w + 9] = glow;
            pixels[27 * w + 11] = glow;
            pixels[26 * w + 13] = glow;
            pixels[26 * w + 14] = glow;
            pixels[27 * w + 13] = glow;
            pixels[27 * w + 14] = glow;
            pixels[26 * w + 15] = glow;

            FillRectPx(pixels, w, 3, 20, w - 6, 3, priceBg);
            FillRectPx(pixels, w, 4, 21, 2, 1, lcdGreen);
            FillRectPx(pixels, w, 7, 21, 1, 1, lcdGreen);
            FillRectPx(pixels, w, 9, 21, 1, 1, lcdGreen);
            FillRectPx(pixels, w, 11, 21, 2, 1, lcdGreen);
            FillRectPx(pixels, w, 14, 21, 1, 1, lcdGreen);
            FillRectPx(pixels, w, 16, 21, 1, 1, lcdGreen);

            FillRectPx(pixels, w, 3, 15, w - 6, 3, priceBg);
            FillRectPx(pixels, w, 4, 16, 2, 1, lcdAmber);
            FillRectPx(pixels, w, 7, 16, 1, 1, lcdAmber);
            FillRectPx(pixels, w, 9, 16, 1, 1, lcdAmber);
            FillRectPx(pixels, w, 11, 16, 2, 1, lcdAmber);
            FillRectPx(pixels, w, 14, 16, 1, 1, lcdAmber);
            FillRectPx(pixels, w, 16, 16, 1, 1, lcdAmber);

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

        private static void FillRectPx(Color[] pixels, int textureWidth, int x, int y, int rectWidth, int rectHeight, Color color)
        {
            int endX = Mathf.Min(x + rectWidth, textureWidth);
            int endY = Mathf.Min(y + rectHeight, pixels.Length / textureWidth);
            for (int py = Mathf.Max(0, y); py < endY; py++)
            {
                int rowStart = py * textureWidth;
                for (int px = Mathf.Max(0, x); px < endX; px++)
                {
                    pixels[rowStart + px] = color;
                }
            }
        }
    }
}
