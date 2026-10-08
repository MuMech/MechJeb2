/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

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
    ///     the vessel.
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
            double controllableMass)
        {
            SimPart p = NewProceduralPart(v, "proceduralAvionics", persistentId, inverseStage, 1, modulesMass);
            AddElectricCharge(p, electricCharge, electricCharge);
            AddModule(SimModuleAvionics.Borrow(p)).ControllableMass = controllableMass;
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

        public static SimPart RORFTankIntegral(SimVessel v, uint persistentId, int inverseStage, double modulesMass, double nitrogen)
        {
            SimPart p = NewProceduralPart(v, "RO-RFTank-Integral", persistentId, inverseStage, 1, modulesMass);
            AddResource(p, NITROGEN, nitrogen, nitrogen, NITROGEN_DENSITY);
            return p;
        }

        public static SimPart KzResizableFairingBase(SimVessel v, uint persistentId, int inverseStage, double modulesMass,
            SimPart attachedPart) =>
            WithModulesMass(NewDecoupler(v, "KzResizableFairingBase", persistentId, inverseStage, 0, attachedPart), modulesMass);

        public static SimPart KzFlatAdapter(SimVessel v, uint persistentId, int inverseStage, double modulesMass, SimPart attachedPart) =>
            WithModulesMass(NewDecoupler(v, "KzFlatAdapter", persistentId, inverseStage, 0, attachedPart), modulesMass);

        public static SimPart KzThrustPlate(SimVessel v, uint persistentId, int inverseStage, double modulesMass) =>
            NewProceduralPart(v, "KzThrustPlate", persistentId, inverseStage, 0, modulesMass);

        public static SimPart KzProcFairingSide1(SimVessel v, uint persistentId, int inverseStage, double modulesMass)
        {
            SimPart p = NewProceduralPart(v, "KzProcFairingSide1", persistentId, inverseStage, 0, modulesMass);
            AddModule(SimProceduralFairingDecoupler.Borrow(p)).IsDecoupled = false;
            return p;
        }

        // the RP-1 launch clamp carries the free resources that its pumps supply to the vessel on the pad
        public static SimPart RP1LaunchClamp1(SimVessel v, uint persistentId, int inverseStage)
        {
            SimPart p = LaunchClamp1(v, persistentId, inverseStage);
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
            r.IsEnabled        = true;
            r.RcsEnabled       = false;
            r.G                = 9.80665;
            r.ISPMult          = 1;
            r.ThrustPercentage = 100;
            r.MaxFuelFlow      = 0.010197162129779282;
            // the SimVesselUpdater copies these from the ModuleRCS, and SimModuleRCS.Update() recomputes them
            r.Isp          = 100;
            r.Thrust       = 10;
            r.MassFlowRate = 0.010197162129779282;
            AppendConstant(r.AtmosphereCurve, 1, 1, 100);
            AppendCubicHermite(r.AtmosphereCurve, 1, 5, 1, 4, 100, -24.999750137329102, 0.0010000000474974513, -24.999750137329102);
            AppendConstant(r.AtmosphereCurve, 5, 5, 0.0010000000474974513);
            r.Propellants.Add(new SimPropellant(NITROGEN, false, 1, SimFlowMode.STACK_PRIORITY_SEARCH, NITROGEN_DENSITY));
            return p;
        }

        public static SimPart ROEMerlin1D(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection) =>
            NewMerlin(v, "ROE-Merlin1D", persistentId, inverseStage, thrustDirection, 0.46998396515846252, 0.29972443f, 0.108201399f,
                914.119995f, 330, 2.651231050491333, 311, 288.5);

        public static SimPart ROEMerlin1DV(SimVessel v, uint persistentId, int inverseStage, V3 thrustDirection) =>
            NewMerlin(v, "ROE-Merlin1DV", persistentId, inverseStage, thrustDirection, 0.48997199535369873, 0.273717612f, 0.105487883f,
                934.119995f, 360, 2.6730942726135254, 348, 227);

        // a RealFuels Merlin with ullage, residuals, spoolup and auto cutoff, and an isp which is linear in the atmospheric
        // pressure up to 1 atm, burning kerolox
        private static SimPart NewMerlin(SimVessel v, string name, uint persistentId, int inverseStage, V3 thrustDirection, double dryMass,
            float maxFuelFlow, float minFuelFlow, float maxThrust, float minThrust, double spoolupTime, double vacuumIsp, double seaLevelIsp)
        {
            const double RESIDUALS = 0.0094669296351484397;

            SimPart p = NewPart(v, name, persistentId, inverseStage, dryMass);
            p.IsEngine        = true;
            p.EngineResiduals = RESIDUALS;
            AddResource(p, TEATEB, 4, 4, TEATEB_DENSITY);

            SimModuleEngines e = NewModuleEngines(p);
            e.IsEnabled         = true;
            e.MaxFuelFlow       = maxFuelFlow;
            e.MinFuelFlow       = minFuelFlow;
            e.MaxThrust         = maxThrust;
            e.MinThrust         = minThrust;
            e.ModuleResiduals   = RESIDUALS;
            e.ModuleSpoolupTime = spoolupTime;
            e.AutoCutoff        = true;
            e.IsModuleEnginesRf = true;
            e.Ullage            = true;
            AppendConstant(e.AtmosphereCurve, 0, 0, vacuumIsp);
            AppendCubicHermite(e.AtmosphereCurve, 0, 1, 0, 1, vacuumIsp, seaLevelIsp - vacuumIsp, seaLevelIsp, seaLevelIsp - vacuumIsp);
            AppendConstant(e.AtmosphereCurve, 1, 1, seaLevelIsp);
            e.Propellants.Add(new SimPropellant(COOLED_RP1, false, 0.3877f, SimFlowMode.STACK_PRIORITY_SEARCH, COOLED_RP1_DENSITY));
            e.Propellants.Add(new SimPropellant(COOLED_LQD_OXYGEN, false, 0.6123f, SimFlowMode.STACK_PRIORITY_SEARCH,
                COOLED_LQD_OXYGEN_DENSITY));
            e.ThrustTransformMultipliers.Add(1);
            e.ThrustDirectionVectors.Add(thrustDirection);
            return p;
        }

        // the prefabs weigh dryMass and the IPartMassModifier modules of the configured part add (or subtract) modulesMass
        private static SimPart NewProceduralPart(SimVessel v, string name, uint persistentId, int inverseStage, double dryMass,
            double modulesMass) =>
            WithModulesMass(NewPart(v, name, persistentId, inverseStage, dryMass), modulesMass);

        private static SimPart WithModulesMass(SimPart p, double modulesMass)
        {
            p.ModulesStagedMass   = modulesMass;
            p.ModulesUnstagedMass = modulesMass;
            return p;
        }
    }
}
