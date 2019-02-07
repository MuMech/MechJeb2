using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSolarPanelController : MechJebModuleDeployableController
    {
        public MechJebModuleSolarPanelController(MechJebCore core)
            : base(core)
        { }
        
        [GeneralInfoItem("Toggle solar panels", InfoItem.Category.Misc, showInEditor = false)]
        public void SolarPanelDeployButton()
        {
            autoDeploy = GUILayout.Toggle(autoDeploy, "Auto-deploy solar panels");
            
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
                    return "Toggle solar panels (currently extended)";
                case DeployablePartState.RETRACTED:
                    return "Toggle solar panels (currently retracted)";
                default:
                    return "Toggle solar panels";
            }
        }
    }
}
