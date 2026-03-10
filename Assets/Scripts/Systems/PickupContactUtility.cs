using UnityEngine;

namespace Deadlight.Systems
{
    public static class PickupContactUtility
    {
        public static bool IsTightPickupContact(Collider2D pickupCollider, SpriteRenderer pickupRenderer, Collider2D playerCollider)
        {
            if (pickupCollider == null || playerCollider == null)
            {
                return false;
            }

            ColliderDistance2D colliderDistance = pickupCollider.Distance(playerCollider);
            if (colliderDistance.isOverlapped || colliderDistance.distance <= 0.03f)
            {
                return true;
            }

            Vector2 pickupCenter = pickupRenderer != null ? pickupRenderer.bounds.center : pickupCollider.bounds.center;
            Vector2 playerClosest = playerCollider.ClosestPoint(pickupCenter);
            float allowedDistance = Mathf.Max(GetPickupVisualRadius(pickupRenderer, pickupCollider), 0.18f) + 0.1f;
            return Vector2.Distance(playerClosest, pickupCenter) <= allowedDistance;
        }

        public static bool IsWithinPickupRange(Transform pickup, SpriteRenderer pickupRenderer, Collider2D playerCollider, float fallbackRadius)
        {
            if (pickup == null || playerCollider == null)
            {
                return false;
            }

            Vector2 pickupCenter = pickupRenderer != null ? pickupRenderer.bounds.center : pickup.position;
            Vector2 playerClosest = playerCollider.ClosestPoint(pickupCenter);
            float allowedDistance = Mathf.Max(GetPickupVisualRadius(pickupRenderer, null), fallbackRadius) + 0.08f;
            return Vector2.Distance(playerClosest, pickupCenter) <= allowedDistance;
        }

        private static float GetPickupVisualRadius(SpriteRenderer pickupRenderer, Collider2D pickupCollider)
        {
            if (pickupRenderer != null && pickupRenderer.sprite != null)
            {
                Vector3 extents = pickupRenderer.bounds.extents;
                return Mathf.Max(0.12f, Mathf.Min(extents.x, extents.y));
            }

            if (pickupCollider is CircleCollider2D circle)
            {
                float scale = Mathf.Max(pickupCollider.transform.lossyScale.x, pickupCollider.transform.lossyScale.y);
                return Mathf.Max(0.12f, circle.radius * scale);
            }

            return 0.18f;
        }
    }
}
