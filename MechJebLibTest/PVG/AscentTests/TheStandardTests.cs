using System;
using AssertExtensions;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.PVG;
using static MechJebLib.Utils.Statics;
using Xunit;

namespace PVG
{
    public class TheStandardTests
    {
        // This is fairly sensitive to initial bootstrapping and will often compute a negative coast instead of the optimal positive coast
        [Fact]
        public void OptmizedBoosterWithCoastElliptical()
        {
            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var v0 = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var u0 = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double t0 = 661803.431918959;
            double mu = 3.986004418e+14;
            double rbody = 6.371e+6;
            
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0,false, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, true)
                .AddOptimizedCoast(678.290157913434, 0, 450, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, false, true)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();
            
            pvg.Znorm.ShouldBeZero(1e-9);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof) =
                Functions.KeplerianFromStateVectors(mu, rf, vf);
            
            solution.R(t0).ShouldEqual(r0, EPS);
            solution.V(t0).ShouldEqual(v0, EPS);
            solution.M(t0).ShouldEqual(49119.7842689869, EPS);
            solution.Vgo(t0).ShouldEqual(9369.7098973030625, 1e-7);
            solution.Pv(t0).ShouldEqual(new V3(0.25565104209238126, 0.23733387212263146, 0.13943178322206456), 1e-7);

            smaf.ShouldEqual(8616511.1913318466, 1e-7);
            eccf.ShouldEqual(0.23913520746130185, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(Deg2Rad(270), 1e-7);
            argpf.ShouldEqual(1.7643342516077158, 1e-7);
            ClampPi(tanof).ShouldBeZero(1e-7);
        }
    }
}
