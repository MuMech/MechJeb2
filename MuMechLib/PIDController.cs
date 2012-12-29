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

        void IConfigNode.Load(ConfigNode node)
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

        void IConfigNode.Save(ConfigNode node)
        {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
    }
}
