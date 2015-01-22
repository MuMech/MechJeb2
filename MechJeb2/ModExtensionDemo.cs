using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{

    // This file is to show how to use the callback in VesselState (and the other module who will have them soon)

    public class ModExtensionDemo : ComputerModule
    {
        public ModExtensionDemo(MechJebCore core) : base(core) { }

        private void partUpdate(Part p)
        {
            //vesselState.ctrlTorqueAvailable.Add(new Vector3d(1, 1, 0));
            //vesselState.ctrlTorqueAvailable.Add(new Vector3d(-1, -1, 0));
        }

        private void partModuleUpdate(PartModule pm)
        {
            //vesselState.mass += 2;
        }
        
        public override void OnStart(PartModule.StartState state)
        {
            //print("ModExtensionTest adding MJ2 callback");
            //vesselState.vesselStatePartExtensions.Add(partUpdate);
            //vesselState.vesselStatePartModuleExtensions.Add(partModuleUpdate);
        }
    }
}
