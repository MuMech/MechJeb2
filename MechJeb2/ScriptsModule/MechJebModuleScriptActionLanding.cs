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
		[Persistent(pass = (int)Pass.Type)]
		private bool landTarget = false;
		[Persistent(pass = (int)Pass.Type)]
		private EditableAngle targetLattitude;
		[Persistent(pass = (int)Pass.Type)]
		private EditableAngle targetLongitude;

		public MechJebModuleScriptActionLanding (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			module = core.GetComputerModule<MechJebModuleLandingGuidance>();
			this.readModuleConfiguration();
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (this.landTarget)
			{
				core.landing.LandAtPositionTarget(this);
			}
			else
			{
				core.landing.LandUntargeted(this);
			}
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
			if (core.target.PositionTargetExists)
			{
				landTarget = true;
				targetLattitude = core.target.targetLatitude;
				targetLongitude = core.target.targetLongitude;
			}
		}

		override public void writeModuleConfiguration()
		{
			core.landing.deployGears = deployGear;
			core.landing.deployChutes = deployChutes;
			core.landing.touchdownSpeed = touchdownSpeed;
			if (landTarget)
			{
				core.target.targetLatitude = targetLattitude;
				core.target.targetLongitude = targetLongitude;
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
			if (this.isStarted() && !this.isExecuted() && core.landing.CurrentStep == null)
			{
				this.endAction();
			}
		}
	}
}

