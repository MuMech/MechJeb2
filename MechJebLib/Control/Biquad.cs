namespace MechJebLib.Control
{
    public class Biquad
    {
        public double B0 { get; }
        public double B1 { get; }
        public double B2 { get; }
        public double A1 { get; }
        public double A2 { get; }

        private double _s0;
        private double _s1;

        public Biquad(double b0, double b1, double b2, double a1, double a2)
        {
            B0 = b0;
            B1 = b1;
            B2 = b2;
            A1 = a1;
            A2 = a2;
        }

        public Biquad(double b0, double b1, double b2, double a0, double a1, double a2)
            : this(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0)
        {
        }

        public double Process(double x)
        {
            double y = B0 * x + _s0;
            _s0 = B1 * x - A1 * y + _s1;
            _s1 = B2 * x - A2 * y;
            return y;
        }

        public void Reset()
        {
            _s0 = 0;
            _s1 = 0;
        }
    }
}
