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
            maxRoll = 30F;
            throttleUpPitch = 0.8F;
            throttleDownPitch = 0.7F;
            mode = Mode.AUTOLAND;
            landed = false;
            loweredGear = false;
            brakes = false;
        }

        public void HoldHeadingAndAltitude(object controller)
        {
            users.Add(controller);
            AutopilotOn();
            maxRoll = 30F;
            throttleUpPitch = 0.8F;
            throttleDownPitch = 0.7F;
            mode = Mode.HOLD;
            landed = false;
            loweredGear = false;
            brakes = false;
        }

        bool autopilotOn = false;
        public void AutopilotOn()
        {
            autopilotOn = true;
            rollLowPass.initialize();
            desiredRollLowPass.initialize();
            rollCorrectionLowPass.initialize();
            yawCorrectionLowPass.initialize();
            velocityPitchLowPass.initialize();
            desiredAoALowPass.initialize();
            headingLowPass.initialize();
            velocityHeadingLowPass.initialize();
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

        public float takeoffPitch = 0.5F;
        public void DriveHeadingAndAltitudeHold(FlightCtrlState s)
        {
            if (!part.vessel.Landed)
            {
                if (!autopilotOn)
                    AutopilotOn();
                if (!loweredGear)
                {
                    if ((part.vessel.altitude - part.vessel.terrainAltitude) < 100.0)
                    {
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
                        loweredGear = true;
                    }
                }
                else
                {
                    if ((part.vessel.altitude - part.vessel.terrainAltitude) > 100.0)
                    {
                        vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                        loweredGear = false;
                    }
                }

                //takeoff or set for flight
                if (landed)
                {
                    //vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                    loweredGear = true;
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
                    loweredGear = true;
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

            double targetClimbRate = (targetAltitude - vesselState.altitudeASL) / (30.0 * Math.Pow((CelestialBodyExtensions.RealMaxAtmosphereAltitude(mainBody) / (CelestialBodyExtensions.RealMaxAtmosphereAltitude(mainBody) - vesselState.altitudeASL)), 5));
            double targetFlightPathAngle = 180 / Math.PI * Math.Asin(Mathf.Clamp((float)(targetClimbRate / vesselState.speedSurface), (float)Math.Sin(-Math.PI / 7), (float)Math.Sin(Math.PI / 7)));
            
            double heading = targetHeading;
            //if (loweredGear == true) heading = vesselState.vesselHeading;

            AimVelocityVector(targetFlightPathAngle, targetHeading);
        }

        bool landed = false;
        double landTime = 0;
        public double aimAltitude = 0;
        public double distanceFrom = 0;
        public double runwayHeading = 0;
        public enum HeadingState { RIGHT, LEFT, OFF };
        public HeadingState autolandHeadingState = HeadingState.OFF;
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
                if (Math.Abs(MuUtils.ClampDegrees180(headingToWaypoint - vesselState.vesselHeading)) > 170)
                {
                    //make sure the heading doesn't keep switching from side to side
                    switch (autolandHeadingState)
                    {
                        case HeadingState.RIGHT:
                            headingToWaypoint = MuUtils.ClampDegrees360(vesselState.vesselHeading + 90);
                            break;
                        case HeadingState.LEFT:
                            headingToWaypoint = MuUtils.ClampDegrees360(vesselState.vesselHeading - 90);
                            break;
                        case HeadingState.OFF:
                            if (headingToWaypoint - vesselState.vesselHeading > 0)
                                autolandHeadingState = HeadingState.RIGHT;
                            else
                                autolandHeadingState = HeadingState.LEFT;
                            break;
                    }
                }
                else
                    autolandHeadingState = HeadingState.OFF;

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
                    double horizontalDifference = 9900;
                    if (horizontalDistanceToRunway > 30000) { aimAltitude = 3000; horizontalDifference = 29900; }
                    if (horizontalDistanceToRunway > 50000) { aimAltitude = 8000; horizontalDifference = 49900; }
                    double setupFPA = 180 / Math.PI * Math.Atan2(aimAltitude - vessel.altitude, horizontalDistanceToRunway - horizontalDifference);

                    AimVelocityVector(setupFPA, headingToWaypoint);
                }
                else
                {
                    double flightPathAngleToRunway = 180 / Math.PI * Math.Atan2(verticalDistanceToRunway, horizontalDistanceToRunway);
                    double desiredFPA = Mathf.Clamp((float)(flightPathAngleToRunway + 3 * (flightPathAngleToRunway + glideslope)), -20.0F, 5.0F);

                    throttleUpPitch = 0.9F;
                    throttleDownPitch = 0.8F;

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

        public PIDController pitchPID = new PIDController(0.6, 0.2, 0.6, 25, -10);
        public PIDController pitchCorrectionPID = new PIDController(0.1, 0.05, 0.1, 1, -1);
        public PIDController rollPID = new PIDController(10, 0, 5);
        public PIDController rollCorrectionPID = new PIDController(0.01, 0, 0.01, 1, -1);
        public PIDController yawCorrectionPID = new PIDController(0.1, 0, 0.1, 1, -1);
        double altitudePercent = 0;
        public double pitchCorrectionPidKp = 0.2;
        public double lowPitchCorPidKd = 0.2;
        public double highPitchCorPidKd = 1;

        public double rollCorrectionPidKp = 0.005;
        public double lowRollCorPidKd = 0.005;
        public double highRollCorPidKd = 0.025;

        public double yawCorrectionPidKp = 0.05;
        public double lowYawCorPidKd = 0.05;
        public double highYawCorPidKd = 0.1;

        public double lowPitchPidMax = 25;
        public double highPitchPidMax = 5;
        public double lowPitchPidMin = -10;
        public double highPitchPidMin = 0;
        double controlGain = 1;
        void setPIDAltitude()
        {
            altitudePercent = vessel.altitude / CelestialBodyExtensions.RealMaxAtmosphereAltitude(mainBody);

            controlGain = vesselState.mass / 50;

            pitchCorrectionPID.Kd = lowPitchCorPidKd + (highPitchCorPidKd - lowPitchCorPidKd) * altitudePercent;
            rollCorrectionPID.Kd = lowRollCorPidKd + (highRollCorPidKd - lowRollCorPidKd) * altitudePercent;
            yawCorrectionPID.Kd = lowYawCorPidKd + (highYawCorPidKd - lowYawCorPidKd) * altitudePercent;

            pitchCorrectionPID.Kp = pitchCorrectionPidKp * controlGain;
            pitchCorrectionPID.Kd *= controlGain;

            rollCorrectionPID.Kp = rollCorrectionPidKp * controlGain;
            rollCorrectionPID.Kd *= controlGain;

            yawCorrectionPID.Kp = yawCorrectionPidKp * controlGain;
            yawCorrectionPID.Kd *= controlGain;

            pitchPID.max = lowPitchPidMax + (highPitchPidMax - lowPitchPidMax) * altitudePercent;
            pitchPID.min = lowPitchPidMin + (highPitchPidMin - lowPitchPidMin) * altitudePercent;
        }

        public float maxRoll = 30.0F;
        public LowPass180 rollLowPass = new LowPass180(0.15, 5, 0);
        public LowPass180 desiredRollLowPass = new LowPass180(0.1, 5, 30);
        public LowPass180 rollCorrectionLowPass = new LowPass180(0.1, 5, 60);
        public LowPass180 yawCorrectionLowPass = new LowPass180(0.1, 5, 60);
        public LowPass180 velocityPitchLowPass = new LowPass180(0.15, 5, 0);
        public LowPass180 desiredAoALowPass = new LowPass180(0.1, 5, 0);
        public LowPass360 headingLowPass = new LowPass360(0.15, 5, 0);
        public LowPass360 velocityHeadingLowPass = new LowPass360(0.15, 5, 0);
        public double noseRoll = 0;
        public double noseYaw = 0;
        public double desiredAoA = 0;
        public double pitchCorrection = 0;
        public double desiredRoll = 0;
        public double desiredYaw = 0;
        public double rollCorrection = 0;
        public double yawCorrection = 0;
        public float throttleUpPitch = 0.6F;
        public float throttleDownPitch = 0.4F;
        public double velocityHeading = 0;
        public double velocityHeadingTest = 0;
        public double velocityPitch = 0;
        void AimVelocityVector(double desiredFpa, double desiredHeading)
        {
            if (autopilotOn)
            {
                setPIDAltitude();
                
                velocityHeading = velocityHeadingLowPass.calc(180 / Math.PI * Math.Atan2(Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.east),
                                                                    Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.north)));
                velocityPitch = velocityPitchLowPass.calc(Math.Atan2(vesselState.speedVertical, vesselState.speedSurface) * (180.0 / Math.PI));

                //horizontal control
                noseRoll = rollLowPass.calc(vesselState.vesselRoll);
                noseYaw = MuUtils.ClampDegrees180(headingLowPass.calc(vesselState.vesselHeading) - velocityHeading);
                desiredRoll = desiredRollLowPass.calc(- Mathf.Clamp((float)rollPID.Compute(MuUtils.ClampDegrees180(desiredHeading - velocityHeading)), -maxRoll, maxRoll));
                desiredYaw = -desiredRoll / 3;// -(vesselState.vesselPitch - velocityPitch) * (desiredRoll / 60);
                rollCorrection = rollCorrectionLowPass.calc(Mathf.Clamp((float)rollCorrectionPID.Compute(noseRoll - desiredRoll), (float)rollCorrectionPID.min, (float)rollCorrectionPID.max));
                yawCorrection = yawCorrectionLowPass.calc(Mathf.Clamp((float)yawCorrectionPID.Compute(desiredYaw - noseYaw), (float)yawCorrectionPID.min, (float)yawCorrectionPID.max));

                //vertical control
                desiredAoA = desiredAoALowPass.calc(Mathf.Clamp((float)pitchPID.Compute(desiredFpa - velocityPitch), (float)pitchPID.min, (float)pitchPID.max));
                pitchCorrection = Mathf.Clamp((float)pitchCorrectionPID.Compute(desiredAoA - (vesselState.vesselPitch - velocityPitch)), (float)pitchCorrectionPID.min, (float)pitchCorrectionPID.max);

                vessel.ctrlState.roll = (float)rollCorrection;
                vessel.ctrlState.pitch = (float)pitchCorrection;
                vessel.ctrlState.yaw = (float)yawCorrection;

                if (pitchPID.intAccum > 100) pitchPID.intAccum = 100;
                if (pitchPID.intAccum < -100) pitchPID.intAccum = -100;
                if (pitchCorrectionPID.intAccum > 10) pitchCorrectionPID.intAccum = 10;
                if (pitchCorrectionPID.intAccum < -10) pitchCorrectionPID.intAccum = -10;
                //if (rollPID.intAccum > 100) rollPID.intAccum = 100;
                //if (rollPID.intAccum < -100) rollPID.intAccum = -100;
                //if (rollCorrectionPID.intAccum > 10) rollCorrectionPID.intAccum = 10;
                //if (rollCorrectionPID.intAccum < -10) rollCorrectionPID.intAccum = -10;

                //regulate throttle
                float pitchPercent = (float)((vesselState.vesselPitch - velocityPitch) / pitchPID.max);
                if (pitchPercent > throttleUpPitch) vessel.ctrlState.mainThrottle += 0.01F * (float)(pitchPercent - throttleUpPitch) * vessel.ctrlState.mainThrottle;
                if (pitchPercent < throttleDownPitch && vessel.altitude < 0.4 * CelestialBodyExtensions.RealMaxAtmosphereAltitude(mainBody)) vessel.ctrlState.mainThrottle += 0.001F * (float)(pitchPercent - throttleDownPitch) * vessel.ctrlState.mainThrottle;

            }
        }

        public class LowPass360
        {
            private double y_old, alpha, timeConstant, dt, passGap;
            int calcCount, delayCount;

            public double lastResult { get { return y_old; } }

            public double TimeConstant
            {
                get { return this.timeConstant; }
                set
                {
                    if (value != timeConstant)
                    {
                        this.timeConstant = value;
                        initialize();
                    }
                }
            }

            public LowPass360(double timeConst)
            {
                this.timeConstant = timeConst;
                clear();
                initialize();
                this.passGap = 0;
                this.delayCount = 0;
            }

            public LowPass360(double timeConst, double passGap, int delayCount)
            {
                this.timeConstant = timeConst;
                clear();
                initialize();
                this.passGap = passGap;
                this.delayCount = delayCount;
            }

            public void initialize()
            {
                dt = TimeWarp.fixedDeltaTime;
                this.alpha = TimeWarp.fixedDeltaTime / (timeConstant - TimeWarp.fixedDeltaTime);
                calcCount = 0;
                y_old = 0;
            }

            public double calc(double x)
            {
                x = MuUtils.ClampDegrees360(x);
                if (dt != TimeWarp.fixedDeltaTime)
                {
                    initialize();
                    y_old = x;
                }
                else
                {
                    if (MuUtils.ClampDegrees180(x - y_old) > passGap)
                        x = MuUtils.ClampDegrees360(y_old + passGap);
                    else if (MuUtils.ClampDegrees180(x - y_old) < -passGap)
                        x = MuUtils.ClampDegrees360(y_old - passGap);

                    y_old = MuUtils.ClampDegrees360(y_old + alpha * MuUtils.ClampDegrees180(x - y_old));
                }

                if (calcCount <= delayCount)
                {
                    calcCount++;
                    return 0;
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
            private double y_old, alpha, timeConstant, dt, passGap;
            int calcCount, delayCount;

            public double lastResult { get { return y_old; } }

            public double TimeConstant
            {
                get { return this.timeConstant; }
                set
                {
                    if (value != timeConstant)
                    {
                        this.timeConstant = value;
                        initialize();
                    }
                }
            }

            public LowPass180(double timeConst)
            {
                this.timeConstant = timeConst;
                clear();
                initialize();
                this.passGap = 0;
                this.delayCount = 0;
            }

            public LowPass180(double timeConst, double passGap, int delayCount)
            {
                this.timeConstant = timeConst;
                clear();
                initialize();
                this.passGap = passGap;
                this.delayCount = delayCount;
            }


            public void initialize()
            {
                dt = TimeWarp.fixedDeltaTime;
                this.alpha = TimeWarp.fixedDeltaTime / (timeConstant - TimeWarp.fixedDeltaTime);
                calcCount = 0;
                y_old = 0;
            }

            public double calc(double x)
            {
                MuUtils.ClampDegrees180(x);
                if (dt != TimeWarp.fixedDeltaTime)
                {
                    initialize();
                    y_old = x;
                }
                else
                {
                    if (MuUtils.ClampDegrees180(x - y_old) > passGap)
                        x = MuUtils.ClampDegrees180(y_old + passGap);
                    else if (MuUtils.ClampDegrees180(x - y_old) < -passGap)
                        x = MuUtils.ClampDegrees180(y_old - passGap);

                    y_old = MuUtils.ClampDegrees180(y_old + alpha * MuUtils.ClampDegrees180(x - y_old));
                }


                if (calcCount <= delayCount)
                {
                    calcCount++;
                    return 0;
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
