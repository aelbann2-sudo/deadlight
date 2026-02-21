using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Deadlight.Core;
using Deadlight.Systems;

namespace Deadlight.Narrative
{
    public enum ObjectiveType
    {
        Survive,
        ReachLocation,
        DefendPoint,
        DestroyTarget,
        KillBoss
    }

    public class StoryObjective : MonoBehaviour
    {
        public static StoryObjective Instance { get; private set; }

        [Header("Current Objective")]
        private ObjectiveType currentType;
        private string currentDescription;
        private bool isObjectiveActive;
        private bool isObjectiveComplete;

        [Header("UI")]
        private Text objectiveText;
        private Image objectiveMarker;
        private Transform targetLocation;

        [Header("Rewards")]
        private int pointReward;
        private bool grantsPowerup;

        private static readonly ObjectiveData[] nightObjectives = {
            new ObjectiveData(1, ObjectiveType.Survive, "SURVIVE THE NIGHT", 100, false, Vector3.zero),
            new ObjectiveData(2, ObjectiveType.ReachLocation, "REACH THE RADIO TOWER", 150, false, new Vector3(-9, 0, 0)),
            new ObjectiveData(3, ObjectiveType.DefendPoint, "DEFEND THE CRASH SITE", 200, false, new Vector3(0, 10, 0)),
            new ObjectiveData(4, ObjectiveType.DestroyTarget, "DESTROY THE INFECTED NEST", 250, true, new Vector3(0, -10, 0)),
            new ObjectiveData(5, ObjectiveType.KillBoss, "KILL SUBJECT 23", 500, true, Vector3.zero)
        };

        private struct ObjectiveData
        {
            public int night;
            public ObjectiveType type;
            public string description;
            public int reward;
            public bool powerup;
            public Vector3 location;

            public ObjectiveData(int n, ObjectiveType t, string d, int r, bool p, Vector3 l)
            {
                night = n;
                type = t;
                description = d;
                reward = r;
                powerup = p;
                location = l;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CreateUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.NightPhase)
            {
                int night = GameManager.Instance?.CurrentNight ?? 1;
                StartNightObjective(night);
            }
            else if (state == GameState.DayPhase)
            {
                if (isObjectiveActive && !isObjectiveComplete)
                {
                    CompleteObjective();
                }
            }
        }

        private void StartNightObjective(int night)
        {
            if (night < 1 || night > nightObjectives.Length) return;

            var objective = nightObjectives[night - 1];
            currentType = objective.type;
            currentDescription = objective.description;
            pointReward = objective.reward;
            grantsPowerup = objective.powerup;
            isObjectiveActive = true;
            isObjectiveComplete = false;

            if (objective.location != Vector3.zero)
            {
                CreateObjectiveMarker(objective.location);
                targetLocation = objectiveMarker?.transform;
            }

            ShowObjectiveAnnouncement();
            UpdateUI();
        }

        private void ShowObjectiveAnnouncement()
        {
            if (RadioTransmissions.Instance != null)
            {
                RadioTransmissions.Instance.ShowMessage($"OBJECTIVE: {currentDescription}", 4f);
            }

            if (FloatingTextManager.Instance != null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    FloatingTextManager.Instance.SpawnText("NEW OBJECTIVE", player.transform.position + Vector3.up * 2f,
                        new Color(1f, 0.9f, 0.3f), 2f, 28);
                }
            }
        }

        private void Update()
        {
            if (!isObjectiveActive || isObjectiveComplete) return;

            CheckObjectiveProgress();
            UpdateMarkerPosition();
        }

        private void CheckObjectiveProgress()
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            switch (currentType)
            {
                case ObjectiveType.ReachLocation:
                    if (targetLocation != null)
                    {
                        float dist = Vector3.Distance(player.transform.position, targetLocation.position);
                        if (dist < 2f)
                        {
                            CompleteObjective();
                        }
                    }
                    break;

                case ObjectiveType.DefendPoint:
                    if (targetLocation != null)
                    {
                        float dist = Vector3.Distance(player.transform.position, targetLocation.position);
                        if (dist > 8f)
                        {
                            ShowWarning("Stay near the objective!");
                        }
                    }
                    break;
            }
        }

        private void ShowWarning(string message)
        {
            if (FloatingTextManager.Instance != null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    FloatingTextManager.Instance.SpawnText(message, player.transform.position + Vector3.up,
                        new Color(1f, 0.4f, 0.3f), 1f, 20);
                }
            }
        }

        public void CompleteObjective()
        {
            if (isObjectiveComplete) return;
            isObjectiveComplete = true;
            isObjectiveActive = false;

            if (PointsSystem.Instance != null)
            {
                PointsSystem.Instance.AddPoints(pointReward, "Objective Complete");
            }

            if (grantsPowerup && PowerupSystem.Instance != null)
            {
                PowerupSystem.Instance.GrantRandomPowerup();
            }

            if (RadioTransmissions.Instance != null)
            {
                RadioTransmissions.Instance.ShowMessage($"OBJECTIVE COMPLETE! +{pointReward} POINTS", 3f);
            }

            if (FloatingTextManager.Instance != null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    FloatingTextManager.Instance.SpawnText("OBJECTIVE COMPLETE!", player.transform.position + Vector3.up * 2f,
                        new Color(0.3f, 1f, 0.4f), 2f, 32);
                }
            }

            if (objectiveMarker != null)
            {
                Destroy(objectiveMarker.gameObject);
            }

            UpdateUI();
        }

        private void CreateUI()
        {
            Canvas screenCanvas = null;
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screenCanvas = c;
                    break;
                }
            }
            if (screenCanvas == null) return;

            var objTextObj = new GameObject("ObjectiveText");
            objTextObj.transform.SetParent(screenCanvas.transform);

            var rect = objTextObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -100);
            rect.sizeDelta = new Vector2(400, 30);

            objectiveText = objTextObj.AddComponent<Text>();
            objectiveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (objectiveText.font == null)
                objectiveText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            objectiveText.fontSize = 18;
            objectiveText.color = new Color(1f, 0.9f, 0.4f);
            objectiveText.alignment = TextAnchor.MiddleLeft;

            var outline = objTextObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
        }

        private void UpdateUI()
        {
            if (objectiveText == null) return;

            if (isObjectiveActive && !isObjectiveComplete)
            {
                objectiveText.text = $">> {currentDescription}";
                objectiveText.color = new Color(1f, 0.9f, 0.4f);
            }
            else if (isObjectiveComplete)
            {
                objectiveText.text = $"[COMPLETE] {currentDescription}";
                objectiveText.color = new Color(0.4f, 1f, 0.4f);
            }
            else
            {
                objectiveText.text = "";
            }
        }

        private void CreateObjectiveMarker(Vector3 position)
        {
            if (objectiveMarker != null)
            {
                Destroy(objectiveMarker.gameObject);
            }

            var markerObj = new GameObject("ObjectiveMarker");
            markerObj.transform.position = position + Vector3.up * 2f;

            var sr = markerObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateMarkerSprite();
            sr.sortingOrder = 100;

            markerObj.AddComponent<MarkerBob>();

            objectiveMarker = markerObj.AddComponent<Image>();
            objectiveMarker.enabled = false;
        }

        private Sprite CreateMarkerSprite()
        {
            int size = 24;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color markerColor = new Color(1f, 0.9f, 0.3f);

            int cx = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float dy = y;
                    
                    if (y < size * 0.6f && dx < (size / 2f) * (1f - dy / (size * 0.6f)))
                    {
                        pixels[y * size + x] = markerColor;
                    }
                    else if (y >= size * 0.6f)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, size * 0.7f));
                        if (dist < size * 0.2f)
                        {
                            pixels[y * size + x] = markerColor;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 16f);
        }

        private void UpdateMarkerPosition()
        {
            if (objectiveMarker == null || !isObjectiveActive) return;

            float bob = Mathf.Sin(Time.time * 3f) * 0.2f;
            objectiveMarker.transform.position = new Vector3(
                objectiveMarker.transform.position.x,
                objectiveMarker.transform.position.y + bob * Time.deltaTime,
                0
            );
        }
    }

    public class MarkerBob : MonoBehaviour
    {
        private Vector3 startPos;

        void Start()
        {
            startPos = transform.position;
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * 3f) * 0.3f;
            transform.position = startPos + Vector3.up * bob;

            float pulse = 1f + Mathf.Sin(Time.time * 2f) * 0.1f;
            transform.localScale = Vector3.one * pulse;
        }
    }
}
