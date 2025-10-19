/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PSGTests.AscentTests
{
    public class Titan2Tests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public Titan2Tests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        // this is a forced periapsis attachment problem, which chooses an ApR such that it winds up burning the whole rocket.
        [Fact]
        public void FlightPathAngle4Elliptical()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(153180, 2194400, 296, 156, 4, 4)
                .AddStageUsingThrust(31980, 443700, 315, 180, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, true, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(153180, 1e-9);

            solution.U(0).normalized.ShouldEqual(new V3(0.68052484128120905, 0.6317658413531414, 0.37115746267392064), 1e-2);
            solution.Vgo(0).ShouldEqual(9369.7098971395462, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        // this is FlightPathAngle4Elliptical but burning the whole rocket.
        [Fact]
        public void FlightPathAngle3EnergyElliptical()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(153180, 2194400, 296, 156, 4, 4)
                .AddStageUsingThrust(31980, 443700, 315, 180, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, true, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(153180, 1e-9);

            solution.U(0).normalized.ShouldEqual(new V3(0.68052484128120905, 0.6317658413531414, 0.37115746267392064), 1e-2);
            solution.Vgo(0).ShouldEqual(9369.7098971395462, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        [Fact]
        public void FlightPathAngle5Elliptical()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(153180, 2194400, 296, 156, 4, 4)
                .AddStageUsingThrust(31980, 443700, 315, 180, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, true, true)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(153180, 1e-9);

            solution.U(0).normalized.ShouldEqual(new V3(0.68052484128120905, 0.6317658413531414, 0.37115746267392064), 1e-2);
            solution.Vgo(0).ShouldEqual(9369.7098971395462, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        [Fact]
        public void FlightPathAngle4EnergyElliptical()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(153180, 2194400, 296, 156, 4, 4)
                .AddStageUsingThrust(31980, 443700, 315, 180, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, true, true)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(153180, 1e-9);

            solution.U(0).normalized.ShouldEqual(new V3(0.68052484128120905, 0.6317658413531414, 0.37115746267392064), 1e-2);
            solution.Vgo(0).ShouldEqual(9369.7098971395462, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        [Fact]
        public void Kepler3Elliptical()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(157355.487476332, 1e-9);

            solution.Vgo(0).ShouldEqual(9334.0379052112639, 1e-3);
            solution.U(0).normalized.ShouldEqual(new V3(0.67311315112262493, 0.64198533000545521, 0.36711513431559445), 1e-2);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.6625138637074874, 1e-2);
            ClampPi(tanof).ShouldEqual(0.095809701921327317, 1e-2);
        }

        [Fact]
        public void Kepler4Elliptical()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, true)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(157355.487476332, 1e-9);

            solution.Vgo(0).ShouldEqual(9334.0379052112639, 1e-3);
            solution.U(0).normalized.ShouldEqual(new V3(0.67311315112262493, 0.64198533000545521, 0.36711513431559445), 1e-2);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.6625138637074874, 1e-2);
            ClampPi(tanof).ShouldEqual(0.095809701921327317, 1e-2);
        }

        /*
                // extreme 1130 x 1132 with free attachment
                [Fact]
                public void Kepler3ExtremeElliptical()
                {
                    Logger.Register(o => _testOutputHelper.WriteLine((string)o));
                    var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
                    var    v0    = new V3(0, 407.862893197274, 0);
                    double t0    = 0;
                    double PeR   = 6.371e+6 + 1.130e+6;
                    double ApR   = 6.371e+6 + 1.132e+6;
                    double rbody = 6.371e+6;
                    double incT  = Deg2Rad(28.608);
                    double mu    = 3.986004418e+14;

                    Ascent ascent = Ascent.Builder()
                        .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                        .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3)
                        .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                        .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                        .Build();

                    ascent.Run();

                    Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
                    using Solution solution = psg.Solution ?? throw new Exception("null solution");

                    (V3 rf, V3 vf) = solution.TerminalStateVectors();

                    (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                        Astro.KeplerianFromStateVectors(mu, rf, vf);

                    solution.R(0).ShouldEqual(r0, 1e-9);
                    solution.V(0).ShouldEqual(v0, 1e-9);
                    solution.M(0).ShouldEqual(157355.487476332, 1e-9);

                    // this is 0.12 seconds before tau with an 18 kg final mass
                    solution.Vgo(0).ShouldEqual(27198.789343451925, 1e-3);

                    tanof.ShouldEqual(0.0063452857807080321, 0.1);

                    solution.U(0).normalized.ShouldEqual(new V3(0.87754205429590737, 0.029189246151212624, 0.47861041657202014), 1e-2);

                    double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
                    double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

                    perf.ShouldEqual(PeR, 1e-6);
                    aprf.ShouldEqual(ApR, 1e-6);

                    psg.PrimalFeasibility.ShouldBeZero(1e-5);
                    smaf.ShouldEqual(7502000.0002534958, 1e-6);
                    eccf.ShouldEqual(0.00013329824589634466, 1e-6);
                    incf.ShouldEqual(incT, 1e-6);
                    lanf.ShouldEqual(Deg2Rad(270), 1e-2);
                    argpf.ShouldEqual(1.5965429879704516, 1e-2);
                }
                */

        [Fact]
        public void Circular()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(157355.487476332, 1e-9);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Vgo(0).ShouldEqual(8518.1366714811684, 1e-3);

            solution.U(0).normalized.ShouldEqual(new V3(0.69734975209707417, 0.6074944955387559, 0.38033374967291772), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);

            Ascent ascent2 = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .OldSolution(solution)
                .Build();

            ascent2.Run();

            Optimizer      psg2      = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = psg2.Solution ?? throw new Exception("null solution");
        }

        [Fact]
        public void CircularOptimizedBooster()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3, allowShutdown: false)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(157355.487476332, 1e-9);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Vgo(0).ShouldEqual(8562.0125605993326, 1e-3);

            solution.U(0).normalized.ShouldEqual(new V3(0.71091349069509358, 0.58674209774568975, 0.38773150436958898), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
        }

        [Fact]
        public void CircularCoast()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddCoast(32758.6353093992, 0, 450, 3, 3)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(157355.487476332, 1e-9);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Vgo(0).ShouldEqual(8474.9669267192712, 1e-3);

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldEqual(46.192475615255354, 1e-2);
            solution.Tgo(solution.T0, 2).ShouldBePositive();

            solution.U(0).normalized.ShouldEqual(new V3(0.67428533585626949, 0.64038742246275127, 0.36775431336792186), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);

            Ascent ascent2 = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddCoast(32758.6353093992, 0, 450, 3, 3)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .OldSolution(solution)
                .Build();

            ascent2.Run();

            Optimizer      psg2      = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = psg2.Solution ?? throw new Exception("null solution");
        }

        [Fact]
        public void CircularCoastOptimizedBooster()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
            var    v0    = new V3(0, 407.862893197274, 0);
            double t0    = 0;
            double PeR   = 6.371e+6 + 185e+3;
            double ApR   = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT  = Deg2Rad(28.608);
            double mu    = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddCoast(32758.6353093992, 0, 450, 3, 3)
                //.AddFixedCoast(32758.6353093992, -9.5, 3, 3)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3, allowShutdown: false)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldEqual(14.925595039046339, 1e-3);
            solution.Tgo(solution.T0, 2).ShouldBePositive();
            solution.Vgo(0).ShouldEqual(8557.8736963926785, 1e-3);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(157355.487476332, 1e-9);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.U(0).normalized.ShouldEqual(new V3(0.70743037652982543, 0.59217914188368692, 0.38583173311790908), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
        }
    }
}
