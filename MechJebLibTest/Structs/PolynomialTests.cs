using System;
using MechJebLib.Primitives;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Structs
{
    public class PolynomialTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public PolynomialTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private const double TOL = Polynomial.DEFAULT_ERROR;

        [Fact]
        private void BasicTest()
        {
            var p = new Polynomial(new double[] { 1, 2, 3 });
            p.N.ShouldEqual(2);
            p.Coef.Length.ShouldEqual(3);

            var p2 = new Polynomial(2);
            p2.N.ShouldEqual(2);
            p2.Coef.Length.ShouldEqual(3);
        }

        [Fact]
        private void EvalTest()
        {
            // 3x^2 + 2x + 1
            var p = new Polynomial(new double[] { 1, 2, 3 });
            p.Eval(0).ShouldEqual(1);
            p.Eval(1).ShouldEqual(6);
        }

        [Fact]
        private void EvalWithDerivative()
        {
            // 3x^2 + 2x + 1
            var p = new Polynomial(new double[] { 1, 2, 3 });
            p.EvalWithDerivative(0).Item1.ShouldEqual(1);
            p.EvalWithDerivative(0).Item2.ShouldEqual(2);
            p.EvalWithDerivative(1).Item1.ShouldEqual(6);
            p.EvalWithDerivative(1).Item2.ShouldEqual(8);
        }

        [Fact]
        private void Derivative()
        {
            // 3x^2 + 2x + 1
            var p = new Polynomial(new double[] { 1, 2, 3 });
            Polynomial d = p.Derivative();
            Assert.Equal(d.Coef, new double[] { 2, 6});

            d.Eval(0).ShouldEqual(2);
            d.Eval(1).ShouldEqual(8);
        }

        [Fact]
        private void Deflate()
        {
            // (x + 2) * (x - 1) = x^2 + x - 2
            var p = new Polynomial(new double[] { -2, 1, 1 });
            p.Eval(1).ShouldEqual(0);
            p.Eval(-2).ShouldEqual(0);
            Polynomial p2 = p.Deflate(1);
            Assert.Equal(p2.Coef, new double[] { 2, 1 });
            p2.Eval(-2).ShouldEqual(0);
            Polynomial p3 = p.Deflate(-2);
            Assert.Equal(p3.Coef, new double[] { -1, 1 });
            p3.Eval(1).ShouldEqual(0);
        }

        [Fact]
        private void Inflate()
        {
            // ( x - 1 ) ( x + -root )
            var p = new Polynomial(new double[] { -1, 1 });
            Polynomial p2 = p.Inflate(-2);
            Assert.Equal(p2.Coef, new double[] { -2, 1, 1 });
            p2.Eval(-2).ShouldEqual(0);
            p2.Eval(1).ShouldEqual(0);
        }

        [Fact]
        private void SimpleRoots()
        {
            var p = new Polynomial(new double[] { -1, 1 });
            double[] roots = new double[1];
            p.Roots(roots).ShouldEqual(1);
            roots[0].ShouldEqual(1, TOL);

            var p2 = new Polynomial(new double[] { 2, -3, 1 });
            double[] roots2 = new double[2];
            p2.Roots(roots2).ShouldEqual(2);
            roots2[0].ShouldEqual(1, TOL);
            roots2[1].ShouldEqual(2, TOL);

            var p3 = new Polynomial(new double[] { -6, 11, -6, 1 });
            double[] roots3 = new double[3];
            p3.Roots(roots3).ShouldEqual(3);
            roots3[0].ShouldEqual(1, TOL);
            roots3[1].ShouldEqual(2, TOL);
            roots3[2].ShouldEqual(3, TOL);

            var p4 = new Polynomial(new double[] { 24, -50, 35, -10, 1 });
            double[] roots4 = new double[4];
            p4.Roots(roots4).ShouldEqual(4);
            roots4[0].ShouldEqual(1, TOL);
            roots4[1].ShouldEqual(2, TOL);
            roots4[2].ShouldEqual(3, TOL);
            roots4[3].ShouldEqual(4, TOL);

            var p5 = new Polynomial(new double[] { -120, 274, -225, 85, -15, 1 });
            double[] roots5 = new double[5];
            p5.Roots(roots5).ShouldEqual(5);
            roots5[0].ShouldEqual(1, TOL);
            roots5[1].ShouldEqual(2, TOL);
            roots5[2].ShouldEqual(3, TOL);
            roots5[3].ShouldEqual(4, TOL);
            roots5[4].ShouldEqual(5, TOL);
        }

        [Fact]
        private void HornerTest()
        {
            var p = new Polynomial(new double[]
            {
                243 / 1024.0, -4293 / 1024.0, 35505 / 1024.0, -182475 / 1024.0, 326145 / 512.0, -859865 / 512.0, 1729585 / 512.0, -2707595 / 512.0,
                6667815 / 1024.0, -6479385 / 1024.0, 4952541 / 1024.0, -2946351 / 1024.0, 334365 / 256.0, -13995 / 32.0, 815 / 8.0, -59 / 4.0, 1.0
            });

            // 0.9935,1.0065
            for (double i = 0.9935; i <= 1.0065; i += (1.0065-0.9935) / 100)
            {
                _testOutputHelper.WriteLine($"{i}: {p.Eval(i)}");
            }
        }

        [Fact]
        private void CloseRoots()
        {
            // [-44.149638011189943,-45.905312521339077,-41.133492622121,-45.90529538500369]
            // [3826911.9367215615,346447.5884191448,11753.221808035894,177.09373853965371,1]
            var p = new Polynomial(new[] { 3826911.936721561, 346447.5884191448, 11753.221808035893, 177.0937385396537, 1 });


            double delta = 45.905312521339077 - 45.90529538500369;
            for (double x = -45.905312521339077 - delta; x <= -45.90529538500369 + delta; x += delta / 100)
            {
                double y = p.Eval(x);
                _testOutputHelper.WriteLine($"p({x}) = {y}");
            }

            //p.Eval(-45.905312521339077).ShouldEqual(0); // 2.7398955294959734E-09 vs 2.7939677238464355E-09
            double[] roots = new double[4];
            p.Roots(roots, 0, true).ShouldEqual(4);
        }

        [Fact]
        private void RandomQuarticFourRealRoots()
        {
            var random = new Random();
            const int NTRIALS = 5000;

            for (int i = 0; i < NTRIALS; i++)
            {
                double r1 = random.NextDouble() * 100 - 50;
                double r2 = random.NextDouble() * 100 - 50;
                double r3 = random.NextDouble() * 100 - 50;
                double r4 = random.NextDouble() * 100 - 50;
                double[] expectedRoots = { r1, r2, r3, r4 };

                double c0 = r1 * r2 * r3 * r4;
                double c1 = -r1 * r2 * r3 - r1 * r2 * r4 - r1 * r3 * r4 - r2 * r3 * r4;
                double c2 = r1 * r2 + r1 * r3 + r2 * r3 + r1 * r4 + r2 * r4 + r3 * r4;
                double c3 = -r1 - r2 - r3 - r4;

                var p = new Polynomial(new[] { c0, c1, c2, c3, 1 });

                double[] roots = new double[4];
                //p.Roots(roots, xError: 0, boundError: true).ShouldEqual(4);
                //roots.ShouldContain(expectedRoots, 1e-10);

                try
                {
                    p.Roots(roots, 0, true).ShouldEqual(4);
                    roots.ShouldContain(expectedRoots, 1e-10);
                }
                catch (Exception)
                {
                    _testOutputHelper.WriteLine(DoubleArrayString(expectedRoots));
                    _testOutputHelper.WriteLine(DoubleArrayString(p.Coef));
                    throw;
                }

                //p.Roots(roots, boundError: true).ShouldEqual(4);
                //roots.ShouldContain(expectedRoots, TOL*2);
            }
        }
    }
}
