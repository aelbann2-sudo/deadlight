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
            config.halfWidth = 13;
            config.halfHeight = 13;
            config.perimeterHalfW = 12f;
            config.perimeterHalfH = 12f;
            config.houseCount = 6;
            config.treeCount = 8;
            config.rockCount = 4;
            config.crateCount = 4;
            config.barrelCount = 4;
            config.carCount = 2;
            config.pathWidth = 2f;
            config.hasDiagonalConcrete = true;
            config.groundTint = Color.white;
            config.buildingTint = Color.white;
            config.enemySpawnPositions = new[] {
                new Vector3(8, 6, 0), new Vector3(-8, 6, 0), new Vector3(9, -6, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-5, 9, 0), new Vector3(7, 9, 0),
                new Vector3(-9, -3, 0), new Vector3(9, -3, 0),
                new Vector3(-3, -9, 0), new Vector3(3, -9, 0)
            };
            return config;
        }

        public static MapConfig CreateIndustrial()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Industrial District";
            config.mapType = MapType.Industrial;
            config.description = "Warehouses and narrow corridors. Tight chokepoints, limited escape routes.";
            config.halfWidth = 11;
            config.halfHeight = 14;
            config.perimeterHalfW = 10f;
            config.perimeterHalfH = 13f;
            config.houseCount = 8;
            config.treeCount = 2;
            config.rockCount = 6;
            config.crateCount = 10;
            config.barrelCount = 8;
            config.carCount = 4;
            config.pathWidth = 1.5f;
            config.hasDiagonalConcrete = false;
            config.groundTint = new Color(0.85f, 0.82f, 0.78f);
            config.buildingTint = new Color(0.7f, 0.7f, 0.75f);
            config.enemySpawnPositions = new[] {
                new Vector3(7, 10, 0), new Vector3(-7, 10, 0),
                new Vector3(7, -10, 0), new Vector3(-7, -10, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-4, 10, 0), new Vector3(4, 10, 0),
                new Vector3(-6, -2, 0), new Vector3(6, -2, 0),
                new Vector3(-2, -10, 0), new Vector3(2, -10, 0)
            };
            return config;
        }

        public static MapConfig CreateSuburban()
        {
            var config = CreateInstance<MapConfig>();
            config.mapName = "Suburban Outskirts";
            config.mapType = MapType.Suburban;
            config.description = "Houses, yards, and wide open spaces. Rewards mobility, less natural cover.";
            config.halfWidth = 15;
            config.halfHeight = 12;
            config.perimeterHalfW = 14f;
            config.perimeterHalfH = 11f;
            config.houseCount = 10;
            config.treeCount = 14;
            config.rockCount = 3;
            config.crateCount = 2;
            config.barrelCount = 2;
            config.carCount = 5;
            config.pathWidth = 2.5f;
            config.hasDiagonalConcrete = false;
            config.groundTint = new Color(0.95f, 1f, 0.9f);
            config.buildingTint = new Color(0.9f, 0.85f, 0.8f);
            config.enemySpawnPositions = new[] {
                new Vector3(12, 8, 0), new Vector3(-12, 8, 0),
                new Vector3(12, -8, 0), new Vector3(-12, -8, 0)
            };
            config.lorePositions = new[] {
                new Vector3(-8, 8, 0), new Vector3(8, 8, 0),
                new Vector3(-10, -3, 0), new Vector3(10, -3, 0),
                new Vector3(-4, -8, 0), new Vector3(4, -8, 0)
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
