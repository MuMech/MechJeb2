using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public class PontryaginNode : PontryaginBase {
        public PontryaginNode(double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV, double bt) : base(mu, r0, v0, pv0, pr0, dV)
        {
            tc1 = bt / 2.0;
            tgo = bt;
            tc2 = 0;
            tc1_bar = tc1 / t_scale;
            tgo_bar = tgo / t_scale;
            tc2_bar = tc2 / t_scale;
        }

        private Orbit target;
        private double tc1, tc1_bar;
        private double tc2, tc2_bar;
        private double Tf;

        public void intercept(Orbit target)
        {
            this.target = target;
            bcfun = intercept;
        }

        private void intercept(double[] yT, double[] z)
        {
            Vector3d rf;
            Vector3d vf;
            target.GetOrbitalStateVectorsAtUT(Tf, out rf, out vf);
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            rf = rot * (rf.xzy / r_scale);
            vf = rot * (vf.xzy / v_scale);

            z[0] = yT[0] - rf[0];
            z[1] = yT[1] - rf[1];
            z[2] = yT[2] - rf[2];
            z[3] = yT[3] - vf[0];
            z[4] = yT[4] - vf[1];
            z[5] = yT[5] - vf[2];
        }

        public override void Bootstrap(double t0)
        {
            // set the final time guess
            Tf = t0 + tgo * 3.0 / 2.0;

            // build arcs off of ksp stages, with coasts
            List<Arc> arcs = new List<Arc>();

            arcs.Add(new Arc(new Stage(this, m0: stages[0].m0, isp: 0, thrust: 0, ksp_stage: stages[0].ksp_stage)));

            for(int i = 0; i < stages.Count; i++)
            {
                arcs.Add(new Arc(stages[i]));
            }

            arcs.Add(new Arc(new Stage(this, m0: stages[stages.Count-1].m0, isp: 0, thrust: 0, ksp_stage: stages[stages.Count-1].ksp_stage)));

            arcs[arcs.Count-1].infinite = true;
            arcs[arcs.Count-1].allow_negative_coast = true;

            // allocate y0
            y0 = new double[arcIndex(arcs, arcs.Count)];

            // initialize overall burn time (FIXME: duplicates code with UpdateY0)
            y0[0] = tgo_bar;
            y0[1] = 0;

            UpdateY0(arcs);

            // add guesses for coast burntimes
            y0[arcIndex(arcs, 0, parameters: true)] = tc1_bar;
            y0[arcIndex(arcs, 0, parameters: true)+1] = 0;
            y0[arcIndex(arcs, arcs.Count-1, parameters: true)] = tc2_bar;


            // seed continuity initial conditions
            yf = new double[arcs.Count*13];
            multipleIntegrate(y0, yf, arcs, initialize: true);

            for(int j = 0; j < y0.Length; j++)
                Debug.Log("bootstrap - y0[" + j + "] = " + y0[j]);
            //Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                //Debug.Log("optimizer failed");
                y0 = null;
                return;
            }

            if (y0[0] < 0)
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                //Debug.Log("optimizer failed2");
                y0 = null;
                return;
            }

            //Debug.Log("optimizer done");

            Solution new_sol = new Solution(t_scale, v_scale, r_scale, t0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            //Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                //Debug.Log("optimizer failed");
                y0 = null;
                return;
            }

            if (y0[0] < 0)
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                //Debug.Log("optimizer failed2");
                y0 = null;
                return;
            }

            //Debug.Log("optimizer done");

            new_sol = new Solution(t_scale, v_scale, r_scale, t0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            arcs[arcs.Count-1].infinite = false;

            //Debug.Log("running optimizer");

            if ( !runOptimizer(arcs) )
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                //Debug.Log("optimizer failed");
                y0 = null;
                return;
            }

            if (y0[0] < 0)
            {
                for(int k = 0; k < y0.Length; k++)
                    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                //Debug.Log("optimizer failed2");
                y0 = null;
                return;
            }

            //Debug.Log("optimizer done");

            new_sol = new Solution(t_scale, v_scale, r_scale, t0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            for(int k = 0; k < y0.Length; k++)
                Debug.Log("new y0[" + k + "] = " + y0[k]);

            this.solution = new_sol;
            //Debug.Log("done with bootstrap");
        }
    }
}
