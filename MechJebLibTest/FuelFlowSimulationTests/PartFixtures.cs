/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.FuelFlowSimulation;
using MechJebLib.FuelFlowSimulation.PartModules;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using static MechJebLibTest.FuelFlowSimulationTests.ResourceFixtures;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    /// <summary>
    ///     Stock KSP parts as SimParts, with the values taken from SimVessel dumps captured in KSP.  The values which
    ///     depend on the vessel (persistentIds, staging, thrust directions) are parameters.
    /// </summary>
    public static class PartFixtures
    {
        public static SimPart ProbeStackLarge(SimVessel v, uint persistentId, int inverseStage = -1)
        {
            SimPart p = NewPart(v, "probeStackLarge", persistentId, inverseStage, 0.5f);
            AddElectricCharge(p, 30, 30);
            return p;
        }

        public static SimPart RocketNoseConeV3(SimVessel v, uint persistentId, int inverseStage = -1) =>
            NewPart(v, "rocketNoseCone.v3", persistentId, inverseStage, 0.2f);

        public static SimPart BatteryBankLarge(SimVessel v, uint persistentId, int inverseStage = -1)
        {
            SimPart p = NewPart(v, "batteryBankLarge", persistentId, inverseStage, 0.2f);
            AddElectricCharge(p, 4000, 4000);
            return p;
        }

        public static SimPart AsasModule1_2(SimVessel v, uint persistentId, int inverseStage = -1) =>
            NewPart(v, "asasmodule1-2", persistentId, inverseStage, 0.2f);

        public static SimPart LargeSolarPanel(SimVessel v, uint persistentId, int inverseStage = -1) =>
            NewPart(v, "largeSolarPanel", persistentId, inverseStage, 0.3f);

        public static SimPart Rockomax64BW(SimVessel v, uint persistentId, int inverseStage = -1, double liquidFuel = 2880,
            double oxidizer = 3520)
        {
            SimPart p = NewPart(v, "Rockomax64.BW", persistentId, inverseStage, 4);
            AddLiquidFuel(p, liquidFuel, 2880);
            AddOxidizer(p, oxidizer, 3520);
            return p;
        }

        public static SimPart Decoupler2(SimVessel v, uint persistentId, int inverseStage, SimPart attachedPart) =>
            NewDecoupler(v, "Decoupler.2", persistentId, inverseStage, 0.16f, attachedPart);

        public static SimPart RadialDecoupler2(SimVessel v, uint persistentId, int inverseStage, SimPart attachedPart) =>
            NewDecoupler(v, "radialDecoupler2", persistentId, inverseStage, 0.05f, attachedPart);

        // the Clamp-O-Tron, written by hand from dockingPort2.cfg.  it is staged, but its config disables its staging, which
        // the player can enable.  attachedPart is the part docked to it, on its reference node.
        public static SimPart DockingPort2(SimVessel v, uint persistentId, int inverseStage, SimPart? attachedPart, bool staged = true,
            bool stagingEnabled = false)
        {
            SimPart p = NewPart(v, "dockingPort2", persistentId, inverseStage, 0.05f);

            SimModuleDockingNode d = AddModule(SimModuleDockingNode.Borrow(p));
            d.Index = 0;
            d.Staged = staged;
            d.StagingEnabled = stagingEnabled;
            d.AttachedPart = attachedPart;
            return p;
        }

        // the AE-FF2 fairing base, written by hand from fairingSize2.cfg.  its ModuleProceduralFairing weighs the panels,
        // panelMass, until it is staged and jettisons them, and nothing after: the base stays on the vessel.
        public static SimPart FairingSize2(SimVessel v, uint persistentId, int inverseStage, double panelMass)
        {
            SimPart p = NewPart(v, "fairingSize2", persistentId, inverseStage, 0.175f);
            p.ModulesUnstagedMass = panelMass;
            p.ModulesStagedMass = 0;
            return p;
        }

        // the fuel line only affects the crossfeed sets, which KSP computes and the fixtures copy from the captures
        public static SimPart FuelLine(SimVessel v, uint persistentId, int inverseStage = -1) =>
            NewPart(v, "fuelLine", persistentId, inverseStage, 0.05f);

        public static SimPart StrutConnector(SimVessel v, uint persistentId, int inverseStage = -1) =>
            NewPart(v, "strutConnector", persistentId, inverseStage, 0.05f);

        public static SimPart LaunchClamp1(SimVessel v, uint persistentId, int inverseStage, int moduleIndex = -1)
        {
            SimPart p = NewPart(v, "launchClamp1", persistentId, inverseStage, 0.1f);
            p.IsLaunchClamp = true;
            v.HasLaunchClamp = true;
            AddModule(SimLaunchClamp.Borrow(p)).Index = moduleIndex;
            return p;
        }

        public static SimPart LiquidEngineMainsailV2(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection)
        {
            SimPart p = NewPart(v, "liquidEngineMainsail.v2", persistentId, inverseStage, 6);
            p.IsEngine = true;

            SimModuleEngines e = NewModuleEngines(p);
            e.IsEnabled = true;
            e.Index = 1;
            // KSP derives this from maxThrust and the vacuum isp in single precision, the dump prints it to 7 digits as
            // 0.4934111 but the 0.4934111f literal is one ulp off, which shows up in the propellant the asparagus staging strands
            e.MaxFuelFlow = 1500f / (310f * 9.80665f);
            e.MaxThrust = 1500;
            e.Propellants.Add(new SimPropellant(LIQUID_FUEL, false, 0.9f, SimFlowMode.STACK_PRIORITY_SEARCH, LIQUID_FUEL_DENSITY));
            e.Propellants.Add(new SimPropellant(OXIDIZER, false, 1.1f, SimFlowMode.STACK_PRIORITY_SEARCH, OXIDIZER_DENSITY));
            e.ThrustTransformMultipliers.Add(1);
            e.ThrustDirectionVectors.Add(thrustDirection);
            AppendConstant(e.AtmosphereCurve, 0, 0, 310f);
            AppendCubicHermite(e.AtmosphereCurve, 0, 1, 0, 1, 310f, -25f, 285f, -30.3124370574951f);
            AppendCubicHermite(e.AtmosphereCurve, 1, 9, 1, 8, 285f, -30.3124370574951f, 0.001f, -35.6248741149902f);
            AppendConstant(e.AtmosphereCurve, 9, 9, 0.001f);
            return p;
        }

        public static SimPart ShockConeIntake(SimVessel v, uint persistentId, int inverseStage = -1)
        {
            SimPart p = NewPart(v, "shockConeIntake", persistentId, inverseStage, 0.12f);
            AddIntakeAir(p, 2, 2);
            return p;
        }

        // the J-X4 Whiplash, written by hand from jetEngineTurbo.cfg since no capture has a jet
        public static SimPart TurboFanEngine(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection)
        {
            SimPart p = NewPart(v, "turboFanEngine", persistentId, inverseStage, 1.8f);
            p.IsEngine = true;

            SimModuleEngines e = NewModuleEngines(p);
            e.IsEnabled = true;
            e.Index = 1;
            e.MaxFuelFlow = 130f / (4000f * 9.80665f);
            e.MaxThrust = 130;
            e.AtmChangeFlow = true;
            e.UseAtmCurve = true;
            e.UseVelCurve = true;
            e.FlowMultCap = 2;
            e.Propellants.Add(new SimPropellant(INTAKE_AIR, true, 8, SimFlowMode.ALL_VESSEL, INTAKE_AIR_DENSITY));
            e.Propellants.Add(new SimPropellant(LIQUID_FUEL, false, 1, SimFlowMode.STAGE_STACK_FLOW_BALANCE, LIQUID_FUEL_DENSITY));
            e.ThrustTransformMultipliers.Add(1);
            e.ThrustDirectionVectors.Add(thrustDirection);
            AppendConstant(e.AtmosphereCurve, 0, 0, 4000f);
            AppendConstant(e.ATMCurve, 0, 0, 0);
            AppendCubicHermite(e.ATMCurve, 0, 0.045, 0, 0.045, 0, 0, 0.166f, 4.304647f);
            AppendCubicHermite(e.ATMCurve, 0.045, 0.16, 0.045, 0.115, 0.166f, 4.304647f, 0.5f, 0.5779132f);
            AppendCubicHermite(e.ATMCurve, 0.16, 0.5, 0.16, 0.34, 0.5f, 0.5779132f, 0.6f, 0.4809403f);
            AppendCubicHermite(e.ATMCurve, 0.5, 1, 0.5, 0.5, 0.6f, 0.4809403f, 1, 1.013946f);
            AppendConstant(e.ATMCurve, 1, 1, 1);
            // the tests stand still, so only the velCurve's first key matters
            AppendConstant(e.VelCurve, 0, 0, 1);
            return p;
        }

        // the CR-7 R.A.P.I.E.R., written by hand from rapierEngine.cfg.  its MultiModeEngine (module 0) switches between the
        // AirBreathing (module 1) and ClosedCycle (module 2) ModuleEnginesFX by enabling one and disabling the other, and it
        // starts out in AirBreathing.  the tests stand still, so only the velCurve's first key matters.
        public static SimPart Rapier(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection)
        {
            SimPart p = NewPart(v, "RAPIER", persistentId, inverseStage, 2);
            p.IsEngine = true;

            SimModuleEngines air = NewModuleEngines(p);
            air.IsEnabled = true;
            air.Index = 1;
            air.MaxFuelFlow = 105f / (3200f * 9.80665f);
            air.MaxThrust = 105;
            air.AtmChangeFlow = true;
            air.UseAtmCurve = true;
            air.UseVelCurve = true;
            air.FlowMultCap = 3;
            air.Propellants.Add(new SimPropellant(INTAKE_AIR, true, 6, SimFlowMode.ALL_VESSEL, INTAKE_AIR_DENSITY));
            air.Propellants.Add(new SimPropellant(LIQUID_FUEL, false, 1, SimFlowMode.STAGE_STACK_FLOW_BALANCE, LIQUID_FUEL_DENSITY));
            air.ThrustTransformMultipliers.Add(1);
            air.ThrustDirectionVectors.Add(thrustDirection);
            AppendConstant(air.AtmosphereCurve, 0, 0, 3200f);
            AppendConstant(air.ATMCurve, 0, 0, 0);
            AppendCubicHermite(air.ATMCurve, 0, 0.018, 0, 0.018, 0, 0, 0.09f, 7.914787f);
            AppendCubicHermite(air.ATMCurve, 0.018, 0.08, 0.018, 0.062, 0.09f, 7.914787f, 0.3f, 1.051923f);
            AppendCubicHermite(air.ATMCurve, 0.08, 0.35, 0.08, 0.27, 0.3f, 1.051923f, 0.5f, 0.3927226f);
            AppendCubicHermite(air.ATMCurve, 0.35, 1, 0.35, 0.65, 0.5f, 0.3927226f, 1, 1.055097f);
            AppendConstant(air.ATMCurve, 1, 1, 1);
            AppendConstant(air.VelCurve, 0, 0, 1);

            SimModuleEngines closedCycle = NewModuleEngines(p);
            closedCycle.IsEnabled = false;
            closedCycle.Index = 2;
            closedCycle.MaxFuelFlow = 180f / (305f * 9.80665f);
            closedCycle.MaxThrust = 180;
            closedCycle.Propellants.Add(new SimPropellant(LIQUID_FUEL, false, 0.9f, SimFlowMode.STAGE_STACK_FLOW_BALANCE,
                LIQUID_FUEL_DENSITY));
            closedCycle.Propellants.Add(new SimPropellant(OXIDIZER, false, 1.1f, SimFlowMode.STAGE_STACK_FLOW_BALANCE, OXIDIZER_DENSITY));
            closedCycle.ThrustTransformMultipliers.Add(1);
            closedCycle.ThrustDirectionVectors.Add(thrustDirection);
            // the keys have no tangents, so Unity smooths them as it does the Mainsail's: the slope to the next key at the ends,
            // the mean of the slopes on either side in between
            AppendConstant(closedCycle.AtmosphereCurve, 0, 0, 305f);
            AppendCubicHermite(closedCycle.AtmosphereCurve, 0, 1, 0, 1, 305f, -30f, 275f, -32.1874375f);
            AppendCubicHermite(closedCycle.AtmosphereCurve, 1, 9, 1, 8, 275f, -32.1874375f, 0.001f, -34.374875f);
            AppendConstant(closedCycle.AtmosphereCurve, 9, 9, 0.001f);
            return p;
        }

        // the IX-6315 Dawn, written by hand from ionEngine.cfg.  xenon false leaves it on ElectricCharge alone, and
        // xenonIgnoreForIsp marks its XenonGas ignoreForIsp, so that no propellant with mass counts for the isp: no stock
        // engine is either, but mods have such engines
        public static SimPart IonEngine(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection, bool xenon = true,
            bool xenonIgnoreForIsp = false)
        {
            SimPart p = NewPart(v, "ionEngine", persistentId, inverseStage, 0.25f);
            p.IsEngine = true;

            SimModuleEngines e = NewModuleEngines(p);
            e.IsEnabled = true;
            e.Index = 0;
            e.MaxFuelFlow = 2f / (4200f * 9.80665f);
            e.MaxThrust = 2;
            e.Propellants.Add(new SimPropellant(ELECTRIC_CHARGE, false, 1.8f, SimFlowMode.STAGE_PRIORITY_FLOW, ELECTRIC_CHARGE_DENSITY));
            if (xenon)
                e.Propellants.Add(new SimPropellant(XENON_GAS, xenonIgnoreForIsp, 0.1f, SimFlowMode.STAGE_PRIORITY_FLOW, XENON_GAS_DENSITY));
            e.ThrustTransformMultipliers.Add(1);
            e.ThrustDirectionVectors.Add(thrustDirection);
            AppendConstant(e.AtmosphereCurve, 0, 0, 4200f);
            return p;
        }

        // a xenon tank, with round made-up figures: 0.04 t of xenon
        public static SimPart XenonTank(SimVessel v, uint persistentId, int inverseStage = -1)
        {
            SimPart p = NewPart(v, "xenonTank", persistentId, inverseStage, 0.05f);
            AddXenonGas(p, 400, 400);
            return p;
        }

        // an RCS thruster on ElectricCharge alone, with the flow of the cold gas thruster in RP1PartFixtures.  KSP gives such a
        // thruster a maxFuelFlow of 0 (its mixture has no density), this one is what a vessel built some other way could have.
        public static SimPart ElectricRcs(SimVessel v, uint persistentId, int inverseStage = -1)
        {
            SimPart p = NewPart(v, "electricRcs", persistentId, inverseStage, 0.05f);

            SimModuleRCS r = AddModule(SimModuleRCS.Borrow(p));
            r.IsEnabled = true;
            r.Index = 0;
            r.RcsEnabled = false;
            r.G = 9.80665;
            r.ISPMult = 1;
            r.ThrustPercentage = 100;
            r.MaxFuelFlow = 0.010197162129779282;
            r.Isp = 100;
            r.Thrust = 10;
            r.MassFlowRate = 0.010197162129779282;
            AppendConstant(r.AtmosphereCurve, 0, 0, 100);
            r.Propellants.Add(new SimPropellant(ELECTRIC_CHARGE, false, 1, SimFlowMode.STAGE_PRIORITY_FLOW, ELECTRIC_CHARGE_DENSITY));
            return p;
        }

        // the O-10 Puff, written by hand from omsEngine.cfg: a monopropellant engine, which draws on the same tanks as RCS.  the
        // tests run in vacuum, so only the atmosphereCurve's first key matters.  residuals stands in for the predicted residuals
        // RealFuels gives its engines, which a stock engine doesn't have.
        public static SimPart OmsEngine(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection, double residuals = 0)
        {
            SimPart p = NewPart(v, "omsEngine", persistentId, inverseStage, 0.09f);
            p.IsEngine = true;
            p.EngineResiduals = residuals;

            SimModuleEngines e = NewModuleEngines(p);
            e.IsEnabled = true;
            e.Index = 0;
            e.MaxFuelFlow = 20f / (250f * 9.80665f);
            e.MaxThrust = 20;
            e.ModuleResiduals = residuals;
            e.Propellants.Add(new SimPropellant(MONOPROPELLANT, false, 0.9f, SimFlowMode.STAGE_PRIORITY_FLOW, MONOPROPELLANT_DENSITY));
            e.ThrustTransformMultipliers.Add(1);
            e.ThrustDirectionVectors.Add(thrustDirection);
            AppendConstant(e.AtmosphereCurve, 0, 0, 250f);
            return p;
        }

        // the RV-105 RCS thruster block, written by hand from rcsBlockRV-105.cfg.  KSP's ModuleRCS gives it a maxFuelFlow of
        // thrusterPower / (isp * G), and it is switched on, as it is in the editor.  only the atmosphereCurve's first key matters.
        public static SimPart RCSBlockV2(SimVessel v, uint persistentId, int inverseStage = -1)
        {
            SimPart p = NewPart(v, "RCSBlock.v2", persistentId, inverseStage, 0.04f);

            SimModuleRCS r = AddModule(SimModuleRCS.Borrow(p));
            r.IsEnabled = true;
            r.StagingEnabled = false;
            r.Index = 1;
            r.RcsEnabled = true;
            r.G = 9.80665;
            r.ISPMult = 1;
            r.ThrustPercentage = 100;
            r.MaxFuelFlow = 1 / (240 * 9.80665);
            r.Isp = 240;
            r.Thrust = 1;
            r.MassFlowRate = 1 / (240 * 9.80665);
            AppendConstant(r.AtmosphereCurve, 0, 0, 240);
            r.Propellants.Add(new SimPropellant(MONOPROPELLANT, false, 1, SimFlowMode.STAGE_PRIORITY_FLOW, MONOPROPELLANT_DENSITY));
            return p;
        }

        // the FL-R750 RCS fuel tank, 3 t of monopropellant
        public static SimPart RCSTank1_2(SimVessel v, uint persistentId, int inverseStage = -1)
        {
            SimPart p = NewPart(v, "RCSTank1-2", persistentId, inverseStage, 0.4f);
            AddMonoPropellant(p, 750, 750);
            return p;
        }

        // the Sepratron I, written by hand from solidBoosterSep.cfg: a solid motor, so its throttle is locked at its thrust
        // limiter.  only the atmosphereCurve's first key matters.
        public static SimPart SepMotor1(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection, float throttleLimiter = 100)
        {
            SimPart p = NewPart(v, "sepMotor1", persistentId, inverseStage, 0.0125f);
            p.IsEngine = true;
            p.IsThrottleLocked = true;
            AddSolidFuel(p, 8, 8);

            SimModuleEngines e = NewModuleEngines(p);
            e.IsEnabled = true;
            e.Index = 0;
            e.MaxFuelFlow = 18f / (154f * 9.80665f);
            e.MaxThrust = 18;
            e.ThrottleLocked = true;
            e.ThrottleLimiter = throttleLimiter;
            e.Propellants.Add(new SimPropellant(SOLID_FUEL, false, 1, SimFlowMode.NO_FLOW, SOLID_FUEL_DENSITY));
            e.ThrustTransformMultipliers.Add(1);
            e.ThrustDirectionVectors.Add(thrustDirection);
            AppendConstant(e.AtmosphereCurve, 0, 0, 154f);
            return p;
        }

        /// <summary>
        ///     Borrows a SimPart and sets every field that the KSP SimVesselBuilder and SimVesselUpdater would set, using
        ///     the declared defaults (which are the fields the SimVessel dump omits).  This matters since pooled SimParts are
        ///     not reset when they are released.
        /// </summary>
        public static SimPart NewPart(SimVessel v, string name, uint persistentId, int inverseStage, double dryMass)
        {
            var p = SimPart.Borrow(v, name);
            p.PersistentId = persistentId;
            p.InverseStage = inverseStage;
            p.DecoupledInStage = -1;
            p.StagingOn = true;
            p.ActivatesEvenIfDisconnected = true;
            p.IsThrottleLocked = false;
            p.IsEnabled = false;
            p.IsRoot = false;
            p.IsLaunchClamp = false;
            p.IsEngine = false;
            p.ResourceRequestRemainingThreshold = 1e-12;
            // every captured part has this priority, it is KSP's default of ten times the part's inverseStage
            p.ResourcePriority = 10 * inverseStage;
            p.Mass = dryMass; // the KSP Part.mass that the builder copies is the dry mass
            p.DryMass = dryMass;
            p.CrewMass = 0;
            p.ModulesStagedMass = 0;
            p.ModulesUnstagedMass = 0;
            p.DisabledResourcesMass = 0;
            p.EngineResiduals = 0;
            v.Parts.Add(p);
            return p;
        }

        /// <summary>
        ///     A staged, unfired decoupler.  The attachedPart is the part on the decoupler's explosive node, which separates
        ///     from the decoupler when it fires.
        /// </summary>
        public static SimPart NewDecoupler(SimVessel v, string name, uint persistentId, int inverseStage, double dryMass,
            SimPart attachedPart, int moduleIndex = -1)
        {
            SimPart p = NewPart(v, name, persistentId, inverseStage, dryMass);

            SimModuleDecouple d = AddModule(SimModuleDecouple.Borrow(p));
            d.Index = moduleIndex;
            d.IsDecoupled = false;
            d.IsOmniDecoupler = false;
            d.Staged = true;
            d.AttachedPart = attachedPart;
            return p;
        }

        /// <summary>
        ///     Resets the SimPartModule fields to their declared defaults and adds the module to its part.
        /// </summary>
        public static T AddModule<T>(T m) where T : SimPartModule
        {
            m.IsEnabled = false;
            m.ModuleIsEnabled = true;
            m.StagingEnabled = true;
            m.Part.Modules.Add(m);
            return m;
        }

        /// <summary>
        ///     Borrows a SimModuleEngines with every field set to what a stock engine with no optional curves has, including
        ///     the fields the engine computes, since pooled modules are not reset when they are released.  The caller sets the
        ///     engine specific fields, propellants, thrust transforms and atmosphereCurve.
        /// </summary>
        public static SimModuleEngines NewModuleEngines(SimPart p)
        {
            SimModuleEngines e = AddModule(SimModuleEngines.Borrow(p));
            e.IsOperational = false;
            e.IsUnrestartableDeadEngine = false;
            e.NoPropellants = false;
            e.MaxFuelFlow = 0;
            e.MinFuelFlow = 0;
            e.MaxThrust = 0;
            e.MinThrust = 0;
            e.G = 9.80665f;
            e.ThrottleLocked = false;
            e.ThrottleLimiter = 100;
            e.MultIsp = 1;
            e.MultFlow = 1;
            e.FlowMultiplier = 1;
            e.Clamp = 1e-5f;
            e.FlowMultCap = float.MaxValue;
            e.FlowMultCapSharpness = 2;
            e.AtmChangeFlow = false;
            e.UseAtmCurve = false;
            e.UseAtmCurveIsp = false;
            e.UseThrottleIspCurve = false;
            e.UseThrustCurve = false;
            e.UseVelCurve = false;
            e.UseVelCurveIsp = false;
            e.ModuleResiduals = 0;
            e.ModuleSpoolupTime = 0;
            e.AutoCutoff = false;
            e.IsModuleEnginesRf = false;
            e.Ullage = false;
            e.ThrustCurrent = V3.zero;
            e.ThrustMax = V3.zero;
            e.ThrustMin = V3.zero;
            e.MassFlowRate = 0;
            e.ISP = 0;
            return e;
        }

        // these rebuild a DoubleInterpolant from its dump, taking the arguments in the order they are printed
        public static void AppendConstant(DoubleInterpolant curve, double leftT, double rightT, double y) =>
            curve.Append(ConstantDoubleNode.Rent(y), leftT, rightT);

        public static void AppendCubicPolynomial(DoubleInterpolant curve, double leftT, double rightT, double t, double invH, double c0,
            double c1, double c2, double c3) =>
            curve.Append(CubicHermiteDoubleNode.RentPolynomial(t, invH, c0, c1, c2, c3), leftT, rightT);

        // this builds a cubic node from FloatCurve keyframe data, for hand-written fixtures
        public static void AppendCubicHermite(DoubleInterpolant curve, double leftT, double rightT, double t, double h, double y, double dy,
            double ynew, double dynew) =>
            curve.Append(CubicHermiteDoubleNode.Rent(t, h, y, dy, ynew, dynew), leftT, rightT);
    }
}
