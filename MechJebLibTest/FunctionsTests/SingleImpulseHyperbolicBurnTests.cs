/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using static System.Math;
using Random = System.Random;

namespace MechJebLibTest.FunctionsTests
{
    public class SingleImpulseHyperbolicBurnTests
    {
        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i < 100; i++)
                yield return new object[] { i };
        }

        // the burn is on the parking orbit dt after the reference, and leaves it on a hyperbola with the requested v-infinity
        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomOrbitsReachTheBurnAndTheSpeed(int seed)
        {
            var rng = new Random(seed);

            // eccentricities on both sides of the 0.1 that switches between the line search and the analytic solution
            double ecc = 0.3 * rng.NextDouble();
            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(1.0, 1.0, ecc, PI * rng.NextDouble(), 2 * PI * rng.NextDouble(),
                2 * PI * rng.NextDouble(), 2 * PI * rng.NextDouble());
            var vInf = new V3(2 * rng.NextDouble() - 1, 2 * rng.NextDouble() - 1, 2 * rng.NextDouble() - 1);

            (V3 vNeg, V3 vPos, V3 r, double dt) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vInf);

            double period = 2 * PI * Sqrt(Pow(Astro.SmaFromStateVectors(1.0, r0, v0), 3));
            Assert.InRange(dt, 0, period);

            (V3 rBurn, V3 vBurn) = Shepperd.Solve(1.0, dt, r0, v0);
            rBurn.ShouldEqual(r, 1e-8);
            vBurn.ShouldEqual(vNeg, 1e-8);

            (0.5 * vPos.sqrMagnitude - 1.0 / r.magnitude).ShouldEqual(0.5 * vInf.sqrMagnitude, 1e-8);

            // and it leaves along vInf: the outgoing asymptote is (sqrt(e^2 - 1) (h^ x e^) - e^) / e, with e the eccentricity vector
            V3 eVec = Astro.EccVecFromStateVectors(1.0, r, vPos);
            double e = eVec.magnitude;
            V3 asymptote = (Sqrt(e * e - 1) * V3.Cross(V3.Cross(r, vPos).normalized, eVec / e) - eVec / e) / e;
            Assert.True((asymptote - vInf.normalized).magnitude < 1e-8, $"leaves along {asymptote}, not {vInf.normalized}");
        }

        // An ellipse with its periapsis on +x, the reference at the apoapsis, and a v-infinity that puts the burn there too.
        // The burn's true anomaly was sign(0) * acos(-1) = 0, the periapsis, and a round-off negative coast would be a full period.
        [Fact]
        public void BurnOppositeThePeriapsisAtTheReference()
        {
            const double RP = 1.0;
            const double RA = 2.0;
            double va = Sqrt(2.0 * RP / (RA * (RA + RP)));
            var r0 = new V3(-RA, 0, 0);
            var v0 = new V3(0, -va, 0);
            var vInf = new V3(0, -0.5, 0);

            (V3 _, V3 _, V3 r, double dt) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vInf);

            r.ShouldEqual(r0, 1e-9);
            dt.ShouldEqual(0, 1e-9);
        }

        [Fact]
        public void ZeroVInfinityThrowsArgumentException()
        {
            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(1.0, 1.0, 0.01, 0.1, 0.2, 0.3, 0.4);

            Assert.Throws<ArgumentException>(() => Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, V3.zero));
            Assert.Throws<ArgumentException>(() => Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, new V3(double.NaN, 0, 0)));
        }
    }
}
