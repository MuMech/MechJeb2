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

        // An ellipse with its periapsis on +x and the reference exactly at the apoapsis.  The reference's true anomaly from eq 21 of
        // the paper was sign(0) * acos(-1) = 0, the periapsis, which put the coast to the burn off by half an orbit.
        [Fact]
        public void ReferenceExactlyOppositeThePeriapsis()
        {
            const double RP = 1.0;
            const double RA = 2.0;
            double va = Sqrt(2.0 * RP / (RA * (RA + RP)));
            var r0 = new V3(-RA, 0, 0);
            var v0 = new V3(0, -va, 0);
            var vInf = new V3(0, -0.5, 0);

            (V3 vNeg, V3 _, V3 r, double dt) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vInf);

            (V3 rBurn, V3 vBurn) = Shepperd.Solve(1.0, dt, r0, v0);
            rBurn.ShouldEqual(r, 1e-8);
            vBurn.ShouldEqual(vNeg, 1e-8);
        }

        public static IEnumerable<object[]> EccentricitiesAndDeclinations()
        {
            foreach (double ecc in new[] { 0, 0.001, 0.05, 0.0999, 0.1001, 0.2, 0.5, 0.7, 0.9, 0.99 })
                foreach (double declination in new[] { 0, 1e-6, 0.05, -0.17, 0.7, 1.2, 1.5, PI / 2, -PI / 2 })
                    yield return new object[] { ecc, declination };
        }

        // The velocity at r on the hyperbola with the asymptote speed * uHat, which doesn't use the paper: v = a u + (a - speed) r^,
        // where a = (speed + sqrt(speed^2 + 4 mu / (r (1 + u.r^)))) / 2.  1 + u.r^ is written so that it keeps its accuracy near -1.
        private static V3 DepartureVelocity(double speed, V3 uHat, V3 r)
        {
            V3 rHat = r.normalized;
            double a = 0.5 * (speed + Sqrt(speed * speed + 8 / (r.magnitude * (uHat + rHat).sqrMagnitude)));
            return a * uHat + (a - speed) * rHat;
        }

        // The cheapest burn at any eccentricity and declination, checked against every burn position around the parking orbit.
        [Theory]
        [MemberData(nameof(EccentricitiesAndDeclinations))]
        public void CheapestBurnAroundTheWholeOrbit(double ecc, double declination)
        {
            const int BURN_POSITIONS = 7200;

            var rng = new Random(1234);

            for (int i = 0; i < 8; i++)
            {
                (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(1.0, 1 - ecc * ecc, ecc, PI * rng.NextDouble(), 2 * PI * rng.NextDouble(),
                    2 * PI * rng.NextDouble(), 2 * PI * rng.NextDouble());

                V3 h = V3.Cross(r0, v0);
                V3 hHat = h.normalized;
                V3 eVec = Astro.EccVecFromStateVectors(1.0, r0, v0);
                V3 pHat = ecc > 0 ? eVec.normalized : r0.normalized;
                V3 qHat = V3.Cross(hHat, pHat);

                double speed = Pow(10, -1.5 + 2 * rng.NextDouble());
                double azimuth = 2 * PI * rng.NextDouble();
                V3 uHat = Cos(declination) * (Cos(azimuth) * pHat + Sin(azimuth) * qHat) + Sin(declination) * hHat;
                V3 vInf = speed * uHat;

                double bruteForce = double.PositiveInfinity;
                for (int j = 0; j < BURN_POSITIONS; j++)
                {
                    double nu = (j + 0.5) * 2 * PI / BURN_POSITIONS;
                    V3 position = h.sqrMagnitude / (1 + ecc * Cos(nu)) * (Cos(nu) * pHat + Sin(nu) * qHat);
                    V3 vMinus = (-Sin(nu) * pHat + (ecc + Cos(nu)) * qHat) / h.magnitude;
                    bruteForce = Min(bruteForce, (DepartureVelocity(speed, uHat, position) - vMinus).magnitude);
                }

                (V3 vNeg, V3 vPos, V3 r, double dt) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vInf);

                string where = $"speed={speed} azimuth={azimuth} case {i}";

                // The search can beat the brute force but must not lose to it.  With the hyperbola in the parking plane dv jumps as the
                // burn passes exactly opposite vInf, where the hyperbola flips from prograde to retrograde, and the best burn can sit
                // right at the low side of that jump, closer than the burn positions above get.
                double dv = (vPos - vNeg).magnitude;
                Assert.True(dv < bruteForce * (1 + 1e-6), $"dv = {dv}, brute force = {bruteForce} at {where}");

                // and that burn has to be a real one
                vPos.ShouldEqual(DepartureVelocity(speed, uHat, r), 1e-6 * vPos.magnitude);

                (V3 rBurn, V3 vBurn) = Shepperd.Solve(1.0, dt, r0, v0);
                Assert.True((rBurn - r).magnitude < 1e-7, $"reaches {rBurn}, not {r} at {where}");
                Assert.True((vBurn - vNeg).magnitude < 1e-6 * vNeg.magnitude, $"reaches {vBurn}, not {vNeg} at {where}");

                V3 asymptote = OutgoingAsymptote(r, vPos);
                Assert.True((asymptote - uHat).magnitude < 1e-7, $"leaves along {asymptote}, not {uHat} at {where}");
            }
        }

        // A v-infinity placed so that the hyperbola is tangent to the parking orbit at its periapsis, with the reference elsewhere on
        // the orbit.  No burn can beat that one, and its dv is known exactly.
        [Theory]
        [InlineData(0.0)]
        [InlineData(0.05)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void TangentBurnAtThePeriapsis(double ecc)
        {
            const double SPEED = 0.45;
            const double NU0 = 1.0;

            double rp = 1 - ecc;
            double vp = Sqrt((1 + ecc) / (1 - ecc));
            double l = 1 - ecc * ecc;
            double nuInf = Acos(-1 / (1 + rp * SPEED * SPEED));

            // tilted so that no component is exactly zero
            Q3 tilt = Q3.AngleAxis(0.7, new V3(1, 2, 3));
            V3 r0 = tilt * (l / (1 + ecc * Cos(NU0)) * new V3(Cos(NU0), Sin(NU0), 0));
            V3 v0 = tilt * (new V3(-Sin(NU0), ecc + Cos(NU0), 0) / Sqrt(l));
            V3 vInf = tilt * (SPEED * new V3(Cos(nuInf), Sin(nuInf), 0));

            (V3 vNeg, V3 vPos, V3 r, double _) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vInf);

            (vPos - vNeg).magnitude.ShouldEqual(Sqrt(SPEED * SPEED + 2 / rp) - vp, 1e-8);

            // every burn position on a circular orbit is its periapsis
            if (ecc > 0)
                r.ShouldEqual(tilt * new V3(rp, 0, 0), 1e-4);
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
        [InlineData(2e-3)]
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
