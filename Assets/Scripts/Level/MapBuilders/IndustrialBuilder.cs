using Deadlight.Visuals;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public class IndustrialBuilder : MapBuilderBase
    {
        private readonly struct WarehouseLot
        {
            public readonly Vector3 Position;
            public readonly Vector2 Size;
            public readonly int Variant;
            public readonly bool DockFacesSouth;

            public WarehouseLot(float x, float y, float width, float height, int variant, bool dockFacesSouth)
            {
                Position = new Vector3(x, y, 0f);
                Size = new Vector2(width, height);
                Variant = variant;
                DockFacesSouth = dockFacesSouth;
            }
        }

        private static readonly WarehouseLot[] WarehouseLots =
        {
            new WarehouseLot(-10.5f, 21f, 6.8f, 3.6f, 0, true),
            new WarehouseLot(10.5f, 18.5f, 7.4f, 3.9f, 1, true),
            new WarehouseLot(-9f, 7.2f, 7.4f, 3.7f, 2, false),
            new WarehouseLot(9.5f, 7.4f, 6.8f, 3.5f, 0, false),
            new WarehouseLot(-23.5f, -18.8f, 8f, 3.7f, 1, true),
        };

        private static readonly Vector3[] StreetPostPositions =
        {
            new Vector3(-30f, 0f, 0f),
            new Vector3(-12f, 3f, 0f),
            new Vector3(12f, -3f, 0f),
            new Vector3(30f, 0f, 0f),
            new Vector3(-24f, 17f, 0f),
            new Vector3(24f, 17f, 0f),
            new Vector3(-24f, -18f, 0f),
            new Vector3(24f, -18f, 0f),
            new Vector3(0f, 26f, 0f),
            new Vector3(0f, -24f, 0f),
        };

        private static readonly Vector3[] VehiclePositions =
        {
            new Vector3(-14f, 0.9f, 0f),
            new Vector3(12f, -0.9f, 0f),
            new Vector3(-24f, -5.5f, 0f),
            new Vector3(24f, -6f, 0f),
            new Vector3(-27.5f, -22.2f, 0f),
            new Vector3(20.5f, 21.5f, 0f),
            new Vector3(-2.5f, -17.8f, 0f),
        };

        private static readonly Vector3[] EdgeTreePositions =
        {
            new Vector3(-33.5f, 31f, 0f),
            new Vector3(33.5f, 28f, 0f),
            new Vector3(-34f, -7f, 0f),
            new Vector3(33f, -14f, 0f),
            new Vector3(30f, -31.5f, 0f),
            new Vector3(-34f, 18f, 0f),
            new Vector3(34f, 4f, 0f),
            new Vector3(-33f, -28f, 0f),
            new Vector3(14f, -33f, 0f),
            new Vector3(-14f, -33f, 0f),
            new Vector3(-33f, 33f, 0f),
            new Vector3(33f, 33f, 0f),
        };

        private static readonly Vector3[] RubblePositions =
        {
            new Vector3(-31f, 27.5f, 0f),
            new Vector3(31f, 22f, 0f),
            new Vector3(-19f, -6.5f, 0f),
            new Vector3(18f, -9f, 0f),
            new Vector3(-2f, 29f, 0f),
            new Vector3(6f, -31f, 0f),
            new Vector3(-31f, -31f, 0f),
            new Vector3(15f, 30f, 0f),
            new Vector3(-15f, -14f, 0f),
            new Vector3(15f, -14f, 0f),
            new Vector3(-28f, -10f, 0f),
            new Vector3(28f, -10f, 0f),
            new Vector3(-8f, 20f, 0f),
            new Vector3(8f, 15f, 0f),
        };

        protected override string GetMapName() => "IndustrialDistrict";

        public override bool OwnsLandmarks => true;

        public override void BuildLandmarks(Transform parent)
        {
            IndustrialLandmarks.Create(parent);
        }

        protected override void BuildLayout()
        {
            ReserveLandmarkSpaces();
            BuildWarehouseBlocks();
            BuildWestSalvageYard();
            BuildEastProcessYard();
            BuildSouthDocks();
            BuildLabApproach();
            BuildMaintenanceYard();
            BuildCenterRoadDetails();
            BuildNorthParkingLot();
            BuildEastServiceArea();
            BuildSoutheastScrapPile();
            BuildRoadScatter();
            PlaceVehicles();
            PlaceStreetPosts();
            ScatterEdgeTrees();
            ScatterRubble();
        }

        private void ReserveLandmarkSpaces()
        {
            ReserveSpace(IndustrialLayout.CrashSitePosition, new Vector2(9f, 6f));
            ReserveSpace(IndustrialLayout.ResearchLabPosition, new Vector2(10f, 5.5f));
            ReserveSpace(IndustrialLayout.FuelDepotPosition, new Vector2(8f, 6f));
            ReserveSpace(IndustrialLayout.LoadingDockPosition, new Vector2(9f, 5.5f));
            ReserveSpace(IndustrialLayout.ControlOfficePosition, new Vector2(7f, 5f));
            ReserveSpace(IndustrialLayout.CraneYardPosition, new Vector2(8f, 7f));

            foreach (Vector3 pos in IndustrialLayout.StreetlightPositions)
            {
                ReserveSpace(pos, new Vector2(0.8f, 0.8f));
            }
        }

        private void BuildWarehouseBlocks()
        {
            var blocks = CreateDistrictRoot("WarehouseBlocks");
            Color[] colors =
            {
                new Color(0.58f, 0.6f, 0.64f),
                new Color(0.63f, 0.58f, 0.54f),
                new Color(0.52f, 0.56f, 0.6f),
            };

            foreach (WarehouseLot lot in WarehouseLots)
            {
                SpawnWarehouse(blocks, lot, colors[lot.Variant % colors.Length]);
            }
        }

        private void BuildWestSalvageYard()
        {
            var yard = CreateDistrictRoot("WestSalvageYard");
            Color fenceColor = new Color(0.54f, 0.55f, 0.5f);

            SpawnFence(yard, new Vector3(-29f, 14.5f, 0f), new Vector3(-19f, 14.5f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(-29f, -2.5f, 0f), new Vector3(-26f, -2.5f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(-22f, -2.5f, 0f), new Vector3(-19f, -2.5f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(-29f, -2.5f, 0f), new Vector3(-29f, 14.5f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(-19f, -2.5f, 0f), new Vector3(-19f, 14.5f, 0f), fenceColor);

            SpawnContainer(yard, new Vector3(-26.3f, 10.6f, 0f), false, new Color(0.58f, 0.27f, 0.24f));
            SpawnContainer(yard, new Vector3(-26.3f, 8f, 0f), false, new Color(0.31f, 0.46f, 0.58f));
            SpawnContainer(yard, new Vector3(-26.3f, 5.4f, 0f), false, new Color(0.42f, 0.47f, 0.26f));
            SpawnContainer(yard, new Vector3(-22.1f, 11.1f, 0f), true, new Color(0.36f, 0.34f, 0.38f));
            SpawnContainer(yard, new Vector3(-22.1f, 7.5f, 0f), true, new Color(0.58f, 0.42f, 0.24f));

            TrySpawnDumpster(yard, new Vector3(-26.2f, 1.4f, 0f));
            TrySpawnDumpster(yard, new Vector3(-21.4f, 2.8f, 0f));
            TrySpawnBarrel(yard, new Vector3(-23.8f, -0.2f, 0f), true);
            TrySpawnBarrel(yard, new Vector3(-22.7f, -0.6f, 0f), false);
            TrySpawnCrate(yard, new Vector3(-20.6f, 10.8f, 0f));
        }

        private void BuildEastProcessYard()
        {
            var yard = CreateDistrictRoot("EastProcessYard");
            Color fenceColor = new Color(0.56f, 0.56f, 0.51f);

            SpawnFence(yard, new Vector3(19f, 14.8f, 0f), new Vector3(29f, 14.8f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(19f, -2.5f, 0f), new Vector3(22f, -2.5f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(26f, -2.5f, 0f), new Vector3(29f, -2.5f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(19f, -2.5f, 0f), new Vector3(19f, 14.8f, 0f), fenceColor);
            SpawnFence(yard, new Vector3(29f, -2.5f, 0f), new Vector3(29f, 14.8f, 0f), fenceColor);

            SpawnFuelTank(yard, new Vector3(21.6f, 9.7f, 0f));
            SpawnFuelTank(yard, new Vector3(26.5f, 9.7f, 0f));
            SpawnFuelTank(yard, new Vector3(24.1f, 13f, 0f));

            SpawnPipeRack(yard, new Vector3(24f, 4.6f, 0f), 7.2f, false);
            SpawnPipeRack(yard, new Vector3(24f, 2.7f, 0f), 7.2f, false);
            SpawnContainer(yard, new Vector3(21f, 6.5f, 0f), true, new Color(0.42f, 0.44f, 0.48f));
            SpawnContainer(yard, new Vector3(27.1f, 6.7f, 0f), false, new Color(0.55f, 0.42f, 0.22f));

            TrySpawnBarrel(yard, new Vector3(20.8f, 2.6f, 0f), true);
            TrySpawnBarrel(yard, new Vector3(22f, 1.9f, 0f), false);
            TrySpawnCrate(yard, new Vector3(27.3f, 1.6f, 0f));
            TrySpawnCrate(yard, new Vector3(24.4f, 6.5f, 0f));
            TrySpawnDumpster(yard, new Vector3(28.2f, 5.2f, 0f));
        }

        private void BuildSouthDocks()
        {
            var docks = CreateDistrictRoot("SouthDocks");
            Color fenceColor = new Color(0.54f, 0.55f, 0.5f);

            SpawnDockPlatform(docks, new Vector3(-23.5f, -23.8f, 0f), new Vector2(6.8f, 1.7f));
            SpawnFence(docks, new Vector3(-30f, -27.8f, 0f), new Vector3(-17f, -27.8f, 0f), fenceColor);
            SpawnFence(docks, new Vector3(-30f, -27.8f, 0f), new Vector3(-30f, -19f, 0f), fenceColor);

            TrySpawnCrate(docks, new Vector3(-26.1f, -24.6f, 0f));
            TrySpawnCrate(docks, new Vector3(-23.7f, -24.5f, 0f));
            TrySpawnCrate(docks, new Vector3(-21.3f, -24.6f, 0f));
            TrySpawnBarrel(docks, new Vector3(-19.8f, -22.3f, 0f), false);
            TrySpawnBarrel(docks, new Vector3(-27.6f, -21.9f, 0f), true);
            TrySpawnDumpster(docks, new Vector3(-18.9f, -19.8f, 0f));
        }

        private void BuildLabApproach()
        {
            var lab = CreateDistrictRoot("LabApproach");
            Color fenceColor = new Color(0.56f, 0.57f, 0.52f);

            SpawnFence(lab, new Vector3(-8f, -25.2f, 0f), new Vector3(-8f, -31.8f, 0f), fenceColor);
            SpawnFence(lab, new Vector3(8f, -25.2f, 0f), new Vector3(8f, -31.8f, 0f), fenceColor);
            SpawnFence(lab, new Vector3(-8f, -31.8f, 0f), new Vector3(-2f, -31.8f, 0f), fenceColor);
            SpawnFence(lab, new Vector3(2f, -31.8f, 0f), new Vector3(8f, -31.8f, 0f), fenceColor);

            SpawnPipeRack(lab, new Vector3(0f, -18.4f, 0f), 10f, false);
            TrySpawnCar(lab, new Vector3(-4.3f, -17.9f, 0f), 0f);
            TrySpawnCar(lab, new Vector3(4.4f, -17.8f, 0f), 180f);
            TrySpawnBarrel(lab, new Vector3(-6.3f, -24.5f, 0f), false);
            TrySpawnBarrel(lab, new Vector3(6.2f, -24.4f, 0f), false);
        }

        private void BuildMaintenanceYard()
        {
            var yard = CreateDistrictRoot("MaintenanceYard");
            Color shedColor = new Color(0.6f, 0.62f, 0.67f);

            SpawnServiceShed(yard, new Vector3(-6.4f, -9.8f, 0f), new Vector2(4.8f, 2.7f), shedColor, 0);
            SpawnServiceShed(yard, new Vector3(6.6f, -9.2f, 0f), new Vector2(4.4f, 2.5f), shedColor, 2);
            SpawnPipeRack(yard, new Vector3(0f, -8.2f, 0f), 8.2f, false);
            TrySpawnDumpster(yard, new Vector3(0.2f, -12.1f, 0f));
            TrySpawnCrate(yard, new Vector3(-1.7f, -12f, 0f));
            TrySpawnBarrel(yard, new Vector3(2f, -11.8f, 0f), false);
        }

        private void BuildCenterRoadDetails()
        {
            var center = CreateDistrictRoot("CenterRoadDetails");
            SpawnPipeRack(center, new Vector3(0f, 11.2f, 0f), 14f, false);
            SpawnPipeRack(center, new Vector3(0f, -10.5f, 0f), 12f, false);

            TrySpawnCrate(center, new Vector3(-4.8f, 2.8f, 0f));
            TrySpawnCrate(center, new Vector3(6.1f, -2.8f, 0f));
            TrySpawnCrate(center, new Vector3(-7.5f, -1.4f, 0f));
            TrySpawnCrate(center, new Vector3(8.2f, 1.2f, 0f));
            TrySpawnBarrel(center, new Vector3(-1.2f, 3.1f, 0f), false);
            TrySpawnBarrel(center, new Vector3(2.2f, -2.9f, 0f), false);
            TrySpawnBarrel(center, new Vector3(-5.6f, -3.5f, 0f), true);
            TrySpawnBarrel(center, new Vector3(5.8f, 3.7f, 0f), false);
            TrySpawnDumpster(center, new Vector3(-3f, -4.8f, 0f));
            TrySpawnDumpster(center, new Vector3(3.5f, 5.2f, 0f));
        }

        private void BuildNorthParkingLot()
        {
            var lot = CreateDistrictRoot("NorthParkingLot");

            TrySpawnCar(lot, new Vector3(-6.4f, 26.5f, 0f), 90f);
            TrySpawnCar(lot, new Vector3(-3.8f, 26.5f, 0f), 90f);
            TrySpawnCar(lot, new Vector3(3.8f, 26.2f, 0f), 90f);
            TrySpawnCar(lot, new Vector3(6.4f, 26.4f, 0f), 90f);

            TrySpawnBarrel(lot, new Vector3(-8.5f, 27.2f, 0f), false);
            TrySpawnBarrel(lot, new Vector3(8.5f, 27.3f, 0f), false);
            TrySpawnCrate(lot, new Vector3(0f, 28.3f, 0f));
            TrySpawnDumpster(lot, new Vector3(-10.5f, 28f, 0f));
        }

        private void BuildEastServiceArea()
        {
            var area = CreateDistrictRoot("EastServiceArea");

            SpawnContainer(area, new Vector3(16.5f, -15f, 0f), false, new Color(0.52f, 0.34f, 0.28f));
            SpawnContainer(area, new Vector3(16.5f, -17.5f, 0f), false, new Color(0.34f, 0.44f, 0.54f));

            TrySpawnCrate(area, new Vector3(14f, -14f, 0f));
            TrySpawnCrate(area, new Vector3(14.5f, -17.2f, 0f));
            TrySpawnBarrel(area, new Vector3(18.8f, -14.5f, 0f), true);
            TrySpawnBarrel(area, new Vector3(19f, -16.8f, 0f), false);
            TrySpawnDumpster(area, new Vector3(12.5f, -16f, 0f));
            TrySpawnCar(area, new Vector3(13f, -20.5f, 0f), 0f);
        }

        private void BuildSoutheastScrapPile()
        {
            var scrap = CreateDistrictRoot("SoutheastScrap");
            Color fenceColor = new Color(0.54f, 0.55f, 0.5f);

            SpawnFence(scrap, new Vector3(17f, -27.8f, 0f), new Vector3(29f, -27.8f, 0f), fenceColor);

            SpawnContainer(scrap, new Vector3(20f, -25.5f, 0f), true, new Color(0.42f, 0.56f, 0.36f));
            SpawnContainer(scrap, new Vector3(26f, -25f, 0f), false, new Color(0.56f, 0.38f, 0.3f));

            TrySpawnCrate(scrap, new Vector3(22.5f, -24.8f, 0f));
            TrySpawnCrate(scrap, new Vector3(24f, -26.5f, 0f));
            TrySpawnBarrel(scrap, new Vector3(18.5f, -23.5f, 0f), false);
            TrySpawnBarrel(scrap, new Vector3(28f, -25.8f, 0f), true);
            TrySpawnDumpster(scrap, new Vector3(23f, -22.8f, 0f));
        }

        private void BuildRoadScatter()
        {
            var scatter = CreateDistrictRoot("RoadScatter");

            TrySpawnCar(scatter, new Vector3(-14f, 0f, 0f), 0f);
            TrySpawnCar(scatter, new Vector3(14f, 0f, 0f), 180f);
            TrySpawnCar(scatter, new Vector3(0f, -14f, 0f), 90f);
            TrySpawnCar(scatter, new Vector3(0f, 18f, 0f), 90f);
            TrySpawnCar(scatter, new Vector3(-20f, -14f, 0f), 0f);
            TrySpawnCar(scatter, new Vector3(20f, -14f, 0f), 180f);

            TrySpawnCrate(scatter, new Vector3(-10f, 14f, 0f));
            TrySpawnCrate(scatter, new Vector3(10f, -14f, 0f));
            TrySpawnCrate(scatter, new Vector3(-14f, -6f, 0f));
            TrySpawnCrate(scatter, new Vector3(14f, 6f, 0f));

            TrySpawnBarrel(scatter, new Vector3(-16f, 3f, 0f), false);
            TrySpawnBarrel(scatter, new Vector3(16f, -3f, 0f), false);
            TrySpawnBarrel(scatter, new Vector3(-6f, -14f, 0f), true);
            TrySpawnBarrel(scatter, new Vector3(6f, 14f, 0f), false);

            TrySpawnDumpster(scatter, new Vector3(-22f, 0f, 0f));
            TrySpawnDumpster(scatter, new Vector3(22f, 0f, 0f));
            TrySpawnDumpster(scatter, new Vector3(0f, -20f, 0f));
        }

        private void PlaceVehicles()
        {
            var vehicles = CreateDistrictRoot("Vehicles");
            float[] angles = { 0f, 180f, 90f, 90f, 0f, 180f, 0f };

            for (int i = 0; i < VehiclePositions.Length; i++)
            {
                TrySpawnCar(vehicles, VehiclePositions[i], angles[i]);
            }
        }

        private void PlaceStreetPosts()
        {
            var posts = CreateDistrictRoot("StreetPosts");
            foreach (Vector3 pos in StreetPostPositions)
            {
                SpawnStreetPost(posts, pos);
            }
        }

        private void ScatterEdgeTrees()
        {
            var trees = CreateDistrictRoot("EdgeTrees");
            foreach (Vector3 pos in EdgeTreePositions)
            {
                if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.8f, 0.8f)))
                {
                    SpawnTree(trees, pos, false);
                }
            }
        }

        private void ScatterRubble()
        {
            var rubble = CreateDistrictRoot("Rubble");
            foreach (Vector3 pos in RubblePositions)
            {
                if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.8f, 0.8f)))
                {
                    SpawnRock(rubble, pos, false);
                }
            }
        }

        private Transform CreateDistrictRoot(string name)
        {
            var district = new GameObject(name).transform;
            district.SetParent(root);
            return district;
        }

        private void SpawnWarehouse(Transform parent, WarehouseLot lot, Color tint)
        {
            if (!TryPlace(lot.Position, lot.Size))
            {
                return;
            }

            var warehouse = new GameObject("Warehouse");
            warehouse.transform.SetParent(parent);
            warehouse.transform.position = lot.Position;

            var sr = warehouse.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWarehouseSprite(lot.Variant);
            sr.sortingOrder = Mathf.RoundToInt(-lot.Position.y);
            sr.color = tint;
            warehouse.transform.localScale = new Vector3(lot.Size.x / 4.6f, lot.Size.y / 2.8f, 1f);

            var col = warehouse.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyBaseFootprint(col, lot.Size, 0.9f, 0.42f, 0.04f, 0.8f);

            Vector3 doorBase = lot.DockFacesSouth ? new Vector3(0f, -lot.Size.y * 0.42f, 0f) : new Vector3(0f, lot.Size.y * 0.42f, 0f);
            CreateDoor(warehouse.transform, doorBase + new Vector3(-1.4f, 0f, 0f), 1.0f);
            CreateDoor(warehouse.transform, doorBase + new Vector3(1.3f, 0f, 0f), 1.0f);
            CreateVent(warehouse.transform, new Vector3(-1.5f, 0.9f, 0f));
            CreateVent(warehouse.transform, new Vector3(1.6f, 1.1f, 0f));

            if (lot.DockFacesSouth)
            {
                TrySpawnCrate(parent, lot.Position + new Vector3(-2.4f, -2.4f, 0f));
                TrySpawnBarrel(parent, lot.Position + new Vector3(2.6f, -2.1f, 0f), false);
            }
            else
            {
                TrySpawnBarrel(parent, lot.Position + new Vector3(-2.1f, 2.1f, 0f), false);
                TrySpawnCrate(parent, lot.Position + new Vector3(2.4f, 2.5f, 0f));
            }
        }

        private void SpawnServiceShed(Transform parent, Vector3 pos, Vector2 size, Color tint, int variant)
        {
            if (!TryPlace(pos, size))
            {
                return;
            }

            var shed = new GameObject("ServiceShed");
            shed.transform.SetParent(parent);
            shed.transform.position = pos;

            var sr = shed.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWarehouseSprite(variant);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            sr.color = tint;
            shed.transform.localScale = new Vector3(size.x / 4.6f, size.y / 2.8f, 1f);

            var col = shed.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplyBaseFootprint(col, size, 0.9f, 0.44f, 0.03f, 0.65f);

            CreateDoor(shed.transform, new Vector3(0f, -size.y * 0.4f, 0f), 0.8f);
        }

        private void SpawnContainer(Transform parent, Vector3 pos, bool vertical, Color color)
        {
            Vector2 size = vertical ? new Vector2(1.1f, 2.1f) : new Vector2(2.1f, 1.1f);
            if (!TryPlace(pos, size))
            {
                return;
            }

            var container = new GameObject("ShippingContainer");
            container.transform.SetParent(parent);
            container.transform.position = pos;
            if (vertical)
            {
                container.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }

            var sr = container.AddComponent<SpriteRenderer>();
            sr.sprite = CreateContainerSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            sr.color = color;
            container.transform.localScale = new Vector3(1.45f, 1.05f, 1f);

            var col = container.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 0.9f);
        }

        private void SpawnFuelTank(Transform parent, Vector3 pos)
        {
            if (!TryPlace(pos, new Vector2(2.4f, 1.7f)))
            {
                return;
            }

            var tank = new GameObject("FuelTank").transform;
            tank.SetParent(parent);
            tank.position = pos;

            var sr = tank.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateTankSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);

            var col = tank.gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.2f, 1.4f);
        }

        private void SpawnDockPlatform(Transform parent, Vector3 pos, Vector2 size)
        {
            if (!TryPlace(pos, size))
            {
                return;
            }

            var dock = new GameObject("LoadingPlatform");
            dock.transform.SetParent(parent);
            dock.transform.position = pos;

            var sr = dock.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDockPlatformSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            dock.transform.localScale = new Vector3(size.x / 4.5f, size.y / 1.1f, 1f);

            var col = dock.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private void SpawnPipeRack(Transform parent, Vector3 pos, float length, bool vertical)
        {
            var rack = new GameObject("PipeRack");
            rack.transform.SetParent(parent);
            rack.transform.position = pos;
            rack.transform.rotation = vertical ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;

            var sr = rack.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePipeRackSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;
            rack.transform.localScale = new Vector3(length / 4f, 1f, 1f);
        }

        private void CreateDoor(Transform parent, Vector3 localPos, float scale)
        {
            var door = new GameObject("RollUpDoor");
            door.transform.SetParent(parent);
            door.transform.localPosition = localPos;

            var sr = door.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDoorSprite();
            sr.sortingOrder = parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
            door.transform.localScale = Vector3.one * scale;
        }

        private void CreateVent(Transform parent, Vector3 localPos)
        {
            var vent = new GameObject("Vent");
            vent.transform.SetParent(parent);
            vent.transform.localPosition = localPos;

            var sr = vent.AddComponent<SpriteRenderer>();
            sr.sprite = CreateVentSprite();
            sr.sortingOrder = parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
        }

        private void SpawnStreetPost(Transform parent, Vector3 pos)
        {
            var post = new GameObject("StreetPost");
            post.transform.SetParent(parent);
            post.transform.position = pos;

            var sr = post.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateStreetPostSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 2;
        }

        private void TrySpawnCrate(Transform parent, Vector3 pos)
        {
            if (TryPlace(pos, new Vector2(0.9f, 0.9f)))
            {
                SpawnCrate(parent, pos, false);
            }
        }

        private void TrySpawnBarrel(Transform parent, Vector3 pos, bool explosive)
        {
            if (TryPlace(pos, new Vector2(0.65f, 0.65f)))
            {
                SpawnBarrel(parent, pos, explosive, false);
            }
        }

        private void TrySpawnDumpster(Transform parent, Vector3 pos)
        {
            if (TryPlace(pos, new Vector2(1.3f, 0.7f)))
            {
                SpawnDumpster(parent, pos, false);
            }
        }

        private void TrySpawnCar(Transform parent, Vector3 pos, float angle)
        {
            if (TryPlace(pos, new Vector2(1.8f, 1f)))
            {
                SpawnCar(parent, pos, angle, false);
            }
        }

        private static Sprite CreateWarehouseSprite(int variant)
        {
            const int w = 72;
            const int h = 36;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color[] walls =
            {
                new Color(0.78f, 0.76f, 0.72f),
                new Color(0.74f, 0.7f, 0.66f),
                new Color(0.69f, 0.73f, 0.77f),
            };
            Color wall = walls[variant % walls.Length];
            Color roof = new Color(0.34f, 0.22f, 0.2f);
            Color trim = new Color(0.5f, 0.52f, 0.56f);

            FillRect(px, w, 4, 4, 64, 20, wall);
            FillRect(px, w, 0, 24, 72, 5, roof);
            FillRect(px, w, 6, 8, 60, 2, trim);
            FillRect(px, w, 8, 13, 10, 6, new Color(0.48f, 0.52f, 0.56f));
            FillRect(px, w, 27, 13, 10, 6, new Color(0.48f, 0.52f, 0.56f));
            FillRect(px, w, 46, 13, 10, 6, new Color(0.48f, 0.52f, 0.56f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateDoorSprite()
        {
            const int w = 20;
            const int h = 14;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 16, 10, new Color(0.55f, 0.57f, 0.62f));
            FillRect(px, w, 3, 4, 14, 1, new Color(0.72f, 0.74f, 0.78f));
            FillRect(px, w, 3, 7, 14, 1, new Color(0.72f, 0.74f, 0.78f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateVentSprite()
        {
            const int w = 18;
            const int h = 10;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 14, 6, new Color(0.38f, 0.4f, 0.44f));
            FillRect(px, w, 4, 4, 10, 1, new Color(0.62f, 0.64f, 0.68f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateContainerSprite()
        {
            const int w = 40;
            const int h = 18;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 36, 14, Color.white);
            FillRect(px, w, 5, 4, 2, 10, new Color(0.84f, 0.84f, 0.84f));
            FillRect(px, w, 12, 4, 2, 10, new Color(0.84f, 0.84f, 0.84f));
            FillRect(px, w, 19, 4, 2, 10, new Color(0.84f, 0.84f, 0.84f));
            FillRect(px, w, 26, 4, 2, 10, new Color(0.84f, 0.84f, 0.84f));
            FillRect(px, w, 33, 4, 2, 10, new Color(0.84f, 0.84f, 0.84f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateTankSprite()
        {
            const int w = 34;
            const int h = 22;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 3, 4, 28, 14, new Color(0.6f, 0.62f, 0.66f));
            FillRect(px, w, 8, 10, 18, 2, new Color(0.74f, 0.76f, 0.8f));
            FillRect(px, w, 6, 17, 22, 2, new Color(0.92f, 0.74f, 0.18f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreateDockPlatformSprite()
        {
            const int w = 48;
            const int h = 16;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            FillRect(px, w, 2, 2, 44, 10, new Color(0.6f, 0.63f, 0.68f));
            FillRect(px, w, 2, 12, 44, 2, new Color(0.38f, 0.4f, 0.44f));
            return CreateSprite(tex, px);
        }

        private static Sprite CreatePipeRackSprite()
        {
            const int w = 48;
            const int h = 12;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            Color pipe = new Color(0.44f, 0.45f, 0.48f);
            FillRect(px, w, 2, 3, 44, 2, pipe);
            FillRect(px, w, 2, 7, 44, 2, pipe);
            FillRect(px, w, 10, 0, 2, 12, new Color(0.34f, 0.35f, 0.38f));
            FillRect(px, w, 36, 0, 2, 12, new Color(0.34f, 0.35f, 0.38f));
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
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.18f), 16f);
        }
    }
}
