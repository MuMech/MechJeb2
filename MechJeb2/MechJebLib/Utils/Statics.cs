/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MechJebLib.Primitives;

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
        public static double Clamp(double x, double min, double max)
        {
            return x < min ? min : x > max ? max : x;
        }

        /// <summary>
        ///     Clamp first value between min and max by truncating.
        /// </summary>
        /// <param name="x">Value to clamp</param>
        /// <param name="min">Min value</param>
        /// <param name="max">Max value</param>
        /// <returns>Clamped value</returns>
        public static int Clamp(int x, int min, int max)
        {
            return x < min ? min : x > max ? max : x;
        }

        /// <summary>
        ///     Clamps the value between 0 and 1.
        /// </summary>
        /// <param name="x">Value to clamp</param>
        /// <returns>Clamped value</returns>
        public static double Clamp01(double x)
        {
            return Clamp(x, 0, 1);
        }

        /// <summary>
        ///     Convert Degrees to Radians.
        /// </summary>
        /// <param name="deg">degrees</param>
        /// <returns>radians</returns>
        public static double Deg2Rad(double deg)
        {
            return deg * UtilMath.Deg2Rad;
        }

        /// <summary>
        ///     Convert Radians to Degrees.
        /// </summary>
        /// <param name="rad">Radians</param>
        /// <returns>Degrees</returns>
        public static double Rad2Deg(double rad)
        {
            return rad * UtilMath.Rad2Deg;
        }

        /// <summary>
        ///     Safe inverse cosine that clamps its input.
        /// </summary>
        /// <param name="x">Cosine value</param>
        /// <returns>Radians</returns>
        public static double SafeAcos(double x)
        {
            return Math.Acos(Clamp(x, -1.0, 1.0));
        }

        /// <summary>
        ///     Safe inverse sine that clamps its input.
        /// </summary>
        /// <param name="x">Sine value</param>
        /// <returns>Radians</returns>
        public static double SafeAsin(double x)
        {
            return Math.Asin(Clamp(x, -1.0, 1.0));
        }

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
            return x > PI ? x - TAU : x;
        }

        /// <summary>
        ///     Helper to check if a value is finite (not NaN or Ininity).
        /// </summary>
        /// <param name="x">Value</param>
        /// <returns>True if the value is finite</returns>
        public static bool IsFinite(double x)
        {
            return !double.IsNaN(x) && !double.IsInfinity(x);
        }

        /// <summary>
        ///     Helper to check if a vector is finite in all its compoenents (not NaN or Ininity).
        /// </summary>
        /// <param name="v">Vector</param>
        /// <returns>True if all the components are finite</returns>
        public static bool IsFinite(V3 v)
        {
            return IsFinite(v[0]) && IsFinite(v[1]) && IsFinite(v[2]);
        }

        /// <summary>
        ///     Helper to check if a vector is finite in all its compoenents (not NaN or Ininity).
        /// </summary>
        /// <param name="v">Vector</param>
        /// <returns>True if all the components are finite</returns>
        public static bool IsFinite(Vector3d v)
        {
            return IsFinite(v[0]) && IsFinite(v[1]) && IsFinite(v[2]);
        }

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
        /// <returns>true if the values are nearly the same</returns>
        public static bool NearlyEqual(double a, double b, double epsilon = EPS)
        {
            const double MIN_NORMAL = 2.2250738585072014E-308d;
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double diff = Math.Abs(a - b);

            if (a.Equals(b))
                return true;
            if (a == 0 || b == 0 || absA + absB < MIN_NORMAL)
                return diff < epsilon * MIN_NORMAL;
            return diff / (absA + absB) < epsilon;
        }

        /// <summary>
        ///     Compares two V3 values with a relative tolerance.
        /// </summary>
        /// <param name="a">first vector</param>
        /// <param name="b">second vector</param>
        /// <param name="epsilon">relative tolerance (e.g. 1e-15)</param>
        /// <returns>true if the values are nearly the same</returns>
        public static bool NearlyEqual(V3 a, V3 b, double epsilon = EPS)
        {
            return NearlyEqual(a[0], b[0], epsilon) && NearlyEqual(a[1], b[1], epsilon) && NearlyEqual(a[2], b[2], epsilon);
        }

        /// <summary>
        ///     Compares two Vector3d values with a relative tolerance.
        /// </summary>
        /// <param name="a">first vector</param>
        /// <param name="b">second vector</param>
        /// <param name="epsilon">relative tolerance (e.g. 1e-15)</param>
        /// <returns>true if the values are nearly the same</returns>
        public static bool NearlyEqual(Vector3d a, Vector3d b, double epsilon = EPS)
        {
            return NearlyEqual(a[0], b[0], epsilon) && NearlyEqual(a[1], b[1], epsilon) && NearlyEqual(a[2], b[2], epsilon);
        }

        /// <summary>
        ///     Debugging helper for printing double arrays to logs
        /// </summary>
        /// <param name="array">Array of doubles</param>
        /// <returns>String format</returns>
        public static string DoubleArrayString(IList<double> array)
        {
            var sb = new StringBuilder();

            sb.Append("[");
            int last = array.Count - 1;

            for (int i = 0; i <= last; i++)
            {
                sb.Append(array[i].ToString(CultureInfo.CurrentCulture));
                if (i < last)
                    sb.Append(",");
            }

            sb.Append("]");


            return sb.ToString();
        }

        private static readonly string[] _posPrefix = { " ", "k", "M", "G", "T", "P", "E", "Z", "Y" };
        private static readonly string[] _negPrefix = { " ", "m", "μ", "n", "p", "f", "a", "z", "y" };

        public static string ToSI(this double d, int maxPrecision = -99, int sigFigs = 4)
        {
            if (!IsFinite(d)) return d.ToString();

            // this is an offset to d to deal with rounding (e.g. 9.9995 gets rounded to 10.00 so gains a wholeDigit)
            // (also 999.95 should be rounded to 1k, so bumps up an SI prefix)
            // FIXME: probably needs to be fixed to work when maxPrecision kicks in
            double offset = 5 * (d != 0 ? Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) - sigFigs) : 0);

            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(d) + offset));

            int index = d != 0 ? (int)Math.Abs(Math.Floor(exponent / 3.0)) : 0; // index of the SI prefix
            if (index > 8) index = 8;                                           // there's only 8 SI prefixes

            int siExponent = Math.Sign(exponent) * index * 3; // the SI prefix exponent

            string unit = exponent < 0 ? _negPrefix[index] : _posPrefix[index];

            d /= Math.Pow(10, siExponent); // scale d by the SI prefix exponent

            int wholeDigits = d != 0 ? exponent - siExponent + 1 : 1;
            int decimalDigits = Clamp(sigFigs - wholeDigits, 0, siExponent - maxPrecision);

            return $"{d.ToString("F" + decimalDigits)} {unit}";
        }

        public static void Log(string message)
        {
            Logger.Log(message);
        }
    }
}
