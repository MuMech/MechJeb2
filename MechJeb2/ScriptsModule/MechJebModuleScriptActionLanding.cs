using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionLanding : MechJebModuleScriptAction
	{
		public static String NAME = "Landing";

		MechJebModuleLandingGuidance module;
		MechJebModuleLandingAutopilot module_autopilot;
		[Persistent(pass = (int)Pass.Type)]
		private bool deployGear = true;
		[Persistent(pass = (int)Pass.Type)]
		private bool deployChutes = true;
		[Persistent(pass = (int)Pass.Type)]
		public EditableInt limitChutesStage = 0;
		[Persistent(pass = (int)Pass.Type)]
		private EditableDouble touchdownSpeed;
		[Persistent(pass = (int)Pass.Type)]
		private bool landTarget = false;
		[Persistent(pass = (int)Pass.Type)]
		private EditableAngle targetLattitude = new EditableAngle(0);
		[Persistent(pass = (int)Pass.Type)]
		private EditableAngle targetLongitude = new EditableAngle(0);

		public MechJebModuleScriptActionLanding (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			module = core.GetComputerModule<MechJebModuleLandingGuidance>();
			module_autopilot = core.GetComputerModule<MechJebModuleLandingAutopilot>();
			this.readModuleConfiguration();
		}

		override public void activateAction()
		{
			base.activateAction();
			if (this.landTarget)
			{
				module_autopilot.LandAtPositionTarget(this);
			}
			else
			{
				module_autopilot.LandUntargeted(this);
			}
			this.writeModuleConfiguration();
			module_autopilot.users.Add(module);
		}

		override public void endAction()
		{
			base.endAction();
			module_autopilot.users.Remove(module);
		}

		override public void readModuleConfiguration()
		{
			deployGear = module_autopilot.deployGears;
			deployChutes = module_autopilot.deployChutes;
			limitChutesStage = module_autopilot.limitChutesStage;
			touchdownSpeed = module_autopilot.touchdownSpeed;
			if (core.target.PositionTargetExists)
			{
				landTarget = true;
				targetLattitude = core.target.targetLatitude;
				targetLongitude = core.target.targetLongitude;
			}
		}

		override public void writeModuleConfiguration()
		{
			module_autopilot.deployGears = deployGear;
			module_autopilot.deployChutes = deployChutes;
			module_autopilot.touchdownSpeed = touchdownSpeed;
			module_autopilot.limitChutesStage = limitChutesStage;
			if (landTarget)
			{
				core.target.SetPositionTarget(core.vessel.mainBody, targetLattitude, targetLongitude);
			}
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			if (landTarget)
			{
				GUILayout.Label("Land At Target");
			}
			else
			{
				GUILayout.Label("Land Somewhere");
			}
			if (this.isStarted())
			{
				GUILayout.Label(core.landing.CurrentStep.ToString());
			}
			base.postWindowGUI(windowID);
		}

		override public void afterOnFixedUpdate()
		{
			if (this.isStarted() && !this.isExecuted() && module_autopilot.CurrentStep == null)
			{
				this.endAction();
			}
		}
	}
}

