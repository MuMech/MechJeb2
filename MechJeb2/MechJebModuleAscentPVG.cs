using System;
using KSP.Localization;

/*
 * Optimized launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPVG : MechJebModuleAscentBase
    {
        public MechJebModuleAscentPVG(MechJebCore core) : base(core)
        {
        }
        
        private MechJebModuleAscentMenu     _ascentMenu => core.GetComputerModule<MechJebModuleAscentMenu>();

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

        public override void timedLaunchHook()
        {
            // timedLaunch kills the optimizer so re-enable it here
            core.guidance.enabled = true;
        }

        public override bool DriveAscent(FlightCtrlState s)
        {
            SetTarget();
            core.guidance.AssertStart();
            switch (_mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent(s);
                    break;

                case AscentMode.PITCHPROGRAM:
                    DrivePitchProgram(s);
                    break;

                case AscentMode.ZEROLIFT:
                    DriveZeroLift(s);
                    break;

                case AscentMode.GUIDANCE:
                    DriveGuidance(s);
                    break;
            }

            return _mode != AscentMode.EXIT;
        }

        private void SetTarget()
        {
            double peR = mainBody.Radius + _ascentSettings.desiredOrbitAltitude;
            double apR = mainBody.Radius + _ascentSettings.DesiredApoapsis;
            double attR = mainBody.Radius + _ascentSettings.DesiredAttachAlt;

            bool lanflag = _ascentMenu.launchingToPlane || _ascentMenu.launchingToMatchLAN || _ascentMenu.launchingToLAN;
            double lan = _ascentMenu.launchingToPlane || _ascentMenu.launchingToMatchLAN ? core.target.TargetOrbit.LAN : (double)_ascentSettings.desiredLAN;

            double inclination = _ascentSettings.desiredInclination;

            // if we are launchingToPlane other code in MJ fixes the sign of the inclination to be correct
            // FIXME: can we just use autopilot.desiredInclination here and rely on the other code to update that value?
            if (_ascentMenu.launchingToPlane)
                inclination = Math.Sign(inclination) * core.target.TargetOrbit.inclination;

            core.glueball.SetTarget(peR, apR, attR, inclination, lan, _ascentSettings.AttachAltFlag, lanflag);
        }

        private double _pitchStartTime;

        private void DriveVerticalAscent(FlightCtrlState s)
        {
            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90, core.guidance.Heading);

            bool liftedOff = vessel.LiftedOff() && !vessel.Landed && vesselState.altitudeBottom > 5;

            core.attitude.SetActuationControl(liftedOff, liftedOff, liftedOff);
            core.attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && vesselState.altitudeBottom > _ascentSettings.rollAltitude);

            if (!liftedOff)
            {
                status = Localizer.Format("#MechJeb_Ascent_status12"); //"Awaiting liftoff"
            }
            else
            {
                if (vesselState.surfaceVelocity.magnitude > _ascentSettings.PitchStartVelocity)
                {
                    _mode           = AscentMode.PITCHPROGRAM;
                    _pitchStartTime = autopilot.MET;
                    return;
                }

                double dv = _ascentSettings.PitchStartVelocity - vesselState.surfaceVelocity.magnitude;
                status = Localizer.Format("#MechJeb_Ascent_status13", string.Format("{0:F2}", dv)); //Vertical ascent  <<1>>m/s to go
            }
        }

        private void DrivePitchProgram(FlightCtrlState s)
        {
            double dt = autopilot.MET - _pitchStartTime;
            double theta = dt * _ascentSettings.PitchRate;
            double pitch = 90 - theta;
            
            // we need to initiate by at least 10 degrees, then transition to zerolift when srfvel catches up
            if (vesselState.currentPitch > srfvelPitch() && vesselState.currentPitch < 80)
            {
                _mode = AscentMode.ZEROLIFT;
                return;
            }

            if (!_ascentSettings.StagingTriggerFlag)
                status = Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - core.guidance.Pitch:F}"); //Pitch program <<1>>° to guidance
            else
                status = $"Pitch Program until stage {_ascentSettings.StagingTrigger.val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            attitudeTo(pitch, core.guidance.Heading);
        }

        private bool CheckForGuidanceTransition(double pitch)
        {
            if (!mainBody.atmosphere) return true;

            if (!_ascentSettings.StagingTriggerFlag)
            {
                if (pitch <= core.guidance.Pitch && core.guidance.IsStable()) return true;

                // dynamic pressure needs to fall by 10% before we level trigger
                if (vesselState.maxDynamicPressure > vesselState.dynamicPressure * 1.1)
                    if (vesselState.dynamicPressure < _ascentSettings.DynamicPressureTrigger)
                        return true;
            }
            else
            {
                if (core.guidance.IsStable() && vessel.currentStage >= _ascentSettings.StagingTrigger) return true;
            }

            return false;
        }

        private void DriveZeroLift(FlightCtrlState s)
        {
            double pitch = srfvelPitch();

            if (!_ascentSettings.StagingTriggerFlag)
                status = Localizer.Format("#MechJeb_Ascent_status14", $"{pitch - core.guidance.Pitch:F}"); //Gravity Turn <<1>>° to guidance
            else
                status = $"Gravity Turn until stage {_ascentSettings.StagingTrigger.val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            attitudeTo(pitch, core.guidance.Heading);
        }

        private void DriveGuidance(FlightCtrlState s)
        {
            if (core.guidance.Status == PVGStatus.FINISHED)
            {
                _mode = AscentMode.EXIT;
                return;
            }

            if (!core.guidance.IsStable())
            {
                double pitch = Math.Min(Math.Min(90, srfvelPitch()), vesselState.vesselPitch);
                attitudeTo(pitch, srfvelHeading());
                status = Localizer.Format("#MechJeb_Ascent_status16"); //"WARNING: Unstable Guidance"
            }
            else
            {
                status = Localizer.Format("#MechJeb_Ascent_status17"); //"Stable Guidance"
                attitudeTo(core.guidance.Pitch, core.guidance.Heading);
            }
        }
    }
}
