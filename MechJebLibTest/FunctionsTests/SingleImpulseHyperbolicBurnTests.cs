/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

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

        // the outgoing asymptote is (sqrt(e^2 - 1) (h^ x e^) - e^) / e, with e the eccentricity vector
        private static V3 OutgoingAsymptote(V3 r, V3 v)
        {
            V3 eVec = Astro.EccVecFromStateVectors(1.0, r, v);
            double e = eVec.magnitude;
            return (Sqrt(e * e - 1) * V3.Cross(V3.Cross(r, v).normalized, eVec / e) - eVec / e) / e;
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

            // and it leaves along vInf
            V3 asymptote = OutgoingAsymptote(r, vPos);
            Assert.True((asymptote - vInf.normalized).magnitude < 1e-8, $"leaves along {asymptote}, not {vInf.normalized}");
        }

        // An ellipse with its periapsis on +x, the reference at the apoapsis, and a v-infinity that puts the burn there too.
        // The burn's true anomaly from eq 21 of the paper was sign(0) * acos(-1) = 0, the periapsis.
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

        // A declination this small barely changes the problem, so the answer must stay next to the coplanar one.  With only the
        // rotation of eq 38 the useful range of that rotation is about as wide as the declination, and the line search can't see it.
        [Theory]
        [InlineData(2e-10)]
        [InlineData(1e-9)]
        [InlineData(-1e-9)]
        [InlineData(1e-8)]
        [InlineData(1e-7)]
        [InlineData(1e-6)]
        [InlineData(-1e-6)]
        [InlineData(1e-5)]
        [InlineData(1e-4)]
        [InlineData(1e-3)]
        [InlineData(2e-3)] // just past the switch back to eq 38
        public void NearlyCoplanarStaysNextToTheCoplanarAnswer(double declination)
        {
            // tilted so that no component is exactly zero
            Q3 tilt = Q3.AngleAxis(0.7, new V3(1, 2, 3));

            foreach (double ecc in new[] { 0.001, 0.05 })
            {
                foreach (double speed in new[] { 0.45, 1.5 })
                {
                    for (int i = 0; i < 12; i++)
                    {
                        V3 r0 = tilt * new V3(1 - ecc, 0, 0);
                        V3 v0 = tilt * new V3(0, Sqrt((1 + ecc) / (1 - ecc)), 0);
                        double azimuth = i * PI / 6;
                        V3 coplanarVInf = tilt * new V3(speed * Cos(azimuth), speed * Sin(azimuth), 0);
                        V3 vInf = tilt * new V3(speed * Cos(declination) * Cos(azimuth), speed * Cos(declination) * Sin(azimuth),
                            speed * Sin(declination));

                        (V3 vNegCoplanar, V3 vPosCoplanar, V3 _, double _) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, coplanarVInf);
                        (V3 vNeg, V3 vPos, V3 r, double _) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vInf);

                        string where = $"ecc={ecc} speed={speed} azimuth={azimuth}";
                        Assert.True(vPos.IsFinite(), $"vPos is {vPos} at {where}");

                        double dv = (vPos - vNeg).magnitude;
                        double dvCoplanar = (vPosCoplanar - vNegCoplanar).magnitude;
                        Assert.True(Abs(dv - dvCoplanar) < 1e-4, $"dv = {dv}, coplanar dv = {dvCoplanar} at {where}");

                        V3 asymptote = OutgoingAsymptote(r, vPos);
                        Assert.True((asymptote - vInf.normalized).magnitude < 1e-8, $"leaves along {asymptote}, not {vInf.normalized} at {where}");
                    }
                }
            }
        }
    }
}
