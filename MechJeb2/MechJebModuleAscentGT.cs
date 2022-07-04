using System;
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

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _maxholdAPTime = 0.0F;
            _mode          = AscentMode.VERTICAL_ASCENT;
        }

        private bool IsVerticalAscent(double altitude, double velocity)
        {
            if (altitude < _ascentSettings.turnStartAltitude && velocity < _ascentSettings.turnStartVelocity)
            {
                return true;
            }

            return false;
        }

        private enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, HOLD_AP, COAST_TO_APOAPSIS, EXIT }

        private AscentMode _mode;

        public override bool DriveAscent(FlightCtrlState s)
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
            if (_ascentSettings.autoThrottle && orbit.ApA > _ascentSettings.intermediateAltitude) _mode = AscentMode.GRAVITY_TURN;

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);

            bool liftedOff = vessel.LiftedOff() && !vessel.Landed;

            core.attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && vesselState.altitudeBottom > _ascentSettings.rollAltitude);

            if (_ascentSettings.autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) status = Localizer.Format("#MechJeb_Ascent_status6");  //"Awaiting liftoff"
            else status                                      = Localizer.Format("#MechJeb_Ascent_status18"); //"Vertical ascent"
        }

        private void DriveInitiateTurn()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > _ascentSettings.desiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            if (90 - _ascentSettings.turnStartPitch >= srfvelPitch())
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            if (orbit.ApA > _ascentSettings.intermediateAltitude)
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

            attitudeTo(90 - _ascentSettings.turnStartPitch);

            if (_ascentSettings.autoThrottle)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, _ascentSettings.intermediateAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    status = Localizer.Format("#MechJeb_Ascent_status19"); //"Fine tuning intermediate altitude"
                    return;
                }
            }

            status = Localizer.Format("#MechJeb_Ascent_status20"); //"Initiate gravity turn"
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
            if (orbit.ApA > _ascentSettings.desiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }


            if (FixedTimeToAp() < _ascentSettings.holdAPTime && _maxholdAPTime > _ascentSettings.holdAPTime)
            {
                _mode = AscentMode.HOLD_AP;
                return;
            }

            _maxholdAPTime = Math.Max(_maxholdAPTime, FixedTimeToAp());

            // fade pitch from AoA at 90% of intermediateAltitude to 0 at 95% of intermediateAltitude
            double pitchfade = MuUtils.Clamp(-20 * vesselState.altitudeASL / _ascentSettings.intermediateAltitude + 19, 0.0, 1.0);

            // srfvelPitch == zero AoA
            attitudeTo(srfvelPitch() * pitchfade);

            if (_ascentSettings.autoThrottle)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, _ascentSettings.intermediateAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    status = Localizer.Format("#MechJeb_Ascent_status19"); //"Fine tuning intermediate altitude"
                    return;
                }
            }

            status = Localizer.Format("#MechJeb_Ascent_status22"); //"Gravity turn"
        }

        private void DriveHoldAP()
        {
            //stop the intermediate "burn" when our apoapsis reaches the desired altitude
            if (orbit.ApA > _ascentSettings.desiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            // generally this is in response to typing numbers into the intermediate altitude text box and
            // an accidental transition to later in the state machine
            if (orbit.ApA < 0.98 * _ascentSettings.intermediateAltitude)
            {
                _mode = AscentMode.GRAVITY_TURN;
                return;
            }

            attitudeTo(0); /* FIXME: corrective steering */

            core.thrust.targetThrottle = FixedTimeToAp() < _ascentSettings.holdAPTime ? 1.0F : 0.1F;

            status = Localizer.Format("#MechJeb_Ascent_status24"); //"Holding AP"
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
            if (orbit.ApA < _ascentSettings.desiredOrbitAltitude - 1000.0)
            {
                _mode = AscentMode.HOLD_AP;
                core.warp.MinimumWarp();
                return;
            }

            core.thrust.targetThrottle = 0;

            // follow surface velocity to reduce flipping
            attitudeTo(srfvelPitch());

            if (_ascentSettings.autoThrottle && orbit.ApA < _ascentSettings.desiredOrbitAltitude)
            {
                core.warp.WarpPhysicsAtRate(1);
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, _ascentSettings.desiredOrbitAltitude + mainBody.Radius);
            }
            else
            {
                if (core.node.autowarp)
                {
                    //warp at x2 physical warp:
                    core.warp.WarpPhysicsAtRate(2);
                }
            }

            status = Localizer.Format("#MechJeb_Ascent_status23"); //"Coasting to edge of atmosphere"
        }
    }
}
