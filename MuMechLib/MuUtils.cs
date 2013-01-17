using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public static class MuUtils
    {
        public static float ResourceDensity(int type)
        {
            return PartResourceLibrary.Instance.GetDefinition(type).density;
        }

        public static string ToSI(double d, int digits = 3, int MinMagnitude = 0, int MaxMagnitude = int.MaxValue)
        {
            float exponent = (float)Math.Log10(Math.Abs(d));
            exponent = Mathf.Clamp(exponent, (float)MinMagnitude, (float)MaxMagnitude);

            if (exponent >= 0)
            {
                switch ((int)Math.Floor(exponent))
                {
                    case 0:
                    case 1:
                    case 2:
                        return d.ToString("F" + digits);
                    case 3:
                    case 4:
                    case 5:
                        return (d / 1e3).ToString("F" + digits) + "k";
                    case 6:
                    case 7:
                    case 8:
                        return (d / 1e6).ToString("F" + digits) + "M";
                    case 9:
                    case 10:
                    case 11:
                        return (d / 1e9).ToString("F" + digits) + "G";
                    case 12:
                    case 13:
                    case 14:
                        return (d / 1e12).ToString("F" + digits) + "T";
                    case 15:
                    case 16:
                    case 17:
                        return (d / 1e15).ToString("F" + digits) + "P";
                    case 18:
                    case 19:
                    case 20:
                        return (d / 1e18).ToString("F" + digits) + "E";
                    case 21:
                    case 22:
                    case 23:
                        return (d / 1e21).ToString("F" + digits) + "Z";
                    default:
                        return (d / 1e24).ToString("F" + digits) + "Y";
                }
            }
            else if (exponent < 0)
            {
                switch ((int)Math.Floor(exponent))
                {
                    case -1:
                    case -2:
                    case -3:
                        return (d * 1e3).ToString("F" + digits) + "m";
                    case -4:
                    case -5:
                    case -6:
                        return (d * 1e6).ToString("F" + digits) + "μ";
                    case -7:
                    case -8:
                    case -9:
                        return (d * 1e9).ToString("F" + digits) + "n";
                    case -10:
                    case -11:
                    case -12:
                        return (d * 1e12).ToString("F" + digits) + "p";
                    case -13:
                    case -14:
                    case -15:
                        return (d * 1e15).ToString("F" + digits) + "f";
                    case -16:
                    case -17:
                    case -18:
                        return (d * 1e18).ToString("F" + digits) + "a";
                    case -19:
                    case -20:
                    case -21:
                        return (d * 1e21).ToString("F" + digits) + "z";
                    default:
                        return (d * 1e24).ToString("F" + digits) + "y";
                }
            }
            else
            {
                return "0";
            }
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
            //do pos and vel need to be yz-swapped?
            ret.UpdateFromStateVectors(OrbitExtensions.SwapYZ(pos - body.position), OrbitExtensions.SwapYZ(vel), body, UT);
            return ret;
        }

        public static bool PhysicsRunning()
        {
            return (TimeWarp.WarpMode == TimeWarp.Modes.LOW) || (TimeWarp.CurrentRateIndex == 0);
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
                if(d.ContainsKey(key)) d[key] = value;
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
        public int Count  { get { return d.Count; } }
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
}
