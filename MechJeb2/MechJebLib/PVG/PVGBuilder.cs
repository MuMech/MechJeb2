#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PVG.Terminal;

namespace MechJebLib.PVG
{
    public enum TerminalType
    {
        FPA4_REDUCED, FPA4_PROPELLANT
    }

    public partial class Optimizer
    {

        public class PVGBuilder
        {
            private readonly List<Phase>  _phases = new List<Phase>();
            private          V3           _r0;
            private          V3           _v0;
            private          double       _t0;
            private          double       _mu;
            private          TerminalType _terminalType;
            private          double       _gammaT;
            private          double       _incT;
            private          double       _rT;
            private          double       _vT;

            public Optimizer Build()
            {
                double m0 = _phases[0].m0;
                var problem = new Problem(_r0, _v0, m0, _t0, _mu);
                foreach (Phase phase in _phases)
                {
                    phase.Rescale(problem.Scale);
                }

                problem.Terminal = BuildTerminal(problem.Scale);

                var solver = new Optimizer(problem, _phases);
                return solver;
            }
            
            public PVGBuilder Initial(V3 r0, V3 v0, double t0, double mu)
            {
                _r0  = r0;
                _v0  = v0;
                _t0  = t0;
                _mu  = mu;
                return this;
            }

            private IPVGTerminal BuildTerminal(Scale scale)
            {
                switch (_terminalType)
                {
                    case TerminalType.FPA4_REDUCED:
                        return new FlightPathAngle4Reduced(_gammaT, _rT / scale.lengthScale, _vT / scale.velocityScale, _incT);
                    case TerminalType.FPA4_PROPELLANT:
                        return new FlightPathAngle4Propellant(_gammaT, _rT / scale.lengthScale, _vT / scale.velocityScale, _incT);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public PVGBuilder TerminalFPA4(double rT, double vT, double gammaT, double incT, TerminalType terminalType)
            {
                _rT           = rT;
                _vT           = vT;
                _gammaT       = gammaT;
                _incT         = incT;
                _terminalType = terminalType;

                return this;
            }

            public PVGBuilder Phases(IList<Phase> phases)
            {
                _phases.Clear();
                for(int i = 0; i < phases.Count; i++)
                {
                    _phases.Add(phases[i]);
                }

                return this;
            }
        }
    }
}
