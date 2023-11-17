extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRoverController : ComputerModule
    {
        public readonly List<MechJebWaypoint> Waypoints     = new List<MechJebWaypoint>();
        public          int                   WaypointIndex = -1;
        private         CelestialBody         lastBody;
        public          bool                  LoopWaypoints = false;

        [ToggleInfoItem("#MechJeb_ControlHeading", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.LOCAL)] // Heading control
        public bool ControlHeading;

        [EditableInfoItem("#MechJeb_Heading", InfoItem.Category.Rover, width = 40)]
        [Persistent(pass = (int)Pass.LOCAL)] // Heading
        public EditableDouble heading = 0;

        [ToggleInfoItem("#MechJeb_ControlSpeed", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.LOCAL)] // Speed control
        public bool ControlSpeed;

        [EditableInfoItem("#MechJeb_Speed", InfoItem.Category.Rover, width = 40)]
        [Persistent(pass = (int)Pass.LOCAL)] // Speed
        public readonly EditableDouble speed = 10;

        [ToggleInfoItem("#MechJeb_BrakeOnEject", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.LOCAL)] // Brake on Pilot Eject
        public readonly bool BrakeOnEject;

        [ToggleInfoItem("#MechJeb_BrakeOnEnergyDepletion", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.LOCAL)] // Brake on Energy Depletion
        public readonly bool BrakeOnEnergyDepletion;

        [ToggleInfoItem("#MechJeb_WarpToDaylight", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.LOCAL)] // Warp until Day if Depleted
        public readonly bool WarpToDaylight;

        public bool waitingForDaylight;

        [ToggleInfoItem("#MechJeb_StabilityControl", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.LOCAL)] // Stability Control
        public readonly bool StabilityControl;

        [ToggleInfoItem("#MechJeb_LimitAcceleration", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.LOCAL | (int)Pass.TYPE)] // Limit Acceleration
        public bool LimitAcceleration;

        public PIDController headingPID;
        public PIDController speedPID;

//		private LineRenderer line;

        [EditableInfoItem("#MechJeb_SafeTurnspeed", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.TYPE)] // Safe turnspeed
        public readonly EditableDouble turnSpeed = 3;

        [EditableInfoItem("#MechJeb_TerrainLookAhead", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Terrain Look Ahead
        public readonly EditableDouble terrainLookAhead = 1.0;

        [EditableInfoItem("#MechJeb_BrakeSpeedLimit", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.TYPE)] // Brake Speed Limit
        public readonly EditableDouble brakeSpeedLimit = 0.7;

        [EditableInfoItem("#MechJeb_HeadingPIDP", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)]        // Heading PID P
        public readonly EditableDouble hPIDp = 0.03; // 0.01

        [EditableInfoItem("#MechJeb_HeadingPIDI", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)]         // Heading PID I
        public readonly EditableDouble hPIDi = 0.002; // 0.001

        [EditableInfoItem("#MechJeb_HeadingPIDD", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Heading PID D
        public readonly EditableDouble hPIDd = 0.005;

        [EditableInfoItem("#MechJeb_SpeedPIDP", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Speed PID P
        public readonly EditableDouble sPIDp = 2.0;

        [EditableInfoItem("#MechJeb_SpeedPIDI", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Speed PID I
        public readonly EditableDouble sPIDi = 0.1;

        [EditableInfoItem("#MechJeb_SpeedPIDD", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Speed PID D
        public readonly EditableDouble sPIDd = 0.001;

        [ValueInfoItem("#MechJeb_SpeedIntAcc", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")] // Speed Int Acc
        public double speedIntAcc;

        [ValueInfoItem("#MechJeb_Traction", InfoItem.Category.Rover, format = "F0", units = "%")] // Traction
        public float traction;

        [EditableInfoItem("#MechJeb_TractionBrakeLimit", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.TYPE)] // Traction Brake Limit
        public EditableDouble tractionLimit = 75;

        public readonly List<(PartModule, BaseField)> wheelbases = new List<(PartModule, BaseField)>();

        public override void OnStart(PartModule.StartState state)
        {
            headingPID = new PIDController(hPIDp, hPIDi, hPIDd);
            speedPID   = new PIDController(sPIDp, sPIDi, sPIDd);

            if (HighLogic.LoadedSceneIsFlight && Orbit != null)
            {
                lastBody = Orbit.referenceBody;
            }

//			MechJebRouteRenderer.NewLineRenderer(ref line);
//			line.enabled = false;

            GameEvents.onVesselWasModified.Add(OnVesselModified);

            base.OnStart(state);
        }

        public void OnVesselModified(Vessel v)
        {
            // TODO : need to look into the new ModuleWheelSteering ModuleWheelMotor ModuleWheelBrakes ModuleWheelBase ModuleWheelSuspension and see what they could bring

            try
            {
                wheelbases.Clear();
                wheelbases.AddRange(Vessel.Parts.Where(
                    p => p.HasModule<ModuleWheelBase>()
                         && p.GetModule<ModuleWheelBase>().wheelType != WheelType.LEG
                ).Select(p =>
                {
                    PartModule pm = p.Modules.GetModule("ModuleWheelBase");
                    return (pm, pm.Fields["isGrounded"]);
                }));
                wheelbases.AddRange(Vessel.Parts.Where(
                    p => p.Modules.Contains("KSPWheelBase") &&
                         p.Modules.Contains("KSPWheelRotation")
                ).Select(p =>
                {
                    PartModule pm = p.Modules.GetModule("KSPWheelBase");
                    return (pm, pm.Fields["grounded"]);
                }));
            }
            catch (Exception) { }
        }

        [ValueInfoItem("#MechJeb_Headingerror", InfoItem.Category.Rover, format = "F1", units = "º")] // Heading error
        public double headingErr;

        [ValueInfoItem("#MechJeb_Speederror", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")] // Speed error
        public double speedErr;

        public          double        tgtSpeed;
        public readonly MovingAverage etaSpeed = new MovingAverage(50);
        private         double        lastETA;
        private         float         lastThrottle;
        private         double        curSpeed;

        public double HeadingToPos(Vector3 fromPos, Vector3 toPos)
        {
            Transform origin = MainBody.transform;

            // thanks to Cilph who did most of this since I don't understand anything ~ BR2k
            Vector3 up = fromPos - origin.position; // position relative to origin, "up" vector
            up.Normalize();

            // mark north and target directions on horizontal plane
            var north = Vector3.ProjectOnPlane(origin.up, up);
            var target = Vector3.ProjectOnPlane(toPos - fromPos, up); // no need to normalize

            // apply protractor
            return Vector3.SignedAngle(north, target, up);
        }

        public float TurningSpeed(double speed, double error) =>
            (float)Math.Max(speed / (Math.Abs(error) / 3 > 1 ? Math.Abs(error) / 3 : 1), turnSpeed);

        public void CalculateTraction()
        {
            if (wheelbases.Count == 0) { OnVesselModified(Vessel); }

            traction = 0;

            foreach ((PartModule pm, BaseField grounded) in wheelbases)
            {
                if (grounded.GetValue<bool>(pm))
                {
                    traction += 100;
                }
            }

            traction /= wheelbases.Count;
        }

        protected override void OnModuleDisabled()
        {
            if (Core.Attitude.Users.Contains(this))
            {
//				line.enabled = false;
                Core.Attitude.attitudeDeactivate();
                Core.Attitude.Users.Remove(this);
            }

            base.OnModuleDisabled();
        }

        private float  Square(float number)  => number * number;
        private double Square(double number) => number * number;

        public override void
            Drive(FlightCtrlState s) // TODO put the brake in when running out of power to prevent nighttime solar failures on hills, or atleast try to
        {
            // TODO make distance calculation for 'reached' determination consider the rover and waypoint on sealevel to prevent height differences from messing it up -- should be done now?
            if (Orbit.referenceBody != lastBody)
            {
                WaypointIndex = -1;
                Waypoints.Clear();
            }

            MechJebWaypoint wp = WaypointIndex > -1 && WaypointIndex < Waypoints.Count ? Waypoints[WaypointIndex] : null;

            bool brake = Vessel.ActionGroups[KSPActionGroup.Brakes]; // keep brakes locked if they are
            curSpeed = Vector3d.Dot(VesselState.surfaceVelocity, VesselState.forward);

            CalculateTraction();
            speedIntAcc = speedPID.INTAccum;

            if (wp != null && wp.Body == Orbit.referenceBody)
            {
                if (ControlHeading)
                {
                    double newHeading = Math.Round(HeadingToPos(Vessel.CoM, wp.Position), 1);

                    // update GUI text only if the value changed
                    if (newHeading != heading)
                        heading.Val = newHeading;
                }

                if (ControlSpeed)
                {
                    MechJebWaypoint nextWP = WaypointIndex < Waypoints.Count - 1 ? Waypoints[WaypointIndex + 1] : LoopWaypoints ? Waypoints[0] : null;
                    float distance = Vector3.Distance(Vessel.CoM, wp.Position);
                    if (wp.Target != null) { distance += (float)(wp.Target.srfSpeed * curSpeed) / 2; }

                    // var maxSpeed = (wp.MaxSpeed > 0 ? Math.Min((float)speed, wp.MaxSpeed) : speed); // use waypoints maxSpeed if set and smaller than set the speed or just stick with the set speed
                    double
                        maxSpeed = wp.MaxSpeed > 0
                            ? wp.MaxSpeed
                            : speed; // speed used to go towards the waypoint, using the waypoints maxSpeed if set or just stick with the set speed
                    double minSpeed = wp.MinSpeed > 0 ? wp.MinSpeed :
                        nextWP != null                ? TurningSpeed(nextWP.MaxSpeed > 0 ? nextWP.MaxSpeed : speed,
                            MuUtils.ClampDegrees180(heading - HeadingToPos(wp.Position, nextWP.Position))) :
                        distance - wp.Radius > 50 ? turnSpeed.Val : 1;
                    minSpeed = wp.Quicksave ? 1 : minSpeed;
                    // ^ speed used to go through the waypoint, using half the set speed or maxSpeed as minSpeed for routing waypoints (all except the last)
                    double newSpeed = Math.Min(maxSpeed, Math.Max((distance - wp.Radius) / curSpeed, minSpeed)); // brake when getting closer
                    newSpeed = newSpeed > turnSpeed ? TurningSpeed(newSpeed, headingErr) : newSpeed;             // reduce speed when turning a lot
                    float radius = Math.Max(wp.Radius, 10);
                    if (distance < radius)
                    {
                        if (WaypointIndex + 1 >= Waypoints.Count) // last waypoint
                        {
                            newSpeed = new[] { newSpeed, distance < radius * 0.8 ? 0 : 1 }.Min();
                            // ^ limit speed so it'll only go from 1m/s to full stop when braking to prevent accidents on moons
                            if (LoopWaypoints)
                            {
                                WaypointIndex = 0;
                            }
                            else
                            {
                                newSpeed = 0;
                                brake    = true;
                                if (curSpeed < brakeSpeedLimit)
                                {
                                    if (wp.Quicksave)
                                    {
                                        if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR)
                                        {
                                            WaypointIndex  = -1;
                                            ControlHeading = ControlSpeed = false;
                                            QuickSaveLoad.QuickSave();
                                        }
                                    }
                                    else
                                    {
                                        WaypointIndex  = -1;
                                        ControlHeading = ControlSpeed = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (wp.Quicksave)
                            {
                                newSpeed = 0;
                                if (curSpeed < brakeSpeedLimit)
                                {
                                    if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR)
                                    {
                                        WaypointIndex++;
                                        QuickSaveLoad.QuickSave();
                                    }
                                }
                            }
                            else
                            {
                                WaypointIndex++;
                            }
                        }
                    }

                    brake = brake || ((s.wheelThrottle == 0 || !Vessel.isActiveVessel) && curSpeed < brakeSpeedLimit && newSpeed < brakeSpeedLimit);
                    // ^ brake if needed to prevent rolling, hopefully
                    tgtSpeed = newSpeed >= 0 ? newSpeed : 0;
                }
            }

            if (ControlHeading)
            {
                headingPID.INTAccum = Mathf.Clamp((float)headingPID.INTAccum, -1, 1);

                double instantaneousHeading = VesselState.rotationVesselSurface.eulerAngles.y;
                headingErr = MuUtils.ClampDegrees180(instantaneousHeading - heading);
                if (s.wheelSteer == s.wheelSteerTrim || FlightGlobals.ActiveVessel != Vessel)
                {
                    float limit = Math.Abs(curSpeed) > turnSpeed ? Mathf.Clamp((float)((turnSpeed + 6) / Square(curSpeed)), 0.1f, 1f) : 1f;
                    // turnSpeed needs to be higher than curSpeed or it will never steer as much as it could even at 0.2m/s above it
                    double act = headingPID.Compute(headingErr);
                    if (traction >= tractionLimit)
                    {
                        s.wheelSteer = Mathf.Clamp((float)act, -limit, limit);
                        // prevents it from flying above a waypoint and landing with steering at max while still going fast
                    }
                }
            }

            // Brake if there is no controler (Pilot eject from seat)
            if (BrakeOnEject && Vessel.GetReferenceTransformPart() == null)
            {
                s.wheelThrottle = 0;
                brake           = true;
            }
            else if (ControlSpeed)
            {
                speedPID.INTAccum = Mathf.Clamp((float)speedPID.INTAccum, -5, 5);

                speedErr = (WaypointIndex == -1 ? speed.Val : tgtSpeed) - Vector3d.Dot(VesselState.surfaceVelocity, VesselState.forward);
                if (s.wheelThrottle == s.wheelThrottleTrim || FlightGlobals.ActiveVessel != Vessel)
                {
                    float act = (float)speedPID.Compute(speedErr);
                    s.wheelThrottle = Mathf.Clamp(act, -1f, 1f);
                    if (curSpeed < 0 && s.wheelThrottle < 0) { s.wheelThrottle = 0; } // don't go backwards

                    if (Mathf.Sign(act) + Mathf.Sign(s.wheelThrottle) == 0) { s.wheelThrottle = Mathf.Clamp(act, -1f, 1f); }

                    if (speedErr < -1 && StabilityControl && Mathf.Sign(s.wheelThrottle) + Math.Sign(curSpeed) == 0)
                    {
                        brake = true;
                    }

                    lastThrottle = Mathf.Clamp(s.wheelThrottle, -1, 1);
                }
            }

            if (StabilityControl)
            {
                Physics.Raycast(Vessel.CoM + VesselState.surfaceVelocity * terrainLookAhead + VesselState.up * 100, -VesselState.up,
                    out RaycastHit hit, 500,
                    1 << 15, QueryTriggerInteraction.Ignore);
                Vector3 norm = hit.normal;

                if (!Core.Attitude.Users.Contains(this))
                {
                    Core.Attitude.Users.Add(this);
                }

                float fSpeed = (float)curSpeed;
                Vector3 fwd = traction > 0
                    ? // V when the speed is low go for the vessels forward, else with a bit of velocity
                    VesselState.forward * 4 - Vessel.transform.right * s.wheelSteer * Mathf.Sign(fSpeed)
                    :                            // and then add the steering
                    VesselState.surfaceVelocity; // in the air so follow velocity
                Vector3.OrthoNormalize(ref norm, ref fwd);
                var quat = Quaternion.LookRotation(fwd, norm);

                if (VesselState.torqueAvailable.sqrMagnitude > 0)
                    Core.Attitude.attitudeTo(quat, AttitudeReference.INERTIAL, this);
            }

            if (BrakeOnEnergyDepletion)
            {
                List<Part> batteries = Vessel.Parts.FindAll(p =>
                    p.Resources.Contains(PartResourceLibrary.ElectricityHashcode) &&
                    p.Resources.Get(PartResourceLibrary.ElectricityHashcode).flowState);
                double energyLeft = batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).amount) /
                                    batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).maxAmount);
                bool openSolars =
                    Vessel.mainBody.atmosphere && // true if in atmosphere and there are breakable solarpanels that aren't broken nor retracted
                    Vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p =>
                        p.isBreakable && p.deployState != ModuleDeployablePart.DeployState.BROKEN &&
                        p.deployState != ModuleDeployablePart.DeployState.RETRACTED).Count > 0;

                if (openSolars && energyLeft > 0.99)
                {
                    Vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p => p.isBreakable &&
                                                                                                  p.deployState == ModuleDeployablePart.DeployState
                                                                                                      .EXTENDED).ForEach(p => p.Retract());
                }

                if (energyLeft < 0.05 && Math.Sign(s.wheelThrottle) + Math.Sign(curSpeed) != 0)
                {
                    s.wheelThrottle = 0;
                } // save remaining energy by not using it for acceleration

                if (openSolars || energyLeft < 0.03) { tgtSpeed = 0; }

                if (curSpeed < brakeSpeedLimit && (energyLeft < 0.05 || openSolars))
                {
                    brake = true;
                }

                if (curSpeed < 0.1 && energyLeft < 0.05 && !waitingForDaylight &&
                    Vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>()
                        .FindAll(p => p.deployState == ModuleDeployablePart.DeployState.EXTENDED).Count > 0)
                {
                    waitingForDaylight = true;
                }
            }

            if (s.wheelThrottle != 0 && (Math.Sign(s.wheelThrottle) + Math.Sign(curSpeed) != 0 || curSpeed < 1))
            {
                brake = false; // the AP or user want to drive into the direction of momentum so release the brake
            }

            if (Vessel.isActiveVessel)
            {
                if (GameSettings.BRAKES.GetKeyUp())
                {
                    brake = false; // release the brakes if the user lets go of them
                }

                if (GameSettings.BRAKES.GetKey())
                {
                    brake = true; // brake if the user brakes and we aren't about to flip
                }
            }

            tractionLimit = (double)Mathf.Clamp((float)tractionLimit, 0, 100);
            Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes,
                brake && (StabilityControl && (ControlHeading || ControlSpeed) ? traction >= tractionLimit : true));
            // only let go of the brake when losing traction if the AP is driving, otherwise assume the player knows when to let go of it
            // also to not constantly turn off the parking brake from going over a small bump
            if (brake && curSpeed < 0.1) { s.wheelThrottle = 0; }
        }

        public override void OnFixedUpdate()
        {
            if (!Core.GetComputerModule<MechJebModuleWaypointWindow>().Enabled)
            {
                Waypoints.ForEach(wp => wp.Update()); // update waypoints unless the waypoint window is (hopefully) doing that already
            }

            if (Orbit != null && lastBody != Orbit.referenceBody) { lastBody = Orbit.referenceBody; }

            headingPID.Kp = hPIDp;
            headingPID.Ki = hPIDi;
            headingPID.Kd = hPIDd;
            speedPID.Kp   = sPIDp;
            speedPID.Ki   = sPIDi;
            speedPID.Kd   = sPIDd;

            if (lastETA + 0.2 < DateTime.Now.TimeOfDay.TotalSeconds)
            {
                etaSpeed.Value = curSpeed;
                lastETA        = DateTime.Now.TimeOfDay.TotalSeconds;
            }

            if (!Core.GetComputerModule<MechJebModuleRoverWindow>().Enabled)
            {
                Core.GetComputerModule<MechJebModuleRoverWindow>()
                    .OnUpdate(); // update users for Stability Control, Brake on Eject and Brake on Energy Depletion
            }
        }

        public override void OnUpdate()
        {
            if (WarpToDaylight && waitingForDaylight && Vessel.isActiveVessel)
            {
                List<Part> batteries = Vessel.Parts.FindAll(p =>
                    p.Resources.Contains(PartResourceLibrary.ElectricityHashcode) &&
                    p.Resources.Get(PartResourceLibrary.ElectricityHashcode).flowState);
                double energyLeft = batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).amount) /
                                    batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).maxAmount);

                if (waitingForDaylight)
                {
                    if (Vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>()
                            .FindAll(p => p.deployState == ModuleDeployablePart.DeployState.EXTENDED).Count == 0)
                    {
                        waitingForDaylight = false;
                    }

                    Core.Warp.WarpRegularAtRate(energyLeft < 0.9 ? 1000 : 50);
                    if (energyLeft > 0.99)
                    {
                        waitingForDaylight = false;
                        Core.Warp.MinimumWarp();
                    }
                }
            }
            else if (!WarpToDaylight && waitingForDaylight)
            {
                waitingForDaylight = false;
            }

            if (!Core.GetComputerModule<MechJebModuleRoverWindow>().Enabled)
            {
                Core.GetComputerModule<MechJebModuleRoverWindow>()
                    .OnUpdate(); // update users for Stability Control, Brake on Eject and Brake on Energy Depletion
            }

            if (!StabilityControl && Core.Attitude.Users.Contains(this))
            {
                Core.Attitude.attitudeDeactivate();
                Core.Attitude.Users.Remove(this);
            }
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (local != null)
            {
                ConfigNode wps = local.GetNode("Waypoints");
                if (wps != null && wps.HasNode("Waypoint"))
                {
                    int.TryParse(wps.GetValue("Index"), out WaypointIndex);
                    Waypoints.Clear();
                    foreach (ConfigNode cn in wps.GetNodes("Waypoint"))
                    {
                        Waypoints.Add(new MechJebWaypoint(cn));
                    }
                }
            }
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);

            if (local == null) return;

            if (local.HasNode("Waypoints")) { local.RemoveNode("Waypoints"); }

            if (Waypoints.Count > 0)
            {
                ConfigNode cn = local.AddNode("Waypoints");
                cn.AddValue("Index", WaypointIndex);
                foreach (MechJebWaypoint wp in Waypoints)
                {
                    cn.AddNode(wp.ToConfigNode());
                }
            }
        }

        public MechJebModuleRoverController(MechJebCore core) : base(core) { }
    }
}
