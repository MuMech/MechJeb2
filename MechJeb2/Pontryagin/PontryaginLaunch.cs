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

            /* magnitude of terminal costate vector = 1.0 (dummy constraint for H(tf)=0 to optimize burntime because BC is keplerian) */
            int n = 13 * arcs.Count;
            z[n] = 0.0;
            for(int i = 0; i < 6; i++)
                z[n] = z[n] + yf[i+6+13*(arcs.Count-1)] * yf[i+6+13*(arcs.Count-1)];
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
                        /* H0 at the start of the coast = 0 */
                        Vector3d r = new Vector3d(y0[index+2], y0[index+3], y0[index+4]);
                        Vector3d v = new Vector3d(y0[index+5], y0[index+6], y0[index+7]);
                        Vector3d pv = new Vector3d(y0[index+8], y0[index+9], y0[index+10]);
                        Vector3d pr = new Vector3d(y0[index+11], y0[index+12], y0[index+13]);
                        double rm = r.magnitude;

                        z[n] = Vector3d.Dot(pr, v) - Vector3d.Dot(pv, r) / (rm * rm * rm);
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
        private void InsertCoast(List<Arc> arcs, int i, Solution sol)
        {
            int bottom = arcIndex(arcs, i, parameters: true);

            if ( arcs[i].thrust == 0 )
                throw new Exception("adding a coast before a coast");

            arcs.Insert(i, new Arc(new Stage(this, m0: arcs[i].m0, isp: 0, thrust: 0, ksp_stage: arcs[i].ksp_stage)));
            double[] y0_new = new double[arcIndex(arcs, arcs.Count)];

            double tmin = sol.segments[i].tmin;
            double dt = sol.segments[i].tmax - tmin;

            // copy all the lower arcs
            Array.Copy(y0, 0, y0_new, 0, bottom);
            // initialize the coast
            y0_new[bottom] = dt/2.0;
            y0_new[bottom+1] = 0.0;
            // subtract half the burn time of the upper stage
            y0_new[0] = y0[0] - dt/2.0;

            // FIXME: copy the rest of the parameters for upper stage coasts
            y0 = y0_new;
            yf = new double[arcs.Count*13];  /* somewhat confusingly y0 contains the state, costate and parameters, while yf omits the parameters */
            multipleIntegrate(y0, yf, arcs, initialize: true);
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

            arcs[arcs.Count-1].infinite = true;

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

            /*
            for(int j = 0; j < y0.Length; j++)
                Debug.Log("bootstrap - y0[" + j + "] = " + y0[j]);
                */

            //Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                /*
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                    */
                //Debug.Log("optimizer failed");
                y0 = null;
                return;
            }

            if (y0[0] < 0)
            {
                /*
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                    */
                //Debug.Log("optimizer failed2");
                y0 = null;
                return;
            }

            //Debug.Log("optimizer done");

            Solution new_sol = new Solution(t_scale, v_scale, r_scale, 0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            //for(int i = arcs.Count-1; i > 0 ; i--)
                //InsertCoast(arcs, i);
            InsertCoast(arcs, arcs.Count-1, new_sol);

            //Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                /*
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                    */
                //Debug.Log("optimizer failed");
                y0 = null;
                return;
            }

            if (y0[0] < 0)
            {
                /*
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                    */
                //Debug.Log("optimizer failed2");
                y0 = null;
                return;
            }

            //Debug.Log("optimizer done");

            new_sol = new Solution(t_scale, v_scale, r_scale, 0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            arcs[arcs.Count-1].infinite = false;

            //Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                /*
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                    */
                //Debug.Log("optimizer failed");
                y0 = null;
                return;
            }

            if (y0[0] < 0)
            {
                /*
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                    */
                //Debug.Log("optimizer failed2");
                y0 = null;
                return;
            }

            //Debug.Log("optimizer done");

            new_sol = new Solution(t_scale, v_scale, r_scale, 0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            /*
            for(int k = 0; k < y0.Length; k++)
                Debug.Log("new y0[" + k + "] = " + y0[k]);
                */

            this.solution = new_sol;
            //Debug.Log("done with bootstrap");
        }

        public override void Optimize(double t0)
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
                    while(last_arcs[0].done)
                    {
                        // FIXME: if we fail, then we'll re-shrink y0 again and again
                        Debug.Log("shrinking y0 array");
                        double[] y0_old = y0;
                        y0 = new double[arcIndex(last_arcs, last_arcs.Count-1)];
                        // copy the 2 burntime parameters
                        Array.Copy(y0_old, 0, y0, 0, 2);
                        // copy the upper N-1 arcs
                        int start = arcIndex(last_arcs, 1, parameters: true);
                        int end = y0_old.Length;
                        Array.Copy(y0_old, start, y0, 2, end - start);
                        last_arcs.RemoveAt(0);
                    }

                    if (last_arcs.Count == 0)
                    {
                        // something in staging went nuts, so re-bootstrap
                        y0 = null;
                        Bootstrap();
                        return;
                    }


                    UpdateY0(last_arcs);
                    //Debug.Log("normal optimizer run start");

                    if ( !runOptimizer(last_arcs) )
                    {
                        //Debug.Log("optimizer failed3");
                        y0 = null;
                        return;
                    }

                    if (y0[0] < 0)
                    {
                        //Debug.Log("optimizer failed4");
                        y0 = null;
                        return;
                    }

                    Solution sol = new Solution(t_scale, v_scale, r_scale, t0);

                    for(int i = 0; i < y0.Length; i++)
                        Debug.Log("y0[" + i + "] = " + y0[i]);

                    multipleIntegrate(y0, sol, last_arcs, 10);

                    this.solution = sol;

                    //Debug.Log("Optimize done");
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }
}
