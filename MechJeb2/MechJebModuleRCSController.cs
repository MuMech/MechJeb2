extern alias JetBrainsAnnotations;
using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSController : ComputerModule
    {
        public Vector3d targetVelocity = Vector3d.zero;

        public readonly PIDControllerV2 pid;
        private         Vector3d        lastAct                 = Vector3d.zero;
        private         Vector3d        worldVelocityDelta      = Vector3d.zero;
        private         Vector3d        prev_worldVelocityDelta = Vector3d.zero;

        private enum ControlType
        {
            TARGET_VELOCITY,
            VELOCITY_ERROR,
            VELOCITY_TARGET_REL,
            POSITION_TARGET_REL
        }

        private ControlType controlType;

        [Persistent(pass = (int)Pass.GLOBAL)]
        [ToggleInfoItem("#MechJeb_conserveFuel", InfoItem.Category.Thrust)] //Conserve RCS fuel
        public readonly bool conserveFuel;

        [EditableInfoItem("#MechJeb_conserveThreshold", InfoItem.Category.Thrust, rightLabel = "m/s")] //Conserve RCS fuel threshold
        public readonly EditableDouble conserveThreshold = 0.05;

        [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        [EditableInfoItem("#MechJeb_RCSTf", InfoItem.Category.Thrust)] //RCS Tf
        public EditableDouble Tf = 1;

        [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble Kp = 0.125;

        [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble Ki = 0.07;

        [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble Kd = 0.53;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool rcsManualPID;

        [Persistent(pass = (int)Pass.GLOBAL)]
        [ToggleInfoItem("#MechJeb_RCSThrottle", InfoItem.Category.Thrust)] //RCS throttle when 0kn thrust
        public bool rcsThrottle = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        [ToggleInfoItem("#MechJeb_rcsForRotation", InfoItem.Category.Thrust)] //Use RCS for rotation
        public bool rcsForRotation = true;

        public MechJebModuleRCSController(MechJebCore core)
            : base(core)
        {
            Priority = 600;
            pid      = new PIDControllerV2(Kp, Ki, Kd, 1, -1);
        }

        protected override void OnModuleEnabled()
        {
            setPIDParameters();
            pid.Reset();
            lastAct                 = Vector3d.zero;
            worldVelocityDelta      = Vector3d.zero;
            prev_worldVelocityDelta = Vector3d.zero;
            controlType             = ControlType.VELOCITY_ERROR;
            base.OnModuleEnabled();
        }

        public bool rcsDeactivate()
        {
            Users.Clear();
            return true;
        }

        public void setPIDParameters()
        {
            if (rcsManualPID)
            {
                pid.Kd = Kd;
                pid.Kp = Kp;
                pid.Ki = Ki;
            }
            else
            {
                Tf = Math.Max(Tf, 0.02);

                pid.Kd = 0.53 / Tf;
                pid.Kp = pid.Kd / (3 * Math.Sqrt(2) * Tf);
                pid.Ki = pid.Kp / (12 * Math.Sqrt(2) * Tf);

                Kd.Val = pid.Kd;
                Kp.Val = pid.Kp;
                Ki.Val = pid.Ki;
            }
        }

        [GeneralInfoItem("#MechJeb_RCSPid", InfoItem.Category.Thrust)] //RCS Pid
        public void PIDGUI()
        {
            GUILayout.BeginVertical();
            rcsManualPID = GUILayout.Toggle(rcsManualPID, "RCS Manual Pid");
            if (rcsManualPID)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("P", GUILayout.ExpandWidth(false));
                Kp.Text = GUILayout.TextField(Kp.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));

                GUILayout.Label("I", GUILayout.ExpandWidth(false));
                Kd.Text = GUILayout.TextField(Kd.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));

                GUILayout.Label("D", GUILayout.ExpandWidth(false));
                Ki.Text = GUILayout.TextField(Ki.Text, GUILayout.ExpandWidth(true), GUILayout.Width(60));
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Tf", GUILayout.ExpandWidth(false));
                Tf.Text = GUILayout.TextField(Tf.Text, GUILayout.ExpandWidth(true), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("P", GUILayout.ExpandWidth(false));
                GUILayout.Label(Kp.Val.ToString("F4"), GUILayout.ExpandWidth(true));

                GUILayout.Label("I", GUILayout.ExpandWidth(false));
                GUILayout.Label(Ki.Val.ToString("F4"), GUILayout.ExpandWidth(true));

                GUILayout.Label("D", GUILayout.ExpandWidth(false));
                GUILayout.Label(Kd.Val.ToString("F4"), GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            setPIDParameters();
        }

        // When evaluating how fast RCS can accelerate and calculate a speed that available thrust should
        // be multiplied by that since the PID controller actual lower the used acceleration
        public double rcsAccelFactor() => pid.Kp;

        protected override void OnModuleDisabled()
        {
            Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
            base.OnModuleDisabled();
        }

        public void SetTargetWorldVelocity(Vector3d vel)
        {
            targetVelocity = vel;
            controlType    = ControlType.TARGET_VELOCITY;
        }

        public void SetWorldVelocityError(Vector3d dv)
        {
            worldVelocityDelta = -dv;
            if (controlType != ControlType.VELOCITY_ERROR)
            {
                prev_worldVelocityDelta = worldVelocityDelta;
                controlType             = ControlType.VELOCITY_ERROR;
            }
        }

        public void SetTargetRelative(Vector3d vel)
        {
            targetVelocity = vel;
            controlType    = ControlType.VELOCITY_TARGET_REL;
        }

        public override void Drive(FlightCtrlState s)
        {
            setPIDParameters();

            switch (controlType)
            {
                case ControlType.TARGET_VELOCITY:
                    // Removed the gravity since it also affect the target and we don't know the target pos here.
                    // Since the difference is negligable for docking it's removed
                    // TODO : add it back once we use the RCS Controler for other use than docking. Account for current acceleration beside gravity ?
                    worldVelocityDelta = VesselState.orbitalVelocity - targetVelocity;
                    //worldVelocityDelta += TimeWarp.fixedDeltaTime * vesselState.gravityForce; //account for one frame's worth of gravity
                    //worldVelocityDelta -= TimeWarp.fixedDeltaTime * gravityForce = FlightGlobals.getGeeForceAtPosition(  Here be the target position  ); ; //account for one frame's worth of gravity
                    break;

                case ControlType.VELOCITY_ERROR:
                    // worldVelocityDelta already contains the velocity error
                    break;

                case ControlType.VELOCITY_TARGET_REL:
                    if (Core.Target.Target == null)
                    {
                        rcsDeactivate();
                        return;
                    }

                    worldVelocityDelta = Core.Target.RelativeVelocity - targetVelocity;
                    break;
            }

            // We work in local vessel coordinate
            Vector3d velocityDelta = Quaternion.Inverse(Vessel.GetTransform().rotation) * worldVelocityDelta;

            if (!conserveFuel || velocityDelta.magnitude > conserveThreshold)
            {
                if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                {
                    Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                }

                var rcs = new Vector3d();

                for (int i = 0; i < Vector6.Values.Length; i++)
                {
                    Vector6.Direction dir = Vector6.Values[i];
                    double dirDv = Vector3d.Dot(velocityDelta, Vector6.Directions[(int)dir]);
                    double dirAvail = VesselState.rcsThrustAvailable[dir];
                    if (dirAvail > 0 && Math.Abs(dirDv) > 0.001)
                    {
                        double dirAction = dirDv / (dirAvail * TimeWarp.fixedDeltaTime / VesselState.mass);
                        if (dirAction > 0)
                        {
                            rcs += Vector6.Directions[(int)dir] * dirAction;
                        }
                    }
                }

                Vector3d omega = Vector3d.zero;

                switch (controlType)
                {
                    case ControlType.TARGET_VELOCITY:
                        omega = Quaternion.Inverse(Vessel.GetTransform().rotation) * (Vessel.acceleration - VesselState.gravityForce);
                        break;

                    case ControlType.VELOCITY_TARGET_REL:
                    case ControlType.VELOCITY_ERROR:
                        omega                   = (worldVelocityDelta - prev_worldVelocityDelta) / TimeWarp.fixedDeltaTime;
                        prev_worldVelocityDelta = worldVelocityDelta;
                        break;
                }

                rcs = pid.Compute(rcs, omega);

                // Disabled the low pass filter for now. Was doing more harm than good
                //rcs = lastAct + (rcs - lastAct) * (1 / ((Tf / TimeWarp.fixedDeltaTime) + 1));
                lastAct = rcs;

                s.X = Mathf.Clamp((float)rcs.x, -1, 1);
                s.Y = Mathf.Clamp((float)rcs.z, -1, 1); //note that z and
                s.Z = Mathf.Clamp((float)rcs.y, -1, 1); //y must be swapped
            }
            else if (conserveFuel)
            {
                if (Vessel.ActionGroups[KSPActionGroup.RCS])
                {
                    Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
                }
            }

            base.Drive(s);
        }
    }
}
