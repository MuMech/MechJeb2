namespace MuMech.AttitudeControllers
{
    public class PIDLoop
    {
        public double Kp        { get; set; } = 1.0;
        public double Ki        { get; set; }
        public double Kd        { get; set; }
        public double Ts        { get; set; } = 0.02;
        public double N         { get; set; } = 50;
        public double AlphaIn   { get; set; } = 1.0;
        public double AlphaOut  { get; set; } = 1.0;
        public double MinOutput { get; set; } = double.MinValue;
        public double MaxOutput { get; set; } = double.MaxValue;

        // internal state for PID filter
        private double _d1, _d2;

        // internal state for last measured and last output for low pass filters
        private double _m1 = double.NaN;
        private double _o1 = double.NaN;

        public double Update(double reference, double measured)
        {
            // lowpass filter the input
            measured = _m1.IsFinite() ? _m1 + AlphaIn * (measured - _m1) : measured;

            double e0 = reference - measured;

            // trapezoidal PID with derivative filtering as a digital biquad filter
            double a0 = 2 * N * Ts + 4;
            double a1 = -8 / a0;
            double a2 = (-2 * N * Ts + 4) / a0;
            double b0 = (4 * Kp + 4 * Kd * N + 2 * Ki * Ts + 2 * Kp * N * Ts + Ki * N * Ts * Ts) / a0;
            double b1 = (2 * Ki * N * Ts * Ts - 8 * Kp - 8 * Kd * N) / a0;
            double b2 = (4 * Kp + 4 * Kd * N - 2 * Ki * Ts - 2 * Kp * N * Ts + Ki * N * Ts * Ts) / a0;

            // transposed direct form 2
            double u0 = b0 * e0 + _d1;
            u0  = MuUtils.Clamp(u0, MinOutput, MaxOutput);
            _d1 = b1 * e0 - a1 * u0 + _d2;
            _d2 = b2 * e0 - a2 * u0;

            // low pass filter the output
            _o1 = _o1.IsFinite() ? _o1 + AlphaOut * (u0 - _o1) : u0;

            _m1 = measured;

            return _o1;
        }

        public void Reset()
        {
            _d1 = _d2 = 0;
            _m1 = double.NaN;
            _o1 = double.NaN;
        }
    }
}