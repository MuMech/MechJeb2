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
            AUTO
        }
        public static Mode[] Target2Mode = { Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.SURFACE, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.ADVANCED, Mode.AUTO };
        public static string[] ModeTexts = { "OBT", "SURF", "TGT", "ADV", "AUTO" };
        public static string[] TargetTexts = { "OFF", "KILL\nROT", "NODE", "SURF", "PRO\nGRAD", "RETR\nGRAD", "NML\n+", "NML\n-", "RAD\n+", "RAD\n-", "RVEL\n+", "RVEL\n-", "TGT\n+", "TGT\n-", "PAR\n+", "PAR\n-", "ADV", "AUTO" };

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
        public AttitudeReference advReference = AttitudeReference.INERTIAL;
        [Persistent(pass = (int)Pass.Local)]
        public Vector6.Direction advDirection = Vector6.Direction.FORWARD;

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

            if (core.attitude.enabled && !core.attitude.users.AmIUser(this))
            {
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
                        break;
                    case Mode.SURFACE:
                        GuiUtils.SimpleTextBox("HDG:", srfHdg);
                        GuiUtils.SimpleTextBox("PIT:", srfPit);

                        if (GUILayout.Button("EXECUTE", GUILayout.ExpandWidth(true)))
                        {
                            target = Target.SURFACE;
                            Engage();
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
                        }
                        else
                        {
                            GUILayout.Label("Please select a target");
                        }
                        break;
                    case Mode.ADVANCED:
                        GUILayout.Label("Reference:");
                        advReference = (AttitudeReference)GuiUtils.ArrowSelector((int)advReference, Enum.GetValues(typeof(AttitudeReference)).Length, advReference.ToString());

                        GUILayout.Label("Direction:");
                        advDirection = (Vector6.Direction)GuiUtils.ArrowSelector((int)advDirection, Enum.GetValues(typeof(Vector6.Direction)).Length, advDirection.ToString());

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
            if (target == Target.OFF)
            {
                core.attitude.users.Remove(this);
            }
            else
            {
                core.attitude.users.Add(this);
            }

            switch (target)
            {
                case Target.KILLROT:
                    core.attitude.attitudeKILLROT = true;
                    core.attitude.attitudeTo(Quaternion.LookRotation(part.vessel.GetTransform().up, -part.vessel.GetTransform().forward), AttitudeReference.INERTIAL, this);
                    break;
                case Target.NODE:
                    core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE, this);
                    break;
                case Target.SURFACE:
                    Quaternion r = Quaternion.AngleAxis((float)srfHdg, Vector3.up) * Quaternion.AngleAxis(-(float)srfPit, Vector3.right);
                    core.attitude.attitudeTo(r * Vector3d.forward, AttitudeReference.SURFACE_NORTH, this);
                    break;
                case Target.PROGRADE:
                    core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.ORBIT, this);
                    break;
                case Target.RETROGRADE:
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT, this);
                    break;
                case Target.NORMAL_PLUS:
                    core.attitude.attitudeTo(Vector3d.left, AttitudeReference.ORBIT, this);
                    break;
                case Target.NORMAL_MINUS:
                    core.attitude.attitudeTo(Vector3d.right, AttitudeReference.ORBIT, this);
                    break;
                case Target.RADIAL_PLUS:
                    core.attitude.attitudeTo(Vector3d.up, AttitudeReference.ORBIT, this);
                    break;
                case Target.RADIAL_MINUS:
                    core.attitude.attitudeTo(Vector3d.down, AttitudeReference.ORBIT, this);
                    break;
                case Target.RELATIVE_PLUS:
                    core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.RELATIVE_VELOCITY, this);
                    break;
                case Target.RELATIVE_MINUS:
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.RELATIVE_VELOCITY, this);
                    break;
                case Target.TARGET_PLUS:
                    core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.TARGET, this);
                    break;
                case Target.TARGET_MINUS:
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.TARGET, this);
                    break;
                case Target.PARALLEL_PLUS:
                    core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.TARGET_ORIENTATION, this);
                    break;
                case Target.PARALLEL_MINUS:
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.TARGET_ORIENTATION, this);
                    break;
                case Target.ADVANCED:
                    core.attitude.attitudeTo(Vector6.directions[advDirection], advReference, this);
                    break;
            }
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
