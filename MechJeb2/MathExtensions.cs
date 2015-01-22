using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public static class MathExtensions
    {
        public static Vector3d Sign(this Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }
            	    	
        public static Vector3d Reorder(this Vector3d vector, int order)
        {
            switch (order)
            {
                case 123:
                    return new Vector3d(vector.x, vector.y, vector.z);
                case 132:
                    return new Vector3d(vector.x, vector.z, vector.y);
                case 213:
                    return new Vector3d(vector.y, vector.x, vector.z);
                case 231:
                    return new Vector3d(vector.y, vector.z, vector.x);
                case 312:
                    return new Vector3d(vector.z, vector.x, vector.y);
                case 321:
                    return new Vector3d(vector.z, vector.y, vector.x);
            }
            throw new ArgumentException("Invalid order", "order");
        }

        public static Vector3d Invert(this Vector3d vector)
        {
            return new Vector3d(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }

        public static Vector3 ProjectIntoPlane(this Vector3 vector, Vector3 planeNormal)
        {
            return vector - Vector3.Project(vector, planeNormal);
        }

        public static float AngleInPlane(this Vector3 vector, Vector3 planeNormal, Vector3 other)
        {
            Vector3 v1 = vector.ProjectIntoPlane(planeNormal);
            Vector3 v2 = other.ProjectIntoPlane(planeNormal);

            if ((v1.magnitude == 0) || (v2.magnitude == 0))
            {
                return float.NaN;
            }

            v1.Normalize();
            v2.Normalize();

            Quaternion rot = Quaternion.FromToRotation(planeNormal, new Vector3(0, 0, 1));

            Vector3 r1 = rot * v1;
            Vector3 r2 = rot * v2;

            return (float)((Math.Atan2(r1.y, r1.x) - Math.Atan2(r2.y, r2.x)) * 180.0 / Math.PI);
        }
    }
}
