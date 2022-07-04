using System;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentClassic : MechJebModuleAscentBase
    {
        public MechJebModuleAscentClassic(MechJebCore core) : base(core) { }
        
        private double _actualTurnStart;

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _mode = AscentMode.VERTICAL_ASCENT;
        }

        public override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            _ascentSettings.enabled = false;
        }

        public double VerticalAscentEnd()
        {
            return _ascentSettings.autoPath ? _ascentSettings.autoTurnStartAltitude : base._ascentSettings.turnStartAltitude;
        }

        private double SpeedAscentEnd()
        {
            return _ascentSettings.autoPath ? _ascentSettings.autoTurnStartVelocity : base._ascentSettings.turnStartVelocity;
        }

        private bool IsVerticalAscent(double altitude, double velocity)
        {
            _actualTurnStart = Math.Min(_actualTurnStart, _ascentSettings.autoTurnStartAltitude);
            if (altitude < VerticalAscentEnd() && velocity < SpeedAscentEnd())
            {
                _actualTurnStart = Math.Max(_actualTurnStart, altitude);
                return true;
            }

            return false;
        }

        public double FlightPathAngle(double altitude, double velocity)
        {
            double turnEnd = _ascentSettings.autoPath ? _ascentSettings.autoTurnEndAltitude : _ascentSettings.turnEndAltitude;

            if (IsVerticalAscent(altitude, velocity)) return 90.0;

            if (altitude > turnEnd) return _ascentSettings.turnEndAngle;

            return Mathf.Clamp(
                (float)(90.0 - Math.Pow((altitude - _actualTurnStart) / (turnEnd - _actualTurnStart), _ascentSettings.turnShapeExponent) *
                    (90.0 - _ascentSettings.turnEndAngle)), 0.01F, 89.99F);
        }

        private enum AscentMode { VERTICAL_ASCENT, GRAVITY_TURN, COAST_TO_APOAPSIS, EXIT }

        private AscentMode _mode;

        public override bool DriveAscent(FlightCtrlState s)
        {
            switch (_mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent();
                    break;

                case AscentMode.GRAVITY_TURN:
                    DriveGravityTurn();
                    break;

                case AscentMode.COAST_TO_APOAPSIS:
                    DriveCoastToApoapsis();
                    break;

                case AscentMode.EXIT:
                    return false;
            }

            return true;
        }

        private void DriveVerticalAscent()
        {
            if (!IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface)) _mode                      = AscentMode.GRAVITY_TURN;
            if (base._ascentSettings.autoThrottle && orbit.ApA > base._ascentSettings.desiredOrbitAltitude) _mode = AscentMode.COAST_TO_APOAPSIS;

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);

            bool liftedOff = vessel.LiftedOff() && !vessel.Landed;

            core.attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && vesselState.altitudeBottom > base._ascentSettings.rollAltitude);

            if (base._ascentSettings.autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) status = Localizer.Format("#MechJeb_Ascent_status6");  //"Awaiting liftoff"
            else status                                      = Localizer.Format("#MechJeb_Ascent_status18"); //"Vertical ascent"
        }

        private void DriveGravityTurn()
        {
            //stop the gravity turn when our apoapsis reaches the desired altitude
            if (base._ascentSettings.autoThrottle && orbit.ApA > base._ascentSettings.desiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface))
            {
                _mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            if (base._ascentSettings.autoThrottle)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, base._ascentSettings.desiredOrbitAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    // follow surface velocity to reduce flipping
                    attitudeTo(srfvelPitch());
                    status = Localizer.Format("#MechJeb_Ascent_status21"); //"Fine tuning apoapsis"
                    return;
                }
            }

            double desiredFlightPathAngle = FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);

            if (base._ascentSettings.correctiveSteering)
            {
                double actualFlightPathAngle = Math.Atan2(vesselState.speedVertical, vesselState.speedSurfaceHorizontal) * UtilMath.Rad2Deg;

                /* form an isosceles triangle with unit vectors pointing in the desired and actual flight path angle directions and find the length of the base */
                double velocityError = 2 * Math.Sin(UtilMath.Deg2Rad * (desiredFlightPathAngle - actualFlightPathAngle) / 2);

                double difficulty = vesselState.surfaceVelocity.magnitude * 0.02 / vesselState.ThrustAccel(core.thrust.targetThrottle);
                difficulty = MuUtils.Clamp(difficulty, 0.1, 1.0);
                double steerOffset = base._ascentSettings.correctiveSteeringGain * difficulty * velocityError;

                double steerAngle = MuUtils.Clamp(Math.Asin(steerOffset) * UtilMath.Rad2Deg, -30, 30);

                desiredFlightPathAngle = MuUtils.Clamp(desiredFlightPathAngle + steerAngle, -90, 90);
            }

            attitudeTo(desiredFlightPathAngle);

            status = Localizer.Format("#MechJeb_Ascent_status22"); //"Gravity turn"
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
            if (orbit.ApA < base._ascentSettings.desiredOrbitAltitude - 1000.0)
            {
                _mode = AscentMode.GRAVITY_TURN;
                core.warp.MinimumWarp();
                return;
            }

            core.thrust.targetThrottle = 0;

            // follow surface velocity to reduce flipping
            attitudeTo(srfvelPitch());

            if (base._ascentSettings.autoThrottle && orbit.ApA < base._ascentSettings.desiredOrbitAltitude)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, base._ascentSettings.desiredOrbitAltitude + mainBody.Radius);
            }

            if (core.node.autowarp)
            {
                //warp at x2 physical warp:
                core.warp.WarpPhysicsAtRate(2);
            }

            status = Localizer.Format("#MechJeb_Ascent_status23"); //"Coasting to edge of atmosphere"
        }
    }
}
