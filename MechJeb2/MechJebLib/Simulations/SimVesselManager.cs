/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.Simulations
{
    // FIXME: the SimVesselManager needs to be broken out of MechJebLib eventually to isolate the parts that
    // need to link against KSP GameObjects (MechJebLibBindings.dll or something like that)
    public partial class SimVesselManager
    {
        public List<FuelStats> Segments => FuelFlowSimulation.Segments;

        private readonly SimVesselBuilder   _builder;
        private readonly SimVesselUpdater   _updater;
        private          SimVessel          _vessel;
        private          IShipconstruct     _kspVessel;
        public readonly  FuelFlowSimulation FuelFlowSimulation = new FuelFlowSimulation();
        public           bool               DVLinearThrust     = true; // include cos losses

        private readonly Dictionary<Part, SimPart>             _partMapping              = new Dictionary<Part, SimPart>();
        private readonly Dictionary<SimPart, Part>             _inversePartMapping       = new Dictionary<SimPart, Part>();
        private readonly Dictionary<SimPartModule, PartModule> _inversePartModuleMapping = new Dictionary<SimPartModule, PartModule>();

        public double T => _vessel.T;
        public V3     R => _vessel.R;
        public V3     V => _vessel.V;
        public V3     U => _vessel.U;

        public SimVesselManager()
        {
            _builder   = new SimVesselBuilder(this);
            _updater   = new SimVesselUpdater(this);
            _vessel    = SimVessel.Borrow();
            _kspVessel = null!;
        }

        public void Build(IShipconstruct vessel)
        {
            Clear();
            _builder.BuildVessel(vessel);
            _builder.BuildParts();
            Update();
            _builder.UpdateLinks();
            _builder.UpdateCrossFeedSet();
            DecouplingAnalyzer.Analyze(_vessel);
            _builder.UpdateEngineSet();
        }

        public void Update() => _updater.Update();

        public void SetConditions(double atmDensity, double atmPressure, double machNumber) =>
            _vessel.SetConditions(atmDensity, atmPressure, machNumber);

        public void SetInitial(double t, V3 r, V3 v, V3 u) => _vessel.SetInitial(t, r, v, u);

        public void StartFuelFlowSimulationJob()
        {
            FuelFlowSimulation.DVLinearThrust = DVLinearThrust;
            FuelFlowSimulation.StartJob(_vessel);
        }

        private void Clear()
        {
            _partMapping.Clear();
            _inversePartMapping.Clear();
            _inversePartModuleMapping.Clear();
        }

        public void Release()
        {
            Clear();
            _vessel.Dispose();
        }
    }
}
