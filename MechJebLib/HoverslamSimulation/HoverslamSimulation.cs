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
            _events = new List<Event> { new Event(BurnResidual) };
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
            MaxEvals = 12000,
            ThrowOnMaxEvals = true,
            ThrowOnMinStep = false
        };

        // ReSharper disable once NullableWarningSuppressionIsUsed
        private Phase _phase = null!;

        private readonly Action<Vec, double, Vec> _rhs;

        private bool PropagatePhase(ref HoverslamLayout x, double t0, ref double tf, Phase phase)
        {
            _phase = phase;

            if (!phase.Coast && VelocityError(x.R, x.V).magnitude <= BurnEndDv(phase, x.M))
            {
                // stop because we're already past where the event would fire
                tf = t0;
                FinishBurn(ref x, ref tf, phase);
                return true;
            }

            using var y0 = Vec.Rent(N);
            using var yf = Vec.Rent(N);

            x.CopyTo(y0);
            if (phase.Coast)
                _solver.Solve(_rhs, y0, yf, t0, tf);
            else
                _solver.Solve(_rhs, y0, yf, t0, tf, events: _events);
            x.CopyFrom(yf);

            tf = _solver.T;

            if (_solver.Status != AbstractIVP.IVPStatus.EventTerminated)
                return false;

            FinishBurn(ref x, ref tf, phase);
            return true;
        }

        // The burn steers against the velocity error, so the steering flips from one step to the next as the error reaches zero and
        // the integrator can only creep up on that point.  The burn's event ends the integration a little earlier, once the error
        // is down to what the thrust removes in BURN_END_TIME: the steps that near the flip are short enough to end inside that
        // window, which they rarely do inside a fixed millimetre per second.  FinishBurn does the rest analytically.  BURN_END_DV
        // is the window's floor.
        private const double BURN_END_TIME = 0.02;
        private const double BURN_END_DV = 1e-3;

        private static double BurnEndDv(Phase phase, double m) => Math.Max(BURN_END_DV, BURN_END_TIME * phase.VacThrust / m);

        private readonly List<Event> _events;

        private double BurnResidual(IList<double> yin, double t, AbstractIVP i)
        {
            var y = HoverslamLayout.CreateFrom(yin);

            return VelocityError(y.R, y.V).magnitude - BurnEndDv(_phase, y.M);
        }

        // Terminal phase analytical landing: the time dt that the thrust, burning mass as it goes, takes to null the velocity error
        // while the error drifts along itself at driftAlong, from ve ln(m / (m - mdot dt)) - driftAlong dt = |dv|.  Newton's
        // method from the constant-acceleration time, where the left side is already at least |dv|, steps down to the root.
        private void FinishBurn(ref HoverslamLayout x, ref double t, Phase phase)
        {
            V3 dv = VelocityError(x.R, x.V);
            double dvMag = dv.magnitude;
            double r = x.R.magnitude;
            V3 rhat = x.R / r;

            // how the error changes without thrust: gravity, less how the target velocity changes as the vessel moves (the
            // ground's rotation, and the final descent's direction turning with the position)
            V3 drift = -_mu / (r * r) * rhat - V3.Cross(_w, x.V) - _vfm / r * (x.V - V3.Dot(x.V, rhat) * rhat);
            double driftAlong = V3.Dot(dv.normalized, drift);
            double rate = phase.VacThrust / x.M - driftAlong; // -d|dv|/dt

            // TWR < 1 while already inside the threshold, leave the residual alone
            if (rate <= 0)
                return;

            // a start that leaves the stage some mass, where the left side is still far above any |dv| inside the window
            double dt = Math.Min(dvMag / rate, 0.999 * x.M / phase.Mdot);

            for (int i = 0; i < 20; i++)
            {
                double m = x.M - phase.Mdot * dt;
                double step = (phase.VexVacuum * Math.Log(x.M / m) - driftAlong * dt - dvMag) / (phase.VacThrust / m - driftAlong);
                dt -= step;
                if (Math.Abs(step) <= 1e-12 * dt)
                    break;
            }

            double mf = x.M - phase.Mdot * dt;

            x.R += (x.V - 0.5 * dv) * dt;
            x.V = V3.Cross(_w, x.R) + x.R.normalized * _vfm;
            x.DV += phase.VexVacuum * Math.Log(x.M / mf);
            x.M = mf;
            t += dt;
        }

        // surface-relative velocity minus the target final velocity, the burn steers retrograde to this
        private V3 VelocityError(V3 r, V3 v) => v - V3.Cross(_w, r) - r.normalized * _vfm;

        private TrajectoryResult PropagateTrajectory(double t)
        {
            var x = new HoverslamLayout { R = _r0, V = _v0, M = _phases[0].M0, DV = 0 };

            double t0 = 0;
            double tf = t;

            using var coastPhase = Phase.NewCoast(1.0, 1.0, t0, tf, 0, 0);
            PropagatePhase(ref x, t0, ref tf, coastPhase);

            V3 uBurn = -VelocityError(x.R, x.V).normalized;
            double af = 0;

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
            _solver.CancellationToken = CancelToken;

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
            //CheckDebug.True(_phase.Normalized);

            var y = HoverslamLayout.CreateFrom(yin);
            var dy = new HoverslamLayout();

            double at = _phase.VacThrust / y.M;

            double r2 = y.R.sqrMagnitude;
            double r = Math.Sqrt(r2);
            double r3 = r2 * r;

            V3 u = -VelocityError(y.R, y.V).normalized;

            dy.R = y.V;
            dy.V = -_mu * y.R / r3 + at * u;
            dy.M = -_phase.Mdot;
            dy.DV = at;

            dy.CopyTo(dyout);
        }
    }
}
