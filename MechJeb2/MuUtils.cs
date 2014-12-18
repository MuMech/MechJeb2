﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace MuMech
{
    public static class MuUtils
    {
		public static float ResourceDensity(int type)
        {
            return PartResourceLibrary.Instance.GetDefinition(type).density;
        }

        //Puts numbers into SI format, e.g. 1234 -> "1.234 k", 0.0045678 -> "4.568 m"
        //maxPrecision is the exponent of the smallest place value that will be shown; for example
        //if maxPrecision = -1 and digitsAfterDecimal = 3 then 12.345 will be formatted as "12.3"
        //while 56789 will be formated as "56.789 k"
        public static string ToSI(double d, int maxPrecision = -99, int sigFigs = 4)
        {
            if (d == 0 || double.IsInfinity(d) || double.IsNaN(d)) return d.ToString() + " ";

            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(d))); //exponent of d if it were expressed in scientific notation

            string[] units = new string[] { "y", "z", "a", "f", "p", "n", "μ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y" };
            const int unitIndexOffset = 8; //index of "" in the units array
            int unitIndex = (int)Math.Floor(exponent / 3.0) + unitIndexOffset;
            if (unitIndex < 0) unitIndex = 0;
            if (unitIndex >= units.Length) unitIndex = units.Length - 1;
            string unit = units[unitIndex];

            int actualExponent = (unitIndex - unitIndexOffset) * 3; //exponent of the unit we will us, e.g. 3 for k.
            d /= Math.Pow(10, actualExponent);

            int digitsAfterDecimal = sigFigs - (int)(Math.Ceiling(Math.Log10(Math.Abs(d))));

            if (digitsAfterDecimal > actualExponent - maxPrecision) digitsAfterDecimal = actualExponent - maxPrecision;
            if (digitsAfterDecimal < 0) digitsAfterDecimal = 0;

            string ret = d.ToString("F" + digitsAfterDecimal) + " " + unit;

            return ret;
        }

        public static string PrettyPrint(Vector3d vector, string format = "F3")
        {
            return "[" + (vector.x >= 0 ? " " : "") + vector.x.ToString(format) + ", " + (vector.y >= 0 ? " " : "") + vector.y.ToString(format) + ", " + (vector.z >= 0 ? " " : "") + vector.z.ToString(format) + "]";
        }

        public static string PrettyPrint(Quaternion quaternion, string format = "F3")
        {
            return "[" + (quaternion.x >= 0 ? " " : "") + quaternion.x.ToString(format) + ", " + (quaternion.y >= 0 ? " " : "") + quaternion.y.ToString(format) + ", " + (quaternion.z >= 0 ? " " : "") + quaternion.z.ToString(format) + ", " + (quaternion.w >= 0 ? " " : "") + quaternion.w.ToString(format) + "]";
        }

        //For some reason, Math doesn't have the inverse hyperbolic trigonometric functions:
        //asinh(x) = log(x + sqrt(x^2 + 1))
        public static double Asinh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x + 1));
        }

        //acosh(x) = log(x + sqrt(x^2 - 1))
        public static double Acosh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }

        //atanh(x) = (log(1+x) - log(1-x))/2
        public static double Atanh(double x)
        {
            return 0.5 * (Math.Log(1 + x) - Math.Log(1 - x));
        }

        //since there doesn't seem to be a Math.Clamp?
        public static double Clamp(double x, double min, double max)
        {
            if (x < min) return min;
            if (x > max) return max;
            return x;
        }

        //keeps angles in the range 0 to 360
        public static double ClampDegrees360(double angle)
        {
            angle = angle % 360.0;
            if (angle < 0) return angle + 360.0;
            else return angle;
        }

        //keeps angles in the range -180 to 180
        public static double ClampDegrees180(double angle)
        {
            angle = ClampDegrees360(angle);
            if (angle > 180) angle -= 360;
            return angle;
        }

        public static double ClampRadiansTwoPi(double angle)
        {
            angle = angle % (2 * Math.PI);
            if (angle < 0) return angle + 2 * Math.PI;
            else return angle;
        }

        public static double ClampRadiansPi(double angle)
        {
            angle = ClampRadiansTwoPi(angle);
            if (angle > Math.PI) angle -= 2 * Math.PI;
            return angle;
        }

        public static Orbit OrbitFromStateVectors(Vector3d pos, Vector3d vel, CelestialBody body, double UT)
        {
            Orbit ret = new Orbit();
            ret.UpdateFromStateVectors(OrbitExtensions.SwapYZ(pos - body.position), OrbitExtensions.SwapYZ(vel), body, UT);
            if (double.IsNaN(ret.argumentOfPeriapsis))
            {
                Vector3d vectorToAN = Quaternion.AngleAxis(-(float)ret.LAN, Planetarium.up) * Planetarium.right;
                Vector3d vectorToPe = OrbitExtensions.SwapYZ(ret.eccVec);
                double cosArgumentOfPeriapsis = Vector3d.Dot(vectorToAN, vectorToPe) / (vectorToAN.magnitude * vectorToPe.magnitude);
                //Squad's UpdateFromStateVectors is missing these checks, which are needed due to finite precision arithmetic:
                if(cosArgumentOfPeriapsis > 1) {
                    ret.argumentOfPeriapsis = 0;
                } else if(cosArgumentOfPeriapsis < -1) {
                    ret.argumentOfPeriapsis = 180;
                } else {
                    ret.argumentOfPeriapsis = Math.Acos(cosArgumentOfPeriapsis);
                }
            }
            return ret;
        }

        public static bool PhysicsRunning()
        {
            return (TimeWarp.WarpMode == TimeWarp.Modes.LOW) || (TimeWarp.CurrentRateIndex == 0);
        }

        //Some black magic to access the system clipboard from within Unity, found somewhere on the Web.
        //Unfortunately it doesn't seem we have access to the System.Windows.Forms.Clipboard class, which would 
        //make this easier.
        private static PropertyInfo m_systemCopyBufferProperty = null;
        private static PropertyInfo GetSystemCopyBufferProperty()
        {
            if (m_systemCopyBufferProperty == null)
            {
                Type T = typeof(GUIUtility);
                m_systemCopyBufferProperty = T.GetProperty("systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic);
                if (m_systemCopyBufferProperty == null)
                    throw new Exception("Can't access internal member 'GUIUtility.systemCopyBuffer' it may have been removed / renamed");
            }
            return m_systemCopyBufferProperty;
        }
        public static string SystemClipboard
        {
            get
            {
                PropertyInfo P = GetSystemCopyBufferProperty();
                return (string)P.GetValue(null, null);
            }
            set
            {
                PropertyInfo P = GetSystemCopyBufferProperty();
                P.SetValue(null, value, null);
            }
        }
 
		public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
		{
			T tmp = list[indexA];
			list[indexA] = list[indexB];
			list[indexB] = tmp;
			return list;
        }
    }

    public class MovingAverage
    {
        private double[] store;
        private int storeSize;
        private int nextIndex = 0;

        public double value
        {
            get
            {
                double tmp = 0;
                foreach (double i in store)
                {
                    tmp += i;
                }
                return tmp / storeSize;
            }
            set
            {
                store[nextIndex] = value;
                nextIndex = (nextIndex + 1) % storeSize;
            }
        }

        public MovingAverage(int size = 10, double startingValue = 0)
        {
            storeSize = size;
            store = new double[size];
            force(startingValue);
        }

        public void force(double newValue)
        {
            for (int i = 0; i < storeSize; i++)
            {
                store[i] = newValue;
            }
        }

        public static implicit operator double(MovingAverage v)
        {
            return v.value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public string ToString(string format)
        {
            return value.ToString(format);
        }
    }

    //A simple wrapper around a Dictionary, with the only change being that
    //accessing the value of a nonexistent key returns a default value instead of an error.
    class DefaultableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        Dictionary<TKey, TValue> d = new Dictionary<TKey, TValue>();
        TValue defaultValue;

        public DefaultableDictionary(TValue defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (d.ContainsKey(key)) return d[key];
                else return defaultValue;
            }
            set
            {
                if (d.ContainsKey(key)) d[key] = value;
                else d.Add(key, value);
            }
        }

        public void Add(TKey key, TValue value) { d.Add(key, value); }
        public bool ContainsKey(TKey key) { return d.ContainsKey(key); }
        public ICollection<TKey> Keys { get { return d.Keys; } }
        public bool Remove(TKey key) { return d.Remove(key); }
        public bool TryGetValue(TKey key, out TValue value) { return d.TryGetValue(key, out value); }
        public ICollection<TValue> Values { get { return d.Values; } }
        public void Add(KeyValuePair<TKey, TValue> item) { ((IDictionary<TKey, TValue>)d).Add(item); }
        public void Clear() { d.Clear(); }
        public bool Contains(KeyValuePair<TKey, TValue> item) { return ((IDictionary<TKey, TValue>)d).Contains(item); }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { ((IDictionary<TKey, TValue>)d).CopyTo(array, arrayIndex); }
        public int Count { get { return d.Count; } }
        public bool IsReadOnly { get { return ((IDictionary<TKey, TValue>)d).IsReadOnly; } }
        public bool Remove(KeyValuePair<TKey, TValue> item) { return ((IDictionary<TKey, TValue>)d).Remove(item); }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { return d.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return ((System.Collections.IEnumerable)d).GetEnumerator(); }
    }

    //Represents a 2x2 matrix
    public class Matrix2x2
    {
        double a, b, c, d;

        //  [a    b]
        //  [      ]
        //  [c    d]

        public Matrix2x2(double a, double b, double c, double d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public Matrix2x2 inverse()
        {
            //           1  [d   -c]
            //inverse = --- [      ]
            //          det [-b   a]

            double det = a * d - b * c;
            return new Matrix2x2(d / det, -b / det, -c / det, a / det);
        }

        public static Vector2d operator *(Matrix2x2 M, Vector2d vec)
        {
            return new Vector2d(M.a * vec.x + M.b * vec.y, M.c * vec.x + M.d * vec.y);
        }
    }

    //Is it silly to have a Matrix2x2 and a Matrix3x3, and not a generic Matrix? Yes.
    //However I don't feel like implementing inverse() for matrices larger than 
    //2x2, and I want Matrix3x3f to interact nicely with Vector3.
    public class Matrix3x3f
    {
        //row index, then column index
        private float[,] e = new float[3, 3];

        public float this[int i, int j] 
        {
            get { return e[i, j]; }
            set { e[i, j] = value; }
        }

		public Matrix3x3f transpose()
		{
			Matrix3x3f ret = new Matrix3x3f();
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					ret.e[i, j] = e[j, i];
				}
			}
			return ret;
		}

        public static Vector3d operator *(Matrix3x3f M, Vector3 v) 
        {
            Vector3 ret = Vector3.zero;
            for(int i = 0; i < 3; i++) {
                for(int j = 0; j < 3; j++) {
                    ret[i] += M.e[i, j] * v[j];
                }
            }
            return ret;
        }
    }
}
