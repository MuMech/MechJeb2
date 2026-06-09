using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;
using Random = System.Random;

namespace MechJebLibTest.FunctionsTests
{
    public class KeplerianFromStateVectorsTests
    {
        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i <= 500; i++)
                yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomOrbitalElementsForwardAndBack(int seed)
        {
            var rng = new Random(seed);

            var r0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            var v0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);

            (double _, double ecc, double inc, double lan, double argp, double nu, double l) =
                Astro.KeplerianFromStateVectors(1.0, r0, v0);
            (V3 r02, V3 v02) = Astro.StateVectorsFromKeplerian(1.0, l, ecc, inc, lan, argp, nu);

            r02.ShouldEqual(r0, 1e-8);
            v02.ShouldEqual(v0, 1e-8);
        }

        // Hyperbolic equatorial prograde at periapsis. a = -1, e = 2 → r_pe = 1, v_pe = √3.
        [Fact]
        public void HyperbolicEquatorialProgradeAtPeriapsis()
        {
            var r = new V3(1, 0, 0);
            var v = new V3(0, Sqrt(3), 0);

            (double sma, double ecc, double inc, double lan, double argp, double nu, double l) =
                Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(-1.0);
            ecc.ShouldEqual(2.0);
            inc.ShouldEqual(0.0);
            lan.ShouldEqual(0.0);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(0.0);
            l.ShouldEqual(3.0); // a(1 - e²) = -1·(1 - 4) = 3
        }

        // Hyperbolic equatorial retrograde at periapsis. Same shape, velocity flipped.
        [Fact]
        public void HyperbolicEquatorialRetrogradeAtPeriapsis()
        {
            var r = new V3(1, 0, 0);
            var v = new V3(0, -Sqrt(3), 0);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) =
                Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(-1.0);
            ecc.ShouldEqual(2.0);
            inc.ShouldEqual(PI);
            lan.ShouldEqual(0.0);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(0.0);
        }

        // Eccentric equatorial prograde (i = 0) with periapsis at angle π/4 from +x̂.
        // Ω is undefined → 0; ω carries the longitude of periapsis.
        [Fact]
        public void EccentricEquatorialProgradeWithArgp()
        {
            // r_pe = a(1-e) = 0.5 at direction (cos π/4, sin π/4, 0)
            // v_pe magnitude = √(μ(1+e)/(a(1-e))) = √3, perpendicular and prograde
            var r = new V3(Sqrt(2) / 4, Sqrt(2) / 4, 0);
            var v = new V3(-Sqrt(6) / 2, Sqrt(6) / 2, 0);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) =
                Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.5);
            inc.ShouldEqual(0.0);
            lan.ShouldEqual(0.0);
            argp.ShouldEqual(PI / 4);
            nu.ShouldEqual(0.0);
        }

        // Eccentric equatorial retrograde (i = π) with longitude of periapsis = π/4.
        // Retrograde flips the sense — argp should be measured accordingly.
        [Fact]
        public void EccentricEquatorialRetrogradeWithArgp()
        {
            // Same periapsis position; velocity reversed in y to make orbit retrograde.
            var r = new V3(Sqrt(2) / 4, Sqrt(2) / 4, 0);
            var v = new V3(Sqrt(6) / 2, -Sqrt(6) / 2, 0);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) =
                Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.5);
            inc.ShouldEqual(PI);
            lan.ShouldEqual(0.0);
            // For retrograde equatorial, argp is measured CW from +x̂ in the orbital plane.
            argp.ShouldEqual(2 * PI - PI / 4);
            nu.ShouldEqual(0.0);
        }

        // Eccentric prograde at apoapsis. Exercises ν = π (opposite from the usual periapsis cases).
        [Fact]
        public void EccentricEquatorialProgradeAtApoapsis()
        {
            // a=1, e=0.5, r_ap = 1.5 along -x̂.
            // v² = μ(2/r - 1/a) = 2/1.5 - 1 = 1/3 → v = 1/√3, prograde at apoapsis is -ŷ.
            var r = new V3(-1.5, 0, 0);
            var v = new V3(0, -1 / Sqrt(3), 0);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) =
                Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.5);
            inc.ShouldEqual(0.0);
            lan.ShouldEqual(0.0);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(PI);
        }

        // Eccentric polar (i = π/2) with ν = π/2 — position at 90° true anomaly,
        // not at periapsis or a node. Ω = 0, ω = 0, periapsis at +x̂.
        [Fact]
        public void EccentricPolarAtNinetyDegreesTrueAnomaly()
        {
            // a = 1, e = 0.5, l = a(1-e²) = 0.75.
            // r(ν=π/2) = l / (1 + e·cos ν) = 0.75; direction is +ẑ (perifocal q̂ rotated into xz plane by i=π/2).
            // Perifocal velocity at ν=π/2: v_p = √(μ/l) · (-sin ν, e+cos ν) = √(4/3) · (-1, 0.5).
            // Rotated to inertial (Ω=ω=0, i=π/2): (-1)·x̂ + 0.5·ẑ scaled by √(4/3).
            double s = Sqrt(4.0 / 3.0);
            var r = new V3(0, 0, 0.75);
            var v = new V3(-s, 0, 0.5 * s);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) =
                Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.5);
            inc.ShouldEqual(PI / 2);
            lan.ShouldEqual(0.0);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(PI / 2);
        }
    }
}
