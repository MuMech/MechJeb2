extern alias JetBrainsAnnotations;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
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
        public void AutoDisableSmartRCS() { } //"Disable SmartRcs automatically"

        protected void TargetButton(Target bt)
        {
            if (GUILayout.Button(TargetTexts[(int)bt], target == bt ? btActive : btNormal, GuiUtils.LayoutExpandWidth, GUILayout.ExpandHeight(true)))
            {
                target = bt;
                Engage();
            }
        }

        public MechJebModuleSmartRcs(MechJebCore core) : base(core)
        {
        }

        // shared by the IMGUI window and the new UI: true when RCS is used by an other module (and then switches SmartRcs off if configured so)
        internal bool RcsUsedByOtherModule()
        {
            if (!Core.RCS.Enabled)
                return false;

            bool other = false;
            for (int i = 0; i < Core.RCS.Users.Count; i++)
            {
                if (!Equals(Core.RCS.Users[i]))
                {
                    other = true;
                    break;
                }
            }

            if (!other)
                return false;

            if (autoDisableSmartRCS)
            {
                target = Target.OFF;
                if (Core.RCS.Users.Contains(this))
                    Core.RCS.Users.Remove(this); // so we don't suddenly turn on when the other autopilot finishes
            }

            return true;
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

        public override string GetName() => Localizer.Format("#MechJeb_SmartRcs_title"); //"SmartRcs"

        public override string IconName() => "SmartRcs";
    }
}
