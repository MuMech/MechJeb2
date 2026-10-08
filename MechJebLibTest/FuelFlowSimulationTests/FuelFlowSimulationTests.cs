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

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class FuelFlowSimulationTests
    {
        private const double MAINSAIL_MAX_FUEL_FLOW = 1500f / (310f * 9.80665f);
        private const double MAINSAIL_VACUUM_ISP    = 310;

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

        // Mainsails burning the propellant to empty, their fuel flow does not depend on the atmosphere so only the thrust,
        // isp and delta-v change with the atmospheric pressure.  the propellant ratios are single precision, so the oxidizer
        // runs out with a sliver of liquid fuel left over.
        private static void AssertMainsailBurn(FuelStats s, int kspStage, double startMass, double propellant, double isp,
            double dirMagnitude, double stagedMass = 0, int engines = 1, double tolerance = 1e-6)
        {
            double endMass = startMass - propellant;

            s.KSPStage.ShouldEqual(kspStage);
            s.StagedMass.ShouldEqual(stagedMass, tolerance);
            s.StartMass.ShouldEqual(startMass, tolerance);
            s.EndMass.ShouldEqual(endMass, tolerance);
            s.DeltaTime.ShouldEqual(propellant / (engines * MAINSAIL_MAX_FUEL_FLOW), tolerance);
            s.Thrust.ShouldEqual(engines * MAINSAIL_MAX_FUEL_FLOW * isp * G0 * dirMagnitude, tolerance);
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

        // the Mainsail atmosphereCurve between its 0 atm and 1 atm keys, worked out independently of the DoubleInterpolant
        private static double MainsailIsp(double p)
        {
            double p2 = p * p;
            double p3 = p2 * p;

            return (2 * p3 - 3 * p2 + 1) * 310 + (p3 - 2 * p2 + p) * -25 + (-2 * p3 + 3 * p2) * 285 + (p3 - p2) * -30.3124370574951f;
        }
    }
}
