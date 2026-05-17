/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.HoverslamSimulation
{
    public partial class HoverslamSimulation
    {
        public class HoverslamSimulationManager
        {
            private double _height;
            private V3 _r0;
            private V3 _v0;
            private V3 _w;
            private double _t0;
            private double _mu;
            private double _vfm;
            private readonly bool _debug;

            private readonly List<Phase> _phases = new List<Phase>();

            public HoverslamSimulationManager(bool debug = true)
            {
                _debug = debug;
            }

            // reset the manager object
            public HoverslamSimulationManager Reset()
            {
                _height = 0;
                _r0 = V3.zero;
                _v0 = V3.zero;
                _t0 = 0;
                _mu = 0;
                _w = V3.zero;
                _vfm = 0;

                foreach (Phase phase in _phases)
                    phase.Dispose();

                _phases.Clear();

                return this;
            }

            public void Reconfigure(HoverslamSimulation simulation)
            {
                if (_phases.Count == 0)
                    throw new InvalidOperationException("HoverslamSimulationBuilder: AddStage() must be called at least once before Build()");
                if (_mu <= 0)
                    throw new InvalidOperationException("HoverslamSimulationBuilder: Initial() must be called before Build()");
                if (_height <= 0)
                    throw new InvalidOperationException("HoverslamSimulationBuilder: TargetConditions() must be called before Build()");

                simulation._height = _height;
                simulation._r0 = _r0;
                simulation._v0 = _v0;
                simulation._t0 = _t0;
                simulation._mu = _mu;
                simulation._w = _w;
                simulation._vfm = _vfm;

                // empty out the destination list first
                foreach (Phase phase in simulation._phases)
                    phase.Dispose();
                simulation._phases.Clear();

                // then move all the phases into the destination list
                foreach (Phase phase in _phases)
                    simulation._phases.Add(phase);
                _phases.Clear();
            }

            public HoverslamSimulationManager Initial(V3 r0, V3 v0, double t0, double mu, V3 w)
            {
                if (_debug)
                    DebugPrint($"[MechJebLib.HoverslamSimulationBuilder] Initial(new V3({r0}), new V3({v0}), {t0}, {mu}, new V3({w}))");

                _r0 = r0;
                _v0 = v0;
                _t0 = t0;
                _mu = mu;
                _w = w;

                return this;
            }

            public HoverslamSimulationManager AddStage(double m0, double mf, double thrust, double isp, int kspStage, int mjPhase)
            {
                if (_debug)
                    DebugPrint($"[MechJebLib.HoverslamSimulationBuilder] AddStage({m0}, {mf}, {thrust}, {isp}, {kspStage}, {mjPhase})");

                _phases.Add(Phase.NewStage(m0, mf, thrust, isp, kspStage, mjPhase));

                return this;
            }

            public HoverslamSimulationManager AddCoast(double m0, double ct, int kspStage, int mjPhase)
            {
                if (_debug)
                    DebugPrint($"[MechJebLib.HoverslamSimulationBuilder] AddCoast({m0}, {ct}, {kspStage}, {mjPhase})");

                _phases.Add(Phase.NewCoast(m0, ct, ct, kspStage, mjPhase));

                return this;
            }

            public HoverslamSimulationManager TargetConditions(double height, double descentSpeed)
            {
                if (_debug)
                    DebugPrint($"[MechJebLib.HoverslamSimulationBuilder] TargetConditions({height}, {descentSpeed})");

                _height = height;
                _vfm = -descentSpeed; // a positive final descent speed is a negative final vertical speed

                return this;
            }
        }
    }
}
