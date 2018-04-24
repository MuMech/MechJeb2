using System;
using Xunit;
//using MuMech.Pontryagin;

namespace MuMech
{
    public class PontryaginTest
    {
        /* this might be an initially retrograde burn from a circular orbit */
        [Fact]
        public void centralForceThrustSimple()
        {
            Pontryagin p = new Pontryagin();
            double[] y = { 1, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 0, 32740 };
            double[] dy = new double[13];
            Pontryagin.Engine e = new Pontryagin.Engine(63136.1585987428, 25092.0703945434);
            p.centralForceThrust(y, 0, dy, e);
            double[] dyexp = { 0, 1, 0, -1, -0.766404104903586, 0, 0, 0, 0, 0, -1, 0, -63136.1585987428 };
            for(int i = 0; i < 13; i++)
            {
                Assert.Equal(0, dy[i] - dyexp[i], 14);
            }
        }

        /* this is the initial conditions of the singleIntegrateComplex() problem */
        [Fact]
        public void centralForceThrustComplex()
        {
            Pontryagin p = new Pontryagin();
            double[] y = { 0.916851279873005, -0.399228920038652, -7.56664470074416e-27, 0.399228920038653, 0.916851279873009, -4.78371853721768e-27, 0.010574128246995, -0.853926456601413, 0.356322653686804, 0.18249592267875, 0.302938323862286, -0.15467163789007, 32740 };
            double[] dy = new double[13];
            Pontryagin.Engine e = new Pontryagin.Engine(63136.1585987428, 25092.0703945434);
            p.centralForceThrust(y, 0, dy, e);
            /* from matlab running the same problem */
            double[] dyexp = { 0.399228920038653, 0.916851279873009, -4.78371853721768e-27, -0.908093429346993, -0.308021842619037, 0.295118469071904, -0.18249592267875, -0.302938323862286, 0.15467163789007, -0.953789412037603, -0.434009046696293, 0.356322653686806, -63136.1585987428 };
            for(int i = 0; i < 13; i++)
            {
                Assert.Equal(0, dy[i] - dyexp[i], 14);
            }
        }

        /* this is the (normalized) kernel of a 32.74t rocket with an LR-91 in a 185x185 0 inclination orbit doing
           a burn to change to a 1000x185 orbit with a 135 degree inclination change, for over 14,000dV burning
           the rocket down to 282kg (ludicrously burning up half of the 589kg LR-91 along with the rest of
           the rocket) */
        [Fact]
        public void singleIntegrateComplex()
        {
            Pontryagin p = new Pontryagin();
            double[] y0 = { 0.916851279873005, -0.399228920038652, -7.56664470074416e-27, 0.399228920038653, 0.916851279873009, -4.78371853721768e-27, 0.010574128246995, -0.853926456601413, 0.356322653686804, 0.18249592267875, 0.302938323862286, -0.15467163789007, 32740 };
            double[] yf = new double[13];
            Pontryagin.Engine e = new Pontryagin.Engine(63136.1585987428, 25092.0703945434);
            double t = -0.410675683157609;
            p.singleIntegrate(y0, yf, 0, ref t, 0.514092715632044, e);
            /* these are from matlab's ode45 running the same problem */
            double[] yfexp = { 0.994701154258547, -0.0747987076177664, 0.0747990550682555, -0.102784424098583, -0.72378325317481, 0.723705406777111, 0.00201415681002393, -0.918310448019604, 0.387151866478535, -0.0830534635509012, -0.0901110052478756, 0.0356709459759326, 282.160771396895 };
            /* even between matlab's ode45 and ode23 solver we can't compare more precisely than this */
            for(int i = 0; i < 13; i++)
                Assert.Equal(0, yf[i] - yfexp[i], 3);
            Assert.Equal(0, t - 0.103417032474435, 14);
        }

        /* same problem as the rest, but with multiple shooting and a coast-burn -- 26 + 2 starting conditions, 26 output conditions. */
        [Fact]
        public void multpleIntegrateComplex()
        {
            Pontryagin p = new Pontryagin();
            Pontryagin.Problem o = new Pontryagin.Problem();
            double[] y0 = { 1, 0, 0, 0, 1, 0, -0.00794206131892028, -0.921437990960034, 0.388444272035955, -0.0249880816482411, -0.0079420613189204, 0.000443419057124364, 32740, 0.916851279873005, -0.399228920038652, -7.56664470074416e-27, 0.399228920038653, 0.916851279873009, -4.78371853721768e-27, 0.010574128246995, -0.853926456601413, 0.356322653686804, 0.18249592267875, 0.302938323862286, -0.15467163789007, 32740, -0.410675683157609, 0.514092715632044 };
            double[] yf = new double[26];
            p.multipleIntegrate(y0, yf, o);

            double[] yfexp = { 0.916851279873006, -0.399228920038652, 0, 0.399228920038653, 0.916851279873009, 0, 0.010574128246995, -0.853926456601413, 0.356322653686804, 0.182495922678749, 0.302938323862286, -0.15467163789007, 32740, 0.994701157874287, -0.0747993082881341, 0.0747993082881341, -0.102784710382227, -0.723648653663255, 0.723648653663275, 0.00201415851335627, -0.918310454613756, 0.387151869029871, -0.0830532176870933, -0.090110977826096, 0.0356709463649161, 282.160771396965 };
            for(int i = 0; i < 26; i++)
                Assert.Equal(0, yf[i] - yfexp[i], 3);
        }

        [Fact]
        public void optimizationFunctionComplex()
        {
            Pontryagin p = new Pontryagin();
            Pontryagin.Problem o = new Pontryagin.Problem();
            Vector3d rT = new Vector3d(1,0,0);
            Vector3d vT = new Vector3d(0,-0.727502378932227,0.727502378932227);
            o.r0 = new Vector3d(1,0,0);
            o.v0 = new Vector3d(0,1,0);
            o.m0 = 32740;
            o.bcfun = (double[] yT, double[] zterm) => p.terminal5constraint(yT, zterm, rT, vT);
            double[] y0 = { 1, 0, 0, 0, 1, 0, -0.0079393319393622, -0.921121328331003, 0.38831077875087, -0.0249794942101294, -0.00793933193936288, 0.000443266671129501, 32740, 0.916851279873006, -0.399228920038651, -6.63961013907058e-25, 0.399228920038652, 0.916851279873009, -2.95663849598675e-24, 0.0105704943277902, -0.853632995077797, 0.356200199360623, 0.182433205882693, 0.302834215667251, -0.154618483225328, 32740, -0.410675683157609, 0.514092715632044 };
            double[] z = new double[28];
            p.optimizationFunction(y0, z, o);
            for(int i = 0; i < 28; i++)
                Assert.Equal(0, z[i], 4);
        }

        [Fact]
        public void OptimizeComplex()
        {
            Pontryagin p = new Pontryagin();
            Pontryagin.Problem o = new Pontryagin.Problem();
            Vector3d rT = new Vector3d(1,0,0);
            Vector3d vT = new Vector3d(0,-0.727502378932227,0.727502378932227);
            o.r0 = new Vector3d(1,0,0);
            o.v0 = new Vector3d(0,1,0);
            o.m0 = 32740;
            o.bcfun = (double[] yT, double[] zterm) => p.terminal5constraint(yT, zterm, rT, vT);
            double[] y0 = { 1, 0, 0, 0, 1, 0, 0, -0.92161000483999, 0.388117249009669, 0, 0, 0, 32740, 0.967166759159267, -0.254142597722927, 0, 0.254142597722927, 0.96716675915927, 0, 0.00769021351324089, -0.892344077177788, 0.375374101898492, 0.088791298574539, 0.218839933690996, -0.0986371258843934, 32740, -0.256961090562725, 0.513922181125449 };
            double[] yf = new double[28];
            p.Optimize(y0, yf, o);
            double[] yfexp = { 1, 0, 0, 0, 1, 0, -0.0079393319393622, -0.921121328331003, 0.38831077875087, -0.0249794942101294, -0.00793933193936288, 0.000443266671129501, 32740, 0.916851279873006, -0.399228920038651, -6.63961013907058e-25, 0.399228920038652, 0.916851279873009, -2.95663849598675e-24, 0.0105704943277902, -0.853632995077797, 0.356200199360623, 0.182433205882693, 0.302834215667251, -0.154618483225328, 32740, -0.410675683157609, 0.514092715632044 };
            p.Optimize(yf, yf, o);

            for(int i = 0; i < 28; i++)
                Console.WriteLine((yf[i] - yfexp[i]) + " " + yf[i] + " " + yfexp[i]);

            for(int i = 0; i < 28; i++)
                Assert.Equal(0, yf[i] - yfexp[i], 3);

        }
    }
}
