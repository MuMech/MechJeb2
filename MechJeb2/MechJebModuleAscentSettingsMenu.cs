extern alias JetBrainsAnnotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class MechJebModuleAscentSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentSettingsMenu(MechJebCore core) : base(core)
        {
            Hidden = true;
        }

        private MechJebModuleAscentSettings      _ascentSettings => Core.AscentSettings;
        private MechJebModuleAscentBaseAutopilot _autopilot      => Core.Ascent;

        private readonly string _climbString = $"{CachedLocalizer.Instance.MechJebAscentLabel22}: ";
        private readonly string _turnString  = $"{CachedLocalizer.Instance.MechJebAscentLabel23}: ";

        private void ShowAscentSettingsGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.AscentItems");
            GUILayout.BeginVertical(GUI.skin.box);

            Core.Thrust.LimitToPreventOverheatsInfoItem();
            Core.Thrust.LimitToMaxDynamicPressureInfoItem();
            Core.Thrust.LimitAccelerationInfoItem();
            if (_ascentSettings.AscentType != AscentType.PSG) Core.Thrust.LimitThrottleInfoItem();
            Core.Thrust.LimiterMinThrottleInfoItem();
            if (_ascentSettings.AscentType != AscentType.PSG) Core.Thrust.LimitElectricInfoItem();

            if (_ascentSettings.AscentType == AscentType.PSG)
            {
                Core.Thrust.LimitThrottle           = false;
                Core.Thrust.LimitToTerminalVelocity = false;
                Core.Thrust.ElectricThrottle        = false;
            }

            _ascentSettings.ForceRoll = GUILayout.Toggle(_ascentSettings.ForceRoll, CachedLocalizer.Instance.MechJebAscentCheckbox2); //Force Roll
            if (_ascentSettings.ForceRoll)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GuiUtils.SimpleTextBox(_climbString, _ascentSettings.VerticalRoll, "º", 30); //climb
                GuiUtils.SimpleTextBox(_turnString, _ascentSettings.TurnRoll, "º", 30);      //turn
                GuiUtils.SimpleTextBox("Alt: ", _ascentSettings.RollAltitude, "m", 30);
                GUILayout.EndHorizontal();
            }

            if (_ascentSettings.AscentType != AscentType.PSG)
            {
                GUIStyle s = _ascentSettings.LimitingAoA ? GuiUtils.GreenToggle : null;
                string sCurrentMaxAoA = $"º ({_autopilot.CurrentMaxAoA:F1}°)";
                GuiUtils.ToggledTextBox(ref _ascentSettings.LimitAoA, CachedLocalizer.Instance.MechJebAscentCheckbox3, _ascentSettings.MaxAoA,
                    sCurrentMaxAoA, s,
                    30); //Limit AoA to

                if (_ascentSettings.LimitAoA)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    GUIStyle sl = _ascentSettings.LimitingAoA && VesselState.dynamicPressure < _ascentSettings.AOALimitFadeoutPressure
                        ? GuiUtils.GreenLabel
                        : GuiUtils.Skin.label;
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel24, _ascentSettings.AOALimitFadeoutPressure, "Pa", 50,
                        sl); //Dynamic Pressure Fadeout
                    GUILayout.EndHorizontal();
                }

                _ascentSettings.LimitQaEnabled = false; // this is only for PSG
            }

            if (_ascentSettings.AscentType == AscentType.CLASSIC)
            {
                // corrective steering only applies to Classic
                GUILayout.BeginHorizontal();
                _ascentSettings.CorrectiveSteering = GUILayout.Toggle(_ascentSettings.CorrectiveSteering,
                    CachedLocalizer.Instance.MechJebAscentCheckbox4,
                    GuiUtils.ExpandWidth(false)); //Corrective steering
                if (_ascentSettings.CorrectiveSteering)
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel25, _ascentSettings.CorrectiveSteeringGain, width: 40,
                        horizontalFraming: false); // Gain
                GUILayout.EndHorizontal();
            }

            _ascentSettings.Autostage = GUILayout.Toggle(_ascentSettings.Autostage, CachedLocalizer.Instance.MechJebAscentCheckbox5); //Autostage
            if (_ascentSettings.Autostage) Core.Staging.AutostageSettingsInfoItem();

            _ascentSettings.AutodeploySolarPanels =
                GUILayout.Toggle(_ascentSettings.AutodeploySolarPanels, CachedLocalizer.Instance.MechJebAscentCheckbox6); //Auto-deploy solar panels
            _ascentSettings.AutoDeployAntennas =
                GUILayout.Toggle(_ascentSettings.AutoDeployAntennas, CachedLocalizer.Instance.MechJebAscentCheckbox7); //Auto-deploy antennas

            GUILayout.BeginHorizontal();
            Core.Node.Autowarp = GUILayout.Toggle(Core.Node.Autowarp, CachedLocalizer.Instance.MechJebAscentCheckbox8); //Auto-warp
            if (_ascentSettings.AscentType != AscentType.PSG)
            {
                _ascentSettings.SkipCircularization =
                    GUILayout.Toggle(_ascentSettings.SkipCircularization, CachedLocalizer.Instance.MechJebAscentCheckbox9); //Skip Circularization
            }
            else
            {
                // skipCircularization is always true for Optimizer
                _ascentSettings.SkipCircularization = true;
            }

            GUILayout.EndHorizontal();

            if (_ascentSettings.AscentType == AscentType.PSG)
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

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(275), GUILayout.Height(30) };

        public override string GetName() => "Ascent Settings";

        public override string IconName() => "Ascent Settings";
    }
}
