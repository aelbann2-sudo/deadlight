using Deadlight.Data;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public static class IndustrialLayout
    {
        public static readonly Vector3 CrashSitePosition = new Vector3(0f, 30f, 0f);
        public static readonly Vector3 ResearchLabPosition = new Vector3(0f, -29.5f, 0f);
        public static readonly Vector3 FuelDepotPosition = new Vector3(23.5f, 24f, 0f);
        public static readonly Vector3 LoadingDockPosition = new Vector3(-23.5f, -23.5f, 0f);
        public static readonly Vector3 ControlOfficePosition = new Vector3(-24f, 24f, 0f);
        public static readonly Vector3 CraneYardPosition = new Vector3(24f, -22.5f, 0f);

        public static readonly Vector3[] StreetlightPositions =
        {
            new Vector3(-24f, 14f, 0f),
            new Vector3(0f, 14f, 0f),
            new Vector3(24f, 14f, 0f),
            new Vector3(-24f, 0f, 0f),
            new Vector3(24f, 0f, 0f),
            new Vector3(-24f, -14f, 0f),
            new Vector3(0f, -14f, 0f),
            new Vector3(24f, -14f, 0f),
            new Vector3(-8f, 29f, 0f),
            new Vector3(8f, -29f, 0f),
        };

        private static readonly Rect CrashPad = CreateRect(0f, 29f, 15f, 8f);
        private static readonly Rect OfficeLot = CreateRect(-24f, 24f, 10f, 8f);
        private static readonly Rect FuelLot = CreateRect(23.5f, 24f, 11f, 8f);
        private static readonly Rect NorthWestWarehouseLot = CreateRect(-10.5f, 21f, 12f, 9f);
        private static readonly Rect NorthEastWarehouseLot = CreateRect(10.5f, 18.5f, 13f, 10f);
        private static readonly Rect WestSalvageLot = CreateRect(-24f, 6f, 11f, 18f);
        private static readonly Rect MidWestWarehouseLot = CreateRect(-9f, 7f, 13f, 9f);
        private static readonly Rect MidEastWarehouseLot = CreateRect(9.5f, 7f, 11f, 9f);
        private static readonly Rect EastProcessLot = CreateRect(24f, 6f, 11f, 18f);
        private static readonly Rect SouthDockLot = CreateRect(-23.5f, -23.5f, 13f, 9f);
        private static readonly Rect LabLot = CreateRect(0f, -29.5f, 15f, 8f);
        private static readonly Rect CraneLot = CreateRect(24f, -22.5f, 11f, 10f);
        private static readonly Rect SouthCenterApron = CreateRect(0f, -20f, 10f, 7f);
        private static readonly Rect MaintenanceLot = CreateRect(0f, -9.5f, 16f, 8f);

        private static readonly Rect WestGravelLot = CreateRect(-24f, -8f, 12f, 12f);
        private static readonly Rect EastGravelLot = CreateRect(24f, -8f, 12f, 10f);
        private static readonly Rect NorthMedian = CreateRect(0f, 25f, 12f, 4f);
        private static readonly Rect CenterMedian = CreateRect(0f, 8f, 7f, 8f);
        private static readonly Rect[] GrassPatches =
        {
            CreateRect(-33f, 29f, 6f, 10f),
            CreateRect(33f, 28f, 6f, 10f),
            CreateRect(-33f, -14f, 6f, 14f),
            CreateRect(33f, -12f, 6f, 12f),
            CreateRect(31f, -31f, 8f, 8f),
        };

        private const float MainRoadHalfWidth = 2.4f;
        private const float ServiceRoadHalfWidth = 1.8f;
        private const float LaneHalfWidth = 1.6f;
        private const float SpurHalfWidth = 1.4f;
        private const float ShoulderWidth = 0.75f;

        public static int GetTileType(MapConfig config, int x, int y)
        {
            var pos = new Vector2(x, y);

            if (IsAsphalt(pos))
            {
                return 3;
            }

            if (IsConcrete(pos) || IsRoadShoulder(pos))
            {
                return 2;
            }

            if (IsGravel(pos))
            {
                return 1;
            }

            return IsGrass(pos) ? 0 : 1;
        }

        private static bool IsAsphalt(Vector2 pos)
        {
            return InHorizontalRoad(pos, 0f, MainRoadHalfWidth, -36f, 36f) ||
                InHorizontalRoad(pos, 21.5f, ServiceRoadHalfWidth, -34f, 34f) ||
                InHorizontalRoad(pos, -22.5f, ServiceRoadHalfWidth, -34f, 34f) ||
                InVerticalRoad(pos, -24f, LaneHalfWidth, -31f, 31f) ||
                InVerticalRoad(pos, 24f, LaneHalfWidth, -31f, 31f) ||
                InVerticalRoad(pos, 0f, SpurHalfWidth, 3f, 32f) ||
                InVerticalRoad(pos, 0f, SpurHalfWidth, -33f, -19f);
        }

        private static bool IsConcrete(Vector2 pos)
        {
            return Contains(CrashPad, pos) ||
                Contains(OfficeLot, pos) ||
                Contains(FuelLot, pos) ||
                Contains(NorthWestWarehouseLot, pos) ||
                Contains(NorthEastWarehouseLot, pos) ||
                Contains(WestSalvageLot, pos) ||
                Contains(MidWestWarehouseLot, pos) ||
                Contains(MidEastWarehouseLot, pos) ||
                Contains(EastProcessLot, pos) ||
                Contains(SouthDockLot, pos) ||
                Contains(LabLot, pos) ||
                Contains(CraneLot, pos) ||
                Contains(SouthCenterApron, pos) ||
                Contains(MaintenanceLot, pos);
        }

        private static bool IsGravel(Vector2 pos)
        {
            if (Contains(WestGravelLot, pos) || Contains(EastGravelLot, pos))
            {
                return true;
            }

            if (Contains(NorthMedian, pos) || Contains(CenterMedian, pos))
            {
                return true;
            }

            return false;
        }

        private static bool IsGrass(Vector2 pos)
        {
            foreach (Rect patch in GrassPatches)
            {
                if (Contains(patch, pos))
                {
                    return true;
                }
            }

            return Mathf.Abs(pos.x) > 34f || Mathf.Abs(pos.y) > 34f;
        }

        private static bool IsRoadShoulder(Vector2 pos)
        {
            return NearHorizontalRoad(pos, 0f, MainRoadHalfWidth + ShoulderWidth, -36f, 36f) ||
                NearHorizontalRoad(pos, 21.5f, ServiceRoadHalfWidth + ShoulderWidth, -34f, 34f) ||
                NearHorizontalRoad(pos, -22.5f, ServiceRoadHalfWidth + ShoulderWidth, -34f, 34f) ||
                NearVerticalRoad(pos, -24f, LaneHalfWidth + ShoulderWidth, -31f, 31f) ||
                NearVerticalRoad(pos, 24f, LaneHalfWidth + ShoulderWidth, -31f, 31f) ||
                NearVerticalRoad(pos, 0f, SpurHalfWidth + ShoulderWidth, 3f, 32f) ||
                NearVerticalRoad(pos, 0f, SpurHalfWidth + ShoulderWidth, -33f, -19f);
        }

        private static bool InHorizontalRoad(Vector2 pos, float centerY, float halfWidth, float minX, float maxX)
        {
            return pos.x >= minX && pos.x <= maxX && Mathf.Abs(pos.y - centerY) <= halfWidth;
        }

        private static bool InVerticalRoad(Vector2 pos, float centerX, float halfWidth, float minY, float maxY)
        {
            return pos.y >= minY && pos.y <= maxY && Mathf.Abs(pos.x - centerX) <= halfWidth;
        }

        private static bool NearHorizontalRoad(Vector2 pos, float centerY, float halfWidth, float minX, float maxX)
        {
            return pos.x >= minX && pos.x <= maxX && Mathf.Abs(pos.y - centerY) <= halfWidth;
        }

        private static bool NearVerticalRoad(Vector2 pos, float centerX, float halfWidth, float minY, float maxY)
        {
            return pos.y >= minY && pos.y <= maxY && Mathf.Abs(pos.x - centerX) <= halfWidth;
        }

        private static bool Contains(Rect rect, Vector2 pos)
        {
            return pos.x >= rect.xMin && pos.x <= rect.xMax && pos.y >= rect.yMin && pos.y <= rect.yMax;
        }

        private static Rect CreateRect(float centerX, float centerY, float width, float height)
        {
            return new Rect(centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }
    }
}
