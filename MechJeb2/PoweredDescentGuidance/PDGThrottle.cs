using System;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class PDGGuidanceLoop
    {
        /// <summary>
        /// Drive is called every physics tick after OnFixedUpdate.
        /// Sets throttle. Attitude is driven by Core.Attitude.
        /// </summary>
        public override AutopilotStep Drive(FlightCtrlState s)
        {
            float rawThrottle = _targetThrottle;
            float throttleCmd = rawThrottle;

            if (PulseThrottleMode)
                throttleCmd = PulseThrottleCommand(rawThrottle);

            Core.Thrust.RequestActiveThrottle(throttleCmd, allowZero: true);

            CurrentThrottle = throttleCmd;

            return this;
        }

        private float PulseThrottleCommand(float commandedThrottle)
        {
            commandedThrottle = Mathf.Clamp01(commandedThrottle);

            if (commandedThrottle <= 0.0f)
            {
                _pulseTimer = 0.0;
                return 0.0f;
            }

            if (commandedThrottle >= 0.999f)
            {
                _pulseTimer = 0.0;
                return 1.0f;
            }

            double period = Math.Max(0.2, PulsePeriod);

            _pulseTimer += TimeWarp.fixedDeltaTime;
            while (_pulseTimer >= period)
                _pulseTimer -= period;

            double onTime = commandedThrottle * period;

            if (onTime < 0.05)
                return 0.0f;

            if ((period - onTime) < 0.05)
                return 1.0f;

            return _pulseTimer < onTime ? 1.0f : 0.0f;
        }
    }
}