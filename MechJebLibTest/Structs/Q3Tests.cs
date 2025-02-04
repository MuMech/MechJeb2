using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Structs
{
    public class Q3Tests
    {
        [Fact]
        private void EulerAngleTests()
        {
            Assert.Equal(true, V3.zero == Q3.identity.eulerAngles);
            // 45 degree pitch down (negative pitch)
            Assert.Equal(true, new V3(0, -PI / 4, 0) == Q3.AngleAxis(PI / 4, V3.left).eulerAngles);
            Assert.Equal(-PI / 4, Q3.AngleAxis(PI / 4, V3.left).eulerAngles.pitch, 14);
            // 45 degree yaw left (negative yaw)
            Assert.Equal(true, new V3(0, 0, -PI / 4) == Q3.AngleAxis(PI / 4, V3.up).eulerAngles);
            Assert.Equal(-PI / 4, Q3.AngleAxis(PI / 4, V3.up).eulerAngles.yaw, 14);
            // 45 degree roll right
            Assert.Equal(true, new V3(PI / 4, 0, 0) == Q3.AngleAxis(PI / 4, V3.forward).eulerAngles);
            Assert.Equal(PI / 4, Q3.AngleAxis(PI / 4, V3.forward).eulerAngles.roll, 14);
        }

        [Fact]
        private void AngleAxisTests()
        {
            // 90 degree rotation around up (yaw left)
            Assert.Equal(true, V3.up == Q3.AngleAxis(PI / 2, V3.up) * V3.up);
            Assert.Equal(true, V3.left == Q3.AngleAxis(PI / 2, V3.up) * V3.forward);
            Assert.Equal(true, V3.back == Q3.AngleAxis(PI / 2, V3.up) * V3.left);
            Assert.Equal(true, V3.right == Q3.AngleAxis(PI / 2, V3.up) * V3.back);
            Assert.Equal(true, V3.forward == Q3.AngleAxis(PI / 2, V3.up) * V3.right);
            Assert.Equal(true, V3.down == Q3.AngleAxis(PI / 2, V3.up) * V3.down);
            // 90 degree rotation around forward (roll right)
            Assert.Equal(true, V3.forward == Q3.AngleAxis(PI / 2, V3.forward) * V3.forward);
            Assert.Equal(true, V3.right == Q3.AngleAxis(PI / 2, V3.forward) * V3.up);
            Assert.Equal(true, V3.down == Q3.AngleAxis(PI / 2, V3.forward) * V3.right);
            Assert.Equal(true, V3.left == Q3.AngleAxis(PI / 2, V3.forward) * V3.down);
            Assert.Equal(true, V3.up == Q3.AngleAxis(PI / 2, V3.forward) * V3.left);
            Assert.Equal(true, V3.back == Q3.AngleAxis(PI / 2, V3.forward) * V3.back);
            // 90 degree rotation around left (pitch down)
            Assert.Equal(true, V3.left == Q3.AngleAxis(PI / 2, V3.left) * V3.left);
            Assert.Equal(true, V3.forward == Q3.AngleAxis(PI / 2, V3.left) * V3.up);
            Assert.Equal(true, V3.down == Q3.AngleAxis(PI / 2, V3.left) * V3.forward);
            Assert.Equal(true, V3.back == Q3.AngleAxis(PI / 2, V3.left) * V3.down);
            Assert.Equal(true, V3.up == Q3.AngleAxis(PI / 2, V3.left) * V3.back);
            Assert.Equal(true, V3.right == Q3.AngleAxis(PI / 2, V3.left) * V3.right);
        }

        [Fact]
        private void AngleAxisTests2()
        {
            var one   = Q3.AngleAxis(Deg2Rad(30), new V3(1, 0, 0));
            var two   = new V3(1, 1, 1);
            var three = new V3(1, 0.36602540379, 1.36602540377);

            (one * two).ShouldEqual(three, 1e-11);
        }
    }
}
