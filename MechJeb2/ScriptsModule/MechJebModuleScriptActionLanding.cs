using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionLanding : MechJebModuleScriptAction
	{
		public static String NAME = "Landing";

		MechJebModuleLandingGuidance module;
		[Persistent(pass = (int)Pass.Type)]
		private bool deployGear = true;
		[Persistent(pass = (int)Pass.Type)]
		private bool deployChutes = true;
		[Persistent(pass = (int)Pass.Type)]
		private EditableDouble touchdownSpeed;

		public MechJebModuleScriptActionLanding (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			module = core.GetComputerModule<MechJebModuleLandingGuidance>();
			this.readModuleConfiguration();
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			core.landing.LandUntargeted(this);
			this.writeModuleConfiguration();
			core.landing.users.Add(module);
		}

		override public  void endAction()
		{
			base.endAction();
			core.landing.users.Remove(module);
		}

		override public void readModuleConfiguration()
		{
			deployGear = core.landing.deployGears;
			deployChutes = core.landing.deployChutes;
			touchdownSpeed = core.landing.touchdownSpeed;
		}

		override public void writeModuleConfiguration()
		{
			core.landing.deployGears = deployGear;
			core.landing.deployChutes = deployChutes;
			core.landing.touchdownSpeed = touchdownSpeed;
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Land Somewhere");
			if (this.isStarted())
			{
				GUILayout.Label(core.landing.CurrentStep.ToString());
			}
			if (this.isStarted() && !this.isExecuted() && core.landing.CurrentStep == null)
			{
				this.endAction();
			}
			base.postWindowGUI(windowID);
		}
	}
}

