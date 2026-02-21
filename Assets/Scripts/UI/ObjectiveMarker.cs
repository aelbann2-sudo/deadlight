using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using System.Collections.Generic;

namespace Deadlight.UI
{
    public class ObjectiveMarker : MonoBehaviour
    {
        private Canvas markerCanvas;
        private readonly List<MarkerData> markers = new List<MarkerData>();
        private Font font;

        private class MarkerData
        {
            public Transform target;
            public RectTransform uiRoot;
            public Image arrow;
            public Text distText;
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            CreateCanvas();

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.DayPhase)
            {
                StartCoroutine(FindTargetsDelayed());
            }
            else
            {
                ClearMarkers();
            }
        }

        private System.Collections.IEnumerator FindTargetsDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            RefreshTargets();
        }

        public void RefreshTargets()
        {
            ClearMarkers();

            var zones = FindObjectsOfType<ObjectiveZone>();
            foreach (var z in zones)
                AddMarker(z.transform, new Color(0.3f, 0.7f, 1f));

            var beacons = FindObjectsOfType<ObjectiveBeacon>();
            foreach (var b in beacons)
                AddMarker(b.transform, new Color(0.3f, 0.5f, 1f));

            var caches = FindObjectsOfType<ObjectiveCache>();
            foreach (var c in caches)
                AddMarker(c.transform, new Color(0.9f, 0.75f, 0.2f));

            var crates = FindObjectsOfType<Systems.SupplyCrate>();
            foreach (var cr in crates)
                AddMarker(cr.transform, new Color(0.7f, 0.5f, 0.2f, 0.6f));
        }

        private void AddMarker(Transform target, Color color)
        {
            if (markerCanvas == null) return;

            var root = new GameObject("Marker");
            root.transform.SetParent(markerCanvas.transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(30, 30);

            var arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(root.transform, false);
            var arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(20, 20);
            var arrowImg = arrowObj.AddComponent<Image>();
            arrowImg.color = color;
            arrowImg.sprite = CreateDiamondSprite(color);

            var distObj = new GameObject("Dist");
            distObj.transform.SetParent(root.transform, false);
            var distRect = distObj.AddComponent<RectTransform>();
            distRect.anchoredPosition = new Vector2(0, -18);
            distRect.sizeDelta = new Vector2(60, 16);
            var distText = distObj.AddComponent<Text>();
            distText.font = font;
            distText.fontSize = 12;
            distText.alignment = TextAnchor.MiddleCenter;
            distText.color = color;

            markers.Add(new MarkerData
            {
                target = target,
                uiRoot = rootRect,
                arrow = arrowImg,
                distText = distText
            });
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float margin = 40f;
            float screenW = Screen.width;
            float screenH = Screen.height;

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var m = markers[i];
                if (m.target == null)
                {
                    if (m.uiRoot != null) Destroy(m.uiRoot.gameObject);
                    markers.RemoveAt(i);
                    continue;
                }

                Vector3 screenPos = cam.WorldToScreenPoint(m.target.position);
                float dist = Vector2.Distance(cam.transform.position, m.target.position);

                bool onScreen = screenPos.z > 0 &&
                    screenPos.x > margin && screenPos.x < screenW - margin &&
                    screenPos.y > margin && screenPos.y < screenH - margin;

                if (onScreen)
                {
                    m.uiRoot.gameObject.SetActive(dist > 4f);
                    m.uiRoot.position = screenPos;
                    m.arrow.transform.rotation = Quaternion.identity;
                }
                else
                {
                    m.uiRoot.gameObject.SetActive(true);

                    if (screenPos.z < 0)
                    {
                        screenPos.x = screenW - screenPos.x;
                        screenPos.y = screenH - screenPos.y;
                    }

                    screenPos.x = Mathf.Clamp(screenPos.x, margin, screenW - margin);
                    screenPos.y = Mathf.Clamp(screenPos.y, margin, screenH - margin);
                    m.uiRoot.position = screenPos;

                    Vector2 dir = ((Vector2)cam.WorldToScreenPoint(m.target.position) - new Vector2(screenW / 2, screenH / 2)).normalized;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    m.arrow.transform.rotation = Quaternion.Euler(0, 0, angle - 45f);
                }

                m.distText.text = $"{dist:F0}m";
            }
        }

        private void ClearMarkers()
        {
            foreach (var m in markers)
            {
                if (m.uiRoot != null) Destroy(m.uiRoot.gameObject);
            }
            markers.Clear();
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
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            Vector2 center = new Vector2(s / 2f, s / 2f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    px[y * s + x] = (dx + dy <= s / 2f) ? color : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
    }
}
