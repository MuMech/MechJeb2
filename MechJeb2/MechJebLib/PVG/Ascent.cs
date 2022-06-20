using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using JetBrains.Annotations;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.PVG.Integrators;

#nullable enable

namespace MechJebLib.PVG
{
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
        private          double      _inclination;
        private          double      _lan;
        private          bool        _attachAltFlag;
        private          bool        _lanflag;
        private          double      _coastLen;
        private          Solution?   _solution;

        public Optimizer Run()
        {
            _phases[_phases.Count - 1].OptimizeTime = true;

            foreach (Phase phase in _phases)
            {
                phase.Integrator = new VacuumThrustAnalytic();
            }

            (double vT, double gammaT) = Functions.ConvertApsidesTargetToFPA(_peR, _apR, _peR, _mu);

            Optimizer pvg = Optimizer.Builder()
                .Initial(_r0, _v0, _t0, _mu)
                .Phases(_phases)
                .TerminalFPA4(_peR, vT, gammaT, _inclination, TerminalType.FPA4_REDUCED)
                .Build();
                
            if (_solution == null)
            {
                pvg.Bootstrap(V3.one, V3.zero, 0.4);
            }
            else
            {
                pvg.Bootstrap(_solution);
            }
     
            pvg.Run();

            return pvg;
        }
        
        public static AscentBuilder Builder()
        {
            return new AscentBuilder(new Ascent());
        }
    }
}
