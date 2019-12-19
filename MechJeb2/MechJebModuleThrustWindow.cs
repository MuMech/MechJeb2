using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleThrustWindow : DisplayModule
    {

        [Persistent(pass = (int)Pass.Local)]
        public bool autostageSavedState = false;

        public MechJebModuleThrustWindow(MechJebCore core) : base(core) { }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (autostageSavedState && !core.staging.users.Contains(this))
            {
                core.staging.users.Add(this);
            }
        }

        [GeneralInfoItem("#MechJeb_AutostageOnce", InfoItem.Category.Misc)]//Autostage Once
        public void AutostageOnceItem()
        {
            if (core.staging.enabled)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label1", (core.staging.autostagingOnce ?  Localizer.Format("#MechJeb_Utilities_label1_1") : " ")));//"Autostaging"<<1>>"Active" -------<<1>>" once ":""
            }
            if (!core.staging.enabled && GUILayout.Button(Localizer.Format("#MechJeb_Utilities_button1")))//"Autostage once"
            {
                core.staging.AutostageOnce(this);
            }
        }

        [GeneralInfoItem("#MechJeb_Autostage", InfoItem.Category.Misc)]//Autostage
        public void Autostage()
        {
            bool oldAutostage = core.staging.users.Contains(this);
            bool newAutostage = GUILayout.Toggle(oldAutostage, Localizer.Format("#MechJeb_Utilities_checkbox1"));//"Autostage"
            if (newAutostage && !oldAutostage) core.staging.users.Add(this);
            if (!newAutostage && oldAutostage) core.staging.users.Remove(this);
            autostageSavedState = newAutostage;
        }

        // UI stuff
        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            //core.thrust.LimitToTerminalVelocityInfoItem();
            core.thrust.LimitToMaxDynamicPressureInfoItem();
            core.thrust.LimitToPreventOverheatsInfoItem();
            core.thrust.LimitAccelerationInfoItem();
            core.thrust.LimitThrottleInfoItem();
            core.thrust.LimiterMinThrottleInfoItem();
            core.thrust.LimitElectricInfoItem();
            core.thrust.LimitToPreventFlameoutInfoItem();
            if (VesselState.isLoadedRealFuels)
            {
                // does nothing in stock, so we suppress displaying it if RF is not loaded
                core.thrust.LimitToPreventUnstableIgnitionInfoItem();
                core.thrust.AutoRCsUllageInfoItem();
            }
            core.thrust.smoothThrottle = GUILayout.Toggle(core.thrust.smoothThrottle, Localizer.Format("#MechJeb_Utilities_checkbox2"));//"Smooth throttle"
            core.thrust.manageIntakes = GUILayout.Toggle(core.thrust.manageIntakes, Localizer.Format("#MechJeb_Utilities_checkbox3"));//"Manage air intakes"
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            try
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label2"));//"Jet safety margin"
                core.thrust.flameoutSafetyPct.text = GUILayout.TextField(core.thrust.flameoutSafetyPct.text, 5);
                GUILayout.Label("%");
            }
            finally
            {
                GUILayout.EndHorizontal();
            }

            core.thrust.DifferentialThrottle();

			if (core.thrust.differentialThrottle && vessel.LiftedOff())
			{
				switch (core.thrust.differentialThrottleSuccess)
				{
					case MechJebModuleThrustController.DifferentialThrottleStatus.MoreEnginesRequired:
						GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label3"), new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });//"Differential throttle failed\nMore engines required"
						break;
					case MechJebModuleThrustController.DifferentialThrottleStatus.AllEnginesOff:
						GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label4"), new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });//"Differential throttle failed\nNo active engine"
						break;
					case MechJebModuleThrustController.DifferentialThrottleStatus.SolverFailed:
						GUILayout.Label(Localizer.Format("#MechJeb_Utilities_label5"), new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });//"Differential throttle failed\nCannot find solution"
						break;
					case MechJebModuleThrustController.DifferentialThrottleStatus.Success:
						break;
				}
			}

            core.solarpanel.SolarPanelDeployButton();
            core.antennaControl.AntennaDeployButton();

            Autostage();

            if (!core.staging.enabled && GUILayout.Button(Localizer.Format("#MechJeb_Utilities_button1"))) core.staging.AutostageOnce(this);//"Autostage once"

            if (core.staging.enabled) core.staging.AutostageSettingsInfoItem();

            if (core.staging.enabled) GUILayout.Label(core.staging.AutostageStatus());

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }
        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[]{
                GUILayout.Width(250), GUILayout.Height(30)
            };
        }

        public override bool isActive()
        {
            return core.thrust.limiter != MechJebModuleThrustController.LimitMode.None;
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_Utilities_title");//"Utilities"
        }
    }
}
