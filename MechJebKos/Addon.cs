/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using JetBrains.Annotations;
using kOS;
using kOS.AddOns;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // The kOS addon entry point, reached from scripts as ADDONS:MECHJEB.
    //
    // kOS instantiates one Addon per processor (per SharedObjects), so this is inherently
    // local to a single vessel: everything resolves through shared.Vessel and we never reach
    // for FlightGlobals.activeVessel or a global singleton. Cross-vessel coordination is left
    // to kOS itself.
    [kOSAddon("MechJeb")]
    [KOSNomenclature("MechJebAddon")]
    [UsedImplicitly]
    public class Addon : kOS.Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base(shared) => RegisterInitializer(InitializeSuffixes);

        // never cached, it can be updated dyanmically
        private MechJebCore? _core => shared.Vessel.GetMasterMechJeb();

        public override BooleanValue Available() => !(_core is null);

        private void InitializeSuffixes()
        {
            var nodeExecutor = new NodeExecutorBinding(() => _core);
            var stagingController = new StagingControllerBinding(() => _core);
            var thrustController = new ThrustControllerBinding(() => _core);
            var antennaController = new AntennaControllerBinding(() => _core);
            var solarPanelController = new SolarPanelControllerBinding(() => _core);
            var hoverslamAutopilot = new HoverslamAutopilotBinding(() => _core);
            var hoverslamSimulation = new HoverslamSimulationBinding(() => _core);

            AddSuffix("RUNNING", new NoArgsSuffix<BooleanValue>(() =>  _core?.running ?? false,
                "True if MechJeb is present and running on this vessel."));
            AddSuffix("NODEEXECUTOR", new NoArgsSuffix<NodeExecutorBinding>(() => nodeExecutor,
                "The maneuver node executor."));
            AddSuffix("STAGINGCONTROLLER", new NoArgsSuffix<StagingControllerBinding>(() => stagingController,
                "The autostaging controller."));
            AddSuffix("THRUSTCONTROLLER", new NoArgsSuffix<ThrustControllerBinding>(() => thrustController,
                "The thrust controller (throttle limiters)."));
            AddSuffix("ANTENNACONTROLLER", new NoArgsSuffix<AntennaControllerBinding>(() => antennaController,
                "The deployable antenna controller."));
            AddSuffix("SOLARPANELCONTROLLER", new NoArgsSuffix<SolarPanelControllerBinding>(() => solarPanelController,
                "The solar panel controller."));
            AddSuffix("HOVERSLAMAUTOPILOT", new NoArgsSuffix<HoverslamAutopilotBinding>(() => hoverslamAutopilot,
                "The hoverslam (suicide burn) landing autopilot."));
            AddSuffix("HOVERSLAMSIMULATION", new NoArgsSuffix<HoverslamSimulationBinding>(() => hoverslamSimulation,
                "The hoverslam landing predictor/simulation."));
        }
    }
}
