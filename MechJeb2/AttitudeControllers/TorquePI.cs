using System;

namespace MuMech.AttitudeControllers
{
    public class TorquePI
    {
        public PIDLoop Loop { get; set; }

        public double I { get; private set; }

        public MovingAverage TorqueAdjust { get; set; }

        private double tr;

        public double Tr
        {
            get { return tr; }
            set
            {
                tr = value;
                ts = 4.0 * tr / 2.76;
            }
        }

        private double ts;

        public double Ts
        {
            get { return ts; }
            set
            {
                ts = value;
                tr = 2.76 * ts / 4.0;
            }
        }

        public TorquePI()
        {
            Loop = new PIDLoop();
            Ts = 2; /* FIXME: use high pass filter to measure output noise and decrease this value when no noise, and increase when it oscillates */
            TorqueAdjust = new MovingAverage();
        }

        public double Update(double input, double setpoint, double MomentOfInertia, double maxOutput)
        {
            I = MomentOfInertia;

            Loop.Ki = MomentOfInertia * Math.Pow(4.0 / ts, 2);
            Loop.Kp = 2 * Math.Pow(MomentOfInertia * Loop.Ki, 0.5);
            return Loop.Update(input, setpoint, maxOutput);
        }

        public void ResetI()
        {
            Loop.ResetI();
        }
    }
}
