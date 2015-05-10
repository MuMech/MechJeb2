using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleSmartRcs : DisplayModule
    {

        public enum Target
        {
            OFF,
            ZERO_RVEL,
        }

        public static readonly string[] TargetTexts = { "OFF", "ZERO RVEL"};

        public Target target;

        private static GUIStyle btNormal, btActive, btAuto;

        [Persistent(pass = (int)Pass.Global)]
        public bool autoDisableSmartRCS = true;
        [GeneralInfoItem("Disable SmartRcs automatically", InfoItem.Category.Misc)]
        public void AutoDisableSmartRCS()
        {
            autoDisableSmartRCS = GUILayout.Toggle(autoDisableSmartRCS, "Disable SmartRcs automatically");
        }

        protected void TargetButton(Target bt)
        {
            if (GUILayout.Button(TargetTexts[(int)bt], (target == bt) ? btActive : btNormal, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                target = bt;
                Engage();
            }
        }

        public MechJebModuleSmartRcs(MechJebCore core) : base(core)
        {
        }

        protected override void WindowGUI(int windowID)
        {

            if (btNormal == null)
            {
                btNormal = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor = btNormal.focused.textColor = Color.white;
                btNormal.hover.textColor = btNormal.active.textColor = Color.yellow;
                btNormal.onNormal.textColor =
                    btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive = new GUIStyle(btNormal);
                btActive.active = btActive.onActive;
                btActive.normal = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover = btActive.onHover;

                btAuto = new GUIStyle(btNormal);
                btAuto.normal.textColor = Color.red;
                btAuto.onActive =
                    btAuto.onFocused = btAuto.onHover = btAuto.onNormal = btAuto.active = btAuto.focused = btAuto.hover = btAuto.normal;
            }

            // Disable if RCS is used by an other module
            if (core.rcs.enabled && core.rcs.users.Count(u => !this.Equals(u)) > 0)
            {
                if (autoDisableSmartRCS)
                {
                    target = Target.OFF;
                    if (core.rcs.users.Contains(this))
                        core.rcs.users.Remove(this); // so we don't suddenly turn on when the other autopilot finishes
                }
                GUILayout.Button("AUTO", btAuto, GUILayout.ExpandWidth(true));
            }
            else if (core.target.Target == null)
            {
                GUILayout.Label("Choose a target");
            }
            else
            {
                GUILayout.BeginVertical();

                TargetButton(Target.OFF);
                TargetButton(Target.ZERO_RVEL);
                //TargetButton(Target.HOLD_RPOS);

                GUILayout.EndVertical();
            }
            core.rcs.rcsThrottle = GUILayout.Toggle(core.rcs.rcsThrottle, " RCS throttle when engines are offline");
            core.rcs.rcsForRotation = GUILayout.Toggle(core.rcs.rcsForRotation, " Use RCS for rotation");
            base.WindowGUI(windowID);
        }


        public void Engage()
        {
            switch (target)
            {
                case Target.OFF:
                    core.rcs.users.Remove(this);
                    return;
                case Target.ZERO_RVEL:
                    core.rcs.users.Add(this);
                    core.rcs.SetTargetRelative(Vector3d.zero);
                    break;
            }
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(180), GUILayout.Height(100) };
        }
        
        public override string GetName()
        {
            return "SmartRcs";
        }

    }
}
