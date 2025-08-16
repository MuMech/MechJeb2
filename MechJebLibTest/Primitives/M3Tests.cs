using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Structs
{
    public class M3Tests
    {
        [Fact]
        private void IndividualAccessTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            one.m00.ShouldEqual(1);
            one.m01.ShouldEqual(2);
            one.m02.ShouldEqual(3);
            one.m10.ShouldEqual(4);
            one.m11.ShouldEqual(5);
            one.m12.ShouldEqual(6);
            one.m20.ShouldEqual(7);
            one.m21.ShouldEqual(8);
            one.m22.ShouldEqual(9);
        }

        [Fact]
        private void VectorConstructorTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var two = new M3(new V3(1, 4, 7), new V3(2, 5, 8), new V3(3, 6, 9));
            one.ShouldEqual(two);
        }

        [Fact]
        private void TwoDimensionalAccessTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            one[0, 0].ShouldEqual(1);
            one[0, 1].ShouldEqual(2);
            one[0, 2].ShouldEqual(3);
            one[1, 0].ShouldEqual(4);
            one[1, 1].ShouldEqual(5);
            one[1, 2].ShouldEqual(6);
            one[2, 0].ShouldEqual(7);
            one[2, 1].ShouldEqual(8);
            one[2, 2].ShouldEqual(9);
        }

        [Fact]
        private void TwoDimensionalSetterTest()
        {
            var one = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            var two = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            one[0, 0] = 1;
            one[0, 1] = 2;
            one[0, 2] = 3;
            one[1, 0] = 4;
            one[1, 1] = 5;
            one[1, 2] = 6;
            one[2, 0] = 7;
            one[2, 1] = 8;
            one[2, 2] = 9;
            one.ShouldEqual(two);
        }

        [Fact]
        private void OneDimensionalAccessTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            one[0].ShouldEqual(1);
            one[1].ShouldEqual(4);
            one[2].ShouldEqual(7);
            one[3].ShouldEqual(2);
            one[4].ShouldEqual(5);
            one[5].ShouldEqual(8);
            one[6].ShouldEqual(3);
            one[7].ShouldEqual(6);
            one[8].ShouldEqual(9);
        }

        [Fact]
        private void OneDimensionalSetterTest()
        {
            var one = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            var two = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            one[0] = 1;
            one[1] = 4;
            one[2] = 7;
            one[3] = 2;
            one[4] = 5;
            one[5] = 8;
            one[6] = 3;
            one[7] = 6;
            one[8] = 9;
            one.ShouldEqual(two);
        }

        [Fact]
        private void GetColumnTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            one.GetColumn(0).ShouldEqual(new V3(1, 4, 7));
            one.GetColumn(1).ShouldEqual(new V3(2, 5, 8));
            one.GetColumn(2).ShouldEqual(new V3(3, 6, 9));
        }

        [Fact]
        private void GetRowTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            one.GetRow(0).ShouldEqual(new V3(1, 2, 3));
            one.GetRow(1).ShouldEqual(new V3(4, 5, 6));
            one.GetRow(2).ShouldEqual(new V3(7, 8, 9));
        }

        [Fact]
        private void SetColumnTest()
        {
            var one = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            var two = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            one.SetColumn(0, new V3(1, 4, 7));
            one.SetColumn(1, new V3(2, 5, 8));
            one.SetColumn(2, new V3(3, 6, 9));

            one.ShouldEqual(two);
        }

        [Fact]
        private void SetRowTest()
        {
            var one = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            var two = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            one.SetRow(0, new V3(1, 2, 3));
            one.SetRow(1, new V3(4, 5, 6));
            one.SetRow(2, new V3(7, 8, 9));

            one.ShouldEqual(two);
        }

        [Fact]
        private void EqualityTest1()
        {
            var one  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var two  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var nan1 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            var nan2 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);

            Assert.True(one == two);
            Assert.False(nan1 == nan2);
            one[8] = double.NaN;
            two[8] = double.NaN;
            Assert.False(one == two);
        }

        [Fact]
        private void InequalityTest1()
        {
            var one  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var two  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var nan1 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            var nan2 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);

            Assert.False(one != two);
            Assert.True(nan1 != nan2);
            one[8] = double.NaN;
            two[8] = double.NaN;
            Assert.True(one != two);
        }

        [Fact]
        private void EqualityTest2()
        {
            var one  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var two  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var nan1 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            var nan2 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);

            Assert.True(one.Equals(two));
            Assert.True(nan1.Equals(nan2));
            Assert.True(nan1.Equals(nan1));
            one[8] = double.NaN;
            two[8] = double.NaN;
            Assert.True(one.Equals(two));
        }

        [Fact]
        private void MatrixMultiplyTest()
        {
            var one = new M3(9, 1, 4, 2, 6, 7, 3, 5, 8);
            var two = new M3(5, 3, 1, 6, 2, 4, 9, 7, 8);

            var onetwo = new M3(87, 57, 45, 109, 67, 82, 117, 75, 87);
            var twoone = new M3(54, 28, 49, 70, 38, 70, 119, 91, 149);

            (one * two).ShouldEqual(onetwo);

            (two * one).ShouldEqual(twoone);
        }

        [Fact]
        private void DivideByScalarTest()
        {
            var one = new M3(0, 2, 4, 6, 8, 10, 12, 14, 16);
            var two = new M3(0, 1, 2, 3, 4, 5, 6, 7, 8);

            (one / 2).ShouldEqual(two);
        }

        [Fact]
        private void MultiplyByScalarTest()
        {
            var one = new M3(0, 2, 4, 6, 8, 10, 12, 14, 16);
            var two = new M3(0, 1, 2, 3, 4, 5, 6, 7, 8);

            (two * 2.0).ShouldEqual(one);
            (2.0 * two).ShouldEqual(one);
        }

        [Fact]
        private void MultiplyByVectorTest()
        {
            // 30 degrees around x, 45 degrees around y, 60 degrees around z
            var one = new M3(0.35355339059, -0.14644660941, 0.92387953251,
                0.79056941504, 0.58778525229, -0.18301270189,
                -0.49999999999, 0.79389262615, 0.34641016105);

            var two = new V3(1, 2, 3);

            var three = new V3(2.8322987693,
                1.41710181395,
                2.12701573546);
            (one * two).ShouldEqual(three);
        }

        [Fact]
        private void MultiplyByVectorTest2()
        {
            // 30 degrees around x, 45 degrees around y, 60 degrees around z
            var one = new M3(0.35355339059, -0.14644660941, 0.92387953251,
                0.79056941504, 0.58778525229, -0.18301270189,
                -0.49999999999, 0.79389262615, 0.34641016105);

            var two = new V3(1, 2, 3);

            var three = new V3(2.8322987693,
                1.41710181395,
                2.12701573546);
            one.MultiplyVector(two).ShouldEqual(three);
        }

        [Fact]
        private void RotateTest()
        {
            var one   = Q3.AngleAxis(Deg2Rad(30), new V3(1, 0, 0));
            var two   = Q3.AngleAxis(Deg2Rad(45), new V3(0, 1, 0));
            var three = Q3.AngleAxis(Deg2Rad(60), new V3(0, 0, 1));
            var five = new M3(1, 0, 0,
                0, Cos(Deg2Rad(30)), -Sin(Deg2Rad(30)),
                0, Sin(Deg2Rad(30)), Cos(Deg2Rad(30)));
            var six = new M3(Cos(Deg2Rad(45)), 0, Sin(Deg2Rad(45)),
                0, 1, 0,
                -Sin(Deg2Rad(45)), 0, Cos(Deg2Rad(45)));
            var seven = new M3(Cos(Deg2Rad(60)), -Sin(Deg2Rad(60)), 0,
                Sin(Deg2Rad(60)), Cos(Deg2Rad(60)), 0,
                0, 0, 1);

            M3.Rotate(one).ShouldEqual(five);
            M3.Rotate(two).ShouldEqual(six);
            M3.Rotate(three).ShouldEqual(seven);
            M3.Rotate(three * two * one).ShouldEqual(seven * six * five);
        }

        [Fact]
        private void DeterminantStaticMethodTest()
        {
            var one = new M3(1, 0, 2, -1, 5, 0, 0, 3, -9);

            M3.Determinant(one).ShouldEqual(-51);
        }

        [Fact]
        private void DeterminantMethodTest()
        {
            var one = new M3(1, 0, 2, -1, 5, 0, 0, 3, -9);

            one.determinant.ShouldEqual(-51);
        }

        [Fact]
        private void InverseStaticMethodTest()
        {
            var one = new M3(1, 0, 2, -1, 5, 0, 0, 3, -9);
            var two = new M3(0.882352941176471, -0.117647058823529, 0.196078431372549,
                0.176470588235294, 0.176470588235294, 0.0392156862745098,
                0.0588235294117647, 0.0588235294117647, -0.0980392156862745);
            M3.Inverse(one).ShouldEqual(two, 1e-14);
            (M3.Inverse(one) * one).ShouldEqual(M3.identity);
        }

        [Fact]
        private void InverseMethodTest()
        {
            var one = new M3(1, 0, 2, -1, 5, 0, 0, 3, -9);
            var two = new M3(0.882352941176471, -0.117647058823529, 0.196078431372549,
                0.176470588235294, 0.176470588235294, 0.0392156862745098,
                0.0588235294117647, 0.0588235294117647, -0.0980392156862745);
            one.inverse.ShouldEqual(two, 1e-14);
            (one.inverse * one).ShouldEqual(M3.identity);
        }

        [Fact]
        private void ZeroTest()
        {
            var one = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            M3.zero.ShouldEqual(one);
        }

        [Fact]
        private void IdentityTest()
        {
            var one = new M3(1, 0, 0, 0, 1, 0, 0, 0, 1);
            M3.identity.ShouldEqual(one);
        }

        [Fact]
        private void IsIdentityTest()
        {
            Assert.True(M3.identity.isIdentity);
            var two = new M3(0, 0, 1, 0, 1, 0, 1, 0, 0);
            Assert.False(two.isIdentity);
        }

        [Fact]
        private void TransposeStaticMethodTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var two = new M3(1, 4, 7, 2, 5, 8, 3, 6, 9);

            M3.Transpose(one).ShouldEqual(two);

            var three = new M3(0, 1, 0, 0, 0, 1, 1, 0, 0);

            (M3.Transpose(three) * three).ShouldEqual(M3.identity);
        }

        [Fact]
        private void TransposeMethodTest()
        {
            var one = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var two = new M3(1, 4, 7, 2, 5, 8, 3, 6, 9);

            one.transpose.ShouldEqual(two);

            var three = new M3(0, 1, 0, 0, 0, 1, 1, 0, 0);

            (three.transpose * three).ShouldEqual(M3.identity);
        }

        [Fact]
        private void ToStringTest1()
        {
            var one = new M3(0.882352941176471, -0.117647058823529, 0.196078431372549,
                0.176470588235294, 0.176470588235294, 0.0392156862745098,
                0.0588235294117647, 0.0588235294117647, -0.0980392156862745);

            Assert.Equal(
                "[0.882352941176471, -0.117647058823529, 0.196078431372549\n0.176470588235294, 0.176470588235294, 0.0392156862745098\n0.0588235294117647, 0.0588235294117647, -0.0980392156862745]\n",
                one.ToString()
            );
        }

        [Fact]
        private void ToStringTest2()
        {
            var one = new M3(0.882352941176471, -0.117647058823529, 0.196078431372549,
                0.176470588235294, 0.176470588235294, 0.0392156862745098,
                0.0588235294117647, 0.0588235294117647, -0.0980392156862745);

            Assert.Equal(
                "[0.88, -0.12, 0.20\n0.18, 0.18, 0.04\n0.06, 0.06, -0.10]\n",
                one.ToString("F2")
            );
        }
    }
}
