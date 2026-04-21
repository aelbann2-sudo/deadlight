using UnityEngine;
using System.Collections.Generic;

namespace Deadlight.Level
{
    public static class MapFootprintCollider
    {
        private struct SpriteOpaqueFootprint
        {
            public readonly bool hasOpaquePixels;
            public readonly Vector2 sizeRatio;
            public readonly Vector2 centerNorm;

            public SpriteOpaqueFootprint(bool hasOpaquePixels, Vector2 sizeRatio, Vector2 centerNorm)
            {
                this.hasOpaquePixels = hasOpaquePixels;
                this.sizeRatio = sizeRatio;
                this.centerNorm = centerNorm;
            }
        }

        private static readonly Dictionary<int, SpriteOpaqueFootprint> OpaqueFootprintCache = new Dictionary<int, SpriteOpaqueFootprint>();
        private const byte OpaqueAlphaThreshold = 8;

        public static void ApplySpriteFootprint(
            BoxCollider2D collider,
            Sprite sprite,
            Vector3 localScale,
            float widthScale = 0.92f,
            float heightScale = 0.92f)
        {
            if (collider == null)
            {
                throw new System.ArgumentNullException(nameof(collider));
            }

            if (sprite == null)
            {
                throw new System.ArgumentNullException(nameof(sprite));
            }

            Vector2 scale = new Vector2(Mathf.Abs(localScale.x), Mathf.Abs(localScale.y));
            Vector2 spriteSize = Vector2.Scale(sprite.bounds.size, scale);
            Vector2 normalizedPivot = new Vector2(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height);
            SpriteOpaqueFootprint opaque = GetOpaqueFootprint(sprite);
            Vector2 baseSize = opaque.hasOpaquePixels
                ? Vector2.Scale(spriteSize, opaque.sizeRatio)
                : spriteSize;
            Vector2 centerNorm = opaque.hasOpaquePixels
                ? opaque.centerNorm
                : new Vector2(0.5f, 0.5f);

            collider.size = new Vector2(baseSize.x * widthScale, baseSize.y * heightScale);
            collider.offset = new Vector2(
                (centerNorm.x - normalizedPivot.x) * spriteSize.x,
                (centerNorm.y - normalizedPivot.y) * spriteSize.y);
        }

        public static void ApplyCustomSpriteFootprint(
            BoxCollider2D collider,
            Sprite sprite,
            Vector3 localScale,
            Vector2 footprintSize)
        {
            if (collider == null)
            {
                throw new System.ArgumentNullException(nameof(collider));
            }

            if (sprite == null)
            {
                throw new System.ArgumentNullException(nameof(sprite));
            }

            Vector2 scale = new Vector2(Mathf.Abs(localScale.x), Mathf.Abs(localScale.y));
            Vector2 spriteSize = Vector2.Scale(sprite.bounds.size, scale);
            Vector2 normalizedPivot = new Vector2(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height);
            SpriteOpaqueFootprint opaque = GetOpaqueFootprint(sprite);
            Vector2 centerNorm = opaque.hasOpaquePixels
                ? opaque.centerNorm
                : new Vector2(0.5f, 0.5f);

            collider.size = footprintSize;
            collider.offset = new Vector2(
                (centerNorm.x - normalizedPivot.x) * spriteSize.x,
                (centerNorm.y - normalizedPivot.y) * spriteSize.y);
        }

        public static void ApplyBaseFootprint(
            BoxCollider2D collider,
            Vector2 visualSize,
            float widthScale = 0.88f,
            float depthScale = 0.4f,
            float bottomPadding = 0.04f,
            float minDepth = 0.45f)
        {
            if (collider == null)
            {
                return;
            }

            Vector2 footprint = new Vector2(
                visualSize.x * widthScale,
                Mathf.Max(minDepth, visualSize.y * depthScale));

            collider.size = footprint;
            collider.offset = new Vector2(0f, footprint.y * 0.5f + bottomPadding);
        }

        public static void ApplyCenteredFootprint(
            BoxCollider2D collider,
            Vector2 visualSize,
            float widthScale = 0.88f,
            float depthScale = 0.4f,
            float bottomPadding = 0.04f,
            float minDepth = 0.45f)
        {
            if (collider == null)
            {
                return;
            }

            Vector2 footprint = new Vector2(
                visualSize.x * widthScale,
                Mathf.Max(minDepth, visualSize.y * depthScale));

            collider.size = footprint;
            collider.offset = new Vector2(
                0f,
                -((visualSize.y - footprint.y) * 0.5f) + bottomPadding);
        }

        public static void ApplyOccludingBaseFootprint(
            BoxCollider2D collider,
            Vector2 visualSize,
            float widthScale = 0.9f,
            float depthScale = 0.72f,
            float bottomPadding = 0.04f,
            float minDepth = 1.05f)
        {
            if (collider == null)
            {
                return;
            }

            Vector2 footprint = new Vector2(
                visualSize.x * widthScale,
                Mathf.Max(minDepth, visualSize.y * depthScale));

            collider.size = footprint;
            collider.offset = new Vector2(0f, footprint.y * 0.5f + bottomPadding);
        }

        public static void ApplyOccludingCenteredFootprint(
            BoxCollider2D collider,
            Vector2 visualSize,
            float widthScale = 0.9f,
            float depthScale = 0.68f,
            float bottomPadding = 0.03f,
            float minDepth = 0.95f)
        {
            if (collider == null)
            {
                return;
            }

            Vector2 footprint = new Vector2(
                visualSize.x * widthScale,
                Mathf.Max(minDepth, visualSize.y * depthScale));

            collider.size = footprint;
            collider.offset = new Vector2(
                0f,
                -((visualSize.y - footprint.y) * 0.5f) + bottomPadding);
        }

        private static SpriteOpaqueFootprint GetOpaqueFootprint(Sprite sprite)
        {
            int key = sprite.GetInstanceID();
            if (OpaqueFootprintCache.TryGetValue(key, out SpriteOpaqueFootprint cached))
            {
                return cached;
            }

            var fallback = new SpriteOpaqueFootprint(false, Vector2.one, new Vector2(0.5f, 0.5f));
            try
            {
                Texture2D texture = sprite.texture;
                if (texture == null)
                {
                    OpaqueFootprintCache[key] = fallback;
                    return fallback;
                }

                Color32[] pixels = texture.GetPixels32();
                if (pixels == null || pixels.Length == 0)
                {
                    OpaqueFootprintCache[key] = fallback;
                    return fallback;
                }

                Rect textureRect = sprite.textureRect;
                int texMinX = Mathf.FloorToInt(textureRect.xMin);
                int texMinY = Mathf.FloorToInt(textureRect.yMin);
                int texMaxX = Mathf.CeilToInt(textureRect.xMax) - 1;
                int texMaxY = Mathf.CeilToInt(textureRect.yMax) - 1;

                int opaqueMinX = int.MaxValue;
                int opaqueMinY = int.MaxValue;
                int opaqueMaxX = int.MinValue;
                int opaqueMaxY = int.MinValue;

                int textureWidth = texture.width;
                for (int y = texMinY; y <= texMaxY; y++)
                {
                    int row = y * textureWidth;
                    for (int x = texMinX; x <= texMaxX; x++)
                    {
                        Color32 px = pixels[row + x];
                        if (px.a < OpaqueAlphaThreshold)
                        {
                            continue;
                        }

                        if (x < opaqueMinX) opaqueMinX = x;
                        if (x > opaqueMaxX) opaqueMaxX = x;
                        if (y < opaqueMinY) opaqueMinY = y;
                        if (y > opaqueMaxY) opaqueMaxY = y;
                    }
                }

                if (opaqueMinX > opaqueMaxX || opaqueMinY > opaqueMaxY)
                {
                    OpaqueFootprintCache[key] = fallback;
                    return fallback;
                }

                float rectWidth = Mathf.Max(1f, sprite.rect.width);
                float rectHeight = Mathf.Max(1f, sprite.rect.height);
                float localMinX = opaqueMinX - texMinX;
                float localMaxX = opaqueMaxX - texMinX;
                float localMinY = opaqueMinY - texMinY;
                float localMaxY = opaqueMaxY - texMinY;

                float opaqueWidth = localMaxX - localMinX + 1f;
                float opaqueHeight = localMaxY - localMinY + 1f;

                Vector2 sizeRatio = new Vector2(
                    Mathf.Clamp01(opaqueWidth / rectWidth),
                    Mathf.Clamp01(opaqueHeight / rectHeight));

                Vector2 centerNorm = new Vector2(
                    Mathf.Clamp01((localMinX + localMaxX + 1f) * 0.5f / rectWidth),
                    Mathf.Clamp01((localMinY + localMaxY + 1f) * 0.5f / rectHeight));

                var measured = new SpriteOpaqueFootprint(true, sizeRatio, centerNorm);
                OpaqueFootprintCache[key] = measured;
                return measured;
            }
            catch
            {
                OpaqueFootprintCache[key] = fallback;
                return fallback;
            }
        }
    }
}
