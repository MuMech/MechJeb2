using System;
using KSP.UI.Screens;
using UnityEngine;

namespace MuMech
{
    //Todo: -reimplement measurement of LPA
    public class MechJebModuleAscentClassic : MechJebModuleAscentBase
    {
        public MechJebModuleAscentClassic(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartAltitude = new EditableDoubleMult(500, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartVelocity = new EditableDoubleMult(100);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnEndAltitude = new EditableDoubleMult(60000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDouble turnEndAngle = 0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnShapeExponent = new EditableDoubleMult(0.4, 0.01);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public bool autoPath = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public float autoTurnPerc = 0.05f;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public float autoTurnSpdFactor = 18.5f;

        private double actualTurnStart = 0;

        public override void OnModuleEnabled()
        {
            mode = AscentMode.VERTICAL_ASCENT;
        }

        public override void OnModuleDisabled()
        {
        }

        public double autoTurnStartAltitude
        {
            get
            {
                return (vessel.mainBody.atmosphere ? vessel.mainBody.RealMaxAtmosphereAltitude() * autoTurnPerc : vessel.terrainAltitude + 25);
            }
        }

        public double autoTurnStartVelocity
        {
            get
            {
                return vessel.mainBody.atmosphere ? autoTurnSpdFactor * autoTurnSpdFactor * autoTurnSpdFactor * 0.015625f : double.PositiveInfinity;
            }
        }

        public double autoTurnEndAltitude
        {
            get
            {
                var targetAlt = vessel.GetMasterMechJeb().GetComputerModule<MechJebModuleAscentAutopilot>().desiredOrbitAltitude;
                if (vessel.mainBody.atmosphere)
                {
                    return Math.Min(vessel.mainBody.RealMaxAtmosphereAltitude() * 0.85, targetAlt);
                }
                else
                {
                    return Math.Min(30000, targetAlt * 0.85);
                }
            }
        }

        public double VerticalAscentEnd()
        {
            return autoPath ? autoTurnStartAltitude : turnStartAltitude;
        }

        public double SpeedAscentEnd()
        {
            return autoPath ? autoTurnStartVelocity : turnStartVelocity;
        }

        public bool IsVerticalAscent(double altitude, double velocity)
        {
            actualTurnStart = Math.Min(actualTurnStart, autoTurnStartAltitude);
            if (altitude < VerticalAscentEnd() && velocity < SpeedAscentEnd())
            {
                actualTurnStart = Math.Max(actualTurnStart, altitude);
                return true;
            }
            return false;
        }

        public double FlightPathAngle(double altitude, double velocity)
        {
            var turnEnd = (autoPath ? autoTurnEndAltitude : turnEndAltitude);

            if (IsVerticalAscent(altitude, velocity)) return 90.0;

            if (altitude > turnEnd) return turnEndAngle;

            return Mathf.Clamp((float)(90.0 - Math.Pow((altitude - actualTurnStart) / (turnEnd - actualTurnStart), turnShapeExponent) * (90.0 - turnEndAngle)), 0.01F, 89.99F);
        }

        enum AscentMode { VERTICAL_ASCENT, GRAVITY_TURN, COAST_TO_APOAPSIS, EXIT };
        AscentMode mode;

        public override bool DriveAscent(FlightCtrlState s)
        {
            switch (mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent(s);
                    break;

                case AscentMode.GRAVITY_TURN:
                    DriveGravityTurn(s);
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
            if (!IsVerticalAscent(vesselState.altitudeASL, vesselState.speedSurface)) mode = AscentMode.GRAVITY_TURN;
            if (autopilot.autoThrottle && orbit.ApA > autopilot.desiredOrbitAltitude) mode = AscentMode.COAST_TO_APOAPSIS;

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && vesselState.altitudeBottom > 50);

            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) status = "Awaiting liftoff";
            else status = "Vertical ascent";
        }


        void DriveGravityTurn(FlightCtrlState s)
        {
            //stop the gravity turn when our apoapsis reaches the desired altitude
            if (autopilot.autoThrottle && orbit.ApA > autopilot.desiredOrbitAltitude)
            {
                mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(vesselState.altitudeASL, vesselState.speedSurface))
            {
                mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            if (autopilot.autoThrottle)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, autopilot.desiredOrbitAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    // follow surface velocity to reduce flipping
                    attitudeTo(srfvelPitch());
                    status = "Fine tuning apoapsis";
                    return;
                }
            }

            double desiredFlightPathAngle = FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);

            /* FIXME: this code is now somewhat horrible and overly complicated and should probably just a PID to adjust the
               pitch to try to fix the flight angle */
            if (autopilot.correctiveSteering)
            {
                //transition gradually from the rotating to the non-rotating reference frame. this calculation ensures that
                //when our maximum possible apoapsis, given our orbital energy, is desiredOrbitalRadius, then we are
                //fully in the non-rotating reference frame and thus doing the correct calculations to get the right inclination
                double GM = mainBody.gravParameter;
                double potentialDifferenceWithApoapsis = GM / vesselState.radius - GM / (mainBody.Radius + autopilot.desiredOrbitAltitude);
                double verticalSpeedForDesiredApoapsis = Math.Sqrt(2 * potentialDifferenceWithApoapsis);
                double referenceFrameBlend = Mathf.Clamp((float)(vesselState.speedOrbital / verticalSpeedForDesiredApoapsis), 0.0F, 1.0F);

                Vector3d actualVelocityUnit = ((1 - referenceFrameBlend) * vesselState.surfaceVelocity.normalized
                        + referenceFrameBlend * vesselState.orbitalVelocity.normalized).normalized;

                double desiredHeading = UtilMath.Deg2Rad * OrbitalManeuverCalculator.HeadingForLaunchInclination(vessel, vesselState, autopilot.desiredInclination, autopilot.launchLatitude);
                Vector3d desiredHeadingVector = Math.Sin(desiredHeading) * vesselState.east + Math.Cos(desiredHeading) * vesselState.north;

                Vector3d desiredVelocityUnit = Math.Cos(desiredFlightPathAngle * UtilMath.Deg2Rad) * desiredHeadingVector
                    + Math.Sin(desiredFlightPathAngle * UtilMath.Deg2Rad) * vesselState.up;

                Vector3d desiredThrustVector = desiredVelocityUnit;

                Vector3d velocityError = (desiredVelocityUnit - actualVelocityUnit);

                double difficulty = vesselState.surfaceVelocity.magnitude * 0.02 / vesselState.ThrustAccel(core.thrust.targetThrottle);
                difficulty = MuUtils.Clamp(difficulty, 0.1, 1.0);
                Vector3d steerOffset = autopilot.correctiveSteeringGain * difficulty * velocityError;

                //limit the amount of steering to 10 degrees. Furthermore, never steer to a FPA of > 90 (that is, never lean backward)
                double maxOffset = 10 * UtilMath.Deg2Rad;
                if (desiredFlightPathAngle > 80)
                    maxOffset = (90 - desiredFlightPathAngle) * UtilMath.Deg2Rad;
                if (steerOffset.magnitude > maxOffset)
                    steerOffset = maxOffset * steerOffset.normalized;

                desiredThrustVector += steerOffset;
                desiredFlightPathAngle = 90 - Vector3d.Angle(desiredThrustVector, vesselState.up);
            }

            attitudeTo(desiredFlightPathAngle);

            status = "Gravity turn";
        }

        void DriveCoastToApoapsis(FlightCtrlState s)
        {
            core.thrust.targetThrottle = 0;

            double circularSpeed = OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, orbit.ApR);
            double apoapsisSpeed = orbit.SwappedOrbitalVelocityAtUT(orbit.NextApoapsisTime(vesselState.time)).magnitude;
            double circularizeBurnTime = (circularSpeed - apoapsisSpeed) / vesselState.limitedMaxThrustAccel;

            if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
            {
                mode = AscentMode.EXIT;
                core.warp.MinimumWarp();
                return;
            }

            //if our apoapsis has fallen too far, resume the gravity turn
            if (orbit.ApA < autopilot.desiredOrbitAltitude - 1000.0)
            {
                mode = AscentMode.GRAVITY_TURN;
                core.warp.MinimumWarp();
                return;
            }

            core.thrust.targetThrottle = 0;

            // follow surface velocity to reduce flipping
            attitudeTo(srfvelPitch());

            if (autopilot.autoThrottle && orbit.ApA < autopilot.desiredOrbitAltitude)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, autopilot.desiredOrbitAltitude + mainBody.Radius);
            }

            if (core.node.autowarp)
            {
                //warp at x2 physical warp:
                core.warp.WarpPhysicsAtRate(2);
            }

            status = "Coasting to edge of atmosphere";
        }
    }
}
