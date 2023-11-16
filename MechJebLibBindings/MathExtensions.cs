using MechJebLib.Primitives;
using UnityEngine;

namespace MechJebLibBindings
{
    public static class MathExtensions
    {
        public static V3 WorldToV3Rotated(this Vector3d vector) => (QuaternionD.Inverse(Planetarium.fetch.rotation) * vector).xzy.ToV3();

        public static V3 WorldToV3(this Vector3d vector) => vector.xzy.ToV3();

        public static V3 ToV3(this Vector3d vector) => new V3(vector.x, vector.y, vector.z);

        public static Vector3d ToVector3d(this V3 vector) => new Vector3d(vector.x, vector.y, vector.z);

        public static Vector3d V3ToWorld(this V3 vector) => vector.ToVector3d().xzy;

        public static Vector3d V3ToWorldRotated(this V3 vector) => Planetarium.fetch.rotation * vector.ToVector3d().xzy;

        public static Q3 ToQ3(this QuaternionD q) => new Q3(q.z, q.y, q.x, -q.w);

        public static QuaternionD ToQuaternionD(this Q3 q) => new QuaternionD(q.z, q.y, q.x, -q.w);
    }
}
