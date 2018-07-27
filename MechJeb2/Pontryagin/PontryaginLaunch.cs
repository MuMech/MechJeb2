using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public class PontryaginLaunch : PontryaginBase {
        public PontryaginLaunch(double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV) : base(mu, r0, v0, pv0, pr0, dV)
        {
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
        public override int arcIndex(List<Arc> arcs, int n, bool parameters = false)
        {
            int index = 2;
            for(int i=0; i<n; i++)
            {
                if (arcs[i].thrust == 0)
                    index += 15;
                else
                    index += 13;
            }
            // by default this gives the offset to the continuity variables (the common case)
            // set parameters to true to bypass adding that offset and get the index to the parameters instead
            if (!parameters && n != arcs.Count)
            {
                if (arcs[n].thrust == 0)
                    index += 2;
            }

            return index;
        }

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

            /* magnitude of initial costate vector = 1.0 (dummy constraint for H(tf)=0 to optimize burntime because BC is keplerian) */
            int n = 13 * arcs.Count;
            z[n] = 0.0;
            for(int i = 0; i < 6; i++)
                z[n] = z[n] + y0[i+6+arcIndex(arcs,0)] * y0[i+6+arcIndex(arcs,0)];
            z[n] = Math.Sqrt(z[n]) - 1.0;

            n++;

            // positive arc time constraint
            z[n] = ( y0[0] < 0 ) ? ( y0[0] - y0[1] * y0[1] ) : 0;

            n++;

            double total_bt_bar = 0;
            for(int i = 0; i < arcs.Count; i++)
            {
                if ( arcs[i].thrust == 0 )
                {
                    int index = arcIndex(arcs, i, parameters: true);
                    if (total_bt_bar > y0[0])
                    {
                        // force unreachable coasts to zero
                        z[n] = Math.Abs(y0[index]);
                        n++;
                        z[n] = Math.Abs(y0[index+1]);
                        n++;
                    }
                    else
                    {
                        // optimized coast
                        double y0sum = 0.0;
                        double yfsum = 0.0;
                        for(int j = 0; j < 3; j++)
                        {
                            // squared magnitude of the primer vector before + after
                            y0sum += y0[index+2+6+j] * y0[index+2+6+j];
                            yfsum += yf[13*i+6+j] * yf[13*i+6+j];
                        }
                        z[n] = Math.Sqrt(yfsum) - Math.Sqrt(y0sum);
                        n++;
                        z[n] = ( y0[index] < 0 ) ? ( y0[index] - y0[index+1] * y0[index+1] ) : 0;
                        n++;
                    }
                }
                else
                {
                    // sum up burntime of burn arcs
                    total_bt_bar += arcs[i].max_bt_bar;
                }
            }

            /* construct sum of the squares of the residuals for levenberg marquardt */
            for(int i = 0; i < z.Length; i++)
                z[i] = z[i] * z[i];

        }

        /* insert coast before the ith stage */
        private void InsertCoast(List<Arc> arcs, int i)
        {
            int i0_old = arcIndex(arcs, i, parameters: true);
            int i0_old_continuity = arcIndex(arcs, i, parameters: false);
            int n_old = y0.Length;
            arcs.Insert(i, new Arc(new Stage(this, m0: stages[i].m0, isp: 0, thrust: 0)));
            //Array.Copy(y0, 0, y0_new, 0, i0_old);
            //int i0_new_continuity = arcIndex(arcs, i, parameters: false);
            //Array.Copy(y0, i0_old_continuity, y0_new, i0_new_continuity, 13);
            //y0 = y0_new;
        }

        public void Bootstrap()
        {
            // build arcs off of ksp stages, with coasts
            List<Arc> arcs = new List<Arc>();
            for(int i = 0; i < stages.Count; i++)
            {
                //if (i != 0)
                    //arcs.Add(new Arc(new Stage(this, m0: stages[i].m0, isp: 0, thrust: 0)));
                arcs.Add(new Arc(stages[i]));
            }

            //arcs[arcs.Count-1].infinite = true;

            // allocate y0
            y0 = new double[arcIndex(arcs, arcs.Count)];

            // update initial position and guess for first arc
            double ve = g0 * stages[0].isp;
            tgo = ve * stages[0].m0 / stages[0].thrust * ( 1 - Math.Exp(-dV/ve) );
            tgo_bar = tgo / t_scale;

            // initialize coasts to zero
            /*
            for(int i = 0; i < arcs.Count; i++)
            {
                if (arcs[i].thrust == 0)
                {
                    int index = arcIndex(arcs, i, parameters: true);
                    y0[index] = 0;
                    y0[index+1] = 0;
                }
            }
            */

            // initialize overall burn time
            y0[0] = tgo_bar;
            y0[1] = 0;

            UpdateY0(arcs);

            // seed continuity initial conditions
            yf = new double[arcs.Count*13];
            multipleIntegrate(y0, yf, arcs, initialize: true);

            for(int j = 0; j < y0.Length; j++)
                Debug.Log("bootstrap - y0[" + j + "] = " + y0[j]);

            Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                Debug.Log("optimizer failed");
                y0 = null;
                return;
            }

            if (y0[0] < 0)
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                Debug.Log("optimizer failed2");
                y0 = null;
                return;
            }

            Debug.Log("optimizer done");

            Solution new_sol = new Solution(t_scale, v_scale, r_scale, 0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            //for(int i = arcs.Count-1; i > 0 ; i--)
                //InsertCoast(arcs, i);
            //InsertCoast(arcs, arcs.Count-1);

            /*
            Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                Debug.Log("optimizer failed");
                return;
            }

            if (y0[0] < 0)
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                Debug.Log("optimizer failed2");
                return;
            }

            Debug.Log("optimizer done");

            new_sol = new Solution(t_scale, v_scale, r_scale, 0);
            multipleIntegrate(y0, new_sol, arcs, 10);
            */

            for(int k = 0; k < y0.Length; k++)
                Debug.Log("new y0[" + k + "] = " + y0[k]);

            this.solution = new_sol;
            Debug.Log("done with bootstrap");
            last_arcs = arcs;
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
                        y0 = new double[arcIndex(last_arcs, last_arcs.Count-1)];
                        Array.Copy(y0_old, 0, y0, 0, arcIndex(last_arcs, last_arcs.Count-1));
                        last_arcs.RemoveAt(0);
                    }

                    UpdateY0(last_arcs);
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
