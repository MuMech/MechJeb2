/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    ///     Double Precision, Right Handed 3-Vector class using Radians
    /// </summary>
    public struct V3 : IEquatable<V3>, IFormattable
    {
        private const double KEPS = EPS2; // for equality checking

        public double x;
        public double y;
        public double z;

        // we want [0,0,1] to be "up" while conventional aircraft RPY uses positive z-down, so we rotate 180 degrees
        public double roll
        {
            get => x;
            set => x = value;
        }

        public double pitch
        {
            get => y;
            set => y = value;
        }

        public double yaw
        {
            get => z;
            set => z = value;
        }

        public double this[int index]
        {
            get =>
                index switch
                {
                    0 => x,
                    1 => y,
                    2 => z,
                    _ => throw new IndexOutOfRangeException($"Bad V3 index {index} in getter")
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
                    default:
                        throw new IndexOutOfRangeException($"Bad V3 index {index} in setter");
                }
            }
        }

        public V3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public V3(double x, double y)
        {
            this.x = x;
            this.y = y;
            z      = 0.0;
        }

        public void Set(double nx, double ny, double nz)
        {
            x = nx;
            y = ny;
            z = nz;
        }

        public static V3 Scale(V3 a, V3 b) => new V3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static V3 Divide(V3 a, V3 b) => new V3(a.x / b.x, a.y / b.y, a.z / b.z);

        public static V3 Abs(V3 a) => new V3(Math.Abs(a.x), Math.Abs(a.y), Math.Abs(a.z));

        public static V3 Sign(V3 a) => new V3(Math.Sign(a.x), Math.Sign(a.y), Math.Sign(a.z));

        public static V3 Sqrt(V3 a) => new V3(Math.Sqrt(a.x), Math.Sqrt(a.y), Math.Sqrt(a.z));

        public static V3 Max(V3 a, V3 b) => new V3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));

        public static V3 Min(V3 a, V3 b) => new V3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));

        public void Scale(V3 scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
        }

        public static V3 Cross(V3 v1, V3 v2) => new V3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);

        public bool Equals(V3 other) => x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);

        public override bool Equals(object? obj) => obj is V3 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x.GetHashCode();
                hashCode = (hashCode * 397) ^ y.GetHashCode();
                hashCode = (hashCode * 397) ^ z.GetHashCode();
                return hashCode;
            }
        }

        public static double Dot(V3 v1, V3 v2) => v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;

        // FIXME: precision
        public static V3 Project(V3 vector, V3 onNormal)
        {
            double sqrMag = Dot(onNormal, onNormal);
            if (sqrMag < EPS) return zero;

            double dot = Dot(vector, onNormal);
            return new V3(onNormal.x * dot / sqrMag,
                onNormal.y * dot / sqrMag,
                onNormal.z * dot / sqrMag);
        }

        // FIXME: precision
        public static V3 ProjectOnPlane(V3 vector, V3 planeNormal)
        {
            double sqrMag = Dot(planeNormal, planeNormal);
            if (sqrMag < EPS) return vector;

            double dot = Dot(vector, planeNormal);
            return new V3(vector.x - planeNormal.x * dot / sqrMag,
                vector.y - planeNormal.y * dot / sqrMag,
                vector.z - planeNormal.z * dot / sqrMag);
        }

        // FIXME: precision of Math.Acos for small angles
        public static double Angle(V3 from, V3 to)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            double denominator = Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            return denominator < EPS ? 0.0 : SafeAcos(Dot(from, to) / denominator);
        }

        // FIXME: probably left handed
        // FIXME: precision
        public static double SignedAngle(V3 from, V3 to, V3 axis)
        {
            double unsignedAngle = Angle(from, to);

            double cross_x = from.y * to.z - from.z * to.y;
            double cross_y = from.z * to.x - from.x * to.z;
            double cross_z = from.x * to.y - from.y * to.x;
            double sign = Math.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return unsignedAngle * sign;
        }

        // FIXME: precision
        public static double Distance(V3 a, V3 b)
        {
            double diff_x = a.x - b.x;
            double diff_y = a.y - b.y;
            double diff_z = a.z - b.z;
            return Math.Sqrt(diff_x * diff_x + diff_y * diff_y + diff_z * diff_z);
        }

        // FIXME: precision
        public static V3 ClampMagnitude(V3 vector, double maxLength)
        {
            double sqrmag = vector.sqrMagnitude;
            if (sqrmag > maxLength * maxLength)
            {
                double mag = Math.Sqrt(sqrmag);
                double normalized_x = vector.x / mag;
                double normalized_y = vector.y / mag;
                double normalized_z = vector.z / mag;
                return new V3(normalized_x * maxLength,
                    normalized_y * maxLength,
                    normalized_z * maxLength);
            }

            return vector;
        }

        public double max_magnitude => Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), Math.Abs(z));
        public double min_magnitude => Math.Min(Math.Min(Math.Abs(x), Math.Abs(y)), Math.Abs(z));

        public int max_magnitude_index
        {
            get
            {
                int largest_idx = Math.Abs(x) > Math.Abs(y) ? 0 : 1;
                return Math.Abs(z) > Math.Abs(this[largest_idx]) ? 2 : largest_idx;
            }
        }

        public int min_magnitude_index
        {
            get
            {
                int lowest_idx = Math.Abs(x) < Math.Abs(y) ? 0 : 1;
                return Math.Abs(z) < Math.Abs(this[lowest_idx]) ? 2 : lowest_idx;
            }
        }

        private double _internal_magnitude => Math.Sqrt(x * x + y * y + z * z);

        private V3 _internal_normalize => this / _internal_magnitude;

        public static double Magnitude(V3 vector) => vector.magnitude;

        public static V3 Normalize(V3 value)
        {
            double c = value.max_magnitude;
            return c > 0 ? (value / c)._internal_normalize : zero;
        }

        public void Normalize()
        {
            double c = max_magnitude;
            this = c > 0 ? (this / c)._internal_normalize : zero;
        }

        public V3 normalized => Normalize(this);

        public static void OrthoNormalize(ref V3 normal, ref V3 tangent)
        {
            Debug.Assert(normal.magnitude > 0);
            Debug.Assert(tangent.magnitude > 0);
            Debug.Assert(Cross(normal, tangent).magnitude > 0);

            normal.Normalize();
            tangent = Cross(Cross(normal, tangent).normalized, normal).normalized;
        }

        public double magnitude
        {
            get
            {
                double c = max_magnitude;
                return c > 0 ? Math.Max(c, c * (this / c)._internal_magnitude) : 0;
            }
        }

        public static double SqrMagnitude(V3 vector) => vector.sqrMagnitude;

        public double sqrMagnitude => x * x + y * y + z * z;

        public static V3 zero { get; } = new V3(0.0, 0.0, 0.0);

        public static V3 one { get; } = new V3(1.0, 1.0, 1.0);

        public static V3 positiveinfinity { get; } = new V3(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

        public static V3 negativeinfinity { get; } = new V3(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

        public static V3 maxvalue { get; } = new V3(double.MaxValue, double.MaxValue, double.MaxValue);

        public static V3 minvalue { get; } = new V3(double.MinValue, double.MinValue, double.MinValue);

        public static V3 nan { get; } = new V3(double.NaN, double.NaN, double.NaN);

        // These define North-East-Down co-ordinate system that is valid for vessel body orientations.
        public static V3 down { get; } = new V3(0.0, 0.0, 1.0);

        public static V3 up { get; } = new V3(0.0, 0.0, -1.0);

        public static V3 left { get; } = new V3(0.0, -1.0, 0.0);

        public static V3 right { get; } = new V3(0.0, 1.0, 0.0);

        public static V3 forward { get; } = new V3(1.0, 0.0, 0.0);

        public static V3 back { get; } = new V3(-1.0, 0.0, 0.0);

        // This defines the north pole that is valid for body cenetered inertial coordinate systems
        public static V3 northpole { get; } = new V3(0, 0, 1);

        // X,Y,Z axis
        public static V3 xaxis { get; } = new V3(1, 0, 0);

        public static V3 yaxis { get; } = new V3(0, 1, 0);

        public static V3 zaxis { get; } = new V3(0, 0, 1);

        /// <summary>
        ///     Convert vector stored as spherical radius, theta, phi to cartesian x,y,z
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public V3 sph2cart => x * new V3(Cos(z) * Sin(y), Sin(z) * Sin(y), Cos(y));

        /// <summary>
        ///     Convert vector stored as cartesian x,y,z to spherical radius, theta, phi
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public V3 cart2sph
        {
            get
            {
                double r = magnitude;
                return new V3(r, SafeAcos(z / r), Clamp2Pi(Atan2(y, x)));
            }
        }

        public V3 xzy => new V3(this[0], this[2], this[1]);

        public static V3 operator +(V3 a, V3 b) => new V3(a.x + b.x, a.y + b.y, a.z + b.z);

        public static V3 operator -(V3 a, V3 b) => new V3(a.x - b.x, a.y - b.y, a.z - b.z);

        public static V3 operator *(V3 a, V3 b) => new V3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static V3 operator /(V3 a, V3 b) => new V3(a.x / b.x, a.y / b.y, a.z / b.z);

        public static V3 operator -(V3 a) => new V3(-a.x, -a.y, -a.z);

        public static V3 operator *(V3 a, double d) => new V3(a.x * d, a.y * d, a.z * d);

        public static V3 operator *(double d, V3 a) => new V3(a.x * d, a.y * d, a.z * d);

        public static V3 operator /(V3 a, double d) => new V3(a.x / d, a.y / d, a.z / d);

        public static V3 operator /(double d, V3 a) => new V3(d / a.x, d / a.y, d / a.z);

        public static bool operator ==(V3 lhs, V3 rhs)
        {
            double diff_X = lhs.x - rhs.x;
            double diff_Y = lhs.y - rhs.y;
            double diff_Z = lhs.z - rhs.z;
            double sqrmag = diff_X * diff_X + diff_Y * diff_Y + diff_Z * diff_Z;
            return sqrmag < KEPS * KEPS;
        }

        public static bool operator !=(V3 lhs, V3 rhs) => !(lhs == rhs);

        public override string ToString() => ToString(null, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G";
            return
                $"[{x.ToString(format, formatProvider)}, {y.ToString(format, formatProvider)}, {z.ToString(format, formatProvider)}]";
        }

        public bool IsFinite() => Statics.IsFinite(x) && Statics.IsFinite(y) && Statics.IsFinite(z);

        public void CopyFrom(IList<double> other, int index = 0)
        {
            this[0] = other[index];
            this[1] = other[index + 1];
            this[2] = other[index + 2];
        }

        public void CopyTo(IList<double> other, int index = 0)
        {
            other[index]     = this[0];
            other[index + 1] = this[1];
            other[index + 2] = this[2];
        }

        public void CopyTo(double[,] other, int i, int j)
        {
            other[i, j]     = this[0];
            other[i + 1, j] = this[1];
            other[i + 2, j] = this[2];
        }
    }
}
