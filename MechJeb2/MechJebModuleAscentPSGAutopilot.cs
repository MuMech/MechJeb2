extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

/*
 * Optimized launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPSGAutopilot : MechJebModuleAscentBaseAutopilot
    {
        public MechJebModuleAscentPSGAutopilot(MechJebCore core) : base(core)
        {
        }

        protected override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            Debug.Log("Enabling PSG Ascent Autopilot");
            _mode = AscentMode.VERTICAL_ASCENT;
            Core.Guidance.Users.Add(this);
            Core.Guidance.CascadeDisable(this); // disable this module if guidance is disabled
        }

        protected override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            Debug.Log("Disabling PSG Ascent Autopilot");
            Core.Guidance.Users.Remove(this);
            Core.PSGSimulation.Users.Remove(this);
        }

        public override void Drive(FlightCtrlState s)
        {
            if (TimedLaunch)
            {
                if (TMinus <= AscentSettings.WarpCountDown)
                {
                    Core.PSGSimulation.Users.Add(this);
                    Core.Guidance.AssertStart(false);
                }
                else
                {
                    Core.PSGSimulation.Users.Remove(this);
                }
            }
            else
            {
                Core.PSGSimulation.Users.Add(this);
                Core.Guidance.AssertStart();
            }

            base.Drive(s);
        }

        private enum AscentMode
        {
            VERTICAL_ASCENT,
            PITCHPROGRAM,
            GUIDANCE,
            EXIT
        }

        private AscentMode _mode;

        protected override bool DriveAscent2()
        {
            // TODO: at some point we probably still need to:
            // 1. model vertical rise and pitchover in the solver
            // 2. freeze recomputation after launch
            // 3. fly through maxQ using attitude-as-function-of-velocity (rather than time)
            //    -- this should still handle thrust-fault failures by pitching up as we're not making enough velocity for our downrange
            //       and should wind up riding Q-alpha limits
            // 4. pick up closed-loop on the other side, after maxQ
            switch (_mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent();
                    break;

                case AscentMode.PITCHPROGRAM:
                    DrivePitchProgram();
                    break;

                case AscentMode.GUIDANCE:
                    DriveGuidance();
                    break;
            }

            return _mode != AscentMode.EXIT;
        }

        private double _pitchStartTime;

        private void DriveVerticalAscent()
        {
            //during the vertical ascent we just thrust straight up at max throttle
            Core.Attitude.AttitudeToVerticalLaunch(Core.Guidance.Heading,
                AscentSettings.ForceRoll && VesselState.AltitudeBottom > AscentSettings.RollAltitude, AscentSettings.VerticalRoll, this);

            bool liftedOff = Vessel.LiftedOff() && !Vessel.Landed;

            if (!liftedOff)
            {
                SetStatus(Localizer.Format("#MechJeb_Ascent_status12")); //"Awaiting liftoff"
                return;
            }

            if (VesselState.AltitudeBottom > AscentSettings.PitchStartHeight)
            {
                _mode = AscentMode.PITCHPROGRAM;
                _pitchStartTime = VesselState.MET;
                return;
            }

            double dh = AscentSettings.PitchStartHeight - VesselState.AltitudeBottom;
            SetStatus($"Vertical ascent {dh:F2}m to go");
        }

        private void DrivePitchProgram()
        {
            double dt = VesselState.MET - _pitchStartTime;
            double theta = dt * AscentSettings.PitchRate;
            double pitch = 90 - theta;

            SetStatus(Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - Core.Guidance.Pitch:F}")); //Pitch program <<1>>° to guidance

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            Core.Attitude.LimitedAttitudeTo(Core.Guidance.Heading, pitch, AscentSettings.ForceRoll, AscentSettings.TurnRoll, ApplyAoALimiter, this);
        }

        private bool CheckForGuidanceTransition(double pitch)
        {
            if (!MainBody.atmosphere) return true;

            if (pitch <= Core.Guidance.Pitch && Core.Guidance.IsStable()) return true;

            return false;
        }

        private void DriveGuidance()
        {
            if (Core.Guidance.Status == PSGStatus.FINISHED)
            {
                _mode = AscentMode.EXIT;
                return;
            }

            if (!Core.Guidance.IsStable())
            {
                double pitch = Math.Min(Math.Min(90, SrfvelPitch()), VesselState.Pitch);
                Core.Attitude.LimitedAttitudeTo(SrfvelHeading(), pitch, AscentSettings.ForceRoll, AscentSettings.TurnRoll, ApplyAoALimiter, this);
                SetStatus(Localizer.Format("#MechJeb_Ascent_status16"), StatusLevel.WARNING); //"WARNING: Unstable Guidance"
            }
            else
            {
                double ang = Vector3d.Angle(Core.Guidance.Inertial, VesselState.Forward);
                StatusLevel severity = ang > 5 ? StatusLevel.ERROR : ang > 1 ? StatusLevel.WARNING : StatusLevel.NORMAL;
                SetStatus($"Stable Guidance: {ang:F}° deviation", severity);
                Core.Attitude.LimitedAttitudeTo(Core.Guidance.Inertial, AscentSettings.ForceRoll, AscentSettings.TurnRoll, ApplyAoALimiter, this);
            }
        }

        private Vector3d ApplyAoALimiter(Vector3d desiredThrustVector)
        {
            double lim = Clamp(Core.PSGSimulation.LimitQa, 0, 10000);
            AscentSettings.LimitingAoA = VesselState.DynamicPressure * Vector3.Angle(VesselState.SurfaceVelocity, desiredThrustVector) * UtilMath.Deg2Rad > lim;
            if (AscentSettings.LimitingAoA)
            {
                CurrentMaxAoA = lim / VesselState.DynamicPressure * UtilMath.Rad2Deg;
                desiredThrustVector = MathExtensions.RotateTowards(VesselState.SurfaceVelocity, desiredThrustVector, (float)(CurrentMaxAoA * UtilMath.Deg2Rad), 1).normalized;
            }

            return desiredThrustVector;
        }
    }
}
