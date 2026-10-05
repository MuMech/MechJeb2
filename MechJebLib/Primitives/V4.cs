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
    ///     Double Precision 4-Vector class using Radians
    /// </summary>
    public struct V4 : IEquatable<V4>, IFormattable
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public double this[int index]
        {
            get =>
                index switch
                {
                    0 => x,
                    1 => y,
                    2 => z,
                    3 => w,
                    _ => throw new IndexOutOfRangeException($"Bad V4 index {index} in getter")
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
                    case 2:
                        z = value;
                        break;
                    case 3:
                        w = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException($"Bad V4 index {index} in setter");
                }
            }
        }

        public V4(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public void Set(double nx, double ny, double nz, double nw)
        {
            x = nx;
            y = ny;
            z = nz;
            w = nw;
        }

        public static V4 Scale(V4 a, V4 b) => new V4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);

        public static V4 Divide(V4 a, V4 b) => new V4(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);

        public static V4 Abs(V4 a) => new V4(Math.Abs(a.x), Math.Abs(a.y), Math.Abs(a.z), Math.Abs(a.w));

        public static V4 Sign(V4 a) => new V4(Math.Sign(a.x), Math.Sign(a.y), Math.Sign(a.z), Math.Sign(a.w));

        public static V4 Sqrt(V4 a) => new V4(Math.Sqrt(a.x), Math.Sqrt(a.y), Math.Sqrt(a.z), Math.Sqrt(a.w));

        public static V4 Max(V4 a, V4 b) => new V4(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z), Math.Max(a.w, b.w));

        public static V4 Min(V4 a, V4 b) => new V4(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z), Math.Min(a.w, b.w));

        public void Scale(V4 scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
            w *= scale.w;
        }

        public bool Equals(V4 other) => x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && w.Equals(other.w);

        public override bool Equals(object? obj) => obj is V4 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x.GetHashCode();
                hashCode = (hashCode * 397) ^ y.GetHashCode();
                hashCode = (hashCode * 397) ^ z.GetHashCode();
                hashCode = (hashCode * 397) ^ w.GetHashCode();
                return hashCode;
            }
        }

        public static double Dot(V4 v1, V4 v2) => v1.x * v2.x + v1.y * v2.y + v1.z * v2.z + v1.w * v2.w;

        public static M4 Outer(V4 v1, V4 v2) => new M4(v1 * v2.x, v1 * v2.y, v1 * v2.z, v1 * v2.w);

        public static V4 Project(V4 vector, V4 onNormal)
        {
            double invC = 1.0 / Math.Max(vector.max_magnitude, onNormal.max_magnitude);
            if (double.IsPositiveInfinity(invC)) return zero;

            V4 vectorC = vector * invC;
            V4 onNormalC = onNormal * invC;

            double invSqrMag = 1.0 / Dot(onNormalC, onNormalC);
            if (double.IsPositiveInfinity(invSqrMag)) return zero;

            double dot = Dot(vectorC, onNormalC);
            return onNormal * dot * invSqrMag;
        }

        /// <summary>
        ///     Projects the vector onto the hyperplane through the origin which is orthogonal to planeNormal.
        /// </summary>
        public static V4 ProjectOnPlane(V4 vector, V4 planeNormal)
        {
            if (vector == zero)
                return zero;

            if (planeNormal == zero)
                return vector;

            double invC = 1.0 / Math.Max(vector.max_magnitude, planeNormal.max_magnitude);
            if (double.IsPositiveInfinity(invC)) return zero;

            V4 vectorC = vector * invC;
            V4 planeNormalC = planeNormal * invC;

            double invSqrMag = 1.0 / Dot(planeNormalC, planeNormalC);
            double dot = Dot(vectorC, planeNormalC);

            return new V4(vector.x - planeNormal.x * dot * invSqrMag,
                vector.y - planeNormal.y * dot * invSqrMag,
                vector.z - planeNormal.z * dot * invSqrMag,
                vector.w - planeNormal.w * dot * invSqrMag);
        }

        public static V4 Lerp(V4 a, V4 b, double t) => a + t * (b - a);

        /// <summary>
        ///     Interpolates along the great circle from a to b at constant angular rate, with the magnitude
        ///     interpolated linearly.  Extrapolates for t outside of [0,1].
        /// </summary>
        public static V4 Slerp(V4 a, V4 b, double t)
        {
            double magA = a.magnitude;
            double magB = b.magnitude;

            if (magA == 0 || magB == 0)
                return Lerp(a, b, t);

            V4 na = a / magA;
            V4 nb = b / magB;

            double magnitude = magA + t * (magB - magA);
            double dot = Clamp(Dot(na, nb), -1.0, 1.0);

            // nearly parallel: the great circle is ill conditioned here and the chord is accurate to O(theta^2)
            if (dot > 1.0 - 1e-12)
                return Lerp(na, nb, t).normalized * magnitude;

            // antiparallel: the great circle is not unique, so pick one deterministically
            V4 perp = dot < -1.0 + 1e-12 ? na.orthonormal : (nb - dot * na).normalized;

            double theta = Acos(dot) * t;

            return (Cos(theta) * na + Sin(theta) * perp) * magnitude;
        }

        /// <summary>
        ///     The unsigned angle between two vectors in [0, π].  Uses Kahan's formula, which is accurate for
        ///     nearly parallel and nearly antiparallel vectors.
        /// </summary>
        public static double Angle(V4 from, V4 to)
        {
            double c = Math.Max(from.max_magnitude, to.max_magnitude);
            if (c <= 0) return 0;

            V4 from_scaled = from / c;
            V4 to_scaled = to / c;

            double from_mag = from_scaled.magnitude;
            double to_mag = to_scaled.magnitude;
            if (from_mag <= 0 || to_mag <= 0) return 0;

            V4 from_unit = from_scaled / from_mag;
            V4 to_unit = to_scaled / to_mag;

            return 2.0 * Atan2((from_unit - to_unit).magnitude, (from_unit + to_unit).magnitude);
        }

        public static double Distance(V4 a, V4 b) => new V4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w).magnitude;

        public static V4 ClampMagnitude(V4 vector, double maxLength)
        {
            double mag = vector.magnitude;
            if (mag > maxLength)
                return vector.normalized * maxLength;

            return vector;
        }

        public double max_magnitude => Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), Math.Max(Math.Abs(z), Math.Abs(w)));
        public double min_magnitude => Math.Min(Math.Min(Math.Abs(x), Math.Abs(y)), Math.Min(Math.Abs(z), Math.Abs(w)));

        public int max_magnitude_index
        {
            get
            {
                int largest_idx = 0;
                for (int i = 1; i < 4; i++)
                    if (Math.Abs(this[i]) > Math.Abs(this[largest_idx]))
                        largest_idx = i;
                return largest_idx;
            }
        }

        public int min_magnitude_index
        {
            get
            {
                int lowest_idx = 0;
                for (int i = 1; i < 4; i++)
                    if (Math.Abs(this[i]) < Math.Abs(this[lowest_idx]))
                        lowest_idx = i;
                return lowest_idx;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Magnitude(V4 vector) => vector.magnitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 Normalize(V4 v)
        {
            double norm = v.magnitude;
            return norm > 0 ? v / norm : zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 SafeNormalize(V4 v)
        {
            double c = v.max_magnitude;
            return c > 0 ? (v / c).normalized : zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            double norm = Math.Sqrt(x * x + y * y + z * z + w * w);
            this = norm > 0 ? this / norm : zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SafeNormalize()
        {
            double c = max_magnitude;
            this = c > 0 ? (this / c).normalized : zero;
        }

        /// <summary>
        ///     A unit vector perpendicular to this one, rotated 90 degrees counter-clockwise in both the xy and zw planes.
        /// </summary>
        public V4 orthonormal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new V4(-y, x, -w, z).normalized;
        }

        public V4 normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Normalize(this);
        }

        public V4 safeNormalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SafeNormalize(this);
        }

        public static void OrthoNormalize(ref V4 normal, ref V4 tangent)
        {
            Debug.Assert(normal.magnitude > 0);
            Debug.Assert(tangent.magnitude > 0);

            normal.Normalize();
            tangent -= Dot(tangent, normal) * normal;

            Debug.Assert(tangent.magnitude > 0);

            tangent.Normalize();
        }

        public double magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Sqrt(x * x + y * y + z * z + w * w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SqrMagnitude(V4 vector) => vector.sqrMagnitude;

        public double sqrMagnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => x * x + y * y + z * z + w * w;
        }

        public static V4 zero { get; } = new V4(0.0, 0.0, 0.0, 0.0);

        public static V4 one { get; } = new V4(1.0, 1.0, 1.0, 1.0);

        public static V4 positiveinfinity { get; } =
            new V4(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

        public static V4 negativeinfinity { get; } =
            new V4(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

        public static V4 maxvalue { get; } = new V4(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue);

        public static V4 minvalue { get; } = new V4(double.MinValue, double.MinValue, double.MinValue, double.MinValue);

        public static V4 nan { get; } = new V4(double.NaN, double.NaN, double.NaN, double.NaN);

        // X,Y,Z,W axis
        public static V4 xaxis { get; } = new V4(1, 0, 0, 0);

        public static V4 yaxis { get; } = new V4(0, 1, 0, 0);

        public static V4 zaxis { get; } = new V4(0, 0, 1, 0);

        public static V4 waxis { get; } = new V4(0, 0, 0, 1);

        public static V4 operator +(V4 a, V4 b) => new V4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);

        public static V4 operator -(V4 a, V4 b) => new V4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);

        public static V4 operator *(V4 a, V4 b) => new V4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);

        public static V4 operator /(V4 a, V4 b) => new V4(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);

        public static V4 operator -(V4 a) => new V4(-a.x, -a.y, -a.z, -a.w);

        public static V4 operator *(V4 a, double d) => new V4(a.x * d, a.y * d, a.z * d, a.w * d);

        public static V4 operator *(double d, V4 a) => new V4(a.x * d, a.y * d, a.z * d, a.w * d);

        public static V4 operator /(V4 a, double d) => new V4(a.x / d, a.y / d, a.z / d, a.w / d);

        public static V4 operator /(double d, V4 a) => new V4(d / a.x, d / a.y, d / a.z, d / a.w);

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public static bool operator ==(V4 lhs, V4 rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        public static bool operator !=(V4 lhs, V4 rhs) => !(lhs == rhs);

        public override string ToString() => ToString(null, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G17";
            return
                $"[{x.ToString(format, formatProvider)}, {y.ToString(format, formatProvider)}, {z.ToString(format, formatProvider)}, {w.ToString(format, formatProvider)}]";
        }

        public bool IsFinite() => Statics.IsFinite(x) && Statics.IsFinite(y) && Statics.IsFinite(z) && Statics.IsFinite(w);

        public void CopyFrom(IList<double> other, int index = 0)
        {
            this[0] = other[index];
            this[1] = other[index + 1];
            this[2] = other[index + 2];
            this[3] = other[index + 3];
        }

        public void CopyTo(IList<double> other, int index = 0)
        {
            other[index] = this[0];
            other[index + 1] = this[1];
            other[index + 2] = this[2];
            other[index + 3] = this[3];
        }

        public void CopyTo(double[,] other, int i, int j)
        {
            other[i, j] = this[0];
            other[i + 1, j] = this[1];
            other[i + 2, j] = this[2];
            other[i + 3, j] = this[3];
        }

        public static V4 CopyFromIndices(IList<double> array, (int, int, int, int) indices) =>
            new V4(array[indices.Item1], array[indices.Item2], array[indices.Item3], array[indices.Item4]);

        public void CopyToIndices(IList<double> array, (int, int, int, int) indices)
        {
            array[indices.Item1] = this[0];
            array[indices.Item2] = this[1];
            array[indices.Item3] = this[2];
            array[indices.Item4] = this[3];
        }
    }
}
