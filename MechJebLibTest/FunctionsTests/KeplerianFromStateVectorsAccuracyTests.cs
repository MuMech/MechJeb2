using MechJebLib.Functions;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.FunctionsTests
{
    public class KeplerianFromStateVectorsAccuracyTests
    {
        // These tests construct orbits at the boundaries of the classical-element
        // domain where an acos-based extraction loses catastrophic precision.
        //
        // The acos function has derivative -1/sqrt(1-x²), which diverges at x = ±1,
        // so any angle near 0 or π is computed by taking acos of an argument near ±1.
        // For ε ≈ 1e-10, cos(ε) ≈ 1 - 5e-21 rounds to exactly 1.0 in double
        // precision (since 5e-21 < machine epsilon ≈ 2.22e-16), so acos(cos(ε))
        // returns exactly 0 — the small angle is completely destroyed.
        //
        // The atan2-based equinoctial formulation routes the same information through
        // tan(ε/2) ≈ ε/2 representations that preserve every significant bit, so
        // these tests assert recovery to ~1e-14 relative precision. The original
        // Julia / acos formulation would fail these by ~10 orders of magnitude.

        [Fact]
        public void TinyInclinationPrograde_AcosArgumentNearOne()
        {
            // i = 1e-10 (essentially equatorial prograde, slightly tilted about x-axis).
            // hhat ≈ (0, -1e-10, cos(1e-10)). cos(1e-10) rounds to 1.0, so:
            //   acos formulation: inc = acos(1.0) = 0           ← catastrophic loss
            //   atan2 formulation: q = 5e-11, inc = 2·atan(q) ≈ 1e-10   ← preserved
            const double eps = 1e-10;
            var r = new V3(1, 0, 0);
            var v = new V3(0, Cos(eps), Sin(eps));

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(eps);
            lan.ShouldEqual(0.0);  // ascending node still at +x̂
            argp.ShouldEqual(0.0); // circular
            nu.ShouldEqual(0.0);   // position at ascending node
        }

        [Fact]
        public void NearRetrogradeEquatorial_AcosArgumentNearMinusOne()
        {
            // i = π - 1e-10. Same failure mode at the other endpoint:
            //   acos formulation: hhat.z rounds to -1.0; inc = acos(-1.0) = π
            //   atan2 formulation with I=-1: q ≈ 5e-11, inc = π - 2·atan(q) ≈ π - 1e-10
            const double eps = 1e-10;
            var r = new V3(1, 0, 0);
            var v = new V3(0, -Cos(eps), Sin(eps));

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(PI - eps);
            lan.ShouldEqual(0.0);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(0.0);
        }

        [Fact]
        public void TinyAscendingNode_AcosArgumentNearOne()
        {
            // Polar orbit (i = π/2) with Ω = 1e-10, position at the ascending node.
            // Node vector Nv ≈ (1, 1e-10, 0). Nv[0]/|Nv| ≈ 1 rounds to 1.0, so:
            //   acos formulation: Ω = acos(1.0) = 0              ← node azimuth lost
            //   atan2 formulation: p = 1e-10, q = 1, lan = atan2(p, q) ≈ 1e-10
            const double eps = 1e-10;
            var r = new V3(Cos(eps), Sin(eps), 0);
            var v = new V3(0, 0, 1);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(PI / 2);
            lan.ShouldEqual(eps);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(0.0);
        }

        [Fact]
        public void TinyArgumentOfPeriapsis_AcosArgumentNearOne()
        {
            // Polar orbit, Ω = 0, e = 0.5, ω = 1e-10. Position at periapsis.
            // The acos argument reduces to (Nv·ê)/(|Nv|·|ê|) = cos(ω), which rounds
            // to 1.0 in double precision:
            //   acos formulation: ω = acos(1.0) = 0              ← argp lost
            //   atan2 formulation: h ≈ 5e-11, k ≈ 0.5, argp = atan2(h, k) ≈ 1e-10
            //
            // Orbit setup: hhat = -ŷ (so the xz plane is the orbital plane),
            // periapsis at angle ω above +x̂ toward +ẑ.
            //   r at periapsis = a(1-e) · (cos ω, 0, sin ω) = 0.5 · (cos ω, 0, sin ω)
            //   v at periapsis = sqrt(μ(1+e)/(a(1-e))) · (-sin ω, 0, cos ω)
            //                  = sqrt(3) · (-sin ω, 0, cos ω)
            const double eps = 1e-10;
            var r = new V3(0.5 * Cos(eps), 0, 0.5 * Sin(eps));
            var v = new V3(-Sqrt(3) * Sin(eps), 0, Sqrt(3) * Cos(eps));

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0, 1e-15);
            ecc.ShouldEqual(0.5);
            inc.ShouldEqual(PI / 2);
            lan.ShouldEqual(0.0);
            argp.ShouldEqual(eps);
            nu.ShouldBeZero();
        }
    }
}
