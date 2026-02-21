#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Deadlight.Level;
using Deadlight.Narrative;

namespace Deadlight.Editor
{
    public class LevelEditorTools : EditorWindow
    {
        private Vector2 scrollPosition;
        private int selectedZoneType = 0;
        private string zoneName = "New Zone";
        private Vector2 zoneSize = new Vector2(10f, 10f);
        private int spawnActivationNight = 1;
        private float spawnWeight = 1f;

        [MenuItem("Deadlight/Level Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorTools>("Level Editor");
            window.minSize = new Vector2(350, 500);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Deadlight Level Editor", EditorStyles.boldLabel);
            GUILayout.Space(10);

            DrawZoneTools();
            GUILayout.Space(20);
            DrawSpawnPointTools();
            GUILayout.Space(20);
            DrawObstacleTools();
            GUILayout.Space(20);
            DrawNarrativeTools();
            GUILayout.Space(20);
            DrawQuickSetup();

            EditorGUILayout.EndScrollView();
        }

        private void DrawZoneTools()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Zone Creation", EditorStyles.boldLabel);

            zoneName = EditorGUILayout.TextField("Zone Name", zoneName);
            selectedZoneType = EditorGUILayout.Popup("Zone Type", selectedZoneType, 
                new string[] { "Safe Zone", "Resource Zone", "Danger Zone", "Spawn Zone" });
            zoneSize = EditorGUILayout.Vector2Field("Zone Size", zoneSize);

            if (GUILayout.Button("Create Zone"))
            {
                CreateZone();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSpawnPointTools()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Spawn Point Creation", EditorStyles.boldLabel);

            spawnActivationNight = EditorGUILayout.IntSlider("Activation Night", spawnActivationNight, 1, 5);
            spawnWeight = EditorGUILayout.Slider("Spawn Weight", spawnWeight, 0.1f, 2f);

            if (GUILayout.Button("Create Spawn Point"))
            {
                CreateSpawnPoint();
            }

            if (GUILayout.Button("Create Spawn Point at Scene View"))
            {
                CreateSpawnPointAtSceneView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawObstacleTools()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Obstacle Creation", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Wall"))
            {
                CreateObstacle(ObstacleType.Wall);
            }
            if (GUILayout.Button("Barricade"))
            {
                CreateObstacle(ObstacleType.Barricade);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cover"))
            {
                CreateObstacle(ObstacleType.Cover);
            }
            if (GUILayout.Button("Destructible"))
            {
                CreateObstacle(ObstacleType.Destructible);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawNarrativeTools()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Narrative Elements", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Story Trigger"))
            {
                CreateStoryTrigger();
            }

            if (GUILayout.Button("Create Lore Pickup"))
            {
                CreateLorePickup();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickSetup()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Quick Level Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Complete Town Layout"))
            {
                CreateTownLayout();
            }

            if (GUILayout.Button("Create Level Manager"))
            {
                CreateLevelManager();
            }

            if (GUILayout.Button("Create Narrative Manager"))
            {
                CreateNarrativeManager();
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateZone()
        {
            var zoneObj = new GameObject($"Zone_{zoneName}");
            var zone = zoneObj.AddComponent<MapZone>();

            var zoneType = (ZoneType)selectedZoneType;
            var serializedZone = new SerializedObject(zone);
            serializedZone.FindProperty("zoneName").stringValue = zoneName;
            serializedZone.FindProperty("zoneType").enumValueIndex = selectedZoneType;
            serializedZone.FindProperty("zoneSize").vector2Value = zoneSize;
            serializedZone.ApplyModifiedProperties();

            if (SceneView.lastActiveSceneView != null)
            {
                zoneObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Selection.activeGameObject = zoneObj;
            Undo.RegisterCreatedObjectUndo(zoneObj, "Create Zone");
        }

        private void CreateSpawnPoint()
        {
            var spawnObj = new GameObject("SpawnPoint");
            var spawn = spawnObj.AddComponent<SpawnPoint>();

            var serializedSpawn = new SerializedObject(spawn);
            serializedSpawn.FindProperty("activationNight").intValue = spawnActivationNight;
            serializedSpawn.FindProperty("spawnWeight").floatValue = spawnWeight;
            serializedSpawn.ApplyModifiedProperties();

            if (Selection.activeTransform != null)
            {
                spawnObj.transform.SetParent(Selection.activeTransform);
                spawnObj.transform.localPosition = Vector3.zero;
            }

            Selection.activeGameObject = spawnObj;
            Undo.RegisterCreatedObjectUndo(spawnObj, "Create Spawn Point");
        }

        private void CreateSpawnPointAtSceneView()
        {
            var spawnObj = new GameObject("SpawnPoint");
            var spawn = spawnObj.AddComponent<SpawnPoint>();

            var serializedSpawn = new SerializedObject(spawn);
            serializedSpawn.FindProperty("activationNight").intValue = spawnActivationNight;
            serializedSpawn.FindProperty("spawnWeight").floatValue = spawnWeight;
            serializedSpawn.ApplyModifiedProperties();

            if (SceneView.lastActiveSceneView != null)
            {
                spawnObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Selection.activeGameObject = spawnObj;
            Undo.RegisterCreatedObjectUndo(spawnObj, "Create Spawn Point");
        }

        private void CreateObstacle(ObstacleType type)
        {
            var obstacleObj = new GameObject($"Obstacle_{type}");
            var obstacle = obstacleObj.AddComponent<Obstacle>();
            var collider = obstacleObj.AddComponent<BoxCollider2D>();
            var sprite = obstacleObj.AddComponent<SpriteRenderer>();

            sprite.sprite = CreatePlaceholderSprite(type == ObstacleType.Wall ? Color.gray : Color.yellow);

            var serializedObstacle = new SerializedObject(obstacle);
            serializedObstacle.FindProperty("obstacleType").enumValueIndex = (int)type;
            serializedObstacle.FindProperty("obstacleName").stringValue = type.ToString();
            serializedObstacle.ApplyModifiedProperties();

            if (SceneView.lastActiveSceneView != null)
            {
                obstacleObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Selection.activeGameObject = obstacleObj;
            Undo.RegisterCreatedObjectUndo(obstacleObj, $"Create {type}");
        }

        private void CreateStoryTrigger()
        {
            var triggerObj = new GameObject("StoryTrigger");
            triggerObj.AddComponent<StoryTrigger>();
            var collider = triggerObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(3f, 3f);

            if (SceneView.lastActiveSceneView != null)
            {
                triggerObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Selection.activeGameObject = triggerObj;
            Undo.RegisterCreatedObjectUndo(triggerObj, "Create Story Trigger");
        }

        private void CreateLorePickup()
        {
            var loreObj = new GameObject("LorePickup");
            loreObj.AddComponent<LorePickup>();
            var sprite = loreObj.AddComponent<SpriteRenderer>();
            sprite.sprite = CreatePlaceholderSprite(new Color(1f, 0.8f, 0f));
            sprite.sortingOrder = 5;

            if (SceneView.lastActiveSceneView != null)
            {
                loreObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Selection.activeGameObject = loreObj;
            Undo.RegisterCreatedObjectUndo(loreObj, "Create Lore Pickup");
        }

        private void CreateLevelManager()
        {
            if (FindObjectOfType<LevelManager>() != null)
            {
                EditorUtility.DisplayDialog("Level Manager Exists", 
                    "A Level Manager already exists in the scene.", "OK");
                return;
            }

            var managerObj = new GameObject("LevelManager");
            managerObj.AddComponent<LevelManager>();

            Selection.activeGameObject = managerObj;
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Level Manager");
        }

        private void CreateNarrativeManager()
        {
            if (FindObjectOfType<NarrativeManager>() != null)
            {
                EditorUtility.DisplayDialog("Narrative Manager Exists", 
                    "A Narrative Manager already exists in the scene.", "OK");
                return;
            }

            var managerObj = new GameObject("NarrativeManager");
            managerObj.AddComponent<NarrativeManager>();
            managerObj.AddComponent<EnvironmentalLore>();

            Selection.activeGameObject = managerObj;
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Narrative Manager");
        }

        private void CreateTownLayout()
        {
            var levelRoot = new GameObject("Level_AbandonedTown");

            var levelManager = levelRoot.AddComponent<LevelManager>();

            CreateZoneInLayout(levelRoot.transform, "SafeZone_Start", ZoneType.SafeZone, 
                new Vector3(0, -15, 0), new Vector2(15, 10));

            CreateZoneInLayout(levelRoot.transform, "ResourceZone_HardwareStore", ZoneType.ResourceZone, 
                new Vector3(-8, 0, 0), new Vector2(10, 8));

            CreateZoneInLayout(levelRoot.transform, "ResourceZone_GunShop", ZoneType.ResourceZone, 
                new Vector3(8, 0, 0), new Vector2(10, 8));

            CreateZoneInLayout(levelRoot.transform, "DangerZone_GasStation", ZoneType.DangerZone, 
                new Vector3(-8, 15, 0), new Vector2(10, 8));

            CreateZoneInLayout(levelRoot.transform, "DangerZone_North", ZoneType.DangerZone, 
                new Vector3(8, 15, 0), new Vector2(10, 8));

            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(-15, 10, 0), 1, 1f);
            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(15, 10, 0), 1, 1f);
            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(-15, -5, 0), 2, 0.8f);
            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(15, -5, 0), 2, 0.8f);
            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(0, 20, 0), 3, 1.2f);
            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(-20, 0, 0), 4, 1f);
            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(20, 0, 0), 4, 1f);
            CreateSpawnPointInLayout(levelRoot.transform, new Vector3(0, 25, 0), 5, 1.5f);

            var playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.SetParent(levelRoot.transform);
            playerSpawn.transform.position = new Vector3(0, -15, 0);

            Selection.activeGameObject = levelRoot;
            Undo.RegisterCreatedObjectUndo(levelRoot, "Create Town Layout");

            EditorUtility.DisplayDialog("Town Layout Created", 
                "Created level with:\n" +
                "- 1 Safe Zone (player start)\n" +
                "- 2 Resource Zones (Hardware Store, Gun Shop)\n" +
                "- 2 Danger Zones (Gas Station, North)\n" +
                "- 8 Spawn Points (activating on different nights)\n" +
                "- Player Spawn Point", "OK");
        }

        private void CreateZoneInLayout(Transform parent, string name, ZoneType type, Vector3 position, Vector2 size)
        {
            var zoneObj = new GameObject(name);
            zoneObj.transform.SetParent(parent);
            zoneObj.transform.position = position;

            var zone = zoneObj.AddComponent<MapZone>();
            var serialized = new SerializedObject(zone);
            serialized.FindProperty("zoneName").stringValue = name;
            serialized.FindProperty("zoneType").enumValueIndex = (int)type;
            serialized.FindProperty("zoneSize").vector2Value = size;

            if (type == ZoneType.DangerZone)
            {
                serialized.FindProperty("dangerLevel").floatValue = 0.7f;
                serialized.FindProperty("lootMultiplier").floatValue = 1.5f;
            }
            else if (type == ZoneType.ResourceZone)
            {
                serialized.FindProperty("lootMultiplier").floatValue = 1.3f;
            }

            serialized.ApplyModifiedProperties();
        }

        private void CreateSpawnPointInLayout(Transform parent, Vector3 position, int activationNight, float weight)
        {
            var spawnObj = new GameObject($"SpawnPoint_N{activationNight}");
            spawnObj.transform.SetParent(parent);
            spawnObj.transform.position = position;

            var spawn = spawnObj.AddComponent<SpawnPoint>();
            var serialized = new SerializedObject(spawn);
            serialized.FindProperty("activationNight").intValue = activationNight;
            serialized.FindProperty("spawnWeight").floatValue = weight;
            serialized.ApplyModifiedProperties();
        }

        private Sprite CreatePlaceholderSprite(Color color)
        {
            int size = 32;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }

    public static class LevelEditorMenuItems
    {
        [MenuItem("Deadlight/Level/Create Zone %#z")]
        public static void CreateZoneShortcut()
        {
            LevelEditorTools.ShowWindow();
        }

        [MenuItem("Deadlight/Level/Create Spawn Point %#s")]
        public static void CreateSpawnPointShortcut()
        {
            var spawnObj = new GameObject("SpawnPoint");
            spawnObj.AddComponent<SpawnPoint>();

            if (SceneView.lastActiveSceneView != null)
            {
                spawnObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Selection.activeGameObject = spawnObj;
            Undo.RegisterCreatedObjectUndo(spawnObj, "Create Spawn Point");
        }
    }
}
#endif
