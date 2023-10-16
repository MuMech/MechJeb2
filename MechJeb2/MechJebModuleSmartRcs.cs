using System.Linq;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    internal class MechJebModuleSmartRcs : DisplayModule
    {
        public enum Target
        {
            OFF,
            ZERO_RVEL
        }

        public static readonly string[]
            TargetTexts = { Localizer.Format("#MechJeb_SmartRcs_button1"), Localizer.Format("#MechJeb_SmartRcs_button2") }; //"OFF", "ZERO RVEL"

        public Target target;

        private static GUIStyle btNormal, btActive, btAuto;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool autoDisableSmartRCS = true;

        [GeneralInfoItem("#MechJeb_DisableSmartRcsAutomatically", InfoItem.Category.Misc)] //Disable SmartRcs automatically
        public void AutoDisableSmartRCS() =>
            autoDisableSmartRCS =
                GUILayout.Toggle(autoDisableSmartRCS, Localizer.Format("#MechJeb_SmartRcs_checkbox1 ")); //"Disable SmartRcs automatically"

        protected void TargetButton(Target bt)
        {
            if (GUILayout.Button(TargetTexts[(int)bt], target == bt ? btActive : btNormal, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
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
                btNormal                  = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor = btNormal.focused.textColor = Color.white;
                btNormal.hover.textColor  = btNormal.active.textColor  = Color.yellow;
                btNormal.onNormal.textColor =
                    btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive           = new GUIStyle(btNormal);
                btActive.active    = btActive.onActive;
                btActive.normal    = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover     = btActive.onHover;

                btAuto                  = new GUIStyle(btNormal);
                btAuto.normal.textColor = Color.red;
                btAuto.onActive =
                    btAuto.onFocused = btAuto.onHover = btAuto.onNormal = btAuto.active = btAuto.focused = btAuto.hover = btAuto.normal;
            }

            // Disable if RCS is used by an other module
            if (Core.RCS.Enabled && Core.RCS.Users.Count(u => !Equals(u)) > 0)
            {
                if (autoDisableSmartRCS)
                {
                    target = Target.OFF;
                    if (Core.RCS.Users.Contains(this))
                        Core.RCS.Users.Remove(this); // so we don't suddenly turn on when the other autopilot finishes
                }

                GUILayout.Button(Localizer.Format("#MechJeb_SmartRcs_button3"), btAuto, GUILayout.ExpandWidth(true)); //"AUTO"
            }
            else if (Core.Target.Target == null)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_SmartRcs_label1")); //"Choose a target"
            }
            else
            {
                GUILayout.BeginVertical();

                TargetButton(Target.OFF);
                TargetButton(Target.ZERO_RVEL);
                //TargetButton(Target.HOLD_RPOS);

                GUILayout.EndVertical();
            }

            Core.RCS.rcsThrottle =
                GUILayout.Toggle(Core.RCS.rcsThrottle, Localizer.Format("#MechJeb_SmartRcs_checkbox2")); //" RCS throttle when engines are offline"
            Core.RCS.rcsForRotation =
                GUILayout.Toggle(Core.RCS.rcsForRotation, Localizer.Format("#MechJeb_SmartRcs_checkbox3")); // " Use RCS for rotation"
            base.WindowGUI(windowID);
        }

        public void Engage()
        {
            switch (target)
            {
                case Target.OFF:
                    Core.RCS.Users.Remove(this);
                    return;
                case Target.ZERO_RVEL:
                    Core.RCS.Users.Add(this);
                    Core.RCS.SetTargetRelative(Vector3d.zero);
                    break;
            }
        }

        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(180), GUILayout.Height(100) };

        public override string GetName() => Localizer.Format("#MechJeb_SmartRcs_title"); //"SmartRcs"

        public override string IconName() => "SmartRcs";
    }
}
