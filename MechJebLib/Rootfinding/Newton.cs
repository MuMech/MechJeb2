using System;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.Rootfinding
{
    using RootFunc = Func<double, object?, double>;

    public static class Newton
    {
        /// <summary>
        ///     Newton's method search with fallback to Bisection.
        /// </summary>
        /// <param name="f">1 dimensional function</param>
        /// <param name="df">function derivative</param>
        /// <param name="a">minimum bounds</param>
        /// <param name="b">maximum bounds</param>
        /// <param name="o">state object to pass to function</param>
        /// <param name="maxiter">maximum iterations</param>
        /// <param name="tol">tolerance</param>
        /// <returns>value for which the function evaluates to zero</returns>
        /// <exception cref="ArgumentException">guess does not bracket the root</exception>
        public static double Solve(RootFunc f, RootFunc df, double a, double b, object? o, int maxiter = 100, double tol = EPS)
        {
            Check.Finite(a);
            Check.Finite(b);
            Check.NonNegative(tol);

            double fa = f(a, o);
            double fb = f(b, o);

            if (fa == 0)
                return a;

            if (fb == 0)
                return b;

            if (fa * fb > 0)
                throw new ArgumentException("Newton's rootfinding method: guess does not bracket the root");

            if (fa > 0)
                (a, b) = (b, a);

            double c = 0.5 * (a + b);
            double dxold = Abs(b - a);
            double dx = dxold;
            double fc = f(c, o);
            double dfc = df(c, o);
            for (int i = 0; i < maxiter; i++)
            {
                if (((c - b) * dfc - fc) * ((c - a) * dfc - fc) > 0.0 // jumping out of range
                    || Abs(2.0 * fc) > Abs(dxold * dfc))              // not making enough progress
                {
                    // bisection
                    dxold = dx;
                    dx    = 0.5 * (b - a);
                    c     = a + dx;
                    if (a == c)
                        return c;
                }
                else
                {
                    // newton
                    dxold = dx;
                    dx    = fc / dfc;
                    double oldc = c;
                    c -= dx;
                    if (oldc == c)
                        return c;
                }

                if (Abs(dx) < tol)
                    return c;

                fc  = f(c, o);
                dfc = df(c, o);

                if (fc < 0.0)
                    a = c;
                else
                    b = c;
            }

            throw new InvalidOperationException("Newton's method: maximum iterations exceeded");
        }
    }
}
