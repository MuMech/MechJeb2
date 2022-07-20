using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class MechJebModuleAscentSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentSettingsMenu(MechJebCore core) : base(core)
        {
        }
        
        private MechJebModuleAscentSettings      _ascentSettings => core.ascentSettings;
        private MechJebModuleAscentBaseAutopilot _autopilot      => core.ascent;
        
        private readonly string _climbString = $"{CachedLocalizer.Instance.MechJeb_Ascent_label22}: ";
        private readonly string _turnString  = $"{CachedLocalizer.Instance.MechJeb_Ascent_label23}: ";
        
        private void ShowAscentSettingsGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.AscentItems");
            GUILayout.BeginVertical(GUI.skin.box);

            core.thrust.LimitToPreventOverheatsInfoItem();
            core.thrust.LimitToMaxDynamicPressureInfoItem();
            core.thrust.LimitAccelerationInfoItem();
            if (_ascentSettings.AscentType != ascentType.PVG) core.thrust.LimitThrottleInfoItem();
            core.thrust.LimiterMinThrottleInfoItem();
            if (_ascentSettings.AscentType != ascentType.PVG) core.thrust.LimitElectricInfoItem();

            if (_ascentSettings.AscentType == ascentType.PVG)
            {
                // GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label21")) ;//FIXME: g-limiter is down for maintenance
                core.thrust.limitThrottle           = false;
                core.thrust.limitToTerminalVelocity = false;
                core.thrust.electricThrottle        = false;
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

            if (_ascentSettings.AscentType != ascentType.PVG)
            {
                GUIStyle s = _ascentSettings.LimitingAoA ? GuiUtils.greenToggle : null;
                string sCurrentMaxAoA = $"º ({_autopilot.CurrentMaxAoA:F1}°)";
                GuiUtils.ToggledTextBox(ref _ascentSettings.LimitAoA, CachedLocalizer.Instance.MechJeb_Ascent_checkbox3, _ascentSettings.MaxAoA, sCurrentMaxAoA, s,
                    30); //Limit AoA to

                if (_ascentSettings.LimitAoA)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    GUIStyle sl = _ascentSettings.LimitingAoA && vesselState.dynamicPressure < _ascentSettings.AOALimitFadeoutPressure
                        ? GuiUtils.greenLabel
                        : GuiUtils.skin.label;
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label24, _ascentSettings.AOALimitFadeoutPressure, "Pa", 50,
                        sl); //Dynamic Pressure Fadeout
                    GUILayout.EndHorizontal();
                }

                _ascentSettings.LimitQaEnabled = false; // this is only for PVG
            }

            if (_ascentSettings.AscentType == ascentType.CLASSIC)
            {
                // corrective steering only applies to Classic
                GUILayout.BeginHorizontal();
                _ascentSettings.CorrectiveSteering = GUILayout.Toggle(_ascentSettings.CorrectiveSteering, CachedLocalizer.Instance.MechJeb_Ascent_checkbox4,
                    GuiUtils.ExpandWidth(false)); //Corrective steering
                if (_ascentSettings.CorrectiveSteering)
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label25, _ascentSettings.CorrectiveSteeringGain, width: 40,
                        horizontalFraming: false); // Gain
                GUILayout.EndHorizontal();
            }

            _ascentSettings.Autostage = GUILayout.Toggle(_ascentSettings.Autostage, CachedLocalizer.Instance.MechJeb_Ascent_checkbox5); //Autostage
            if (_ascentSettings.Autostage) core.staging.AutostageSettingsInfoItem();

            _ascentSettings.AutodeploySolarPanels =
                GUILayout.Toggle(_ascentSettings.AutodeploySolarPanels, CachedLocalizer.Instance.MechJeb_Ascent_checkbox6); //Auto-deploy solar panels
            _ascentSettings.AutoDeployAntennas =
                GUILayout.Toggle(_ascentSettings.AutoDeployAntennas, CachedLocalizer.Instance.MechJeb_Ascent_checkbox7); //Auto-deploy antennas

            GUILayout.BeginHorizontal();
            core.node.autowarp = GUILayout.Toggle(core.node.autowarp, CachedLocalizer.Instance.MechJeb_Ascent_checkbox8); //Auto-warp
            if (_ascentSettings.AscentType != ascentType.PVG)
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
            
            if (_ascentSettings.AscentType == ascentType.PVG)
                core.settings.rssMode = GUILayout.Toggle(core.settings.rssMode, "Module disabling does not kill throttle");

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
