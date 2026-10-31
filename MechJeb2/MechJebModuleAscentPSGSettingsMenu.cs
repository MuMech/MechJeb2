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

        public override string GetName() => "PSG Settings";

        public override string IconName() => "PSGSettings";
    }
}
