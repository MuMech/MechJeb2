extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using MechJebLib.Control;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleDockingAutopilot : ComputerModule
    {
        public string status = "";

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL)), EditableInfoItem("#MechJeb_DockingSpeedLimit", InfoItem.Category.Thrust, rightLabel = "m/s")]
        //Docking speed limit
        public EditableDouble speedLimit = 1;

        [Persistent(pass = (int)Pass.LOCAL)]
        public readonly EditableDouble rol = new EditableDouble(0);

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool forceRol = false;

        [EditableInfoItem("#MechJeb_DockingSpeedLimit", InfoItem.Category.Thrust, rightLabel = "m/s")] //Docking speed limit
        public EditableDouble overridenSafeDistance = 5;

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool overrideSafeDistance = false;

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool overrideTargetSize = false;

        [EditableInfoItem("#MechJeb_DockingSpeedLimit", InfoItem.Category.Thrust, rightLabel = "m/s")] //Docking speed limit
        public EditableDouble overridenTargetSize = 10;

        public float safeDistance = 10;
        public float targetSize = 5;

        public bool drawBoundingBox;

        public enum DockingStep
        {
            INIT, WRONG_SIDE_BACKING_UP, WRONG_SIDE_LATERAL, WRONG_SIDE_SWITCHSIDE, BACKING_UP, MOVING_TO_START, DOCKING, OFF
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
        public double relativeZ;
        public double relativeLateral;

        private ITargetable lastTarget;
        private Part referencePart;
        private ModuleDockingNode referencePort;

        private const float dockingcorridorRadius = 1;
        private double stopDistance = 0.25;
        private double acquireRange;
        private double captureRange;

        public Box3d vesselBoundingBox;
        public Box3d targetBoundingBox;

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
            }
        }

        public override void OnDestroy() => GameEvents.onPartCouple.Remove(OnPartCouple);

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
        }

        protected override void OnModuleDisabled()
        {
            Core.RCS.Users.Remove(this);
            Core.Attitude.AttitudeDeactivate();
            dockingStep = DockingStep.OFF;
            drawBoundingBox = false;
        }

        private double FixSpeed(double s)
        {
            if (speedLimit != 0)
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

        private double BrakingAcceleration(Vector3d axis)
        {
            Vector3d localAxis = Vessel.ReferenceTransform.InverseTransformDirection(axis);
            return VesselState.RCSThrustAvailable.GetMagnitude(localAxis) * Core.RCS.rcsAccelFactor() / VesselState.Mass;
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
            return velocity + Vector3d.Cross(spin, Core.Target.Position - target.CoM);
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

            bool align = true;

            double timeToAxis;
            double timeToTargetSize;

            switch (dockingStep)
            {
                case DockingStep.WRONG_SIDE_BACKING_UP:
                    zApproachSpeed = MaxSpeedForDistance(safeDistance + zSep + 2.0, -zAxis);
                    if (lateralSep.magnitude < safeDistance)
                        latApproachSpeed *= -1;
                    else if (lateralSep.magnitude < safeDistance * 2)
                        latApproachSpeed = 0;

                    align = false;
                    status = Localizer.Format("#MechJeb_Docking_status1", zApproachSpeed.ToString("F2"),
                        latApproachSpeed.ToString()); //"Backing up at " <<1>> " m/s before moving on target side (lat: "<<2>> " m/s)"
                    break;

                case DockingStep.WRONG_SIDE_LATERAL:
                    zApproachSpeed = 0;
                    latApproachSpeed = -MaxSpeedForDistance(safeDistance - lateralSep.magnitude + 2.0, -lateralSep);
                    status = Localizer.Format("#MechJeb_Docking_status2",
                        latApproachSpeed.ToString("F2")); //Moving away from docking axis at <<1>> m/s to avoid hitting target on backing up
                    break;

                case DockingStep.WRONG_SIDE_SWITCHSIDE:
                    zApproachSpeed = -MaxSpeedForDistance(-zSep + targetSize, -zAxis);
                    if (lateralSep.magnitude < safeDistance)
                        latApproachSpeed *= -1;
                    else if (lateralSep.magnitude < safeDistance * 2)
                        latApproachSpeed = 0;

                    status = Localizer.Format("#MechJeb_Docking_status3", zApproachSpeed.ToString("F2"),
                        latApproachSpeed.ToString()); //"Moving at <<1>> m/s to get on the correct side of the target. (lat: <<2>> m/s)"
                    break;

                case DockingStep.BACKING_UP:
                    if (lateralSep.magnitude < safeDistance)
                        latApproachSpeed *= -1;
                    else if (lateralSep.magnitude < safeDistance * 2)
                        latApproachSpeed = 0;

                    zApproachSpeed = -MaxSpeedForDistance(1 + targetSize - zSep, -zAxis);
                    align = false;
                    status = Localizer.Format("#MechJeb_Docking_status4", zApproachSpeed.ToString("F2")); //"Backing up at " +  + " m/s"
                    break;

                case DockingStep.MOVING_TO_START:
                    if (zSep < safeDistance)
                        zApproachSpeed *= -1;
                    else
                        zApproachSpeed = 0;

                    status = Localizer.Format("#MechJeb_Docking_status5",
                        zApproachSpeed.ToString("F2")); //"Moving toward the starting point at " +  + " m/s."
                    break;

                case DockingStep.DOCKING:
                    timeToAxis = Math.Abs(lateralSep.magnitude / latApproachSpeed);
                    timeToTargetSize = Math.Abs(zSep / zApproachSpeed);

                    // The magnets are pulling by now and they turn the target for as long as they act, so unless we're too far off for the ports to
                    // capture, slowing down only makes that worse.
                    bool committed = zSep < acquireRange && lateralSep.magnitude < captureRange;

                    if (!committed && (zSep <= lateralSep.magnitude * 10 || timeToTargetSize <= timeToAxis * 10) && timeToAxis > 0 && timeToTargetSize > 0)
                    {
                        zApproachSpeed *= Math.Min(timeToTargetSize / timeToAxis, 1);
                    }

                    status = Localizer.Format("#MechJeb_Docking_status6", zApproachSpeed.ToString("F2"),
                        latApproachSpeed.ToString("F2")); //"Moving forward to dock at <<1>> / <<2>> m/s."
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

            // Purpose of of this section is to compensate for relative velocities because the docking code does poorly with very low speed limits
            // And can't seem to keep up if relative velocities are greater than the speed limit.
            //if (relativeZ > 0 && relativeZ > Math.Abs(zApproachSpeed))
            //    zApproachSpeed *= relativeZ / Math.Abs(zApproachSpeed);
            //if (relativeLateral > 0 && relativeLateral > Math.Abs(latApproachSpeed))
            //    latApproachSpeed *= relativeLateral / Math.Abs(latApproachSpeed);


            Vector3d adjustment = -lateralSep.normalized * latApproachSpeed + zApproachSpeed * zAxis;
            Core.RCS.SetTargetWorldVelocity(targetVel + adjustment);
            MechJebModuleDebugArrows.debugVector = adjustment;
            MechJebModuleDebugArrows.debugVector2 = -Core.Target.RelativePosition;
        }

        public override void OnFixedUpdate()
        {
            if (!Core.Target.NormalTargetExists)
            {
                EndDocking();
                return;
            }

            // Check if overrides have changed
            if (!overrideTargetSize)
                targetSize = targetBoundingBox.size.magnitude;
            else
                targetSize = (float)overridenTargetSize.Val;

            if (!overrideSafeDistance)
                safeDistance = vesselBoundingBox.size.magnitude + targetSize + 0.5f;
            else
                safeDistance = (float)overridenSafeDistance.Val;

            UpdateDistance();

            switch (dockingStep)
            {
                case DockingStep.INIT:
                    InitDocking();
                    break;

                case DockingStep.WRONG_SIDE_BACKING_UP:
                    if (-zSep > safeDistance)
                        dockingStep = DockingStep.WRONG_SIDE_LATERAL;
                    break;

                case DockingStep.WRONG_SIDE_LATERAL:
                    if (lateralSep.magnitude > safeDistance)
                        dockingStep = DockingStep.WRONG_SIDE_SWITCHSIDE;
                    break;

                case DockingStep.WRONG_SIDE_SWITCHSIDE:
                    if (zSep > 0)
                        dockingStep = DockingStep.BACKING_UP;
                    break;

                case DockingStep.BACKING_UP:
                    if (zSep > targetSize)
                        dockingStep = DockingStep.MOVING_TO_START;
                    break;

                case DockingStep.MOVING_TO_START:
                    if (lateralSep.magnitude < dockingcorridorRadius && zSep >= targetSize)
                        dockingStep = DockingStep.DOCKING;
                    break;

                case DockingStep.DOCKING:
                    if (zSep < stopDistance)
                    {
                        EndDocking();
                    }
                    // Added checks to make sure we're still good to dock.
                    else if (lateralSep.magnitude > dockingcorridorRadius) // far from docking axis
                    {
                        if (zSep < 0) //we're behind the target
                            dockingStep = DockingStep.WRONG_SIDE_BACKING_UP;
                        else if (lateralSep.magnitude > dockingcorridorRadius) // in front but far from docking axis
                        {
                            if (zSep < 1)
                                dockingStep = DockingStep.MOVING_TO_START;
                        }
                    }

                    break;
            }
        }

        // Where this vessel meets the target: the node of the port it's controlled from. A port's reference transform is usually the part's
        // origin, 28 cm behind the node on a Clamp-O-Tron, and KSP measures its acquire and capture ranges between the nodes.
        private Vector3 DockingPoint()
        {
            Part part = Vessel.GetReferenceTransformPart();
            if (part != referencePart)
            {
                referencePart = part;
                referencePort = Vessel.GetReferenceDockingPort();
            }

            return referencePort != null ? referencePort.GetTransform().position : Vessel.GetTransform().position;
        }

        private void UpdateDistance()
        {
            Vector3d separation = DockingPoint() - Core.Target.Position;
            zAxis = Core.Target.DockingAxis.normalized;
            zSep = -Vector3d.Dot(separation, zAxis); //positive if we are in front of the target, negative if behind
            lateralSep = Vector3d.Exclude(zAxis, separation);
            relativeZ = Vector3d.Dot(Core.Target.RelativeVelocity, zAxis);
            relativeLateral = Vector3d.Dot(lateralSep, Core.Target.RelativeVelocity);
        }

        private void InitDocking()
        {
            lastTarget = Core.Target.Target;

            try
            {
                vesselBoundingBox = Vessel.GetBoundingBox();
                targetBoundingBox = lastTarget.GetVessel().GetBoundingBox();

                if (!overrideTargetSize)
                    targetSize = targetBoundingBox.size.magnitude;
                else
                    targetSize = (float)overridenTargetSize.Val;

                if (!overrideSafeDistance)
                    safeDistance = vesselBoundingBox.size.magnitude + targetSize + 0.5f;
                else
                    safeDistance = (float)overridenSafeDistance.Val;

                // A port pulls the vessel in from its acquire range, so RCS has to stay on until it captures: stopping short leaves nothing to brake
                // against the magnets, and they alone bring a 4 t vessel in at 50 cm/s. Docking ending the target ends the autopilot.
                var port = Core.Target.Target as ModuleDockingNode;
                stopDistance = port != null ? 0 : 0.25;
                acquireRange = port != null ? port.acquireRange : 0;
                captureRange = port != null ? port.captureRange : 0;
            }
            catch (Exception e)
            {
                Print(e);
            }

            if (zSep < 0) //we're behind the target
            {
                // If we're more than half our own bounding box size behind the target port then use wrong side behavior
                // Still needs improvement. The reason for these changes is that to prevent wrong side behavior when our
                // port slipped behind the target by a fractional amount. The result is that rather than avoiding the
                // target ship we end up trying to pass right through it.
                // What's really needed here is code that compares bounding box positions to determine if we just try to back up or change sides completely.
                if (Math.Abs(zSep) > vesselBoundingBox.size.magnitude * 0.5f)
                    dockingStep = DockingStep.WRONG_SIDE_BACKING_UP;
                else
                    dockingStep = DockingStep.BACKING_UP; // Just back straight up.
            }
            else if (lateralSep.magnitude > dockingcorridorRadius) // in front but far from docking axis
            {
                if (zSep < targetSize)
                    dockingStep = DockingStep.BACKING_UP;
                else
                    dockingStep = DockingStep.MOVING_TO_START;
            }
            else
                dockingStep = DockingStep.DOCKING;
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
                vesselBoundingBox = Vessel.GetBoundingBox();
                GLUtils.DrawBoundingBox(Vessel.mainBody, Vessel, vesselBoundingBox, Color.green);

                if (Core.Target.Target != null)
                {
                    Vessel targetVessel = Core.Target.Target.GetVessel();
                    targetBoundingBox = targetVessel.GetBoundingBox();
                    GLUtils.DrawBoundingBox(targetVessel.mainBody, targetVessel, targetBoundingBox, Color.blue);
                }
            }
        }
    }
}
