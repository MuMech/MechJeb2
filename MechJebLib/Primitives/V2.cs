/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable NonReadonlyMemberInGetHashCode
namespace MechJebLib.Primitives
{
    /// <summary>
    ///     Double Precision 2-Vector class using Radians.  Angles are positive counter-clockwise (x-axis towards y-axis).
    /// </summary>
    public struct V2 : IEquatable<V2>, IFormattable
    {
        public double x;
        public double y;

        public double this[int index]
        {
            get =>
                index switch
                {
                    0 => x,
                    1 => y,
                    _ => throw new IndexOutOfRangeException($"Bad V2 index {index} in getter")
                };

            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException($"Bad V2 index {index} in setter");
                }
            }
        }

        public V2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public void Set(double nx, double ny)
        {
            x = nx;
            y = ny;
        }

        public static V2 Scale(V2 a, V2 b) => new V2(a.x * b.x, a.y * b.y);

        public static V2 Divide(V2 a, V2 b) => new V2(a.x / b.x, a.y / b.y);

        public static V2 Abs(V2 a) => new V2(Math.Abs(a.x), Math.Abs(a.y));

        public static V2 Sign(V2 a) => new V2(Math.Sign(a.x), Math.Sign(a.y));

        public static V2 Sqrt(V2 a) => new V2(Math.Sqrt(a.x), Math.Sqrt(a.y));

        public static V2 Max(V2 a, V2 b) => new V2(Math.Max(a.x, b.x), Math.Max(a.y, b.y));

        public static V2 Min(V2 a, V2 b) => new V2(Math.Min(a.x, b.x), Math.Min(a.y, b.y));

        public void Scale(V2 scale)
        {
            x *= scale.x;
            y *= scale.y;
        }

        /// <summary>
        ///     The 2D cross product (perp-dot product), which is the z component of the 3D cross product of the two
        ///     vectors extended with z = 0.  Positive when v2 is counter-clockwise from v1.
        /// </summary>
        public static double Cross(V2 v1, V2 v2) => v1.x * v2.y - v1.y * v2.x;

        public bool Equals(V2 other) => x.Equals(other.x) && y.Equals(other.y);

        public override bool Equals(object? obj) => obj is V2 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (x.GetHashCode() * 397) ^ y.GetHashCode();
            }
        }

        public static double Dot(V2 v1, V2 v2) => v1.x * v2.x + v1.y * v2.y;

        public static M2 Outer(V2 v1, V2 v2) => new M2(v1 * v2.x, v1 * v2.y);

        public static V2 Project(V2 vector, V2 onNormal)
        {
            double invC = 1.0 / Math.Max(vector.max_magnitude, onNormal.max_magnitude);
            if (double.IsPositiveInfinity(invC)) return zero;

            V2 vectorC = vector * invC;
            V2 onNormalC = onNormal * invC;

            double invSqrMag = 1.0 / Dot(onNormalC, onNormalC);
            if (double.IsPositiveInfinity(invSqrMag)) return zero;

            double dot = Dot(vectorC, onNormalC);
            return onNormal * dot * invSqrMag;
        }

        public static V2 Lerp(V2 a, V2 b, double t) => a + t * (b - a);

        /// <summary>
        ///     Interpolates along the circle from a to b (the short way) at constant angular rate, with the magnitude
        ///     interpolated linearly.  Extrapolates for t outside of [0,1].
        /// </summary>
        public static V2 Slerp(V2 a, V2 b, double t)
        {
            double magA = a.magnitude;
            double magB = b.magnitude;

            if (magA == 0 || magB == 0)
                return Lerp(a, b, t);

            V2 na = a / magA;
            V2 nb = b / magB;

            double magnitude = magA + t * (magB - magA);
            double dot = Clamp(Dot(na, nb), -1.0, 1.0);

            // nearly parallel: the arc is ill conditioned here and the chord is accurate to O(theta^2)
            if (dot > 1.0 - 1e-12)
                return Lerp(na, nb, t).normalized * magnitude;

            // antiparallel: the direction of rotation is not unique, so pick counter-clockwise deterministically
            V2 perp = dot < -1.0 + 1e-12 ? na.orthonormal : (nb - dot * na).normalized;

            double theta = Acos(dot) * t;

            return (Cos(theta) * na + Sin(theta) * perp) * magnitude;
        }

        /// <summary>
        ///     The unsigned angle between two vectors in [0, π].
        /// </summary>
        public static double Angle(V2 from, V2 to)
        {
            double c = Math.Max(from.max_magnitude, to.max_magnitude);
            if (c <= 0) return 0;

            V2 from_scaled = from / c;
            V2 to_scaled = to / c;

            return Atan2(Math.Abs(Cross(from_scaled, to_scaled)), Dot(from_scaled, to_scaled));
        }

        /// <summary>
        ///     The signed angle from one vector to another in [-π, π], positive counter-clockwise.
        /// </summary>
        public static double SignedAngle(V2 from, V2 to)
        {
            double c = Math.Max(from.max_magnitude, to.max_magnitude);
            if (c <= 0) return 0;

            V2 from_scaled = from / c;
            V2 to_scaled = to / c;

            return Atan2(Cross(from_scaled, to_scaled), Dot(from_scaled, to_scaled));
        }

        public static double Distance(V2 a, V2 b) => new V2(a.x - b.x, a.y - b.y).magnitude;

        public static V2 ClampMagnitude(V2 vector, double maxLength)
        {
            double mag = vector.magnitude;
            if (mag > maxLength)
                return vector.normalized * maxLength;

            return vector;
        }

        public double max_magnitude => Math.Max(Math.Abs(x), Math.Abs(y));
        public double min_magnitude => Math.Min(Math.Abs(x), Math.Abs(y));

        public int max_magnitude_index => Math.Abs(y) > Math.Abs(x) ? 1 : 0;

        public int min_magnitude_index => Math.Abs(y) < Math.Abs(x) ? 1 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Magnitude(V2 vector) => vector.magnitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V2 Normalize(V2 v)
        {
            double norm = v.magnitude;
            return norm > 0 ? v / norm : zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V2 SafeNormalize(V2 v)
        {
            double c = v.max_magnitude;
            return c > 0 ? (v / c).normalized : zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            double norm = Math.Sqrt(x * x + y * y);
            this = norm > 0 ? this / norm : zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SafeNormalize()
        {
            double c = max_magnitude;
            this = c > 0 ? (this / c).normalized : zero;
        }

        /// <summary>
        ///     The unit vector perpendicular to this one, rotated 90 degrees counter-clockwise.
        /// </summary>
        public V2 orthonormal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new V2(-y, x).normalized;
        }

        public V2 normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Normalize(this);
        }

        public V2 safeNormalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SafeNormalize(this);
        }

        public static void OrthoNormalize(ref V2 normal, ref V2 tangent)
        {
            Debug.Assert(normal.magnitude > 0);
            Debug.Assert(tangent.magnitude > 0);
            Debug.Assert(Cross(normal, tangent) != 0);

            normal.Normalize();
            tangent = (tangent - Dot(tangent, normal) * normal).normalized;
        }

        public double magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Sqrt(x * x + y * y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SqrMagnitude(V2 vector) => vector.sqrMagnitude;

        public double sqrMagnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => x * x + y * y;
        }

        public static V2 zero { get; } = new V2(0.0, 0.0);

        public static V2 one { get; } = new V2(1.0, 1.0);

        public static V2 positiveinfinity { get; } = new V2(double.PositiveInfinity, double.PositiveInfinity);

        public static V2 negativeinfinity { get; } = new V2(double.NegativeInfinity, double.NegativeInfinity);

        public static V2 maxvalue { get; } = new V2(double.MaxValue, double.MaxValue);

        public static V2 minvalue { get; } = new V2(double.MinValue, double.MinValue);

        public static V2 nan { get; } = new V2(double.NaN, double.NaN);

        // X,Y axis
        public static V2 xaxis { get; } = new V2(1, 0);

        public static V2 yaxis { get; } = new V2(0, 1);

        public static V2 operator +(V2 a, V2 b) => new V2(a.x + b.x, a.y + b.y);

        public static V2 operator -(V2 a, V2 b) => new V2(a.x - b.x, a.y - b.y);

        public static V2 operator *(V2 a, V2 b) => new V2(a.x * b.x, a.y * b.y);

        public static V2 operator /(V2 a, V2 b) => new V2(a.x / b.x, a.y / b.y);

        public static V2 operator -(V2 a) => new V2(-a.x, -a.y);

        public static V2 operator *(V2 a, double d) => new V2(a.x * d, a.y * d);

        public static V2 operator *(double d, V2 a) => new V2(a.x * d, a.y * d);

        public static V2 operator /(V2 a, double d) => new V2(a.x / d, a.y / d);

        public static V2 operator /(double d, V2 a) => new V2(d / a.x, d / a.y);

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public static bool operator ==(V2 lhs, V2 rhs) => lhs.x == rhs.x && lhs.y == rhs.y;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        public static bool operator !=(V2 lhs, V2 rhs) => !(lhs == rhs);

        public override string ToString() => ToString(null, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G17";
            return $"[{x.ToString(format, formatProvider)}, {y.ToString(format, formatProvider)}]";
        }

        public bool IsFinite() => Statics.IsFinite(x) && Statics.IsFinite(y);

        public void CopyFrom(IList<double> other, int index = 0)
        {
            this[0] = other[index];
            this[1] = other[index + 1];
        }

        public void CopyTo(IList<double> other, int index = 0)
        {
            other[index] = this[0];
            other[index + 1] = this[1];
        }

        public void CopyTo(double[,] other, int i, int j)
        {
            other[i, j] = this[0];
            other[i + 1, j] = this[1];
        }

        public static V2 CopyFromIndices(IList<double> array, (int, int) indices) => new V2(array[indices.Item1], array[indices.Item2]);

        public void CopyToIndices(IList<double> array, (int, int) indices)
        {
            array[indices.Item1] = this[0];
            array[indices.Item2] = this[1];
        }
    }
}
