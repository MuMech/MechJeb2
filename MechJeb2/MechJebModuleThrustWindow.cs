using UnityEngine;

namespace MuMech
{
    public class MechJebModuleThrustWindow : DisplayModule
    {

        [Persistent(pass = (int)Pass.Local)]
        private bool autostageSavedState = false;

        public MechJebModuleThrustWindow(MechJebCore core) : base(core) { }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (autostageSavedState && !core.staging.users.Contains(this))
            {
                core.staging.users.Add(this);
            }
        }

        [GeneralInfoItem("Autostage Once", InfoItem.Category.Misc)]
        public void AutostageOnceItem()
        {
            if (core.staging.enabled)
            {
                GUILayout.Label("Autostaging" + (core.staging.autostagingOnce ? " once " : " ") + "Active");
            }
            if (!core.staging.enabled && GUILayout.Button("Autostage once"))
            {
                core.staging.AutostageOnce(this);
            }
        }

        [GeneralInfoItem("Autostage", InfoItem.Category.Misc)]
        public void Autostage()
        {
            bool oldAutostage = core.staging.users.Contains(this);
            bool newAutostage = GUILayout.Toggle(oldAutostage, "Autostage");
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
            core.thrust.smoothThrottle = GUILayout.Toggle(core.thrust.smoothThrottle, "Smooth throttle");
            core.thrust.manageIntakes = GUILayout.Toggle(core.thrust.manageIntakes, "Manage air intakes");
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            try
            {
                GUILayout.Label("Jet safety margin");
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
						GUILayout.Label("Differential throttle failed\nMore engines required", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });
						break;
					case MechJebModuleThrustController.DifferentialThrottleStatus.AllEnginesOff:
						GUILayout.Label("Differential throttle failed\nNo active engine", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });
						break;
					case MechJebModuleThrustController.DifferentialThrottleStatus.SolverFailed:
						GUILayout.Label("Differential throttle failed\nCannot find solution", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });
						break;
					case MechJebModuleThrustController.DifferentialThrottleStatus.Success:
						break;
				}
			}

            core.solarpanel.AutoDeploySolarPanelsInfoItem();

            Autostage();

            if (!core.staging.enabled && GUILayout.Button("Autostage once")) core.staging.AutostageOnce(this);

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
            return "Utilities";
        }
    }
}
