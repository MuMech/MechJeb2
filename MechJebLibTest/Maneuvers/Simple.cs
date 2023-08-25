using MechJebLib.Primitives;
using Xunit;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Maneuvers
{
    public class Simple
    {
        [Fact]
        public void DeltaVToChangeInclinationTest1()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            double v185 = MechJebLib.Core.Maths.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeInclination(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeInclination(r0, v0, Deg2Rad(90));
            Assert.Equal(0, (v0 + dv).x, 9);
            Assert.Equal(0, (v0 + dv).y, 9);
            Assert.Equal(v185, (v0 + dv).z, 9);
        }

        [Fact]
        public void DeltaVToChangeFPATest1()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            double v185 = MechJebLib.Core.Maths.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, Deg2Rad(90));
            Assert.Equal(v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, Deg2Rad(-90));
            Assert.Equal(-v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Sqrt(2), v185 / Sqrt(2), 0);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Sqrt(2), dv.x, 9);
            Assert.Equal(v185 - v185 / Sqrt(2), dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Sqrt(2), 0, v185 / Sqrt(2));

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Sqrt(2), dv.x, 9);
            Assert.Equal(0, dv.y, 9);
            Assert.Equal(v185 - v185 / Sqrt(2), dv.z, 9);
        }
    }
}
