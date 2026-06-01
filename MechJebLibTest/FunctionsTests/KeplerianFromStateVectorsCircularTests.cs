using MechJebLib.Functions;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.FunctionsTests
{
    public class KeplerianFromStateVectorsCircularTests
    {
        private static void AssertElements(V3 r, V3 v,
            double esma, double eecc, double einc, double elan, double eargp, double enu)
        {
            (double sma, double ecc, double inc, double lan, double argp, double nu, _) = Astro.KeplerianFromStateVectors(1.0, r, v);
            sma.ShouldEqual(esma);
            ecc.ShouldEqual(eecc);
            inc.ShouldEqual(einc);
            lan.ShouldEqual(elan);
            argp.ShouldEqual(eargp);
            nu.ShouldEqual(enu);
        }

        // hhat = +ẑ, i = 0. Both Ω and ω are undefined → 0.
        // ν measured from +x̂ in prograde (CCW from +ẑ) direction.
        [Fact]
        public void CircularEquatorialPrograde()
        {
            AssertElements(new V3(1, 0, 0), new V3(0, 1, 0), 1.0, 0.0, 0.0, 0.0, 0.0, 0.0);
            AssertElements(new V3(0, 1, 0), new V3(-1, 0, 0), 1.0, 0.0, 0.0, 0.0, 0.0, PI / 2);
            AssertElements(new V3(-1, 0, 0), new V3(0, -1, 0), 1.0, 0.0, 0.0, 0.0, 0.0, PI);
            AssertElements(new V3(0, -1, 0), new V3(1, 0, 0), 1.0, 0.0, 0.0, 0.0, 0.0, 3 * PI / 2);
        }

        // hhat = -ẑ, i = π. Both Ω and ω are undefined → 0.
        // ν measured from +x̂ in retrograde (CW from +ẑ) direction. Auto-selects I = -1.
        [Fact]
        public void CircularEquatorialRetrograde()
        {
            AssertElements(new V3(1, 0, 0), new V3(0, -1, 0), 1.0, 0.0, PI, 0.0, 0.0, 0.0);
            AssertElements(new V3(0, -1, 0), new V3(-1, 0, 0), 1.0, 0.0, PI, 0.0, 0.0, PI / 2);
            AssertElements(new V3(-1, 0, 0), new V3(0, 1, 0), 1.0, 0.0, PI, 0.0, 0.0, PI);
            AssertElements(new V3(0, 1, 0), new V3(1, 0, 0), 1.0, 0.0, PI, 0.0, 0.0, 3 * PI / 2);
        }

        // hhat = +x̂, i = π/2. Ascending node = ẑ × x̂ = +ŷ, so Ω = π/2.
        // ω undefined → 0, so ν measured from ascending node (+ŷ) in orbit direction.
        [Fact]
        public void CircularPolarPlusX()
        {
            AssertElements(new V3(0, 1, 0), new V3(0, 0, 1), 1.0, 0.0, PI / 2, PI / 2, 0.0, 0.0);
            AssertElements(new V3(0, 0, 1), new V3(0, -1, 0), 1.0, 0.0, PI / 2, PI / 2, 0.0, PI / 2);
            AssertElements(new V3(0, -1, 0), new V3(0, 0, -1), 1.0, 0.0, PI / 2, PI / 2, 0.0, PI);
            AssertElements(new V3(0, 0, -1), new V3(0, 1, 0), 1.0, 0.0, PI / 2, PI / 2, 0.0, 3 * PI / 2);
        }

        // hhat = -x̂, i = π/2. Ascending node = ẑ × (-x̂) = -ŷ, so Ω = 3π/2.
        [Fact]
        public void CircularPolarMinusX()
        {
            AssertElements(new V3(0, -1, 0), new V3(0, 0, 1), 1.0, 0.0, PI / 2, 3 * PI / 2, 0.0, 0.0);
            AssertElements(new V3(0, 0, 1), new V3(0, 1, 0), 1.0, 0.0, PI / 2, 3 * PI / 2, 0.0, PI / 2);
            AssertElements(new V3(0, 1, 0), new V3(0, 0, -1), 1.0, 0.0, PI / 2, 3 * PI / 2, 0.0, PI);
            AssertElements(new V3(0, 0, -1), new V3(0, -1, 0), 1.0, 0.0, PI / 2, 3 * PI / 2, 0.0, 3 * PI / 2);
        }

        // hhat = +ŷ, i = π/2. Ascending node = ẑ × ŷ = -x̂, so Ω = π.
        // Includes the original failing case: r=(1,0,0), v=(0,0,-1) at descending node, ν=π.
        [Fact]
        public void CircularPolarPlusY()
        {
            AssertElements(new V3(-1, 0, 0), new V3(0, 0, 1), 1.0, 0.0, PI / 2, PI, 0.0, 0.0);
            AssertElements(new V3(0, 0, 1), new V3(1, 0, 0), 1.0, 0.0, PI / 2, PI, 0.0, PI / 2);
            AssertElements(new V3(1, 0, 0), new V3(0, 0, -1), 1.0, 0.0, PI / 2, PI, 0.0, PI);
            AssertElements(new V3(0, 0, -1), new V3(-1, 0, 0), 1.0, 0.0, PI / 2, PI, 0.0, 3 * PI / 2);
        }

        // hhat = -ŷ, i = π/2. Ascending node = ẑ × (-ŷ) = +x̂, so Ω = 0.
        [Fact]
        public void CircularPolarMinusY()
        {
            AssertElements(new V3(1, 0, 0), new V3(0, 0, 1), 1.0, 0.0, PI / 2, 0.0, 0.0, 0.0);
            AssertElements(new V3(0, 0, 1), new V3(-1, 0, 0), 1.0, 0.0, PI / 2, 0.0, 0.0, PI / 2);
            AssertElements(new V3(-1, 0, 0), new V3(0, 0, -1), 1.0, 0.0, PI / 2, 0.0, 0.0, PI);
            AssertElements(new V3(0, 0, -1), new V3(1, 0, 0), 1.0, 0.0, PI / 2, 0.0, 0.0, 3 * PI / 2);
        }
    }
}
