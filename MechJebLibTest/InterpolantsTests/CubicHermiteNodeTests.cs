/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using MechJebLibTest.FuelFlowSimulationTests;
using Xunit;

namespace MechJebLibTest.InterpolantsTests
{
    public class CubicHermiteNodeTests
    {
        // A zero-length PSG phase builds nodes with t1 == t2 (and non-finite derivatives), which used to evaluate 0/0 = NaN.
        [Fact]
        public void ZeroWidthVecNodeIsConstant()
        {
            using var y = Vec.Rent(2);
            using var dy = Vec.Rent(2);
            using var ynew = Vec.Rent(2);
            using var dynew = Vec.Rent(2);
            y[0] = 1.0;
            y[1] = 2.0;
            dy[0] = dy[1] = double.NaN;
            ynew[0] = 1.0;
            ynew[1] = 2.0;
            dynew[0] = dynew[1] = double.PositiveInfinity;

            using var interpolant = VecInterpolant.Rent();
            interpolant.Append(CubicHermiteVecNode.Rent(0.5, 0, y, dy, ynew, dynew), 0.5, 0.5);

            using Vec yout = interpolant.Evaluate(0.5);

            yout[0].ShouldEqual(1.0, 0);
            yout[1].ShouldEqual(2.0, 0);
        }

        // h is nonzero but below the roundoff of t, so t + h == t
        [Fact]
        public void SubRoundoffWidthDoubleNodeIsConstant()
        {
            using var interpolant = DoubleInterpolant.Rent();
            interpolant.Append(CubicHermiteDoubleNode.Rent(0.5, 1e-17, 3.0, double.NaN, 3.0, double.NaN), 0.5, 0.5);

            interpolant.Evaluate(0.5).ShouldEqual(3.0, 0);
        }

        private const double T     = 2.5;
        private const double H     = 0.75;
        private const double Y     = 1.25;
        private const double DY    = -3.0;
        private const double YNEW  = 4.5;
        private const double DYNEW = 0.5;

        // the cubic in the Hermite basis, as an independent reference for the nodes' power-basis form
        private static double HermiteBasis(double y, double dy, double ynew, double dynew, double x)
        {
            double s = (x - T) / H;
            double s2 = s * s;
            double s3 = s2 * s;
            return (2 * s3 - 3 * s2 + 1) * y + (s3 - 2 * s2 + s) * H * dy + (-2 * s3 + 3 * s2) * ynew + (s3 - s2) * H * dynew;
        }

        [Fact]
        public void DoubleNodeMatchesHermiteBasis()
        {
            using var interpolant = DoubleInterpolant.Rent();
            interpolant.Append(CubicHermiteDoubleNode.Rent(T, H, Y, DY, YNEW, DYNEW), T, T + H);

            interpolant.Evaluate(T).ShouldEqual(Y, 0);

            for (int i = 0; i <= 8; i++)
            {
                double x = T + i * H / 8;
                interpolant.Evaluate(x).ShouldEqual(HermiteBasis(Y, DY, YNEW, DYNEW, x), 1e-14);
            }
        }

        [Fact]
        public void VecNodeMatchesHermiteBasis()
        {
            using var y = Vec.Rent(2);
            using var dy = Vec.Rent(2);
            using var ynew = Vec.Rent(2);
            using var dynew = Vec.Rent(2);
            y[0]     = Y;
            dy[0]    = DY;
            ynew[0]  = YNEW;
            dynew[0] = DYNEW;
            y[1]     = -7.0;
            dy[1]    = 11.0;
            ynew[1]  = 2.0;
            dynew[1] = -4.0;

            using var interpolant = VecInterpolant.Rent();
            interpolant.Append(CubicHermiteVecNode.Rent(T, H, y, dy, ynew, dynew), T, T + H);

            for (int i = 0; i <= 8; i++)
            {
                double x = T + i * H / 8;
                using Vec yout = interpolant.Evaluate(x);
                for (int j = 0; j < 2; j++)
                    yout[j].ShouldEqual(HermiteBasis(y[j], dy[j], ynew[j], dynew[j], x), 1e-14);
            }
        }

        // the dump is pasted into fixtures, so rebuilding from it has to reproduce the node bit-for-bit
        [Fact]
        public void DoubleNodeDumpRoundTrips()
        {
            using var original = DoubleInterpolant.Rent();
            original.Append(CubicHermiteDoubleNode.Rent(T, H, Y, DY, YNEW, DYNEW), T, T + H);

            var fields = new Dictionary<string, double>();
            foreach (Match m in Regex.Matches(original.ToString(), @"(\w+)=([^ \]]+)"))
                fields[m.Groups[1].Value] = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);

            using var rebuilt = DoubleInterpolant.Rent();
            PartFixtures.AppendCubicPolynomial(rebuilt, fields["leftT"], fields["rightT"], fields["t"], fields["invH"], fields["c0"],
                fields["c1"], fields["c2"], fields["c3"]);

            rebuilt.ToString().ShouldEqual(original.ToString());

            for (int i = 0; i <= 8; i++)
            {
                double x = T + i * H / 8;
                rebuilt.Evaluate(x).ShouldEqual(original.Evaluate(x), 0);
            }
        }
    }
}
