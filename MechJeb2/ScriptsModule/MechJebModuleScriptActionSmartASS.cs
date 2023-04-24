using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionSmartASS : MechJebModuleScriptAction
    {
        public static    string                NAME = "SmartASS";
        private readonly MechJebModuleSmartASS smartAss;

        private static readonly MechJebModuleSmartASS.Mode[] Target2Mode  = MechJebModuleSmartASS.Target2Mode;
        private static readonly bool[]                       TargetIsMode = MechJebModuleSmartASS.TargetIsMode;
        private static readonly string[]                     ModeTexts    = MechJebModuleSmartASS.ScriptModeTexts;
        private static readonly string[]                     TargetTexts  = MechJebModuleSmartASS.ScriptTargetTexts;

        // Target/Mode -> Mode index
        private readonly int[] targetModeSelectorIdxs = new int[TargetTexts.Length];

        private readonly int[] modeModeSelectorIdxs = new int[ModeTexts.Length];

        // Mode index -> Mode/Target
        private readonly List<bool>                         isTargetMode = new List<bool>();
        private readonly List<MechJebModuleSmartASS.Target> targetModes  = new List<MechJebModuleSmartASS.Target>();
        private readonly List<MechJebModuleSmartASS.Mode>   modeModes    = new List<MechJebModuleSmartASS.Mode>();
        private readonly List<string>                       modeStrings  = new List<string>();

        // Target -> Target index
        private readonly int[] targetSelectorIdxs = new int[TargetTexts.Length];

        // Mode -> Target index -> Target
        private readonly List<MechJebModuleSmartASS.Target>[] targets       = new List<MechJebModuleSmartASS.Target>[ModeTexts.Length];
        private readonly List<string>[]                       targetStrings = new List<string>[ModeTexts.Length];

        private static readonly string[] ReferenceTexts = Enum.GetNames(typeof(AttitudeReference));
        private static readonly string[] DirectionTexts = Enum.GetNames(typeof(Vector6.Direction));

        private MechJebModuleSmartASS.Target target;

        [Persistent(pass = (int)Pass.Type)]
        private string targetName;

        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble srfHdg = new EditableDouble(90);

        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble srfPit = new EditableDouble(90);

        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble srfRol = new EditableDouble(0);

        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble srfVelYaw = new EditableDouble(0);

        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble srfVelPit = new EditableDouble(0);

        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble srfVelRol = new EditableDouble(0);

        [Persistent(pass = (int)Pass.Type)]
        public EditableDouble rol = new EditableDouble(0);

        [Persistent(pass = (int)Pass.Type)]
        public AttitudeReference advReference = AttitudeReference.INERTIAL;

        [Persistent(pass = (int)Pass.Type)]
        public Vector6.Direction advDirection = Vector6.Direction.FORWARD;

        [Persistent(pass = (int)Pass.Type)]
        private bool forceRol;

        [Persistent(pass = (int)Pass.Type)]
        private bool forcePitch = true;

        [Persistent(pass = (int)Pass.Type)]
        private bool forceYaw = true;

        public MechJebModuleScriptActionSmartASS(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
            smartAss = core.GetComputerModule<MechJebModuleSmartASS>();
            int modeIdx = 0;
            foreach (MechJebModuleSmartASS.Target target in Enum.GetValues(typeof(MechJebModuleSmartASS.Target)))
            {
                if (TargetIsMode[(int)target] && target != MechJebModuleSmartASS.Target.AUTO)
                {
                    isTargetMode.Add(true);
                    modeModes.Add(0);
                    targetModes.Add(target);
                    modeStrings.Add(TargetTexts[(int)target]);
                    targetModeSelectorIdxs[(int)target] = modeIdx++;
                }
            }

            foreach (MechJebModuleSmartASS.Mode mode in Enum.GetValues(typeof(MechJebModuleSmartASS.Mode)))
            {
                if (mode != MechJebModuleSmartASS.Mode.AUTO)
                {
                    modeModes.Add(mode);
                    isTargetMode.Add(false);
                    targetModes.Add(0);
                    modeStrings.Add(ModeTexts[(int)mode]);
                    modeModeSelectorIdxs[(int)mode] = modeIdx++;
                    targets[(int)mode]              = new List<MechJebModuleSmartASS.Target>();
                    targetStrings[(int)mode]        = new List<string>();
                }
            }

            foreach (MechJebModuleSmartASS.Target target in Enum.GetValues(typeof(MechJebModuleSmartASS.Target)))
            {
                if (!TargetIsMode[(int)target])
                {
                    MechJebModuleSmartASS.Mode mode = Target2Mode[(int)target];
                    targetSelectorIdxs[(int)target] = targets[(int)mode].Count;
                    targets[(int)mode].Add(target);
                    targetStrings[(int)mode].Add(TargetTexts[(int)target]);
                }
            }
        }

        public override void activateAction()
        {
            base.activateAction();
            smartAss.mode         = Target2Mode[(int)target];
            smartAss.target       = target;
            smartAss.srfHdg       = srfHdg;
            smartAss.srfPit       = srfPit;
            smartAss.srfRol       = srfRol;
            smartAss.srfVelYaw    = srfVelYaw;
            smartAss.srfVelPit    = srfVelPit;
            smartAss.srfVelRol    = srfVelRol;
            smartAss.rol          = rol;
            smartAss.advReference = advReference;
            smartAss.advDirection = advDirection;
            smartAss.forceRol     = forceRol;
            smartAss.forcePitch   = forcePitch;
            smartAss.forceYaw     = forceYaw;
            smartAss.Engage();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void postLoad(ConfigNode node)
        {
            foreach (MechJebModuleSmartASS.Target target in Enum.GetValues(typeof(MechJebModuleSmartASS.Target)))
            {
                if (TargetTexts[(int)target].CompareTo(targetName) == 0)
                {
                    this.target = target;
                    break;
                }
            }
        }

        public override void afterOnFixedUpdate()
        {
            if (!isStarted() || isExecuted())
            {
                return;
            }

            if (target == MechJebModuleSmartASS.Target.KILLROT)
            {
                if (core.vesselState.angularVelocity.magnitude < 0.002)
                {
                    endAction();
                }
            }
            else if (core.attitude.attitudeAngleFromTarget() < 2 && core.vesselState.angularVelocity.magnitude < 0.02)
            {
                endAction();
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            int modeSelectorIdx = TargetIsMode[(int)target]
                ? targetModeSelectorIdxs[(int)target]
                : modeModeSelectorIdxs[(int)Target2Mode[(int)target]];
            GUILayout.BeginHorizontal(GUILayout.Width(150));
            modeSelectorIdx = GuiUtils.ComboBox.Box(modeSelectorIdx, modeStrings.ToArray(), modeStrings);
            GUILayout.EndHorizontal();
            bool showForceRol = false;
            if (isTargetMode[modeSelectorIdx])
            {
                target       = targetModes[modeSelectorIdx];
                showForceRol = target == MechJebModuleSmartASS.Target.NODE;
            }
            else
            {
                MechJebModuleSmartASS.Mode mode = modeModes[modeSelectorIdx];
                List<string> targetStrings = this.targetStrings[(int)mode];
                if (targetStrings.Count == 1)
                {
                    target = targets[(int)mode][0];
                }
                else
                {
                    int targetSelectorIdx = Target2Mode[(int)target] == mode ? targetSelectorIdxs[(int)target] : 0;
                    GUILayout.BeginHorizontal(GUILayout.Width(160));
                    targetSelectorIdx = GuiUtils.ComboBox.Box(targetSelectorIdx, targetStrings.ToArray(), targetStrings);
                    GUILayout.EndHorizontal();
                    target = targets[(int)mode][targetSelectorIdx];
                }

                switch (mode)
                {
                    case MechJebModuleSmartASS.Mode.ORBITAL:
                    case MechJebModuleSmartASS.Mode.TARGET:
                        showForceRol = true;
                        break;
                    case MechJebModuleSmartASS.Mode.ADVANCED:
                        GUILayout.BeginHorizontal(GUILayout.Width(220));
                        advReference = (AttitudeReference)GuiUtils.ComboBox.Box((int)advReference, ReferenceTexts, ReferenceTexts);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(GUILayout.Width(200));
                        advDirection = (Vector6.Direction)GuiUtils.ComboBox.Box((int)advDirection, DirectionTexts, DirectionTexts);
                        GUILayout.EndHorizontal();
                        showForceRol = true;
                        break;
                }

                switch (target)
                {
                    case MechJebModuleSmartASS.Target.SURFACE:
                        GUILayout.Label("", GUI.skin.label, GUILayout.ExpandWidth(true));
                        forceYaw    = GUILayout.Toggle(forceYaw, "Heading: ", GUILayout.ExpandWidth(false));
                        srfHdg.text = GUILayout.TextField(srfHdg.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
                        GUILayout.Label("°", GUILayout.ExpandWidth(true));
                        forcePitch  = GUILayout.Toggle(forcePitch, "Pitch: ", GUILayout.ExpandWidth(false));
                        srfPit.text = GUILayout.TextField(srfPit.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
                        GUILayout.Label("°", GUILayout.ExpandWidth(true));
                        forceRol    = GUILayout.Toggle(forceRol, "Roll: ", GUILayout.ExpandWidth(false));
                        srfRol.text = GUILayout.TextField(srfRol.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
                        GUILayout.Label("°", GUILayout.ExpandWidth(false));
                        break;
                    case MechJebModuleSmartASS.Target.SURFACE_PROGRADE:
                    case MechJebModuleSmartASS.Target.SURFACE_RETROGRADE:
                        GUILayout.Label("", GUI.skin.label, GUILayout.ExpandWidth(true));
                        forcePitch     = GUILayout.Toggle(forcePitch, "Pitch: ", GUILayout.ExpandWidth(false));
                        srfVelPit.text = GUILayout.TextField(srfVelPit.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
                        GUILayout.Label("°", GUILayout.ExpandWidth(true));
                        forceRol       = GUILayout.Toggle(forceRol, "Roll: ", GUILayout.ExpandWidth(false));
                        srfVelRol.text = GUILayout.TextField(srfVelRol.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
                        GUILayout.Label("°", GUILayout.ExpandWidth(true));
                        forceYaw       = GUILayout.Toggle(forceYaw, "Yaw: ", GUILayout.ExpandWidth(false));
                        srfVelYaw.text = GUILayout.TextField(srfVelYaw.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
                        GUILayout.Label("°", GUILayout.ExpandWidth(false));
                        break;
                }
            }

            GUILayout.Label("", GUILayout.ExpandWidth(true));
            if (showForceRol)
            {
                forceRol = GUILayout.Toggle(forceRol, "Force Roll: ", GUILayout.ExpandWidth(false));
                rol.text = GUILayout.TextField(rol.text, GUILayout.Width(37));
                GUILayout.Label("°", GUILayout.ExpandWidth(false));
            }

            targetName = TargetTexts[(int)target];
            postWindowGUI(windowID);
        }
    }
}
// vim: set noet:
