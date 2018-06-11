using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public class PontryaginNode : PontryaginBase {
        public PontryaginNode(double mu, Vector3d r0, Vector3d v0, Vector3d pv0, Vector3d pr0, double dV) : base(mu, r0, v0, pv0, pr0, dV)
        {
        }

        public override int arcIndex { get { return 4; } }

        public Orbit before;
        public Orbit after;
        public bool coasting = true;
        public double Ti_guess;  /* set to node.UT - est BT/2 and let the initial coast phase in the optimizer fix it */
        public double Tf_guess;  /* set to node.UT + est BT/2 and let the final coast phase in the optimizer fix it */

        public void intercept(Orbit before, Orbit after, bool coasting)
        {
            this.before = before;
            this.after = after;
            this.coasting = coasting;
            bcfun = intercept;
        }

        private void intercept(double[] yT, double[] z)
        {
            Vector3d rf;
            Vector3d vf;
            after.GetOrbitalStateVectorsAtUT(Tf_guess, out rf, out vf);

            z[0] = yT[0] - rf[0];
            z[1] = yT[1] - rf[1];
            z[2] = yT[2] - rf[2];
            z[3] = yT[3] - vf[0];
            z[4] = yT[4] - vf[1];
            z[5] = yT[5] - vf[2];
        }

        public override void optimizationFunction(double[] y0, double[] z, object o)
        {
            List<Arc> arcs = (List<Arc>)o;
            base.optimizationFunction(y0, z, o);

            Vector3d r1 = new Vector3d(yf[0], yf[1], yf[2]);
            Vector3d v1 = new Vector3d(yf[3], yf[4], yf[5]);
            Vector3d pv1 = new Vector3d(yf[6], yf[7], yf[8]);
            Vector3d pr1 = new Vector3d(yf[9], yf[10], yf[11]);
            double r1m = r1.magnitude;

            int w = 13 * ( arcs.Count - 2 );
            /* final minus one */
            Vector3d rfm1 = new Vector3d(yf[w+0], yf[w+1], yf[w+2]);
            Vector3d vfm1 = new Vector3d(yf[w+3], yf[w+4], yf[w+5]);
            Vector3d pvfm1 = new Vector3d(yf[w+6], yf[w+7], yf[w+8]);
            Vector3d prfm1 = new Vector3d(yf[w+9], yf[w+10], yf[w+11]);
            double rfm1m = rfm1.magnitude;

            /* magnitude of initial costate vector = 1.0 (dummy constraint for H(tf)=0 because BC is keplerian) */
            int n = 13 * arcs.Count;
            z[n] = 0.0;
            for(int i = 0; i < 6; i++)
                z[n] = z[n] + y0[6+i+arcIndex] * y0[6+i+arcIndex];
            z[n] = Math.Sqrt(z[n]) - 1.0;

            // optimization of the burntime
            z[n+1] = Vector3d.Dot(prfm1, vfm1) - Vector3d.Dot(pvfm1, rfm1) / (rfm1m * rfm1m * rfm1m );

            // positive arc time constraint
            z[n+2] = ( y0[1] < 0 ) ? ( y0[1] - y0[2] * y0[2] ) * 1000 : y0[2] * y0[2];

            /* H0t1 = 0 to optimize the time of the initial coast */
            if (coasting)
                z[n+3] = Vector3d.Dot(pr1, v1) - Vector3d.Dot(pv1, r1) / (r1m * r1m * r1m );
            else
                z[n+3] = y0[3];

            /* construct sum of the squares of the residuals for levenberg marquardt */
            for(int i = 0; i < z.Length; i++)
                z[i] = z[i] * z[i];
        }

        public void Bootstrap()
        {
            y0 = new double[13 + arcIndex];
            double ve = g0 * arcs[0].isp;
            Debug.Log("dV = " + dV);
            tgo = ve * arcs[0].m0 / arcs[0].thrust * ( 1 - Math.Exp(-dV/ve) );
            tgo_bar = tgo / t_scale;
            Debug.Log("tgo = " + tgo);
            Debug.Log("tgo_bar = " + tgo_bar);
            UpdateY0Arc(0);

            for(int i = 1; i < arcs.Count; i++)
            {
                for(int j = 0; j < y0.Length; j++)
                    Debug.Log("  " + i + " y0[" + j + "] = " + y0[j]);

                List<Arc> subarcs = arcs.GetRange(0, i);
                runOptimizer(subarcs);  // FIXME: check return value

                Solution sol = new Solution(t_scale, v_scale, r_scale, 0);

                for(int j = 0; j < y0.Length; j++)
                    Debug.Log(i + " y0[" + j + "] = " + y0[j]);

                multipleIntegrate(y0, sol, subarcs, 10);

                double[] y0_new = new double[13 * (i + 1) + arcIndex];
                Array.Copy(y0, 0, y0_new, 0, 13*i + arcIndex);
                y0 = y0_new;

                double t = arcs[i-1].max_bt_bar;
                Vector3d r = sol.r_bar(t);
                Vector3d v = sol.v_bar(t);
                Vector3d pv = sol.pv_bar(t);
                Vector3d pr = sol.pr_bar(t);
                double m = sol.m_bar(t);

                y0[arcIndex + 13 * i + 0] = r[0];
                y0[arcIndex + 13 * i + 1] = r[1];
                y0[arcIndex + 13 * i + 2] = r[2];
                y0[arcIndex + 13 * i + 3] = v[0];
                y0[arcIndex + 13 * i + 4] = v[1];
                y0[arcIndex + 13 * i + 5] = v[2];
                y0[arcIndex + 13 * i + 6] = pv[0];
                y0[arcIndex + 13 * i + 7] = pv[1];
                y0[arcIndex + 13 * i + 8] = pv[2];
                y0[arcIndex + 13 * i + 9] = pr[0];
                y0[arcIndex + 13 * i + 10] = pr[1];
                y0[arcIndex + 13 * i + 11] = pr[2];
                y0[arcIndex + 13 * i + 12] = m;
            }
        }

        public override void Optimize(double t0)
        {
            initializing = false;

            try {
                if ( y0 != null )
                {
                    if ( y0.Length > 13*arcs.Count + arcIndex )
                    {
                        /* probably normal staging, so just shrink */
                        double[] y0_old = y0;
                        y0 = new double[13*arcs.Count + arcIndex];
                        Array.Copy(y0_old, 0, y0, 0, 13*arcs.Count + arcIndex);
                    }
                    else if ( y0.Length != 13*arcs.Count + arcIndex )
                    {
                        y0 = null;
                    }
                }

                NormalizeArcs();

                if (y0 == null)
                {
                    initializing = true;
                    Bootstrap();
                }
                else
                {
                    UpdateY0Arc(0);
                }

                for(int i = 0; i < y0.Length; i++)
                    Debug.Log("n y0[" + i + "] = " + y0[i]);

                if ( runOptimizer(arcs) )
                {
                    if (y0[0] < 0)
                    {
                        y0 = null;
                        return;
                    }

                    Solution sol = new Solution(t_scale, v_scale, r_scale, t0);

                    for(int i = 0; i < y0.Length; i++)
                        Debug.Log("y0[" + i + "] = " + y0[i]);

                    multipleIntegrate(y0, sol, arcs, 10);

                    this.solution = sol;

                    Debug.Log("rf = " + sol.r_bar(sol.tmax()) + "(" + sol.r_bar(sol.tmax()).magnitude + ") vf = " + sol.v_bar(sol.tmax()) + "(" + sol.v_bar(sol.tmax()).magnitude + ")");
                    Debug.Log("rf = " + sol.r(sol.tf()) + "(" + sol.r(sol.tf()).magnitude + ") vf = " + sol.v(sol.tf()) + "(" + sol.v(sol.tf()).magnitude + ")");

                } else {
                    y0 = null;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        public override void SynchStages(List<int> kspstages, FuelFlowSimulation.Stats[] vacStats, double m0)
        {
            AddArc(type: ArcType.COAST, m0: m0, dt0: 0, isp: 0, thrust: 0); /* FIXME: dt */
            base.SynchStages(kspstages, vacStats, m0);
            AddArc(type: ArcType.COAST, m0: -1, dt0: 0, isp: 0, thrust: 0); /* FIXME: dt */
        }
    }
}
