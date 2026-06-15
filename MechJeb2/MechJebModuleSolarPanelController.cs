extern alias JetBrainsAnnotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    //[UsedImplicitly]
    public class MechJebModuleSolarPanelController : MechJebModuleDeployableController
    {
        public MechJebModuleSolarPanelController(MechJebCore core)
            : base(core)
        {
        }

        [GeneralInfoItem("#MechJeb_ToggleSolarPanels", InfoItem.Category.Misc, showInEditor = false)] //Toggle solar panels
        public void SolarPanelDeployButton()
        {
            AutoDeploy = GUILayout.Toggle(AutoDeploy, Localizer.Format("#MechJeb_SolarPanelDeployButton")); //"Auto-deploy solar panels"

            if (!GUILayout.Button(ButtonText)) return;

            if (ExtendingOrRetracting())
                return;

            if (!Extended)
                ExtendAll();
            else
                RetractAll();
        }

        protected override bool IsModules(ModuleDeployablePart p) => p is ModuleDeployableSolarPanel;

        protected override string GetButtonText(DeployablePartState deployablePartState)
        {
            return deployablePartState switch
            {
                DeployablePartState.EXTENDED => Localizer.Format("#MechJeb_SolarPanelDeploy"), //"Toggle solar panels (currently extended)"
                DeployablePartState.RETRACTED => Localizer.Format("#MechJeb_SolarPanelRetracted"), //"Toggle solar panels (currently retracted)"
                _ => Localizer.Format("#MechJeb_SolarPanelToggle")
            };
        }
    }
}
