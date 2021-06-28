using System;
using KSP.Localization;
using UnityEngine;

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

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public readonly EditableDouble PitchStartVelocity = new EditableDouble(50);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public readonly EditableDouble PitchRate = new EditableDouble(0.50);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredApoapsis = new EditableDoubleMult(0, 1000);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredAttachAlt = new EditableDoubleMult(0, 1000);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DynamicPressureTrigger = new EditableDoubleMult(10000, 1000);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public readonly EditableInt StagingTrigger = new EditableInt(1);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public bool FixedCoast = true;

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public readonly EditableDouble FixedCoastLength = new EditableDouble(0);

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public bool AttachAltFlag = false;

        [Persistent(pass = (int) (Pass.Type | Pass.Global))]
        public bool StagingTriggerFlag = false;

        private MechJebModuleAscentGuidance AscentGuidance => core.GetComputerModule<MechJebModuleAscentGuidance>();

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _mode                 = AscentMode.VERTICAL_ASCENT;
            core.guidance.enabled = true;
        }

        public override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            core.guidance.enabled = false;
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

        // convert PeA/ApA values to SMA+ECC
        private void ConvertToSMAEcc(double PeA, double ApA, out double sma, out double ecc)
        {
            double PeR = mainBody.Radius + PeA;
            double ApR = mainBody.Radius + ApA;

            /* remap nonsense ApAs onto circular orbits */
            if (ApA >= 0 && ApA < PeA)
                ApR = PeR;

            sma = (PeR + ApR) / 2;

            ecc = (ApR - PeR) / (ApR + PeR);
        }

        private void ConvertToVTRT(double sma, double ecc, double attachAlt, out double gammaT, out double rT, out double vT)
        {
            rT = mainBody.Radius + attachAlt;
            double h = Math.Sqrt(mainBody.gravParameter * sma * (1 - ecc * ecc));
            vT     = Math.Sqrt(mainBody.gravParameter * (2 / rT - 1 / sma));
            gammaT = Math.Acos(MuUtils.Clamp(h / (rT * vT), -1, 1)) * UtilMath.Rad2Deg;
        }

        private void SetTarget()
        {
            bool lanflag = false;

            double sma = 0;
            double ecc = 0;
            double vT = 0;
            double rT = 0;
            double gammaT = 0;
            double LAN = 0;

            double attachAlt = DesiredAttachAlt;
            if (attachAlt < autopilot.desiredOrbitAltitude)
                attachAlt = autopilot.desiredOrbitAltitude;
            if (attachAlt > DesiredApoapsis && DesiredApoapsis > autopilot.desiredOrbitAltitude)
                attachAlt = DesiredApoapsis;

            ConvertToSMAEcc(autopilot.desiredOrbitAltitude, DesiredApoapsis, out sma, out ecc);
            ConvertToVTRT(sma, ecc, AttachAltFlag ? attachAlt : autopilot.desiredOrbitAltitude, out gammaT, out rT, out vT);
            double inclination = autopilot.desiredInclination;

            if (AscentGuidance.launchingToPlane && core.target.NormalTargetExists)
            {
                LAN         = core.target.TargetOrbit.LAN;
                inclination = core.target.TargetOrbit.inclination;
                lanflag     = true;
            }
            else if (AscentGuidance.launchingToMatchLAN && core.target.NormalTargetExists)
            {
                LAN     = core.target.TargetOrbit.LAN;
                lanflag = true;
            }
            else if (AscentGuidance.launchingToLAN)
            {
                LAN     = autopilot.desiredLAN;
                lanflag = true;
            }

            double coastLen = FixedCoast ? FixedCoastLength : -1;
            if (FixedCoast && FixedCoastLength < 0)
                coastLen = 0;

            if (lanflag)
            {
                if (ecc < 1e-4 || AttachAltFlag)
                    core.guidance.flightangle5constraint(rT, vT, inclination, gammaT, LAN, sma, coastLen, false, true);
                else
                    core.guidance.keplerian4constraintArgPfree(sma, ecc, inclination, LAN, coastLen, false, true);
            }
            else
            {
                if (ecc < 1e-4 || AttachAltFlag)
                    core.guidance.flightangle4constraint(rT, vT, inclination, gammaT, sma, coastLen, false);
                else
                    core.guidance.keplerian3constraint(sma, ecc, inclination, coastLen, false);
            }
        }

        private double _pitchStartTime;

        private void DriveVerticalAscent(FlightCtrlState s)
        {
            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90, core.guidance.heading);

            bool liftedOff = vessel.LiftedOff() && !vessel.Landed && vesselState.altitudeBottom > 5;

            core.attitude.SetActuationControl(liftedOff, liftedOff, liftedOff);
            core.attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && vesselState.altitudeBottom > autopilot.rollAltitude);

            if (!liftedOff)
            {
                status = Localizer.Format("#MechJeb_Ascent_status12"); //"Awaiting liftoff"
            }
            else
            {
                if (vesselState.surfaceVelocity.magnitude > PitchStartVelocity)
                {
                    _mode           = AscentMode.PITCHPROGRAM;
                    _pitchStartTime = autopilot.MET;
                    return;
                }

                double dv = PitchStartVelocity - vesselState.surfaceVelocity.magnitude;
                status = Localizer.Format("#MechJeb_Ascent_status13", string.Format("{0:F2}", dv)); //Vertical ascent  <<1>>m/s to go
            }
        }

        private void DrivePitchProgram(FlightCtrlState s)
        {
            double dt = autopilot.MET - _pitchStartTime;
            double theta = dt * PitchRate;
            double pitch = 90 - theta;

            // we need to initiate by at least 10 degrees, then transition to zerolift when srfvel catches up
            if (pitch > srfvelPitch() && pitch < 80)
            {
                _mode = AscentMode.ZEROLIFT;
                return;
            }

            if (!StagingTriggerFlag)
                status = Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - core.guidance.pitch:F}"); //Pitch program <<1>>° to guidance
            else
                status = $"Pitch Program until stage {StagingTrigger.val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            attitudeTo(pitch, core.guidance.heading);
        }

        private bool CheckForGuidanceTransition(double pitch)
        {
            if (!mainBody.atmosphere) return true;

            if (!StagingTriggerFlag)
            {
                if (pitch <= core.guidance.pitch && core.guidance.isStable()) return true;

                // dynamic pressure needs to fall by 10% before we level trigger
                if (vesselState.maxDynamicPressure > vesselState.dynamicPressure * 1.1)
                    if (vesselState.dynamicPressure < DynamicPressureTrigger)
                        return true;
            }
            else
            {
                if (core.guidance.isStable() && core.guidance.solution.arc(0).rocket_stage >= StagingTrigger) return true;
            }

            return false;
        }

        private void DriveZeroLift(FlightCtrlState s)
        {
            double pitch = srfvelPitch();

            if (!StagingTriggerFlag)
                status = Localizer.Format("#MechJeb_Ascent_status14", $"{pitch - core.guidance.pitch:F}"); //Gravity Turn <<1>>° to guidance
            else
                status = $"Gravity Turn until stage {StagingTrigger.val}";

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            attitudeTo(pitch, core.guidance.heading);
        }

        private void DriveGuidance(FlightCtrlState s)
        {
            if (core.guidance.status == PVGStatus.FINISHED)
            {
                _mode = AscentMode.EXIT;
                return;
            }

            if (!core.guidance.isStable())
            {
                double pitch = Math.Min(Math.Min(90, srfvelPitch()), vesselState.vesselPitch);
                attitudeTo(pitch, srfvelHeading());
                status = Localizer.Format("#MechJeb_Ascent_status16"); //"WARNING: Unstable Guidance"
            }
            else
            {
                status = Localizer.Format("#MechJeb_Ascent_status17"); //"Stable Guidance"
                attitudeTo(core.guidance.pitch, core.guidance.heading);
            }
        }
    }
}
