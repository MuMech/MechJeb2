/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using AssertExtensions;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Utils
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
        public void ClampTest()
        {
            Clamp(-2.0, 0, 1).ShouldEqual(0);
            Clamp(2.0, 0, 1).ShouldEqual(1);
        }

        [Fact]
        public void ToSITest()
        {
            Assert.Equal("0.000001000 q", 1e-36.ToSI());
            Assert.Equal("0.001000 q", 1e-33.ToSI());
            Assert.Equal("1.000 q", 1e-30.ToSI());
            Assert.Equal("1.000 r", 1e-27.ToSI());
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
            Assert.Equal("1.000 R", 1e27.ToSI());
            Assert.Equal("1.000 Q", 1e30.ToSI());
            Assert.Equal("1000 Q", 1e33.ToSI());
            Assert.Equal("1000000 Q", 1e36.ToSI());
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

            Assert.Equal("0 q", 1.23456e-33.ToSI(-1));
            Assert.Equal("0 q", 1.23456e-32.ToSI(-1));
            Assert.Equal("0 q", 1.23456e-31.ToSI(-1));
            Assert.Equal("1 q", 1.23456e-30.ToSI(-1));
            Assert.Equal("12 q", 1.23456e-29.ToSI(-1));
            Assert.Equal("123 q", 1.23456e-28.ToSI(-1));
            Assert.Equal("1 r", 1.23456e-27.ToSI(-1));
            Assert.Equal("12 r", 1.23456e-26.ToSI(-1));
            Assert.Equal("123 r", 1.23456e-25.ToSI(-1));
            Assert.Equal("1 y", 1.23456e-24.ToSI(-1));
            Assert.Equal("12 y", 1.23456e-23.ToSI(-1));
            Assert.Equal("123 y", 1.23456e-22.ToSI(-1));
            Assert.Equal("1 z", 1.23456e-21.ToSI(-1));
            Assert.Equal("12 z", 1.23456e-20.ToSI(-1));
            Assert.Equal("123 z", 1.23456e-19.ToSI(-1));
            Assert.Equal("1 a", 1.23456e-18.ToSI(-1));
            Assert.Equal("12 a", 1.23456e-17.ToSI(-1));
            Assert.Equal("123 a", 1.23456e-16.ToSI(-1));
            Assert.Equal("1 f", 1.23456e-15.ToSI(-1));
            Assert.Equal("0.0  ", 0d.ToSI(-1));
            Assert.Equal("1.0  ", 1d.ToSI(-1));
            Assert.Equal("1.2  ", 1.23456.ToSI(-1));
            Assert.Equal("12.3  ", 12.3456.ToSI(-1));
            Assert.Equal("123.5  ", 123.456.ToSI(-1));
            Assert.Equal("1.235 k", 1234.56.ToSI(-1));
            Assert.Equal("12.35 k", 12345.6.ToSI(-1));
            Assert.Equal("123.5 k", 1.23456e5.ToSI(-1));
            Assert.Equal("1.235 M", 1.23456e6.ToSI(-1));
            Assert.Equal("12.35 M", 1.23456e7.ToSI(-1));
            Assert.Equal("123.5 M", 1.23456e8.ToSI(-1));
            Assert.Equal("1.235 G", 1.23456e9.ToSI(-1));
            Assert.Equal("12.35 G", 1.23456e10.ToSI(-1));
        }
    }
}
