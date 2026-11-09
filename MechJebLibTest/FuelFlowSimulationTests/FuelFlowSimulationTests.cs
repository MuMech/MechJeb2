/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.FuelFlowSimulation.PartModules;
using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static MechJebLibTest.FuelFlowSimulationTests.ResourceFixtures;
using static MechJebLibTest.FuelFlowSimulationTests.RP1PartFixtures;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class FuelFlowSimulationTests
    {
        private const double MAINSAIL_MAX_FUEL_FLOW = 1500f / (310f * 9.80665f);
        private const double MAINSAIL_VACUUM_ISP = 310;
        private const double MAINSAIL_MASS = 6;
        private const double JET_MAX_FUEL_FLOW = 130f / (4000f * 9.80665f);
        private const double JET_ISP = 4000;
        private const double SEA_LEVEL_DENSITY = 1.225;
        private const double RD108_MAX_FUEL_FLOW = 941.44f / (314.68f * 9.80665f);
        private const double RD108_VACUUM_ISP = 314.68f;
        private const double RD108_SEA_LEVEL_ISP = 247.71f;
        private const double RS68_MAX_FUEL_FLOW = 3380.1f / (410f * 9.80665f);
        private const double RS68_VACUUM_ISP = 410f;
        private const double RS68_MASS = 6.6 * 0.8333;

        // the mass of the liquid fuel and oxidizer in a full Rockomax64
        private const double ROCKOMAX64_PROPELLANT = 32;
        private const double ROCKOMAX64_LIQUID_FUEL = 14.4;

        // a full Rockomax64 (36), the Whiplash (1.8), and the shock cone intake (0.12) with its 2 units of air
        private const double JET_TIN_CAN_MASS = 37.93;

        // the RAPIER's AirBreathing mode
        private const double RAPIER_AIR_MAX_FUEL_FLOW = 105f / (3200f * 9.80665f);
        private const double RAPIER_AIR_ISP = 3200;

        // the JetTinCan with the RAPIER (2) in place of the Whiplash
        private const double RAPIER_TIN_CAN_MASS = 38.13;

        private const double ION_MAX_FUEL_FLOW = 2f / (4200f * 9.80665f);
        private const double ION_ISP = 4200;

        // the probe core (0.5), the battery bank (0.2) and the ion engine (0.25): ElectricCharge has no mass
        private const double ION_TIN_CAN_MASS = 0.95;

        // the xenon tank (0.05) and its xenon (0.04)
        private const double XENON_TANK_MASS = 0.09;
        private const double XENON_TANK_XENON = 0.04;

        // the RealFuels Merlins of the MyFalcon9Block5 and the MerlinPair
        private const float MERLIN_MAX_FUEL_FLOW = 0.29972443f;
        private const float MERLIN_MIN_FUEL_FLOW = 0.108201399f;
        private const double MERLIN_VACUUM_ISP = 311;
        private const double MERLIN_SPOOLUP_TIME = 2.651231050491333;
        private const float MVAC_MAX_FUEL_FLOW = 0.273717612f;
        private const float MVAC_MIN_FUEL_FLOW = 0.105487883f;
        private const double MVAC_SPOOLUP_TIME = 2.6730942726135254;
        private const double MERLIN_RESIDUALS = 0.0094669296351484397;

        // a Merlin with its TEATEB
        private const double MERLIN_MASS = 0.46998396515846252 + 4 * TEATEB_DENSITY;

        // the propellant in a full tank of the MerlinPair, which is 5 t dry
        private const double MERLIN_PAIR_PROPELLANT =
            SimVesselFixtures.MERLIN_PAIR_KEROLOX * (0.3877f * COOLED_RP1_DENSITY + 0.6123f * COOLED_LQD_OXYGEN_DENSITY);

        private const double PUFF_MAX_FUEL_FLOW = 20f / (250f * 9.80665f);
        private const double PUFF_ISP = 250;
        private const double RCS_BLOCK_MAX_FUEL_FLOW = 1 / (240 * 9.80665);
        private const double RCS_BLOCK_ISP = 240;

        // the 750 units of monopropellant in an FL-R750
        private const double RCS_TANK_MONOPROPELLANT = 750 * MONOPROPELLANT_DENSITY;

        // the probe core (0.5), the FL-R750 (0.4), the Puff (0.09) and the RV-105 (0.04)
        private const double PUFF_TIN_CAN_DRY_MASS = 1.03;

        private const double SEPRATRON_MAX_FUEL_FLOW = 18f / (154f * 9.80665f);
        private const double SEPRATRON_ISP = 154;
        private const double SEPRATRON_SOLID_FUEL = 8 * SOLID_FUEL_DENSITY;

        // the probe core (0.5) and the Sepratron (0.0125) with its solid fuel
        private const double SEPRATRON_TIN_CAN_MASS = 0.5125 + SEPRATRON_SOLID_FUEL;

        [Fact]
        public void MainsailTinCanMatchesCapture() => AssertMatchesCapture("MainsailTinCan.out", SimVesselFixtures.MainsailTinCan);

        [Fact]
        public void MainsailTinCan2StageMatchesCapture() =>
            AssertMatchesCapture("MainsailTinCan2Stage.out", SimVesselFixtures.MainsailTinCan2Stage);

        [Fact]
        public void MainsailTinCanAsparagusMatchesCapture() =>
            AssertMatchesCapture("MainsailTinCanAsparagus.out", SimVesselFixtures.MainsailTinCanAsparagus);

        [Fact]
        public void MyFalcon9Block5MatchesCapture() => AssertMatchesCapture("MyFalcon9Block5.out", SimVesselFixtures.MyFalcon9Block5);

        // the Mainsail burns both full tanks to empty after the launch clamps are released
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MainsailTinCan(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan(kscPad);
            double isp = kscPad ? MainsailIsp(v.ATMPressure) : MAINSAIL_VACUUM_ISP;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7, 0);
            AssertMainsailBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude);
            AssertMassesMatchCapture(v, "MainsailTinCan.out", kscPad);
        }

        // the editor's CurrentStage one too low: the extra top segment still holds the whole vessel, so the stage below
        // stages no mass
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MainsailTinCanWithTheEditorsCurrentStage(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.MainsailTinCanAtStage(0, kscPad);
            double isp = kscPad ? MainsailIsp(v.ATMPressure) : MAINSAIL_VACUUM_ISP;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7, 0);
            AssertMainsailBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude);
        }

        // a new, empty craft in the editor: no parts, no thrust, no exception
        [Fact]
        public void EmptyVessel()
        {
            SimVessel v = SimVesselFixtures.Finish(SimVesselFixtures.NewVessel(0));

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertNoBurn(segments[0], 0, 0, 0);
        }

        // at sea level the Whiplash runs at full flow, and burns the liquid fuel but no oxidizer and no intake air
        [Fact]
        public void JetBurnsInTheAtmosphere()
        {
            SimVessel v = SimVesselFixtures.JetTinCan();
            v.SetConditions(SEA_LEVEL_DENSITY, 1, 0);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, JET_TIN_CAN_MASS, ROCKOMAX64_LIQUID_FUEL, JET_MAX_FUEL_FLOW, JET_ISP, 1);
        }

        // with no air the engine has no flow at all, not the tiny flow of KSP's CLAMP, which would make up a long burn with
        // all of the delta-v and almost no thrust
        [Fact]
        public void JetDoesNotBurnInVacuum()
        {
            SimVessel v = SimVesselFixtures.JetTinCan();
            v.SetConditions(0, 0, 0);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertNoBurn(segments[0], 0, JET_TIN_CAN_MASS, 0);
        }

        // a stock intake gathers nothing in an atmosphere without oxygen (Eve, Duna), so the jet doesn't burn there either
        [Fact]
        public void JetDoesNotBurnWithoutOxygen()
        {
            SimVessel v = SimVesselFixtures.JetTinCan();
            v.SetConditions(SEA_LEVEL_DENSITY, 1, 0, false);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertNoBurn(segments[0], 0, JET_TIN_CAN_MASS, 0);
        }

        // the stock ion engine: its xenon is the propellant that has mass and counts for the isp
        [Fact]
        public void IonEngineBurnsItsXenon()
        {
            SimVessel v = SimVesselFixtures.IonTinCan(true);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, ION_TIN_CAN_MASS + XENON_TANK_MASS, XENON_TANK_XENON, ION_MAX_FUEL_FLOW, ION_ISP, 1);
        }

        // with its xenon ignoreForIsp no propellant with mass counts for the isp: KSP's ModuleEngines then divides the mass flow
        // by 1, not by the mixture's density of 0, so the engine draws 0.1 units of xenon per tonne of flow, a very long burn.
        // this was an infinite flow, which emptied the tank in one 1 ms step.
        [Fact]
        public void IgnoreForIspXenonBurnsAsInKsp()
        {
            SimVessel v = SimVesselFixtures.IonTinCan(true, true);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            FuelStats s = segments[0];
            s.KSPStage.ShouldEqual(0);
            s.StartMass.ShouldEqual(ION_TIN_CAN_MASS + XENON_TANK_MASS, 1e-6);
            s.EndMass.ShouldEqual(ION_TIN_CAN_MASS + XENON_TANK_MASS - XENON_TANK_XENON, 1e-6);
            s.DeltaTime.ShouldEqual(400 / (0.1f * ION_MAX_FUEL_FLOW), 1e-6);
            s.Thrust.ShouldEqual(2, 1e-6);
        }

        // an engine on ElectricCharge alone draws nothing the simulation can run out of, so it would run for ever: it gives the
        // stage its thrust but no burn.  this ran 10,000 steps of 1 ms and threw.
        [Fact]
        public void ElectricOnlyEngineHasThrustButNoBurn()
        {
            SimVessel v = SimVesselFixtures.IonTinCan();

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertNoBurn(segments[0], 0, ION_TIN_CAN_MASS, 0, 2);
        }

        // beside a rocket the electric engine adds its thrust while the rocket burns, and the stage ends when the rocket burns out
        [Fact]
        public void ElectricOnlyEngineBesideARocket()
        {
            SimVessel v = SimVesselFixtures.IonTinCan(withMainsail: true);
            double startMass = ION_TIN_CAN_MASS + ROCKOMAX64_PROPELLANT + 4 + MAINSAIL_MASS;

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            FuelStats s = segments[0];
            s.KSPStage.ShouldEqual(0);
            s.StartMass.ShouldEqual(startMass, 1e-6);
            s.EndMass.ShouldEqual(startMass - ROCKOMAX64_PROPELLANT, 1e-6);
            s.DeltaTime.ShouldEqual(ROCKOMAX64_PROPELLANT / MAINSAIL_MAX_FUEL_FLOW, 1e-6);
            s.Thrust.ShouldEqual(MAINSAIL_MAX_FUEL_FLOW * MAINSAIL_VACUUM_ISP * 9.80665f + 2, 1e-6);
        }

        // an RCS thruster on ElectricCharge alone has no fuel flow, so no thrust, as in KSP.  this ran 10,000 steps of 1 ms
        // with a NaN delta-v and threw.
        [Fact]
        public void ElectricOnlyRcsHasNoThrust()
        {
            SimVessel v = SimVesselFixtures.IonTinCan(true, electricRcs: true);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, ION_TIN_CAN_MASS + XENON_TANK_MASS + 0.05, XENON_TANK_XENON, ION_MAX_FUEL_FLOW, ION_ISP, 1);
            segments[0].RcsThrust.ShouldBeZero();
            segments[0].MinRcsDeltaV.ShouldBeZero();
            segments[0].MaxRcsDeltaV.ShouldBeZero();
        }

        // in vacuum the rocket beside the jet has the fuel to itself
        [Fact]
        public void RocketBesideAJetBurnsAloneInVacuum()
        {
            SimVessel v = SimVesselFixtures.JetTinCan(true);
            v.SetConditions(0, 0, 0);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertMainsailBurn(segments[0], 0, JET_TIN_CAN_MASS + MAINSAIL_MASS, ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, 1);
            // the jet's CLAMP flow adds less thrust than the tolerance of a burn
            segments[0].Thrust.ShouldEqual(MAINSAIL_MAX_FUEL_FLOW * MAINSAIL_VACUUM_ISP * 9.80665f, 1e-7);
        }

        // sample [0,1] atm in 0.1 atm steps, then [2,10] in 1 atm steps
        // the value of 10 walks off the end of the stock FloatCurve interpolant (ends at 9)
        public static IEnumerable<object[]> AtmosphericPressures()
        {
            for (int i = 0; i <= 10; i++)
                yield return new object[] { i / 10.0 };
            for (int i = 2; i <= 10; i++)
                yield return new object[] { (double)i };
        }

        // sweeps the pressure across the Mainsail's atmosphereCurve: inside its cubic segments between the keys at 0, 1 and
        // 9 atm, and past the 9 atm key, where a FloatCurve clamps to the last key.  the Mainsail's fuel flow does not depend
        // on the atmosphere, so only the isp, thrust and delta-v change.
        [Theory]
        [MemberData(nameof(AtmosphericPressures))]
        public void MainsailTinCanAtmosphericPressure(double atmPressure)
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan();
            // the Mainsail uses neither the density nor the mach number
            v.SetConditions(0, atmPressure, 0);
            double isp = MainsailIsp(atmPressure);
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7, 0);
            AssertMainsailBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude);
        }

        // the Mainsail lights as the launch clamps are released, but the drop tank is outside its crossfeed set, so the vessel
        // stages before the lit engine burns anything to drop the full tank as dead mass, then the Mainsail burns the core's tanks
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MainsailTinCanDropTank(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.MainsailTinCanDropTank(kscPad);
            double isp = kscPad ? MainsailIsp(v.ATMPressure) : MAINSAIL_VACUUM_ISP;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(3, segments.Count);
            AssertNoBurn(segments[2], 2, 115.75, 0);
            AssertNoBurn(segments[1], 1, 115.75, 0, MAINSAIL_MAX_FUEL_FLOW * isp * G0 * dirMagnitude);
            // the radial decoupler and the drop tank, still full
            AssertMainsailBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude, 36.05);
        }

        // with crossfeed enabled on the decoupler the Mainsail can reach the drop tank, which has the higher resource priority,
        // so the Mainsail drains the drop tank before the core's tanks.  the vessel stages once the drop tank is empty and
        // the Mainsail burns the core's tanks.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MainsailTinCanDropTankCrossFeed(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.MainsailTinCanDropTank(kscPad, true);
            double isp = kscPad ? MainsailIsp(v.ATMPressure) : MAINSAIL_VACUUM_ISP;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(3, segments.Count);
            AssertNoBurn(segments[2], 2, 115.75, 0);

            // when the drop tank's oxidizer runs out the Mainsail takes its stranded liquid fuel in 1 ms steps, the
            // simulation's minimum, while drawing oxidizer from the core's tanks, as with the asparagus boosters.
            const double TOLERANCE = 1e-4;
            AssertMainsailBurn(segments[1], 1, 115.75, ROCKOMAX64_PROPELLANT, isp, dirMagnitude, tolerance: TOLERANCE);
            // the radial decoupler and the empty drop tank
            AssertMainsailBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude, 4.05, tolerance: TOLERANCE);
        }

        // tanks that share a priority share the demand in proportion to what each holds, as in KSP's PartSet.ProcessRequest,
        // so they run dry together.  here the drop tank, half full, has the core's priority (which KSP's flow priority tweakable
        // can give it).  with the demand split equally it would run dry first and the vessel would stage it with 32 t still in
        // the core tanks, a staging that KSP doesn't do.
        [Fact]
        public void TanksOfOnePriorityRunDryTogether()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCanDropTank(false, true);
            SimPart dropTank = v.Parts.Find(p => p.PersistentId == 1000000002);
            dropTank.ResourcePriority = -10;
            AddLiquidFuel(dropTank, 1440, 2880);
            AddOxidizer(dropTank, 1760, 3520);
            double dirMagnitude = ThrustDirectionMagnitude(v);
            const double START_MASS = 115.75 - ROCKOMAX64_PROPELLANT / 2;
            const double PROPELLANT = 2.5 * ROCKOMAX64_PROPELLANT;
            const double TOLERANCE = 1e-4;

            List<FuelStats> segments = Run(v);

            Assert.Equal(3, segments.Count);
            AssertNoBurn(segments[2], 2, START_MASS, 0);
            AssertMainsailBurn(segments[1], 1, START_MASS, PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude, tolerance: TOLERANCE);
            // then the radial decoupler and the empty drop tank go, with nothing left to burn
            AssertNoBurn(segments[0], 0, START_MASS - PROPELLANT - 4.05, 4.05);
        }

        // the lower Mainsail lights while the launch clamps hold the vessel and burns the lower tank, then the decoupler
        // drops the lower stage and the upper Mainsail burns the upper tank
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MainsailTinCan2Stage(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage(kscPad);
            double isp = kscPad ? MainsailIsp(v.ATMPressure) : MAINSAIL_VACUUM_ISP;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(5, segments.Count);
            AssertNoBurn(segments[4], 4, 85.86, 0);
            // releasing the clamps drops nothing but massless parts, so the vessel stages before the lit engine burns anything
            AssertNoBurn(segments[3], 3, 85.86, 0, MAINSAIL_MAX_FUEL_FLOW * isp * G0 * dirMagnitude);
            AssertMainsailBurn(segments[2], 2, 85.86, ROCKOMAX64_PROPELLANT, isp, dirMagnitude);
            // the decoupler, the empty lower tank and the lower Mainsail
            AssertNoBurn(segments[1], 1, 43.7, 10.16);
            AssertMainsailBurn(segments[0], 0, 43.7, ROCKOMAX64_PROPELLANT, isp, dirMagnitude);
            AssertMassesMatchCapture(v, "MainsailTinCan2Stage.out", kscPad);
        }

        // all five Mainsails light while the launch clamps hold the vessel.  the boosters' tanks have the higher resource
        // priorities, so every engine drains the outer boosters, which are then dropped, and then the inner boosters, which
        // are then dropped, before the core Mainsail burns the core's tanks.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MainsailTinCanAsparagus(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.MainsailTinCanAsparagus(kscPad);
            double isp = kscPad ? MainsailIsp(v.ATMPressure) : MAINSAIL_VACUUM_ISP;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(5, segments.Count);
            AssertNoBurn(segments[4], 4, 393.1, 0);
            AssertNoBurn(segments[3], 3, 393.1, 0, 5 * MAINSAIL_MAX_FUEL_FLOW * isp * G0 * dirMagnitude);

            // when the boosters' oxidizer runs out the remaining engines take the stranded liquid fuel from the boosters in 1 ms
            // steps, the simulation's minimum, while drawing oxidizer from the tanks inboard.  that shifts the burns by ~4e-5
            // from the ideal rocket equation and strands more liquid fuel in the core's tanks, which is pinned down exactly by
            // matching the masses KSP's own run left behind.
            const double TOLERANCE = 1e-4;
            AssertMainsailBurn(segments[2], 2, 393.1, 4 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude, engines: 5, tolerance: TOLERANCE);
            // each booster drops its decoupler, nose cone, fuel line, empty tanks and Mainsail
            AssertMainsailBurn(segments[1], 1, 236.5, 4 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude, 28.6, 3, TOLERANCE);
            AssertMainsailBurn(segments[0], 0, 79.9, 2 * ROCKOMAX64_PROPELLANT, isp, dirMagnitude, 28.6, tolerance: TOLERANCE);
            AssertMassesMatchCapture(v, "MainsailTinCanAsparagus.out", kscPad);
        }

        // the same staging as the fuel line asparagus: every engine drains the outer boosters first, but their Mainsails are
        // still running on the tanks inboard of them when their own tanks run dry.  having drop tanks, they are held only by
        // those tanks, so the boosters are dropped as soon as they are empty rather than riding along until the core is dry.
        [Fact]
        public void MainsailTinCanCrossfeedAsparagus()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCanCrossfeedAsparagus();
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(5, segments.Count);
            AssertNoBurn(segments[4], 4, 392.9, 0);
            AssertNoBurn(segments[3], 3, 392.9, 0, 5 * MAINSAIL_MAX_FUEL_FLOW * MAINSAIL_VACUUM_ISP * G0 * dirMagnitude);

            const double TOLERANCE = 1e-4;
            AssertMainsailBurn(segments[2], 2, 392.9, 4 * ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude, engines: 5,
                tolerance: TOLERANCE);
            // each booster drops its decoupler, nose cone, empty tanks and its still running Mainsail
            AssertMainsailBurn(segments[1], 1, 236.4, 4 * ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude, 28.5, 3, TOLERANCE);
            AssertMainsailBurn(segments[0], 0, 79.9, 2 * ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude, 28.5,
                tolerance: TOLERANCE);
        }

        // the Mainsails on the radial decouplers have no drop tanks, they burn the core's tanks, so they are held until they
        // flame out with the core Mainsail when the core is dry.
        [Fact]
        public void MainsailTinCanEnginePods()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCanEnginePods();
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(3, segments.Count);
            AssertNoBurn(segments[2], 2, 90.6, 0);
            AssertMainsailBurn(segments[1], 1, 90.6, 2 * ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude, engines: 3);
            // the two decouplers and the burned out pod Mainsails
            AssertNoBurn(segments[0], 0, 14.5, 12.1);
        }

        // the nine Merlins light while the launch clamps hold the vessel and burn the first stage down to its RealFuels
        // residuals, about 4076 m/s in vacuum.  dropping the first stage lights the Merlin Vacuum, the payload fairing is
        // jettisoned before it burns anything, and it burns the upper stage down to its residuals, about 7298 m/s in vacuum.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MyFalcon9Block5(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.MyFalcon9Block5(kscPad);
            // the Merlins' atmosphereCurves are linear between vacuum and 1 atm
            double merlinIsp = 311 - 22.5 * v.ATMPressure;
            double mvacIsp = 348 - 121 * v.ATMPressure;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            const double PAD_MASS = 557.28197016018203;
            const double UPPER_STACK_MASS = 122.56389040936439;
            const double PAYLOAD_FAIRING_MASS = 2 * 0.82329875230789185;
            // the lead ballast payload and the upper stage avionics
            double payloadMass = 1 - 0.91552984714508057 + 659.92299999999966 * LEAD_BALLAST_DENSITY + 1 - 0.70491535891778767;
            double firstStagePropellant = (1 - MERLIN_RESIDUALS) *
                (149242.99723951373 * COOLED_RP1_DENSITY + 235701.53739390321 * COOLED_LQD_OXYGEN_DENSITY);
            double upperStagePropellant = (1 - MERLIN_RESIDUALS) *
                (38750.520237705852 * COOLED_RP1_DENSITY + 61199.234562294143 * COOLED_LQD_OXYGEN_DENSITY);
            double nitrogen = (4 * 44323.939999999995 + 4 * 200000) * NITROGEN_DENSITY;

            List<FuelStats> segments = Run(v);

            Assert.Equal(6, segments.Count);

            // the first stage's avionics can control 600t, the upper stage's 150t
            for (int i = 0; i < segments.Count; i++)
                segments[i].ControllableMass.ShouldEqual(i >= 3 ? 600 : 150);

            // the spoolup is that of the engines which ignite in the segment's stage: the Merlins while the launch clamps hold
            // the vessel, the Merlin Vacuum as the first stage drops.  the other segments have no engines, or engines which are
            // already running, and the NodeExecutor adds up the spoolup of every segment of a burn.  the spoolup was once
            // counted again in every segment the engines burned in.
            for (int i = 0; i < segments.Count; i++)
                segments[i].SpoolUpTime.ShouldEqual(i == 4 ? MERLIN_SPOOLUP_TIME : i == 2 ? MVAC_SPOOLUP_TIME : 0, 1e-12);

            // the thrust range, which the PSG's throttle limits use: the minimum is the engines' at their minimum fuel flow
            segments[3].MaxThrust.ShouldEqual(9 * MERLIN_MAX_FUEL_FLOW * merlinIsp * G0 * dirMagnitude, 1e-6);
            segments[3].MinThrust.ShouldEqual(9 * MERLIN_MIN_FUEL_FLOW * merlinIsp * G0 * dirMagnitude, 1e-6);
            segments[1].MaxThrust.ShouldEqual(MVAC_MAX_FUEL_FLOW * mvacIsp * G0 * dirMagnitude, 1e-6);
            segments[1].MinThrust.ShouldEqual(MVAC_MIN_FUEL_FLOW * mvacIsp * G0 * dirMagnitude, 1e-6);

            AssertNoBurn(segments[5], 5, PAD_MASS, 0);
            AssertNoBurn(segments[4], 4, PAD_MASS, 0, 9 * MERLIN_MAX_FUEL_FLOW * merlinIsp * G0 * dirMagnitude);

            // the burns run one 1 ms step, the simulation's minimum, past the propellant reaching the residuals, which adds
            // ~7e-6 to their time and delta-v.
            const double TOLERANCE = 1e-5;
            AssertBurn(segments[3], 3, PAD_MASS, firstStagePropellant, MERLIN_MAX_FUEL_FLOW, merlinIsp, dirMagnitude, engines: 9,
                tolerance: TOLERANCE);

            // the first stage drops with its residuals and the interstage fairing, and the Merlin Vacuum lights
            AssertNoBurn(segments[2], 2, UPPER_STACK_MASS, PAD_MASS - firstStagePropellant - UPPER_STACK_MASS,
                MVAC_MAX_FUEL_FLOW * mvacIsp * G0 * dirMagnitude);
            AssertBurn(segments[1], 1, UPPER_STACK_MASS - PAYLOAD_FAIRING_MASS, upperStagePropellant, MVAC_MAX_FUEL_FLOW, mvacIsp,
                dirMagnitude, PAYLOAD_FAIRING_MASS, tolerance: TOLERANCE);

            // the four 10 kN cold gas thrusters settle the Merlin Vacuum's propellant in ~0.71 s, using the RealFuels ullage
            // approximation, and their RCS delta-v uses all the nitrogen.
            segments[1].RcsUllageTime.ShouldEqual(0.35 / (1.5 * 40 / segments[1].StartMass), 1e-12);
            segments[1].RcsThrust.ShouldEqual(40, 1e-12);
            segments[1].RcsISP.ShouldEqual(100, 1e-6);
            segments[1].RcsMass.ShouldEqual(nitrogen, 1e-12);

            // the upper stage drops with its residuals, nitrogen and the fairing base
            AssertNoBurn(segments[0], 0, payloadMass, segments[1].EndMass - payloadMass);

            AssertMassesMatchCapture(v, "MyFalcon9Block5.out", kscPad);
        }

        // the RD-108's HTP is ignoreForIsp: its fuel flow and rated isp are for the kerosene and liquid oxygen alone, so its
        // thrust is the bench thrust, but it burns the HTP on top of that.  that lowers the isp of the whole flow and the
        // delta-v by the mass of HTP burned per unit mass of kerolox, ~2.8%.  in vacuum the isp is ~306.16s against the
        // rated 314.68s, and the tin can gets ~8218 m/s against the ~8446 m/s the rated isp would give.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RD108TinCan(bool kscPad)
        {
            SimVessel v = SimVesselFixtures.RD108TinCan(kscPad);
            // the atmosphereCurve is linear between vacuum and 1 atm
            double ratedIsp = RD108_VACUUM_ISP + (RD108_SEA_LEVEL_ISP - RD108_VACUUM_ISP) * v.ATMPressure;
            double keroloxDensity = RD108_KEROSENE_RATIO * KEROSENE_DENSITY + RD108_LQD_OXYGEN_RATIO * LQD_OXYGEN_DENSITY;
            double htpMass = RD108_HTP_RATIO * HTP_DENSITY;
            double htpPerKerolox = htpMass / keroloxDensity;
            double propellant = SimVesselFixtures.RD108_TIN_CAN_KEROLOX * (keroloxDensity + htpMass);
            // the RD-108 with its TEATEB, and the tank
            double dryMass = 1.278 + TEATEB_DENSITY + 5;

            List<FuelStats> segments = Run(v);

            // the HTP burns out along with the kerolox, leaving the dry mass
            Assert.Single(segments);
            AssertBurn(segments[0], 0, dryMass + propellant, propellant, RD108_MAX_FUEL_FLOW, ratedIsp, 1, ignoredForIsp: htpPerKerolox);
        }

        // MechJeb2 PR #1612: each RS-68 burns the Ablator in its own nozzle, which is NO_FLOW.  all three RS-68s drain the
        // boosters' tanks first, and when those are dry the boosters' RS-68s flame out still holding ~706 units of Ablator,
        // which the core's crossfeed set reaches through the fuel lines.  the core RS-68 can only burn its own Ablator, so
        // that must not keep the boosters on: they are dropped at ~100 s and the core burns its own tank alone to ~251 s.
        // the old simulation counted the boosters' Ablator as reachable and carried the empty boosters until the core burned
        // out.  the Ablator is ignoreForIsp, which takes the isp from the rated 410 s to ~408.09 s.
        [Fact]
        public void RS68TinCanAsparagus()
        {
            SimVessel v = SimVesselFixtures.RS68TinCanAsparagus();
            double hydroloxDensity = RS68_LQD_HYDROGEN_RATIO * LQD_HYDROGEN_DENSITY + RS68_LQD_OXYGEN_RATIO * LQD_OXYGEN_DENSITY;
            double ablatorPerHydrolox = RS68_ABLATOR_RATIO * ABLATOR_DENSITY / hydroloxDensity;
            // the litres of hydrolox, and the units of Ablator, each RS-68 burns per second
            double volumeFlow = RS68_MAX_FUEL_FLOW / hydroloxDensity;
            double ablatorFlow = RS68_ABLATOR_RATIO * volumeFlow;
            // each booster's tank feeds its own RS-68 and half of the core's
            double boosterTime = SimVesselFixtures.RS68_TIN_CAN_HYDROLOX / (1.5 * volumeFlow);
            double hydrolox = SimVesselFixtures.RS68_TIN_CAN_HYDROLOX * hydroloxDensity;
            const double TANK_MASS = 10;
            const double RADIAL_DECOUPLER2_MASS = 0.05f;
            const double FUEL_LINE_MASS = 0.05f;
            double padMass = 3 * (TANK_MASS + hydrolox + RS68_MASS + RS68_ABLATOR * ABLATOR_DENSITY) +
                2 * (RADIAL_DECOUPLER2_MASS + FUEL_LINE_MASS);
            double boosterBurn = 2 * hydrolox * (1 + ablatorPerHydrolox);
            double coreBurn = hydrolox * (1 + ablatorPerHydrolox);
            // each booster drops its decoupler, empty tank, fuel line, and RS-68 with the Ablator it did not burn
            double boosterAblator = RS68_ABLATOR - ablatorFlow * boosterTime;
            double stagedMass = 2 * (RADIAL_DECOUPLER2_MASS + TANK_MASS + FUEL_LINE_MASS + RS68_MASS + boosterAblator * ABLATOR_DENSITY);

            List<FuelStats> segments = Run(v);

            Assert.Equal(3, segments.Count);
            AssertNoBurn(segments[2], 2, padMass, 0);
            AssertBurn(segments[1], 1, padMass, boosterBurn, RS68_MAX_FUEL_FLOW, RS68_VACUUM_ISP, 1, engines: 3,
                ignoredForIsp: ablatorPerHydrolox);
            // the core's burn runs one 1 ms step, the simulation's minimum, past its tank running dry, which adds ~7e-6 to its
            // time, isp and delta-v
            AssertBurn(segments[0], 0, padMass - boosterBurn - stagedMass, coreBurn, RS68_MAX_FUEL_FLOW, RS68_VACUUM_ISP, 1, stagedMass,
                tolerance: 1e-5, ignoredForIsp: ablatorPerHydrolox);

            // every RS-68 burned Ablator for as long as it ran, and only its own: the boosters' RS-68s until the boosters were
            // dropped, and the core's to the end.  the core drawing on the boosters' Ablator would leave them less and it more.
            // the boosters were dropped still holding ~706 units, and the core stopped on its hydrolox with ~114 left.
            List<SimPart> engines = v.Parts.FindAll(p => p.IsEngine);
            Assert.Equal(3, engines.Count);
            Assert.Single(engines, p => p.DecoupledInStage == -1);
            foreach (SimPart engine in engines)
            {
                bool booster = engine.DecoupledInStage == 0;
                double runTime = booster ? segments[1].DeltaTime : segments[1].DeltaTime + segments[0].DeltaTime;
                double ablator = engine.Resources[ABLATOR].Amount;
                ablator.ShouldEqual(RS68_ABLATOR - ablatorFlow * runTime, 1e-8);
                ablator.ShouldEqual(booster ? 705.67 : 114.16, 1e-4);
            }
        }

        // MechJeb2 PR #1901: as in KSP's ModuleEngines, multFlow (which TestFlight's loss of thrust failures lower) scales the
        // fuel flow, so at half the flow the Mainsail burns for twice as long at half the thrust, with the same isp and
        // delta-v.  scaling the thrust alone would halve the isp and the delta-v over the same burn time.
        [Fact]
        public void MultFlowScalesTheFuelFlow()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan();
            FirstEngine(v).MultFlow = 0.5;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7, 0);
            AssertBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, 0.5 * MAINSAIL_MAX_FUEL_FLOW, MAINSAIL_VACUUM_ISP, dirMagnitude);
            segments[0].MaxThrust.ShouldEqual(0.5 * MAINSAIL_MAX_FUEL_FLOW * MAINSAIL_VACUUM_ISP * G0 * dirMagnitude, 1e-6);
        }

        // with all of its thrust lost the Mainsail has no fuel flow, so it burns nothing
        [Fact]
        public void ZeroMultFlowBurnsNothing()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan();
            FirstEngine(v).MultFlow = 0;

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7, 0);
            AssertNoBurn(segments[0], 0, 79.7, 0);
        }

        // MechJeb2 issue #1781: a solid motor's throttle is locked at its thrust limiter, so at 50% the Sepratron burns for
        // twice as long at half the thrust, with the same delta-v, and its thrust can't go below that either.  the limiter
        // was reset to 100% when the engine was activated, which kept the burn time of the full thrust.
        [Theory]
        [InlineData(100f)]
        [InlineData(50f)]
        public void SepratronThrustLimiter(float throttleLimiter)
        {
            SimVessel v = SimVesselFixtures.SepratronTinCan(throttleLimiter);
            double maxFuelFlow = throttleLimiter / 100 * SEPRATRON_MAX_FUEL_FLOW;

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, SEPRATRON_TIN_CAN_MASS, SEPRATRON_SOLID_FUEL, maxFuelFlow, SEPRATRON_ISP, 1);
            segments[0].MaxThrust.ShouldEqual(maxFuelFlow * SEPRATRON_ISP * G0, 1e-6);
            segments[0].MinThrust.ShouldEqual(maxFuelFlow * SEPRATRON_ISP * G0, 1e-6);
        }

        // higher up, the Whiplash's ATMCurve gives it a flowMultiplier below 1, and it burns its fuel more slowly, at less
        // thrust and the same isp.  the mass flow already carries the flowMultiplier, so the thrust taking it again as well
        // would lower the isp with it.  at sea level the flowMultiplier is 1, which can't tell the two apart.
        [Fact]
        public void JetFlowMultiplierKeepsTheIsp()
        {
            SimVessel v = SimVesselFixtures.JetTinCan();
            // the ATMCurve takes the density over 49/40 kg/m³, here 0.3: inside its cubic segment from 0.16 to 0.5
            v.SetConditions(0.3 * 49 / 40, 0.3, 0);
            double flowMultiplier = Hermite(0.3, 0.16, 0.5, 0.5f, 0.5779132f, 0.6f, 0.4809403f);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, JET_TIN_CAN_MASS, ROCKOMAX64_LIQUID_FUEL, flowMultiplier * JET_MAX_FUEL_FLOW, JET_ISP, 1);
        }

        // RealFuels' autoCutoff shuts a Merlin's symmetry counterpart down with it when it flames out, so when pod B's tank
        // runs down to its residuals a second before pod A's, both Merlins stop and pod A's tank keeps that second of
        // propellant.  pod A's Merlin is already in the active engines when pod B's flames out, and has to be taken out again.
        // without autoCutoff pod A's Merlin would burn on alone, in a segment of its own, for that second.
        //
        // the tanks are large enough that the rounding leaves pod B's RP-1 or LOX a sliver over its residuals after the step
        // which should empty it, and the simulation steps at least 1 ms, so pod A's Merlin can burn 1 ms of its second
        // finishing that sliver off with pod B's.  the tolerances allow for that one step.
        [Fact]
        public void AutoCutoffShutsDownTheSymmetryCounterpart()
        {
            SimVessel v = SimVesselFixtures.MerlinPair(true, 1);
            double burnTime = MerlinPairBurnTime(1);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, MerlinPairMass(1), 2 * MERLIN_MAX_FUEL_FLOW * burnTime, MERLIN_MAX_FUEL_FLOW, MERLIN_VACUUM_ISP, 1,
                engines: 2, tolerance: 1e-4);

            SimPart tankA = v.Parts.Find(p => p.PersistentId == 2);
            (PropellantMass(tankA) - MERLIN_RESIDUALS * MERLIN_PAIR_PROPELLANT).ShouldEqual(MERLIN_MAX_FUEL_FLOW, 1e-3);
        }

        // without autoCutoff pod A's Merlin burns on alone for the second of propellant pod B's tank lacks.  the stage splits
        // into two segments of the same KSP stage, which the staging controller's hot staging adds up, and the second one has
        // no spoolup since its Merlin is already running.  as above, pod A's Merlin can burn 1 ms of its second alongside the
        // end of pod B's.
        [Fact]
        public void EngineBurningOnAloneSplitsTheStage()
        {
            SimVessel v = SimVesselFixtures.MerlinPair(false, 1);
            double burnTime = MerlinPairBurnTime(1);
            double startMass = MerlinPairMass(1);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertBurn(segments[1], 0, startMass, 2 * MERLIN_MAX_FUEL_FLOW * burnTime, MERLIN_MAX_FUEL_FLOW, MERLIN_VACUUM_ISP, 1,
                engines: 2, tolerance: 1e-4);
            segments[1].SpoolUpTime.ShouldEqual(MERLIN_SPOOLUP_TIME, 1e-12);

            // pod A's Merlin burns its tank down to its residuals
            FuelStats s = segments[0];
            s.KSPStage.ShouldEqual(0);
            s.StagedMass.ShouldBeZero();
            s.StartMass.ShouldEqual(segments[1].EndMass);
            s.EndMass.ShouldEqual(startMass - MERLIN_MAX_FUEL_FLOW * (2 * burnTime + 1), 1e-6);
            s.DeltaTime.ShouldEqual(1, 1e-3);
            s.Thrust.ShouldEqual(MERLIN_MAX_FUEL_FLOW * MERLIN_VACUUM_ISP * G0, 1e-6);
            s.Isp.ShouldEqual(MERLIN_VACUUM_ISP, 1e-6);
            s.SpoolUpTime.ShouldBeZero();
        }

        // pod B's tank lacks 10 ms of propellant: a segment that short isn't split off, it goes into the segment of the two
        // Merlins.  splitting the stage at every change of thrust made segments of next to no time or delta-v.
        [Fact]
        public void NoSegmentForAShortBurnOn()
        {
            const double DEFICIT = 0.01;
            SimVessel v = SimVesselFixtures.MerlinPair(false, DEFICIT);
            double burnTime = MerlinPairBurnTime(DEFICIT);
            double startMass = MerlinPairMass(DEFICIT);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            FuelStats s = segments[0];
            s.KSPStage.ShouldEqual(0);
            s.StartMass.ShouldEqual(startMass, 1e-6);
            s.EndMass.ShouldEqual(startMass - MERLIN_MAX_FUEL_FLOW * (2 * burnTime + DEFICIT), 1e-6);
            s.DeltaTime.ShouldEqual(burnTime + DEFICIT, 1e-6);
            s.Thrust.ShouldEqual(2 * MERLIN_MAX_FUEL_FLOW * MERLIN_VACUUM_ISP * G0, 1e-6);
        }

        // the Puff and the RV-105 burn the same monopropellant.  at the start of the stage the RCS has the whole tank, but the
        // Puff burns it all, leaving the RCS none.  the RCS propellant and time are those of the whole tank: taking them from
        // the end of the stage left them at 0.
        [Fact]
        public void RcsSharingAnEnginesTank()
        {
            SimVessel v = SimVesselFixtures.PuffTinCan();
            const double START_MASS = PUFF_TIN_CAN_DRY_MASS + RCS_TANK_MONOPROPELLANT;

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            FuelStats s = segments[0];
            AssertBurn(s, 0, START_MASS, RCS_TANK_MONOPROPELLANT, PUFF_MAX_FUEL_FLOW, PUFF_ISP, 1);
            s.RcsThrust.ShouldEqual(1, 1e-6);
            s.RcsISP.ShouldEqual(RCS_BLOCK_ISP, 1e-6);
            s.MinRcsDeltaV.ShouldEqual(RcsDeltaV(START_MASS, RCS_TANK_MONOPROPELLANT), 1e-6);
            s.RcsMass.ShouldEqual(RCS_TANK_MONOPROPELLANT, 1e-6);
            s.RcsDeltaTime.ShouldEqual(RCS_TANK_MONOPROPELLANT / RCS_BLOCK_MAX_FUEL_FLOW, 1e-6);
            s.MaxRcsDeltaV.ShouldBeZero();
        }

        // with residuals, as RealFuels gives its engines, the Puff stops with 1% of the tank left.  the RCS has no residuals of
        // its own and burns that, so it has some delta-v left at the end of the stage.  the engine's residuals must be cleared
        // for that: the RCS can't burn below them.
        [Fact]
        public void RcsBurnsTheResidualsAnEngineLeaves()
        {
            const double RESIDUALS = 0.01;
            SimVessel v = SimVesselFixtures.PuffTinCan(RESIDUALS);
            const double START_MASS = PUFF_TIN_CAN_DRY_MASS + RCS_TANK_MONOPROPELLANT;
            const double END_MASS = START_MASS - (1 - RESIDUALS) * RCS_TANK_MONOPROPELLANT;

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            FuelStats s = segments[0];
            AssertBurn(s, 0, START_MASS, (1 - RESIDUALS) * RCS_TANK_MONOPROPELLANT, PUFF_MAX_FUEL_FLOW, PUFF_ISP, 1);
            s.MinRcsDeltaV.ShouldEqual(RcsDeltaV(START_MASS, RCS_TANK_MONOPROPELLANT), 1e-6);
            s.RcsMass.ShouldEqual(RCS_TANK_MONOPROPELLANT, 1e-6);
            s.MaxRcsDeltaV.ShouldEqual(RcsDeltaV(END_MASS, RESIDUALS * RCS_TANK_MONOPROPELLANT), 1e-6);

            // the RCS calculation puts back what it burns
            SimPart tank = v.Parts.Find(p => p.PersistentId == 2);
            tank.Resources[MONOPROPELLANT].Amount.ShouldEqual(RESIDUALS * 750, 1e-9);
        }

        // the Puff burns the drop tank down to its residuals and the vessel stages it with the Puff still running, now on the
        // core tank, which holds the Puff's residuals.  ModuleRCS has none, so they are cleared for the RCS delta-v at the end
        // of the stage, which burns the drop tank's residuals and the whole core tank.  left on, they would stop the RCS short
        // of the last 1% of the core tank.
        [Fact]
        public void RcsAtStagingIgnoresTheResidualsOfARunningEngine()
        {
            const double RESIDUALS = 0.01;
            SimVessel v = SimVesselFixtures.PuffTinCanDropTank(RESIDUALS);
            // the core, and the radial decoupler (0.05) with the drop tank
            const double START_MASS = PUFF_TIN_CAN_DRY_MASS + RCS_TANK_MONOPROPELLANT + 0.45 + RCS_TANK_MONOPROPELLANT;
            const double BURNED = (1 - RESIDUALS) * RCS_TANK_MONOPROPELLANT;
            const double STAGING_MASS = START_MASS - BURNED;
            // the drop tank goes with its residuals
            const double STAGED_MASS = 0.45 + RESIDUALS * RCS_TANK_MONOPROPELLANT;

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertBurn(segments[1], 1, START_MASS, BURNED, PUFF_MAX_FUEL_FLOW, PUFF_ISP, 1);
            segments[1].MinRcsDeltaV.ShouldEqual(RcsDeltaV(START_MASS, 2 * RCS_TANK_MONOPROPELLANT), 1e-6);
            segments[1].MaxRcsDeltaV.ShouldEqual(RcsDeltaV(STAGING_MASS, (1 + RESIDUALS) * RCS_TANK_MONOPROPELLANT), 1e-6);

            // the Puff burns the core tank down to its own residuals
            AssertBurn(segments[0], 0, STAGING_MASS - STAGED_MASS, BURNED, PUFF_MAX_FUEL_FLOW, PUFF_ISP, 1, STAGED_MASS);
            segments[0].MinRcsDeltaV.ShouldEqual(RcsDeltaV(STAGING_MASS - STAGED_MASS, RCS_TANK_MONOPROPELLANT), 1e-6);
            segments[0].MaxRcsDeltaV.ShouldEqual(RcsDeltaV(STAGING_MASS - STAGED_MASS - BURNED, RESIDUALS * RCS_TANK_MONOPROPELLANT), 1e-6);
        }

        // MechJeb2 issue #1919: the decoupler drops the RCS tank, so after staging neither the RCS block nor the Puff above it
        // can reach any monopropellant.  stage priority flow has to look at the parts still on the vessel, not every part:
        // the RCS spun until it hit the step limit, and the Puff counted as running with nothing to burn.
        [Fact]
        public void RcsAndEngineLoseTheirDecoupledTank()
        {
            SimVessel v = SimVesselFixtures.RcsOverDecoupledTank();
            // the probe core (0.5), the RV-105 (0.04) and the Puff (0.09)
            const double UPPER_MASS = 0.63;
            // the decoupler (0.16) and the FL-R750
            const double LOWER_MASS = 0.56 + RCS_TANK_MONOPROPELLANT;

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, UPPER_MASS + LOWER_MASS, 0);
            segments[1].MinRcsDeltaV.ShouldEqual(RcsDeltaV(UPPER_MASS + LOWER_MASS, RCS_TANK_MONOPROPELLANT), 1e-6);
            segments[1].MaxRcsDeltaV.ShouldEqual(RcsDeltaV(UPPER_MASS + LOWER_MASS, RCS_TANK_MONOPROPELLANT), 1e-6);

            AssertNoBurn(segments[0], 0, UPPER_MASS, LOWER_MASS);
            segments[0].RcsThrust.ShouldBeZero();
            segments[0].MinRcsDeltaV.ShouldBeZero();
            segments[0].MaxRcsDeltaV.ShouldBeZero();
        }

        // the DecouplingAnalyzer starts from the root part, wherever it is in the list: from the lower Mainsail the decoupler
        // would drop the upper stage instead of the lower one
        [Fact]
        public void RootPartAnywhereInTheList()
        {
            List<FuelStats> expected = Run(SimVesselFixtures.MainsailTinCan2Stage());
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage();
            SimVesselFixtures.MovePartToFront(v, v.Parts.Find(p => p.PersistentId == 296107038));

            List<FuelStats> segments = Run(v);

            Assert.Equal(expected.Count, segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].KSPStage.ShouldEqual(expected[i].KSPStage);
                segments[i].StartMass.ShouldEqual(expected[i].StartMass, 1e-9);
                segments[i].EndMass.ShouldEqual(expected[i].EndMass, 1e-9);
                segments[i].DeltaV.ShouldEqual(expected[i].DeltaV, 1e-9);
            }
        }

        // MechJeb2 PR #1870: an engine with no ignitions left has no delta-v, the upper stage keeps its propellant
        [Fact]
        public void UnrestartableDeadEngineBurnsNothing()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage();
            ((SimModuleEngines)v.Parts.Find(p => p.PersistentId == 1899096253).Modules[0]).IsUnrestartableDeadEngine = true;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(5, segments.Count);
            AssertNoBurn(segments[4], 4, 85.86, 0);
            AssertNoBurn(segments[3], 3, 85.86, 0, MAINSAIL_MAX_FUEL_FLOW * MAINSAIL_VACUUM_ISP * G0 * dirMagnitude);
            AssertMainsailBurn(segments[2], 2, 85.86, ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude);
            AssertNoBurn(segments[1], 1, 43.7, 10.16);
            AssertNoBurn(segments[0], 0, 43.7, 0);
        }

        // an engine which leaves its maxFuelFlow at zero gets the fuel flow of its maxThrust at its vacuum isp, as KSP's
        // ModuleEngines.OnLoad would have given it.  the burn used that, but the segment's maximum thrust took the zero.
        [Fact]
        public void ZeroMaxFuelFlowFallsBackToMaxThrust()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan();
            FirstEngine(v).MaxFuelFlow = 0;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7, 0);
            AssertMainsailBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude);
            segments[0].MaxThrust.ShouldEqual(MAINSAIL_MAX_FUEL_FLOW * MAINSAIL_VACUUM_ISP * G0 * dirMagnitude, 1e-6);
            segments[0].MinThrust.ShouldBeZero();
        }

        // MechJeb2 bfee87fd and 7369cce3: an engine with a minimum thrust, as RealFuels engines have, throttles between its
        // minimum and its maximum, and the thrust limiter scales the throttle.  so at 50% the Merlins burn halfway between
        // their minimum and maximum flows, and at 0% at their minimum, and their thrust can't go below the minimum.  as in the
        // AutoCutoff test, the rounding can leave a tank a sliver over its residuals, which takes one more step of 1 ms.
        [Theory]
        [InlineData(50f)]
        [InlineData(0f)]
        public void MerlinThrustLimiter(float throttleLimiter)
        {
            SimVessel v = SimVesselFixtures.MerlinPair(false, 0);
            foreach (SimPart merlin in v.Parts.FindAll(p => p.IsEngine))
                ((SimModuleEngines)merlin.Modules[0]).ThrottleLimiter = throttleLimiter;
            double flow = MERLIN_MIN_FUEL_FLOW + throttleLimiter / 100 * (MERLIN_MAX_FUEL_FLOW - MERLIN_MIN_FUEL_FLOW);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, MerlinPairMass(0), 2 * (1 - MERLIN_RESIDUALS) * MERLIN_PAIR_PROPELLANT, flow, MERLIN_VACUUM_ISP,
                1, engines: 2, tolerance: 1e-4);
            segments[0].MaxThrust.ShouldEqual(2 * flow * MERLIN_VACUUM_ISP * G0, 1e-6);
            segments[0].MinThrust.ShouldEqual(2 * MERLIN_MIN_FUEL_FLOW * MERLIN_VACUUM_ISP * G0, 1e-6);
        }

        // MechJeb2 efde192d and 4ad4ecd9: canted engines push the vessel along with only the cosine of their thrust, their
        // sideways thrusts cancel out.  the "include cosine losses" option (DVLinearThrust) gives the delta-v of what is left,
        // without it the delta-v is that of the engines' whole thrust.  the maximum thrust is the vessel's either way.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CantedEnginesCosineLosses(bool dvLinearThrust)
        {
            const double CANT = 20 * Math.PI / 180;
            SimVessel v = SimVesselFixtures.CantedMainsailPair(CANT);
            double effectiveIsp = MAINSAIL_VACUUM_ISP * (dvLinearThrust ? Math.Cos(CANT) : 1);

            List<FuelStats> segments = Run(v, dvLinearThrust);

            Assert.Single(segments);
            AssertMainsailBurn(segments[0], 0, 0.5 + 4 + ROCKOMAX64_PROPELLANT + 2 * MAINSAIL_MASS, ROCKOMAX64_PROPELLANT, effectiveIsp, 1,
                engines: 2);
            segments[0].MaxThrust.ShouldEqual(2 * MAINSAIL_MAX_FUEL_FLOW * MAINSAIL_VACUUM_ISP * G0 * Math.Cos(CANT), 1e-6);
        }

        // MechJeb2 57aa35dd: KSP splits an engine's thrust between its nozzles by its thrustTransformMultipliers.  here the
        // Mainsail's thrust is split three to one between two nozzles canted 30° either way, so its thrust is the length of
        // the weighted sum of their directions.
        [Fact]
        public void ThrustTransformMultipliersWeightTheNozzles()
        {
            const double CANT = 30 * Math.PI / 180;
            var up = new V3(Math.Cos(CANT), Math.Sin(CANT), 0);
            var down = new V3(Math.Cos(CANT), -Math.Sin(CANT), 0);
            SimVessel v = SimVesselFixtures.MainsailTinCan();
            SimModuleEngines e = FirstEngine(v);
            e.ThrustDirectionVectors.Clear();
            e.ThrustTransformMultipliers.Clear();
            e.ThrustDirectionVectors.Add(up);
            e.ThrustTransformMultipliers.Add(0.75);
            e.ThrustDirectionVectors.Add(down);
            e.ThrustTransformMultipliers.Add(0.25);
            double effectiveIsp = MAINSAIL_VACUUM_ISP * (0.75 * up + 0.25 * down).magnitude;

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7, 0);
            AssertMainsailBurn(segments[0], 0, 79.7, 2 * ROCKOMAX64_PROPELLANT, effectiveIsp, 1);
            segments[0].MaxThrust.ShouldEqual(MAINSAIL_MAX_FUEL_FLOW * effectiveIsp * G0, 1e-6);
        }

        // MechJeb2 940db6f0: a stage which decouples nothing and only lights engines doesn't stage while an engine is still
        // burning.  so the ion probe burns its xenon on the ion engine alone, and only then lights the Sepratrons it brakes
        // with.  staging as soon as the ion engine was lit gave the ion stage the thrust of the Sepratrons, and the
        // maneuver nodes the burn times of that.
        [Fact]
        public void IgnitionOnlyStageWaitsForBurnout()
        {
            SimVessel v = SimVesselFixtures.IonProbeWithBrakingSepratrons();
            double startMass = ION_TIN_CAN_MASS + XENON_TANK_MASS + 2 * (0.0125 + SEPRATRON_SOLID_FUEL);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertBurn(segments[1], 1, startMass, XENON_TANK_XENON, ION_MAX_FUEL_FLOW, ION_ISP, 1);
            AssertBurn(segments[0], 0, startMass - XENON_TANK_XENON, 2 * SEPRATRON_SOLID_FUEL, SEPRATRON_MAX_FUEL_FLOW, SEPRATRON_ISP,
                1, engines: 2);
        }

        // MechJeb2 issue #747: the mass of a fairing's panels stays on the vessel until the fairing is staged.  the stock
        // fairing jettisons its panels and keeps its base, so staging it drops no part, and the vessel only stages it once the
        // Mainsail has burned out: the panels ride through the burn.
        [Fact]
        public void StockFairingPanelsCarriedUntilTheirStage()
        {
            const double PANEL_MASS = 0.4;
            SimVessel v = SimVesselFixtures.FairingTinCan(PANEL_MASS);
            // the probe core, the fairing base, the empty Rockomax64 and the Mainsail
            const double DRY_MASS = 0.5 + 0.175 + 4 + MAINSAIL_MASS;

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertMainsailBurn(segments[1], 1, DRY_MASS + PANEL_MASS + ROCKOMAX64_PROPELLANT, ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP,
                1);
            AssertNoBurn(segments[0], 0, DRY_MASS, PANEL_MASS);
        }

        // MechJeb2 003968b6 and 82b34265: the crew, and the resources whose flow is disabled, are dead weight.  here the upper
        // tank's oxidizer is locked, so the Mainsail burns the oxidizer of the lower tank alone, with as much liquid fuel, and
        // carries the rest.  the probe core carries the mass of a crew, as a pod would.
        [Fact]
        public void CrewAndDisabledResourcesAreDeadWeight()
        {
            const double CREW_MASS = 0.1;
            SimVessel v = SimVesselFixtures.MainsailTinCan();
            v.Parts.Find(p => p.PersistentId == 2919945081).CrewMass = CREW_MASS;
            // as SimVesselUpdater.UpdateResources leaves a resource with its flowState off
            SimPart upperTank = v.Parts.Find(p => p.PersistentId == 706255026);
            upperTank.Resources.Remove(OXIDIZER);
            upperTank.DisabledResourcesMass = 3520 * OXIDIZER_DENSITY;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(2, segments.Count);
            AssertNoBurn(segments[1], 1, 79.7 + CREW_MASS, 0);
            AssertMainsailBurn(segments[0], 0, 79.7 + CREW_MASS, ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude);
        }

        // MechJeb2 b765f493: a RealismOverhaul ullage motor can burn out with a little more propellant left than the
        // simulation takes for empty.  KSP has flamed it out with "No propellants", so it has no burn.
        [Fact]
        public void NoPropellantsEngineBurnsNothing()
        {
            const double LEFT_OVER = 0.08;
            SimVessel v = SimVesselFixtures.SepratronTinCan(100);
            SimPart sepratron = v.Parts.Find(p => p.IsEngine);
            AddSolidFuel(sepratron, LEFT_OVER, 8);
            ((SimModuleEngines)sepratron.Modules[0]).NoPropellants = true;

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertNoBurn(segments[0], 0, SEPRATRON_TIN_CAN_MASS - SEPRATRON_SOLID_FUEL + LEFT_OVER * SOLID_FUEL_DENSITY, 0);
        }

        // MechJeb2 c3abe3a8: the simulation takes every enabled engine module of a part.  a multi-mode engine switches mode by
        // enabling one module and disabling the other, so the RAPIER in its AirBreathing mode burns the liquid fuel alone, as
        // the Whiplash does, and its ClosedCycle mode burns none of the oxidizer.
        [Fact]
        public void MultiModeEngineBurnsOnlyItsEnabledMode()
        {
            SimVessel v = SimVesselFixtures.RapierTinCan();
            v.SetConditions(SEA_LEVEL_DENSITY, 1, 0);

            List<FuelStats> segments = Run(v);

            Assert.Single(segments);
            AssertBurn(segments[0], 0, RAPIER_TIN_CAN_MASS, ROCKOMAX64_LIQUID_FUEL, RAPIER_AIR_MAX_FUEL_FLOW, RAPIER_AIR_ISP, 1);
        }

        // MechJeb2 issue #278: an engine lit by hand before the vessel has staged burns in the current stage.  here the lower
        // Mainsail of the MainsailTinCan2Stage is lit on the pad, while the vessel is in stage 4.  stage 3 would only light
        // it, so the vessel doesn't stage until it burns out, and its burn falls in stage 4.
        [Fact]
        public void EngineLitBeforeItsStageBurnsInTheCurrentStage()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage();
            ((SimModuleEngines)v.Parts.Find(p => p.PersistentId == 296107038).Modules[0]).IsOperational = true;
            double dirMagnitude = ThrustDirectionMagnitude(v);

            List<FuelStats> segments = Run(v);

            Assert.Equal(5, segments.Count);
            AssertMainsailBurn(segments[4], 4, 85.86, ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude);
            AssertNoBurn(segments[3], 3, 53.86, 0);
            AssertNoBurn(segments[2], 2, 53.86, 0);
            // the decoupler, the empty lower tank and the lower Mainsail
            AssertNoBurn(segments[1], 1, 43.7, 10.16);
            AssertMainsailBurn(segments[0], 0, 43.7, ROCKOMAX64_PROPELLANT, MAINSAIL_VACUUM_ISP, dirMagnitude);
        }

        private static SimModuleEngines FirstEngine(SimVessel v) => (SimModuleEngines)v.Parts.Find(p => p.IsEngine).Modules[0];

        // the mass of the resources in the part
        private static double PropellantMass(SimPart part)
        {
            double mass = 0;
            foreach (SimResource resource in part.Resources.Values)
                mass += resource.Amount * resource.Density;
            return mass;
        }

        // the delta-v of the RV-105 burning propellant from a vessel of mass, with the engines off
        private static double RcsDeltaV(double mass, double propellant) => RCS_BLOCK_ISP * G0 * Math.Log(mass / (mass - propellant));

        // the MerlinPair on the pad, pod B's tank short of deficitSeconds of the Merlin's flow: the probe core (0.5), and the
        // tanks with their Merlins
        private static double MerlinPairMass(double deficitSeconds) =>
            0.5 + 2 * (5 + MERLIN_MASS + MERLIN_PAIR_PROPELLANT) - deficitSeconds * MERLIN_MAX_FUEL_FLOW;

        // how long the two Merlins of the MerlinPair burn together: until pod B's tank is down to its residuals
        private static double MerlinPairBurnTime(double deficitSeconds) =>
            (1 - MERLIN_RESIDUALS) * SimVesselFixtures.MERLIN_PAIR_KEROLOX / SimVesselFixtures.MERLIN_KEROLOX_FLOW - deficitSeconds;

        private static List<FuelStats> Run(SimVessel v, bool dvLinearThrust = true)
        {
            var sim = new FuelFlowSimulation { DVLinearThrust = dvLinearThrust };
            sim.Run(v);

            foreach (FuelStats s in sim.Segments)
                Print(s.ToString());

            return sim.Segments;
        }

        private static string CapturedDump(string fileName, bool kscPad) =>
            Captures.Load(fileName).Find(dump => Captures.IsVacuum(dump) != kscPad) ??
            throw new Exception($"{fileName} has no {(kscPad ? "KSC pad" : "vacuum")} dump");

        // the captures hold a dump in vacuum and a dump on the launchpad at KSC, in either order
        private static void AssertMatchesCapture(string fileName, Func<bool, SimVessel> fixture)
        {
            List<string> dumps = Captures.Load(fileName);
            Assert.Equal(2, dumps.Count);
            Assert.NotEqual(Captures.IsVacuum(dumps[0]), Captures.IsVacuum(dumps[1]));

            foreach (string dump in dumps)
                AssertMatchesDump(dump, fixture(!Captures.IsVacuum(dump)));
        }

        private static void AssertMatchesDump(string dump, SimVessel v)
        {
            string actual = v.ToString();

            if (Captures.Normalize(actual) != Captures.Normalize(dump))
            {
                Print($"captured:\n{dump}");
                Print($"fixture:\n{actual}");
            }

            Assert.Equal(Captures.Normalize(dump), Captures.Normalize(actual));
        }

        // SimVesselManager.Update() does not refresh SimPart.Mass, so the masses in the capture are what KSP's own run of the
        // FuelFlowSimulation left behind, down to the slivers of propellant it strands in the tanks.
        private static void AssertMassesMatchCapture(SimVessel v, string fileName, bool kscPad)
        {
            Dictionary<string, double> masses = Captures.PartMasses(CapturedDump(fileName, kscPad));
            Assert.Equal(masses.Count, v.Parts.Count);

            List<string> mismatches = v.Parts.FindAll(p => !NearlyEqual(p.Mass, masses[p.Ident], 1e-14))
               .ConvertAll(p => $"{p.Ident}: Mass={p.Mass:R} captured={masses[p.Ident]:R}");

            Assert.Empty(mismatches);
        }

        // a segment which stages without burning anything, after stagedMass was dropped by the previous staging event
        private static void AssertNoBurn(FuelStats s, int kspStage, double mass, double stagedMass, double thrust = 0)
        {
            s.KSPStage.ShouldEqual(kspStage);
            s.DeltaV.ShouldBeZero();
            s.DeltaTime.ShouldBeZero();
            s.StartMass.ShouldEqual(mass, 1e-6);
            s.EndMass.ShouldEqual(mass, 1e-6);
            s.StagedMass.ShouldEqual(stagedMass, 1e-6);
            s.Thrust.ShouldEqual(thrust, 1e-6);
        }

        // Mainsails burning the propellant to empty.  the propellant ratios are single precision, so the oxidizer runs out
        // with a sliver of liquid fuel left over.
        private static void AssertMainsailBurn(FuelStats s, int kspStage, double startMass, double propellant, double isp,
            double dirMagnitude, double stagedMass = 0, int engines = 1, double tolerance = 1e-6) =>
            AssertBurn(s, kspStage, startMass, propellant, MAINSAIL_MAX_FUEL_FLOW, isp, dirMagnitude, stagedMass, engines, tolerance);

        // engines burning the propellant at full throttle, their fuel flow does not depend on the atmosphere so only the
        // thrust, isp and delta-v change with the atmospheric pressure.  the maxFuelFlow and the rated isp are for the
        // propellants that count for the isp, ignoredForIsp is the mass of the ignoreForIsp propellants burned per unit mass
        // of those: it adds to the flow but not to the thrust, which lowers the isp of the whole flow.
        private static void AssertBurn(FuelStats s, int kspStage, double startMass, double propellant, double maxFuelFlow, double isp,
            double dirMagnitude, double stagedMass = 0, int engines = 1, double tolerance = 1e-6, double ignoredForIsp = 0)
        {
            double endMass = startMass - propellant;
            double effectiveIsp = isp / (1 + ignoredForIsp);

            s.KSPStage.ShouldEqual(kspStage);
            s.StagedMass.ShouldEqual(stagedMass, tolerance);
            s.StartMass.ShouldEqual(startMass, tolerance);
            s.EndMass.ShouldEqual(endMass, tolerance);
            s.DeltaTime.ShouldEqual(propellant / (engines * maxFuelFlow * (1 + ignoredForIsp)), tolerance);
            s.Thrust.ShouldEqual(engines * maxFuelFlow * isp * G0 * dirMagnitude, tolerance);
            s.Isp.ShouldEqual(effectiveIsp, tolerance);
            s.DeltaV.ShouldEqual(effectiveIsp * G0 * Math.Log(startMass / endMass), tolerance);
        }

        // the captured thrust directions are unit vectors rotated in single precision, so they are only nearly unit length.
        // the engines of a vessel point the same way to within single precision, so the first engine stands in for them all.
        private static double ThrustDirectionMagnitude(SimVessel v)
        {
            SimPart engine = v.Parts.Find(p => p.IsEngine);
            return ((SimModuleEngines)engine.Modules[0]).ThrustDirectionVectors[0].magnitude;
        }

        // This should replicate the MainsailISP FloatCurve both within its range and outside of it.
        // The constant value below and above the range should duplicate actual in-game behavior.
        private static double MainsailIsp(double p)
        {
            if (p <= 0)
                return 310f;
            if (p <= 1)
                return Hermite(p, 0, 1, 310f, -25f, 285f, -30.3124370574951f);
            if (p <= 9)
                return Hermite(p, 1, 9, 285f, -30.3124370574951f, 0.001f, -35.6248741149902f);
            return 0.001f;
        }

        // the cubic Hermite between keys (t0, y0) and (t1, y1) with slopes m0 and m1, in the Hermite basis
        private static double Hermite(double t, double t0, double t1, double y0, double m0, double y1, double m1)
        {
            double h = t1 - t0;
            double s = (t - t0) / h;
            double s2 = s * s;
            double s3 = s2 * s;

            return (2 * s3 - 3 * s2 + 1) * y0 + (s3 - 2 * s2 + s) * h * m0 + (-2 * s3 + 3 * s2) * y1 + (s3 - s2) * h * m1;
        }
    }
}
