// PDGMathUtils.cs
// Lightweight math helpers shared across all PDG components.

using UnityEngine;

namespace MuMech.Landing
{
    internal static class PDGMathUtils
    {
        /// <summary>Returns true when <paramref name="value"/> is neither NaN nor infinite.</summary>
        public static bool IsFinite(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value);

        /// <summary>Formats a <see cref="Vector3d"/> as a bracketed, three-decimal string for logging.</summary>
        public static string FormatVector(Vector3d value)
            => $"[{value.x:F3},{value.y:F3},{value.z:F3}]";
    }
}