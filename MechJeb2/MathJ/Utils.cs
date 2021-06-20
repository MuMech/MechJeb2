using System;
using System.Buffers;

namespace MuMech.MathJ
{
    public static class Utils
    {
        public const double EPS = 2.24e-15;

        // we need a thread-safe pool of doubles
        public static readonly ArrayPool<double> DoublePool = ArrayPool<double>.Shared;


        /// <summary>
        ///     Compares two double values with a relative tolerance.
        /// </summary>
        /// <param name="a">first value</param>
        /// <param name="b">second value</param>
        /// <param name="epsilon">relative tolerance (e.g. 1e-15)</param>
        /// <returns>true if the valus are nearly the same</returns>
        public static bool NearlyEqual(double a, double b, double epsilon = EPS)
        {
            const double minNormal = 2.2250738585072014E-308d;
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double diff = Math.Abs(a - b);

            if (a.Equals(b))
                return true;
            if (a == 0 || b == 0 || absA + absB < minNormal)
                return diff < epsilon * minNormal;
            return diff / (absA + absB) < epsilon;
        }
    }
}
