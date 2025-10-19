using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static System.Math;

namespace MechJebLib.PSG
{
    public class AscentGuesser
    {
        private readonly Problem         _problem;
        private readonly PhaseCollection _phases;

        public AscentGuesser(Problem problem, PhaseCollection phases)
        {
            _problem = problem;
            _phases  = phases;
            _events  = new List<Event> { new Event(OrbitalEnergy) };
        }

        private class VacuumThrustKernel
        {
            public static int N => InterpolantLayout.INTERPOLANT_LAYOUT_LEN;

            // ReSharper disable once NullableWarningSuppressionIsUsed
            public Phase Phase = null!;

            public void dydt(IList<double> yin, double x, IList<double> dyout)
            {
                Check.True(Phase.Normalized);

                var y  = InterpolantLayout.CreateFrom(yin);
                var dy = new InterpolantLayout();

                double thrust = Phase.Thrust;
                double at     = thrust / y.M;

                double r2 = V3.Dot(y.R, y.R);
                double r  = Sqrt(r2);
                double r3 = r2 * r;

                dy.R  = y.V;
                dy.V  = -y.R / r3 + at * y.U;
                dy.M  = -Phase.Mdot;
                dy.U  = V3.zero;
                dy.Dv = at;

                dy.CopyTo(dyout);
            }
        }

        private double OrbitalEnergy(IList<double> yin, double t, AbstractIVP i)
        {
            var y = InterpolantLayout.CreateFrom(yin);

            double r  = y.R.magnitude;
            double v2 = y.V.sqrMagnitude;
            double e  = 0.5 * v2 - 1.0 / r;

            return e - _targetOrbitalEnergy;
        }

        private          double             _targetOrbitalEnergy;
        private readonly VacuumThrustKernel _ode    = new VacuumThrustKernel();
        private readonly DP5                _solver = new DP5();
        private readonly List<Event>        _events;

        private void Integrate(Vn y0, Vn yf, Phase phase, double t0, double tf, Solution solution)
        {
            _solver.ThrowOnMaxIter = true;
            _solver.Maxiter        = 2000;
            _solver.Rtol           = 1e-6;
            _solver.Atol           = 1e-6;
            _ode.Phase             = phase;
            var interpolant = Hn.Get(VacuumThrustKernel.N);
            _solver.Solve(_ode.dydt, y0, yf, t0, tf, interpolant, _events);
            solution.AddSegment(interpolant.MinTime, interpolant.MaxTime, interpolant, phase);
        }

        private void Shooting(Solution solution, V3 u0, double targetOrbitalEnergy)
        {
            using var initial  = Vn.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var terminal = Vn.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

            var y0 = new InterpolantLayout();

            double t0 = 0;

            y0.R  = _problem.R0;
            y0.V  = _problem.V0;
            y0.U  = u0.normalized;
            y0.Dv = 0;

            foreach (Phase phase in _phases)
            {
                y0.M = phase.m0;

                double e0 = 0.5 * y0.V.sqrMagnitude - 1.0 / y0.R.magnitude;

                double bt = phase.bt;
                if (e0 > targetOrbitalEnergy)
                    bt = 0;
                double tf = t0 + bt;

                y0.CopyTo(initial);

                Integrate(initial, terminal, phase, t0, tf, solution);

                y0.CopyFrom(terminal);

                t0 = solution.Tmax;
            }
        }

        public Solution InitialGuess(double incT, double targetOrbitalEnergy)
        {
            _targetOrbitalEnergy = targetOrbitalEnergy;

            V3 r0 = _problem.R0;
            // guess the initial launch direction
            V3 enu = Astro.ENUHeadingForInclination(incT, r0);
            // add 45 degrees up
            enu.z = 1.0;
            V3 u0 = Astro.ENUToECI(r0, enu).normalized;

            var solution = new Solution(_problem);

            Shooting(solution, u0, targetOrbitalEnergy);

            return solution;
        }
    }
}
