/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.FuelFlowSimulation.PartModules;
using Xunit;
using static MechJebLib.Utils.Statics;
using static MechJebLibTest.FuelFlowSimulationTests.ResourceFixtures;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class FuelFlowSimulationTests
    {
        private const double MAINSAIL_MAX_FUEL_FLOW = 1500f / (310f * 9.80665f);
        private const double MAINSAIL_VACUUM_ISP = 310;

        // the mass of the liquid fuel and oxidizer in a full Rockomax64
        private const double ROCKOMAX64_PROPELLANT = 32;

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

            const float MERLIN_MAX_FUEL_FLOW = 0.29972443f;
            const float MVAC_MAX_FUEL_FLOW = 0.273717612f;
            const double RESIDUALS = 0.0094669296351484397;
            const double PAD_MASS = 557.28197016018203;
            const double UPPER_STACK_MASS = 122.56389040936439;
            const double PAYLOAD_FAIRING_MASS = 2 * 0.82329875230789185;
            // the lead ballast payload and the upper stage avionics
            double payloadMass = 1 - 0.91552984714508057 + 659.92299999999966 * LEAD_BALLAST_DENSITY + 1 - 0.70491535891778767;
            double firstStagePropellant = (1 - RESIDUALS) *
                (149242.99723951373 * COOLED_RP1_DENSITY + 235701.53739390321 * COOLED_LQD_OXYGEN_DENSITY);
            double upperStagePropellant = (1 - RESIDUALS) *
                (38750.520237705852 * COOLED_RP1_DENSITY + 61199.234562294143 * COOLED_LQD_OXYGEN_DENSITY);
            double nitrogen = (4 * 44323.939999999995 + 4 * 200000) * NITROGEN_DENSITY;

            List<FuelStats> segments = Run(v);

            Assert.Equal(6, segments.Count);

            // the first stage's avionics can control 600t, the upper stage's 150t
            for (int i = 0; i < segments.Count; i++)
                segments[i].ControllableMass.ShouldEqual(i >= 3 ? 600 : 150);

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

        private static List<FuelStats> Run(SimVessel v)
        {
            var sim = new FuelFlowSimulation();
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
        // thrust, isp and delta-v change with the atmospheric pressure.
        private static void AssertBurn(FuelStats s, int kspStage, double startMass, double propellant, double maxFuelFlow, double isp,
            double dirMagnitude, double stagedMass = 0, int engines = 1, double tolerance = 1e-6)
        {
            double endMass = startMass - propellant;

            s.KSPStage.ShouldEqual(kspStage);
            s.StagedMass.ShouldEqual(stagedMass, tolerance);
            s.StartMass.ShouldEqual(startMass, tolerance);
            s.EndMass.ShouldEqual(endMass, tolerance);
            s.DeltaTime.ShouldEqual(propellant / (engines * maxFuelFlow), tolerance);
            s.Thrust.ShouldEqual(engines * maxFuelFlow * isp * G0 * dirMagnitude, tolerance);
            s.Isp.ShouldEqual(isp, tolerance);
            s.DeltaV.ShouldEqual(isp * G0 * Math.Log(startMass / endMass), tolerance);
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
