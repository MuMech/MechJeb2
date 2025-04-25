using System;
using System.Threading;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.Rootfinding;
using MechJebLib.TwoBody;
using static System.Math;

namespace MechJebLib.FunctionImpls
{
    /// <summary>
    /// Algorithm from:
    ///
    /// Han, S., Jo, B. U., & Ho, K. (2024). Terminal Soft Landing Guidance Law Using Analytic
    /// Gravity Turn Trajectory. Journal of Guidance, Control, and Dynamics, 47(9), 1808-1821.
    ///
    /// - beta is the constant twr of the rocket
    /// - g is considered constant (flat planet approximation)
    /// </summary>
    public static class RealSuicideBurnCalc
    {
        private class Args
        {
            public double Mu;
            public V3     R0;
            public V3     V0;
            public double Beta;
            public double G;
            public double Radius;

            public void Set(double mu, V3 r0, V3 v0, double g, double beta, double radius)
            {
                Mu     = mu;
                R0     = r0;
                V0     = v0;
                G      = g;
                Beta   = beta;
                Radius = radius;
            }
        }

        private static readonly ThreadLocal<Args> _threadArgs = new ThreadLocal<Args>(() => new Args());

        private static readonly Func<double, object?, double> _f = F;

        private static double C(double v0, double gamma0, double beta)
        {
            double sec = 1 / Cos(gamma0);
            return v0 / (sec * Pow(sec + Tan(gamma0), beta));
        }

        private static double Fx(double gamma, double beta)
        {
            double sec = 1.0 / Cos(gamma);
            double tan = Tan(gamma);
            return 1.0 / (4.0 * beta * beta - 1.0) * (2.0 * beta * sec - tan) * Pow(sec + tan, 2.0 * beta);
        }

        private static double Fz(double gamma, double beta)
        {
            double sec = 1.0 / Cos(gamma);
            double tan = Tan(gamma);
            return 1.0 / (4.0 * beta * beta - 4.0) * (2.0 * beta * sec * tan - 2.0 * tan * tan - 1.0) * Pow(sec + tan, 2.0 * beta);
        }

        private static double F(double dt, object? o)
        {
            var args = (Args)o!;

            double beta = args.Beta;

            (V3 r1, V3 v1) = Shepperd.Solve(args.Mu, dt, args.R0, args.V0); // coasting trajectory

            double gamma0 = Astro.FlightPathAngle(r1, v1);
            double c      = C(v1.magnitude, gamma0, beta);

            // Fz(-pi/2, beta) is zero
            double val = r1.magnitude + c * c * Fz(gamma0, beta) / args.G - args.Radius;

            return val;
        }

        // FIXME: should allow passing a guess for dt based on the previous calculation
        public static (double dt, V3 rland) Run(double mu, V3 r0, V3 v0, double beta, double radius)
        {
            Args args = _threadArgs.Value;

            // Ensure the surface is below the vessel
            radius = Min(radius, r0.magnitude);

            double g = mu / (radius * radius);
            args.Set(mu, r0, v0, g, beta, radius);

            if (Astro.PeriapsisFromStateVectors(mu, r0, v0) > radius)
                return (double.PositiveInfinity, V3.zero);

            double b = Astro.TimeToNextRadius(mu, r0, v0, radius);
            double a = Astro.TimeToNextApoapsis(mu, r0, v0);
            if (a > b)
                a -= Astro.PeriodFromStateVectors(mu, r0, v0);

            double dt;

            try
            {
                dt = BrentRoot.Solve(_f, a, b, args);
            }
            catch (Exception)
            {
                return (double.NaN, V3.zero);
            }

            (V3 rign, V3 vign) = Shepperd.Solve(mu, dt, r0, v0);

            double gamma0    = Astro.FlightPathAngle(rign, vign);
            double c         = C(vign.magnitude, gamma0, beta);
            double downrange = c * c * Fx(gamma0, beta) / args.G;

            // Adjust r1 to account for the downrange distance
            var rot = Q3.AngleAxis(downrange / radius, V3.Cross(rign, vign).normalized);
            V3  rland  = rot * rign.normalized * radius;

            return (dt, rland);
        }
    }
}
