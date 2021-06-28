using System;
using System.Collections;
using System.Threading;
using MuMech.MathJ;
using UnityEngine;

namespace MuMech
{
    public enum BCType
    {
        KEPLER5,
        KEPLER4,
        KEPLER3,
        FLIGHTANGLE4,
        FLIGHTANGLE5
    }

    public abstract class PontryaginBase
    {
        /* dead code but kept as it can be useful for debugging
        protected double LAN(Vector3d r, Vector3d v)
        {
            var n = new Vector3d(0, -1, 0); // angular momentum vectors point south in KSP and we're in xzy coords
            Vector3d h = Vector3d.Cross(r, v);
            Vector3d an = -Vector3d.Cross(n, h); // needs to be negative (or swapped) in left handed coordinate system
            return an[2] >= 0 ? Math.Acos(an[0] / an.magnitude) : 2.0 * Math.PI - Math.Acos(an[0] / an.magnitude);
        }
        */

        // metrics
        public int    successful_converges;
        public int    max_lm_iteration_count;
        public int    last_lm_iteration_count;
        public int    last_lm_status;
        public double last_znorm;
        public string last_failure_cause;
        public double last_success_time;

        protected    MechJebModuleLogicalStageTracking.StageContainer stages => core.stageTracking.Stages;
        public       double mu;
        public       BCType bctype;
        public       Action<double[], double[], bool> bcfun;
        public const double g0 = 9.80665;
        public       Vector3d r0, v0, r0_bar, v0_bar;
        public       Vector3d pv0, pr0;
        public       double tgo, tgo_bar, vgo, vgo_bar; // FIXME: tgo + tgo_bar seem a little useless -- vgo + vgo_bar seem completely useless?
        public       double g_bar, r_scale, v_scale, t_scale; /* problem scaling */
        public       double dV, dV_bar; /* guess at dV */
        protected    bool fixed_final_time;
        protected    MechJebCore core;

        public PontryaginBase(MechJebCore core, double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV)
        {
            this.core = core;
            QuaternionD rot = QuaternionD.Inverse(Planetarium.fetch.rotation);
            r0       = rot * r0;
            v0       = rot * v0;
            pv0      = rot * pv0;
            pr0      = rot * pr0;
            this.r0  = r0;
            this.v0  = v0;
            this.mu  = mu;
            this.pv0 = pv0;
            this.pr0 = pr0;
            this.dV  = dV;
            double r0m = this.r0.magnitude;
            g_bar   = mu / (r0m * r0m);
            r_scale = r0m;
            v_scale = Math.Sqrt(r0m * g_bar);
            t_scale = Math.Sqrt(r0m / g_bar);

            r0_bar = this.r0 / r_scale;
            v0_bar = this.v0 / v_scale;

            dV_bar           = dV / v_scale;
            fixed_final_time = false;
        }

        public void UpdatePosition(Vector3d r0, Vector3d v0, Vector3d lambda, Vector3d lambdaDot, double tgo, double vgo)
        {
            // this is safe (and must be kept safe) to hammer on every tick and to not update
            // the current position while a guidance solution thread is currently running.
            if (_thread != null && _thread.IsAlive)
                return;

            QuaternionD rot = QuaternionD.Inverse(Planetarium.fetch.rotation);
            r0      = rot * r0;
            v0      = rot * v0;
            this.r0 = r0;
            this.v0 = v0;
            if (Solution != null)
            {
                /* uhm, FIXME: this round trip is silly */
                pv0      = rot * lambda;
                pr0      = rot * lambdaDot;
                this.tgo = tgo;
                tgo_bar  = tgo / t_scale;
                this.vgo = vgo;
                vgo_bar  = vgo / v_scale;
            }

            r0_bar = this.r0 / r_scale;
            v0_bar = this.v0 / v_scale;
        }

        private class VacuumThrustKernel<T> : ODE<T> where T : ODESolver, new()
        {
            public override int N => 14;
            public override int M => 0;

            public Arc    arc   { get; set; }
            public double g_bar { get; set; }

            protected override void dydt(double[] y, double x, double[] dy)
            {
                double At = arc.Thrust / (y[12] * g_bar);
                if (arc.infinite) At = At * 2;
                double r2 = y[0] * y[0] + y[1] * y[1] + y[2] * y[2];
                double r = Math.Sqrt(r2);
                double r3 = r2 * r;
                double r5 = r3 * r2;
                double pvm = Math.Sqrt(y[6] * y[6] + y[7] * y[7] + y[8] * y[8]);
                double rdotpv = y[0] * y[6] + y[1] * y[7] + y[2] * y[8];

                /* dr = v */
                dy[0] = y[3];
                dy[1] = y[4];
                dy[2] = y[5];
                /* dv = - r / r^3 + At * u */
                dy[3] = -y[0] / r3 + At * y[6] / pvm;
                dy[4] = -y[1] / r3 + At * y[7] / pvm;
                dy[5] = -y[2] / r3 + At * y[8] / pvm;
                /* dpv = - pr */
                dy[6] = -y[9];
                dy[7] = -y[10];
                dy[8] = -y[11];
                /* dpr = pv / r3 - 3 / r5 dot(r, pv) r */
                dy[9]  = y[6] / r3 - 3 / r5 * rdotpv * y[0];
                dy[10] = y[7] / r3 - 3 / r5 * rdotpv * y[1];
                dy[11] = y[8] / r3 - 3 / r5 * rdotpv * y[2];
                /* m = mdot */
                dy[12] = arc.Thrust == 0 || arc.infinite ? 0 : -arc.Thrust / arc.c;
                /* accumulated ∆v of the arc */
                dy[13] = At;
            }
        }

        private readonly VacuumThrustKernel<DormandPrince> _integrator = new VacuumThrustKernel<DormandPrince>();

        private readonly double ckEps = 1e-6; /* matches Matlab's default 1e-6 ode45 reltol? */

        /* used to update y0 to yf without intermediate values */
        protected void singleIntegrate(double[] y0, double[] yf, int n, ref double t, double dt, ArcList arcs, ref double dV)
        {
            singleIntegrate(y0, yf, null, n, ref t, dt, arcs, ref dV);
        }

        /* used to pull intermediate values off to do cubic spline interpolation */
        protected void singleIntegrate(double[] y0, Solution sol, int n, ref double t, double dt, ArcList arcs, ref double dV)
        {
            singleIntegrate(y0, null, sol, n, ref t, dt, arcs, ref dV);
        }

        private void singleIntegrate(double[] y0, double[] yf, Solution sol, int n, ref double t, double dt, ArcList arcs, ref double dV)
        {
            Arc e = arcs[n];

            double[] y = new double[14];
            double[] y1 = new double[14];
            Array.Copy(y0, arcIndex(arcs, n), y, 0, 13);
            y[13] = dV;

            // fix the starting point to being r0_bar, v0_bar
            if (n == 0)
            {
                y[0] = r0_bar[0];
                y[1] = r0_bar[1];
                y[2] = r0_bar[2];
                y[3] = v0_bar[0];
                y[4] = v0_bar[1];
                y[5] = v0_bar[2];
            }

            CN interpolant = null;

            if (sol != null)
            {
                interpolant = _integrator.GetInterpolant();
                interpolant.Clear();
            }

            _integrator.Integrator.Hmin     = 1e-15;
            _integrator.Integrator.Accuracy = ckEps;
            _integrator.arc                 = e;
            _integrator.g_bar               = g_bar;
            _integrator.Integrate(y, y1, t, t + dt, interpolant);

            t    = t + dt;
            e.dV = (y1[13] - dV) * v_scale;
            dV   = y1[13];

            if (sol != null) sol.AddSegment(interpolant, e);

            if (yf != null)
                for (int i = 0; i < 13; i++)
                {
                    int j = 13 * n + i;
                    yf[j] = y1[i];
                }
        }

        // normal integration with no midpoints
        protected void multipleIntegrate(double[] y0, double[] yf, ArcList arcs, bool initialize = false)
        {
            multipleIntegrate(y0, yf, null, arcs, initialize);
        }

        // for generating the interpolated chebyshev solution
        protected void multipleIntegrate(double[] y0, Solution sol, ArcList arcs)
        {
            multipleIntegrate(y0, null, sol, arcs);
        }

        // copy the nth
        private void copy_yf_to_y0(double[] y0, double[] yf, int n, ArcList arcs)
        {
            int yf_index = 13 * n;
            int y0_index = arcIndex(arcs, n + 1);
            Array.Copy(yf, yf_index, y0, y0_index, 13);
            double m0 = arcs[n + 1].M0;
            if (m0 > 0)
                y0[y0_index + 12] = m0;
        }

        private bool overburning;
        private int  activeBurns;

        private void multipleIntegrate(double[] y0, double[] yf, Solution sol, ArcList arcs, bool initialize = false)
        {
            if (y0 == null)
                Fatal("internal error - y0 is null");

            double t = 0;

            double tgo = y0[0];
            double dV = 0;

            // if we are burning past the current "top" of the rocket
            overburning = false;
            // how many burn phases are currently active (some burns might have tgo = 0 but they'll get pruned next tick)
            activeBurns = 0;
            for (int i = 0; i < arcs.Count; i++)
                if (arcs[i].coast)
                {
                    double coast_time;

                    if (arcs[i].use_fixed_time)
                        coast_time = arcs[i].fixed_tbar - t;
                    else if (arcs[i].use_fixed_time2)
                        coast_time = arcs[i].fixed_tbar;
                    else
                        coast_time = y0[arcIndex(arcs, i, true)];

                    if (yf != null)
                    {
                        singleIntegrate(y0, yf, i, ref t, coast_time, arcs, ref dV);
                        if (initialize && i < arcs.Count - 1)
                            copy_yf_to_y0(y0, yf, i, arcs);
                    }
                    else
                    {
                        singleIntegrate(y0, sol, i, ref t, coast_time, arcs, ref dV);
                    }

                    arcs[i].complete_burn = false;
                }
                else
                {
                    activeBurns++;
                    if (tgo <= arcs[i].MaxBtBar || i == arcs.Count - 1)
                    {
                        // overburning is handled as an "exception" that the caller needs to check
                        if (tgo > arcs[i].MaxBtBar && i == arcs.Count - 1)
                            overburning = true;

                        if (yf != null)
                        {
                            // normal integration with no midpoints
                            singleIntegrate(y0, yf, i, ref t, tgo, arcs, ref dV);
                            if (initialize && i < arcs.Count - 1)
                                copy_yf_to_y0(y0, yf, i, arcs);
                        }
                        else
                        {
                            // for generating the interpolated chebyshev solution
                            singleIntegrate(y0, sol, i, ref t, tgo, arcs, ref dV);
                        }

                        arcs[i].complete_burn = false;
                        tgo                   = 0;
                    }
                    else
                    {
                        if (yf != null)
                        {
                            // normal integration with no midpoints
                            singleIntegrate(y0, yf, i, ref t, arcs[i].MaxBtBar, arcs, ref dV);
                            if (initialize && i < arcs.Count - 1)
                                copy_yf_to_y0(y0, yf, i, arcs);
                        }
                        else
                        {
                            // for generating the interpolated chebyshev solution
                            singleIntegrate(y0, sol, i, ref t, arcs[i].MaxBtBar, arcs, ref dV);
                        }

                        arcs[i].complete_burn =  true;
                        tgo                   -= arcs[i].MaxBtBar;
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
        public int arcIndex(ArcList arcs, int n, bool parameters = false)
        {
            int index = 1;
            for (int i = 0; i < n; i++)
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

            // by default this gives the offset to the continuity variables (the common case)
            // set parameters to true to bypass adding that offset and get the index to the parameters instead
            if (!parameters && n != arcs.Count)
                if (arcs[n].coast)
                    if (!arcs[n].use_fixed_time)
                        index += 1;

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
            for (int i = 0; i < zterm.Length; i++) znorm += zterm[i] * zterm[i];
            return Math.Sqrt(znorm);
        }

        public double[] yf;

        public void optimizationFunction(double[] y0, double[] z, object o)
        {
            var arcs = (ArcList) o;
            yf = new double[arcs.Count * 13]; /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
            multipleIntegrate(y0, yf, arcs);

            /* initial conditions */
            z[0] = y0[arcIndex(arcs, 0) + 0] - r0_bar[0];
            z[1] = y0[arcIndex(arcs, 0) + 1] - r0_bar[1];
            z[2] = y0[arcIndex(arcs, 0) + 2] - r0_bar[2];
            z[3] = y0[arcIndex(arcs, 0) + 3] - v0_bar[0];
            z[4] = y0[arcIndex(arcs, 0) + 4] - v0_bar[1];
            z[5] = y0[arcIndex(arcs, 0) + 5] - v0_bar[2];
            z[6] = y0[arcIndex(arcs, 0) + 12] - arcs[0].M0;

            /* terminal constraints */
            double[] yT = new double[13];
            Array.Copy(yf, (arcs.Count - 1) * 13, yT, 0, 13);
            double[] zterm = new double[6];
            if (bcfun == null)
                throw new Exception("No bcfun was provided to the Pontryagin optimizer");

            bcfun(yT, zterm, false);

            z[7]  = zterm[0];
            z[8]  = zterm[1];
            z[9]  = zterm[2];
            z[10] = zterm[3];
            z[11] = zterm[4];
            z[12] = zterm[5];

            /* multiple shooting continuity */
            for (int i = 1; i < arcs.Count; i++)
            for (int j = 0; j < 13; j++)
                if (j == 12)
                {
                    if (arcs[i].M0 <= 0) // negative mass => continuity rather than mass jettison
                        /* continuity */
                        z[j + 13 * i] = y0[j + arcIndex(arcs, i)] - yf[j + 13 * (i - 1)];
                    else
                        /* mass jettison */
                        z[j + 13 * i] = y0[j + arcIndex(arcs, i)] - arcs[i].M0;
                }
                else
                {
                    z[j + 13 * i] = y0[j + arcIndex(arcs, i)] - yf[j + 13 * (i - 1)];
                }

            /* magnitude of terminal costate vector = 1.0 (dummy constraint for H(tf)=0 to optimize burntime because BC is keplerian) */
            int n = 13 * arcs.Count;

            // FIXME: this isn't fixed_final_time, it is optimized terminal burn to a fixed final time coast.
            if (fixed_final_time)
            {
                int i = 0;

                // find the last burn arc (uh, this has to be arcs.Count - 1 right?)
                for (int j = 0; j < arcs.Count; j++)
                    if (!arcs[j].coast)
                        i = j;

                var r2 = new Vector3d(yf[i * 13 + 0], yf[i * 13 + 1], yf[i * 13 + 2]);
                var v2 = new Vector3d(yf[i * 13 + 3], yf[i * 13 + 4], yf[i * 13 + 5]);
                var pv2 = new Vector3d(yf[i * 13 + 6], yf[i * 13 + 7], yf[i * 13 + 8]);
                var pr2 = new Vector3d(yf[i * 13 + 9], yf[i * 13 + 10], yf[i * 13 + 11]);
                double r2m = r2.magnitude;

                /* H0 at the end of the final burn = 0 */
                double H0t2 = Vector3d.Dot(pr2, v2) - Vector3d.Dot(pv2, r2) / (r2m * r2m * r2m);
                z[n] = H0t2;
            }
            else
            {
                z[n] = 0.0;
                for (int i = 0; i < 6; i++)
                    z[n] += yf[i + 6 + 13 * (arcs.Count - 1)] * yf[i + 6 + 13 * (arcs.Count - 1)];
                z[n] = Math.Sqrt(z[n]) - 1.0;
            }

            n++;

            double total_bt_bar = 0;
            for (int i = 0; i < arcs.Count; i++)
            {
                if (arcs[i].coast)
                {
                    if (arcs[i].use_fixed_time2)
                    {
                        int index = arcIndex(arcs, i, true);
                        z[n] = y0[index] - arcs[i].fixed_tbar;
                        n++;
                    }
                    else if (arcs[i].coast_after_jettison)
                    {
                        // H0 should be zero throughout the entire coast and by continuity
                        // be zero at the start of the subsequent burn, per Lu, 2008 we use
                        // the start of the subsequent burn.
                        int index = arcIndex(arcs, i + 1);

                        var r = new Vector3d(y0[index + 0], y0[index + 1], y0[index + 2]);
                        var v = new Vector3d(y0[index + 3], y0[index + 4], y0[index + 5]);
                        var pv = new Vector3d(y0[index + 6], y0[index + 7], y0[index + 8]);
                        var pr = new Vector3d(y0[index + 9], y0[index + 10], y0[index + 11]);
                        double rm = r.magnitude;

                        double H0t1 = Vector3d.Dot(pr, v) - Vector3d.Dot(pv, r) / (rm * rm * rm);

                        z[n] = H0t1;
                        n++;
                    }
                }

                // sum up burntime of burn arcs
                if (!arcs[i].coast)
                    total_bt_bar += arcs[i].MaxBtBar;
            }

            double znorm = 0.0;
            for (int i = 0; i < n; i++)
                znorm += z[i] * z[i];
            znorm = Math.Sqrt(znorm);
            if (znorm < 1e-9) alglib.minlmrequesttermination(state);
        }

        private readonly double
            lmEpsx = 1e-10; // now that we request termination when the znorm gets < 1e-9 this value could be pushed up if necessary

        private readonly int
            lmIter = 20000; // should revisit this, 20,000 seems like an awful lot now that the math is more stable, but clearly this is very high

        private readonly double lmDiffStep = 1e-9; // diffstep may be able to be pushed up to 1e-15?

        private alglib.minlmstate state;

        public bool runOptimizer(ArcList arcs)
        {
            for (int i = 0; i < y0.Length; i++)
            {
                //DebugLog("y0["+i+"]=" + y0[i]);
            }

            double[] z = new double[arcIndex(arcs, arcs.Count)];
            optimizationFunction(y0, z, arcs);

            double znorm = 0.0;

            for (int i = 0; i < z.Length; i++) znorm += z[i] * z[i];

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

            var rep = new alglib.minlmreport();
            alglib.minlmcreatev(y0.Length, y0, lmDiffStep, out state);
            //alglib.minlmsetbc(state, bndl, bndu);
            alglib.minlmsetcond(state, lmEpsx, lmIter);
            alglib.minlmoptimize(state, optimizationFunction, null, arcs);

            double[] y0_new = new double[y0.Length];
            alglib.minlmresultsbuf(state, ref y0_new, rep);
            last_lm_iteration_count = rep.iterationscount;
            last_lm_status          = rep.terminationtype;

            if (last_lm_iteration_count > max_lm_iteration_count)
                max_lm_iteration_count = last_lm_iteration_count;

            for (int i = 0; i < y0.Length; i++)
            {
                //DebugLog("y0_new["+i+"]=" + y0_new[i]);
            }

            optimizationFunction(y0_new, z, arcs);

            znorm = 0.0;
            double max_z = 0.0;

            for (int i = 0; i < z.Length; i++)
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

        public void UpdateY0(ArcList arcs)
        {
            /* FIXME: some of the round tripping here is silly */
            //Stage s = stages[0];  // FIXME: shouldn't we be using the arcs.stages?
            int arcindex = arcIndex(arcs, 0);
            y0[arcindex]      = r0_bar[0]; // FIXME: shouldn't we pull all this off of the current solution?
            y0[arcindex + 1]  = r0_bar[1];
            y0[arcindex + 2]  = r0_bar[2];
            y0[arcindex + 3]  = v0_bar[0];
            y0[arcindex + 4]  = v0_bar[1];
            y0[arcindex + 5]  = v0_bar[2];
            y0[arcindex + 6]  = pv0[0];
            y0[arcindex + 7]  = pv0[1];
            y0[arcindex + 8]  = pv0[2];
            y0[arcindex + 9]  = pr0[0];
            y0[arcindex + 10] = pr0[1];
            y0[arcindex + 11] = pr0[2];

            for (int i = 0; i < arcs.Count; i++)
            {
                arcindex = arcIndex(arcs, i);
                if (arcs[i].M0 > 0) // don't update guess if we're not staging (optimizer has to find the terminal mass of prior stage)
                    y0[arcindex + 12] = arcs[i].M0; // update all stages for the m0 in stage stats (boiloff may update upper stages)
            }

            if (Solution != null)
                y0[0] = Solution.tburn_bar(0); // FIXME: need to pass actual t0 here
            else
                y0[0] = tgo_bar;
        }

        /* insert coast before the ith stage */
        protected void InsertCoast(ArcList arcs, int i, Solution sol, double fixedLength = -1)
        {
            if (arcs[i].coast)
                throw new Exception("adding a coast before a coast");

            Arc arc;

            if (fixedLength < 0)
                arc = new Arc(this, sol.t0)
                {
                    coast_after_jettison = true,
                    coast = true
                };
            else
                arc = new Arc(this, sol.t0)
                {
                    use_fixed_time2 = true,
                    coast           = true,
                    fixed_time      = fixedLength,
                    fixed_tbar      = fixedLength / t_scale
                };

            arcs.Insert(i, arc);
            double[] y0_new = new double[arcIndex(arcs, arcs.Count)];

            int bottom = arcIndex(arcs, i, true);
            // copy all the lower arcs
            Array.Copy(y0, 0, y0_new, 0, bottom);
            // initialize the coast
            y0_new[bottom] = fixedLength < 0 ? y0[0] : fixedLength;

            // FIXME: copy the rest of the parameters for upper stage coasts
            y0 = y0_new;
            yf = new double[arcs.Count * 13];
            multipleIntegrate(y0, yf, arcs, true);
        }

        /* tweak coast at the ith stage, inserting burntime of the ith stage / 2 */
        protected void RetryCoast(ArcList arcs, int i, Solution sol)
        {
            int bottom = arcIndex(arcs, i, true);

            if (!arcs[i].coast)
                throw new Exception("trying to update a non-coasting coast");

            double tmin = sol.segments[i + 1].Tmin;
            double dt = sol.segments[i + 1].Tmax - tmin;

            y0[bottom]     = dt / 2.0;
            y0[bottom + 1] = 0.0;

            multipleIntegrate(y0, yf, arcs, true);
        }

        /* remove an arc from the solution */
        protected void RemoveArc(ArcList arcs, int i, Solution sol)
        {
            int bottom = arcIndex(arcs, i, true);
            int top = arcIndex(arcs, i + 1, true);

            arcs.RemoveAt(i);

            double[] y0_new = new double[arcIndex(arcs, arcs.Count)];
            // copy all the lower arcs
            Array.Copy(y0, 0, y0_new, 0, bottom);
            // copy all the upper arcs
            Array.Copy(y0, top, y0_new, bottom, y0_new.Length - bottom);

            y0 = y0_new;
            yf = new double[arcs.Count * 13];

            if (arcs[0].coast)
            {
                arcs[0].coast_after_jettison = false; /* we can't be after a jettison if we're first */
                arcs[0].use_fixed_time2      = false; /* also can't have a fixed time when we're in the coast */
            }

            multipleIntegrate(y0, yf, arcs, true);
        }

        protected abstract void Bootstrap(double t0);

        private void Optimize(double t0)
        {
            ArcList last_arcs = null;

            if (Solution != null && y0 != null)
            {
                last_arcs = new ArcList();
                for (int i = 0; i < Solution.arcs.Count; i++)
                {
                    Arc arc = Solution.arcs[i];
                    arc.UpdateStageInfo(t0);         // FIXME: this has the effect of synch'ing the stage information to the solution
                    last_arcs.Add(Solution.arcs[i]); // shallow copy
                    if (Solution.tgo(t0, i) <= 0)    // this is responsible for skipping done coasts and anything else in the past
                    {
                        DebugLog($"PVG marking stage {i} done: {Solution.arcs[i]}");
                        Solution.arcs[i].done = true; // this has the side effect of marking the arc done in the existing solution
                    }
                }
            }

            try
            {
                if (last_arcs != null)
                    // since we shift the zero of tbar forwards on every re-optimize we have to do this here
                    for (int i = 0; i < last_arcs.Count; i++)
                        if (last_arcs[i].use_fixed_time)
                            last_arcs[i].fixed_tbar = (last_arcs[i].fixed_time - t0) / t_scale;

                if (y0 == null)
                {
                    successful_converges   = 0;
                    max_lm_iteration_count = 0;
                    Bootstrap(t0);
                }
                else
                {
                    while (last_arcs[0].done)
                    {
                        DebugLog($"PVG: removing stage 0: {last_arcs[0]}");

                        RemoveArc(last_arcs, 0, Solution);

                        DebugLog($"PVG: arcs in solution after removing stage: {last_arcs}");
                    }

                    if (last_arcs.Count == 0)
                    {
                        Fatal("Zero stages after shrinking rocket");
                        return;
                    }

                    UpdateY0(last_arcs);

                    if (!runOptimizer(last_arcs))
                    {
                        Fatal("Converged optimizer iteration failed");
                        return;
                    }

                    multipleIntegrate(y0, yf, last_arcs);

                    if (overburning)
                        if (activeBurns < stages.Count)
                        {
                            DebugLog("PVG: adding another available stage to the solution");
                            DebugLog($"PVG: arcs currently in solution: {last_arcs}");
                            DebugLog($"PVG: stages in rocket: {stages}");
                            last_arcs.Add(new Arc(this, stage: stages[activeBurns], t0: t0));
                            double[] y0_new = new double[arcIndex(last_arcs, last_arcs.Count)];
                            Array.Copy(y0, 0, y0_new, 0, y0.Length);
                            y0 = y0_new;
                            yf = new double[last_arcs.Count * 13];
                            multipleIntegrate(y0, yf, last_arcs, true);
                            if (!runOptimizer(last_arcs))
                            {
                                Fatal("Optimzer failed after adjusting for overburning");
                                return;
                            }

                            DebugLog($"PVG: arcs in solution after adding stage to top: {last_arcs}");
                        }
                    // else we should do something here

                    var sol = new Solution(t_scale, v_scale, r_scale, t0);

                    multipleIntegrate(y0, sol, last_arcs);

                    Solution = sol;

                    yf = new double[last_arcs.Count * 13];
                    multipleIntegrate(y0, yf, last_arcs);

                    successful_converges += 1;
                    last_success_time    =  Planetarium.GetUniversalTime();
                }
            }
            catch (alglib.alglibexception e)
            {
                _lastAlglibException = e;
                Fatal("Uncaught Alglib Exception (" + e.GetType().Name + "): " + e.Message + "; " + e.msg);
            }
            catch (Exception e)
            {
                _lastException = e;
                Fatal("Uncaught Exception (" + e.GetType().Name + "): " + e.Message);
            }
        }

        private alglib.alglibexception _lastAlglibException;
        private Exception              _lastException;

        public Solution Solution;

        private Thread _thread;

        private double _startTime;

        public double running_time(double t0)
        {
            if (_thread != null)
                return t0 - _startTime;
            return 0;
        }

        public bool threadStart(double t0)
        {
            if (_thread != null && _thread.IsAlive) return false;

            if (_thread != null) _thread.Abort();

            _startTime = t0;
            _thread    = new Thread(() => Optimize(t0));
            _thread.Start();
            return true;
        }

        // FIXME: use the thread safe Debug.Log that is kicking around the MJ sources, didn't need to write this

        // Minimum viable message Queue
        private readonly Queue _logQueue = new Queue();
        private readonly Mutex _logMutex = new Mutex();

        // NOTE: The Debug.Log() method is NOT THREADSAFE IN UNITY and causes CTD.
        //
        // All functions in the optimizer which runs in a separate thread (nearly every single one
        // of them) must call DebugLog() and not Debug.Log().
        //
        protected void DebugLog(string s)
        {
            _logMutex.WaitOne();
            _logQueue.Enqueue(s);
            _logMutex.ReleaseMutex();
        }

        // Similarly we can't log fatal stacktraces to the log from the thread, so we have to save state
        // here in order for the Janitorial() task to Debug.Log them properly.
        //
        protected void Fatal(string s)
        {
            last_failure_cause = s;
            y0                 = null;
        }

        // need to call this every tick or so in order to dump exceptions to the log from
        // the main thread since Debug.Log is not threadsafe in Unity.  (this must also
        // obviously be kept safe to call every tick and must not update any inputs from
        // the calling guidance controller while a thread might already be running).
        //
        public void Janitorial()
        {
            if (_lastAlglibException != null)
            {
                alglib.alglibexception e = _lastAlglibException;
                _lastAlglibException = null;
                Debug.Log("An exception occurred: " + e.GetType().Name);
                Debug.Log("Message: " + e.Message);
                Debug.Log("MSG: " + e.msg);
                Debug.Log("Stack Trace:\n" + e.StackTrace);
            }

            if (_lastException != null)
            {
                Exception e = _lastException;
                _lastException = null;
                Debug.Log("An exception occurred: " + e.GetType().Name);
                Debug.Log("Message: " + e.Message);
                Debug.Log("Stack Trace:\n" + e.StackTrace);
            }

            _logMutex.WaitOne();
            while (_logQueue.Count > 0) Debug.Log((string) _logQueue.Dequeue());
            _logMutex.ReleaseMutex();
        }

        // Does what it says on the tin.
        //
        public void KillThread()
        {
            if (_thread != null)
                _thread.Abort();
        }
    }
}
