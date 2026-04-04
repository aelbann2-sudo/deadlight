using UnityEngine;

namespace Deadlight.Level
{
    public static class MapFootprintCollider
    {
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

            collider.size = new Vector2(spriteSize.x * widthScale, spriteSize.y * heightScale);
            collider.offset = new Vector2(0f, (0.5f - normalizedPivot.y) * spriteSize.y);
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
    }
}
