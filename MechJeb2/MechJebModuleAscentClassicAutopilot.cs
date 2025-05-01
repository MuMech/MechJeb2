extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentClassicAutopilot : MechJebModuleAscentBaseAutopilot
    {
        public MechJebModuleAscentClassicAutopilot(MechJebCore core) : base(core) { }

        private double _actualTurnStart;

        protected override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _mode = AscentMode.VERTICAL_ASCENT;
        }

        protected override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
            AscentSettings.Enabled = false;
        }

        public double VerticalAscentEnd() => AscentSettings.AutoPath ? AscentSettings.AutoTurnStartAltitude : AscentSettings.TurnStartAltitude;

        private double SpeedAscentEnd() => AscentSettings.AutoPath ? AscentSettings.AutoTurnStartVelocity : AscentSettings.TurnStartVelocity;

        private bool IsVerticalAscent(double altitude, double velocity)
        {
            _actualTurnStart = Math.Min(_actualTurnStart, AscentSettings.AutoTurnStartAltitude);
            if (altitude < VerticalAscentEnd() && velocity < SpeedAscentEnd())
            {
                _actualTurnStart = Math.Max(_actualTurnStart, altitude);
                return true;
            }

            return false;
        }

        public double FlightPathAngle(double altitude, double velocity)
        {
            double turnEnd = AscentSettings.AutoPath ? AscentSettings.AutoTurnEndAltitude : AscentSettings.TurnEndAltitude;

            if (IsVerticalAscent(altitude, velocity)) return 90.0;

            if (altitude > turnEnd) return AscentSettings.TurnEndAngle;

            return Mathf.Clamp(
                (float)(90.0 - Math.Pow((altitude - _actualTurnStart) / (turnEnd - _actualTurnStart), AscentSettings.TurnShapeExponent) *
                    (90.0 - AscentSettings.TurnEndAngle)), 0.01F, 89.99F);
        }

        private enum AscentMode { VERTICAL_ASCENT, GRAVITY_TURN, COAST_TO_APOAPSIS, EXIT }

        private AscentMode _mode;

        protected override bool DriveAscent2()
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
            if (!IsVerticalAscent(VesselState.altitudeTrue, VesselState.speedSurface)) _mode = AscentMode.GRAVITY_TURN;
            if (Orbit.ApA > AscentSettings.DesiredOrbitAltitude) _mode                       = AscentMode.COAST_TO_APOAPSIS;

            VerticalAttitude();

            bool liftedOff = Vessel.LiftedOff() && !Vessel.Landed;

            Core.Attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && AscentSettings.ForceRoll && VesselState.altitudeBottom > AscentSettings.RollAltitude);

            Core.Thrust.TargetThrottle = 1.0F;

            if (!Vessel.LiftedOff() || Vessel.Landed) Status = Localizer.Format("#MechJeb_Ascent_status6");  //"Awaiting liftoff"
            else Status                                      = Localizer.Format("#MechJeb_Ascent_status18"); //"Vertical ascent"
        }

        private double _desiredHeading;
        private double _desiredPitch;

        private void DriveGravityTurn()
        {
            //stop the gravity turn when our apoapsis reaches the desired altitude
            if (Orbit.ApA > AscentSettings.DesiredOrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(VesselState.altitudeTrue, VesselState.speedSurface))
            {
                _mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            Core.Thrust.TargetThrottle = ThrottleToRaiseApoapsis(Orbit.ApR, AscentSettings.DesiredOrbitAltitude + MainBody.Radius);
            if (Core.Thrust.TargetThrottle < 1.0F)
            {
                AttitudeTo(_desiredPitch * UtilMath.Rad2Deg, _desiredHeading);
                Status = Localizer.Format("#MechJeb_Ascent_status21"); //"Fine tuning apoapsis"
                return;
            }

            _desiredPitch = FlightPathAngle(VesselState.altitudeASL, VesselState.speedSurface) * UtilMath.Deg2Rad;

            if (AscentSettings.CorrectiveSteering)
            {
                double actualFlightPathAngle = Math.Atan2(VesselState.speedVertical, VesselState.speedSurfaceHorizontal);

                double fpaError = _desiredPitch - actualFlightPathAngle;

                double difficulty = VesselState.surfaceVelocity.magnitude * 0.02 / VesselState.ThrustAccel(Core.Thrust.TargetThrottle);
                difficulty = MuUtils.Clamp(difficulty, 0.1, 1.0);

                double steerOffset = AscentSettings.CorrectiveSteeringGain * difficulty * fpaError;

                double steerAngle = MuUtils.Clamp(Math.Asin(steerOffset), -Math.PI / 6, Math.PI / 6);

                _desiredPitch = MuUtils.Clamp(_desiredPitch + steerAngle, -Math.PI / 2, Math.PI / 2);
            }

            _desiredHeading = OrbitalManeuverCalculator.HeadingForLaunchInclination(Vessel.orbit, AscentSettings.DesiredInclination, AscentSettings.DesiredOrbitAltitude.Val);
            AttitudeTo(_desiredPitch * UtilMath.Rad2Deg, _desiredHeading);

            Status = Localizer.Format("#MechJeb_Ascent_status22"); //"Gravity turn"
        }

        private void DriveCoastToApoapsis()
        {
            Core.Thrust.TargetThrottle = 0;

            if (VesselState.altitudeASL > MainBody.RealMaxAtmosphereAltitude())
            {
                _mode = AscentMode.EXIT;
                Core.Warp.MinimumWarp();
                return;
            }

            Core.Thrust.TargetThrottle = 0;

            // follow surface velocity to reduce flipping
            AttitudeTo(VesselState.orbitalVelocity);

            if (Orbit.ApA < AscentSettings.DesiredOrbitAltitude)
                Core.Thrust.TargetThrottle = ThrottleToRaiseApoapsis(Orbit.ApR, AscentSettings.DesiredOrbitAltitude + MainBody.Radius);

            if (Core.Node.Autowarp)
                Core.Warp.WarpPhysicsAtRate(2); // 2x physics warp

            Status = Localizer.Format("#MechJeb_Ascent_status23"); //"Coasting to edge of atmosphere"
        }
    }
}
