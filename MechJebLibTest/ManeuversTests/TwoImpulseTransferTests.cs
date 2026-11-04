/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.ManeuversTests
{
    public class TwoImpulseTransferTests
    {
        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i < 50; i++)
                yield return new object[] { i };
        }

        private static (V3 r, V3 v) Circular(double mu, double radius, double angle) =>
            (new V3(radius * Cos(angle), radius * Sin(angle), 0), Sqrt(mu / radius) * new V3(-Sin(angle), Cos(angle), 0));

        // the search can reach a transfer of nearly a whole revolution, whose ends are too close together for Russell
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        private void OrbitsOfNearlyTheSameSize(double radiusDifference)
        {
            const double MU = 3.5316e12;

            for (int i = 0; i < 36; i++)
            {
                (V3 r1, V3 v1) = Circular(MU, 800e3, 0);
                (V3 r2, V3 v2) = Circular(MU, 800e3 + radiusDifference, (i + 0.5) * 10 * Deg2Rad(1));

                (V3 dv1, double dt1, V3 _, double dt2) = TwoImpulseTransfer.NextManeuver(MU, r1, v1, r2, v2, coplanar: false);

                (V3 rburn, V3 vburn) = Shepperd.Solve(MU, dt1, r1, v1);
                (V3 rarrival, V3 _) = Shepperd.Solve(MU, dt2 - dt1, rburn, vburn + dv1);
                (V3 rtarget, V3 _) = Shepperd.Solve(MU, dt2, r2, v2);

                Assert.True(dt1 > 0);
                Assert.InRange((rarrival - rtarget).magnitude, 0, 1);
            }
        }

        [Fact]
        private void HyperbolicTarget()
        {
            const double MU = 3.5316e12;

            (V3 r1, V3 v1) = Circular(MU, 800e3, 0);
            (V3 r2, V3 v2) = Circular(MU, 900e3, 1);
            v2 *= 1.6;

            (V3 dv1, double dt1, V3 _, double dt2) = TwoImpulseTransfer.NextManeuver(MU, r1, v1, r2, v2, coplanar: false);

            (V3 rburn, V3 vburn) = Shepperd.Solve(MU, dt1, r1, v1);
            (V3 rarrival, V3 _) = Shepperd.Solve(MU, dt2 - dt1, rburn, vburn + dv1);
            (V3 rtarget, V3 _) = Shepperd.Solve(MU, dt2, r2, v2);

            Assert.InRange((rarrival - rtarget).magnitude, 0, 1);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void HohmannTest(int seed)
        {
            var random = new Random(seed);

            // construct some random circular coplanar hohmann transfers
            double mu = random.NextDouble() * 10 + 1;

            // construct a random circular orbit
            var r1 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var v1 = new V3(random.NextDouble() - 0.5, random.NextDouble() - 0.5, random.NextDouble() - 0.5);
            v1 = (v1 - V3.Dot(r1.normalized, v1) * r1.normalized).normalized * Astro.CircularVelocity(mu, r1.magnitude);

            // construct another random circular coplanar orbit
            var h1 = V3.Cross(r1, v1);
            var r2 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            r2 -= V3.Dot(h1.normalized, r2) * h1.normalized;
            V3 v2 = V3.Cross(h1, r2).normalized * Astro.CircularVelocity(mu, r2.magnitude);

            // this algorithm has issues with very large (normalized) synodic periods
            var scale = Scale.Create(mu, Sqrt(r1.magnitude * r2.magnitude));
            double synodicPeriod = Astro.SynodicPeriod(mu, r1, v1, r2, v2) / scale.TimeScale;
            if (synodicPeriod > 1000)
                return;

            (V3 dv1, double dt1, V3 dv2, double dt2) = TwoImpulseTransfer.NextManeuver(mu, r1, v1, r2, v2);

            (double dv1Hoh, double dv2Hoh, double ttHoh, double _) = Astro.HohmannTransferParameters(mu, r1, r2);

            dv1.magnitude.ShouldEqual(Abs(dv1Hoh), 1e-6);
            dv2.magnitude.ShouldEqual(Abs(dv2Hoh), 1e-6);
            (dt2 - dt1).ShouldEqual(ttHoh, 1e-3);

            (V3 rburn1, V3 vburn1) = Shepperd.Solve(mu, dt1, r1, v1);
            (V3 rburn2, V3 vburn2) = Shepperd.Solve(mu, dt2 - dt1, rburn1, vburn1 + dv1);
            (V3 rf, V3 vf) = Shepperd.Solve(mu, dt2, r2, v2);

            rf.ShouldEqual(rburn2, 1e-6);
            vf.ShouldEqual(vburn2 + dv2, 1e-6);
        }

        [Fact]
        private void GeoTestFixed()
        {
            double mu = 3.986004418e+14;
            var r1 = new V3(5673188.62234991, 1106269.57811856, 3093900.30098098);
            var v1 = new V3(-1154.38931594925, 7685.58250511721, -631.330049272638);

            double nu = 3.24639265358979;
            (V3 r2, V3 v2) = Astro.StateVectorsFromKeplerian(mu, 42164000, 0, 0, 0, 0, nu);

            (V3 dv1, double dt1, V3 dv2, double dt2) = TwoImpulseTransfer.NextManeuver(mu, r1, v1, r2, v2, coplanar: false);
            // dt2 is from now, like dt1: the transfer meets the target there
            (V3 rburn1, V3 vburn1) = Shepperd.Solve(mu, dt1, r1, v1);
            (V3 rburn2, V3 vburn2) = Shepperd.Solve(mu, dt2 - dt1, rburn1, vburn1 + dv1);
            (V3 rf, V3 vf) = Shepperd.Solve(mu, dt2, r2, v2);
            // in metres and m/s: a component near zero can't be compared relatively
            Assert.True((rf - rburn2).magnitude < 1e-2, $"misses the target by {(rf - rburn2).magnitude} m");
            Assert.True((vf - (vburn2 + dv2)).magnitude < 1e-3, $"misses the target's velocity by {(vf - (vburn2 + dv2)).magnitude} m/s");
            double inc = Astro.IncFromStateVectors(rburn1, vburn1 + dv1);

            dv1.magnitude.ShouldEqual(2484.20137552452, 1e-4);
            dv2.magnitude.ShouldEqual(1793.10206031673, 1e-4);
            dt1.ShouldEqual(1177.74844650851, 1e-4);
            inc.ShouldEqual(Deg2Rad(26.440413305834294), 1e-4);
            dt2.ShouldEqual(20098.223757535023, 1e-4);
        }

        [Fact]
        private void GeoTestFree()
        {
            double mu = 3.986004418e+14;
            var r1 = new V3(5673188.62234991, 1106269.57811856, 3093900.30098098);
            var v1 = new V3(-1154.38931594925, 7685.58250511721, -631.330049272638);

            (V3 r2, V3 v2) = Astro.StateVectorsFromKeplerian(mu, 42164000, 0, 0, 0, 0, 0);

            (V3 dv1, double dt1, V3 dv2, double dt2) = TwoImpulseTransfer.NextManeuver(mu, r1, v1, r2, v2, coplanar: false, rendezvous: false);
            // without a rendezvous the transfer reaches the target's orbit, not the target, at dt2 (from now, like dt1)
            (V3 rburn1, V3 vburn1) = Shepperd.Solve(mu, dt1, r1, v1);
            (V3 rburn2, V3 vburn2) = Shepperd.Solve(mu, dt2 - dt1, rburn1, vburn1 + dv1);
            rburn2.magnitude.ShouldEqual(42164000, 1e-6);
            Astro.EccFromStateVectors(mu, rburn2, vburn2 + dv2).ShouldEqual(0, 1e-6);
            double inc = Astro.IncFromStateVectors(rburn1, vburn1 + dv1);

            dv1.magnitude.ShouldEqual(2484.20137552452, 1e-4);
            dv2.magnitude.ShouldEqual(1793.10206031673, 1e-4);
            dt1.ShouldEqual(1177.74844650851, 1e-3);
            inc.ShouldEqual(Deg2Rad(26.440413305834294), 1e-4);
            dt2.ShouldEqual(20097.46881536536, 1e-3);
        }
    }
}
