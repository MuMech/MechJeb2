/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class FuelFlowSimulationTests
    {
        private const double MAINSAIL_MAX_FUEL_FLOW = 0.4934111f;
        private const double MAINSAIL_VACUUM_ISP    = 310;

        // the mass of the liquid fuel and oxidizer in a full Rockomax64
        private const double ROCKOMAX64_PROPELLANT = 32;

        [Fact]
        public void MainsailTinCanMatchesCapture() => AssertMatchesCapture("MainsailTinCan.out", SimVesselFixtures.MainsailTinCan);

        [Fact]
        public void MainsailTinCan2StageMatchesCapture() =>
            AssertMatchesCapture("MainsailTinCan2Stage.out", SimVesselFixtures.MainsailTinCan2Stage);

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
            AssertTankResidue(v);
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
            AssertTankResidue(v);
        }

        private static List<FuelStats> Run(SimVessel v)
        {
            var sim = new FuelFlowSimulation();
            sim.Run(v);

            foreach (FuelStats s in sim.Segments)
                Print(s.ToString());

            return sim.Segments;
        }

        // the captures hold a dump in vacuum followed by a dump on the launchpad at KSC
        private static void AssertMatchesCapture(string fileName, Func<bool, SimVessel> fixture)
        {
            List<string> dumps = Captures.Load(fileName);
            Assert.Equal(2, dumps.Count);

            AssertMatchesDump(dumps[0], fixture(false));
            AssertMatchesDump(dumps[1], fixture(true));
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

        // a single Mainsail burning the propellant to empty, its fuel flow does not depend on the atmosphere so only the
        // thrust, isp and delta-v change with the atmospheric pressure.  the propellant ratios are single precision, so the
        // oxidizer runs out with a sliver of liquid fuel left over.
        private static void AssertMainsailBurn(FuelStats s, int kspStage, double startMass, double propellant, double isp,
            double dirMagnitude, double stagedMass = 0)
        {
            double endMass = startMass - propellant;

            s.KSPStage.ShouldEqual(kspStage);
            s.StagedMass.ShouldEqual(stagedMass, 1e-6);
            s.StartMass.ShouldEqual(startMass, 1e-6);
            s.EndMass.ShouldEqual(endMass, 1e-6);
            s.DeltaTime.ShouldEqual(propellant / MAINSAIL_MAX_FUEL_FLOW, 1e-6);
            s.Thrust.ShouldEqual(MAINSAIL_MAX_FUEL_FLOW * isp * G0 * dirMagnitude, 1e-6);
            s.Isp.ShouldEqual(isp, 1e-6);
            s.DeltaV.ShouldEqual(isp * G0 * Math.Log(startMass / endMass), 1e-6);
        }

        // KSP's own simulation of these vessels left the same sliver in each tank, which is the stale SimPart.Mass of the
        // tanks in the captures
        private static void AssertTankResidue(SimVessel v)
        {
            List<SimPart> tanks = v.Parts.FindAll(p => p.Name == "Rockomax64.BW");
            Assert.NotEmpty(tanks);

            foreach (SimPart tank in tanks)
                tank.Mass.ShouldEqual(4.00000069358129, 1e-14);
        }

        // the captured thrust directions are unit vectors rotated in single precision, so they are only nearly unit length
        private static double ThrustDirectionMagnitude(SimVessel v) => v.EnginesActivatedInStage[0][0].ThrustDirectionVectors[0].magnitude;

        // the Mainsail atmosphereCurve between its 0 atm and 1 atm keys, worked out independently of the DoubleInterpolant
        private static double MainsailIsp(double p)
        {
            double p2 = p * p;
            double p3 = p2 * p;

            return (2 * p3 - 3 * p2 + 1) * 310 + (p3 - 2 * p2 + p) * -25 + (-2 * p3 + 3 * p2) * 285 + (p3 - p2) * -30.3124370574951f;
        }
    }
}
