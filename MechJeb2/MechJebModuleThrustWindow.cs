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

        // The saved state is the Autostage toggle: the window as a staging user, but not as the user of a pending "autostage once"
        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            if (HighLogic.LoadedSceneIsFlight)
                autostageSavedState = Core.Staging.Users.Contains(this) && !Core.Staging.IsAutostageOnceUser(this);

            base.OnSave(local, type, global);
        }

        [GeneralInfoItem("#MechJeb_AutostageOnce", InfoItem.Category.Misc)] //Autostage Once
        public void AutostageOnceItem() { }

        [GeneralInfoItem("#MechJeb_Autostage", InfoItem.Category.Misc)] //Autostage
        public void Autostage() { }

        /// <summary>
        ///     Active while one of its own settings acts: a limiter of the general throttle limits (not an autopilot's own, which
        ///     apply instead while it flies), the jet flameout limiter, or its Autostage switch.
        /// </summary>
        public override bool IsActive()
        {
            MechJebModuleThrustController.LimitMode limiter = Core.Thrust.Limiter;
            if (limiter == MechJebModuleThrustController.LimitMode.FLAMEOUT) return true;
            if (limiter != MechJebModuleThrustController.LimitMode.NONE && Core.Thrust.LimitsOverrideUser == null) return true;
            return Core.Staging.Enabled && Core.Staging.Users.Contains(this);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Utilities_title"); //"Utilities"

        public override string IconName() => "Utilities";
    }
}
