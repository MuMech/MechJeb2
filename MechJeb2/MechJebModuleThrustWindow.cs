using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
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

        // UI stuff
        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            //core.thrust.LimitToTerminalVelocityInfoItem();
            Core.Thrust.LimitToMaxDynamicPressureInfoItem();
            Core.Thrust.LimitToPreventOverheatsInfoItem();
            Core.Thrust.LimitAccelerationInfoItem();
            Core.Thrust.LimitThrottleInfoItem();
            Core.Thrust.LimiterMinThrottleInfoItem();
            Core.Thrust.LimitElectricInfoItem();
            Core.Thrust.LimitToPreventFlameoutInfoItem();
            if (VesselState.isLoadedRealFuels)
            {
                // does nothing in stock, so we suppress displaying it if RF is not loaded
                Core.Thrust.LimitToPreventUnstableIgnitionInfoItem();
                Core.Thrust.AutoRCsUllageInfoItem();
            }

            Core.Thrust.SmoothThrottle =
                GUILayout.Toggle(Core.Thrust.SmoothThrottle, Localizer.Format("#MechJeb_Utilities_checkbox2")); //"Smooth throttle"
            Core.Thrust.ManageIntakes =
                GUILayout.Toggle(Core.Thrust.ManageIntakes, Localizer.Format("#MechJeb_Utilities_checkbox3")); //"Manage air intakes"
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            try
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label2")); //"Jet safety margin"
                Core.Thrust.FlameoutSafetyPct.text = GUILayout.TextField(Core.Thrust.FlameoutSafetyPct.text, 5);
                GUILayout.Label("%");
            }
            finally
            {
                GUILayout.EndHorizontal();
            }

            Core.Thrust.DifferentialThrottleMenu();

            if (Core.Thrust.DifferentialThrottle && Vessel.LiftedOff())
            {
                switch (Core.Thrust.DifferentialThrottleSuccess)
                {
                    case MechJebModuleThrustController.DifferentialThrottleStatus.MORE_ENGINES_REQUIRED:
                        GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label3"),
                            GuiUtils.yellowLabel); //"Differential throttle failed\nMore engines required"
                        break;
                    case MechJebModuleThrustController.DifferentialThrottleStatus.ALL_ENGINES_OFF:
                        GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label4"),
                            GuiUtils.yellowLabel); //"Differential throttle failed\nNo active engine"
                        break;
                    case MechJebModuleThrustController.DifferentialThrottleStatus.SOLVER_FAILED:
                        GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label5"),
                            GuiUtils.yellowLabel); //"Differential throttle failed\nCannot find solution"
                        break;
                    case MechJebModuleThrustController.DifferentialThrottleStatus.SUCCESS:
                        break;
                }
            }

            Core.Solarpanel.SolarPanelDeployButton();
            Core.AntennaControl.AntennaDeployButton();

            Autostage();

            if (!Core.Staging.Enabled && GUILayout.Button(Localizer.Format("#MechJeb_Utilities_button1")))
                Core.Staging.AutostageOnce(this); //"Autostage once"

            if (Core.Staging.Enabled) Core.Staging.AutostageSettingsInfoItem();

            if (Core.Staging.Enabled) GUILayout.Label(Core.Staging.AutostageStatus());

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(250), GUILayout.Height(30) };

        public override bool isActive() => Core.Thrust.Limiter != MechJebModuleThrustController.LimitMode.NONE;

        public override string GetName() => Localizer.Format("#MechJeb_Utilities_title"); //"Utilities"

        public override string IconName() => "Utilities";
    }
}
