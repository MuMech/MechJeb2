/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;

namespace MechJebLib.Utils
{
    /// <summary>
    ///     Static class for helpers which are common enough that they're used as syntactic sugar.
    /// </summary>
    public static class Statics
    {
        public const double PI  = Math.PI;

        public const double TAU = 2 * PI;

        /// <summary>
        ///     Normal machine epsilon.  The Double.Epsilon in C# is one ULP above zero which is somewhat useless.
        /// </summary>
        public const double EPS = 2.2204e-16;

        /// <summary>
        ///     Twice machine epsilon.
        /// </summary>
        public const double EPS2 = EPS * 2;

        /// <summary>
        ///     Value of the standard gravity constant in m/s.
        /// </summary>
        public const float G0 = 9.80665f;

        /// <summary>
        ///     Clamp first value between min and max by truncating.
        /// </summary>
        /// <param name="x">Value to clamp</param>
        /// <param name="min">Min value</param>
        /// <param name="max">Max value</param>
        /// <returns>Clamped value</returns>
        public static double Clamp(double x, double min, double max) => x < min ? min : x > max ? max : x;

        /// <summary>
        ///     Clamps the value between 0 and 1.
        /// </summary>
        /// <param name="x">Value to clamp</param>
        /// <returns>Clamped value</returns>
        public static double Clamp01(double x) => Clamp(x, 0, 1);


        /// <summary>
        ///     Convert Degrees to Radians.
        /// </summary>
        /// <param name="deg">degrees</param>
        /// <returns>radians</returns>
        public static double Deg2Rad(double deg) => deg * UtilMath.Deg2Rad;

        /// <summary>
        ///     Convert Radians to Degrees.
        /// </summary>
        /// <param name="rad">Radians</param>
        /// <returns>Degrees</returns>
        public static double Rad2Deg(double rad) => rad * UtilMath.Rad2Deg;

        /// <summary>
        ///     Safe inverse cosine that clamps its input.
        /// </summary>
        /// <param name="x">Cosine value</param>
        /// <returns>Radians</returns>
        public static double SafeAcos(double x) => Math.Acos(Clamp(x, -1.0, 1.0));

        /// <summary>
        ///     Safe inverse sine that clamps its input.
        /// </summary>
        /// <param name="x">Sine value</param>
        /// <returns>Radians</returns>
        public static double SafeAsin(double x) => Math.Asin(Clamp(x, -1.0, 1.0));

        /// <summary>
        ///     Returns the equivalent value in radians between 0 and 2*pi.
        /// </summary>
        /// <param name="x">Radians</param>
        /// <returns>Radians</returns>
        public static double Clamp2Pi(double x)
        {
            x %= TAU;
            return x < 0 ? x + TAU : x;
        }

        /// <summary>
        ///     Returns the equivalent value in radians between -pi and pi.
        /// </summary>
        /// <param name="x">Radians</param>
        /// <returns>Radians</returns>
        public static double ClampPi(double x)
        {
            x = Clamp2Pi(x);
            return x > PI ? x : x -= TAU;
        }

        /// <summary>
        ///     Helper to check if a value is finite (not NaN or Ininity).
        /// </summary>
        /// <param name="x">Value</param>
        /// <returns>True if the value is finite</returns>
        public static bool IsFinite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        /// <summary>
        ///     Helper to check if a vector is finite in all its compoenents (not NaN or Ininity).
        /// </summary>
        /// <param name="v">Vector</param>
        /// <returns>True if all the components are finite</returns>
        public static bool IsFinite(Vector3d v) => IsFinite(v[0]) && IsFinite(v[1]) && IsFinite(v[2]);

        /// <summary>
        ///     This is like KSPs Mathfx.Approx().
        /// </summary>
        /// <param name="val"></param>
        /// <param name="about"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static bool Approx(double val, double about, double range)
        {
            return val > about - range && val < about + range;
        }

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
