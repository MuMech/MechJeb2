/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using AssertExtensions;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Maths
{
    public class FunctionsTests
    {
        private const double PERIOD = 86164.0905;

        private const double ACC  = EPS * 8;
        private const double ACC2 = 1e-7; // due west launches have some mathematical irregularities

        [Fact]
        public void HeadingForInclinationTest1()
        {
            double heading = Functions.HeadingForInclination(Deg2Rad(0), 0);
            heading.ShouldEqual(Deg2Rad(90), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(45), 0);
            heading.ShouldEqual(Deg2Rad(45), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(90), 0);
            heading.ShouldEqual(Deg2Rad(0), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(135), 0);
            heading.ShouldEqual(Deg2Rad(315), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(180), 0);
            heading.ShouldEqual(Deg2Rad(270), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(-45), 0);
            heading.ShouldEqual(Deg2Rad(135), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(-90), 0);
            heading.ShouldEqual(Deg2Rad(180), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(-135), 0);
            heading.ShouldEqual(Deg2Rad(225), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(-180), 0);
            heading.ShouldEqual(Deg2Rad(270), 1e-15);

            heading = Functions.HeadingForInclination(Deg2Rad(0), Deg2Rad(45));
            heading.ShouldEqual(Deg2Rad(90), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(45), Deg2Rad(45));
            heading.ShouldEqual(Deg2Rad(90), 1e-15);
            heading = Functions.HeadingForInclination(Deg2Rad(90), Deg2Rad(45));
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

            (double sma, double ecc, double inc, double lan, double argp, double tanom) = Functions.KeplerianFromStateVectors(mu, r, v);
            sma.ShouldEqual(smaEx, 4e-15);
            ecc.ShouldEqual(eccEx, 4e-15);
            inc.ShouldEqual(incEx, EPS);
            lan.ShouldEqual(lanEx, 4e-15);
            argp.ShouldEqual(argpEx, 4e-15);
            tanom.ShouldEqual(tanomEx, 4e-15);
        }

        [Fact]
        public void KeplerianFromStateVectorsTest2()
        {
            double sma, ecc, inc, lan, argp, tanom;
            (sma, ecc, inc, lan, argp, tanom) = Functions.KeplerianFromStateVectors(1.0, new V3(1, 0, 0), new V3(0, 0, 1));
            sma.ShouldEqual(1.0, EPS);
            ecc.ShouldEqual(0.0, EPS);
            inc.ShouldEqual(PI / 2, EPS);
            lan.ShouldEqual(0, EPS);
            argp.ShouldEqual(0, EPS);
            tanom.ShouldEqual(0, EPS);
            (sma, ecc, inc, lan, argp, tanom) = Functions.KeplerianFromStateVectors(1.0, new V3(1, 0, 0), new V3(0, 1, 0));
            sma.ShouldEqual(1.0, EPS);
            ecc.ShouldEqual(0.0, EPS);
            inc.ShouldEqual(0.0, EPS);
            lan.ShouldEqual(0, EPS);
            argp.ShouldEqual(0, EPS);
            tanom.ShouldEqual(0, EPS);
            (sma, ecc, inc, lan, argp, tanom) = Functions.KeplerianFromStateVectors(1.0, new V3(1, 0, 0), new V3(0, 0, -1));
            sma.ShouldEqual(1.0, EPS);
            ecc.ShouldEqual(0.0, EPS);
            inc.ShouldEqual(PI/2, EPS);
            lan.ShouldEqual(PI, EPS);
            argp.ShouldEqual(0, EPS);
            tanom.ShouldEqual(PI, EPS);
            (sma, ecc, inc, lan, argp, tanom) = Functions.KeplerianFromStateVectors(1.0, new V3(0, 1, 0), new V3(-1, 0, 0));
            sma.ShouldEqual(1.0, EPS);
            ecc.ShouldEqual(0.0, EPS);
            inc.ShouldEqual(0.0, EPS);
            lan.ShouldEqual(0, EPS);
            argp.ShouldEqual(0, EPS);
            tanom.ShouldEqual(PI/2, EPS);
        }

        [Fact]
        public void DeltaVToChangeApoapsisProgradeTest1()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = 6.371e+6 + 185e+3;
            const double r1000 = 6.371e+6 + 1000e+3;
            double v185 = Functions.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = Functions.DeltaVToChangeApoapsisPrograde(mu, r0, v0, r185);
            Assert.Equal(V3.zero, dv);

            dv = Functions.DeltaVToChangeApoapsisPrograde(mu, r0, v0, r1000);

            (double smaf, double eccf, double incf, double argpf, double lanf, double tanof) = Functions.KeplerianFromStateVectors(mu, r0, v0 + dv);
            Assert.Equal(1.0, Functions.ApoapsisFromKeplerian(smaf, eccf) / r1000, 12);

            dv = Functions.DeltaVToChangeApoapsisPrograde(mu, r0, v0, rearth);
            Assert.Equal(V3.zero, dv);
        }

        [Fact]
        public void DeltaVToChangeInclinationTest1()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            double v185 = Functions.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = Functions.DeltaVToChangeInclination(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = Functions.DeltaVToChangeInclination(r0, v0, Deg2Rad(90));
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
            double v185 = Functions.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = Functions.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = Functions.DeltaVToChangeFPA(r0, v0, Deg2Rad(90));
            Assert.Equal(v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            dv = Functions.DeltaVToChangeFPA(r0, v0, Deg2Rad(-90));
            Assert.Equal(-v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Math.Sqrt(2), v185 / Math.Sqrt(2), 0);

            dv = Functions.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Math.Sqrt(2), dv.x, 9);
            Assert.Equal(v185 - v185 / Math.Sqrt(2), dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Math.Sqrt(2), 0, v185 / Math.Sqrt(2));

            dv = Functions.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Math.Sqrt(2), dv.x, 9);
            Assert.Equal(0, dv.y, 9);
            Assert.Equal(v185 - v185 / Math.Sqrt(2), dv.z, 9);
        }

        [Fact]
        public void ECIToPitchHeadingTest1()
        {
            (double pitch, double heading) = Functions.ECIToPitchHeading(new V3(10, 10, 0), new V3(0, 0, 10));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(0, heading, 9);
            (pitch, heading) = Functions.ECIToPitchHeading(new V3(10, 10, 0), new V3(0, 0, -10));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(Deg2Rad(180), heading, 9);
            (pitch, heading) = Functions.ECIToPitchHeading(new V3(10, 10, 0), new V3(-10, 10, 0));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(Deg2Rad(90), heading, 9);
            (pitch, heading) = Functions.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, -10, 0));
            Assert.Equal(0, pitch, 9);
            Assert.Equal(Deg2Rad(270), heading, 9);
            (pitch, heading) = Functions.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, 10, 0));
            Assert.Equal(Deg2Rad(90), pitch, 9);
            Assert.Equal(Deg2Rad(90), heading, 9);
            (pitch, heading) = Functions.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, 10, Math.Sqrt(200)));
            Assert.Equal(Deg2Rad(45), pitch, 9);
            Assert.Equal(0, heading, 9);
            (pitch, heading) = Functions.ECIToPitchHeading(new V3(10, 10, 0), new V3(10, 10, -Math.Sqrt(200)));
            Assert.Equal(Deg2Rad(45), pitch, 9);
            Assert.Equal(Deg2Rad(180), heading, 9);
        }

        [Fact]
        public void IncFromStateVectorsTest1()
        {
            double inc = Functions.IncFromStateVectors(new V3(10, 10, 0), new V3(0, 10, 0));
            Assert.Equal(0, inc);
            inc = Functions.IncFromStateVectors(new V3(10, 10, 0), new V3(0, 0, 10));
            Assert.Equal(Math.PI / 2, inc);
            inc = Functions.IncFromStateVectors(new V3(10, 10, 0), new V3(0, -10, 0));
            Assert.Equal(Math.PI, inc);
            inc = Functions.IncFromStateVectors(new V3(10, 10, 0), new V3(0, 0, -10));
            Assert.Equal(Math.PI / 2, inc);
        }

        [Fact]
        public void VelocityForHeadingTest1()
        {
            V3 vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), 0);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], 10, 9);
            vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), Math.PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), Math.PI);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], -10, 9);
            vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(0, 10, 0), 3 * Math.PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], -10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), 0);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], 10, 9);
            vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), Math.PI / 2);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], 10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), Math.PI);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], -10, 9);
            vf = Functions.VelocityForHeading(new V3(10, 0, 0), new V3(10, 10, 0), 3 * Math.PI / 2);
            Assert.Equal(vf[0], 10, 9);
            Assert.Equal(vf[1], -10, 9);
            Assert.Equal(vf[2], 0, 9);
        }

        [Fact]
        public void VelocityForInclinationTest1()
        {
            V3 vf = Functions.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), 0);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = Functions.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), Math.PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], 10, 9);
            vf = Functions.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), Math.PI);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], -10, 9);
            Assert.Equal(vf[2], 0, 9);
            vf = Functions.VelocityForInclination(new V3(10, 0, 0), new V3(0, 10, 0), -Math.PI / 2);
            Assert.Equal(vf[0], 0, 9);
            Assert.Equal(vf[1], 0, 9);
            Assert.Equal(vf[2], -10, 9);
            vf = Functions.VelocityForInclination(new V3(10, 0, 0), new V3(10, 10, 0), 0);
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

            (double sma, double ecc) = Functions.SmaEccFromApsides(r185, r1000);
            double v185 = Functions.VmagFromVisViva(mu, sma, r185);
            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            double period = Functions.PeriodFromStateVectors(mu, r0, v0);

            Shepperd.Solve(mu, period / 3.0, r0, v0, out V3 r1, out V3 v1);
            Shepperd.Solve(mu, period * 0.75, r0, v0, out V3 r2, out V3 v2);

            Assert.Equal(period / 3.0, Functions.TimeToNextRadius(mu, r0, v0, r1.magnitude), 9);
            Assert.Equal(period * 0.25, Functions.TimeToNextRadius(mu, r0, v0, r2.magnitude), 9);
            Assert.Equal(period / 2.0, Functions.TimeToNextRadius(mu, r0, v0, r1000), 9);
        }

        [Fact]
        public void ApoapsisFromStateVectorsTest()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            const double r1000 = rearth + 1000e+3;

            (double sma, double ecc) = Functions.SmaEccFromApsides(r185, r1000);
            double v185 = Functions.VmagFromVisViva(mu, sma, r185);
            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            Assert.Equal(1.0, Functions.ApoapsisFromStateVectors(mu, r0, v0) / r1000, 14);
            Assert.Equal(1.0, Functions.PeriapsisFromStateVectors(mu, r0, v0) / r185, 14);
        }

        [Fact]
        public void ApsidesFromKeplerianTest()
        {
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            const double r1000 = rearth + 1000e+3;

            (double sma, double ecc) = Functions.SmaEccFromApsides(r185, r1000);
            Assert.Equal(r185, Functions.PeriapsisFromKeplerian(sma, ecc));
            Assert.Equal(r1000, Functions.ApoapsisFromKeplerian(sma, ecc));
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
                        Functions.TimeToPlane(PERIOD, 28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(PERIOD, -28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                    }

                    // reverse
                    delay = PERIOD / 8 * ((i - j + 8) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(-PERIOD, -28.608, lng, lan, 90).ShouldEqual(delay, ACC);
                    }

                    // advance by 180 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 4) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, 28.608, lng, lan, -90).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(PERIOD, -28.608, lng, lan, -90).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 180 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 4) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 28.608, lng, lan, -90).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(-PERIOD, -28.608, lng, lan, -90).ShouldEqual(delay, ACC);
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
                        Functions.TimeToPlane(PERIOD, 45, lng, lan, 45).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(PERIOD, 45, lng, lan, -45).ShouldEqual(delay, ACC);
                    }

                    // advance by 270 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -45, lng, lan, 45).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(PERIOD, -45, lng, lan, -45).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 45, lng, lan, 45).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(-PERIOD, 45, lng, lan, -45).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -45, lng, lan, 45).ShouldEqual(delay, ACC);
                        Functions.TimeToPlane(-PERIOD, -45, lng, lan, -45).ShouldEqual(delay, ACC);
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
                        Functions.TimeToPlane(PERIOD, 45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(PERIOD, 45, lng, lan, -135).ShouldEqual(delay, ACC2);
                    }

                    // advance by 90 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(PERIOD, -45, lng, lan, -135).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(-PERIOD, 45, lng, lan, -135).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -45, lng, lan, 135).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(-PERIOD, -45, lng, lan, -135).ShouldEqual(delay, ACC2);
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
                        Functions.TimeToPlane(PERIOD, 45, lng, lan, 0).ShouldEqual(delay, ACC);
                    }

                    // advance by 270 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -45, lng, lan, 0).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 45, lng, lan, 0).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -45, lng, lan, 0).ShouldEqual(delay, ACC);
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
                        Functions.TimeToPlane(PERIOD, 45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(PERIOD, 45, lng, lan, -180).ShouldEqual(delay, ACC2);
                    }

                    // advance by 90 degrees
                    delay = PERIOD / 8 * ((j - i + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(PERIOD, -45, lng, lan, -180).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 90 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 2) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(-PERIOD, 45, lng, lan, -180).ShouldEqual(delay, ACC2);
                    }

                    // reverse and advance by 270 degrees
                    delay = PERIOD / 8 * ((i - j + 8 + 6) % 8);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -45, lng, lan, 180).ShouldEqual(delay, ACC2);
                        Functions.TimeToPlane(-PERIOD, -45, lng, lan, -180).ShouldEqual(delay, ACC2);
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
                        Functions.TimeToPlane(PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 150 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 330 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 11) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 210 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 330 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 11) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 210 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 30 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 1) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 150 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
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
                        Functions.TimeToPlane(PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 210 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 30 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 1) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // advance by 150 degrees
                    delay = PERIOD / 12 * ((j - i + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 30 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 1) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 150 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 5) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, 28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 330 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 11) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -28.608, lng, lan, inc).ShouldEqual(delay, ACC);
                    }

                    // reverse and advance by 210 degrees
                    delay = PERIOD / 12 * ((i - j + 12 + 7) % 12);

                    if (delay != 0)
                    {
                        Functions.TimeToPlane(-PERIOD, -28.608, lng, lan, -inc).ShouldEqual(delay, ACC);
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
                        Functions.TimeToPlane(PERIOD, 90, lng, lan, inc).ShouldBeZero(ACC);
                        Functions.TimeToPlane(PERIOD, -90, lng, lan, inc).ShouldBeZero(ACC);
                    }
                }
            }
        }


    }
}
