#nullable enable

using System.Collections.Generic;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.PVG.Integrators;

namespace MechJebLib.PVG
{
    /// <summary>
    ///     TODO:
    ///     - proper cold bootstrap of the Pv/Pr guess based on launch azimuth + 45 degrees (DONE)
    ///     - infinite ISP upper stage bootstrap (THIS here is what Reset Guidance should do) (DONE)
    ///     - handle LAN and FPA5 (DONE)
    ///     - handle attachaltflag (DONE)
    ///     - handle coasting
    ///     - handle re-guessing if FPA4 got the northgoing/southgoing sense wrong (and/or pin the LAN first then remove the LAN constraint)
    ///     - bootstrap with periapsis attachment first then do kepler4/kepler5 for free attachment (DONE)
    /// </summary>
    public partial class Ascent
    {
        private readonly List<Phase> _phases = new List<Phase>();
        private          V3          _r0;
        private          V3          _v0;
        private          double      _t0;
        private          double      _mu;
        private          double      _peR;
        private          double      _apR;
        private          double      _attR;
        private          double      _vT;
        private          double      _gammaT;
        private          double      _incT;
        private          double      _lanT;
        private          double      _smaT;
        private          double      _eccT;
        private          bool        _attachAltFlag;
        private          bool        _lanflag;
        private          double      _coastLen;
        private          Solution?   _solution;
        private          Phase       _lastPhase => _phases[_phases.Count - 1];

        public Optimizer Run()
        {
            foreach (Phase phase in _phases)
            {
                phase.Integrator = new VacuumThrustAnalytic();
            }

            using Optimizer.OptimizerBuilder builder = Optimizer.Builder()
                .Initial(_r0, _v0, _t0, _mu)
                .Phases(_phases);

            _lastPhase.OptimizeTime = true;

            return _solution == null ? InitialBootstrapping(builder) : ConvergedOptimization(builder);
        }

        private Optimizer ConvergedOptimization(Optimizer.OptimizerBuilder builder)
        {
            (_smaT, _eccT) = Functions.SmaEccFromApsides(_peR, _apR);

            if (_attachAltFlag || _eccT < 1e-4)
            {
                if (!_attachAltFlag)
                    _attR = _peR;

                ApplyFPA(builder);
            }
            else
            {
                ApplyKepler(builder);
            }

            Optimizer pvg = builder.Build();
            pvg.Bootstrap(_solution!);
            pvg.Run();

            return pvg;
        }

        private void ApplyFPA(Optimizer.OptimizerBuilder builder)
        {
            (_vT, _gammaT) = Functions.ConvertApsidesTargetToFPA(_peR, _apR, _attR, _mu);
            if (_lanflag)
            {
                builder.TerminalFPA5(_peR, _vT, _gammaT, _incT, _lanT);
            }
            else
            {
                builder.TerminalFPA4(_peR, _vT, _gammaT, _incT);
            }
        }

        private void ApplyKepler(Optimizer.OptimizerBuilder builder)
        {
            (_smaT, _eccT) = Functions.SmaEccFromApsides(_peR, _apR);

            if (_lanflag)
            {
                builder.TerminalKepler4(_smaT, _eccT, _incT, _lanT);
            }
            else
            {
                builder.TerminalKepler3(_smaT, _eccT, _incT);
            }
        }

        private Optimizer InitialBootstrapping(Optimizer.OptimizerBuilder builder)
        {
            // if we're not doing fixed attachment we still bootstrap with periapasis attachment first
            if (!_attachAltFlag)
                _attR = _peR;

            ApplyFPA(builder);

            Optimizer pvg = builder.Build();

            // guess the initial launch direction
            V3 enu = Functions.ENUHeadingForInclination(_incT, _r0);
            enu.z = 1.0; // add 45 degrees up
            V3 pvGuess = Functions.ENUToECI(_r0, enu).normalized;

            // converge with infinite ISP
            _lastPhase.infinite = true;
            pvg.Bootstrap(pvGuess, _r0.normalized);

            // FIXME: add the coast

            // reconverge
            _lastPhase.infinite = false;
            using Solution solution = pvg.GetSolution();
            pvg.Bootstrap(solution);

            // redo it with Kepler
            if (!_attachAltFlag)
            {
                using Solution solution2 = pvg.GetSolution();
                pvg.Dispose();

                ApplyKepler(builder);

                pvg = builder.Build();
                pvg.Bootstrap(solution2);
            }

            return pvg;
        }

        public static AscentBuilder Builder()
        {
            return new AscentBuilder(new Ascent());
        }
    }
}
