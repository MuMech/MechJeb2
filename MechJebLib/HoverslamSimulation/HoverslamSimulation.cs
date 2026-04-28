/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Rootfinding;
using MechJebLib.Utils;

namespace MechJebLib.HoverslamSimulation
{
    public partial class HoverslamSimulation : AsyncJob
    {
        private double _height;
        private V3     _r0;
        private V3     _v0;
        private double _t0;
        private double _mu;
        private V3     _w;
        private double _vfm;

        private List<Phase> _phases = new List<Phase>();

        public V3     Rf;
        public V3     Vf;
        public double IgnitionUT;
        public V3     IgnitionAttitude;
        public double FinalThrustAccel;
        public double LandingUT;
        public double Dv;

        public static HoverslamSimulationBuilder Builder() => new HoverslamSimulationBuilder();

        private void Reset()
        {
            Rf = V3.zero;
            Vf = V3.zero;
            IgnitionUT = double.NaN;
            LandingUT = double.NaN;
            IgnitionAttitude = V3.zero;
            FinalThrustAccel = 0;
            Dv = 0;
        }

        private struct TrajectoryResult
        {
            public HoverslamLayout X;
            public double          Tf;
            public V3              UBurn;
            public double          Af;
        }

        private bool PropagatePhase(ref HoverslamLayout x, double t0, ref double tf, Phase phase)
        {
            var solver = new DP5
            {
                Rtol = 1e-9,
                Atol = 1e-9,
                Maxiter = 2000,
                ThrowOnMaxIter = false,
                ThrowOnMinStep = false
            };
            var ode = new HoverslamODE(_mu, _w, _vfm, phase);
            var f   = new Action<IList<double>, double, IList<double>>(ode.dydt);

            using var y0 = Vn.Rent(HoverslamODE.N);
            using var yf = Vn.Rent(HoverslamODE.N);

            x.CopyTo(y0);
            if (phase.Coast)
                solver.Solve(f, y0, yf, t0, tf);
            else
                solver.Solve(f, y0, yf, t0, tf, events: _events);
            x.CopyFrom(yf);

            tf = solver.T;
            return solver.Status == AbstractIVP.IVPStatus.EventTerminated;
        }

        private readonly List<Event> _events = new List<Event> { new Event(ZeroVerticalVelocity) };

        private static double ZeroVerticalVelocity(IList<double> yin, double t, AbstractIVP i)
        {
            var y = HoverslamLayout.CreateFrom(yin);

            return V3.Dot(y.R, y.V);
        }

        private TrajectoryResult PropagateTrajectory(double t)
        {
            var x = new HoverslamLayout { R = _r0, V = _v0, M = _phases[0].M0, DV = 0 };

            double t0 = 0;
            double tf = t;

            var coastPhase = Phase.NewCoast(1.0, t0, tf, 0, 0);
            PropagatePhase(ref x, t0, ref tf, coastPhase);

            V3     vRel  = x.V - V3.Cross(_w, x.R);
            V3     vf    = x.R.normalized * _vfm;
            V3     uBurn = -(vRel - vf).normalized;
            double af    = 0;

            for (int p = 0; p < _phases.Count; p++)
            {
                Phase phase = _phases[p];
                x.M = phase.M0;
                t0 = tf;
                tf = p == _phases.Count - 1 ? t0 + phase.Tau : t0 + phase.Bt;

                bool done = PropagatePhase(ref x, t0, ref tf, phase);

                af = phase.VacThrust / x.M;

                if (done)
                    break;
            }

            return new TrajectoryResult { X = x, Tf = tf, UBurn = uBurn, Af = af };
        }

        private double AltitudeResidual(double t, object? o) => PropagateTrajectory(t).X.R.magnitude - _height;

        public override void Run(object? o = null)
        {
            double maxt = Astro.TimeToNextRadius(_mu, _r0, _v0, _height);
            double mint = Astro.TimeToNextApoapsis(_mu, _r0, _v0);
            if (mint > maxt)
                mint -= Astro.PeriodFromStateVectors(_mu, _r0, _v0);
            double t1;

            try
            {
                t1 = BrentRoot.Solve(AltitudeResidual, mint, maxt, null, rtol: 1e-8);
            }
            catch (Exception)
            {
                Reset();
                throw;
            }

            TrajectoryResult result = PropagateTrajectory(t1);

            IgnitionUT = _t0 + t1;
            IgnitionAttitude = result.UBurn;
            Rf = result.X.R;
            Vf = result.X.V;
            Dv = result.X.DV;
            LandingUT = _t0 + result.Tf;
            FinalThrustAccel = result.Af;
        }

        private class HoverslamODE
        {
            public static int N => HoverslamLayout.HOVERSLAM_LAYOUT_LEN;

            private readonly Phase  _phase;
            private readonly double _mu;
            private readonly V3     _w;
            private readonly double _vfm; // final vertical speed, should be negative or zero

            public HoverslamODE(double mu, V3 w, double vfm, Phase phase)
            {
                _mu = mu;
                _w = w;
                _vfm = vfm;
                _phase = phase;
            }

            public void dydt(IList<double> yin, double x, IList<double> dyout)
            {
                // TODO: normalization
                //Check.True(_phase.Normalized);

                var y  = HoverslamLayout.CreateFrom(yin);
                var dy = new HoverslamLayout();

                double at = _phase.VacThrust / y.M;

                double r2 = y.R.sqrMagnitude;
                double r  = Math.Sqrt(r2);
                double r3 = r2 * r;

                V3 vRel = y.V - V3.Cross(_w, y.R);
                V3 vf   = y.R.normalized * _vfm;
                V3 u    = -(vRel - vf).normalized;

                dy.R = y.V;
                dy.V = -_mu * y.R / r3 + at * u;
                dy.M = -_phase.Mdot;
                dy.DV = at;

                dy.CopyTo(dyout);
            }
        }
    }
}
