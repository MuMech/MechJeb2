/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;

namespace MechJebLib.PSG
{
    public partial class Optimizer
    {
        public class OptimizerBuilder : IDisposable
        {
            private ITerminal?          _terminal;
            private List<Phase>         _phases = null!;
            private Problem.ProblemCost _cost;
            private V3                  _r0;
            private V3                  _v0;
            private V3                  _u0;
            private double              _t0;
            private double              _mu;
            private double              _rbody;
            private double              _hT;

            public Optimizer Build(List<Phase> phases)
            {
                _phases = phases;

                double m0 = _phases[0].m0;

                if (_terminal == null)
                    throw new Exception("Optimizer.Build() called with no terminal conditions");

                // XXX: this is both a ProblemBuilder and an OptimizerBuilder
                var problem = new Problem(_r0, _v0, _u0, m0, _t0, _mu, _rbody, _terminal, _cost);

                var normalizedPhases = new List<Phase>();

                foreach (Phase phase in _phases)
                    normalizedPhases.Add(phase.Rescale(problem.Scale));

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
                _terminal = new FlightPathAngle4(gammaT, rT, vT, incT);
                _cost     = Problem.ProblemCost.MIN_THRUST_ACCEL;

                return this;
            }

            public OptimizerBuilder TerminalFPA5(double rT, double vT, double gammaT, double incT, double lanT)
            {
                _terminal = new FlightPathAngle5(gammaT, rT, vT, incT, lanT);
                _cost     = Problem.ProblemCost.MIN_THRUST_ACCEL;

                return this;
            }

            public OptimizerBuilder TerminalKepler3(double smaT, double eccT, double incT)
            {
                _terminal = new Kepler3(smaT, eccT, incT);
                _cost     = Problem.ProblemCost.MIN_THRUST_ACCEL;

                return this;
            }

            public OptimizerBuilder TerminalKepler4(double smaT, double eccT, double incT, double lanT)
            {
                _terminal = new Kepler4(smaT, eccT, incT, lanT);
                _cost     = Problem.ProblemCost.MIN_THRUST_ACCEL;

                return this;
            }

            public OptimizerBuilder TerminalKepler5(double smaT, double eccT, double incT, double lanT, double argpT)

            {
                _terminal = new Kepler5(smaT, eccT, incT, lanT, argpT);
                _cost     = Problem.ProblemCost.MIN_THRUST_ACCEL;

                return this;
            }

            public OptimizerBuilder TerminalEnergy3(double rT, double gammaT, double incT)
            {
                _terminal = new FlightPathAngle3Energy(gammaT, rT, incT);
                _cost     = Problem.ProblemCost.MAX_ENERGY;

                return this;
            }

            public OptimizerBuilder TerminalEnergy4(double rT, double incT, double lanT)
            {
                _terminal = new FlightPathAngle4Energy(rT, incT, lanT);
                _cost     = Problem.ProblemCost.MAX_ENERGY;

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
