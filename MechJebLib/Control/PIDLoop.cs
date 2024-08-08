/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.Control
{
    public class PIDLoop : IPIDLoop
    {
        // internal state for PID filter
        private double _d1, _d2;
        private double _u1 = double.NaN;

        // internal state for last measured and last output for low pass filters
        private double _y1 = double.NaN;
        public double Kp { get; set; } = 1.0;
        public double Ki { get; set; }
        public double Kd { get; set; }
        public double H { get; set; } = 0.02;
        public double N { get; set; } = 50; // N = (4/Ts) * (1 - e^(-Ts/(2*Tf))) (trapezoidal discretization)
        public double B { get; set; } = 1;
        public double C { get; set; } = 1;
        public double SmoothIn { get; set; } = 1.0;
        public double SmoothOut { get; set; } = 1.0;
        public double ProportionalDeadband { get; set; }
        public double IntegralDeadband { get; set; }
        public double DerivativeDeadband { get; set; }
        public double OutputDeadband { get; set; }
        public double MinOutput { get; set; } = double.MinValue;
        public double MaxOutput { get; set; } = double.MaxValue;

        /// <summary>
        /// </summary>
        /// <param name="r">reference signal</param>
        /// <param name="y">measured control signal</param>
        /// <returns></returns>
        public double Update(double r, double y)
        {
            // lowpass filter the input
            y = IsFinite(_y1) ? _y1 + SmoothIn * (y - _y1) : y;

            double ep = ApplyDeadband(B * r - y, ProportionalDeadband);
            double ei = ApplyDeadband(r - y, IntegralDeadband);
            double ed = ApplyDeadband(C * r - y, DerivativeDeadband);

            // trapezoidal PID with derivative filtering as a digital biquad filter
            double a0 = 2 * N * H + 4;
            double a1 = -8 / a0;
            double a2 = (-2 * N * H + 4) / a0;
            double b0 = (4 * Kp * ep + 4 * Kd * ed * N + 2 * Ki * ei * H + 2 * Kp * ep * N * H + Ki * ei * N * H * H) /
                        a0;
            double b1 = (2 * Ki * ei * N * H * H - 8 * Kp * ep - 8 * Kd * ed * N) / a0;
            double b2 = (4 * Kp * ep + 4 * Kd * ed * N - 2 * Ki * ei * H - 2 * Kp * ep * N * H + Ki * ei * N * H * H) /
                        a0;

            // if we have NaN values saved into internal state that needs to be cleared here or it won't reset
            if (!IsFinite(_d1))
                _d1 = 0;
            if (!IsFinite(_d2))
                _d2 = 0;

            // transposed direct form 2
            double z = ApplyDeadband(b0 + _d1, OutputDeadband);
            double u = Clamp(z, MinOutput, MaxOutput);
            _d1 = b1 - a1 * u + _d2;
            _d2 = b2 - a2 * u;

            // low pass filter the output
            _u1 = IsFinite(_u1) ? _u1 + SmoothOut * (u - _u1) : u;

            _y1 = y;

            return _u1;
        }

        private double ApplyDeadband(double v, double deadband)
        {
            if (Abs(v) < deadband)
                return 0;
            return v - Sign(v) * deadband;
        }

        public void Reset()
        {
            _d1 = _d2 = 0;
            _y1 = _u1 = double.NaN;
        }
    }
}
