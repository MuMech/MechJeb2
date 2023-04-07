﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MuMech
{
    public static class MuUtils
    {
        public const double DBL_EPSILON = 2.2204460492503131e-16;

        public static float ResourceDensity(int type)
        {
            return PartResourceLibrary.Instance.GetDefinition(type).density;
        }

        private static readonly string cfgPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/MechJeb2/Plugins/PluginData/MechJeb2");

        public static string GetCfgPath(string file)
        {
            return Path.Combine(cfgPath, file);
        }

        public static string PadPositive(double x, string format = "F3")
        {
            string s = x.ToString(format);
            return s[0] == '-' ? s : " " + s;
        }

        public static string PrettyPrint(Vector3d vector, string format = "F3")
        {
            return "[" + PadPositive(vector.x, format) + ", " + PadPositive(vector.y, format) + ", " + PadPositive(vector.z, format) + " ]";
        }

        public static string PrettyPrint(Quaternion quaternion, string format = "F3")
        {
            return "[" + PadPositive(quaternion.x, format) + ", " + PadPositive(quaternion.y, format) + ", " + PadPositive(quaternion.z, format) +
                   ", " + PadPositive(quaternion.w, format) + "]";
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

        //clamp to [0,1]
        public static double Clamp01(double x)
        {
            return Clamp(x, 0, 1);
        }

        //keeps angles in the range 0 to 360
        public static double ClampDegrees360(double angle)
        {
            angle = angle % 360.0;
            if (angle < 0) return angle + 360.0;
            return angle;
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
            return angle;
        }

        public static double ClampRadiansPi(double angle)
        {
            angle = ClampRadiansTwoPi(angle);
            if (angle > Math.PI) angle -= 2 * Math.PI;
            return angle;
        }

        // angle between two headings handling rotation around 360 degrees (angle between 355 and 5 == 10)
        public static double headingAngle(double angle1, double angle2)
        {
            return Math.Min((angle1 - angle2 + 360) % 360, (angle2 - angle1 + 360) % 360);
        }

        public static double IntPow(double val, int exp)
        {
            double result = val;
            for (int i = 1; i < exp; ++i)
                result *= val;
            return result;
        }

        public static Orbit OrbitFromStateVectors(Vector3d pos, Vector3d vel, CelestialBody body, double UT)
        {
            var ret = new Orbit();
            ret.UpdateFromStateVectors((pos - body.position).xzy, vel.xzy, body, UT);
            if (double.IsNaN(ret.argumentOfPeriapsis))
            {
                Vector3d vectorToAN = Quaternion.AngleAxis(-(float)ret.LAN, Planetarium.up) * Planetarium.right;
                Vector3d vectorToPe = ret.eccVec.xzy;
                double cosArgumentOfPeriapsis = Vector3d.Dot(vectorToAN, vectorToPe) / (vectorToAN.magnitude * vectorToPe.magnitude);
                //Squad's UpdateFromStateVectors is missing these checks, which are needed due to finite precision arithmetic:
                if (cosArgumentOfPeriapsis > 1)
                {
                    ret.argumentOfPeriapsis = 0;
                }
                else if (cosArgumentOfPeriapsis < -1)
                {
                    ret.argumentOfPeriapsis = 180;
                }
                else
                {
                    ret.argumentOfPeriapsis = Math.Acos(cosArgumentOfPeriapsis);
                }
            }

            return ret;
        }

        public static bool PhysicsRunning()
        {
            return TimeWarp.WarpMode == TimeWarp.Modes.LOW || TimeWarp.CurrentRateIndex == 0;
        }

        public static string SystemClipboard
        {
            get => GUIUtility.systemCopyBuffer;
            set => GUIUtility.systemCopyBuffer = value;
        }

        public static ICollection<T> Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }

        public static void DrawLine(Texture2D tex, int x1, int y1, int x2, int y2, Color col)
        {
            int dy = y2 - y1;
            int dx = x2 - x1;
            int stepx, stepy;

            if (dy < 0)
            {
                dy    = -dy;
                stepy = -1;
            }
            else { stepy = 1; }

            if (dx < 0)
            {
                dx    = -dx;
                stepx = -1;
            }
            else { stepx = 1; }

            dy <<= 1;
            dx <<= 1;

            float fraction = 0;

            tex.SetPixel(x1, y1, col);
            if (dx > dy)
            {
                fraction = dy - (dx >> 1);
                while (Mathf.Abs(x1 - x2) > 1)
                {
                    if (fraction >= 0)
                    {
                        y1       += stepy;
                        fraction -= dx;
                    }

                    x1       += stepx;
                    fraction += dy;
                    tex.SetPixel(x1, y1, col);
                }
            }
            else
            {
                fraction = dx - (dy >> 1);
                while (Mathf.Abs(y1 - y2) > 1)
                {
                    if (fraction >= 0)
                    {
                        x1       += stepx;
                        fraction -= dy;
                    }

                    y1       += stepy;
                    fraction += dx;
                    tex.SetPixel(x1, y1, col);
                }
            }
        }

        public static Color HSVtoRGB(float hue, float saturation, float value, float alpha)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - Mathf.Floor(hue / 60);

            float v = value;
            float p = value * (1 - saturation);
            float q = value * (1 - f * saturation);
            float t = value * (1 - (1 - f) * saturation);

            if (hi == 0)
                return new Color(v, t, p, alpha);
            if (hi == 1)
                return new Color(q, v, p, alpha);
            if (hi == 2)
                return new Color(p, v, t, alpha);
            if (hi == 3)
                return new Color(p, q, v, alpha);
            if (hi == 4)
                return new Color(t, p, v, alpha);

            return new Color(v, p, q, alpha);
        }
    }

    public class MovingAverage
    {
        private readonly double[] store;
        private readonly int      storeSize;
        private          int      nextIndex;

        public double value
        {
            get
            {
                double tmp = 0;
                for (int i = 0; i < store.Length; i++)
                {
                    tmp += store[i];
                }

                return tmp / storeSize;
            }
            set
            {
                store[nextIndex] = value;
                nextIndex        = (nextIndex + 1) % storeSize;
            }
        }

        public MovingAverage(int size = 10, double startingValue = 0)
        {
            storeSize = size;
            store     = new double[size];
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

    public class MovingAverage3d
    {
        private readonly Vector3d[] store;
        private readonly int        storeSize;
        private          int        nextIndex;

        public Vector3d value
        {
            get
            {
                Vector3d tmp = Vector3d.zero;
                for (int i = 0; i < store.Length; i++)
                {
                    tmp += store[i];
                }

                return tmp / storeSize;
            }
            set
            {
                store[nextIndex] = value;
                nextIndex        = (nextIndex + 1) % storeSize;
            }
        }

        public MovingAverage3d(int size = 10, Vector3d startingValue = default)
        {
            storeSize = size;
            store     = new Vector3d[size];
            force(startingValue);
        }

        public void force(Vector3d newValue)
        {
            for (int i = 0; i < storeSize; i++)
            {
                store[i] = newValue;
            }
        }

        public static implicit operator Vector3d(MovingAverage3d v)
        {
            return v.value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public string ToString(string format)
        {
            return MuUtils.PrettyPrint(value, format);
        }
    }

    //A simple wrapper around a Dictionary, with the only change being that
    //The keys are also stored in a list so they can be iterated without allocating an IEnumerator
    internal class KeyableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        protected Dictionary<TKey, TValue> d = new Dictionary<TKey, TValue>();

        // Also store the keys in a list so we can iterate them without allocating an IEnumerator
        protected List<TKey> k = new List<TKey>();

        public virtual TValue this[TKey key]
        {
            get => d[key];
            set
            {
                if (d.ContainsKey(key)) d[key] = value;
                else
                {
                    k.Add(key);
                    d.Add(key, value);
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            k.Add(key);
            d.Add(key, value);
        }

        public bool              ContainsKey(TKey key) { return d.ContainsKey(key); }
        public ICollection<TKey> Keys                  => d.Keys;
        public List<TKey>        KeysList              => k;

        public bool Remove(TKey key)
        {
            return d.Remove(key) && k.Remove(key);
        }

        public bool                TryGetValue(TKey key, out TValue value) { return d.TryGetValue(key, out value); }
        public ICollection<TValue> Values                                  => d.Values;

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)d).Add(item);
            k.Add(item.Key);
        }

        public void Clear()
        {
            d.Clear();
            k.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)                  { return ((IDictionary<TKey, TValue>)d).Contains(item); }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { ((IDictionary<TKey, TValue>)d).CopyTo(array, arrayIndex); }
        public int  Count                                                      => d.Count;
        public bool IsReadOnly                                                 => ((IDictionary<TKey, TValue>)d).IsReadOnly;

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)d).Remove(item) && k.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { return d.GetEnumerator(); }
        IEnumerator IEnumerable.                       GetEnumerator() { return ((IEnumerable)d).GetEnumerator(); }
    }

    //A simple wrapper around a Dictionary, with the only change being that
    //accessing the value of a nonexistent key returns a default value instead of an error.
    internal class DefaultableDictionary<TKey, TValue> : KeyableDictionary<TKey, TValue>
    {
        private readonly TValue defaultValue;

        public DefaultableDictionary(TValue defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public override TValue this[TKey key]
        {
            get
            {
                TValue val;
                if (d.TryGetValue(key, out val))
                    return val;

                return defaultValue;
            }
            set
            {
                if (d.ContainsKey(key)) d[key] = value;
                else
                {
                    k.Add(key);
                    d.Add(key, value);
                }
            }
        }
    }

    //Represents a 2x2 matrix
    public class Matrix2x2
    {
        private readonly double a, b, c, d;

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
}
