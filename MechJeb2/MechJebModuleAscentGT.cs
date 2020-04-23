using System;
using KSP.UI.Screens;
using UnityEngine;
using KSP.Localization;

/*
 * NOTE: THAT THIS IS NOT INTENDED TO BE A PERFECTLY FAITHFUL REIMPLEMENTATION OF
 *       THE GRAVITY TURN MOD FOR KSP.
 */

namespace MuMech
{
    public class MechJebModuleAscentGT : MechJebModuleAscentBase
    {
        public MechJebModuleAscentGT(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartAltitude = new EditableDoubleMult(500, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartVelocity = new EditableDoubleMult(50);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartPitch = new EditableDoubleMult(25);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult intermediateAltitude = new EditableDoubleMult(45000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult holdAPTime = new EditableDoubleMult(1);

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            maxholdAPTime = 0.0F;
            mode = AscentMode.VERTICAL_ASCENT;
        }

        public override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
        }

        public bool IsVerticalAscent(double altitude, double velocity)
        {
            if (altitude < turnStartAltitude && velocity < turnStartVelocity)
            {
                return true;
            }
            return false;
        }

        enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, HOLD_AP, COAST_TO_APOAPSIS, EXIT };
        AscentMode mode;

        public override bool DriveAscent(FlightCtrlState s)
        {
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

                case AscentMode.HOLD_AP:
                    DriveHoldAP(s);
                    break;

                case AscentMode.COAST_TO_APOAPSIS:
                    DriveCoastToApoapsis(s);
                    break;

                case AscentMode.EXIT:
                    return false;
            }
            return true;
        }

        void DriveVerticalAscent(FlightCtrlState s)
        {
            if (!IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface)) mode = AscentMode.INITIATE_TURN;
            if (autopilot.autoThrottle && orbit.ApA > intermediateAltitude) mode = AscentMode.GRAVITY_TURN;

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && vesselState.altitudeBottom > 50);

            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) status = Localizer.Format("#MechJeb_Ascent_status6");//"Awaiting liftoff"
            else status = Localizer.Format("#MechJeb_Ascent_status18");//"Vertical ascent"
        }

        void DriveInitiateTurn(FlightCtrlState s)
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > autopilot.desiredOrbitAltitude)
            {
                mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            if ((90 - turnStartPitch) >= srfvelPitch())
            {
                mode = AscentMode.GRAVITY_TURN;
                return;
            }

            if (orbit.ApA > intermediateAltitude)
            {
                mode = AscentMode.GRAVITY_TURN;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface))
            {
                mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            attitudeTo(90 - turnStartPitch);

            if (autopilot.autoThrottle)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, intermediateAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    status = Localizer.Format("#MechJeb_Ascent_status19");//"Fine tuning intermediate altitude"
                    return;
                }
            }

            status = Localizer.Format("#MechJeb_Ascent_status20");//"Initiate gravity turn"
        }

        double fixedTimeToAp()
        {
            if ( vessel.orbit.timeToPe < vessel.orbit.timeToAp )
                return vessel.orbit.timeToAp - vessel.orbit.period;
            else
                return vessel.orbit.timeToAp;
        }

        double maxholdAPTime = 0;

        void DriveGravityTurn(FlightCtrlState s)
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > autopilot.desiredOrbitAltitude)
            {
                mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }


            if (fixedTimeToAp() < holdAPTime && maxholdAPTime > holdAPTime)
            {
                mode = AscentMode.HOLD_AP;
                return;
            }

            maxholdAPTime = Math.Max(maxholdAPTime, fixedTimeToAp());

            // fade pitch from AoA at 90% of intermediateAltitude to 0 at 95% of intermediateAltitude
            double pitchfade = MuUtils.Clamp(- 20 * vesselState.altitudeASL / intermediateAltitude + 19, 0.0, 1.0);

            // srfvelPitch == zero AoA
            attitudeTo(srfvelPitch() * pitchfade);

            if (autopilot.autoThrottle)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, intermediateAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    status = Localizer.Format("#MechJeb_Ascent_status19");//"Fine tuning intermediate altitude"
                    return;
                }
            }

            status = Localizer.Format("#MechJeb_Ascent_status22");//"Gravity turn"
        }

        void DriveHoldAP(FlightCtrlState s)
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > autopilot.desiredOrbitAltitude)
            {
                mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            // generally this is in response to typing numbers into the intermediate altitude text box and
            // an accidental transition to later in the state machine
            if (orbit.ApA < 0.98 * intermediateAltitude)
            {
                mode = AscentMode.GRAVITY_TURN;
                return;
            }

            attitudeTo(0);  /* FIXME: corrective steering */

            if (fixedTimeToAp() < holdAPTime)
            {
                core.thrust.targetThrottle = 1.0F;
            }
            else
            {
                core.thrust.targetThrottle = 0.1F;
            }
            status = Localizer.Format("#MechJeb_Ascent_status24");//"Holding AP"
        }

        void DriveCoastToApoapsis(FlightCtrlState s)
        {
            core.thrust.targetThrottle = 0;

            double apoapsisSpeed = orbit.SwappedOrbitalVelocityAtUT(orbit.NextApoapsisTime(vesselState.time)).magnitude;

            if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
            {
                mode = AscentMode.EXIT;
                core.warp.MinimumWarp();
                return;
            }

            //if our apoapsis has fallen too far, resume the gravity turn
            if (orbit.ApA < autopilot.desiredOrbitAltitude - 1000.0)
            {
                mode = AscentMode.HOLD_AP;
                core.warp.MinimumWarp();
                return;
            }

            core.thrust.targetThrottle = 0;

            // follow surface velocity to reduce flipping
            attitudeTo(srfvelPitch());

            if (autopilot.autoThrottle && orbit.ApA < autopilot.desiredOrbitAltitude)
            {
                core.warp.WarpPhysicsAtRate(1);
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, autopilot.desiredOrbitAltitude + mainBody.Radius);
            }
            else
            {
                if (core.node.autowarp)
                {
                    //warp at x2 physical warp:
                    core.warp.WarpPhysicsAtRate(2);
                }
            }

            status = Localizer.Format("#MechJeb_Ascent_status23");//"Coasting to edge of atmosphere"
        }
    }
}
