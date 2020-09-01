using System;

namespace MuMech.AttitudeControllers
{
    public class TorquePI
    {
        public PIDLoop Loop { get; set; }

        public TorquePI()
        {
            Loop = new PIDLoop();
        }

        public double Update(double input, double setpoint, double MomentOfInertia, double maxOutput)
        {
            Loop.Ki = MomentOfInertia * 4;
            Loop.Kp = 2 * Math.Pow(MomentOfInertia * Loop.Ki, 0.5);
            return Loop.Update(input, setpoint, maxOutput);
        }

        public void ResetI()
        {
            Loop.ResetI();
        }
    }
}
