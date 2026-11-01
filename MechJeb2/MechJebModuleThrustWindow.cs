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
        public void AutostageOnceItem() { }

        [GeneralInfoItem("#MechJeb_Autostage", InfoItem.Category.Misc)] //Autostage
        public void Autostage() { }

        public override bool IsActive() => Core.Thrust.Limiter != MechJebModuleThrustController.LimitMode.NONE;

        public override string GetName() => Localizer.Format("#MechJeb_Utilities_title"); //"Utilities"

        public override string IconName() => "Utilities";
    }
}
