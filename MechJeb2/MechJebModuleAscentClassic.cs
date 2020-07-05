using System;
using KSP.UI.Screens;
using UnityEngine;
using KSP.Localization;

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
            base.OnModuleEnabled();
            mode = AscentMode.VERTICAL_ASCENT;
        }

        public override void OnModuleDisabled()
        {
            base.OnModuleDisabled();
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
            if (!IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface)) mode = AscentMode.GRAVITY_TURN;
            if (autopilot.autoThrottle && orbit.ApA > autopilot.desiredOrbitAltitude) mode = AscentMode.COAST_TO_APOAPSIS;

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && vesselState.altitudeBottom > 50);

            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) status = Localizer.Format("#MechJeb_Ascent_status6");//"Awaiting liftoff"
            else status = Localizer.Format("#MechJeb_Ascent_status18");//"Vertical ascent"
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
            if (IsVerticalAscent(vesselState.altitudeTrue, vesselState.speedSurface))
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
                    status = Localizer.Format("#MechJeb_Ascent_status21");//"Fine tuning apoapsis"
                    return;
                }
            }

            double desiredFlightPathAngle = FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);

            if (autopilot.correctiveSteering)
            {

                double actualFlightPathAngle = Math.Atan2(vesselState.speedVertical, vesselState.speedSurfaceHorizontal) * UtilMath.Rad2Deg;

                /* form an isosceles triangle with unit vectors pointing in the desired and actual flight path angle directions and find the length of the base */
                double velocityError = 2 * Math.Sin( UtilMath.Deg2Rad * ( desiredFlightPathAngle - actualFlightPathAngle ) / 2 );

                double difficulty = vesselState.surfaceVelocity.magnitude * 0.02 / vesselState.ThrustAccel(core.thrust.targetThrottle);
                difficulty = MuUtils.Clamp(difficulty, 0.1, 1.0);
                double steerOffset = autopilot.correctiveSteeringGain * difficulty * velocityError;

                double steerAngle = MuUtils.Clamp(Math.Asin(steerOffset) * UtilMath.Rad2Deg, -30, 30);

                desiredFlightPathAngle = MuUtils.Clamp(desiredFlightPathAngle + steerAngle, -90, 90);
            }

            attitudeTo(desiredFlightPathAngle);

            status = Localizer.Format("#MechJeb_Ascent_status22");//"Gravity turn"
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

            status = Localizer.Format("#MechJeb_Ascent_status23");//"Coasting to edge of atmosphere"
        }
    }
}
