using System;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace AssertExtensions.Utils
{
    public class StaticTests
    {
        [Fact]
        public void ClampPiTest()
        {
            ClampPi(-TAU).ShouldBeZero(double.Epsilon);
            ClampPi(-3 * PI / 2).ShouldEqual(PI / 2, double.Epsilon);
            ClampPi(-PI).ShouldEqual(PI, double.Epsilon);
            ClampPi(-PI/2).ShouldEqual(-PI/2, double.Epsilon);
            ClampPi(0).ShouldBeZero(double.Epsilon);
            ClampPi(PI/2).ShouldEqual(PI/2, double.Epsilon);
            ClampPi(PI).ShouldEqual(PI, double.Epsilon);
            ClampPi(3*PI/2).ShouldEqual(-PI/2, double.Epsilon);
            ClampPi(TAU).ShouldBeZero(double.Epsilon);
            ClampPi(3*PI).ShouldEqual(PI, double.Epsilon);
        }

        [Fact]
        public void Clamp2PiTest()
        {
            Clamp2Pi(-TAU).ShouldBeZero(double.Epsilon);
            Clamp2Pi(-3 * PI / 2).ShouldEqual(PI / 2, double.Epsilon);
            Clamp2Pi(-PI).ShouldEqual(PI, double.Epsilon);
            Clamp2Pi(-PI/2).ShouldEqual(3*PI/2, double.Epsilon);
            Clamp2Pi(0).ShouldBeZero(double.Epsilon);
            Clamp2Pi(PI/2).ShouldEqual(PI/2, double.Epsilon);
            Clamp2Pi(PI).ShouldEqual(PI, double.Epsilon);
            Clamp2Pi(3*PI/2).ShouldEqual(3*PI/2, double.Epsilon);
            Clamp2Pi(TAU).ShouldBeZero(double.Epsilon);
            Clamp2Pi(3*PI).ShouldEqual(PI, double.Epsilon);
        }
    }
}
