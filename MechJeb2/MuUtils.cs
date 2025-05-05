using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MuMech
{
    public static class MuUtils
    {
        private static readonly string _cfgPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/MechJeb2/Plugins/PluginData/MechJeb2");

        public static string GetCfgPath(string file) => Path.Combine(_cfgPath, file);

        public static string PadPositive(double x, string format = "F3")
        {
            string s = x.ToString(format);
            return s[0] == '-' ? s : " " + s;
        }

        public static string PadPositiveSci(double x, string format = "F3")
        {
            string s = x > 1000000 ? x.ToString("G3") : x.ToString(format);
            return s[0] == '-' ? s : " " + s;
        }

        public static string PrettyPrint(Vector3d vector, string format = "F3") => "[" + PadPositive(vector.x, format) + ", " +
                                                                                   PadPositive(vector.y, format) + ", " +
                                                                                   PadPositive(vector.z, format) + " ]";

        public static string PrettyPrintSci(Vector3d vector, string format = "F3") => "[" + PadPositiveSci(vector.x, format) + ", " +
            PadPositiveSci(vector.y, format) + ", " +
            PadPositiveSci(vector.z, format) + " ]";

        public static string PrettyPrint(Quaternion quaternion, string format = "F3") =>
            "[" + PadPositive(quaternion.x, format) + ", " + PadPositive(quaternion.y, format) + ", " + PadPositive(quaternion.z, format) +
            ", " + PadPositive(quaternion.w, format) + "]";

        //acosh(x) = log(x + sqrt(x^2 - 1))
        public static double Acosh(double x) => Math.Log(x + Math.Sqrt(x * x - 1));

        //since there doesn't seem to be a Math.Clamp?
        public static double Clamp(double x, double min, double max)
        {
            if (x < min) return min;
            if (x > max) return max;
            return x;
        }

        //clamp to [0,1]
        public static double Clamp01(double x) => Clamp(x, 0, 1);

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

        public static Orbit OrbitFromStateVectors(Vector3d pos, Vector3d vel, CelestialBody body, double ut)
        {
            var ret = new Orbit();
            ret.UpdateFromStateVectors((pos - body.position).xzy, vel.xzy, body, ut);
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

        public static void Swap<T>(this IList<T> list, int indexA, int indexB) => (list[indexA], list[indexB]) = (list[indexB], list[indexA]);

        public static bool PhysicsRunning() => TimeWarp.WarpMode == TimeWarp.Modes.LOW || TimeWarp.CurrentRateIndex == 0;

        public static string SystemClipboard
        {
            get => GUIUtility.systemCopyBuffer;
            set => GUIUtility.systemCopyBuffer = value;
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
        private readonly double[] _store;
        private readonly int      _storeSize;
        private          int      _nextIndex;

        public double Value
        {
            get
            {
                double tmp = 0;
                for (int i = 0; i < _store.Length; i++)
                {
                    tmp += _store[i];
                }

                return tmp / _storeSize;
            }
            set
            {
                _store[_nextIndex] = value;
                _nextIndex         = (_nextIndex + 1) % _storeSize;
            }
        }

        public MovingAverage(int size = 10, double startingValue = 0)
        {
            _storeSize = size;
            _store     = new double[size];
            Force(startingValue);
        }

        private void Force(double newValue)
        {
            for (int i = 0; i < _storeSize; i++)
            {
                _store[i] = newValue;
            }
        }

        public static implicit operator double(MovingAverage v) => v.Value;

        public override string ToString() => Value.ToString();

        public string ToString(string format) => Value.ToString(format);
    }

    public class MovingAverage3d
    {
        private readonly Vector3d[] _store;
        private readonly int        _storeSize;
        private          int        _nextIndex;

        public Vector3d Value
        {
            get
            {
                Vector3d tmp = Vector3d.zero;
                for (int i = 0; i < _store.Length; i++)
                {
                    tmp += _store[i];
                }

                return tmp / _storeSize;
            }
            set
            {
                _store[_nextIndex] = value;
                _nextIndex         = (_nextIndex + 1) % _storeSize;
            }
        }

        public MovingAverage3d(int size = 10, Vector3d startingValue = default)
        {
            _storeSize = size;
            _store     = new Vector3d[size];
            Force(startingValue);
        }

        private void Force(Vector3d newValue)
        {
            for (int i = 0; i < _storeSize; i++)
            {
                _store[i] = newValue;
            }
        }

        public static implicit operator Vector3d(MovingAverage3d v) => v.Value;

        public override string ToString() => Value.ToString();

        public string ToString(string format) => MuUtils.PrettyPrint(Value, format);
    }

    //Represents a 2x2 matrix
    public class Matrix2X2
    {
        private readonly double _a, _b, _c, _d;

        //  [a    b]
        //  [      ]
        //  [c    d]

        public Matrix2X2(double a, double b, double c, double d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public Matrix2X2 Inverse()
        {
            //           1  [d   -c]
            //inverse = --- [      ]
            //          det [-b   a]

            double det = _a * _d - _b * _c;
            return new Matrix2X2(_d / det, -_b / det, -_c / det, _a / det);
        }

        public static Vector2d operator *(Matrix2X2 m, Vector2d vec) => new Vector2d(m._a * vec.x + m._b * vec.y, m._c * vec.x + m._d * vec.y);
    }
}
