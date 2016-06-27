using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionRendezvous : MechJebModuleScriptAction
	{
		public static String NAME = "RendezVous";

		[Persistent(pass = (int)Pass.Type)]
		private int actionType = 0;
		private MechJebModuleRendezvousGuidance module;

		public MechJebModuleScriptActionRendezvous (MechJebModuleScript scriptModule, MechJebModuleRendezvousGuidance module, int actionType):base(scriptModule, name)
		{
			this.actionType = actionType;
			this.module = module;
		}

		public MechJebModuleScriptActionRendezvous(MechJebModuleScript scriptModule, String name, MechJebModuleRendezvousGuidance module) : base(scriptModule, name)
		{
			this.module = module;
		}

		override public void activateAction(int actionIndex) {
			base.activateAction(actionIndex);
			if (actionType == 0) {
				module.executeHohmann ();
			} else if (actionType == 1) {
				module.executeMatchVelocities ();
			} else if (actionType == 2) {
				module.executeGetCloser ();
			}
		}
		override public  void endAction() {
			base.endAction();
		}

		override public void readModuleConfiguration() {
		}

		override public void writeModuleConfiguration() {
		}

		override public void WindowGUI(int windowID) {
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			if (actionType == 0) {
				GUILayout.Label ("Intercept target with Hohmann transfer");
			} else if (actionType == 1) {
				GUILayout.Label ("Match velocities");
			} else if (actionType == 2) {
				GUILayout.Label ("Get Closer");
			}
			base.postWindowGUI(windowID);
		}
	}
}

