/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using MechJebLib.Control;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.ControlTests
{
    public class RCSSolverTests
    {
        private const double TOLERANCE = 0.02;

        private static RCSSolver.Thruster Nozzle(double y, V3 direction, double power = 1) =>
            new RCSSolver.Thruster(new[] { new V3(0, y, 0) }, new[] { direction }, power, V3.one);

        // a block on a ring around the Y axis, with nozzles along the axis and around the ring
        private static RCSSolver.Thruster Block(double y, double angle)
        {
            var pos = new V3(1.25 * Cos(angle), y, 1.25 * Sin(angle));
            var tangent = new V3(-Sin(angle), 0, Cos(angle));

            return new RCSSolver.Thruster(new[] { pos, pos, pos, pos }, new[] { V3.yaxis, -V3.yaxis, tangent, -tangent }, 1, V3.one);
        }

        private static double[] Solve(V3 direction, params RCSSolver.Thruster[] thrusters) =>
            new RCSSolver().Run(thrusters, direction, V3.zero, RCSSolverTuningParams.Default);

        [Fact]
        public void LowersTheThrusterWithTheLongerArm()
        {
            double[] throttles = Solve(V3.xaxis, Nozzle(3, V3.xaxis), Nozzle(-1, V3.xaxis));

            Assert.Equal(1.0 / 3, throttles[0], TOLERANCE);
            Assert.Equal(1, throttles[1], TOLERANCE);
        }

        [Fact]
        public void WeighsThrustersByTheirPower()
        {
            // 2 kN at 1 m against 1 kN at 2 m is already balanced
            double[] throttles = Solve(V3.xaxis, Nozzle(1, V3.xaxis, 2), Nozzle(-2, V3.xaxis));

            Assert.Equal(1, throttles[0], TOLERANCE);
            Assert.Equal(1, throttles[1], TOLERANCE);
        }

        [Fact]
        public void TakesTheArmFromTheNozzleThatFires()
        {
            // the block's other nozzle, 5 m out, faces away from the direction and stays shut
            var block = new RCSSolver.Thruster(new[] { new V3(0, 5, 0), new V3(0, 1, 0) }, new[] { -V3.xaxis, V3.xaxis }, 1, V3.one);

            double[] throttles = Solve(V3.xaxis, block, Nozzle(-1, V3.xaxis));

            Assert.Equal(1, throttles[0], TOLERANCE);
            Assert.Equal(1, throttles[1], TOLERANCE);
        }

        [Fact]
        public void TheThrottlesDoNotDependOnTheScaleOfThePower()
        {
            double[] small = Solve(V3.xaxis, Nozzle(3, V3.xaxis), Nozzle(-1, V3.xaxis));
            double[] large = Solve(V3.xaxis, Nozzle(3, V3.xaxis, 12), Nozzle(-1, V3.xaxis, 12));

            Assert.Equal(small[0], large[0], 1e-6);
            Assert.Equal(small[1], large[1], 1e-6);
        }

        [Fact]
        public void AThrusterThatCannotPushStaysAtFullPower()
        {
            // its throttle does nothing for the translation, and at full power it still works for attitude
            double[] throttles = Solve(V3.xaxis, Nozzle(3, V3.xaxis), Nozzle(-1, V3.xaxis), Nozzle(0, V3.zaxis));

            Assert.Equal(1.0 / 3, throttles[0], TOLERANCE);
            Assert.Equal(1, throttles[1], TOLERANCE);
            Assert.Equal(1, throttles[2]);
        }

        [Fact]
        public void WithNothingAbleToPushEveryThrusterStaysAtFullPower()
        {
            double[] throttles = Solve(V3.xaxis, Nozzle(1, V3.zaxis), Nozzle(-1, -V3.zaxis));

            Assert.Equal(new double[] { 1, 1 }, throttles);
        }

        [Fact]
        public void ANozzleJustOffPerpendicularCannotPush()
        {
            // real nozzles are never exactly at 90 degrees, and ModuleRCS gives nothing under 5 % anyway
            V3 almost = new V3(1e-7, 0, 1).normalized;

            Assert.Equal(V3.zero, Nozzle(0, almost).GetThrust(V3.xaxis, V3.zero));
            Assert.Equal(1, Solve(V3.xaxis, Nozzle(3, V3.xaxis), Nozzle(-1, V3.xaxis), Nozzle(0, almost))[2]);
        }

        [Fact]
        public void BalancesTwoRingsOfBlocks()
        {
            // four 4-way blocks in each ring, with the CoM 5 m below one ring and 3 m above the other
            double[] angles = { 0, PI / 2, PI, 3 * PI / 2 };
            RCSSolver.Thruster[] blocks = angles.Select(a => Block(5, a)).Concat(angles.Select(a => Block(-3, a))).ToArray();

            double[] throttles = Solve(V3.xaxis, blocks);

            // only the blocks at 90 and 270 degrees have a nozzle facing along X, and the others keep working for attitude
            Assert.Equal(new double[] { 1, 1, 1, 1 }, new[] { throttles[0], throttles[2], throttles[4], throttles[6] });
            Assert.Equal(0.6, throttles[1], TOLERANCE);
            Assert.Equal(0.6, throttles[3], TOLERANCE);
            Assert.Equal(1, throttles[5], TOLERANCE);
            Assert.Equal(1, throttles[7], TOLERANCE);
        }

        [Fact]
        public void ThrustIsInKilonewtons()
        {
            RCSSolver.Thruster thruster = Nozzle(0, V3.xaxis, 2);

            Assert.Equal(2, thruster.GetThrust(V3.xaxis, V3.zero).x, 12);
            Assert.Equal(Sqrt(2), thruster.GetThrust(new V3(1, 1, 0).normalized, V3.zero).x, 12);
        }

        [Fact]
        public void ADisabledAxisGivesNoThrust()
        {
            // enableX off: ModuleRCS never sees the X part of the input
            var thruster = new RCSSolver.Thruster(new[] { V3.zero, V3.zero }, new[] { V3.xaxis, V3.yaxis }, 1, new V3(0, 1, 1));

            V3 thrust = thruster.GetThrust(new V3(1, 1, 0).normalized, V3.zero);

            Assert.Equal(0, thrust.x);
            Assert.Equal(Sqrt(0.5), thrust.y, 12);
            Assert.Equal(V3.zero, thruster.GetThrust(V3.xaxis, V3.zero));
        }

        [Fact]
        public void TorqueAddsUpEachNozzle()
        {
            var thruster = new RCSSolver.Thruster(new[] { new V3(0, 2, 0), new V3(0, -2, 0) }, new[] { V3.xaxis, V3.xaxis }, 1, V3.one);

            Assert.Equal(0, thruster.GetTorque(V3.xaxis, V3.zero).magnitude, 12);
        }
    }
}
