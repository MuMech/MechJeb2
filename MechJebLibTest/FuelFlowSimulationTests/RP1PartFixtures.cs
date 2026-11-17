/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.FuelFlowSimulation.PartModules;
using MechJebLib.Primitives;
using static MechJebLibTest.FuelFlowSimulationTests.PartFixtures;
using static MechJebLibTest.FuelFlowSimulationTests.ResourceFixtures;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    /// <summary>
    ///     RP-1 (RealismOverhaul, RealFuels, ProceduralParts and ProceduralFairings) parts as SimParts, with the values taken
    ///     from SimVessel dumps captured in KSP.  The procedural parts get their mass from the IPartMassModifier modules of
    ///     their configuration, so the module mass and resource amounts are parameters along with the values which depend on
    ///     the vessel.  The modules have the Index of the module in the KSP part's module list.
    /// </summary>
    public static class RP1PartFixtures
    {
        public static SimPart ROTServiceModule(SimVessel v, uint persistentId, double modulesMass, double leadBallast)
        {
            SimPart p = NewProceduralPart(v, "ROT-ServiceModule", persistentId, -1, 1, modulesMass);
            AddResource(p, LEAD_BALLAST, leadBallast, leadBallast, LEAD_BALLAST_DENSITY);
            return p;
        }

        public static SimPart ProceduralAvionics(SimVessel v, uint persistentId, int inverseStage, double modulesMass, double electricCharge,
            double controllableMass) =>
            ProceduralAvionics(v, persistentId, inverseStage, modulesMass, electricCharge, electricCharge, controllableMass);

        // on the pad an avionics part can have drawn some of its ElectricCharge
        public static SimPart ProceduralAvionics(SimVessel v, uint persistentId, int inverseStage, double modulesMass, double electricCharge,
            double maxElectricCharge, double controllableMass)
        {
            SimPart p = NewProceduralPart(v, "proceduralAvionics", persistentId, inverseStage, 1, modulesMass);
            AddElectricCharge(p, electricCharge, maxElectricCharge);
            SimModuleAvionics a = AddModule(SimModuleAvionics.Borrow(p));
            a.Index = 23;
            a.ControllableMass = controllableMass;
            return p;
        }

        public static SimPart ROTGenericTankSeparate(SimVessel v, uint persistentId, int inverseStage, double modulesMass, double rp1,
            double lox)
        {
            SimPart p = NewProceduralPart(v, "ROT-GenericTank-Separate", persistentId, inverseStage, 1, modulesMass);
            AddResource(p, COOLED_RP1, rp1, rp1, COOLED_RP1_DENSITY);
            AddResource(p, COOLED_LQD_OXYGEN, lox, lox, COOLED_LQD_OXYGEN_DENSITY);
            return p;
        }

        // a procedural tank holding the RD-108's propellants, written by hand since no capture has it
        public static SimPart ROTGenericTankSeparateKeroloxHTP(SimVessel v, uint persistentId, int inverseStage, double modulesMass,
            double kerosene, double lox, double htp)
        {
            SimPart p = NewProceduralPart(v, "ROT-GenericTank-Separate", persistentId, inverseStage, 1, modulesMass);
            AddResource(p, KEROSENE, kerosene, kerosene, KEROSENE_DENSITY);
            AddResource(p, LQD_OXYGEN, lox, lox, LQD_OXYGEN_DENSITY);
            AddResource(p, HTP, htp, htp, HTP_DENSITY);
            return p;
        }

        // a procedural tank holding the RS-68's propellants, written by hand since no capture has it
        public static SimPart ROTGenericTankSeparateHydrolox(SimVessel v, uint persistentId, int inverseStage, double modulesMass,
            double lh2, double lox)
        {
            SimPart p = NewProceduralPart(v, "ROT-GenericTank-Separate", persistentId, inverseStage, 1, modulesMass);
            AddResource(p, LQD_HYDROGEN, lh2, lh2, LQD_HYDROGEN_DENSITY);
            AddResource(p, LQD_OXYGEN, lox, lox, LQD_OXYGEN_DENSITY);
            return p;
        }

        // a procedural tank of the Titan II's Aerozine50 and NTO.  the dump lists a part's resources in the order the part
        // added them, which is not the same for each tank of the MyTitanII capture.
        public static SimPart ROTGenericTankSeparateHypergolic(SimVessel v, uint persistentId, int inverseStage, double modulesMass,
            double aerozine50, double nto, bool ntoFirst = false)
        {
            SimPart p = NewProceduralPart(v, "ROT-GenericTank-Separate", persistentId, inverseStage, 1, modulesMass);
            if (ntoFirst)
                AddResource(p, NTO, nto, nto, NTO_DENSITY);
            AddResource(p, AEROZINE50, aerozine50, aerozine50, AEROZINE50_DENSITY);
            if (!ntoFirst)
                AddResource(p, NTO, nto, nto, NTO_DENSITY);
            return p;
        }

        public static SimPart RORFTankIntegral(SimVessel v, uint persistentId, int inverseStage, double modulesMass, double nitrogen) =>
            NewNitrogenTank(v, "RO-RFTank-Integral", persistentId, inverseStage, modulesMass, nitrogen);

        // a radial service module tank, holding the nitrogen of the Titan II upper stage's RCS
        public static SimPart RORFTankSM(SimVessel v, uint persistentId, int inverseStage, double modulesMass, double nitrogen) =>
            NewNitrogenTank(v, "RO-RFTank-SM", persistentId, inverseStage, modulesMass, nitrogen);

        private static SimPart NewNitrogenTank(SimVessel v, string name, uint persistentId, int inverseStage, double modulesMass,
            double nitrogen)
        {
            SimPart p = NewProceduralPart(v, name, persistentId, inverseStage, 1, modulesMass);
            AddResource(p, NITROGEN, nitrogen, nitrogen, NITROGEN_DENSITY);
            return p;
        }

        public static SimPart KzResizableFairingBase(SimVessel v, uint persistentId, int inverseStage, double modulesMass,
            SimPart attachedPart) =>
            WithModulesMass(NewDecoupler(v, "KzResizableFairingBase", persistentId, inverseStage, 0, attachedPart, 2), modulesMass);

        public static SimPart KzFlatAdapter(SimVessel v, uint persistentId, int inverseStage, double modulesMass, SimPart attachedPart) =>
            WithModulesMass(NewDecoupler(v, "KzFlatAdapter", persistentId, inverseStage, 0, attachedPart, 2), modulesMass);

        public static SimPart KzInterstageAdapter2(SimVessel v, uint persistentId, int inverseStage, double modulesMass,
            SimPart attachedPart) =>
            WithModulesMass(NewDecoupler(v, "KzInterstageAdapter2", persistentId, inverseStage, 0, attachedPart, 2), modulesMass);

        public static SimPart KzThrustPlate(SimVessel v, uint persistentId, int inverseStage, double modulesMass) =>
            NewProceduralPart(v, "KzThrustPlate", persistentId, inverseStage, 0, modulesMass);

        public static SimPart KzProcFairingSide1(SimVessel v, uint persistentId, int inverseStage, double modulesMass)
        {
            SimPart p = NewProceduralPart(v, "KzProcFairingSide1", persistentId, inverseStage, 0, modulesMass);
            SimProceduralFairingDecoupler d = AddModule(SimProceduralFairingDecoupler.Borrow(p));
            d.Index = 1;
            d.IsDecoupled = false;
            return p;
        }

        // the RP-1 launch clamp carries the free resources that its pumps supply to the vessel on the pad
        public static SimPart RP1LaunchClamp1(SimVessel v, uint persistentId, int inverseStage)
        {
            SimPart p = LaunchClamp1(v, persistentId, inverseStage, 1);
            AddResource(p, CLAMP_PUMP, 10, 10, 0);
            AddResource(p, AIR_PUMP, 5, 5, 0);
            return p;
        }

        // a 10 kN cold gas thruster on the Falcon 9 upper stage
        public static SimPart TE219F9CGT(SimVessel v, uint persistentId, int inverseStage)
        {
            SimPart p = NewPart(v, "TE2.19.F9.CGT", persistentId, inverseStage, 0.034499999135732651);
            AddResource(p, NITROGEN, 200000, 200000, NITROGEN_DENSITY);

            SimModuleRCS r = AddModule(SimModuleRCS.Borrow(p));
            r.IsEnabled = true;
            r.Index = 0;
            r.RcsEnabled = false;
            r.G = 9.80665;
            r.ISPMult = 1;
            r.ThrustPercentage = 100;
            r.MaxFuelFlow = 0.010197162129779282;
            // the SimVesselUpdater copies these from the ModuleRCS, and SimModuleRCS.Update() recomputes them
            r.Isp = 100;
            r.Thrust = 10;
            r.MassFlowRate = 0.010197162129779282;
            AppendConstant(r.AtmosphereCurve, 1, 1, 100);
            AppendCubicHermite(r.AtmosphereCurve, 1, 5, 1, 4, 100, -24.999750137329102, 0.0010000000474974513, -24.999750137329102);
            AppendConstant(r.AtmosphereCurve, 5, 5, 0.0010000000474974513);
            r.Propellants.Add(new SimPropellant(NITROGEN, false, 1, SimFlowMode.STACK_PRIORITY_SEARCH, NITROGEN_DENSITY));
            return p;
        }

        // a 43 N nitrogen cold gas thruster on the Titan II upper stage.  RealFuels takes some mass off the part.
        public static SimPart RCSBlock138(SimVessel v, uint persistentId, int inverseStage)
        {
            SimPart p = WithModulesMass(NewPart(v, "RCSBlock.138", persistentId, inverseStage, 0.012836580164730549),
                -0.0025100000202655792);

            SimModuleRCS r = AddModule(SimModuleRCS.Borrow(p));
            r.IsEnabled = true;
            r.Index = 0;
            r.RcsEnabled = false;
            r.G = 9.80665;
            r.ISPMult = 1;
            r.ThrustPercentage = 100;
            r.MaxFuelFlow = TITAN_RCS_MAX_FUEL_FLOW;
            // the SimVesselUpdater copies these from the ModuleRCS, and SimModuleRCS.Update() recomputes them
            r.Isp = 50.700000762939453;
            r.Thrust = TITAN_RCS_THRUST;
            r.MassFlowRate = TITAN_RCS_MAX_FUEL_FLOW;
            AppendConstant(r.AtmosphereCurve, 0, 0, 50.700000762939453);
            AppendCubicPolynomial(r.AtmosphereCurve, 0, 1, 0, 1, 50.700000762939453, -5.070000171661377, -69.053571462631226,
                46.857780694961548);
            AppendConstant(r.AtmosphereCurve, 1, 1, 23.434209823608398);
            r.Propellants.Add(new SimPropellant(NITROGEN, false, 1, SimFlowMode.STACK_PRIORITY_SEARCH, NITROGEN_DENSITY));
            return p;
        }

        public const double TITAN_RCS_MAX_FUEL_FLOW = 8.7041927049057551E-05;
        public const double TITAN_RCS_THRUST = 0.043276999145746224;

        // the hypergolic Titan II engines, with the propellant ratios of their RealismOverhaul configurations
        public const float LR87_AEROZINE50_RATIO = 0.454f;
        public const float LR87_NTO_RATIO = 0.546f;
        public const float LR91_AEROZINE50_RATIO = 0.4586f;
        public const float LR91_NTO_RATIO = 0.5414f;

        // the LR87-11, the Titan II first stage engine.  the six thrust transforms of its two chambers carry 588/590 of the
        // thrust and the last two 1/590 each.  the caller gives the eight thrust directions, which follow the vessel.
        public static SimPart ROELR87_11(SimVessel v, uint persistentId, int inverseStage, params V3[] thrustDirections)
        {
            SimModuleEngines e = NewRFEngine(v, "ROE-LR87.11", persistentId, inverseStage, 1.8625799417495728, 1, 0.780386746f,
                0.780386746f, 2311.19995f, 2311.19995f, 0.0049362938111973832, 3.2784943580627441);
            WithModulesMass(e.Part, -0.34650695323944092);
            e.ThrottleResponseRate = 0.377188444f;
            AppendConstant(e.AtmosphereCurve, 0, 0, 302);
            AppendCubicPolynomial(e.AtmosphereCurve, 0, 1, 0, 1, 302, -48, 0, 0);
            AppendConstant(e.AtmosphereCurve, 1, 1, 254);
            AddEmptyCurves(e);
            AddThrustTransforms(e, new[] { 0.16610167920589447, 0.16610167920589447, 0.16610167920589447, 0.16610167920589447,
                0.16610167920589447, 0.16610167920589447, 0.0016949150012806058, 0.0016949150012806058 }, thrustDirections);
            e.Propellants.Add(new SimPropellant(AEROZINE50, false, LR87_AEROZINE50_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH,
                AEROZINE50_DENSITY));
            e.Propellants.Add(new SimPropellant(NTO, false, LR87_NTO_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH, NTO_DENSITY));
            return e.Part;
        }

        // the LR91, the Titan II second stage engine.  three thrust transforms carry 0.99135 of the thrust and three more
        // the rest.  its atmosphereCurve is linear but for the single precision of its keys.
        public static SimPart ROELR91(SimVessel v, uint persistentId, int inverseStage, params V3[] thrustDirections)
        {
            SimModuleEngines e = NewRFEngine(v, "ROE-LR91", persistentId, inverseStage, 0.62099999189376831, 1, 0.147977576f,
                0.147977576f, 448.700012f, 448.700012f, 0.0047444175548941175, 2.3807389736175537);
            WithModulesMass(e.Part, -0.12099999189376831);
            e.ThrottleResponseRate = 0.520853162f;
            AppendConstant(e.AtmosphereCurve, 0, 0, 309.20001220703125);
            AppendCubicPolynomial(e.AtmosphereCurve, 0, 1, 0, 1, 309.20001220703125, -114.19999694824219, -4.57763671875E-05,
                3.0517578125E-05);
            AppendConstant(e.AtmosphereCurve, 1, 1, 195);
            AddEmptyCurves(e);
            AddThrustTransforms(e, new[] { 0.33044996857643127, 0.33044996857643127, 0.33044996857643127, 0.0028833332471549511,
                0.0028833332471549511, 0.0028833332471549511 }, thrustDirections);
            e.Propellants.Add(new SimPropellant(AEROZINE50, false, LR91_AEROZINE50_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH,
                AEROZINE50_DENSITY));
            e.Propellants.Add(new SimPropellant(NTO, false, LR91_NTO_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH, NTO_DENSITY));
            return e.Part;
        }

        // the SimVesselBuilder loads an empty FloatCurve as a constant 0, which the dump prints.  captures from before that,
        // like the MyFalcon9Block5's, have the optional curves empty.
        private static void AddEmptyCurves(SimModuleEngines e)
        {
            AppendConstant(e.ThrustCurve, 0, 0, 0);
            AppendConstant(e.ThrottleIspCurve, 0, 0, 0);
            AppendConstant(e.ThrottleIspCurveAtmStrength, 0, 0, 0);
            AppendConstant(e.VelCurve, 0, 0, 0);
            AppendConstant(e.VelCurveIsp, 0, 0, 0);
            AppendConstant(e.ATMCurve, 0, 0, 0);
            AppendConstant(e.ATMCurveIsp, 0, 0, 0);
        }

        private static void AddThrustTransforms(SimModuleEngines e, double[] multipliers, V3[] directions)
        {
            if (multipliers.Length != directions.Length)
                throw new ArgumentException($"{e.Part.Name} has {multipliers.Length} thrust transforms, not {directions.Length}");

            for (int i = 0; i < multipliers.Length; i++)
            {
                e.ThrustTransformMultipliers.Add(multipliers[i]);
                e.ThrustDirectionVectors.Add(directions[i]);
            }
        }

        public static SimPart ROEMerlin1D(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection) =>
            NewMerlin(v, "ROE-Merlin1D", persistentId, inverseStage, thrustDirection, 0.46998396515846252, 0.29972443f, 0.108201399f,
                914.119995f, 330, 2.651231050491333, 311, 288.5);

        public static SimPart ROEMerlin1DV(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection) =>
            NewMerlin(v, "ROE-Merlin1DV", persistentId, inverseStage, thrustDirection, 0.48997199535369873, 0.273717612f, 0.105487883f,
                934.119995f, 360, 2.6730942726135254, 348, 227);

        // the RD-108 burns HTP to drive its turbopumps, it is ignoreForIsp so the engine's rated isp is for the kerolox alone
        public const float RD108_KEROSENE_RATIO = 0.3806f;
        public const float RD108_LQD_OXYGEN_RATIO = 0.6194f;
        public const float RD108_HTP_RATIO = 0.0195f;

        // the RD-108 in its RD-108-8D75 configuration (the R-7 8K71 core engine), written by hand from the RealismOverhaul
        // config since no capture has it.  the residuals and spoolup that RealFuels would compute are left at zero, and the
        // main chamber (0.852 of the thrust) and the four verniers (0.037 each) are lumped into one thrust transform.
        public static SimPart ROERD108(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection)
        {
            const float MAX_FUEL_FLOW = 941.44f / (314.68f * 9.80665f);

            SimModuleEngines e = NewRFEngine(v, "ROE-RD108", persistentId, inverseStage, thrustDirection, 1.278, 0, MAX_FUEL_FLOW,
                MAX_FUEL_FLOW, 941.44f, 941.44f, 0, 0, 314.68f, 247.71f);
            AddResource(e.Part, TEATEB, 1, 1, TEATEB_DENSITY);
            e.Propellants.Add(new SimPropellant(KEROSENE, false, RD108_KEROSENE_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH,
                KEROSENE_DENSITY));
            e.Propellants.Add(new SimPropellant(LQD_OXYGEN, false, RD108_LQD_OXYGEN_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH,
                LQD_OXYGEN_DENSITY));
            e.Propellants.Add(new SimPropellant(HTP, true, RD108_HTP_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH, HTP_DENSITY));
            return e.Part;
        }

        // the RS-68 burns its ablative nozzle liner as Ablator, which is NO_FLOW so each engine burns only its own, and is
        // ignoreForIsp so the engine's rated isp is for the hydrolox alone
        public const float RS68_LQD_HYDROGEN_RATIO = 0.7285f;
        public const float RS68_LQD_OXYGEN_RATIO = 0.2715f;
        public const float RS68_ABLATOR_RATIO = 0.00169f;
        public const double RS68_ABLATOR = 1100;

        // the RS-68 in its RS-68 configuration (the Delta IV engine) with the ablative nozzle, written by hand from the
        // RealismOverhaul config since no capture has it.  the B9PartSwitch nozzle gives it 1100 units of Ablator.  the
        // residuals and spoolup that RealFuels would compute are left at zero, and the main chamber (0.98 of the thrust) and
        // the roll control exhaust are lumped into one thrust transform.
        public static SimPart ROERS68(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection)
        {
            SimModuleEngines e = NewRFEngine(v, "ROE-RS68", persistentId, inverseStage, thrustDirection, 6.6 * 0.8333, 0,
                3380.1f / (410f * 9.80665f), 1822.6f / (410f * 9.80665f), 3380.1f, 1822.6f, 0, 0, 410f, 365f);
            AddResource(e.Part, ABLATOR, RS68_ABLATOR, RS68_ABLATOR, ABLATOR_DENSITY);
            e.Propellants.Add(new SimPropellant(LQD_HYDROGEN, false, RS68_LQD_HYDROGEN_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH,
                LQD_HYDROGEN_DENSITY));
            e.Propellants.Add(new SimPropellant(LQD_OXYGEN, false, RS68_LQD_OXYGEN_RATIO, SimFlowMode.STACK_PRIORITY_SEARCH,
                LQD_OXYGEN_DENSITY));
            // the SimVesselBuilder takes the flow mode from Propellant.GetFlowMode(), which falls back to the NO_FLOW of the
            // Ablator resource definition
            e.Propellants.Add(new SimPropellant(ABLATOR, true, RS68_ABLATOR_RATIO, SimFlowMode.NO_FLOW, ABLATOR_DENSITY));
            return e.Part;
        }

        // a RealFuels Merlin burning kerolox
        private static SimPart NewMerlin(SimVessel v, string name, uint persistentId, int inverseStage, V3 thrustDirection, double dryMass,
            float maxFuelFlow, float minFuelFlow, float maxThrust, float minThrust, double spoolupTime, double vacuumIsp, double seaLevelIsp)
        {
            SimModuleEngines e = NewRFEngine(v, name, persistentId, inverseStage, thrustDirection, dryMass, 1, maxFuelFlow, minFuelFlow,
                maxThrust, minThrust, 0.0094669296351484397, spoolupTime, vacuumIsp, seaLevelIsp);
            AddResource(e.Part, TEATEB, 4, 4, TEATEB_DENSITY);
            e.Propellants.Add(new SimPropellant(COOLED_RP1, false, 0.3877f, SimFlowMode.STACK_PRIORITY_SEARCH, COOLED_RP1_DENSITY));
            e.Propellants.Add(new SimPropellant(COOLED_LQD_OXYGEN, false, 0.6123f, SimFlowMode.STACK_PRIORITY_SEARCH,
                COOLED_LQD_OXYGEN_DENSITY));
            return e.Part;
        }

        // a RealFuels engine with ullage, residuals, spoolup and auto cutoff, a single thrust transform, and an isp which is
        // linear in the atmospheric pressure up to 1 atm.  the caller adds the part's resources and the propellants.
        private static SimModuleEngines NewRFEngine(SimVessel v, string name, uint persistentId, int inverseStage, V3 thrustDirection,
            double dryMass, int moduleIndex, float maxFuelFlow, float minFuelFlow, float maxThrust, float minThrust, double residuals,
            double spoolupTime, double vacuumIsp, double seaLevelIsp)
        {
            SimModuleEngines e = NewRFEngine(v, name, persistentId, inverseStage, dryMass, moduleIndex, maxFuelFlow, minFuelFlow, maxThrust,
                minThrust, residuals, spoolupTime);
            AppendConstant(e.AtmosphereCurve, 0, 0, vacuumIsp);
            AppendCubicHermite(e.AtmosphereCurve, 0, 1, 0, 1, vacuumIsp, seaLevelIsp - vacuumIsp, seaLevelIsp, seaLevelIsp - vacuumIsp);
            AppendConstant(e.AtmosphereCurve, 1, 1, seaLevelIsp);
            e.ThrustTransformMultipliers.Add(1);
            e.ThrustDirectionVectors.Add(thrustDirection);
            return e;
        }

        // a RealFuels engine with ullage, residuals, spoolup and auto cutoff.  the caller adds the atmosphereCurve, the thrust
        // transforms, the part's resources and the propellants.
        private static SimModuleEngines NewRFEngine(SimVessel v, string name, uint persistentId, int inverseStage, double dryMass,
            int moduleIndex, float maxFuelFlow, float minFuelFlow, float maxThrust, float minThrust, double residuals, double spoolupTime)
        {
            SimPart p = NewPart(v, name, persistentId, inverseStage, dryMass);
            p.IsEngine = true;
            p.EngineResiduals = residuals;

            SimModuleEngines e = NewModuleEngines(p);
            e.IsEnabled = true;
            e.Index = moduleIndex;
            e.MaxFuelFlow = maxFuelFlow;
            e.MinFuelFlow = minFuelFlow;
            e.MaxThrust = maxThrust;
            e.MinThrust = minThrust;
            e.ModuleResiduals = residuals;
            e.ModuleSpoolupTime = spoolupTime;
            e.AutoCutoff = true;
            e.IsModuleEnginesRf = true;
            e.Ullage = true;
            return e;
        }

        // the prefabs weigh dryMass and the IPartMassModifier modules of the configured part add (or subtract) modulesMass
        private static SimPart NewProceduralPart(SimVessel v, string name, uint persistentId, int inverseStage, double dryMass,
            double modulesMass) =>
            WithModulesMass(NewPart(v, name, persistentId, inverseStage, dryMass), modulesMass);

        private static SimPart WithModulesMass(SimPart p, double modulesMass)
        {
            p.ModulesStagedMass = modulesMass;
            p.ModulesUnstagedMass = modulesMass;
            return p;
        }
    }
}
