using System;
using UnityEngine;
using System.Collections.Generic;

/*
 * Atlas/Centaur-style PEG launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPEG : MechJebModuleAscentBase
    {
        public MechJebModuleAscentPEG(MechJebCore core) : base(core) { }

        /* default pitch program here works decently at SLT of about 1.4 */
        [Persistent(pass = (int)(Pass.Type))]
        public EditableDoubleMult pitchStartTime = new EditableDoubleMult(10);
        [Persistent(pass = (int)(Pass.Type))]
        public EditableDoubleMult pitchRate = new EditableDoubleMult(0.75);
        [Persistent(pass = (int)(Pass.Type))]
        public EditableDoubleMult pitchEndTime = new EditableDoubleMult(55);
        [Persistent(pass = (int)(Pass.Global))]
        public EditableDoubleMult desiredApoapsis = new EditableDoubleMult(100000, -1);
        [Persistent(pass = (int)(Pass.Type))]
        public EditableDoubleMult terminalGuidanceSecs = new EditableDoubleMult(10);
        [Persistent(pass = (int)(Pass.Type))]
        public EditableDoubleMult stageLowDVLimit = new EditableDoubleMult(20);
        [Persistent(pass = (int)(Pass.Type))]
        public EditableInt edit_num_stages = new EditableInt(2);
        private int last_edit_num_stages; // need this to track if the user updated the edit_num_stages value
        public int num_stages;            // need this so that PEG can decreate the num_stages independently of edit_num_stages

        /* this deliberately does not persist, it is for emergencies only */
        public EditableDoubleMult pitchBias = new EditableDoubleMult(0);

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        public override void OnModuleEnabled()
        {
            SetNumStages(edit_num_stages);
            last_edit_num_stages = edit_num_stages;
            mode = AscentMode.VERTICAL_ASCENT;
            InitStageStats();
            needsConverge = true;
        }

        public override void OnModuleDisabled()
        {
            stages.Clear();
        }

        private enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, EXIT };
        private AscentMode mode;

        /* guidancePitchAngle -- output from the guidance algorithm, not 'manual' pitch */
        public double guidancePitch { get { return Math.Asin(stages[0].A + stages[0].G / stages[0].a0 ) * UtilMath.Rad2Deg; } }

        public bool guidanceEnabled = true;
        public int convergenceSteps { get; private set; }

        /* saneGuidance tracks the state of the last iteration and if it was sane or not */
        private bool saneGuidance = false;
        /* terminal guidance is set when we're in terminal guidance phase */
        public bool terminalGuidance = false;
        /* needsConverge is set when we need to re-converge again */
        public bool needsConverge = true;

        /* clears state so that we fully reconverge next time */
        public void ResetBooleans() {
            saneGuidance = false;
            terminalGuidance = false;
            needsConverge = true;
        }

        /* tangential velocity at burnout */
        private double v_burnout;
        /* radius at burnout */
        private double r_burnout;
        /* gravParameter */
        private double GM;
        /* ending radial velocity */
        private double rd_burnout;
        /* angular velocity at burnout */
        private double w_burnout;
        /* angular momentum at burnout */
        private double h_burnout;
        /* G at burnout */
        private double G_burnout;
        /* specific orbital energy at burnout */
        private double eT;
        /* average change in the angle of thrust over the whole burn */
        private double fd_r;

        public double total_T;
        public double total_dV;

        /*
         * handle tracking ksp/mechjeb stage information and keep state about stages
         * (this is mildly awful because of everything that can dynamically happen to
         * stages in flight, including players manually rearranging stages).
         */

        public List<StageInfo> stages = new List<StageInfo>();

        private void SetNumStages(int n)
        {
            num_stages = n;
            InitConstantCache();
        }

        private void DirtyCacheForStage(int snum)
        {
            if (snum >= stages.Count)
                return;

            if (stages[snum].b_dirty == null)
            {
                Debug.Log("[MechJeb] MechJebModuleAscentPEG DirtyCacheForStage() called when b_dirty is null");
                return;
            }

            if (stages[snum].c_dirty == null)
            {
                Debug.Log("[MechJeb] MechJebModuleAscentPEG DirtyCacheForStage() called when c_dirty is null");
                return;
            }

            for(int i = 0; i <= num_stages; i++) {
                stages[snum].b_dirty[i] = stages[snum].c_dirty[i] = true;
            }
        }

        private void InitConstantCache()
        {
            for(int i = 0; i < num_stages && i < stages.Count; i++) {
                stages[i].b = new double[num_stages+1];
                stages[i].c = new double[num_stages+1];
                stages[i].b_dirty = new bool[num_stages+1];
                stages[i].c_dirty = new bool[num_stages+1];
                DirtyCacheForStage(i);
            }
        }

        public class StageInfo
        {
            /* updated directly from vehicle stats */
            public double v_e;       /* current/initial exhaust velocity */
            public double tau;
            public double a0;        /* current/initial acceleration */
            public double avail_dV;  /* stage stat from MJ */
            public double avail_T;   /* stage stat from MJ */

            public double dV; /* lower stages are == avail_dV, upper stage needs estimation */
            public double T;  /* lower stages are == avail_T, upper stage needs estimation */

            /* constant cache */
            public double[] b;
            public double[] c;
            public bool[] b_dirty;
            public bool[] c_dirty;

            /* steering constants */
            public double A;
            public double B;
            public double dA;
            public double dB;

            /* output from estimation */
            public double G;         /* current G */
            public double GT;        /* final G */
            public double aT;        /* final acceleration */

            /* input */
            public double r;         /* current radius */
            public double rd;        /* initial upwards velocity */
            public double h;         /* current angular momentum */

            /* output deltas */
            public double dr;        /* delta r */
            public double drd;       /* delta rdot */
            public double dh;        /* delta h */

            /* output estimates */
            public double rT;        /* ending radius */
            public double rdT;       /* final upwards velocity */
            public double hT;        /* ending angular momentum */

            /* steering */
            public double f_r;       /* current/initial steering sin angle */
            public double f_rT;      /* ending steering sin angle */

            /* misc */
            public List<Part> parts;
            public int kspStage;
            public override string ToString()
            {
                return "A = " + A + "\n" +
                       "B = " + B + "\n" +
                       "dA = " + dA + "\n" +
                       "dB = " + dB + "\n" +
                       "G = " + G + "\n" +
                       "GT = " + GT + "\n" +
                       "T = " + T + "\n" +
                       "dV = " + dV + "\n" +
                       "a0 = " + a0 + "\n" +
                       "aT = " + aT + "\n" +
                       "v_e = " + v_e + "\n" +
                       "tau = " + tau + "\n" +
                       "r = " + r + "\n" +
                       "dr = " + dr + "\n" +
                       "rT = " + rT + "\n" +
                       "rd = " + rd + "\n" +
                       "drd = " + drd + "\n" +
                       "rdT = " + rdT + "\n" +
                       "h = " + h + "\n" +
                       "dh = " + dh + "\n" +
                       "hT = " + hT + "\n" +
                       "f_r = " + f_r + "\n" +
                       "f_rT = " + f_rT + "\n";
            }
        }

        void UpdateStageFromMechJeb(StageInfo stage, bool atmo)
        {
            /* stage.kspStage must be corrected before calling this */
            int s = stage.kspStage;
            FuelFlowSimulation.Stats[] mjstats = atmo ? atmoStats : vacStats;

            stage.avail_dV = mjstats[s].deltaV;
            stage.avail_T = mjstats[s].deltaTime;
            stage.v_e = mjstats[s].isp * 9.80665;
            stage.a0 = mjstats[s].startThrust / mjstats[s].startMass;
            stage.tau = stage.v_e / stage.a0;
        }

        public List<Part> skippedParts = new List<Part>();

        public void InitStageStats()
        {
            Debug.Log("MechJebModuleAscentPEG.InitStageStats()");
            terminalGuidance = false;
            stages.Clear();
            skippedParts.Clear();
            for ( int i = vacStats.Length-1; i >= 0; i-- )
            {
                if ( vacStats[i].deltaV > stageLowDVLimit )
                {
                    StageInfo stage = new StageInfo();
                    stage.parts = vacStats[i].parts;
                    stage.kspStage = i;
                    UpdateStageFromMechJeb(stage, i == 0);
                    stages.Add( stage );
                }
                else
                {
                    skippedParts.AddRange( vacStats[i].parts );
                }
            }
            /* sometimes parts we want also wind up in zero dV stages and we need to remove them here */
            for ( int i = 0; i < stages.Count; i++ ) {
                for ( int j = 0; j < stages[i].parts.Count; j++ ) {
                    if ( skippedParts.Contains(stages[i].parts[j]) )
                        skippedParts.Remove(stages[i].parts[j]);
                }
            }
            InitConstantCache();
            ResetBooleans();
        }

        bool PartsListsMatch(List<Part> one, List<Part> two)
        {
            for(int i = 0; i < one.Count; i++)
            {
                /* skip burned sepratrons that wind up in the stage, etc */
                if ( skippedParts.Contains(one[i]) )
                    continue;

                if ( !two.Contains(one[i]) )
                    return false;
            }
            for(int i = 0; i < two.Count; i++)
            {
                if ( skippedParts.Contains(two[i]) )
                    continue;

                if ( !one.Contains(two[i]) )
                    return false;
            }
            return true;
        }

        int FixKSPStage(int oldstage, List<Part> parts)
        {
            if (oldstage < vacStats.Length && PartsListsMatch(vacStats[oldstage].parts, parts))
            {
                return oldstage;
            }

            for( int i = 0; i < vacStats.Length; i++ )
            {
                if (PartsListsMatch(vacStats[i].parts, parts))
                    return i;
            }
            return -1;
        }

        void UpdateStageStats()
        {
            if ( stages.Count == 0 )
                InitStageStats();

            for ( int i = 0; i < stages.Count; i++ )
            {
                StageInfo stage = stages[i];

                stage.kspStage = FixKSPStage(stage.kspStage, stage.parts);

                if ( stage.kspStage >= 0 )
                {
                    UpdateStageFromMechJeb(stage, i == 0);
                }
            }
            for ( int i = stages.Count - 1; i >= 0; i-- )
            {
                if ( stages[i].kspStage < 0 ) {
                    /* if someone runs a booster program though the full boster we don't consume a PEG stage */
                    /* also if PEG is disabled manually we don't consume stages */
                    if ( mode == AscentMode.GRAVITY_TURN && guidanceEnabled )
                        SetNumStages( Math.Max(1, num_stages - 1) );
                    stages.RemoveAt(i);
                    InitConstantCache();
                }
            }
            // dirty the constant cache for the bottom stage because v_e and such is live and may be changing (even exoatmospheric due to Agathorn)
            DirtyCacheForStage(0);
        }

        private double smaT() {
            if ( desiredApoapsis > autopilot.desiredOrbitAltitude )
                return (autopilot.desiredOrbitAltitude + 2 * mainBody.Radius + desiredApoapsis) / 2;
            else
                return autopilot.desiredOrbitAltitude + mainBody.Radius;
        }

        private void UpdateRocketStats() {
            UpdateStageStats();

            GM = mainBody.gravParameter;
            stages[num_stages - 1].rT = r_burnout = autopilot.desiredOrbitAltitude + mainBody.Radius;

            v_burnout = Math.Sqrt(GM * (2/r_burnout - 1/smaT()));  /* FIXME: assumes periapsis insertion */
            stages[0].r = mainBody.position.magnitude;

            eT = - GM / (2*smaT());
            /* XXX: estimate on the pad is very low, might need mean-radius or might need potential energy term, should be closer to 8100m/s */
            // dVest = Math.Sqrt(2 * ( eT + GM / r )) - vessel.obt_velocity.magnitude;

            stages[num_stages - 1].rdT = rd_burnout = 0;  /* FIXME: assumes periapsis insertion */
            stages[0].rd = vesselState.speedVertical;

            stages[num_stages - 1].h = h_burnout = r_burnout * v_burnout;  /* FIXME: assumes periapsis insertion */
            stages[0].h = Vector3.Cross(mainBody.position, vessel.obt_velocity).magnitude;
        }

        private double b(int n, int snum)
        {
            StageInfo stage = stages[snum];

            if (!stage.b_dirty[n])
                return stage.b[n];

            double ret;

            if (n == 0)
                ret = stage.dV;
            else
                ret = b(n-1, snum) * stage.tau - stage.v_e * MuUtils.IntPow(stage.T, n) / n;

            stage.b[n] = ret;
            stage.b_dirty[n] = false;

            return ret;
        }

        private double c(int n, int snum)
        {
            StageInfo stage = stages[snum];

            if (!stage.c_dirty[n])
                return stage.c[n];

            double ret;

            if (n == 0)
                ret = b(0, snum) * stage.T - b(1, snum);
            else
                ret = c(n-1, snum) * stage.tau - stage.v_e * MuUtils.IntPow(stage.T, n+1) / ( n * (n + 1 ) );

            stage.c[n] = ret;
            stage.c_dirty[n] = false;

            return ret;
        }

        private double Alpha(int snum)
        {
            double sum = 0;
            for(int l = 0; l <= snum; l++)
                sum += b(0, l);

            return sum;
        }

        private double Beta(int snum)
        {
            double sum = 0;
            for(int l = 0; l <= snum; l++)
            {
                double sum2 = 0;
                for(int k = 0; k <= (l-1); k++)
                {
                    sum2 += stages[k].T;
                }

                sum += b(1, l) + b(0, l) * sum2;
            }

            return sum;
        }

        private double Gamma(int snum)
        {
            double sum = 0;
            for(int l = 0; l <= snum; l++)
            {
                double sum2 = 0;
                for(int k = 0; k <= (l-1); k++) {
                    sum2 += b(0, k);
                }

                sum += c(0, l) + stages[l].T * sum2;
            }

            return sum;
        }

        private double Delta(int snum)
        {
            double sum = 0;
            for(int l = 0; l <= snum; l++)
            {
                double sum2 = 0;
                for(int k = 0; k <= (l-1); k++) {
                    double sum3 = 0;
                    for(int i = 0; i <= (k - 1); i++) {
                        sum3 += stages[i].T;
                    }
                    sum2 += c(0, l) * stages[k].T + b(1, k) * stages[l].T + b(0, k) * stages[l].T * sum3;
                }
                sum += c(1, l) + sum2;
            }

            return sum;
        }

        private double Drd(int snum)
        {
            double sum = 0;
            for(int l = 0; l <= snum; l++) {
                double sum2 = 0;
                for(int k = 0; k <= (l-1); k++) {
                    double sum3 = 0;
                    for(int i = 0; i <= (k-1); i++) {
                        sum3 += stages[i].dB;
                    }
                    sum2 += b(0, l) * stages[k].dA + b(0, l) * stages[k].T * sum3 + b(1, l) * stages[k].dB;
                }
                sum += sum2;
            }
            return rd_burnout - stages[0].rd - sum;
        }

        private double Dr(int snum)
        {
            double sum = 0;
            for(int l = 0; l <= snum; l++) {
                double sum2 = 0;
                for(int k = 0; k<= (l-1); k++) {
                    double sum3 = 0;
                    for(int i = 0; i <= (k-1); i++) {
                        double sum4 = 0;
                        for(int m = 0; m <= (i-1); m++) {
                            sum4 += stages[m].dB;
                        }
                        sum3 += b(0, k) * stages[l].T * stages[i].dA + b(0,k) * stages[i].T * stages[l].T * sum4 + b(1,k) * stages[l].T * stages[i].dB + c(0,l) * stages[k].T * stages[i].dB;
                    }
                    sum2 += c(0,l) * stages[k].dA + c(1,l) * stages[k].dB + sum3;
                }
                sum += sum2;
            }
            return r_burnout - stages[0].r  - stages[0].rd * total_T - sum;
        }

        private void peg_solve(int snum)
        {
            double drd = Drd(snum);
            double dr = Dr(snum);

            double alpha = Alpha(snum);
            double beta  = Beta(snum);
            double gamma = Gamma(snum);
            double delta = Delta(snum);

            double D = alpha * delta - beta * gamma;

            stages[0].A = ( delta * drd - beta * dr ) / D;
            stages[0].B = ( alpha * dr - gamma * drd ) / D;
        }

        private void peg_update(double dt)
        {
            stages[0].A = stages[0].A + stages[0].B * dt;
            stages[num_stages - 1].T -= dt;

            for (int i = 0; i < num_stages - 1 ; i++) {
                /* all the lower stages are assumed to burn completely */
                stages[i].dV = stages[i].avail_dV;
                stages[i].T = stages[i].avail_T;
            }
            for (int i = 0; i < num_stages; i++) {
                stages[i].dA = 0.0;
                stages[i].dB = 0.0;
            }
        }

        private void peg_estimate(int snum)
        {
            StageInfo stage = stages[snum];

            bool upper = ( snum == num_stages - 1);  /* final stage */

            double A = stage.A;
            double B = stage.B;
            double T = stage.T;

            double v_e = stage.v_e;
            double a0 = stage.a0;
            double tau = v_e / a0;
            double r = stage.r;
            double rd = stage.rd;
            double h = stage.h;

            double G = stage.G = ( GM - h * h / r ) / ( r * r );

            double f_r = stage.f_r = A + G / a0;

            /* current sin pitch  */
            // double f_r = A + G / a0;
            /* sin pitch at burnout */
            // double f_rT = A + B * T + GT/aT;
            /* appx rate of sin pitch */
            // double fd_r = ( f_rT - f_r ) / T;

            /* cos pitch */
            // note that when this Sqrt goes NaN that is a useful condition to terminate guidance, clamping it will increase terminal guidance wiggles
            double f_th = Math.Sqrt(1.0D - stage.f_r * stage.f_r);
            /* cos pitch rate */
            double fd_th = - stage.f_r * fd_r;
            /* cos pitch accel */
            double fdd_th = - ( fd_r * fd_r ) / 2.0D;

            double rT;
            double rdT;
            double hT;


            if (upper) {
                double dh = stage.dh = h_burnout - h;
                double rbar = (r_burnout + r ) / 2.0;

                hT = stage.hT = h + dh;

                /* updated estimate of dV to burn */
                //stage.dV = ( dh / rbar + v_e * T * ( fd_th + fdd_th * ( tau + T / 2.0 ) ) ) / ( ( fdd_th * tau + fd_th ) * tau + f_th );
                stage.dV = ( dh / rbar + v_e * T * ( fd_th + fdd_th * tau ) + ( fdd_th * v_e * T * T ) / 2.0 ) / ( f_th + fd_th * tau + fdd_th * tau * tau );

                /* updated estimate of T */
                T = stage.T = tau * ( 1 - Math.Exp( - stage.dV / v_e ) );

                /* now that we have the updated T and dV values, dirty the upper stages constant cache */
                DirtyCacheForStage(snum);

                total_T = 0.0;
                for(int i = 0; i < num_stages; i++)
                {
                    total_T += stages[i].T;
                }

                rT = stage.rT;
                rdT = stage.rdT;
            } else { /* boosters */
                double dr = stage.dr = rd * T + c(0,snum) * A + c(1, snum) * B;
                double drd = stage.drd = b(0,snum) * A + b(1, snum) * B;
                rT = stage.rT = r + dr;
                rdT = stage.rdT = rd + drd;
                //Debug.Log("f_th: " + f_th + " fd_th: " + fd_th + " fdd_th: " + fdd_th);
                //Debug.Log("b(0,0): " + b(0,0) + " b(1,0): " + b(1,0) + " b(2,0): " + b(2,0));
                double dh = stage.dh = ( r + rT ) / 2.0 * ( f_th * b(0, snum) + fd_th * b(1, snum) + fdd_th * b(2, snum) );
                hT = stage.hT = h + dh;
            }

            double aT = stage.aT = a0 / ( 1.0D - T / tau );
            double GT = stage.GT = ( GM - hT * hT / rT ) / ( rT * rT );

            double f_rT = stage.f_rT = A + B * T + GT / aT;

            /* set next stages initial conditions */
            if (!upper) {
                double dA = stage.dA = GT * ( 1.0 / aT - 1.0 / stages[snum+1].a0 );
                double dB = stage.dB = GT * ( 1.0 / stages[snum+1].v_e - 1.0 / v_e ) + ( 3 * hT * hT / rT - 2 * GM ) * rdT / ( rT * rT * rT ) * ( 1.0 / aT - 1.0 / stages[snum+1].a0 );
                stages[snum+1].r  = rT;
                stages[snum+1].rd = rdT;
                stages[snum+1].h  = hT;
                stages[snum+1].A  = A + B * T + dA;
                stages[snum+1].B  = B + dB;
            }
        }

        private void peg_final() {
            total_T = 0.0;
            total_dV = 0.0;
            for(int i = 0; i < num_stages; i++)
            {
                total_T += stages[i].T;
                total_dV += stages[i].dV;
            }
            /* appx rate of sin pitch over the whole burn */
            fd_r = ( stages[num_stages-1].f_rT - stages[0].f_r ) / total_T;
        }

        private bool bad_dV()
        {
            /* FIXME: this could look for other obviously insane values */
            return stages[0].dV <= 0.0D;
        }

        private bool bad_pitch()
        {
            return Double.IsNaN(guidancePitch);
        }

        private bool bad_guidance(StageInfo stage)
        {
            double A = stage.A;
            double T = stage.T;
            double B = stage.B;
            return Double.IsNaN(T) || Double.IsInfinity(T) || T <= 0.0D || Double.IsNaN(A) || Double.IsInfinity(A) || Double.IsNaN(B) || Double.IsInfinity(B);
        }

        private void converge(double dt)
        {
            double Astart = stages[0].A;
            double Bstart = stages[0].B;
            double Tstart = stages[num_stages-1].T;

            if (num_stages == 1 && stages[0].T < terminalGuidanceSecs && saneGuidance)
                terminalGuidance = true;

            if (!terminalGuidance) {
                if (needsConverge || bad_guidance(stages[0]) || bad_pitch() || bad_dV() || !saneGuidance )
                {
                    stages[num_stages-1].T = stages[num_stages-1].avail_T;
                    stages[0].A = Math.Sin(srfvelPitch()) - stages[0].G / stages[0].a0;
                    stages[0].B = 0.0036;
                    fd_r = -0.001;
                    dt = 0.0;
                }
            }

            bool stable = false;

            peg_update(dt);

            if (terminalGuidance)
            {
                peg_estimate(0);
                peg_final();
                stable = true; /* terminal guidance is always considered stable */
            }
            else
            {
                for(convergenceSteps = 1; convergenceSteps <= 50; convergenceSteps++) {
                    double oldT = stages[num_stages-1].T;

                    for(int i = 0; i < num_stages; i++)
                        peg_estimate(i);

                    peg_final();

                    peg_solve(num_stages - 1);

                    if ( Math.Abs(stages[num_stages-1].T - oldT) < 0.1 ) {
                        stable = true;
                        break;
                    }

                    // abort cycles if we hit NaN values or something insane
                    if (bad_guidance(stages[0]))
                        break;
                }

                terminalGuidance = false;
            }

            if (!stable || bad_guidance(stages[0]) || bad_dV() || bad_pitch())
            {
                /* if we're within 30 secs of burnout and we just lost guidance, restore A,B,T and switch to terminal guidance */
                if (saneGuidance && num_stages == 1 && stages[0].T <= 30)
                {

                    stages[0].A = Astart;
                    stages[0].B = Bstart;
                    stages[num_stages-1].T = Tstart;
                    terminalGuidance = true;
                }
                else
                    saneGuidance = false;
            }
            else
            {
                needsConverge = false;
                saneGuidance = true;
            }
        }

        private double last_time = 0.0D;

        public override bool DriveAscent(FlightCtrlState s)
        {
            stats.RequestUpdate(this);
            stats.liveSLT = true;  /* yes, this disables the button, yes, it is important we do this */
            if (last_edit_num_stages != edit_num_stages)
            {
                SetNumStages(edit_num_stages);
                last_edit_num_stages = edit_num_stages;
            }

            UpdateRocketStats();

            if (last_time != 0.0D)
                converge(vesselState.time - last_time);
            else
                converge(0);

            last_time = vesselState.time;

            switch (mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent(s);
                    break;

                case AscentMode.INITIATE_TURN:
                    DriveInitiateTurn(s);
                    break;

                case AscentMode.GRAVITY_TURN:
                    DriveGravityTurn(s);
                    break;
            }

            return (mode != AscentMode.EXIT);
        }

        private void DriveVerticalAscent(FlightCtrlState s)
        {

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);
            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && vesselState.altitudeBottom > 50);

            if (!vessel.LiftedOff() || vessel.Landed) {
                status = "Awaiting liftoff";
            }
            else
            {
                if ((vesselState.time - vessel.launchTime ) > pitchStartTime)
                {
                    mode = AscentMode.INITIATE_TURN;
                    return;
                }
                double dt = pitchStartTime - ( vesselState.time - vessel.launchTime );
                status = String.Format("Vertical ascent {0:F2} s", dt);
            }
        }

        private void DriveInitiateTurn(FlightCtrlState s)
        {
            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;
            if ((vesselState.time - vessel.launchTime ) > pitchEndTime)
            {
                mode = AscentMode.GRAVITY_TURN;
                return;
            }

            double dt = vesselState.time - vessel.launchTime - pitchStartTime;
            double theta = dt * pitchRate;
            attitudeTo(Math.Min(90, 90 - theta + pitchBias));

            status = String.Format("Pitch program {0:F2} s", pitchEndTime - pitchStartTime - dt);
        }

        private void DriveGravityTurn(FlightCtrlState s)
        {
            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;
            if ((vesselState.time - vessel.launchTime ) < pitchEndTime)
            {
                /* this can happen when users update the endtime box */
                mode = AscentMode.INITIATE_TURN;
                return;
            }

            if (stages[0].h >= h_burnout)
            {
                status = "Angular momentum target achieved";
                core.thrust.targetThrottle = 0.0F;
                mode = AscentMode.EXIT;
                return;
            }

            if (saneGuidance && guidanceEnabled) {
                if (terminalGuidance)
                    status = "Locked Terminal Guidance";
                else
                    status = "Stable PEG Guidance";

                attitudeTo(guidancePitch);
            }
            else
            {
                // srfvelPitch == zero AoA
                status = "Unguided Gravity Turn";
                attitudeTo(Math.Min(90, srfvelPitch() + pitchBias));
            }
        }
    }
}
