using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public enum ArcType { COAST, BURN };

    /*
     * MULTIBURN - strictly N burns without coasts, with a single overall optimized burntime, with mass jettison
     */

    public enum ProbType { MULTIBURN, COASTBURN };

    public class Pontryagin {
        public class Arc
        {
            public double isp, thrust;
            public double c;
            public ArcType type;
            public double m0, dt0;
            public double dt0_bar;
            public double max_bt, max_bt_bar;
            public double dV;  /* actual integrated time of the burn */

            public override string ToString()
            {
                return type + " isp:" + isp + " thrust:" + thrust + " maxt:" + max_bt;
            }

            public Arc(ArcType type, double m0, double dt0, double isp = 0, double thrust = 0, double max_bt = 0)
            {
                if (type == ArcType.COAST && ( isp != 0 || thrust != 0 ) )
                    throw new Exception("Non zero isp and/or thrust given to coast arc");

                if (type == ArcType.BURN && ( isp  == 0 || thrust == 0 ) )
                    throw new Exception("Zero isp and/or thrust given to burn arc");

                this.isp = isp;
                this.thrust = thrust;
                this.type = type;
                this.c = 0;
                this.m0 = m0;
                this.dt0 = dt0;
                this.max_bt = max_bt;
            }
        }

        public class Solution
        {
            public double t0;  // kerbal time
            public double t_scale;
            public double v_scale;
            public double r_scale;

            public Solution(double t_scale, double v_scale, double r_scale, double t0)
            {
                this.t_scale = t_scale;
                this.v_scale = v_scale;
                this.r_scale = r_scale;
                this.t0 = t0;
            }

            public double tgo(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return ( tmax() - tbar ) * t_scale;
            }

            public double vgo(double t)
            {
                return dV(tf()) - dV(t);
            }

            public double tf() // kerbal time
            {
                return t0 + tmax() * t_scale;
            }

            public double tmax() // normalized time
            {
                return segments[segments.Count-1].tmax;
            }

            public double tmin() // normalized time
            {
                return segments[0].tmin;
            }

            public Vector3d r(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return Planetarium.fetch.rotation * new Vector3d( interpolate(0, tbar), interpolate(1, tbar), interpolate(2, tbar) ) * r_scale;
            }

            public Vector3d r_bar(double tbar)
            {
                return new Vector3d( interpolate(0, tbar), interpolate(1, tbar), interpolate(2, tbar) );
            }

            public Vector3d v(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return Planetarium.fetch.rotation * new Vector3d( interpolate(3, tbar), interpolate(4, tbar), interpolate(5, tbar) ) * v_scale;
            }

            public Vector3d v_bar(double tbar)
            {
                return new Vector3d( interpolate(3, tbar), interpolate(4, tbar), interpolate(5, tbar) );
            }

            public Vector3d pv(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                var pv = new Vector3d( interpolate(6, tbar), interpolate(7, tbar), interpolate(8, tbar) );
                return Planetarium.fetch.rotation * new Vector3d( interpolate(6, tbar), interpolate(7, tbar), interpolate(8, tbar) );
            }

            public Vector3d pr(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return Planetarium.fetch.rotation * new Vector3d( interpolate(9, tbar), interpolate(10, tbar), interpolate(11, tbar) );
            }

            public double m(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return interpolate(12, tbar);
            }

            public double dV(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return interpolate(13, tbar) * v_scale;
            }

            public void pitch_and_heading(double t, ref double pitch, ref double heading)
            {
                double tbar = ( t - t0 ) / t_scale;
                Vector3d rbar = new Vector3d( interpolate(0, tbar), interpolate(1, tbar), interpolate(2, tbar) );
                Vector3d pv = new Vector3d( interpolate(6, tbar), interpolate(7, tbar), interpolate(8, tbar) );
                Vector3d headVec = pv - Vector3d.Dot(pv, rbar) * rbar;
                Vector3d east = Vector3d.Cross(rbar, new Vector3d(0, 1, 0)).normalized;
                Vector3d north = Vector3d.Cross(east, rbar).normalized;
                pitch = 90.0 - Vector3d.Angle(pv, rbar);
                heading = MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(headVec, east), Vector3d.Dot(headVec, north)));
            }

            double interpolate(int i, double tbar)
            {
                for(int k = 0; k < segments.Count; k++)
                {
                    Segment s = segments[k];
                    if (tbar < s.tmax)
                        return alglib.barycentriccalc(s.interpolant[i], tbar);
                }
                return alglib.barycentriccalc(segments[segments.Count-1].interpolant[i], tbar);
            }

            List<Segment> segments = new List<Segment>();

            class Segment
            {
                public Arc arc;
                public double tmin, tmax;
                public alglib.barycentricinterpolant[] interpolant = new alglib.barycentricinterpolant[14];

                public Segment(double[] t, double[,] y, Arc a)
                {
                    arc = a;
                    int n = t.Length;
                    tmin = t[0];
                    tmax = t[n-1];
                    for(int i = 0; i < 14; i++)
                    {
                        double[] yi = new double[n];
                        for(int j = 0; j < n; j++)
                        {
                            yi[j] = y[j,i];
                        }
                        alglib.polynomialbuildeqdist(tmin, tmax, yi, out interpolant[i]);
/*
                        double[] yi = new double[n-2];
                        for(int j = 1; j < (n-1); j++)
                        {
                            yi[j-1] = y[j,i];
                        }
                        alglib.polynomialbuildcheb1(tmin, tmax, yi, out interpolant[i]);
                        */
                    }
                }
            }

            public void AddSegment(double[] t, double[,] y, Arc a)
            {
                segments.Add(new Segment(t, y, a));
            }
        }

        public List<Arc> arcs;
        public double mu;
        public Action<double[], double[]> bcfun;
        public const double g0 = 9.80665;
        public ProbType type;
        public Vector3d r0, v0, r0_bar, v0_bar;
        public Vector3d pv0, pr0;
        public double tgo, tgo_bar, vgo, vgo_bar;
        public double g_bar, r_scale, v_scale, t_scale;  /* problem scaling */


        public Pontryagin(ProbType type, double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0)
        {
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            this.r0 = rot * r0;
            this.v0 = rot * v0;
            this.arcs = new List<Arc>();
            this.mu = mu;
            this.type = type;
            this.pv0 = rot * pv0;
            this.pr0 = rot * pr0;
            double r0m = r0.magnitude;
            g_bar = mu / ( r0m * r0m );
            r_scale = r0m;
            v_scale = Math.Sqrt( r0m * g_bar );
            t_scale = Math.Sqrt( r0m / g_bar );
            r0_bar = r0 / r_scale;
            v0_bar = v0 / v_scale;
        }

        public void UpdatePosition(Vector3d r0, Vector3d v0, Vector3d lambda, Vector3d lambdaDot, double tgo, double vgo)
        {
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            this.r0 = rot * r0;
            this.v0 = rot * v0;
            if (solution != null)
            {
                /* uhm, FIXME: this round trip is silly */
                this.pv0 = rot * lambda;
                this.pr0 = rot * lambdaDot;
                this.tgo = tgo;
                this.tgo_bar = tgo / t_scale;
                this.vgo = vgo;
                this.vgo_bar = vgo / v_scale;
            }
            r0_bar = r0 / r_scale;
            v0_bar = v0 / v_scale;
        }

        public void NormalizeArc(int i)
        {
            arcs[i].c = g0 * arcs[i].isp / Math.Sqrt( r_scale / g_bar );
            arcs[i].dt0_bar = arcs[i].dt0 / t_scale;
            arcs[i].max_bt_bar = arcs[i].max_bt / t_scale;
        }

        public void NormalizeArcs()
        {
            for(int i = 0; i < arcs.Count; i++)
                NormalizeArc(i);
        }

        public void AddArc(ArcType type, double m0, double dt0, double isp = 0, double thrust = 0, double max_bt = 0)
        {
            arcs.Add(new Arc(type, m0, dt0, isp, thrust, max_bt));
        }

        public int numArcs { get { return arcs.Count; } }
        private int arcIndex { get { return 2; } }

        public void centralForceThrust(double[] y, double x, double[] dy, object o)
        {
            Arc arc = (Arc)o;
            double At = arc.thrust / ( y[12] * g_bar );
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
            dy[12] = ( arc.thrust == 0 ) ? 0 : - arc.thrust / arc.c;
            dy[13] = At;
        }

        // 4-constraint PEG with free LAN
        public void flightangle4constraint(double rTm, double vTm, double gamma, double inc)
        {
            bcfun = (double[] yT, double[] zterm) => flightangle4constraint(yT, zterm, rTm, vTm, gamma, inc);
        }

        private void flightangle4constraint(double[] yT, double[] z, double rTm, double vTm, double gamma, double inc)
        {
            double rTm_bar = rTm / r_scale;
            double vTm_bar = vTm / v_scale;

            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d n = new Vector3d(0, -1, 0);  /* angular momentum vectors point south in KSP */
            Vector3d rn = Vector3d.Cross(rf, n);
            Vector3d vn = Vector3d.Cross(vf, n);
            Vector3d hf = Vector3d.Cross(rf, vf);

            z[0] = ( rf.magnitude * rf.magnitude - rTm_bar * rTm_bar ) / 2.0;
            z[1] = ( vf.magnitude * vf.magnitude - vTm_bar * vTm_bar ) / 2.0;
            z[2] = Vector3d.Dot(n, hf) - hf.magnitude * Math.Cos(inc);
            z[3] = Vector3d.Dot(rf, vf) - rf.magnitude * vf.magnitude * Math.Sin(gamma);
            z[4] = rTm_bar * rTm_bar * ( Vector3d.Dot(vf, prf) - vTm_bar * Math.Sin(gamma) / rTm_bar * Vector3d.Dot(rf, prf) ) -
                vTm_bar * vTm_bar * ( Vector3d.Dot(rf, pvf) - rTm_bar * Math.Sin(gamma) / vTm_bar * Vector3d.Dot(vf, pvf) );
            z[5] = Vector3d.Dot(hf, prf) * Vector3d.Dot(hf, rn) + Vector3d.Dot(hf, pvf) * Vector3d.Dot(hf, vn);
        }

        // free attachment into the orbit defined by rT + vT
        public void terminal5constraint(Vector3d rT, Vector3d vT)
        {
            bcfun = (double[] yT, double[] zterm) => terminal5constraint(yT, zterm, rT, vT);
        }

        private void terminal5constraint(double[] yT, double[] z, Vector3d rT, Vector3d vT)
        {
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            Vector3d rT_bar = rot * rT / r_scale;
            Vector3d vT_bar = rot * vT / v_scale;

            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d hT = Vector3d.Cross(rT_bar, vT_bar);

            if (hT[1] == 0)
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
            z[4] = emiss[2];
            z[5] = trans;
        }

        double ckEps = 1e-9;

        /* used to update y0 to yf without intermediate values */
        public void singleIntegrate(double[] y0, double[] yf, int n, ref double t, double dt, Arc e, ref double dV)
        {
            singleIntegrate(y0, yf, null, n, ref t, dt, e, 2, ref dV);
        }

        public void singleIntegrate(double[] y0, Solution sol, int n, ref double t, double dt, Arc e, int count, ref double dV)
        {
            singleIntegrate(y0, null, sol, n, ref t, dt, e, count, ref dV);
        }

        public void singleIntegrate(double[] y0, double[] yf, Solution sol, int n, ref double t, double dt, Arc e, int count, ref double dV)
        {
            if ( count < 2)
                count = 2;

            if ( dt == 0 && yf != null)
            {
                Array.Copy(y0, 13*n+arcIndex, yf, 13*n, 13);
                return;
            }

            double[] y = new double[14];
            Array.Copy(y0, 13*n+arcIndex, y, 0, 13);
            y[13] = dV;

            /* time to burn the entire stage */
            double tau = e.isp * g0 * y[12] / e.thrust / t_scale;

            /* clip the integration so we don't burn the whole rocket (causes infinite spinning) */
            if ( dt > 0.9999 * tau )
                dt = 0.9999 * tau;

            double[] x = new double[count];
            for(int i = 0; i < count; i++)
                x[i] = t + dt * i / (count - 1 );

            /*
            // Chebyshev sampling
            for(int i = 1; i <= (count-2); i++)
                x[i] = 0.5 * (t + dt ) + 0.5 * (dt - t) * Math.Cos(Math.PI*(2*i-1)/(2*(count-2)));
            // But also get the endpoints exactly
            x[0] = t;
            x[count-1] = t + dt;
            */

            double[] xtbl;
            double[,] ytbl;
            int m;
            alglib.odesolverstate s;
            alglib.odesolverreport rep;
            alglib.odesolverrkck(y, 14, x, count, ckEps, 0, out s);
            alglib.odesolversolve(s, centralForceThrust, e);
            alglib.odesolverresults(s, out m, out xtbl, out ytbl, out rep);
            t = t + dt;

            e.dV = ( ytbl[count-1,13] - dV ) * v_scale;
            dV = ytbl[count-1,13];

            if (sol != null && dt != 0)
            {
                sol.AddSegment(xtbl, ytbl, e);
            }

            if (yf != null)
            {
                for(int i = 0; i < 13; i++)
                {
                    int j = 13*n+i;
                    yf[j] = ytbl[1,i];
                }
            }
        }

        public void multipleIntegrate(double[] y0, double[] yf)
        {
            multipleIntegrate(y0, yf, null);
        }

        public void multipleIntegrate(double[] y0, Solution sol, int count)
        {
            multipleIntegrate(y0, null, sol, count);
        }

        public void multipleIntegrate(double[] y0, double[] yf, Solution sol, int count = 2)
        {
            double t = 0;

            double tgo = y0[0];
            double dV = 0;

            for(int i = 0; i < arcs.Count; i++)
            {
                if ( (tgo <= arcs[i].max_bt_bar) || (i == arcs.Count-1) )
                {
                    if (yf != null) {
                        singleIntegrate(y0, yf, i, ref t, tgo, arcs[i], ref dV);
                    } else {
                        singleIntegrate(y0, sol, i, ref t, tgo, arcs[i], count, ref dV);
                    }
                    tgo = 0;
                }
                else
                {
                    if (yf != null) {
                        singleIntegrate(y0, yf, i, ref t, arcs[i].max_bt_bar, arcs[i], ref dV);
                    } else {
                        singleIntegrate(y0, sol, i, ref t, arcs[i].max_bt_bar, arcs[i], count, ref dV);
                    }
                    tgo -= arcs[i].max_bt_bar;
                }
            }
        }

        public void optimizationFunction(double[] y0, double[] z, object o)
        {
            double[] yf = new double[numArcs*13];  /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
            multipleIntegrate(y0, yf);

            /* initial conditions */
            z[0] = y0[arcIndex+0] - r0_bar[0];
            z[1] = y0[arcIndex+1] - r0_bar[1];
            z[2] = y0[arcIndex+2] - r0_bar[2];
            z[3] = y0[arcIndex+3] - v0_bar[0];
            z[4] = y0[arcIndex+4] - v0_bar[1];
            z[5] = y0[arcIndex+5] - v0_bar[2];
            z[6] = y0[arcIndex+12] - arcs[0].m0;

            /* terminal constraints */
            double[] yT = new double[13];
            Array.Copy(yf, (numArcs-1)*13, yT, 0, 13);
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
            for(int i = 1; i < numArcs; i++)
            {
                for(int j = 0; j < 13; j++)
                {
                    if ( j == 12 )
                    {
                        /* mass jettison */
                        z[j+13*i] = y0[j+13*i+arcIndex] - arcs[i].m0;
                        //z[j+13*i] = y0[j+13*i] - yf[j+13*(i-1)];
                    }
                    else
                        z[j+13*i] = y0[j+13*i+arcIndex] - yf[j+13*(i-1)];
                }
            }

            Vector3d r1 = new Vector3d(yf[0], yf[1], yf[2]);
            Vector3d v1 = new Vector3d(yf[3], yf[4], yf[5]);
            Vector3d pv1 = new Vector3d(yf[6], yf[7], yf[8]);
            Vector3d pr1 = new Vector3d(yf[9], yf[10], yf[11]);

            /* magnitude of initial costate vector = 1.0 (dummy constraint for H(tf)=0 because BC is keplerian) */
            int n = 13 * numArcs;
            z[n] = 0.0;
            for(int i = 0; i < 6; i++)
                z[n] = z[n] + y0[6+i+arcIndex] * y0[6+i+arcIndex];
            z[n] = Math.Sqrt(z[n]) - 1.0;

            // positive arc time constraint
            z[n+1] = ( y0[0] < 0 ) ? ( y0[0] - y0[1] * y0[1] ) * 1000 : 0;

            /* if (z.Length == 28)
                z[27] = Vector3d.Dot(pr1, v1) - Vector3d.Dot(pv1, r1) / ( r1.magnitude * r1.magnitude * r1.magnitude );  /* H0(t1) = 0 */

            /* construct sum of the squares of the residuals for levenberg marquardt */
            for(int i = 0; i < z.Length; i++)
                z[i] = z[i] * z[i];
        }


        double lmEpsx = 0; // 1e-15;
        int lmIter = 20000;
        double lmDiffStep = 0.0001;

        public bool runOptimizer()
        {

            alglib.minlmstate state;
            alglib.minlmreport rep = new alglib.minlmreport();
            alglib.minlmcreatev(y0.Length, y0, lmDiffStep, out state);  /* y0.Length must == z.Length returned by the BC function for square problems */
            alglib.minlmsetcond(state, lmEpsx, lmIter);
            //alglib.minlmsetcond(state, 0, 0);
            /*Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start(); */
            alglib.minlmoptimize(state, optimizationFunction, null, null);
            alglib.minlmresultsbuf(state, ref y0, rep);
            return rep.terminationtype == 2;

            /*
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
            */
        }

        public double[] y0;

        public void UpdateY0Arc(int i)
        {
            Arc a = arcs[i];
            y0[arcIndex+i*13] = r0_bar[0];
            y0[arcIndex+i*13+1] = r0_bar[1];
            y0[arcIndex+i*13+2] = r0_bar[2];
            y0[arcIndex+i*13+3] = v0_bar[0];
            y0[arcIndex+i*13+4] = v0_bar[1];
            y0[arcIndex+i*13+5] = v0_bar[2];
            y0[arcIndex+i*13+6] = pv0[0];
            y0[arcIndex+i*13+7] = pv0[1];
            y0[arcIndex+i*13+8] = pv0[2];
            y0[arcIndex+i*13+9] = pr0[0];
            y0[arcIndex+i*13+10] = pr0[1];
            y0[arcIndex+i*13+11] = pr0[2];
            y0[arcIndex+i*13+12] = a.m0;
            y0[0] = tgo_bar;
            y0[1] = 0;
        }

        public Solution solution;

        public void Optimize(double t0)
        {
            try {
                int numTimes = 2; /* MULTIBURN */

                // FIXME: staging happened so we reconverge the entire problem
                if ( y0 != null )
                {
                    if ( y0.Length > 13*numArcs + numTimes )
                    {
                        /* probably normal staging */
                        double[] y0_old = y0;
                        y0 = new double[13*numArcs + numTimes];
                        Array.Copy(y0_old, 0, y0, 0, 13*numArcs + numTimes);
                    }
                    else if ( y0.Length != 13*numArcs + numTimes )
                    {
                        y0 = null;
                    }
                }

                NormalizeArcs();

                if (y0 == null)
                {
                    y0 = new double[13*numArcs + numTimes];

                    for(int i = 0; i < numArcs; i++)
                    {
                        UpdateY0Arc(i);
                    }
                }
                else
                {
                    UpdateY0Arc(0);
                }

                for(int i = 0; i < y0.Length; i++)
                    Debug.Log("  y0[" + i + "] = " + y0[i]);

                if ( runOptimizer() )
                {
                    double[] z = new double[13 * numArcs + numTimes];
                    optimizationFunction(y0, z, null);
                    double znorm = 0.0;
                    for(int i = 0; i < z.Length; i++)
                    {
                        znorm += z[i] * z[i];
                        Debug.Log("z[" + i + "] = " + z[i]);
                    }
                    znorm = Math.Sqrt(znorm);
                    Debug.Log("znorm = " + znorm);

                    if (znorm < 1e-5 && y0[13 * numArcs] > 0)
                    {
                        Solution sol = new Solution(t_scale, v_scale, r_scale, t0);

                        for(int i = 0; i < y0.Length; i++)
                            Debug.Log("y0[" + i + "] = " + y0[i]);

                        multipleIntegrate(y0, sol, 10);

                        this.solution = sol;

                        Debug.Log("rf = " + sol.r_bar(sol.tmax()) + "(" + sol.r_bar(sol.tmax()).magnitude + ") vf = " + sol.v_bar(sol.tmax()) + "(" + sol.v_bar(sol.tmax()).magnitude + ")");
                        Debug.Log("rf = " + sol.r(sol.tf()) + "(" + sol.r(sol.tf()).magnitude + ") vf = " + sol.v(sol.tf()) + "(" + sol.v(sol.tf()).magnitude + ")");
                    } else {
                      y0 = null;
                    }
                } else {
                    y0 = null;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private Thread thread;

        public void SynchStages(List<int> kspstages, FuelFlowSimulation.Stats[] vacStats)
        {
            if (thread != null && thread.IsAlive)
                return;

            arcs.Clear();

            for(int k = 0; k < kspstages.Count; k++)
            {
                int i = kspstages[k];
                AddArc(type: ArcType.BURN, m0: vacStats[i].startMass*1000, dt0: 1, isp: vacStats[i].isp, thrust: vacStats[i].startThrust * 1000, max_bt: vacStats[i].deltaTime);
            }
            for(int k = 0; k < arcs.Count; k++)
                Debug.Log(arcs[k]);
        }

        public bool threadStart(double t0)
        {
            if (thread != null && thread.IsAlive)
                return false;
            thread = new Thread(() => Optimize(t0));
            thread.Start();
            return true;
        }
    }
}
