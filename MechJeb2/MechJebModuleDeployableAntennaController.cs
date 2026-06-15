extern alias JetBrainsAnnotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleDeployableAntennaController : MechJebModuleDeployableController
    {
        public MechJebModuleDeployableAntennaController(MechJebCore core) : base(core)
        {
        }

        [GeneralInfoItem("#MechJeb_ToggleAntennas", InfoItem.Category.Misc, showInEditor = false)] //Toggle antennas
        public void AntennaDeployButton()
        {
            AutoDeploy = GUILayout.Toggle(AutoDeploy, Localizer.Format("#MechJeb_Autodeployantennas")); //"Auto-deploy antennas"

            if (!GUILayout.Button(ButtonText)) return;

            if (ExtendingOrRetracting())
                return;

            if (!Extended)
                ExtendAll();
            else
                RetractAll();
        }

        protected override bool IsModules(ModuleDeployablePart p) => p is ModuleDeployableAntenna;

        protected override string GetButtonText(DeployablePartState deployablePartState)
        {
            return deployablePartState switch
            {
                DeployablePartState.EXTENDED => Localizer.Format("#MechJeb_AntennasEXTENDED"), //"Toggle antennas (currently extended)"
                DeployablePartState.RETRACTED => Localizer.Format("#MechJeb_AntennasRETRACTED"), //"Toggle antennas (currently retracted)"
                _ => Localizer.Format("#MechJeb_AntennasToggle")
            };
        }
    }
}
