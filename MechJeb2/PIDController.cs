using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class PIDController : IConfigNode
    {
        public double prevError, intAccum, Kp, Ki, Kd, max, min;

        public PIDController(double Kp = 0, double Ki = 0, double Kd = 0, double max = double.MaxValue, double min = double.MinValue)
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            Reset();
        }

        public double Compute(double error)
        {
            intAccum += error * TimeWarp.fixedDeltaTime;
            double action = (Kp * error) + (Ki * intAccum) + (Kd * (error - prevError) / TimeWarp.fixedDeltaTime);
            double clamped = Math.Max(min, Math.Min(max, action));
            if (clamped != action)
            {
                intAccum -= error * TimeWarp.fixedDeltaTime;
            }
            prevError = error;

            return action;
        }

        public void Reset()
        {
            prevError = intAccum = 0;
        }

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

    public class PIDControllerV : IConfigNode
    {
        public Vector3d prevError, intAccum;
        public double Kp, Ki, Kd, max, min;

        public PIDControllerV(double Kp = 0, double Ki = 0, double Kd = 0, double max = double.MaxValue, double min = double.MinValue)
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error)
        {
            intAccum += error * TimeWarp.fixedDeltaTime;
            Vector3d action = (Kp * error) + (Ki * intAccum) + (Kd * (error - prevError) / TimeWarp.fixedDeltaTime);
            Vector3d clamped = new Vector3d(Math.Max(min, Math.Min(max, action.x)), Math.Max(min, Math.Min(max, action.y)), Math.Max(min, Math.Min(max, action.z)));
            if (Math.Abs((clamped - action).magnitude) > 0.01)
            {
                intAccum -= error * TimeWarp.fixedDeltaTime;
            }
            prevError = error;

            return action;
        }

        public void Reset()
        {
            prevError = intAccum = Vector3d.zero;
        }

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

    // This PID Controler is used by Raf04 patch for the attitude controler. They have a separate implementation since they use
    // a different set of argument and do more (and less) than the other PID controler
    public class PIDControllerV2 : IConfigNode
    {
        public Vector3d intAccum, derivativeAct, propAct;
        public double Kp, Ki, Kd, max, min;

        public PIDControllerV2(double Kp = 0, double Ki = 0, double Kd = 0, double max = double.MaxValue, double min = double.MinValue)
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error, Vector3d omega )
        {
            derivativeAct = omega * Kd;

            // integral actíon + Anti Windup
            intAccum.x = (Math.Abs(derivativeAct.x) < 0.6 * max) ? intAccum.x + (error.x * Ki * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.x;
            intAccum.y = (Math.Abs(derivativeAct.y) < 0.6 * max) ? intAccum.y + (error.y * Ki * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.y;
            intAccum.z = (Math.Abs(derivativeAct.z) < 0.6 * max) ? intAccum.z + (error.z * Ki * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.z;

            propAct = error * Kp;

            Vector3d action = propAct + derivativeAct + intAccum;

            // action clamp
            action = new Vector3d(Math.Max(min, Math.Min(max, action.x)),
                                  Math.Max(min, Math.Min(max, action.y)),
                                  Math.Max(min, Math.Min(max, action.z)) );
            return action;
        }

        public void Reset()
        {
            intAccum = Vector3d.zero;
        }

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
        public Vector3d Kp, Ki, Kd, intAccum, derivativeAct, propAct;
        public double max, min;

        public PIDControllerV3(Vector3d Kp, Vector3d Ki, Vector3d Kd, double max = double.MaxValue, double min = double.MinValue)
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error, Vector3d omega)
        {
            derivativeAct = omega;
            derivativeAct.Scale(Kd);

            // integral actíon + Anti Windup
            intAccum.x = (Math.Abs(derivativeAct.x) < 0.6 * max) ? intAccum.x + (error.x * Ki.x * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.x;
            intAccum.y = (Math.Abs(derivativeAct.y) < 0.6 * max) ? intAccum.y + (error.y * Ki.y * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.y;
            intAccum.z = (Math.Abs(derivativeAct.z) < 0.6 * max) ? intAccum.z + (error.z * Ki.z * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.z;

            propAct = error;
            propAct.Scale(Kp);

            Vector3d action = propAct + derivativeAct + intAccum;

            // action clamp
            action = new Vector3d(Math.Max(min, Math.Min(max, action.x)),
                                  Math.Max(min, Math.Min(max, action.y)),
                                  Math.Max(min, Math.Min(max, action.z)));
            return action;
        }

        public void Reset()
        {
            intAccum = Vector3d.zero;
        }

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
