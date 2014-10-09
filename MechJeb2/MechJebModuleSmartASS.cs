using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSmartASS : DisplayModule
    {
        public enum Mode
        {
            ORBITAL,
            SURFACE,
            TARGET,
            ADVANCED,
            AUTO
        }
        public enum Target
        {
            OFF,
            KILLROT,
            NODE,
            SURFACE,
            PROGRADE,
            RETROGRADE,
            NORMAL_PLUS,
            NORMAL_MINUS,
            RADIAL_PLUS,
            RADIAL_MINUS,
            RELATIVE_PLUS,
            RELATIVE_MINUS,
            TARGET_PLUS,
            TARGET_MINUS,
            PARALLEL_PLUS,
            PARALLEL_MINUS,
            ADVANCED,
            AUTO,
            SURFACE_PROGRADE,
            SURFACE_RETROGRADE,
            HORIZONTAL_PLUS,
            HORIZONTAL_MINUS,
            VERTICAL_PLUS
        }
        public static Mode[] Target2Mode = { Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.SURFACE, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.ADVANCED, Mode.AUTO, Mode.SURFACE, Mode.SURFACE, Mode.SURFACE, Mode.SURFACE, Mode.SURFACE };
        public static string[] ModeTexts = { "OBT", "SURF", "TGT", "ADV", "AUTO" };
        public static string[] TargetTexts = { "OFF", "KILL\nROT", "NODE", "SURF", "PRO\nGRAD", "RETR\nGRAD", "NML\n+", "NML\n-", "RAD\n+", "RAD\n-", "RVEL\n+", "RVEL\n-", "TGT\n+", "TGT\n-", "PAR\n+", "PAR\n-", "ADV", "AUTO", "SVEL\n+", "SVEL\n-", "HVEL\n+", "HVEL\n-", "UP" };
        public static string[] ReferenceTexts = Enum.GetNames(typeof(AttitudeReference));
        public static string[] directionTexts = Enum.GetNames(typeof(Vector6.Direction));

        public static GUIStyle btNormal, btActive, btAuto;

        [Persistent(pass = (int)Pass.Local)]
        public Mode mode = Mode.ORBITAL;
        [Persistent(pass = (int)Pass.Local)]
        public Target target = Target.OFF;
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfHdg = new EditableDouble(90);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfPit = new EditableDouble(90);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfRol = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfVelYaw = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfVelPit = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfVelRol = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble rol = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public AttitudeReference advReference = AttitudeReference.INERTIAL;
        [Persistent(pass = (int)Pass.Local)]
        public Vector6.Direction advDirection = Vector6.Direction.FORWARD;
        [Persistent(pass = (int)Pass.Local)]
        public Boolean forceRol = false;

        [Persistent(pass = (int)Pass.Global)]
        public bool autoDisableSmartASS = true;
        [GeneralInfoItem("Disable SmartASS automatically", InfoItem.Category.Misc)]
        public void AutoDisableSmartASS()
        {
            autoDisableSmartASS = GUILayout.Toggle(autoDisableSmartASS, "Disable SmartASS automatically");
        }

        public MechJebModuleSmartASS(MechJebCore core) : base(core) { }

        protected void ModeButton(Mode bt)
        {
            if (GUILayout.Button(ModeTexts[(int)bt], (mode == bt) ? btActive : btNormal, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                mode = bt;
            }
        }

        protected void TargetButton(Target bt)
        {
            if (GUILayout.Button(TargetTexts[(int)bt], (target == bt) ? btActive : btNormal, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                target = bt;
                Engage();
            }
        }

        protected void TargetButtonNoEngage(Target bt)
        {
            if (GUILayout.Button(TargetTexts[(int)bt], (target == bt) ? btActive : btNormal, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                target = bt;
            }
        }

        protected void ForceRoll()
        {
            GUILayout.BeginHorizontal();
            forceRol = GUILayout.Toggle(forceRol, "Force Roll :", GUILayout.ExpandWidth(false));
            {
                Engage();
            }
            rol.text = GUILayout.TextField(rol.text, GUILayout.Width(30));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }


        protected override void WindowGUI(int windowID)
        {
            if (btNormal == null)
            {
                btNormal = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor = btNormal.focused.textColor = Color.white;
                btNormal.hover.textColor = btNormal.active.textColor = Color.yellow;
                btNormal.onNormal.textColor = btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive = new GUIStyle(btNormal);
                btActive.active = btActive.onActive;
                btActive.normal = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover = btActive.onHover;

                btAuto = new GUIStyle(btNormal);
                btAuto.normal.textColor = Color.red;
                btAuto.onActive = btAuto.onFocused = btAuto.onHover = btAuto.onNormal = btAuto.active = btAuto.focused = btAuto.hover = btAuto.normal;
            }

            // If any other module use the attitude controler then let them do it
            if (core.attitude.enabled && core.attitude.users.Count(u => !this.Equals(u)) > 0)
            {
                if (autoDisableSmartASS)
                {
                    target = Target.OFF;
                    if (core.attitude.users.Contains(this)) core.attitude.users.Remove(this); // so we don't suddenly turn on when the other autopilot finishes
                }
                GUILayout.Button("AUTO", btAuto, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                TargetButton(Target.OFF);
                TargetButton(Target.KILLROT);
                TargetButton(Target.NODE);
                GUILayout.EndHorizontal();

                GUILayout.Label("Mode:");
                GUILayout.BeginHorizontal();
                ModeButton(Mode.ORBITAL);
                ModeButton(Mode.SURFACE);
                ModeButton(Mode.TARGET);
                ModeButton(Mode.ADVANCED);
                GUILayout.EndHorizontal();

                switch (mode)
                {
                    case Mode.ORBITAL:

                        GUILayout.BeginHorizontal();
                        TargetButton(Target.PROGRADE);
                        TargetButton(Target.NORMAL_PLUS);
                        TargetButton(Target.RADIAL_PLUS);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        TargetButton(Target.RETROGRADE);
                        TargetButton(Target.NORMAL_MINUS);
                        TargetButton(Target.RADIAL_MINUS);
                        GUILayout.EndHorizontal();

                        ForceRoll();

                        break;
                    case Mode.SURFACE:
                        GUILayout.BeginHorizontal();
                        TargetButton(Target.SURFACE_PROGRADE);
                        TargetButton(Target.SURFACE_RETROGRADE);
                        TargetButtonNoEngage(Target.SURFACE);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        TargetButton(Target.HORIZONTAL_PLUS);
                        TargetButton(Target.HORIZONTAL_MINUS);
                        TargetButton(Target.VERTICAL_PLUS);
                        GUILayout.EndHorizontal();
                        if (target == Target.SURFACE) {
                            GuiUtils.SimpleTextBox("HDG:", srfHdg);
                            GuiUtils.SimpleTextBox("PIT:", srfPit);
                            GuiUtils.SimpleTextBox("ROL:", srfRol);
                            if (GUILayout.Button("EXECUTE")) {
                                Engage();
                            }
                        } else if (target == Target.SURFACE_PROGRADE || target == Target.SURFACE_RETROGRADE) {
                            GUILayout.BeginHorizontal();
                            GuiUtils.SimpleTextBox("ROL", srfVelRol, "°", 60);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfVelRol -= 1;
                                Engage();
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfVelRol += 1;
                                Engage();
                            }
                            if (GUILayout.Button("CUR", GUILayout.ExpandWidth(false))) {
                                srfVelRol = -vesselState.vesselRoll.value;
                                Engage();
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfVelRol = 0;
                                Engage();
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GuiUtils.SimpleTextBox("PIT", srfVelPit, "°", 60);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfVelPit -= 1;
                                Engage();
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfVelPit += 1;
                                Engage();
                            }
                            if (GUILayout.Button("CUR", GUILayout.ExpandWidth(false))) {
                                srfVelPit = vesselState.AoA.value;
                                Engage();
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfVelPit = 0;
                                Engage();
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GuiUtils.SimpleTextBox("YAW", srfVelYaw, "°", 60);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfVelYaw -= 1;
                                Engage();
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfVelYaw += 1;
                                Engage();
                            }
                            if (GUILayout.Button("CUR", GUILayout.ExpandWidth(false))) {
                                srfVelYaw = -vesselState.AoS.value;
                                Engage();
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfVelYaw = 0;
                                Engage();
                            }
                            GUILayout.EndHorizontal();
                        }
                        break;
                    case Mode.TARGET:
                        if (core.target.NormalTargetExists)
                        {
                            GUILayout.BeginHorizontal();
                            TargetButton(Target.TARGET_PLUS);
                            TargetButton(Target.RELATIVE_PLUS);
                            TargetButton(Target.PARALLEL_PLUS);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            TargetButton(Target.TARGET_MINUS);
                            TargetButton(Target.RELATIVE_MINUS);
                            TargetButton(Target.PARALLEL_MINUS);
                            GUILayout.EndHorizontal();

                            ForceRoll();
                        }
                        else
                        {
                            GUILayout.Label("Please select a target");
                        }
                        break;
                    case Mode.ADVANCED:
                        GUILayout.Label("Reference:");
                        advReference = (AttitudeReference)GuiUtils.ComboBox.Box((int)advReference, ReferenceTexts, this);

                        GUILayout.Label("Direction:");
                        advDirection = (Vector6.Direction)GuiUtils.ComboBox.Box((int)advDirection, directionTexts, directionTexts);

                        ForceRoll();

                        if (GUILayout.Button("EXECUTE", btNormal, GUILayout.ExpandWidth(true)))
                        {
                            target = Target.ADVANCED;
                            Engage();
                        }
                        break;
                    case Mode.AUTO:
                        break;
                }

                GUILayout.EndVertical();
            }

            base.WindowGUI(windowID);
        }

        public void Engage()
        {
            Quaternion attitude = new Quaternion();
            Vector3d direction = Vector3d.zero;
            AttitudeReference reference = AttitudeReference.ORBIT;
            switch (target)
            {
                case Target.OFF:
                    core.attitude.attitudeDeactivate();
                    return;
                case Target.KILLROT:
                    core.attitude.attitudeKILLROT = true;
                    attitude = Quaternion.LookRotation(part.vessel.GetTransform().up, -part.vessel.GetTransform().forward);
                    reference = AttitudeReference.INERTIAL;
                    break;
                case Target.NODE:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.MANEUVER_NODE;
                    break;
                case Target.SURFACE:
                    attitude = Quaternion.AngleAxis((float)srfHdg, Vector3.up)
                                 * Quaternion.AngleAxis(-(float)srfPit, Vector3.right)
                                 * Quaternion.AngleAxis(-(float)srfRol, Vector3.forward);
                    reference = AttitudeReference.SURFACE_NORTH;
                    break;
                case Target.PROGRADE:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RETROGRADE:
                    direction = Vector3d.back;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.NORMAL_PLUS:
                    direction = Vector3d.left;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.NORMAL_MINUS:
                    direction = Vector3d.right;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RADIAL_PLUS:
                    direction = Vector3d.up;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RADIAL_MINUS:
                    direction = Vector3d.down;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RELATIVE_PLUS:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.RELATIVE_VELOCITY;
                    break;
                case Target.RELATIVE_MINUS:
                    direction = Vector3d.back;
                    reference = AttitudeReference.RELATIVE_VELOCITY;
                    break;
                case Target.TARGET_PLUS:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.TARGET;
                    break;
                case Target.TARGET_MINUS:
                    direction = Vector3d.back;
                    reference = AttitudeReference.TARGET;
                    break;
                case Target.PARALLEL_PLUS:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.TARGET_ORIENTATION;
                    break;
                case Target.PARALLEL_MINUS:
                    direction = Vector3d.back;
                    reference = AttitudeReference.TARGET_ORIENTATION;
                    break;
                case Target.ADVANCED:
                    direction = Vector6.directions[advDirection];
                    reference = advReference;
                    break;
                case Target.SURFACE_PROGRADE:
                    attitude = Quaternion.AngleAxis(-(float) srfVelRol, Vector3.forward) *
                        Quaternion.AngleAxis(-(float)srfVelPit, Vector3.right) *
                        Quaternion.AngleAxis((float)srfVelYaw, Vector3.up);
                    reference = AttitudeReference.SURFACE_VELOCITY;
                    break;
                case Target.SURFACE_RETROGRADE:
                    attitude = Quaternion.AngleAxis((float)srfVelRol + 180, Vector3.forward) *
                        Quaternion.AngleAxis(-(float)srfVelPit + 180, Vector3.right) *
                        Quaternion.AngleAxis((float)srfVelYaw, Vector3.up);
                    reference = AttitudeReference.SURFACE_VELOCITY;
                    break;
                case Target.HORIZONTAL_PLUS:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.SURFACE_HORIZONTAL;
                    break;
                case Target.HORIZONTAL_MINUS:
                    direction = Vector3d.back;
                    reference = AttitudeReference.SURFACE_HORIZONTAL;
                    break;
                case Target.VERTICAL_PLUS:
                    direction = Vector3d.up;
                    reference = AttitudeReference.SURFACE_NORTH;
                    break;
                default:
                    return;
            }

            if (forceRol && direction != Vector3d.zero)
            {
                attitude = Quaternion.LookRotation(direction, Vector3d.up) * Quaternion.AngleAxis(-(float)rol, Vector3d.forward);
                direction = Vector3d.zero;
            }

            if (direction != Vector3d.zero)
                core.attitude.attitudeTo(direction, reference, this);
            else
                core.attitude.attitudeTo(attitude, reference, this);

            core.attitude.pid.Reset();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(180), GUILayout.Height(100) };
        }

        public override string GetName()
        {
            return "Smart A.S.S.";
        }
    }
}
