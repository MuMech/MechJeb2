using System;
using MechJebLib.Primitives;
using MechJebLib.PVG;
using MechJebLib.PVG.Integrators;
using static MechJebLib.Utils.Statics;
using Xunit;
using Xunit.Abstractions;

namespace AssertExtensions.PVG.Integrators
{
    public class VacuumThrustIntegratorTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public VacuumThrustIntegratorTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        void TestOne()
        {
            double[] initial = { -0.081895706968361404, -0.87408474062734443, 0.47882038321543935, 0.051340280325144226, -0.0048104747381358367, 
                8.8629882392912712E-08, 0.48974601817567098, -0.36890385205553877, 0.17538607033093154, -0.080013188175758393, -0.91271362697981473,
                0.49963482634661827, 1, 0, 0 };

            double[] terminal =
            {
                -0.026759795570419059, -0.88035281253801079, 0.47941753836114931, 0.66850742989658496, -0.12037659423286777, 0.031340232987417331,
                0.4941443242347453, -0.1866747291302937, 0.076220953135557615, 0.035848253503130999, -0.82124044301911314, 0.44406426981014113,
                0.14483474439315011, 0, 0.68991373964877345
            };
            
            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            double mu = 3.986004418e+14;
            double m0 = 49119.7842689869;
            
            using var y0 = DD.Rent(initial);
            using var yf = DD.Rent(initial.Length);
            using var wrapper = ArrayWrapper.Rent(y0);

            var scale = new Scale(r0, m0, mu);

            var phase = Phase.NewStageUsingFinalMass(m0, 7114.2513992454, 288.000034332275, 170.308460385726, 3);
            Phase normalizedPhase = phase.Rescale(scale);

            var integrator = new VacuumThrustAnalytic();

            integrator.Integrate(y0, yf, normalizedPhase, 0, 0.21143859334689075);

            using var expected = ArrayWrapper.Rent(terminal);
            using var actual = ArrayWrapper.Rent(yf);
            
            _testOutputHelper.WriteLine($"{(actual.R - expected.R).magnitude/expected.R.magnitude}");
            _testOutputHelper.WriteLine($"{(actual.V - expected.V).magnitude/expected.V.magnitude}");
            _testOutputHelper.WriteLine($"{(actual.PV - expected.PV).magnitude/expected.PV.magnitude}");
            _testOutputHelper.WriteLine($"{(actual.PR - expected.PR).magnitude/expected.PR.magnitude}\n");
            for (int i = 0; i < yf.Count; i++)
                _testOutputHelper.WriteLine($"{yf[i] / terminal[i]}");

            /*
            for (int i = 0; i < yf.Count; i++)
                yf[i].ShouldEqual(terminal[i], EPS);
                */
        }
        
        [Fact]
        void TestTwo()
        {
            double[] initial =
            {
                -0.026759795570419059, -0.88035281253801079, 0.47941753836114931, 0.66850742989658496, -0.12037659423286777, 0.031340232987417331,
                0.4941443242347453, -0.1866747291302937, 0.076220953135557615, 0.035848253503130999, -0.82124044301911314, 0.44406426981014113,
                0.057993452332683458, 0, 0.68991373964877345
            };

            double[] terminal =
            {
                0.047109712867170944,-0.89018657363287546,0.48099760897140348,0.81166191261810106,-0.075137083529584037,-0.00051053125862332409,
                0.48808366292612443,-0.10550522395750975,0.03245889518210119,0.083822746636572881,-0.79455356115633813,0.42713133458266622,
                0.036958492716224652,0,0.84081895291759234
            };
            
            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            double mu = 3.986004418e+14;
            double m0 = 49119.7842689869;
            
            using var y0 = DD.Rent(initial);
            using var yf = DD.Rent(initial.Length);
            using var wrapper = ArrayWrapper.Rent(y0);

            var scale = new Scale(r0, m0, mu);

            // stage: 1 m0: 0.0579934523328269 mf: 0.0277629729088327 thrust: 0.0700729705895292 bt: 0.0507442481894487 isp: 270.15767003304 mdot: 0.209206166695606 (optimized)
            var phase = Phase.NewStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, true);
            Phase normalizedPhase = phase.Rescale(scale);

            var integrator = new VacuumThrustAnalytic();

            integrator.Integrate(y0, yf, normalizedPhase, 0, 0.10054655629279166);

            for (int i = 0; i < yf.Count; i++)
                _testOutputHelper.WriteLine($"{yf[i] / terminal[i]}");
            
            /*
            for (int i = 0; i < yf.Count; i++)
                yf[i].ShouldEqual(terminal[i], EPS);
                */
        }
        
        [Fact]
        void TestThree()
        {
            double[] initial =
            {
                0.27948091968562255,-0.87629682147680643,0.46162785861333505,0.76704862327676382,0.1675555528292863,-0.13002847184975891,
                0.44534202722652572,0.11955432088272852,-0.087560284939524932,0.20710379113249269,-0.75262217242406393,0.39809178059136263,
                0.013808898897445529,0,0.84081895291759234
            };

            double[] terminal =
            {
                0.33879378309256369,-0.86100596113880579,0.45030618690420138,1.1111316964849096,0.31748261957072527,-0.22893330305695009,
                0.43076249468051553,0.16903642775109523,-0.1136843605281575,0.23555616906817203,-0.74935670563088341,0.39487218757780468,
                0.0036152964422362975,0,1.2230365666723386
            };
            
            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            double mu = 3.986004418e+14;
            double m0 = 49119.7842689869;
            
            using var y0 = DD.Rent(initial);
            using var yf = DD.Rent(initial.Length);
            using var wrapper = ArrayWrapper.Rent(y0);

            var scale = new Scale(r0, m0, mu);

            // stage: 0 m0: 0.01380889936729 mf: 0.00361529691208077 thrust: 0.0441169291167964 bt: 0.0658996558651869 isp: 230.039271734103 mdot: 0.15468369783391 (unguided)
            var phase = Phase.NewStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, false, true);
            Phase normalizedPhase = phase.Rescale(scale);
            normalizedPhase.u0 = new V3(0.94884816849167, 0.254723092521293, -0.186556423867935);

            var integrator = new VacuumThrustAnalytic();

            integrator.Integrate(y0, yf, normalizedPhase, 0, 0.065899655865186896);

            for (int i = 0; i < yf.Count; i++)
                _testOutputHelper.WriteLine($"{yf[i] / terminal[i]}");
            
            /*
            for (int i = 0; i < yf.Count; i++)
                yf[i].ShouldEqual(terminal[i], EPS);
                */
        }
    }
}
