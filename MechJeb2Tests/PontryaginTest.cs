using System;
using Xunit;

namespace MuMech
{
    public class PontryaginTest
    {
        [Fact]
        public void OptimizeFiniteBurn()
        {
            double mu = 3.9860044189e+14;
            double rearth = 6.371e+6;
            double r185   = rearth + 0.185e+6;
            double r1000  = rearth + 1.000e+6;
            double v185   = Math.Sqrt(mu/r185);
            double smaT = ( r185 + r1000 ) / 2;
            double vTm = Math.Sqrt(mu * (2/r185 - 1/smaT) );
            double inc = 135 * UtilMath.Deg2Rad;
            Vector3d rT = new Vector3d(r185,0,0);
            Vector3d vT = new Vector3d(0,Math.Cos(inc),Math.Sin(inc)) * vTm;
            Pontryagin p = new Pontryagin(r0: new Vector3d(r185,0,0), v0: new Vector3d(0,v185,0), m0: 32740, mu: mu);
            p.terminal5constraint(rT, vT);

            double[] y0 = { 1, 0, 0, 0, 1, 0, 0, -0.92161000483999, 0.388117249009669, 0, 0, 0, 32740, 0.967166759159267, -0.254142597722927, 0, 0.254142597722927, 0.96716675915927, 0, 0.00769021351324089, -0.892344077177788, 0.375374101898492, 0.088791298574539, 0.218839933690996, -0.0986371258843934, 32740, -0.256961090562725, 0.513922181125449 };
            double[] yf = new double[28];
            p.Optimize(y0, yf);
            double[] yfexp = { 1, 0, 0, 0, 1, 0, -0.0079393319393622, -0.921121328331003, 0.38831077875087, -0.0249794942101294, -0.00793933193936288, 0.000443266671129501, 32740, 0.916851279873006, -0.399228920038651, -6.63961013907058e-25, 0.399228920038652, 0.916851279873009, -2.95663849598675e-24, 0.0105704943277902, -0.853632995077797, 0.356200199360623, 0.182433205882693, 0.302834215667251, -0.154618483225328, 32740, -0.410675683157609, 0.514092715632044 };
            p.Optimize(yf, yf);

            for(int i = 0; i < 28; i++)
                Console.WriteLine((yf[i] - yfexp[i]) + " " + yf[i] + " " + yfexp[i]);

            for(int i = 0; i < 28; i++)
                Assert.Equal(0, yf[i] - yfexp[i], 8);

            Assert.Equal(0, 1);
        }
    }
}
