using System;
using UnityEngine;
using Random = System.Random;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public static class MathExtensions
    {
        public static Vector3d Sign(this Vector3d vector) => new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));

        public static Vector3d Abs(this Vector3d vector) => new Vector3d(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));

        public static Vector3 Abs(this Vector3 vector) => new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));

        public static Vector3d Sqrt(this Vector3d vector) => new Vector3d(Math.Sqrt(vector.x), Math.Sqrt(vector.y), Math.Sqrt(vector.z));

        public static double MaxMagnitude(this Vector3d vector) => Math.Max(Math.Max(Math.Abs(vector.x), Math.Abs(vector.y)), Math.Abs(vector.z));

        public static Vector3d Invert(this Vector3d vector) => new Vector3d(1 / vector.x, 1 / vector.y, 1 / vector.z);

        public static Vector3d InvertNoNaN(this Vector3d vector) => new Vector3d(vector.x != 0 ? 1 / vector.x : 0, vector.y != 0 ? 1 / vector.y : 0,
            vector.z != 0 ? 1 / vector.z : 0);

        public static Vector3d ProjectOnPlane(this Vector3d vector, Vector3d planeNormal) => vector - Vector3d.Project(vector, planeNormal);

        public static Vector3d DeltaEuler(this Quaternion delta) =>
            new Vector3d(
                delta.eulerAngles.x > 180 ? delta.eulerAngles.x - 360.0F : delta.eulerAngles.x,
                -(delta.eulerAngles.y > 180 ? delta.eulerAngles.y - 360.0F : delta.eulerAngles.y),
                delta.eulerAngles.z > 180 ? delta.eulerAngles.z - 360.0F : delta.eulerAngles.z
            );

        public static Vector3d Clamp(this Vector3d value, double min, double max) =>
            new Vector3d(
                Clamp(value.x, min, max),
                Clamp(value.y, min, max),
                Clamp(value.z, min, max)
            );

        public static double Clamp(double val, double min, double max)
        {
            if (val <= min)
                return min;
            if (val >= max)
                return max;
            return val;
        }

        // projects the two vectors onto the normal plane and computes the 0 to 360 angle
        public static double AngleInPlane(this Vector3d vector, Vector3d planeNormal, Vector3d other)
        {
            Vector3d v1 = vector.ProjectOnPlane(planeNormal);
            Vector3d v2 = other.ProjectOnPlane(planeNormal);

            if (v1.magnitude == 0 || v2.magnitude == 0)
                return double.NaN;

            double angle = MuUtils.ClampDegrees360(Math.Acos(Vector3d.Dot(v1.normalized, v2.normalized)) * UtilMath.Rad2Deg);
            if (Vector3d.Dot(Vector3d.Cross(v1, v2), planeNormal) < 0)
                return -angle;
            return angle;
        }

        public static Quaternion Add(this Quaternion left, Quaternion right) =>
            new Quaternion(
                left.x + right.x,
                left.y + right.y,
                left.z + right.z,
                left.w + right.w);

        public static Quaternion Mult(this Quaternion left, float lambda) =>
            new Quaternion(
                left.x * lambda,
                left.y * lambda,
                left.z * lambda,
                left.w * lambda);

        public static Quaternion Conj(this Quaternion left) =>
            new Quaternion(
                -left.x,
                -left.y,
                -left.z,
                left.w);

        public static Vector3d Project(this Vector3d vector, Vector3d onNormal)
        {
            Vector3d normal = onNormal.normalized;
            return normal * Vector3d.Dot(vector, normal);
        }

        public static bool IsFinite(this Vector3d vector) => vector[0].IsFinite() && vector[1].IsFinite() && vector[2].IsFinite();

        // +/- infinity is not a finite number (not finite)
        // NaN is also not a finite number (not a number)
        public static bool IsFinite(this double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        public static double NextGaussian(this Random r, double mu = 0, double sigma = 1)
        {
            double u1 = r.NextDouble();
            double u2 = r.NextDouble();

            double rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2);

            double rand_normal = mu + sigma * rand_std_normal;

            return rand_normal;
        }

        public static QuaternionD FromToRotation(Vector3d fromDirection, Vector3d toDirection)
        {
            if (fromDirection == Vector3d.zero || toDirection == Vector3d.zero)
                return QuaternionD.identity;

            fromDirection.Normalize();
            toDirection.Normalize();

            double dot = Vector3d.Dot(fromDirection, toDirection);

            if (dot < -0.9999999999) // Vectors are pointing in opposite directions (zero cross-product)
            {
                var orthogonal = Vector3d.Cross(fromDirection, Math.Abs(fromDirection.x) < 1.0/Math.Sqrt(2.0) ? Vector3.right : Vector3.up);

                orthogonal.Normalize();

                return new QuaternionD(orthogonal.x, orthogonal.y, orthogonal.z, 0);
            }

            if (dot > 0.9999999999) // Vectors are nearly identical (zero cross-product)
                return QuaternionD.identity;

            Vector3d cross = Vector3.Cross(fromDirection, toDirection);
            double   s     = Math.Sqrt((1 + dot) * 2);
            double   invs  = 1 / s;

            return new QuaternionD(
                cross.x * invs,
                cross.y * invs,
                cross.z * invs,
                s * 0.5f
            );
        }

        public static Vector3d EulerAngles(QuaternionD q)
        {
            double magnitude = Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);

            if (magnitude < EPS)
                return Vector3d.zero;

            if (Math.Abs(magnitude - 1.0) > 1e-10)
            {
                q.x /= magnitude;
                q.y /= magnitude;
                q.z /= magnitude;
                q.w /= magnitude;
            }

            double sqw = q.w * q.w;
            double sqx = q.x * q.x;
            double sqy = q.y * q.y;
            double sqz = q.z * q.z;

            double unit = sqx + sqy + sqz + sqw;
            double test = q.x * q.w - q.y * q.z;

            if (test > 0.499999999 * unit) // North pole gimbal lock
            {
                double yaw   = 2.0 * Math.Atan2(q.y, q.w);

                return new Vector3d(
                    90,
                    Rad2Deg(Clamp2Pi(yaw)),
                    0
                );
            }

            if (test < -0.499999999 * unit) // South pole gimbal lock
            {
                double yaw   = -2.0 * Math.Atan2(q.y, q.w);

                return new Vector3d(
                    270,
                    Rad2Deg(Clamp2Pi(yaw)),
                    0
                );
            }
            else
            {
                double pitch = Math.Asin(2.0 * test / unit);
                double yaw   = Math.Atan2(2.0 * (q.x * q.z + q.w * q.y), sqw - sqx - sqy + sqz);
                double roll  = Math.Atan2(2.0 * (q.x * q.y + q.w * q.z), sqw - sqx + sqy - sqz);

                // Convert to degrees
                return new Vector3d(
                    Rad2Deg(Clamp2Pi(pitch)),
                    Rad2Deg(Clamp2Pi(yaw)),
                    Rad2Deg(Clamp2Pi(roll))
                );
            }
        }

        public static QuaternionD Euler(double x, double y, double z)
        {
            x = Deg2Rad(x);
            y = Deg2Rad(y);
            z = Deg2Rad(z);

            double cx = Math.Cos(x * 0.5);
            double sx = Math.Sin(x * 0.5);
            double cy = Math.Cos(y * 0.5);
            double sy = Math.Sin(y * 0.5);
            double cz = Math.Cos(z * 0.5);
            double sz = Math.Sin(z * 0.5);

            var q = new QuaternionD {
                w = cz * cx * cy + sz * sx * sy,
                x = cz * sx * cy - sz * cx * sy,
                y = cz * cx * sy + sz * sx * cy,
                z = sz * cx * cy - cz * sx * sy
            };

            return q;
        }
    }
}
