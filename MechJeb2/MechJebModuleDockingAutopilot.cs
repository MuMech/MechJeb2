extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using KSP.Localization;
using MechJebLib.Control;
using MechJebLib.Primitives;
using MechJebLibBindings;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleDockingAutopilot : ComputerModule
    {
        // Refreshed a few times a second, not on every tick. It has to stay a field: kRPC-MechJeb reads it by reflection.
        public string status = "";

        private const float statusInterval = 0.2f;
        private string statusKey;
        private float nextStatus;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL)), EditableInfoItem("#MechJeb_DockingSpeedLimit", InfoItem.Category.Thrust, rightLabel = "m/s")]
        //Docking speed limit
        public EditableDouble speedLimit = 1;

        [Persistent(pass = (int)Pass.LOCAL)]
        public readonly EditableDouble rol = new EditableDouble(0);

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool forceRol = false;

        [Persistent(pass = (int)Pass.LOCAL), EditableInfoItem("#MechJeb_Docking_checkbox3", InfoItem.Category.Thrust, rightLabel = "m")] //Safe Distance
        public EditableDouble overridenSafeDistance = 5;

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool overrideSafeDistance = false;

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool overrideTargetSize = false;

        [Persistent(pass = (int)Pass.LOCAL), EditableInfoItem("#MechJeb_Docking_label6", InfoItem.Category.Thrust, rightLabel = "m")] //Start Distance
        public EditableDouble overridenTargetSize = 10;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool stopOnContact = true;

        // How far off the docking axis the route passes the target, and how far in front of the port the approach starts.
        public float safeDistance = 10;
        public float targetSize = 5;

        // How far behind the port the route crosses the axis.
        public float rearDistance = 10;

        public bool drawBoundingBox;

        public enum DockingStep
        {
            INIT, WRONG_SIDE_BACKING_UP, WRONG_SIDE_LATERAL, WRONG_SIDE_SWITCHSIDE, BACKING_UP, MOVING_TO_START, DOCKING, OFF, LEAVING_TARGET
        }

        public DockingStep dockingStep = DockingStep.OFF;

        public struct Box3d
        {
            public Vector3 center;
            public Vector3 size;
        }

        private Vector3d zAxis;
        public double zSep;
        public Vector3d lateralSep;

        private ITargetable lastTarget;
        private ModuleDockingNode wantedPort;
        private Transform referenceTransform;
        private ModuleDockingNode referencePort;
        private bool controlledFromPort;

        private const float dockingcorridorRadius = 1;
        private const double alignedAngle = 5;
        private const double stoppedSpeed = 0.03;
        private const double routeMargin = 2;
        private const double minStartCorridor = 0.25;
        private const double startCorridorSlope = 30;
        private const double leaveSpeed = 1;
        private const int maxRetries = 3;
        private const double captureTimeLimit = 30;
        private const double portTargetRange = 190; // KSP makes a port's vessel the target past 200 m
        private double stopDistance;
        private bool portTarget;
        private bool targetMeasured;
        private bool ownMeasured;
        private double ownRadius;
        private double frontReach;
        private double rearReach;
        private double sideReach;
        private int retries;
        private double timeInAcquireRange;
        private bool rcsWasOn;
        private bool sasWasOn;
        private double acquireRange;
        private double captureRange;

        // At full thrust, with under a quarter of the acceleration that should give, for 3 s: something is holding the vessel.
        private readonly StuckDetector stuck = new StuckDetector(3, 0.25);

        // Both around their docking points, the target's in the frame of its port, so a target that turns takes all of this with it.
        private readonly VesselShape targetShape = new VesselShape();
        private readonly VesselShape ownShape = new VesselShape();
        private V3 routeSide;
        private V3 exitPoint;

        private readonly List<Vector3d> routeLine = new List<Vector3d>();

        public MechJebModuleDockingAutopilot(MechJebCore core)
            : base(core)
        {
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
            {
                Core.AddToPostDrawQueue(DrawBoundingBox);

                GameEvents.onPartCouple.Add(OnPartCouple);
                GameEvents.onVesselWasModified.Add(OnAnyVesselModified);
            }
        }

        public override void OnDestroy()
        {
            GameEvents.onPartCouple.Remove(OnPartCouple);
            GameEvents.onVesselWasModified.Remove(OnAnyVesselModified);
        }

        private void OnAnyVesselModified(Vessel modified)
        {
            if (modified == Vessel)
                ownMeasured = false;
            else if (Core.Target.Target != null && modified == Core.Target.Target.GetVessel())
                targetMeasured = false;
        }

        // Turn off docking AP on successful docking (in case other checks for successful docking fail)
        private void OnPartCouple(GameEvents.FromToAction<Part, Part> ev)
        {
            if (dockingStep != DockingStep.OFF)
            {
                if (ev.from.vessel == Vessel || ev.to.vessel == Vessel)
                {
                    EndDocking();
                }
            }
        }

        protected override void OnModuleEnabled()
        {
            Core.RCS.Users.Add(this);
            Core.Attitude.Users.Add(this);
            dockingStep = DockingStep.INIT;
            status = "";
            statusKey = null;
            ownMeasured = false;
            targetMeasured = false;
            retries = 0;
            timeInAcquireRange = 0;
            wantedPort = Core.GetComputerModule<MechJebModuleDockingGuidance>().selectedTargetPort;
            rcsWasOn = Vessel.ActionGroups[KSPActionGroup.RCS];
            sasWasOn = Vessel.ActionGroups[KSPActionGroup.SAS];
            stuck.Reset();
        }

        protected override void OnModuleDisabled()
        {
            Core.RCS.Users.Remove(this);
            Core.Attitude.Users.Remove(this);
            Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, rcsWasOn);
            Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, sasWasOn);
            dockingStep = DockingStep.OFF;
        }

        private double FixSpeed(double s)
        {
            if (speedLimit > 0)
            {
                if (s > speedLimit) s = speedLimit;
                if (s < -speedLimit) s = -speedLimit;
            }

            return s;
        }

        // Get how fast the ship can move on the axis and still be able to stop before reaching the distance.
        // v2 = u2 + 2 * a * d
        // initial speed v - final speed u = 0  - distance d
        // Maximum speed to brake in time = sqrt( 2 * a * d )
        private double MaxSpeedForDistance(double distance, Vector3d axis) => FixSpeed(Math.Sqrt(2.0 * Math.Abs(distance) * BrakingAcceleration(axis)));

        // The same for closing on the docking axis, with the gain limited near it. The separation is measured at one part, so it carries the
        // vessel's bending, and it changes sign on the axis: a square root all the way down turns that bending into thrust reversals.
        private double MaxSpeedToAxis(double distance, Vector3d axis) =>
            FixSpeed(StoppingSpeed.GainLimited(distance, BrakingAcceleration(axis), Core.RCS.rcsPositionGain()));

        private double BrakingAcceleration(Vector3d axis) => ThrustAcceleration(axis) * Core.RCS.rcsAccelFactor();

        private double ThrustAcceleration(Vector3d axis)
        {
            Vector3d localAxis = Vessel.ReferenceTransform.InverseTransformDirection(axis);
            return VesselState.RCSThrustAvailable.GetMagnitude(localAxis) / VesselState.Mass;
        }

        // The velocity of the point we dock to, and not of the target's CoM: the docking magnets turn the target when the port's axis misses its CoM,
        // and the port then slides away from a vessel that's matching the CoM. The target's spin is mass-weighted, so its bending stays out of this.
        private Vector3d TargetPointVelocity()
        {
            Vector3d velocity = Core.Target.TargetOrbit.GetVel();
            Vessel target = Core.Target.Target.GetVessel();
            if (target == null)
                return velocity;

            Vector3d spin = target.GetTransform().rotation * target.angularVelocity;
            return velocity + Vector3d.Cross(spin, (Vector3d)DockingPoint() - target.CoM);
        }

        public override void Drive(FlightCtrlState s)
        {
            if (!Core.Target.NormalTargetExists)
            {
                EndDocking();
                return;
            }

            if (dockingStep == DockingStep.OFF || dockingStep == DockingStep.INIT)
                return;

            Vector3d targetVel = TargetPointVelocity();

            double zApproachSpeed = MaxSpeedForDistance(Math.Max(zSep - stopDistance, 0), -zAxis);
            double latApproachSpeed = MaxSpeedToAxis(lateralSep.magnitude, lateralSep);
            Vector3d lateralMove = -lateralSep.normalized * latApproachSpeed;

            bool align = true;

            double timeToAxis;
            double timeToTargetSize;

            switch (dockingStep)
            {
                case DockingStep.LEAVING_TARGET:
                    Vector3d toExit = ToExit();
                    zApproachSpeed = 0;
                    latApproachSpeed = Math.Min(MaxSpeedToAxis(toExit.magnitude, -toExit), leaveSpeed);
                    lateralMove = toExit.normalized * latApproachSpeed;
                    align = false;
                    SetStatus("#MechJeb_Docking_status7", latApproachSpeed); //Moving clear of the target at <<1>> m/s
                    break;

                case DockingStep.WRONG_SIDE_BACKING_UP:
                    zApproachSpeed = MaxSpeedForDistance(rearDistance + zSep + 2.0, -zAxis);
                    latApproachSpeed = 0;
                    lateralMove = Vector3d.zero;
                    align = false;
                    SetStatus("#MechJeb_Docking_status1", zApproachSpeed, latApproachSpeed); //"Backing up at " <<1>> " m/s before moving on target side (lat: "<<2>> " m/s)"
                    break;

                case DockingStep.WRONG_SIDE_LATERAL:
                    zApproachSpeed = zSep > -rearDistance ? MaxSpeedToAxis(rearDistance + zSep, -zAxis) : 0;
                    lateralMove = RouteMove(out latApproachSpeed);
                    SetStatus("#MechJeb_Docking_status2", latApproachSpeed); //Moving away from docking axis at <<1>> m/s to avoid hitting target on backing up
                    break;

                case DockingStep.WRONG_SIDE_SWITCHSIDE:
                    zApproachSpeed = -MaxSpeedForDistance(1 + targetSize - zSep, zAxis);
                    lateralMove = RouteMove(out latApproachSpeed);
                    SetStatus("#MechJeb_Docking_status3", zApproachSpeed, latApproachSpeed); //"Moving at <<1>> m/s to get on the correct side of the target. (lat: <<2>> m/s)"
                    break;

                case DockingStep.BACKING_UP:
                    zApproachSpeed = -MaxSpeedForDistance(1 + targetSize - zSep, zAxis);
                    lateralMove = Vector3d.zero;
                    align = false;
                    SetStatus("#MechJeb_Docking_status4", zApproachSpeed); //"Backing up at " +  + " m/s"
                    break;

                case DockingStep.MOVING_TO_START:
                    zApproachSpeed = zSep < targetSize + 1 ? -MaxSpeedToAxis(targetSize + 1 - zSep, zAxis) : 0;

                    SetStatus("#MechJeb_Docking_status5", zApproachSpeed); //"Moving toward the starting point at " +  + " m/s."
                    break;

                case DockingStep.DOCKING:
                    timeToAxis = Math.Abs(lateralSep.magnitude / latApproachSpeed);
                    timeToTargetSize = Math.Abs(zSep / zApproachSpeed);

                    // The magnets are pulling by now and they turn the target for as long as they act, so unless we're too far off for the ports to
                    // capture, slowing down only makes that worse.
                    bool committed = zSep < acquireRange && lateralSep.magnitude < captureRange;

                    if (!committed && (zSep <= lateralSep.magnitude * 10 || timeToTargetSize <= timeToAxis * 10) && timeToAxis > 0 && timeToTargetSize > 0 &&
                        !double.IsInfinity(timeToAxis))
                    {
                        zApproachSpeed *= Math.Min(timeToTargetSize / timeToAxis, 1);
                    }

                    SetStatus("#MechJeb_Docking_status6", zApproachSpeed, latApproachSpeed); //"Moving forward to dock at <<1>> / <<2>> m/s."
                    break;
            }

            if (!align)
                Core.Attitude.AttitudeTo(Quaternion.LookRotation(Vessel.GetTransform().up, -Vessel.GetTransform().forward),
                    AttitudeReference.INERTIAL, this);
            else if (forceRol)
                Core.Attitude.AttitudeTo(Quaternion.LookRotation(Vector3d.back, Vector3d.up) * Quaternion.AngleAxis(-(float)rol, Vector3d.back),
                    AttitudeReference.TARGET_ORIENTATION, this);
            else
                Core.Attitude.AttitudeTo(Vector3d.back, AttitudeReference.TARGET_ORIENTATION, this);

            // The limit is on the vessel's speed, and each axis keeping to it isn't enough for that.
            Vector3d adjustment = lateralMove + zApproachSpeed * zAxis;
            if (speedLimit > 0 && adjustment.magnitude > speedLimit)
                adjustment *= speedLimit / adjustment.magnitude;

            Core.RCS.SetTargetWorldVelocity(targetVel + adjustment);
            MechJebModuleDebugArrows.debugVector = adjustment;

            // Inside the acquire range the magnets are a force of their own, and the ports are meant to meet.
            bool inMagnets = Math.Abs(zSep) < acquireRange && lateralSep.magnitude < acquireRange;
            if (stopOnContact && !inMagnets && TouchingTarget())
            {
                StopWithMessage("#MechJeb_Docking_contact"); //Docking autopilot stopped: the vessel touched the target.
                return;
            }

            Vector3d velocityError = targetVel + adjustment - VesselState.OrbitalVelocity;
            if (stuck.Update(velocityError.ToV3(), (VesselState.OrbitalVelocity - targetVel).ToV3(), ThrustAcceleration(velocityError), TimeWarp.fixedDeltaTime,
                    Core.RCS.Saturated && !inMagnets))
                StopWithMessage("#MechJeb_Docking_stuck"); //Docking autopilot stopped: the vessel isn't moving as commanded, and may be against the target.
        }

        // Speeds are shown as they are: which way they go is in the text.
        private void SetStatus(string key, double first, double second = 0)
        {
            if (key == statusKey && Time.fixedTime < nextStatus)
                return;

            statusKey = key;
            nextStatus = Time.fixedTime + statusInterval;
            status = Localizer.Format(key, Math.Abs(first).ToString("F2"), Math.Abs(second).ToString("F2"));
        }

        public override void OnFixedUpdate()
        {
            if (!Core.Target.NormalTargetExists)
            {
                EndDocking();
                return;
            }

            if (Core.Target.Target.GetVessel() == null)
            {
                StopWithMessage("#MechJeb_Docking_notAVessel"); //Docking autopilot stopped: the target isn't a vessel.
                return;
            }

            RestoreWantedPort();
            UpdateDistance();

            if (controlledFromPort && referencePort == null)
            {
                StopWithMessage("#MechJeb_Docking_controlLost"); //Docking autopilot stopped: the vessel is no longer controlled from a docking port.
                return;
            }

            // A new target has its own port, size and axis. KSP changes it too: past 200 m from the active vessel, a port becomes its vessel.
            if (dockingStep != DockingStep.INIT && Core.Target.Target != lastTarget)
            {
                if (lastTarget is ModuleDockingNode lastPort && Core.Target.Target.GetVessel() == lastPort.vessel)
                    wantedPort = lastPort;

                targetMeasured = false;
                dockingStep = DockingStep.INIT;
            }

            // A target out of physics range has no parts to measure, and parts come and go.
            Vessel targetVessel = Core.Target.Target.GetVessel();
            if (dockingStep != DockingStep.INIT && (!ownMeasured || (!targetMeasured && targetVessel != null && targetVessel.loaded)))
                dockingStep = DockingStep.INIT;

            UpdateLimits();

            // The axis of a turning target sweeps round the vessel, and past this the thrust can't hold it there.
            if (dockingStep != DockingStep.INIT && targetVessel != null && TargetSpinsTooFast(targetVessel))
            {
                StopWithMessage("#MechJeb_Docking_spinning"); //Docking autopilot stopped: the target is spinning too fast to follow.
                return;
            }

            switch (dockingStep)
            {
                case DockingStep.INIT:
                    InitDocking();
                    break;

                case DockingStep.LEAVING_TARGET:
                    if (ToExit().magnitude < dockingcorridorRadius)
                        dockingStep = DockingStep.INIT;
                    break;

                case DockingStep.WRONG_SIDE_BACKING_UP:
                    if (-zSep > rearDistance)
                        dockingStep = DockingStep.WRONG_SIDE_LATERAL;
                    break;

                case DockingStep.WRONG_SIDE_LATERAL:
                    if (ToRoute().magnitude < dockingcorridorRadius)
                        dockingStep = DockingStep.WRONG_SIDE_SWITCHSIDE;
                    break;

                case DockingStep.WRONG_SIDE_SWITCHSIDE:
                    if (zSep > targetSize)
                        dockingStep = DockingStep.MOVING_TO_START;
                    break;

                case DockingStep.BACKING_UP:
                    if (zSep > targetSize)
                        dockingStep = DockingStep.MOVING_TO_START;
                    break;

                case DockingStep.MOVING_TO_START:
                    if (lateralSep.magnitude < StartCorridor() && zSep >= targetSize && IsAligned() && !PortsDisengaging())
                        dockingStep = DockingStep.DOCKING;
                    break;

                case DockingStep.DOCKING:
                    timeInAcquireRange = zSep < acquireRange && lateralSep.magnitude < acquireRange ? timeInAcquireRange + TimeWarp.fixedDeltaTime : 0;
                    if (timeInAcquireRange > captureTimeLimit)
                    {
                        StopWithMessage("#MechJeb_Docking_noCapture"); //Docking autopilot stopped: the ports are together but didn't capture.
                        break;
                    }

                    // Off the docking axis at the port, on either side of its plane: this comes first, so a vessel that's slid past doesn't count as there.
                    if (lateralSep.magnitude > dockingcorridorRadius && zSep < 1)
                        Retry();
                    else if (zSep < stopDistance)
                    {
                        // The speed law has run out by here, so this brakes what's left: RCS is off afterwards, and the target isn't far.
                        if (portTarget)
                            EndDocking();
                        else if (Core.Target.RelativeVelocity.magnitude < stoppedSpeed)
                            StopWithMessage("#MechJeb_Docking_stoppedShort"); //Docking autopilot stopped short of the target: choose a docking port and start it again.
                    }

                    break;
            }
        }

        // The port the player picked comes back as the target once it can be one.
        private void RestoreWantedPort()
        {
            if (wantedPort == null || Core.Target.Target != (ITargetable)wantedPort.vessel)
            {
                wantedPort = null;
                return;
            }

            if (wantedPort.vessel.packed || Vector3d.Distance(Vessel.GetTransform().position, wantedPort.GetTransform().position) > portTargetRange)
                return;

            Core.GetComputerModule<MechJebModuleDockingGuidance>().SelectTargetPort(wantedPort);
            wantedPort = null;
        }

        private void Retry()
        {
            if (++retries > maxRetries)
                StopWithMessage("#MechJeb_Docking_gaveUp"); //Docking autopilot stopped: it couldn't stay on the docking axis.
            else
                dockingStep = DockingStep.INIT;
        }

        private bool TargetSpinsTooFast(Vessel targetVessel)
        {
            double spin = targetVessel.angularVelocity.magnitude;
            double thrust = ThrustAcceleration(lateralSep);
            return thrust > 0 && spin * spin * Core.Target.Distance > thrust / 2;
        }

        private V3 ToTargetFrame(Vector3d direction) => ((Vector3d)(Quaternion.Inverse(Core.Target.Transform.rotation) * direction)).ToV3();

        private Vector3d FromTargetFrame(V3 direction) => Core.Target.Transform.rotation * direction.ToVector3d();

        // From this vessel to where the route passes the target.
        private Vector3d ToRoute() => FromTargetFrame(routeSide) * safeDistance - lateralSep;

        private Vector3d ToExit() => FromTargetFrame(exitPoint) - ((Vector3d)DockingPoint() - Core.Target.Position);

        // Holding a point with a slope, so that a target's turn or a knock doesn't flip a step in the command.
        private Vector3d RouteMove(out double speed)
        {
            Vector3d toRoute = ToRoute();
            speed = MaxSpeedToAxis(toRoute.magnitude, -toRoute);
            return toRoute.normalized * speed;
        }

        private void MeasureShapes()
        {
            if (!ownMeasured)
            {
                Vessel.GetShape(ownShape, Vessel.ReferenceTransform, DockingPoint());
                ownRadius = ownShape.Radius();
                ownMeasured = true;
            }

            Vessel targetVessel = Core.Target.Target.GetVessel();
            if (targetMeasured || targetVessel == null)
                return;

            targetShape.Clear();
            frontReach = rearReach = sideReach = 0;
            if (!targetVessel.loaded)
                return;

            // Around the port's own transform, in the same frame as the target's parts, even when the target is on rails and not moved yet this tick.
            targetVessel.GetShape(targetShape, Core.Target.Transform, Core.Target.Transform.position);
            V3 axis = ToTargetFrame(zAxis);
            frontReach = targetShape.Reach(-axis);
            rearReach = targetShape.Reach(axis);
            targetMeasured = true;
        }

        // The distances the route flies at: past each side of the target by the most this vessel reaches from its own port, whichever way it's turned.
        private void UpdateLimits()
        {
            double clear = ownRadius + routeMargin;
            double overridenSafe = Math.Max(overridenSafeDistance.Val, 0);
            targetSize = overrideTargetSize ? (float)Math.Max(overridenTargetSize.Val, 10) : (float)(frontReach + clear);
            safeDistance = overrideSafeDistance ? (float)overridenSafe : (float)(sideReach + clear);
            rearDistance = overrideSafeDistance ? (float)overridenSafe : (float)(rearReach + clear);

            // A vessel has no capture, and its position is the origin of its control part, inside it. So stop where docking would start.
            stopDistance = portTarget ? 0 : targetSize;
        }

        // A vessel already past the target's reach on its side stays there. If not, it passes where the target reaches least far.
        private void ChooseSide()
        {
            V3 axis = ToTargetFrame(zAxis);
            V3 near = ToTargetFrame(lateralSep);
            routeSide = near.safeNormalized;
            sideReach = targetShape.Reach(routeSide);
            UpdateLimits();
            if (lateralSep.magnitude >= safeDistance)
                return;

            routeSide = targetShape.ShortestSide(axis, near);
            sideReach = targetShape.Reach(routeSide);
            UpdateLimits();
        }

        // Past the target's reach the legs of the route are clear by how they're measured, so the only one to check is the one that gets there.
        private void StartRoute()
        {
            V3 axis = ToTargetFrame(zAxis);
            V3 here = ToTargetFrame((Vector3d)DockingPoint() - Core.Target.Position);
            double need = ownRadius + routeMargin;

            if (zSep >= 0)
            {
                if (lateralSep.magnitude <= dockingcorridorRadius && IsAligned() && !PortsDisengaging())
                    dockingStep = DockingStep.DOCKING;
                else if (zSep >= targetSize)
                    dockingStep = DockingStep.MOVING_TO_START;
                else if (targetShape.Blocks(here, here - axis * (targetSize - zSep), need))
                    Leave(here, need);
                else
                    dockingStep = DockingStep.BACKING_UP;
                return;
            }

            ChooseSide();
            if (lateralSep.magnitude >= safeDistance)
                dockingStep = DockingStep.WRONG_SIDE_SWITCHSIDE;
            else if (-zSep >= rearDistance)
                dockingStep = DockingStep.WRONG_SIDE_LATERAL;
            else if (targetShape.Blocks(here, here + axis * (rearDistance + zSep), need))
                Leave(here, need);
            else
                dockingStep = DockingStep.WRONG_SIDE_BACKING_UP;
        }

        private void Leave(V3 here, double need)
        {
            if (++retries > maxRetries)
                StopWithMessage("#MechJeb_Docking_gaveUp"); //Docking autopilot stopped: it couldn't stay on the docking axis.
            else if (targetShape.Exit(here, need, out exitPoint))
                dockingStep = DockingStep.LEAVING_TARGET;
            else
                dockingStep = zSep < 0 ? DockingStep.WRONG_SIDE_LATERAL : DockingStep.BACKING_UP;
        }

        // Lateral closes with a time constant of about 3 s near the axis, so a short approach has to start closer to it to arrive inside the capture range.
        private double StartCorridor() => Math.Min(Math.Max(zSep / startCorridorSlope, minStartCorridor), dockingcorridorRadius);

        // Ports that have just undocked don't capture until they've been a metre apart (minDistanceToReEngage).
        private bool PortsDisengaging() =>
            (lastTarget is ModuleDockingNode port && port.state.Contains("Disengage")) || (referencePort != null && referencePort.state.Contains("Disengage"));

        // Whether it faces the port. Turning swings a port about the CoM, by many metres on a long vessel, so that happens before closing in.
        private bool IsAligned() => Vector3d.Angle(Vessel.GetTransform().up, zAxis) < alignedAngle;

        // Where this vessel meets the target: the node of the port it's controlled from. A port's reference transform is usually the part's
        // origin, 28 cm behind the node on a Clamp-O-Tron, and KSP measures its acquire and capture ranges between the nodes.
        private Vector3 DockingPoint()
        {
            if (Vessel.ReferenceTransform != referenceTransform)
            {
                referenceTransform = Vessel.ReferenceTransform;
                referencePort = Vessel.GetReferenceDockingPort();
                ownMeasured = false;
            }

            return referencePort != null ? referencePort.GetTransform().position : Vessel.GetTransform().position;
        }

        private void UpdateDistance()
        {
            Vector3d separation = DockingPoint() - Core.Target.PositionD;
            zAxis = Core.Target.DockingAxis.normalized;
            zSep = -Vector3d.Dot(separation, zAxis); //positive if we are in front of the target, negative if behind
            lateralSep = Vector3d.Exclude(zAxis, separation);
        }

        private void InitDocking()
        {
            lastTarget = Core.Target.Target;

            // A port pulls the vessel in from its acquire range, so RCS has to stay on until it captures: stopping short leaves nothing to brake
            // against the magnets, and they alone bring a 4 t vessel in at 50 cm/s. Docking ending the target ends the autopilot.
            var port = lastTarget as ModuleDockingNode;
            portTarget   = port != null;
            acquireRange = port != null ? port.acquireRange : 0;
            captureRange = port != null ? port.captureRange : 0;
            controlledFromPort = referencePort != null;

            if (port != null && port.state.Contains("Disabled"))
            {
                StopWithMessage("#MechJeb_Docking_portClosed"); //Docking autopilot stopped: the target port is closed.
                return;
            }

            if (port != null && referencePort != null && !MechJebModuleDockingGuidance.ArePortsCompatible(referencePort, port))
            {
                StopWithMessage("#MechJeb_Docking_incompatible"); //Docking autopilot stopped: the ports can't dock with each other.
                return;
            }

            MeasureShapes();
            UpdateLimits();
            StartRoute();
        }

        // Whether a part of this vessel is against the target. KSP keeps each part's contacts for as long as they last.
        private bool TouchingTarget()
        {
            Vessel targetVessel = Core.Target.Target.GetVessel();
            for (int i = 0; i < Vessel.parts.Count; i++)
            {
                var contacts = Vessel.parts[i].currentCollisions;
                for (int j = 0; j < contacts.Count; j++)
                {
                    Collider collider = contacts.KeyAt(j);
                    if (collider == null)
                        continue;

                    Part other = FlightGlobals.GetPartUpwardsCached(collider.gameObject);
                    if (other != null && other.vessel == targetVessel)
                        return true;
                }
            }

            return false;
        }

        private void StopWithMessage(string message)
        {
            if (Vessel.isActiveVessel)
            {
                string speed = Localizer.Format("#MechJeb_Docking_relativeSpeed", Core.Target.RelativeVelocity.magnitude.ToString("F2")); //Relative speed <<1>> m/s.
                ScreenMessages.PostScreenMessage(Localizer.Format(message) + "\n" + speed, 8, ScreenMessageStyle.UPPER_CENTER);
            }

            EndDocking();

            // Something went wrong close to the target, so this hands over a vessel that's holding still, and not one left tumbling from a hit.
            Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
            Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
        }

        private void EndDocking()
        {
            dockingStep = DockingStep.OFF;
            Users.Clear();
            Enabled = false;
        }

        private void DrawBoundingBox()
        {
            if (drawBoundingBox && Vessel == FlightGlobals.ActiveVessel)
            {
                GLUtils.DrawBoundingBox(Vessel.mainBody, Vessel, Vessel.GetBoundingBox(), Color.green);

                Vessel targetVessel = Core.Target.Target?.GetVessel();
                if (targetVessel != null)
                    GLUtils.DrawBoundingBox(targetVessel.mainBody, targetVessel, targetVessel.GetBoundingBox(), Color.blue);

                if (dockingStep != DockingStep.OFF && dockingStep != DockingStep.INIT && Core.Target.Target != null)
                {
                    GLUtils.DrawShape(targetShape, Core.Target.Position, Core.Target.Transform.rotation, Color.cyan);
                    GLUtils.DrawPath(Vessel.mainBody, RouteLine(), Color.yellow, false);
                }
            }
        }

        // What's left of the route, from where the vessel is to the port.
        private List<Vector3d> RouteLine()
        {
            Vector3d port = Core.Target.Position;
            Vector3d rear = zAxis * rearDistance;
            Vector3d start = port - zAxis * (targetSize + 1);
            Vector3d side = FromTargetFrame(routeSide) * safeDistance;

            routeLine.Clear();
            routeLine.Add(DockingPoint());
            switch (dockingStep)
            {
                case DockingStep.LEAVING_TARGET:
                    routeLine.Add(port + FromTargetFrame(exitPoint));
                    return routeLine;
                case DockingStep.WRONG_SIDE_BACKING_UP:
                    routeLine.Add(port + lateralSep + rear);
                    routeLine.Add(port + side + rear);
                    routeLine.Add(start + side);
                    break;
                case DockingStep.WRONG_SIDE_LATERAL:
                    routeLine.Add(port + side + rear);
                    routeLine.Add(start + side);
                    break;
                case DockingStep.WRONG_SIDE_SWITCHSIDE:
                    routeLine.Add(start + side);
                    break;
                case DockingStep.BACKING_UP:
                    routeLine.Add(start + lateralSep);
                    break;
            }

            routeLine.Add(start);
            routeLine.Add(port);
            return routeLine;
        }
    }
}
