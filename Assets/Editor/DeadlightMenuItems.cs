#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.Build.Reporting;

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

            BuildForTarget(path, BuildTarget.StandaloneWindows64, "Deadlight.exe", true);
        }

        // Non-interactive entry point for automated validation in batch mode.
        public static void BuildMacHeadlessTest()
        {
            string path = Path.GetFullPath("tmp/build_mac_test");
            Directory.CreateDirectory(path);

            bool success = BuildForTarget(path, BuildTarget.StandaloneOSX, "Deadlight.app", false);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(success ? 0 : 1);
            }
        }

        // Non-interactive Windows test path (requires Windows build support installed).
        public static void BuildWindowsHeadlessTest()
        {
            string path = Path.GetFullPath("tmp/build_windows_test");
            Directory.CreateDirectory(path);

            bool success = BuildForTarget(path, BuildTarget.StandaloneWindows64, "Deadlight.exe", false);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(success ? 0 : 1);
            }
        }

        private static bool BuildForTarget(string outputFolder, BuildTarget target, string outputName, bool showDialogs)
        {
            if (!IsTargetSupportInstalled(target, out string supportMessage))
            {
                Debug.LogError($"[DeadlightMenuItems] {supportMessage}");
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Build Failed", supportMessage, "OK");
                }
                return false;
            }

            var enabledScenePaths = GetValidBuildScenes();

            if (enabledScenePaths.Count == 0)
            {
                const string noScenesMessage = "No valid enabled scenes were found in Build Settings.";
                Debug.LogError($"[DeadlightMenuItems] {noScenesMessage}");
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Build Failed", noScenesMessage, "OK");
                }
                return false;
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = enabledScenePaths.ToArray(),
                locationPathName = Path.Combine(outputFolder, outputName),
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            bool success = report.summary.result == BuildResult.Succeeded;

            if (success)
            {
                Debug.Log($"[DeadlightMenuItems] Build succeeded for {target}: {buildOptions.locationPathName}");
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog(
                        "Build Complete",
                        $"Build created at:\n{buildOptions.locationPathName}",
                        "OK");
                }
            }
            else
            {
                string message = $"Build failed for {target}. Result={report.summary.result}, Errors={report.summary.totalErrors}.";
                Debug.LogError($"[DeadlightMenuItems] {message}");
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Build Failed", message, "OK");
                }
            }

            return success;
        }

        private static bool IsTargetSupportInstalled(BuildTarget target, out string message)
        {
            message = string.Empty;

            string requiredModuleFolder = target switch
            {
                BuildTarget.StandaloneWindows64 => "WindowsStandaloneSupport",
                BuildTarget.StandaloneOSX => "MacStandaloneSupport",
                BuildTarget.WebGL => "WebGLSupport",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(requiredModuleFolder))
            {
                return true;
            }

            var playbackRoots = new List<string>
            {
                Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines")
            };

            try
            {
                string appContents = EditorApplication.applicationContentsPath;
                string editorRoot = Directory.GetParent(appContents)?.Parent?.FullName;
                if (!string.IsNullOrEmpty(editorRoot))
                {
                    playbackRoots.Add(Path.Combine(editorRoot, "PlaybackEngines"));
                }
            }
            catch
            {
                // Fallback to the default root only.
            }

            playbackRoots = playbackRoots
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(Directory.Exists)
                .ToList();

            if (playbackRoots.Count == 0)
            {
                message = "Unity PlaybackEngines directory was not found. Cannot validate build target support.";
                return false;
            }

            bool moduleInstalled = playbackRoots.Any(root =>
                Directory.GetDirectories(root)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Any(name => string.Equals(name, requiredModuleFolder, StringComparison.OrdinalIgnoreCase)));

            if (moduleInstalled)
            {
                return true;
            }

            message = target switch
            {
                BuildTarget.StandaloneWindows64 =>
                    "Windows Build Support is not installed for this Unity editor. Install module: Unity Hub -> Installs -> Add modules -> Windows Build Support (IL2CPP).",
                BuildTarget.StandaloneOSX =>
                    "Mac Build Support is not installed for this Unity editor.",
                BuildTarget.WebGL =>
                    "WebGL Build Support is not installed for this Unity editor.",
                _ =>
                    $"Required build support module '{requiredModuleFolder}' is not installed for target {target}."
            };

            return false;
        }

        private static List<string> GetValidBuildScenes()
        {
            // Build only scenes that both exist and are enabled in Build Settings.
            // Falls back to known runtime scenes if Build Settings is empty/misaligned.
            var enabledScenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .Where(File.Exists)
                .ToList();

            if (enabledScenePaths.Count == 0)
            {
                var fallbackScenes = new List<string>
                {
                    "Assets/Scenes/Game.unity",
                    "Assets/Scenes/GameOver.unity"
                };

                enabledScenePaths = fallbackScenes
                    .Where(File.Exists)
                    .ToList();
            }
            
            return enabledScenePaths;
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
