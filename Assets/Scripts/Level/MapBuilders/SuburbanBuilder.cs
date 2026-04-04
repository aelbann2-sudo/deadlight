using Deadlight.Visuals;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    /// <summary>
    /// Detached-home suburban layout with wider spacing, fewer structures, and more lawn/open space.
    /// </summary>
    public class SuburbanBuilder : MapBuilderBase
    {
        private static readonly Vector3[] RoadCarPositions =
        {
            new Vector3(-9f, 0.9f, 0f),
            new Vector3(10f, -0.9f, 0f),
            new Vector3(-20f, 5f, 0f),
            new Vector3(22f, 6f, 0f),
            new Vector3(-17f, -22f, 0f),
            new Vector3(13f, -22f, 0f),
        };

        private static readonly Vector3[] StreetPostPositions =
        {
            new Vector3(-26f, 2.9f, 0f),
            new Vector3(-4f, 2.9f, 0f),
            new Vector3(18f, -2.9f, 0f),
            new Vector3(-20f, 18f, 0f),
            new Vector3(22f, 18f, 0f),
            new Vector3(-20f, -14f, 0f),
            new Vector3(22f, -14f, 0f),
            new Vector3(0f, -20f, 0f),
            new Vector3(-22f, -22f, 0f),
            new Vector3(8f, -22f, 0f),
        };

        private static readonly Vector3[] TreePositions =
        {
            new Vector3(-33f, 31f, 0f),
            new Vector3(-24f, 33f, 0f),
            new Vector3(-12f, 33f, 0f),
            new Vector3(1f, 33f, 0f),
            new Vector3(14f, 34f, 0f),
            new Vector3(34f, 32f, 0f),
            new Vector3(-34f, 20f, 0f),
            new Vector3(-12f, 18f, 0f),
            new Vector3(6f, 18f, 0f),
            new Vector3(33f, 14f, 0f),
            new Vector3(-34f, 8f, 0f),
            new Vector3(31f, 8f, 0f),
            new Vector3(-34f, -4f, 0f),
            new Vector3(34f, -6f, 0f),
            new Vector3(-34f, -16f, 0f),
            new Vector3(34f, -16f, 0f),
            new Vector3(-32f, -30f, 0f),
            new Vector3(-18f, -33f, 0f),
            new Vector3(0f, -33f, 0f),
            new Vector3(15f, -33f, 0f),
            new Vector3(32f, -31f, 0f),
            new Vector3(-7f, 11f, 0f),
            new Vector3(8f, 12f, 0f),
        };

        private static readonly Vector3[] BushPositions =
        {
            new Vector3(-14f, 14f, 0f),
            new Vector3(-8f, 15f, 0f),
            new Vector3(-6f, -12f, 0f),
            new Vector3(10f, -12f, 0f),
            new Vector3(19f, 30f, 0f),
            new Vector3(30f, 18f, 0f),
        };

        private static readonly Vector3[] CenterTreePositions =
        {
            new Vector3(-4f, 14f, 0f),
            new Vector3(0f, 13f, 0f),
            new Vector3(4.5f, 14.2f, 0f),
            new Vector3(-2.5f, 9f, 0f),
            new Vector3(3.2f, 8.3f, 0f),
        };

        protected override string GetMapName() => "SuburbanDistrict";

        public override bool OwnsLandmarks => true;

        public override void BuildLandmarks(Transform parent)
        {
            SuburbanLandmarks.Create(parent);
        }

        protected override void BuildLayout()
        {
            ReserveLandmarkSpaces();
            BuildDetachedHomes();
            BuildNeighborhoodEdges();
            BuildUtilityLots();
            ScatterCenterTrees();
            PlaceRoadCars();
            PlaceStreetPosts();
            ScatterTrees();
            ScatterBushes();
            ScatterRocks();
        }

        private void ReserveLandmarkSpaces()
        {
            ReserveSpace(SuburbanLayout.CheckpointPosition, new Vector2(8.5f, 5f));
            ReserveSpace(SuburbanLayout.GasStationPosition, new Vector2(8f, 6f));
            ReserveSpace(SuburbanLayout.PlaygroundPosition, new Vector2(6.5f, 5f));
            ReserveSpace(SuburbanLayout.SchoolBusPosition, new Vector2(6f, 3.5f));
            ReserveSpace(SuburbanLayout.AbandonedBusPosition, new Vector2(6f, 3.5f));
            ReserveSpace(SuburbanLayout.CulDeSacPosition, new Vector2(8f, 8f));
            ReserveSpace(SuburbanLayout.SchoolPosition, new Vector2(7f, 4.5f));
            ReserveSpace(SuburbanLayout.HospitalPosition, new Vector2(7.5f, 4.6f));

            foreach (Vector3 pos in SuburbanLayout.StreetlightPositions)
            {
                ReserveSpace(pos, new Vector2(0.8f, 0.8f));
            }
        }

        private void BuildDetachedHomes()
        {
            var homes = CreateDistrictRoot("DetachedHomes");
            foreach (SuburbanLayout.DetachedLot lot in SuburbanLayout.DetachedLots)
            {
                SpawnDetachedLot(homes, lot);
            }
        }

        private void SpawnDetachedLot(Transform parent, SuburbanLayout.DetachedLot lot)
        {
            if (TryPlace(lot.HousePosition, new Vector2(2.5f, 1.8f)))
            {
                SpawnHouse(parent, lot.HousePosition, lot.Variant);
            }

            if (TryPlace(lot.GaragePosition, new Vector2(1.6f, 1.2f)))
            {
                SpawnGarage(parent, lot.GaragePosition, lot.Variant);
            }

            TrySpawnLotTree(parent, lot);
            TrySpawnLotRock(parent, lot);

            if (lot.HasBackFence)
            {
                SpawnBackFence(parent, lot);
            }
        }

        private void BuildNeighborhoodEdges()
        {
            var edges = CreateDistrictRoot("NeighborhoodEdges");
            Color fenceColor = new Color(0.62f, 0.56f, 0.42f);

            SpawnFence(edges, new Vector3(-35f, 24.5f, 0f), new Vector3(-35f, 4f, 0f), fenceColor);
            SpawnFence(edges, new Vector3(-34f, -14f, 0f), new Vector3(-34f, -30f, 0f), fenceColor);
            SpawnFence(edges, new Vector3(35f, 20f, 0f), new Vector3(35f, 33f, 0f), fenceColor);
            SpawnFence(edges, new Vector3(32f, -31f, 0f), new Vector3(35f, -31f, 0f), fenceColor);

            TrySpawnTree(edges, new Vector3(-16f, 12f, 0f));
            TrySpawnTree(edges, new Vector3(-10f, 12.5f, 0f));
            TrySpawnTree(edges, new Vector3(4f, 13f, 0f));
            TrySpawnRock(edges, new Vector3(-12f, 11.5f, 0f));
        }

        private void BuildUtilityLots()
        {
            var lots = CreateDistrictRoot("UtilityLots");
            Color fenceColor = new Color(0.64f, 0.58f, 0.44f);

            SpawnFence(lots, new Vector3(25.8f, -23.1f, 0f), new Vector3(33f, -23.1f, 0f), fenceColor);
            SpawnFence(lots, new Vector3(33f, -23.1f, 0f), new Vector3(33f, -17.2f, 0f), fenceColor);

            TrySpawnDumpster(lots, new Vector3(32.5f, 0.4f, 0f));
            TrySpawnDumpster(lots, new Vector3(31.5f, -22.6f, 0f));
            TrySpawnBarrel(lots, new Vector3(27.8f, 0.7f, 0f));
        }

        private void PlaceRoadCars()
        {
            var cars = CreateDistrictRoot("RoadCars");
            for (int i = 0; i < RoadCarPositions.Length; i++)
            {
                Vector3 pos = RoadCarPositions[i];
                float angle = Mathf.Abs(pos.y) < 2f || Mathf.Abs(pos.y + 22f) < 1.5f ? 0f : 90f;
                TrySpawnRoadCar(cars, pos, angle);
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

        private void ScatterTrees()
        {
            var trees = CreateDistrictRoot("Trees");
            foreach (Vector3 pos in TreePositions)
            {
                TrySpawnTree(trees, pos);
            }
        }

        private void ScatterCenterTrees()
        {
            var trees = CreateDistrictRoot("CenterTrees");
            foreach (Vector3 pos in CenterTreePositions)
            {
                TrySpawnTree(trees, pos);
            }
        }

        private void ScatterBushes()
        {
            var bushes = CreateDistrictRoot("Bushes");
            foreach (Vector3 pos in BushPositions)
            {
                if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.8f, 0.8f)))
                {
                    SpawnBush(bushes, pos, false);
                }
            }
        }

        private void ScatterRocks()
        {
            var rocks = CreateDistrictRoot("Rocks");
            TrySpawnRock(rocks, new Vector3(-24.5f, 31f, 0f));
            TrySpawnRock(rocks, new Vector3(14f, 30.5f, 0f));
            TrySpawnRock(rocks, new Vector3(-33.5f, 0f, 0f));
            TrySpawnRock(rocks, new Vector3(34f, -2f, 0f));
            TrySpawnRock(rocks, new Vector3(-4f, -33f, 0f));
        }

        private Transform CreateDistrictRoot(string name)
        {
            var district = new GameObject(name).transform;
            district.SetParent(root);
            return district;
        }

        private void SpawnHouse(Transform parent, Vector3 pos, int variant)
        {
            var house = new GameObject("House");
            house.transform.SetParent(parent);
            house.transform.position = pos;

            var sr = house.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateHouseSprite(variant % 3);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);

            var col = house.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(col, sr.sprite, house.transform.localScale, 0.9f, 0.92f);
        }

        private void SpawnGarage(Transform parent, Vector3 pos, int variant)
        {
            var garage = new GameObject("Garage");
            garage.transform.SetParent(parent);
            garage.transform.position = pos;

            var sr = garage.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateGarageSprite(variant % 3);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);

            var col = garage.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(col, sr.sprite, garage.transform.localScale, 0.9f, 0.9f);
        }

        private void SpawnBackFence(Transform parent, SuburbanLayout.DetachedLot lot)
        {
            Vector2 drivewayCenter = lot.DrivewayRect.center;
            Vector2 awayFromRoad = new Vector2(lot.HousePosition.x - drivewayCenter.x, lot.HousePosition.y - drivewayCenter.y);
            Color fenceColor = new Color(0.62f, 0.56f, 0.42f);

            if (Mathf.Abs(awayFromRoad.x) > Mathf.Abs(awayFromRoad.y))
            {
                float sign = Mathf.Sign(awayFromRoad.x);
                Vector3 from = lot.HousePosition + new Vector3(sign * 3f, -1.4f, 0f);
                Vector3 to = lot.HousePosition + new Vector3(sign * 3f, 1.6f, 0f);
                SpawnFence(parent, from, to, fenceColor);
                return;
            }

            float ySign = Mathf.Sign(awayFromRoad.y);
            Vector3 left = lot.HousePosition + new Vector3(-2.6f, ySign * 2.5f, 0f);
            Vector3 right = lot.HousePosition + new Vector3(2.6f, ySign * 2.5f, 0f);
            SpawnFence(parent, left, right, fenceColor);
        }

        private void TrySpawnLotTree(Transform parent, SuburbanLayout.DetachedLot lot)
        {
            Vector2 drivewayCenter = lot.DrivewayRect.center;
            Vector2 awayFromRoad = new Vector2(lot.HousePosition.x - drivewayCenter.x, lot.HousePosition.y - drivewayCenter.y).normalized;
            Vector2 sideways = new Vector2(-awayFromRoad.y, awayFromRoad.x) * (lot.GarageOnRight ? -1.6f : 1.6f);
            Vector3 pos = lot.HousePosition + new Vector3(awayFromRoad.x * 2.6f + sideways.x, awayFromRoad.y * 2.6f + sideways.y, 0f);
            TrySpawnTree(parent, pos);
        }

        private void TrySpawnLotRock(Transform parent, SuburbanLayout.DetachedLot lot)
        {
            Vector2 drivewayCenter = lot.DrivewayRect.center;
            Vector2 awayFromRoad = new Vector2(lot.HousePosition.x - drivewayCenter.x, lot.HousePosition.y - drivewayCenter.y).normalized;
            Vector3 pos = lot.HousePosition + new Vector3(-awayFromRoad.y * 1.8f, awayFromRoad.x * 1.8f, 0f);
            TrySpawnRock(parent, pos);
        }

        private void TrySpawnTree(Transform parent, Vector3 pos)
        {
            if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.75f, 0.75f)))
            {
                SpawnTree(parent, pos, false);
            }
        }

        private void TrySpawnRock(Transform parent, Vector3 pos)
        {
            if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.8f, 0.8f)))
            {
                SpawnRock(parent, pos, false);
            }
        }

        private void TrySpawnBarrel(Transform parent, Vector3 pos)
        {
            if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.65f, 0.65f)))
            {
                SpawnBarrel(parent, pos, false, false);
            }
        }

        private void TrySpawnDumpster(Transform parent, Vector3 pos)
        {
            if (!IsRoad(pos) && TryPlace(pos, new Vector2(1.3f, 0.7f)))
            {
                SpawnDumpster(parent, pos, false);
            }
        }

        private void TrySpawnRoadCar(Transform parent, Vector3 pos, float angle)
        {
            if (IsRoad(pos) && TryPlace(pos, new Vector2(1.8f, 1f)))
            {
                SpawnCar(parent, pos, angle, false);
            }
        }

        private void SpawnStreetPost(Transform parent, Vector3 pos)
        {
            var post = new GameObject("StreetPost");
            post.transform.SetParent(parent);
            post.transform.position = pos;

            var sr = post.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateStreetPostSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 4;
            sr.color = new Color(0.62f, 0.6f, 0.58f);
        }
    }
}
