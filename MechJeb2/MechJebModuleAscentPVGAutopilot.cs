extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;

/*
 * Optimized launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPVGAutopilot : MechJebModuleAscentBaseAutopilot
    {
        public MechJebModuleAscentPVGAutopilot(MechJebCore core) : base(core)
        {
        }

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        protected override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _mode = AscentMode.VERTICAL_ASCENT;
            Core.Guidance.Users.Add(this);
            Core.Guidance.CascadeDisable(this);
            Core.Glueball.Users.Add(this);
        }

        protected override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            Core.Guidance.Users.Remove(this);
            Core.Glueball.Users.Remove(this);
        }

        private enum AscentMode
        {
            VERTICAL_ASCENT,
            PITCHPROGRAM,
            ZEROLIFT,
            GUIDANCE,
            EXIT
        }

        private AscentMode _mode;

        protected override void TimedLaunchHook() =>
            // timedLaunch kills the optimizer so re-enable it here
            Core.Guidance.Enabled = true;

        protected override bool DriveAscent2()
        {
            SetTarget();
            Core.Guidance.AssertStart();
            switch (_mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent();
                    break;

                case AscentMode.PITCHPROGRAM:
                    DrivePitchProgram();
                    break;

                case AscentMode.ZEROLIFT:
                    DriveZeroLift();
                    break;

                case AscentMode.GUIDANCE:
                    DriveGuidance();
                    break;
            }

            return _mode != AscentMode.EXIT;
        }

        private void SetTarget()
        {
            double peR = MainBody.Radius + AscentSettings.DesiredOrbitAltitude;
            double apR = MainBody.Radius + AscentSettings.DesiredApoapsis;
            if (_ascentSettings.DesiredApoapsis < 0)
                apR = _ascentSettings.DesiredApoapsis;
            double attR = MainBody.Radius +
                          (AscentSettings.OptimizeStage < 0 ? AscentSettings.DesiredAttachAltFixed : AscentSettings.DesiredAttachAlt);

            bool lanflag = _ascentSettings.LaunchingToPlane || _ascentSettings.LaunchingToMatchLan || _ascentSettings.LaunchingToLan;
            double lan = _ascentSettings.LaunchingToPlane || _ascentSettings.LaunchingToMatchLan
                ? Core.Target.TargetOrbit.LAN
                : (double)AscentSettings.DesiredLan;

            double inclination = AscentSettings.DesiredInclination;
            bool attachAltFlag = AscentSettings.OptimizeStage < 0 || AscentSettings.AttachAltFlag;

            // if we are launchingToPlane other code in MJ fixes the sign of the inclination to be correct
            // FIXME: can we just use autopilot.desiredInclination here and rely on the other code to update that value?
            if (_ascentSettings.LaunchingToPlane)
                inclination = Math.Sign(inclination) * Core.Target.TargetOrbit.inclination;

            // FIXME: kinda need to break this up it is getting very magical with all the non-obvious ignored combinations of options
            Core.Glueball.SetTarget(peR, apR, attR, inclination, lan, AscentSettings.DesiredFPA, attachAltFlag, lanflag);
        }

        private double _pitchStartTime;

        private void DriveVerticalAscent()
        {
            //during the vertical ascent we just thrust straight up at max throttle
            AttitudeTo(90, Core.Guidance.Heading);

            bool liftedOff = Vessel.LiftedOff() && !Vessel.Landed && VesselState.altitudeBottom > 5;

            Core.Attitude.SetActuationControl(liftedOff, liftedOff, liftedOff);
            Core.Attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && VesselState.altitudeBottom > AscentSettings.RollAltitude);

            if (!liftedOff)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status12"); //"Awaiting liftoff"
            }
            else
            {
                if (VesselState.surfaceVelocity.magnitude > AscentSettings.PitchStartVelocity)
                {
                    _mode           = AscentMode.PITCHPROGRAM;
                    _pitchStartTime = MET;
                    return;
                }

                double dv = AscentSettings.PitchStartVelocity - VesselState.surfaceVelocity.magnitude;
                Status = Localizer.Format("#MechJeb_Ascent_status13", $"{dv:F2}"); //Vertical ascent  <<1>>m/s to go
            }
        }

        private void DrivePitchProgram()
        {
            double dt = MET - _pitchStartTime;
            double theta = dt * AscentSettings.PitchRate;
            double pitch = 90 - theta;

            // we need to initiate by at least 3 degrees, then transition to zerolift when srfvel catches up
            if (VesselState.currentPitch > SrfvelPitch() && VesselState.currentPitch < 87)
            {
                _mode = AscentMode.ZEROLIFT;
                return;
            }

            if (!AscentSettings.StagingTriggerFlag)
                Status = Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - Core.Guidance.Pitch:F}"); //Pitch program <<1>>° to guidance
            else
                Status = $"Pitch Program until stage {AscentSettings.StagingTrigger.Val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            AttitudeTo(pitch, Core.Guidance.Heading);
        }

        private bool CheckForGuidanceTransition(double pitch)
        {
            if (!MainBody.atmosphere) return true;

            if (!AscentSettings.StagingTriggerFlag)
            {
                if (pitch <= Core.Guidance.Pitch && Core.Guidance.IsStable()) return true;

                // dynamic pressure needs to fall by 10% before we level trigger
                if (VesselState.maxDynamicPressure > VesselState.dynamicPressure * 1.1)
                    if (VesselState.dynamicPressure < AscentSettings.DynamicPressureTrigger)
                        return true;
            }
            else
            {
                if (Core.Guidance.IsStable() && Vessel.currentStage <= AscentSettings.StagingTrigger) return true;
            }

            return false;
        }

        private void DriveZeroLift()
        {
            double pitch = SrfvelPitch();

            if (!AscentSettings.StagingTriggerFlag)
                Status = Localizer.Format("#MechJeb_Ascent_status14", $"{pitch - Core.Guidance.Pitch:F}"); //Gravity Turn <<1>>° to guidance
            else
                Status = $"Gravity Turn until stage {AscentSettings.StagingTrigger.Val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            AttitudeTo(pitch, Core.Guidance.Heading);
        }

        private void DriveGuidance()
        {
            if (Core.Guidance.Status == PVGStatus.FINISHED)
            {
                _mode = AscentMode.EXIT;
                return;
            }

            if (!Core.Guidance.IsStable())
            {
                double pitch = Math.Min(Math.Min(90, SrfvelPitch()), VesselState.vesselPitch);
                AttitudeTo(pitch, SrfvelHeading());
                Status = Localizer.Format("#MechJeb_Ascent_status16"); //"WARNING: Unstable Guidance"
            }
            else
            {
                Status = Localizer.Format("#MechJeb_Ascent_status17"); //"Stable Guidance"
                AttitudeTo(Core.Guidance.Pitch, Core.Guidance.Heading);
            }
        }
    }
}
