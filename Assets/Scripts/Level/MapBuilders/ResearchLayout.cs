using Deadlight.Data;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public static class ResearchLayout
    {
        public static readonly Vector3 MainLabPosition = new Vector3(0f, -24f, 0f);
        public static readonly Vector3 QuarantineGatePosition = new Vector3(0f, 26f, 0f);
        public static readonly Vector3 DataVaultPosition = new Vector3(24f, 0f, 0f);
        public static readonly Vector3 BioContainmentPosition = new Vector3(-24f, 0f, 0f);
        public static readonly Vector3 ReactorYardPosition = new Vector3(0f, 8f, 0f);

        private static readonly Rect MainLabLot = CreateRect(0f, -24f, 16f, 9f);
        private static readonly Rect QuarantineGateLot = CreateRect(0f, 26f, 14f, 7f);
        private static readonly Rect DataVaultLot = CreateRect(24f, 0f, 10f, 14f);
        private static readonly Rect BioContainmentLot = CreateRect(-24f, 0f, 10f, 14f);
        private static readonly Rect ReactorYardLot = CreateRect(0f, 8f, 12f, 9f);

        private const float MainCorridorHalfWidth = 2.3f;
        private const float SideCorridorHalfWidth = 1.6f;
        private const float ShoulderWidth = 0.7f;

        private static readonly Rect[] GrassPatches = new[]
        {
            CreateRect(-28f, 28f, 10f, 10f),
            CreateRect(28f, 28f, 10f, 10f),
            CreateRect(-28f, -28f, 10f, 10f),
            CreateRect(28f, -28f, 10f, 10f),
            CreateRect(-34f, 0f, 6f, 12f),
            CreateRect(34f, 0f, 6f, 12f),
            CreateRect(0f, 34f, 12f, 6f),
            CreateRect(0f, -34f, 12f, 6f),
            CreateRect(-28f, 14f, 8f, 6f),
            CreateRect(28f, -14f, 8f, 6f),
        };

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

            if (IsGrass(pos))
            {
                return 0;
            }

            return 1;
        }

        private static bool IsAsphalt(Vector2 pos)
        {
            return InHorizontalRoad(pos, 0f, MainCorridorHalfWidth, -36f, 36f) ||
                InVerticalRoad(pos, 0f, MainCorridorHalfWidth, -36f, 36f) ||
                InHorizontalRoad(pos, 18f, SideCorridorHalfWidth, -28f, 28f) ||
                InHorizontalRoad(pos, -18f, SideCorridorHalfWidth, -28f, 28f) ||
                InVerticalRoad(pos, 18f, SideCorridorHalfWidth, -28f, 28f) ||
                InVerticalRoad(pos, -18f, SideCorridorHalfWidth, -28f, 28f);
        }

        private static bool IsConcrete(Vector2 pos)
        {
            return Contains(MainLabLot, pos) ||
                Contains(QuarantineGateLot, pos) ||
                Contains(DataVaultLot, pos) ||
                Contains(BioContainmentLot, pos) ||
                Contains(ReactorYardLot, pos);
        }

        private static bool IsRoadShoulder(Vector2 pos)
        {
            return NearHorizontalRoad(pos, 0f, MainCorridorHalfWidth + ShoulderWidth, -36f, 36f) ||
                NearVerticalRoad(pos, 0f, MainCorridorHalfWidth + ShoulderWidth, -36f, 36f) ||
                NearHorizontalRoad(pos, 18f, SideCorridorHalfWidth + ShoulderWidth, -28f, 28f) ||
                NearHorizontalRoad(pos, -18f, SideCorridorHalfWidth + ShoulderWidth, -28f, 28f) ||
                NearVerticalRoad(pos, 18f, SideCorridorHalfWidth + ShoulderWidth, -28f, 28f) ||
                NearVerticalRoad(pos, -18f, SideCorridorHalfWidth + ShoulderWidth, -28f, 28f);
        }

        private static bool IsGrass(Vector2 pos)
        {
            for (int i = 0; i < GrassPatches.Length; i++)
            {
                if (Contains(GrassPatches[i], pos))
                {
                    return true;
                }
            }
            return false;
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
