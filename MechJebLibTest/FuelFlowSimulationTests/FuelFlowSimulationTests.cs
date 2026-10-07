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

        // the masses of the MainsailTinCan with full and empty tanks
        private const double WET_MASS = 79.7;
        private const double DRY_MASS = 15.7;

        [Fact]
        public void MainsailTinCanMatchesCapture()
        {
            List<string> dumps = Captures.Load("MainsailTinCan.out");
            Assert.Equal(2, dumps.Count);

            AssertMatchesCapture(dumps[0], SimVesselFixtures.MainsailTinCan());

            SimVessel atmo = SimVesselFixtures.MainsailTinCan();
            SimVesselFixtures.SetKSCPadConditions(atmo);
            AssertMatchesCapture(dumps[1], atmo);
        }

        [Fact]
        public void MainsailTinCanVacuum()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan();

            AssertMainsailTinCanBurn(v, MAINSAIL_VACUUM_ISP);
        }

        [Fact]
        public void MainsailTinCanKSCPad()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan();
            SimVesselFixtures.SetKSCPadConditions(v);

            AssertMainsailTinCanBurn(v, MainsailIsp(v.ATMPressure));
        }

        private static void AssertMatchesCapture(string capture, SimVessel v)
        {
            string actual = v.ToString();

            if (Captures.Normalize(actual) != Captures.Normalize(capture))
            {
                Print($"captured:\n{capture}");
                Print($"fixture:\n{actual}");
            }

            Assert.Equal(Captures.Normalize(capture), Captures.Normalize(actual));
        }

        // the Mainsail burns both full tanks to empty after the launch clamps are released, the fuel flow does not depend on
        // the atmosphere so only the thrust, isp and delta-v change with the atmospheric pressure.
        private static void AssertMainsailTinCanBurn(SimVessel v, double isp)
        {
            // the captured thrust direction is a unit vector rotated in single precision, so it is only nearly unit length
            double dirMagnitude = v.EnginesActivatedInStage[0][0].ThrustDirectionVectors[0].magnitude;

            var sim = new FuelFlowSimulation();
            sim.Run(v);

            foreach (FuelStats s in sim.Segments)
                Print(s.ToString());

            Assert.Equal(2, sim.Segments.Count);

            // the launch clamps have no mass while they hold the vessel, so releasing them stages nothing
            FuelStats clamps = sim.Segments[1];
            clamps.KSPStage.ShouldEqual(1);
            clamps.DeltaV.ShouldBeZero();
            clamps.DeltaTime.ShouldBeZero();
            clamps.StartMass.ShouldEqual(WET_MASS, 1e-6);
            clamps.EndMass.ShouldEqual(WET_MASS, 1e-6);

            // the propellant ratios are single precision, so the oxidizer runs out with a sliver of liquid fuel left over
            FuelStats burn = sim.Segments[0];
            burn.KSPStage.ShouldEqual(0);
            burn.StagedMass.ShouldBeZero(1e-6);
            burn.StartMass.ShouldEqual(WET_MASS, 1e-6);
            burn.EndMass.ShouldEqual(DRY_MASS, 1e-6);
            burn.DeltaTime.ShouldEqual(64 / MAINSAIL_MAX_FUEL_FLOW, 1e-6);
            burn.Thrust.ShouldEqual(MAINSAIL_MAX_FUEL_FLOW * isp * G0 * dirMagnitude, 1e-6);
            burn.Isp.ShouldEqual(isp, 1e-6);
            burn.DeltaV.ShouldEqual(isp * G0 * Math.Log(WET_MASS / DRY_MASS), 1e-6);

            // KSP's own simulation of this vessel left the same sliver in each tank, which is the stale SimPart.Mass of the
            // tanks in the capture
            foreach (SimPart tank in v.Parts.FindAll(p => p.Name == "Rockomax64.BW"))
                tank.Mass.ShouldEqual(4.00000069358129, 1e-14);
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
