/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Globalization;
using static MechJebLib.Utils.Statics;

#nullable enable

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable NonReadonlyMemberInGetHashCode
namespace MechJebLib.Primitives
{
    public struct Q3 : IEquatable<Q3>, IFormattable
    {
        private const double KEPS = EPS * 2.0; // for equality checking

        public double x, y, z, w;

        // Access the x, y, z, w components using [0], [1], [2], [3] respectively.
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default:
                        throw new IndexOutOfRangeException("Invalid Q3 index!");
                }
            }

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
                        throw new IndexOutOfRangeException("Invalid Q3 index!");
                }
            }
        }

        public Q3(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public void Set(double X, double Y, double Z, double W)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        // The identity rotation (RO). This quaternion corresponds to "no rotation": the object
        public static Q3 identity { get; } = new Q3(0.0, 0.0, 0.0, 1.0);

        // Combines rotations /lhs/ and /rhs/.
        public static Q3 operator *(Q3 q1, Q3 q2)
        {
            return new Q3(
                q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,
                q1.w * q2.y - q1.x * q2.z + q1.y * q2.w + q1.z * q2.x,
                q1.w * q2.z + q1.x * q2.y - q1.y * q2.x + q1.z * q2.w,
                q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
            );
        }

        // Rotates the point /point/ with /rotation/.
        public static V3 operator *(Q3 q, V3 v)
        {
            double x = q.x * 2.0;
            double y = q.y * 2.0;
            double z = q.z * 2.0;
            double xx = q.x * x;
            double yy = q.y * y;
            double zz = q.z * z;
            double xy = q.x * y;
            double xz = q.x * z;
            double yz = q.y * z;
            double wx = q.w * x;
            double wy = q.w * y;
            double wz = q.w * z;

            var res = new V3(
                (1.0 - (yy + zz)) * v.x + (xy - wz) * v.y + (xz + wy) * v.z,
                (xy + wz) * v.x + (1.0 - (xx + zz)) * v.y + (yz - wx) * v.z,
                (xz - wy) * v.x + (yz + wx) * v.y + (1.0 - (xx + yy)) * v.z
            );
            return res;
        }

        // Is the dot product of two quaternions within tolerance for them to be considered equal?
        private static bool IsEqualUsingDot(double dot)
        {
            // Returns false in the presence of NaN values.
            return dot > 1.0 - KEPS;
        }

        // Are two quaternions equal to each other?
        public static bool operator ==(Q3 lhs, Q3 rhs)
        {
            return IsEqualUsingDot(Dot(lhs, rhs));
        }

        // Are two quaternions different from each other?
        public static bool operator !=(Q3 lhs, Q3 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // The dot product between two rotations.
        public static double Dot(Q3 a, Q3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        }

        /*
        public void SetLookRotation(V3 view)
        {
            V3 up = V3.up;
            SetLookRotation(view, up);
        }


        // Creates a rotation with the specified /forward/ and /upwards/ directions.
        public void SetLookRotation(V3 view, V3 up)
        {
            this = LookRotation(view, up);
        }
        */

        // FIXME: small angle precision?
        // Returns the angle in radians between two rotations /a/ and /b/.
        public static double Angle(Q3 a, Q3 b)
        {
            double dot = Dot(a, b);
            return IsEqualUsingDot(dot) ? 0.0f : Math.Acos(Math.Min(Math.Abs(dot), 1.0)) * 2.0;
        }

        // FIXME: kill degrees with fire, fix euler angles
        // Makes euler angles positive 0/360 with 0.0001 hacked to support old behaviour of Q3ToEuler
        private static V3 Internal_MakePositive(V3 euler)
        {
            double negativeFlip = Rad2Deg(-0.0001f);
            double positiveFlip = 360.0f + negativeFlip;

            if (euler.x < negativeFlip)
                euler.x += 360.0f;
            else if (euler.x > positiveFlip)
                euler.x -= 360.0f;

            if (euler.y < negativeFlip)
                euler.y += 360.0f;
            else if (euler.y > positiveFlip)
                euler.y -= 360.0f;

            if (euler.z < negativeFlip)
                euler.z += 360.0f;
            else if (euler.z > positiveFlip)
                euler.z -= 360.0f;

            return euler;
        }

        // this produces the mathmetical ZYX intrinsic euler angles, which is aircraft roll, pitch, yaw
        public static V3 ToEulerAngles(Q3 q)
        {
            var angles = new V3();

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.w * q.x + q.y * q.z);
            double cosr_cosp = 1 - 2 * (q.x * q.x + q.y * q.y);
            angles.x = Math.Atan2(sinr_cosp, cosr_cosp);

            // negative pitch (y-axis rotation)
            double sinp = 2 * (q.w * q.y - q.z * q.x);
            angles.y = SafeAsin(sinp);

            // negative yaw (z-axis rotation)
            double siny_cosp = 2 * (q.w * q.z + q.x * q.y);
            double cosy_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
            angles.z = Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }

        public V3 eulerAngles => ToEulerAngles(this);
        //   set { this = Internal_FromEulerRad(Deg2Rad(value)); }
        /*
        public static Q3 Euler(double x, double y, double z) { return Internal_FromEulerRad(Rad2Deg(new V3(x, y, z))); }
        public static Q3 Euler(V3 euler) { return Internal_FromEulerRad(Deg2Rad(euler)); }
        public void ToAngleAxis(out double angle, out V3 axis) { Internal_ToAxisAngleRad(this, out axis, out angle); angle = Rad2Deg(angle);  }
        public void SetFromToRotation(V3 fromDirection, V3 toDirection) { this = FromToRotation(fromDirection, toDirection); }

        public static Q3 RotateTowards(Q3 from, Q3 to, double maxDegreesDelta)
        {
            double angle = Q3.Angle(from, to);
            if (angle == 0.0f) return to;
            return SlerpUnclamped(from, to, Math.Min(1.0f, maxDegreesDelta / angle));
        }
        */

        public static Q3 LookRotation(V3 forward, V3 upwards = default)
        {
            if (upwards == default)
                upwards = V3.up;

            forward = V3.Normalize(forward);
            var right = V3.Normalize(V3.Cross(forward, upwards));
            upwards = V3.Cross(right, forward);

            // FIXME: slurp these into an M3 and write an M3-rotation-matrix-to-Q3 function
            double m00 = forward.x;
            double m01 = forward.y;
            double m02 = forward.z;
            double m10 = right.x;
            double m11 = right.y;
            double m12 = right.z;
            double m20 = -upwards.x;
            double m21 = -upwards.y;
            double m22 = -upwards.z;

            double trace = m00 + m11 + m22;
            var q = new Q3();
            if (trace > 0f)
            {
                double num = Math.Sqrt(trace + 1);
                q.w = num * 0.5;
                num = 0.5 / num;
                q.x = (m12 - m21) * num;
                q.y = (m20 - m02) * num;
                q.z = (m01 - m10) * num;
                return q;
            }

            if (m00 >= m11 && m00 >= m22)
            {
                double num7 = Math.Sqrt(1 + m00 - m11 - m22);
                double num4 = 0.5 / num7;
                q.x = 0.5 * num7;
                q.y = (m01 + m10) * num4;
                q.z = (m02 + m20) * num4;
                q.w = (m12 - m21) * num4;
                return q;
            }

            if (m11 > m22)
            {
                double num6 = Math.Sqrt(1 + m11 - m00 - m22);
                double num3 = 0.5 / num6;
                q.x = (m10 + m01) * num3;
                q.y = 0.5 * num6;
                q.z = (m21 + m12) * num3;
                q.w = (m20 - m02) * num3;
                return q;
            }

            double num5 = Math.Sqrt(1 + m22 - m00 - m11);
            double num2 = 0.5 / num5;
            q.x = (m20 + m02) * num2;
            q.y = (m21 + m12) * num2;
            q.z = 0.5 * num5;
            q.w = (m01 - m10) * num2;
            return q;
        }

        public static Q3 AngleAxis(double angle, V3 axis)
        {
            var q = new Q3();
            V3 a = axis.normalized;
            q.x = a.x * Math.Sin(angle / 2.0);
            q.y = a.y * Math.Sin(angle / 2.0);
            q.z = a.z * Math.Sin(angle / 2.0);
            q.w = Math.Cos(angle / 2.0);
            return q;
        }

        public static Q3 Inverse(Q3 q)
        {
            double mag2 = Dot(q, q);

            return new Q3(-q.x / mag2, -q.y / mag2, -q.z / mag2, q.w / mag2);
        }

        // FIXME: precision
        public static Q3 Normalize(Q3 q)
        {
            double mag = Math.Sqrt(Dot(q, q));

            return mag < EPS ? identity : new Q3(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        }

        public void Normalize()
        {
            this = Normalize(this);
        }

        public Q3 normalized => Normalize(this);

        public override string ToString()
        {
            return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string format)
        {
            return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G";
            return
                $"({x.ToString(format, formatProvider)}, {y.ToString(format, formatProvider)}, {z.ToString(format, formatProvider)}, {w.ToString(format, formatProvider)})";
        }

        public bool Equals(Q3 other)
        {
            return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && w.Equals(other.w);
        }

        public override bool Equals(object? obj)
        {
            return obj is Q3 other && Equals(other);
        }

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
    }
}
