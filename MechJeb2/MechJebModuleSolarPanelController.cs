using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleSolarPanelController : MechJebModuleDeployableController
    {
        public MechJebModuleSolarPanelController(MechJebCore core)
            : base(core)
        { }
        
        [GeneralInfoItem("#MechJeb_ToggleSolarPanels", InfoItem.Category.Misc, showInEditor = false)]//Toggle solar panels
        public void SolarPanelDeployButton()
        {
            autoDeploy = GUILayout.Toggle(autoDeploy, Localizer.Format("#MechJeb_SolarPanelDeployButton"));//"Auto-deploy solar panels"
            
            if (GUILayout.Button(buttonText))
            {
                if (ExtendingOrRetracting())
                    return;

                if (!extended)
                    ExtendAll();
                else
                    RetractAll();
            }
        }

        protected override bool isModules(ModuleDeployablePart p)
        {
            return p is ModuleDeployableSolarPanel;
        }

        protected override string getButtonText(DeployablePartState deployablePartState)
        {
            switch (deployablePartState)
            {
                case DeployablePartState.EXTENDED:
                    return Localizer.Format("#MechJeb_SolarPanelDeploy");//"Toggle solar panels (currently extended)"
                case DeployablePartState.RETRACTED:
                    return Localizer.Format("#MechJeb_SolarPanelRetracted");//"Toggle solar panels (currently retracted)"
                default:
                    return Localizer.Format("#MechJeb_SolarPanelToggle");//"Toggle solar panels"
            }
        }
    }
}
