using MechJebLib.Rootfinding;
using Xunit;
using static System.Math;

namespace MechJebLibTest.RootfindingTests
{
    public class NewtonTests
    {
        private static double F1(double x, object? o)  => x * x * x;
        private static double DF1(double x, object? o) => 3 * x * x;

        private static double F2(double x, object? o)  => x * x - 2 * x - 1;
        private static double DF2(double x, object? o) => 2 * x - 2;

        private static double F3(double x, object? o)  => Exp(x) - Cos(x);
        private static double DF3(double x, object? o) => Exp(x) + Sin(x);

        // x = y^3
        private static double F4(double x, object? o)  => Sign(x) * Pow(Abs(x), 1 / 3.0);
        private static double DF4(double x, object? o) => 1.0 / (3.0 * Pow(Abs(x), 2.0 / 3.0));

        // y = x^3 - 2x + 2
        private static double F5(double x, object? o)  => x * x * x - 2 * x + 2;
        private static double DF5(double x, object? o) => 3 * x * x - 2;

        [Fact]
        private void TestEndpoints()
        {
            double x = Newton.Solve(F1, DF1, -2.0, 1.0, null);

            x.ShouldBeZero(1e-15);
            F1(x, null).ShouldBeZero();

            x = Newton.Solve(F1, DF1, 1.0, -2.0, null);

            x.ShouldBeZero(1e-15);
            F1(x, null).ShouldBeZero();

            x = Newton.Solve(F1, DF1, 0, 1.0, null);

            x.ShouldBeZero(1e-15);
            F1(x, null).ShouldBeZero();

            x = Newton.Solve(F1, DF1, -1.0, 0, null);

            x.ShouldBeZero(1e-15);
            F1(x, null).ShouldBeZero();

            x = Newton.Solve(F1, DF1, 1.0, 0, null);

            x.ShouldBeZero(1e-15);
            F1(x, null).ShouldBeZero();

            x = Newton.Solve(F1, DF1, 0, -1.0, null);

            x.ShouldBeZero(1e-15);
            F1(x, null).ShouldBeZero();
        }

        [Fact]
        private void TestQuadratic()
        {
            double x = Newton.Solve(F2, DF2, -1.0, 1, null);

            x.ShouldEqual(1 - Sqrt(2), 1e-15);
            F2(x, null).ShouldBeZero();

            x = Newton.Solve(F2, DF2, 1, -1.0, null);

            x.ShouldEqual(1 - Sqrt(2), 1e-15);
            F2(x, null).ShouldBeZero();

            x = Newton.Solve(F2, DF2, 1, 3, null);

            x.ShouldEqual(1 + Sqrt(2), 1e-15);
            F2(x, null).ShouldBeZero();

            x = Newton.Solve(F2, DF2, 3, 1, null);

            x.ShouldEqual(1 + Sqrt(2), 1e-15);
            F2(x, null).ShouldBeZero();
        }

        [Fact]
        private void TestTranscendental()
        {
            double x = Newton.Solve(F3, DF3, -6, -4, null);
            x.ShouldEqual(-4.7212927588476861);
            F3(x, null).ShouldBeZero();

            x = Newton.Solve(F3, DF3, -4, -6, null);
            x.ShouldEqual(-4.7212927588476861);
            F3(x, null).ShouldBeZero();

            x = Newton.Solve(F3, DF3, -8, -6, null);
            x.ShouldEqual(-7.853593279971248);
            F3(x, null).ShouldBeZero();

            x = Newton.Solve(F3, DF3, -6, -8, null);
            x.ShouldEqual(-7.853593279971248);
            F3(x, null).ShouldBeZero();

            x = Newton.Solve(F3, DF3, -1, 2, null);
            x.ShouldBeZero();
            F3(x, null).ShouldBeZero();

            x = Newton.Solve(F3, DF3, 2, -1, null);
            x.ShouldBeZero();
            F3(x, null).ShouldBeZero();
        }

        [Fact]
        private void JumpingOutOfRange()
        {
            // this must get a near zero derivative and jumps and finds an out of bounds zero
            double x = Newton.Solve(F3, DF3, -1.78852734594335, -4.99845444737331, null);
            x.ShouldEqual(-4.7212927588476861);
            F3(x, null).ShouldBeZero();
        }

        [Fact]
        private void NeedsBisect()
        {
            // this has an infinite slope at the solution
            double x = Newton.Solve(F4, DF4, -6.08755791377628, 7.46999802415725, null);
            x.ShouldBeZero(1e-15);
            F4(x, null).ShouldBeZero(1e-5);
        }

        [Fact]
        private void Cycles()
        {
            // this cycles with an intial guess of zero (bounds chosen so the initial guess cycles)
            double x = Newton.Solve(F5, DF5, -3, 3, null);
            x.ShouldEqual(-1.7692923542386314);
            F5(x, null).ShouldBeZero(1e-15);
        }
    }
}
