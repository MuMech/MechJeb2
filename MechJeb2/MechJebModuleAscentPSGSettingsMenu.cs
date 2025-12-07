extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleAscentPSGSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentPSGSettingsMenu(MechJebCore core) : base(core)
        {
            Hidden = true;
        }

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        private static GUIStyle _btNormal;
        private static GUIStyle _btActive;

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(100) };

        private void SetupButtonStyles()
        {
            if (_btNormal == null)
            {
                _btNormal                  = new GUIStyle(GUI.skin.button);
                _btNormal.normal.textColor = _btNormal.focused.textColor = Color.white;
                _btNormal.hover.textColor  = _btNormal.active.textColor  = Color.yellow;
                _btNormal.onNormal.textColor =
                    _btNormal.onFocused.textColor = _btNormal.onHover.textColor = _btNormal.onActive.textColor = Color.green;
                _btNormal.padding = new RectOffset(0, 0, 0, 0);

                _btActive           = new GUIStyle(_btNormal);
                _btActive.active    = _btActive.onActive;
                _btActive.normal    = _btActive.onNormal;
                _btActive.onFocused = _btActive.focused;
                _btActive.hover     = _btActive.onHover;
            }
        }

        protected override void WindowGUI(int windowID)
        {
            SetupButtonStyles();

            int topstage = -1;

            List<FuelStats> vacStats = Core.StageStats.VacStats;

            if (vacStats.Count > 0 && _ascentSettings.LastStage <= vacStats[vacStats.Count - 1].KSPStage)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                _ascentSettings.LastStage.Val = Clamp(_ascentSettings.LastStage.Val, 0, vacStats[vacStats.Count - 1].KSPStage);

                _ascentSettings.OptimizeStageFlag = false;

                foreach (FuelStats stats in Core.StageStats.VacStats)
                {
                    if (stats.KSPStage < _ascentSettings.LastStage)
                        continue;

                    if (stats.DeltaV < _ascentSettings.MinDeltaV.Val)
                        continue;

                    if (topstage < 0)
                        topstage = stats.KSPStage;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{stats.KSPStage,3} {stats.DeltaV:##,###0} m/s");
                    if (_ascentSettings.UnguidedStages.Contains(stats.KSPStage))
                        GUILayout.Label(" (unguided)");
                    if (_ascentSettings.FixedStages.Contains(stats.KSPStage))
                        GUILayout.Label(" (fixed)");
                    else
                        _ascentSettings.OptimizeStageFlag = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox("Min ∆v: ", _ascentSettings.MinDeltaV, "m/s", 30);
            GuiUtils.SimpleTextBox("Last Stage: ", _ascentSettings.LastStage);
            GuiUtils.ToggledTextBox(ref _ascentSettings.FixedStagesFlag, "Fixed Burn Stages: ", _ascentSettings.FixedStagesInternal);
            GuiUtils.ToggledTextBox(ref _ascentSettings.UnguidedStagesFlag, "Unguided Stages: ", _ascentSettings.UnguidedStagesInternal);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            //_ascentSettings.ExtendIfRequired = GUILayout.Toggle(_ascentSettings.ExtendIfRequired, "Extend if Required");
            GuiUtils.ToggledTextBox(ref _ascentSettings.CoastStageFlag, "Coast Stage: ", _ascentSettings.CoastStageInternal);
            GUILayout.BeginHorizontal();
            _ascentSettings.CoastBeforeFlag = GUILayout.Toggle(_ascentSettings.CoastBeforeFlag, "Coast Before");
            _ascentSettings.CoastBeforeFlag = !GUILayout.Toggle(!_ascentSettings.CoastBeforeFlag, "Coast After");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Min Coast:", _ascentSettings.MinCoast, "s", 40);
            GuiUtils.SimpleTextBox("Max Coast:", _ascentSettings.MaxCoast, "s", 40);
            GUILayout.EndHorizontal();
            GuiUtils.SimpleTextBox("Ullage lead time: ", Core.Guidance.UllageLeadTime, "s", 60);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GuiUtils.ToggledTextBox(ref _ascentSettings.SpinupStageFlag, "Spinup stage: ", _ascentSettings.SpinupStageInternal, width: 30);
            GuiUtils.SimpleTextBox("ω: ", _ascentSettings.SpinupAngularVelocity, "rpm", 30);
            GUILayout.EndHorizontal();
            GuiUtils.SimpleTextBox("Spinup lead time: ", _ascentSettings.SpinupLeadTime, "s", 60);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel13, _ascentSettings.PitchStartVelocity, "m/s",
                40);                                                                                                     //Booster Pitch start:
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel14, _ascentSettings.PitchRate, "°/s", 40); //Booster Pitch rate:
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox("Q Trigger:", _ascentSettings.DynamicPressureTrigger, "kPa", 40);
            GuiUtils.ToggledTextBox(ref _ascentSettings.StagingTriggerFlag, "PSG After Stage:", _ascentSettings.StagingTrigger, width: 40);
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel17, _ascentSettings.LimitQa, "Pa-rad"); //Qα limit
            if (_ascentSettings.LimitQa < 1000 || _ascentSettings.LimitQa > 4000)
            {
                if (_ascentSettings.LimitQa < 0 || _ascentSettings.LimitQa > 10000)
                    GUILayout.Label("Qα limit has been clamped to between 0 and 10000 Pa-rad", GuiUtils.RedLabel);
                else
                    GUILayout.Label(CachedLocalizer.Instance.MechJebAscentLabel20,
                        GuiUtils.YellowLabel); //Qα limit is recommended to be 1000 to 4000 Pa-rad
            }

            Core.Guidance.ShouldDrawTrajectory = GUILayout.Toggle(Core.Guidance.ShouldDrawTrajectory, "Draw Trajectory on Map");
            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override string GetName() => "PSG Settings";

        public override string IconName() => "PSGSettings";
    }
}
