using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.ManeuversTests
{
    public class InterplanetaryTransferTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public InterplanetaryTransferTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i <= 25; i++)
                yield return new object[] { i };
        }

        [Fact]
        private void EarthToMercury()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-5393756.1398685, 2579906.9721393, -2964978.45117617);
            var v0 = new V3(-3856.63799573101, -6569.97859538819, 1299.1134217111);
            double mu1 = 398600435436096;
            var r1 = new V3(142463209097.952, 147485866.387222, 38721854973.0728);
            var v1 = new V3(2694.98267839614, 28693.1174312002, -8955.43648332019);
            double soi1 = 924649202.461023;
            double mu2 = 22031780000000;
            var r2 = new V3(-17610435060.8862, 60947867948.4771, -28806334951.8025);
            var v2 = new V3(-35202.7776429461, -13422.0371670933, -9917.35856458486);
            double soi2 = 112408990.754424;
            double mu3 = 1.32712440041939E+20;
            double arrivalDT = 7646859.02846741;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: 0, optguard: true);
            _testOutputHelper.WriteLine($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            dv.magnitude.ShouldEqual(6255.6098503827661, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void EarthToMercuryRandom(int seed)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var random = new Random(seed);

            double mu1 = 398600435436096;
            var r1 = new V3(142463209097.952, 147485866.387222, 38721854973.0728);
            var v1 = new V3(2694.98267839614, 28693.1174312002, -8955.43648332019);
            double soi1 = 924649202.461023;
            double mu2 = 22031780000000;
            var r2 = new V3(-17610435060.8862, 60947867948.4771, -28806334951.8025);
            var v2 = new V3(-35202.7776429461, -13422.0371670933, -9917.35856458486);
            double soi2 = 112408990.754424;
            double mu3 = 1.32712440041939E+20;
            double arrivalDT = 323443.026350487 + 8066251.26376697;

            const double EARTH_SURFACE = 6378145;

            double per = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            double apr = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            if (per > apr)
                (per, apr) = (apr, per);
            double l = 1 / (0.5 * (1 / apr + 1 / per));
            double ecc = (apr - per) / (apr + per);
            double inc = PI * random.NextDouble();
            double lan = TAU * random.NextDouble();
            double argp = TAU * random.NextDouble();
            double nu = TAU * random.NextDouble();

            Print($"per: {per} apr: {apr} l: {l} ecc: {ecc} inc: {Rad2Deg(inc)} lan: {Rad2Deg(lan)} argp: {Rad2Deg(argp)} nu: {Rad2Deg(nu)}");

            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(mu1, l, ecc, inc, lan, argp, nu);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: 0, optguard: false);
            _testOutputHelper.WriteLine($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void EarthToCeres()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(6521378.923092721, -144016.87747755065, 1410886.635286456);
            var v0 = new V3(864.52724109710925, 6940.5943694265807, -3287.3841869385583);
            var r1 = new V3(22199151070.873016, 139845008280.15186, -53671512076.940094);
            var v1 = new V3(-28779.993950097334, 2818.9196949053821, -5428.3526123540723);
            var r2 = new V3(-398438838799.81482, 95904198928.415253, -176169845629.96323);
            var v2 = new V3(-5534.8713945405507, -15167.338982527044, 3683.7661474865149);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 62632500000.000008;
            double soi2 = 76962905.730546668;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 52458966.698702089;
            double arrivalBounds = 210031.37569600236;
            double arrivalDTlower = arrivalDT - arrivalBounds;
            double arrivalDTupper = arrivalDT + arrivalBounds;
            double peR = 573000;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            _testOutputHelper.WriteLine($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            dv.magnitude.ShouldEqual(4944.8587050280121, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void EarthToMars()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-4813533.7100184178, 4067332.94001712, 2197061.3473151801);
            var v0 = new V3(-3943.6447628964834, -6096.7177540935272, 2646.3673516840317);
            var r1 = new V3(72259723607.744156, -129249886043.4872, -23994554196.157143);
            var v1 = new V3(23464.265746216355, 14594.055858142867, -10895.306707940774);
            var r2 = new V3(141814014170.92267, -184968009549.99579, -48407945794.035919);
            var v2 = new V3(14750.434672844203, 15845.36047434548, -8009.1168536898813);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 42828373620699.094;
            double soi2 = 577254070.87249529;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 30453859.533329464;
            double arrivalBounds = 350952.48146891204;
            double arrivalDTlower = arrivalDT - arrivalBounds;
            double arrivalDTupper = arrivalDT + arrivalBounds;
            double peR = 3510800;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            _testOutputHelper.WriteLine($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            dv.magnitude.ShouldEqual(3640.1292730002069, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void EarthToAsteroid()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-3613600.2089605439, -5103455.2935977345, 2331613.3986294419);
            var v0 = new V3(5387.6194247482008, -4951.1281054341907, -2487.2177707052929);
            var r1 = new V3(-102996353813.89156, -93428051172.649872, 48712283511.210747);
            var v1 = new V3(17892.040644066445, -23454.205300374037, -6637.6727074400696);
            var r2 = new V3(216715673370.22906, -318827504282.35291, 128837531026.59201);
            var v2 = new V3(12822.776831847617, 9031.0223200621294, 5997.2223306495371);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 0;
            double soi2 = 0;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 54876414.676081017;
            double peR = 0;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: peR, optguard: true);
            _testOutputHelper.WriteLine($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            dv.magnitude.ShouldEqual(4789.592962160028, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            r2soi2.ShouldEqual(rsoi2helio, 1e-6);
        }

        [Fact]
        private void EarthToJupiter()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-3912331.3024163404, 4747993.7355074612, 2586640.1496319249);
            var v0 = new V3(-5125.9892585184534, -5380.116158810044, 2122.3228889072616);
            var r1 = new V3(137412324343.95822, -23942591869.675461, -60338571254.994019);
            var v1 = new V3(5424.8571535079955, 28799.680047142847, 733.01461940330978);
            var r2 = new V3(172357530979.00909, 720778222761.22327, -15224336000.222565);
            var v2 = new V3(-12248.149100605306, 2887.3625226233494, 5401.8948520706454);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 1.2668653492180082E+17;
            double soi2 = 48196176124.28714;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 96640527.803545117;
            double arrivalDTlower = 0;
            double arrivalDTupper = double.PositiveInfinity;
            double peR = 70941833.416772693;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            _testOutputHelper.WriteLine($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            // deep-well case (Jupiter, focusing factor ~10): exercises the analytic-b warm start + Jacobian
            // preconditioner landing the cheap (~6340 m/s, textbook Earth->Jupiter) basin.
            dv.magnitude.ShouldEqual(6339.7019346132129, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void EarthToVenus()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-2524692.3521177084, -6010111.3403185587, -1430397.88334662);
            var v0 = new V3(6697.4581339203833, -2033.861683166539, -3276.0604346038458);
            var r1 = new V3(81107302085.431351, -113426897855.24834, -60210730828.347397);
            var v1 = new V3(23670.714980723751, 17290.925848593863, -1058.9529613984164);
            var r2 = new V3(-27443019348.482018, -102420278412.13942, -21215617737.593018);
            var v2 = new V3(32023.172615437456, -6174.1220120587095, -12826.981238246499);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 324858592000000.06;
            double soi2 = 616280853.74695194;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 12000356.02100748;
            double arrivalDTlower = 0;
            double arrivalDTupper = double.PositiveInfinity;
            double peR = 6204000;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            _testOutputHelper.WriteLine($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            dv.magnitude.ShouldEqual(3449.6799310543329, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }
    }
}
