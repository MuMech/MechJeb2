using static System.Math;

namespace MechJebLib.Control
{
    public class DeltaSigmaThrottleModulator
    {
        public double MinOnTime;
        public double MinOffTime;

        private double _accumulator; // impulse debt in m/s (positive = owed)
        private bool _outputOn;
        private double _stateTimer;

        // Smallest non-zero throttle we'll emit in the continuous regime.
        // RealFuels (with allowZero) treats throttle == 0 as "engine off",
        // so we never want to return exactly zero when the engine should
        // be running at minimum.
        private const double ENGINE_ON_EPSILON = 1e-4;

        public DeltaSigmaThrottleModulator(double minOffTime, double minOnTime)
        {
            MinOffTime = minOffTime;
            MinOnTime = minOnTime;
        }

        public void Reset()
        {
            _accumulator = 0.0;
            _outputOn = false;
            _stateTimer = 0.0;
        }

        /// <summary>
        ///     Translates a commanded acceleration (m/s²) into a RealFuels-style
        ///     throttle command (fraction of available control authority).
        ///     0 = engine off; (0, 1] = engine on, mapping linearly between
        ///     minThrustAccel and maxThrustAccel.
        ///     Above minThrustAccel, returns the continuous throttle directly.
        ///     Below minThrustAccel, delta-sigma modulates between full-off (0)
        ///     and full-on (1) so the time-integrated acceleration tracks the
        ///     commanded profile.
        /// </summary>
        public float ThrottleCommand(double commandedAccel,
            double minThrustAccel,
            double maxThrustAccel,
            double dt)
        {
            // Hard rails.
            if (commandedAccel <= 0.0)
            {
                Reset();
                return 0.0f;
            }

            if (commandedAccel >= maxThrustAccel)
            {
                Reset();
                _outputOn = true;
                return 1.0f;
            }

            // Continuous regime: the engine can throttle to deliver
            // commandedAccel without pulsing.
            if (commandedAccel >= minThrustAccel)
            {
                Reset();
                double t = (commandedAccel - minThrustAccel)
                    / (maxThrustAccel - minThrustAccel);
                return (float)Max(ENGINE_ON_EPSILON, t);
            }

            // PWM regime: pulse between 0 (engine off) and 1 (engine at
            // maxThrustAccel). The integrator carries impulse debt in m/s.
            double delivered = _outputOn ? maxThrustAccel : 0.0;
            _accumulator += (commandedAccel - delivered) * dt;
            _stateTimer += dt;

            // Anti-windup: bound to one max-dwell of impulse debt.
            double clamp = maxThrustAccel * Max(MinOnTime, MinOffTime);
            if (_accumulator > clamp) _accumulator = clamp;
            if (_accumulator < -clamp) _accumulator = -clamp;

            if (_outputOn)
            {
                if (_accumulator < 0.0 && _stateTimer >= MinOnTime)
                {
                    _outputOn = false;
                    _stateTimer = 0.0;
                }
            }
            else
            {
                if (_accumulator > 0.0 && _stateTimer >= MinOffTime)
                {
                    _outputOn = true;
                    _stateTimer = 0.0;
                }
            }

            return _outputOn ? 1.0f : 0.0f;
        }
    }
}
