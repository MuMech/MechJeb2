using MechJebLib.Functions;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.FunctionsTests
{
    public class KeplerianFromStateVectorsRetrogradeCoverageTests
    {
        // 1. Inclined circular retrograde with non-trivial Ω. Auto-selects I = -1
        //    and exercises the retrograde equinoctial math with p, q ≠ 0.
        //    Construction: i = 3π/4, Ω = π/2, position at ascending node (ν = 0).
        [Fact]
        public void InclinedCircularRetrogradeWithNonTrivialLan()
        {
            var r = new V3(0, 1, 0);
            var v = new V3(Sqrt(2) / 2, 0, Sqrt(2) / 2);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(3 * PI / 4);
            lan.ShouldEqual(PI / 2);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(0.0);
        }

        // 2. Eccentric retrograde with non-trivial Ω, ω = 0.
        //    a = 1, e = 0.5, i = 3π/4, Ω = π/2, ω = 0, ν = 0 (at periapsis).
        //    |r_pe| = a(1-e) = 0.5,  |v_pe| = sqrt(μ(1+e)/(a(1-e))) = sqrt(3).
        [Fact]
        public void EccentricRetrograde()
        {
            var r = new V3(0, 0.5, 0);
            var v = new V3(Sqrt(6) / 2, 0, Sqrt(6) / 2);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0, EPS2);
            ecc.ShouldEqual(0.5);
            inc.ShouldEqual(3 * PI / 4);
            lan.ShouldEqual(PI / 2);
            argp.ShouldEqual(0.0);
            nu.ShouldEqual(0.0);
        }

        // 3. Eccentric retrograde with all angles non-degenerate.
        //    Same orbit shape as above, with ω = π/3, at periapsis.
        //    Periapsis position direction = cos(ω)·n̂ + sin(ω)·m̂,
        //    Periapsis velocity direction = -sin(ω)·n̂ + cos(ω)·m̂,
        //    where n̂ = (0,1,0) (ascending node) and m̂ = (√2/2, 0, √2/2) (in-plane perp).
        [Fact]
        public void EccentricRetrogradeWithArgp()
        {
            var r = new V3(Sqrt(6) / 8, 0.25, Sqrt(6) / 8);
            var v = new V3(Sqrt(6) / 4, -1.5, Sqrt(6) / 4);

            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);

            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.5);
            inc.ShouldEqual(3 * PI / 4);
            lan.ShouldEqual(PI / 2);
            argp.ShouldEqual(PI / 3, EPS2);
            nu.ShouldEqual(0.0);
        }
    }
}
