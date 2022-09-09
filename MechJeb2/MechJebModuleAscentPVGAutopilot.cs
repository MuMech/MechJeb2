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
        
        private MechJebModuleAscentSettings _ascentSettings => core.ascentSettings;

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _mode                 = AscentMode.VERTICAL_ASCENT;
            core.guidance.enabled = true;
            core.glueball.enabled = true;
        }

        public override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            core.guidance.enabled = false;
            core.glueball.enabled = false;
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

        protected override void TimedLaunchHook()
        {
            // timedLaunch kills the optimizer so re-enable it here
            core.guidance.enabled = true;
        }

        protected override bool DriveAscent2()
        {
            SetTarget();
            core.guidance.AssertStart();
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
            double peR = mainBody.Radius + AscentSettings.DesiredOrbitAltitude;
            double apR = mainBody.Radius + AscentSettings.DesiredApoapsis;
            if (_ascentSettings.DesiredApoapsis < 0)
                apR = _ascentSettings.DesiredApoapsis;
            double attR = mainBody.Radius + AscentSettings.DesiredAttachAlt;

            bool lanflag = _ascentSettings.LaunchingToPlane || _ascentSettings.LaunchingToMatchLan || _ascentSettings.LaunchingToLan;
            double lan = _ascentSettings.LaunchingToPlane || _ascentSettings.LaunchingToMatchLan ? core.target.TargetOrbit.LAN : (double)AscentSettings.DesiredLan;

            double inclination = AscentSettings.DesiredInclination;

            // if we are launchingToPlane other code in MJ fixes the sign of the inclination to be correct
            // FIXME: can we just use autopilot.desiredInclination here and rely on the other code to update that value?
            if (_ascentSettings.LaunchingToPlane)
                inclination = Math.Sign(inclination) * core.target.TargetOrbit.inclination;

            core.glueball.SetTarget(peR, apR, attR, inclination, lan, AscentSettings.AttachAltFlag, lanflag);
        }

        private double _pitchStartTime;

        private void DriveVerticalAscent()
        {
            //during the vertical ascent we just thrust straight up at max throttle
            AttitudeTo(90, core.guidance.Heading);

            bool liftedOff = vessel.LiftedOff() && !vessel.Landed && vesselState.altitudeBottom > 5;

            core.attitude.SetActuationControl(liftedOff, liftedOff, liftedOff);
            core.attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && vesselState.altitudeBottom > AscentSettings.RollAltitude);

            if (!liftedOff)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status12"); //"Awaiting liftoff"
            }
            else
            {
                if (vesselState.surfaceVelocity.magnitude > AscentSettings.PitchStartVelocity)
                {
                    _mode           = AscentMode.PITCHPROGRAM;
                    _pitchStartTime = MET;
                    return;
                }

                double dv = AscentSettings.PitchStartVelocity - vesselState.surfaceVelocity.magnitude;
                Status = Localizer.Format("#MechJeb_Ascent_status13", string.Format("{0:F2}", dv)); //Vertical ascent  <<1>>m/s to go
            }
        }

        private void DrivePitchProgram()
        {
            double dt = MET - _pitchStartTime;
            double theta = dt * AscentSettings.PitchRate;
            double pitch = 90 - theta;
            
            // we need to initiate by at least 10 degrees, then transition to zerolift when srfvel catches up
            if (vesselState.currentPitch > SrfvelPitch() && vesselState.currentPitch < 80)
            {
                _mode = AscentMode.ZEROLIFT;
                return;
            }

            if (!AscentSettings.StagingTriggerFlag)
                Status = Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - core.guidance.Pitch:F}"); //Pitch program <<1>>° to guidance
            else
                Status = $"Pitch Program until stage {AscentSettings.StagingTrigger.val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            AttitudeTo(pitch, core.guidance.Heading);
        }

        private bool CheckForGuidanceTransition(double pitch)
        {
            if (!mainBody.atmosphere) return true;

            if (!AscentSettings.StagingTriggerFlag)
            {
                if (pitch <= core.guidance.Pitch && core.guidance.IsStable()) return true;

                // dynamic pressure needs to fall by 10% before we level trigger
                if (vesselState.maxDynamicPressure > vesselState.dynamicPressure * 1.1)
                    if (vesselState.dynamicPressure < AscentSettings.DynamicPressureTrigger)
                        return true;
            }
            else
            {
                if (core.guidance.IsStable() && vessel.currentStage >= AscentSettings.StagingTrigger) return true;
            }

            return false;
        }

        private void DriveZeroLift()
        {
            double pitch = SrfvelPitch();

            if (!AscentSettings.StagingTriggerFlag)
                Status = Localizer.Format("#MechJeb_Ascent_status14", $"{pitch - core.guidance.Pitch:F}"); //Gravity Turn <<1>>° to guidance
            else
                Status = $"Gravity Turn until stage {AscentSettings.StagingTrigger.val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            AttitudeTo(pitch, core.guidance.Heading);
        }

        private void DriveGuidance()
        {
            if (core.guidance.Status == PVGStatus.FINISHED)
            {
                _mode = AscentMode.EXIT;
                return;
            }

            if (!core.guidance.IsStable())
            {
                double pitch = Math.Min(Math.Min(90, SrfvelPitch()), vesselState.vesselPitch);
                AttitudeTo(pitch, SrfvelHeading());
                Status = Localizer.Format("#MechJeb_Ascent_status16"); //"WARNING: Unstable Guidance"
            }
            else
            {
                Status = Localizer.Format("#MechJeb_Ascent_status17"); //"Stable Guidance"
                AttitudeTo(core.guidance.Pitch, core.guidance.Heading);
            }
        }
    }
}
