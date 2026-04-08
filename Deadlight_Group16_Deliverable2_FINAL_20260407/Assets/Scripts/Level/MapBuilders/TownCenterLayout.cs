using Deadlight.Data;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public enum TownCenterBlockStyle
    {
        PlazaEdge,
        Commercial,
        Courtyard,
        PocketPark,
        ParkingLot,
        ServiceYard,
        CivicLot,
        FuelLot,
        CheckpointLot,
        CrashCorridor,
        SchoolLot,
        HospitalLot
    }

    public static class TownCenterLayout
    {
        private const float PlazaRadius = 7f;
        private const float PlazaEdgeExtent = 8f;
        private const float SidewalkWidth = 1.5f;

        public static int GetTileType(MapConfig config, int x, int y)
        {
            var pos = new Vector2(x, y);
            if (pos.magnitude < PlazaRadius)
            {
                return 2;
            }

            float mainRoadHalf = config != null ? config.mainRoadWidth : 2f;
            float sideRoadHalf = config != null ? config.sideRoadWidth : 1.5f;
            float spacing = config != null ? config.streetGridSpacing : 10f;

            if (Mathf.Abs(pos.x) < mainRoadHalf || Mathf.Abs(pos.y) < mainRoadHalf)
            {
                return 3;
            }

            float distToStreetX = DistanceToGridLine(pos.x, spacing);
            float distToStreetY = DistanceToGridLine(pos.y, spacing);
            if (distToStreetX < sideRoadHalf || distToStreetY < sideRoadHalf)
            {
                return 3;
            }

            if (distToStreetX < sideRoadHalf + SidewalkWidth || distToStreetY < sideRoadHalf + SidewalkWidth)
            {
                return 2;
            }

            Vector2 blockCenter = GetNearestBlockCenter(pos, spacing);
            TownCenterBlockStyle style = GetBlockStyle(config, blockCenter);
            Vector2 local = pos - blockCenter;

            return style switch
            {
                TownCenterBlockStyle.PocketPark => GetPocketParkTile(local),
                TownCenterBlockStyle.ParkingLot => GetParkingLotTile(local),
                TownCenterBlockStyle.FuelLot => GetFuelLotTile(local),
                TownCenterBlockStyle.CheckpointLot => GetCheckpointTile(local),
                TownCenterBlockStyle.ServiceYard => GetServiceYardTile(local),
                TownCenterBlockStyle.CrashCorridor => 2,
                TownCenterBlockStyle.SchoolLot => 2,
                TownCenterBlockStyle.HospitalLot => 2,
                _ => 2
            };
        }

        public static TownCenterBlockStyle GetBlockStyle(MapConfig config, Vector2 blockCenter)
        {
            if (Approximately(blockCenter, new Vector2(25f, 25f)))
            {
                return TownCenterBlockStyle.SchoolLot;
            }

            if (Approximately(blockCenter, new Vector2(-15f, 5f)))
            {
                return TownCenterBlockStyle.HospitalLot;
            }

            if (Mathf.Abs(blockCenter.x) <= 5f && blockCenter.y >= 25f)
            {
                return TownCenterBlockStyle.CrashCorridor;
            }

            if (blockCenter.x <= -15f && Mathf.Abs(blockCenter.y) <= 5f)
            {
                return TownCenterBlockStyle.CheckpointLot;
            }

            if (blockCenter.x >= 15f && blockCenter.y <= -15f)
            {
                return TownCenterBlockStyle.FuelLot;
            }

            if (blockCenter.x >= 15f && blockCenter.y >= 15f)
            {
                return TownCenterBlockStyle.CivicLot;
            }

            if (Mathf.Abs(blockCenter.x) <= 15f && Mathf.Abs(blockCenter.y) <= 15f)
            {
                return TownCenterBlockStyle.PlazaEdge;
            }

            int hash = Hash(blockCenter);
            if (Mathf.Abs(blockCenter.x) >= 25f || Mathf.Abs(blockCenter.y) >= 25f)
            {
                return (hash % 4) switch
                {
                    0 => TownCenterBlockStyle.PocketPark,
                    1 => TownCenterBlockStyle.ParkingLot,
                    2 => TownCenterBlockStyle.ServiceYard,
                    _ => TownCenterBlockStyle.ParkingLot
                };
            }

            return (hash % 5) switch
            {
                0 => TownCenterBlockStyle.Commercial,
                1 => TownCenterBlockStyle.Courtyard,
                2 => TownCenterBlockStyle.PocketPark,
                3 => TownCenterBlockStyle.ParkingLot,
                _ => TownCenterBlockStyle.ServiceYard
            };
        }

        public static bool IsCentralPlaza(Vector3 pos)
        {
            return Mathf.Abs(pos.x) < PlazaEdgeExtent && Mathf.Abs(pos.y) < PlazaEdgeExtent;
        }

        public static Vector2 GetNearestBlockCenter(Vector2 pos, float spacing)
        {
            float halfSpacing = spacing * 0.5f;
            float centerX = Mathf.Round((pos.x - halfSpacing) / spacing) * spacing + halfSpacing;
            float centerY = Mathf.Round((pos.y - halfSpacing) / spacing) * spacing + halfSpacing;
            return new Vector2(centerX, centerY);
        }

        private static float DistanceToGridLine(float value, float spacing)
        {
            if (spacing <= 0f)
            {
                return float.MaxValue;
            }

            float nearestStreet = Mathf.Round(value / spacing) * spacing;
            return Mathf.Abs(value - nearestStreet);
        }

        private static int GetPocketParkTile(Vector2 local)
        {
            return Mathf.Abs(local.x) < 2.4f && Mathf.Abs(local.y) < 2.4f ? 0 : 2;
        }

        private static int GetParkingLotTile(Vector2 local)
        {
            if (Mathf.Abs(local.y) < 0.45f)
            {
                return 2;
            }

            return 3;
        }

        private static int GetFuelLotTile(Vector2 local)
        {
            if (local.y > 0.6f)
            {
                return 2;
            }

            return 3;
        }

        private static int GetCheckpointTile(Vector2 local)
        {
            if (Mathf.Abs(local.x) < 0.6f || Mathf.Abs(local.y) < 0.6f)
            {
                return 1;
            }

            return 2;
        }

        private static int GetServiceYardTile(Vector2 local)
        {
            if (local.x > 0.8f || local.y < -0.8f)
            {
                return 1;
            }

            return 2;
        }

        private static int Hash(Vector2 center)
        {
            int x = Mathf.RoundToInt(center.x);
            int y = Mathf.RoundToInt(center.y);
            int hash = x * 73856093 ^ y * 19349663;
            return Mathf.Abs(hash);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.1f && Mathf.Abs(a.y - b.y) < 0.1f;
        }
    }
}
