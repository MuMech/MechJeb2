using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleDeployableAntennaController : MechJebModuleDeployableController
    {
        public MechJebModuleDeployableAntennaController(MechJebCore core) : base(core)
        { }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            extended = !AllRetracted();
            buttonText = "Toggle antennas";
        }

        [GeneralInfoItem("Toggle antennas", InfoItem.Category.Misc, showInEditor = false)]
        public void AntennaDeployButton()
        {
            autoDeploy = GUILayout.Toggle(autoDeploy, "Auto-deploy antennas");

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
            return p.Modules.GetModules<ModuleDeployableAntenna>().ConvertAll(x => (ModuleDeployablePart)x);
        }

        protected override string getButtonText(DeployablePartState deployablePartState)
        {
            switch (deployablePartState)
            {
                case DeployablePartState.EXTENDED:
                    return "Toggle antennas (currently extended)";
                case DeployablePartState.RETRACTED:
                    return "Toggle antennas (currently retracted)";
                default:
                    return "Toggle antennas";
            }
        }
    }
}

