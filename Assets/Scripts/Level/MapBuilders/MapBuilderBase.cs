using System.Collections.Generic;
using UnityEngine;
using Deadlight.Data;
using Deadlight.Visuals;

namespace Deadlight.Level.MapBuilders
{
    public abstract class MapBuilderBase
    {
        protected MapConfig config;
        protected Transform root;
        protected float boundW;  // usable half-width (inside perimeter)
        protected float boundH;  // usable half-height
        protected readonly List<Rect> occupiedAreas = new List<Rect>();
        private static Sprite cachedFenceSprite;
        private System.Func<int, int, int> getTileType;

        public virtual bool OwnsLandmarks => false;

        public void Build(Transform parent, MapConfig mapConfig, System.Func<int, int, int> tileTypeResolver = null)
        {
            config = mapConfig;
            getTileType = tileTypeResolver;
            occupiedAreas.Clear();
            // Match the actual tile map bounds so generated content reaches the playable edge.
            boundW = config.halfWidth - 0.5f;
            boundH = config.halfHeight - 0.5f;
            root = new GameObject(GetMapName()).transform;
            root.SetParent(parent);
            BuildLayout();
        }

        protected abstract string GetMapName();
        protected abstract void BuildLayout();

        public virtual void BuildLandmarks(Transform parent)
        {
        }

        /// <summary>Clamp a position to stay inside the usable map bounds.</summary>
        protected Vector3 Clamp(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, -boundW, boundW);
            pos.y = Mathf.Clamp(pos.y, -boundH, boundH);
            return pos;
        }

        /// <summary>Returns true if the position is outside usable bounds.</summary>
        protected bool OutOfBounds(Vector3 pos)
        {
            return Mathf.Abs(pos.x) > boundW || Mathf.Abs(pos.y) > boundH;
        }

        protected bool TryPlace(Vector3 pos, Vector2 size)
        {
            pos = Clamp(pos);
            if (!CanPlace(pos, size))
            {
                return false;
            }

            RegisterPlacement(pos, size);
            return true;
        }

        protected bool IsRoad(Vector3 pos)
        {
            if (getTileType == null)
            {
                return false;
            }

            int tileType = getTileType(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            return tileType == 1 || tileType == 3;
        }

        protected void RegisterPlacement(Vector3 pos, Vector2 size)
        {
            pos = Clamp(pos);
            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            occupiedAreas.Add(new Rect(
                pos.x - size.x * 0.5f,
                pos.y - size.y * 0.5f,
                size.x,
                size.y));
        }

        protected void ReserveSpace(Vector3 pos, Vector2 size)
        {
            RegisterPlacement(pos, size);
        }

        private bool CanPlace(Vector3 pos, Vector2 size)
        {
            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;
            if (pos.x - halfW < -boundW || pos.x + halfW > boundW)
            {
                return false;
            }

            if (pos.y - halfH < -boundH || pos.y + halfH > boundH)
            {
                return false;
            }

            var candidate = new Rect(
                pos.x - halfW,
                pos.y - halfH,
                size.x,
                size.y);

            foreach (var occupied in occupiedAreas)
            {
                if (occupied.Overlaps(candidate))
                {
                    return false;
                }
            }

            return true;
        }

        // ---- Shared spawn helpers ----

        protected GameObject SpawnBuilding(Transform parent, Vector3 pos, Vector2 colliderSize, int variant, Color tint, string label = "Building", bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, colliderSize);
            }
            var obj = new GameObject(label);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            var sprite = ProceduralSpriteGenerator.CreateBuildingSprite(variant % 3);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(obj.transform, false);
            visual.transform.localScale = new Vector3(
                colliderSize.x / sprite.bounds.size.x,
                colliderSize.y / sprite.bounds.size.y,
                1f);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 2f);
            sr.color = tint;
            var col = obj.AddComponent<BoxCollider2D>();
            MapFootprintCollider.ApplySpriteFootprint(col, sprite, visual.transform.localScale, 0.92f, 0.94f);
            return obj;
        }

        protected GameObject SpawnTree(Transform parent, Vector3 pos, bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, new Vector2(0.75f, 0.75f));
            }
            var obj = new GameObject("Tree");
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * Random.Range(1.2f, 2f);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateTreeSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;
            if (registerPlacement)
            {
                // Small trunk collider only — canopy is walkable-under
                var col = obj.AddComponent<CircleCollider2D>();
                col.radius = 0.15f;
                col.offset = new Vector2(0, -0.3f);
            }
            return obj;
        }

        protected GameObject SpawnBush(Transform parent, Vector3 pos, bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, new Vector2(0.9f, 0.9f));
            }
            var obj = new GameObject("Bush");
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateTreeSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;
            sr.color = new Color(0.3f, 0.6f, 0.3f);
            // No collider — bushes are visual only, player walks through them
            return obj;
        }

        protected GameObject SpawnRock(Transform parent, Vector3 pos, bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, new Vector2(0.8f, 0.8f));
            }
            var obj = new GameObject("Rock");
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateRockSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            if (registerPlacement)
            {
                var col = obj.AddComponent<CircleCollider2D>();
                col.radius = 0.3f;
            }
            return obj;
        }

        protected GameObject SpawnCrate(Transform parent, Vector3 pos, bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, new Vector2(0.9f, 0.9f));
            }
            var obj = new GameObject("Crate");
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            return obj;
        }

        protected GameObject SpawnBarrel(Transform parent, Vector3 pos, bool explosive = false, bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, new Vector2(0.65f, 0.65f));
            }
            var obj = new GameObject("Barrel");
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateBarrelSprite(explosive);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            return obj;
        }

        protected GameObject SpawnCar(Transform parent, Vector3 pos, float angle = 0f, bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, new Vector2(1.8f, 1f));
            }
            var obj = new GameObject("Car");
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.Euler(0, 0, angle);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCarSprite(Random.Range(0, 4));
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            if (registerPlacement)
            {
                var col = obj.AddComponent<BoxCollider2D>();
                MapFootprintCollider.ApplyCustomSpriteFootprint(col, sr.sprite, obj.transform.localScale, new Vector2(1.5f, 0.7f));
            }
            return obj;
        }

        protected GameObject SpawnFence(Transform parent, Vector3 from, Vector3 to, Color tint, bool hasCollider = true, bool registerPlacement = true)
        {
            Vector3 midpoint = Clamp((from + to) * 0.5f);
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length <= Mathf.Epsilon)
            {
                return null;
            }

            var obj = new GameObject("Fence");
            obj.transform.SetParent(parent);
            obj.transform.position = midpoint;
            obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            Sprite sprite = GetFenceSprite();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(obj.transform, false);
            visual.transform.localScale = new Vector3(length / sprite.bounds.size.x, 1f, 1f);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = Mathf.RoundToInt(-midpoint.y * 2f) + 1;

            if (hasCollider)
            {
                var col = obj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(length, 0.55f);
            }

            if (registerPlacement)
            {
                Vector2 footprint = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2(length, 0.45f)
                    : new Vector2(0.45f, length);
                RegisterPlacement(midpoint, footprint);
            }

            return obj;
        }

        protected GameObject SpawnDumpster(Transform parent, Vector3 pos, bool registerPlacement = true)
        {
            pos = Clamp(pos);
            if (registerPlacement)
            {
                RegisterPlacement(pos, new Vector2(1.3f, 0.7f));
            }
            var obj = new GameObject("Dumpster");
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            sr.color = new Color(0.25f, 0.35f, 0.25f);
            obj.transform.localScale = new Vector3(1.3f, 0.9f, 1f);
            return obj;
        }

        protected Vector3 Rnd(float range) =>
            new Vector3(Random.Range(-range, range), Random.Range(-range, range), 0);

        protected bool TooClose(Vector3 pos, Vector3[] existing, float minDist)
        {
            foreach (var e in existing)
            {
                if (e == Vector3.zero) continue;
                if (Vector3.Distance(pos, e) < minDist) return true;
            }
            return false;
        }

        private static Sprite GetFenceSprite()
        {
            if (cachedFenceSprite != null)
            {
                return cachedFenceSprite;
            }

            const int width = 32;
            const int height = 16;
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[width * height];
            // Transparent background
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            Color rail = new Color(0.82f, 0.84f, 0.88f);
            Color post = new Color(0.55f, 0.58f, 0.65f);
            Color shadow = new Color(0.25f, 0.27f, 0.3f, 0.6f);

            // Shadow strip at bottom
            FillRect(pixels, width, 0, 0, width, 2, shadow);
            // End posts (full height, bright)
            FillRect(pixels, width, 0, 2, 4, height - 2, post);
            FillRect(pixels, width, width - 4, 2, 4, height - 2, post);
            // Mid posts
            FillRect(pixels, width, 9, 2, 3, height - 2, post);
            FillRect(pixels, width, width - 12, 2, 3, height - 2, post);
            // Top rail (bright)
            FillRect(pixels, width, 4, height - 4, width - 8, 3, rail);
            // Bottom rail
            FillRect(pixels, width, 4, 4, width - 8, 3, rail);

            texture.SetPixels(pixels);
            texture.Apply();
            cachedFenceSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 16f);
            return cachedFenceSprite;
        }

        private static void FillRect(Color[] pixels, int width, int x, int y, int rectWidth, int rectHeight, Color color)
        {
            for (int row = y; row < y + rectHeight; row++)
            {
                for (int col = x; col < x + rectWidth; col++)
                {
                    pixels[row * width + col] = color;
                }
            }
        }

    }
}
