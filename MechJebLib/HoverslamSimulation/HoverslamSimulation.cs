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
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.HoverslamSimulation
{
    public partial class HoverslamSimulation : AsyncJob
    {
        private double _height;
        private V3 _r0;
        private V3 _v0;
        private double _t0;
        private double _mu;
        private V3 _w;
        private double _vfm;

        private readonly List<Phase> _phases = new List<Phase>();

        public V3 Rf;
        public V3 Vf;
        public double IgnitionUT;
        public V3 IgnitionAttitude;
        public double FinalThrustAccel;
        public double LandingUT;
        public double Dv;

        public HoverslamSimulation()
        {
            _eventFunc = AltitudeResidual;
            _rhs = Rhs;
        }

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
            public double Tf;
            public V3 UBurn;
            public double Af;
        }

        private readonly Tsit5 _solver = new Tsit5
        {
            Rtol = 1e-6,
            Atol = 1e-6,
            Maxiter = 2000,
            ThrowOnMaxIter = false,
            ThrowOnMinStep = false
        };

        // ReSharper disable once NullableWarningSuppressionIsUsed
        private Phase _phase = null!;

        private readonly Action<Vec, double, Vec> _rhs;

        private bool PropagatePhase(ref HoverslamLayout x, double t0, ref double tf, Phase phase)
        {
            _phase = phase;

            using var y0 = Vec.Rent(N);
            using var yf = Vec.Rent(N);

            x.CopyTo(y0);
            if (phase.Coast)
                _solver.Solve(_rhs, y0, yf, t0, tf);
            else
                _solver.Solve(_rhs, y0, yf, t0, tf, events: _events);
            x.CopyFrom(yf);

            tf = _solver.T;
            return _solver.Status == AbstractIVP.IVPStatus.EventTerminated;
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

            using var coastPhase = Phase.NewCoast(1.0, t0, tf, 0, 0);
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

        private readonly Func<double, object?, double> _eventFunc;

        private (double maxT, double minT) GenerateBracketGuess()
        {
            double maxT;
            double minT;

            if (Astro.EccFromStateVectors(_mu, _r0, _v0) < 1.0)
            {
                double tanom = Astro.TrueAnomalyFromStateVectors(_mu, _r0, _v0);
                if (ClampPi(tanom) > 0)
                {
                    // still rising, advance to the apoapsis
                    minT = Astro.TimeToNextApoapsis(_mu, _r0, _v0);
                    (V3 rApr, V3 vApr) = Shepperd.Solve(_mu, minT, _r0, _v0);
                    maxT = minT + Astro.TimeToNextRadius(_mu, rApr, vApr, _height);
                }
                else
                {
                    // falling
                    minT = Astro.TimeToNextApoapsis(_mu, _r0, _v0) - Astro.PeriodFromStateVectors(_mu, _r0, _v0);
                    maxT = Astro.TimeToNextRadius(_mu, _r0, _v0, _height);
                }
            }
            else
            {
                // heuristically, try to capture a range that includes the ignition point.  I'd rather not
                // use the mainBody SOI radius here, but that would certainly do it.
                minT = Astro.TimeToLastRadius(_mu, _r0, _v0, _r0.magnitude * 2.0);
                maxT = Astro.TimeToNextRadius(_mu, _r0, _v0, _height);
            }

            return (maxT, minT);
        }

        private double SafeRootSolve(double minT, double maxT)
        {
            try
            {
                return BrentRoot.Solve(_eventFunc, minT, maxT, null, rtol: 1e-6);
            }
            catch (Exception)
            {
                Reset();
                throw;
            }
        }

        public override void Run(object? o = null)
        {
            (double maxT, double minT) = GenerateBracketGuess();

            double t1 = SafeRootSolve(minT, maxT);

            TrajectoryResult result = PropagateTrajectory(t1);

            IgnitionUT = _t0 + t1;
            IgnitionAttitude = result.UBurn;
            Rf = result.X.R;
            Vf = result.X.V;
            Dv = result.X.DV;
            LandingUT = _t0 + result.Tf;
            FinalThrustAccel = result.Af;
        }

        public static int N => HoverslamLayout.HOVERSLAM_LAYOUT_LEN;

        public void Rhs(Vec yin, double x, Vec dyout)
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
