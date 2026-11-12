/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Functions;
using MechJebLib.Interpolants;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     The initial guess for the optimizer, by single shooting.  The stages are flown in order at full throttle with a
    ///     single cutoff, which may land in any stage.  On the pad the rocket rises vertically, and in an atmosphere it then
    ///     kicks over into a gravity turn, until it switches to linear tangent steering u = normalize(A - B (t - ts)) once it
    ///     is out of the atmosphere.  A small SQP (with finite difference gradients) over the kick, its azimuth, A, B and the
    ///     cutoff time burns the least propellant into a circular orbit at the target's GuessRadius() and inclination.
    ///     Coasts are zero length, and unguided stages, shutdown flags and throttling are ignored: all of that is left to
    ///     the optimizer, this only has to produce a roughly feasible path.
    /// </summary>
    public class AscentGuesser
    {
        private enum Mode { NONE, VERTICAL, PITCHOVER, GRAVITY_TURN, LINEAR_TANGENT }

        private const double V_KICK        = 50;   // vertical speed (m/s) which ends the vertical rise
        private const double T_PITCHOVER   = 5;    // time (s) to ramp the kick in
        private const double T_DECAY       = 5;    // time constant (s) of the decay of the kick into the gravity turn
        private const double KICK0         = 3;    // starting kick (degrees)
        private const double MAX_KICK      = 45;   // degrees
        private const double RHO_SWITCH    = 1e-3; // density relative to the surface where the gravity turn ends
        private const double IMPACT_MARGIN = 10;   // distance (m) below the launch site or the surface which is an impact

        private const double DIFFSTEP             = 1e-6;
        private const double TRUST_REGION_LIMIT   = 1e-9;
        private const int    MAXITS               = 100;
        private const double CONSTRAINT_TOLERANCE = 1e-8;
        private const double OPTIMALITY_TOLERANCE = 1e-4;

        // alglib's SQP rejects a step when the sum of the squares of the constraints (or the Jacobian, which this makes
        // through the finite differences) is not finite, so this makes it back off from a trajectory which hits the ground.
        private const double INFEASIBLE = 1e300;

        // retries of the initial pass which raise the linear tangent steering, and shrink the kick, to stay off the ground
        private const int INITIAL_RETRIES = 4;

        private readonly Problem _problem;
        private readonly FlightPlan? _flightPlan;

        // the integration tolerances are tight to keep the step size selection noise well under the finite differences
        private readonly Tsit5 _solver = new Tsit5 { Rtol = 1e-10, Atol = 1e-10, MaxEvals = 100000 };
        private readonly FsqpKktStop _kktStop = new FsqpKktStop();

        private readonly bool _atmosphere;
        private readonly double _invDensityR0;
        private readonly double _rFloor;
        private readonly double _rSwitch;
        private readonly double _vKick;
        private readonly double _tPitchover;
        private readonly double _tDecay;

        private readonly Event _impactEvent;
        private readonly Event _kickEvent;
        private readonly Event _switchEvent;
        private readonly Event _energyEvent;
        private readonly List<Event> _events = new List<Event>();

        // the problem being guessed
        // ReSharper disable once NullableWarningSuppressionIsUsed
        private PhaseCollection _phases = null!;
        private bool _padStart;
        private bool _kickVariables;
        private double _rT;
        private double _incT;
        private double _tStackEnd;

        // the steering parameters of the shot, which are the variables of the NLP
        private double _kick;
        private double _psi;
        private V3 _a;
        private V3 _b;
        private double _tCut;

        // tilts the linear tangent steering picked by the initial pass towards the vertical
        private double _pitchUp;

        // the state of the shot
        // ReSharper disable once NullableWarningSuppressionIsUsed
        private Phase _phase = null!;
        private Mode _mode;
        private double _tk;
        private V3 _rk;
        private V3 _ek;
        private double _t2;
        private V3 _delta;
        private double _ts;
        private V3 _rs;
        private InterpolantLayout _yf;

        // The state at the switch to linear tangent steering in the last shot to reach it.  Most of the finite differences
        // only vary A, B and the cutoff, which leave the trajectory up to the switch alone, so a shot with the same kick and
        // azimuth which cuts off after the switch starts from here.
        private bool _switchCached;
        private double _cachedKick;
        private double _cachedPsi;
        private int _cachedPhase;
        private double _cachedT;
        private InterpolantLayout _cachedY;

        public AscentGuesser(Problem problem, FlightPlan? flightPlan)
        {
            _problem = problem;
            _flightPlan = flightPlan;

            // the same atmosphere as AscentProblem
            _atmosphere = problem.H0 > 0 && problem.Rho0CdAref > 0;
            _invDensityR0 = problem.H0 > 0 ? 1.0 / AscentProblem.NormalizedDensity(problem.R0.magnitude, problem.RBody, problem.H0).M : 0;

            Scale scale = problem.Scale;
            _rFloor = Min(problem.R0.magnitude, problem.RBody) - IMPACT_MARGIN / scale.LengthScale;
            _rSwitch = problem.RBody + problem.H0 * Log(1.0 / RHO_SWITCH);
            _vKick = V_KICK / scale.VelocityScale;
            _tPitchover = T_PITCHOVER / scale.TimeScale;
            _tDecay = T_DECAY / scale.TimeScale;

            _impactEvent = new Event(Impact);
            _kickEvent = new Event(Kick);
            _switchEvent = new Event(Switch);
            _energyEvent = new Event(OrbitalEnergy);
        }

        private double Impact(Vec yin, double t, AbstractIVP i) => InterpolantLayout.CreateFrom(yin).R.magnitude - _rFloor;

        private double Kick(Vec yin, double t, AbstractIVP i)
        {
            var y = InterpolantLayout.CreateFrom(yin);

            return V3.Dot(y.V, y.R.normalized) - _vKick;
        }

        private double Switch(Vec yin, double t, AbstractIVP i) => InterpolantLayout.CreateFrom(yin).R.magnitude - _rSwitch;

        // the orbital energy relative to the circular orbit at rT
        private double OrbitalEnergy(Vec yin, double t, AbstractIVP i)
        {
            var y = InterpolantLayout.CreateFrom(yin);

            return 0.5 * y.V.sqrMagnitude - 1.0 / y.R.magnitude + 0.5 / _rT;
        }

        private V3 SurfaceVelocity(V3 r, V3 v) => v - V3.Cross(_problem.W, r);

        // the steering law, unnormalized, which is also what the U of the state carries
        private V3 Steering(V3 r, V3 v, double t)
        {
            switch (_mode)
            {
                case Mode.VERTICAL:
                    return r;
                case Mode.PITCHOVER:
                    double theta = _kick * (t - _tk) / _tPitchover;
                    return Cos(theta) * _rk + Sin(theta) * _ek;
                case Mode.GRAVITY_TURN:
                    return SurfaceVelocity(r, v).normalized + Exp(-(t - _t2) / _tDecay) * _delta;
                default:
                    return _a - _b * (t - _ts);
            }
        }

        // the time derivative of the steering law along the trajectory, where a is the acceleration
        private V3 SteeringRate(V3 r, V3 v, V3 a, double t)
        {
            switch (_mode)
            {
                case Mode.VERTICAL:
                    return v;
                case Mode.PITCHOVER:
                    double theta = _kick * (t - _tk) / _tPitchover;
                    return _kick / _tPitchover * (-Sin(theta) * _rk + Cos(theta) * _ek);
                case Mode.GRAVITY_TURN:
                    V3 vr = SurfaceVelocity(r, v);
                    double vrm = vr.magnitude;
                    V3 vrhat = vr / vrm;
                    V3 ar = a - V3.Cross(_problem.W, v);
                    return (ar - V3.Dot(vrhat, ar) * vrhat) / vrm - Exp(-(t - _t2) / _tDecay) / _tDecay * _delta;
                default:
                    return -_b;
            }
        }

        // the optimizer's dynamics (AscentProblem.VDotAtmo and VDotVacuum) at full throttle, with the drag written so that
        // it is still finite at zero airspeed on the pad
        private V3 Acceleration(V3 r, V3 v, double m, V3 u)
        {
            double rm = r.magnitude;
            V3 gravity = -r / (rm * rm * rm);

            if (!_atmosphere)
                return gravity + _phase.VacThrust / m * u;

            double density = AscentProblem.NormalizedDensity(rm, _problem.RBody, _problem.H0).M;
            V3 vr = SurfaceVelocity(r, v);
            V3 drag = 0.5 * _problem.Rho0CdAref * density * vr.magnitude * vr;
            double thrust = _phase.Mdot * (_phase.VexCurrent + (_phase.VexVacuum - _phase.VexCurrent) * (1.0 - density * _invDensityR0));
            return gravity + thrust / m * u - drag / m;
        }

        private void Rhs(Vec yin, double t, Vec dyout)
        {
            CheckDebug.True(_phase.Normalized);

            var y = InterpolantLayout.CreateFrom(yin);
            var dy = new InterpolantLayout();

            V3 a = Acceleration(y.R, y.V, y.M, Steering(y.R, y.V, t).normalized);

            dy.R = y.V;
            dy.V = a;
            dy.M = -_phase.Mdot;
            dy.U = SteeringRate(y.R, y.V, a, t);
            dy.T = 0;

            dy.CopyTo(dyout);
        }

        // the old guess: 45 degrees up on the heading for the target inclination
        private V3 FortyFiveDegreesUp(V3 r)
        {
            V3 enu = Astro.ENUHeadingForInclination(_incT, r);
            enu.z = 1.0;
            return Astro.ENUToECI(r, enu).normalized;
        }

        private Mode AfterVertical => _atmosphere ? Mode.PITCHOVER : Mode.LINEAR_TANGENT;

        // the initial pass picks A itself (with B = 0)
        private void EnterMode(Mode mode, ref InterpolantLayout y, double t, bool initialPass)
        {
            Mode previous = _mode;
            V3 u = previous == Mode.NONE ? _problem.U0 : Steering(y.R, y.V, t).normalized;

            _mode = mode;

            switch (mode)
            {
                case Mode.VERTICAL:
                    if (V3.Dot(y.V, y.R.normalized) >= _vKick)
                    {
                        EnterMode(AfterVertical, ref y, t, initialPass);
                        return;
                    }

                    break;
                case Mode.PITCHOVER:
                    _tk = t;
                    _rk = y.R.normalized;
                    _ek = Astro.ENUToECI(y.R, new V3(Sin(_psi), Cos(_psi), 0));
                    break;
                case Mode.GRAVITY_TURN:
                    // the decaying offset from the airspeed direction makes the steering continuous at the end of the kick
                    _t2 = t;
                    _delta = Cos(_kick) * _rk + Sin(_kick) * _ek - SurfaceVelocity(y.R, y.V).normalized;
                    if (y.R.magnitude >= _rSwitch)
                    {
                        EnterMode(Mode.LINEAR_TANGENT, ref y, t, initialPass);
                        return;
                    }

                    break;
                case Mode.LINEAR_TANGENT:
                    _ts = t;
                    _rs = y.R;
                    if (initialPass)
                    {
                        _a = (previous == Mode.VERTICAL ? FortyFiveDegreesUp(y.R) : u) + _pitchUp * y.R.normalized;
                        _a = _a.normalized;
                        _b = V3.zero;
                    }

                    break;
            }

            y.U = Steering(y.R, y.V, t);
        }

        // the terminal event which stopped the last Solve: the earliest of the ones which triggered in its last step
        private Event FiredEvent()
        {
            Event? fired = null;

            foreach (Event e in _events)
                if (!double.IsNaN(e.Time) && (fired == null || e.Time < fired.Time))
                    fired = e;

            return fired ?? throw new InvalidOperationException("AscentGuesser internal error: no event fired");
        }

        // when all the stages would burn out flying them in order
        private double StackEndTime()
        {
            double t = _problem.T0;
            double m = _problem.M0;

            foreach (Phase phase in _phases)
            {
                if (!phase.MassContinuity)
                    m = phase.M0;

                if (phase.Coast)
                    continue;

                double bt = Max(phase.BurnTimeFromMass(m), 0);
                t += bt;
                m -= phase.Mdot * bt;
            }

            return t;
        }

        /// <summary>
        ///     Flies the stack from the initial state, with one segment per phase recorded into the solution if there is one.
        ///     The initial pass also cuts off at the orbital energy of the target orbit.
        /// </summary>
        /// <returns>false if the rocket hits the ground</returns>
        private bool Shoot(Solution? solution, bool initialPass)
        {
            using var y0 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var yf = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

            // the solution needs the whole trajectory, and the initial pass picks A at the switch
            // ReSharper disable CompareOfFloatsByEqualityOperator
            bool resume = solution == null && !initialPass && _switchCached && _kick == _cachedKick && _psi == _cachedPsi &&
                          _cachedT < _tCut;
            // ReSharper restore CompareOfFloatsByEqualityOperator

            InterpolantLayout y;
            double t;
            int p0;

            _ts = double.PositiveInfinity;

            if (resume)
            {
                // part way through a phase, so its mass is the mass at the switch
                y = _cachedY;
                t = _cachedT;
                p0 = _cachedPhase;
                _phase = _phases[p0];
                _mode = Mode.GRAVITY_TURN;
                EnterMode(Mode.LINEAR_TANGENT, ref y, t, false);
            }
            else
            {
                y = new InterpolantLayout { R = _problem.R0, V = _problem.V0, M = _problem.M0 };
                t = _problem.T0;
                p0 = 0;
                _phase = _phases[0];
                _mode = Mode.NONE;
                EnterMode(_padStart ? Mode.VERTICAL : Mode.LINEAR_TANGENT, ref y, t, initialPass);
            }

            for (int p = p0; p < _phases.Count; p++)
            {
                _phase = _phases[p];

                if (!_phase.MassContinuity && !(resume && p == p0))
                    y.M = _phase.M0;

                y.T = _phase.Coast ? 0 : 1;

                double tf = _phase.Coast ? t : Max(Min(t + _phase.BurnTimeFromMass(y.M), _tCut), t);

                VecInterpolant? interpolant = solution != null ? VecInterpolant.Rent() : null;

                if (interpolant != null && tf <= t)
                {
                    // a zero length segment
                    y.CopyTo(y0);
                    _solver.Solve(Rhs, y0, yf, t, t, interpolant);
                }

                // mode switches restart the integration, a phase has a handful of them at most
                for (int segments = 0; t < tf; segments++)
                {
                    if (segments > 8)
                        throw new InvalidOperationException("AscentGuesser internal error: too many segments in a phase");

                    double tEnd = _mode == Mode.PITCHOVER ? Min(tf, _tk + _tPitchover) : tf;

                    _events.Clear();
                    _events.Add(_impactEvent);
                    if (_mode == Mode.VERTICAL)
                        _events.Add(_kickEvent);
                    if (_mode == Mode.GRAVITY_TURN)
                        _events.Add(_switchEvent);
                    if (initialPass)
                        _events.Add(_energyEvent);
                    foreach (Event e in _events)
                        e.Time = double.NaN;

                    y.CopyTo(y0);
                    _solver.Solve(Rhs, y0, yf, t, tEnd, interpolant, _events);
                    y.CopyFrom(yf);

                    if (_solver.Status != AbstractIVP.IVPStatus.EventTerminated)
                    {
                        t = tEnd;
                        if (_mode == Mode.PITCHOVER && t >= _tk + _tPitchover)
                            EnterMode(Mode.GRAVITY_TURN, ref y, t, initialPass);
                        continue;
                    }

                    t = _solver.T;
                    Event fired = FiredEvent();

                    if (fired == _impactEvent)
                    {
                        interpolant?.Dispose();
                        return false;
                    }

                    if (fired == _energyEvent)
                    {
                        // every later burn is zero length
                        _tCut = t;
                        break;
                    }

                    if (fired == _switchEvent && solution == null && !initialPass)
                    {
                        _switchCached = true;
                        _cachedKick = _kick;
                        _cachedPsi = _psi;
                        _cachedPhase = p;
                        _cachedT = t;
                        _cachedY = y;
                    }

                    EnterMode(fired == _kickEvent ? AfterVertical : Mode.LINEAR_TANGENT, ref y, t, initialPass);
                }

                if (interpolant != null)
                    solution!.AddSegment(interpolant, _phase);
            }

            _yf = y;

            return true;
        }

        private int NumVariables => _kickVariables ? 9 : 7;

        private int NumConstraints => 4 + InclinationConstraints.NumConstraints(_incT);

        private double[] PackVariables()
        {
            double[] x = new double[NumVariables];
            int i = 0;

            if (_kickVariables)
            {
                x[i++] = _kick;
                x[i++] = _psi;
            }

            _a.CopyTo(x, i);
            _b.CopyTo(x, i + 3);
            x[i + 6] = _tCut;

            return x;
        }

        private void UnpackVariables(double[] x)
        {
            int i = 0;

            if (_kickVariables)
            {
                _kick = x[i++];
                _psi = x[i++];
            }

            _a = new V3(x[i], x[i + 1], x[i + 2]);
            _b = new V3(x[i + 3], x[i + 4], x[i + 5]);
            _tCut = x[i + 6];
        }

        // minimizing the cutoff time minimizes the propellant burned, since the stack burns in order at full throttle.  the
        // delivered mass itself jumps down at each staging, which would make the SQP hang on to burned out stages.
        private void NLPFunction(double[] x, double[] fi, object? o)
        {
            UnpackVariables(x);

            bool feasible;

            try
            {
                feasible = Shoot(null, false);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                // integration failures (max evals, step size underflow, non-finite values)
                feasible = false;
            }

            if (!feasible)
            {
                for (int i = 0; i < fi.Length; i++)
                    fi[i] = INFEASIBLE;
                return;
            }

            V3 rf = _yf.R;
            V3 vf = _yf.V;
            int ci = 0;

            fi[ci++] = _tCut;
            fi[ci++] = rf.magnitude / _rT - 1;
            fi[ci++] = V3.Dot(rf.normalized, vf.normalized);
            fi[ci++] = _rT * vf.sqrMagnitude - 1;

            // the same inclination rows as InclinationConstraints
            if (InclinationConstraints.IsEquatorial(_incT))
            {
                fi[ci++] = rf.z;
                fi[ci++] = vf.z;
            }
            else
            {
                fi[ci++] = V3.Cross(rf, vf).normalized.z - Cos(Abs(_incT));
            }

            // fixes the scale of A and B, which the steering doesn't depend on
            fi[ci] = _a.sqrMagnitude + _b.sqrMagnitude - 1;
        }

        private bool SafeShoot()
        {
            try
            {
                return Shoot(null, false);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                return false;
            }
        }

        // The initial pass flies straight along A after the switch and cuts off at the orbital energy of the target orbit,
        // retrying with A tilted up and the kick shrunk if it hits the ground.  Then B is set to turn the steering from A to
        // the horizontal of the target plane at the cutoff.
        private void InitialVariables()
        {
            bool feasible = false;

            for (int i = 0; i < INITIAL_RETRIES && !feasible; i++)
            {
                _pitchUp = i == 0 ? 0 : Pow(3, i - 1);
                _kick = Deg2Rad(KICK0) / (1 + i);
                _psi = Astro.HeadingForInclination(_incT, _problem.R0);
                _a = _problem.U0;
                _b = V3.zero;
                _tCut = _tStackEnd;

                feasible = Shoot(null, true);
            }

            if (!feasible)
                throw new Exception("AscentGuesser: the rocket hits the ground even going nearly straight up");

            double tCut0 = _tCut;
            V3 a0 = _a;

            if (tCut0 > _ts)
            {
                // the horizontal of the target plane, rotated downrange by the angle that the initial pass flew
                V3 rshat = _rs.normalized;
                V3 horizontal = Astro.ENUToECI(_rs, Astro.ENUHeadingForInclination(_incT, _rs));
                double range = Atan2(V3.Cross(_rs, _yf.R).magnitude, V3.Dot(_rs, _yf.R));
                V3 uf = Cos(range) * horizontal - Sin(range) * rshat;
                V3 b0 = (a0 - uf) / (tCut0 - _ts);

                double norm = Sqrt(a0.sqrMagnitude + b0.sqrMagnitude);
                _a = a0 / norm;
                _b = b0 / norm;
                _tCut = tCut0;

                if (SafeShoot())
                    return;
            }

            // fall back to the initial pass, which is known to stay off the ground
            _a = a0;
            _b = V3.zero;
            _tCut = tCut0;
        }

        public Solution InitialGuess(PhaseCollection phases, CancellationToken cancellationToken = default)
        {
            _phases = phases;
            _solver.CancellationToken = cancellationToken;
            _switchCached = false;

            _rT = _problem.Terminal.GuessRadius();
            _incT = _problem.Terminal.IncT();
            _padStart = _problem.T0 <= 0;
            _kickVariables = _padStart && _atmosphere;
            _tStackEnd = StackEndTime();

            InitialVariables();

            double[] x0 = PackVariables();
            int n = x0.Length;
            int nc = NumConstraints;

            double[] s = new double[n];
            double[] bndl = new double[n];
            double[] bndu = new double[n];

            for (int i = 0; i < n; i++)
            {
                s[i] = 1.0;
                bndl[i] = double.NegativeInfinity;
                bndu[i] = double.PositiveInfinity;
            }

            if (_kickVariables)
            {
                s[0] = 0.05;
                bndl[0] = 0;
                bndu[0] = Deg2Rad(MAX_KICK);
            }

            bndl[n - 1] = _problem.T0;
            bndu[n - 1] = _tStackEnd;

            // all equality constraints
            double[] nl = new double[nc];
            double[] nu = new double[nc];

            alglib.minnlccreatef(n, x0, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetscale(state, s);
            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetnlc2(state, nl, nu);
            // the 5-point default costs twice as many shots, and didn't take fewer iterations
            alglib.minnlcsetnumdiff(state, 3);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond3(state, 0, TRUST_REGION_LIMIT, MAXITS);

            try
            {
                alglib.minnlcoptimize(state, NLPFunction, _kktStop.Attach(state, CONSTRAINT_TOLERANCE, OPTIMALITY_TOLERANCE), null);
            }
            catch (alglib.alglibexception) when (cancellationToken.IsCancellationRequested)
            {
                // ALGLIB rewraps the OperationCanceledException thrown from our callback
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            catch (alglib.alglibexception e)
            {
                // alglibexception never passes its message to the base constructor, so Message is useless
                throw new Exception(e.msg, e);
            }
            finally
            {
                _kktStop.Detach();
            }

            alglib.minnlcresults(state, out double[] x, out alglib.minnlcreport rep);

            PrintDebug($"AscentGuesser: terminationtype: {rep.terminationtype} iterations: {rep.iterationscount} primal infeasibility: {_kktStop.PrimalInfeasibility}");

            if (rep.terminationtype < 0)
                throw new Exception($"AscentGuesser: SQP solver terminated abnormally: {rep.terminationtype}");

            UnpackVariables(x);

            var solution = new Solution(_problem, _flightPlan);

            if (!Shoot(solution, false))
                throw new Exception("AscentGuesser internal error: the solution hits the ground");

            return solution;
        }
    }
}
