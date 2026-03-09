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

        // New properties for map grammar
        [Header("Map Grammar")]
        public float buildingDensity = 1.0f;
        public float coverDensity = 1.0f;
        public float openAreaSize = 1.0f;
        public float roadComplexity = 1.0f;
        public MapLayoutType layoutType = MapLayoutType.Grid;

        public enum MapLayoutType
        {
            Grid,
            Organic,
            Corridor
        }

        public static MapConfig CreateTownCenter()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Town Center";
            config.mapType = MapType.TownCenter;
            config.description = "Streets, shops, and open plazas. Balanced layout with moderate cover.";
            config.halfWidth = 36; // Increased from 24
            config.halfHeight = 36; // Increased from 24
            config.perimeterHalfW = 35f;
            config.perimeterHalfH = 35f;
            config.houseCount = 24; // Increased from 12
            config.treeCount = 20; // Increased from 14
            config.rockCount = 8; // Increased from 6
            config.crateCount = 16; // Increased from 8
            config.barrelCount = 12; // Increased from 6
            config.carCount = 12; // Increased from 6
            config.pathWidth = 2f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 10f;
            config.mainRoadWidth = 2f;
            config.sideRoadWidth = 1.5f;
            config.groundTint = Color.white;
            config.buildingTint = Color.white;
            config.buildingDensity = 1.2f;
            config.coverDensity = 1.0f;
            config.openAreaSize = 1.5f;
            config.roadComplexity = 1.2f;
            config.layoutType = MapLayoutType.Grid;
            config.enemySpawnPositions = new[] {
                new Vector3(28, 24, 0), new Vector3(-28, 24, 0),
                new Vector3(28, -24, 0), new Vector3(-28, -24, 0),
                new Vector3(0, 30, 0), new Vector3(0, -30, 0),
                new Vector3(20, 0, 0), new Vector3(-20, 0, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-15, 28, 0), new Vector3(15, 28, 0),
                new Vector3(-28, -8, 0), new Vector3(28, -8, 0),
                new Vector3(-8, -28, 0), new Vector3(8, -28, 0),
                new Vector3(-20, 15, 0), new Vector3(20, -15, 0)
            };
            return config;
        }

        public static MapConfig CreateIndustrial()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Industrial District";
            config.mapType = MapType.Industrial;
            config.description = "Warehouses and narrow corridors. Tight chokepoints, limited escape routes.";
            config.halfWidth = 30; // Increased from 20
            config.halfHeight = 39; // Increased from 26
            config.perimeterHalfW = 29f;
            config.perimeterHalfH = 38f;
            config.houseCount = 20; // Increased from 14
            config.treeCount = 6; // Reduced from 3
            config.rockCount = 15; // Increased from 10
            config.crateCount = 30; // Increased from 20
            config.barrelCount = 24; // Increased from 16
            config.carCount = 12; // Increased from 6
            config.pathWidth = 1.5f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 10f;
            config.mainRoadWidth = 2f;
            config.sideRoadWidth = 1f;
            config.groundTint = new Color(0.85f, 0.82f, 0.78f);
            config.buildingTint = new Color(0.7f, 0.7f, 0.75f);
            config.buildingDensity = 1.5f;
            config.coverDensity = 1.8f;
            config.openAreaSize = 0.7f;
            config.roadComplexity = 1.5f;
            config.layoutType = MapLayoutType.Corridor;
            config.enemySpawnPositions = new[] {
                new Vector3(22, 32, 0), new Vector3(-22, 32, 0),
                new Vector3(22, -32, 0), new Vector3(-22, -32, 0),
                new Vector3(0, 36, 0),
                new Vector3(15, 0, 0), new Vector3(-15, 0, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-12, 32, 0), new Vector3(12, 32, 0),
                new Vector3(-18, -6, 0), new Vector3(18, -6, 0),
                new Vector3(-6, -32, 0), new Vector3(6, -32, 0),
                new Vector3(-20, 18, 0), new Vector3(20, -18, 0)
            };
            return config;
        }

        public static MapConfig CreateSuburban()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Suburban Outskirts";
            config.mapType = MapType.Suburban;
            config.description = "Houses, yards, and wide open spaces. Rewards mobility, less natural cover.";
            config.halfWidth = 40; // Increased from 27
            config.halfHeight = 33; // Increased from 22
            config.perimeterHalfW = 39f;
            config.perimeterHalfH = 32f;
            config.houseCount = 32; // Increased from 16
            config.treeCount = 42; // Increased from 28
            config.rockCount = 8; // Increased from 5
            config.crateCount = 8; // Increased from 4
            config.barrelCount = 6; // Increased from 3
            config.carCount = 16; // Increased from 8
            config.pathWidth = 2.5f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 0f;
            config.mainRoadWidth = 2.5f;
            config.sideRoadWidth = 2f;
            config.groundTint = new Color(0.95f, 1f, 0.9f);
            config.buildingTint = new Color(0.9f, 0.85f, 0.8f);
            config.buildingDensity = 1.0f;
            config.coverDensity = 0.7f;
            config.openAreaSize = 2.0f;
            config.roadComplexity = 0.8f;
            config.layoutType = MapLayoutType.Organic;
            config.enemySpawnPositions = new[] {
                new Vector3(33, 26, 0), new Vector3(-33, 26, 0),
                new Vector3(33, -26, 0), new Vector3(-33, -26, 0),
                new Vector3(0, 28, 0), new Vector3(0, -28, 0),
                new Vector3(25, 0, 0), new Vector3(-25, 0, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-24, 24, 0), new Vector3(24, 24, 0),
                new Vector3(-30, -8, 0), new Vector3(30, -8, 0),
                new Vector3(-12, -24, 0), new Vector3(12, -24, 0),
                new Vector3(-20, 12, 0), new Vector3(20, -12, 0)
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
