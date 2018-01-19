using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSolarPanelController : MechJebModuleDeployableController
    {
        public MechJebModuleSolarPanelController(MechJebCore core)
            : base(core)
        { }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            buttonText = "Toggle solar panels";
            extended = !AllRetracted();
        }


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

                extended = !extended;
            }
        }

        protected override List<ModuleDeployablePart> getModules(Part p)
        {
            return p.Modules.GetModules<ModuleDeployableSolarPanel>().ConvertAll(x => (ModuleDeployablePart)x);
        }
    }
}
