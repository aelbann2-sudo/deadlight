using UnityEngine;

namespace Deadlight.Level
{
    public static class MapFootprintCollider
    {
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
    }
}
