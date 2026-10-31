extern alias JetBrainsAnnotations;
using MechJebLibBindings.PSG;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentPSGSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentPSGSettingsMenu(MechJebCore core) : base(core) { }

        public override bool ShowInMenu => false;

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        private static GUIStyle _btNormal;
        private static GUIStyle _btActive;

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(300), GUILayout.Height(100) };

        private void SetupButtonStyles()
        {
            if (_btNormal == null)
            {
                _btNormal = new GUIStyle(GUI.skin.button);
                _btNormal.normal.textColor = _btNormal.focused.textColor = Color.white;
                _btNormal.hover.textColor = _btNormal.active.textColor = Color.yellow;
                _btNormal.onNormal.textColor =
                    _btNormal.onFocused.textColor = _btNormal.onHover.textColor = _btNormal.onActive.textColor = Color.green;
                _btNormal.padding = new RectOffset(0, 0, 0, 0);

                _btActive = new GUIStyle(_btNormal);
                _btActive.active = _btActive.onActive;
                _btActive.normal = _btActive.onNormal;
                _btActive.onFocused = _btActive.focused;
                _btActive.hover = _btActive.onHover;
            }
        }

        // True when there are stages to show. The FlightPlan itself is kept current by MechJebModuleAscentMenu.OnFixedUpdate.
        // Shared with the new UI.
        internal bool HasStages => Core.StageStats.VacStats.Count > 0;

        internal void DrawFlightPlanStages() => Core.PSGSimulation.FlightPlan?.DrawStageGUI();

        protected override void WindowGUI(int windowID)
        {
            SetupButtonStyles();

            if (HasStages)
                DrawFlightPlanStages();

            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox("Min ΔV: ", Core.PSGSimulation.MinDeltaV, "m/s", 30);
            GuiUtils.SimpleTextBox("Last Stage: ", _ascentSettings.LastStage);
            GuiUtils.ToggledTextBox(ref _ascentSettings.FixedStagesFlag, "Fixed Burn Stages: ", _ascentSettings.FixedStagesInternal);
            GuiUtils.ToggledTextBox(ref _ascentSettings.UnguidedStagesFlag, "Unguided Stages: ", _ascentSettings.UnguidedStagesInternal);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            //_ascentSettings.ExtendIfRequired = GUILayout.Toggle(_ascentSettings.ExtendIfRequired, "Extend if Required");
            GuiUtils.ToggledTextBox(ref _ascentSettings.CoastStageFlag, "Coast Stage: ", _ascentSettings.CoastStageInternal);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_ascentSettings.CoastLocation == -1, "Coast Before"))
                _ascentSettings.CoastLocation = -1;
            if (GUILayout.Toggle(_ascentSettings.CoastLocation == 0, "Coast During"))
                _ascentSettings.CoastLocation = 0;
            if (GUILayout.Toggle(_ascentSettings.CoastLocation == 1, "Coast After"))
                _ascentSettings.CoastLocation = 1;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Min Coast:", Core.PSGSimulation.MinCoast, "s", 40);
            GuiUtils.SimpleTextBox("Max Coast:", Core.PSGSimulation.MaxCoast, "s", 40);
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
            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Cd:", Core.PSGSimulation.Cd, "", 40);
            GuiUtils.SimpleTextBox("Aref:", Core.PSGSimulation.Aref, "m²", 40);
            GUILayout.EndHorizontal();
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel13, _ascentSettings.PitchStartHeight, "m",
                40); //Booster Pitch start:
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel14, _ascentSettings.PitchRate, "°/s", 40); //Booster Pitch rate:
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel17, Core.PSGSimulation.LimitQa, "Pa-rad"); //Qα limit
            if (Core.PSGSimulation.LimitQa < 1000 || Core.PSGSimulation.LimitQa > 4000)
            {
                if (Core.PSGSimulation.LimitQa < 0 || Core.PSGSimulation.LimitQa > 10000)
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
