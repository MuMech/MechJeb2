#nullable enable

using System.Collections.Generic;

namespace MechJebLib.Simulations
{
    public partial class SimVesselManager
    {
        public List<FuelStats> Segments => _fuelFlowSimulation.Segments;

        private readonly SimVesselBuilder   _builder;
        private readonly SimVesselUpdater   _updater;
        private          SimVessel          _vessel;
        private          IShipconstruct     _kspVessel;
        private readonly FuelFlowSimulation _fuelFlowSimulation = new FuelFlowSimulation();
        public           bool               DVLinearThrust      = true; // include cos losses

        private readonly Dictionary<Part, SimPart>             _partMapping              = new Dictionary<Part, SimPart>();
        private readonly Dictionary<SimPart, Part>             _inversePartMapping       = new Dictionary<SimPart, Part>();
        private readonly Dictionary<SimPartModule, PartModule> _inversePartModuleMapping = new Dictionary<SimPartModule, PartModule>();

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

        public void Update()
        {
            _updater.Update();
        }

        public void SetConditions(double atmDensity, double atmPressure, double machNumber)
        {
            _vessel.SetConditions(atmDensity, atmPressure, machNumber);
        }

        public void RunFuelFlowSimulation()
        {
            _fuelFlowSimulation.DVLinearThrust = DVLinearThrust;
            _fuelFlowSimulation.Run(_vessel);
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
