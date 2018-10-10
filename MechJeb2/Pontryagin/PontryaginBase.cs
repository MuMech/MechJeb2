using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public abstract class PontryaginBase {
        public class Arc
        {
            public Stage stage;
            public double dV;  /* actual integrated time of the burn */

            public double isp { get { return stage.isp; } }
            public double thrust { get { return stage.thrust; } }
            public double m0 { get { return stage.m0; } }
            public double max_bt { get { return stage.max_bt; } }
            public double c { get { return stage.c; } }
            public double max_bt_bar { get { return stage.max_bt_bar; } }
            public int ksp_stage { get { return stage.ksp_stage; } }

            public bool complete_burn = false;
            public bool done = false;
            public bool coast_after_jettison = false;
            public bool use_fixed_time = false;
            public double fixed_time = 0;
            public double fixed_tbar = 0;

            public bool infinite = false;  /* zero mdot, infinite burntime+isp */

            public override string ToString()
            {
                return "stage: " + stage + ( done ? " (done)" : "" ) + ( infinite ? " (infinite) " : "" );
            }

            public Arc(Stage stage, bool done = false, bool coast_after_jettison = false, bool use_fixed_time = false, double fixed_time = 0)
            {
                this.stage = stage;
                this.done = done;
                this.coast_after_jettison = coast_after_jettison;
                this.use_fixed_time = use_fixed_time;
                this.fixed_time = fixed_time;
            }
        }

        // FIXME?  think this is dead code, but it may be useful for debugging...
        protected double LAN(Vector3d r, Vector3d v)
        {
            Vector3d n = new Vector3d(0, -1, 0);  /* angular momentum vectors point south in KSP and we're in xzy coords */
            Vector3d h = Vector3d.Cross(r, v);
            Vector3d an = -Vector3d.Cross(n, h);  /* needs to be negative (or swapped) in left handed coordinate system */
            return ( an[2] >= 0 ) ? Math.Acos(an[0] / an.magnitude) : ( 2.0 * Math.PI - Math.Acos(an[0] / an.magnitude) );
        }

        /* these objects track KSP stages */
        public class Stage
        {
            public double isp, thrust, m0, max_bt;
            public double c, max_bt_bar;
            public int ksp_stage;
            public bool staged = false;  /* if this stage has been jettisoned */

            public override string ToString()
            {
                return "ksp_stage: "+ ksp_stage + " isp:" + isp + " thrust:" + thrust + " m0: " + m0 + " maxt:" + max_bt + " maxtbar: " + max_bt_bar + " c: " + c;
            }

            public Stage(PontryaginBase p, double m0, double isp = 0, double thrust = 0, double max_bt = 0, int ksp_stage = -1)
            {
                UpdateStage(p, m0, isp, thrust, max_bt, ksp_stage);
            }

            public void UpdateStage(PontryaginBase p, double m0, double isp = 0, double thrust = 0, double max_bt = 0, int ksp_stage = -1)
            {
                this.isp = isp;
                this.thrust = thrust;
                this.m0 = m0;
                this.max_bt = max_bt;
                this.c = g0 * isp / p.t_scale;
                this.max_bt_bar = max_bt / p.t_scale;
                this.ksp_stage = ksp_stage;
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

            public double tgo(double t, int n) // tgo for each segment/arc in the solution
            {
                double tbar = ( t - t0 ) / t_scale;
                return tgo_bar(tbar, n) * t_scale;
            }

            public double current_tgo(double t)
            {
                int n = segment(t);
                return tgo(t, n);
            }

            public double tgo_bar(double tbar, int n) // tgo for each segment/arc in the solution
            {
                if (tbar > segments[n].tmin)
                    return Math.Max(segments[n].tmax - tbar, 0);
                else
                    return segments[n].tmax - segments[n].tmin;
            }

            public double tburn_bar(double tbar)
            {
                double tburn = 0.0;
                for(int i = 0; i < segments.Count; i++)
                {
                    if (arcs[i].thrust == 0)
                        continue;
                    tburn += tgo_bar(tbar, i);
                }
                return tburn;
            }

            public String ArcString(double t, int n)
            {
                Arc arc = arcs[n];
                if (arc.thrust == 0)
                {
                    return String.Format("coast: {0:F1}s", tgo(t, n));
                }
                else
                {
                    return String.Format("burn {0}: {1:F1}s {2:F1} m/s", arc.ksp_stage, tgo(t, n), dV(t, n));
                }
            }

            public double vgo(double t)
            {
                return dV(tf()) - dV(t);
            }

            public double tf() // tmax in kerbal time
            {
                return t0 + tmax() * t_scale;
            }

            public Vector3d vf()
            {
                return v(tf());
            }

            public Vector3d rf()
            {
                return r(tf());
            }

            public double tmax() // normalized time
            {
                int last = segments.Count-1;
                if ( arcs[last].thrust == 0 )
                    // we do not include the time of a final coast in overall tgo/vgo
                    return segments[last].tmin;
                else
                    return segments[last].tmax;
            }

            public double tmin() // normalized time
            {
                return segments[0].tmin;
            }

            public double tbar(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                if ( tbar < tmin() )
                    return tmin();

                if ( tbar > tmax() )
                    return tmax();

                return tbar;
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
                return Planetarium.fetch.rotation * new Vector3d( interpolate(6, tbar), interpolate(7, tbar), interpolate(8, tbar) );
            }

            public Vector3d pv_bar(double tbar)
            {
                return new Vector3d( interpolate(6, tbar), interpolate(7, tbar), interpolate(8, tbar) );
            }

            public Vector3d pr(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return Planetarium.fetch.rotation * new Vector3d( interpolate(9, tbar), interpolate(10, tbar), interpolate(11, tbar) );
            }

            public Vector3d pr_bar(double tbar)
            {
                return new Vector3d( interpolate(9, tbar), interpolate(10, tbar), interpolate(11, tbar) );
            }

            public double m(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return interpolate(12, tbar);
            }

            public double m_bar(double tbar)
            {
                return interpolate(12, tbar);
            }

            public double dV(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return interpolate(13, tbar) * v_scale;
            }

            public double dV(double t, int n)
            {
                double tbar = ( t - t0 ) / t_scale;
                double tmin = segments[n].tmin;
                double tmax = segments[n].tmax;

                if (tbar > tmin)
                    tmin = tbar;

                return ( interpolate(13, tmax) - interpolate(13, tmin) ) * v_scale;
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
                        return s.interpolate(i, tbar);
                }
                return segments[segments.Count-1].interpolate(i, tbar);
            }

            public int num_segments { get { return segments.Count; } }
            public List<Segment> segments = new List<Segment>();
            public List<Arc> arcs = new List<Arc>();

            // Arc from index
            public Arc arc(int n)
            {
                return arcs[n];
            }

            // Segment from time
            public int segment(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                for(int k = 0; k < segments.Count; k++)
                {
                    Segment s = segments[k];
                    if (tbar < s.tmax)
                        return k;
                }
                return segments.Count-1;
            }

            // Arc from time
            public Arc arc(double t)
            {
                return arcs[segment(t)];
            }

            public class Segment
            {
                public double tmin, tmax;
                public alglib.spline1dinterpolant[] interpolant = new alglib.spline1dinterpolant[14];
                public double[] ysaved;

                public double interpolate(int i, double tbar)
                {
                    if (interpolant[i] != null)
                        return alglib.spline1dcalc(interpolant[i], tbar);
                    else
                        return ysaved[i];
                }

                public Segment(List<double> t, List<double[]> y)
                {
                    int n = t.Count;
                    double[] ti = new double[n];

                    //Debug.Log("t.Count = " + t.Count);
                    //Debug.Log("y.Count = " + y.Count);
                    //Debug.Log("y[0].Length = " + y[0].Length);

                    tmin = t[0];
                    tmax = t[n-1];

                    // if tmin == tmax (zero delta-t arc) alglib explodes
                    if (tmin != tmax)
                    {
                        for(int j = 0; j < n; j++)
                        {
                            ti[j] = t[j];
                            //Debug.Log(ti[j]);
                        }

                        for(int i = 0; i < 14; i++)
                        {
                            double[] yi = new double[n];
                            for(int j = 0; j < n; j++)
                            {
                                yi[j] = y[j][i];
                            }
                            alglib.spline1dbuildcubic(ti, yi, out interpolant[i]);
                        }
                    } else {
                        ysaved = y[0];
                    }
                }
            }

            public void AddSegment(List<double> t, List<double[]> y, Arc a)
            {
                segments.Add(new Segment(t, y));
                arcs.Add(a);
            }
        }

        public List<Stage> stages = new List<Stage>();
        public double mu;
        public Action<double[], double[]> bcfun;
        public const double g0 = 9.80665;
        public Vector3d r0, v0, r0_bar, v0_bar;
        public Vector3d pv0, pr0;
        public double tgo, tgo_bar, vgo, vgo_bar; // FIXME: tgo + tgo_bar seem a little useless -- vgo + vgo_bar seem completely useless?
        public double g_bar, r_scale, v_scale, t_scale;  /* problem scaling */
        public double dV, dV_bar;  /* guess at dV */

        protected bool fixed_final_time;

        public PontryaginBase(double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV)
        {
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            r0 = rot * r0;
            v0 = rot * v0;
            //Debug.Log("LAN1 = " + LAN(r0, v0) + " r = " + r0 + " v = " + v0);
            pv0 = rot * pv0;
            pr0 = rot * pr0;
            this.r0 = r0;
            this.v0 = v0;
            this.mu = mu;
            this.pv0 = pv0;
            this.pr0 = pr0;
            this.dV = dV;
            double r0m = this.r0.magnitude;
            g_bar = mu / ( r0m * r0m );
            r_scale = r0m;
            v_scale = Math.Sqrt( r0m * g_bar );
            t_scale = Math.Sqrt( r0m / g_bar );
            r0_bar = this.r0 / r_scale;
            v0_bar = this.v0 / v_scale;
            //Debug.Log("LAN1 = " + LAN(r0_bar, v0_bar) + " r = " + r0_bar + " v = " + v0_bar);
            dV_bar = dV / v_scale;
            fixed_final_time = false;
        }

        public void UpdatePosition(Vector3d r0, Vector3d v0, Vector3d lambda, Vector3d lambdaDot, double tgo, double vgo)
        {
            if (thread != null && thread.IsAlive)
                return;

            //Debug.Log("r0m = " + r0.magnitude + " v0m = " + v0.magnitude);

            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            r0 = rot * r0;
            v0 = rot * v0;
            this.r0 = r0;
            this.v0 = v0;
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
            r0_bar = this.r0 / r_scale;
            v0_bar = this.v0 / v_scale;
        }

        public void centralForceThrust(double[] y, double x, double[] dy, object o)
        {
            Arc arc = (Arc)o;
            double At = arc.thrust / ( y[12] * g_bar );
            if (arc.infinite) At = At * 2;
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
            dy[12] = ( arc.thrust == 0 || arc.infinite ) ? 0 : - arc.thrust / arc.c;
            /* accumulated ∆v of the arc */
            dy[13] = At;
        }

        /*
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
        */

        double ckEps = 1e-6;  /* matches Matlab's default 1e-6 ode45 reltol? */

        /* used to update y0 to yf without intermediate values */
        public void singleIntegrate(double[] y0, double[] yf, int n, ref double t, double dt, List<Arc> arcs, ref double dV)
        {
            singleIntegrate(y0: y0, yf: yf, sol: null, n: n, t: ref t, dt: dt, arcs: arcs, count: 2, dV: ref dV);
        }

        /* used to pull intermediate values off to do chebyshev polynomial interpolation */
        public void singleIntegrate(double[] y0, Solution sol, int n, ref double t, double dt, List<Arc> arcs, int count, ref double dV)
        {
            singleIntegrate(y0: y0, yf: null, sol: sol, n: n, t: ref t, dt: dt, arcs: arcs, count: count, dV: ref dV);
        }

        public void singleIntegrate(double[] y0, double[] yf, Solution sol, int n, ref double t, double dt, List<Arc> arcs, int count, ref double dV)
        {
            Arc e = arcs[n];

            if ( count < 2)
                count = 2;

            /*
            // negative burns or coasts don't work with alglib, need to replace the rkf45 implementation
            if ( dt < 0 )
                dt = 0;

            // zero time segments also don't work with alglib either
            if ( dt < 1e-16 && yf != null)
            {
                Array.Copy(y0, arcIndex(arcs,n), yf, 13*n, 13);
                return;
            }

            if ( dt < 1e-16 )
                throw new Exception("dt is zero");
                */

            double[] y = new double[14];
            Array.Copy(y0, arcIndex(arcs,n), y, 0, 13);
            y[13] = dV;


            /* time to burn the entire stage */
            double tau = e.isp * g0 * y[12] / e.thrust / t_scale;

            /* clip dt at 99.9% of tau */
            if ( dt > 0.999 * tau && !e.infinite )
                dt = 0.999 * tau;

            // XXX: remove this hack
            bool allvals;
            if (sol != null)
                allvals = true;
            else
                allvals = false;
            //count = 2;

            double[] x = new double[count];

            for(int i = 0; i < count; i++)
                x[i] = t + dt * i / (count - 1 );

            /*
            // Chebyshev sampling
            for(int k = 1; k < (count-1); k++)
            {
                int l = count - 2;
                x[k] = t + 0.5 * dt  + 0.5 * dt * Math.Cos(Math.PI*(2*(l-k)+1)/(2*l));
            }

            // But also get the endpoints exactly
            x[0] = t;
            x[count-1] = t + dt;
            */

            ODE ode = new ODE(y, 14, x, ckEps, 0, hmin: 1e-6, allvals: allvals);
            ode.RKF45(centralForceThrust, e);

            t = t + dt;
            e.dV = ( ode.ytbl[13][count-1] - dV ) * v_scale;
            dV = ode.ytbl[13][count-1];

            if (sol != null)
            {
                //Debug.Log("--------------");
                //for(int i = 0; i < count; i++)
                //    Debug.Log(x[i]);
                //Debug.Log("ylist.Count = " + ode.ylist.Count);
                sol.AddSegment(ode.xlist, ode.ylist, e);
            }

            if (yf != null)
            {
                for(int i = 0; i < 13; i++)
                {
                    int j = 13*n+i;
                    yf[j] = ode.ytbl[i][1];
                }
            }
        }

        // normal integration with no midpoints
        public void multipleIntegrate(double[] y0, double[] yf, List<Arc> arcs, bool initialize = false)
        {
            multipleIntegrate(y0, yf, null, arcs, initialize: initialize);
        }

        // for generating the interpolated chebyshev solution
        public void multipleIntegrate(double[] y0, Solution sol, List<Arc> arcs, int count)
        {
            multipleIntegrate(y0, null, sol, arcs, count: count);
            //Debug.Log("DONE: rf = " + sol.rf().xzy.magnitude + "; vf = " + sol.vf().xzy.magnitude + " tmin = " + sol.tmin() + " tmax = " + sol.tmax() + " v_barf = " + sol.v_bar(sol.tmax()).xzy.magnitude + "; r_barf = " + sol.r_bar(sol.tmax()).xzy.magnitude + " vf2 = " + sol.v_bar(sol.tmax()).xzy.magnitude * v_scale + "; rf2 = " + sol.r_bar(sol.tmax()).xzy.magnitude * r_scale );
        }

        // copy the nth
        private void copy_yf_to_y0(double[] y0, double[] yf, int n, List<Arc> arcs)
        {
            int yf_index = 13 * n;
            int y0_index = arcIndex(arcs, n+1);
            Array.Copy(yf, yf_index, y0, y0_index, 13);
            double m0 = arcs[n+1].m0;
            if (m0 > 0)
                y0[y0_index+12] = m0;
        }

        const double MAX_COAST_TAU = 2;

        private void multipleIntegrate(double[] y0, double[] yf, Solution sol, List<Arc> arcs, int count = 2, bool initialize = false)
        {
            double t = 0;

            double tgo = y0[0];
            double dV = 0;

            for(int i = 0; i < arcs.Count; i++)
            {
                if (arcs[i].thrust == 0)
                {
                    double coast_time;

                    if (arcs[i].use_fixed_time)
                        coast_time = arcs[i].fixed_tbar - t;
                    else
                        coast_time = y0[arcIndex(arcs, i, parameters: true)];

                    //if (sol != null)
                    //    Debug.Log("coast_time = " + coast_time + " t = " + t);

                    if (yf != null) {
                        // normal integration with no midpoints
                        singleIntegrate(y0, yf, i, ref t, coast_time, arcs, ref dV);
                        if (initialize && i < (arcs.Count - 1))
                            copy_yf_to_y0(y0, yf, i, arcs);
                    } else {
                        // for generating the interpolated chebyshev solution
                        singleIntegrate(y0, sol, i, ref t, coast_time, arcs, count, ref dV);
                    }
                    arcs[i].complete_burn = false;
                }
                else
                {
                    if ( (tgo <= arcs[i].max_bt_bar) || (i == arcs.Count-1) ) // FIXME?: we're still allowing overburning here
                    {
                        if (yf != null) {
                            // normal integration with no midpoints
                            singleIntegrate(y0, yf, i, ref t, tgo, arcs, ref dV);
                            if (initialize && i < (arcs.Count - 1))
                                copy_yf_to_y0(y0, yf, i, arcs);
                        } else {
                            // for generating the interpolated chebyshev solution
                            singleIntegrate(y0, sol, i, ref t, tgo, arcs, count, ref dV);
                        }
                        arcs[i].complete_burn = false;
                        tgo = 0;
                    }
                    else
                    {
                        if (yf != null) {
                            // normal integration with no midpoints
                            singleIntegrate(y0, yf, i, ref t, arcs[i].max_bt_bar, arcs, ref dV);
                            if (initialize && i < (arcs.Count - 1))
                                copy_yf_to_y0(y0, yf, i, arcs);
                        } else {
                            // for generating the interpolated chebyshev solution
                            singleIntegrate(y0, sol, i, ref t, arcs[i].max_bt_bar, arcs, count, ref dV);
                        }
                        arcs[i].complete_burn = true;
                        tgo -= arcs[i].max_bt_bar;
                    }
                }
            }
        }

        /*
         * 2 parameters at the start for total burntime, each coast has 2 parameters, each burn has 0 parameters
         *
         * 0 - total burntime
         * 1 - slack variable for burntime
         * 2-14 - y0 for burn0
         * 15 - coast time
         * 16 - slack for coast time
         * 17-29 - y0 for coast0
         * 30-42 - y0 for burn0
         */
        public int arcIndex(List<Arc> arcs, int n, bool parameters = false)
        {
            int index = 2;
            for(int i=0; i<n; i++)
            {
                if (arcs[i].thrust == 0)
                {
                    if (arcs[i].use_fixed_time)
                        index += 13;
                    else
                        index += 15;
                }
                else
                {
                    index += 13;
                }
            }
            // by default this gives the offset to the continuity variables (the common case)
            // set parameters to true to bypass adding that offset and get the index to the parameters instead
            if (!parameters && n != arcs.Count)
            {
                if (arcs[n].thrust == 0)
                {
                    if (!arcs[n].use_fixed_time)
                    {
                        index += 2;
                    }
                }
            }

            return index;
        }

        public double[] yf;

        public void optimizationFunction(double[] y0, double[] z, object o)
        {
            List<Arc> arcs = (List<Arc>)o;
            yf = new double[arcs.Count*13];  /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
            multipleIntegrate(y0, yf, arcs);

            /* initial conditions */
            z[0] = y0[arcIndex(arcs,0)+0] - r0_bar[0];
            z[1] = y0[arcIndex(arcs,0)+1] - r0_bar[1];
            z[2] = y0[arcIndex(arcs,0)+2] - r0_bar[2];
            z[3] = y0[arcIndex(arcs,0)+3] - v0_bar[0];
            z[4] = y0[arcIndex(arcs,0)+4] - v0_bar[1];
            z[5] = y0[arcIndex(arcs,0)+5] - v0_bar[2];
            z[6] = y0[arcIndex(arcs,0)+12] - arcs[0].m0;

            /* terminal constraints */
            double[] yT = new double[13];
            Array.Copy(yf, (arcs.Count-1)*13, yT, 0, 13);
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
            for(int i = 1; i < arcs.Count; i++)
            {
                for(int j = 0; j < 13; j++)
                {
                    if ( j == 12 )
                    {
                        if (arcs[i].m0 < 0) // negative mass => continuity rather than mass jettison
                        {
                            /* continuity */
                            z[j+13*i] = y0[j+arcIndex(arcs,i)] - yf[j+13*(i-1)];
                        }
                        else
                        {
                            /* mass jettison */
                            z[j+13*i] = y0[j+arcIndex(arcs,i)] - arcs[i].m0;
                        }
                    }
                    else
                    {
                        z[j+13*i] = y0[j+arcIndex(arcs,i)] - yf[j+13*(i-1)];
                    }
                }
            }

            /* magnitude of terminal costate vector = 1.0 (dummy constraint for H(tf)=0 to optimize burntime because BC is keplerian) */
            int n = 13 * arcs.Count;

            // FIXME: this isn't fixed_final_time, it is optimized terminal burn to a fixed final time coast.
            if (fixed_final_time)
            {
                int i = 0;

                // find the last burn arc (uh, this has to be arcs.Count - 1 right?)
                for(int j = 0; j < arcs.Count; j++)
                {
                    if (arcs[j].thrust > 0)
                        i = j;
                }

                Vector3d r2 = new Vector3d(yf[i*13+0], yf[i*13+1], yf[i*13+2]);
                Vector3d v2 = new Vector3d(yf[i*13+3], yf[i*13+4], yf[i*13+5]);
                Vector3d pv2 = new Vector3d(yf[i*13+6], yf[i*13+7], yf[i*13+8]);
                Vector3d pr2 = new Vector3d(yf[i*13+9], yf[i*13+10], yf[i*13+11]);
                double r2m = r2.magnitude;

                /* H0 at the end of the final burn = 0 */
                double H0t2 = Vector3d.Dot(pr2, v2) - Vector3d.Dot(pv2, r2) / (r2m * r2m * r2m);
                z[n] = H0t2;
            }
            else
            {
                z[n] = 0.0;
                for(int i = 0; i < 6; i++)
                    z[n] = z[n] + yf[i+6+13*(arcs.Count-1)] * yf[i+6+13*(arcs.Count-1)];
                z[n] = Math.Sqrt(z[n]) - 1.0;
            }
            n++;

            // positive arc time constraint
            z[n] = y0[1];
            // z[n] = ( y0[0] < 0 ) ? y0[0] - y0[1] * y0[1] : y0[1];
            n++;

            double total_bt_bar = 0;
            for(int i = 0; i < arcs.Count; i++)
            {
                if ( arcs[i].thrust == 0 && !arcs[i].use_fixed_time )
                {
                    int index = arcIndex(arcs, i, parameters: true);
                    if (total_bt_bar > y0[0] || (i == arcs.Count -1))
                    {
                        if (arcs[i].coast_after_jettison)
                        {
                            Debug.Log("in setting coasts to zero");
                            z[n] = y0[index];
                            n++;
                            z[n] = y0[index+1];
                            n++;
                        }
                        else
                        {
                            z[n] = y0[index];
                            n++;
                            z[n] = y0[index+1];
                            n++;
                        }
                    }
                    else
                    {
                        Vector3d r = new Vector3d(y0[index+2], y0[index+3], y0[index+4]);
                        Vector3d v = new Vector3d(y0[index+5], y0[index+6], y0[index+7]);
                        Vector3d pv = new Vector3d(y0[index+8], y0[index+9], y0[index+10]);
                        Vector3d pr = new Vector3d(y0[index+11], y0[index+12], y0[index+13]);
                        double rm = r.magnitude;

                        Vector3d r2 = new Vector3d(yf[i*13+0], yf[i*13+1], yf[i*13+2]);
                        Vector3d v2 = new Vector3d(yf[i*13+3], yf[i*13+4], yf[i*13+5]);
                        Vector3d pv2 = new Vector3d(yf[i*13+6], yf[i*13+7], yf[i*13+8]);
                        Vector3d pr2 = new Vector3d(yf[i*13+9], yf[i*13+10], yf[i*13+11]);
                        double r2m = r2.magnitude;

                        double H0t1 = Vector3d.Dot(pr, v) - Vector3d.Dot(pv, r) / (rm * rm * rm);
                        double H0t2 = Vector3d.Dot(pr2, v2) - Vector3d.Dot(pv2, r2) / (r2m * r2m * r2m);
                        if (arcs[i].coast_after_jettison)
                        {
                            Debug.Log("in normal coast");
                            //z[n] = y0[index];
                            z[n] = ( 1.0 - 1.0 / (y0[index]/1.0e-4 + 1.0) ) * H0t2 * ( 1.0 + 1.0 / ( ( y0[index] - 2.0 ) / 1e-4 - 1.0 ) );
                            /*
                            if (y0[index] <= 1e-15 || y0[index] >= 2 - 1e-15)
                                z[n] = 0;
                            else
                                z[n] = H0t2;
                                */

                            z[n] = H0t2;

                            //z[n] = y0[index];
                            // z[n] = pv2.magnitude - pv.magnitude;
                        }
                        else
                        {
                            /* H0 at the end of the coast = 0 */
                            z[n] = H0t2;
                        }

                        /*
                        if (i == 0 || i == arcs.Count - 1)
                        {
                            // first or last coast
                            z[n] = Vector3d.Dot(pr2, v2) - Vector3d.Dot(pv2, r2) / (r2m * r2m * r2m);
                        }
                        else
                        {
                            // interior coasts
                            z[n] = pv2.magnitude - pv.magnitude;
                        }
                        */

                        n++;

                        if (arcs[i].coast_after_jettison)
                        {
                            //z[n] = ( y0[index] < 0 ) ? y0[index] - y0[index+1] * y0[index+1] : y0[index+1] * y0[index+1];
                            //z[n] = y0[index] - Math.Abs(y0[index+1]); // * y0[index+1];
                            z[n] = y0[index+1];
                            //n++;
                            //z[n] = ( y0[index] > 2 ) ? y0[index] - 2 + y0[index+2] * y0[index+2] : y0[index+2] * y0[index+2];
                            //z[n] = y0[index] - 2 + Math.Abs(y0[index+2]); // * y0[index+2];
                            //z[n] = y0[index+2];
                        }
                        else
                        {
                            //if ( y0[index] < 0 )
                            //    z[n-1] = 0;
                            z[n] = y0[index+1];
                            // z[n] = y0[index] - y0[index+1] * y0[index+1];
                        }
                        n++;
                    }
                }
                // sum up burntime of burn arcs
                if ( arcs[i].thrust > 0 )
                    total_bt_bar += arcs[i].max_bt_bar;
            }

            /* construct sum of the squares of the residuals for levenberg marquardt */
            /*
            for(int i = 0; i < z.Length; i++)
                //z[i] = z[i] * z[i];
                z[i] = Math.Abs(z[i]);
                */
        }

        // NOTE TO SELF:  STOP FUCKING WITH THESE NUMBERS
        double lmEpsx = 0; // 1e-8;  // going to 1e-10 seems to cause the optimizer to flail with 1e-15 or less differences produced in the zero value
                               // 1e-8 produces plenty accurate numbers, unless the transversality conditions are broken in some way
        int lmIter = 0; // 20000;    // 20,000 does seem roughly appropriate -- 12,000 has been necessary to find some solutions
        double lmDiffStep = 1e-6; // 1e-8; // this matching lmEspx probably makes sense

        public bool runOptimizer(List<Arc> arcs)
        {
            Debug.Log("arcs in runOptimizer:");
            for(int i = 0; i < arcs.Count; i++)
               Debug.Log(arcs[i]);

            for(int i = 0; i < y0.Length; i++)
                Debug.Log("runOptimizer before - y0[" + i + "] = " + y0[i]);

            double[] z = new double[arcIndex(arcs,arcs.Count)];
            optimizationFunction(y0, z, arcs);

            double znorm = 0.0;

            for(int i = 0; i < z.Length; i++)
            {
                znorm += z[i] * z[i];
                Debug.Log("zbefore[" + i + "] = " + z[i]);
            }

            znorm = Math.Sqrt(znorm);
            //Debug.Log("znorm = " + znorm);

            double[] bndl = new double[arcIndex(arcs,arcs.Count)];
            double[] bndu = new double[arcIndex(arcs,arcs.Count)];
            for(int i = 0; i < bndl.Length; i++)
            {
                bndl[i] = Double.NegativeInfinity;
                bndu[i] = Double.PositiveInfinity;
            }

            bndl[0] = 0;

            for(int i = 0; i < arcs.Count; i++)
            {
                if (arcs[i].coast_after_jettison)
                {
                    int j = arcIndex(arcs, i, parameters: true);
                    bndl[j] = 0.0;
                    bndu[j] = 2.0;
                }
            }

            alglib.minlmstate state;
            alglib.minlmreport rep = new alglib.minlmreport();
            alglib.minlmcreatev(y0.Length, y0, lmDiffStep, out state);  /* y0.Length must == z.Length returned by the BC function for square problems */
            alglib.minlmsetbc(state, bndl, bndu);
            alglib.minlmsetcond(state, lmEpsx, lmIter);
            //Debug.Log("about to minlmoptmize");
            alglib.minlmoptimize(state, optimizationFunction, null, arcs);
            //Debug.Log("minlmoptimize done");

            double[] y0_new = new double[y0.Length];
            alglib.minlmresultsbuf(state, ref y0_new, rep);
            //Debug.Log("MechJeb minlmoptimize termination code: " + rep.terminationtype);
            //Debug.Log("MechJeb minlmoptimize iterations: " + rep.iterationscount);

            optimizationFunction(y0_new, z, arcs);

            znorm = 0.0;
            double max_z = 0.0;

            for(int i = 0; i < z.Length; i++)
            {
                if (z[i] > max_z)
                    max_z = z[i];
                znorm += z[i] * z[i];
                Debug.Log("z[" + i + "] = " + z[i]);
            }

            znorm = Math.Sqrt(znorm);
            Debug.Log("znorm = " + znorm);

            // this comes first because after max-iterations we may still have an acceptable solution
            if (max_z < 1e-5)
            {
                y0 = y0_new;
                return true;
            }

            // lol
            if ( (rep.terminationtype != 2) && (rep.terminationtype != 7) )
                return false;

            return false;
        }

        public double[] y0;

        public void UpdateY0(List<Arc> arcs)
        {
            /* FIXME: some of the round tripping here is silly */
            Stage s = stages[0];  // FIXME: shouldn't we be using the arcs.stages?
            int arcindex = arcIndex(arcs,0);
            y0[arcindex]   = r0_bar[0];  // FIXME: shouldn't we pull all this off of the current solution?
            y0[arcindex+1] = r0_bar[1];
            y0[arcindex+2] = r0_bar[2];
            y0[arcindex+3] = v0_bar[0];
            y0[arcindex+4] = v0_bar[1];
            y0[arcindex+5] = v0_bar[2];
            y0[arcindex+6] = pv0[0];
            y0[arcindex+7] = pv0[1];
            y0[arcindex+8] = pv0[2];
            y0[arcindex+9] = pr0[0];
            y0[arcindex+10] = pr0[1];
            y0[arcindex+11] = pr0[2];

            for(int i = 0; i < arcs.Count; i++)
            {
                arcindex = arcIndex(arcs,i);
                if (arcs[i].m0 > 0)  // don't update guess if we're not staging (optimizer has to find the terminal mass of prior stage)
                    y0[arcindex+12] = arcs[i].m0;  // update all stages for the m0 in stage stats (boiloff may update upper stages)
            }

            if (solution != null)
            {
                //Debug.Log("tburn_bar = " + solution.tburn_bar(0));
                y0[0] = solution.tburn_bar(0); // FIXME: need to pass actual t0 here
            }
            else
            {
                y0[0] = tgo_bar;
            }
            y0[1] = 0;
        }

        /* insert coast before the ith stage */
        protected void InsertCoast(List<Arc> arcs, int i, Solution sol)
        {
            int bottom = arcIndex(arcs, i, parameters: true);

            if ( arcs[i].thrust == 0 )
                throw new Exception("adding a coast before a coast");

            arcs.Insert(i, new Arc(new Stage(this, m0: arcs[i].m0, isp: 0, thrust: 0, ksp_stage: arcs[i].ksp_stage), coast_after_jettison: true));
            double[] y0_new = new double[arcIndex(arcs, arcs.Count)];

            double tmin = sol.segments[i].tmin;
            double dt = sol.segments[i].tmax - tmin;

            // copy all the lower arcs
            Array.Copy(y0, 0, y0_new, 0, bottom);
            // initialize the coast
            //y0_new[bottom] = Math.Asin(MuUtils.Clamp(0.5 / MAX_COAST_TAU - 1, -1, 1));
            y0_new[bottom] = dt/2.0;
            y0_new[bottom+1] = 0.0;
            // keep the burntime of the upper stage constant
            y0_new[0] = y0[0]; // - dt/2.0;

            // FIXME: copy the rest of the parameters for upper stage coasts
            y0 = y0_new;
            yf = new double[arcs.Count*13];  /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
            multipleIntegrate(y0, yf, arcs, initialize: true);
        }

        /* tweak coast at the ith stage, inserting burntime of the ith stage / 2 */
        protected void RetryCoast(List<Arc> arcs, int i, Solution sol)
        {
            int bottom = arcIndex(arcs, i, parameters: true);

            if ( arcs[i].thrust != 0 )
                throw new Exception("trying to update a non-coasting coast");

            double tmin = sol.segments[i+1].tmin;
            double dt = sol.segments[i+1].tmax - tmin;

            y0[bottom] = dt/2.0;
            y0[bottom+1] = 0.0;

            multipleIntegrate(y0, yf, arcs, initialize: true);
        }

        /* remove an arc from the solution */
        protected void RemoveArc(List<Arc> arcs, int i, Solution sol)
        {
            int bottom = arcIndex(arcs, i, parameters: true);
            int top = arcIndex(arcs, i+1, parameters: true);

            arcs.RemoveAt(i);

            double[] y0_new = new double[arcIndex(arcs, arcs.Count)];
            // copy all the lower arcs
            Array.Copy(y0, 0, y0_new, 0, bottom);
            // copy all the upper arcs
            Array.Copy(y0, top, y0_new, bottom, y0_new.Length - bottom);

            y0 = y0_new;
            yf = new double[arcs.Count*13];  /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
            multipleIntegrate(y0, yf, arcs, initialize: true);
        }

        public abstract void Bootstrap(double t0);

        public virtual void Optimize(double t0)
        {
            List<Arc> last_arcs = null;

            if (solution != null && y0 != null)
            {
                last_arcs = new List<Arc>();
                for(int i = 0; i < solution.arcs.Count; i++)
                    last_arcs.Add(solution.arcs[i]);  // shallow copy
            }

            try {
                //Debug.Log("starting optimize");
                if (stages != null)
                {
                    //Debug.Log("stages: ");
                    //for(int i = 0; i < stages.Count; i++)
                    //    Debug.Log(stages[i]);
                }
                if (last_arcs != null)
                {
                    // since we shift the zero of tbar forwards on every re-optimize we have to do this here
                    for(int i = 0; i < last_arcs.Count; i++)
                    {
                        if ( last_arcs[i].use_fixed_time )
                        {
                            last_arcs[i].fixed_tbar = ( last_arcs[i].fixed_time - t0 ) / t_scale;
                        }
                    }
                    //Debug.Log("arcs: ");
                    //for(int i = 0; i < last_arcs.Count; i++)
                    //    Debug.Log(last_arcs[i]);
                }

                if (y0 == null)
                {
                    Bootstrap(t0);
                }
                else
                {
                    while(last_arcs[0].done)
                    {
                        // FIXME: if we fail, then we'll re-shrink y0 again and again
                        //Debug.Log("shrinking y0 array");
                        double[] y0_old = y0;
                        y0 = new double[arcIndex(last_arcs, last_arcs.Count-1)];
                        // copy the 2 burntime parameters
                        Array.Copy(y0_old, 0, y0, 0, 2);
                        // copy the upper N-1 arcs
                        int start = arcIndex(last_arcs, 1, parameters: true);
                        int end = y0_old.Length;
                        Array.Copy(y0_old, start, y0, 2, end - start);
                        last_arcs.RemoveAt(0);

                        if (last_arcs[0].thrust == 0)
                            last_arcs[0].coast_after_jettison = false;  /* we can't be after a jettison if we're first */
                    }

                    if (last_arcs.Count == 0)
                    {
                        // something in staging went nuts, so re-bootstrap
                        y0 = null;
                        Bootstrap(t0);
                        return;
                    }

                    // fix up coast stage mass for boiloff
                    for(int i = 0; i < last_arcs.Count; i++)
                    {
                        if (last_arcs[i].thrust == 0 && last_arcs[i].m0 > 0 && i < last_arcs.Count - 1)
                            last_arcs[i].stage.m0 = last_arcs[i+1].stage.m0;
                        //FIXME: what about mass continuity for final coasts?
                    }


                    UpdateY0(last_arcs);
                    //Debug.Log("normal optimizer run start");

                    if ( !runOptimizer(last_arcs) )
                    {
                        //Debug.Log("optimizer failed3");
                        y0 = null;
                        return;
                    }

                    Solution sol = new Solution(t_scale, v_scale, r_scale, t0);

                    //for(int i = 0; i < y0.Length; i++)
                    //    Debug.Log("y0[" + i + "] = " + y0[i]);

                    multipleIntegrate(y0, sol, last_arcs, 10);

                    this.solution = sol;

                    yf = new double[last_arcs.Count*13];  /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
                    multipleIntegrate(y0, yf, last_arcs);

                    //for(int i = 0; i < yf.Length; i++)
                    //    Debug.Log("yf[" + i + "] = " + yf[i]);

                    //Debug.Log("r_scale = " + r_scale + " v_scale = " + v_scale);


                    //Debug.Log("Optimize done");

                }
            }
            catch (Exception e)
            {
                //Debug.Log(e);
            }
        }

        public Solution solution;

        private Thread thread;

        /* very simple stage tracking for now */
        public virtual void SynchStages(List<int> kspstages, FuelFlowSimulation.Stats[] vacStats, double vesselMass, double currentThrust, double time)
        {
            if (thread != null && thread.IsAlive)
                return;

            bool first_thrusting = true;

            if (stages == null)
                stages = new List<Stage>();

            if (stages.Count > kspstages.Count)
            {
                for(int i = 0; i < (stages.Count - kspstages.Count); i++)
                {
                    //Debug.Log("shrinking stage list by one");
                    stages[0].staged = true;
                    stages.RemoveAt(0);
                }
            }

            int offset = ( stages.Count > kspstages.Count ) ? stages.Count - kspstages.Count : 0;

            for(int stage_index = 0 ; stage_index < kspstages.Count; stage_index++)
            {
                int ksp_stage = kspstages[stage_index];

                double m0 = vacStats[ksp_stage].startMass * 1000;
                double isp = vacStats[ksp_stage].isp;
                double thrust = vacStats[ksp_stage].startThrust * 1000;

                if ( first_thrusting && solution != null && solution.tgo(time) < 10 )
                {
                    //if (Math.Abs(vesselMass * 1000 - m0) > m0 * 0.01)
                        //Debug.Log("MechJeb: mass of current stage is off by > 1%, precision of the burn will be off, fix the Delta-V display (or you're on RCS and this is normal).");

                    //if (Math.Abs(currentThrust * 1000 - thrust) > thrust *0.01)
                        //Debug.Log("MechJeb: thrust of current stage is off by > 1%, precision of the burn will be off, fix the Delta-V display (or you're on RCS and this is normal).");

                //    m0 = vesselMass * 1000;
                //    thrust = currentThrust * 1000;
                }

                double max_bt = vacStats[ksp_stage].deltaTime;

                if (stage_index >= stages.Count)
                {
                    //Debug.Log("adding a new found stage");
                    stages.Add(new Stage(this, m0: m0, isp: isp, thrust: thrust, max_bt: max_bt, ksp_stage: ksp_stage));
                }
                else
                {
                    stages[stage_index+offset].UpdateStage(this, m0: m0, isp: isp, thrust: thrust, max_bt: max_bt, ksp_stage: ksp_stage);
                }

                first_thrusting = false;
            }
        }

        public void forceBootstrap()
        {
            KillThread();
            y0 = null;
        }

        public bool threadStart(double t0)
        {
            if (thread != null && thread.IsAlive)
            {
                return false;
            }
            else
            {
                if (thread != null)
                {
                    thread.Abort();
                }

                thread = new Thread(() => Optimize(t0));
                thread.Start();
                return true;
            }
        }

        public void KillThread()
        {
            if (thread != null)
                thread.Abort();
        }
    }
}
