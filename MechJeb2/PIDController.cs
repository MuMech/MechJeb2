using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class PIDController : IConfigNode
    {
        public double prevError, intAccum, intDecay, Kp, Ki, Kd, max, min;

        public PIDController(double Kp = 0, double Ki = 0, double Kd = 0, double max = double.MaxValue, double min = double.MinValue, double intDecay = 0)
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            this.intDecay = intDecay;
            Reset();
        }

        public double Compute(double error)
        {
            if (intDecay != 0) intAccum *= intDecay;
            intAccum += error * TimeWarp.fixedDeltaTime;
            if (intDecay != 0) intAccum /= intDecay; //for larger decays
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
}
