using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSpaceplaneAutopilot : ComputerModule
    {
        public void Autoland(object controller)
        {
            users.Add(controller);
            core.attitude.users.Add(this);
            mode = Mode.AUTOLAND;
            loweredGear = false;
        }

        public void HoldHeadingAndAltitude(object controller)
        {
            users.Add(controller);
            core.attitude.users.Add(this);
            mode = Mode.HOLD;
        }

        public void AutopilotOff()
        {
            mode = Mode.OFF;
            users.Clear();
            core.attitude.attitudeDeactivate();
        }

        public static List<Runway> runways;

        public enum Mode { AUTOLAND, HOLD, OFF };
        public Mode mode = Mode.OFF;

        //autoland parameters
        public Runway runway; //the runway to land at
        public EditableDouble glideslope = 3;      //the FPA to approach at during autoland
        public EditableDouble touchdownPoint = 100; //how many meters down the runway to touch down

        //heading and altitude hold parameters
        public EditableDouble targetAltitude = 1000;
        public EditableDouble targetHeading = 90;

        bool loweredGear = false;

        public override void OnStart(PartModule.StartState state)
        {
            if (runways == null && HighLogic.LoadedSceneIsFlight)
                InitRunwaysList();
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
        }

        public override void Drive(FlightCtrlState s)
        {
            switch (mode)
            {
                case Mode.AUTOLAND:
                    DriveAutoland(s);
                    break;

                case Mode.HOLD:
                    DriveHeadingAndAltitudeHold(s);
                    break;
            }
        }

        public void DriveHeadingAndAltitudeHold(FlightCtrlState s)
        {
            double targetClimbRate = (targetAltitude - vesselState.altitudeASL) / 30.0;
            double targetFlightPathAngle = 180 / Math.PI * Math.Asin(Mathf.Clamp((float)(targetClimbRate / vesselState.speedSurface), (float)Math.Sin(-Math.PI / 9), (float)Math.Sin(Math.PI / 9)));
            AimVelocityVector(targetFlightPathAngle, targetHeading);
        }

        public void DriveAutoland(FlightCtrlState s)
        {
            if (!part.vessel.Landed)
            {
                Vector3d runwayStart = RunwayStart();

                if (!loweredGear && (vesselState.CoM - runwayStart).magnitude < 1000.0)
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
                    loweredGear = true;
                }

                Vector3d vectorToWaypoint = ILSAimDirection();
                double headingToWaypoint = vesselState.HeadingFromDirection(vectorToWaypoint);

                Vector3d vectorToRunway = runwayStart - vesselState.CoM;
                double verticalDistanceToRunway = Vector3d.Dot(vectorToRunway, vesselState.up) + (vesselState.altitudeTrue - vesselState.altitudeBottom);
                double horizontalDistanceToRunway = Math.Sqrt(vectorToRunway.sqrMagnitude - verticalDistanceToRunway * verticalDistanceToRunway);
                double flightPathAngleToRunway = 180 / Math.PI * Math.Atan2(verticalDistanceToRunway, horizontalDistanceToRunway);
                double desiredFPA = Mathf.Clamp((float)(flightPathAngleToRunway + 3 * (flightPathAngleToRunway + glideslope)), -20.0F, 0.0F);

                AimVelocityVector(desiredFPA, headingToWaypoint);
            }
            else
            {
                //keep the plane aligned with the runway:
                Vector3d runwayDir = runway.End(vesselState.CoM) - runway.Start(vesselState.CoM);
                if (Vector3d.Dot(runwayDir, vesselState.forward) < 0) runwayDir *= -1;
                double runwayHeading = 180 / Math.PI * Math.Atan2(Vector3d.Dot(runwayDir, vesselState.east), Vector3d.Dot(runwayDir, vesselState.north));
                core.attitude.attitudeTo(runwayHeading, 0, 0, this);
            }
        }

        public double stableAoA = 0; //we average AoA over time to get an estimate of what pitch will produce what FPA
        public double pitchCorrection = 0; //we average (commanded pitch - actual pitch) over time in order to fix this offset in our commands
        public const float maxYaw = 10.0F;
        public const float maxRoll = 10.0F;
        public const float maxPitchCorrection = 5.0F;
        public const double AoAtimeConstant = 2.0;
        public const double pitchCorrectionTimeConstant = 15.0;

        void AimVelocityVector(double desiredFpa, double desiredHeading)
        {
            //horizontal control
            double velocityHeading = 180 / Math.PI * Math.Atan2(Vector3d.Dot(vesselState.surfaceVelocity, vesselState.east),
                                                                Vector3d.Dot(vesselState.surfaceVelocity, vesselState.north));
            double headingTurn = Mathf.Clamp((float)MuUtils.ClampDegrees180(desiredHeading - velocityHeading), -maxYaw, maxYaw);
            double noseHeading = velocityHeading + headingTurn;
            double noseRoll = (maxRoll / maxYaw) * headingTurn;

            //vertical control
            double nosePitch = desiredFpa + stableAoA + pitchCorrection;

            core.attitude.attitudeTo(noseHeading, nosePitch, noseRoll, this);

            double flightPathAngle = 180 / Math.PI * Math.Atan2(vesselState.speedVertical, vesselState.speedSurfaceHorizontal);
            double AoA = vesselState.vesselPitch - flightPathAngle;
            stableAoA = (AoAtimeConstant * stableAoA + vesselState.deltaT * AoA) / (AoAtimeConstant + vesselState.deltaT); //a sort of integral error

            pitchCorrection = (pitchCorrectionTimeConstant * pitchCorrection + vesselState.deltaT * (nosePitch - vesselState.vesselPitch)) / (pitchCorrectionTimeConstant + vesselState.deltaT);
            pitchCorrection = Mathf.Clamp((float)pitchCorrection, -maxPitchCorrection, maxPitchCorrection);
        }

        Vector3d RunwayStart()
        {
            Vector3d runwayStart = runway.Start(vesselState.CoM);
            Vector3d runwayEnd = runway.End(vesselState.CoM);
            Vector3d runwayDir = (runwayEnd - runwayStart).normalized;
            runwayStart += touchdownPoint * runwayDir;
            return runwayStart;
        }

        public Vector3d ILSAimDirection()
        {
            //positions of the start and end of the runway
            Vector3d runwayStart = RunwayStart();
            Vector3d runwayEnd = runway.End(vesselState.CoM);

            //a coordinate system oriented to the runway
            Vector3d runwayUpUnit = runway.Up();
            Vector3d runwayHorizontalUnit = Vector3d.Exclude(runwayUpUnit, runwayStart - runwayEnd).normalized;
            Vector3d runwayLeftUnit = -Vector3d.Cross(runwayHorizontalUnit, runwayUpUnit).normalized;

            Vector3d vesselUnit = (vesselState.CoM - runwayStart).normalized;

            double leftSpeed = Vector3d.Dot(vesselState.surfaceVelocity, runwayLeftUnit);
            double verticalSpeed = vesselState.speedVertical;
            double horizontalSpeed = vesselState.speedSurfaceHorizontal;
            double flightPathAngle = 180 / Math.PI * Math.Atan2(verticalSpeed, horizontalSpeed);

            double leftDisplacement = Vector3d.Dot(runwayLeftUnit, vesselState.CoM - runwayStart);

            Vector3d vectorToRunway = runwayStart - vesselState.CoM;
            double verticalDistanceToRunway = Vector3d.Dot(vectorToRunway, vesselState.up) + (vesselState.altitudeTrue - vesselState.altitudeBottom);
            double horizontalDistanceToRunway = Math.Sqrt(vectorToRunway.sqrMagnitude - verticalDistanceToRunway * verticalDistanceToRunway);
            double flightPathAngleToRunway = 180 / Math.PI * Math.Atan2(verticalDistanceToRunway, horizontalDistanceToRunway);

            Vector3d aimToward = runwayStart - 3 * leftDisplacement * runwayLeftUnit;
            Vector3d aimDir = aimToward - vesselState.CoM;

            return aimDir;
        }

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
                            start = new Runway.Endpoint { latitude = startLatitude, longitude = startLongitude, altitude = startAltitude },
                            end = new Runway.Endpoint { latitude = endLatitude, longitude = endLongitude, altitude = endAltitude }
                        });
                    }
                }
            }

            // Create a default config file in MJ dir for those ?
            if (!runways.Any(p => p.name == "KSC runway"))
                runways.Add(new Runway //The runway at KSC
                {
                    name = "KSC runway",
                    body = Planetarium.fetch.Home,
                    start = new Runway.Endpoint { latitude = -0.050185, longitude = -74.490867, altitude = 69.01 },
                    end = new Runway.Endpoint { latitude = -0.0485981, longitude = -74.726413, altitude = 69.01 }
                });

            if (!runways.Any(p => p.name == "Island runway"))
                runways.Add(new Runway //The runway on the island off the KSC coast.
                {
                    name = "Island runway",
                    body = Planetarium.fetch.Home,
                    start = new Runway.Endpoint { latitude = -1.517306, longitude = -71.965488, altitude = 133.17 },
                    end = new Runway.Endpoint { latitude = -1.515980, longitude = -71.852408, altitude = 133.17 }
                });
        }

        public MechJebModuleSpaceplaneAutopilot(MechJebCore core) : base(core) { }
    }

    public struct Runway
    {
        public struct Endpoint
        {
            public double latitude;
            public double longitude;
            public double altitude;

            public Vector3d Position()
            {
                //hardcoded to use Kerbin for the moment:
                return FlightGlobals.currentMainBody.GetWorldSurfacePosition(latitude, longitude, altitude);
            }
        }

        public string name;
        public CelestialBody body;
        public Endpoint start;
        public Endpoint end;

        public Vector3d Start(Vector3d approachPosition)
        {
            Vector3d startPos = start.Position();
            Vector3d endPos = end.Position();
            if (Vector3d.Distance(startPos, approachPosition) < Vector3d.Distance(endPos, approachPosition)) return startPos;
            else return endPos;
        }

        public Vector3d End(Vector3d approachPosition)
        {
            Vector3d startPos = start.Position();
            Vector3d endPos = end.Position();
            if (Vector3d.Distance(startPos, approachPosition) < Vector3d.Distance(endPos, approachPosition)) return endPos;
            else return startPos;
        }

        public Vector3d Up()
        {
            return FlightGlobals.currentMainBody.GetSurfaceNVector(start.latitude, start.longitude);
        }
    }
}
