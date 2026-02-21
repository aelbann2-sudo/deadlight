using UnityEngine;

namespace Deadlight.Level
{
    public enum ZoneType
    {
        SafeZone,
        ResourceZone,
        DangerZone,
        SpawnZone
    }

    public class MapZone : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private string zoneName = "Unnamed Zone";
        [SerializeField] private ZoneType zoneType = ZoneType.SafeZone;
        [SerializeField] private Vector2 zoneSize = new Vector2(10f, 10f);
        
        [Header("Gameplay Properties")]
        [SerializeField, Range(0f, 1f)] private float dangerLevel = 0f;
        [SerializeField, Range(0.5f, 3f)] private float lootMultiplier = 1f;
        [SerializeField] private bool isShopLocation = false;
        
        [Header("Visual Settings")]
        [SerializeField] private Color dayColor = new Color(0.5f, 0.8f, 0.5f, 0.3f);
        [SerializeField] private Color nightColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);
        [SerializeField] private bool showZoneVisual = true;
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPointsInZone;
        [SerializeField] private int maxEnemiesInZone = 5;

        public string ZoneName => zoneName;
        public ZoneType Type => zoneType;
        public Vector2 ZoneSize => zoneSize;
        public float DangerLevel => dangerLevel;
        public float LootMultiplier => lootMultiplier;
        public bool IsShopLocation => isShopLocation;
        public Bounds ZoneBounds => new Bounds(transform.position, new Vector3(zoneSize.x, zoneSize.y, 1f));

        private SpriteRenderer zoneVisual;
        private bool isNight = false;

        private void Awake()
        {
            SetupZoneVisual();
        }

        private void Start()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void SetupZoneVisual()
        {
            if (!showZoneVisual) return;

            var visualObj = new GameObject("ZoneVisual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;

            zoneVisual = visualObj.AddComponent<SpriteRenderer>();
            zoneVisual.sprite = CreateZoneSprite();
            zoneVisual.color = dayColor;
            zoneVisual.sortingOrder = -10;
        }

        private Sprite CreateZoneSprite()
        {
            int width = Mathf.RoundToInt(zoneSize.x * 10);
            int height = Mathf.RoundToInt(zoneSize.y * 10);
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 10f);
        }

        private void HandleGameStateChanged(Core.GameState newState)
        {
            isNight = newState == Core.GameState.NightPhase;
            UpdateZoneVisual();
        }

        private void UpdateZoneVisual()
        {
            if (zoneVisual == null) return;
            zoneVisual.color = isNight ? nightColor : dayColor;
        }

        public bool ContainsPoint(Vector3 point)
        {
            return ZoneBounds.Contains(point);
        }

        public Vector3 GetRandomPointInZone()
        {
            float x = Random.Range(-zoneSize.x / 2f, zoneSize.x / 2f);
            float y = Random.Range(-zoneSize.y / 2f, zoneSize.y / 2f);
            return transform.position + new Vector3(x, y, 0);
        }

        public Transform[] GetSpawnPoints()
        {
            return spawnPointsInZone;
        }

        private void OnDrawGizmos()
        {
            Color gizmoColor = zoneType switch
            {
                ZoneType.SafeZone => Color.green,
                ZoneType.ResourceZone => Color.yellow,
                ZoneType.DangerZone => Color.red,
                ZoneType.SpawnZone => Color.magenta,
                _ => Color.white
            };

            gizmoColor.a = 0.3f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(transform.position, new Vector3(zoneSize.x, zoneSize.y, 0.1f));

            gizmoColor.a = 1f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, new Vector3(zoneSize.x, zoneSize.y, 0.1f));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (zoneSize.y / 2 + 0.5f), 
                $"{zoneName}\n{zoneType}\nDanger: {dangerLevel:P0}");
            #endif
        }
    }
}
