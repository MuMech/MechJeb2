using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleThrustWindow : DisplayModule
    {
        public MechJebModuleThrustWindow(MechJebCore core) : base(core) { }

        // UI stuff
        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            //core.thrust.limitToTerminalVelocity = GUILayout.Toggle(core.thrust.limitToTerminalVelocity, "Limit to terminal velocity");
            core.thrust.LimitToTerminalVelocityInfoItem();
            core.thrust.LimitToPreventOverheatsInfoItem();
            core.thrust.LimitAccelerationInfoItem();
            core.thrust.LimitThrottleInfoItem();
            core.thrust.LimitToPreventFlameoutInfoItem();
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

            bool oldAutostage = core.staging.users.Contains(this);
            bool newAutostage = GUILayout.Toggle(oldAutostage, "Autostage");
            if (newAutostage && !oldAutostage) core.staging.users.Add(this);
            if (!newAutostage && oldAutostage) core.staging.users.Remove(this);
            
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

        public override string GetName()
        {
            return "Utilities";
        }
    }
}
