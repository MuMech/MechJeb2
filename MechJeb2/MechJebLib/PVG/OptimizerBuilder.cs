/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PVG.Terminal;

namespace MechJebLib.PVG
{
    public partial class Optimizer
    {
        public class OptimizerBuilder : IDisposable
        {
            private IPVGTerminal? _terminal;
            private List<Phase>   _phases = null!;
            private V3            _r0;
            private V3            _v0;
            private V3            _u0;
            private double        _t0;
            private double        _mu;
            private double        _rbody;
            private double        _hT;
            
            public Optimizer Build(List<Phase> phases)
            {
                _phases = phases;
                
                double m0 = _phases[0].m0;
                var problem = new Problem(_r0, _v0, _u0, m0, _t0, _mu, _rbody);

                var normalizedPhases = new List<Phase>();
                
                foreach (Phase phase in _phases)
                {
                    normalizedPhases.Add(phase.Rescale(problem.Scale));
                }

                if (_terminal == null)
                    throw new Exception("Optimizer.Build() called with no terminal conditions");

                problem.Terminal = _terminal.Rescale(problem.Scale);
                
                var solver = new Optimizer(problem, normalizedPhases);
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

            public OptimizerBuilder TerminalFPA4(double rT, double vT, double gammaT, double incT)
            {
                _terminal = new FlightPathAngle4Reduced(gammaT, rT, vT, incT);

                return this;
            }

            public OptimizerBuilder TerminalFPA5(double rT, double vT, double gammaT, double incT, double lanT)
            {
                _terminal = new FlightPathAngle5Reduced(gammaT, rT, vT, incT, lanT);

                return this;
            }

            public OptimizerBuilder TerminalKepler3(double smaT, double eccT, double incT)
            {
                _terminal = new Kepler3Reduced(smaT, eccT, incT);

                return this;
            }

            public OptimizerBuilder TerminalKepler4(double smaT, double eccT, double incT, double lanT)
            {
                _terminal = new Kepler4Reduced(smaT, eccT, incT, lanT);

                return this;
            }

            public OptimizerBuilder TerminalKepler5(double smaT, double eccT, double incT, double lanT, double argpT)

            {
                _terminal = new Kepler5Reduced(smaT, eccT, incT, lanT, argpT);

                return this;
            }

            public OptimizerBuilder TerminalEnergy3(double rT, double gammaT, double incT)
            {
                _terminal = new FlightPathAngle3Energy(gammaT, rT, incT);

                return this;
            }

            public OptimizerBuilder TerminalEnergy4(double rT, double incT, double lanT)
            {
                _terminal = new FlightPathAngle4Energy(rT, incT, lanT);

                return this;
            }

            public OptimizerBuilder TerminalConditions(double hT)
            {
                _hT = hT;

                return this;
            }

            public void Dispose()
            {
            }
        }
    }
}
