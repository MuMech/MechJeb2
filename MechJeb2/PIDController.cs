using System;

namespace MuMech
{
    public class PIDController : IConfigNode
    {
        private          double _prevError;
        public           double INTAccum, Kp, Ki, Kd;
        private readonly double _max;
        private readonly double _min;

        public PIDController(double kp = 0, double ki = 0, double kd = 0, double max = double.MaxValue, double min = double.MinValue)
        {
            Kp   = kp;
            Ki   = ki;
            Kd   = kd;
            _max = max;
            _min = min;
            Reset();
        }

        public double Compute(double error)
        {
            INTAccum += error * TimeWarp.fixedDeltaTime;
            double action = Kp * error + Ki * INTAccum + Kd * (error - _prevError) / TimeWarp.fixedDeltaTime;
            double clamped = Math.Max(_min, Math.Min(_max, action));
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (clamped != action)
                INTAccum -= error * TimeWarp.fixedDeltaTime;

            _prevError = error;

            return action;
        }

        public void Reset() => _prevError = INTAccum = 0;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("Kp"))
                Kp = Convert.ToDouble(node.GetValue("Kp"));

            if (node.HasValue("Ki"))
                Ki = Convert.ToDouble(node.GetValue("Ki"));

            if (node.HasValue("Kd"))
                Kd = Convert.ToDouble(node.GetValue("Kd"));
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
    }

    public class PIDControllerV2 : IConfigNode
    {
        private          Vector3d _intAccum;
        private          Vector3d _derivativeAct;
        private          Vector3d _propAct;
        public           double   Kp, Ki, Kd;
        private readonly double   _max;
        private readonly double   _min;

        public PIDControllerV2(double kp = 0, double ki = 0, double kd = 0, double max = double.MaxValue, double min = double.MinValue)
        {
            Kp   = kp;
            Ki   = ki;
            Kd   = kd;
            _max = max;
            _min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error, Vector3d omega)
        {
            _derivativeAct = omega * Kd;

            // integral actíon + Anti Windup
            _intAccum.x = Math.Abs(_derivativeAct.x) < 0.6 * _max ? _intAccum.x + error.x * Ki * TimeWarp.fixedDeltaTime : 0.9 * _intAccum.x;
            _intAccum.y = Math.Abs(_derivativeAct.y) < 0.6 * _max ? _intAccum.y + error.y * Ki * TimeWarp.fixedDeltaTime : 0.9 * _intAccum.y;
            _intAccum.z = Math.Abs(_derivativeAct.z) < 0.6 * _max ? _intAccum.z + error.z * Ki * TimeWarp.fixedDeltaTime : 0.9 * _intAccum.z;

            _propAct = error * Kp;

            Vector3d action = _propAct + _derivativeAct + _intAccum;

            // action clamp
            action = new Vector3d(Math.Max(_min, Math.Min(_max, action.x)),
                Math.Max(_min, Math.Min(_max, action.y)),
                Math.Max(_min, Math.Min(_max, action.z)));
            return action;
        }

        public Vector3d Compute(Vector3d error, Vector3d omega, Vector3d wlimit)
        {
            _derivativeAct =  omega * Kd;
            wlimit         *= Kd;

            // integral actíon + Anti Windup
            _intAccum.x = Math.Abs(_derivativeAct.x) < 0.6 * _max ? _intAccum.x + error.x * Ki * TimeWarp.fixedDeltaTime : 0.9 * _intAccum.x;
            _intAccum.y = Math.Abs(_derivativeAct.y) < 0.6 * _max ? _intAccum.y + error.y * Ki * TimeWarp.fixedDeltaTime : 0.9 * _intAccum.y;
            _intAccum.z = Math.Abs(_derivativeAct.z) < 0.6 * _max ? _intAccum.z + error.z * Ki * TimeWarp.fixedDeltaTime : 0.9 * _intAccum.z;

            _propAct = error * Kp;

            Vector3d action = _propAct + _intAccum;

            // Clamp (propAct + intAccum) to limit the angular velocity:
            action = new Vector3d(Math.Max(-wlimit.x, Math.Min(wlimit.x, action.x)),
                Math.Max(-wlimit.y, Math.Min(wlimit.y, action.y)),
                Math.Max(-wlimit.z, Math.Min(wlimit.z, action.z)));

            // add. derivative action
            action += _derivativeAct;

            // action clamp
            action = new Vector3d(Math.Max(_min, Math.Min(_max, action.x)),
                Math.Max(_min, Math.Min(_max, action.y)),
                Math.Max(_min, Math.Min(_max, action.z)));
            return action;
        }

        public void Reset() => _intAccum = Vector3d.zero;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("Kp"))
            {
                Kp = Convert.ToDouble(node.GetValue("Kp"));
            }

            if (node.HasValue("Ki"))
            {
                Ki = Convert.ToDouble(node.GetValue("Ki"));
            }

            if (node.HasValue("Kd"))
            {
                Kd = Convert.ToDouble(node.GetValue("Kd"));
            }
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
    }

    public class PIDControllerV3 : IConfigNode
    {
        public           Vector3d Kp, Ki, Kd, INTAccum, DerivativeAct, PropAct;
        private readonly double   _max;
        private readonly double   _min;

        public PIDControllerV3(Vector3d kp, Vector3d ki, Vector3d kd, double max = double.MaxValue, double min = double.MinValue)
        {
            Kp   = kp;
            Ki   = ki;
            Kd   = kd;
            _max = max;
            _min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error, Vector3d omega, Vector3d wlimit)
        {
            DerivativeAct = Vector3d.Scale(omega, Kd);
            wlimit        = Vector3d.Scale(wlimit, Kd);

            // integral actíon + Anti Windup
            INTAccum.x = Math.Abs(DerivativeAct.x) < 0.6 * _max ? INTAccum.x + error.x * Ki.x * TimeWarp.fixedDeltaTime : 0.9 * INTAccum.x;
            INTAccum.y = Math.Abs(DerivativeAct.y) < 0.6 * _max ? INTAccum.y + error.y * Ki.y * TimeWarp.fixedDeltaTime : 0.9 * INTAccum.y;
            INTAccum.z = Math.Abs(DerivativeAct.z) < 0.6 * _max ? INTAccum.z + error.z * Ki.z * TimeWarp.fixedDeltaTime : 0.9 * INTAccum.z;

            PropAct = Vector3d.Scale(error, Kp);

            Vector3d action = PropAct + INTAccum;

            // Clamp (propAct + intAccum) to limit the angular velocity:
            action = new Vector3d(Math.Max(-wlimit.x, Math.Min(wlimit.x, action.x)),
                Math.Max(-wlimit.y, Math.Min(wlimit.y, action.y)),
                Math.Max(-wlimit.z, Math.Min(wlimit.z, action.z)));

            // add. derivative action
            action += DerivativeAct;

            // action clamp
            action = new Vector3d(Math.Max(_min, Math.Min(_max, action.x)),
                Math.Max(_min, Math.Min(_max, action.y)),
                Math.Max(_min, Math.Min(_max, action.z)));
            return action;
        }

        public void Reset() => INTAccum = Vector3d.zero;

        public void Load(ConfigNode node)
        {
            if (node.HasValue("Kp"))
            {
                Kp = ConfigNode.ParseVector3D(node.GetValue("Kp"));
            }

            if (node.HasValue("Ki"))
            {
                Ki = ConfigNode.ParseVector3D(node.GetValue("Ki"));
            }

            if (node.HasValue("Kd"))
            {
                Kd = ConfigNode.ParseVector3D(node.GetValue("Kd"));
            }
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
    }
}
