using UnityEngine;

namespace BR.Config
{
    public static class LaneCoords
    {
        // Set these to the exact centers you used for the guides:
        // Left, Center, Right
        public static readonly float[] X = { -1.67f, 0f, 1.67f };

        public static float Get(int laneIndex)
        {
            // 0=Left, 1=Center, 2=Right (clamped, so no crashes)
            return X[Mathf.Clamp(laneIndex, 0, X.Length - 1)];
        }

        public static int Count => X.Length;
    }
}
