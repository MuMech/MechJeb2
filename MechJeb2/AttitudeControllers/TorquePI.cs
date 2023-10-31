namespace MuMech.AttitudeControllers
{
    public class TorquePI
    {
        private KosPIDLoop _loop { get; } = new KosPIDLoop();

        public double Update(double input, double setpoint, double momentOfInertia, double maxOutput)
        {
            _loop.Ki = 4 * momentOfInertia;
            _loop.Kp = 4 * momentOfInertia;
            return _loop.Update(input, setpoint, maxOutput);
        }

        public void ResetI() => _loop.ResetI();
    }
}
