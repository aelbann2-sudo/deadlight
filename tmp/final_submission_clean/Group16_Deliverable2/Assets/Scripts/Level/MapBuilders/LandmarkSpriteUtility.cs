using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public static class LandmarkSpriteUtility
    {
        public static int ResolveSortingOrder(Transform parent, Vector3 localPosition, int orderOffset)
        {
            Vector3 worldPosition = parent != null ? parent.TransformPoint(localPosition) : localPosition;
            return Mathf.RoundToInt(-worldPosition.y * 2f) + orderOffset;
        }
    }
}
