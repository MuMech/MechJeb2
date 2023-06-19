using System;
using JetBrains.Annotations;
using KSP.Localization;

/*
 * NOTE: THAT THIS IS NOT INTENDED TO BE A PERFECTLY FAITHFUL REIMPLEMENTATION OF
 *       THE GRAVITY TURN MOD FOR KSP.
 */

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleAscentGTAutopilot : MechJebModuleAscentBaseAutopilot
    {
        public MechJebModuleAscentGTAutopilot(MechJebCore core) : base(core) { }

        protected override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _maxholdAPTime = 0.0F;
            _mode          = AscentMode.VERTICAL_ASCENT;
        }

        private bool IsVerticalAscent(double altitude, double velocity)
        {
            if (altitude < AscentSettings.TurnStartAltitude && velocity < AscentSettings.TurnStartVelocity)
            {
                return true;
            }

            return false;
        }

        private enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, HOLD_AP, COAST_TO_APOAPSIS, EXIT }

        private AscentMode _mode;

        protected override bool DriveAscent2()
        {
            switch (_mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent();
                    break;

                case AscentMode.INITIATE_TURN:
                    DriveInitiateTurn();
                    break;

                case AscentMode.GRAVITY_TURN:
                    DriveGravityTurn();
                    break;

                case AscentMode.HOLD_AP:
                    DriveHoldAP();
                    break;

                case AscentMode.COAST_TO_APOAPSIS:
                    DriveCoastToApoapsis();
                    break;

                case AscentMode.EXIT:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        private void DriveVerticalAscent()
        {
            if (!IsVerticalAscent(VesselState.altitudeTrue, VesselState.speedSurface)) _mode = AscentMode.INITIATE_TURN;
            if (Orbit.ApA > AscentSettings.IntermediateAltitude) _mode                       = AscentMode.GRAVITY_TURN;

            //during the vertical ascent we just thrust straight up at max throttle
            AttitudeTo(90);

            bool liftedOff = Vessel.LiftedOff() && !Vessel.Landed;

            Core.Attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && VesselState.altitudeBottom > AscentSettings.RollAltitude);

            Core.Thrust.targetThrottle = 1.0F;

            if (!Vessel.LiftedOff() || Vessel.Landed) Status = Localizer.Format("#MechJeb_Ascent_status6");  //"Awaiting liftoff"
            else Status                                      = Localizer.Format("#MechJeb_Ascent_status18"); //"Vertical ascent"
        }

        private void DriveInitiateTurn()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (Orbit.ApA > AscentSettings.DesiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            if (90 - AscentSettings.TurnStartPitch >= SrfvelPitch())
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            if (Orbit.ApA > AscentSettings.IntermediateAltitude)
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(VesselState.altitudeTrue, VesselState.speedSurface))
            {
                _mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            AttitudeTo(90 - AscentSettings.TurnStartPitch);


            Core.Thrust.targetThrottle = ThrottleToRaiseApoapsis(Orbit.ApR, AscentSettings.IntermediateAltitude + MainBody.Radius);
            if (Core.Thrust.targetThrottle < 1.0F)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status19"); //"Fine tuning intermediate altitude"
                return;
            }


            Status = Localizer.Format("#MechJeb_Ascent_status20"); //"Initiate gravity turn"
        }

        private double FixedTimeToAp()
        {
            if (Vessel.orbit.timeToPe < Vessel.orbit.timeToAp)
                return Vessel.orbit.timeToAp - Vessel.orbit.period;
            return Vessel.orbit.timeToAp;
        }

        private double _maxholdAPTime;

        private void DriveGravityTurn()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (Orbit.ApA > AscentSettings.DesiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }


            if (FixedTimeToAp() < AscentSettings.HoldAPTime && _maxholdAPTime > AscentSettings.HoldAPTime)
            {
                _mode = AscentMode.HOLD_AP;
                return;
            }

            _maxholdAPTime = Math.Max(_maxholdAPTime, FixedTimeToAp());

            // fade pitch from AoA at 90% of intermediateAltitude to 0 at 95% of intermediateAltitude
            double pitchfade = MuUtils.Clamp(-20 * VesselState.altitudeASL / AscentSettings.IntermediateAltitude + 19, 0.0, 1.0);

            // srfvelPitch == zero AoA
            AttitudeTo(SrfvelPitch() * pitchfade);

            Core.Thrust.targetThrottle = ThrottleToRaiseApoapsis(Orbit.ApR, AscentSettings.IntermediateAltitude + MainBody.Radius);
            if (Core.Thrust.targetThrottle < 1.0F)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status19"); //"Fine tuning intermediate altitude"
                return;
            }


            Status = Localizer.Format("#MechJeb_Ascent_status22"); //"Gravity turn"
        }

        private void DriveHoldAP()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (Orbit.ApA > AscentSettings.DesiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            // generally this is in response to typing numbers into the intermediate altitude text box and
            // an accidental transition to later in the state machine
            if (Orbit.ApA < 0.98 * AscentSettings.IntermediateAltitude)
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            AttitudeTo(0); /* FIXME: corrective steering */

            Core.Thrust.targetThrottle = FixedTimeToAp() < AscentSettings.HoldAPTime ? 1.0F : 0.1F;

            Status = Localizer.Format("#MechJeb_Ascent_status24"); //"Holding AP"
        }

        private void DriveCoastToApoapsis()
        {
            Core.Thrust.targetThrottle = 0;

            if (VesselState.altitudeASL > MainBody.RealMaxAtmosphereAltitude())
            {
                _mode = AscentMode.EXIT;
                Core.Warp.MinimumWarp();
                return;
            }

            //if our apoapsis has fallen too far, resume the gravity turn
            if (Orbit.ApA < AscentSettings.DesiredOrbitAltitude - 1000.0)
            {
                _mode = AscentMode.HOLD_AP;
                Core.Warp.MinimumWarp();
                return;
            }

            Core.Thrust.targetThrottle = 0;

            // follow surface velocity to reduce flipping
            AttitudeTo(SrfvelPitch());

            if (Orbit.ApA < AscentSettings.DesiredOrbitAltitude)
            {
                Core.Warp.WarpPhysicsAtRate(1);
                Core.Thrust.targetThrottle = ThrottleToRaiseApoapsis(Orbit.ApR, AscentSettings.DesiredOrbitAltitude + MainBody.Radius);
            }
            else
            {
                if (Core.Node.autowarp)
                {
                    //warp at x2 physical warp:
                    Core.Warp.WarpPhysicsAtRate(2);
                }
            }

            Status = Localizer.Format("#MechJeb_Ascent_status23"); //"Coasting to edge of atmosphere"
        }
    }
}
