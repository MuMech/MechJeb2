using System.Collections.Generic;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using static MechJebLib.Utils.AutoDiff;

namespace MechJebLib.PSG
{
    public static class GuidanceLQR
    {
        private readonly struct Jacobian
        {
            public readonly M3 Ar;
            public readonly M3 Av;
            public readonly M3 Bu;

            public Jacobian(M3 ar, M3 av, M3 bu)
            {
                Ar = ar;
                Av = av;
                Bu = bu;
            }
        }

        private class RiccatiRHS
        {
            // weighting on position errors (diagonal cost matrix)
            private readonly M3 _qrr;

            // weighting on velocity errors (diagonal cost matrix)
            private readonly M3 _qvv;

            // weighting on correlated position-velocity errors (typically zero)
            private readonly M3 _qrv;

            // control effort penalty, increase for less control effort
            private readonly double _rho;

            public RiccatiRHS(double rho, M3 qrr, M3 qrv, M3 qvv)
            {
                _rho = rho;
                _qrr = qrr;
                _qrv = qrv;
                _qvv = qvv;
            }

            public static int N => RiccatiLayout.RICCATI_LAYOUT_LEN;

            public void RHS(IList<double> pin, double t, IList<double> dpout, JacobianCallback getJacobians)
            {
                var p = RiccatiLayout.CreateFrom(pin);

                Jacobian j = getJacobians(t);

                M3 s = 1.0 / _rho * (j.Bu * j.Bu.T());

                M3 prr = j.Ar.T() * p.Prv.T() + p.Prv * j.Ar - p.Prv * s * p.Prv.T() + _qrr;
                M3 prv = j.Ar.T() * p.Pvv + p.Prr + j.Av.T() * p.Prv.T() + p.Prv * j.Av - p.Prv * s * p.Pvv + _qrv;
                M3 pvv = p.Prv + p.Prv.T() + j.Av.T() * p.Pvv + p.Pvv * j.Av - p.Pvv * s * p.Pvv + _qvv;

                var dp = new RiccatiLayout(-prr, -prv, -pvv);

                dp.CopyTo(dpout);
            }
        }

        private static Hn Integrate(RiccatiRHS ode, Vn y0, Vn yf, double t0, double tf, JacobianCallback getJacobians)
        {
            var solver = new DP5 { ThrowOnMaxIter = true, Maxiter = 2000, Rtol = 1e-6, Atol = 1e-6 };

            var interpolant = Hn.Get(RiccatiRHS.N);

            solver.Solve(RHS, y0, yf, t0, tf, interpolant);

            return interpolant;

            void RHS(IList<double> pin, double t, IList<double> dpout)
            {
                ode.RHS(pin, t, dpout, getJacobians);
            }
        }

        private delegate Jacobian JacobianCallback(double t);

        private static Jacobian GetJacobians(double t, Solution solution, Problem problem, Phase phase, int p)
        {
            double rho0CdAref = problem.Rho0CdAref;
            double rBody      = problem.RBody;
            double h0         = problem.H0;
            double r0         = problem.R0.magnitude;
            V3     w          = problem.W;

            double mdot       = phase.Mdot;
            double vacThrust  = phase.Thrust;
            double vexCurrent = phase.VexCurrent;
            double vexVacuum  = phase.VexVacuum;

            var d = new HermiteSimpsonDualPoint { R = new DualV3(solution.RBar(p, t)), V = new DualV3(solution.VBar(p, t)), M = new Dual(solution.MBar(p, t)), U = new DualV3(solution.UBar(p, t)) };

            // dvdot / dr

            d.R = new DualV3(d.R.M, new V3(1, 0, 0));

            DualV3 ans1 = Vdot(ref d);

            d.R = new DualV3(d.R.M, new V3(0, 1, 0));

            DualV3 ans2 = Vdot(ref d);

            d.R = new DualV3(d.R.M, new V3(0, 0, 1));

            DualV3 ans3 = Vdot(ref d);

            d.R = new DualV3(d.R.M, new V3(0, 0, 0));

            // dvdot / dv

            d.V = new DualV3(d.V.M, new V3(1, 0, 0));

            DualV3 ans4 = Vdot(ref d);

            d.V = new DualV3(d.V.M, new V3(0, 1, 0));

            DualV3 ans5 = Vdot(ref d);

            d.V = new DualV3(d.V.M, new V3(0, 0, 1));

            DualV3 ans6 = Vdot(ref d);

            d.V = new DualV3(d.V.M, new V3(0, 0, 0));

            // dvdot / du

            d.U = new DualV3(d.U.M, new V3(1, 0, 0));

            DualV3 ans7 = Vdot(ref d);

            d.U = new DualV3(d.U.M, new V3(0, 1, 0));

            DualV3 ans8 = Vdot(ref d);

            d.U = new DualV3(d.U.M, new V3(0, 0, 1));

            DualV3 ans9 = Vdot(ref d);

            d.U = new DualV3(d.U.M, new V3(0, 0, 0));

            return new Jacobian(
                new M3(ans1.D, ans2.D, ans3.D),
                new M3(ans4.D, ans5.D, ans6.D),
                new M3(ans7.D, ans8.D, ans9.D)
            );

            DualV3 Vdot(ref HermiteSimpsonDualPoint d)
            {
                if (h0 > 0 && rho0CdAref > 0)
                {
                    Dual   r        = d.R.magnitude;
                    Dual   r3       = d.R.sqrMagnitude * r;
                    DualV3 vr       = d.V - DualV3.Cross(w, d.R);
                    var    normAtm  = Dual.Exp(-(r - rBody) / h0);
                    var    normAtm2 = Dual.Exp(-(r - r0) / h0);
                    DualV3 drag     = 0.5 * rho0CdAref * normAtm * vr.sqrMagnitude * vr.normalized;
                    Dual   thrust   = mdot * (vexCurrent + (vexVacuum - vexCurrent) * (1.0 - normAtm2));

                    return -d.R / r3 + thrust / d.M * d.U - drag / d.M;
                }
                else
                {
                    Dual r3 = d.R.sqrMagnitude * d.R.magnitude;
                    return -d.R / r3 + vacThrust / d.M * d.U;
                }
            }
        }

        public static void ApplyLQR(Solution solution)
        {
            const double RHO = 1.0;

            Problem problem = solution.Problem;

            using var initial  = Vn.Rent(RiccatiLayout.RICCATI_LAYOUT_LEN);
            using var terminal = Vn.Rent(RiccatiLayout.RICCATI_LAYOUT_LEN);

            var interpolants = new List<Hn>();

            var ode = new RiccatiRHS(RHO, M3.identity, M3.zero, M3.identity);
            for (int p = solution.Phases.Count - 1; p >= 0; p--)
            {
                Phase phase = solution.Phases[p];

                // XXX: need to "skip" coast phases and reset the riccati state to zero
                // -- but we may want to include an interpolant for the coast phase as well to keep them 1:1

                int p2 = p;

                interpolants.Add(
                    Integrate(ode, initial, terminal, solution.EndTbar(p), solution.StartTbar(p), GetJacobiansFn)
                );

                initial.CopyFrom(terminal);

                continue;

                Jacobian GetJacobiansFn(double t)
                {
                    return GetJacobians(t, solution, problem, phase, p2);
                }
            }

            interpolants.Reverse();

            const int NUM_SEGMENTS = 10;

            for (int p = 0; p < solution.Phases.Count; p++)
            {
                Phase  phase = solution.Phases[p];
                double ti    = solution.StartTbar(p);
                double tf    = solution.EndTbar(p);
                double dt    = (tf - ti) / NUM_SEGMENTS;

                var lqrInterpolant = Hn.Get(LQRLayout.LQR_LAYOUT_LEN);

                for (int i = 0; i <= NUM_SEGMENTS; i++)
                {
                    double t = ti + (tf - ti) * dt;

                    Jacobian j          = GetJacobians(t, solution, problem, phase, p);
                    using Vn pRaw       = interpolants[p].Evaluate(t);
                    var      riccati    = RiccatiLayout.CreateFrom(pRaw);
                    M3       buTOverRho = 1.0 / RHO * j.Bu.T();
                    M3       kr         = buTOverRho * riccati.Prv.T();
                    M3       kv         = buTOverRho * riccati.Pvv;

                    var       layout = new LQRLayout(kr, kv);
                    using var lqr    = Vn.Rent(LQRLayout.LQR_LAYOUT_LEN);
                    layout.CopyTo(lqr);

                    lqrInterpolant.Add(t, lqr);
                }

                solution.AddController(lqrInterpolant);
            }
        }
    }
}
