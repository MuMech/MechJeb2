/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Text;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.HoverslamSimulation
{
    public partial class HoverslamSimulation
    {
        public class HoverslamSimulationBuilder
        {
            private double _height;
            private V3 _r0;
            private V3 _v0;
            private V3 _w;
            private double _t0;
            private double _mu;
            private double _vfm;

            private readonly List<Phase> _phases = new List<Phase>();

            public HoverslamSimulation Build()
            {
                if (_phases.Count == 0)
                    throw new InvalidOperationException("HoverslamSimulationBuilder: AddStage() must be called at least once before Build()");
                if (_mu <= 0)
                    throw new InvalidOperationException("HoverslamSimulationBuilder: Initial() must be called before Build()");
                if (_height <= 0)
                    throw new InvalidOperationException("HoverslamSimulationBuilder: TargetConditions() must be called before Build()");

                var hoverslam = new HoverslamSimulation
                {
                    _height = _height,
                    _r0 = _r0,
                    _v0 = _v0,
                    _t0 = _t0,
                    _mu = _mu,
                    _w = _w,
                    _vfm = _vfm,
                    _phases = new List<Phase>(_phases)
                };

                return hoverslam;
            }

            public HoverslamSimulationBuilder Initial(V3 r0, V3 v0, double t0, double mu, V3 w)
            {
                DebugPrint($"[MechJebLib.HoverslamSimulationBuilder] Initial(new V3({r0}), new V3({v0}), {t0}, {mu}, new V3({w}))");
                _r0 = r0;
                _v0 = v0;
                _t0 = t0;
                _mu = mu;
                _w = w;

                return this;
            }

            public HoverslamSimulationBuilder AddStage(double m0, double mf, double thrust, double isp, int kspStage, int mjPhase)
            {
                var sb = new StringBuilder();
                sb.Append($"[MechJebLib.HoverslamSimulationBuilder] AddStage({m0}, {mf}, {thrust}, {isp}, {kspStage}, {mjPhase}");
                sb.Append(")");
                DebugPrint(sb.ToString());

                _phases.Add(Phase.NewStage(m0, mf, thrust, isp, kspStage, mjPhase));

                return this;
            }

            public HoverslamSimulationBuilder AddCoast(double m0, double ct, int kspStage, int mjPhase)
            {
                var sb = new StringBuilder();
                sb.Append($"[MechJebLib.HoverslamSimulationBuilder] AddCoast({m0}, {ct}, {kspStage}, {mjPhase}");
                sb.Append(")");
                DebugPrint(sb.ToString());

                _phases.Add(Phase.NewCoast(m0, ct, ct, kspStage, mjPhase));

                return this;
            }

            public HoverslamSimulationBuilder TargetConditions(double height, double descentSpeed)
            {
                DebugPrint($"[MechJebLib.HoverslamSimulationBuilder] TargetConditions({height}, {descentSpeed})");

                _height = height;
                _vfm = -descentSpeed; // a positive final descent speed is a negative final vertical speed

                return this;
            }
        }
    }
}
