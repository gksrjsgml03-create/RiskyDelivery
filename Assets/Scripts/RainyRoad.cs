using UnityEngine;

namespace RiskyDelivery
{
    public static class RainyRoad
    {
        // Shared geometry for visible water and grip queries; x/y correspond to world x/z.
        public static readonly Rect[] Patches = {
            new Rect(-6.9f, -8, 13.8f, 9),
            new Rect(-6.9f, 8, 13.8f, 11)
        };

        public static bool Contains(Vector3 position)
        {
            if (position.y > 2 || position.y < -1) return false;
            foreach (Rect patch in Patches)
                if (patch.Contains(new Vector2(position.x, position.z))) return true;
            return false;
        }
    }
}
