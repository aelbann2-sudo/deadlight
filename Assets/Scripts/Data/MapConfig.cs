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
            config.description = "Warehouses, salvage lots, fuel storage, and loading docks. Dense cover with cleaner district reads.";
            config.halfWidth = 36;
            config.halfHeight = 36;
            config.perimeterHalfW = 35f;
            config.perimeterHalfH = 35f;
            config.houseCount = 0;
            config.treeCount = 5;
            config.rockCount = 8;
            config.crateCount = 10;
            config.barrelCount = 12;
            config.carCount = 7;
            config.pathWidth = 1.5f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 0f;
            config.mainRoadWidth = 2.4f;
            config.sideRoadWidth = 1.6f;
            config.groundTint = new Color(0.76f, 0.76f, 0.7f);
            config.buildingTint = new Color(0.74f, 0.73f, 0.72f);
            config.buildingDensity = 0.95f;
            config.coverDensity = 1.05f;
            config.openAreaSize = 1.35f;
            config.roadComplexity = 1.1f;
            config.layoutType = MapLayoutType.Corridor;
            config.enemySpawnPositions = new[] {
                new Vector3(24, 30, 0), new Vector3(-24, 30, 0),
                new Vector3(24, -30, 0), new Vector3(-24, -30, 0),
                new Vector3(0, 32, 0),
                new Vector3(16, 0, 0), new Vector3(-16, 0, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-24, 20, 0), new Vector3(24, 20, 0),
                new Vector3(-18, -6, 0), new Vector3(18, -6, 0),
                new Vector3(-6, -30, 0), new Vector3(6, -30, 0),
                new Vector3(-10, 10, 0), new Vector3(10, -10, 0)
            };
            return config;
        }

        public static MapConfig CreateSuburban()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Suburban Outskirts";
            config.mapType = MapType.Suburban;
            config.description = "Houses, yards, and wide open spaces. Rewards mobility, less natural cover.";
            config.halfWidth = 36;
            config.halfHeight = 36;
            config.perimeterHalfW = 35f;
            config.perimeterHalfH = 35f;
            config.houseCount = 14;
            config.treeCount = 26;
            config.rockCount = 6;
            config.crateCount = 3;
            config.barrelCount = 4;
            config.carCount = 8;
            config.pathWidth = 2.5f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 0f;
            config.mainRoadWidth = 2.5f;
            config.sideRoadWidth = 1.6f;
            config.groundTint = new Color(0.95f, 1f, 0.9f);
            config.buildingTint = new Color(0.9f, 0.85f, 0.8f);
            config.buildingDensity = 0.6f;
            config.coverDensity = 0.8f;
            config.openAreaSize = 2.5f;
            config.roadComplexity = 0.75f;
            config.layoutType = MapLayoutType.Organic;
            config.enemySpawnPositions = new[] {
                new Vector3(30, 30, 0), new Vector3(-30, 30, 0),
                new Vector3(30, -30, 0), new Vector3(-30, -30, 0),
                new Vector3(0, 32, 0), new Vector3(0, -32, 0),
                new Vector3(26, 0, 0), new Vector3(-26, 0, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-24, 26, 0), new Vector3(12, 26, 0),
                new Vector3(-30, -12, 0), new Vector3(30, -20, 0),
                new Vector3(-10, -28, 0), new Vector3(20, -28, 0),
                new Vector3(-12, 14, 0), new Vector3(22, 24, 0)
            };
            return config;
        }

        public static MapConfig CreateResearch()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Research Complex";
            config.mapType = MapType.Research;
            config.description = "Quarantine labs, containment corridors, and pressure chokepoints. Tightest final-level arena.";
            config.halfWidth = 36;
            config.halfHeight = 36;
            config.perimeterHalfW = 35f;
            config.perimeterHalfH = 35f;
            config.houseCount = 0;
            config.treeCount = 4;
            config.rockCount = 10;
            config.crateCount = 8;
            config.barrelCount = 16;
            config.carCount = 4;
            config.pathWidth = 1.2f;
            config.hasDiagonalConcrete = false;
            config.streetGridSpacing = 0f;
            config.mainRoadWidth = 1.8f;
            config.sideRoadWidth = 1.2f;
            config.groundTint = new Color(0.76f, 0.8f, 0.84f);
            config.buildingTint = new Color(0.78f, 0.83f, 0.9f);
            config.buildingDensity = 1.35f;
            config.coverDensity = 1.25f;
            config.openAreaSize = 0.8f;
            config.roadComplexity = 1.4f;
            config.layoutType = MapLayoutType.Corridor;
            config.enemySpawnPositions = new[]
            {
                new Vector3(30f, 30f, 0f), new Vector3(-30f, 30f, 0f),
                new Vector3(30f, -30f, 0f), new Vector3(-30f, -30f, 0f),
                new Vector3(0f, 32f, 0f), new Vector3(0f, -32f, 0f),
                new Vector3(32f, 0f, 0f), new Vector3(-32f, 0f, 0f)
            };
            config.lorePositions = new[]
            {
                new Vector3(-26f, 26f, 0f), new Vector3(26f, 26f, 0f),
                new Vector3(-26f, -26f, 0f), new Vector3(26f, -26f, 0f),
                new Vector3(-12f, 8f, 0f), new Vector3(12f, 8f, 0f),
                new Vector3(-10f, -18f, 0f), new Vector3(10f, -18f, 0f)
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
                MapType.Research => CreateResearch(),
                _ => CreateTownCenter()
            };
        }
    }
}
