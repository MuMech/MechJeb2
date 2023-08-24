/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using AssertExtensions;
using MechJebLib.Core.Functions;
using MechJebLib.Core.TwoBody;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Maths
{
    public class FunctionsTests
    {
        private const double PERIOD = 86164.0905;

        private const double ACC  = EPS * 16;
        private const double ACC2 = 1e-7; // due west launches have some mathematical irregularities

        private readonly ITestOutputHelper _testOutputHelper;

        public FunctionsTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void HeadingForInclinationTest1()
        {
            double heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(0), 0);
            heading.ShouldEqual(Deg2Rad(90), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(45), 0);
            heading.ShouldEqual(Deg2Rad(45), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(90), 0);
            heading.ShouldEqual(Deg2Rad(0), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(135), 0);
            heading.ShouldEqual(Deg2Rad(315), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(180), 0);
            heading.ShouldEqual(Deg2Rad(270), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(-45), 0);
            heading.ShouldEqual(Deg2Rad(135), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(-90), 0);
            heading.ShouldEqual(Deg2Rad(180), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(-135), 0);
            heading.ShouldEqual(Deg2Rad(225), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(-180), 0);
            heading.ShouldEqual(Deg2Rad(270), 1e-15);

            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(0), Deg2Rad(45));
            heading.ShouldEqual(Deg2Rad(90), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(45), Deg2Rad(45));
            heading.ShouldEqual(Deg2Rad(90), 1e-15);
            heading = MechJebLib.Core.Maths.HeadingForInclination(Deg2Rad(90), Deg2Rad(45));
            heading.ShouldEqual(Deg2Rad(0), 1e-15);
        }

        [Fact]
        public void KeplerianFromStateVectorsTest1()
        {
            double smaEx = 3.843084377707066e+08;
            double eccEx = 5.328149353682574e-02;
            double incEx = 4.950221141769940e-01;
            double argpEx = 3.486541150390846e+00;
            double lanEx = 4.008351366616158e-02;
            double tanomEx = 7.853981633974483e-01;
            var r = new V3(
                -1.455451021873417e+08,
                -3.000298697925529e+08,
                -1.586943000620733e+08
            );
            var v = new V3(
                9.572921091669031e+02,
                -3.895747803416348e+02,
                -2.308551508912105e+02
            );
            double mu = 3.986004418000000e+14;

            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) =
                MechJebLib.Core.Maths.KeplerianFromStateVectors(mu, r, v);
            sma.ShouldEqual(smaEx, 4e-15);
            ecc.ShouldEqual(eccEx, 8e-15);
            inc.ShouldEqual(incEx);
            lan.ShouldEqual(lanEx, 4e-15);
            argp.ShouldEqual(argpEx, 4e-15);
            tanom.ShouldEqual(tanomEx, 8e-15);
        }

        [Fact]
        public void KeplerianFromStateVectorsTest2()
        {
            double sma, ecc, inc, lan, argp, tanom;
            (sma, ecc, inc, lan, argp, tanom, _) = MechJebLib.Core.Maths.KeplerianFromStateVectors(1.0, new V3(1, 0, 0), new V3(0, 0, 1));
            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(PI / 2);
            lan.ShouldEqual(0);
            argp.ShouldEqual(0);
            tanom.ShouldEqual(0);
            (sma, ecc, inc, lan, argp, tanom, _) = MechJebLib.Core.Maths.KeplerianFromStateVectors(1.0, new V3(1, 0, 0), new V3(0, 1, 0));
            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(0.0);
            lan.ShouldEqual(0);
            argp.ShouldEqual(0);
            tanom.ShouldEqual(0);
            (sma, ecc, inc, lan, argp, tanom, _) = MechJebLib.Core.Maths.KeplerianFromStateVectors(1.0, new V3(1, 0, 0), new V3(0, 0, -1));
            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(PI / 2);
            lan.ShouldEqual(PI);
            argp.ShouldEqual(0);
            tanom.ShouldEqual(PI);
            (sma, ecc, inc, lan, argp, tanom, _) = MechJebLib.Core.Maths.KeplerianFromStateVectors(1.0, new V3(0, 1, 0), new V3(-1, 0, 0));
            sma.ShouldEqual(1.0);
            ecc.ShouldEqual(0.0);
            inc.ShouldEqual(0.0);
            lan.ShouldEqual(0);
            argp.ShouldEqual(0);
            tanom.ShouldEqual(PI / 2);
        }

        [Fact]
        private void RandomOrbitalElementsForwardAndBack()
        {
            const int NTRIALS = 5000;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);

                (double _, double ecc, double inc, double lan, double argp, double nu, double l) =
                    MechJebLib.Core.Maths.KeplerianFromStateVectors(1.0, r0, v0);
                (V3 r02, V3 v02) = MechJebLib.Core.Maths.StateVectorsFromKeplerian(1.0, l, ecc, inc, lan, argp, nu);

                if ((r02 - r0).magnitude / r0.magnitude > 1e-8 || (v02 - v0).magnitude / v0.magnitude > 1e-8)
                {
                    _testOutputHelper.WriteLine($"r0: {r0} v0: {v0}\nr02: {r02} v02: {v02}\n");
                }

                r02.ShouldEqual(r0, 1e-8);
                v02.ShouldEqual(v0, 1e-8);
            }
        }

        [Fact]
        public void DeltaVToChangeInclinationTest1()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            double v185 = MechJebLib.Core.Maths.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = Maneuvers.DeltaVToChangeInclination(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = Maneuvers.DeltaVToChangeInclination(r0, v0, Deg2Rad(90));
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

            V3 dv = Maneuvers.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = Maneuvers.DeltaVToChangeFPA(r0, v0, Deg2Rad(90));
            Assert.Equal(v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            dv = Maneuvers.DeltaVToChangeFPA(r0, v0, Deg2Rad(-90));
            Assert.Equal(-v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Sqrt(2), v185 / Sqrt(2), 0);

            dv = Maneuvers.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Sqrt(2), dv.x, 9);
            Assert.Equal(v185 - v185 / Sqrt(2), dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Sqrt(2), 0, v185 / Sqrt(2));

            dv = Maneuvers.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Sqrt(2), dv.x, 9);
            Assert.Equal(0, dv.y, 9);
            Assert.Equal(v185 - v185 / Sqrt(2), dv.z, 9);
        }

        [Fact]
        public void ECIToPitchHeadingTest1()
        {
            (double pitch, double heading) = MechJebLib.Core.Maths.ECIToPitchHeading(new V3(10, 10, 0), new V3(0, 0, 10));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(0, heading, 9);
            (pitch, heading) = MechJebLib.Core.Maths.ECIToPitchHeading(new V3(10, 10, 0), new V3(0, 0, -10));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(Deg2Rad(180), heading, 9);
            (pitch, heading) = MechJebLib.Core.Maths.ECIToPitchHeading(new V3(10, 10, 0), new V3(-10, 10, 0));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(Deg2Rad(90), heading, 9);
            (pitch, heading) = MechJebLib.Core.Maths.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, -10, 0));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(Deg2Rad(270), heading, 9);
            (pitch, heading) = MechJebLib.Core.Maths.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, 10, 0));
            Assert.Equal(Deg2Rad(90), pitch, 9);
            Assert.Equal(Deg2Rad(90), heading, 9);
            (pitch, heading) = MechJebLib.Core.Maths.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, 10, Sqrt(200)));
            Assert.Equal(Deg2Rad(45), pitch, 9);
            Assert.Equal(0, heading, 9);
            (pitch, heading) = MechJebLib.Core.Maths.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, 10, -Sqrt(200)));
            Assert.Equal(Deg2Rad(45), pitch, 9);
            Assert.Equal(Deg2Rad(180), heading, 9);
        }

        [Fact]
        public void IncFromStateVectorsTest1()
        {
            double inc = MechJebLib.Core.Maths.IncFromStateVectors(new V3(10, 10, 0), new V3(0, 10, 0));
            Assert.Equal(0, inc);
            inc = MechJebLib.Core.Maths.IncFromStateVectors(new V3(10, 10, 0), new V3(0, 0, 10));
            Assert.Equal(PI / 2, inc);
            inc = MechJebLib.Core.Maths.IncFromStateVectors(new V3(10, 10, 0), new V3(0, -10, 0));
            Assert.Equal(PI, inc);
            inc = MechJebLib.Core.Maths.IncFromStateVectors(new V3(10, 10, 0), new V3(0, 0, -10));
            Assert.Equal(PI / 2, inc);
        }

        [Fact]
        public void VelocityForHeadingTest1()
        {
            V3 vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), 0);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], 10, 9);
            vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), PI);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], -10, 9);
            vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), 3 * PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], -10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), 0);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], 10, 9);
            vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), PI / 2);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], 10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), PI);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], -10, 9);
            vf = MechJebLib.Core.Maths.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), 3 * PI / 2);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], -10, 9);
            Assert.Equal(vf[2], 0, 9);
        }

        [Fact]
        public void VelocityForInclinationTest1()
        {
            V3 vf = MechJebLib.Core.Maths.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), 0);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = MechJebLib.Core.Maths.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], 10, 9);
            vf = MechJebLib.Core.Maths.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), PI);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], -10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = MechJebLib.Core.Maths.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), -PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], -10, 9);
            vf = MechJebLib.Core.Maths.VelocityForInclination(new V3(10, 0, 0), new V3(10, 10, 0), 0);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], 10, 9);
            Assert.Equal(vf[2], 0, 9);
        }

        [Fact]
        public void TimeToNextRadiusTest()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            const double r1000 = rearth + 1000e+3;

            (double sma, double ecc) = MechJebLib.Core.Maths.SmaEccFromApsides(r185, r1000);
            double v185 = MechJebLib.Core.Maths.VmagFromVisViva(mu, sma, r185);
            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            double period = MechJebLib.Core.Maths.PeriodFromStateVectors(mu, r0, v0);

            (V3 r1, V3 v1) = Shepperd.Solve(mu, period / 3.0, r0, v0);
            (V3 r2, V3 v2) = Shepperd.Solve(mu, period * 0.75, r0, v0);

            Assert.Equal(period / 3.0, MechJebLib.Core.Maths.TimeToNextRadius(mu, r0, v0, r1.magnitude), 9);
            Assert.Equal(period * 0.25, MechJebLib.Core.Maths.TimeToNextRadius(mu, r0, v0, r2.magnitude), 9);
            Assert.Equal(period / 2.0, MechJebLib.Core.Maths.TimeToNextRadius(mu, r0, v0, r1000), 9);
        }

        [Fact]
        public void ApoapsisFromStateVectorsTest()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            const double r1000 = rearth + 1000e+3;

            (double sma, double ecc) = MechJebLib.Core.Maths.SmaEccFromApsides(r185, r1000);
            double v185 = MechJebLib.Core.Maths.VmagFromVisViva(mu, sma, r185);
            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            Assert.Equal(1.0, MechJebLib.Core.Maths.ApoapsisFromStateVectors(mu, r0, v0) / r1000, 14);
            Assert.Equal(1.0, MechJebLib.Core.Maths.PeriapsisFromStateVectors(mu, r0, v0) / r185, 14);
        }

        [Fact]
        public void ApsidesFromKeplerianTest()
        {
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            const double r1000 = rearth + 1000e+3;

            (double sma, double ecc) = MechJebLib.Core.Maths.SmaEccFromApsides(r185, r1000);
            Assert.Equal(r185, MechJebLib.Core.Maths.PeriapsisFromKeplerian(sma, ecc));
            Assert.Equal(r1000, MechJebLib.Core.Maths.ApoapsisFromKeplerian(sma, ecc));
        }

        [Fact]
        private void Test90()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    double lng = i * 45;
                    double lan = j * 45;

                    // zero degree advance
                    double delay = PERIOD / 8 * ((j - i + 8) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                    }

                    // reverse
                    delay = PERIOD / 8 * ((i - j + 8) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                    }

                    // advance by 180 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 4) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 28.608, lng, lan, -90).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -28.608, lng, lan, -90).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 180 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 4) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 28.608, lng, lan, -90).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -28.608, lng, lan, -90).ShouldEqual(delay, ACC);
                    }
                }
            }
        }

        [Fact]
        private void Test45At45()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    double lng = i * 45;
                    double lan = j * 45;

                    // advance by 90 degrees
                    double delay = PERIOD / 8 * ((j - i + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 45, lng, lan, 45).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 45, lng, lan, -45).ShouldEqual(delay, ACC);
                    }

                    // advance by 270 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -45, lng, lan, 45).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -45, lng, lan, -45).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 45, lng, lan, 45).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 45, lng, lan, -45).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -45, lng, lan, 45).ShouldEqual(delay, ACC);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -45, lng, lan, -45).ShouldEqual(delay, ACC);
                    }
                }
            }
        }

        [Fact]
        private void Test135At45()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    double lng = i * 45;
                    double lan = j * 45;

                    // advance by 270 degrees
                    double delay = PERIOD / 8 * ((j - i + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 45, lng, lan, -135).ShouldEqual(delay, ACC2);
                    }

                    // advance by 90 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -45, lng, lan, -135).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 45, lng, lan, -135).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -45, lng, lan, -135).ShouldEqual(delay, ACC2);
                    }
                }
            }
        }

        [Fact]
        private void EquatorialAt45()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    double lng = i * 45;
                    double lan = j * 45;

                    // advance by 90 degrees
                    double delay = PERIOD / 8 * ((j - i + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 45, lng, lan, 0).ShouldEqual(delay, ACC);
                    }

                    // advance by 270 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -45, lng, lan, 0).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 45, lng, lan, 0).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -45, lng, lan, 0).ShouldEqual(delay, ACC);
                    }
                }
            }
        }

        [Fact]
        private void RetrogradeEquatorialAt45()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    double lng = i * 45;
                    double lan = j * 45;

                    // advance by 270 degrees
                    double delay = PERIOD / 8 * ((j - i + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 45, lng, lan, -180).ShouldEqual(delay, ACC2);
                    }

                    // advance by 90 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -45, lng, lan, -180).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 45, lng, lan, -180).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -45, lng, lan, -180).ShouldEqual(delay, ACC2);
                    }
                }
            }
        }

        [Fact]
        private void Test47AtKSCLat()
        {
            // this produces a 330 degree LAN from 28.608 so everything is 30 degrees offset
            const double inc = 47.486638356389;

            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    double lng = i * 30;
                    double lan = j * 30;

                    // advance by 30 degrees
                    double delay = PERIOD / 12 * ((j - i + 12 + 1) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 150 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 330 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 11) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 210 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 330 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 11) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 210 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 30 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 1) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 150 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }
                }
            }
        }

        [Fact]
        private void Test132AtKSCLat()
        {
            // similar to the 47 degree tests only retrograde
            const double inc = 180 - 47.486638356389;

            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    double lng = i * 30;
                    double lan = j * 30;

                    // advance by 330 degrees
                    double delay = PERIOD / 12 * ((j - i + 12 + 11) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 210 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 30 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 1) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 150 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 30 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 1) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 150 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 330 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 11) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 210 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        MechJebLib.Core.Maths.TimeToPlane(-PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }
                }
            }
        }

        [Fact]
        private void Poles()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    for (int k = 0; k <= 8; k++)
                    {
                        double lng = i * 45;
                        double lan = j * 45;
                        double inc = k * 45 - 180;
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, 90, lng, lan, inc).ShouldBeZero(ACC);
                        MechJebLib.Core.Maths.TimeToPlane(PERIOD, -90, lng, lan, inc).ShouldBeZero(ACC);
                    }
                }
            }
        }

        [Fact]
        private void ChangeOrbitalElementTest()
        {
            const int NTRIALS = 50;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double newR = random.NextDouble() * r.magnitude;

                double rscale = random.NextDouble() * 1.5e8 + 1;
                double vscale = random.NextDouble() * 3e4 + 1;
                r    *= rscale;
                v    *= vscale;
                newR *= rscale;
                double mu = rscale * vscale * vscale;

                V3 dv = ChangeOrbitalElement.ChangePeriapsis(mu, r, v, newR, true);
                MechJebLib.Core.Maths.PeriapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newR, 1e-9);

                // validate this API works left handed.
                V3 dv2 = ChangeOrbitalElement.ChangePeriapsis(mu, r.xzy, v.xzy, newR);
                dv2.ShouldEqual(dv.xzy, 1e-3);

                // now test apoapsis changing to elliptical
                newR = random.NextDouble() * rscale * 1e9 + r.magnitude;
                V3 dv3 = ChangeOrbitalElement.ChangeApoapsis(mu, r, v, newR, true);
                MechJebLib.Core.Maths.ApoapsisFromStateVectors(mu, r, v + dv3).ShouldEqual(newR, 1e-5);

                // now test apoapsis changing to hyperbolic
                newR = -(random.NextDouble() * 1e9 + 1e3) * rscale;
                V3 dv4 = ChangeOrbitalElement.ChangeApoapsis(mu, r, v, newR, true);
                MechJebLib.Core.Maths.ApoapsisFromStateVectors(mu, r, v + dv4).ShouldEqual(newR, 1e-5);

                // now test changing the SMA.
                newR = random.NextDouble() * 1e3 * rscale;
                /*
                mu   = 8657343022269006;
                r    = new V3(-37559037.128101, -13154072.5832729, -86913972.3215555);
                v    = new V3(23370.6359939819, 12558.3695536628, 17864.1347044172);
                newR = 35354091.544774853;
                */
                V3 dv5 = ChangeOrbitalElement.ChangeSMA(mu, r, v, newR, true);
                MechJebLib.Core.Maths.SmaFromStateVectors(mu, r, v + dv5).ShouldEqual(newR, 1e-9);

                // now test changing the Ecc
                double newEcc = random.NextDouble() * 5 + 2.5;
                V3 dv6 = ChangeOrbitalElement.ChangeECC(mu, r, v, newEcc);
                (_, double ecc) = MechJebLib.Core.Maths.SmaEccFromStateVectors(mu, r, v + dv6);
                ecc.ShouldEqual(newEcc, 1e-9);
            }
        }

        [Fact]
        private void NextManeuverToReturnFromMoonTest()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            double centralMu = 398600435436096;
            double moonMu = 4902800066163.8;
            var moonR0 = new V3(325420116.073166, -166367503.579338, -138858150.96145);
            var moonV0 = new V3(577.012296778094, 761.848508254181, 297.464594270612);
            double moonSOI = 66167158.6569544;
            var r0 = new V3(4198676.73768844, 5187520.71497923, -3.29371833446352);
            var v0 = new V3(-666.230112925872, 539.234048888927, 0.000277598267012666);
            double peR = 6.3781e6 + 60000000; // 60Mm

            // this test periodically just fails, although its about 1-in-30 right now
            for (int i = 0; i < 1; i++)
            {
                var random = new Random();
                r0 = new V3(6616710 * random.NextDouble() - 3308350, 6616710 * random.NextDouble() - 3308350,
                    6616710 * random.NextDouble() - 3308350);
                v0 = new V3(2000 * random.NextDouble() - 1000, 2000 * random.NextDouble() - 1000, 2000 * random.NextDouble() - 1000);

                /*
                r0 = new V3(1448096.6091073, 2242802.60448088, 593799.176165359);
                v0 = new V3(316.818652356425, -684.168486708854, -453.106628476226);

                r0 = new V3(72567.8252483425, -1202508.27881747, 995820.818247795);
                v0 = new V3(733.61121478193, -501.358138165138, -26.4032981481417);

                r0 = new V3(1105140.06213014, -8431008.05414815, -4729658.84487529);
                v0 = new V3(-293.374690393787, -1173.3597686151, -411.755491683683);
                */

                (V3 dv, double dt, double newPeR) =
                    ReturnFromMoon.NextManeuver(398600435436096, 4902800066163.8, moonR0, moonV0, 66167158.6569544, r0, v0, peR, 0, 0);

                (V3 r1, V3 v1) = Shepperd.Solve(moonMu, dt, r0, v0);

                double tt1 = MechJebLib.Core.Maths.TimeToNextRadius(moonMu, r1, v1 + dv, moonSOI);

                (V3 r2, V3 v2) = Shepperd.Solve(moonMu, tt1, r1, v1 + dv);

                (V3 rtest, V3 vtest) = Shepperd.Solve(moonMu, tt1 * 0.1, r1, v1 + dv);

                _testOutputHelper.WriteLine($"rtest = {rtest}, vtest = {vtest}");
                _testOutputHelper.WriteLine($"dv = {dv}, dt = {dt}, peR = {newPeR}");

                (V3 moonR2, V3 moonV2) = Shepperd.Solve(centralMu, dt + tt1, moonR0, moonV0);

                V3 r3 = moonR2 + r2;
                V3 v3 = moonV2 + v2;

                MechJebLib.Core.Maths.PeriapsisFromStateVectors(centralMu, r3, v3).ShouldEqual(peR, 1e-4);
                newPeR.ShouldEqual(peR, 1e-4);
            }
        }
    }
}
