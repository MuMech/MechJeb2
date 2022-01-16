using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionRendezvousAP : MechJebModuleScriptAction
	{
		public static String NAME = "RendezvousAP";
		[Persistent(pass = (int)Pass.Type)]
		private readonly EditableDouble desiredDistance = 100;
		[Persistent(pass = (int)Pass.Type)]
		private readonly EditableDouble maxPhasingOrbits = 5;
		[Persistent(pass = (int)Pass.Type)]
		private bool autowarp;
        readonly MechJebModuleRendezvousAutopilot autopilot;
        readonly MechJebModuleRendezvousAutopilotWindow module;

		public MechJebModuleScriptActionRendezvousAP (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			this.autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();
			this.module = core.GetComputerModule<MechJebModuleRendezvousAutopilotWindow>();
			this.readModuleConfiguration();
		}

		public override void readModuleConfiguration()
		{
			autowarp = core.node.autowarp;
		}

		public override void writeModuleConfiguration()
		{
			core.node.autowarp = autowarp;
		}

		public override void activateAction()
		{
			base.activateAction();

			this.writeModuleConfiguration();
			autopilot.users.Add(module);
			autopilot.enabled = true;

			this.endAction();
		}

		public override  void endAction()
		{
			base.endAction();

			autopilot.users.Remove(module);
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Rendezvous Autopilot");
			if (autopilot != null)
			{
				if (!autopilot.enabled)
				{
					GuiUtils.SimpleTextBox("final distance:", autopilot.desiredDistance, "m");
					GuiUtils.SimpleTextBox("Max # of phasing orb.:", autopilot.maxPhasingOrbits);

					if (autopilot.maxPhasingOrbits < 5)
					{
						GUIStyle s = new GUIStyle(GUI.skin.label);
						s.normal.textColor = Color.yellow;
						GUILayout.Label("Max # of phasing orb. must be at least 5.", s);
					}
				}
				else
				{
					GUILayout.Label("Status: " + autopilot.status);
				}
			}
			base.postWindowGUI(windowID);
		}

		public override void afterOnFixedUpdate()
		{
			if (this.isStarted() && !this.isExecuted() && autopilot.enabled == false)
			{
				this.endAction();
			}
		}
	}
}

