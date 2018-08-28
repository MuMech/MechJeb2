using System;
using KSP.UI.Screens;
using UnityEngine;
using System.Collections.Generic;

/*
 * Optimized launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPEG : MechJebModuleAscentBase
    {
        public MechJebModuleAscentPEG(MechJebCore core) : base(core) { }

        /* default pitch program here works decently at SLT of about 1.4 */
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pitchStartTime = new EditableDouble(10);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pitchRate = new EditableDouble(0.75);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pitchEndTime = new EditableDouble(55);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredApoapsis = new EditableDoubleMult(0, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool pitchEndToggle = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool pegAfterStageToggle = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pegAfterStage = new EditableDouble(0);

        /* this deliberately does not persist, it is for emergencies only */
        public EditableDouble pitchBias = new EditableDouble(0);

        private MechJebModuleAscentGuidance ascentGuidance { get { return core.GetComputerModule<MechJebModuleAscentGuidance>(); } }

        public override void OnModuleEnabled()
        {
            mode = AscentMode.VERTICAL_ASCENT;
            Debug.Log("MechJebModuleAscentPEG enabled, optimizer should be enabled now");
            core.optimizer.enabled = true;
        }

        public override void OnModuleDisabled()
        {
            Debug.Log("MechJebModuleAscentPEG disabled, optimizer should be disabled now");
            core.optimizer.enabled = false;
        }

        private enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, GUIDANCE, EXIT };
        private AscentMode mode;

        public override bool DriveAscent(FlightCtrlState s)
        {
            setTarget();
            core.optimizer.AssertStart(allow_execution: true);
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

                case AscentMode.GUIDANCE:
                    DriveGuidance(s);
                    break;
            }

            return (mode != AscentMode.EXIT);
        }

        private void setTarget()
        {
            if ( ascentGuidance.launchingToPlane && core.target.NormalTargetExists )
            {
                core.optimizer.TargetPeInsertMatchOrbitPlane(autopilot.desiredOrbitAltitude, desiredApoapsis, core.target.TargetOrbit);
                //autopilot.desiredInclination = Math.Acos(-Vector3d.Dot(-Planetarium.up, core.optimizer.iy)) * UtilMath.Rad2Deg;
            }
            else
            {
                core.optimizer.TargetPeInsertMatchInc(autopilot.desiredOrbitAltitude, desiredApoapsis, autopilot.desiredInclination);
            }
        }

        private void attitudeToGuidance(double pitch, bool handleManualAz = false, bool surfHeading = false)
        {
            double heading;

            if (surfHeading)
                heading = srfvelHeading();
            else
                heading = core.optimizer.heading;

            attitudeTo(pitch, heading);
        }

        private void DriveVerticalAscent(FlightCtrlState s)
        {

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeToGuidance(90, handleManualAz: true);

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && (vesselState.altitudeBottom > 50));

            if (!vessel.LiftedOff() || vessel.Landed) {
                status = "Awaiting liftoff";
            }
            else
            {
                if (autopilot.MET > pitchStartTime)
                {
                    mode = AscentMode.INITIATE_TURN;
                    return;
                }
                double dt = pitchStartTime - autopilot.MET;
                status = String.Format("Vertical ascent {0:F2} s", dt);
            }
        }

        private void DriveInitiateTurn(FlightCtrlState s)
        {
            double dt = autopilot.MET - pitchStartTime;
            double theta = dt * pitchRate;
            double pitch = 90 - theta + pitchBias;

            if (pitchEndToggle)
            {
                if ( autopilot.MET > pitchEndTime )
                {
                    mode = AscentMode.GRAVITY_TURN;
                    return;
                }
                status = String.Format("Pitch program {0:F2} s", pitchEndTime - pitchStartTime - dt);
            } else {
                if ( pitch < core.optimizer.pitch )
                {
                    mode = AscentMode.GUIDANCE;
                    return;
                }
                status = String.Format("Pitch program {0:F2} °", pitch - core.optimizer.pitch);
            }

            attitudeToGuidance(pitch, handleManualAz: true);
        }

        private void DriveGravityTurn(FlightCtrlState s)
        {
            double pitch = Math.Min(Math.Min(90, srfvelPitch() + pitchBias), vesselState.vesselPitch);

            if (pitchEndToggle && autopilot.MET < pitchEndTime)
            {
                // this can happen when users update the endtime box
                mode = AscentMode.INITIATE_TURN;
                return;
            }

            if ( pegAfterStageToggle )
            {
                if ( StageManager.CurrentStage < pegAfterStage )
                {
                    mode = AscentMode.GUIDANCE;
                    return;
                }
                attitudeToGuidance(pitch);
            }
            else if ( core.optimizer.isStable() )
            {
                if ( pitch < core.optimizer.pitch )
                {
                    mode = AscentMode.GUIDANCE;
                    return;
                }
                else
                {
                    attitudeToGuidance(pitch);
                }
            }
            else
            {
                attitudeToGuidance(pitch, surfHeading: true);
            }

            status = "Unguided Gravity Turn";
        }

        private void DriveGuidance(FlightCtrlState s)
        {
            if (core.optimizer.status == PegStatus.FINISHED)
            {
                mode = AscentMode.EXIT;
                return;
            }

            if (core.optimizer.isStable())
                status = "Stable Guidance";
            else
                status = "WARNING: Unstable Guidance";

            attitudeToGuidance(core.optimizer.pitch);
        }
    }
}
