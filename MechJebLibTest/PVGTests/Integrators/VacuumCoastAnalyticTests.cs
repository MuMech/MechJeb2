using System;
using MechJebLib.Primitives;
using MechJebLib.PVG;
using MechJebLib.PVG.Integrators;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PVGTests.Integrators
{
    public class VacuumCoastAnalyticTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public VacuumCoastAnalyticTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        private void RandomComparedToIntegrated()
        {
            const int NTRIALS = 50;

            var random = new Random();

            var integrated = new VacuumThrustIntegrator();
            var analytic = new VacuumCoastAnalytic();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var pr0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var pv0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 40 * random.NextDouble() - 20;

                using var yin = Vn.Rent(IntegratorRecord.INTEGRATOR_REC_LEN);
                using var yout = Vn.Rent(IntegratorRecord.INTEGRATOR_REC_LEN);
                using var yout2 = Vn.Rent(IntegratorRecord.INTEGRATOR_REC_LEN);

                var y0 = new IntegratorRecord();
                var yf = IntegratorRecord.CreateFrom(yout);
                var yf2 = IntegratorRecord.CreateFrom(yout2);

                y0.R  = r0;
                y0.V  = v0;
                y0.Pv = pv0;
                y0.Pr = pr0;
                y0.M  = 1;

                y0.CopyTo(yin);

                var phase = Phase.NewFixedCoast(1.0, dt, 1, 1);
                phase.Normalized = true;

                try
                {
                    integrated.Integrate(yin, yout, phase, 0, dt);
                }
                catch (ArgumentException)
                {
                    // the ODE integrator can throw iterations exceeded for certain initial
                    // conditions it inherently has issues with.
                    continue;
                }

                analytic.Integrate(yin, yout2, phase, 0, dt);

                if (!NearlyEqual(yf.R, yf2.R, 1e-8) || !NearlyEqual(yf.V, yf2.V, 1e-8) || !NearlyEqual(yf.Pv, yf2.Pv, 1e-8) ||
                    !NearlyEqual(yf.Pr, yf2.Pr, 1e-8))
                {
                    _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + "\nrf:" + yf.R + " vf:" + yf.V + "\nrf2:" + yf2.R +
                                                " vf2:" +
                                                yf2.V + "\n");
                    _testOutputHelper.WriteLine("pr0 :" + pr0 + " pv0:" + pv0 + " dt:" + dt + "\npv:" + yf.Pv + " pr:" + yf.Pr + "\npv2:" + yf2.Pv +
                                                " pr2:" +
                                                yf2.Pr + "\n");
                }
            }
        }
    }
}
