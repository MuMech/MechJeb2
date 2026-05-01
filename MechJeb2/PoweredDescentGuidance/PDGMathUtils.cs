using System;
using UnityEngine;

namespace MuMech.Landing
{
    internal static class PDGMathUtils
    {
        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static string FormatVector(Vector3d value)
        {
            return "[" + value.x.ToString("F3") + "," +
                         value.y.ToString("F3") + "," +
                         value.z.ToString("F3") + "]";
        }
    }
}