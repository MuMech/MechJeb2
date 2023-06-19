using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleAscentSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentSettingsMenu(MechJebCore core) : base(core)
        {
            hidden = true;
        }

        private MechJebModuleAscentSettings      _ascentSettings => Core.AscentSettings;
        private MechJebModuleAscentBaseAutopilot _autopilot      => Core.Ascent;

        private readonly string _climbString = $"{CachedLocalizer.Instance.MechJeb_Ascent_label22}: ";
        private readonly string _turnString  = $"{CachedLocalizer.Instance.MechJeb_Ascent_label23}: ";

        private void ShowAscentSettingsGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.AscentItems");
            GUILayout.BeginVertical(GUI.skin.box);

            Core.Thrust.LimitToPreventOverheatsInfoItem();
            Core.Thrust.LimitToMaxDynamicPressureInfoItem();
            Core.Thrust.LimitAccelerationInfoItem();
            if (_ascentSettings.AscentType != AscentType.PVG) Core.Thrust.LimitThrottleInfoItem();
            Core.Thrust.LimiterMinThrottleInfoItem();
            if (_ascentSettings.AscentType != AscentType.PVG) Core.Thrust.LimitElectricInfoItem();

            if (_ascentSettings.AscentType == AscentType.PVG)
            {
                Core.Thrust.limitThrottle           = false;
                Core.Thrust.limitToTerminalVelocity = false;
                Core.Thrust.electricThrottle        = false;
            }

            _ascentSettings.ForceRoll = GUILayout.Toggle(_ascentSettings.ForceRoll, CachedLocalizer.Instance.MechJeb_Ascent_checkbox2); //Force Roll
            if (_ascentSettings.ForceRoll)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GuiUtils.SimpleTextBox(_climbString, _ascentSettings.VerticalRoll, "º", 30); //climb
                GuiUtils.SimpleTextBox(_turnString, _ascentSettings.TurnRoll, "º", 30);      //turn
                GuiUtils.SimpleTextBox("Alt: ", _ascentSettings.RollAltitude, "m", 30);
                GUILayout.EndHorizontal();
            }

            if (_ascentSettings.AscentType != AscentType.PVG)
            {
                GUIStyle s = _ascentSettings.LimitingAoA ? GuiUtils.greenToggle : null;
                string sCurrentMaxAoA = $"º ({_autopilot.CurrentMaxAoA:F1}°)";
                GuiUtils.ToggledTextBox(ref _ascentSettings.LimitAoA, CachedLocalizer.Instance.MechJeb_Ascent_checkbox3, _ascentSettings.MaxAoA,
                    sCurrentMaxAoA, s,
                    30); //Limit AoA to

                if (_ascentSettings.LimitAoA)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    GUIStyle sl = _ascentSettings.LimitingAoA && VesselState.dynamicPressure < _ascentSettings.AOALimitFadeoutPressure
                        ? GuiUtils.greenLabel
                        : GuiUtils.skin.label;
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label24, _ascentSettings.AOALimitFadeoutPressure, "Pa", 50,
                        sl); //Dynamic Pressure Fadeout
                    GUILayout.EndHorizontal();
                }

                _ascentSettings.LimitQaEnabled = false; // this is only for PVG
            }

            if (_ascentSettings.AscentType == AscentType.CLASSIC)
            {
                // corrective steering only applies to Classic
                GUILayout.BeginHorizontal();
                _ascentSettings.CorrectiveSteering = GUILayout.Toggle(_ascentSettings.CorrectiveSteering,
                    CachedLocalizer.Instance.MechJeb_Ascent_checkbox4,
                    GuiUtils.ExpandWidth(false)); //Corrective steering
                if (_ascentSettings.CorrectiveSteering)
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label25, _ascentSettings.CorrectiveSteeringGain, width: 40,
                        horizontalFraming: false); // Gain
                GUILayout.EndHorizontal();
            }

            _ascentSettings.Autostage = GUILayout.Toggle(_ascentSettings.Autostage, CachedLocalizer.Instance.MechJeb_Ascent_checkbox5); //Autostage
            if (_ascentSettings.Autostage) Core.Staging.AutostageSettingsInfoItem();

            _ascentSettings.AutodeploySolarPanels =
                GUILayout.Toggle(_ascentSettings.AutodeploySolarPanels, CachedLocalizer.Instance.MechJeb_Ascent_checkbox6); //Auto-deploy solar panels
            _ascentSettings.AutoDeployAntennas =
                GUILayout.Toggle(_ascentSettings.AutoDeployAntennas, CachedLocalizer.Instance.MechJeb_Ascent_checkbox7); //Auto-deploy antennas

            GUILayout.BeginHorizontal();
            Core.Node.autowarp = GUILayout.Toggle(Core.Node.autowarp, CachedLocalizer.Instance.MechJeb_Ascent_checkbox8); //Auto-warp
            if (_ascentSettings.AscentType != AscentType.PVG)
            {
                _ascentSettings.SkipCircularization =
                    GUILayout.Toggle(_ascentSettings.SkipCircularization, CachedLocalizer.Instance.MechJeb_Ascent_checkbox9); //Skip Circularization
            }
            else
            {
                // skipCircularization is always true for Optimizer
                _ascentSettings.SkipCircularization = true;
            }

            GUILayout.EndHorizontal();

            if (_ascentSettings.AscentType == AscentType.PVG)
                Core.Settings.rssMode = GUILayout.Toggle(Core.Settings.rssMode, "Module disabling does not kill throttle");

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            ShowAscentSettingsGUIElements();
            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(275), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return "Ascent Settings";
        }

        public override string IconName()
        {
            return "Ascent Settings";
        }
    }
}
