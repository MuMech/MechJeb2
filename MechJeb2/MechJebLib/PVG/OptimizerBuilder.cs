/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.PVG.Terminal;

namespace MechJebLib.PVG
{
    public enum TerminalType
    {
        FPA4_REDUCED, FPA4_PROPELLANT, FPA5_REDUCED,
        KEPLER3_REDUCED, KEPLER4_REDUCED, KEPLER5_REDUCED,
        FPA3_ENERGY, FPA4_ENERGY,
    }

    public partial class Optimizer
    {
        public class OptimizerBuilder : IDisposable
        {
            private readonly List<Phase>  _phases = new List<Phase>();
            private          V3           _r0;
            private          V3           _v0;
            private          V3           _u0;
            private          double       _t0;
            private          double       _mu;
            private          double       _rbody;
            private          TerminalType _terminalType;
            private          double       _gammaT;
            private          double       _rT;
            private          double       _vT;
            private          double       _incT;
            private          double       _lanT;
            private          double       _smaT;
            private          double       _eccT;
            private          double       _argpT;
            private          double       _hT;

            public Optimizer Build()
            {
                double m0 = _phases[0].m0;
                var problem = new Problem(_r0, _v0, _u0, m0, _t0, _mu, _rbody);
                foreach (Phase phase in _phases)
                {
                    phase.Rescale(problem.Scale);
                }

                problem.Terminal = BuildTerminal(problem.Scale);

                problem.TerminalConditions = new TerminalConditions(_hT, true);

                var solver = new Optimizer(problem, _phases);
                return solver;
            }

            public OptimizerBuilder Initial(V3 r0, V3 v0, V3 u0, double t0, double mu, double rbody)
            {
                _r0    = r0;
                _v0    = v0;
                _u0    = u0.normalized;
                _t0    = t0;
                _mu    = mu;
                _rbody = rbody;
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
                    case TerminalType.FPA5_REDUCED:
                        return new FlightPathAngle5Reduced(_gammaT, _rT / scale.lengthScale, _vT / scale.velocityScale, _incT, _lanT);
                    case TerminalType.KEPLER3_REDUCED:
                        return new Kepler3Reduced(_smaT/scale.lengthScale, _eccT, _incT);
                    case TerminalType.KEPLER4_REDUCED:
                        return new Kepler4Reduced(_smaT/scale.lengthScale, _eccT, _incT, _lanT);
                    case TerminalType.KEPLER5_REDUCED:
                        return new Kepler5Reduced(_smaT/scale.lengthScale, _eccT, _incT, _lanT, _argpT);
                    case TerminalType.FPA3_ENERGY:
                        return new FlightPathAngle3Energy(0, _rT / scale.lengthScale, _incT);
                    case TerminalType.FPA4_ENERGY:
                        return new FlightPathAngle4Energy(_rT / scale.lengthScale, _incT, _lanT);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public OptimizerBuilder TerminalFPA4(double rT, double vT, double gammaT, double incT,
                TerminalType terminalType = TerminalType.FPA4_REDUCED)
            {
                _rT           = rT;
                _vT           = vT;
                _gammaT       = gammaT;
                _incT         = incT;
                _terminalType = terminalType;

                return this;
            }

            public OptimizerBuilder TerminalFPA5(double rT, double vT, double gammaT, double incT, double lanT,
                TerminalType terminalType = TerminalType.FPA5_REDUCED)
            {
                _rT           = rT;
                _vT           = vT;
                _gammaT       = gammaT;
                _incT         = incT;
                _lanT         = lanT;
                _terminalType = terminalType;

                return this;
            }

            public OptimizerBuilder TerminalKepler3(double smaT, double eccT, double incT, TerminalType terminalType = TerminalType.KEPLER3_REDUCED)
            {
                _smaT         = smaT;
                _eccT         = eccT;
                _incT         = incT;
                _terminalType = terminalType;

                return this;
            }

            public OptimizerBuilder TerminalKepler4(double smaT, double eccT, double incT, double lanT,
                TerminalType terminalType = TerminalType.KEPLER4_REDUCED)
            {
                _smaT         = smaT;
                _eccT         = eccT;
                _incT         = incT;
                _lanT         = lanT;
                _terminalType = terminalType;

                return this;
            }

            public OptimizerBuilder TerminalKepler5(double smaT, double eccT, double incT, double lanT, double argpT,
                TerminalType terminalType = TerminalType.KEPLER5_REDUCED)
            {
                _smaT         = smaT;
                _eccT         = eccT;
                _incT         = incT;
                _lanT         = lanT;
                _argpT        = argpT;
                _terminalType = terminalType;

                return this;
            }

            public OptimizerBuilder TerminalEnergy3(double rT, double incT, TerminalType terminalType = TerminalType.FPA3_ENERGY)
            {
                _gammaT       = 0;
                _rT           = rT;
                _incT         = incT;
                _terminalType = terminalType;

                return this;
            }

            public OptimizerBuilder TerminalEnergy4(double rT, double incT, double lanT, TerminalType terminalType = TerminalType.FPA4_ENERGY)
            {
                _gammaT       = 0;
                _rT           = rT;
                _incT         = incT;
                _lanT         = lanT;
                _terminalType = terminalType;

                return this;
            }

            public OptimizerBuilder TerminalConditions(double hT)
            {
                _hT = hT;
                
                return this;
            }

            public OptimizerBuilder Phases(IList<Phase> phases)
            {
                _phases.Clear();
                for (int i = 0; i < phases.Count; i++)
                {
                    _phases.Add(phases[i]);
                }

                return this;
            }

            public void Dispose()
            {
            }
        }
    }
}
