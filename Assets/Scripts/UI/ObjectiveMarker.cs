using System.Collections;
using System.Collections.Generic;
using Deadlight.Core;
using Deadlight.Enemy;
using Deadlight.Narrative;
using Deadlight.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.UI
{
    public class ObjectiveMarker : MonoBehaviour
    {
        private Canvas markerCanvas;
        private readonly List<MarkerData> markers = new List<MarkerData>();
        private readonly HashSet<SupplyCrate> lootedCrates = new HashSet<SupplyCrate>();
        private Font font;
        private Transform player;
        private float nextSupportRefreshTime;
        private DayObjectiveSystem boundObjectiveSystem;
        private Coroutine delayedFindCoroutine;

        private const float MarkerHideDistance = 4f;
        private const float SupportRefreshInterval = 0.35f;
        private const float EnemyMarkerMinDistance = 6f;

        private static readonly Color MissionMarkerColor = new Color(1f, 0.78f, 0.25f);
        private static readonly Color DropMarkerColor = new Color(0.35f, 0.85f, 1f);
        private static readonly Color EnemyMarkerColor = new Color(1f, 0.38f, 0.32f);

        private enum MarkerKind
        {
            Mission,
            Drop,
            EnemyHint
        }

        private class MarkerData
        {
            public Transform target;
            public RectTransform uiRoot;
            public Image arrow;
            public Text distText;
            public MarkerKind kind;
            public string prefix;
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            CreateCanvas();
            CachePlayerTransform();
            BindObjectiveSystem();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            SupplyCrate.OnCrateSuccessfullyLooted += OnCrateSuccessfullyLooted;
            RefreshTargets();
            RefreshSupportMarkers();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            if (delayedFindCoroutine != null)
            {
                StopCoroutine(delayedFindCoroutine);
                delayedFindCoroutine = null;
            }

            UnbindObjectiveSystem();
            SupplyCrate.OnCrateSuccessfullyLooted -= OnCrateSuccessfullyLooted;
            ClearMarkers();
        }

        private void BindObjectiveSystem()
        {
            RebindObjectiveSystem(DayObjectiveSystem.Instance);
        }

        private void UnbindObjectiveSystem()
        {
            if (boundObjectiveSystem == null)
            {
                return;
            }

            boundObjectiveSystem.OnObjectiveGenerated -= HandleObjectiveChanged;
            boundObjectiveSystem.OnObjectiveCompleted -= HandleObjectiveChanged;
            boundObjectiveSystem = null;
        }

        private void RebindObjectiveSystem(DayObjectiveSystem nextSystem)
        {
            if (boundObjectiveSystem == nextSystem)
            {
                return;
            }

            UnbindObjectiveSystem();
            boundObjectiveSystem = nextSystem;

            if (boundObjectiveSystem == null)
            {
                return;
            }

            boundObjectiveSystem.OnObjectiveGenerated += HandleObjectiveChanged;
            boundObjectiveSystem.OnObjectiveCompleted += HandleObjectiveChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (delayedFindCoroutine != null)
            {
                StopCoroutine(delayedFindCoroutine);
                delayedFindCoroutine = null;
            }

            if (state == GameState.DayPhase)
            {
                delayedFindCoroutine = StartCoroutine(FindTargetsDelayed());
                return;
            }

            if (state == GameState.NightPhase)
            {
                ClearMarkers(MarkerKind.Mission);
                RefreshSupportMarkers();
                return;
            }

            ClearMarkers();
        }

        private void HandleObjectiveChanged(DayObjective objective)
        {
            RefreshTargets();
        }

        private IEnumerator FindTargetsDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            delayedFindCoroutine = null;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.DayPhase)
            {
                yield break;
            }

            RefreshTargets();
            RefreshSupportMarkers();
        }

        public void RefreshTargets()
        {
            BindObjectiveSystem();
            ClearMarkers(MarkerKind.Mission);

            var storyTargets = FindObjectsByType<StoryObjectiveTarget>(FindObjectsSortMode.None);
            foreach (var target in storyTargets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddMarker(target.transform, MissionMarkerColor, MarkerKind.Mission, "OBJ ");
            }

            SyncObjectiveInteractableMarkers();
        }

        public void PingContestedDrop(Transform dropTransform)
        {
            if (dropTransform == null)
            {
                return;
            }

            AddMarker(dropTransform, DropMarkerColor, MarkerKind.Drop, "DROP ");
        }

        private MarkerData AddMarker(Transform target, Color color, MarkerKind kind, string prefix)
        {
            if (markerCanvas == null || target == null)
            {
                return null;
            }

            var existing = FindMarker(target, kind);
            if (existing != null)
            {
                ApplyMarkerColor(existing, color);
                existing.prefix = prefix;
                return existing;
            }

            var root = new GameObject("Marker");
            root.transform.SetParent(markerCanvas.transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(34f, 34f);

            var arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(root.transform, false);
            var arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(22f, 22f);
            var arrowImg = arrowObj.AddComponent<Image>();
            arrowImg.color = color;
            arrowImg.sprite = CreateDiamondSprite(color);

            var distObj = new GameObject("Dist");
            distObj.transform.SetParent(root.transform, false);
            var distRect = distObj.AddComponent<RectTransform>();
            distRect.anchoredPosition = new Vector2(0f, -19f);
            distRect.sizeDelta = new Vector2(96f, 16f);
            var distText = distObj.AddComponent<Text>();
            distText.font = font;
            distText.fontSize = 12;
            distText.alignment = TextAnchor.MiddleCenter;
            distText.color = color;

            var marker = new MarkerData
            {
                target = target,
                uiRoot = rootRect,
                arrow = arrowImg,
                distText = distText,
                kind = kind,
                prefix = prefix
            };

            markers.Add(marker);
            return marker;
        }

        private void ApplyMarkerColor(MarkerData marker, Color color)
        {
            if (marker == null)
            {
                return;
            }

            if (marker.arrow != null)
            {
                marker.arrow.color = color;
            }

            if (marker.distText != null)
            {
                marker.distText.color = color;
            }
        }

        private MarkerData FindMarker(Transform target, MarkerKind kind)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];
                if (marker.kind == kind && marker.target == target)
                {
                    return marker;
                }
            }

            return null;
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            if (Time.time >= nextSupportRefreshTime)
            {
                nextSupportRefreshTime = Time.time + SupportRefreshInterval;
                RefreshSupportMarkers();
            }

            float margin = 40f;
            float screenW = Screen.width;
            float screenH = Screen.height;

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var marker = markers[i];
                if (ShouldCullMarker(marker))
                {
                    RemoveMarkerAt(i);
                    continue;
                }

                Vector3 screenPos = cam.WorldToScreenPoint(marker.target.position);
                float dist = Vector2.Distance(cam.transform.position, marker.target.position);

                bool onScreen = screenPos.z > 0f &&
                                screenPos.x > margin && screenPos.x < screenW - margin &&
                                screenPos.y > margin && screenPos.y < screenH - margin;

                if (onScreen)
                {
                    marker.uiRoot.gameObject.SetActive(dist > MarkerHideDistance);
                    marker.uiRoot.position = screenPos;
                    marker.arrow.transform.rotation = Quaternion.identity;
                }
                else
                {
                    marker.uiRoot.gameObject.SetActive(true);

                    if (screenPos.z < 0f)
                    {
                        screenPos.x = screenW - screenPos.x;
                        screenPos.y = screenH - screenPos.y;
                    }

                    screenPos.x = Mathf.Clamp(screenPos.x, margin, screenW - margin);
                    screenPos.y = Mathf.Clamp(screenPos.y, margin, screenH - margin);
                    marker.uiRoot.position = screenPos;

                    Vector2 dir = ((Vector2)cam.WorldToScreenPoint(marker.target.position) -
                                   new Vector2(screenW * 0.5f, screenH * 0.5f)).normalized;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    marker.arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle - 45f);
                }

                string prefix = string.IsNullOrEmpty(marker.prefix) ? string.Empty : marker.prefix;
                marker.distText.text = $"{prefix}{dist:F0}m";
            }
        }

        private void RefreshSupportMarkers()
        {
            PruneLootedCrates();

            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                ClearMarkers(MarkerKind.Drop);
                ClearMarkers(MarkerKind.EnemyHint);
                return;
            }

            bool isDay = gameManager.CurrentState == GameState.DayPhase;
            bool isNight = gameManager.CurrentState == GameState.NightPhase;

            if (!isDay && !isNight)
            {
                ClearMarkers(MarkerKind.Drop);
                ClearMarkers(MarkerKind.EnemyHint);
                return;
            }

            SyncDropMarkers();

            if (isNight)
            {
                SyncNearestEnemyMarker();
            }
            else
            {
                ClearMarkers(MarkerKind.EnemyHint);
            }
        }

        private void SyncDropMarkers()
        {
            var crates = FindObjectsByType<SupplyCrate>(FindObjectsSortMode.None);
            var activeCrates = new HashSet<Transform>();

            for (int i = 0; i < crates.Length; i++)
            {
                var crate = crates[i];
                if (crate == null || !crate.gameObject.activeInHierarchy || lootedCrates.Contains(crate))
                {
                    continue;
                }

                activeCrates.Add(crate.transform);
            }

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var marker = markers[i];
                if (marker.kind != MarkerKind.Drop)
                {
                    continue;
                }

                if (marker.target == null || !activeCrates.Contains(marker.target))
                {
                    RemoveMarkerAt(i);
                }
            }

            foreach (var crateTransform in activeCrates)
            {
                AddMarker(crateTransform, DropMarkerColor, MarkerKind.Drop, "DROP ");
            }
        }

        private void SyncObjectiveInteractableMarkers()
        {
            var activeObjective = DayObjectiveSystem.Instance != null ? DayObjectiveSystem.Instance.ActiveObjective : null;
            if (activeObjective == null || activeObjective.IsComplete)
            {
                return;
            }

            var zones = FindObjectsByType<ObjectiveZone>(FindObjectsSortMode.None);
            foreach (var zone in zones)
            {
                if (zone == null || !zone.gameObject.activeInHierarchy || zone.IsComplete)
                {
                    continue;
                }

                AddMarker(zone.transform, MissionMarkerColor, MarkerKind.Mission, "ZONE ");
            }

            var beacons = FindObjectsByType<ObjectiveBeacon>(FindObjectsSortMode.None);
            foreach (var beacon in beacons)
            {
                if (beacon == null || !beacon.gameObject.activeInHierarchy || beacon.IsComplete)
                {
                    continue;
                }

                AddMarker(beacon.transform, MissionMarkerColor, MarkerKind.Mission, "BEACON ");
            }

            var caches = FindObjectsByType<ObjectiveCache>(FindObjectsSortMode.None);
            foreach (var cache in caches)
            {
                if (cache == null || !cache.gameObject.activeInHierarchy || cache.IsComplete)
                {
                    continue;
                }

                AddMarker(cache.transform, MissionMarkerColor, MarkerKind.Mission, "CACHE ");
            }
        }

        private void SyncNearestEnemyMarker()
        {
            CachePlayerTransform();
            if (player == null)
            {
                ClearMarkers(MarkerKind.EnemyHint);
                return;
            }

            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            Transform nearest = null;
            float nearestSqrDist = float.MaxValue;

            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || !enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 offset = enemy.transform.position - player.position;
                float sqrDist = offset.sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = enemy.transform;
                }
            }

            if (nearest == null || nearestSqrDist < EnemyMarkerMinDistance * EnemyMarkerMinDistance)
            {
                ClearMarkers(MarkerKind.EnemyHint);
                return;
            }

            RemoveDuplicateEnemyMarkers(nearest);
            AddMarker(nearest, EnemyMarkerColor, MarkerKind.EnemyHint, "ENEMY ");
        }

        private void RemoveDuplicateEnemyMarkers(Transform keepTarget)
        {
            bool keptOne = false;

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var marker = markers[i];
                if (marker.kind != MarkerKind.EnemyHint)
                {
                    continue;
                }

                if (!keptOne && marker.target == keepTarget)
                {
                    keptOne = true;
                    continue;
                }

                RemoveMarkerAt(i);
            }
        }

        private void CachePlayerTransform()
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                return;
            }

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Player");
            }

            player = playerObj != null ? playerObj.transform : null;
        }

        private static bool IsCompletedMissionTarget(Transform target)
        {
            if (target == null)
            {
                return true;
            }

            var zone = target.GetComponent<ObjectiveZone>();
            if (zone != null)
            {
                return zone.IsComplete;
            }

            var beacon = target.GetComponent<ObjectiveBeacon>();
            if (beacon != null)
            {
                return beacon.IsComplete;
            }

            var cache = target.GetComponent<ObjectiveCache>();
            if (cache != null)
            {
                return cache.IsComplete;
            }

            return false;
        }

        private void ClearMarkers()
        {
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                RemoveMarkerAt(i);
            }
        }

        private void ClearMarkers(MarkerKind kind)
        {
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                if (markers[i].kind == kind)
                {
                    RemoveMarkerAt(i);
                }
            }
        }

        private void OnCrateSuccessfullyLooted(SupplyCrate crate)
        {
            if (crate == null)
            {
                return;
            }

            lootedCrates.Add(crate);
            RemoveMarkersForTarget(crate.transform, MarkerKind.Drop);
        }

        private void RemoveMarkersForTarget(Transform target, MarkerKind kind)
        {
            if (target == null)
            {
                return;
            }

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                if (markers[i].kind == kind && markers[i].target == target)
                {
                    RemoveMarkerAt(i);
                }
            }
        }

        private void PruneLootedCrates()
        {
            if (lootedCrates.Count == 0)
            {
                return;
            }

            var expiredCrates = new List<SupplyCrate>();
            foreach (var crate in lootedCrates)
            {
                if (crate == null || !crate.gameObject.activeInHierarchy)
                {
                    expiredCrates.Add(crate);
                }
            }

            for (int i = 0; i < expiredCrates.Count; i++)
            {
                lootedCrates.Remove(expiredCrates[i]);
            }
        }

        private void RemoveMarkerAt(int index)
        {
            if (index < 0 || index >= markers.Count)
            {
                return;
            }

            var marker = markers[index];
            if (marker.arrow != null && marker.arrow.sprite != null)
            {
                if (marker.arrow.sprite.texture != null)
                {
                    Destroy(marker.arrow.sprite.texture);
                }

                Destroy(marker.arrow.sprite);
            }

            if (marker.uiRoot != null)
            {
                Destroy(marker.uiRoot.gameObject);
            }

            markers.RemoveAt(index);
        }

        private bool ShouldCullMarker(MarkerData marker)
        {
            if (marker == null || marker.target == null || !marker.target.gameObject.activeInHierarchy)
            {
                return true;
            }

            return marker.kind switch
            {
                MarkerKind.Mission => IsCompletedMissionTarget(marker.target),
                MarkerKind.Drop => !IsDroppedMarkerStillValid(marker.target),
                MarkerKind.EnemyHint => !IsEnemyTargetAlive(marker.target),
                _ => false
            };
        }

        private bool IsDroppedMarkerStillValid(Transform target)
        {
            var crate = target != null ? target.GetComponent<SupplyCrate>() : null;
            return crate != null && !lootedCrates.Contains(crate);
        }

        private static bool IsEnemyTargetAlive(Transform target)
        {
            var health = target != null ? target.GetComponent<EnemyHealth>() : null;
            return health != null && health.IsAlive;
        }

        private void CreateCanvas()
        {
            var canvasObj = new GameObject("ObjectiveMarkerCanvas");
            canvasObj.transform.SetParent(transform);
            markerCanvas = canvasObj.AddComponent<Canvas>();
            markerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            markerCanvas.sortingOrder = 95;
            canvasObj.AddComponent<CanvasScaler>();
        }

        private static Sprite CreateDiamondSprite(Color color)
        {
            int size = 16;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    pixels[y * size + x] = (dx + dy <= size * 0.5f) ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
