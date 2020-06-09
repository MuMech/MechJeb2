using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public abstract class PontryaginBase {
        //const double MAX_COAST_TAU = 1.5;

        public class Arc
        {
            private PontryaginBase p;
            private MechJebModuleLogicalStageTracking.Stage stage;
            public double dV;  /* actual integrated time of the burn */

            // values copied from the stage (important to copy them since the stage values will change
            // in the main thread).

            double _isp;
            public double isp { get { return _isp; } }
            double _thrust;
            public double thrust { get { return _thrust; } }
            double _m0;
            public double m0 { get { return _m0; } }
            double _avail_dV;
            public double avail_dV { get { return _avail_dV; } }
            double _max_bt;
            public double max_bt { get { return _max_bt; } }
            double _c;
            public double c { get { return _c; } }
            public double max_bt_bar { get { return max_bt / p.t_scale; } }
            int _ksp_stage;
            public int ksp_stage { get { return _ksp_stage; } }
            int _rocket_stage;
            public int rocket_stage { get { return _rocket_stage; } }
            public double _synch_time;

            public bool complete_burn = false;
            public bool _done = false;
            public bool done { get { return (stage != null && stage.staged) || _done; } set { _done = value; } }
            public bool coast = false;
            public bool coast_after_jettison = false;
            public bool use_fixed_time = false; // confusingly, this is fixed end-time for terminal coasts to rendezvous
            public bool use_fixed_time2 = false; // this is a fixed time segment, but the constant appears in the y0 vector with a trivial constraint
            public double fixed_time = 0;
            public double fixed_tbar = 0;

            // zero mdot, infinite burntime+isp
            public bool infinite = false;

            public override string ToString()
            {
                return "ksp_stage:"+ ksp_stage + " rocket_stage:" + rocket_stage + " isp:" + isp + " thrust:" + thrust + " c:" + c + " m0:" + m0 + " maxt:" + max_bt + " maxtbar:" + max_bt_bar + " avail ∆v:" + avail_dV + " used ∆v:" + dV + ( done ? " (done)" : "" ) + ( infinite ? " (infinite) " : "" );
            }

            // create a local copy of the information for the optimizer
            public void UpdateStageInfo(double t0)
            {
                if (stage != null)
                {
                    _isp = stage.isp;
                    _thrust = stage.startThrust;
                    _m0 = stage.startMass;
                    _avail_dV = stage.deltaV;
                    _max_bt = stage.deltaTime;
                    _c = stage.v_e / p.t_scale;
                    _ksp_stage = stage.ksp_stage;
                    _rocket_stage = stage.rocket_stage;
                }
                else
                {
                    _isp = 0;
                    _thrust = 0;
                    _m0 = -1;
                    _avail_dV = 0;
                    _max_bt = 0;
                    _c = 0;
                    _ksp_stage = -1;
                    _rocket_stage = -1;
                }
                _synch_time = t0;
            }

            public Arc(PontryaginBase p, double t0, MechJebModuleLogicalStageTracking.Stage stage = null, bool done = false, bool coast_after_jettison = false, bool use_fixed_time = false, double fixed_time = 0, bool coast = false)
            {
                this.p = p;
                this.stage = stage;
                this.done = done;
                this.coast_after_jettison = coast_after_jettison;
                this.use_fixed_time = use_fixed_time;
                this.fixed_time = fixed_time;
                this.coast = coast;
                UpdateStageInfo(t0);
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

            // this is the tgo of the "booster" stage, this is deliberately allowed to go negative if
            // we're staging so "current" may be a misnomer.
            //
            public double current_tgo(double t)
            {
                double tbar = ( t - t0 ) / t_scale;
                return ( segments[0].tmax - tbar ) * t_scale;
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
                    if (arcs[i].coast)
                        continue;
                    tburn += tgo_bar(tbar, i);
                }
                return tburn;
            }

            public String ArcString(double t, int n)
            {
                Arc arc = arcs[n];
                if (arc.ksp_stage < 0)
                {
                    return String.Format("coast: {0:F1}s", tgo(t, n));
                }
                else
                {
                    return String.Format("burn {0}: {1:F1}s {2:F1}m/s ({3:F1})", arc.ksp_stage, tgo(t, n), dV(t, n), arc.avail_dV - dV(arc._synch_time, n));
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
                if ( arcs[last].coast )
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

            public Arc last_arc()
            {
                return arcs[arcs.Count-1];
            }

            public Arc terminal_burn_arc()
            {
                for(int k = arcs.Count-1; k >= 0; k--)
                {
                    if ( arcs[k].thrust > 0 )
                        return arcs[k];
                }
                return arcs[0];
            }

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

            // Synch stats from LogicalStageTracking controller
            public void UpdateStageInfo(double t0)
            {
                for(int i = 0; i < arcs.Count; i++)
                    arcs[i].UpdateStageInfo(t0);
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
                    double[][] yi = new double[14][];

                    tmin = t[0];
                    tmax = t[n-1];

                    // if tmin == tmax (zero delta-t arc) alglib explodes
                    if (tmin != tmax)
                    {
                        for(int i = 0; i < 14; i++)
                            yi[i] = new double[n];

                        int j2 = 0;

                        for(int j = 0; j < n; j++)
                        {
                            if (j > 1 && Math.Abs(t[j] - ti[j2-1]) < 1e-15)
                                continue;

                            ti[j2] = t[j];
                            for(int i = 0; i < 14; i++)
                            {
                                yi[i][j2] = y[j][i];
                            }

                            j2++;
                        }

                        for(int i = 0; i < 14; i++)
                          alglib.spline1dbuildcubic(ti, yi[i], j2, 0, 0, 0, 0, out interpolant[i]);

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

        // metrics
        public int successful_converges = 0;
        public int max_lm_iteration_count = 0;
        public int last_lm_iteration_count = 0;
        public int last_lm_status = 0;
        public double last_znorm = 0;
        public String last_failure_cause = null;
        public double last_success_time = 0;

        protected List<MechJebModuleLogicalStageTracking.Stage> stages { get { return core.stageTracking.stages; } }
        public double mu;
        public Action<double[], double[], bool> bcfun;
        public const double g0 = 9.80665;
        public Vector3d r0, v0, r0_bar, v0_bar;
        public Vector3d pv0, pr0;
        public double tgo, tgo_bar, vgo, vgo_bar; // FIXME: tgo + tgo_bar seem a little useless -- vgo + vgo_bar seem completely useless?
        public double g_bar, r_scale, v_scale, t_scale;  /* problem scaling */
        public double dV, dV_bar;  /* guess at dV */
        protected bool fixed_final_time;
        protected MechJebCore core;

        public PontryaginBase(MechJebCore core, double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV)
        {
            this.core = core;
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            r0 = rot * r0;
            v0 = rot * v0;
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

            dV_bar = dV / v_scale;
            fixed_final_time = false;
        }

        public void UpdatePosition(Vector3d r0, Vector3d v0, Vector3d lambda, Vector3d lambdaDot, double tgo, double vgo)
        {
            // this is safe (and must be kept safe) to hammer on every tick and to not update
            // the current position while a guidance solution thread is currently running.
            if (thread != null && thread.IsAlive)
                return;

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

        public void centralForceThrust(Span<double> y, double x, Span<double> dy, int n, object o)
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

        double ckEps = 1e-6;  /* matches Matlab's default 1e-6 ode45 reltol? */

        /* used to update y0 to yf without intermediate values */
        public void singleIntegrate(double[] y0, double[] yf, int n, ref double t, double dt, List<Arc> arcs, ref double dV)
        {
            singleIntegrate(y0: y0, yf: yf, sol: null, n: n, t: ref t, dt: dt, arcs: arcs, count: 2, dV: ref dV);
        }

        /* used to pull intermediate values off to do cubic spline interpolation */
        public void singleIntegrate(double[] y0, Solution sol, int n, ref double t, double dt, List<Arc> arcs, int count, ref double dV)
        {
            singleIntegrate(y0: y0, yf: null, sol: sol, n: n, t: ref t, dt: dt, arcs: arcs, count: count, dV: ref dV);
        }

        public void singleIntegrate(double[] y0, double[] yf, Solution sol, int n, ref double t, double dt, List<Arc> arcs, int count, ref double dV)
        {
            Arc e = arcs[n];

            // gotta have at least a start and an end
            if ( count < 2)
                count = 2;

            double[] yi = new double[14];
            Array.Copy(y0, arcIndex(arcs,n), yi, 0, 13);
            yi[13] = dV;

            // fix the starting point to being r0_bar, v0_bar
            if ( n == 0 )
            {
                yi[0] = r0_bar[0];
                yi[1] = r0_bar[1];
                yi[2] = r0_bar[2];
                yi[3] = v0_bar[0];
                yi[4] = v0_bar[1];
                yi[5] = v0_bar[2];
            }

            /*
            if ( e.thrust > 0 )
            {
                // time to burn the entire stage
                double tau = e.isp * g0 * y[12] / e.thrust / t_scale;

                if ( dt > tau )
                    Debug.Log("TAU EXCEEEDED!");
                // clip dt at 99.9% of tau
//                if ( dt > 0.999 * tau && !e.infinite )
//                    dt = 0.999 * tau;
            }
            */

            Span<double> xtbl = stackalloc double[count];
            for(int i = 0; i < count; i++)
                xtbl[i] = t + dt * i / (count - 1 );

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

            List<double> xlist = null;
            List<double []> ylist = null;

            if (sol != null)
            {
                ylist = new List<double []>();
                xlist = new List<double>();
            }

            // FIXME: remove this allocation by using a jagged span
            double[][] ytbl = new double[14][];
            for(int i = 0; i < 14; i++)
                ytbl[i] = new double[count];

            ODE.RKDP547FM(centralForceThrust, e, yi, 14, xtbl, ytbl, ckEps, 0, hmin: 1e-15, xlist: xlist, ylist: ylist);

            t = t + dt;
            e.dV = ( ytbl[13][count-1] - dV ) * v_scale;
            dV = ytbl[13][count-1];

            if (sol != null)
            {
                sol.AddSegment(xlist, ylist, e);
            }

            if (yf != null)
            {
                for(int i = 0; i < 13; i++)
                {
                    int j = 13*n+i;
                    yf[j] = ytbl[i][1];
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

        bool overburning = false;

        private void multipleIntegrate(double[] y0, double[] yf, Solution sol, List<Arc> arcs, int count = 2, bool initialize = false)
        {
            if (y0 == null)
                Fatal("internal error - y0 is null");

            double t = 0;

            double tgo = y0[0];
            double dV = 0;

            overburning = false;
            for(int i = 0; i < arcs.Count; i++)
            {
                if (arcs[i].coast)
                {
                    double coast_time;

                    if (arcs[i].use_fixed_time)
                        coast_time = arcs[i].fixed_tbar - t;
                    else if (arcs[i].use_fixed_time2)
                        coast_time = arcs[i].fixed_tbar;
                    else
                        coast_time = y0[arcIndex(arcs, i, parameters: true)];

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
                        // overburning is handled as an "exception" that the caller needs to check
                        if (tgo > arcs[i].max_bt_bar && i == arcs.Count - 1)
                            overburning = true;

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
            int index = 1;
            for(int i=0; i<n; i++)
            {
                if (arcs[i].coast)
                {
                    if (arcs[i].use_fixed_time)
                        index += 13;
                    else
                        index += 14;
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
                if (arcs[n].coast)
                {
                    if (!arcs[n].use_fixed_time)
                    {
                        index += 1;
                    }
                }
            }

            return index;
        }

        public double znormAtStateVectors(Vector3d r, Vector3d v)
        {
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);

            Vector3d r_bar = rot * r / r_scale;
            Vector3d v_bar = rot * v / v_scale;

            double[] yT = new double[13];
            double[] zterm = new double[6];

            yT[0] = r_bar[0];
            yT[1] = r_bar[1];
            yT[2] = r_bar[2];
            yT[3] = v_bar[0];
            yT[4] = v_bar[1];
            yT[5] = v_bar[2];

            bcfun(yT, zterm, true);

            double znorm = 0.0;
            for(int i = 0; i < zterm.Length; i++)
            {
                znorm += zterm[i] * zterm[i];
            }
            return Math.Sqrt(znorm);
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

            bcfun(yT, zterm, false);

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
                        if (arcs[i].m0 <= 0) // negative mass => continuity rather than mass jettison
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
                    if (!arcs[j].coast)
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
                    z[n] += yf[i+6+13*(arcs.Count-1)] * yf[i+6+13*(arcs.Count-1)];
                z[n] = Math.Sqrt(z[n]) - 1.0;
            }
            n++;

            double total_bt_bar = 0;
            for(int i = 0; i < arcs.Count; i++)
            {
                if ( arcs[i].coast )
                {
                    if (arcs[i].use_fixed_time2)
                    {
                        int index = arcIndex(arcs, i, parameters: true);
                        z[n] = y0[index] - arcs[i].fixed_tbar;
                        n++;
                    }
                    else if (arcs[i].coast_after_jettison)
                    {
                        // H0 should be zero throughout the entire coast and by continuity
                        // be zero at the start of the subsequent burn, per Lu, 2008 we use
                        // the start of the subsequent burn.
                        int index = arcIndex(arcs, i+1);

                        Vector3d r = new Vector3d(y0[index+0], y0[index+1], y0[index+2]);
                        Vector3d v = new Vector3d(y0[index+3], y0[index+4], y0[index+5]);
                        Vector3d pv = new Vector3d(y0[index+6], y0[index+7], y0[index+8]);
                        Vector3d pr = new Vector3d(y0[index+9], y0[index+10], y0[index+11]);
                        double rm = r.magnitude;

                        double H0t1 = Vector3d.Dot(pr, v) - Vector3d.Dot(pv, r) / (rm * rm * rm);

                        z[n] = H0t1;
                        n++;
                    }
                }
                // sum up burntime of burn arcs
                if ( !arcs[i].coast )
                    total_bt_bar += arcs[i].max_bt_bar;
            }

            double znorm = 0.0;
            for(int i = 0; i < n; i++)
                znorm += z[i] * z[i];
            znorm = Math.Sqrt(znorm);
            if (znorm < 1e-9)
            {
                alglib.minlmrequesttermination(state);
            }
        }

        double lmEpsx =  1e-10;   // now that we request termination when the znorm gets < 1e-9 this value could be pushed up if necessary
        int lmIter = 20000;       // should revisit this, 20,000 seems like an awful lot now that the math is more stable, but clearly this is very high
        double lmDiffStep = 1e-9; // diffstep may be able to be pushed up to 1e-15?

        alglib.minlmstate state;

        public bool runOptimizer(List<Arc> arcs)
        {
            for(int i = 0; i < y0.Length; i++)
            {
                //DebugLog("y0["+i+"]=" + y0[i]);
            }

            double[] z = new double[arcIndex(arcs,arcs.Count)];
            optimizationFunction(y0, z, arcs);

            double znorm = 0.0;

            for(int i = 0; i < z.Length; i++)
            {
                znorm += z[i] * z[i];
            }

            znorm = Math.Sqrt(znorm);

            /*
            double[] bndl = new double[arcIndex(arcs,arcs.Count)];
            double[] bndu = new double[arcIndex(arcs,arcs.Count)];
            for(int i = 0; i < bndl.Length; i++)
            {
                bndl[i] = Double.NegativeInfinity;
                bndu[i] = Double.PositiveInfinity;
            }

            for(int i = 0; i < arcs.Count; i++)
            {
                if (arcs[i].coast_after_jettison)
                {
                    int index = arcIndex(arcs, i, parameters: true);
                    bndl[index] = 0;
                }
            }
            bndl[0] = 0;
            */

            alglib.minlmreport rep = new alglib.minlmreport();
            alglib.minlmcreatev(y0.Length, y0, lmDiffStep, out state);  /* y0.Length must == z.Length returned by the BC function for square problems */
            // alglib.minlmsetbc(state, bndl, bndu);
            alglib.minlmsetcond(state, lmEpsx, lmIter);
            alglib.minlmoptimize(state, optimizationFunction, null, arcs);

            double[] y0_new = new double[y0.Length];
            alglib.minlmresultsbuf(state, ref y0_new, rep);
            last_lm_iteration_count = rep.iterationscount;
            last_lm_status = rep.terminationtype;

            if (last_lm_iteration_count > max_lm_iteration_count)
                max_lm_iteration_count = last_lm_iteration_count;

            for(int i = 0; i < y0.Length; i++)
            {
                //DebugLog("y0_new["+i+"]=" + y0_new[i]);
            }

            optimizationFunction(y0_new, z, arcs);

            znorm = 0.0;
            double max_z = 0.0;

            for(int i = 0; i < z.Length; i++)
            {
                if (z[i] > max_z)
                    max_z = z[i];
                znorm += z[i] * z[i];
                //DebugLog("z["+i+"]=" + z[i]);
            }
            //DebugLog("znorm = " + Math.Sqrt(znorm));

            last_znorm = Math.Sqrt(znorm);

            // this comes first because after max-iterations we may still have an acceptable solution.
            // we check the largest z-value rather than znorm because for a lot of dimensions several slightly
            // off z values can add up to failure when they're all acceptable tolerances.
            if (max_z < 1e-5)
            {
                y0 = y0_new;
                return true;
            }

            /*
            if ( (rep.terminationtype != 2) && (rep.terminationtype != 7) )
                return false;
                */

            return false;
        }

        public double[] y0;

        public void UpdateY0(List<Arc> arcs)
        {
            /* FIXME: some of the round tripping here is silly */
            //Stage s = stages[0];  // FIXME: shouldn't we be using the arcs.stages?
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
                {
                    y0[arcindex+12] = arcs[i].m0;  // update all stages for the m0 in stage stats (boiloff may update upper stages)
                }
            }

            if (solution != null)
            {
                y0[0] = solution.tburn_bar(0); // FIXME: need to pass actual t0 here
            }
            else
            {
                y0[0] = tgo_bar;
            }
        }

        /* insert coast before the ith stage */
        protected void InsertCoast(List<Arc> arcs, int i, Solution sol)
        {
            int bottom = arcIndex(arcs, i, parameters: true);

            if ( arcs[i].coast )
                throw new Exception("adding a coast before a coast");

            arcs.Insert(i, new Arc(this, t0: sol.t0, coast_after_jettison: true, coast: true));
            double[] y0_new = new double[arcIndex(arcs, arcs.Count)];

            double tmin = sol.segments[i].tmin;
            double dt = sol.segments[i].tmax - tmin;

            // copy all the lower arcs
            Array.Copy(y0, 0, y0_new, 0, bottom);
            // initialize the coast
            //y0_new[bottom] = Math.Asin(MuUtils.Clamp(0.5 / MAX_COAST_TAU - 1, -1, 1));
            y0_new[bottom] = y0[0]; // dt/2.0;
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

            if ( !arcs[i].coast )
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
                {
                    Arc arc = solution.arcs[i];
                    arc.UpdateStageInfo(t0); // FIXME: this has the effect of synch'ing the stage information to the solution
                    last_arcs.Add(solution.arcs[i]);  // shallow copy
                    if ( solution.tgo(t0, i) <= 0 ) // this is responsible for skipping done coasts and anything else in the past
                    {
                        solution.arcs[i].done = true; // this has the side effect of marking the arc done in the existing solution
                    }
                }
            }

            try {
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
                }

                if (y0 == null)
                {
                    successful_converges = 0;
                    max_lm_iteration_count = 0;
                    Bootstrap(t0);
                }
                else
                {
                    while(last_arcs[0].done)
                    {
                        double[] y0_old = y0;
                        last_arcs.RemoveAt(0);
                        int new_upper_length = arcIndex(last_arcs,last_arcs.Count) - 2;

                        // copy the upper N-1 arcs
                        int start = y0_old.Length - new_upper_length;
                        // neeed the upper N-1 arcs plus the 2 burntime parameters
                        y0 = new double[new_upper_length+2];
                        // copy the 2 burntime parameters
                        Array.Copy(y0_old, 0, y0, 0, 2);
                        Array.Copy(y0_old, start, y0, 2, new_upper_length);

                        if (last_arcs[0].coast)
                        {
                            last_arcs[0].coast_after_jettison = false;  /* we can't be after a jettison if we're first */
                            last_arcs[0].use_fixed_time2 = false;  /* also can't have a fixed time when we're in the coast */
                        }
                    }

                    if (last_arcs.Count == 0)
                    {
                        Fatal("Zero stages after shrinking rocket");
                        return;
                    }

                    // fix up coast stage mass for boiloff
                    // FIXME: this is now broken because m0 of a coast arc is always < 0 and coasts have no associated Stage
                    // (does it matter? can this be deleted? coast stages don't care about mass)
                    //for(int i = 0; i < last_arcs.Count; i++)
                    //{
                    //    if (last_arcs[i].thrust == 0 && last_arcs[i].m0 > 0 && i < last_arcs.Count - 1)
                    //        last_arcs[i].stage.startMass = last_arcs[i+1].stage.startMass;
                    //}

                    UpdateY0(last_arcs);

                    if ( !runOptimizer(last_arcs) )
                    {
                        Fatal("Converged optimizer iteration failed");
                        return;
                    }

                    if ( overburning )
                    {
                        if ( last_arcs.Count < stages.Count )
                        {
                            DebugLog("Overburning: adding another available stage to the solution");
                            last_arcs.Add(new Arc(this, stage: stages[last_arcs.Count], t0: t0));
                            double[] y0_new = new double[arcIndex(last_arcs, last_arcs.Count)];
                            Array.Copy(y0, 0, y0_new, 0, y0.Length);
                            y0 = y0_new;
                            yf = new double[last_arcs.Count*13];
                            multipleIntegrate(y0, yf, last_arcs, initialize: true);
                            if ( !runOptimizer(last_arcs) )
                            {
                                Fatal("Optimzer failed after adjusting for overburning");
                                return;
                            }
                        }
                        // else we should do something here
                    }

                    Solution sol = new Solution(t_scale, v_scale, r_scale, t0);

                    multipleIntegrate(y0, sol, last_arcs, 10);

                    this.solution = sol;

                    yf = new double[last_arcs.Count*13];  /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
                    multipleIntegrate(y0, yf, last_arcs);

                    successful_converges += 1;
                    last_success_time = Planetarium.GetUniversalTime();
                }
            }
            catch (alglib.alglibexception e)
            {
                last_alglib_exception = e;
                Fatal("Uncaught Alglib Exception (" + e.GetType().Name + "): " + e.Message + "; " + e.msg);
            }
            catch (Exception e)
            {
                last_exception = e;
                Fatal("Uncaught Exception (" + e.GetType().Name + "): " + e.Message);
            }
        }

        public alglib.alglibexception last_alglib_exception = null;
        public Exception last_exception = null;

        public Solution solution;

        private Thread thread;

        // FIXME: still not using this, should it just be removed?
        public void forceBootstrap()
        {
            KillThread();
            y0 = null;
        }

        public double start_time;

        public double running_time(double t0)
        {
            if (thread != null)
                return t0 - start_time;
            else
                return 0;
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

                start_time = t0;
                thread = new Thread(() => Optimize(t0));
                thread.Start();
                return true;
            }
        }

        // FIXME: use the thread safe Debug.Log that is kicking around the MJ sources, didn't need to write this

        // Minimum viable message Queue
        Queue logQueue = new Queue();
        Mutex mut = new Mutex();

        // NOTE: The Debug.Log() method is NOT THREADSAFE IN UNITY and causes CTD.
        //
        // All functions in the optimizer which runs in a separate thread (nearly every single one
        // of them) must call DebugLog() and not Debug.Log().
        //
        protected void DebugLog(string s)
        {
            mut.WaitOne();
            logQueue.Enqueue(s);
            mut.ReleaseMutex();
        }

        // Similarly we can't log fatal stacktraces to the log from the thread, so we have to save state
        // here in order for the Janitorial() task to Debug.Log them properly.
        //
        protected void Fatal(string s)
        {
            last_failure_cause = s;
            y0 = null;
        }

        // need to call this every tick or so in order to dump exceptions to the log from
        // the main thread since Debug.Log is not threadsafe in Unity.  (this must also
        // obviously be kept safe to call every tick and must not update any inputs from
        // the calling guidance controller while a thread might already be running).
        //
        public void Janitorial()
        {
            if ( last_alglib_exception != null )
            {
                alglib.alglibexception e = last_alglib_exception;
                last_alglib_exception = null;
                Debug.Log("An exception occurred: " + e.GetType().Name);
                Debug.Log("Message: " + e.Message);
                Debug.Log("MSG: " + e.msg);
                Debug.Log("Stack Trace:\n" + e.StackTrace);
            }
            if ( last_exception != null )
            {
                Exception e = last_exception;
                last_exception = null;
                Debug.Log("An exception occurred: " + e.GetType().Name);
                Debug.Log("Message: " + e.Message);
                Debug.Log("Stack Trace:\n" + e.StackTrace);
            }

            mut.WaitOne();
            while ( logQueue.Count > 0 )
            {
                Debug.Log((string)logQueue.Dequeue());
            }
            mut.ReleaseMutex();
        }

        // Does what it says on the tin.
        //
        public void KillThread()
        {
            if (thread != null)
                thread.Abort();
        }
    }
}
