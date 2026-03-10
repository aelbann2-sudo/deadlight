#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using Deadlight.Core;
using Deadlight.Data;
using Deadlight.Level;
using Deadlight.Level.MapBuilders;
using Deadlight.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deadlight.Editor
{
    public static class MapCaptureTools
    {
        private const int CaptureSeed = 12345;
        private static readonly Color BackgroundColor = new Color(0.12f, 0.14f, 0.1f);

        private readonly struct CaptureView
        {
            public readonly string Name;
            public readonly Vector3 Position;
            public readonly float OrthographicSize;
            public readonly int Width;
            public readonly int Height;

            public CaptureView(string name, Vector3 position, float orthographicSize, int width, int height)
            {
                Name = name;
                Position = position;
                OrthographicSize = orthographicSize;
                Width = width;
                Height = height;
            }
        }

        private static readonly CaptureView[] TownCenterViews =
        {
            new CaptureView("overview", new Vector3(0f, 0f, -10f), 39f, 2400, 2400),
            new CaptureView("center", new Vector3(0f, 0f, -10f), 3.5f, 1600, 900),
            new CaptureView("plaza_north", new Vector3(0f, 12f, -10f), 3.5f, 1600, 900),
            new CaptureView("crashsite", new Vector3(0f, 30f, -10f), 3.5f, 1600, 900),
            new CaptureView("checkpoint", new Vector3(-25f, 0f, -10f), 3.5f, 1600, 900),
            new CaptureView("gasstation", new Vector3(25f, -20f, -10f), 3.5f, 1600, 900),
            new CaptureView("diner", new Vector3(-20f, 25f, -10f), 3.5f, 1600, 900),
            new CaptureView("school", new Vector3(25f, 25f, -10f), 3.5f, 1600, 900),
            new CaptureView("hospital", new Vector3(-15f, 5f, -10f), 3.5f, 1600, 900),
        };

        private static readonly CaptureView[] SuburbanViews =
        {
            new CaptureView("overview", new Vector3(0f, 0f, -10f), 39f, 2400, 2400),
            new CaptureView("center", new Vector3(0f, 12f, -10f), 5f, 1600, 900),
            new CaptureView("north", new Vector3(0f, 27f, -10f), 5f, 1600, 900),
            new CaptureView("south", new Vector3(0f, -27f, -10f), 5f, 1600, 900),
            new CaptureView("west", new Vector3(-28f, 9f, -10f), 5f, 1600, 900),
            new CaptureView("east", new Vector3(24f, 26f, -10f), 5f, 1600, 900),
            new CaptureView("playground", new Vector3(-12f, 14f, -10f), 4f, 1600, 900),
            new CaptureView("school", new Vector3(-10.5f, -11.4f, -10f), 4f, 1600, 900),
            new CaptureView("hospital", new Vector3(10.5f, -11.3f, -10f), 4f, 1600, 900),
            new CaptureView("checkpoint", new Vector3(0f, -28f, -10f), 4f, 1600, 900),
            new CaptureView("gasstation", new Vector3(30f, 2f, -10f), 4f, 1600, 900),
        };

        private static readonly CaptureView[] IndustrialViews =
        {
            new CaptureView("overview", new Vector3(0f, 0f, -10f), 39f, 2400, 2400),
            new CaptureView("north_yard", new Vector3(0f, 24f, -10f), 5f, 1600, 900),
            new CaptureView("center", new Vector3(0f, 0f, -10f), 5f, 1600, 900),
            new CaptureView("west_compound", new Vector3(-24f, 8f, -10f), 5f, 1600, 900),
            new CaptureView("east_yard", new Vector3(24f, 8f, -10f), 5f, 1600, 900),
            new CaptureView("lab", new Vector3(0f, -29.5f, -10f), 4f, 1600, 900),
            new CaptureView("fuel_depot", new Vector3(23.5f, 24f, -10f), 4f, 1600, 900),
            new CaptureView("loading_dock", new Vector3(-23.5f, -23.5f, -10f), 4f, 1600, 900),
            new CaptureView("crane_yard", new Vector3(24f, -22.5f, -10f), 4f, 1600, 900),
        };

        [MenuItem("Deadlight/Capture Maps/TownCenter")]
        public static void CaptureTownCenterMenu()
        {
            CaptureMap(MapType.TownCenter, "TownCenter", TownCenterViews);
        }

        public static void CaptureTownCenterBatch()
        {
            try
            {
                CaptureTownCenterMenu();
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Deadlight/Capture Maps/Suburban")]
        public static void CaptureSuburbanMenu()
        {
            CaptureMap(MapType.Suburban, "Suburban", SuburbanViews);
        }

        public static void CaptureSuburbanBatch()
        {
            try
            {
                CaptureSuburbanMenu();
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Deadlight/Capture Maps/Industrial")]
        public static void CaptureIndustrialMenu()
        {
            CaptureMap(MapType.Industrial, "Industrial", IndustrialViews);
        }

        public static void CaptureIndustrialBatch()
        {
            try
            {
                CaptureIndustrialMenu();
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureMap(MapType mapType, string mapFolderName, IReadOnlyList<CaptureView> views)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            string outputDir = Path.Combine(projectRoot, "Artifacts", "MapCaptures", mapFolderName);
            Directory.CreateDirectory(outputDir);
            foreach (string file in Directory.GetFiles(outputDir, "*.png"))
            {
                File.Delete(file);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Random.InitState(CaptureSeed);

            MapConfig config = MapConfig.GetConfigForType(mapType);
            BuildMapScene(config);
            WriteSummary(outputDir, mapType, config, views);

            foreach (CaptureView view in views)
            {
                CaptureToPng(outputDir, view);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[MapCaptureTools] Captured {mapType} to {outputDir}");
        }

        private static void BuildMapScene(MapConfig config)
        {
            var root = new GameObject("MapCaptureRoot");

            CreateGround(root.transform, config);

            var environment = new GameObject("Environment");
            environment.transform.SetParent(root.transform);
            MapBuilderBase builder = CreateMapEnvironment(environment.transform, config);
            CreateLandmarks(environment.transform, builder);

            CreatePlayerMarker(root.transform);
        }

        private static void CreateGround(Transform parent, MapConfig config)
        {
            var groundParent = new GameObject("Ground");
            groundParent.transform.SetParent(parent);

            Sprite grassSprite = ProceduralSpriteGenerator.CreateGroundTile(0);
            Sprite pathSprite = ProceduralSpriteGenerator.CreateGroundTile(1);
            Sprite concreteSprite = ProceduralSpriteGenerator.CreateGroundTile(2);
            Sprite asphaltSprite = ProceduralSpriteGenerator.CreateGroundTile(3);

            for (int x = -config.halfWidth; x <= config.halfWidth; x++)
            {
                for (int y = -config.halfHeight; y <= config.halfHeight; y++)
                {
                    var tile = new GameObject($"T_{x}_{y}");
                    tile.transform.SetParent(groundParent.transform);
                    tile.transform.position = new Vector3(x, y, 0f);

                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -200;

                    int tileType = ResolveTileType(config, x, y);
                    sr.sprite = tileType switch
                    {
                        1 => pathSprite,
                        2 => concreteSprite,
                        3 => asphaltSprite,
                        _ => grassSprite
                    };

                    float shade = Random.Range(0.9f, 1.05f);
                    Color tint = config.groundTint;
                    sr.color = new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1f);
                }
            }
        }

        private static MapBuilderBase CreateMapEnvironment(Transform parent, MapConfig config)
        {
            MapBuilderBase builder = CreateMapBuilder(config.mapType);
            builder.Build(parent, config, (x, y) => ResolveTileType(config, x, y));
            return builder;
        }

        private static MapBuilderBase CreateMapBuilder(MapType mapType)
        {
            return mapType switch
            {
                MapType.Industrial => new IndustrialBuilder(),
                MapType.Suburban => new SuburbanBuilder(),
                _ => new TownCenterBuilder()
            };
        }

        private static void CreateLandmarks(Transform parent, MapBuilderBase builder)
        {
            builder?.BuildLandmarks(parent);
        }

        private static void CreatePlayerMarker(Transform parent)
        {
            var player = new GameObject("PlayerMarker");
            player.transform.SetParent(parent);
            player.transform.position = Vector3.zero;

            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreatePlayerSprite(0, 0);
            sr.sortingOrder = 20;
        }

        private static int ResolveTileType(MapConfig config, int x, int y)
        {
            return config.mapType switch
            {
                MapType.TownCenter => GetTileTypeTownCenter(config, x, y),
                MapType.Industrial => GetTileTypeIndustrial(config, x, y),
                MapType.Suburban => GetTileTypeSuburban(config, x, y),
                _ => GetTileTypeTownCenter(config, x, y)
            };
        }

        private static int GetTileTypeTownCenter(MapConfig config, int x, int y)
        {
            return TownCenterLayout.GetTileType(config, x, y);
        }

        private static int GetTileTypeIndustrial(MapConfig config, int x, int y)
        {
            return IndustrialLayout.GetTileType(config, x, y);
        }

        private static int GetTileTypeSuburban(MapConfig config, int x, int y)
        {
            return SuburbanLayout.GetTileType(config, x, y);
        }

        private static void CaptureToPng(string outputDir, CaptureView view)
        {
            var cameraObject = new GameObject($"CaptureCamera_{view.Name}");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = view.Position;
            camera.orthographic = true;
            camera.orthographicSize = view.OrthographicSize;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;

            var renderTexture = new RenderTexture(view.Width, view.Height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 1;

            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            var texture = new Texture2D(view.Width, view.Height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, view.Width, view.Height), 0, 0);
            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(outputDir, $"{view.Name}.png"), png);

            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
        }

        private static void WriteSummary(string outputDir, MapType mapType, MapConfig config, IReadOnlyList<CaptureView> views)
        {
            var spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

            var counts = new Dictionary<string, int>();
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                string name = sr.gameObject.name;
                if (!counts.ContainsKey(name))
                {
                    counts[name] = 0;
                }

                counts[name]++;
            }

            var names = new List<string>(counts.Keys);
            names.Sort();

            var sb = new StringBuilder();
            sb.AppendLine($"Map: {mapType}");
            sb.AppendLine($"Seed: {CaptureSeed}");
            sb.AppendLine($"Dimensions: {config.halfWidth} x {config.halfHeight}");
            sb.AppendLine($"StreetGridSpacing: {config.streetGridSpacing}");
            sb.AppendLine($"SpriteRenderers: {spriteRenderers.Length}");
            sb.AppendLine($"Colliders2D: {colliders.Length}");
            sb.AppendLine();
            sb.AppendLine("Views:");
            foreach (CaptureView view in views)
            {
                sb.AppendLine($"- {view.Name}: pos=({view.Position.x}, {view.Position.y}), ortho={view.OrthographicSize}, size={view.Width}x{view.Height}");
            }

            sb.AppendLine();
            sb.AppendLine("Sprite Counts:");
            foreach (string name in names)
            {
                sb.AppendLine($"- {name}: {counts[name]}");
            }

            File.WriteAllText(Path.Combine(outputDir, "summary.txt"), sb.ToString());
        }
    }
}
#endif
