using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleAscentPVGSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentPVGSettingsMenu(MechJebCore core) : base(core)
        {
            hidden = true;
        }

        private MechJebModuleAscentSettings _ascentSettings => core.ascentSettings;

        private static GUIStyle _btNormal;
        private static GUIStyle _btActive;

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(300), GUILayout.Height(100) };
        }

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
            GUILayout.BeginVertical(GUI.skin.box);

            int topstage = -1;

            _ascentSettings.LastStage.val = Clamp(_ascentSettings.LastStage.val, 0, core.stageStats.vacStats.Length - 1);
            
            for (int i = _ascentSettings.LastStage; i < core.stageStats.vacStats.Length; i++)
            {
                FuelFlowSimulation.FuelStats stats = core.stageStats.vacStats[i];
                if (stats.DeltaV < _ascentSettings.MinDeltaV.val)
                    continue;

                if (topstage < 0)
                    topstage = i;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i,3} {stats.DeltaV:##,###0} m/s");
                if (_ascentSettings.UnguidedStages.Contains(i))
                    GUILayout.Label(" (unguided)");
                if (_ascentSettings.OptimizeStage == i)
                    GUILayout.Label(" (optimize)");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            // this should only run once -- maybe it should run in some hook like scene loading or something?
            // we need DV info though.
            if (_ascentSettings.OptimizeStageInternal < 0)
                _ascentSettings.OptimizeStageInternal.val = topstage;
      
            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox("Min ∆v: ", _ascentSettings.MinDeltaV, "m/s", 30);
            GuiUtils.SimpleTextBox("Last Stage: ", _ascentSettings.LastStage);
            GuiUtils.ToggledTextBox(ref _ascentSettings.OptimizeStageFlag, "Optimize Stage: ", _ascentSettings.OptimizeStageInternal);
            GuiUtils.ToggledTextBox(ref _ascentSettings.UnguidedStagesFlag, "Unguided Stages: ", _ascentSettings.UnguidedStagesInternal);
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical(GUI.skin.box);
            //_ascentSettings.ExtendIfRequired = GUILayout.Toggle(_ascentSettings.ExtendIfRequired, "Extend if Required");
            GuiUtils.ToggledTextBox(ref _ascentSettings.CoastStageFlag, "Coast Stage: ", _ascentSettings.CoastStageInternal);
            GUILayout.BeginHorizontal();
            _ascentSettings.CoastBeforeFlag = GUILayout.Toggle(_ascentSettings.CoastBeforeFlag, "Coast Before");
            _ascentSettings.CoastBeforeFlag = !GUILayout.Toggle(!_ascentSettings.CoastBeforeFlag, "Coast After");
            GUILayout.EndHorizontal();
            //GuiUtils.SimpleTextBox("Max Coast: ", _ascentSettings.MaxCoast, "s", width: 60);
            GuiUtils.ToggledTextBox(ref _ascentSettings.FixedCoast, "Fixed Coast Length:", _ascentSettings.FixedCoastLength, "s", width: 40);
            GuiUtils.SimpleTextBox("Ullage lead time: ", core.guidance.UllageLeadTime, "s", 60);
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GuiUtils.ToggledTextBox(ref _ascentSettings.SpinupStageFlag, "Spinup stage: ", _ascentSettings.SpinupStageInternal, width: 30);
            GuiUtils.SimpleTextBox("ω: ", _ascentSettings.SpinupAngularVelocity, "rpm", 30);
            GUILayout.EndHorizontal();
            GuiUtils.SimpleTextBox("Spinup lead time: ", _ascentSettings.SpinupLeadTime, "s", 60);
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label13, _ascentSettings.PitchStartVelocity, "m/s",
                40);                                                                                                       //Booster Pitch start:
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label14, _ascentSettings.PitchRate, "°/s", 40); //Booster Pitch rate:
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox("Q Trigger:", _ascentSettings.DynamicPressureTrigger, "kPa", 40);
            GuiUtils.ToggledTextBox(ref _ascentSettings.StagingTriggerFlag, "PVG After Stage:", _ascentSettings.StagingTrigger, width: 40);
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label17, _ascentSettings.LimitQa, "Pa-rad"); //Qα limit
            if (_ascentSettings.LimitQa < 1000 || _ascentSettings.LimitQa > 4000)
            {
                if (_ascentSettings.LimitQa < 0 || _ascentSettings.LimitQa > 10000)
                    GUILayout.Label("Qα limit has been clamped to between 0 and 10000 Pa-rad", GuiUtils.redLabel);
                else
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label20,
                        GuiUtils.yellowLabel); //Qα limit is recommended to be 1000 to 4000 Pa-rad
            }
            core.guidance.ShouldDrawTrajectory = GUILayout.Toggle(core.guidance.ShouldDrawTrajectory, "Draw Trajectory on Map");
            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override string GetName()
        {
            return "PVG Settings";
        }

        public override string IconName()
        {
            return "PVGSettings";
        }
    }
}
