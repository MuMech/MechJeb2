extern alias JetBrainsAnnotations;
using KSP.Localization;
using MechJebLibBindings;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleThrustWindow : DisplayModule
    {
        [Persistent(pass = (int)Pass.LOCAL)]
        public bool autostageSavedState;

        public MechJebModuleThrustWindow(MechJebCore core) : base(core) { }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (autostageSavedState && !Core.Staging.Users.Contains(this))
            {
                Core.Staging.Users.Add(this);
            }
        }

        [GeneralInfoItem("#MechJeb_AutostageOnce", InfoItem.Category.Misc)] //Autostage Once
        public void AutostageOnceItem()
        {
            if (Core.Staging.Enabled)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label1",
                    Core.Staging.AutostagingOnce
                        ? Localizer.Format("#MechJeb_Utilities_label1_1")
                        : " ")); //"Autostaging"<<1>>"Active" -------<<1>>" once ":""
            }

            if (!Core.Staging.Enabled && GUILayout.Button(Localizer.Format("#MechJeb_Utilities_button1"))) //"Autostage once"
            {
                Core.Staging.AutostageOnce(this);
            }
        }

        [GeneralInfoItem("#MechJeb_Autostage", InfoItem.Category.Misc)] //Autostage
        public void Autostage()
        {
            bool oldAutostage = Core.Staging.Users.Contains(this);
            bool newAutostage = GUILayout.Toggle(oldAutostage, Localizer.Format("#MechJeb_Utilities_checkbox1")); //"Autostage"
            if (newAutostage && !oldAutostage) Core.Staging.Users.Add(this);
            if (!newAutostage && oldAutostage) Core.Staging.Users.Remove(this);
            autostageSavedState = newAutostage;
        }

        public override bool IsActive() => Core.Thrust.Limiter != MechJebModuleThrustController.LimitMode.NONE;

        public override string GetName() => Localizer.Format("#MechJeb_Utilities_title"); //"Utilities"

        public override string IconName() => "Utilities";
    }
}
