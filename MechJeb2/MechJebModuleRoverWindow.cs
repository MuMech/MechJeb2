using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleRoverWindow : DisplayModule
    {
        public MechJebModuleRoverWindow(MechJebCore core) : base(core) { }

        public override string GetName()
        {
            return "Rover Autopilot";
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(50) };
        }

        protected override void WindowGUI(int windowID)
        {
            MechJebModuleCustomWindowEditor ed = core.GetComputerModule<MechJebModuleCustomWindowEditor>();

            ed.registry.Find(i => i.id == "ToggleInfoItem:MechJebModuleRoverController.ControlHeading").DrawItem();
            ed.registry.Find(i => i.id == "EditableInfoItem:MechJebModuleRoverController.heading").DrawItem();
            ed.registry.Find(i => i.id == "ValueInfoItem:MechJebModuleRoverController.headingErr").DrawItem();
            ed.registry.Find(i => i.id == "ToggleInfoItem:MechJebModuleRoverController.ControlSpeed").DrawItem();
            ed.registry.Find(i => i.id == "EditableInfoItem:MechJebModuleRoverController.speed").DrawItem();
            ed.registry.Find(i => i.id == "ValueInfoItem:MechJebModuleRoverController.speedErr").DrawItem();

            base.WindowGUI(windowID);
        }
    }
}
