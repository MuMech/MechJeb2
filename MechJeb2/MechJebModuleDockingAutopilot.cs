using System;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleDockingAutopilot : ComputerModule
    {
        public string status = "";

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        [EditableInfoItem("#MechJeb_DockingSpeedLimit", InfoItem.Category.Thrust, rightLabel = "m/s")]//Docking speed limit
        public EditableDouble speedLimit = 1;
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble rol = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public Boolean forceRol = false;

        [EditableInfoItem("#MechJeb_DockingSpeedLimit", InfoItem.Category.Thrust, rightLabel = "m/s")]//Docking speed limit
        public EditableDouble overridenSafeDistance = 5;

        [Persistent(pass = (int)Pass.Local)]
        public Boolean overrideSafeDistance = false;

        [Persistent(pass = (int)Pass.Local)]
        public Boolean overrideTargetSize = false;
        [EditableInfoItem("#MechJeb_DockingSpeedLimit", InfoItem.Category.Thrust, rightLabel = "m/s")]//Docking speed limit
        public EditableDouble overridenTargetSize = 10;

        public float safeDistance = 10;
        public float targetSize = 5;

        public Boolean drawBoundingBox = false;

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

        Vector3d zAxis;
        public double zSep;
        public Vector3d lateralSep;
        public double relativeZ;
        public double relativeLateral;

        ITargetable lastTarget;

        const float dockingcorridorRadius = 1;
        double acquireRange = 0.25;

        public MechJebModuleDockingAutopilot.Box3d vesselBoundingBox;
        public MechJebModuleDockingAutopilot.Box3d targetBoundingBox;

        public MechJebModuleDockingAutopilot(MechJebCore core)
            : base(core)
        {
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
            {
                core.AddToPostDrawQueue(DrawBoundingBox);

                // Turn off docking AP on successful docking (in case other checks for successful docking fail)
                GameEvents.onPartCouple.Add((GameEvents.FromToAction<Part, Part> ev) =>
                {
                    if (dockingStep != DockingStep.OFF)
                    {
                        if (ev.from.vessel == vessel || ev.to.vessel == vessel)
                        {
                            EndDocking();
                        }
                    }
                });
            }
        }

        public override void OnModuleEnabled()
        {
            core.rcs.users.Add(this);
            core.attitude.users.Add(this);
            dockingStep = DockingStep.INIT;
        }

        public override void OnModuleDisabled()
        {
            core.rcs.users.Remove(this);
            core.attitude.attitudeDeactivate();
            dockingStep = DockingStep.OFF;
            drawBoundingBox = false;
        }

        private double FixSpeed(double s)
        {
            if (speedLimit != 0)
            {
                if (s >  speedLimit) s =  speedLimit;
                if (s < -speedLimit) s = -speedLimit;
            }
            return s;
        }

        // Get how fast the ship can move on the axis and still be able to stop before reaching the distance.
        // v2 = u2 + 2 * a * d
        // initial speed v - final speed u = 0  - distance d
        // Maximum speed to brake in time = sqrt( 2 * a * d )
        private double MaxSpeedForDistance(double distance, Vector3d axis)
        {
            Vector3d localAxis = vessel.ReferenceTransform.InverseTransformDirection(axis);
            return FixSpeed(Math.Sqrt(2.0 * Math.Abs(distance) * vesselState.rcsThrustAvailable.GetMagnitude(localAxis) * core.rcs.rcsAccelFactor() / vesselState.mass));
        }

        public override void Drive(FlightCtrlState s)
        {
            if (!core.target.NormalTargetExists)
            {
                EndDocking();
                return;
            }

            if (dockingStep == DockingStep.OFF || dockingStep == DockingStep.INIT)
                return;

            Vector3d targetVel = core.target.TargetOrbit.GetVel();

            double zApproachSpeed = MaxSpeedForDistance(Math.Max(zSep - acquireRange, 0), -zAxis);
            double latApproachSpeed = MaxSpeedForDistance(lateralSep.magnitude, -lateralSep); // TODO check if it should be +lateralSep

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
                    status = Localizer.Format("#MechJeb_Docking_status1", zApproachSpeed.ToString("F2"),latApproachSpeed.ToString());//"Backing up at " <<1>> " m/s before moving on target side (lat: "<<2>> " m/s)"
                    break;

                case DockingStep.WRONG_SIDE_LATERAL:
                    zApproachSpeed = 0;
                    latApproachSpeed = -MaxSpeedForDistance(safeDistance - lateralSep.magnitude + 2.0, -lateralSep);
                    status = Localizer.Format("#MechJeb_Docking_status2", latApproachSpeed.ToString("F2"));//Moving away from docking axis at <<1>> m/s to avoid hitting target on backing up
                    break;

                case DockingStep.WRONG_SIDE_SWITCHSIDE:
                    zApproachSpeed = -MaxSpeedForDistance(-zSep + targetSize, -zAxis);
                    if (lateralSep.magnitude < safeDistance)
                        latApproachSpeed *= -1;
                    else if (lateralSep.magnitude < safeDistance * 2)
                        latApproachSpeed = 0;
                    
                    status = Localizer.Format("#MechJeb_Docking_status3", zApproachSpeed.ToString("F2"),latApproachSpeed.ToString());//"Moving at <<1>> m/s to get on the correct side of the target. (lat: <<2>> m/s)"
                    break;

                case DockingStep.BACKING_UP:
                    if (lateralSep.magnitude < safeDistance)
                        latApproachSpeed *= -1;
                    else if (lateralSep.magnitude < safeDistance * 2)
                        latApproachSpeed = 0;

                    zApproachSpeed = -MaxSpeedForDistance(1 + targetSize - zSep, -zAxis);
                    align = false;
                    status = Localizer.Format("#MechJeb_Docking_status4",zApproachSpeed.ToString("F2"));//"Backing up at " +  + " m/s"
                    break;

                case DockingStep.MOVING_TO_START:
                    if (zSep < safeDistance)
                        zApproachSpeed *= -1;
                    else
                        zApproachSpeed = 0;

                    status = Localizer.Format("#MechJeb_Docking_status5", zApproachSpeed.ToString("F2"));//"Moving toward the starting point at " +  + " m/s."
                    break;

                case DockingStep.DOCKING:
                    timeToAxis = Math.Abs(lateralSep.magnitude / latApproachSpeed);
                    timeToTargetSize = Math.Abs(zSep / zApproachSpeed);

                    if ((zSep <= lateralSep.magnitude * 10 || timeToTargetSize <= timeToAxis * 10) && timeToAxis > 0 && timeToTargetSize > 0)
                    {
                        zApproachSpeed *= Math.Min(timeToTargetSize / timeToAxis, 1);
                        latApproachSpeed = FixSpeed(latApproachSpeed*2);
                    }

                    status = Localizer.Format("#MechJeb_Docking_status6", zApproachSpeed.ToString("F2"),latApproachSpeed.ToString("F2"));//"Moving forward to dock at <<1>> / <<2>> m/s."
                    break;

                default:
                    break;
            }

            if (!align)
                core.attitude.attitudeTo(Quaternion.LookRotation(vessel.GetTransform().up, -vessel.GetTransform().forward), AttitudeReference.INERTIAL, this);
            else
                if (forceRol)
                    core.attitude.attitudeTo(Quaternion.LookRotation(Vector3d.back, Vector3d.up) * Quaternion.AngleAxis(-(float)rol, Vector3d.back), AttitudeReference.TARGET_ORIENTATION, this);
                else
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.TARGET_ORIENTATION, this);

            // Purpose of of this section is to compensate for relative velocities because the docking code does poorly with very low speed limits
            // And can't seem to keep up if relative velocities are greater than the speed limit.
            //if (relativeZ > 0 && relativeZ > Math.Abs(zApproachSpeed))
            //    zApproachSpeed *= relativeZ / Math.Abs(zApproachSpeed);
            //if (relativeLateral > 0 && relativeLateral > Math.Abs(latApproachSpeed))
            //    latApproachSpeed *= relativeLateral / Math.Abs(latApproachSpeed);
            

            Vector3d adjustment = (-lateralSep.normalized * latApproachSpeed) + (zApproachSpeed * zAxis);
            core.rcs.SetTargetWorldVelocity(targetVel + adjustment);
            MechJebModuleDebugArrows.debugVector = adjustment;
            MechJebModuleDebugArrows.debugVector2 = -core.target.RelativePosition;
        }


        public override void OnFixedUpdate()
        {
            if (!core.target.NormalTargetExists)
            {
                EndDocking();
                return;
            }

            // Check if overrides have changed
            if (!overrideTargetSize)
                targetSize = targetBoundingBox.size.magnitude;
            else
                targetSize = (float)overridenTargetSize.val;

            if (!overrideSafeDistance)
                safeDistance = vesselBoundingBox.size.magnitude + targetSize + 0.5f;
            else
                safeDistance = (float)overridenSafeDistance.val;

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
                    if (zSep < acquireRange)
                    {
                        EndDocking();
                    }
                // Added checks to make sure we're still good to dock.
                    else if (lateralSep.magnitude > dockingcorridorRadius) // far from docking axis
                    {
                        if (zSep < 0)  //we're behind the target
                            dockingStep = DockingStep.WRONG_SIDE_BACKING_UP;
                        else if (lateralSep.magnitude > dockingcorridorRadius) // in front but far from docking axis
                        {
                            if (zSep < 1)
                                dockingStep = DockingStep.MOVING_TO_START;
                        }
                    }

                    break;

                default:
                    break;
            }

        }

        void UpdateDistance()
        {
            Vector3d separation = core.target.RelativePosition;
            zAxis = core.target.DockingAxis.normalized;
            zSep = -Vector3d.Dot(separation, zAxis); //positive if we are in front of the target, negative if behind
            lateralSep = Vector3d.Exclude(zAxis, separation);
            relativeZ = Vector3d.Dot(core.target.RelativeVelocity, zAxis);
            relativeLateral = Vector3d.Dot(lateralSep, core.target.RelativeVelocity);
        }

        void InitDocking()
        {
            lastTarget = core.target.Target;

            try
            {
                vesselBoundingBox = vessel.GetBoundingBox();
                targetBoundingBox = lastTarget.GetVessel().GetBoundingBox();

                if (!overrideTargetSize)
                    targetSize = targetBoundingBox.size.magnitude;
                else
                    targetSize = (float)overridenTargetSize.val;
                
                if (!overrideSafeDistance)
                    safeDistance = vesselBoundingBox.size.magnitude + targetSize + 0.5f;
                else
                    safeDistance = (float)overridenSafeDistance.val;

                if (core.target.Target is ModuleDockingNode)
                    acquireRange = ((ModuleDockingNode)core.target.Target).acquireRange * 0.5;
                else
                    acquireRange = 0.25;

            }
            catch (Exception e)
            {
                print(e);
            }

            if (zSep < 0)  //we're behind the target
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

        void EndDocking()
        {
            dockingStep = DockingStep.OFF;
            users.Clear();
            enabled = false;
        }

        void DrawBoundingBox()
        {
            if (drawBoundingBox && vessel == FlightGlobals.ActiveVessel)
            {
                vesselBoundingBox = vessel.GetBoundingBox();
                GLUtils.DrawBoundingBox(vessel.mainBody, vessel, vesselBoundingBox, Color.green);

                if (core.target.Target != null)
                {
                    Vessel targetVessel = core.target.Target.GetVessel();
                    targetBoundingBox = targetVessel.GetBoundingBox();
                    GLUtils.DrawBoundingBox(targetVessel.mainBody, targetVessel, targetBoundingBox, Color.blue);
                }
            }
        }
    }
}
