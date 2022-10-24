using UnityEngine;

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

            for (int i = _ascentSettings.LastStage; i < core.stageStats.vacStats.Length; i++)
            {
                FuelFlowSimulation.FuelStats stats = core.stageStats.vacStats[i];
                if (stats.DeltaV < _ascentSettings.MinDeltaV.val)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i,3} {stats.DeltaV:##,###0} m/s");
                if (_ascentSettings.UnguidedStages.val.Contains(i))
                    GUILayout.Label(" (unguided)");
                if (_ascentSettings.OptimizeStage == i)
                    GUILayout.Label(" (optimize)");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox("Optimize Stage: ", _ascentSettings.OptimizeStage);
            GuiUtils.SimpleTextBox("Unguided Stages: ", _ascentSettings.UnguidedStages);
            GuiUtils.SimpleTextBox("Last Stage: ", _ascentSettings.LastStage);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box);
            //_ascentSettings.ExtendIfRequired = GUILayout.Toggle(_ascentSettings.ExtendIfRequired, "Extend if Required");
            GuiUtils.SimpleTextBox("Coast Stage: ", _ascentSettings.CoastStage);
            /*
            GUILayout.BeginHorizontal();
            GUILayout.Label("Coast Timing: ");
            bool coastStart  = GUILayout.Toggle(_coastSetting == -1, "Start");
            bool coastMiddle = GUILayout.Toggle(_coastSetting == 0, "Middle");
            bool coastEnd    = GUILayout.Toggle(_coastSetting == 1, "End");
            if (coastStart && _coastSetting != -1)
                _coastSetting = -1;
            if (coastMiddle && _coastSetting != 0)
                _coastSetting = 0;
            if (coastEnd && _coastSetting != 1)
                _coastSetting = 1;
            GUILayout.EndHorizontal();
            */
            //GuiUtils.SimpleTextBox("Max Coast: ", _ascentSettings.MaxCoast, "s", width: 60);
            GuiUtils.ToggledTextBox(ref _ascentSettings.FixedCoast, "Fixed Coast Length:", _ascentSettings.FixedCoastLength, "s", width: 40);
            GuiUtils.SimpleTextBox("Min ∆v: ", _ascentSettings.MinDeltaV, "m/s", width: 30);
            GuiUtils.SimpleTextBox("Pre-stage time: ", _ascentSettings.PreStageTime, "s");
            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Spinup stage: ", _ascentSettings.SpinupStage, width: 30);
            GuiUtils.SimpleTextBox("ω: ", _ascentSettings.SpinupAngularVelocity, "rpm", width: 30);
            GUILayout.EndHorizontal();
            GuiUtils.SimpleTextBox("Spinup lead time: ", _ascentSettings.SpinupLeadTime, "s", width: 60);
            GuiUtils.SimpleTextBox("Ullage lead time: ", core.guidance.UllageLeadTime, "s", width: 60);
            core.guidance.ShouldDrawTrajectory = GUILayout.Toggle(core.guidance.ShouldDrawTrajectory, "Draw Trajectory on Map");
            
            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override string GetName()
        {
            return "PVG Settings Editor";
        }

        public override string IconName()
        {
            return "PVGStagingEditor";
        }
    }
}
