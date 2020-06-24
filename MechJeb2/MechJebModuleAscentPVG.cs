using System;
using KSP.UI.Screens;
using UnityEngine;
using System.Collections.Generic;
using KSP.Localization;

/*
 * Optimized launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPVG : MechJebModuleAscentBase
    {
        public MechJebModuleAscentPVG(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pitchStartVelocity = new EditableDouble(50);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pitchRate = new EditableDouble(0.50);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredApoapsis = new EditableDoubleMult(0, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredAttachAlt = new EditableDoubleMult(0, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool omitCoast = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool attachAltFlag = false;

        private MechJebModuleAscentGuidance ascentGuidance { get { return core.GetComputerModule<MechJebModuleAscentGuidance>(); } }

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            mode = AscentMode.VERTICAL_ASCENT;
            core.guidance.enabled = true;
        }

        public override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            core.guidance.enabled = false;
        }

        private enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GUIDANCE, EXIT };
        private AscentMode mode;

        public override void timedLaunchHook()
        {
            // timedLaunch kills the optimizer so re-enable it here
            core.guidance.enabled = true;
        }

        public override bool DriveAscent(FlightCtrlState s)
        {
            setTarget();
            core.guidance.AssertStart(allow_execution: true);
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

        // convert PeA/ApA values to SMA+ECC
        private void ConvertToSMAEcc(double PeA, double ApA, out double sma, out double ecc)
        {
            double PeR = mainBody.Radius + PeA;
            double ApR = mainBody.Radius + ApA;

            /* remap nonsense ApAs onto circular orbits */
            if ( ApA >= 0 && ApA < PeA )
                ApR = PeR;

            sma = (PeR + ApR) / 2;

            ecc = (ApR - PeR) / (ApR + PeR);
        }

        /*
        private void ConvertToVTRT(double sma, double ecc, double gamma, out double rt, out double vt)
        {
            // All this math comes from symbolic algebraic equation solving in Matlab:
            // f1 = h == cos(gamma) * r * v
            // f2 = v^2 == -mu*(1/a - 2/r)
            // f3 = h == sqrt(mu * a * ( 1 - e^ 2 ))
            // S = solve(f1, f2, f3)
            // and we pick the higher velocity root closer to the periapsis
            double h = Math.Sqrt( mainBody.gravParameter * sma * ( 1 - ecc * ecc ) );
            double cg = Math.Cos( gamma * UtilMath.Deg2Rad );
            double d1 = sma * ( ecc - 1 ) * ( ecc + 1 );
            double d2 = d1 * h * cg;
            double n1 = h * Math.Sqrt( ecc*ecc + cg*cg - 1 ) + h * cg; // FIXME: should check sqrt for imaginary numbers
            vt = - n1 / d1;
            rt = 2 * sma + n1 * sma * sma * ( 1 -  ecc * ecc) / d2;
        }
        */

        private void ConvertToVTRT(double sma, double ecc, double attachAlt, out double gammaT, out double rT, out double vT)
        {
            rT = mainBody.Radius + attachAlt;
            double h = Math.Sqrt( mainBody.gravParameter * sma * ( 1 - ecc * ecc ) );
            vT = Math.Sqrt( mainBody.gravParameter * ( 2 / rT - 1 / sma ) );
            gammaT = Math.Acos( MuUtils.Clamp( h / ( rT * vT ), -1, 1 ) ) * UtilMath.Rad2Deg;
        }

        private void setTarget()
        {
            bool lanflag = false;

            double sma = 0;
            double ecc = 0;
            double vT = 0;
            double rT = 0;
            double gammaT = 0;
            double LAN = 0;

            double attachAlt = desiredAttachAlt;
            if (attachAlt < autopilot.desiredOrbitAltitude)
                attachAlt = autopilot.desiredOrbitAltitude;
            if (attachAlt > desiredApoapsis && desiredApoapsis > autopilot.desiredOrbitAltitude)
                attachAlt = desiredApoapsis;

            ConvertToSMAEcc(autopilot.desiredOrbitAltitude, desiredApoapsis, out sma, out ecc);
            ConvertToVTRT(sma, ecc, attachAltFlag ? attachAlt : autopilot.desiredOrbitAltitude, out gammaT, out rT, out vT);
            double inclination = autopilot.desiredInclination;

            if ( ascentGuidance.launchingToPlane && core.target.NormalTargetExists )
            {
                LAN = core.target.TargetOrbit.LAN;
                inclination = core.target.TargetOrbit.inclination;
                lanflag = true;
            }
            else if ( ascentGuidance.launchingToMatchLAN && core.target.NormalTargetExists )
            {
                LAN = core.target.TargetOrbit.LAN;
                lanflag = true;

            }
            else if ( ascentGuidance.launchingToLAN )
            {
                LAN = autopilot.desiredLAN;
                lanflag = true;
            }

            if ( lanflag )
            {
                if (ecc < 1e-4 || attachAltFlag)
                    core.guidance.flightangle5constraint(rT, vT, inclination, gammaT, LAN, sma, omitCoast, false, true);
                else
                    core.guidance.keplerian4constraintArgPfree(sma, ecc, inclination, LAN, omitCoast, false, true);
            }
            else
            {
                if (ecc < 1e-4 || attachAltFlag)
                    core.guidance.flightangle4constraint(rT, vT, inclination, gammaT, sma, omitCoast, false);
                else
                    core.guidance.keplerian3constraint(sma, ecc, inclination, omitCoast, false);
            }
        }

        private double pitchStartTime;

        private void DriveVerticalAscent(FlightCtrlState s)
        {

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90, core.guidance.heading);

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && (vesselState.altitudeBottom > 50));

            if (!vessel.LiftedOff() || vessel.Landed) {
                status = Localizer.Format("#MechJeb_Ascent_status12");//"Awaiting liftoff"
            }
            else
            {
                if (vesselState.surfaceVelocity.magnitude > pitchStartVelocity)
                {
                    mode = AscentMode.INITIATE_TURN;
                    pitchStartTime = autopilot.MET;
                    return;
                }
                double dv = pitchStartVelocity - vesselState.surfaceVelocity.magnitude;
                status = Localizer.Format("#MechJeb_Ascent_status13", String.Format("{0:F2}", dv));//Vertical ascent  <<1>>m/s to go
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
                status = Localizer.Format("#MechJeb_Ascent_status14", String.Format("{0:F}", pitch - core.guidance.pitch));//Gravity Turn <<1>>° to guidance
            }
            else
            {
                pitch = pitch_program;
                status = Localizer.Format("#MechJeb_Ascent_status15", String.Format("{0:F}", pitch - core.guidance.pitch));//Pitch program <<1>>° to guidance
            }

            if ( pitch <= core.guidance.pitch && core.guidance.isStable() )
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
                pitch = fade * core.guidance.pitch + ( 1.0 - fade ) * pitch;
            }

            attitudeTo(pitch, core.guidance.heading);
        }

        private void DriveGuidance(FlightCtrlState s)
        {
            if ( core.guidance.status == PVGStatus.FINISHED )
            {
                mode = AscentMode.EXIT;
                return;
            }

            if ( !core.guidance.isStable() )
            {
                double pitch = Math.Min(Math.Min(90, srfvelPitch()), vesselState.vesselPitch);
                attitudeTo(pitch, srfvelHeading());
                status = Localizer.Format("#MechJeb_Ascent_status16");//"WARNING: Unstable Guidance"
            }
            else
            {

                status = Localizer.Format("#MechJeb_Ascent_status17");//"Stable Guidance"
                attitudeTo(core.guidance.pitch, core.guidance.heading);
            }
        }
    }
}
