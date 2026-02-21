#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Deadlight.Editor
{
    public static class DeadlightMenuItems
    {
        [MenuItem("Deadlight/Create/Weapon Data")]
        public static void CreateWeaponData()
        {
            CreateScriptableObject<Data.WeaponData>("NewWeapon");
        }

        [MenuItem("Deadlight/Create/Enemy Data")]
        public static void CreateEnemyData()
        {
            CreateScriptableObject<Data.EnemyData>("NewEnemy");
        }

        [MenuItem("Deadlight/Create/Night Config")]
        public static void CreateNightConfig()
        {
            CreateScriptableObject<Core.NightConfig>("Night_X");
        }

        [MenuItem("Deadlight/Create/Difficulty Settings")]
        public static void CreateDifficultySettings()
        {
            CreateScriptableObject<Core.DifficultySettings>("NewDifficulty");
        }

        private static void CreateScriptableObject<T>(string defaultName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {typeof(T).Name}",
                defaultName,
                "asset",
                $"Create a new {typeof(T).Name}");
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
        }

        [MenuItem("Deadlight/Play Game")]
        public static void PlayGame()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            else
            {
                string gamePath = "Assets/Scenes/Game.unity";
                if (System.IO.File.Exists(gamePath))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(gamePath);
                    EditorApplication.isPlaying = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("Scene Not Found", 
                        "Game scene not found. Run Setup Wizard first.", "OK");
                }
            }
        }

        [MenuItem("Deadlight/Build Windows")]
        public static void BuildWindows()
        {
            string path = EditorUtility.SaveFolderPanel("Choose Build Location", "", "");
            if (string.IsNullOrEmpty(path)) return;

            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Game.unity",
                "Assets/Scenes/GameOver.unity"
            };

            BuildPipeline.BuildPlayer(scenes, 
                System.IO.Path.Combine(path, "Deadlight.exe"),
                BuildTarget.StandaloneWindows64, 
                BuildOptions.None);

            EditorUtility.DisplayDialog("Build Complete", 
                $"Windows build created at:\n{path}", "OK");
        }

        [MenuItem("Deadlight/Documentation/Open README")]
        public static void OpenReadme()
        {
            string readmePath = System.IO.Path.Combine(Application.dataPath, "../README.md");
            if (System.IO.File.Exists(readmePath))
            {
                Application.OpenURL("file://" + readmePath);
            }
        }

        [MenuItem("Deadlight/Documentation/Open Setup Guide")]
        public static void OpenSetupGuide()
        {
            string guidePath = System.IO.Path.Combine(Application.dataPath, "../SETUP_GUIDE.md");
            if (System.IO.File.Exists(guidePath))
            {
                Application.OpenURL("file://" + guidePath);
            }
        }
    }
}
#endif
