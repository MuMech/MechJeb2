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
            AutopilotOn();
            maxRoll = 22.5F;
            mode = Mode.AUTOLAND;
            landed = false;
            loweredGear = false;
            brakes = false;
        }

        public void HoldHeadingAndAltitude(object controller)
        {
            users.Add(controller);
            AutopilotOn();
            maxRoll = 22.5F;
            mode = Mode.HOLD;
            landed = false;
            loweredGear = false;
            brakes = false;
        }

        bool autopilotOn = false;
        public void AutopilotOn()
        {
            autopilotOn = true;
        }

        public void AutopilotOff()
        {
            autopilotOn = false;
        }

        public void Abort()
        {
            users.Clear();
            AutopilotOff();
            mode = Mode.OFF;
        }

        public static Runway[] runways = 
        {
            new Runway //The runway at KSC
            { 
                name = "KSC runway",
                start = new Runway.Endpoint { latitude = -0.040633, longitude = -74.6908, altitude = 67 }, 
                end = new Runway.Endpoint { latitude = -0.041774, longitude = -74.5241702, altitude = 67 } 
            },
            new Runway //The runway on the island off the KSC coast.
            { 
                name = "Island runway",
                start = new Runway.Endpoint { latitude = -1.547474, longitude = -71.9611702, altitude = 48 },
                end = new Runway.Endpoint { latitude = -1.530174, longitude = -71.8791702, altitude = 48 }
            }
        };

        public enum Mode { AUTOLAND, HOLD, OFF };
        public Mode mode = Mode.OFF;

        //autoland parameters
        public Runway runway = runways[0]; //the runway to land at
        public EditableDouble glideslope = 3;      //the FPA to approach at during autoland
        public EditableDouble touchdownPoint = 100; //how many meters down the runway to touch down

        //heading and altitude hold parameters
        public EditableDouble targetAltitude = 1000;
        public EditableDouble targetHeading = 90;

        bool loweredGear = false;
        bool brakes = false;

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

        bool gearDown = false;
        public float takeoffPitch = 0.5F;
        public void DriveHeadingAndAltitudeHold(FlightCtrlState s)
        {
            if (!part.vessel.Landed)
            {
                if (!autopilotOn)
                    AutopilotOn();
                if (!gearDown)
                {
                    if (part.vessel.terrainAltitude < 100.0)
                    {
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
                        gearDown = true;
                    }
                }
                else
                {
                    if (part.vessel.terrainAltitude > 100.0)
                    {
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                        gearDown = false;
                    }
                }

                //takeoff or set for flight
                if (landed)
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                    maxRoll = 25.0F;
                    landed = false;
                }
            }
            else
            {
                if (!landed)
                {
                    vessel.ctrlState.mainThrottle = 0;
                    AutopilotOff();
                    landTime = vesselState.time;
                    landed = true;
                }
                //apply breaks a little after touchdown
                if (!brakes && vesselState.time > landTime + 1.0)
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                    brakes = true;
                }
                //if engines are started, disengage brakes and prepare for takeoff
                if (brakes && vessel.ctrlState.mainThrottle > 0.01)
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
                    brakes = false;
                }
                //pitch up for takeoff
                if (!brakes && vesselState.time > landTime + 1.0)
                {
                    vessel.ctrlState.pitch = takeoffPitch;
                }
                vessel.ctrlState.yaw = (float)MuUtils.ClampDegrees180(targetHeading - vesselState.vesselHeading) * 0.1F;
            }

            double targetClimbRate = (targetAltitude - vesselState.altitudeASL) / (30.0 * Math.Pow((CelestialBodyExtensions.RealMaxAtmosphereAltitude(mainBody) / (CelestialBodyExtensions.RealMaxAtmosphereAltitude(mainBody) - vesselState.altitudeASL)), 4));
            double targetFlightPathAngle = 180 / Math.PI * Math.Asin(Mathf.Clamp((float)(targetClimbRate / vesselState.speedSurface), (float)Math.Sin(-Math.PI / 6), (float)Math.Sin(Math.PI / 6)));
            AimVelocityVector(targetFlightPathAngle, targetHeading);
        }

        bool landed = false;
        double landTime = 0;
        public double aimAltitude = 0;
        public double distanceFrom = 0;
        public double runwayHeading = 0;
        public void DriveAutoland(FlightCtrlState s)
        {
            if (!part.vessel.Landed)
            {
                if (landed) landed = false;
                
                Vector3d runwayStart = RunwayStart();

                if (!autopilotOn)
                    AutopilotOn();

                //prepare for landing
                if (!loweredGear && (vesselState.CoM - runwayStart).magnitude < 1000.0)
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
                    loweredGear = true;
                }

                Vector3d vectorToWaypoint = ILSAimDirection();
                double headingToWaypoint = vesselState.HeadingFromDirection(vectorToWaypoint);

                //stop any rolling and aim down runway before touching down
                if ((vesselState.CoM - runwayStart).magnitude < 500.0)
                {
                    Vector3d runwayDir = runway.End(vesselState.CoM) - runway.Start(vesselState.CoM);
                    runwayHeading = 180 / Math.PI * Math.Atan2(Vector3d.Dot(runwayDir, vesselState.east), Vector3d.Dot(runwayDir, vesselState.north));
                    headingToWaypoint = runwayHeading;
                }

                Vector3d vectorToRunway = runwayStart - vesselState.CoM;
                double verticalDistanceToRunway = Vector3d.Dot(vectorToRunway, vesselState.up);
                double horizontalDistanceToRunway = Math.Sqrt(vectorToRunway.sqrMagnitude - verticalDistanceToRunway * verticalDistanceToRunway);
                distanceFrom = horizontalDistanceToRunway;
                if (horizontalDistanceToRunway > 10000)
                {
                    aimAltitude = 1500;
                    double horizontalDifference = 10000;
                    if (horizontalDistanceToRunway > 30000) { aimAltitude = 3000; horizontalDifference = 30000; }
                    if (horizontalDistanceToRunway > 50000) { aimAltitude = 8000; horizontalDifference = 50000; }
                    double setupFPA = 180 / Math.PI * Math.Atan2(aimAltitude - vessel.altitude, horizontalDistanceToRunway - horizontalDifference);

                    AimVelocityVector(setupFPA, headingToWaypoint);
                }
                else
                {
                    double flightPathAngleToRunway = 180 / Math.PI * Math.Atan2(verticalDistanceToRunway, horizontalDistanceToRunway);
                    double desiredFPA = Mathf.Clamp((float)(flightPathAngleToRunway + 3 * (flightPathAngleToRunway + glideslope)), -20.0F, 5.0F);

                    aimAltitude = verticalDistanceToRunway;
                    AimVelocityVector(desiredFPA, headingToWaypoint);
                }
            }
            else
            {
                if (!landed)
                {
                    vessel.ctrlState.mainThrottle = 0;
                    landTime = vesselState.time;
                    AutopilotOff();
                    landed = true;
                }
                //apply breaks a little after touchdown
                if (!brakes && vesselState.time > landTime + 1.0)
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                    vessel.ctrlState.pitch = -1;
                    brakes = true;
                }
                //keep the plane aligned with the runway:
                Vector3d runwayDir = runway.End(vesselState.CoM) - runway.Start(vesselState.CoM);
                if (Vector3d.Dot(runwayDir, vesselState.forward) < 0) runwayDir *= -1;
                runwayHeading = 180 / Math.PI * Math.Atan2(Vector3d.Dot(runwayDir, vesselState.east), Vector3d.Dot(runwayDir, vesselState.north));
                vessel.ctrlState.pitch = -0.5F;
                vessel.ctrlState.yaw = (float)MuUtils.ClampDegrees180(runwayHeading - vesselState.vesselHeading) * 0.1F;
            }
        }

        public float maxRoll = 25.0F;
        public PIDController pitchPID = new PIDController(1, 0.3, 0.1, 25, -10);
        public PIDController pitchCorrectionPID = new PIDController(0.2, 0.05, 0.1, 1, -1);
        public PIDController rollPID = new PIDController(2.5, 0, 0.5);
        public PIDController rollCorrectionPID = new PIDController(0.006, 0, 0.003, 1, -1);
        public LowPass180 rollLowPass = new LowPass180(0.35);
        public double noseRoll = 0;
        public double desiredAoA = 0;
        public double pitchCorrection = 0;
        public double desiredRoll = 0;
        public double rollCorrection = 0;
        public float throttleUpPitch = 0.5F;
        public float throttleDownPitch = 0.0F;
        void AimVelocityVector(double desiredFpa, double desiredHeading)
        {
            if (autopilotOn)
            {
                //horizontal control
                double velocityHeading = 180 / Math.PI * Math.Atan2(Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.east),
                                                                    Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.north));
                noseRoll = rollLowPass.calc(vesselState.vesselRoll);
                desiredRoll = -Mathf.Clamp((float)rollPID.Compute(MuUtils.ClampDegrees180(desiredHeading - velocityHeading)), -maxRoll, maxRoll);
                rollCorrection = Mathf.Clamp((float)rollCorrectionPID.Compute(noseRoll - desiredRoll), (float)rollCorrectionPID.min, (float)rollCorrectionPID.max);

                //vertical control
                double velocityPitch = Math.Atan2(vesselState.speedVertical, vesselState.speedSurface) * (180.0 / Math.PI);
                desiredAoA = Mathf.Clamp((float)pitchPID.Compute(desiredFpa - velocityPitch), (float)pitchPID.min, (float)pitchPID.max);
                pitchCorrection = Mathf.Clamp((float)pitchCorrectionPID.Compute(desiredAoA - (vesselState.vesselPitch - velocityPitch)), (float)pitchCorrectionPID.min, (float)pitchCorrectionPID.max);

                vessel.ctrlState.roll = (float)rollCorrection;
                vessel.ctrlState.pitch = (float)pitchCorrection;

                if (pitchPID.intAccum > 100) pitchPID.intAccum = 100;
                if (pitchPID.intAccum < -100) pitchPID.intAccum = -100;
                if (pitchCorrectionPID.intAccum > 10) pitchCorrectionPID.intAccum = 10;
                if (pitchCorrectionPID.intAccum < -10) pitchCorrectionPID.intAccum = -10;
                //if (rollPID.intAccum > 100) rollPID.intAccum = 100;
                //if (rollPID.intAccum < -100) rollPID.intAccum = -100;
                //if (rollCorrectionPID.intAccum > 10) rollCorrectionPID.intAccum = 10;
                //if (rollCorrectionPID.intAccum < -10) rollCorrectionPID.intAccum = -10;

                //regulate throttle
                if ((vesselState.vesselPitch - velocityPitch) > pitchPID.max * 0.95 && mode == Mode.HOLD)
                    vessel.ctrlState.mainThrottle = 1F;
                else
                {
                    if (pitchCorrection > throttleUpPitch) vessel.ctrlState.mainThrottle += 0.002F * (float)(pitchCorrection - throttleUpPitch) * vessel.ctrlState.mainThrottle;
                    if (pitchCorrection < throttleDownPitch) vessel.ctrlState.mainThrottle += 0.002F * (float)(pitchCorrection - throttleDownPitch) * vessel.ctrlState.mainThrottle;
                }

            }
        }

        public class LowPass360
        {
            public double y_old, alpha, timeConstant, dt;

            public LowPass360(double timeConst)
            {
                this.timeConstant = timeConst;
                clear();
                initialize();
            }

            private void initialize()
            {
                dt = TimeWarp.fixedDeltaTime;
                this.alpha = TimeWarp.fixedDeltaTime / (timeConstant - TimeWarp.fixedDeltaTime);
            }

            public double calc(double x)
            {
                if (dt != TimeWarp.fixedDeltaTime)
                {
                    initialize();
                    y_old = x;
                }
                else
                {
                    y_old = MuUtils.ClampDegrees360(y_old + alpha * MuUtils.ClampDegrees180(x - y_old));
                }

                return y_old;
            }

            public void clear()
            {
                y_old = 0;
            }
        }

        public class LowPass180
        {
            public double y_old, alpha, timeConstant, dt;

            public LowPass180(double timeConst)
            {
                this.timeConstant = timeConst;
                clear();
                initialize();
            }

            private void initialize()
            {
                dt = TimeWarp.fixedDeltaTime;
                this.alpha = TimeWarp.fixedDeltaTime / (timeConstant - TimeWarp.fixedDeltaTime);
            }

            public double calc(double x)
            {
                if (dt != TimeWarp.fixedDeltaTime)
                {
                    initialize();
                    y_old = x;
                }
                else
                {
                    y_old = MuUtils.ClampDegrees180(y_old + alpha * MuUtils.ClampDegrees180(x - y_old));
                }

                return y_old;
            }

            public void clear()
            {
                y_old = 0;
            }
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

            double leftSpeed = Vector3d.Dot(vesselState.velocityVesselSurface, runwayLeftUnit);
            double verticalSpeed = vesselState.speedVertical;
            double horizontalSpeed = vesselState.speedSurfaceHorizontal;
            double flightPathAngle = 180 / Math.PI * Math.Atan2(verticalSpeed, horizontalSpeed);

            double leftDisplacement = Vector3d.Dot(runwayLeftUnit, vesselState.CoM - runwayStart);

            Vector3d vectorToRunway = runwayStart - vesselState.CoM;
            double verticalDistanceToRunway = Vector3d.Dot(vectorToRunway, vesselState.up);
            double horizontalDistanceToRunway = Math.Sqrt(vectorToRunway.sqrMagnitude - verticalDistanceToRunway * verticalDistanceToRunway);
            double flightPathAngleToRunway = 180 / Math.PI * Math.Atan2(verticalDistanceToRunway, horizontalDistanceToRunway);

            Vector3d aimToward = runwayStart - 3 * leftDisplacement * runwayLeftUnit;
            Vector3d aimDir = aimToward - vesselState.CoM;

            return aimDir;
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
                return FlightGlobals.Bodies[1].GetWorldSurfacePosition(latitude, longitude, altitude);
            }
        }

        public string name;
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
            return FlightGlobals.Bodies[1].GetSurfaceNVector(start.latitude, start.longitude);
        }
    }
}
