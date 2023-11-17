/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using MechJebLib.Primitives;
using static System.Math;

namespace MechJebLib.Utils
{
    /// <summary>
    ///     Static class for helpers which are common enough that they're used as syntactic sugar.
    /// </summary>
    public static class Statics
    {
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double x, double min, double max) => x < min ? min : x > max ? max : x;

        /// <summary>
        ///     Clamp first value between min and max by truncating.
        /// </summary>
        /// <param name="x">Value to clamp</param>
        /// <param name="min">Min value</param>
        /// <param name="max">Max value</param>
        /// <returns>Clamped value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int x, int min, int max) => x < min ? min : x > max ? max : x;

        /// <summary>
        ///     Clamps the value between 0 and 1.
        /// </summary>
        /// <param name="x">Value to clamp</param>
        /// <returns>Clamped value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp01(double x) => Clamp(x, 0, 1);

        public const double DEG2RAD = PI / 180.0;
        public const double RAD2DEG = 180.0 / PI;

        /// <summary>
        ///     Convert Degrees to Radians.
        /// </summary>
        /// <param name="deg">degrees</param>
        /// <returns>radians</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Deg2Rad(double deg) => deg * DEG2RAD;

        /// <summary>
        ///     Linear interpolation.
        /// </summary>
        /// <param name="a">starting value</param>
        /// <param name="b">ending value</param>
        /// <param name="t">fraction between start and end</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double a, double b, double t) => a + (b - a) * Clamp01(t);

        /// <summary>
        ///     Convert Radians to Degrees.
        /// </summary>
        /// <param name="rad">Radians</param>
        /// <returns>Degrees</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Rad2Deg(double rad) => rad * RAD2DEG;

        /// <summary>
        ///     Safe inverse cosine that clamps its input.
        /// </summary>
        /// <param name="x">Cosine value</param>
        /// <returns>Radians</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SafeAcos(double x) => Acos(Clamp(x, -1.0, 1.0));

        /// <summary>
        ///     Safe inverse sine that clamps its input.
        /// </summary>
        /// <param name="x">Sine value</param>
        /// <returns>Radians</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SafeAsin(double x) => Asin(Clamp(x, -1.0, 1.0));

        /// <summary>
        ///     Inverse hyperbolic tangent funtion.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Atanh(double x)
        {
            if (Abs(x) > 1)
                throw new ArgumentException($"Argument to Atanh is out of range: {x}");

            return 0.5 * Log((1 + x) / (1 - x));
        }

        /// <summary>
        ///     Inverse hyperbolic cosine function.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Acosh(double x)
        {
            if (x < 1)
                throw new ArgumentException($"Argument to Acosh is out of range: {x}");

            return Log(x + Sqrt(x * x - 1));
        }

        /// <summary>
        ///     Inverse hyperbolic sine function.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Asinh(double x) => Log(x + Sqrt(x * x + 1));

        /// <summary>
        ///     Raise floating point number to an integral power using exponentiation by squaring.
        /// </summary>
        /// <param name="x">base</param>
        /// <param name="n">integral exponent</param>
        /// <returns>x raised to n</returns>
        public static double Powi(double x, int n)
        {
            if (n < 0)
            {
                x = 1 / x;
                n = -n;
            }

            if (n == 0)
                return 1;
            double y = 1;
            while (n > 1)
            {
                if (n % 2 == 0)
                {
                    x *= x;
                    n /= 2;
                }
                else
                {
                    y *= x;
                    x *= x;
                    n =  (n - 1) / 2;
                }
            }

            return x * y;
        }

        /// <summary>
        ///     Returns the equivalent value in radians between 0 and 2*pi.
        /// </summary>
        /// <param name="x">Radians</param>
        /// <returns>Radians</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        /// <summary>
        ///     Helper to check if a vector is finite in all its compoenents (not NaN or Ininity).
        /// </summary>
        /// <param name="v">Vector</param>
        /// <returns>True if all the components are finite</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(V3 v) => IsFinite(v[0]) && IsFinite(v[1]) && IsFinite(v[2]);

        /*
        /// <summary>
        ///     Helper to check if a vector is finite in all its compoenents (not NaN or Ininity).
        /// </summary>
        /// <param name="v">Vector</param>
        /// <returns>True if all the components are finite</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(Vector3d v)
        {
            return IsFinite(v[0]) && IsFinite(v[1]) && IsFinite(v[2]);
        }
        */

        /// <summary>
        ///     Helper to check if a number is within a range.  The first bound does not need to
        ///     be lower than the second bound.
        /// </summary>
        /// <param name="x">Number to check</param>
        /// <param name="a">First Bound</param>
        /// <param name="b">Second Bound</param>
        /// <returns>True if the number is between the bounds</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithin(this double x, double a, double b)
        {
            if (a < b)
                return a <= x && x <= b;

            return b <= x && x <= a;
        }

        /// <summary>
        ///     This is like KSPs Mathfx.Approx().
        /// </summary>
        /// <param name="val"></param>
        /// <param name="about"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approx(double val, double about, double range) => val > about - range && val < about + range;

        /// <summary>
        ///     Compares two double values with a relative tolerance.
        /// </summary>
        /// <param name="a">first value</param>
        /// <param name="b">second value</param>
        /// <param name="epsilon">relative tolerance (e.g. 1e-15)</param>
        /// <returns>true if the values are nearly the same</returns>
        public static bool NearlyEqual(double a, double b, double epsilon = EPS)
        {
            if (a.Equals(b))
                return true;

            double diff = Abs(a - b);

            if (a == 0 || b == 0)
                return diff < epsilon;

            epsilon = Max(Abs(a), Abs(b)) * epsilon;

            return diff < epsilon;
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
            if (a.Equals(b))
                return true;

            var diff = V3.Abs(a - b);

            double epsilon2 = Max(a.magnitude, b.magnitude) * epsilon;

            for (int i = 0; i < 3; i++)
            {
                if ((a[i] == 0 || b[i] == 0) && diff[i] > epsilon)
                    return false;
                if (diff[i] > epsilon2)
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Compares two M3 matricies with a relative tolerance.
        /// </summary>
        /// <param name="a">first vector</param>
        /// <param name="b">second vector</param>
        /// <param name="epsilon">relative tolerance (e.g. 1e-15)</param>
        /// <returns>true if the values are nearly the same</returns>
        public static bool NearlyEqual(M3 a, M3 b, double epsilon = EPS)
        {
            if (a.Equals(b))
                return true;

            double epsilon2 = Max(a.max_magnitude, b.max_magnitude) * epsilon;

            for (int i = 0; i < 9; i++)
            {
                if ((a[i] == 0 || b[i] == 0) && Abs(a[i] - b[i]) > epsilon)
                    return false;

                if (Abs(a[i] - b[i]) > epsilon2)
                    return false;
            }

            return true;
        }

        /*
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
        */

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

        public static string DoubleArraySparsity(IList<double> user, IList<double> numerical, double tol)
        {
            var sb = new StringBuilder();

            sb.Append("[");
            int last = user.Count - 1;

            for (int i = 0; i <= last; i++)
            {
                if (user[i] == 0 && numerical[i] == 0)
                    sb.Append("⚫"); // zero in both
                else if (NearlyEqual(user[i], numerical[i], tol))
                    sb.Append("✅"); // agrees
                else if (user[i] == 0)
                    sb.Append("❗"); // not done yet
                else
                    sb.Append("❌"); // mistake
                if (i < last)
                    sb.Append(",");
            }

            sb.Append("]");


            return sb.ToString();
        }

        public static string DoubleMatrixString(double[,] matrix)
        {
            var sb = new StringBuilder();

            for (int i = 0; i <= matrix.GetUpperBound(0); i++)
                sb.AppendLine(DoubleArrayString(GetRow(matrix, i)));

            return sb.ToString();
        }

        public static string DoubleMatrixSparsityCheck(double[,] user, double[,] numerical, double tol)
        {
            var sb = new StringBuilder();

            for (int i = 0; i <= user.GetUpperBound(0); i++)
                sb.AppendLine(DoubleArraySparsity(GetRow(user, i), GetRow(numerical, i), tol));

            return sb.ToString();
        }

        public static double[] GetRow(double[,] array, int row)
        {
            int cols = array.GetUpperBound(1) + 1;
            double[] result = new double[cols];

            int size = sizeof(double);

            Buffer.BlockCopy(array, row * cols * size, result, 0, cols * size);

            return result;
        }

        public static double DoubleArrayMagnitude(IList<double> array)
        {
            int last = array.Count - 1;

            double sumsq = 0;

            for (int i = 0; i <= last; i++)
            {
                sumsq += array[i] * array[i];
            }

            return Sqrt(sumsq);
        }

        private static readonly string[] _posPrefix = { " ", "k", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" };
        private static readonly string[] _negPrefix = { " ", "m", "μ", "n", "p", "f", "a", "z", "y", "r", "q" };

        public static string ToSI(this double d, int sigFigs = 4, int maxPrecision = int.MaxValue)
        {
            if (!IsFinite(d)) return d.ToString();

            if (maxPrecision == int.MaxValue)
                maxPrecision = -29 - sigFigs;

            // this is an offset to d to deal with rounding (e.g. 9.9995 gets rounded to 10.00 so gains a wholeDigit)
            // (also 999.95 should be rounded to 1k, so bumps up an SI prefix)
            double offset = 5 * (d != 0 ? Pow(10, Floor(Log10(Abs(d))) - sigFigs) : 0);

            int exponent = (int)Floor(Log10(Abs(d) + offset));

            int index = d != 0 ? (int)Abs(Floor(exponent / 3.0)) : 0; // index of the SI prefix
            if (index > 10) index = 10;                               // there's only 10 SI prefixes

            int siExponent = Sign(exponent) * index * 3; // the SI prefix exponent

            string unit = exponent < 0 ? _negPrefix[index] : _posPrefix[index];

            d /= Pow(10, siExponent); // scale d by the SI prefix exponent

            int wholeDigits = d != 0 ? exponent - siExponent + 1 : 1;
            int maxDecimalDigits = siExponent - maxPrecision;
            int decimalDigits = sigFigs - wholeDigits;
            decimalDigits = decimalDigits > maxDecimalDigits ? maxDecimalDigits : decimalDigits;
            decimalDigits = decimalDigits < 0 ? 0 : decimalDigits;

            return $"{d.ToString("F" + decimalDigits)} {unit}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToSI(this float f, int maxPrecision = -99, int sigFigs = 4) => ((double)f).ToSI(maxPrecision, sigFigs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(string message) => Logger.Print(message);

        public static void CopyFrom(this double[] dest, double[] source)
        {
            for (int i = 0; i < source.Length && i < dest.Length; i++)
                dest[i] = source[i];
        }

        public static void CopyTo(this double[] source, double[] dest)
        {
            for (int i = 0; i < source.Length && i < dest.Length; i++)
                dest[i] = source[i];
        }

        public static void CopyFrom(this IList<double> dest, IReadOnlyList<double> source)
        {
            for (int i = 0; i < source.Count && i < dest.Count; i++)
                dest[i] = source[i];
        }

        public static void CopyTo(this IReadOnlyList<double> source, IList<double> dest)
        {
            for (int i = 0; i < source.Count && i < dest.Count; i++)
                dest[i] = source[i];
        }

        public static void Set(this IList<double> a, int index, V3 v)
        {
            a[index]     = v.x;
            a[index + 1] = v.y;
            a[index + 2] = v.z;
        }

        public static V3 Get(this IList<double> a, int index) => new V3(a[index], a[index + 1], a[index + 2]);

        public static void Set(this double[] a, int index, V3 v)
        {
            a[index]     = v.x;
            a[index + 1] = v.y;
            a[index + 2] = v.z;
        }

        public static V3 Get(this double[] a, int index) => new V3(a[index], a[index + 1], a[index + 2]);

        public static double[] Expand(this double[] a, int n) => a.Length < n ? new double[n] : a;
    }
}
