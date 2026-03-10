using Deadlight.Visuals;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public static class IndustrialLandmarks
    {
        public static void Create(Transform parent)
        {
            var root = new GameObject("IndustrialLandmarks").transform;
            root.SetParent(parent);

            CreateCrashSite(root, IndustrialLayout.CrashSitePosition);
            CreateResearchLab(root, IndustrialLayout.ResearchLabPosition);
            CreateFuelDepot(root, IndustrialLayout.FuelDepotPosition);
            CreateLoadingDock(root, IndustrialLayout.LoadingDockPosition);
            CreateControlOffice(root, IndustrialLayout.ControlOfficePosition);
            CreateCraneYard(root, IndustrialLayout.CraneYardPosition);
            CreateStreetlights(root, IndustrialLayout.StreetlightPositions);
        }

        private static void CreateCrashSite(Transform parent, Vector3 position)
        {
            var crash = new GameObject("CrashSite").transform;
            crash.SetParent(parent);
            crash.position = position;

            var body = CreateSpriteObject(crash, "HelicopterBody", CreateHelicopterSprite(), Vector3.zero, 6);
            body.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
            var bodyCollider = body.AddComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(3.1f, 1.3f);

            var fire = new GameObject("Fire");
            fire.transform.SetParent(crash);
            fire.transform.localPosition = new Vector3(-0.9f, 0.3f, 0f);
            fire.AddComponent<Deadlight.Level.FireEffect>();

            var smoke = new GameObject("Smoke");
            smoke.transform.SetParent(crash);
            smoke.transform.localPosition = new Vector3(0.6f, 0.8f, 0f);
            smoke.AddComponent<Deadlight.Level.SmokeEffect>();

            var debrisA = CreateSpriteObject(crash, "Debris_0", CreateDebrisSprite(), new Vector3(-2f, -0.7f, 0f), 5);
            debrisA.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            var debrisB = CreateSpriteObject(crash, "Debris_1", CreateDebrisSprite(), new Vector3(1.8f, -0.1f, 0f), 5);
            debrisB.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            var crate = CreateSpriteObject(crash, "SupplyCrate", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(2.4f, -0.8f, 0f), 5);
            crate.GetComponent<SpriteRenderer>().color = new Color(0.38f, 0.48f, 0.3f);
        }

        private static void CreateResearchLab(Transform parent, Vector3 position)
        {
            var lab = new GameObject("ResearchLabEntrance").transform;
            lab.SetParent(parent);
            lab.position = position;

            var building = CreateSpriteObject(lab, "LabBuilding", CreateLabBuildingSprite(), Vector3.zero, 6);
            var collider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyBaseFootprint(collider, new Vector2(5.2f, 2.7f), 0.9f, 0.42f, 0.04f, 0.75f);

            var glow = CreateSpriteObject(lab, "EerieGlow", CreateGlowSprite(new Color(0.28f, 0.88f, 0.4f, 0.28f)), new Vector3(0f, -0.2f, 0f), 4);
            glow.transform.localScale = Vector3.one * 2.4f;
            glow.AddComponent<Deadlight.Level.GlowPulse>();

            CreateSpriteObject(lab, "HazardSign", CreateHazardSignSprite(), new Vector3(3f, 0.35f, 0f), 7);
            CreateSpriteObject(lab, "HazardSign", CreateHazardSignSprite(), new Vector3(-3f, 0.35f, 0f), 7);
        }

        private static void CreateFuelDepot(Transform parent, Vector3 position)
        {
            var depot = new GameObject("FuelDepot").transform;
            depot.SetParent(parent);
            depot.position = position;

            var tankA = CreateSpriteObject(depot, "FuelTank", CreateFuelTankSprite(), new Vector3(-1.3f, 0.3f, 0f), 5);
            var tankACol = tankA.AddComponent<BoxCollider2D>();
            tankACol.size = new Vector2(2.2f, 1.3f);

            var tankB = CreateSpriteObject(depot, "FuelTank", CreateFuelTankSprite(), new Vector3(1.4f, 0.3f, 0f), 5);
            var tankBCol = tankB.AddComponent<BoxCollider2D>();
            tankBCol.size = new Vector2(2.2f, 1.3f);

            var pump = CreateSpriteObject(depot, "FuelPump", CreateFuelPumpSprite(), new Vector3(-2.6f, -1f, 0f), 6);
            var pumpCol = pump.AddComponent<BoxCollider2D>();
            pumpCol.size = new Vector2(0.5f, 0.9f);

            CreateSpriteObject(depot, "HazardSign", CreateHazardSignSprite(), new Vector3(2.9f, -0.2f, 0f), 7);
        }

        private static void CreateLoadingDock(Transform parent, Vector3 position)
        {
            var dock = new GameObject("LoadingDock").transform;
            dock.SetParent(parent);
            dock.position = position;

            var platform = CreateSpriteObject(dock, "Platform", CreatePlatformSprite(), Vector3.zero, 5);
            var collider = platform.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(4.8f, 1.7f);

            CreateSpriteObject(dock, "DockCrate", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(-1.6f, -0.8f, 0f), 6);
            CreateSpriteObject(dock, "DockCrate", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(0f, -0.8f, 0f), 6);
            CreateSpriteObject(dock, "DockCrate", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(1.6f, -0.8f, 0f), 6);
        }

        private static void CreateControlOffice(Transform parent, Vector3 position)
        {
            var office = new GameObject("ControlOffice").transform;
            office.SetParent(parent);
            office.position = position;

            var building = CreateSpriteObject(office, "OfficeBuilding", CreateOfficeBuildingSprite(), Vector3.zero, 6);
            var collider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyBaseFootprint(collider, new Vector2(3.6f, 2.7f), 0.9f, 0.42f, 0.04f, 0.7f);

            var camera = CreateSpriteObject(office, "SecurityCamera", CreateCameraSprite(), new Vector3(1.4f, 0.9f, 0f), 7);
            camera.AddComponent<Deadlight.Level.SearchlightEffect>();
        }

        private static void CreateCraneYard(Transform parent, Vector3 position)
        {
            var yard = new GameObject("CraneYard").transform;
            yard.SetParent(parent);
            yard.position = position;

            var crane = CreateSpriteObject(yard, "Crane", CreateCraneSprite(), new Vector3(0f, 0.3f, 0f), 7);
            var craneCollider = crane.AddComponent<BoxCollider2D>();
            craneCollider.size = new Vector2(2.2f, 3.4f);

            CreateSpriteObject(yard, "Container", CreateContainerSprite(), new Vector3(-1.9f, -1.1f, 0f), 5);
            CreateSpriteObject(yard, "Container", CreateContainerSprite(), new Vector3(1.9f, -1.1f, 0f), 5);
            CreateSpriteObject(yard, "Container", CreateContainerSprite(), new Vector3(0f, -0.4f, 0f), 6);
        }

        private static void CreateStreetlights(Transform parent, Vector3[] positions)
        {
            var lights = new GameObject("Streetlights").transform;
            lights.SetParent(parent);

            foreach (Vector3 pos in positions)
            {
                var light = new GameObject("Streetlight").transform;
                light.SetParent(lights);
                light.position = pos;

                var pole = CreateSpriteObject(light, "Pole", CreateStreetlightSprite(), Vector3.zero, Mathf.RoundToInt(-pos.y) + 8);
                var poleCollider = pole.AddComponent<CircleCollider2D>();
                poleCollider.radius = 0.15f;

                var glow = CreateSpriteObject(light, "LightGlow", CreateGlowSprite(new Color(1f, 0.92f, 0.68f, 0.24f)), new Vector3(0f, 1.1f, 0f), -100);
                glow.transform.localScale = Vector3.one * 2.4f;
                if (Random.value < 0.25f)
                {
                    glow.AddComponent<Deadlight.Level.FlickeringLight>();
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
            sr.sortingOrder = sortingOrder;
            return go;
        }

        private static Sprite CreateHelicopterSprite()
        {
            const int w = 54;
            const int h = 24;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color body = new Color(0.36f, 0.38f, 0.4f);
            Color rotor = new Color(0.14f, 0.14f, 0.16f);

            FillRect(px, w, 10, 7, 24, 10, body);
            FillRect(px, w, 2, 10, 10, 4, body);
            FillRect(px, w, 34, 10, 16, 3, body);
            FillRect(px, w, 16, 18, 20, 2, rotor);
            FillRect(px, w, 0, 20, 54, 2, rotor);
            return CreateSprite(tex, px);
        }

        private static Sprite CreateDebrisSprite()
        {
            const int w = 16;
            const int h = 10;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 12, 5, new Color(0.28f, 0.3f, 0.32f));
            FillRect(px, w, 4, 4, 5, 2, new Color(0.44f, 0.24f, 0.16f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateLabBuildingSprite()
        {
            const int w = 60;
            const int h = 32;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color wall = new Color(0.78f, 0.81f, 0.86f);
            Color trim = new Color(0.24f, 0.46f, 0.36f);
            Color window = new Color(0.62f, 0.88f, 0.96f);

            FillRect(px, w, 4, 4, 52, 18, wall);
            FillRect(px, w, 0, 22, 60, 4, trim);
            FillRect(px, w, 12, 10, 10, 6, window);
            FillRect(px, w, 24, 10, 10, 6, window);
            FillRect(px, w, 38, 10, 10, 6, window);
            FillRect(px, w, 25, 4, 10, 10, new Color(0.18f, 0.4f, 0.28f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateOfficeBuildingSprite()
        {
            const int w = 42;
            const int h = 34;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color wall = new Color(0.68f, 0.7f, 0.76f);
            Color window = new Color(0.68f, 0.88f, 0.96f);

            FillRect(px, w, 3, 3, 36, 24, wall);
            FillRect(px, w, 0, 27, 42, 4, new Color(0.32f, 0.2f, 0.18f));
            for (int y = 8; y <= 18; y += 6)
            {
                for (int x = 7; x <= 27; x += 10)
                {
                    FillRect(px, w, x, y, 5, 4, window);
                }
            }

            return CreateSprite(tex, px);
        }

        private static Sprite CreateCameraSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            var px = new Color[size * size];
            FillRect(px, size, 4, 5, 8, 6, new Color(0.3f, 0.32f, 0.36f));
            FillRect(px, size, 10, 7, 3, 2, new Color(0.12f, 0.12f, 0.14f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateCraneSprite()
        {
            const int w = 36;
            const int h = 50;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 14, 2, 6, 36, new Color(0.62f, 0.64f, 0.7f));
            FillRect(px, w, 8, 34, 22, 4, new Color(0.48f, 0.5f, 0.56f));
            FillRect(px, w, 22, 30, 6, 4, new Color(0.38f, 0.4f, 0.44f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateContainerSprite()
        {
            const int w = 24;
            const int h = 16;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 20, 12, new Color(0.24f, 0.46f, 0.84f));
            FillRect(px, w, 4, 5, 16, 2, new Color(0.92f, 0.92f, 0.94f));
            FillRect(px, w, 4, 9, 16, 2, new Color(0.92f, 0.92f, 0.94f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateFuelTankSprite()
        {
            const int w = 30;
            const int h = 22;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 3, 4, 24, 12, new Color(0.58f, 0.6f, 0.66f));
            FillRect(px, w, 7, 10, 16, 2, new Color(0.76f, 0.78f, 0.82f));
            FillRect(px, w, 6, 16, 18, 2, new Color(0.9f, 0.74f, 0.16f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateFuelPumpSprite()
        {
            const int w = 12;
            const int h = 18;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 8, 12, new Color(0.8f, 0.14f, 0.14f));
            FillRect(px, w, 3, 10, 6, 3, new Color(0.74f, 0.9f, 1f));
            FillRect(px, w, 8, 6, 2, 6, new Color(0.16f, 0.16f, 0.2f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateHazardSignSprite()
        {
            const int w = 16;
            const int h = 18;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 3, 3, 10, 10, new Color(0.98f, 0.84f, 0.12f));
            FillRect(px, w, 7, 0, 2, 3, new Color(0.46f, 0.48f, 0.52f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreatePlatformSprite()
        {
            const int w = 44;
            const int h = 16;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 3, 40, 9, new Color(0.64f, 0.67f, 0.72f));
            FillRect(px, w, 2, 12, 40, 2, new Color(0.34f, 0.36f, 0.4f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateStreetlightSprite()
        {
            const int w = 12;
            const int h = 32;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 5, 0, 2, 24, new Color(0.46f, 0.48f, 0.54f));
            FillRect(px, w, 5, 23, 5, 2, new Color(0.46f, 0.48f, 0.54f));
            FillRect(px, w, 8, 22, 2, 2, new Color(0.96f, 0.92f, 0.68f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateGlowSprite(Color color)
        {
            const int size = 32;
            var tex = new Texture2D(size, size);
            var px = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - distance / radius) * color.a;
                    px[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            return CreateSprite(tex, px);
        }

        private static void FillRect(Color[] pixels, int width, int x, int y, int rectWidth, int rectHeight, Color color)
        {
            for (int py = y; py < y + rectHeight; py++)
            {
                for (int px = x; px < x + rectWidth; px++)
                {
                    pixels[py * width + px] = color;
                }
            }
        }

        private static Sprite CreateSprite(Texture2D tex, Color[] pixels)
        {
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.2f), 16f);
        }
    }
}
