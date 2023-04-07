﻿using System;
using MechJebLib.Primitives;
using UnityEngine;
using Random = System.Random;

namespace MuMech
{
    public static class MathExtensions
    {
        public static Vector3d Sign(this Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }

        public static Vector3d Abs(this Vector3d vector)
        {
            return new Vector3d(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
        }

        public static Vector3 Abs(this Vector3 vector)
        {
            return new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
        }

        public static Vector3d Sqrt(this Vector3d vector)
        {
            return new Vector3d(Math.Sqrt(vector.x), Math.Sqrt(vector.y), Math.Sqrt(vector.z));
        }

        public static double MaxMagnitude(this Vector3d vector)
        {
            return Math.Max(Math.Max(Math.Abs(vector.x), Math.Abs(vector.y)), Math.Abs(vector.z));
        }

        public static Vector3d Invert(this Vector3d vector)
        {
            return new Vector3d(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }

        public static Vector3d InvertNoNaN(this Vector3d vector)
        {
            return new Vector3d(vector.x != 0 ? 1 / vector.x : 0, vector.y != 0 ? 1 / vector.y : 0, vector.z != 0 ? 1 / vector.z : 0);
        }

        public static Vector3d ProjectOnPlane(this Vector3d vector, Vector3d planeNormal)
        {
            return vector - Vector3d.Project(vector, planeNormal);
        }

        public static Vector3d DeltaEuler(this Quaternion delta)
        {
            return new Vector3d(
                delta.eulerAngles.x > 180 ? delta.eulerAngles.x - 360.0F : delta.eulerAngles.x,
                -(delta.eulerAngles.y > 180 ? delta.eulerAngles.y - 360.0F : delta.eulerAngles.y),
                delta.eulerAngles.z > 180 ? delta.eulerAngles.z - 360.0F : delta.eulerAngles.z
            );
        }

        public static Vector3d Clamp(this Vector3d value, double min, double max)
        {
            return new Vector3d(
                Clamp(value.x, min, max),
                Clamp(value.y, min, max),
                Clamp(value.z, min, max)
            );
        }

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

        public static Quaternion Add(this Quaternion left, Quaternion right)
        {
            return new Quaternion(
                left.x + right.x,
                left.y + right.y,
                left.z + right.z,
                left.w + right.w);
        }

        public static Quaternion Mult(this Quaternion left, float lambda)
        {
            return new Quaternion(
                left.x * lambda,
                left.y * lambda,
                left.z * lambda,
                left.w * lambda);
        }

        public static Quaternion Conj(this Quaternion left)
        {
            return new Quaternion(
                -left.x,
                -left.y,
                -left.z,
                left.w);
        }

        public static Vector3d Project(this Vector3d vector, Vector3d onNormal)
        {
            Vector3d normal = onNormal.normalized;
            return normal * Vector3d.Dot(vector, normal);
        }

        public static bool IsFinite(this Vector3d vector)
        {
            return vector[0].IsFinite() && vector[1].IsFinite() && vector[2].IsFinite();
        }

        // +/- infinity is not a finite number (not finite)
        // NaN is also not a finite number (not a number)
        public static bool IsFinite(this double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }

        public static double NextGaussian(this Random r, double mu = 0, double sigma = 1)
        {
            double u1 = r.NextDouble();
            double u2 = r.NextDouble();

            double rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2);

            double rand_normal = mu + sigma * rand_std_normal;

            return rand_normal;
        }

        public static V3 WorldToV3Rotated(this Vector3d vector)
        {
            return (QuaternionD.Inverse(Planetarium.fetch.rotation) * vector).xzy.ToV3();
        }

        public static V3 WorldToV3(this Vector3d vector)
        {
            return vector.xzy.ToV3();
        }

        public static V3 ToV3(this Vector3d vector)
        {
            return new V3(vector.x, vector.y, vector.z);
        }

        public static Vector3d ToVector3d(this V3 vector)
        {
            return new Vector3d(vector.x, vector.y, vector.z);
        }

        public static Vector3d V3ToWorld(this V3 vector)
        {
            return vector.ToVector3d().xzy;
        }

        public static Vector3d V3ToWorldRotated(this V3 vector)
        {
            return Planetarium.fetch.rotation * vector.ToVector3d().xzy;
        }

        public static Q3 ToQ3(this QuaternionD q)
        {
            return new Q3(q.z, q.y, q.x, -q.w);
        }

        public static QuaternionD ToQuaternionD(this Q3 q)
        {
            return new QuaternionD(q.z, q.y, q.x, -q.w);
        }
    }
}
