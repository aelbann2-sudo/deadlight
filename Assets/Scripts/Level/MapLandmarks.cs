using UnityEngine;
using Deadlight.Core;
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

        public void CreateAllLandmarks(Transform parent, MapType mapType)
        {
            switch (mapType)
            {
                case MapType.TownCenter:
                    CreateTownCenterLandmarks(parent);
                    break;

                case MapType.Industrial:
                    CreateIndustrialLandmarks(parent);
                    break;

                case MapType.Suburban:
                    CreateSuburbanLandmarks(parent);
                    break;
            }
        }

        // Town Center Landmarks
        private void CreateTownCenterLandmarks(Transform parent)
        {
            CreateCrashedHelicopter(parent, new Vector3(0, 30, 0));
            CreateMilitaryCheckpoint(parent, new Vector3(-25, 0, 0));
            CreateGasStation(parent, new Vector3(25, -20, 0));
            CreateTownPlaza(parent, new Vector3(0, 0, 0));
            CreateDiner(parent, new Vector3(-20, 25, 0));
            CreatePoliceKiosk(parent, new Vector3(20, 25, 0));
            
            CreateStreetlights(parent, new Vector3[] {
                new Vector3(-15, 15, 0), new Vector3(15, 15, 0),
                new Vector3(-15, -15, 0), new Vector3(15, -15, 0),
                new Vector3(-30, 0, 0), new Vector3(30, 0, 0),
                new Vector3(0, 30, 0), new Vector3(0, -30, 0),
                new Vector3(-15, 30, 0), new Vector3(15, -30, 0),
                new Vector3(-25, 15, 0), new Vector3(25, -15, 0),
            });
        }

        // Industrial District Landmarks
        private void CreateIndustrialLandmarks(Transform parent)
        {
            CreateCrashedHelicopter(parent, new Vector3(0, 35, 0));
            CreateResearchLabEntrance(parent, new Vector3(0, -35, 0));
            CreateFuelDepot(parent, new Vector3(20, 20, 0));
            CreateLoadingDock(parent, new Vector3(-15, -30, 0));
            CreateControlOffice(parent, new Vector3(-25, 25, 0));
            CreateCraneYard(parent, new Vector3(25, -25, 0));
            
            CreateStreetlights(parent, new Vector3[] {
                new Vector3(-20, 0, 0), new Vector3(20, 0, 0),
                new Vector3(-20, 15, 0), new Vector3(20, 15, 0),
                new Vector3(-20, -15, 0), new Vector3(20, -15, 0),
                new Vector3(0, 30, 0), new Vector3(0, -30, 0),
                new Vector3(-15, 25, 0), new Vector3(15, -25, 0),
            });
        }

        // Suburban Area Landmarks
        private void CreateSuburbanLandmarks(Transform parent)
        {
            CreateMilitaryCheckpoint(parent, new Vector3(0, -25, 0));
            CreateGasStation(parent, new Vector3(30, 0, 0));
            CreatePlayground(parent, new Vector3(-15, 20, 0));
            CreateAbandonedBus(parent, new Vector3(20, -25, 0));
            CreateSchoolBus(parent, new Vector3(-25, -15, 0));
            CreateCulDeSacCenter(parent, new Vector3(0, 25, 0));
            
            CreateStreetlights(parent, new Vector3[] {
                new Vector3(Mathf.Sin(-15 * 0.15f) * 6f, -15, 0),
                new Vector3(Mathf.Sin(-5 * 0.15f) * 6f, -5, 0),
                new Vector3(Mathf.Sin(5 * 0.15f) * 6f, 5, 0),
                new Vector3(Mathf.Sin(15 * 0.15f) * 6f, 15, 0),
                new Vector3(-25, 0, 0), new Vector3(25, 0, 0),
                new Vector3(-20, 25, 0), new Vector3(20, -25, 0),
            });
        }

        // ===================== LANDMARK CREATION METHODS =====================

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

        // ===================== NEW TOWN CENTER LANDMARKS =====================

        private void CreateDiner(Transform parent, Vector3 position)
        {
            var diner = new GameObject("Diner");
            diner.transform.SetParent(parent);
            diner.transform.position = position;

            // Diner building
            var building = new GameObject("Building");
            building.transform.SetParent(diner.transform);
            building.transform.localPosition = Vector3.zero;
            var sr = building.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDinerSprite();
            sr.sortingOrder = 5;
            var col = building.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 2f);

            // Neon sign
            var sign = new GameObject("NeonSign");
            sign.transform.SetParent(diner.transform);
            sign.transform.localPosition = new Vector3(0, 1.2f, 0);
            var signSr = sign.AddComponent<SpriteRenderer>();
            signSr.sprite = CreateDinerSignSprite();
            signSr.sortingOrder = 6;
            sign.AddComponent<FlickeringLight>();
        }

        private void CreatePoliceKiosk(Transform parent, Vector3 position)
        {
            var kiosk = new GameObject("PoliceKiosk");
            kiosk.transform.SetParent(parent);
            kiosk.transform.position = position;

            // Kiosk building
            var building = new GameObject("Building");
            building.transform.SetParent(kiosk.transform);
            building.transform.localPosition = Vector3.zero;
            var sr = building.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePoliceKioskSprite();
            sr.sortingOrder = 5;
            var col = building.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 1.5f);

            // Police car
            var car = new GameObject("PoliceCar");
            car.transform.SetParent(kiosk.transform);
            car.transform.localPosition = new Vector3(-1.5f, -0.8f, 0);
            var carSr = car.AddComponent<SpriteRenderer>();
            carSr.sprite = CreatePoliceCarSprite();
            carSr.sortingOrder = 4;
            var carCol = car.AddComponent<BoxCollider2D>();
            carCol.size = new Vector2(1.2f, 0.6f);
        }

        private void CreateTownPlaza(Transform parent, Vector3 position)
        {
            var plaza = new GameObject("TownPlaza");
            plaza.transform.SetParent(parent);
            plaza.transform.position = position;

            // Fountain structure
            var fountain = new GameObject("Fountain");
            fountain.transform.SetParent(plaza.transform);
            fountain.transform.localPosition = Vector3.zero;
            var sr = fountain.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFountainSprite();
            sr.sortingOrder = 6;
            var col = fountain.AddComponent<CircleCollider2D>();
            col.radius = 0.8f;

            // Water glow
            var waterGlow = new GameObject("WaterGlow");
            waterGlow.transform.SetParent(plaza.transform);
            waterGlow.transform.localPosition = Vector3.zero;
            var wsr = waterGlow.AddComponent<SpriteRenderer>();
            wsr.sprite = CreateGlowSprite(new Color(0.3f, 0.5f, 0.9f, 0.2f));
            wsr.sortingOrder = -100;
            waterGlow.transform.localScale = Vector3.one * 4f;
            waterGlow.AddComponent<GlowPulse>();
        }

        // ===================== NEW INDUSTRIAL LANDMARKS =====================

        private void CreateControlOffice(Transform parent, Vector3 position)
        {
            var office = new GameObject("ControlOffice");
            office.transform.SetParent(parent);
            office.transform.position = position;

            // Office building
            var building = new GameObject("Building");
            building.transform.SetParent(office.transform);
            building.transform.localPosition = Vector3.zero;
            var sr = building.AddComponent<SpriteRenderer>();
            sr.sprite = CreateOfficeBuildingSprite();
            sr.sortingOrder = 5;
            var col = building.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 2.5f);

            // Security camera
            var camera = new GameObject("SecurityCamera");
            camera.transform.SetParent(office.transform);
            camera.transform.localPosition = new Vector3(1.2f, 0.8f, 0);
            var camSr = camera.AddComponent<SpriteRenderer>();
            camSr.sprite = CreateCameraSprite();
            camSr.sortingOrder = 6;
            camera.AddComponent<SearchlightEffect>();
        }

        private void CreateCraneYard(Transform parent, Vector3 position)
        {
            var yard = new GameObject("CraneYard");
            yard.transform.SetParent(parent);
            yard.transform.position = position;

            // Crane structure
            var crane = new GameObject("Crane");
            crane.transform.SetParent(yard.transform);
            crane.transform.localPosition = Vector3.zero;
            var sr = crane.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCraneSprite();
            sr.sortingOrder = 7;
            var col = crane.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 3f);

            // Containers
            Vector3[] containerPositions = {
                new Vector3(-1.5f, -1f, 0), new Vector3(1.5f, -1f, 0),
                new Vector3(0, -0.5f, 0)
            };

            foreach (var pos in containerPositions)
            {
                var container = new GameObject("Container");
                container.transform.SetParent(yard.transform);
                container.transform.localPosition = pos;
                var contSr = container.AddComponent<SpriteRenderer>();
                contSr.sprite = CreateContainerSprite();
                contSr.sortingOrder = 5;
                var contCol = container.AddComponent<BoxCollider2D>();
                contCol.size = new Vector2(1.5f, 1f);
            }
        }

        // ===================== NEW SUBURBAN LANDMARKS =====================

        private void CreateSchoolBus(Transform parent, Vector3 position)
        {
            var bus = new GameObject("SchoolBus");
            bus.transform.SetParent(parent);
            bus.transform.position = position;
            bus.transform.rotation = Quaternion.Euler(0, 0, -5f);

            var sr = bus.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSchoolBusSprite();
            sr.sortingOrder = 5;
            sr.color = new Color(1f, 0.8f, 0.2f);

            var col = bus.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 1.2f);
        }
        private void CreateCulDeSacCenter(Transform parent, Vector3 position)
        {
            var center = new GameObject("CulDeSacCenter");
            center.transform.SetParent(parent);
            center.transform.position = position;

            // Mailbox
            var mailbox = new GameObject("Mailbox");
            mailbox.transform.SetParent(center.transform);
            mailbox.transform.localPosition = new Vector3(-1f, 0, 0);
            var mailSr = mailbox.AddComponent<SpriteRenderer>();
            mailSr.sprite = CreateMailboxSprite();
            mailSr.sortingOrder = 5;
            var mailCol = mailbox.AddComponent<BoxCollider2D>();
            mailCol.size = new Vector2(0.5f, 1f);

            // Picnic table
            var table = new GameObject("PicnicTable");
            table.transform.SetParent(center.transform);
            table.transform.localPosition = new Vector3(1f, 0, 0);
            var tableSr = table.AddComponent<SpriteRenderer>();
            tableSr.sprite = CreatePicnicTableSprite();
            tableSr.sortingOrder = 4;
            var tableCol = table.AddComponent<BoxCollider2D>();
            tableCol.size = new Vector2(2f, 0.8f);

            // Swing set
            var swing = new GameObject("SwingSet");
            swing.transform.SetParent(center.transform);
            swing.transform.localPosition = new Vector3(0, 1.5f, 0);
            var swingSr = swing.AddComponent<SpriteRenderer>();
            swingSr.sprite = CreateSwingSetSprite();
            swingSr.sortingOrder = 6;
        }

        // ===================== SHARED LANDMARKS =====================

        private void CreateFuelDepot(Transform parent, Vector3 position)
        {
            var depot = new GameObject("FuelDepot");
            depot.transform.SetParent(parent);
            depot.transform.position = position;

            var tank = new GameObject("FuelTank");
            tank.transform.SetParent(depot.transform);
            tank.transform.localPosition = Vector3.zero;
            var sr = tank.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFuelTankSprite();
            sr.sortingOrder = 5;
            var col = tank.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 1.5f);

            var sign = new GameObject("HazardSign");
            sign.transform.SetParent(depot.transform);
            sign.transform.localPosition = new Vector3(1.5f, 0.5f, 0);
            var signSr = sign.AddComponent<SpriteRenderer>();
            signSr.sprite = CreateHazardSignSprite();
            signSr.sortingOrder = 6;

            var pump = new GameObject("FuelPump");
            pump.transform.SetParent(depot.transform);
            pump.transform.localPosition = new Vector3(-1.5f, -0.5f, 0);
            var pumpSr = pump.AddComponent<SpriteRenderer>();
            pumpSr.sprite = CreateFuelPumpSprite();
            pumpSr.sortingOrder = 5;
            var pumpCol = pump.AddComponent<BoxCollider2D>();
            pumpCol.size = new Vector2(0.5f, 1f);
        }

        private void CreateLoadingDock(Transform parent, Vector3 position)
        {
            var dock = new GameObject("LoadingDock");
            dock.transform.SetParent(parent);
            dock.transform.position = position;

            var platform = new GameObject("Platform");
            platform.transform.SetParent(dock.transform);
            platform.transform.localPosition = Vector3.zero;
            var sr = platform.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlatformSprite();
            sr.sortingOrder = 4;
            var col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 1.5f);

            Vector3[] cratePositions = {
                new Vector3(-1.2f, -0.8f, 0), new Vector3(0, -0.8f, 0), new Vector3(1.2f, -0.8f, 0)
            };
            foreach (var pos in cratePositions)
            {
                var crate = new GameObject("DockCrate");
                crate.transform.SetParent(dock.transform);
                crate.transform.localPosition = pos;
                var crateSr = crate.AddComponent<SpriteRenderer>();
                crateSr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
                crateSr.sortingOrder = 5;
                var crateCol = crate.AddComponent<BoxCollider2D>();
                crateCol.size = new Vector2(0.8f, 0.8f);
            }
        }

        private void CreatePlayground(Transform parent, Vector3 position)
        {
            var playground = new GameObject("Playground");
            playground.transform.SetParent(parent);
            playground.transform.position = position;

            var swing = new GameObject("SwingSet");
            swing.transform.SetParent(playground.transform);
            swing.transform.localPosition = new Vector3(-1.5f, 0, 0);
            var swingSr = swing.AddComponent<SpriteRenderer>();
            swingSr.sprite = CreateSwingSetSprite();
            swingSr.sortingOrder = 5;
            var swingCol = swing.AddComponent<BoxCollider2D>();
            swingCol.size = new Vector2(2f, 2f);

            var slide = new GameObject("Slide");
            slide.transform.SetParent(playground.transform);
            slide.transform.localPosition = new Vector3(1.5f, 0, 0);
            var slideSr = slide.AddComponent<SpriteRenderer>();
            slideSr.sprite = CreateSlideSprite();
            slideSr.sortingOrder = 5;
            var slideCol = slide.AddComponent<BoxCollider2D>();
            slideCol.size = new Vector2(1.5f, 2f);
        }

        private void CreateAbandonedBus(Transform parent, Vector3 position)
        {
            var bus = new GameObject("AbandonedBus");
            bus.transform.SetParent(parent);
            bus.transform.position = position;
            bus.transform.rotation = Quaternion.Euler(0, 0, 8f);

            var sr = bus.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBusSprite();
            sr.sortingOrder = 5;
            sr.color = new Color(0.6f, 0.55f, 0.45f);

            var col = bus.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 1.5f);

            for (int i = 0; i < 2; i++)
            {
                var debris = new GameObject($"Debris_{i}");
                debris.transform.SetParent(bus.transform);
                debris.transform.localPosition = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), 0);
                var dsr = debris.AddComponent<SpriteRenderer>();
                dsr.sprite = CreateDebrisSprite();
                dsr.sortingOrder = 4;
                dsr.color = new Color(0.4f, 0.4f, 0.35f);
            }
        }

        // ===================== STREETLIGHTS =====================

        private void CreateStreetlights(Transform parent, Vector3[] positions)
        {
            var lightsParent = new GameObject("Streetlights");
            lightsParent.transform.SetParent(parent);

            foreach (var pos in positions)
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

        // ===================== SPRITE CREATION =====================

        private Sprite CreateDinerSprite()
        {
            int w = 48, h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color wallColor = new Color(0.9f, 0.7f, 0.2f);
            Color roofColor = new Color(0.7f, 0.2f, 0.2f);

            // Building walls
            for (int y = 4; y < h - 4; y++)
                for (int x = 4; x < w - 4; x++)
                    pixels[y * w + x] = wallColor;

            // Roof
            for (int y = h - 6; y < h; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = roofColor;

            // Windows
            for (int wx = 8; wx < w - 8; wx += 8)
                for (int y = h/2; y < h - 8; y++)
                    for (int dx = 0; dx < 4 && wx + dx < w; dx++)
                        pixels[y * w + wx + dx] = new Color(0.7f, 0.9f, 1f, 0.8f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateDinerSignSprite()
        {
            int w = 32, h = 16;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color signColor = new Color(0.9f, 0.2f, 0.2f);
            Color textColor = new Color(1f, 1f, 0.8f);

            // Sign background
            for (int y = 2; y < h - 2; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = signColor;

            // "OPEN" text
            for (int i = 6; i < 12; i++)
                pixels[8 * w + i] = textColor;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreatePoliceKioskSprite()
        {
            int w = 32, h = 24;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color kioskColor = new Color(0.2f, 0.2f, 0.6f);
            Color detailColor = new Color(0.8f, 0.8f, 0.9f);

            // Kiosk base
            for (int y = 2; y < h - 2; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = kioskColor;

            // Blue stripe
            for (int y = h/2 - 1; y < h/2 + 1; y++)
                for (int x = 4; x < w - 4; x++)
                    pixels[y * w + x] = detailColor;

            // Window
            for (int y = 6; y < h - 6; y++)
                for (int x = w/2 - 3; x < w/2 + 3; x++)
                    pixels[y * w + x] = new Color(0.7f, 0.9f, 1f, 0.8f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreatePoliceCarSprite()
        {
            int w = 24, h = 12;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color bodyColor = new Color(0.2f, 0.2f, 0.6f);
            Color stripeColor = new Color(0.9f, 0.9f, 0.2f);

            // Car body
            for (int y = 2; y < h - 2; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = bodyColor;

            // Stripe
            for (int y = h/2 - 1; y < h/2 + 1; y++)
                for (int x = 4; x < w - 4; x++)
                    pixels[y * w + x] = stripeColor;

            // Wheels
            for (int y = 0; y < 3; y++)
                for (int x = 4; x < 8; x++)
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.1f);
            for (int y = 0; y < 3; y++)
                for (int x = w - 8; x < w - 4; x++)
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.1f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateOfficeBuildingSprite()
        {
            int w = 48, h = 40;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color wallColor = new Color(0.6f, 0.6f, 0.7f);
            Color windowColor = new Color(0.7f, 0.9f, 1f, 0.8f);

            // Building walls
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = wallColor;

            // Windows in grid pattern
            for (int wy = 4; wy < h - 4; wy += 6)
                for (int wx = 6; wx < w - 6; wx += 8)
                    for (int dy = 0; dy < 3 && wy + dy < h; dy++)
                        for (int dx = 0; dx < 4 && wx + dx < w; dx++)
                            pixels[(wy + dy) * w + (wx + dx)] = windowColor;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateCameraSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color cameraColor = new Color(0.3f, 0.3f, 0.3f);
            Color lensColor = new Color(0.1f, 0.1f, 0.1f);

            // Camera base
            for (int y = 4; y < 12; y++)
                for (int x = 4; x < 12; x++)
                    pixels[y * size + x] = cameraColor;

            // Lens
            for (int y = 6; y < 10; y++)
                for (int x = 6; x < 10; x++)
                    pixels[y * size + x] = lensColor;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateCraneSprite()
        {
            int w = 32, h = 48;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color craneColor = new Color(0.5f, 0.5f, 0.55f);
            Color armColor = new Color(0.4f, 0.4f, 0.45f);

            // Crane tower
            for (int y = 0; y < h - 8; y++)
                for (int x = w/2 - 3; x < w/2 + 3; x++)
                    pixels[y * w + x] = craneColor;

            // Crane arm
            for (int y = h - 12; y < h - 8; y++)
                for (int x = w/2 - 10; x < w/2 + 10; x++)
                    pixels[y * w + x] = armColor;

            // Counterweight
            for (int y = h - 16; y < h - 12; y++)
                for (int x = w/2 + 6; x < w/2 + 12; x++)
                    pixels[y * w + x] = new Color(0.3f, 0.3f, 0.35f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateContainerSprite()
        {
            int w = 24, h = 16;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color containerColor = new Color(0.2f, 0.4f, 0.8f);
            Color stripeColor = new Color(0.9f, 0.9f, 0.9f);

            // Container body
            for (int y = 2; y < h - 2; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = containerColor;

            // White stripes
            for (int y = 4; y < 6; y++)
                for (int x = 4; x < w - 4; x++)
                    pixels[y * w + x] = stripeColor;
            for (int y = h - 6; y < h - 4; y++)
                for (int x = 4; x < w - 4; x++)
                    pixels[y * w + x] = stripeColor;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateSchoolBusSprite()
        {
            int w = 48, h = 20;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color bodyColor = new Color(1f, 0.8f, 0.2f);
            Color detailColor = new Color(0.2f, 0.2f, 0.8f);

            // Bus body
            for (int y = 3; y < h - 3; y++)
                for (int x = 3; x < w - 3; x++)
                    pixels[y * w + x] = bodyColor;

            // "SCHOOL" text
            for (int i = 12; i < 24; i++)
                pixels[(h/2) * w + i] = detailColor;

            // Wheels
            for (int y = 0; y < 4; y++)
                for (int x = 8; x < 14; x++)
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.1f);
            for (int y = 0; y < 4; y++)
                for (int x = w - 14; x < w - 8; x++)
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.1f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateMailboxSprite()
        {
            int w = 12, h = 16;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color boxColor = new Color(0.8f, 0.2f, 0.2f);
            Color postColor = new Color(0.4f, 0.4f, 0.4f);

            // Post
            for (int y = 0; y < h - 4; y++)
                for (int x = w/2 - 1; x < w/2 + 1; x++)
                    pixels[y * w + x] = postColor;

            // Mailbox
            for (int y = h - 8; y < h - 2; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = boxColor;

            // Flag
            for (int y = h - 6; y < h - 4; y++)
                for (int x = w - 4; x < w - 1; x++)
                    pixels[y * w + x] = new Color(0.9f, 0.9f, 0.9f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreatePicnicTableSprite()
        {
            int w = 32, h = 12;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color tableColor = new Color(0.6f, 0.4f, 0.2f);
            Color benchColor = new Color(0.5f, 0.3f, 0.1f);

            // Tabletop
            for (int y = 6; y < 8; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = tableColor;

            // Benches
            for (int y = 2; y < 4; y++)
                for (int x = 4; x < w - 4; x++)
                    pixels[y * w + x] = benchColor;
            for (int y = h - 4; y < h - 2; y++)
                for (int x = 4; x < w - 4; x++)
                    pixels[y * w + x] = benchColor;

            // Legs
            for (int y = 4; y < 6; y++)
                for (int x = 6; x < 8; x++)
                    pixels[y * w + x] = tableColor;
            for (int y = 4; y < 6; y++)
                for (int x = w - 8; x < w - 6; x++)
                    pixels[y * w + x] = tableColor;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ===================== EXISTING SPRITE HELPERS =====================

        private Sprite CreateFountainSprite()
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
                    if (dist < 0.9f && dist > 0.6f)
                        pixels[y * size + x] = new Color(0.5f, 0.5f, 0.55f);
                    else if (dist <= 0.6f)
                        pixels[y * size + x] = new Color(0.3f, 0.5f, 0.8f, 0.7f);
                }
            }

            // Center column
            for (int y = size / 2 - 4; y < size / 2 + 6; y++)
                for (int x = size / 2 - 2; x < size / 2 + 2; x++)
                    if (y >= 0 && y < size && x >= 0 && x < size)
                        pixels[y * size + x] = new Color(0.6f, 0.6f, 0.65f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateFuelTankSprite()
        {
            int w = 32, h = 24;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color tankColor = new Color(0.5f, 0.5f, 0.55f);

            for (int y = 4; y < h - 4; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = tankColor;

            // Highlight stripe
            for (int x = 4; x < w - 4; x++)
            {
                pixels[(h / 2) * w + x] = new Color(0.6f, 0.6f, 0.65f);
                pixels[(h / 2 + 1) * w + x] = new Color(0.6f, 0.6f, 0.65f);
            }

            // Hazard stripe
            for (int x = 4; x < w - 4; x += 4)
                for (int dy = 0; dy < 3; dy++)
                    if (h - 6 + dy < h)
                        pixels[(h - 6 + dy) * w + x] = new Color(0.9f, 0.7f, 0.1f);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreatePlatformSprite()
        {
            int w = 64, h = 24;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color concrete = new Color(0.45f, 0.45f, 0.48f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float shade = 0.95f + Random.Range(-0.05f, 0.05f);
                    pixels[y * w + x] = concrete * shade;
                }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateSwingSetSprite()
        {
            int w = 32, h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color metal = new Color(0.4f, 0.4f, 0.45f);

            // Two vertical poles
            for (int y = 0; y < h; y++)
            {
                pixels[y * w + 4] = metal;
                pixels[y * w + 5] = metal;
                pixels[y * w + w - 5] = metal;
                pixels[y * w + w - 6] = metal;
            }

            // Top bar
            for (int x = 4; x < w - 4; x++)
            {
                pixels[(h - 2) * w + x] = metal;
                pixels[(h - 3) * w + x] = metal;
            }

            // Chains and seats
            for (int seat = 0; seat < 2; seat++)
            {
                int sx = 10 + seat * 12;
                for (int y = h / 3; y < h - 3; y += 2)
                    if (sx < w) pixels[y * w + sx] = new Color(0.3f, 0.3f, 0.35f);
                for (int x = sx - 2; x < sx + 3 && x < w; x++)
                    pixels[(h / 3) * w + x] = new Color(0.35f, 0.2f, 0.1f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateSlideSprite()
        {
            int w = 24, h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color metal = new Color(0.6f, 0.3f, 0.3f);

            // Slide ramp (diagonal)
            for (int y = 0; y < h; y++)
            {
                int x = (int)(4 + (float)y / h * (w - 12));
                for (int dx = 0; dx < 6 && x + dx < w; dx++)
                    pixels[y * w + x + dx] = metal;
            }

            // Ladder
            for (int y = 0; y < h; y++)
            {
                pixels[y * w + w - 3] = new Color(0.4f, 0.4f, 0.45f);
                pixels[y * w + w - 4] = new Color(0.4f, 0.4f, 0.45f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }
        private Sprite CreateBusSprite()
        {
            int w = 64, h = 24;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            Color body = new Color(0.85f, 0.7f, 0.15f);
            Color dark = new Color(0.3f, 0.3f, 0.25f);

            // Bus body
            for (int y = 4; y < h - 4; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = body;

            // Windows
            for (int wx = 8; wx < w - 8; wx += 8)
                for (int y = h / 2; y < h - 6; y++)
                    for (int dx = 0; dx < 5 && wx + dx < w; dx++)
                        pixels[y * w + wx + dx] = new Color(0.4f, 0.5f, 0.6f, 0.8f);

            // Wheels
            for (int y = 0; y < 6; y++)
            {
                for (int x = 8; x < 16; x++) pixels[y * w + x] = dark;
                for (int x = w - 16; x < w - 8; x++) pixels[y * w + x] = dark;
            }

            // Roof line
            for (int x = 2; x < w - 2; x++)
                pixels[(h - 5) * w + x] = dark;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ===================== EXISTING SPRITE HELPERS =====================

        private Sprite CreateHelicopterSprite()
        {
            int w = 64, h = 32;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            for (int y = 8; y < 24; y++)
                for (int x = 8; x < 56; x++)
                    pixels[y * w + x] = new Color(0.3f, 0.35f, 0.3f);

            for (int y = 12; y < 20; y++)
                for (int x = 0; x < 16; x++)
                    pixels[y * w + x] = new Color(0.25f, 0.3f, 0.25f);

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
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = sandColor * (0.9f + Random.Range(-0.1f, 0.1f));

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
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = (x / 6) % 2 == 0
                        ? new Color(0.9f, 0.2f, 0.1f)
                        : new Color(0.9f, 0.9f, 0.9f);

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
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = bagColor;

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
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = (y < 8 || x < 4 || x >= w - 4) ? darkColor : wallColor;

            for (int y = 16; y < 32; y++)
                for (int x = 24; x < 40; x++)
                    pixels[y * w + x] = new Color(0.1f, 0.3f, 0.15f, 0.8f);

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
                for (int x = 0; x < size; x++)
                    if (Mathf.Abs(x - size / 2f) + Mathf.Abs(y - size / 2f) < size / 2f)
                        pixels[y * size + x] = new Color(1f, 0.8f, 0f);

            for (int i = 4; i < 12; i++) pixels[8 * size + i] = Color.black;
            for (int i = 5; i < 11; i++) pixels[i * size + 8] = Color.black;

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
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = roofColor;

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
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = new Color(0.1f, 0.1f, 0.15f);

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

            for (int y = 0; y < h; y++)
                for (int x = 2; x < w - 2; x++)
                    pixels[y * w + x] = new Color(0.8f, 0.2f, 0.2f);

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
                for (int x = 1; x < w - 1; x++)
                    pixels[y * w + x] = lampColor;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 16f);
        }

        private Sprite CreateGlowSprite(Color glowColor)
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    pixels[y * size + x] = dist < 1f
                        ? new Color(glowColor.r, glowColor.g, glowColor.b, (1f - dist) * glowColor.a)
                        : Color.clear;
                }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }

    // ===================== EFFECT CLASSES =====================

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
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = y - center.y;
                    if (dy > 0 && dx < (size / 2f) * (1f - dy / size))
                        pixels[y * size + x] = Color.Lerp(new Color(1f, 0.6f, 0.1f), new Color(1f, 0.2f, 0f, 0f), dy / size);
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
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    if (dist < 1f)
                        pixels[y * size + x] = new Color(0.5f, 0.5f, 0.5f, (1f - dist) * 0.6f);
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
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w / 2f) / (w / 2f);
                    float dy = (float)y / h;
                    pixels[y * w + x] = new Color(1f, 1f, 0.8f, (1f - dx) * (1f - dy) * 0.3f);
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
