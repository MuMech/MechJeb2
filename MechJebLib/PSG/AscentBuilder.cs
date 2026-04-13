/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using System.Text;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public partial class Ascent
    {
        public class AscentBuilder
        {
            private readonly PhaseCollection _phases = new PhaseCollection();

            private V3        _r0               { get; set; }
            private V3        _v0               { get; set; }
            private V3        _u0               { get; set; }
            private double    _t0               { get; set; }
            private double    _mu               { get; set; }
            private double    _rbody            { get; set; } = 1.0;
            private double    _h0               { get; set; }
            private double    _rho0CdAref       { get; set; }
            private double    _rho0QAlphaMaxInv { get; set; }
            private double    _rho0QMaxInv      { get; set; }
            private V3        _w                { get; set; } = V3.zero;
            private double    _apR              { get; set; }
            private double    _peR              { get; set; }
            private double    _attR             { get; set; }
            private double    _incT             { get; set; }
            private double    _lanT             { get; set; }
            private double    _argpT            { get; set; }
            private double    _fpaT             { get; set; }
            private bool      _attachAltFlag    { get; set; }
            private bool      _lanflag          { get; set; }
            private bool      _argpflag         { get; set; }
            private bool      _fixedBurnTime    { get; set; }
            private Solution? _solution         { get; set; }

            public AscentBuilder AddStage(double m0, double mf, double thrust, double isp, int kspStage,
                int mjPhase, bool unguided = false, bool allowShutdown = true, bool massContinuity = false, double ispCurrent = -1,
                double minThrottle = 1.0)
            {
                var sb = new StringBuilder();
                sb.Append($"[MechJebLib.AscentBuilder] AddStage({m0}, {mf}, {thrust}, {isp}, {kspStage}, {mjPhase}");
                if (unguided)
                    sb.Append(", unguided: true");
                if (allowShutdown)
                    sb.Append(", allowShutdown: true");
                if (massContinuity)
                    sb.Append(", massContinuity: true");
                if (ispCurrent >= 0)
                    sb.Append($", ispCurrent: {ispCurrent}");
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (minThrottle != 1.0)
                    sb.Append($", minThrottle: {minThrottle}");
                sb.Append(")");
                DebugPrint(sb.ToString());

                _phases.Add(Phase.NewStage(m0, mf, thrust, isp, kspStage, mjPhase, unguided, allowShutdown, massContinuity, ispCurrent, minThrottle));

                return this;
            }

            public AscentBuilder AerodynamicConstants(double cd, double aRef, double rho0, double qAlphaMax, double qMax, double h0, V3 w)
            {
                DebugPrint($"AerodynamicConstants({cd},  {aRef}, {rho0}, {qAlphaMax}, {qMax}, {h0}, new V3({w}))");
                _h0               = h0;
                _rho0CdAref       = cd * aRef * rho0;
                _rho0QAlphaMaxInv = qAlphaMax > 0 ? rho0 / qAlphaMax : 0;
                _rho0QMaxInv      = qMax > 0 ? rho0 / qMax : 0;
                _w                = w;
                return this;
            }

            public AscentBuilder AddCoast(double m0, double mint, double maxt, int kspStage, int mjPhase, bool unguided = false, bool massContinuity = false)
            {
                var sb = new StringBuilder();
                sb.Append($"[MechJebLib.AscentBuilder] AddCoast({m0}, {mint}, {maxt}, {kspStage}, {mjPhase}");
                if (unguided)
                    sb.Append(", unguided: true");
                if (massContinuity)
                    sb.Append(", massContinuity: true");
                sb.Append(")");
                DebugPrint(sb.ToString());

                _phases.Add(Phase.NewCoast(m0, mint, maxt, kspStage, mjPhase, unguided, massContinuity));

                return this;
            }

            public AscentBuilder Initial(V3 r0, V3 v0, V3 u0, double t0, double mu, double rbody)
            {
                Check.NonZeroFinite(r0);
                Check.NonZeroFinite(v0);
                Check.Finite(t0);
                Check.PositiveFinite(mu);
                Check.PositiveFinite(rbody);

                DebugPrint($"[MechJebLib.AscentBuilder] Initial(new V3({r0}), new V3({v0}), new V3({u0}), {t0}, {mu}, {rbody})");
                _r0    = r0;
                _v0    = v0;
                _u0    = u0.normalized;
                _t0    = t0;
                _mu    = mu;
                _rbody = rbody;
                return this;
            }

            public Ascent Build()
            {
                double    m0 = _phases[0].M0;
                ITerminal terminal;

                _fixedBurnTime = true;
                foreach (Phase? p in _phases)
                    if (p.AllowShutdown && !p.Coast)
                        _fixedBurnTime = false;

                if (_fixedBurnTime)
                {
                    if (_lanflag)
                        terminal = new FlightPathAngle4Energy(_fpaT, _attR, _incT, _lanT);
                    else
                        terminal = new FlightPathAngle3Energy(_fpaT, _attR, _incT);
                }
                else
                {
                    (double smaT, double eccT) = Astro.SmaEccFromApsides(_peR, _apR);

                    // for nearly circular orbits, force periapsis attachment
                    if (!_attachAltFlag && eccT < 1e-4)
                    {
                        _attachAltFlag = true;
                        _attR          = _peR;
                    }

                    (double vT, double gammaT) = Astro.FPATargetFromApsides(_peR, _apR, _attR, _mu);

                    if (_attachAltFlag)
                    {
                        if (_lanflag)
                            terminal = new FlightPathAngle5(gammaT, _attR, vT, _incT, _lanT);
                        else
                            terminal = new FlightPathAngle4(gammaT, _attR, vT, _incT);
                    }
                    else
                    {
                        if (_argpflag)
                            terminal = new Kepler5(smaT, eccT, _incT, _lanT, _argpT);
                        else if (_lanflag)
                            terminal = new Kepler4(smaT, eccT, _incT, _lanT);
                        else
                            terminal = new Kepler3(smaT, eccT, _incT);
                    }
                }

                var problem = new Problem(_r0, _v0, _u0, m0, _t0, _mu, _rbody, _h0, _rho0CdAref, _rho0QAlphaMaxInv, _rho0QMaxInv, _w, terminal);

                var normalizedPhases = new PhaseCollection();

                normalizedPhases.AddRange(_phases.Select(phase => phase.Rescale(problem.Scale)));

                normalizedPhases.FixLastShutdownStage();

                var ascent = new Ascent(problem, normalizedPhases, _solution, _fixedBurnTime);

                return ascent;
            }


            public AscentBuilder SetTarget(double peR, double apR, double attR, double inclination, double lan, double argp,
                double fpa, bool attachAltFlag, bool lanflag, bool argpflag)
            {
                DebugPrint(
                    $"[MechJebLib.AscentBuilder] SetTarget({peR}, {apR}, {attR}, {inclination}, {lan}, {fpa}, {argp}, {(attachAltFlag ? "true" : "false")}, {(lanflag ? "true" : "false")}, {(argpflag ? "true" : "false")})");
                _peR           = peR;
                _apR           = apR;
                _attR          = attR;
                _incT          = inclination;
                _lanT          = lan;
                _argpT         = argp;
                _fpaT          = fpa;
                _attachAltFlag = attachAltFlag;
                _lanflag       = lanflag;
                _argpflag      = argpflag;

                return this;
            }

            public AscentBuilder OldSolution(Solution solution)
            {
                DebugPrint("[MechJebLib.AscentBuilder] OldSolution(<solution>)");

                _solution = solution;

                return this;
            }
        }
    }
}
