using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public class PontryaginNode : PontryaginBase {
        public PontryaginNode(MechJebCore core, double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV, double bt) : base(core, mu, r0, v0, pv0, pr0, dV)
        {
            tc1 = bt / 2.0;
            tgo = bt;
            tc2 = 0;
            tc1_bar = tc1 / t_scale;
            tgo_bar = tgo / t_scale;
            tc2_bar = tc2 / t_scale;

            fixed_final_time = true;
        }

        private Orbit target;
        private readonly double tc1, tc1_bar;
        private readonly double tc2, tc2_bar;
        private double Tf;
        private Vector3d rT;
        private Vector3d vT;

        private bool initialized = false;

        public void intercept(Orbit target)
        {
            this.target = target;
            if (!initialized)
            {
                bcfun = intercept; // terminal5constraint;
                initialized = true;
            }
        }

        private void intercept(double[] yT, double[] z, bool terminal)
        {
            z[0] = yT[0] - rT[0];
            z[1] = yT[1] - rT[1];
            z[2] = yT[2] - rT[2];
            z[3] = yT[3] - vT[0];
            z[4] = yT[4] - vT[1];
            z[5] = yT[5] - vT[2];
            if ( terminal )
            {
                throw new Exception("need to fix this");
            }

            // no transversality for 6-constraint intercept
        }

        private void terminal5constraint(double[] yT, double[] z, bool terminal)
        {
            var rTp = rT;
            var vTp = vT;
            var rf = new Vector3d(yT[0], yT[1], yT[2]);
            var vf = new Vector3d(yT[3], yT[4], yT[5]);
            var pvf = new Vector3d(yT[6], yT[7], yT[8]);
            var prf = new Vector3d(yT[9], yT[10], yT[11]);

            var hT = Vector3d.Cross(rTp, vTp);

            if (Math.Abs(hT[1]) <= 1e-6)
            {
                // FIXME: this needs to go to temporary variables or needs to unfuck itself afterwards so that rf/vf can be updated as they're external
                rTp = rTp.Reorder(231);
                vTp = vTp.Reorder(231);
                rf = rf.Reorder(231);
                vf = vf.Reorder(231);
                prf = prf.Reorder(231);
                pvf = pvf.Reorder(231);
                hT = Vector3d.Cross(rTp, vTp);
            }

            var hf = Vector3d.Cross(rf, vf);
            var eT = - ( rTp.normalized + Vector3d.Cross(hT, vTp) );
            var ef = - ( rf.normalized + Vector3d.Cross(hf, vf) );
            var hmiss = hf - hT;
            var emiss = ef - eT;
            var trans = Vector3d.Dot(prf, vf) - (Vector3d.Dot(pvf, rf) / ( rf.magnitude * rf.magnitude * rf.magnitude ));

            if (!terminal)
            {
                z[0] = hmiss[0];
                z[1] = hmiss[1];
                z[2] = hmiss[2];
                z[3] = emiss[0];
                z[4] = emiss[2];
                z[5] = trans;
            }
            else
            {
                z[0] = hmiss.magnitude;
                z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        protected override void Bootstrap(double t0)
        {
            // set the final time guess
            Tf = t0 + (tgo * 3.0 / 2.0);

            target.GetOrbitalStateVectorsAtUT(Tf, out var rf, out var vf);
            QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
            rT = rot * (rf.xzy / r_scale);
            vT = rot * (vf.xzy / v_scale);

            // build arcs off of ksp stages, with coasts
            var arcs = new ArcList();

            arcs.Add(new Arc(this, t0: t0, coast: true));

            for(var i = 0; i < stages.Count; i++)
            {
                arcs.Add(new Arc(this, stage: stages[i], t0: t0));
            }

            arcs.Add(new Arc(this, coast: true, t0: t0));
            // arcs.Add(new Arc(new Stage(this, m0: -1, isp: 0, thrust: 0, ksp_stage: stages[stages.Count-1].ksp_stage), done: true));

            arcs[arcs.Count-1].use_fixed_time = true;
            arcs[arcs.Count-1].fixed_time = Tf;
            arcs[arcs.Count-1].fixed_tbar = ( Tf - t0 ) / t_scale;

            // allocate y0
            y0 = new double[arcIndex(arcs, arcs.Count)];

            // initialize overall burn time (FIXME: duplicates code with UpdateY0)
            y0[0] = tgo_bar;
            y0[1] = 0;

            UpdateY0(arcs);

            // add guesses for coast burntimes
            y0[arcIndex(arcs, 0, parameters: true)] = tc1_bar;
            y0[arcIndex(arcs, 0, parameters: true)+1] = 0;

            // seed continuity initial conditions
            yf = new double[arcs.Count*13];
            multipleIntegrate(y0, yf, arcs, initialize: true);

            var y0_saved = new double[y0.Length];
            Array.Copy(y0, y0_saved, y0.Length);

            //for(int j = 0; j < y0.Length; j++)
            //    Debug.Log("bootstrap - y0[" + j + "] = " + y0[j]);
            //Debug.Log("running optimizer");

            var success = runOptimizer(arcs);

            if ( !success )
            {
                //for(int k = 0; k < y0.Length; k++)
                //    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                //Debug.Log("optimizer failed12");
                y0 = null;
                return;
            }

            var new_sol = new Solution(t_scale, v_scale, r_scale, t0);
            multipleIntegrate(y0, new_sol, arcs);

            //Debug.Log("optimizer done");
            if ( new_sol.tgo(new_sol.t0, 0) < 0 )
            {
                // coast is less than zero, delete it and reconverge
                RemoveArc(arcs, 0, new_sol);
                UpdateY0(arcs); // reset to current starting position
                multipleIntegrate(y0, yf, arcs, initialize: true);
                //Debug.Log("running optimizer4");
                bcfun = terminal5constraint;

                if ( !runOptimizer(arcs) )
                {
                    //for(int k = 0; k < y0.Length; k++)
                    //    Debug.Log("failed - y0[" + k + "] = " + y0[k]);
                    //Debug.Log("optimizer failed14");
                    y0 = null;
                    return;
                }

                //Debug.Log("optimizer done");
                new_sol = new Solution(t_scale, v_scale, r_scale, t0);
                multipleIntegrate(y0, new_sol, arcs);
            }

            //for(int k = 0; k < y0.Length; k++)
                //Debug.Log("new y0[" + k + "] = " + y0[k]);

            this.Solution = new_sol;
            //Debug.Log("done with bootstrap");
        }
    }
}
