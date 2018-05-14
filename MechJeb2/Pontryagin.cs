using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech {
    public enum ArcType { COAST, BURN };

    public class Pontryagin {
        public struct Arc
        {
            public double isp, thrust;
            public double At, c;
            public ArcType type;

            public Arc(ArcType type, double isp = 0, double thrust = 0)
            {
                if (type == ArcType.COAST && ( isp != 0 || thrust != 0 ) )
                    throw new Exception("Non zero isp and/or thrust given to coast arc");

                if (type == ArcType.BURN && ( isp  == 0 || thrust == 0 ) )
                    throw new Exception("Zero isp and/or thrust given to burn arc");

                this.isp = isp;
                this.thrust = thrust;
                this.type = type;
                this.At = this.c = 0;
            }
        }

        public List<Arc> arcs;
        public Vector3d r0, r0_bar;
        public Vector3d v0, v0_bar;
        public double m0;
        public double mu;
        public Action<double[], double[]> bcfun;
        public const double g0 = 9.80665;

        // this constructor is mostly intended for testing
        public Pontryagin()
        {
        }

        public Pontryagin(Vector3d r0, Vector3d v0, double m0, double mu, List<Arc> arcs = null)
        {
            if (arcs == null)
                this.arcs = new List<Arc>();
            else
                this.arcs = arcs;
            this.r0 = r0;
            this.v0 = v0;
            this.m0 = m0;
            this.mu = mu;
        }

        public double g_bar, r_scale, v_scale, t_scale;  /* scaling constants */

        public void Normalize()
        {
            double r0m = r0.magnitude;
            g_bar = mu / ( r0m * r0m );
            r_scale = r0m;
            v_scale = Math.Sqrt( r0m * g_bar );
            t_scale = Math.Sqrt( r0m / g_bar );
            r0_bar = r0 / r_scale;
            v0_bar = v0 / v_scale;
            for(int i = 0; i < arcs.Count; i++)
            {
                Arc arc = arcs[i];
                arc.c = arc.isp / Math.Sqrt( r_scale / ( g0 * g0 * g0 ) );
            }
        }

        public void UpdateCurrentState(Vector3d r0, Vector3d v0, double m0)
        {
            this.r0 = r0;
            this.v0 = v0;
            this.m0 = m0;
        }

        public void AddArc(Arc arc) {
            arcs.Add(arc);
        }

        public int numArcs { get { return arcs.Count; } }
        private int timeIndex { get { return arcs.Count * 13; } }

        public void centralForceThrust(double[] y, double x, double[] dy, object o)
        {
            Arc arc = (Arc)o;
            double At = arc.thrust / ( y[12] * g0 );
            double r2 = y[0]*y[0] + y[1]*y[1] + y[2]*y[2];
            double r = Math.Sqrt(r2);
            double r3 = r2 * r;
            double r5 = r3 * r2;
            double pvm = Math.Sqrt(y[6]*y[6] + y[7]*y[7] + y[8]*y[8]);
            double rdotpv = y[0] * y[6] + y[1] * y[7] + y[2] * y[8];

            /* dr = v */
            dy[0] = y[3];
            dy[1] = y[4];
            dy[2] = y[5];
            /* dv = - r / r^3 + At * u */
            dy[3] = - y[0] / r3 + At * y[6] / pvm;
            dy[4] = - y[1] / r3 + At * y[7] / pvm;
            dy[5] = - y[2] / r3 + At * y[8] / pvm;
            /* dpv = - pr */
            dy[6] = - y[9];
            dy[7] = - y[10];
            dy[8] = - y[11];
            /* dpr = pv / r3 - 3 / r5 dot(r, pv) r */
            dy[9]  = y[6] / r3 - 3 / r5 * rdotpv * y[0];
            dy[10] = y[7] / r3 - 3 / r5 * rdotpv * y[1];
            dy[11] = y[8] / r3 - 3 / r5 * rdotpv * y[2];
            /* m = mdot */
            dy[12] = - arc.thrust / arc.c;
            /* FIXME: should throw if we get down to less than a kg or something */
        }

        // free attachment into the orbit defined by rT + vT
        public void terminal5constraint(Vector3d rT, Vector3d vT)
        {
            bcfun = (double[] yT, double[] zterm) => terminal5constraint(yT, zterm, rT, vT);
        }

        private void terminal5constraint(double[] yT, double[] z, Vector3d rT, Vector3d vT)
        {
            Vector3d rT_bar = rT / r_scale;
            Vector3d vT_bar = vT / v_scale;

            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d hT = Vector3d.Cross(rT_bar, vT_bar);

            if (hT[2] == 0)
            {
                /* degenerate inc=90 case so swap all the coordinates */
                rf = rf.Reorder(231);
                vf = vf.Reorder(231);
                rT_bar = rT_bar.Reorder(231);
                vT_bar = vT_bar.Reorder(231);
                prf = prf.Reorder(231);
                pvf = pvf.Reorder(231);
                hT = Vector3d.Cross(rT_bar, vT_bar);
            }

            Vector3d hf = Vector3d.Cross(rf, vf);
            Vector3d eT = - ( rT_bar.normalized + Vector3d.Cross(hT, vT_bar) );
            Vector3d ef = - ( rf.normalized + Vector3d.Cross(hf, vf) );
            Vector3d hmiss = hf - hT;
            Vector3d emiss = ef - eT;
            double trans = Vector3d.Dot(prf, vf) - Vector3d.Dot(pvf, rf) / ( rf.magnitude * rf.magnitude * rf.magnitude );

            z[0] = hmiss[0];
            z[1] = hmiss[1];
            z[2] = hmiss[2];
            z[3] = emiss[0];
            z[4] = emiss[1];
            z[5] = trans;
        }

        public void singleIntegrate(double[] y0, double[] yf, int n, ref double t, double dt, Arc e)
        {
            double eps = 0.00001;
            double h = 0;
            double[] y = new double[13];
            double[] x = new double[2]{t, t+dt};
            double[] xtbl;
            double[,] ytbl;
            int m;
            alglib.odesolverstate s;
            alglib.odesolverreport rep;
            Array.Copy(y0, 13*n, y, 0, 13);
            alglib.odesolverrkck(y, 13, x, 2, eps, h, out s);
            alglib.odesolversolve(s, centralForceThrust, e);
            alglib.odesolverresults(s, out m, out xtbl, out ytbl, out rep);
            for(int i = 0; i < ytbl.GetLength(1); i++)
            {
                int j = 13*n+i;
                yf[j] = ytbl[1,i];
            }
            t = t + dt;
        }

        public void multipleIntegrate(double[] y0, double[] yf)
        {
            double t = 0;

            for(int i = 0; i < arcs.Count; i++)
            {
               singleIntegrate(y0, yf, i, ref t, y0[timeIndex + i], arcs[i]);
            }

            //arc = new Arc(ArcType.BURN, isp: 316, thrust: 232.7 * 1000);
        }

        public void optimizationFunction(double[] y0, double[] z, object o)
        {
            double[] yf = new double[26];
            multipleIntegrate(y0, yf);

            /* initial conditions */
            z[0] = y0[0] - r0[0];
            z[1] = y0[1] - r0[1];
            z[2] = y0[2] - r0[2];
            z[3] = y0[3] - v0[0];
            z[4] = y0[4] - v0[1];
            z[5] = y0[5] - v0[2];
            z[6] = y0[12] - m0;

            /* terminal constraints */
            double[] yT = new double[13];
            Array.Copy(yf, 13, yT, 0, 13);
            double[] zterm = new double[6];
            if ( bcfun == null )
                throw new Exception("No bcfun was provided to the Pontryagin optimizer");

            bcfun(yT, zterm);

            z[7] = zterm[0];
            z[8] = zterm[1];
            z[9] = zterm[2];
            z[10] = zterm[3];
            z[11] = zterm[4];
            z[12] = zterm[5];

            /* multiple shooting continuity */
            for(int i = 0; i < 13; i++)
                z[i+13] = y0[i+13] - yf[i];

            Vector3d r1 = new Vector3d(yf[0], yf[1], yf[2]);
            Vector3d v1 = new Vector3d(yf[3], yf[4], yf[5]);
            Vector3d pv1 = new Vector3d(yf[6], yf[7], yf[8]);
            Vector3d pr1 = new Vector3d(yf[9], yf[10], yf[11]);

            /* switching condition for coast */
            z[26] = Vector3d.Dot(pr1, v1) - Vector3d.Dot(pv1, r1) / ( r1.magnitude * r1.magnitude * r1.magnitude );  /* H0(t1) = 0 */

            /* magnitude of initial costate vector = 1.0 (dummy constraint because BC is keplerian) */
            z[27] = 0.0;
            for(int i = 0; i < 6; i++)
                z[27] = z[27] + y0[6+i] * y0[6+i];
            z[27] = Math.Sqrt(z[27]) - 1.0;

            /* construct sum of the squares of the residuals for levenberg marquardt */
            for(int i = 0; i < 28; i++)
                z[i] = z[i] * z[i];
        }


        public void Optimize(double[] y0, double[] yf)
        {
            Normalize();
            alglib.minlmstate state;
            alglib.minlmreport rep = new alglib.minlmreport();
            alglib.minlmcreatev(28, 28, y0, 0.001, out state);  /* TODO: what should diffstate be? */
            alglib.minlmsetcond(state, 0.00001, 1000);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            alglib.minlmoptimize(state, optimizationFunction, null, null);
            alglib.minlmresultsbuf(state, ref yf, rep);
            stopWatch.Stop();
            Console.WriteLine("Report Code: " + rep.terminationtype);
            Console.WriteLine("Iterations: " + rep.iterationscount);
            Console.WriteLine("NFunc: " + rep.nfunc);
            Console.WriteLine("NJac: " + rep.njac);
            Console.WriteLine("NHess: " + rep.nhess);
            Console.WriteLine("NCholesky: " + rep.ncholesky);
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            Console.WriteLine("RunTime " + elapsedTime);
        }
    }
}
