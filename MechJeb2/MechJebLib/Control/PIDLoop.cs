/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Control
{
    public class PIDLoop
    {
        public double Kp        { get; set; } = 1.0;
        public double Ki        { get; set; }
        public double Kd        { get; set; }
        public double Ts        { get; set; } = 0.02;
        public double N         { get; set; } = 50;
        public double B         { get; set; } = 1;
        public double C         { get; set; } = 1;
        public double SmoothIn  { get; set; } = 1.0;
        public double SmoothOut { get; set; } = 1.0;
        public double Deadband  { get; set; } = 0;
        public double MinOutput { get; set; } = double.MinValue;
        public double MaxOutput { get; set; } = double.MaxValue;
        public bool   Clegg     { get; set; } = true;

        // internal state for PID filter
        private double _d1, _d2;

        // internal state for last measured and last output for low pass filters
        private double _m1 = double.NaN;
        private double _o1 = double.NaN;

        // for zero crossing logic
        private double _delta1 = double.NaN;

        public double Update(double reference, double measured)
        {
            // lowpass filter the input
            measured = _m1.IsFinite() ? _m1 + SmoothIn * (measured - _m1) : measured;

            double delta = reference - measured;

            if (Math.Abs(delta) < Deadband)
                delta = 0;
            else
                delta -= Math.Sign(delta) * Deadband;

            // clegg filtering on zero-crossing to remove integral windup
            if (_delta1.IsFinite() && Clegg && ((delta < 0 && _delta1 > 0) || (delta > 0 && _delta1 < 0)))
                _d1 = _d2 = 0;

            double ep = B * delta;
            double ei = delta;
            double ed = C * delta;

            // trapezoidal PID with derivative filtering as a digital biquad filter
            double a0 = 2 * N * Ts + 4;
            double a1 = -8 / a0;
            double a2 = (-2 * N * Ts + 4) / a0;
            double b0 = (4 * Kp * ep + 4 * Kd * ed * N + 2 * Ki * ei * Ts + 2 * Kp * ep * N * Ts + Ki * ei * N * Ts * Ts) / a0;
            double b1 = (2 * Ki * ei * N * Ts * Ts - 8 * Kp * ep - 8 * Kd * ed * N) / a0;
            double b2 = (4 * Kp * ep + 4 * Kd * ed * N - 2 * Ki * ei * Ts - 2 * Kp * ep * N * Ts + Ki * ei * N * Ts * Ts) / a0;

            // if we have NaN values saved into internal state that needs to be cleared here or it won't reset
            if (!_d1.IsFinite())
                _d1 = 0;
            if (!_d2.IsFinite())
                _d2 = 0;

            // transposed direct form 2
            double u0 = b0 + _d1;
            u0  = Clamp(u0, MinOutput, MaxOutput);
            _d1 = b1 - a1 * u0 + _d2;
            _d2 = b2 - a2 * u0;

            // low pass filter the output
            _o1 = _o1.IsFinite() ? _o1 + SmoothOut * (u0 - _o1) : u0;

            _m1     = measured;
            _delta1 = delta;

            return _o1;
        }

        public void Reset()
        {
            _d1 = _d2 = 0;
            _m1 = _o1 = _delta1 = double.NaN;
        }
    }
}
