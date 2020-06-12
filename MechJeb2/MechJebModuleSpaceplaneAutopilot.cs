using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleSpaceplaneAutopilot : ComputerModule
    {
        public MechJebModuleSpaceplaneAutopilot(MechJebCore core) : base(core) { }

        public MechJebModuleAirplaneAutopilot Autopilot
        {
            get
            {
                return core.GetComputerModule<MechJebModuleAirplaneAutopilot>();
            }
        }

        public MechJebModuleRoverController RoverPilot
        {
            get
            {
                return core.GetComputerModule<MechJebModuleRoverController>();
            }
        }

        public void Autoland(object controller)
        {
            users.Add(controller);
            Autopilot.users.Add(this);
            RoverPilot.users.Add(this);

            RoverPilot.ControlHeading = false;
            RoverPilot.ControlSpeed = false;

            approachState = AutolandApproachState.START;
            bEngagedReverseThrusters = false;
        }

        public void AutopilotOff()
        {
            users.Clear();
            Autopilot.users.Remove(this);
            RoverPilot.users.Remove(this);
            core.attitude.attitudeDeactivate();

            RoverPilot.ControlHeading = false;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (runways == null && HighLogic.LoadedSceneIsFlight)
                InitRunwaysList();
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
        }

        public enum AutolandApproachState
        {
            START,
            IAP,
            FAP,
            GLIDESLOPEINTERCEPT,
            TOUCHDOWN,
            WAITINGFORFLARE,
            FLARE,
            ROLLOUT
        };

        public AutolandApproachState approachState = AutolandApproachState.START;

        public string AutolandApproachStateToHumanReadableDescription()
        {
            switch (approachState)
            {
                case AutolandApproachState.START:
                    return "";
                case AutolandApproachState.IAP:
                    return Localizer.Format("#MechJeb_ApproAndLand_approachState1");//Proceeding to the initial approach point
                case AutolandApproachState.FAP:
                    return Localizer.Format("#MechJeb_ApproAndLand_approachState2");//Proceeding to the final approach point
                case AutolandApproachState.GLIDESLOPEINTERCEPT:
                    return Localizer.Format("#MechJeb_ApproAndLand_approachState3");//Intercepting the glide slope
                case AutolandApproachState.TOUCHDOWN:
                    return Localizer.Format("#MechJeb_ApproAndLand_approachState4");//Proceeding to touchdown point
                case AutolandApproachState.WAITINGFORFLARE:
                    return Localizer.Format("#MechJeb_ApproAndLand_approachState5");//Waiting for flare
                case AutolandApproachState.FLARE:
                    return Localizer.Format("#MechJeb_ApproAndLand_approachState6");//Flaring
                case AutolandApproachState.ROLLOUT:
                    return Localizer.Format("#MechJeb_ApproAndLand_approachState7");//Rolling out
            }

            return "";
        }

        public override void Drive(FlightCtrlState s)
        {
            Vector3d vectorToWaypoint = GetAutolandTargetVector();

            // Make sure autopilot is enabled properly
            if (!Autopilot.HeadingHoldEnabled)
            {
                Autopilot.EnableHeadingHold();
                Autopilot.HeadingTarget = vesselState.vesselHeading;
            }

            if (Autopilot.AltitudeHoldEnabled)
            {
                Autopilot.DisableAltitudeHold();
            }

            if (!Autopilot.VertSpeedHoldEnabled)
            {
                Autopilot.EnableVertSpeedHold();
                Autopilot.VertSpeedTarget = vesselState.speedVertical;
            }

            if (!Autopilot.SpeedHoldEnabled)
            {
                Autopilot.EnableSpeedHold();
                Autopilot.SpeedTarget = vesselState.speedSurface;
            }

            // Set autopilot target and max values for navigation
            Autopilot.SpeedTarget = GetAutolandTargetSpeed();
            Autopilot.HeadingTarget = RoverPilot.heading = GetAutolandTargetHeading(vectorToWaypoint);
            Autopilot.VertSpeedTarget = GetAutolandTargetVerticalSpeed(vectorToWaypoint);
            Autopilot.RollMax = GetAutolandTargetBankAngle();

            if (approachState == AutolandApproachState.FLARE)
            {
                double exponentPerMeter = (Math.Log(targetFlareAoA + 1) - Math.Log(1)) / startFlareAtAltitude;
                double desiredAoA = Math.Exp((startFlareAtAltitude - vesselState.altitudeTrue) * exponentPerMeter) - 1;

                //core.attitude.attitudeTo(Autopilot.HeadingTarget, Math.Max(desiredAoA, flareStartAoA), 0, this, true, false, false);

                //Autopilot.DisableVertSpeedHold();
            }
            else if (approachState == AutolandApproachState.TOUCHDOWN)
            {
                vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
            }
            else if (approachState == AutolandApproachState.ROLLOUT)
            {
                Autopilot.DisableVertSpeedHold();
                Autopilot.DisableSpeedHold();
                RoverPilot.ControlHeading = true;

                // Smoothen the main gear touchdown
                double exponentPerMeterPerSecond = (Math.Log(touchdownMomentAoA + 1) - Math.Log(1)) / touchdownMomentSpeed;
                double desiredAoA = touchdownMomentAoA - (Math.Exp(exponentPerMeterPerSecond * (touchdownMomentSpeed - vesselState.speedSurfaceHorizontal)) - 1);
                double currentAoA = vesselState.AoA;
                core.attitude.attitudeTo(Autopilot.HeadingTarget, Math.Min(desiredAoA, currentAoA), 0, this, true, false, false);

                // Engage reverse thrusters and full throttle
                SetReverseThrusters(bEngageReverseIfAvailable && vesselState.speedSurfaceHorizontal > 10);

                if (bEngagedReverseThrusters)
                    s.mainThrottle = 1;
                else
                    s.mainThrottle = 0;

                if (bBreakAsSoonAsLanded)
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                }
                else
                {
                    // Apply brakes under 30 (if there are no reversers) otherwise under 10 m/s.
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, bEngagedReverseThrusters ? vesselState.speedSurfaceHorizontal < 10 : vesselState.speedSurfaceHorizontal < 30);

                }

            }
        }

        private void SetReverseThrusters(bool bEngage)
        {
            if (bEngage == bEngagedReverseThrusters)
                return;

            foreach (Part part in vessel.parts)
            {
                if (part.IsEngine())
                {
                    foreach (ModuleAnimateGeneric module in part.FindModulesImplementing<ModuleAnimateGeneric>())
                    {
                        module.Toggle();
                        bEngagedReverseThrusters = bEngage;
                    }
                }
            }
        }

        /// <summary>
        /// Set to true if reverse thrusters are engaged.
        /// </summary>
        private bool bEngagedReverseThrusters = false;

        /// <summary>
        /// Set to true if user wants reverse thrust upon touchdown.
        /// </summary>
        public bool bEngageReverseIfAvailable = true;

        [Persistent(pass = (int)(Pass.Global | Pass.Local))]
        public bool bBreakAsSoonAsLanded = false;

        /// <summary>
        /// The runway to land at.
        /// </summary>
        public Runway runway;

        /// <summary>
        /// Glide slope angle for approach (3-5 seems to work best).
        /// </summary>
        [Persistent(pass = (int)(Pass.Global | Pass.Local))]
        public EditableDouble glideslope = 3.0;

        /// <summary>
        /// The angle between the runway centerline and an intercept to that
        /// line where the lines intersect at the final approach point, on
        /// both sides. This forms an approach where if the vessel is within
        /// the cone, it will align with the final approach point. Otherwise,
        /// it will fly towards the initial approach point and then turn
        /// around and intercept the glide slope for final approach.
        /// </summary>
        private const double lateralApproachConeAngle = 30.0;

        /// <summary>
        /// Final approach distance in meters, at which point the aircraft
        /// should be aligned with the runway and only minor adjustments should
        /// be required.
        /// </summary>
        private const double lateralDistanceFromTouchdownToFinalApproach = 3700;

        /// <summary>
        /// Approach intercept angle; the angle at which the aircraft will
        /// intercept the glide slope laterally.
        /// </summary>
        private const double lateralInterceptAngle = 30.0;

        /// <summary>
        /// Target angle of attack during flare.
        /// </summary>
        private const double targetFlareAoA = 15.0;

        /// <summary>
        /// Altitude in meters when flare will start.
        /// </summary>
        private const double startFlareAtAltitude = 20.0;

        /// <summary>
        /// Rate of turn in degrees per second.
        /// </summary>
        public const double targetRateOfTurn = 3.0;

        /// <summary>
        /// Minimum approach speed in meters per second. Stall + 10 seems to
        /// result in a decent approach and landing.
        /// </summary>
        [Persistent(pass = (int)(Pass.Global | Pass.Local))]
        public EditableDouble approachSpeed = 80.0;

        [Persistent(pass = (int)(Pass.Global | Pass.Local))]
        public EditableDouble touchdownSpeed = 60.0;

        /// <summary>
        /// Maximum allowed bank angle.
        /// </summary>
        [Persistent(pass = (int)(Pass.Global | Pass.Local))]
        public EditableDouble maximumSafeBankAngle = 25.0;

        /// <summary>
        /// Maximum allowed vertical speed in m/s
        /// </summary>
        public const double maximumSafeVerticalSpeed = 15.0;

        /// <summary>
        /// Angle of attack at the start of flare state.
        /// </summary>
        private double flareStartAoA = 0.0;

        /// <summary>
        /// Angle between centerline and aircraft at the point where
        /// the aircraft is perpendicular to the initial approach point
        /// at a distance of turn diameter.
        /// </summary>
        private double angleToFinalApproachPointTurnDiameter = 20.0;

        /// <summary>
        /// Touchdown AoA and speed recorded for smooth main gear touchdown.
        /// </summary>
        private double touchdownMomentAoA = 0.0;
        private double touchdownMomentSpeed = 0.0;

        /// <summary>
        /// Threshold in seconds to move on to the next waypoint.
        /// </summary>
        private double secondsThresholdToNextWaypoint = 5.0;

        public double GetAutolandTargetAltitude(Vector3d vectorToWaypoint)
        {
            double lat, lon, alt;
            runway.body.GetLatLonAlt(vectorToWaypoint, out lat, out lon, out alt);

            return alt;
        }

        public double GetAutolandTargetVerticalSpeed(Vector3d vectorToWaypoint)
        {
            double timeToWaypoint = LateralDistance(vesselState.CoM, vectorToWaypoint) / vesselState.speedSurfaceHorizontal;
            double deltaAlt = GetAutolandTargetAltitude(vectorToWaypoint) - 10 - vesselState.altitudeASL;

            double vertSpeed = deltaAlt / timeToWaypoint;

            // If we are on final, we want to maintain glideslope as much as
            // possible so that we don't overshoot or undershoot the runway.
            if (approachState == AutolandApproachState.TOUCHDOWN || approachState == AutolandApproachState.FAP)
            {
                Vector3d vectorToCorrectPointOnGlideslope = runway.GetPointOnGlideslope(glideslope, LateralDistance(vesselState.CoM, runway.GetVectorToTouchdown()));
                double desiredAlt = GetAutolandTargetAltitude(vectorToCorrectPointOnGlideslope) - 10;
                double deltaToCorrectAlt = desiredAlt - vesselState.altitudeTrue;

                Debug.Assert(vertSpeed < 0);

                if (!UtilMath.Approximately(deltaAlt, 0))
                {
                    double remainingVertSpeedRange = maximumSafeVerticalSpeed - (deltaAlt > 0 ? vertSpeed : -1 * vertSpeed);
                    double expPerMeter = (Math.Log(remainingVertSpeedRange + 1) - Math.Log(1)) / desiredAlt;

                    double adjustment = Math.Exp(expPerMeter * Math.Abs(deltaToCorrectAlt)) - 1;

                    vertSpeed += deltaToCorrectAlt > 0 ? adjustment : -1 * adjustment;
                }
            }

            if (approachState == AutolandApproachState.FLARE)
            {
                vertSpeed = deltaAlt / 8 - 0.2f;
                vertSpeed = UtilMath.Clamp(vertSpeed, -4, 4);
            }

            return UtilMath.Clamp(vertSpeed, -maximumSafeVerticalSpeed, maximumSafeVerticalSpeed);
        }

        public double GetAutolandAlignmentError(Vector3d vectorToWaypoint)
        {
            Vector3d runwayDir = (runway.End() - runway.Start()).normalized;
            return Math.Atan2(Vector3d.Dot(runway.Up(), Vector3d.Cross(vectorToWaypoint, runwayDir)), Vector3d.Dot(vectorToWaypoint, runwayDir)) * UtilMath.Rad2Deg;
        }

        public double GetAutolandTargetHeading(Vector3d vectorToWaypoint)
        {
            double targetHeading = vesselState.HeadingFromDirection(vectorToWaypoint);

            // If we are on final, align with runway and maintain
            switch (approachState)
            {
                case AutolandApproachState.FAP:
                case AutolandApproachState.TOUCHDOWN:
                case AutolandApproachState.WAITINGFORFLARE:
                case AutolandApproachState.ROLLOUT:
                case AutolandApproachState.FLARE:
                {
                    double alignOffset = GetAutolandAlignmentError(vectorToWaypoint);
                    Debug.Assert(alignOffset < lateralInterceptAngle);

                    double exponentPerDegreeOfError = (Math.Log(3) - Math.Log(1)) / lateralInterceptAngle;
                    double offsetMultiplier = Math.Exp((lateralInterceptAngle - Math.Abs(alignOffset)) * exponentPerDegreeOfError);

                    targetHeading -= alignOffset * offsetMultiplier;
                    break;
                }
            }

            return targetHeading;
        }

        public double GetAutolandMaxBankAngle()
        {
            if (approachState == AutolandApproachState.TOUCHDOWN)
                return 10.0;

            return maximumSafeBankAngle;
        }

        public double GetAutolandLateralDistanceFromTouchdownToFinalApproach()
        {
            // Formula is x = cot(omega) * 2r
            return ((1.0 / Math.Tan(angleToFinalApproachPointTurnDiameter * UtilMath.Deg2Rad)) * GetAutolandTurnRadius() * 2.0) + lateralDistanceFromTouchdownToFinalApproach;
        }

        public double GetAutolandTurnRadius()
        {
            // Formula is r = v / (RoT * (pi/180))
            return vesselState.speedSurfaceHorizontal / (GetAutolandMaxRateOfTurn() * UtilMath.Deg2Rad);
        }

        public double GetAutolandMaxRateOfTurn()
        {
            // Formula is RoT = (g * (180/pi) * tan(Bank)) / v
            return (runway.GetGravitationalAcceleration() * UtilMath.Rad2Deg * Math.Tan(GetAutolandTargetBankAngle() * UtilMath.Deg2Rad)) / vesselState.speedSurfaceHorizontal;
        }

        public double GetAutolandTargetBankAngle()
        {
            // Formula is Bank = atan((v * t) / (g * (180/pi)))
            return Math.Min(Math.Atan((vesselState.speedSurfaceHorizontal * targetRateOfTurn) / (runway.GetGravitationalAcceleration() * UtilMath.Rad2Deg)) * UtilMath.Rad2Deg, GetAutolandMaxBankAngle());
        }

        public double GetAutolandTargetSpeed()
        {
            if (vessel.Landed)
                return 0;

            switch (approachState)
            {
                case AutolandApproachState.ROLLOUT:
                    return 0;
            }

            switch (approachState)
            {
                case AutolandApproachState.WAITINGFORFLARE:
                case AutolandApproachState.TOUCHDOWN:
                case AutolandApproachState.FLARE:
                    return touchdownSpeed;
            }

            if (approachState == AutolandApproachState.FAP){
                return (touchdownSpeed + approachSpeed) / 2;
            }

            return approachSpeed;
        }

        public double GetAutolandLateralDistanceToNextWaypoint()
        {
            return LateralDistance(vesselState.CoM, GetAutolandTargetVector());
        }

        /// <summary>
        /// Computes and returns the target vector for approach and autoland.
        /// </summary>
        /// <returns></returns>
        public Vector3d GetAutolandTargetVector()
        {
            // positions of the start and end of the runway
            Vector3d runwayStart = runway.GetVectorToTouchdown();
            Vector3d runwayEnd = runway.End();

            // get the initial and final approach vectors
            Vector3d initialApproachVector = runway.GetPointOnGlideslope(glideslope, GetAutolandLateralDistanceFromTouchdownToFinalApproach() - runway.touchdownPoint);
            Vector3d finalApproachVector = runway.GetPointOnGlideslope(glideslope, lateralDistanceFromTouchdownToFinalApproach - runway.touchdownPoint);

            // determine whether the vessel is within the approach cone or not
            Vector3d finalApproachVectorProjectedOnGroundPlane = finalApproachVector.ProjectOnPlane(runway.Up());
            Vector3d initialApproachVectorProjectedOnGroundPlane = initialApproachVector.ProjectOnPlane(runway.Up());
            Vector3d runwayDirectionVectorProjectedOnGroundPlane = (runwayEnd - runwayStart).ProjectOnPlane(runway.Up());

            double lateralAngleOfFinalApproachVector = Vector3d.Angle(finalApproachVectorProjectedOnGroundPlane, runwayDirectionVectorProjectedOnGroundPlane);
            double lateralAngleOfInitialApproachVector = Vector3d.Angle(initialApproachVectorProjectedOnGroundPlane, runwayDirectionVectorProjectedOnGroundPlane);

            if (approachState == AutolandApproachState.START)
            {
                if (lateralAngleOfFinalApproachVector < lateralApproachConeAngle)
                {
                    // We are within the approach cone, we can skip IAP and
                    // instead start intercepting the glideslope.
                    approachState = AutolandApproachState.GLIDESLOPEINTERCEPT;
                    return FindVectorToGlideslopeIntercept(finalApproachVector, lateralAngleOfFinalApproachVector);
                }

                approachState = AutolandApproachState.IAP;
                return initialApproachVector;
            }
            else if (approachState == AutolandApproachState.IAP)
            {
                if (lateralAngleOfInitialApproachVector > 180 - lateralApproachConeAngle)
                {
                    // We are within the "bad" cone. We have to go all the way
                    // to IAP without cutting corners.
                    return initialApproachVector;
                }

                if (lateralAngleOfFinalApproachVector < lateralApproachConeAngle)
                {
                    // We are in the approach cone, start glideslope intercept.
                    approachState = AutolandApproachState.GLIDESLOPEINTERCEPT;
                    return FindVectorToGlideslopeIntercept(finalApproachVector, lateralAngleOfFinalApproachVector);
                }

                return initialApproachVector;
            }
            else if (approachState == AutolandApproachState.GLIDESLOPEINTERCEPT)
            {
                Vector3d vectorToGlideslopeIntercept = FindVectorToGlideslopeIntercept(finalApproachVector,  lateralAngleOfFinalApproachVector);

                // Determine whether we should start turning towards FAP.
                double estimatedTimeToTurn = lateralInterceptAngle / GetAutolandMaxRateOfTurn();
                double timeToGlideslopeIntercept = LateralDistance(vesselState.CoM, vectorToGlideslopeIntercept) / vesselState.speedSurfaceHorizontal;

                if (estimatedTimeToTurn >= timeToGlideslopeIntercept)
                {
                    approachState = AutolandApproachState.FAP;
                    return finalApproachVector;
                }

                // Otherwise, continue flying towards the glideslope intercept.
                return vectorToGlideslopeIntercept;
            }
            else if (approachState == AutolandApproachState.FAP)
            {
                if (lateralAngleOfFinalApproachVector > lateralInterceptAngle)
                {
                    // Cancel final approach, go back to initial approach.
                    approachState = AutolandApproachState.IAP;
                    return initialApproachVector;
                }

                double timeToFAP = LateralDistance(vesselState.CoM, finalApproachVector) / vesselState.speedSurfaceHorizontal;

                if (GetAutolandAlignmentError(finalApproachVector) < 3.0 && timeToFAP < secondsThresholdToNextWaypoint)
                {
                    approachState = AutolandApproachState.TOUCHDOWN;
                    return runway.GetVectorToTouchdown();
                }

                return finalApproachVector;
            }
            else if (approachState == AutolandApproachState.TOUCHDOWN)
            {
                double timeToTouchdown = LateralDistance(vesselState.CoM, runway.GetVectorToTouchdown()) / vesselState.speedSurfaceHorizontal;

                if (vesselState.altitudeTrue < startFlareAtAltitude + 10)
                {
                    approachState = AutolandApproachState.WAITINGFORFLARE;
                    return runway.End();
                }

                return runway.GetVectorToTouchdown();
            }
            else if (approachState == AutolandApproachState.WAITINGFORFLARE)
            {
                if (vesselState.altitudeTrue < startFlareAtAltitude)
                {
                    approachState = AutolandApproachState.FLARE;
                    flareStartAoA = vesselState.AoA;
                }

                return runway.End();
            }
            else if (approachState == AutolandApproachState.FLARE)
            {
                if (vessel.Landed)
                {
                    touchdownMomentAoA = vesselState.AoA;
                    touchdownMomentSpeed = vesselState.speedSurfaceHorizontal;
                    approachState = AutolandApproachState.ROLLOUT;
                }

                return runway.End();
            }
            else if (approachState == AutolandApproachState.ROLLOUT)
            {
                if (vesselState.speedSurface < 1.0)
                    AutopilotOff();

                return runway.End();
            }

            Debug.Assert(false);
            return runway.Start();
        }

        /// <summary>
        /// Finds a point on the glide slope intercept where the angle between
        /// vessel and the point is lateralInterceptAngle degrees.
        /// </summary>
        /// <param name="finalApproachVector"></param>
        /// <param name="lateralAngleOfFinalApproachVector"></param>
        /// <returns></returns>
        private Vector3d FindVectorToGlideslopeIntercept(Vector3d finalApproachVector, double lateralAngleOfFinalApproachVector)
        {
            // Determine the three angles of the triangle, one of which is
            // the lateral angle of final approach vector, and the other is
            // 180 - lateral intercept angle.
            double theta = 180 - lateralInterceptAngle;
            double omega = 180 - lateralAngleOfFinalApproachVector - theta;

            // We know the lateral distance to the final approach point, we
            // want to find a point on the glide slope which we can
            // intercept at a given angle.
            double dist = (LateralDistance(vesselState.CoM, finalApproachVector) * Math.Sin(UtilMath.Deg2Rad * omega)) / Math.Sin(UtilMath.Deg2Rad * theta);

            // If this is a bad intercept, proceed to IAP.
            if (dist < lateralDistanceFromTouchdownToFinalApproach)
            {
                approachState = AutolandApproachState.IAP;
                return runway.GetPointOnGlideslope(glideslope, GetAutolandLateralDistanceFromTouchdownToFinalApproach() - runway.touchdownPoint);
            }

            return runway.GetPointOnGlideslope(glideslope, dist + lateralDistanceFromTouchdownToFinalApproach);
        }

        private double LateralDistance(Vector3d v1, Vector3d v2)
        {
            return Vector3d.Distance(v1.ProjectOnPlane(runway.Up()), v2.ProjectOnPlane(runway.Up()));
        }

        public static List<Runway> runways;

        private void InitRunwaysList()
        {
            runways = new List<Runway>();

            // Import landing sites form a user createded .cfg
            foreach (var mjConf in GameDatabase.Instance.GetConfigs("MechJeb2Landing"))
            {
                foreach (ConfigNode site in mjConf.config.GetNode("Runways").GetNodes("Runway"))
                {
                    string runwayName = site.GetValue("name");
                    ConfigNode start = site.GetNode("start");
                    ConfigNode end = site.GetNode("end");
                    double touchdown = 0.0;
                    double.TryParse(site.GetValue("touchdown"), out touchdown);

                    if (runwayName == null || start == null || end == null)
                        continue;

                    string lat = start.GetValue("latitude");
                    string lon = start.GetValue("longitude");
                    string alt = start.GetValue("altitude");
                    
                    if (lat == null || lon == null || alt == null)
                        continue;

                    double startLatitude, startLongitude, startAltitude;
                    double.TryParse(lat, out startLatitude);
                    double.TryParse(lon, out startLongitude);
                    double.TryParse(alt, out startAltitude);

                    lat = end.GetValue("latitude");
                    lon = end.GetValue("longitude");
                    alt = end.GetValue("altitude");

                    if (lat == null || lon == null || alt == null)
                        continue;

                    double endLatitude, endLongitude, endAltitude;
                    double.TryParse(lat, out endLatitude);
                    double.TryParse(lon, out endLongitude);
                    double.TryParse(alt, out endAltitude);

                    string bodyName = site.GetValue("body");
                    CelestialBody body = bodyName != null ? FlightGlobals.Bodies.Find(b => b.bodyName == bodyName) : Planetarium.fetch.Home;

                    if (body != null && !runways.Any(p => p.name == runwayName))
                    {
                        runways.Add(new Runway()
                        {
                            name = runwayName,
                            body = body,
                            touchdownPoint = touchdown,
                            start = new Runway.Endpoint { latitude = startLatitude, longitude = startLongitude, altitude = startAltitude },
                            end = new Runway.Endpoint { latitude = endLatitude, longitude = endLongitude, altitude = endAltitude }
                        });
                    }
                }
            }

            // TODO: deploy LandingSites.cfg?
            if (!runways.Any(p => p.name == "KSC Runway 09"))
                runways.Add(new Runway
                {
                    name = "KSC Runway 09",
                    body = Planetarium.fetch.Home,
                    start = new Runway.Endpoint { latitude = -0.0485981, longitude = -74.726413, altitude = 69.01 },
                    end = new Runway.Endpoint { latitude = -0.050185, longitude = -74.490867, altitude = 69.01 },
                    touchdownPoint = 100.0
                });

            if (!runways.Any(p => p.name == "KSC Runway 27"))
                runways.Add(new Runway
                {
                    name = "KSC Runway 27",
                    body = Planetarium.fetch.Home,
                    start = new Runway.Endpoint { latitude = -0.050185, longitude = -74.490867, altitude = 69.01 },
                    end = new Runway.Endpoint { latitude = -0.0485981, longitude = -74.726413, altitude = 69.01 },
                    touchdownPoint = 100.0
                });

            if (!runways.Any(p => p.name == "Island Runway 09"))
                runways.Add(new Runway
                {
                    name = "Island Runway 09",
                    body = Planetarium.fetch.Home,
                    start = new Runway.Endpoint { latitude = -1.517306, longitude = -71.965488, altitude = 133.17 },
                    end = new Runway.Endpoint { latitude = -1.515980, longitude = -71.852408, altitude = 133.17 },
                    touchdownPoint = 25.0
                });

            if (!runways.Any(p => p.name == "Island Runway 27"))
                runways.Add(new Runway
                {
                    name = "Island Runway 27",
                    body = Planetarium.fetch.Home,
                    start = new Runway.Endpoint { latitude = -1.515980, longitude = -71.852408, altitude = 133.17 },
                    end = new Runway.Endpoint { latitude = -1.517306, longitude = -71.965488, altitude = 133.17 },
                    touchdownPoint = 25.0
                });
        }
    }

    public struct Runway
    {
        public struct Endpoint
        {
            public double latitude;
            public double longitude;
            public double altitude;

            public Vector3d Position(CelestialBody body)
            {
                return body.GetWorldSurfacePosition(latitude, longitude, altitude);
            }

            public Vector3d Up(CelestialBody body)
            {
                return body.GetSurfaceNVector(latitude, longitude);
            }
        }

        public string name;
        public double touchdownPoint;
        public CelestialBody body;
        public Endpoint start;
        public Endpoint end;

        public Vector3d Start() { return start.Position(body); }
        public Vector3d End() { return end.Position(body); }
        public Vector3d Up() { return start.Up(body); }

        public double GetGravitationalAcceleration()
        {
            return body.GeeASL * 9.81;
        }

        public Vector3d GetVectorToTouchdown()
        {
            Vector3d runwayStart = Start();
            Vector3d runwayEnd = End();

            Vector3d runwayDir = (runwayEnd - runwayStart).normalized;
            runwayStart += touchdownPoint * runwayDir;

            return runwayStart;
        }

        public Vector3d GetPointOnGlideslope(double glideslope, double distanceOnCenterline)
        {
            Vector3d runwayStart = GetVectorToTouchdown();
            Vector3d runwayEnd = End();

            Vector3d runwayDir = (runwayEnd - runwayStart).normalized;

            Vector3d glideslopeDir = QuaternionD.AngleAxis(glideslope, Vector3d.up) * runwayDir;
            Vector3d pointOnGlideslope = runwayStart - (distanceOnCenterline * glideslopeDir);

            double latAtDistance, lonAtDistance, altAtDistance;
            body.GetLatLonAlt(pointOnGlideslope, out latAtDistance, out lonAtDistance, out altAtDistance);

            double latAtTouchdown, lonAtTouchdown, altAtTouchdown;
            body.GetLatLonAlt(GetVectorToTouchdown(), out latAtTouchdown, out lonAtTouchdown, out altAtTouchdown);

            if (altAtDistance < altAtTouchdown)
            {
                // TODO: can we optimize this?
                glideslopeDir = QuaternionD.AngleAxis(-glideslope, Vector3d.up) * runwayDir;
                pointOnGlideslope = runwayStart - (distanceOnCenterline * glideslopeDir);
            }

            return pointOnGlideslope;
        }
    }
}
