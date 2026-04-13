using UnityEngine;
using Deadlight.Visuals;

namespace Deadlight.Level.MapBuilders
{
    /// <summary>
    /// Town Center: mixed-use commercial core with plaza-adjacent storefronts, civic lots,
    /// pocket parks, parking courts, service yards, and more deliberate landmark clearings.
    /// </summary>
    public class TownCenterBuilder : MapBuilderBase
    {
        protected override string GetMapName() => "TownCenter";
        public override bool OwnsLandmarks => true;

        public override void BuildLandmarks(Transform parent)
        {
            TownCenterLandmarks.Create(parent);
        }

        protected override void BuildLayout()
        {
            int hw = Mathf.FloorToInt(boundW);
            int hh = Mathf.FloorToInt(boundH);

            ReserveLandmarkSpaces();
            BuildCentralPlaza();
            BuildDistrictBlocks(hw, hh);
            BuildConnectorAlleys(hw, hh);
            BuildDeadEndPockets(hw, hh);
            BuildPerimeterEdges(hw, hh);
            PlaceStreetParking(hw, hh);
            PlaceStreetFurniture(hw, hh);
            PlaceStreetPosts();
            ScatterDecor(hw, hh);
        }

        private void ReserveLandmarkSpaces()
        {
            ReserveSpace(TownCenterLandmarks.CrashSitePosition, new Vector2(8.5f, 4.5f));
            ReserveSpace(TownCenterLandmarks.MilitaryCheckpointPosition, new Vector2(7f, 4.5f));
            ReserveSpace(TownCenterLandmarks.GasStationPosition, new Vector2(6f, 5f));
            ReserveSpace(TownCenterLandmarks.DinerPosition, new Vector2(5.5f, 4f));
            ReserveSpace(TownCenterLandmarks.SchoolPosition, new Vector2(7.5f, 5.5f));
            ReserveSpace(TownCenterLandmarks.HospitalPosition, new Vector2(7.5f, 5.5f));

            foreach (Vector3 pos in TownCenterLandmarks.StreetlightPositions)
            {
                ReserveStreetlightPad(pos);
            }
        }

        private void ReserveStreetlightPad(Vector3 pos)
        {
            ReserveSpace(pos, new Vector2(0.9f, 0.9f));
        }

        private void BuildCentralPlaza()
        {
            var plaza = new GameObject("CentralPlaza").transform;
            plaza.SetParent(root);

            var fountain = new GameObject("Fountain");
            fountain.transform.SetParent(plaza);
            fountain.transform.position = Vector3.zero;
            var fountainRenderer = fountain.AddComponent<SpriteRenderer>();
            fountainRenderer.sprite = ProceduralSpriteGenerator.CreateFountainSprite();
            fountainRenderer.sortingOrder = 6;
            var fountainCollider = fountain.AddComponent<CircleCollider2D>();
            fountainCollider.radius = 0.9f;
            RegisterPlacement(Vector3.zero, new Vector2(2f, 2f));

            Vector3[] benchPositions =
            {
                new Vector3(-4.5f, 0f, 0f),
                new Vector3(4.5f, 0f, 0f),
                new Vector3(0f, -4.5f, 0f),
                new Vector3(0f, 4.5f, 0f)
            };

            foreach (Vector3 pos in benchPositions)
            {
                var bench = new GameObject("Bench");
                bench.transform.SetParent(plaza);
                bench.transform.position = pos;
                var sr = bench.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
                sr.sortingOrder = 4;
                sr.color = new Color(0.5f, 0.34f, 0.22f);
                var col = bench.AddComponent<BoxCollider2D>();
                MapFootprintCollider.ApplyCustomSpriteFootprint(col, sr.sprite, bench.transform.localScale, new Vector2(0.9f, 0.35f));
                RegisterPlacement(pos, new Vector2(0.9f, 0.35f));
            }

            Vector3[] planterPositions =
            {
                new Vector3(-5.5f, -5.5f, 0f),
                new Vector3(5.5f, -5.5f, 0f),
                new Vector3(-5.5f, 5.5f, 0f),
                new Vector3(5.5f, 5.5f, 0f)
            };

            foreach (Vector3 pos in planterPositions)
            {
                if (TryPlace(pos, new Vector2(0.75f, 0.75f)))
                {
                    SpawnTree(plaza, pos, false);
                }
            }

            Vector3[] coverPositions =
            {
                new Vector3(-2.5f, 3f, 0f),
                new Vector3(2.5f, 3f, 0f),
                new Vector3(-2.5f, -3f, 0f),
                new Vector3(2.5f, -3f, 0f)
            };

            foreach (Vector3 pos in coverPositions)
            {
                SpawnRock(plaza, pos);
            }
        }

        private void BuildDistrictBlocks(int hw, int hh)
        {
            var districts = new GameObject("DistrictBlocks").transform;
            districts.SetParent(root);

            float spacing = config.streetGridSpacing;
            for (float bx = -hw + spacing; bx < hw; bx += spacing)
            {
                for (float by = -hh + spacing; by < hh; by += spacing)
                {
                    Vector3 center = new Vector3(bx, by, 0f);
                    if (TownCenterLayout.IsCentralPlaza(center))
                    {
                        continue;
                    }

                    TownCenterBlockStyle style = TownCenterLayout.GetBlockStyle(config, new Vector2(center.x, center.y));
                    var block = new GameObject(style.ToString()).transform;
                    block.SetParent(districts);
                    block.position = center;

                    Color tint = GetBuildingTint(style, center);
                    switch (style)
                    {
                        case TownCenterBlockStyle.PlazaEdge:
                            BuildPlazaEdgeBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.Commercial:
                            BuildCommercialBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.Courtyard:
                            BuildCourtyardBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.PocketPark:
                            BuildPocketParkBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.ParkingLot:
                            BuildParkingLotBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.ServiceYard:
                            BuildServiceYardBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.CivicLot:
                            BuildCivicBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.FuelLot:
                            BuildFuelBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.CheckpointLot:
                            BuildCheckpointBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.CrashCorridor:
                            BuildCrashCorridorBlock(block, center, tint);
                            break;
                        case TownCenterBlockStyle.SchoolLot:
                            BuildSchoolLot(block, center);
                            break;
                        case TownCenterBlockStyle.HospitalLot:
                            BuildHospitalLot(block, center);
                            break;
                    }

                    AddOuterRingAccent(block, center, style);
                }
            }
        }

        private void BuildPlazaEdgeBlock(Transform block, Vector3 center, Color tint)
        {
            bool eastWest = Mathf.Abs(center.x) > Mathf.Abs(center.y);
            float outerSign = eastWest ? Mathf.Sign(center.x) : Mathf.Sign(center.y);

            if (eastWest)
            {
                float outerX = center.x + outerSign * 1.9f;
                SpawnBuilding(block, new Vector3(outerX, center.y + 1.5f, 0f), new Vector2(1.8f, 2.2f), 0, tint, "PlazaShop_A");
                SpawnBuilding(block, new Vector3(outerX, center.y - 1.5f, 0f), new Vector2(1.8f, 2.1f), 1, ShiftTint(tint, -0.05f), "PlazaShop_B");
                SpawnBarrel(block, center + new Vector3(-outerSign * 0.8f, 1.2f, 0f), false);
                SpawnRock(block, center + new Vector3(-outerSign * 0.9f, -1.2f, 0f));
                if (TryPlace(center + new Vector3(-outerSign * 2.2f, 0f, 0f), new Vector2(0.75f, 0.75f)))
                {
                    SpawnTree(block, center + new Vector3(-outerSign * 2.2f, 0f, 0f), false);
                }
            }
            else
            {
                float outerY = center.y + outerSign * 1.9f;
                SpawnBuilding(block, new Vector3(center.x - 1.5f, outerY, 0f), new Vector2(1.8f, 2.1f), 0, tint, "PlazaShop_A");
                SpawnBuilding(block, new Vector3(center.x + 1.5f, outerY, 0f), new Vector2(1.8f, 2.2f), 1, ShiftTint(tint, -0.05f), "PlazaShop_B");
                SpawnBarrel(block, center + new Vector3(1f, -outerSign * 0.8f, 0f), false);
                SpawnRock(block, center + new Vector3(-1.1f, -outerSign * 0.9f, 0f));
                if (TryPlace(center + new Vector3(0f, -outerSign * 2.2f, 0f), new Vector2(0.75f, 0.75f)))
                {
                    SpawnTree(block, center + new Vector3(0f, -outerSign * 2.2f, 0f), false);
                }
            }
        }

        private void BuildCommercialBlock(Transform block, Vector3 center, Color tint)
        {
            bool horizontal = Random.value > 0.5f;
            if (horizontal)
            {
                SpawnBuilding(block, center + new Vector3(-1.9f, 1.9f, 0f), new Vector2(1.8f, 2.2f), Random.Range(0, 3), tint, "Cafe");
                SpawnBuilding(block, center + new Vector3(1.5f, -1.4f, 0f), new Vector2(2.5f, 2f), Random.Range(0, 3), ShiftTint(tint, 0.03f), "RowStore");
                SpawnBarrel(block, center + new Vector3(2.4f, -2.2f, 0f), false);
                SpawnBarrel(block, center + new Vector3(2f, 1.6f, 0f), false);
            }
            else
            {
                SpawnBuilding(block, center + new Vector3(1.7f, 1.8f, 0f), new Vector2(1.8f, 2.2f), Random.Range(0, 3), ShiftTint(tint, -0.04f), "CornerShop");
                SpawnBuilding(block, center + new Vector3(-1.2f, -1.7f, 0f), new Vector2(2.4f, 2.1f), Random.Range(0, 3), tint, "RowStore");
                SpawnBarrel(block, center + new Vector3(-2.3f, -2.1f, 0f), false);
                SpawnBarrel(block, center + new Vector3(-2f, 1.4f, 0f), false);
            }

            if (TryPlace(center + new Vector3(0f, 2.6f, 0f), new Vector2(1.4f, 0.9f)))
            {
                SpawnDumpster(block, center + new Vector3(0f, 2.6f, 0f), false);
            }
        }

        private void BuildCourtyardBlock(Transform block, Vector3 center, Color tint)
        {
            bool horizontal = Mathf.Abs(center.x) > Mathf.Abs(center.y);
            if (horizontal)
            {
                SpawnBuilding(block, center + new Vector3(-2.4f, 0f, 0f), new Vector2(1.8f, 2.2f), 0, tint, "CourtWest");
                SpawnBuilding(block, center + new Vector3(1.7f, 2f, 0f), new Vector2(2.3f, 2f), 2, ShiftTint(tint, 0.03f), "CourtNorth");
            }
            else
            {
                SpawnBuilding(block, center + new Vector3(0f, 2.4f, 0f), new Vector2(2.4f, 2f), 0, tint, "CourtNorth");
                SpawnBuilding(block, center + new Vector3(-2.2f, -0.8f, 0f), new Vector2(1.7f, 2.2f), 2, ShiftTint(tint, 0.03f), "CourtWest");
            }

            SpawnRock(block, center + new Vector3(0.2f, 0.4f, 0f));
            SpawnBarrel(block, center + new Vector3(-2.3f, -2.1f, 0f), false);
            if (TryPlace(center + new Vector3(1.5f, -0.9f, 0f), new Vector2(1.1f, 1.1f)))
            {
                SpawnTree(block, center + new Vector3(1.5f, -0.9f, 0f), false);
            }
        }

        private void BuildPocketParkBlock(Transform block, Vector3 center, Color tint)
        {
            bool northSouth = Mathf.Abs(center.y) >= Mathf.Abs(center.x);
            if (northSouth)
            {
                float buildingY = center.y - Mathf.Sign(center.y == 0f ? 1f : center.y) * 2.2f;
                SpawnBuilding(block, new Vector3(center.x, buildingY, 0f), new Vector2(2.4f, 2f), 1, tint, "ParkShop_A");
            }
            else
            {
                float buildingX = center.x - Mathf.Sign(center.x) * 2.2f;
                SpawnBuilding(block, new Vector3(buildingX, center.y, 0f), new Vector2(2.4f, 2f), 1, tint, "ParkShop_A");
            }

            Vector3[] parkPositions =
            {
                center + new Vector3(-1.7f, 1.4f, 0f),
                center + new Vector3(1.6f, 1.1f, 0f),
                center + new Vector3(0.3f, -1.4f, 0f)
            };

            foreach (Vector3 pos in parkPositions)
            {
                if (TryPlace(pos, new Vector2(1.15f, 1.15f)))
                {
                    SpawnTree(block, pos, false);
                }
            }

            SpawnRock(block, center + new Vector3(-0.3f, 0.1f, 0f));
            if (Random.value > 0.55f)
            {
                SpawnBarrel(block, center + new Vector3(2.3f, -2.2f, 0f), false);
            }
        }

        private void BuildParkingLotBlock(Transform block, Vector3 center, Color tint)
        {
            Vector3[] carPositions =
            {
                center + new Vector3(-1.8f, 0.8f, 0f),
                center + new Vector3(1.7f, -0.4f, 0f)
            };

            foreach (Vector3 pos in carPositions)
            {
                if (TryPlace(pos, new Vector2(1.8f, 1f)))
                {
                    SpawnCar(block, pos, Random.value > 0.5f ? 0f : 180f, false);
                }
            }

            if (Random.value > 0.45f)
            {
                SpawnBuilding(block, center + new Vector3(0f, 2.5f, 0f), new Vector2(2.5f, 1.9f), Random.Range(0, 3), tint, "LotShops");
            }

            SpawnDumpster(block, center + new Vector3(0f, -2.4f, 0f));
            SpawnRock(block, center + new Vector3(-2.2f, 2.2f, 0f));
        }

        private void BuildServiceYardBlock(Transform block, Vector3 center, Color tint)
        {
            SpawnBuilding(block, center + new Vector3(-2.1f, 1.9f, 0f), new Vector2(1.8f, 2.2f), 2, tint, "ServiceShop");

            Color fenceTint = new Color(0.44f, 0.42f, 0.4f);
            SpawnFence(block, center + new Vector3(-2.8f, -0.8f, 0f), center + new Vector3(2.5f, -0.8f, 0f), fenceTint);
            SpawnFence(block, center + new Vector3(2.5f, -0.8f, 0f), center + new Vector3(2.5f, 2.4f, 0f), fenceTint);

            SpawnDumpster(block, center + new Vector3(0.8f, 0.9f, 0f));
            SpawnBarrel(block, center + new Vector3(-0.4f, -1.9f, 0f), false);
            SpawnBarrel(block, center + new Vector3(1.5f, 0.2f, 0f), Random.value > 0.8f);
            if (Random.value > 0.5f)
            {
                SpawnBarrel(block, center + new Vector3(1.6f, -2.1f, 0f), false);
            }
        }

        private void BuildCivicBlock(Transform block, Vector3 center, Color tint)
        {
            SpawnBuilding(block, center + new Vector3(0f, 2f, 0f), new Vector2(3.1f, 2.1f), 2, ShiftTint(tint, 0.08f), "CivicHall");

            if (TryPlace(center + new Vector3(1.4f, -0.6f, 0f), new Vector2(1.8f, 1f)))
            {
                SpawnCar(block, center + new Vector3(1.4f, -0.6f, 0f), 0f, false);
            }

            SpawnRock(block, center + new Vector3(-1.2f, -0.2f, 0f));
            if (Random.value > 0.6f)
            {
                SpawnBarrel(block, center + new Vector3(2.3f, 2.3f, 0f), false);
            }
        }

        private void BuildFuelBlock(Transform block, Vector3 center, Color tint)
        {
            SpawnBuilding(block, center + new Vector3(-1.8f, 2.1f, 0f), new Vector2(2.1f, 2.2f), 1, ShiftTint(tint, -0.02f), "ServiceStore");

            if (TryPlace(center + new Vector3(-1.3f, -0.2f, 0f), new Vector2(1.8f, 1f)))
            {
                SpawnCar(block, center + new Vector3(-1.3f, -0.2f, 0f), 0f, false);
            }

            if (TryPlace(center + new Vector3(1.5f, -0.5f, 0f), new Vector2(1.8f, 1f)))
            {
                SpawnCar(block, center + new Vector3(1.5f, -0.5f, 0f), 180f, false);
            }

            if (Random.value > 0.4f)
            {
                SpawnBarrel(block, center + new Vector3(-2.2f, 2.6f, 0f), false);
            }
            SpawnDumpster(block, center + new Vector3(2.2f, -2.1f, 0f));
        }

        private void BuildCheckpointBlock(Transform block, Vector3 center, Color tint)
        {
            SpawnBuilding(block, center + new Vector3(2.1f, 1.9f, 0f), new Vector2(1.8f, 2.1f), 2, ShiftTint(tint, -0.06f), "Barracks");

            Color fenceTint = new Color(0.5f, 0.47f, 0.42f);
            SpawnFence(block, center + new Vector3(-2.8f, 0f, 0f), center + new Vector3(0.8f, 0f, 0f), fenceTint);
            SpawnBarrel(block, center + new Vector3(-1.4f, 1.4f, 0f), false);
            SpawnBarrel(block, center + new Vector3(-1.1f, -1.5f, 0f), false);
        }

        private void BuildCrashCorridorBlock(Transform block, Vector3 center, Color tint)
        {
            float sideSign = center.x < 0f ? -1f : 1f;
            if (Mathf.Abs(center.x) > 5.1f)
            {
                SpawnBuilding(block, center + new Vector3(2f * sideSign, -1.8f, 0f), new Vector2(1.8f, 2.2f), Random.Range(0, 3), tint, "CornerBuilding");
            }

            if (TryPlace(center + new Vector3(2f * sideSign, 0.1f, 0f), new Vector2(1.8f, 1f)))
            {
                SpawnCar(block, center + new Vector3(2f * sideSign, 0.1f, 0f), 90f, false);
            }

            SpawnBarrel(block, center + new Vector3(0.4f * sideSign, 2.2f, 0f), false);
            SpawnBarrel(block, center + new Vector3(-0.6f * sideSign, 1.4f, 0f), false);
        }

        private void BuildSchoolLot(Transform block, Vector3 center)
        {
            if (TryPlace(center + new Vector3(-2.5f, -2f, 0f), new Vector2(1.15f, 1.15f)))
            {
                SpawnTree(block, center + new Vector3(-2.5f, -2f, 0f), false);
            }

            if (TryPlace(center + new Vector3(2.5f, -2f, 0f), new Vector2(1.15f, 1.15f)))
            {
                SpawnTree(block, center + new Vector3(2.5f, -2f, 0f), false);
            }
        }

        private void BuildHospitalLot(Transform block, Vector3 center)
        {
            if (TryPlace(center + new Vector3(-2.4f, -2.1f, 0f), new Vector2(1.8f, 1f)))
            {
                SpawnCar(block, center + new Vector3(-2.4f, -2.1f, 0f), 0f, false);
            }

            if (TryPlace(center + new Vector3(2.5f, -2.1f, 0f), new Vector2(1.15f, 1.15f)))
            {
                SpawnTree(block, center + new Vector3(2.5f, -2.1f, 0f), false);
            }
        }

        private void AddOuterRingAccent(Transform block, Vector3 center, TownCenterBlockStyle style)
        {
            if (Mathf.Abs(center.x) < 25f && Mathf.Abs(center.y) < 25f)
            {
                return;
            }

            switch (style)
            {
                case TownCenterBlockStyle.PocketPark:
                    if (TryPlace(center + new Vector3(2.2f, 2f, 0f), new Vector2(1.15f, 1.15f)))
                    {
                        SpawnTree(block, center + new Vector3(2.2f, 2f, 0f), false);
                    }
                    break;
                case TownCenterBlockStyle.ParkingLot:
                    if (TryPlace(center + new Vector3(-2.1f, 2.1f, 0f), new Vector2(1.8f, 1f)))
                    {
                        SpawnCar(block, center + new Vector3(-2.1f, 2.1f, 0f), 90f, false);
                    }
                    break;
                case TownCenterBlockStyle.ServiceYard:
                    if (TryPlace(center + new Vector3(2.1f, 2f, 0f), new Vector2(1.3f, 0.7f)))
                    {
                        SpawnDumpster(block, center + new Vector3(2.1f, 2f, 0f), false);
                    }
                    break;
            }
        }

        private void BuildConnectorAlleys(int hw, int hh)
        {
            var connectors = new GameObject("Connectors").transform;
            connectors.SetParent(root);

            float spacing = config.streetGridSpacing;
            for (float by = -hh + spacing; by < hh; by += spacing)
            {
                for (float bx = -hw + spacing * 0.5f; bx < hw - spacing * 0.5f; bx += spacing)
                {
                    Vector3 midpoint = new Vector3(bx, by, 0f);
                    if (TownCenterLayout.IsCentralPlaza(midpoint) || !IsRoad(midpoint))
                    {
                        continue;
                    }

                    if (Random.value < 0.55f)
                    {
                        continue;
                    }

                    Vector3 coverPos = midpoint + new Vector3(0f, Random.Range(-1.1f, 1.1f), 0f);
                    if (TryPlace(coverPos, new Vector2(1.3f, 0.7f)))
                    {
                        SpawnDumpster(connectors, coverPos, false);
                    }

                    if (Random.value > 0.5f)
                    {
                        Vector3 barrelPos = midpoint + new Vector3(Random.Range(-1.4f, -0.7f), -1.1f, 0f);
                        if (TryPlace(barrelPos, new Vector2(0.65f, 0.65f)))
                        {
                            SpawnBarrel(connectors, barrelPos, false, false);
                        }
                    }
                }
            }
        }

        private void BuildPerimeterEdges(int hw, int hh)
        {
            var perimeter = new GameObject("PerimeterDetail").transform;
            perimeter.SetParent(root);

            Vector3[] horizontalCompounds =
            {
                new Vector3(-24f, hh - 3.5f, 0f),
                new Vector3(24f, hh - 3.5f, 0f),
                new Vector3(-24f, -hh + 3.5f, 0f),
                new Vector3(24f, -hh + 3.5f, 0f)
            };

            foreach (Vector3 center in horizontalCompounds)
            {
                BuildPerimeterCompound(perimeter, center, true);
            }

            Vector3[] verticalCompounds =
            {
                new Vector3(-hw + 3.5f, 18f, 0f),
                new Vector3(-hw + 3.5f, -18f, 0f),
                new Vector3(hw - 3.5f, 18f, 0f),
                new Vector3(hw - 3.5f, -18f, 0f)
            };

            foreach (Vector3 center in verticalCompounds)
            {
                BuildPerimeterCompound(perimeter, center, false);
            }
        }

        private void BuildPerimeterCompound(Transform parent, Vector3 center, bool horizontal)
        {
            if (!TryPlace(center, horizontal ? new Vector2(6.8f, 3.6f) : new Vector2(3.6f, 6.8f)))
            {
                return;
            }

            var compound = new GameObject(horizontal ? "PerimeterCompound_H" : "PerimeterCompound_V").transform;
            compound.SetParent(parent);
            compound.position = center;

            Color fenceTint = new Color(0.42f, 0.4f, 0.38f);
            if (horizontal)
            {
                SpawnFence(compound, center + new Vector3(-3f, 1.3f, 0f), center + new Vector3(3f, 1.3f, 0f), fenceTint, true, false);
                SpawnFence(compound, center + new Vector3(-3f, -1.3f, 0f), center + new Vector3(3f, -1.3f, 0f), fenceTint, true, false);
                SpawnFence(compound, center + new Vector3(-3f, -1.3f, 0f), center + new Vector3(-3f, 1.3f, 0f), fenceTint, true, false);
                SpawnBarrel(compound, center + new Vector3(-1.8f, 0.4f, 0f), false, false);
                SpawnDumpster(compound, center + new Vector3(1.6f, -0.3f, 0f), false);
                if (Random.value > 0.45f)
                {
                    SpawnCar(compound, center + new Vector3(0f, 0f, 0f), 0f, false);
                }
            }
            else
            {
                SpawnFence(compound, center + new Vector3(1.3f, -3f, 0f), center + new Vector3(1.3f, 3f, 0f), fenceTint, true, false);
                SpawnFence(compound, center + new Vector3(-1.3f, -3f, 0f), center + new Vector3(-1.3f, 3f, 0f), fenceTint, true, false);
                SpawnFence(compound, center + new Vector3(-1.3f, -3f, 0f), center + new Vector3(1.3f, -3f, 0f), fenceTint, true, false);
                SpawnBarrel(compound, center + new Vector3(0.3f, -1.8f, 0f), false, false);
                SpawnDumpster(compound, center + new Vector3(-0.2f, 1.6f, 0f), false);
                if (Random.value > 0.5f)
                {
                    SpawnCar(compound, center + new Vector3(0.1f, 0f, 0f), 90f, false);
                }
            }
        }

        private void BuildDeadEndPockets(int hw, int hh)
        {
            var pockets = new GameObject("DeadEnds").transform;
            pockets.SetParent(root);

            Color fenceTint = new Color(0.48f, 0.44f, 0.38f);
            Vector3[] deadEndSpots =
            {
                new Vector3(-hw + 5f, hh * 0.55f, 0f),
                new Vector3(hw - 5f, -hh * 0.35f, 0f),
                new Vector3(-hw * 0.25f, hh - 5f, 0f),
                new Vector3(hw * 0.45f, -hh + 5f, 0f)
            };

            foreach (Vector3 spot in deadEndSpots)
            {
                var pocket = new GameObject("DeadEnd").transform;
                pocket.SetParent(pockets);

                bool openRight = spot.x < 0f;
                float sign = openRight ? 1f : -1f;
                float width = 3.2f;
                float depth = 4f;

                SpawnFence(pocket, spot + new Vector3(-width * sign, depth, 0f), spot + new Vector3(-width * sign, -depth, 0f), fenceTint);
                SpawnFence(pocket, spot + new Vector3(-width * sign, depth, 0f), spot + new Vector3(0f, depth, 0f), fenceTint);
                SpawnFence(pocket, spot + new Vector3(-width * sign, -depth, 0f), spot + new Vector3(0f, -depth, 0f), fenceTint);

                SpawnBarrel(pocket, spot + new Vector3(-width * sign * 0.8f, -1.6f, 0f), false);
                SpawnDumpster(pocket, spot + new Vector3(-width * sign * 0.35f, 1.7f, 0f));
                SpawnBarrel(pocket, spot + new Vector3(-width * sign * 0.2f, -1.7f, 0f), Random.value > 0.75f);
            }
        }

        private void PlaceStreetParking(int hw, int hh)
        {
            var parking = new GameObject("StreetParking").transform;
            parking.SetParent(root);

            int count = Mathf.Max(6, config.carCount / 2);
            var placed = new Vector3[count];
            float[] streetLines = { -30f, -20f, -10f, 0f, 10f, 20f, 30f };

            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    bool verticalStreet = Random.value > 0.5f;
                    float line = streetLines[Random.Range(0, streetLines.Length)];
                    float curbOffset = Random.value > 0.5f ? 1.8f : -1.8f;

                    float x;
                    float y;
                    float angle;
                    if (verticalStreet)
                    {
                        x = line + curbOffset;
                        y = Random.Range(-hh + 4f, hh - 4f);
                        angle = 90f + Random.Range(-4f, 4f);
                    }
                    else
                    {
                        x = Random.Range(-hw + 4f, hw - 4f);
                        y = line + curbOffset;
                        angle = Random.Range(-4f, 4f);
                    }

                    Vector3 pos = new Vector3(x, y, 0f);
                    if (TownCenterLayout.IsCentralPlaza(pos))
                    {
                        continue;
                    }

                    if (Mathf.Abs(x - Mathf.Round(x / config.streetGridSpacing) * config.streetGridSpacing) < 2.2f &&
                        Mathf.Abs(y - Mathf.Round(y / config.streetGridSpacing) * config.streetGridSpacing) < 2.2f)
                    {
                        continue;
                    }

                    if (TooClose(pos, placed, 4f) || !TryPlace(pos, new Vector2(1.8f, 1f)))
                    {
                        continue;
                    }

                    SpawnCar(parking, pos, angle, false);
                    placed[i] = pos;
                    break;
                }
            }
        }

        private void PlaceStreetFurniture(int hw, int hh)
        {
            var furniture = new GameObject("StreetFurniture").transform;
            furniture.SetParent(root);

            float spacing = config.streetGridSpacing;
            for (float x = -hw + spacing; x < hw; x += spacing)
            {
                for (float y = -hh + spacing; y < hh; y += spacing)
                {
                    Vector3 corner = new Vector3(x + 2f, y + 2f, 0f);
                    if (TownCenterLayout.IsCentralPlaza(corner) || Random.value > 0.45f)
                    {
                        continue;
                    }

                    if (!TryPlace(corner, new Vector2(0.9f, 0.9f)))
                    {
                        continue;
                    }

                    float roll = Random.value;
                    if (roll < 0.5f)
                    {
                        SpawnBarrel(furniture, corner, false, false);
                    }
                    else
                    {
                        SpawnRock(furniture, corner, false);
                    }
                }
            }
        }

        private void PlaceStreetPosts()
        {
            var posts = new GameObject("StreetPosts").transform;
            posts.SetParent(root);

            Vector3[] postPositions =
            {
                new Vector3(-18f, 22f, 0f),
                new Vector3(12f, 22f, 0f),
                new Vector3(-22f, 12f, 0f),
                new Vector3(18f, 12f, 0f),
                new Vector3(-8f, 18f, 0f),
                new Vector3(22f, -8f, 0f),
                new Vector3(-18f, -12f, 0f),
                new Vector3(8f, -18f, 0f),
                new Vector3(-28f, 6f, 0f),
                new Vector3(28f, -6f, 0f)
            };

            foreach (Vector3 pos in postPositions)
            {
                if (TownCenterLayout.IsCentralPlaza(pos))
                {
                    continue;
                }

                if (TryPlace(pos, new Vector2(0.3f, 0.3f)))
                {
                    SpawnStreetPost(posts, pos);
                }
            }
        }

        private void ScatterDecor(int hw, int hh)
        {
            var decor = new GameObject("Decor").transform;
            decor.SetParent(root);

            Vector3[] curatedTreeSpots =
            {
                new Vector3(-24f, 18f, 0f),
                new Vector3(-24f, -18f, 0f),
                new Vector3(24f, 18f, 0f),
                new Vector3(24f, -18f, 0f),
                new Vector3(-18f, 24f, 0f),
                new Vector3(18f, 24f, 0f),
                new Vector3(-18f, -24f, 0f),
                new Vector3(18f, -24f, 0f),
                new Vector3(-12f, 14f, 0f),
                new Vector3(12f, 14f, 0f),
                new Vector3(-14f, -12f, 0f),
                new Vector3(14f, -12f, 0f),
                new Vector3(-30f, 8f, 0f),
                new Vector3(30f, -8f, 0f)
            };

            foreach (Vector3 pos in curatedTreeSpots)
            {
                if (TryPlace(pos, new Vector2(1.2f, 1.2f)))
                {
                    SpawnTree(decor, pos, false);
                }
            }
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

        private Color GetBuildingTint(TownCenterBlockStyle style, Vector3 center)
        {
            Color baseTint = config.buildingTint;
            Color tint = style switch
            {
                TownCenterBlockStyle.CivicLot => Color.Lerp(baseTint, new Color(0.82f, 0.88f, 0.96f), 0.22f),
                TownCenterBlockStyle.FuelLot => Color.Lerp(baseTint, new Color(0.94f, 0.88f, 0.72f), 0.18f),
                TownCenterBlockStyle.ServiceYard => Color.Lerp(baseTint, new Color(0.74f, 0.74f, 0.78f), 0.2f),
                TownCenterBlockStyle.PocketPark => Color.Lerp(baseTint, new Color(0.88f, 0.84f, 0.78f), 0.14f),
                TownCenterBlockStyle.SchoolLot => Color.Lerp(baseTint, new Color(0.94f, 0.9f, 0.7f), 0.2f),
                TownCenterBlockStyle.HospitalLot => Color.Lerp(baseTint, new Color(0.92f, 0.96f, 0.98f), 0.24f),
                _ => Color.Lerp(baseTint, new Color(0.92f, 0.88f, 0.82f), 0.08f)
            };

            if (center.y > 15f)
            {
                tint = Color.Lerp(tint, new Color(0.96f, 0.93f, 0.88f), 0.06f);
            }

            return tint;
        }

        private Color ShiftTint(Color tint, float amount)
        {
            return new Color(
                Mathf.Clamp01(tint.r + amount),
                Mathf.Clamp01(tint.g + amount),
                Mathf.Clamp01(tint.b + amount),
                tint.a);
        }
    }
}
