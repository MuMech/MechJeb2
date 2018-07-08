using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public class PontryaginLaunch : PontryaginBase {
        public PontryaginLaunch(double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV) : base(mu, r0, v0, pv0, pr0, dV)
        {
        }

        public override int arcIndex { get { return 3; } }

        double rTm;
        double vTm;
        double gamma;
        double inc;

        // 4-constraint PEG with free LAN
        public void flightangle4constraint(double rTm, double vTm, double gamma, double inc)
        {
            this.rTm = rTm;
            this.vTm = vTm;
            this.gamma = gamma;
            this.inc = inc;
            bcfun = flightangle4constraint;
        }

        private void flightangle4constraint(double[] yT, double[] z)
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

        public override void optimizationFunction(double[] y0, double[] z, object o)
        {

            List<Arc> arcs = (List<Arc>)o;
            base.optimizationFunction(y0, z, o);

            /* magnitude of initial costate vector = 1.0 (dummy constraint for H(tf)=0 because BC is keplerian) */
            int n = 13 * arcs.Count;
            z[n] = 0.0;
            for(int i = 0; i < 6; i++)
                z[n] = z[n] + y0[6+i+arcIndex] * y0[6+i+arcIndex];
            z[n] = Math.Sqrt(z[n]) - 1.0;

            // positive arc time constraint
            z[n+1] = ( y0[0] < 0 ) ? ( y0[0] - y0[1] * y0[1] ) * 1000 : y0[1] * y0[1];

            z[n+2] = y0[2] * y0[2]; // FIXME: remove this if its unnecessary now

            /* construct sum of the squares of the residuals for levenberg marquardt */
            for(int i = 0; i < z.Length; i++)
                z[i] = z[i] * z[i];

        }

        public void Bootstrap()
        {
            Solution new_sol = null;
            List<Arc> new_arcs = new List<Arc>();
            y0 = new double[13 + arcIndex];
            double ve = g0 * stages[0].isp;
            tgo = ve * stages[0].m0 / stages[0].thrust * ( 1 - Math.Exp(-dV/ve) );
            tgo_bar = tgo / t_scale;
            UpdateY0();
            y0[0] = tgo_bar;
            y0[1] = 0;

            for(int i = 0; i < stages.Count; i++)
            {
                new_arcs.Add(new Arc(stages[i]));

                /*
                for(int j = 0; i < (new_arcs.Count-1); j++)
                    new_arcs[j].infinite = false;

                new_arcs[new_arcs.Count-1].infinite = true;
                */

                for(int j = 0; j < y0.Length; j++)
                    Debug.Log("bootstrap - y0[" + j + "] = " + y0[j]);

                Debug.Log("running optimizer");

                if ( !runOptimizer(new_arcs) )
                {
                    Debug.Log("optimizer failed");
                    y0 = null;
                    return;
                }

                if (y0[0] < 0)
                {
                    Debug.Log("optimizer failed2");
                    y0 = null;
                    return;
                }

                Debug.Log("optimizer done");

                new_sol = new Solution(t_scale, v_scale, r_scale, 0);

                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("y0[" + k + "] = " + y0[k]);

                multipleIntegrate(y0, new_sol, new_arcs, 10);

                if (i < (stages.Count - 1))
                {
                    double[] y0_new = new double[13 * (i + 2) + arcIndex];
                    Array.Copy(y0, 0, y0_new, 0, 13*(i+1) + arcIndex);
                    y0 = y0_new;

                    double t    = new_arcs[i].max_bt_bar;
                    Vector3d r  = new_sol.r_bar(t);
                    Vector3d v  = new_sol.v_bar(t);
                    Vector3d pv = new_sol.pv_bar(t);
                    Vector3d pr = new_sol.pr_bar(t);
                    double m    = new_sol.m_bar(t);

                    y0[arcIndex + 13 * (i+1) + 0] = r[0];
                    y0[arcIndex + 13 * (i+1) + 1] = r[1];
                    y0[arcIndex + 13 * (i+1) + 2] = r[2];
                    y0[arcIndex + 13 * (i+1) + 3] = v[0];
                    y0[arcIndex + 13 * (i+1) + 4] = v[1];
                    y0[arcIndex + 13 * (i+1) + 5] = v[2];
                    y0[arcIndex + 13 * (i+1) + 6] = pv[0];
                    y0[arcIndex + 13 * (i+1) + 7] = pv[1];
                    y0[arcIndex + 13 * (i+1) + 8] = pv[2];
                    y0[arcIndex + 13 * (i+1) + 9] = pr[0];
                    y0[arcIndex + 13 * (i+1) + 10] = pr[1];
                    y0[arcIndex + 13 * (i+1) + 11] = pr[2];
                    y0[arcIndex + 13 * (i+1) + 12] = m;
                }

                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("new y0[" + k + "] = " + y0[k]);
            }
            this.solution = new_sol;
            Debug.Log("done with bootstrap");
            last_arcs = new_arcs;
        }

        public override void Optimize(double t0)
        {
            try {
                Debug.Log("starting optimize");
                if (stages != null)
                {
                    Debug.Log("stages: ");
                    for(int i = 0; i < stages.Count; i++)
                        Debug.Log(stages[i]);
                }
                if (last_arcs != null)
                {
                    Debug.Log("arcs: ");
                    for(int i = 0; i < last_arcs.Count; i++)
                        Debug.Log(last_arcs[i]);
                }

                if (y0 == null)
                {
                    Bootstrap();
                }
                else
                {
                    while(last_arcs[0].stage.staged)
                    {
                        Debug.Log("shrinking y0 array");
                        double[] y0_old = y0;
                        y0 = new double[13*last_arcs.Count + arcIndex];
                        Array.Copy(y0_old, 0, y0, 0, 13*last_arcs.Count + arcIndex);
                        last_arcs.RemoveAt(0);
                    }

                    UpdateY0();
                    y0[0] = tgo_bar;
                    y0[1] = 0;
                    Debug.Log("normal optimizer run start");

                    if ( !runOptimizer(last_arcs) )
                    {
                        Debug.Log("optimizer failed3");
                        y0 = null;
                        return;
                    }

                    if (y0[0] < 0)
                    {
                        Debug.Log("optimizer failed4");
                        y0 = null;
                        return;
                    }

                    Solution sol = new Solution(t_scale, v_scale, r_scale, t0);

                    for(int i = 0; i < y0.Length; i++)
                        Debug.Log("y0[" + i + "] = " + y0[i]);

                    multipleIntegrate(y0, sol, last_arcs, 10);

                    this.solution = sol;

                    Debug.Log("rf = " + sol.r_bar(sol.tmax()) + "(" + sol.r_bar(sol.tmax()).magnitude + ") vf = " + sol.v_bar(sol.tmax()) + "(" + sol.v_bar(sol.tmax()).magnitude + ")");
                    Debug.Log("rf = " + sol.r(sol.tf()) + "(" + sol.r(sol.tf()).magnitude + ") vf = " + sol.v(sol.tf()) + "(" + sol.v(sol.tf()).magnitude + ")");
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }
}
