using System;
using KSP.Localization;

/*
 * NOTE: THAT THIS IS NOT INTENDED TO BE A PERFECTLY FAITHFUL REIMPLEMENTATION OF
 *       THE GRAVITY TURN MOD FOR KSP.
 */

namespace MuMech
{
    public class MechJebModuleAscentGTAutopilot : MechJebModuleAscentBaseAutopilot
    {
        public MechJebModuleAscentGTAutopilot(MechJebCore core) : base(core) { }

        public override void OnModuleEnabled()
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
            if (!IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface)) _mode            = AscentMode.INITIATE_TURN;
            if (orbit.ApA > AscentSettings.IntermediateAltitude) _mode = AscentMode.GRAVITY_TURN;

            //during the vertical ascent we just thrust straight up at max throttle
            AttitudeTo(90);

            bool liftedOff = vessel.LiftedOff() && !vessel.Landed;

            core.attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && vesselState.altitudeBottom > AscentSettings.RollAltitude);

            core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) Status = Localizer.Format("#MechJeb_Ascent_status6");  //"Awaiting liftoff"
            else Status                                      = Localizer.Format("#MechJeb_Ascent_status18"); //"Vertical ascent"
        }

        private void DriveInitiateTurn()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > AscentSettings.DesiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            if (90 - AscentSettings.TurnStartPitch >= SrfvelPitch())
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            if (orbit.ApA > AscentSettings.IntermediateAltitude)
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface))
            {
                _mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            AttitudeTo(90 - AscentSettings.TurnStartPitch);


                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, AscentSettings.IntermediateAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    Status = Localizer.Format("#MechJeb_Ascent_status19"); //"Fine tuning intermediate altitude"
                    return;
                }


            Status = Localizer.Format("#MechJeb_Ascent_status20"); //"Initiate gravity turn"
        }

        private double FixedTimeToAp()
        {
            if (vessel.orbit.timeToPe < vessel.orbit.timeToAp)
                return vessel.orbit.timeToAp - vessel.orbit.period;
            return vessel.orbit.timeToAp;
        }

        private double _maxholdAPTime;

        private void DriveGravityTurn()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > AscentSettings.DesiredOrbitAltitude)
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
            double pitchfade = MuUtils.Clamp(-20 * vesselState.altitudeASL / AscentSettings.IntermediateAltitude + 19, 0.0, 1.0);

            // srfvelPitch == zero AoA
            AttitudeTo(SrfvelPitch() * pitchfade);

                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, AscentSettings.IntermediateAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    Status = Localizer.Format("#MechJeb_Ascent_status19"); //"Fine tuning intermediate altitude"
                    return;
                }


                Status = Localizer.Format("#MechJeb_Ascent_status22"); //"Gravity turn"
        }

        private void DriveHoldAP()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > AscentSettings.DesiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            // generally this is in response to typing numbers into the intermediate altitude text box and
            // an accidental transition to later in the state machine
            if (orbit.ApA < 0.98 * AscentSettings.IntermediateAltitude)
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            AttitudeTo(0); /* FIXME: corrective steering */

            core.thrust.targetThrottle = FixedTimeToAp() < AscentSettings.HoldAPTime ? 1.0F : 0.1F;

            Status = Localizer.Format("#MechJeb_Ascent_status24"); //"Holding AP"
        }

        private void DriveCoastToApoapsis()
        {
            core.thrust.targetThrottle = 0;

            if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
            {
                _mode = AscentMode.EXIT;
                core.warp.MinimumWarp();
                return;
            }

            //if our apoapsis has fallen too far, resume the gravity turn
            if (orbit.ApA < AscentSettings.DesiredOrbitAltitude - 1000.0)
            {
                _mode = AscentMode.HOLD_AP;
                core.warp.MinimumWarp();
                return;
            }

            core.thrust.targetThrottle = 0;

            // follow surface velocity to reduce flipping
            AttitudeTo(SrfvelPitch());

            if (orbit.ApA < AscentSettings.DesiredOrbitAltitude)
            {
                core.warp.WarpPhysicsAtRate(1);
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, AscentSettings.DesiredOrbitAltitude + mainBody.Radius);
            }
            else
            {
                if (core.node.autowarp)
                {
                    //warp at x2 physical warp:
                    core.warp.WarpPhysicsAtRate(2);
                }
            }

            Status = Localizer.Format("#MechJeb_Ascent_status23"); //"Coasting to edge of atmosphere"
        }
    }
}
