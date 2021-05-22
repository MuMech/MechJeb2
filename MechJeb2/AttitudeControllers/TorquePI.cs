using System;

namespace MuMech.AttitudeControllers
{
    public class TorquePI
    {
        public KosPIDLoop Loop { get; set; }

        public TorquePI()
        {
            Loop = new KosPIDLoop();
        }

        public double Update(double input, double setpoint, double MomentOfInertia, double maxOutput)
        {
            Loop.Ki = 4 * MomentOfInertia;
            Loop.Kp = 4 * MomentOfInertia;
            return Loop.Update(input, setpoint, maxOutput);
        }

        public void ResetI()
        {
            Loop.ResetI();
        }
    }
}
