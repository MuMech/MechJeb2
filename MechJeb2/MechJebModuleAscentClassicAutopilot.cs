extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;


namespace MuMech
{
    public class MechJebModuleAscentClassicAutopilot : ComputerModule, IAscentAutopilot
    {
        public MechJebModuleAscentClassicAutopilot(MechJebCore core) : base(core) { }

        public AscentStatus Status { get; } = new AscentStatus();

        UserPool IAscentAutopilot.Users => Users;

        private MechJebModuleAscentSettings AscentSettings => Core.AscentSettings;

        private MechJebModuleLaunchCountdown Countdown => Core.LaunchCountdown;

        private double _actualTurnStart;

        // the altitude above sea level the vessel was engaged at on the ground (0 when engaged in flight): from a site above the
        // turn start altitude the turn starts there, not from sea level
        private double _launchAltitude;

        protected override void OnModuleEnabled()
        {
            _placedCircularizeNode = false;
            _mode = AscentMode.PRELAUNCH;
            _launchAltitude = Vessel.LandedOrSplashed ? Math.Max(VesselState.AltitudeASL, 0) : 0;
            _actualTurnStart = _launchAltitude;

            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            if (AscentSettings.Autostage) Core.Staging.Users.Add(this);
            Countdown.Users.Add(this);

            Status.Set(Localizer.Format("#MechJeb_Ascent_status1")); //"Pre Launch"
        }

        protected override void OnModuleDisabled()
        {
            Core.Attitude.AttitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);
            Countdown.Users.Remove(this);
            Countdown.Abort();

            Status.Set(Localizer.Format("#MechJeb_Ascent_status2")); //"Off"

            if (_placedCircularizeNode) Core.Node.Abort();
            if (_mode == AscentMode.COAST_TO_APOAPSIS) Core.Warp.MinimumWarp();
        }

        public double VerticalAscentEnd() => AscentSettings.AutoPath ? AscentSettings.AutoTurnStartAltitude : AscentSettings.TurnStartAltitude;

        // An orbit inside the atmosphere would never end the coast to its edge, and the rocket would fall back: aim at least
        // 1 km above it.
        private double OrbitAltitude =>
            MainBody.atmosphere ? Math.Max(AscentSettings.DesiredOrbitAltitude, MainBody.RealMaxAtmosphereAltitude() + 1000) : AscentSettings.DesiredOrbitAltitude;

        // a turn shape of 0 drops the pitch to the end angle the moment the turn starts
        private const double MIN_TURN_SHAPE_EXPONENT = 0.1;

        private double SpeedAscentEnd() => AscentSettings.AutoPath ? AscentSettings.AutoTurnStartVelocity : AscentSettings.TurnStartVelocity;

        private bool IsVerticalAscent(double altitude, double velocity)
        {
            _actualTurnStart = Math.Min(_actualTurnStart, Math.Max(VerticalAscentEnd(), _launchAltitude));
            if (altitude < VerticalAscentEnd() && velocity < SpeedAscentEnd())
            {
                _actualTurnStart = Math.Max(_actualTurnStart, altitude);
                return true;
            }

            return false;
        }

        public double FlightPathAngle(double altitude, double velocity)
        {
            if (IsVerticalAscent(altitude, velocity)) return 90.0;

            return TurnAngle(altitude, _actualTurnStart);
        }

        /// <summary>
        ///     The planned path for the Ascent Path Editor's picture: vertical up to the turn start altitude, then the turn from
        ///     there. Unlike FlightPathAngle it neither reads nor moves the turn start of a flight in progress.
        /// </summary>
        public double PlannedFlightPathAngle(double altitude)
        {
            double turnStart = VerticalAscentEnd();
            return altitude < turnStart ? 90.0 : TurnAngle(altitude, turnStart);
        }

        private double TurnAngle(double altitude, double turnStart)
        {
            double turnEnd = AscentSettings.AutoPath ? AscentSettings.AutoTurnEndAltitude : AscentSettings.TurnEndAltitude;

            if (altitude > turnEnd) return AscentSettings.TurnEndAngle;

            // below the altitude the turn started at (falling back, or a different altitude reference) Pow of a negative base is NaN
            double turnFraction = altitude > turnStart ? (altitude - turnStart) / (turnEnd - turnStart) : 0;

            double turnShape = Math.Max(AscentSettings.TurnShapeExponent, MIN_TURN_SHAPE_EXPONENT);

            return Mathf.Clamp((float)(90.0 - Math.Pow(turnFraction, turnShape) * (90.0 - AscentSettings.TurnEndAngle)),
                0.01F, 89.99F);
        }

        private enum AscentMode { PRELAUNCH, VERTICAL_ASCENT, GRAVITY_TURN, COAST_TO_APOAPSIS, CIRCULARIZE }

        private AscentMode _mode;

        public override void Drive(FlightCtrlState s)
        {
            AscentSettings.LimitingAoA = false;

            if (_mode == AscentMode.PRELAUNCH)
            {
                if (Countdown.DrivePrelaunch(Status)) _mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            if (Countdown.HoldForLiftoff(Status)) return;

            Core.Solarpanel.DriveForAscent();
            Core.AntennaControl.DriveForAscent();

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

                case AscentMode.CIRCULARIZE:
                    if (!DriveCircularizationBurn()) Users.Clear();
                    break;
            }
        }

        private void DriveVerticalAscent()
        {
            if (!IsVerticalAscent(VesselState.AltitudeASL, VesselState.SpeedSurface)) _mode = AscentMode.GRAVITY_TURN;
            if (Orbit.ApA > OrbitAltitude) _mode = AscentMode.COAST_TO_APOAPSIS;

            Core.Attitude.AttitudeToVerticalLaunch(
                OrbitalManeuverCalculator.HeadingForLaunchInclination(Vessel.orbit, AscentSettings.DesiredInclination, OrbitAltitude),
                AscentSettings.ForceRoll && VesselState.AltitudeBottom > AscentSettings.RollAltitude, AscentSettings.VerticalRoll, this);

            Core.Thrust.TargetThrottle = 1.0F;

            if (!Vessel.LiftedOff() || Vessel.Landed) Status.Set(Localizer.Format("#MechJeb_Ascent_status6")); //"Awaiting liftoff"
            else Status.Set(Localizer.Format("#MechJeb_Ascent_status18")); //"Vertical ascent"
        }

        private double _desiredHeading;
        private double _desiredPitch;

        private void DriveGravityTurn()
        {
            //stop the gravity turn when our apoapsis reaches the desired altitude
            if (Orbit.ApA > OrbitAltitude)
            {
                _mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(VesselState.AltitudeASL, VesselState.SpeedSurface))
            {
                _mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            Core.Thrust.TargetThrottle = ThrottleToRaiseApoapsis(Orbit.ApR, OrbitAltitude + MainBody.Radius);
            if (Core.Thrust.TargetThrottle < 1.0F)
            {
                Core.Attitude.LimitedAttitudeTo(_desiredHeading, _desiredPitch * UtilMath.Rad2Deg, AscentSettings.ForceRoll, AscentSettings.TurnRoll,
                    ApplyAoALimiter, this);
                Status.Set(Localizer.Format("#MechJeb_Ascent_status21")); //"Fine tuning apoapsis"
                return;
            }

            _desiredPitch = FlightPathAngle(VesselState.AltitudeASL, VesselState.SpeedSurface) * UtilMath.Deg2Rad;

            if (AscentSettings.CorrectiveSteering)
            {
                double actualFlightPathAngle = Math.Atan2(VesselState.SpeedVertical, VesselState.SpeedSurfaceHorizontal);

                double fpaError = _desiredPitch - actualFlightPathAngle;

                double difficulty = VesselState.SurfaceVelocity.magnitude * 0.02 / VesselState.ThrustAccel(Core.Thrust.TargetThrottle);
                difficulty = Clamp(difficulty, 0.1, 1.0);

                double steerOffset = AscentSettings.CorrectiveSteeringGain * difficulty * fpaError;

                double steerAngle = Clamp(SafeAsin(steerOffset), -Math.PI / 6, Math.PI / 6);

                _desiredPitch = Clamp(_desiredPitch + steerAngle, -Math.PI / 2, Math.PI / 2);
            }

            _desiredHeading = OrbitalManeuverCalculator.HeadingForLaunchInclination(Vessel.orbit, AscentSettings.DesiredInclination, OrbitAltitude);
            Core.Attitude.LimitedAttitudeTo(_desiredHeading, _desiredPitch * UtilMath.Rad2Deg, AscentSettings.ForceRoll, AscentSettings.TurnRoll,
                ApplyAoALimiter, this);

            Status.Set(Localizer.Format("#MechJeb_Ascent_status22")); //"Gravity turn"
        }

        private void DriveCoastToApoapsis()
        {
            if (VesselState.AltitudeASL > MainBody.RealMaxAtmosphereAltitude())
            {
                Core.Thrust.ThrustOff();
                _mode = AscentMode.CIRCULARIZE;
                Core.Warp.MinimumWarp();
                return;
            }

            // follow surface velocity to reduce flipping
            Core.Attitude.LimitedAttitudeTo(VesselState.OrbitalVelocity, AscentSettings.ForceRoll, AscentSettings.TurnRoll, ApplyAoALimiter, this);

            if (Orbit.ApA < OrbitAltitude)
                Core.Thrust.TargetThrottle = ThrottleToRaiseApoapsis(Orbit.ApR, OrbitAltitude + MainBody.Radius);
            else
                Core.Thrust.ThrustOff();

            if (Core.Node.Autowarp)
                Core.Warp.WarpPhysicsAtRate(2); // 2x physics warp

            Status.Set(Localizer.Format("#MechJeb_Ascent_status23")); //"Coasting to edge of atmosphere"
        }

        private bool _placedCircularizeNode;

        private bool DriveCircularizationBurn()
        {
            if (!Vessel.patchedConicsUnlocked() || AscentSettings.SkipCircularization)
                return false;

            if (_placedCircularizeNode)
            {
                //finished circularize
                if (Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    return false;

                // the node executor stopped with the node left (knocked off the burn, or aborted by the player): the
                // ascent ends there and leaves the node to the player, rather than hold the throttle at 0
                if (!Core.Node.Enabled)
                    return false;
            }
            else
            {
                //place circularization node
                Vessel.RemoveAllManeuverNodes();
                double ut = Orbit.NextApoapsisTime(VesselState.Time);
                //During the circularization burn, try to correct any inclination errors because it's better to combine the two burns.
                //  For example, if you're about to do a 1500 m/s circularization burn, if you combine a 200 m/s inclination correction
                //  into it, you actually only spend 1513 m/s to execute combined manuver.  Mechjeb should also do correction burns before
                //  this if possible, and this can't correct all errors... but it's better then nothing.
                //   (A better version of this should try to match inclination & LAN if target is specified)
                // FIXME? this inclination correction is unlikely to be at tha AN/DN and will throw the LAN off with anything other than high
                // TWR launches from equatorial launch sites -- should probably be made optional (or clip it if the correction is too large).
                Vector3d inclinationCorrection =
                    OrbitalManeuverCalculator.DeltaVToChangeInclination(Orbit, ut, AscentSettings.DesiredInclination);
                // Circularize on the perturbed orbit rather than fixing the SMA to the desired altitude.  This
                // avoids errors that creep in when the orbit at this point is inaccurate while still circularizing.
                Vector3d circularizeCorrection =
                    OrbitalManeuverCalculator.DeltaVToCircularize(Orbit.PerturbedOrbit(ut, inclinationCorrection), ut);
                Vector3d dV = inclinationCorrection + circularizeCorrection;
                Vessel.PlaceManeuverNode(Orbit, dV, ut);
                _placedCircularizeNode = true;

                Core.Node.ExecuteOneNode(this);
            }

            Status.Set(Localizer.Format(
                Core.Node.State == MechJebModuleNodeExecutor.States.BURN ? "#MechJeb_Ascent_status7" : "#MechJeb_Ascent_status8"));

            return true;
        }

        //data used by ThrottleToRaiseApoapsis
        private float _raiseApoapsisLastThrottle;
        private double _raiseApoapsisLastApR;
        private double _raiseApoapsisLastUT;
        private readonly MovingAverage _raiseApoapsisRatePerThrottle = new MovingAverage(3);

        //gives a throttle setting that reduces as we approach the desired apoapsis
        //so that we can precisely match the desired apoapsis instead of overshooting it
        private float ThrottleToRaiseApoapsis(double currentApR, double finalApR)
        {
            float desiredThrottle;

            if (currentApR > finalApR + 5.0)
            {
                desiredThrottle = 0.0F; //done, throttle down
            }
            else if (_raiseApoapsisLastUT > VesselState.Time - 1)
            {
                //reduce throttle as apoapsis nears target
                double instantRatePerThrottle =
                    (Orbit.ApR - _raiseApoapsisLastApR) / ((VesselState.Time - _raiseApoapsisLastUT) * _raiseApoapsisLastThrottle);
                instantRatePerThrottle = Math.Max(1.0, instantRatePerThrottle); //avoid problems from negative rates
                _raiseApoapsisRatePerThrottle.Value = instantRatePerThrottle;
                double desiredApRate = (finalApR - currentApR) / 1.0;
                desiredThrottle = Mathf.Clamp((float)(desiredApRate / _raiseApoapsisRatePerThrottle), 0.05F, 1.0F);
            }
            else
            {
                desiredThrottle = 1.0F; //no recent data point; just use max thrust.
            }

            //record data for next frame
            _raiseApoapsisLastThrottle = desiredThrottle;
            _raiseApoapsisLastApR = Orbit.ApR;
            _raiseApoapsisLastUT = VesselState.Time;

            return desiredThrottle;
        }

        private Vector3d ApplyAoALimiter(Vector3d desiredThrustVector)
        {
            if (!AscentSettings.LimitAoA)
                return desiredThrustVector;

            double fade = VesselState.DynamicPressure < AscentSettings.AOALimitFadeoutPressure ? AscentSettings.AOALimitFadeoutPressure / VesselState.DynamicPressure : 1;
            AscentSettings.CurrentMaxAoA = IsFinite(fade) ? Math.Min(fade * AscentSettings.MaxAoA, 180d) : 180d;
            AscentSettings.LimitingAoA = Vessel.altitude < MainBody.atmosphereDepth && Vector3d.Angle(VesselState.SurfaceVelocity, desiredThrustVector) > AscentSettings.CurrentMaxAoA;

            if (AscentSettings.LimitingAoA)
                desiredThrustVector = MathExtensions.RotateTowards(VesselState.SurfaceVelocity, desiredThrustVector, (float)(AscentSettings.CurrentMaxAoA * Mathf.Deg2Rad), 1).normalized;

            return desiredThrustVector;
        }
    }
}
