using AssertExtensions;
using MechJebLib.Core;
using Xunit;

namespace MechJebLibTest.MathsTests
{
    public class BisectionTests
    {
        [Fact]
        private void SingularityTest()
        {
            double x, fx;

            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, -1.0, 1.0, null);
            x.ShouldEqual(-4.9406564584124654E-324, 0);
            fx.ShouldEqual(double.NegativeInfinity);
            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, 1.0, -1.0, null, 0, false);
            x.ShouldEqual(-4.9406564584124654E-324, 0);
            fx.ShouldEqual(double.NegativeInfinity);
            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, -1.0, 1.0, null, 0, false);
            x.ShouldEqual(0, 0);
            fx.ShouldEqual(double.PositiveInfinity);
            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, 1.0, -1.0, null);
            x.ShouldEqual(0, 0);
            fx.ShouldEqual(double.PositiveInfinity);
        }

        [Fact]
        private void IrrationalTest()
        {
            double x, fx;

            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 2.0, 3.0, null);
            x.ShouldEqual(2.4494897427831779, 0);
            fx.ShouldEqual(-4.4408920985006262E-16);
            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 2.0, 3.0, null, 0, false);
            x.ShouldEqual(2.4494897427831783, 0);
            fx.ShouldEqual(4.4408920985006262E-16);
            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 3.0, 2.0, null);
            x.ShouldEqual(2.4494897427831783, 0);
            fx.ShouldEqual(4.4408920985006262E-16);
            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 3.0, 2.0, null, 0, false);
            x.ShouldEqual(2.4494897427831779, 0);
            fx.ShouldEqual(-4.4408920985006262E-16);
        }
    }
}
