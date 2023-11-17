using System;
using MechJebLib.Functions;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using Xunit.Abstractions;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.ManeuversTests
{
    public class TwoImpulseTransferTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TwoImpulseTransferTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        private void HohmannTest()
        {
            const int NTRIALS = 50;

            var random = new Random();
            //Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            for (int i = 0; i < NTRIALS; i++)
            {
                // construct some random circular coplanar hohmann transfers
                double mu = random.NextDouble() * 10 + 1;

                // construct a random circular orbit
                var r1 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v1 = new V3(random.NextDouble() - 0.5, random.NextDouble() - 0.5, random.NextDouble() - 0.5);
                v1 = (v1 - V3.Dot(r1.normalized, v1) * r1.normalized).normalized * Astro.CircularVelocity(mu, r1.magnitude);

                // construct another random circular coplanar orbit
                var h1 = V3.Cross(r1, v1);
                var r2 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                r2 -= V3.Dot(h1.normalized, r2) * h1.normalized;
                V3 v2 = V3.Cross(h1, r2).normalized * Astro.CircularVelocity(mu, r2.magnitude);

                // this algorithm has issues with very large (normalized) synodic periods
                var scale = Scale.Create(mu, Sqrt(r1.magnitude * r2.magnitude));
                double synodicPeriod = Astro.SynodicPeriod(mu, r1, v1, r2, v2) / scale.TimeScale;
                if (synodicPeriod > 1000)
                    continue;

                (V3 dv1, double dt1, V3 dv2, double dt2) = TwoImpulseTransfer.NextManeuver(mu, r1, v1, r2, v2);

                (double dv1Hoh, double dv2Hoh, double ttHoh, double _) = Astro.HohmannTransferParameters(mu, r1, r2);

                dv1.magnitude.ShouldEqual(Abs(dv1Hoh), 1e-6);
                dv2.magnitude.ShouldEqual(Abs(dv2Hoh), 1e-6);
                (dt2 - dt1).ShouldEqual(ttHoh, 1e-3);

                (V3 rburn1, V3 vburn1) = Shepperd.Solve(mu, dt1, r1, v1);
                (V3 rburn2, V3 vburn2) = Shepperd.Solve(mu, dt2 - dt1, rburn1, vburn1 + dv1);
                (V3 rf, V3 vf)         = Shepperd.Solve(mu, dt2, r2, v2);

                rf.ShouldEqual(rburn2, 1e-6);
                vf.ShouldEqual(vburn2 + dv2, 1e-6);
            }
        }

        [Fact]
        private void GeoTestFixed()
        {
            double mu = 3.986004418e+14;
            var r1 = new V3(5673188.62234991, 1106269.57811856, 3093900.30098098);
            var v1 = new V3(-1154.38931594925, 7685.58250511721, -631.330049272638);

            double nu = 3.24639265358979;
            (V3 r2, V3 v2) = Astro.StateVectorsFromKeplerian(mu, 42164000, 0, 0, 0, 0, nu);

            (V3 dv1, double dt1, V3 dv2, double dt2) = TwoImpulseTransfer.NextManeuver(mu, r1, v1, r2, v2, coplanar: false);
            (V3 rburn1, V3 vburn1)                   = Shepperd.Solve(mu, dt1, r1, v1);
            (V3 _, V3 _)                             = Shepperd.Solve(mu, dt2, rburn1, vburn1 + dv1);
            (V3 _, V3 _)                             = Shepperd.Solve(mu, dt1 + dt2, r2, v2);
            double inc = Astro.IncFromStateVectors(rburn1, vburn1 + dv1);

            dv1.magnitude.ShouldEqual(2484.20137552452, 1e-4);
            dv2.magnitude.ShouldEqual(1793.10206031673, 1e-4);
            dt1.ShouldEqual(1177.74844650851, 1e-4);
            inc.ShouldEqual(Deg2Rad(26.440413305834294), 1e-4);
            dt2.ShouldEqual(20098.223757535023, 1e-4);
        }

        [Fact]
        private void GeoTestFree()
        {
            double mu = 3.986004418e+14;
            var r1 = new V3(5673188.62234991, 1106269.57811856, 3093900.30098098);
            var v1 = new V3(-1154.38931594925, 7685.58250511721, -631.330049272638);

            (V3 r2, V3 v2) = Astro.StateVectorsFromKeplerian(mu, 42164000, 0, 0, 0, 0, 0);

            (V3 dv1, double dt1, V3 dv2, double dt2) = TwoImpulseTransfer.NextManeuver(mu, r1, v1, r2, v2, coplanar: false, rendezvous: false);
            (V3 rburn1, V3 vburn1)                   = Shepperd.Solve(mu, dt1, r1, v1);
            (V3 _, V3 _)                             = Shepperd.Solve(mu, dt2, rburn1, vburn1 + dv1);
            (V3 _, V3 _)                             = Shepperd.Solve(mu, dt1 + dt2, r2, v2);
            double inc = Astro.IncFromStateVectors(rburn1, vburn1 + dv1);

            dv1.magnitude.ShouldEqual(2484.20137552452, 1e-4);
            dv2.magnitude.ShouldEqual(1793.10206031673, 1e-4);
            dt1.ShouldEqual(1177.74844650851, 1e-4);
            inc.ShouldEqual(Deg2Rad(26.440413305834294), 1e-4);
            dt2.ShouldEqual(20097.46881536536, 1e-4);
        }
    }
}
