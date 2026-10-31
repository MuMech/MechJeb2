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
        // True when there are stages to show. The FlightPlan itself is kept current by MechJebModuleAscentMenu.OnFixedUpdate.
        // Shared with the new UI.
        internal bool HasStages => Core.StageStats.VacStats.Count > 0;

        public override string GetName() => "PSG Settings";

        public override string IconName() => "PSGSettings";
    }
}
