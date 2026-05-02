using System;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class PDGGuidanceLoop
    {
        /// <summary>
        /// Called every physics tick after <see cref="OnFixedUpdate"/> to write throttle.
        /// Attitude is driven via <c>Core.Attitude</c> inside <see cref="OnFixedUpdate"/>.
        /// </summary>
        public override AutopilotStep Drive(FlightCtrlState s)
        {
            float throttleCmd = PulseThrottleMode
                ? PulseThrottleCommand(_targetThrottle)
                : _targetThrottle;

            Core.Thrust.RequestActiveThrottle(throttleCmd, allowZero: true);
            CurrentThrottle = throttleCmd;
            return this;
        }

        // =========================================================================
        // Pulse-width modulation (optional throttle dithering)
        // =========================================================================

        /// <summary>
        /// Converts a continuous throttle command to a binary (0/1) PWM signal with
        /// the configured <see cref="PulsePeriod"/>.  Handles edge cases: command &lt;=0
        /// → always off; command ≥1 → always on; on-time &lt; 50 ms → treated as off.
        /// </summary>
        private float PulseThrottleCommand(float commandedThrottle)
        {
            commandedThrottle = Mathf.Clamp01(commandedThrottle);
            if (commandedThrottle <= 0.0f) { _pulseTimer = 0.0; return 0.0f; }
            if (commandedThrottle >= 0.999f) { _pulseTimer = 0.0; return 1.0f; }

            double period = Math.Max(0.2, PulsePeriod);
            _pulseTimer += TimeWarp.fixedDeltaTime;
            while (_pulseTimer >= period) _pulseTimer -= period;

            double onTime = commandedThrottle * period;
            if (onTime < 0.05)              return 0.0f;
            if ((period - onTime) < 0.05)   return 1.0f;
            return _pulseTimer < onTime ? 1.0f : 0.0f;
        }
    }
}