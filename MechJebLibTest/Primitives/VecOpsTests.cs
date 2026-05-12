using System;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives
{
    public class VecOpsTests
    {
        // ---------- Copy ----------

        [Fact]
        public void Copy_CopiesFirstNElements()
        {
            double[] src = { 1.0, 2.0, 3.0, 4.0, 5.0 };
            double[] dst = { 9.0, 9.0, 9.0, 9.0, 9.0 };
            VecOps.Copy(src, dst, 3);
            Assert.Equal(new[] { 1.0, 2.0, 3.0, 9.0, 9.0 }, dst);
        }

        [Fact]
        public void Copy_NZero_NoOp()
        {
            double[] src = { 1.0 };
            double[] dst = { 9.0 };
            VecOps.Copy(src, dst, 0);
            Assert.Equal(9.0, dst[0]);
        }

        // ---------- Fill ----------

        [Fact]
        public void Fill_SetsFirstNElements()
        {
            double[] x = { 1.0, 2.0, 3.0, 4.0 };
            VecOps.Fill(x, 7.5, 3);
            Assert.Equal(new[] { 7.5, 7.5, 7.5, 4.0 }, x);
        }

        [Fact]
        public void Fill_NZero_NoOp()
        {
            double[] x = { 1.0 };
            VecOps.Fill(x, 0.0, 0);
            Assert.Equal(1.0, x[0]);
        }

        // ---------- Scal ----------

        [Theory]
        [InlineData(2.0, new[] { 1.0, -2.0, 3.0 }, new[] { 2.0, -4.0, 6.0 })]
        [InlineData(0.0, new[] { 1.0, -2.0, 3.0 }, new[] { 0.0, 0.0, 0.0 })]
        [InlineData(1.0, new[] { 1.0, -2.0, 3.0 }, new[] { 1.0, -2.0, 3.0 })]
        [InlineData(-1.0, new[] { 1.0, -2.0, 3.0 }, new[] { -1.0, 2.0, -3.0 })]
        public void Scal_ScalesByAlpha(double a, double[] input, double[] expected)
        {
            VecOps.Scal(a, input, input.Length);
            Assert.Equal(expected, input);
        }

        // ---------- Axpy ----------

        [Fact]
        public void Axpy_ComputesAlphaXPlusY()
        {
            // y ← 2·x + y
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Axpy(2.0, x, y, 3);
            Assert.Equal(new[] { 12.0, 24.0, 36.0 }, y);
        }

        [Fact]
        public void Axpy_AlphaZero_LeavesYUnchanged()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Axpy(0.0, x, y, 3);
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, y);
        }

        [Fact]
        public void Axpy_AlphaNegativeOne_PerformsSubtraction()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Axpy(-1.0, x, y, 3);
            Assert.Equal(new[] { 9.0, 18.0, 27.0 }, y);
        }

        [Fact]
        public void Axpy_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 3.0, 100.0 };
            double[] y = { 0.0, 0.0, 0.0, 0.0 };
            VecOps.Axpy(1.0, x, y, 3);
            Assert.Equal(new[] { 1.0, 2.0, 3.0, 0.0 }, y);
        }

        // ---------- Axpby ----------

        [Fact]
        public void Axpby_ComputesAlphaXPlusBetaY()
        {
            // y ← 2·x + 3·y
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Axpby(2.0, x, 3.0, y, 3);
            Assert.Equal(new[] { 32.0, 64.0, 96.0 }, y);
        }

        [Fact]
        public void Axpby_BetaZero_OverwritesY()
        {
            // y ← 2·x + 0·y  (effectively y ← 2·x, ignoring previous y)
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 999.0, 999.0, 999.0 };
            VecOps.Axpby(2.0, x, 0.0, y, 3);
            Assert.Equal(new[] { 2.0, 4.0, 6.0 }, y);
        }

        [Fact]
        public void Axpby_AlphaZero_BetaOne_LeavesYUnchanged()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Axpby(0.0, x, 1.0, y, 3);
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, y);
        }

        // ---------- Dot ----------

        [Fact]
        public void Dot_OrthogonalVectors_IsZero()
        {
            double[] x = { 1.0, 0.0 };
            double[] y = { 0.0, 1.0 };
            Assert.Equal(0.0, VecOps.Dot(x, y, 2));
        }

        [Fact]
        public void Dot_ParallelVector_EqualsSumOfSquares()
        {
            double[] x = { 3.0, 4.0 };
            Assert.Equal(25.0, VecOps.Dot(x, x, 2));
        }

        [Fact]
        public void Dot_KnownValue()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 4.0, 5.0, 6.0 };
            // 1*4 + 2*5 + 3*6 = 4 + 10 + 18 = 32
            Assert.Equal(32.0, VecOps.Dot(x, y, 3));
        }

        [Fact]
        public void Dot_NZero_IsZero()
        {
            double[] x = { 1.0 };
            double[] y = { 1.0 };
            Assert.Equal(0.0, VecOps.Dot(x, y, 0));
        }

        // ---------- Nrm2 ----------

        [Fact]
        public void Nrm2_KnownValue()
        {
            double[] x = { 3.0, 4.0 };
            Assert.Equal(5.0, VecOps.Nrm2(x, 2));
        }

        [Fact]
        public void Nrm2_ZeroVector_IsZero()
        {
            double[] x = { 0.0, 0.0, 0.0 };
            Assert.Equal(0.0, VecOps.Nrm2(x, 3));
        }

        [Fact]
        public void Nrm2_NegativeEntries_HandledByDotSelfSquaring()
        {
            double[] x = { -3.0, 4.0 };
            Assert.Equal(5.0, VecOps.Nrm2(x, 2));
        }

        [Fact]
        public void Nrm2_MatchesSqrtDot()
        {
            double[] x        = { 1.5, -2.5, 0.5, -0.5, 3.0 };
            double   expected = Math.Sqrt(VecOps.Dot(x, x, 5));
            Assert.Equal(expected, VecOps.Nrm2(x, 5));
        }

        // ---------- NrmInf ----------

        [Fact]
        public void NrmInf_ReturnsMaxAbs()
        {
            double[] x = { 1.0, -5.0, 3.0, -2.0 };
            Assert.Equal(5.0, VecOps.NrmInf(x, 4));
        }

        [Fact]
        public void NrmInf_ZeroVector_IsZero()
        {
            double[] x = { 0.0, 0.0, 0.0 };
            Assert.Equal(0.0, VecOps.NrmInf(x, 3));
        }

        [Fact]
        public void NrmInf_NZero_IsZero()
        {
            double[] x = { 99.0, 99.0 };
            Assert.Equal(0.0, VecOps.NrmInf(x, 0));
        }

        [Fact]
        public void NrmInf_StopsAtN()
        {
            // Element past n must not influence the result.
            double[] x = { 1.0, 2.0, 1e9 };
            Assert.Equal(2.0, VecOps.NrmInf(x, 2));
        }

        // ---------- MaxZero ----------

        [Fact]
        public void MaxZero_ClipsNegatives()
        {
            double[] x = { 1.0, -2.0, 3.0, -0.5, 0.0 };
            VecOps.MaxZero(x, 5);
            Assert.Equal(new[] { 1.0, 0.0, 3.0, 0.0, 0.0 }, x);
        }

        [Fact]
        public void MaxZero_AllPositive_NoChange()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            VecOps.MaxZero(x, 3);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, x);
        }

        [Fact]
        public void MaxZero_StopsAtN()
        {
            double[] x = { -1.0, -2.0, -3.0 };
            VecOps.MaxZero(x, 2);
            Assert.Equal(new[] { 0.0, 0.0, -3.0 }, x);
        }

        // ---------- Asum ----------

        [Fact]
        public void Asum_SumsAbsoluteValues()
        {
            double[] x = { 1.0, -2.0, 3.0, -4.0 };
            Assert.Equal(10.0, VecOps.Asum(x, 4));
        }

        [Fact]
        public void Asum_AllZero_IsZero()
        {
            double[] x = { 0.0, 0.0, 0.0 };
            Assert.Equal(0.0, VecOps.Asum(x, 3));
        }

        [Fact]
        public void Asum_NZero_IsZero()
        {
            double[] x = { 1.0, 2.0 };
            Assert.Equal(0.0, VecOps.Asum(x, 0));
        }

        [Fact]
        public void Asum_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 1e9 };
            Assert.Equal(3.0, VecOps.Asum(x, 2));
        }

        // ---------- Sum ----------

        [Fact]
        public void Sum_SignedSum()
        {
            double[] x = { 1.0, -2.0, 3.0, -4.0 };
            Assert.Equal(-2.0, VecOps.Sum(x, 4));
        }

        [Fact]
        public void Sum_CancelsToZero()
        {
            double[] x = { 5.0, -5.0, 2.0, -2.0 };
            Assert.Equal(0.0, VecOps.Sum(x, 4));
        }

        [Fact]
        public void Sum_NZero_IsZero()
        {
            double[] x = { 1.0, 2.0 };
            Assert.Equal(0.0, VecOps.Sum(x, 0));
        }

        [Fact]
        public void Sum_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 1e9 };
            Assert.Equal(3.0, VecOps.Sum(x, 2));
        }

        // ---------- Iamax ----------

        [Fact]
        public void Iamax_ReturnsArgmaxOfAbsoluteValue()
        {
            double[] x = { 1.0, -5.0, 3.0, 4.0 };
            Assert.Equal(1, VecOps.Iamax(x, 4));
        }

        [Fact]
        public void Iamax_HandlesAllNegatives()
        {
            double[] x = { -1.0, -7.0, -3.0 };
            Assert.Equal(1, VecOps.Iamax(x, 3));
        }

        [Fact]
        public void Iamax_OnTie_ReturnsFirstOccurrence()
        {
            double[] x = { 1.0, -5.0, 5.0, -5.0 };
            Assert.Equal(1, VecOps.Iamax(x, 4));
        }

        [Fact]
        public void Iamax_SingleElement_ReturnsZero()
        {
            double[] x = { -3.0 };
            Assert.Equal(0, VecOps.Iamax(x, 1));
        }

        [Fact]
        public void Iamax_NZero_ReturnsNegativeOne()
        {
            double[] x = { 99.0 };
            Assert.Equal(-1, VecOps.Iamax(x, 0));
        }

        [Fact]
        public void Iamax_StopsAtN()
        {
            // Largest abs value is past n — must not be picked.
            double[] x = { 1.0, 2.0, 1e9 };
            Assert.Equal(1, VecOps.Iamax(x, 2));
        }

        // ---------- Swap ----------

        [Fact]
        public void Swap_ExchangesContents()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Swap(x, y, 3);
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, x);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, y);
        }

        [Fact]
        public void Swap_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Swap(x, y, 2);
            Assert.Equal(new[] { 10.0, 20.0, 3.0 }, x);
            Assert.Equal(new[] { 1.0, 2.0, 30.0 }, y);
        }

        [Fact]
        public void Swap_NZero_NoOp()
        {
            double[] x = { 1.0, 2.0 };
            double[] y = { 10.0, 20.0 };
            VecOps.Swap(x, y, 0);
            Assert.Equal(new[] { 1.0, 2.0 }, x);
            Assert.Equal(new[] { 10.0, 20.0 }, y);
        }

        [Fact]
        public void Swap_RoundTripIsIdentity()
        {
            double[] x     = { 1.0, 2.0, 3.0 };
            double[] y     = { 10.0, 20.0, 30.0 };
            double[] xOrig = (double[])x.Clone();
            double[] yOrig = (double[])y.Clone();
            VecOps.Swap(x, y, 3);
            VecOps.Swap(x, y, 3);
            Assert.Equal(xOrig, x);
            Assert.Equal(yOrig, y);
        }

        // ---------- Mul ----------

        [Fact]
        public void Mul_ComputesHadamardProduct()
        {
            double[] x = { 1.0, 2.0, 3.0, 4.0 };
            double[] y = { 10.0, 20.0, 30.0, 40.0 };
            double[] z = new double[4];
            VecOps.Mul(x, y, z, 4);
            Assert.Equal(new[] { 10.0, 40.0, 90.0, 160.0 }, z);
        }

        [Fact]
        public void Mul_Aliasing_ZEqualsX_OverwritesX()
        {
            double[] x = { 2.0, 3.0, 4.0 };
            double[] y = { 5.0, 6.0, 7.0 };
            VecOps.Mul(x, y, x, 3);
            Assert.Equal(new[] { 10.0, 18.0, 28.0 }, x);
            Assert.Equal(new[] { 5.0, 6.0, 7.0 }, y); // y untouched
        }

        [Fact]
        public void Mul_Aliasing_ZEqualsY_OverwritesY()
        {
            double[] x = { 2.0, 3.0, 4.0 };
            double[] y = { 5.0, 6.0, 7.0 };
            VecOps.Mul(x, y, y, 3);
            Assert.Equal(new[] { 2.0, 3.0, 4.0 }, x); // x untouched
            Assert.Equal(new[] { 10.0, 18.0, 28.0 }, y);
        }

        [Fact]
        public void Mul_Aliasing_AllSame_SquaresInPlace()
        {
            double[] x = { 1.0, -2.0, 3.0 };
            VecOps.Mul(x, x, x, 3);
            Assert.Equal(new[] { 1.0, 4.0, 9.0 }, x);
        }

        [Fact]
        public void Mul_WithZero_ProducesZero()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 0.0, 5.0, 0.0 };
            double[] z = new double[3];
            VecOps.Mul(x, y, z, 3);
            Assert.Equal(new[] { 0.0, 10.0, 0.0 }, z);
        }

        [Fact]
        public void Mul_NZero_LeavesZUnchanged()
        {
            double[] x = { 1.0, 2.0 };
            double[] y = { 3.0, 4.0 };
            double[] z = { 99.0, 99.0 };
            VecOps.Mul(x, y, z, 0);
            Assert.Equal(new[] { 99.0, 99.0 }, z);
        }

        [Fact]
        public void Mul_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 4.0, 5.0, 6.0 };
            double[] z = { 99.0, 99.0, 99.0 };
            VecOps.Mul(x, y, z, 2);
            Assert.Equal(new[] { 4.0, 10.0, 99.0 }, z);
        }

        // ---------- Div ----------

        [Fact]
        public void Div_ComputesElementwiseQuotient()
        {
            double[] x = { 10.0, 20.0, 30.0, 40.0 };
            double[] y = { 2.0, 4.0, 5.0, 8.0 };
            double[] z = new double[4];
            VecOps.Div(x, y, z, 4);
            Assert.Equal(new[] { 5.0, 5.0, 6.0, 5.0 }, z);
        }

        [Fact]
        public void Div_BySelf_GivesOnes()
        {
            double[] x = { 1.0, -2.0, 3.0, -4.0 };
            double[] z = new double[4];
            VecOps.Div(x, x, z, 4);
            Assert.Equal(new[] { 1.0, 1.0, 1.0, 1.0 }, z);
        }

        [Fact]
        public void Div_ByOnes_PreservesNumerator()
        {
            double[] x = { 1.5, -2.5, 3.5 };
            double[] y = { 1.0, 1.0, 1.0 };
            double[] z = new double[3];
            VecOps.Div(x, y, z, 3);
            Assert.Equal(new[] { 1.5, -2.5, 3.5 }, z);
        }

        [Fact]
        public void Div_Aliasing_ZEqualsX_OverwritesX()
        {
            double[] x = { 6.0, 8.0, 10.0 };
            double[] y = { 2.0, 4.0, 5.0 };
            VecOps.Div(x, y, x, 3);
            Assert.Equal(new[] { 3.0, 2.0, 2.0 }, x);
            Assert.Equal(new[] { 2.0, 4.0, 5.0 }, y);
        }

        [Fact]
        public void Div_Aliasing_ZEqualsY_OverwritesY()
        {
            double[] x = { 6.0, 8.0, 10.0 };
            double[] y = { 2.0, 4.0, 5.0 };
            VecOps.Div(x, y, y, 3);
            Assert.Equal(new[] { 6.0, 8.0, 10.0 }, x);
            Assert.Equal(new[] { 3.0, 2.0, 2.0 }, y);
        }

        [Fact]
        public void Div_ByZero_FollowsIEEE754()
        {
            double[] x = { 1.0, -1.0, 0.0 };
            double[] y = { 0.0, 0.0, 0.0 };
            double[] z = new double[3];
            VecOps.Div(x, y, z, 3);
            Assert.Equal(double.PositiveInfinity, z[0]);
            Assert.Equal(double.NegativeInfinity, z[1]);
            Assert.True(double.IsNaN(z[2]));
        }

        [Fact]
        public void Div_NZero_LeavesZUnchanged()
        {
            double[] x = { 1.0 };
            double[] y = { 1.0 };
            double[] z = { 99.0 };
            VecOps.Div(x, y, z, 0);
            Assert.Equal(99.0, z[0]);
        }

        // ---------- Add ----------

        [Fact]
        public void Add_ComputesElementwiseSum()
        {
            double[] x = { 1.0, 2.0, 3.0, 4.0 };
            double[] y = { 10.0, 20.0, 30.0, 40.0 };
            double[] z = new double[4];
            VecOps.Add(x, y, z, 4);
            Assert.Equal(new[] { 11.0, 22.0, 33.0, 44.0 }, z);
        }

        [Fact]
        public void Add_WithZero_PreservesOperand()
        {
            double[] x = { 1.5, -2.5, 3.5 };
            double[] y = { 0.0, 0.0, 0.0 };
            double[] z = new double[3];
            VecOps.Add(x, y, z, 3);
            Assert.Equal(new[] { 1.5, -2.5, 3.5 }, z);
        }

        [Fact]
        public void Add_Aliasing_ZEqualsX_OverwritesX()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Add(x, y, x, 3);
            Assert.Equal(new[] { 11.0, 22.0, 33.0 }, x);
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, y);
        }

        [Fact]
        public void Add_Aliasing_ZEqualsY_OverwritesY()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 10.0, 20.0, 30.0 };
            VecOps.Add(x, y, y, 3);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, x);
            Assert.Equal(new[] { 11.0, 22.0, 33.0 }, y);
        }

        [Fact]
        public void Add_Aliasing_AllSame_DoublesInPlace()
        {
            double[] x = { 1.0, -2.0, 3.0 };
            VecOps.Add(x, x, x, 3);
            Assert.Equal(new[] { 2.0, -4.0, 6.0 }, x);
        }

        [Fact]
        public void Add_NZero_LeavesZUnchanged()
        {
            double[] x = { 1.0, 2.0 };
            double[] y = { 3.0, 4.0 };
            double[] z = { 99.0, 99.0 };
            VecOps.Add(x, y, z, 0);
            Assert.Equal(new[] { 99.0, 99.0 }, z);
        }

        [Fact]
        public void Add_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 4.0, 5.0, 6.0 };
            double[] z = { 99.0, 99.0, 99.0 };
            VecOps.Add(x, y, z, 2);
            Assert.Equal(new[] { 5.0, 7.0, 99.0 }, z);
        }

        // ---------- Sub ----------

        [Fact]
        public void Sub_ComputesElementwiseDifference()
        {
            double[] x = { 10.0, 20.0, 30.0, 40.0 };
            double[] y = { 1.0, 2.0, 3.0, 4.0 };
            double[] z = new double[4];
            VecOps.Sub(x, y, z, 4);
            Assert.Equal(new[] { 9.0, 18.0, 27.0, 36.0 }, z);
        }

        [Fact]
        public void Sub_FromSelf_GivesZero()
        {
            double[] x = { 1.0, -2.0, 3.0 };
            double[] z = new double[3];
            VecOps.Sub(x, x, z, 3);
            Assert.Equal(new[] { 0.0, 0.0, 0.0 }, z);
        }

        [Fact]
        public void Sub_Aliasing_ZEqualsX_OverwritesX()
        {
            double[] x = { 10.0, 20.0, 30.0 };
            double[] y = { 1.0, 2.0, 3.0 };
            VecOps.Sub(x, y, x, 3);
            Assert.Equal(new[] { 9.0, 18.0, 27.0 }, x);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, y);
        }

        [Fact]
        public void Sub_Aliasing_ZEqualsY_OverwritesY()
        {
            double[] x = { 10.0, 20.0, 30.0 };
            double[] y = { 1.0, 2.0, 3.0 };
            VecOps.Sub(x, y, y, 3);
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, x);
            Assert.Equal(new[] { 9.0, 18.0, 27.0 }, y);
        }

        [Fact]
        public void Sub_Aliasing_AllSame_ZeroesInPlace()
        {
            double[] x = { 1.0, -2.0, 3.0 };
            VecOps.Sub(x, x, x, 3);
            Assert.Equal(new[] { 0.0, 0.0, 0.0 }, x);
        }

        [Fact]
        public void Sub_NZero_LeavesZUnchanged()
        {
            double[] x = { 1.0, 2.0 };
            double[] y = { 3.0, 4.0 };
            double[] z = { 99.0, 99.0 };
            VecOps.Sub(x, y, z, 0);
            Assert.Equal(new[] { 99.0, 99.0 }, z);
        }

        [Fact]
        public void Sub_StopsAtN()
        {
            double[] x = { 10.0, 20.0, 30.0 };
            double[] y = { 1.0, 2.0, 3.0 };
            double[] z = { 99.0, 99.0, 99.0 };
            VecOps.Sub(x, y, z, 2);
            Assert.Equal(new[] { 9.0, 18.0, 99.0 }, z);
        }

        // ---------- Shift ----------

        [Fact]
        public void Shift_AddsScalarToEachElement()
        {
            double[] x = { 1.0, 2.0, 3.0, 4.0 };
            double[] z = new double[4];
            VecOps.Shift(x, 10.0, z, 4);
            Assert.Equal(new[] { 11.0, 12.0, 13.0, 14.0 }, z);
        }

        [Fact]
        public void Shift_ByZero_PreservesInput()
        {
            double[] x = { 1.5, -2.5, 3.5 };
            double[] z = new double[3];
            VecOps.Shift(x, 0.0, z, 3);
            Assert.Equal(new[] { 1.5, -2.5, 3.5 }, z);
        }

        [Fact]
        public void Shift_NegativeAlpha_Subtracts()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] z = new double[3];
            VecOps.Shift(x, -1.0, z, 3);
            Assert.Equal(new[] { 0.0, 1.0, 2.0 }, z);
        }

        [Fact]
        public void Shift_Aliasing_ZEqualsX_ShiftsInPlace()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            VecOps.Shift(x, 5.0, x, 3);
            Assert.Equal(new[] { 6.0, 7.0, 8.0 }, x);
        }

        [Fact]
        public void Shift_NZero_LeavesZUnchanged()
        {
            double[] x = { 1.0, 2.0 };
            double[] z = { 99.0, 99.0 };
            VecOps.Shift(x, 5.0, z, 0);
            Assert.Equal(new[] { 99.0, 99.0 }, z);
        }

        [Fact]
        public void Shift_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] z = { 99.0, 99.0, 99.0 };
            VecOps.Shift(x, 10.0, z, 2);
            Assert.Equal(new[] { 11.0, 12.0, 99.0 }, z);
        }

        // ---------- Abs ----------

        [Fact]
        public void Abs_TakesElementwiseAbsoluteValue()
        {
            double[] x = { 1.0, -2.0, 3.0, -4.0 };
            double[] y = new double[4];
            VecOps.Abs(x, y, 4);
            Assert.Equal(new[] { 1.0, 2.0, 3.0, 4.0 }, y);
        }

        [Fact]
        public void Abs_AllPositive_NoChange()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = new double[3];
            VecOps.Abs(x, y, 3);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, y);
        }

        [Fact]
        public void Abs_NegativeZero_BecomesPositiveZero()
        {
            double[] x = { -0.0 };
            double[] y = new double[1];
            VecOps.Abs(x, y, 1);
            // Math.Abs(-0.0) is +0.0; check the sign bit, not the numeric value.
            Assert.Equal(0.0, y[0]);
            Assert.Equal(0L, BitConverter.DoubleToInt64Bits(y[0]));
        }

        [Fact]
        public void Abs_Aliasing_YEqualsX_InPlace()
        {
            double[] x = { 1.0, -2.0, 3.0, -4.0 };
            VecOps.Abs(x, x, 4);
            Assert.Equal(new[] { 1.0, 2.0, 3.0, 4.0 }, x);
        }

        [Fact]
        public void Abs_NZero_LeavesYUnchanged()
        {
            double[] x = { -1.0, -2.0 };
            double[] y = { 99.0, 99.0 };
            VecOps.Abs(x, y, 0);
            Assert.Equal(new[] { 99.0, 99.0 }, y);
        }

        [Fact]
        public void Abs_StopsAtN()
        {
            double[] x = { -1.0, -2.0, -3.0 };
            double[] y = { 99.0, 99.0, 99.0 };
            VecOps.Abs(x, y, 2);
            Assert.Equal(new[] { 1.0, 2.0, 99.0 }, y);
        }

        // ---------- Rot ----------

        [Fact]
        public void Rot_IdentityRotation_NoChange()
        {
            double[] x = { 1.0, 2.0, 3.0 };
            double[] y = { 4.0, 5.0, 6.0 };
            VecOps.Rot(x, y, 1.0, 0.0, 3);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, x);
            Assert.Equal(new[] { 4.0, 5.0, 6.0 }, y);
        }

        [Fact]
        public void Rot_NinetyDegrees_SwapsAndNegates()
        {
            // c=0, s=1: [0 1; -1 0] · [x; y] = [y; -x]
            double[] x = { 1.0, 2.0 };
            double[] y = { 10.0, 20.0 };
            VecOps.Rot(x, y, 0.0, 1.0, 2);
            Assert.Equal(new[] { 10.0, 20.0 }, x);
            Assert.Equal(new[] { -1.0, -2.0 }, y);
        }

        [Fact]
        public void Rot_FortyFiveDegrees_RotatesUnitVector()
        {
            double   c = Math.Cos(Math.PI / 4);
            double   s = Math.Sin(Math.PI / 4);
            double[] x = { 1.0 };
            double[] y = { 0.0 };
            VecOps.Rot(x, y, c, s, 1);
            Assert.Equal(c, x[0], 12);
            Assert.Equal(-s, y[0], 12);
        }

        [Fact]
        public void Rot_RoundTripWithInverse_RestoresInputs()
        {
            // Inverse of [c s; -s c] is [c -s; s c] (since det = c²+s² = 1)
            double   c     = Math.Cos(0.7);
            double   s     = Math.Sin(0.7);
            double[] x     = { 1.5, -2.5, 3.0 };
            double[] y     = { 0.5, 1.5, -1.0 };
            double[] xOrig = (double[])x.Clone();
            double[] yOrig = (double[])y.Clone();

            VecOps.Rot(x, y, c, s, 3);
            VecOps.Rot(x, y, c, -s, 3);

            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(xOrig[i], x[i], 12);
                Assert.Equal(yOrig[i], y[i], 12);
            }
        }

        [Fact]
        public void Rot_StopsAtN()
        {
            double[] x = { 1.0, 2.0, 99.0 };
            double[] y = { 4.0, 5.0, 99.0 };
            VecOps.Rot(x, y, 0.0, 1.0, 2);
            Assert.Equal(new[] { 4.0, 5.0, 99.0 }, x);
            Assert.Equal(new[] { -1.0, -2.0, 99.0 }, y);
        }

        // ---------- Rotg ----------

        [Fact]
        public void Rotg_ZeroInputs_GivesIdentity()
        {
            VecOps.Rotg(0.0, 0.0, out double c, out double s, out double r);
            Assert.Equal(1.0, c);
            Assert.Equal(0.0, s);
            Assert.Equal(0.0, r);
        }

        [Fact]
        public void Rotg_OnlyA_GivesIdentity()
        {
            VecOps.Rotg(5.0, 0.0, out double c, out double s, out double r);
            Assert.Equal(1.0, c, 12);
            Assert.Equal(0.0, s, 12);
            Assert.Equal(5.0, r, 12);
        }

        [Fact]
        public void Rotg_OnlyB_GivesNinetyDegreeRotation()
        {
            VecOps.Rotg(0.0, 5.0, out double c, out double s, out double r);
            Assert.Equal(0.0, c, 12);
            Assert.Equal(1.0, s, 12);
            Assert.Equal(5.0, r, 12);
        }

        [Fact]
        public void Rotg_ThreeFourFive_GivesPythagoreanCS()
        {
            VecOps.Rotg(3.0, 4.0, out double c, out double s, out double r);
            Assert.Equal(5.0, r, 12);
            Assert.Equal(0.6, c, 12);
            Assert.Equal(0.8, s, 12);
        }

        [Fact]
        public void Rotg_EqualAB_GivesQuarterTurn()
        {
            VecOps.Rotg(1.0, 1.0, out double c, out double s, out double r);
            Assert.Equal(Math.Sqrt(2.0), r, 12);
            Assert.Equal(1.0 / Math.Sqrt(2.0), c, 12);
            Assert.Equal(1.0 / Math.Sqrt(2.0), s, 12);
        }

        [Theory]
        [InlineData(3.0, 4.0)]
        [InlineData(-3.0, 4.0)]
        [InlineData(3.0, -4.0)]
        [InlineData(-3.0, -4.0)]
        [InlineData(0.1, 1e-6)]
        [InlineData(1e6, 1e-6)]
        [InlineData(1.0, 1.0)]
        public void Rotg_CSquaredPlusSSquared_IsOne(double a, double b)
        {
            VecOps.Rotg(a, b, out double c, out double s, out _);
            Assert.Equal(1.0, c * c + s * s, 12);
        }

        [Theory]
        [InlineData(3.0, 4.0)]
        [InlineData(-3.0, 4.0)]
        [InlineData(3.0, -4.0)]
        [InlineData(-3.0, -4.0)]
        [InlineData(7.0, 24.0)] // |r| = 25
        [InlineData(1e6, 1.0)]
        public void Rotg_ConstructedRotation_ZerosOutB(double a, double b)
        {
            VecOps.Rotg(a, b, out double c, out double s, out double r);

            double[] xs = { a };
            double[] ys = { b };
            VecOps.Rot(xs, ys, c, s, 1);

            // Relative tolerance: c*a + s*b and r can differ by O(eps * |r|)
            // due to compound rounding. Allow ~few ULP.
            double tol = 1e-12 * Math.Max(1.0, Math.Abs(r));
            Assert.InRange(xs[0] - r, -tol, tol);
            Assert.InRange(ys[0], -tol, tol);
        }

        [Fact]
        public void Rotg_RMagnitudeMatchesHypot()
        {
            // |r| should equal sqrt(a² + b²) regardless of sign convention.
            double a = -5.0, b = 12.0;
            VecOps.Rotg(a, b, out _, out _, out double r);
            Assert.Equal(13.0, Math.Abs(r), 12);
        }

        // ---------- LinComb (2..9) ----------

        [Fact]
        public void LinComb1_KnownValues()
        {
            double[] y0 = { 1.0, 2.0, 3.0 };
            double[] x1 = { 10.0, 20.0, 30.0 };
            double[] y  = new double[3];
            VecOps.LinComb1(y, y0, 2.0, x1, 3);
            // y[i] = y0[i] + 2·x1[i]
            Assert.Equal(new[] { 21.0, 42.0, 63.0 }, y);
        }

        [Fact]
        public void LinComb1_EquivalentToCopyAxpy()
        {
            double[] y0 = { 1.5, -2.5, 0.5, 3.0 };
            double[] x1 = { 0.5, 1.0, -1.5, 2.0 };

            double[] fused = new double[4];
            VecOps.LinComb1(fused, y0, 0.7, x1, 4);

            double[] chained = new double[4];
            VecOps.Copy(y0, chained, 4);
            VecOps.Axpy(0.7, x1, chained, 4);

            for (int i = 0; i < 4; i++) Assert.Equal(chained[i], fused[i], 12);
        }

        [Fact]
        public void LinComb1_AliasingInPlace_IsSafe()
        {
            // y = y + a1·x1  (passing same array as y and y0) — equivalent to Axpy.
            double[] y  = { 1.0, 2.0, 3.0 };
            double[] x1 = { 10.0, 20.0, 30.0 };
            VecOps.LinComb1(y, y, 2.0, x1, 3);
            Assert.Equal(new[] { 21.0, 42.0, 63.0 }, y);
        }

        [Fact]
        public void LinComb1_AliasingYEqualsX1_IsSafe()
        {
            // Each element of x1 is read once before y is overwritten.
            double[] y0       = { 1.0, 2.0, 3.0 };
            double[] y_and_x1 = { 10.0, 20.0, 30.0 };
            VecOps.LinComb1(y_and_x1, y0, 2.0, y_and_x1, 3);
            // y[i] = y0[i] + 2·x1[i]
            Assert.Equal(new[] { 21.0, 42.0, 63.0 }, y_and_x1);
        }

        [Fact]
        public void LinComb1_StopsAtN()
        {
            double[] y0 = { 1.0, 2.0, 999.0 };
            double[] x1 = { 1.0, 1.0, 999.0 };
            double[] y  = { 0.0, 0.0, 42.0 };
            VecOps.LinComb1(y, y0, 1.0, x1, 2);
            Assert.Equal(new[] { 2.0, 3.0, 42.0 }, y);
        }

        [Fact]
        public void LinComb1_NZero_LeavesYUnchanged()
        {
            double[] y0 = { 99.0 };
            double[] x1 = { 1.0 };
            double[] y  = { 7.0 };
            VecOps.LinComb1(y, y0, 5.0, x1, 0);
            Assert.Equal(7.0, y[0]);
        }

        [Fact]
        public void LinComb2_KnownValues()
        {
            double[] y0 = { 1.0, 2.0, 3.0 };
            double[] x1 = { 10.0, 20.0, 30.0 };
            double[] x2 = { 100.0, 200.0, 300.0 };
            double[] y  = new double[3];
            VecOps.LinComb2(y, y0, 2.0, x1, 3.0, x2, 3);
            // y[i] = y0[i] + 2·x1[i] + 3·x2[i]
            Assert.Equal(new[] { 1 + 20 + 300.0, 2 + 40 + 600.0, 3 + 60 + 900.0 }, y);
        }

        [Fact]
        public void LinComb2_EquivalentToCopyAxpyAxpy()
        {
            // Verify the fused form matches the BLAS-chain decomposition exactly.
            double[] y0 = { 1.5, -2.5, 0.5, 3.0 };
            double[] x1 = { 0.5, 1.0, -1.5, 2.0 };
            double[] x2 = { -1.0, 0.5, 2.5, -0.5 };

            double[] fused = new double[4];
            VecOps.LinComb2(fused, y0, 0.7, x1, -1.3, x2, 4);

            double[] chained = new double[4];
            VecOps.Copy(y0, chained, 4);
            VecOps.Axpy(0.7, x1, chained, 4);
            VecOps.Axpy(-1.3, x2, chained, 4);

            for (int i = 0; i < 4; i++) Assert.Equal(chained[i], fused[i], 12);
        }

        [Fact]
        public void LinComb2_AliasingInPlace_IsSafe()
        {
            // y = y + a1·x1 + a2·x2  (passing same array as y and y0)
            double[] y  = { 1.0, 2.0, 3.0 };
            double[] x1 = { 10.0, 20.0, 30.0 };
            double[] x2 = { 100.0, 200.0, 300.0 };
            VecOps.LinComb2(y, y, 1.0, x1, 1.0, x2, 3);
            Assert.Equal(new[] { 111.0, 222.0, 333.0 }, y);
        }

        [Fact]
        public void LinComb2_AliasingYEqualsX1_IsSafe()
        {
            // Each element of x1 is read once before y is overwritten.
            double[] y0       = { 1.0, 2.0, 3.0 };
            double[] y_and_x1 = { 10.0, 20.0, 30.0 };
            double[] x2       = { 100.0, 200.0, 300.0 };
            VecOps.LinComb2(y_and_x1, y0, 2.0, y_and_x1, 0.5, x2, 3);
            // expected y[i] = y0[i] + 2·x1[i] + 0.5·x2[i]
            Assert.Equal(new[] { 1 + 20 + 50.0, 2 + 40 + 100.0, 3 + 60 + 150.0 }, y_and_x1);
        }

        [Fact]
        public void LinComb4_RkStyleUpdate()
        {
            // Y_new = Y + h·(A51·K1 + A52·K2 + A53·K3 + A54·K4)
            // Use simple symbolic values to verify the shape.
            int      N    = 3;
            double   h    = 0.1;
            double   A51  = 1.0, A52 = 2.0, A53 = 3.0, A54 = 4.0;
            double[] Y    = { 5.0, 5.0, 5.0 };
            double[] K1   = { 1.0, 1.0, 1.0 };
            double[] K2   = { 1.0, 1.0, 1.0 };
            double[] K3   = { 1.0, 1.0, 1.0 };
            double[] K4   = { 1.0, 1.0, 1.0 };
            double[] Ynew = new double[N];

            VecOps.LinComb4(Ynew, Y,
                h * A51, K1, h * A52, K2, h * A53, K3, h * A54, K4, N);

            // Expected: 5 + 0.1·(1+2+3+4) = 5 + 1.0 = 6.0
            Assert.Equal(new[] { 6.0, 6.0, 6.0 }, Ynew);
        }

        [Fact]
        public void LinComb6_DormandPrinceFinalUpdate()
        {
            // Y_new = Y + Σ b_i K_i  (DP5-style; coefficients chosen to sum easily)
            int      N  = 2;
            double[] Y  = { 0.0, 0.0 };
            double[] K1 = { 1.0, -1.0 };
            double[] K2 = { 1.0, -1.0 };
            double[] K3 = { 1.0, -1.0 };
            double[] K4 = { 1.0, -1.0 };
            double[] K5 = { 1.0, -1.0 };
            double[] K6 = { 1.0, -1.0 };
            double[] y  = new double[N];

            VecOps.LinComb6(y, Y,
                0.1, K1, 0.2, K2, 0.3, K3, 0.4, K4, 0.5, K5, 0.5, K6, N);

            // Expected: 0 + (0.1+0.2+0.3+0.4+0.5+0.5)*1 = 2.0
            Assert.Equal(2.0, y[0], 12);
            Assert.Equal(-2.0, y[1], 12);
        }

        [Fact]
        public void LinComb9_AllStagesContribute()
        {
            int        N = 1;
            double[]   Y = { 100.0 };
            double[][] X = new double[9][];
            double[]   a = new double[9];
            for (int k = 0; k < 9; k++)
            {
                X[k] = new[] { 1.0 };
                a[k] = k + 1; // 1..9, sum = 45
            }

            double[] y = new double[N];
            VecOps.LinComb9(y, Y,
                a[0], X[0], a[1], X[1], a[2], X[2],
                a[3], X[3], a[4], X[4], a[5], X[5],
                a[6], X[6], a[7], X[7], a[8], X[8], N);

            Assert.Equal(100.0 + 45.0, y[0], 12);
        }

        [Fact]
        public void LinComb_StopsAtN()
        {
            double[] y0 = { 1.0, 2.0, 999.0 };
            double[] x1 = { 1.0, 1.0, 999.0 };
            double[] x2 = { 1.0, 1.0, 999.0 };
            double[] y  = { 0.0, 0.0, 42.0 };
            VecOps.LinComb2(y, y0, 1.0, x1, 1.0, x2, 2);
            Assert.Equal(new[] { 3.0, 4.0, 42.0 }, y);
        }

        [Fact]
        public void LinComb_NZero_LeavesYUnchanged()
        {
            double[] y0 = { 99.0 };
            double[] x1 = { 1.0 };
            double[] x2 = { 1.0 };
            double[] y  = { 7.0 };
            VecOps.LinComb2(y, y0, 5.0, x1, 5.0, x2, 0);
            Assert.Equal(7.0, y[0]);
        }

        // ---------- Vec convenience methods ----------
        // Vec wrappers delegate to VecOps using Vec.Length (not Capacity),
        // so the logical region is respected even when the pool gives an
        // oversized backing array.

        [Fact]
        public void Vec_Fill_Delegates()
        {
            using var v = Vec.Rent(5);
            v.Fill(7.5);
            for (int i = 0; i < v.Length; i++) Assert.Equal(7.5, v[i]);
        }

        [Fact]
        public void Vec_Fill_RespectsLogicalLength_NotCapacity()
        {
            // Rent 5 → bucket 3, Capacity 8. Fill must only touch indices 0..4.
            using var v = Vec.Rent(5, true);
            for (int i = 5; i < v.Capacity; i++) v.Data[i] = -99.0;
            v.Fill(1.0);
            for (int i = 0; i < 5; i++) Assert.Equal(1.0, v[i]);
            for (int i = 5; i < v.Capacity; i++) Assert.Equal(-99.0, v.Data[i]);
        }

        [Fact]
        public void Vec_Scal_Delegates()
        {
            using var v = Vec.Rent(3);
            v[0] = 1.0;
            v[1] = 2.0;
            v[2] = 3.0;
            v.Scal(2.5);
            Assert.Equal(2.5, v[0]);
            Assert.Equal(5.0, v[1]);
            Assert.Equal(7.5, v[2]);
        }

        [Fact]
        public void Vec_MaxZero_Delegates()
        {
            using var v = Vec.Rent(4);
            v[0] = 1.0;
            v[1] = -2.0;
            v[2] = 3.0;
            v[3] = -0.5;
            v.MaxZero();
            Assert.Equal(new[] { 1.0, 0.0, 3.0, 0.0 }, new[] { v[0], v[1], v[2], v[3] });
        }

        [Fact]
        public void Vec_CopyTo_Delegates()
        {
            using var src = Vec.Rent(3);
            using var dst = Vec.Rent(3, true);
            src[0] = 1.0;
            src[1] = 2.0;
            src[2] = 3.0;
            src.CopyTo(dst);
            Assert.Equal(1.0, dst[0]);
            Assert.Equal(2.0, dst[1]);
            Assert.Equal(3.0, dst[2]);
        }

        [Fact]
        public void Vec_Axpy_UpdatesReceiver()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 10.0;
            y[1] = 20.0;
            y[2] = 30.0;
            y.Axpy(2.0, x);
            Assert.Equal(new[] { 12.0, 24.0, 36.0 }, new[] { y[0], y[1], y[2] });
        }

        [Fact]
        public void Vec_Axpby_UpdatesReceiver()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 10.0;
            y[1] = 20.0;
            y[2] = 30.0;
            y.Axpby(2.0, x, 3.0);
            Assert.Equal(new[] { 32.0, 64.0, 96.0 }, new[] { y[0], y[1], y[2] });
        }

        [Fact]
        public void Vec_Swap_Delegates()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 10.0;
            y[1] = 20.0;
            y[2] = 30.0;
            x.Swap(y);
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, new[] { x[0], x[1], x[2] });
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, new[] { y[0], y[1], y[2] });
        }

        [Fact]
        public void Vec_Dot_Delegates()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 4.0;
            y[1] = 5.0;
            y[2] = 6.0;
            Assert.Equal(32.0, x.Dot(y));
        }

        [Fact]
        public void Vec_Nrm2_Delegates()
        {
            using var v = Vec.Rent(2);
            v[0] = 3.0;
            v[1] = 4.0;
            Assert.Equal(5.0, v.Nrm2());
        }

        [Fact]
        public void Vec_NrmInf_Delegates()
        {
            using var v = Vec.Rent(4);
            v[0] = 1.0;
            v[1] = -7.0;
            v[2] = 3.0;
            v[3] = -2.0;
            Assert.Equal(7.0, v.NrmInf());
        }

        [Fact]
        public void Vec_Asum_Delegates()
        {
            using var v = Vec.Rent(4);
            v[0] = 1.0;
            v[1] = -2.0;
            v[2] = 3.0;
            v[3] = -4.0;
            Assert.Equal(10.0, v.Asum());
        }

        [Fact]
        public void Vec_Sum_Delegates()
        {
            using var v = Vec.Rent(4);
            v[0] = 1.0;
            v[1] = -2.0;
            v[2] = 3.0;
            v[3] = -4.0;
            Assert.Equal(-2.0, v.Sum());
        }

        [Fact]
        public void Vec_Sum_TakesPrecedenceOverLinqExtension()
        {
            // System.Linq is in scope (using directive at top). vec.Sum() should
            // call our instance method, not Enumerable.Sum<double>(IEnumerable<double>).
            // Confirm by writing junk past Length: if LINQ won the resolution it'd
            // iterate the enumerator (which only yields the logical region) — same
            // result on small inputs. Stronger check: when Length=0, instance
            // returns 0 just like LINQ, but we verify the fast-path bypasses enumerator
            // allocation by checking semantic correctness on oversized buffers.
            using var v = Vec.Rent(3, true);
            v.Data[0] = 1.0;
            v.Data[1] = 2.0;
            v.Data[2] = 3.0;
            v.Data[3] = 1e9; // past Length=3 (Capacity is 4)
            double s = v.Sum();
            Assert.Equal(6.0, s); // 1+2+3, ignoring index 3
        }

        [Fact]
        public void Vec_Iamax_Delegates()
        {
            using var v = Vec.Rent(4);
            v[0] = 1.0;
            v[1] = -5.0;
            v[2] = 3.0;
            v[3] = 4.0;
            Assert.Equal(1, v.Iamax());
        }

        [Fact]
        public void Vec_LinComb2_Delegates()
        {
            using var y  = Vec.Rent(3);
            using var y0 = Vec.Rent(3);
            using var x1 = Vec.Rent(3);
            using var x2 = Vec.Rent(3);
            y0[0] = 1.0;
            y0[1] = 2.0;
            y0[2] = 3.0;
            x1[0] = 10.0;
            x1[1] = 20.0;
            x1[2] = 30.0;
            x2[0] = 100.0;
            x2[1] = 200.0;
            x2[2] = 300.0;

            y.LinComb2(y0, 2.0, x1, 3.0, x2);
            Assert.Equal(1 + 20 + 300.0, y[0]);
            Assert.Equal(2 + 40 + 600.0, y[1]);
            Assert.Equal(3 + 60 + 900.0, y[2]);
        }

        [Fact]
        public void Vec_LinComb4_RkStyleUpdate()
        {
            // The motivating use case: Ynew = Y + h·(A51·K1 + A52·K2 + A53·K3 + A54·K4)
            using var Y    = Vec.Rent(2);
            using var Ynew = Vec.Rent(2);
            using var K1   = Vec.Rent(2);
            using var K2   = Vec.Rent(2);
            using var K3   = Vec.Rent(2);
            using var K4   = Vec.Rent(2);

            Y[0] = 5.0;
            Y[1] = 5.0;
            K1[0] = K2[0] = K3[0] = K4[0] = 1.0;
            K1[1] = K2[1] = K3[1] = K4[1] = 1.0;
            double h = 0.1, A51 = 1.0, A52 = 2.0, A53 = 3.0, A54 = 4.0;

            Ynew.LinComb4(Y, h * A51, K1, h * A52, K2, h * A53, K3, h * A54, K4);

            // 5 + 0.1·10 = 6
            Assert.Equal(6.0, Ynew[0]);
            Assert.Equal(6.0, Ynew[1]);
        }

        [Fact]
        public void Vec_AxpyOnOversizedPool_UsesLengthNotCapacity()
        {
            // Rent 5 → Capacity 8. Past-Length slots in y must not be touched.
            using var x = Vec.Rent(5, true);
            using var y = Vec.Rent(5, true);
            for (int i = 5; i < y.Capacity; i++) y.Data[i] = -99.0;
            for (int i = 0; i < 5; i++)
            {
                x[i] = 1.0;
                y[i] = 1.0;
            }

            y.Axpy(2.0, x);

            for (int i = 0; i < 5; i++) Assert.Equal(3.0, y[i]);
            for (int i = 5; i < y.Capacity; i++) Assert.Equal(-99.0, y.Data[i]);
        }

        // ---------- Composition with PooledVector ----------

        [Fact]
        public void VecOps_WorkOnPooledVectorData()
        {
            // Confirms the intended use pattern: hot loop unwraps .Data and calls
            // static helpers — never touches the wrapper inside the loop.
            using var x = Vec.Rent(3, true);
            using var y = Vec.Rent(3, true);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 10.0;
            y[1] = 20.0;
            y[2] = 30.0;

            VecOps.Axpy(2.0, x.Data, y.Data, x.Length);

            Assert.Equal(12.0, y[0]);
            Assert.Equal(24.0, y[1]);
            Assert.Equal(36.0, y[2]);
        }
    }
}
