using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public void Autoland(object controller)
        {
            users.Add(controller);
            Autopilot.users.Add(this);

            approachState = AutolandApproachState.START;
        }

        public double saveSpeedTarget, saveHeadingTarget, saveVertSpeedTarget, saveRollMax;

        public void AutopilotOff()
        {
            users.Clear();
            Autopilot.users.Remove(this);
            core.attitude.attitudeDeactivate();
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
                    return "Proceeding to the initial approach point";
                case AutolandApproachState.FAP:
                    return "Proceeding to the final approach point";
                case AutolandApproachState.GLIDESLOPEINTERCEPT:
                    return "Intercepting the glide slope";
                case AutolandApproachState.TOUCHDOWN:
                    return "Proceeding to touchdown point";
                case AutolandApproachState.WAITINGFORFLARE:
                    return "Waiting for flare";
                case AutolandApproachState.FLARE:
                    return "Flaring";
                case AutolandApproachState.ROLLOUT:
                    return "Rolling out";
            }

            return "";
        }

        LineRenderer line;

        public override void Drive(FlightCtrlState s)
        {
            if (line == null)
            {
                GameObject obj = new GameObject("Line");

                // Then create renderer itself...
                line = obj.AddComponent<LineRenderer>();
                line.useWorldSpace = true;

                line.material = new Material(Shader.Find("Particles/Additive"));
                line.SetColors(Color.red, Color.red);
                line.SetWidth(1, 0);

                line.SetVertexCount(2);
            }

            Vector3d vectorToWaypoint = GetAutolandTargetVector();

            line.SetPosition(0, Vector3d.zero);
            line.SetPosition(1, vectorToWaypoint);

            // Make sure autopilot is enabled properly
            if (!Autopilot.HeadingHoldEnabled)
            {
                Autopilot.EnableHeadingHold();
                Autopilot.HeadingTarget = vesselState.vesselHeading;
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
            Autopilot.HeadingTarget = GetAutolandTargetHeading(vectorToWaypoint);
            Autopilot.VertSpeedTarget = GetAutolandTargetVerticalSpeed(vectorToWaypoint);
            Autopilot.RollMax = GetAutolandTargetBankAngle();

            if (approachState == AutolandApproachState.FLARE)
            {
                Autopilot.DisableVertSpeedHold();

                double exponentPerMeter = (Math.Log(targetFlareAoA) / startFlareAtAltitude);
                double desiredAoA = Math.Exp((startFlareAtAltitude - vesselState.altitudeTrue) * exponentPerMeter);

                core.attitude.attitudeTo(Autopilot.HeadingTarget, Math.Max(desiredAoA, flareStartAoA), 0, this, true, false, false);
            }
            else if (approachState == AutolandApproachState.TOUCHDOWN)
            {
                vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
            }
            else if (approachState == AutolandApproachState.ROLLOUT)
            {
                Autopilot.DisableSpeedHold();
                core.thrust.ThrustOff();

                vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            }
        }

        /// <summary>
        /// The runway to land at.
        /// </summary>
        public Runway runway;

        /// <summary>
        /// Glide slope angle for approach (3-5 seems to work best).
        /// </summary>
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

        // The initial approach distance. A vessel outside the approach cone
        // will fly towards this point.
        //private const double lateralDistanceFromTouchdownToInitialApproach = 10000.0;

        /// <summary>
        /// Approach intercept angle; the angle at which the aircraft will
        /// intercept the glide slope laterally.
        /// </summary>
        private const double lateralInterceptAngle = 30.0;

        /// <summary>
        /// Target angle of attack during flare.
        /// </summary>
        private const double targetFlareAoA = 10.0;

        /// <summary>
        /// Altitude in meters when flare will start.
        /// </summary>
        private const double startFlareAtAltitude = 30.0;

        /// <summary>
        /// Rate of turn in degrees per second.
        /// </summary>
        public EditableDouble targetRateOfTurn = 3.0;

        /// <summary>
        /// Minimum approach speed in meters per second. Stall + 10 seems to
        /// result in a decent approach and landing.
        /// </summary>
        public EditableDouble minimumApproachSpeed = 60.0;

        /// <summary>
        /// Cruise speed in meters per second.
        /// </summary>
        public EditableDouble cruiseSpeed = 100.0;

        /// <summary>
        /// Maximum safe bank angle the aircraft can handle.
        /// </summary>
        public EditableDouble maximumSafeBankAngle = 25.0;

        /// <summary>
        /// Maximum safe vertical speed the aircraft can handle.
        /// </summary>
        public EditableDouble maximumSafeVerticalSpeed = 20.0;

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

        public double GetAutolandTargetAltitude(Vector3d vectorToWaypoint)
        {
            double lat, lon, alt;
            runway.body.GetLatLonAlt(vectorToWaypoint, out lat, out lon, out alt);

            return alt;
        }

        public double GetAutolandTargetVerticalSpeed(Vector3d vectorToWaypoint)
        {
            double timeToWaypoint = LateralDistance(vesselState.CoM, vectorToWaypoint) / vesselState.speedSurfaceHorizontal;
            double deltaAlt =  GetAutolandTargetAltitude(vectorToWaypoint) - vesselState.altitudeASL;

            return UtilMath.Clamp(deltaAlt / timeToWaypoint, -maximumSafeVerticalSpeed, maximumSafeVerticalSpeed);
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
                case AutolandApproachState.FLARE:
                {
                    Vector3d runwayDir = (runway.End() - runway.Start()).normalized;
                    double alignOffset = Math.Atan2(Vector3d.Dot(runway.Up(), Vector3d.Cross(vectorToWaypoint, runwayDir)), Vector3d.Dot(vectorToWaypoint, runwayDir)) * UtilMath.Rad2Deg;
                    Debug.Assert(alignOffset < lateralInterceptAngle);

                    double exponentPerDegreeOfError = (Math.Log(2.5) - Math.Log(1.0)) / lateralInterceptAngle;
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
            return (1.0 / Math.Tan(angleToFinalApproachPointTurnDiameter * UtilMath.Deg2Rad)) * GetAutolandTurnRadius() * 2.0;
        }

        public double GetAutolandTurnRadius()
        {
            // Formula is r = v / (RoT * (pi/180))
            return vesselState.speedSurfaceHorizontal / (GetAutolandMaxRateOfTurn() * UtilMath.Deg2Rad);
        }

        public double GetAutolandMaxRateOfTurn()
        {
            // Formula is RoT = (g * (180/pi) * tan(Bank)) / v
            return (runway.GetGravitationalAcceleration() * UtilMath.Rad2Deg * Math.Tan(GetAutolandMaxBankAngle() * UtilMath.Deg2Rad)) / vesselState.speedSurfaceHorizontal;
        }

        public double GetAutolandTargetBankAngle()
        {
            // Formula is Bank = atan((v * t) / (g * (180/pi)))
            return Math.Min(Math.Atan((vesselState.speedSurfaceHorizontal * targetRateOfTurn) / (runway.GetGravitationalAcceleration() * UtilMath.Deg2Rad)) * UtilMath.Rad2Deg, GetAutolandMaxRateOfTurn());
        }

        public double GetAutolandTargetSpeed()
        {
            if (vessel.Landed)
                return 0;

            switch (approachState)
            {
                case AutolandApproachState.FAP:
                    return minimumApproachSpeed * 1.5;

                case AutolandApproachState.TOUCHDOWN:
                case AutolandApproachState.WAITINGFORFLARE:
                    return minimumApproachSpeed;

                case AutolandApproachState.ROLLOUT:
                case AutolandApproachState.FLARE:
                    return 0;
            }

            return cruiseSpeed;
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
                double estimatedTimeToTurn = lateralAngleOfFinalApproachVector / GetAutolandMaxRateOfTurn();
                double timeToGlideslopeIntercept = LateralDistance(vesselState.CoM, vectorToGlideslopeIntercept) / vesselState.speedSurfaceHorizontal;

                if (estimatedTimeToTurn >= timeToGlideslopeIntercept || timeToGlideslopeIntercept < 3)
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

                // TODO: this is rather arbitrary
                if (timeToFAP < 3)
                {
                    approachState = AutolandApproachState.TOUCHDOWN;
                    return runway.GetVectorToTouchdown();
                }

                return finalApproachVector;
            }
            else if (approachState == AutolandApproachState.TOUCHDOWN)
            {
                // TODO: also arbitrary
                if (LateralDistance(vesselState.CoM, runway.GetVectorToTouchdown()) < 200.0 || vesselState.altitudeTrue < 50.0)
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
                    approachState = AutolandApproachState.ROLLOUT;

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

            // The approach line 
            Vector3d runwayDir = (runwayEnd - runwayStart).normalized;
            runwayDir = QuaternionD.AngleAxis(Math.Sign(runwayDir.z) * glideslope, Vector3d.up) * runwayDir;

            runwayStart -= distanceOnCenterline * runwayDir;

            return runwayStart;
        }
    }
}
