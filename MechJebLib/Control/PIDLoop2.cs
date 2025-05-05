/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.Control
{
    //
    // 1. 2DOF PIDF controller with derivative filtering
    // 2. trapezoidal discretization
    // 3. standard and parallel form parameters
    // 4. optional input and output deadbands
    // 5. optional low pass filtering of input and output
    // 6. optional Clegg/First Order Reset Elements(FORE) integrator
    // 7. optional output saturation and tracking anti-windup
    //
    public class PIDLoop2 : IPIDLoop
    {
        // internal state for last error
        private double _ei1, _ed1;
        private double _u1 = double.NaN;

        // internal state for last measured and last output for low pass filters
        private double _y1 = double.NaN;

        // internal state for PID filter
        public double PTerm { get; private set; }
        public double ITerm { get; private set; }
        public double DTerm { get; private set; }

        // standard form parameters
        public double K  { get; set; } = 1.0;
        public double Ti { get; set; }
        public double Td { get; set; }
        public double N  { get; set; } = 50;
        public double H  { get; set; } = 0.02;

        // parallel form parameters
        public double Kp { set => K = value; } // TODO: rescale Ti and Td to keep Ki and Kd constant
        public double Ki { set => Ti = K / value; }
        public double Kd { set => Td = K * value; }
        public double Tf { set => N = 4 / value * (1 - Exp(-value / (2 * H))); }

        // 2DOF PIDF parameters
        public double B { get; set; } = 1;
        public double C { get; set; } = 1;

        // optional extensions
        public double SmoothIn             { get; set; } = 1.0;
        public double SmoothOut            { get; set; } = 1.0;
        public double ProportionalDeadband { get; set; }
        public double IntegralDeadband     { get; set; } // probably the most useful deadband and necessary
        public double DerivativeDeadband   { get; set; }
        public double OutputDeadband       { get; set; }
        public double MinOutput            { get; set; } = double.MinValue;
        public double MaxOutput            { get; set; } = double.MaxValue;
        public bool   FORE                 { get; set; } // not recommended if integrator required to zero the setpoint
        public double FORETerm             { get; set; } = -1.0;

        public double Update(double r, double y)
        {
            // low-pass filter the input
            y = IsFinite(_y1) ? _y1 + SmoothIn * (y - _y1) : y;

            double ep = ApplyDeadband(B * r - y, ProportionalDeadband);
            double ei = ApplyDeadband(r - y, IntegralDeadband);
            double ed = ApplyDeadband(C * r - y, DerivativeDeadband);

            PTerm = K * ep;

            if (FORE)
                if (ei * ITerm < 0)
                    ITerm = 0;
                else
                    ITerm += FORETerm * ITerm;

            double k = K == 0 ? 1 : K;
            ITerm += 0.5 * k * H * (ei + _ei1) / Ti;

            double den = 2 + N * H;
            DTerm = (2 - N * H) / den * DTerm + 2 * N * Td / (K * den) * (ed - _ed1);

            // fix any NaNs saved into internal state (also fixes Ti == 0 case)
            if (!IsFinite(ITerm))
                ITerm = 0;
            if (!IsFinite(DTerm))
                DTerm = 0;

            double z = ApplyDeadband(PTerm + ITerm + DTerm, OutputDeadband);
            double u = Clamp(z, MinOutput, MaxOutput);

            // anti-windup
            // TODO: optional clamping
            if (Ti != 0)
            {
                double tr = Td == 0 ? Ti : Sqrt(Ti * Td);
                ITerm += H / tr * (u - z);
            }

            // low-pass filter the output
            _u1 = IsFinite(_u1) ? _u1 + SmoothOut * (u - _u1) : u;

            _y1  = y;
            _ei1 = ei;
            _ed1 = ed;

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
            ITerm = DTerm = _ei1 = _ed1 = 0;
            _y1   = _u1   = double.NaN;
        }
    }
}
