/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.PVG
{
    public partial class Ascent
    {
        public class AscentBuilder
        {
            public readonly List<Phase> _phases = new List<Phase>();
            
            public V3        _r0                  { get; private set; }
            public V3        _v0                  { get; private set; }
            public V3        _u0                  { get; private set; }
            public double    _t0                  { get; private set; }
            public double    _mu                  { get; private set; }
            public double    _rbody               { get; private set; }
            public double    _apR                 { get; private set; }
            public double    _peR                 { get; private set; }
            public double    _attR                { get; private set; }
            public double    _incT                { get; private set; }
            public double    _lanT                { get; private set; }
            public double    _fpaT                { get; private set; }
            public double    _hT                  { get; private set; }
            public bool      _attachAltFlag       { get; private set; }
            public bool      _lanflag             { get; private set; }
            public bool      _fixedBurnTime       { get; private set; }
            public int       _optimizedPhase      { get; private set; }
            public int       _optimizedCoastPhase { get; private set; } = -1;
            public Solution? _solution            { get; private set; }
            
            
            public AscentBuilder AddStageUsingFinalMass(double m0, double mf, double isp, double bt, int kspStage, bool optimizeTime = false, bool unguided = false)
            {
                Log($"[MechJebLib.AscentBuilder] AddStageUsingFinalMass({m0}, {mf}, {isp}, {bt}, {kspStage}, {optimizeTime}, {unguided})");
                
                _phases.Add(Phase.NewStageUsingFinalMass(m0, mf, isp, bt, kspStage, optimizeTime, unguided));
                if (optimizeTime)
                    _optimizedPhase = _phases.Count - 1;
                
                return this;
            }
            
            public AscentBuilder AddStageUsingThrust(double m0, double thrust, double isp, double bt, int kspStage, bool optimizeTime = false, bool unguided = false)
            {
                Log($"[MechJebLib.AscentBuilder] AddStageUsingThrust({m0}, {thrust}, {isp}, {bt}, {kspStage}, {optimizeTime}, {unguided})");
                
                _phases.Add(Phase.NewStageUsingThrust(m0, thrust, isp, bt, kspStage, optimizeTime, unguided));
                if (optimizeTime)
                    _optimizedPhase = _phases.Count - 1;
                
                return this;
            }
            
            public AscentBuilder AddFixedCoast(double m0, double ct, int kspStage)
            {
                Log($"[MechJebLib.AscentBuilder] AddFixedCoast({m0}, {ct}, {kspStage})");
                
                _phases.Add(Phase.NewFixedCoast(m0, ct, kspStage));
                
                return this;
            }
            
            public AscentBuilder AddOptimizedCoast(double m0, double mint, double maxt, int kspStage)
            {
                Log($"[MechJebLib.AscentBuilder] AddOptimizedCoast({m0}, {mint}, {maxt}, {kspStage})");
                
                _phases.Add(Phase.NewOptimizedCoast(m0, mint, maxt, kspStage));
                _optimizedCoastPhase = _phases.Count - 1;
                
                return this;
            }
            
            public AscentBuilder Initial(V3 r0, V3 v0, V3 u0, double t0, double mu, double rbody)
            {
                Check.NonZeroFinite(r0);
                Check.NonZeroFinite(v0);
                Check.Finite(t0);
                Check.PositiveFinite(mu);
                Check.PositiveFinite(rbody);
                
                Log($"[MechJebLib.AscentBuilder] Initial({r0}, {v0}, {u0}, {t0}, {mu}, {rbody})");
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
                var ascent = new Ascent(this);

                return ascent;
            }

            // This magical interface needs to be busted up
            public AscentBuilder SetTarget(double peR, double apR, double attR, double inclination, double lan, double fpa, bool attachAltFlag, bool lanflag)
            {
                Log($"[MechJebLib.AscentBuilder] SetTarget({peR} {apR} {attR} {inclination} {lan} {fpa} {attachAltFlag} {lanflag})");
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
                _solution = solution;

                return this;
            }

            public AscentBuilder FixedBurnTime(bool fixedBurnTime)
            {
                _fixedBurnTime = fixedBurnTime;

                return this;
            }

            public AscentBuilder TerminalConditions(double hT)
            {
                _hT = hT;

                return this;
            }
        }
    }
}
