using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRoverWindow : DisplayModule
    {
        public MechJebModuleRoverController autopilot;

        public MechJebModuleRoverWindow(MechJebCore core) : base(core) { }

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleRoverController>();
        }

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

            ed.registry.Find(i => i.id == "Toggle:RoverController.ControlHeading").DrawItem();
            ed.registry.Find(i => i.id == "Editable:RoverController.heading").DrawItem();
            ed.registry.Find(i => i.id == "Value:RoverController.headingErr").DrawItem();
            ed.registry.Find(i => i.id == "Toggle:RoverController.ControlSpeed").DrawItem();
            ed.registry.Find(i => i.id == "Editable:RoverController.speed").DrawItem();
            ed.registry.Find(i => i.id == "Value:RoverController.speedErr").DrawItem();

            base.WindowGUI(windowID);
        }

        public override void OnUpdate()
        {
            if (autopilot != null)
            {
                if (autopilot.ControlHeading || autopilot.ControlSpeed)
                {
                    autopilot.users.Add(this);
                }
                else
                {
                    autopilot.users.Remove(this);
                }
            }
        }
    }
}
