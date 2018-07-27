using System;

namespace MuMech
{
    public delegate void ODEFun(double[] y, double x, double[] dy, object o);

    class ODE
    {
        public double[] y0;
        public int n;
        public double[] xtbl;
        public double eps;
        public double hstart;
        public double hmin, hmax;
        public int maxiter;  // since maxiter results in a throw it should be very high
        public double[][] ytbl;

        // y0  - array of starting y0 values
        // n   - dimensionality of the problem
        // x   - array of x values to evaluate at
        // eps - accuracy
        // h   - starting step-size (can be zero)
        public ODE(double[] y0, int n, double[] xtbl, double eps, double h, double hmin = 0, double hmax = 0, int maxiter = 0)
        {
            this.y0      = y0;
            this.n       = n;
            this.xtbl    = xtbl;
            this.eps     = eps;
            this.hstart  = h;
            this.hmin    = hmin;
            this.hmax    = hmax;
            this.maxiter = maxiter;
        }

        // adds an array to another array, with a constant multiplier
        private void rkadd(double[] aref, double[] ain, double c = 1.0)
        {
            if (aref.Length != ain.Length)
                throw new ArgumentException("array lengths do not match");

            for(int i = 0; i < aref.Length; i++)
                aref[i] = aref[i] + c * ain[i];
        }

        public void RKF45(ODEFun f, object o)
        {
            double[] k1  = new double[n];
            double[] k2  = new double[n];
            double[] k3  = new double[n];
            double[] k4  = new double[n];
            double[] k5  = new double[n];
            double[] k6  = new double[n];
            double[] a   = new double[n]; // accumulator
            double[] err = new double[n]; // error
            double[] z   = new double[n]; // left as zeros
            double h     = hstart;
            double x     = xtbl[0];
            double[] y   = new double[n];
            double[] dy  = new double[n];
            ytbl         = new double[n][]; // output
            for(int i = 0; i < n; i++)
                ytbl[i] = new double[xtbl.Length];

            y0.CopyTo(y, 0);

            // auto-guess starting value of h based on smallest dx in xtbl
            if ( h == 0 )
            {
                double v = xtbl[1] - xtbl[0];
                for(int i = 1; i < xtbl.Length-1; i++)
                {
                    v = Math.Min(v, Math.Abs(xtbl[i+1]-xtbl[i]));
                }
                h = 0.001*v;
            }

            bool at_hmin;
            double niter = 0;

            int j = 0;

            for(int i = 0; i < n; i++)
                ytbl[i][j] = y[i];

            j++;

            while(j < xtbl.Length)
            {
                double xf = xtbl[j];

                while (Math.Abs(x) < Math.Abs(xf))
                {
                    at_hmin = false;
                    if (hmax > 0 && Math.Abs(h) > hmax)
                        h = hmax * Math.Sign(h);
                    if (hmin > 0 && Math.Abs(h) < hmin)
                    {
                        h = hmin * Math.Sign(h);
                        at_hmin = true;
                    }
                    if (Math.Abs(h) > Math.Abs(xf - x))
                        h = xf - x;

                    z.CopyTo(k1, 0);
                    f(y, x, dy, o);
                    rkadd(k1, dy, h);

                    y.CopyTo(a, 0);
                    rkadd(a, k1, 0.25);
                    z.CopyTo(k2, 0);
                    f(a, x+0.25*h, dy, o);
                    rkadd(k2, dy, h);

                    y.CopyTo(a, 0);
                    rkadd(a, k1, 3.0/32.0);
                    rkadd(a, k2, 9.0/32.0);
                    z.CopyTo(k3, 0);
                    f(a, x+3*h/8, dy, o);
                    rkadd(k3, dy, h);

                    y.CopyTo(a, 0);
                    rkadd(a, k1, 1932.0/2197.0);
                    rkadd(a, k2, -7200.0/2197.0);
                    rkadd(a, k3, 7296.0/2197.0);
                    z.CopyTo(k4, 0);
                    f(a, x+12*h/13, dy, o);
                    rkadd(k4, dy, h);

                    y.CopyTo(a, 0);
                    rkadd(a, k1, 439.0/216.0);
                    rkadd(a, k2, -8.0);
                    rkadd(a, k3, 3680.0/513.0);
                    rkadd(a, k4, -845.0/4104.0);
                    z.CopyTo(k5, 0);
                    f(a, x+h, dy, o);
                    rkadd(k5, dy, h);

                    y.CopyTo(a, 0);
                    rkadd(a, k1, -8.0/27.0);
                    rkadd(a, k2, 2.0);
                    rkadd(a, k3, -3544.0/2565.0);
                    rkadd(a, k4, 1859.0/4104.0);
                    rkadd(a, k5, -11.0/40.0);
                    z.CopyTo(k6, 0);
                    f(a, x+0.5*h, dy, o);
                    rkadd(k6, dy, h);

                    z.CopyTo(err, 0);
                    rkadd(err, k1, 1.0/360.0);
                    rkadd(err, k3, -128.0/4275.0);
                    rkadd(err, k4, -2197.0/75240.0);
                    rkadd(err, k5, 1.0/50.0);
                    rkadd(err, k6, 2.0/55.0);

                    double error = 0;
                    for(int i = 0; i < err.Length; i++)
                    {
                        error = Math.Max(error, Math.Abs(err[i] / h));
                    }
                    double s = Math.Pow(0.5*eps/error,0.25);

                    if (error < eps || at_hmin)
                    {
                        rkadd(y, k1, 25.0/216.0);
                        rkadd(y, k3, 1408.0/2565.0);
                        rkadd(y, k4, 2197.0/4104.0);
                        rkadd(y, k5, -0.2);
                        x = x + h;
                    }

                    if (s < 0.1)
                        s = 0.1;
                    if (s > 4)
                        s = 4;
                    h = h*s;

                    if (maxiter > 0 && niter++ >= maxiter)
                        throw new ArgumentException("maximum iterations exceeded");
                }

                for(int i = 0; i < n; i++)
                    ytbl[i][j] = y[i];

                j++;
            }
        }

        public static void centralForce(double[] y, double x, double[] dy, object o)
        {
            double r2 = y[0] * y[0] + y[1] * y[1] + y[2] * y[2];
            double r1 = Math.Sqrt(r2);
            double r3 = r2 * r1;

            // dx = v
            dy[0] = y[3];
            dy[1] = y[4];
            dy[2] = y[5];
            // dv = - r/r^3
            dy[3] = - y[0] / r3;
            dy[4] = - y[1] / r3;
            dy[5] = - y[2] / r3;
        }

        static void Main(string[] args)
        {
            double[] y0 = { 1, 0, 0, 0, 1, 0 };
            double[] x = { 0, -0.78539816339, -2 * 0.78539816339, -3 * 0.78539816339, -4 * 0.78539816339 };
            ODE ode = new ODE(y0, 6, x, 1e-9, 0);
            ode.RKF45(centralForce, null);
            for(int i = 0; i < 5; i++)
            {
                for(int j = 0; j < 6; j++)
                {
                    Console.Write(ode.ytbl[j][i]);
                    if (j != 5)
                        Console.Write(", ");
                }
                Console.Write("\n");
            }
        }
    }
}
