using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleThrustWindow : DisplayModule
    {
        [Persistent(pass = (int)Pass.Local)]
        public bool autostageSavedState;

        public MechJebModuleThrustWindow(MechJebCore core) : base(core) { }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (autostageSavedState && !core.Staging.users.Contains(this))
            {
                core.Staging.users.Add(this);
            }
        }

        [GeneralInfoItem("#MechJeb_AutostageOnce", InfoItem.Category.Misc)] //Autostage Once
        public void AutostageOnceItem()
        {
            if (core.Staging.enabled)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label1",
                    core.Staging.autostagingOnce
                        ? Localizer.Format("#MechJeb_Utilities_label1_1")
                        : " ")); //"Autostaging"<<1>>"Active" -------<<1>>" once ":""
            }

            if (!core.Staging.enabled && GUILayout.Button(Localizer.Format("#MechJeb_Utilities_button1"))) //"Autostage once"
            {
                core.Staging.AutostageOnce(this);
            }
        }

        [GeneralInfoItem("#MechJeb_Autostage", InfoItem.Category.Misc)] //Autostage
        public void Autostage()
        {
            bool oldAutostage = core.Staging.users.Contains(this);
            bool newAutostage = GUILayout.Toggle(oldAutostage, Localizer.Format("#MechJeb_Utilities_checkbox1")); //"Autostage"
            if (newAutostage && !oldAutostage) core.Staging.users.Add(this);
            if (!newAutostage && oldAutostage) core.Staging.users.Remove(this);
            autostageSavedState = newAutostage;
        }

        // UI stuff
        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            //core.thrust.LimitToTerminalVelocityInfoItem();
            core.Thrust.LimitToMaxDynamicPressureInfoItem();
            core.Thrust.LimitToPreventOverheatsInfoItem();
            core.Thrust.LimitAccelerationInfoItem();
            core.Thrust.LimitThrottleInfoItem();
            core.Thrust.LimiterMinThrottleInfoItem();
            core.Thrust.LimitElectricInfoItem();
            core.Thrust.LimitToPreventFlameoutInfoItem();
            if (VesselState.isLoadedRealFuels)
            {
                // does nothing in stock, so we suppress displaying it if RF is not loaded
                core.Thrust.LimitToPreventUnstableIgnitionInfoItem();
                core.Thrust.AutoRCsUllageInfoItem();
            }

            core.Thrust.smoothThrottle =
                GUILayout.Toggle(core.Thrust.smoothThrottle, Localizer.Format("#MechJeb_Utilities_checkbox2")); //"Smooth throttle"
            core.Thrust.manageIntakes =
                GUILayout.Toggle(core.Thrust.manageIntakes, Localizer.Format("#MechJeb_Utilities_checkbox3")); //"Manage air intakes"
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            try
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label2")); //"Jet safety margin"
                core.Thrust.flameoutSafetyPct.text = GUILayout.TextField(core.Thrust.flameoutSafetyPct.text, 5);
                GUILayout.Label("%");
            }
            finally
            {
                GUILayout.EndHorizontal();
            }

            core.Thrust.DifferentialThrottle();

            if (core.Thrust.differentialThrottle && vessel.LiftedOff())
            {
                switch (core.Thrust.differentialThrottleSuccess)
                {
                    case MechJebModuleThrustController.DifferentialThrottleStatus.MoreEnginesRequired:
                        GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label3"),
                            GuiUtils.yellowLabel); //"Differential throttle failed\nMore engines required"
                        break;
                    case MechJebModuleThrustController.DifferentialThrottleStatus.AllEnginesOff:
                        GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label4"),
                            GuiUtils.yellowLabel); //"Differential throttle failed\nNo active engine"
                        break;
                    case MechJebModuleThrustController.DifferentialThrottleStatus.SolverFailed:
                        GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label5"),
                            GuiUtils.yellowLabel); //"Differential throttle failed\nCannot find solution"
                        break;
                    case MechJebModuleThrustController.DifferentialThrottleStatus.Success:
                        break;
                }
            }

            core.Solarpanel.SolarPanelDeployButton();
            core.AntennaControl.AntennaDeployButton();

            Autostage();

            if (!core.Staging.enabled && GUILayout.Button(Localizer.Format("#MechJeb_Utilities_button1")))
                core.Staging.AutostageOnce(this); //"Autostage once"

            if (core.Staging.enabled) core.Staging.AutostageSettingsInfoItem();

            if (core.Staging.enabled) GUILayout.Label(core.Staging.AutostageStatus());

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(250), GUILayout.Height(30) };
        }

        public override bool isActive()
        {
            return core.Thrust.limiter != MechJebModuleThrustController.LimitMode.None;
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_Utilities_title"); //"Utilities"
        }

        public override string IconName()
        {
            return "Utilities";
        }
    }
}
