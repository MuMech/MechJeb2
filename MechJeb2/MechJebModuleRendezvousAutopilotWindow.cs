extern alias JetBrainsAnnotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRendezvousAutopilotWindow : DisplayModule
    {
        public MechJebModuleRendezvousAutopilotWindow(MechJebCore core) : base(core) { }

        protected override bool AutopilotEngaged => (_rendezvousAutopilot ??= Core.GetComputerModule<MechJebModuleRendezvousAutopilot>()) != null && _rendezvousAutopilot.Enabled;

        private MechJebModuleRendezvousAutopilot _rendezvousAutopilot;

        public override string GetName() => Localizer.Format("#MechJeb_RZauto_title"); //"Rendezvous Autopilot"

        public override string IconName() => "Rendezvous Autopilot";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
