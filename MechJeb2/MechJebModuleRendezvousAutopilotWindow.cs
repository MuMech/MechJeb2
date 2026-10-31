extern alias JetBrainsAnnotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRendezvousAutopilotWindow : DisplayModule
    {
        public MechJebModuleRendezvousAutopilotWindow(MechJebCore core) : base(core) { }

        public override string GetName() => Localizer.Format("#MechJeb_RZauto_title"); //"Rendezvous Autopilot"

        public override string IconName() => "Rendezvous Autopilot";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
