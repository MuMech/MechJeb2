using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class CoreOperationsTests
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
        private void SubtractionBasicMatrices()
        {
            var a = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);
            var b = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (a - b).ShouldEqual(new M3(8, 6, 4, 2, 0, -2, -4, -6, -8));
        }

        [Fact]
        private void SubtractionWithZeroMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (m - M3.zero).ShouldEqual(m);
            (M3.zero - m).ShouldEqual(-m);
        }

        [Fact]
        private void SubtractionOfIdenticalMatrices()
        {
            var m = new M3(1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5);

            (m - m).ShouldEqual(M3.zero);
        }

        [Fact]
        private void SubtractionWithNegativeElements()
        {
            var a = new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9);
            var b = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (a - b).ShouldEqual(new M3(-2, -4, -6, -8, -10, -12, -14, -16, -18));
        }

        [Fact]
        private void SubtractionIsNotCommutative()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a - b).ShouldEqual(-(b - a));
        }

        [Fact]
        private void SubtractionWithLargeValues()
        {
            var a = new M3(1e150, 2e150, 3e150, 4e150, 5e150, 6e150, 7e150, 8e150, 9e150);
            var b = new M3(1e150, 1e150, 1e150, 1e150, 1e150, 1e150, 1e150, 1e150, 1e150);

            (a - b).ShouldEqual(new M3(0, 1e150, 2e150, 3e150, 4e150, 5e150, 6e150, 7e150, 8e150));
        }

        [Fact]
        private void SubtractionWithSmallValues()
        {
            var a = new M3(1e-150, 2e-150, 3e-150, 4e-150, 5e-150, 6e-150, 7e-150, 8e-150, 9e-150);
            var b = new M3(1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150);

            (a - b).ShouldEqual(new M3(0, 1e-150, 2e-150, 3e-150, 4e-150, 5e-150, 6e-150, 7e-150, 8e-150));
        }

        [Fact]
        private void NegationBasicMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (-m).ShouldEqual(new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9));
        }

        [Fact]
        private void NegationOfZeroMatrix() => (-M3.zero).ShouldEqual(M3.zero);

        [Fact]
        private void NegationOfIdentity() => (-M3.identity).ShouldEqual(new M3(-1, 0, 0, 0, -1, 0, 0, 0, -1));

        [Fact]
        private void DoubleNegation()
        {
            var m = new M3(1.5, -2.5, 3.5, -4.5, 5.5, -6.5, 7.5, -8.5, 9.5);

            (- -m).ShouldEqual(m);
        }

        [Fact]
        private void NegationWithMixedSigns()
        {
            var m = new M3(-1, 2, -3, 4, -5, 6, -7, 8, -9);

            (-m).ShouldEqual(new M3(1, -2, 3, -4, 5, -6, 7, -8, 9));
        }

        [Fact]
        private void NegationPreservesDeterminantMagnitude()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 10);

            (-m).determinant.ShouldEqual(-m.determinant);
        }

        [Fact]
        private void NegationWithLargeValues()
        {
            var m = new M3(1e150, -1e150, 1e150, -1e150, 1e150, -1e150, 1e150, -1e150, 1e150);

            (-m).ShouldEqual(new M3(-1e150, 1e150, -1e150, 1e150, -1e150, 1e150, -1e150, 1e150, -1e150));
        }

        [Fact]
        private void SubtractionAndNegationRelationship()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a - b).ShouldEqual(a + -b);
        }

        [Fact]
        private void SubtractionAssociativityWithNegation()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(2, 3, 4, 5, 6, 7, 8, 9, 10);
            var c = new M3(3, 4, 5, 6, 7, 8, 9, 10, 11);

            (a - b - c).ShouldEqual(a - (b + c));
        }

        [Fact]
        private void AdditionBasicMatrices()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a + b).ShouldEqual(new M3(10, 10, 10, 10, 10, 10, 10, 10, 10));
        }

        [Fact]
        private void AdditionWithZeroMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (m + M3.zero).ShouldEqual(m);
            (M3.zero + m).ShouldEqual(m);
        }

        [Fact]
        private void AdditionWithIdentity()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (m + M3.identity).ShouldEqual(new M3(2, 2, 3, 4, 6, 6, 7, 8, 10));
        }

        [Fact]
        private void AdditionIsCommutative()
        {
            var a = new M3(1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5);
            var b = new M3(-0.5, 4.1, -2.3, 1.7, -3.2, 0.8, 2.1, -1.4, 5.6);

            (a + b).ShouldEqual(b + a);
        }

        [Fact]
        private void AdditionIsAssociative()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);
            var c = new M3(2, 4, 6, 8, 10, 12, 14, 16, 18);

            (a + b + c).ShouldEqual(a + (b + c));
        }

        [Fact]
        private void AdditionWithNegativeElements()
        {
            var a = new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9);
            var b = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (a + b).ShouldEqual(M3.zero);
        }

        [Fact]
        private void AdditionWithMixedSigns()
        {
            var a = new M3(-1, 2, -3, 4, -5, 6, -7, 8, -9);
            var b = new M3(1, -2, 3, -4, 5, -6, 7, -8, 9);

            (a + b).ShouldEqual(M3.zero);
        }

        [Fact]
        private void AdditionWithLargeValues()
        {
            var a = new M3(1e150, 2e150, 3e150, 4e150, 5e150, 6e150, 7e150, 8e150, 9e150);
            var b = new M3(1e150, 1e150, 1e150, 1e150, 1e150, 1e150, 1e150, 1e150, 1e150);

            (a + b).ShouldEqual(new M3(2e150, 3e150, 4e150, 5e150, 6e150, 7e150, 8e150, 9e150, 10e150));
        }

        [Fact]
        private void AdditionWithSmallValues()
        {
            var a = new M3(1e-150, 2e-150, 3e-150, 4e-150, 5e-150, 6e-150, 7e-150, 8e-150, 9e-150);
            var b = new M3(1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150, 1e-150);

            (a + b).ShouldEqual(new M3(2e-150, 3e-150, 4e-150, 5e-150, 6e-150, 7e-150, 8e-150, 9e-150, 10e-150));
        }

        [Fact]
        private void AdditionAndSubtractionInverse()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a + b - b).ShouldEqual(a);
            (a + b - a).ShouldEqual(b);
        }

        [Fact]
        private void AdditionWithNegation()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            (m + -m).ShouldEqual(M3.zero);
        }

        [Fact]
        private void AdditionDistributesOverScalarMultiplication()
        {
            var          a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var          b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);
            const double s = 2.5;

            (s * (a + b)).ShouldEqual(s * a + s * b);
        }

        [Fact]
        private void AdditionPreservesSymmetry()
        {
            var a   = new M3(1, 2, 3, 2, 4, 5, 3, 5, 6);
            var b   = new M3(6, 5, 4, 5, 3, 2, 4, 2, 1);
            M3  sum = a + b;

            sum[0, 1].ShouldEqual(sum[1, 0]);
            sum[0, 2].ShouldEqual(sum[2, 0]);
            sum[1, 2].ShouldEqual(sum[2, 1]);
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
        private void MaxMagnitudePositiveElements()
        {
            new M3(1, 2, 3, 4, 5, 6, 7, 8, 9).max_magnitude.ShouldEqual(9.0);
            new M3(9, 8, 7, 6, 5, 4, 3, 2, 1).max_magnitude.ShouldEqual(9.0);
            new M3(5, 5, 5, 5, 5, 5, 5, 5, 5).max_magnitude.ShouldEqual(5.0);
        }

        [Fact]
        private void MaxMagnitudeNegativeElements()
        {
            new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9).max_magnitude.ShouldEqual(-1.0);
            new M3(-9, -8, -7, -6, -5, -4, -3, -2, -1).max_magnitude.ShouldEqual(-1.0);
        }

        [Fact]
        private void MaxMagnitudeMixedElements()
        {
            new M3(-5, 3, 2, 1, 0, -1, -2, -3, 4).max_magnitude.ShouldEqual(4.0);
            new M3(0, 0, 0, 0, 10, 0, 0, 0, 0).max_magnitude.ShouldEqual(10.0);
        }

        [Fact]
        private void MaxMagnitudeZeroMatrix() => M3.zero.max_magnitude.ShouldEqual(0.0);

        [Fact]
        private void MaxMagnitudeIdentityMatrix() => M3.identity.max_magnitude.ShouldEqual(1.0);

        [Fact]
        private void MinMagnitudePositiveElements()
        {
            new M3(1, 2, 3, 4, 5, 6, 7, 8, 9).min_magnitude.ShouldEqual(1.0);
            new M3(9, 8, 7, 6, 5, 4, 3, 2, 1).min_magnitude.ShouldEqual(1.0);
            new M3(5, 5, 5, 5, 5, 5, 5, 5, 5).min_magnitude.ShouldEqual(5.0);
        }

        [Fact]
        private void MinMagnitudeNegativeElements()
        {
            new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9).min_magnitude.ShouldEqual(-9.0);
            new M3(-9, -8, -7, -6, -5, -4, -3, -2, -1).min_magnitude.ShouldEqual(-9.0);
        }

        [Fact]
        private void MinMagnitudeMixedElements()
        {
            new M3(-5, 3, 2, 1, 0, -1, -2, -3, 4).min_magnitude.ShouldEqual(-5.0);
            new M3(1, 2, 3, 4, -10, 6, 7, 8, 9).min_magnitude.ShouldEqual(-10.0);
        }

        [Fact]
        private void MinMagnitudeZeroMatrix() => M3.zero.min_magnitude.ShouldEqual(0.0);

        [Fact]
        private void MinMagnitudeIdentityMatrix() => M3.identity.min_magnitude.ShouldEqual(0.0);

        [Fact]
        private void MaxMagnitudeLargeValues() => new M3(1e100, 1e200, 1e150, 0, 0, 0, 0, 0, 0).max_magnitude.ShouldEqual(1e200);

        [Fact]
        private void MinMagnitudeSmallValues() => new M3(1e-100, 1e-200, 1e-150, 1, 1, 1, 1, 1, 1).min_magnitude.ShouldEqual(1e-200);

        [Fact]
        private void CopyToBasic()
        {
            var       m     = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            double[,] array = new double[5, 5];

            m.CopyTo(array, 0, 0);

            array[0, 0].ShouldEqual(1);
            array[0, 1].ShouldEqual(2);
            array[0, 2].ShouldEqual(3);
            array[1, 0].ShouldEqual(4);
            array[1, 1].ShouldEqual(5);
            array[1, 2].ShouldEqual(6);
            array[2, 0].ShouldEqual(7);
            array[2, 1].ShouldEqual(8);
            array[2, 2].ShouldEqual(9);
        }

        [Fact]
        private void CopyToWithOffset()
        {
            var       m     = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            double[,] array = new double[6, 6];

            m.CopyTo(array, 1, 2);

            array[0, 2].ShouldEqual(0);
            array[1, 2].ShouldEqual(1);
            array[1, 3].ShouldEqual(2);
            array[1, 4].ShouldEqual(3);
            array[2, 2].ShouldEqual(4);
            array[2, 3].ShouldEqual(5);
            array[2, 4].ShouldEqual(6);
            array[3, 2].ShouldEqual(7);
            array[3, 3].ShouldEqual(8);
            array[3, 4].ShouldEqual(9);
            array[4, 2].ShouldEqual(0);
        }

        [Fact]
        private void CopyToIdentityMatrix()
        {
            double[,] array = new double[3, 3];

            M3.identity.CopyTo(array, 0, 0);

            array[0, 0].ShouldEqual(1);
            array[0, 1].ShouldEqual(0);
            array[0, 2].ShouldEqual(0);
            array[1, 0].ShouldEqual(0);
            array[1, 1].ShouldEqual(1);
            array[1, 2].ShouldEqual(0);
            array[2, 0].ShouldEqual(0);
            array[2, 1].ShouldEqual(0);
            array[2, 2].ShouldEqual(1);
        }

        [Fact]
        private void CopyToZeroMatrix()
        {
            double[,] array = new double[3, 3];
            array[1, 1] = 99;

            M3.zero.CopyTo(array, 0, 0);

            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                array[i, j].ShouldEqual(0);
        }

        [Fact]
        private void CopyToPreservesOtherElements()
        {
            var       m     = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            double[,] array = new double[5, 5];
            array[0, 4] = 100;
            array[4, 0] = 200;

            m.CopyTo(array, 1, 1);

            array[0, 4].ShouldEqual(100);
            array[4, 0].ShouldEqual(200);
            array[0, 0].ShouldEqual(0);
            array[0, 1].ShouldEqual(0);
        }

        [Fact]
        private void CopyToMultipleMatrices()
        {
            var       m1    = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var       m2    = new M3(10, 20, 30, 40, 50, 60, 70, 80, 90);
            double[,] array = new double[3, 6];

            m1.CopyTo(array, 0, 0);
            m2.CopyTo(array, 0, 3);

            array[0, 0].ShouldEqual(1);
            array[0, 3].ShouldEqual(10);
            array[1, 1].ShouldEqual(5);
            array[1, 4].ShouldEqual(50);
            array[2, 2].ShouldEqual(9);
            array[2, 5].ShouldEqual(90);
        }

        [Fact]
        private void CopyToWithNegativeValues()
        {
            var       m     = new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9);
            double[,] array = new double[3, 3];

            m.CopyTo(array, 0, 0);

            array[0, 0].ShouldEqual(-1);
            array[1, 1].ShouldEqual(-5);
            array[2, 2].ShouldEqual(-9);
        }

        [Fact]
        private void CopyToWithLargeValues()
        {
            var       m     = new M3(1e100, 1e200, 1e-100, 1e-200, 0, 1, -1e100, -1e200, 1e150);
            double[,] array = new double[3, 3];

            m.CopyTo(array, 0, 0);

            array[0, 0].ShouldEqual(1e100);
            array[0, 1].ShouldEqual(1e200);
            array[0, 2].ShouldEqual(1e-100);
            array[1, 0].ShouldEqual(1e-200);
            array[2, 2].ShouldEqual(1e150);
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
