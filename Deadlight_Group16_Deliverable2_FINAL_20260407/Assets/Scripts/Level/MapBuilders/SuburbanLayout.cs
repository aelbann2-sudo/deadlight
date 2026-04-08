using Deadlight.Data;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public static class SuburbanLayout
    {
        public readonly struct DetachedLot
        {
            public readonly Vector3 HousePosition;
            public readonly Rect DrivewayRect;
            public readonly bool GarageOnRight;
            public readonly int Variant;
            public readonly bool HasBackFence;

            public DetachedLot(
                float x,
                float y,
                bool garageOnRight,
                int variant,
                float drivewayCenterX,
                float drivewayCenterY,
                float drivewayWidth,
                float drivewayHeight,
                bool hasBackFence)
            {
                HousePosition = new Vector3(x, y, 0f);
                DrivewayRect = new Rect(
                    drivewayCenterX - drivewayWidth * 0.5f,
                    drivewayCenterY - drivewayHeight * 0.5f,
                    drivewayWidth,
                    drivewayHeight);
                GarageOnRight = garageOnRight;
                Variant = variant;
                HasBackFence = hasBackFence;
            }

            public Vector3 GaragePosition => HousePosition + new Vector3(GarageOnRight ? 2.2f : -2.2f, -0.35f, 0f);
        }

        public static readonly Vector3 CheckpointPosition = new Vector3(0f, -28f, 0f);
        public static readonly Vector3 GasStationPosition = new Vector3(30f, 2f, 0f);
        public static readonly Vector3 PlaygroundPosition = new Vector3(-12f, 14f, 0f);
        public static readonly Vector3 SchoolBusPosition = new Vector3(-29f, -20f, 0f);
        public static readonly Vector3 AbandonedBusPosition = new Vector3(29f, -20f, 0f);
        public static readonly Vector3 CulDeSacPosition = new Vector3(24f, 28f, 0f);
        public static readonly Vector3 SchoolPosition = new Vector3(-10.5f, -11.4f, 0f);
        public static readonly Vector3 HospitalPosition = new Vector3(10.5f, -11.3f, 0f);

        public static readonly Vector3[] StreetlightPositions =
        {
            new Vector3(-30f, 22f, 0f),
            new Vector3(-18f, 22f, 0f),
            new Vector3(-6f, 22f, 0f),
            new Vector3(8f, 22f, 0f),
            new Vector3(-20f, 10f, 0f),
            new Vector3(-20f, -4f, 0f),
            new Vector3(-20f, -18f, 0f),
            new Vector3(22f, 10f, 0f),
            new Vector3(22f, 24f, 0f),
            new Vector3(-28f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(28f, 0f, 0f),
            new Vector3(-22f, -22f, 0f),
            new Vector3(-8f, -22f, 0f),
            new Vector3(8f, -22f, 0f),
            new Vector3(22f, -22f, 0f),
            new Vector3(0f, -28f, 0f),
        };

        public static readonly DetachedLot[] DetachedLots =
        {
            new DetachedLot(-30f, 28.4f, true, 0, -27.8f, 25.1f, 1.3f, 6.2f, true),
            new DetachedLot(-18f, 28.1f, false, 1, -20.2f, 24.9f, 1.3f, 5.8f, true),
            new DetachedLot(-6.5f, 27.6f, true, 2, -4.3f, 24.6f, 1.3f, 5.6f, true),
            new DetachedLot(7f, 27.8f, false, 0, 4.8f, 24.8f, 1.3f, 5.8f, true),
            new DetachedLot(16.4f, 29.3f, true, 1, 18.5f, 27.1f, 1.3f, 4.5f, false),
            new DetachedLot(29.8f, 29.6f, true, 2, 32.1f, 27.1f, 1.3f, 4.8f, false),
            new DetachedLot(30.2f, 22.8f, false, 0, 27.9f, 23f, 4.2f, 1.3f, false),
            new DetachedLot(-30f, 14.2f, true, 0, -24.1f, 13.4f, 8.2f, 1.3f, true),
            new DetachedLot(-29.8f, 18.7f, true, 1, -24.2f, 18.7f, 7.8f, 1.3f, false),
            new DetachedLot(-8.6f, 18.4f, true, 2, -8.6f, 20.7f, 1.3f, 5.6f, false),
            new DetachedLot(8.8f, 15.5f, false, 0, 8.8f, 19.2f, 1.3f, 6.2f, false),
            new DetachedLot(30.3f, 11.3f, false, 2, 27.9f, 11.3f, 4.9f, 1.3f, false),
            new DetachedLot(-29.6f, 3f, true, 1, -24.1f, 2.3f, 7.9f, 1.3f, false),
            new DetachedLot(-29.6f, -3.8f, true, 0, -24.1f, -3.8f, 7.8f, 1.3f, false),
            new DetachedLot(-29.4f, -10.7f, true, 2, -24f, -11.5f, 7.8f, 1.3f, true),
            new DetachedLot(30.2f, -7.8f, false, 1, 27.9f, -7.8f, 5f, 1.3f, false),
            new DetachedLot(-23.5f, -29.2f, true, 0, -21.4f, -25.4f, 1.3f, 6.6f, true),
            new DetachedLot(-10.2f, -29.4f, false, 1, -12.3f, -25.6f, 1.3f, 6.8f, true),
            new DetachedLot(6.5f, -29.1f, true, 2, 8.6f, -25.4f, 1.3f, 6.4f, false),
            new DetachedLot(20.4f, -29f, false, 0, 18.3f, -25.5f, 1.3f, 6.4f, true),
        };

        private static readonly Rect GasStationLot = CreateRect(30f, 2f, 8.5f, 7f);
        private static readonly Rect CheckpointLot = CreateRect(0f, -28f, 9f, 6f);
        private static readonly Rect SchoolBusLayby = CreateRect(-29f, -20f, 7f, 4.2f);
        private static readonly Rect AbandonedBusLayby = CreateRect(29f, -20f, 7f, 4.2f);
        private static readonly Rect PlaygroundWalk = CreateRect(-12f, 12.5f, 8.5f, 5f);
        private static readonly Rect PlaygroundCrossWalk = CreateRect(-12f, 15.8f, 2f, 6f);
        private static readonly Rect SchoolLot = CreateRect(-10.5f, -11.4f, 8.2f, 5.6f);
        private static readonly Rect SchoolDrive = CreateRect(-10.5f, -5.5f, 2.4f, 8f);
        private static readonly Rect HospitalLot = CreateRect(10.5f, -11.3f, 8.4f, 5.8f);
        private static readonly Rect HospitalDrive = CreateRect(10.5f, -5.5f, 2.4f, 8f);

        private const float MainRoadHalfWidth = 2.4f;
        private const float CollectorHalfWidth = 1.6f;
        private const float NeighborhoodRoadHalfWidth = 1.4f;
        private const float ShoulderWidth = 0.75f;
        private const float CulDeSacInnerRadius = 2.8f;
        private const float CulDeSacOuterRadius = 4.9f;

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

            return 0;
        }

        private static bool IsAsphalt(Vector2 pos)
        {
            if (InHorizontalRoad(pos, 0f, MainRoadHalfWidth, -36f, 36f) ||
                InVerticalRoad(pos, -20f, CollectorHalfWidth, -32f, 30f) ||
                InVerticalRoad(pos, 22f, CollectorHalfWidth, -8f, 34f) ||
                InHorizontalRoad(pos, 22f, NeighborhoodRoadHalfWidth, -33f, 25f) ||
                InHorizontalRoad(pos, -22f, NeighborhoodRoadHalfWidth, -28f, 24f) ||
                InVerticalRoad(pos, 0f, 1.4f, -34f, -22f) ||
                InVerticalRoad(pos, 24f, 1.4f, 22f, 28f))
            {
                return true;
            }

            Vector2 culDeSacOffset = pos - new Vector2(CulDeSacPosition.x, CulDeSacPosition.y);
            float culDeSacDistance = culDeSacOffset.magnitude;
            return culDeSacDistance >= CulDeSacInnerRadius && culDeSacDistance <= CulDeSacOuterRadius;
        }

        private static bool IsConcrete(Vector2 pos)
        {
            if (Contains(GasStationLot, pos) ||
                Contains(CheckpointLot, pos) ||
                Contains(SchoolBusLayby, pos) ||
                Contains(AbandonedBusLayby, pos) ||
                Contains(PlaygroundWalk, pos) ||
                Contains(PlaygroundCrossWalk, pos) ||
                Contains(SchoolLot, pos) ||
                Contains(SchoolDrive, pos) ||
                Contains(HospitalLot, pos) ||
                Contains(HospitalDrive, pos))
            {
                return true;
            }

            foreach (DetachedLot lot in DetachedLots)
            {
                if (Contains(lot.DrivewayRect, pos))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRoadShoulder(Vector2 pos)
        {
            if (NearHorizontalRoad(pos, 0f, MainRoadHalfWidth + ShoulderWidth, -36f, 36f) ||
                NearVerticalRoad(pos, -20f, CollectorHalfWidth + ShoulderWidth, -32f, 30f) ||
                NearVerticalRoad(pos, 22f, CollectorHalfWidth + ShoulderWidth, -8f, 34f) ||
                NearHorizontalRoad(pos, 22f, NeighborhoodRoadHalfWidth + ShoulderWidth, -33f, 25f) ||
                NearHorizontalRoad(pos, -22f, NeighborhoodRoadHalfWidth + ShoulderWidth, -28f, 24f) ||
                NearVerticalRoad(pos, 0f, 2.1f, -34f, -22f) ||
                NearVerticalRoad(pos, 24f, 2.1f, 22f, 28f))
            {
                return true;
            }

            Vector2 culDeSacOffset = pos - new Vector2(CulDeSacPosition.x, CulDeSacPosition.y);
            float culDeSacDistance = culDeSacOffset.magnitude;
            return culDeSacDistance >= CulDeSacInnerRadius - ShoulderWidth &&
                culDeSacDistance <= CulDeSacOuterRadius + ShoulderWidth;
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
