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
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult pitchStartTime = new EditableDoubleMult(10);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult pitchRate = new EditableDoubleMult(0.75);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult pitchEndTime = new EditableDoubleMult(55);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredApoapsis = new EditableDoubleMult(100000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult terminalGuidanceSecs = new EditableDoubleMult(10);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult stageLowDVLimit = new EditableDoubleMult(20);

        /* this deliberately does not persist, it is for emergencies only */
        public EditableDoubleMult pitchBias = new EditableDoubleMult(0);

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        public override void OnModuleEnabled()
        {
            mode = AscentMode.VERTICAL_ASCENT;
            InitStageStats();
        }

        public override void OnModuleDisabled()
        {
            stages.Clear();
        }

        private enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, EXIT };
        private AscentMode mode;

        /* guidancePitchAngle -- output from the guidance algorithm, not 'manual' pitch */
        public double guidancePitch { get { return Math.Asin(stages[0].A + stages[0].C) * UtilMath.Rad2Deg; } }

        /* dV to add */
        public double dV { get; private set; }
        /* dV estimated from difference in specific orbital energy*/
        public double dVest { get; private set; }

        public bool guidanceEnabled = true;
        public int convergenceSteps { get; private set; }

        private bool saneGuidance = false;
        private bool terminalGuidance = false;

        /* tangential velocity at burnout */
        private double vT;
        /* radius at burnout */
        private double rT;
        /* radius now */
        private double r;
        /* gravParameter */
        private double GM;

        /* ending radial velocity */
        private double rdT;
        /* current radial velocity */
        private double rd;

        /* angular velocity at burnout */
        private double wT;
        /* current angular velocity */
        private double w;
        /* mean radius */
        private double rbar;
        /* angular momentum at burnout */
        private double hT;
        /* angular momentum */
        private double h;
        /* angular momentum to gain */
        private double dh;
        /* specific orbital energy at burnout */
        private double eT;

        /*
         * handle tracking ksp/mechjeb stage information and keep state about stages
         * (this is mildly awful because of everything that can dynamically happen to
         * stages in flight, including players manually rearranging stages).
         */

        public List<StageInfo> stages = new List<StageInfo>();

        public class StageInfo
        {
            public double dV;
            public double v_e;
            public double a0;
            public double deltaTime;
            public double A;
            public double B;
            public double C;
            public double K;
            public double T;
            public List<Part> parts;
            public int kspStage;
        }

        void UpdateStageFromMechJeb(StageInfo stage, bool atmo = false)
        {
            /* stage.kspStage must be corrected before calling this */
            int s = stage.kspStage;
            if (atmo)  /* really "current" stats */
            {
                stage.dV = atmoStats[s].deltaV;
                stage.deltaTime = atmoStats[s].deltaTime;
                stage.v_e = atmoStats[s].isp * 9.80665;
                stage.a0 = atmoStats[s].startThrust / atmoStats[s].startMass;
            }
            else
            {
                stage.dV = vacStats[s].deltaV;
                stage.deltaTime = vacStats[s].deltaTime;
                stage.v_e = vacStats[s].isp * 9.80665;
                stage.a0 = vacStats[s].startThrust / vacStats[s].startMass;
            }
        }

        public List<Part> skippedParts = new List<Part>();

        void InitStageStats()
        {
            Debug.Log("MechJebModuleAscentPEG.InitStageStats()");
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
            /* sometimes parts wind up in zero dV stages and we need to remove them here */
            for ( int i = 0; i < stages.Count; i++ ) {
                for ( int j = 0; j < stages[i].parts.Count; j++ ) {
                    if ( skippedParts.Contains(stages[i].parts[j]) )
                        skippedParts.Remove(stages[i].parts[j]);
                }
            }
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
                    UpdateStageFromMechJeb(stage);
                }
            }
            for ( int i = stages.Count - 1; i >= 0; i-- )
            {
                if ( stages[i].kspStage < 0 )
                    stages.RemoveAt(i);
            }
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
            rT = autopilot.desiredOrbitAltitude + mainBody.Radius;
            vT = Math.Sqrt(GM * (2/rT - 1/smaT()));  /* FIXME: assumes periapsis insertion */
            r = mainBody.position.magnitude;

            eT = - GM / (2*smaT());
            /* XXX: estimate on the pad is very low, might need mean-radius or might need potential energy term, should be closer to 8100m/s */
            dVest = Math.Sqrt(2 * ( eT + GM / r )) - vessel.obt_velocity.magnitude;

            rdT = 0;  /* FIXME: assumes periapsis insertion */
            rd = vesselState.speedVertical;

            wT = vT / rT;
            w = Vector3.Cross(mainBody.position, vessel.obt_velocity).magnitude / (r * r);
            rbar = ( rT + r ) / 2.0D;
            hT = rT * vT;  /* FIXME: assumes periapsis insertion */
            h = Vector3.Cross(mainBody.position, vessel.obt_velocity).magnitude;
            dh = hT - h;
        }

        private void peg_solve1(StageInfo stage)
        {
            double T = stage.T;
            double v_e = stage.v_e;
            double a0 = stage.a0;
            double tau = v_e / a0;

            double b0 = -v_e * Math.Log(1.0D - T/tau);
            double b1 = b0 * tau - v_e * T;
            double c0 = b0 * T - b1;
            double c1 = c0 * tau - v_e * T * T / 2.0D;

            double d = b0 * c1 - b1 * c0;

            stage.A = ( c1 * ( rdT - rd ) - b1 * ( rT - r - rd * T ) ) / d;
            stage.B = ( -c0 * ( rdT - rd ) + b0 * ( rT - r - rd * T ) ) / d;
        }

        private void peg_update(double dt, StageInfo stage)
        {
            /* update old guidance */
            stage.A = stage.A + stage.B * dt;
            /* B does not update */
            stage.T = stage.T - dt;
        }

        private void peg_estimate(StageInfo stage)
        {
            /* update old guidance */
            double A = stage.A;
            double B = stage.B;
            double T = stage.T;

            double v_e = stage.v_e;
            double a0 = stage.a0;
            double tau = v_e / a0;

            double aT = a0 / ( 1.0D - T / tau );
            double C = stage.C = (GM / (r * r) - w * w * r ) / a0;
            double CT = (GM / (rT * rT) - wT * wT * rT ) / aT;

            /* current sin pitch  */
            double f_r = A + C;
            /* sin pitch at burnout */
            double f_rT = A + B * T + CT;
            /* appx rate of sin pitch */
            double fd_r = ( f_rT - f_r ) / T;

            /* placeholder for future yaw term */
            double f_h = 0.0D;
            /* placeholder for future yaw rate term */
            double fd_h = 0.0D;

            /* cos pitch */
            double f_th = 1.0D - f_r * f_r / 2.0D - f_h * f_h / 2.0D;
            /* cos pitch rate */
            double fd_th = - ( f_r * fd_r + f_h * fd_h );
            /* cos pitch accel */
            double fdd_th = - ( fd_r * fd_r + fd_h * fd_h ) / 2.0D;

            /* updated estimate of dV to burn */
            dV = ( dh / rbar + v_e * T * ( fd_th + fdd_th * tau ) + fdd_th * v_e * T * T / 2.0D ) / ( f_th + fd_th * tau + fdd_th * tau * tau );

            /* updated estimate of T */
            stage.T = tau * ( 1 - Math.Exp( - dV / v_e ) );
        }

        private bool bad_dV()
        {
            /* FIXME: this could look for other obviously insane values */
            return dV <= 0.0D;
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

        private void converge(double dt, bool initialize = false)
        {
            if (initialize || bad_guidance(stages[0]) || bad_pitch() || bad_dV() || !saneGuidance)
            {
                stages[0].T = stages[0].deltaTime;
                stages[0].A = -0.4;
                stages[0].B = 0.0036;
                dt = 0.0;
            }

            bool stable = false;

            peg_update(dt, stages[0]);

            if (stages[0].T < terminalGuidanceSecs)
            {
                peg_estimate(stages[0]);
                terminalGuidance = true;
                stable = true; /* terminal guidance is always considered stable */
            }
            else
            {
                for(convergenceSteps = 1; convergenceSteps <= 50; convergenceSteps++) {
                    double oldT = stages[0].T;

                    peg_estimate(stages[0]);
                    peg_solve1(stages[0]);

                    if ( Math.Abs(stages[0].T - oldT) < 0.01 ) {
                        stable = true;
                        break;
                    }
                    /* FIXME: consider breaking out on bad_guidance() here */
                }
                terminalGuidance = false;
            }

            if (!stable || bad_guidance(stages[0]) || bad_dV() || bad_pitch())
            {
                saneGuidance = false;
            }
        }

        private double last_time = 0.0D;

        public override bool DriveAscent(FlightCtrlState s)
        {
            stats.RequestUpdate(this);
            stats.liveSLT = true;  /* yes, this disables the button, yes, it is important we do this */
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

            if (h >= hT)
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
