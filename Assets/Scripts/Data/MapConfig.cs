using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Data
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Deadlight/Map Config")]
    public class MapConfig : ScriptableObject
    {
        public string mapName;
        public MapType mapType;
        public string description;

        [Header("Dimensions")]
        public int halfWidth = 12;
        public int halfHeight = 12;
        public float perimeterHalfW = 12f;
        public float perimeterHalfH = 12f;

        [Header("Environment Density")]
        public int houseCount = 6;
        public int treeCount = 8;
        public int rockCount = 4;
        public int crateCount = 4;
        public int barrelCount = 4;
        public int carCount = 2;

        [Header("Ground Tiles")]
        public float pathWidth = 2f;
        public bool hasDiagonalConcrete = true;

        [Header("Ground Pattern")]
        public float streetGridSpacing = 8f;
        public float mainRoadWidth = 2f;
        public float sideRoadWidth = 1.5f;

        [Header("Spawn Settings")]
        public Vector3[] enemySpawnPositions;
        public Vector3[] lorePositions;

        [Header("Map Theme Colors")]
        public Color groundTint = Color.white;
        public Color buildingTint = Color.white;

        public static MapConfig CreateTownCenter()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Town Center";
            config.mapType = MapType.TownCenter;
            config.description = "Streets, shops, and open plazas. Balanced layout with moderate cover.";
            config.halfWidth = 24;
            config.halfHeight = 24;
            config.perimeterHalfW = 23f;
            config.perimeterHalfH = 23f;
            config.houseCount = 12;
            config.treeCount = 14;
            config.rockCount = 6;
            config.crateCount = 8;
            config.barrelCount = 6;
            config.carCount = 6;
            config.pathWidth = 2f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 10f;
            config.mainRoadWidth = 2f;
            config.sideRoadWidth = 1.5f;
            config.groundTint = Color.white;
            config.buildingTint = Color.white;
            config.enemySpawnPositions = new[] {
                new Vector3(18, 14, 0), new Vector3(-18, 14, 0),
                new Vector3(18, -14, 0), new Vector3(-18, -14, 0),
                new Vector3(0, 20, 0), new Vector3(0, -20, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-10, 18, 0), new Vector3(10, 18, 0),
                new Vector3(-18, -6, 0), new Vector3(18, -6, 0),
                new Vector3(-6, -18, 0), new Vector3(6, -18, 0)
            };
            return config;
        }

        public static MapConfig CreateIndustrial()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Industrial District";
            config.mapType = MapType.Industrial;
            config.description = "Warehouses and narrow corridors. Tight chokepoints, limited escape routes.";
            config.halfWidth = 20;
            config.halfHeight = 26;
            config.perimeterHalfW = 19f;
            config.perimeterHalfH = 25f;
            config.houseCount = 14;
            config.treeCount = 3;
            config.rockCount = 10;
            config.crateCount = 20;
            config.barrelCount = 16;
            config.carCount = 6;
            config.pathWidth = 1.5f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 10f;
            config.mainRoadWidth = 2f;
            config.sideRoadWidth = 1f;
            config.groundTint = new Color(0.85f, 0.82f, 0.78f);
            config.buildingTint = new Color(0.7f, 0.7f, 0.75f);
            config.enemySpawnPositions = new[] {
                new Vector3(14, 20, 0), new Vector3(-14, 20, 0),
                new Vector3(14, -20, 0), new Vector3(-14, -20, 0),
                new Vector3(0, 22, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-8, 20, 0), new Vector3(8, 20, 0),
                new Vector3(-12, -4, 0), new Vector3(12, -4, 0),
                new Vector3(-4, -20, 0), new Vector3(4, -20, 0)
            };
            return config;
        }

        public static MapConfig CreateSuburban()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Suburban Outskirts";
            config.mapType = MapType.Suburban;
            config.description = "Houses, yards, and wide open spaces. Rewards mobility, less natural cover.";
            config.halfWidth = 27;
            config.halfHeight = 22;
            config.perimeterHalfW = 26f;
            config.perimeterHalfH = 21f;
            config.houseCount = 16;
            config.treeCount = 28;
            config.rockCount = 5;
            config.crateCount = 4;
            config.barrelCount = 3;
            config.carCount = 8;
            config.pathWidth = 2.5f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 0f;
            config.mainRoadWidth = 2.5f;
            config.sideRoadWidth = 2f;
            config.groundTint = new Color(0.95f, 1f, 0.9f);
            config.buildingTint = new Color(0.9f, 0.85f, 0.8f);
            config.enemySpawnPositions = new[] {
                new Vector3(22, 16, 0), new Vector3(-22, 16, 0),
                new Vector3(22, -16, 0), new Vector3(-22, -16, 0),
                new Vector3(0, 18, 0), new Vector3(0, -18, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-16, 16, 0), new Vector3(16, 16, 0),
                new Vector3(-20, -6, 0), new Vector3(20, -6, 0),
                new Vector3(-8, -16, 0), new Vector3(8, -16, 0)
            };
            return config;
        }

        public static MapConfig GetConfigForType(MapType type)
        {
            return type switch
            {
                MapType.TownCenter => CreateTownCenter(),
                MapType.Industrial => CreateIndustrial(),
                MapType.Suburban => CreateSuburban(),
                _ => CreateTownCenter()
            };
        }
    }
}
