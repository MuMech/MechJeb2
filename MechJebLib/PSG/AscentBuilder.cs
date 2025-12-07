/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

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

            private V3        _r0            { get; set; }
            private V3        _v0            { get; set; }
            private V3        _u0            { get; set; }
            private double    _t0            { get; set; }
            private double    _mu            { get; set; }
            private double    _rbody         { get; set; }
            private double    _apR           { get; set; }
            private double    _peR           { get; set; }
            private double    _attR          { get; set; }
            private double    _incT          { get; set; }
            private double    _lanT          { get; set; }
            private double    _fpaT          { get; set; }
            private bool      _attachAltFlag { get; set; }
            private bool      _lanflag       { get; set; }
            private bool      _fixedBurnTime { get; set; }
            private Solution? _solution      { get; set; }

            public AscentBuilder AddStageUsingFinalMass(double m0, double mf, double isp, double bt, int kspStage,
                int mjPhase, bool unguided = false, bool allowShutdown = true)
            {
                DebugPrint(
                    $"[MechJebLib.AscentBuilder] AddStageUsingFinalMass({m0}, {mf}, {isp}, {bt}, {kspStage}, {mjPhase}, {(unguided ? "true" : "false")}, {(allowShutdown ? "true" : "false")})");

                _phases.Add(Phase.NewStageUsingFinalMass(m0, mf, isp, bt, kspStage, mjPhase, unguided, allowShutdown));

                return this;
            }

            public AscentBuilder AddStageUsingThrust(double m0, double thrust, double isp, double bt, int kspStage,
                int mjPhase, bool unguided = false, bool allowShutdown = true)
            {
                DebugPrint(
                    $"[MechJebLib.AscentBuilder] AddStageUsingThrust({m0}, {thrust}, {isp}, {bt}, {kspStage}, {mjPhase}, {(unguided ? "true" : "false")}, {(allowShutdown ? "true" : "false")})");

                _phases.Add(Phase.NewStageUsingThrust(m0, thrust, isp, bt, kspStage, mjPhase, unguided, allowShutdown));

                return this;
            }

            public AscentBuilder AddCoast(double m0, double mint, double maxt, int kspStage, int mjPhase, bool unguided = false)
            {
                DebugPrint($"[MechJebLib.AscentBuilder] AddOptimizedCoast({m0}, {mint}, {maxt}, {kspStage}, {mjPhase}, {(unguided ? "true" : "false")})");

                _phases.Add(Phase.NewCoast(m0, mint, maxt, kspStage, mjPhase, unguided));

                return this;
            }

            public AscentBuilder Initial(V3 r0, V3 v0, V3 u0, double t0, double mu, double rbody)
            {
                Check.NonZeroFinite(r0);
                Check.NonZeroFinite(v0);
                Check.Finite(t0);
                Check.PositiveFinite(mu);
                Check.PositiveFinite(rbody);

                DebugPrint($"[MechJebLib.AscentBuilder] Initial({r0}, {v0}, {u0}, {t0}, {mu}, {rbody})");
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
                double    m0 = _phases[0].m0;
                ITerminal terminal;

                _fixedBurnTime = true;
                foreach (Phase? p in _phases)
                    if (p.AllowShutdown && !p.Coast)
                        _fixedBurnTime = false;

                if (_fixedBurnTime)
                {
                    if (_lanflag)
                        terminal = new FlightPathAngle4Energy(_attR, _incT, _lanT);
                    else
                        terminal = new FlightPathAngle3Energy(_fpaT, _attR, _incT);
                }
                else
                {
                    (double smaT, double eccT) = Astro.SmaEccFromApsides(_peR, _apR);
                    (double vT, double gammaT) = Astro.FPATargetFromApsides(_peR, _apR, _attR, _mu);
                    if (_attachAltFlag || eccT < 1e-4)
                    {
                        if (_lanflag)
                            terminal = new FlightPathAngle5(gammaT, _attR, vT, _incT, _lanT);
                        else
                            terminal = new FlightPathAngle4(gammaT, _attR, vT, _incT);
                    }
                    else
                    {
                        if (_lanflag)
                            terminal = new Kepler4(smaT, eccT, _incT, _lanT);
                        else
                            terminal = new Kepler3(smaT, eccT, _incT);
                    }
                }

                var problem = new Problem(_r0, _v0, _u0, m0, _t0, _mu, _rbody, terminal);

                var normalizedPhases = new PhaseCollection();

                foreach (Phase phase in _phases)
                    normalizedPhases.Add(phase.Rescale(problem.Scale));

                var ascent = new Ascent(problem, normalizedPhases, _solution, _fixedBurnTime);

                return ascent;
            }

            public AscentBuilder SetTarget(double peR, double apR, double attR, double inclination, double lan,
                double fpa, bool attachAltFlag, bool lanflag)
            {
                DebugPrint(
                    $"[MechJebLib.AscentBuilder] SetTarget({peR}, {apR}, {attR}, {inclination}, {lan}, {fpa}, {(attachAltFlag ? "true" : "false")}, {(lanflag ? "true" : "false")})");
                _peR           = peR;
                _apR           = apR;
                _attR          = attR;
                _incT          = inclination;
                _lanT          = lan;
                _fpaT          = fpa;
                _attachAltFlag = attachAltFlag;
                _lanflag       = lanflag;

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
