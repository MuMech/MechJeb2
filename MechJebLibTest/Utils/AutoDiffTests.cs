using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.AutoDiff;

namespace MechJebLibTest.Utils
{
    public class AutoDiffTests
    {
        [Fact]
        private void JacobianDifference()
        {
            (V3 result, V3[] jac) = JacobianV3(x => x[0] - x[1], new[] { new V3(1, 2, 3), new V3(4, 5, 6) });
            result.ShouldEqual(new V3(-3, -3, -3));
            jac[0].ShouldEqual(new V3(1, 0, 0));
            jac[1].ShouldEqual(new V3(0, 1, 0));
            jac[2].ShouldEqual(new V3(0, 0, 1));
            jac[3].ShouldEqual(new V3(-1, 0, 0));
            jac[4].ShouldEqual(new V3(0, -1, 0));
            jac[5].ShouldEqual(new V3(0, 0, -1));
        }

        [Fact]
        private void JacobianCross()
        {
            (V3 result, V3[] jac) = JacobianV3(x => DualV3.Cross(x[0], x[1]), new[] { new V3(1, 2, 3), new V3(4, 5, 6) });
            result.ShouldEqual(new V3(-3, 6, -3));
            jac[0].ShouldEqual(new V3(0, -6, 5));
            jac[1].ShouldEqual(new V3(6, 0, -4));
            jac[2].ShouldEqual(new V3(-5, 4, 0));
            jac[3].ShouldEqual(new V3(0, 3, -2));
            jac[4].ShouldEqual(new V3(-3, 0, 1));
            jac[5].ShouldEqual(new V3(2, -1, 0));
        }
    }
}
