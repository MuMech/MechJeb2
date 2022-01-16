using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionDockingAutopilot : MechJebModuleScriptAction
	{
		public static String NAME = "DockingAutopilot";
		private readonly MechJebModuleDockingAutopilot autopilot;
		private readonly MechJebModuleDockingGuidance moduleGuidance;
		[Persistent(pass = (int)Pass.Type)]
		private readonly double speedLimit;
		[Persistent(pass = (int)Pass.Type)]
		private readonly double rol;
		[Persistent(pass = (int)Pass.Type)]
		private readonly bool forceRol;
		[Persistent(pass = (int)Pass.Type)]
		private readonly double overridenSafeDistance;
		[Persistent(pass = (int)Pass.Type)]
		private readonly bool overrideSafeDistance;
		[Persistent(pass = (int)Pass.Type)]
		private readonly bool overrideTargetSize;
		[Persistent(pass = (int)Pass.Type)]
		private readonly double overridenTargetSize;
		[Persistent(pass = (int)Pass.Type)]
		private readonly float safeDistance;
		[Persistent(pass = (int)Pass.Type)]
		private readonly float targetSize;
		[Persistent(pass = (int)Pass.Type)]
		private readonly bool drawBoundingBox;

		public MechJebModuleScriptActionDockingAutopilot (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			
			this.autopilot = core.GetComputerModule<MechJebModuleDockingAutopilot>(); ;
			this.moduleGuidance = core.GetComputerModule<MechJebModuleDockingGuidance>(); ;
			this.speedLimit = autopilot.speedLimit;
			this.rol = autopilot.rol;
			this.forceRol = autopilot.forceRol;
			this.overridenSafeDistance = autopilot.overridenSafeDistance;
			this.overrideSafeDistance = autopilot.overrideSafeDistance;
			this.overrideTargetSize = autopilot.overrideTargetSize;
			this.overridenTargetSize = autopilot.overridenTargetSize;
			this.safeDistance = autopilot.safeDistance;
			this.targetSize = autopilot.targetSize;
			this.drawBoundingBox = autopilot.drawBoundingBox;
		}

		public override void activateAction()
		{
			autopilot.users.Add(this.moduleGuidance);
			autopilot.speedLimit = speedLimit;
			autopilot.rol = rol;
			autopilot.forceRol = forceRol;
			autopilot.overridenSafeDistance = overridenSafeDistance;
			autopilot.overrideSafeDistance = overrideSafeDistance;
			autopilot.overrideTargetSize = overrideTargetSize;
			autopilot.overridenTargetSize = overridenTargetSize;
			autopilot.safeDistance = safeDistance;
			autopilot.targetSize = targetSize;
			autopilot.drawBoundingBox = drawBoundingBox;
			autopilot.enabled = true;
			autopilot.users.Add(this.moduleGuidance);
			autopilot.dockingStep = MechJebModuleDockingAutopilot.DockingStep.INIT;
			base.activateAction();
		}

		public override  void endAction()
		{
			autopilot.users.Remove(this.moduleGuidance);
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Docking Autopilot");
			if (autopilot.status.CompareTo ("") != 0)
			{
				GUILayout.Label (autopilot.status);
			}
			base.postWindowGUI(windowID);
		}

		public override void afterOnFixedUpdate()
		{
			if (this.isStarted() && !this.isExecuted() && autopilot.dockingStep == MechJebModuleDockingAutopilot.DockingStep.OFF)
			{
				this.endAction();
			}
		}

		public override void onAbord()
		{
			this.autopilot.enabled = false;
			base.onAbord();
		}
	}
}

