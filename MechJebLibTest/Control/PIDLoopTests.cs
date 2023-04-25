using MechJebLib.Control;
using Xunit;

namespace MechJebLibTest.Control
{
    public class PIDLoopTests
    {
        [Fact]
        public void ProportionalTest()
        {
            var pid = new PIDLoop { Kp = 2.0, Ts = 0.02 };

            Assert.Equal(2.0, pid.Update(0, -1), 8);
            Assert.Equal(-2.0, pid.Update(0, 1), 8);
        }

        [Fact]
        public void DerivativeTest()
        {
            var pid = new PIDLoop { Kp = 2.0, Kd = 1.0, N = 100, Ts = 0.02 };

            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(-52, pid.Update(0, 1), 8);
            Assert.Equal(-54, pid.Update(0, 2), 8);
            Assert.Equal(-56, pid.Update(0, 3), 8);
        }

        [Fact]
        public void IntegralTest()
        {
            var pid = new PIDLoop { Kp = 2.0, Ki = 1.0, Ts = 0.02 };

            Assert.Equal(2.01, pid.Update(0, -1.0), 8);
            Assert.Equal(2.03, pid.Update(0, -1.0), 8);
            Assert.Equal(2.05, pid.Update(0, -1.0), 8);
            Assert.Equal(2.07, pid.Update(0, -1.0), 8);
        }
    }
}
