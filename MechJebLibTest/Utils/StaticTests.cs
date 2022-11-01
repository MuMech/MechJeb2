/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

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
            ClampPi(-PI / 2).ShouldEqual(-PI / 2, double.Epsilon);
            ClampPi(0).ShouldBeZero(double.Epsilon);
            ClampPi(PI / 2).ShouldEqual(PI / 2, double.Epsilon);
            ClampPi(PI).ShouldEqual(PI, double.Epsilon);
            ClampPi(3 * PI / 2).ShouldEqual(-PI / 2, double.Epsilon);
            ClampPi(TAU).ShouldBeZero(double.Epsilon);
            ClampPi(3 * PI).ShouldEqual(PI, double.Epsilon);
        }

        [Fact]
        public void Clamp2PiTest()
        {
            Clamp2Pi(-TAU).ShouldBeZero(double.Epsilon);
            Clamp2Pi(-3 * PI / 2).ShouldEqual(PI / 2, double.Epsilon);
            Clamp2Pi(-PI).ShouldEqual(PI, double.Epsilon);
            Clamp2Pi(-PI / 2).ShouldEqual(3 * PI / 2, double.Epsilon);
            Clamp2Pi(0).ShouldBeZero(double.Epsilon);
            Clamp2Pi(PI / 2).ShouldEqual(PI / 2, double.Epsilon);
            Clamp2Pi(PI).ShouldEqual(PI, double.Epsilon);
            Clamp2Pi(3 * PI / 2).ShouldEqual(3 * PI / 2, double.Epsilon);
            Clamp2Pi(TAU).ShouldBeZero(double.Epsilon);
            Clamp2Pi(3 * PI).ShouldEqual(PI, double.Epsilon);
        }

        [Fact]
        public void ToSITest()
        {
            Assert.Equal("0.000001000 y", 1e-30.ToSI());
            Assert.Equal("0.001000 y", 1e-27.ToSI());
            Assert.Equal("1.000 y", 1e-24.ToSI());
            Assert.Equal("1.000 z", 1e-21.ToSI());
            Assert.Equal("1.000 a", 1e-18.ToSI());
            Assert.Equal("1.000 f", 1e-15.ToSI());
            Assert.Equal("1.000 p", 1e-12.ToSI());
            Assert.Equal("1.000 n", 1e-9.ToSI());
            Assert.Equal("1.000 μ", 1e-6.ToSI());
            Assert.Equal("1.000 m", 1e-3.ToSI());
            Assert.Equal("0.000  ", 0d.ToSI());
            Assert.Equal("1.000  ", 1d.ToSI());
            Assert.Equal("12.00  ", 12d.ToSI());
            Assert.Equal("123.0  ", 123d.ToSI());
            Assert.Equal("1.000 k", 1e3.ToSI());
            Assert.Equal("1.234 k", 1234d.ToSI());
            Assert.Equal("12.35 k", 12345d.ToSI());
            Assert.Equal("123.5 k", 123456d.ToSI());
            Assert.Equal("999.9 k", (1e6 - 50.001).ToSI());
            Assert.Equal("1.000 M", (1e6 - 50).ToSI());
            Assert.Equal("1.000 M", 1e6.ToSI());
            Assert.Equal("1.235 M", 1234567d.ToSI());
            Assert.Equal("9.999 M", (1e7 - 500.001).ToSI());
            Assert.Equal("10.00 M", (1e7 - 500).ToSI());
            Assert.Equal("10.00 M", 1e7.ToSI());
            Assert.Equal("12.35 M", 12345678d.ToSI());
            Assert.Equal("99.99 M", (1e8 - 5000.001).ToSI());
            Assert.Equal("100.0 M", (1e8 - 5000).ToSI());
            Assert.Equal("123.5 M", 123456789d.ToSI());
            Assert.Equal("1.000 G", 1e9.ToSI());
            Assert.Equal("1.000 T", 1e12.ToSI());
            Assert.Equal("1.000 P", 1e15.ToSI());
            Assert.Equal("1.000 E", 1e18.ToSI());
            Assert.Equal("1.000 Z", 1e21.ToSI());
            Assert.Equal("1.000 Y", 1e24.ToSI());
            Assert.Equal("1000 Y", 1e27.ToSI());
            Assert.Equal("1000000 Y", 1e30.ToSI());
            Assert.Equal("-1.000  ", (-1d).ToSI());
            Assert.Equal("-12.00  ", (-12d).ToSI());
            Assert.Equal("-123.0  ", (-123d).ToSI());
            Assert.Equal("-1.234 k", (-1234d).ToSI());
            Assert.Equal("-12.35 k", (-12345d).ToSI());
            Assert.Equal("-123.5 k", (-123456d).ToSI());
            Assert.Equal("-1.235 M", (-1234567d).ToSI());
            Assert.Equal("-12.35 M", (-12345678d).ToSI());
            Assert.Equal("-123.5 M", (-123456789d).ToSI());
            Assert.Equal("NaN", double.NaN.ToSI());
            Assert.Equal("Infinity", double.PositiveInfinity.ToSI());
            Assert.Equal("-Infinity", double.NegativeInfinity.ToSI());
        }
    }
}
