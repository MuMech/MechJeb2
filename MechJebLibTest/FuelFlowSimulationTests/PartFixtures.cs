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
