using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;
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
        public static bool[] TargetIsMode = { true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false };
        public static string[] ModeTexts = { Localizer.Format("#MechJeb_SmartASS_button1"), Localizer.Format("#MechJeb_SmartASS_button2"), Localizer.Format("#MechJeb_SmartASS_button3"), Localizer.Format("#MechJeb_SmartASS_button4"),Localizer.Format("#MechJeb_SmartASS_button5")  };//"OBT""SURF""TGT""ADV""AUTO"
        public static string[] ScriptModeTexts = { Localizer.Format("#MechJeb_SmartASS_button6"), Localizer.Format("#MechJeb_SmartASS_button7"), Localizer.Format("#MechJeb_SmartASS_button8"), Localizer.Format("#MechJeb_SmartASS_button9"), Localizer.Format("#MechJeb_SmartASS_button10") };//"Orbit""Surface""Target""Advanced""Auto"
        public static string[] TargetTexts = { Localizer.Format("#MechJeb_SmartASS_button11"), Localizer.Format("#MechJeb_SmartASS_button12"), Localizer.Format("#MechJeb_SmartASS_button13"), Localizer.Format("#MechJeb_SmartASS_button14"), Localizer.Format("#MechJeb_SmartASS_button15"), Localizer.Format("#MechJeb_SmartASS_button16"), Localizer.Format("#MechJeb_SmartASS_button17"), Localizer.Format("#MechJeb_SmartASS_button18"), Localizer.Format("#MechJeb_SmartASS_button19"), Localizer.Format("#MechJeb_SmartASS_button20"), Localizer.Format("#MechJeb_SmartASS_button21"), Localizer.Format("#MechJeb_SmartASS_button22"), Localizer.Format("#MechJeb_SmartASS_button23"), Localizer.Format("#MechJeb_SmartASS_button24"), Localizer.Format("#MechJeb_SmartASS_button25"), Localizer.Format("#MechJeb_SmartASS_button26"), Localizer.Format("#MechJeb_SmartASS_button27"), Localizer.Format("#MechJeb_SmartASS_button28"), Localizer.Format("#MechJeb_SmartASS_button29"), Localizer.Format("#MechJeb_SmartASS_button30"), Localizer.Format("#MechJeb_SmartASS_button31"), Localizer.Format("#MechJeb_SmartASS_button32"), Localizer.Format("#MechJeb_SmartASS_button33") };//"OFF""KILL\nROT""NODE""SURF""PRO\nGRAD" "RETR\nGRAD""NML\n+""NML\n-""RAD\n+""RAD\n-""RVEL\n+""RVEL\n-""TGT\n+""TGT\n-""PAR\n+""PAR\n-""ADV""AUTO""SVEL\n+""SVEL\n-""HVEL\n+""HVEL\n-""UP"
        public static string[] ScriptTargetTexts = { Localizer.Format("#MechJeb_SmartASS_button34"), Localizer.Format("#MechJeb_SmartASS_button35"), Localizer.Format("#MechJeb_SmartASS_button36"), Localizer.Format("#MechJeb_SmartASS_button37"), Localizer.Format("#MechJeb_SmartASS_button38"), Localizer.Format("#MechJeb_SmartASS_button39"), Localizer.Format("#MechJeb_SmartASS_button40"), Localizer.Format("#MechJeb_SmartASS_button41"), Localizer.Format("#MechJeb_SmartASS_button42"), Localizer.Format("#MechJeb_SmartASS_button43"), Localizer.Format("#MechJeb_SmartASS_button44"), Localizer.Format("#MechJeb_SmartASS_button45"), Localizer.Format("#MechJeb_SmartASS_button46"), Localizer.Format("#MechJeb_SmartASS_button47"), Localizer.Format("#MechJeb_SmartASS_button48"), Localizer.Format("#MechJeb_SmartASS_button49"), Localizer.Format("#MechJeb_SmartASS_button50"), Localizer.Format("#MechJeb_SmartASS_button51"), Localizer.Format("#MechJeb_SmartASS_button52"), Localizer.Format("#MechJeb_SmartASS_button53"), Localizer.Format("#MechJeb_SmartASS_button54"), Localizer.Format("#MechJeb_SmartASS_button55"),Localizer.Format("#MechJeb_SmartASS_button56")  };//"Off""Kill Rotation""Node""Surface""Prograde""Retrograde""Normal+""Normal-""Radial+""Radial-""Relative Velocity+""Relative Velocity-""Target+""Target-""Parallel+""Parallel-""Advanced""Auto""Surface Velocity+""Surface Velocity-""Horizontal Velocity+""Horizontal Velocity-""Up"
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

        [Persistent(pass = (int)Pass.Local)]
        public Boolean forcePitch = true;

        [Persistent(pass = (int)Pass.Local)]
        public Boolean forceYaw = true;


        [Persistent(pass = (int)Pass.Global)]
        public bool autoDisableSmartASS = true;
        [GeneralInfoItem("#MechJeb_DisableSmartACSAutomatically", InfoItem.Category.Misc)]//Disable SmartACS automatically
        public void AutoDisableSmartASS()
        {
            autoDisableSmartASS = GUILayout.Toggle(autoDisableSmartASS, core.eduMode ? Localizer.Format("#MechJeb_SmartASS_checkbox1") :Localizer.Format("#MechJeb_SmartASS_checkbox2") );//"Disable SmartACS automatically":"Disable SmartASS automatically"
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
            bool _forceRol = forceRol;
            forceRol = GUILayout.Toggle(forceRol, Localizer.Format("#MechJeb_SmartASS_checkbox3"), GUILayout.ExpandWidth(false));//"Force Roll :"
            if (_forceRol != forceRol)
            {
                Engage();
            }
            rol.text = GUILayout.TextField(rol.text, GUILayout.Width(30));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            if (target != Target.OFF)
                Engage(false);
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
                GUILayout.Button(Localizer.Format("#MechJeb_SmartASS_button57"), btAuto, GUILayout.ExpandWidth(true));//"AUTO"
            }
            else
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                TargetButton(Target.OFF);
                TargetButton(Target.KILLROT);
                if (vessel.patchedConicsUnlocked())
                {
                    TargetButton(Target.NODE);
                }
                else
                {
                    GUILayout.Button("-", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(Localizer.Format("#MechJeb_SmartASS_label1"));//"Mode:"
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
                    	double val = (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); // change by 5 if the mod_key is held down, else by 1
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
                            bool changed = false;
                        	GUILayout.BeginHorizontal();
                            forceYaw = GUILayout.Toggle(forceYaw, "", GUILayout.ExpandWidth(false));
                            GuiUtils.SimpleTextBox("HDG", srfHdg, "°", 37);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                            	srfHdg -= val;
                                changed = true;
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfHdg += val;
                                changed = true;
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfHdg = 0;
                                changed = true;
                            }
                            if (GUILayout.Button("90", GUILayout.Width(35))) {
                                srfHdg = 90;
                                changed = true;
                            }
                            GUILayout.EndHorizontal();
                        	GUILayout.BeginHorizontal();
                            forcePitch = GUILayout.Toggle(forcePitch, "", GUILayout.ExpandWidth(false));
                            GuiUtils.SimpleTextBox("PIT", srfPit, "°", 37);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfPit -= val;
                                changed = true;
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfPit += val;
                                changed = true;
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfPit = 0;
                                changed = true;
                            }
                            if (GUILayout.Button("90", GUILayout.Width(35))) {
                                srfPit = 90;
                                changed = true;
                            }
                            GUILayout.EndHorizontal();
                        	GUILayout.BeginHorizontal();
                            forceRol = GUILayout.Toggle(forceRol, "", GUILayout.ExpandWidth(false));
                            GuiUtils.SimpleTextBox("ROL", srfRol, "°", 37);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfRol -= val;
                                changed = true;
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfRol += val;
                                changed = true;
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfRol = 0;
                                changed = true;
                            }
                            if (GUILayout.Button("180", GUILayout.Width(35))) {
                                srfRol = 180;
                                changed = true;
                            }
                            GUILayout.EndHorizontal();
                            if (GUILayout.Button(Localizer.Format("#MechJeb_SmartASS_button58"))){// "EXECUTE"
                                Engage();
                            }
                            if (changed)
                            {
                                Engage(false);
                            }
                            core.attitude.AxisControl(forcePitch, forceYaw, forceRol);
                        } else if (target == Target.SURFACE_PROGRADE || target == Target.SURFACE_RETROGRADE)
                        {
                            bool changed = false;
                            GUILayout.BeginHorizontal();
                            forceRol = GUILayout.Toggle(forceRol, "", GUILayout.ExpandWidth(false));
                            GuiUtils.SimpleTextBox("ROL", srfVelRol, "°", 37);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfVelRol -= val;
                                changed = true;
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfVelRol += val;
                                changed = true;
                            }
                            if (GUILayout.Button("CUR", GUILayout.ExpandWidth(false))) {
                                srfVelRol = -vesselState.vesselRoll.value;
                                changed = true;
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfVelRol = 0;
                                changed = true;
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            forcePitch = GUILayout.Toggle(forcePitch, "", GUILayout.ExpandWidth(false));
                            GuiUtils.SimpleTextBox("PIT", srfVelPit, "°", 37);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfVelPit -= val;
                                changed = true;
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfVelPit += val;
                                changed = true;
                            }
                            if (GUILayout.Button("CUR", GUILayout.ExpandWidth(false))) {
                                srfVelPit = vesselState.AoA.value;
                                changed = true;
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfVelPit = 0;
                                changed = true;
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            forceYaw = GUILayout.Toggle(forceYaw, "", GUILayout.ExpandWidth(false));
                            GuiUtils.SimpleTextBox("YAW", srfVelYaw, "°", 37);
                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) {
                                srfVelYaw -= val;
                                changed = true;
                            }
                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) {
                                srfVelYaw += val;
                                changed = true;
                            }
                            if (GUILayout.Button("CUR", GUILayout.ExpandWidth(false))) {
                                srfVelYaw = -vesselState.AoS.value;
                                changed = true;
                            }
                            if (GUILayout.Button("0", GUILayout.ExpandWidth(false))) {
                                srfVelYaw = 0;
                                changed = true;
                            }
                            GUILayout.EndHorizontal();
                            if (GUILayout.Button(Localizer.Format("#MechJeb_SmartASS_button58")))//"EXECUTE"
                            {
                                Engage();
                            }
                            if (changed)
                            {
                                Engage(false);
                            }
                            core.attitude.AxisControl(forcePitch, forceYaw, forceRol);
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
                            GUILayout.Label(Localizer.Format("#MechJeb_SmartASS_label2"));//"Please select a target"
                        }
                        break;
                    case Mode.ADVANCED:
                        GUILayout.Label(Localizer.Format("#MechJeb_SmartASS_label3"));//"Reference:"
                        advReference = (AttitudeReference)GuiUtils.ComboBox.Box((int)advReference, ReferenceTexts, this);

                        GUILayout.Label(Localizer.Format("#MechJeb_SmartASS_label4"));//"Direction:"
                        advDirection = (Vector6.Direction)GuiUtils.ComboBox.Box((int)advDirection, directionTexts, directionTexts);

                        ForceRoll();

                        if (GUILayout.Button(Localizer.Format("#MechJeb_SmartASS_button58"), btNormal, GUILayout.ExpandWidth(true)))//"EXECUTE"
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

        public void Engage(bool resetPID = true)
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
                    direction = Vector6.directions[(int)advDirection];
                    reference = advReference;
                    break;
                case Target.SURFACE_PROGRADE:
                    attitude = Quaternion.AngleAxis(-(float)srfVelRol, Vector3.forward) *
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

            if (resetPID) { core.attitude.Controller.Reset(); }
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(180), GUILayout.Height(100) };
        }

        public override string GetName()
        {
            return core.eduMode ?  Localizer.Format("#MechJeb_SmartACS_title") : Localizer.Format("#MechJeb_SmartASS_title");//"Smart A.C.S.":"Smart A.S.S."
        }
    }
}
