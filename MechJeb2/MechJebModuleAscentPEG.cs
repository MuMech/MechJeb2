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
        public EditableDouble pitchRate = new EditableDouble(0.50);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredApoapsis = new EditableDoubleMult(0, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool omitCoast = false;

        private MechJebModuleAscentGuidance ascentGuidance { get { return core.GetComputerModule<MechJebModuleAscentGuidance>(); } }

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            mode = AscentMode.VERTICAL_ASCENT;
            Debug.Log("MechJebModuleAscentPEG enabled, optimizer should be enabled now");
            core.optimizer.enabled = true;
        }

        public override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            Debug.Log("MechJebModuleAscentPEG disabled, optimizer should be disabled now");
            core.optimizer.enabled = false;
        }

        private enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GUIDANCE, EXIT };
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
                core.optimizer.TargetPeInsertMatchOrbitPlane(autopilot.desiredOrbitAltitude, desiredApoapsis, core.target.TargetOrbit, omitCoast);
                //autopilot.desiredInclination = Math.Acos(-Vector3d.Dot(-Planetarium.up, core.optimizer.iy)) * UtilMath.Rad2Deg;
            }
            else
            {
                core.optimizer.TargetPeInsertMatchInc(autopilot.desiredOrbitAltitude, desiredApoapsis, autopilot.desiredInclination, omitCoast);
            }
        }

        private void DriveVerticalAscent(FlightCtrlState s)
        {

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90, core.optimizer.heading);

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
            double pitch_program = 90 - theta;
            double pitch;

            if ( !mainBody.atmosphere )
            {
                mode = AscentMode.GUIDANCE;
                return;
            }

            if ( pitch_program > srfvelPitch() )
            {
                pitch = srfvelPitch();
                status = String.Format("Gravity Turn {0:F}° to guidance", pitch - core.optimizer.pitch);
            }
            else
            {
                pitch = pitch_program;
                status = String.Format("Pitch program {0:F}° to guidance", pitch - core.optimizer.pitch);
            }

            if ( pitch <= core.optimizer.pitch && core.optimizer.isStable() )
            {
                mode = AscentMode.GUIDANCE;
                return;
            }

            if ( (vesselState.maxDynamicPressure > 0) && (vesselState.maxDynamicPressure * 0.90 > vesselState.dynamicPressure) )
            {
                mode = AscentMode.GUIDANCE;
                return;
            }

            if (mainBody.atmosphere && vesselState.maxDynamicPressure > 0)
            {
                // from 95% to 90% of dynamic pressure apply a "fade" from the pitch selected above to the guidance pitch
                double fade = MuUtils.Clamp( (0.95 - vesselState.dynamicPressure / vesselState.maxDynamicPressure) * 20.0, 0.0, 1.0);
                pitch = fade * core.optimizer.pitch + ( 1.0 - fade ) * pitch;
            }

            attitudeTo(pitch, core.optimizer.heading);
        }

        private void DriveGuidance(FlightCtrlState s)
        {
            if ( core.optimizer.status == PegStatus.FINISHED )
            {
                mode = AscentMode.EXIT;
                return;
            }

            if ( !core.optimizer.isStable() )
            {
                double pitch = Math.Min(Math.Min(90, srfvelPitch()), vesselState.vesselPitch);
                attitudeTo(pitch, srfvelHeading());
                status = "WARNING: Unstable Guidance";
            }
            else
            {

                status = "Stable Guidance";
                attitudeTo(core.optimizer.pitch, core.optimizer.heading);
            }
        }
    }
}
