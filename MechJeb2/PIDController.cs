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
        public bool _pidReset;
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
            if (_pidReset == true)
            {
                _pidReset = false;
                intAccum = Vector3d.zero;
                prevError = error;
            }
            Vector3d derivativeAct = (error - prevError) * Kd / TimeWarp.fixedDeltaTime;
            
            // Anti-Windup
            intAccum.x = (Math.Abs(derivativeAct.x) < 0.3 * max) ? intAccum.x + (error.x * Ki * TimeWarp.fixedDeltaTime) : 0.8 * intAccum.x;
            intAccum.y = (Math.Abs(derivativeAct.y) < 0.3 * max) ? intAccum.y + (error.y * Ki * TimeWarp.fixedDeltaTime) : 0.8 * intAccum.y;
            intAccum.z = (Math.Abs(derivativeAct.z) < 0.3 * max) ? intAccum.z + (error.z * Ki * TimeWarp.fixedDeltaTime) : 0.8 * intAccum.z;

            Vector3d action = (error * Kp) + intAccum + derivativeAct;
            Vector3d clamped = new Vector3d(Math.Max(min, Math.Min(max, action.x)), Math.Max(min, Math.Min(max, action.y)), Math.Max(min, Math.Min(max, action.z)));
 
            prevError = error;
            action = clamped;

            return action;
        }

        public void Reset()
        {
            _pidReset = true;
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
}
