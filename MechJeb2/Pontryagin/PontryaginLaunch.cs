using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MuMech {
    public class PontryaginLaunch : PontryaginBase {
        // FIXME: pr0 and pv0 need to be moved into the target constraint setting routines and need to not be the responsibility of the caller
        public PontryaginLaunch(MechJebCore core, double mu, Vector3d r0, Vector3d v0, Vector3d pv0, double dV) : base(core, mu, r0, v0, pv0, r0.normalized * 8.0/3.0, dV)
        {
        }

        public bool omitCoast;

        double rTm;
        double vTm;
        double gammaT;
        double incT;
        double smaT;
        double eccT;
        double LANT;
        double ArgPT;
        int numStages = 0;

        // 5-constraint PEG with fixed LAN
        public void flightangle5constraint(double rTm, double vTm, double gammaT, double incT, double LANT)
        {
            this.rTm = rTm / r_scale;
            this.vTm = vTm / v_scale;
            this.gammaT = gammaT;
            this.incT = incT;
            this.LANT = LANT;
            bcfun = flightangle5constraint;
        }

        private void flightangle5constraint(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);
            Vector3d hf = Vector3d.Cross(rf, vf);
            Vector3d hT = new Vector3d( Math.Sin(LANT) * Math.Sin(incT), -Math.Cos(LANT) * Math.Sin(incT), Math.Cos(incT) ) * rTm * vTm * Math.Cos(gammaT);

            hT = -hT.xzy;

            Vector3d hmiss = hf - hT;

            // 5 constraints
            if (!terminal)
            {
                z[0] = ( rf.magnitude * rf.magnitude - rTm * rTm ) / 2.0;
                z[1] = Vector3d.Dot(rf, vf) - rf.magnitude * vf.magnitude * Math.Sin(gammaT);
                z[2] = hmiss[0];
                z[3] = hmiss[1];
                z[4] = hmiss[2];

                // transversality - free argp
                z[5] = Vector3d.Dot(Vector3d.Cross(prf, rf) + Vector3d.Cross(pvf, vf), hT);
            }
            else
            {
                z[0] = hmiss.magnitude;
                z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        // 4-constraint PEG with free LAN
        public void flightangle4constraint(double rTm, double vTm, double gammaT, double incT)
        {
            this.rTm = rTm / r_scale;
            this.vTm = vTm / v_scale;
            this.gammaT = gammaT;
            this.incT = incT;
            bcfun = flightangle4constraint;
        }

        private void flightangle4constraint(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d n = new Vector3d(0, -1, 0);  /* angular momentum vectors point south in KSP and we're in xzy coords */
            Vector3d rn = Vector3d.Cross(rf, n);
            Vector3d vn = Vector3d.Cross(vf, n);
            Vector3d hf = Vector3d.Cross(rf, vf);

            if (!terminal)
            {
                z[0] = ( rf.magnitude * rf.magnitude - rTm * rTm ) / 2.0;
                z[1] = ( vf.magnitude * vf.magnitude - vTm * vTm ) / 2.0;
                z[2] = Vector3d.Dot(n, hf) - hf.magnitude * Math.Cos(incT);
                z[3] = Vector3d.Dot(rf, vf) - rf.magnitude * vf.magnitude * Math.Sin(gammaT);
                z[4] = rTm * rTm * ( Vector3d.Dot(vf, prf) - vTm * Math.Sin(gammaT) / rTm * Vector3d.Dot(rf, prf) ) -
                    vTm * vTm * ( Vector3d.Dot(rf, pvf) - rTm * Math.Sin(gammaT) / vTm * Vector3d.Dot(vf, pvf) );
                z[5] = Vector3d.Dot(hf, prf) * Vector3d.Dot(hf, rn) + Vector3d.Dot(hf, pvf) * Vector3d.Dot(hf, vn);
            }
            else
            {
                double hTm = rTm * vTm * Math.Cos(gammaT);

                z[0] = hf.magnitude - hTm;
                z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        public void keplerian3constraint(double sma, double ecc, double inc)
        {
            this.smaT = sma / r_scale;
            this.eccT = ecc;
            this.incT = inc;
            bcfun = keplerian3constraint;
        }

        // Ping Lu, "ASSESSMENT OF ADAPTIVE GUIDANCE FOR RE6PONSIVE LAUNCH VEHICLES AND SPACECRAFT", AFRL-RV-PSTR-2009-1023
        // https://pdfs.semanticscholar.org/72b3/589f49ad653813a1121fbabccf938df20b48.pdf
        // Section 9.1 Mode 31
        //
        // https://pdfs.semanticscholar.org/2836/c4200719146ccc9ca6c3cd239e6367dfddaa.pdf
        // Section 8.2 Mode 31
        //
        // Those algorithms failed to converge with any stability, so the transversality conditions for free LAN, ArgP and
        // attachment were added from:
        //
        // Beinfeng Pan, "Reduced Transversality Conditions in Optimal Space Trajectories"
        // https://www.researchgate.net/publication/260671510_Reduced_Transversality_Conditions_in_Optimal_Space_Trajectories
        //
        // In addition, dividing the inclination constraint by the angular momentum produced significantly better convergence.
        //
        private void keplerian3constraint(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d hf = Vector3d.Cross(rf, vf);
            Vector3d n = new Vector3d(0, -1, 0);  /* angular momentum vectors point south in KSP and we're in xzy coords */
            Vector3d rn = Vector3d.Cross(rf, n);
            Vector3d vn = Vector3d.Cross(vf, n);

            double hTm = Math.Sqrt( smaT * ( 1 - eccT * eccT ) );

            double smaf = 1.0 / ( 2.0 / rf.magnitude - vf.sqrMagnitude );
            double eccf = Math.Sqrt(1.0 - hf.sqrMagnitude / smaf);
            double rf3 = rf.magnitude * rf.sqrMagnitude;

            if (!terminal)
            {
                z[0] = hf.sqrMagnitude / 2.0 - hTm * hTm / 2.0; // angular momentum constraint
                z[1] = smaT * ( 1 - eccT ) - ( smaf * ( 1 - eccf ) ); // PeA constraint
                //z[1] = 1 / (2 * smaf) - 1 / (2 * smaT); // energy constraint
                z[2] = Vector3d.Dot(n, hf.normalized) - Math.Cos(incT); // inclination constraint
                // transversality
                z[3] = Vector3d.Dot(Vector3d.Cross(prf, rf) + Vector3d.Cross(pvf, vf), hf);
                z[4] = Vector3d.Dot(Vector3d.Cross(prf, rf) + Vector3d.Cross(pvf, vf), n);
                z[5] = Vector3d.Dot(prf, vf) - Vector3d.Dot(pvf, rf) / ( rf.magnitude * rf.magnitude * rf.magnitude );
                //z[3] = Vector3d.Dot(hf, prf) * Vector3d.Dot(hf, Vector3d.Cross(rf, n)) + Vector3d.Dot(hf, pvf) * Vector3d.Dot(hf, Vector3d.Cross(vf, n));
                //z[4] = rf3 * Vector3.Dot(vf, prf) - Vector3d.Dot(rf, pvf);
                //z[5] = Vector3d.Dot(prf, Vector3d.Cross(rf, vn)) * Vector3d.Dot(Vector3d.Cross(rf, hf), Vector3d.Cross(vf, rn)) +
                //    Vector3d.Dot(pvf, Vector3d.Cross(vf, rn)) * Vector3d.Dot(Vector3d.Cross(vf, hf), Vector3d.Cross(rf, vn));
            }
            else
            {
                z[0] = hf.magnitude - hTm;
                z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        // From https://convexoptimization.com/TOOLS/PeterHistos.pdf
        // 3.4.4 Boundary Conditions for Elliptical Orbits with a Fixed Inclination
        //
        private void keplerian3constraintBAD(double[] yT, double[] z, bool terminal)
        {
            //DebugLog("smaT = " + smaT + " / " + smaT * r_scale);
            //DebugLog("eccT = " + eccT);
            //DebugLog("incT = " + incT + " / " + incT * UtilMath.Rad2Deg);

            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d hf = Vector3d.Cross(rf, vf);
            Vector3d n = new Vector3d(0, -1, 0);  /* angular momentum vectors point south in KSP and we're in xzy coords */
            //Vector3d n = new Vector3d(0, 0, 1);

            Vector3d ir = rf.normalized;
            Vector3d iv = vf.normalized;
            Vector3d ih = hf.normalized;

            double hTm = Math.Sqrt( smaT * ( 1 - eccT * eccT ) );

            double smaf = 1.0 / ( 2.0 / rf.magnitude - vf.sqrMagnitude );
            double eccf = Math.Sqrt(1.0 - hf.sqrMagnitude / smaf);

            double ar = Vector3d.Dot(ih, Vector3d.Cross(n, vf));
            double av = Vector3d.Dot(ih, Vector3d.Cross(n, rf));

            double c2vs = 2 * smaf * smaf * vf.magnitude * eccf * ( 1 - eccf ) - smaf * hf.sqrMagnitude * vf.magnitude; // c2v-sqiggle
            double c2rs = 2 * smaf * smaf * eccf * ( 1 - eccf ) / rf.sqrMagnitude - smaf * hf.sqrMagnitude / rf.sqrMagnitude; // c2r-sqiggle

            double As = c2vs * Vector3d.Dot(iv, iv+ir) - Vector3d.Dot(Vector3d.Cross(rf, Vector3d.Cross(rf, vf)), iv + ir);
            double Bs = c2rs * Vector3d.Dot(ir, iv+ir) - Vector3d.Dot(Vector3d.Cross(vf, Vector3d.Cross(vf, rf)), iv + ir);

            // double A = As / eccf;

            double v2s = ( Vector3d.Dot(pvf, iv+ir) / rf.sqrMagnitude - vf.magnitude * Vector3d.Dot(prf, iv + ir) ) / ( As / rf.sqrMagnitude - vf.magnitude * Bs );
            double v3s = ( Vector3d.Dot(pvf, iv+ir) - v2s * As ) / ( Vector3d.Dot(ir, iv) + 1 );

            if (!terminal)
            {
                z[0] = smaT * ( 1 - eccT ) - ( smaf * ( 1 - eccf ) ); // PeA constraint
                z[1] = vf.sqrMagnitude / 2.0 - 1.0 / rf.magnitude + 1.0 / ( 2.0 * smaT ); // E constraint
                z[2] = Vector3d.Dot(n, ih) - Math.Cos(incT); // inc constraint
                // transversality
                z[3] = Vector3d.Dot(ar/av * pvf + prf, ih);
                z[4] = Vector3d.Dot(pvf, ir) - v2s * c2vs * Vector3d.Dot(iv, ir) - v3s * Vector3d.Dot(iv, ir);
                z[5] = Vector3d.Dot(prf, ir) - v2s * c2rs * Vector3d.Dot(iv, ir) - v3s / (vf.magnitude * rf.sqrMagnitude) * Vector3d.Dot(iv, ir);
            }
            else
            {
                z[0] = hf.magnitude - hTm;
                z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        public void keplerian4constraintArgPfree(double sma, double ecc, double inc, double LAN)
        {
            this.smaT = sma / r_scale;
            this.eccT = ecc;
            this.incT = inc;
            this.LANT = LAN;
            bcfun = keplerian4constraintArgPfree;
        }

        private void keplerian4constraintArgPfree(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d hf = Vector3d.Cross(rf, vf);

            Vector3d hT = new Vector3d( Math.Sin(LANT) * Math.Sin(incT), -Math.Cos(LANT) * Math.Sin(incT), Math.Cos(incT) ) * Math.Sqrt(smaT * (1 - eccT*eccT));
            hT = -hT.xzy; // left handed coordinate system
            Vector3d hmiss = hf - hT;

            double smaf = 1.0 / ( 2.0 / rf.magnitude - vf.sqrMagnitude );
            double eccf = Math.Sqrt(1.0 - hf.sqrMagnitude / smaf);

            if (!terminal)
            {
                z[0] = smaT * ( 1 - eccT ) - ( smaf * ( 1 - eccf ) ); // PeA constraint
                z[1] = hmiss[0];
                z[2] = hmiss[1];
                z[3] = hmiss[2];
                // transversality
                z[4] = Vector3d.Dot(Vector3d.Cross(prf, rf) + Vector3d.Cross(pvf, vf), hf);
                z[5] = Vector3d.Dot(prf, vf) - Vector3d.Dot(pvf, rf) / ( rf.magnitude * rf.magnitude * rf.magnitude );
            }
            else
            {
                z[0] = hmiss.magnitude;
                z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        public void keplerian4constraintLANfree(double sma, double ecc, double inc, double ArgP)
        {
            this.smaT = sma / r_scale;
            this.eccT = ecc;
            this.incT = inc;
            this.ArgPT = ArgP;
            bcfun = keplerian4constraintLANfree;
        }

        private void keplerian4constraintLANfree(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d hf = Vector3d.Cross(rf, vf);
            Vector3d n = new Vector3d(0, -1, 0); // angular momentum vectors point south in KSP and we're in xzy coords

            Vector3d eccf = Vector3d.Cross(vf, hf) - rf / rf.magnitude; // ecc vector
            double smaf = 1.0 / ( 2.0 / rf.magnitude - vf.sqrMagnitude );
            double hTm = Math.Sqrt( smaT * ( 1 - eccT * eccT ) );

            if (!terminal)
            {
                z[0] = smaT * ( 1 - eccT ) - ( smaf * ( 1 - eccf.magnitude ) ); // PeA constraint
                z[1] = vf.sqrMagnitude / 2.0 - 1.0 / rf.magnitude + 1.0 / ( 2.0 * smaT ); // E constraint
                z[2] = Vector3d.Dot(n, hf) - hf.magnitude * Math.Cos(incT);
                z[3] = Vector3d.Dot(eccf, Vector3d.Cross(n, hf)) / eccf.magnitude / hf.magnitude - Math.Sin(incT) * Math.Cos(ArgPT);
                z[4] = Vector3d.Dot(Vector3d.Cross(prf, rf) + Vector3d.Cross(pvf, vf), n);
                z[5] = Vector3d.Dot(prf, vf) - Vector3d.Dot(pvf, rf) / ( rf.magnitude * rf.magnitude * rf.magnitude );
            }
            else
            {
                z[0] = hf.magnitude - hTm;
                z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        public void keplerian5constraint(double sma, double ecc, double inc, double LAN, double ArgP)
        {
            this.smaT = sma / r_scale;
            this.eccT = ecc;
            this.incT = inc;
            this.LANT = LAN;
            this.ArgPT = ArgP;
            bcfun = keplerian5constraint;
        }

        private void keplerian5constraint(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d hT = new Vector3d( Math.Sin(LANT) * Math.Sin(incT), -Math.Cos(LANT) * Math.Sin(incT), Math.Cos(incT) ) * Math.Sqrt(smaT * (1 - eccT*eccT));
            hT = -hT.xzy; // left handed coordinate system
            // FIXME: Vector3d.cross(n, hf) is the node vector pointing in LAN dir, another ArgPT rot around hT would give eT direction
            //Vector3d eT = QuaternionD.AngleAxis(-ArgPT * UtilMath.Rad2Deg, hf) * -Vector3d.cross(n, hf);

            // This is the ZXZ intrinsic rotation, converting Vector3d.right (pointing at the periapsis) from the perifocal system.  Thus, the
            // order of applying the angles is backwards.  However, the negative sign for the rotation angle is cancelled out by AngleAxis being
            // a left-handed rotation, so the angles here are positive.  Argh.
            //
            Vector3d eT = QuaternionD.AngleAxis(LANT * UtilMath.Rad2Deg, Vector3d.forward) * QuaternionD.AngleAxis(incT * UtilMath.Rad2Deg, Vector3d.right) * QuaternionD.AngleAxis(ArgPT * UtilMath.Rad2Deg, Vector3d.forward) * Vector3d.right * eccT;
            eT = eT.xzy;

            if (Math.Abs(hT[1]) <= 1e-6) // handle 90 degree singularity
            {
                rf = rf.Reorder(231);
                vf = vf.Reorder(231);
                prf = prf.Reorder(231);
                pvf = pvf.Reorder(231);
                hT = hT.Reorder(231);
                eT = eT.Reorder(231);
            }

            Vector3d hf = Vector3d.Cross(rf, vf);
            Vector3d ef = Vector3d.Cross(vf, hf) - rf.normalized;
            Vector3d hmiss = hf - hT;
            Vector3d emiss = ef - eT;
            double trans = Vector3d.Dot(prf, vf) - Vector3d.Dot(pvf, rf) / ( rf.magnitude * rf.magnitude * rf.magnitude );

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

        public void flightangle3constraintMAXE(double rTm, double gammaT, double incT, int numStages)
        {
            this.rTm = rTm / r_scale;
            this.gammaT = gammaT;
            this.numStages = numStages;
            this.incT = incT;
            bcfun = flightangle3constraintMAXE;
        }

        // fixed-time maximal energy from https://doi.org/10.2514/2.5045
        private void flightangle3constraintMAXE(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d n = new Vector3d(0, -1, 0);  /* angular momentum vectors point south in KSP and we're in xzy coords */
            Vector3d hf = Vector3d.Cross(rf, vf);

            if (!terminal)
            {
                z[0] = ( rf.sqrMagnitude - rTm * rTm ) / 2.0;
                z[1] = Vector3d.Dot(n, hf) - hf.magnitude * Math.Cos(incT);
                z[2] = Vector3d.Dot(rf, vf) - rf.magnitude * vf.magnitude * Math.Sin(gammaT);

                z[3] = Vector3d.Dot(vf, prf) * rf.sqrMagnitude - Vector3d.Dot(rf, pvf) * vf.sqrMagnitude + Vector3d.Dot(rf, vf) * (vf.sqrMagnitude - Vector3d.Dot(rf, prf));
                z[4] = Vector3d.Dot(vf, pvf) - vf.sqrMagnitude;
                z[5] = Vector3d.Dot(hf, prf) * Vector3d.Dot(hf, Vector3d.Cross(rf, n)) + Vector3d.Dot(hf, pvf) * Vector3d.Dot(hf, Vector3d.Cross(vf, n));
            }
            else
            {
                z[0] = z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        public void flightangle4constraintMAXE(double rTm, double gammaT, double incT, double LANT, int numStages)
        {
            this.rTm = rTm / r_scale;
            this.gammaT = gammaT;
            this.incT = incT;
            this.LANT = LANT;
            this.numStages = numStages;
            bcfun = flightangle4constraintMAXE;
        }

        // fixed-time maximal energy from https://doi.org/10.2514/2.5045
        // XXX: this may only work for gammaT of zero?
        private void flightangle4constraintMAXE(double[] yT, double[] z, bool terminal)
        {
            Vector3d rf = new Vector3d(yT[0], yT[1], yT[2]);
            Vector3d vf = new Vector3d(yT[3], yT[4], yT[5]);
            Vector3d pvf = new Vector3d(yT[6], yT[7], yT[8]);
            Vector3d prf = new Vector3d(yT[9], yT[10], yT[11]);

            Vector3d n = new Vector3d(0, -1, 0);  /* angular momentum vectors point south in KSP and we're in xzy coords */
            Vector3d hf = Vector3d.Cross(rf, vf);

            // angular momentum direction unit vector
            Vector3d i_hT = new Vector3d( Math.Sin(LANT) * Math.Sin(incT), -Math.Cos(LANT) * Math.Sin(incT), Math.Cos(incT) );

            if (!terminal)
            {
                z[0] = ( rf.sqrMagnitude - rTm * rTm ) / 2.0;
                z[1] = Vector3d.Dot(n, hf) - hf.magnitude * Math.Cos(incT);
                z[2] = Vector3d.Dot(rf, i_hT);
                z[3] = Vector3d.Dot(vf, i_hT);

                z[4] = Vector3d.Dot(vf, prf) * rf.sqrMagnitude - Vector3d.Dot(rf, pvf) * vf.sqrMagnitude;
                z[5] = Vector3d.Dot(vf, pvf) - vf.sqrMagnitude;
            }
            else
            {
                z[0] = z[1] = z[2] = z[3] = z[4] = z[5] = 0.0;
            }
        }

        public override void Bootstrap(double t0)
        {
            int stageCount = numStages > 0 ? numStages : stages.Count;

            // build arcs off of ksp stages, with coasts
            List<Arc> arcs = new List<Arc>();
            for(int i = 0; i < stageCount; i++)
            {
                arcs.Add(new Arc(this, stage: stages[i], t0: t0));
            }

            // bootstrap with infinite ISP upper stage
            arcs[arcs.Count-1].infinite = true;

            // allocate y0
            y0 = new double[arcIndex(arcs, arcs.Count)];

            // update initial position and guess for first arc
            double ve = g0 * stages[0].isp;
            tgo = ve * stages[0].startMass / stages[0].startThrust * ( 1 - Math.Exp(-dV/ve) );
            tgo_bar = tgo / t_scale;

            // initialize overall burn time
            y0[0] = tgo_bar;

            UpdateY0(arcs);

            // seed continuity initial conditions
            yf = new double[arcs.Count*13];
            multipleIntegrate(y0, yf, arcs, initialize: true);


            if ( !runOptimizer(arcs) )
            {
                Fatal("Target is unreachable even with infinite ISP");
                return;
            }

            Solution new_sol = new Solution(t_scale, v_scale, r_scale, t0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            for(int i = arcs.Count - 1; i >= 0; i--)
            {
                if ( new_sol.tgo(new_sol.t0, i) < 1 )
                {
                    // burn is less than one second, delete it
                    RemoveArc(arcs, i, new_sol);
                }
            }

            bool insertedCoast = false;

            if (arcs.Count > 1 && !omitCoast)
            {
                InsertCoast(arcs, arcs.Count-1, new_sol);
                insertedCoast = true;
            }
            arcs[arcs.Count-1].infinite = false;

            // now that we're done with the infinite burn make sure the total burn time of the guess doesn't exceed the rocket.
            double tot_bt_bar = 0.0;

            for(int i = 0; i < arcs.Count; i++)
            {
                if (!arcs[i].coast)
                {
                    tot_bt_bar += arcs[i].max_bt_bar;
                }
            }

            y0[0] = Math.Min(tot_bt_bar, y0[0]);

            if ( !runOptimizer(arcs) )
            {
                if ( insertedCoast )
                {
                    DebugLog("failed to converge with a coast, removing from the solution");

                    RemoveArc(arcs, arcs.Count-2, new_sol);
                    insertedCoast = false;

                    if ( !runOptimizer(arcs) )
                    {
                        Fatal("Target is unreachable");
                        y0 = null;
                        return;
                    }

                }
                else
                {
                    Fatal("Target is unreachable");
                    y0 = null;
                    return;
                }
            }

            if (insertedCoast)
            {
                new_sol = new Solution(t_scale, v_scale, r_scale, t0);
                multipleIntegrate(y0, new_sol, arcs, 10);

                double coastlen = new_sol.tgo(new_sol.t0, arcs.Count-2); // human seconds
                double coast_time = y0[arcIndex(arcs, arcs.Count-2, parameters: true)]; // normalized units
                double max_coast_time = Math.PI / 5.0; // 36 degrees

                if ( coastlen < 1 )
                {
                    DebugLog("optimum coast of " + coastlen + " seconds was removed from the solution");

                    RemoveArc(arcs, arcs.Count-2, new_sol);

                    if ( !runOptimizer(arcs) )
                    {
                        Fatal("Optimizer exploded after removing negative coast (weird)");
                        return;
                    }
                }
                else if ( coast_time > max_coast_time )
                {
                    DebugLog("optimium coast exceeded maximum normalized time (roughly 1/4 of an arc around the planet) and was truncated.");
                    arcs[arcs.Count-2].use_fixed_time2 = true;
                    arcs[arcs.Count-2].fixed_time = max_coast_time * t_scale;
                    arcs[arcs.Count-2].fixed_tbar = max_coast_time;
                    y0[arcIndex(arcs, arcs.Count-2, parameters: true)] = max_coast_time;
                    multipleIntegrate(y0, yf, arcs, initialize: true);
                    if ( !runOptimizer(arcs) )
                    {
                        Fatal("Optimizer exploded after truncating long coast (weird)");
                        y0 = null;
                        return;
                    }
                }
            }

            new_sol = new Solution(t_scale, v_scale, r_scale, t0);
            multipleIntegrate(y0, new_sol, arcs, 10);

            this.solution = new_sol;

            yf = new double[arcs.Count*13];
            multipleIntegrate(y0, yf, arcs);
        }

        /*
        public override void Optimize(double t0)
        {
            base.Optimize(t0);
        }
        */
    }
}
