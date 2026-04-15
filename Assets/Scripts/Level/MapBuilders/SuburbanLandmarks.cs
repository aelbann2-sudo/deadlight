using Deadlight.Visuals;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public static class SuburbanLandmarks
    {
        public static void Create(Transform parent)
        {
            var root = new GameObject("SuburbanLandmarks").transform;
            root.SetParent(parent);

            CreateCheckpoint(root, SuburbanLayout.CheckpointPosition);
            CreateGasStation(root, SuburbanLayout.GasStationPosition);
            CreatePlayground(root, SuburbanLayout.PlaygroundPosition);
            CreateSchool(root, SuburbanLayout.SchoolPosition);
            CreateHospital(root, SuburbanLayout.HospitalPosition);
            CreateSchoolBus(root, SuburbanLayout.SchoolBusPosition);
            CreateAbandonedBus(root, SuburbanLayout.AbandonedBusPosition);
            CreateCulDeSacCenter(root, SuburbanLayout.CulDeSacPosition);
            CreateStreetlights(root, SuburbanLayout.StreetlightPositions);
        }

        private static void CreateCheckpoint(Transform parent, Vector3 position)
        {
            var checkpoint = new GameObject("MilitaryCheckpoint").transform;
            checkpoint.SetParent(parent);
            checkpoint.position = position;

            var booth = CreateSpriteObject(checkpoint, "GuardBooth", CreateGuardBoothSprite(), new Vector3(-2.3f, 0.3f, 0f), 5);
            var boothCollider = booth.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(boothCollider, booth.GetComponent<SpriteRenderer>().sprite, booth.transform.localScale, 0.9f, 0.88f);

            var sign = CreateSpriteObject(checkpoint, "CheckpointPost", CreateCheckpointSignSprite(), new Vector3(2.35f, -0.1f, 0f), 5);
            sign.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            var ammo = CreateSpriteObject(checkpoint, "AmmoCase", ProceduralSpriteGenerator.CreateCrateSprite(), new Vector3(-2.5f, -1.1f, 0f), 4);
            ammo.GetComponent<SpriteRenderer>().color = new Color(0.38f, 0.48f, 0.3f);

            CreateSpriteObject(checkpoint, "CheckpointLight", CreateStreetlightSprite(), new Vector3(2.6f, 0.7f, 0f), 4);
        }

        private static void CreateGasStation(Transform parent, Vector3 position)
        {
            var station = new GameObject("GasStation").transform;
            station.SetParent(parent);
            station.position = position;

            var canopy = CreateSpriteObject(station, "Canopy", CreateCanopySprite(), new Vector3(-1.4f, 0.2f, 0f), 6);
            var canopyCollider = canopy.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                canopyCollider,
                canopy.GetComponent<SpriteRenderer>().sprite,
                canopy.transform.localScale,
                new Vector2(2.8f, 1.2f));

            var store = CreateSpriteObject(station, "Store", CreateGasStoreSprite(), new Vector3(2f, 0.1f, 0f), 5);
            var storeCollider = store.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(storeCollider, store.GetComponent<SpriteRenderer>().sprite, store.transform.localScale, 0.9f, 0.88f);

            var pumpA = CreateSpriteObject(station, "FuelPump", CreateFuelPumpSprite(), new Vector3(-2f, -1f, 0f), 5);
            var pumpACol = pumpA.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                pumpACol,
                pumpA.GetComponent<SpriteRenderer>().sprite,
                pumpA.transform.localScale,
                new Vector2(0.5f, 0.8f));

            var pumpB = CreateSpriteObject(station, "FuelPump", CreateFuelPumpSprite(), new Vector3(-0.8f, -1f, 0f), 5);
            var pumpBCol = pumpB.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                pumpBCol,
                pumpB.GetComponent<SpriteRenderer>().sprite,
                pumpB.transform.localScale,
                new Vector2(0.5f, 0.8f));

            CreateSpriteObject(station, "NeonSign", CreateGasPriceSignSprite(), new Vector3(3.5f, 1.35f, 0f), 6);
        }

        private static void CreatePlayground(Transform parent, Vector3 position)
        {
            var playground = new GameObject("Playground").transform;
            playground.SetParent(parent);
            playground.position = position;

            var swing = CreateSpriteObject(playground, "SwingSet", CreateSwingSetSprite(), new Vector3(-1.7f, 0f, 0f), 5);
            var swingCollider = swing.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                swingCollider,
                swing.GetComponent<SpriteRenderer>().sprite,
                swing.transform.localScale,
                new Vector2(1.8f, 1.7f));

            var slide = CreateSpriteObject(playground, "Slide", CreateSlideSprite(), new Vector3(1.6f, -0.1f, 0f), 5);
            var slideCollider = slide.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                slideCollider,
                slide.GetComponent<SpriteRenderer>().sprite,
                slide.transform.localScale,
                new Vector2(1.2f, 1.8f));

            CreateSpriteObject(playground, "Bench", CreateBenchSprite(), new Vector3(0f, -1.8f, 0f), 4);
        }

        private static void CreateSchool(Transform parent, Vector3 position)
        {
            var school = new GameObject("School").transform;
            school.SetParent(parent);
            school.position = position;

            var building = CreateSpriteObject(school, "SchoolBuilding", CreateSchoolSprite(), new Vector3(0f, 0f, 0f), 5);
            var buildingCollider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(buildingCollider, building.GetComponent<SpriteRenderer>().sprite, building.transform.localScale, 0.92f, 0.9f);

            var sign = CreateSpriteObject(school, "SchoolSign", CreateSchoolSignSprite(), new Vector3(-2.7f, -1.2f, 0f), 6);

            var court = CreateSpriteObject(school, "BasketballCourt", CreateCourtSprite(), new Vector3(2.2f, -0.9f, 0f), 4);
            court.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var courtCollider = court.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                courtCollider,
                court.GetComponent<SpriteRenderer>().sprite,
                court.transform.localScale,
                new Vector2(1.8f, 1.2f));

            CreateSpriteObject(school, "Flag", CreateFlagPoleSprite(), new Vector3(0f, 1.6f, 0f), 6);
        }

        private static void CreateHospital(Transform parent, Vector3 position)
        {
            var hospital = new GameObject("Hospital").transform;
            hospital.SetParent(parent);
            hospital.position = position;

            var building = CreateSpriteObject(hospital, "ClinicBuilding", CreateHospitalSprite(), new Vector3(0f, 0f, 0f), 5);
            var buildingCollider = building.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(buildingCollider, building.GetComponent<SpriteRenderer>().sprite, building.transform.localScale, 0.92f, 0.9f);

            var sign = CreateSpriteObject(hospital, "HospitalSign", CreateHospitalSignSprite(), new Vector3(2.9f, -1.1f, 0f), 6);

            var ambulance = CreateSpriteObject(hospital, "Ambulance", CreateAmbulanceSprite(), new Vector3(-2.3f, -1.2f, 0f), 5);
            ambulance.transform.rotation = Quaternion.Euler(0f, 0f, 4f);
            var ambulanceCollider = ambulance.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                ambulanceCollider,
                ambulance.GetComponent<SpriteRenderer>().sprite,
                ambulance.transform.localScale,
                new Vector2(2.4f, 1f));
        }

        private static void CreateSchoolBus(Transform parent, Vector3 position)
        {
            var bus = CreateSpriteObject(parent, "SchoolBus", CreateSchoolBusSprite(), position, 5);
            bus.transform.rotation = Quaternion.Euler(0f, 0f, -4f);
            var collider = bus.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                collider,
                bus.GetComponent<SpriteRenderer>().sprite,
                bus.transform.localScale,
                new Vector2(3f, 1.1f));

            CreateSpriteObject(parent, "BusStop", CreateSchoolStopSprite(), position + new Vector3(3f, 0.5f, 0f), 5);
        }

        private static void CreateAbandonedBus(Transform parent, Vector3 position)
        {
            var bus = CreateSpriteObject(parent, "AbandonedBus", CreateAbandonedBusSprite(), position, 5);
            bus.transform.rotation = Quaternion.Euler(0f, 0f, 7f);
            var collider = bus.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                collider,
                bus.GetComponent<SpriteRenderer>().sprite,
                bus.transform.localScale,
                new Vector2(3.8f, 1.3f));

            var debrisA = CreateSpriteObject(bus.transform, "Debris_0", ProceduralSpriteGenerator.CreateRockSprite(), new Vector3(-2.4f, -0.5f, 0f), 4);
            debrisA.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            var debrisB = CreateSpriteObject(bus.transform, "Debris_1", ProceduralSpriteGenerator.CreateRockSprite(), new Vector3(2.1f, 0.4f, 0f), 4);
            debrisB.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        }

        private static void CreateCulDeSacCenter(Transform parent, Vector3 position)
        {
            var center = new GameObject("CulDeSacCenter").transform;
            center.SetParent(parent);
            center.position = position;

            var table = CreateSpriteObject(center, "PicnicTable", CreatePicnicTableSprite(), new Vector3(0f, -0.6f, 0f), 5);
            var tableCollider = table.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                tableCollider,
                table.GetComponent<SpriteRenderer>().sprite,
                table.transform.localScale,
                new Vector2(1.8f, 0.7f));

            var mailbox = CreateSpriteObject(center, "Mailbox", CreateMailboxSprite(), new Vector3(-1.7f, 0.2f, 0f), 5);
            var mailboxCollider = mailbox.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyCustomSpriteFootprint(
                mailboxCollider,
                mailbox.GetComponent<SpriteRenderer>().sprite,
                mailbox.transform.localScale,
                new Vector2(0.35f, 0.8f));

            CreateSpriteObject(center, "SwingSet", CreateSwingSetSprite(), new Vector3(1.7f, 0.6f, 0f), 5);
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

                CreateSpriteObject(light, "Pole", CreateStreetlightSprite(), Vector3.zero, 8);

                var glow = CreateSpriteObject(light, "LightGlow", CreateGlowSprite(new Color(1f, 0.92f, 0.65f, 0.25f)), new Vector3(0f, 1.1f, 0f), -100);
                glow.transform.localScale = Vector3.one * 2.4f;
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

        private static Sprite CreateGuardBoothSprite()
        {
            const int w = 24;
            const int h = 24;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color wall = new Color(0.56f, 0.56f, 0.6f);
            Color roof = new Color(0.24f, 0.24f, 0.28f);
            Color window = new Color(0.65f, 0.84f, 0.96f);

            FillRect(px, w, 2, 2, 20, 16, wall);
            FillRect(px, w, 0, 18, 24, 4, roof);
            FillRect(px, w, 5, 7, 6, 6, window);
            FillRect(px, w, 13, 7, 6, 6, window);
            return CreateSprite(tex, px);
        }

        private static Sprite CreateCheckpointSignSprite()
        {
            const int w = 16;
            const int h = 22;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 7, 0, 2, 12, new Color(0.45f, 0.48f, 0.54f));
            FillRect(px, w, 2, 12, 12, 8, new Color(0.9f, 0.84f, 0.66f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateCanopySprite()
        {
            const int w = 44;
            const int h = 24;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 4, 14, 36, 6, new Color(0.78f, 0.16f, 0.14f));
            FillRect(px, w, 6, 6, 3, 8, new Color(0.36f, 0.38f, 0.42f));
            FillRect(px, w, 35, 6, 3, 8, new Color(0.36f, 0.38f, 0.42f));
            FillRect(px, w, 9, 11, 26, 3, new Color(0.96f, 0.96f, 0.92f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateGasStoreSprite()
        {
            const int w = 34;
            const int h = 26;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 30, 18, new Color(0.84f, 0.8f, 0.72f));
            FillRect(px, w, 0, 20, 34, 4, new Color(0.7f, 0.18f, 0.16f));
            FillRect(px, w, 5, 8, 7, 7, new Color(0.63f, 0.83f, 0.96f));
            FillRect(px, w, 14, 8, 7, 7, new Color(0.63f, 0.83f, 0.96f));
            FillRect(px, w, 22, 2, 7, 12, new Color(0.46f, 0.32f, 0.18f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateFuelPumpSprite()
        {
            const int w = 12;
            const int h = 18;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 8, 12, new Color(0.78f, 0.12f, 0.12f));
            FillRect(px, w, 3, 10, 6, 3, new Color(0.72f, 0.9f, 1f));
            FillRect(px, w, 8, 6, 2, 6, new Color(0.14f, 0.14f, 0.18f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateGasPriceSignSprite()
        {
            const int w = 16;
            const int h = 30;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 7, 0, 2, 10, new Color(0.46f, 0.48f, 0.54f));
            FillRect(px, w, 2, 10, 12, 14, new Color(0.12f, 0.14f, 0.18f));
            FillRect(px, w, 4, 16, 8, 2, new Color(1f, 0.4f, 0.38f));
            FillRect(px, w, 4, 12, 8, 2, new Color(1f, 0.4f, 0.38f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateSwingSetSprite()
        {
            const int w = 30;
            const int h = 28;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color metal = new Color(0.55f, 0.6f, 0.68f);
            FillRect(px, w, 4, 3, 2, 18, metal);
            FillRect(px, w, 24, 3, 2, 18, metal);
            FillRect(px, w, 5, 19, 20, 2, metal);
            FillRect(px, w, 11, 8, 1, 9, metal);
            FillRect(px, w, 18, 8, 1, 9, metal);
            FillRect(px, w, 10, 7, 3, 1, new Color(0.44f, 0.28f, 0.16f));
            FillRect(px, w, 17, 7, 3, 1, new Color(0.44f, 0.28f, 0.16f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateSlideSprite()
        {
            const int w = 24;
            const int h = 28;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color frame = new Color(0.56f, 0.6f, 0.68f);
            Color slide = new Color(0.84f, 0.38f, 0.3f);
            FillRect(px, w, 4, 4, 2, 16, frame);
            FillRect(px, w, 18, 4, 2, 12, frame);
            for (int i = 0; i < 10; i++)
            {
                FillRect(px, w, 7 + i, 17 - i, 2, 2, slide);
            }

            return CreateSprite(tex, px);
        }

        private static Sprite CreateBenchSprite()
        {
            const int w = 20;
            const int h = 10;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 4, 16, 2, new Color(0.55f, 0.34f, 0.2f));
            FillRect(px, w, 5, 1, 2, 3, new Color(0.32f, 0.32f, 0.32f));
            FillRect(px, w, 13, 1, 2, 3, new Color(0.32f, 0.32f, 0.32f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateSchoolSprite()
        {
            const int w = 52;
            const int h = 34;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color brick = new Color(0.75f, 0.5f, 0.38f);
            Color trim = new Color(0.9f, 0.87f, 0.78f);
            Color roof = new Color(0.34f, 0.18f, 0.16f);
            Color window = new Color(0.68f, 0.85f, 0.96f);

            FillRect(px, w, 4, 4, 44, 18, brick);
            FillRect(px, w, 0, 22, 52, 5, roof);
            FillRect(px, w, 20, 4, 12, 11, trim);
            FillRect(px, w, 23, 4, 6, 9, new Color(0.5f, 0.34f, 0.2f));
            FillRect(px, w, 7, 10, 7, 6, window);
            FillRect(px, w, 15, 10, 7, 6, window);
            FillRect(px, w, 30, 10, 7, 6, window);
            FillRect(px, w, 38, 10, 7, 6, window);
            FillRect(px, w, 18, 26, 16, 4, trim);
            FillRect(px, w, 24, 27, 4, 2, new Color(0.62f, 0.2f, 0.18f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateSchoolSignSprite()
        {
            const int w = 16;
            const int h = 24;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 7, 0, 2, 10, new Color(0.44f, 0.46f, 0.52f));
            FillRect(px, w, 2, 10, 12, 8, new Color(0.98f, 0.88f, 0.38f));
            FillRect(px, w, 4, 12, 8, 2, new Color(0.42f, 0.2f, 0.16f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateCourtSprite()
        {
            const int w = 28;
            const int h = 18;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color surface = new Color(0.46f, 0.52f, 0.6f);
            Color line = new Color(0.95f, 0.93f, 0.86f);

            FillRect(px, w, 2, 2, 24, 14, surface);
            FillRect(px, w, 2, 2, 24, 1, line);
            FillRect(px, w, 2, 15, 24, 1, line);
            FillRect(px, w, 2, 2, 1, 14, line);
            FillRect(px, w, 25, 2, 1, 14, line);
            FillRect(px, w, 13, 2, 1, 14, line);
            return CreateSprite(tex, px);
        }

        private static Sprite CreateFlagPoleSprite()
        {
            const int w = 12;
            const int h = 28;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 5, 0, 2, 22, new Color(0.52f, 0.54f, 0.6f));
            FillRect(px, w, 7, 16, 4, 4, new Color(0.88f, 0.16f, 0.16f));
            FillRect(px, w, 7, 12, 4, 4, Color.white);
            FillRect(px, w, 7, 8, 4, 4, new Color(0.12f, 0.28f, 0.66f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateSchoolBusSprite()
        {
            const int w = 44;
            const int h = 18;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color body = new Color(0.95f, 0.76f, 0.18f);
            Color glass = new Color(0.62f, 0.84f, 0.96f);
            FillRect(px, w, 2, 3, 40, 11, body);
            FillRect(px, w, 8, 8, 8, 4, glass);
            FillRect(px, w, 18, 8, 8, 4, glass);
            FillRect(px, w, 28, 8, 8, 4, glass);
            FillRect(px, w, 8, 0, 5, 3, Color.black);
            FillRect(px, w, 31, 0, 5, 3, Color.black);
            return CreateSprite(tex, px);
        }

        private static Sprite CreateSchoolStopSprite()
        {
            const int w = 14;
            const int h = 22;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 6, 0, 2, 10, new Color(0.48f, 0.48f, 0.52f));
            FillRect(px, w, 2, 10, 10, 8, new Color(0.98f, 0.86f, 0.26f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateAbandonedBusSprite()
        {
            const int w = 48;
            const int h = 20;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color body = new Color(0.66f, 0.6f, 0.52f);
            Color rust = new Color(0.48f, 0.3f, 0.18f);
            Color glass = new Color(0.58f, 0.68f, 0.74f);
            FillRect(px, w, 2, 3, 44, 12, body);
            FillRect(px, w, 12, 9, 7, 3, glass);
            FillRect(px, w, 21, 9, 7, 3, glass);
            FillRect(px, w, 30, 9, 7, 3, glass);
            FillRect(px, w, 5, 6, 4, 3, rust);
            FillRect(px, w, 24, 4, 8, 3, rust);
            FillRect(px, w, 10, 0, 5, 3, Color.black);
            FillRect(px, w, 33, 0, 5, 3, Color.black);
            return CreateSprite(tex, px);
        }

        private static Sprite CreatePicnicTableSprite()
        {
            const int w = 24;
            const int h = 14;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color wood = new Color(0.55f, 0.34f, 0.2f);
            FillRect(px, w, 3, 8, 18, 2, wood);
            FillRect(px, w, 4, 4, 16, 2, wood);
            FillRect(px, w, 6, 0, 2, 4, new Color(0.36f, 0.24f, 0.16f));
            FillRect(px, w, 16, 0, 2, 4, new Color(0.36f, 0.24f, 0.16f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateMailboxSprite()
        {
            const int w = 12;
            const int h = 20;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 5, 0, 2, 11, new Color(0.46f, 0.48f, 0.54f));
            FillRect(px, w, 2, 11, 8, 6, new Color(0.82f, 0.22f, 0.2f));
            FillRect(px, w, 9, 13, 1, 3, new Color(0.96f, 0.96f, 0.92f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateHospitalSprite()
        {
            const int w = 54;
            const int h = 34;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color wall = new Color(0.83f, 0.87f, 0.9f);
            Color trim = new Color(0.66f, 0.72f, 0.76f);
            Color roof = new Color(0.54f, 0.22f, 0.22f);
            Color window = new Color(0.7f, 0.88f, 0.96f);

            FillRect(px, w, 4, 4, 46, 19, wall);
            FillRect(px, w, 0, 23, 54, 4, roof);
            FillRect(px, w, 16, 4, 22, 7, trim);
            FillRect(px, w, 24, 5, 6, 5, new Color(0.84f, 0.2f, 0.22f));
            FillRect(px, w, 26, 3, 2, 9, new Color(0.84f, 0.2f, 0.22f));
            FillRect(px, w, 8, 10, 8, 6, window);
            FillRect(px, w, 18, 10, 8, 6, window);
            FillRect(px, w, 28, 10, 8, 6, window);
            FillRect(px, w, 38, 10, 8, 6, window);
            FillRect(px, w, 22, 4, 10, 10, trim);
            FillRect(px, w, 23, 4, 8, 8, new Color(0.86f, 0.88f, 0.9f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateHospitalSignSprite()
        {
            const int w = 18;
            const int h = 26;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 8, 0, 2, 10, new Color(0.46f, 0.48f, 0.54f));
            FillRect(px, w, 3, 10, 12, 10, new Color(0.96f, 0.97f, 0.99f));
            FillRect(px, w, 7, 12, 4, 6, new Color(0.84f, 0.18f, 0.22f));
            FillRect(px, w, 5, 14, 8, 2, new Color(0.84f, 0.18f, 0.22f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateAmbulanceSprite()
        {
            const int w = 36;
            const int h = 18;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color body = new Color(0.95f, 0.95f, 0.94f);
            Color stripe = new Color(0.84f, 0.18f, 0.22f);
            Color glass = new Color(0.7f, 0.88f, 0.96f);

            FillRect(px, w, 2, 3, 30, 11, body);
            FillRect(px, w, 24, 6, 8, 8, body);
            FillRect(px, w, 5, 8, 7, 4, glass);
            FillRect(px, w, 14, 8, 7, 4, glass);
            FillRect(px, w, 22, 8, 4, 4, glass);
            FillRect(px, w, 6, 5, 18, 2, stripe);
            FillRect(px, w, 12, 4, 4, 6, stripe);
            FillRect(px, w, 10, 6, 8, 2, stripe);
            FillRect(px, w, 7, 0, 5, 3, Color.black);
            FillRect(px, w, 24, 0, 5, 3, Color.black);
            return CreateSprite(tex, px);
        }

        private static Sprite CreateStreetlightSprite()
        {
            const int w = 12;
            const int h = 32;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color metal = new Color(0.46f, 0.48f, 0.54f);
            FillRect(px, w, 5, 0, 2, 24, metal);
            FillRect(px, w, 5, 23, 5, 2, metal);
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
