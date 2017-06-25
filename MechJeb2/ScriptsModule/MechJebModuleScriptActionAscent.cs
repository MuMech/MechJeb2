using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionAscent : MechJebModuleScriptAction
	{
		public static String NAME = "Ascent";
		private MechJebModuleAscentAutopilot autopilot;
		private MechJebModuleAscentGuidance ascentModule;
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		private List<String> actionTypes = new List<String>();
		//Module Parameters
		[Persistent(pass = (int)Pass.Type)]
		public MechJebModuleAscentBase ascentPath;
		[Persistent(pass = (int)Pass.Type)]
		public double desiredOrbitAltitude;
		[Persistent(pass = (int)Pass.Type)]
		public double desiredInclination;
		[Persistent(pass = (int)Pass.Type)]
		public bool autoThrottle;
		[Persistent(pass = (int)Pass.Type)]
		public bool correctiveSteering;
		[Persistent(pass = (int)Pass.Type)]
		public bool forceRoll;
		[Persistent(pass = (int)Pass.Type)]
		public double verticalRoll;
		[Persistent(pass = (int)Pass.Type)]
		public double turnRoll;
		[Persistent(pass = (int)Pass.Type)]
		public bool autodeploySolarPanels;
		[Persistent(pass = (int)Pass.Type)]
		public bool _autostage;
		[Persistent(pass = (int)Pass.Type)]
		public bool launchingToRendezvous = false;
		[Persistent(pass = (int)Pass.Type)]
		public CelestialBody mainBody;
		[Persistent(pass = (int)Pass.Type)]
		public Orbit targetOrbit;

		public MechJebModuleScriptActionAscent (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(scriptModule, core, actionsList, NAME)
		{
			this.autopilot = core.GetComputerModule<MechJebModuleAscentAutopilot>();
			this.ascentModule = core.GetComputerModule<MechJebModuleAscentGuidance>();
			this.mainBody = core.target.mainBody;
			this.targetOrbit = core.target.TargetOrbit;
			actionTypes.Add("Ascent Guidance");
			actionTypes.Add("Launching to Rendezvous");
			this.readModuleConfiguration();
		}

		override public void readModuleConfiguration()
		{
			this.ascentPath = this.autopilot.ascentPath;
			this.desiredOrbitAltitude = this.autopilot.desiredOrbitAltitude.val;
			this.desiredInclination = this.autopilot.desiredInclination;
			this.autoThrottle = this.autopilot.autoThrottle;
			this.correctiveSteering = this.autopilot.correctiveSteering;
			this.forceRoll = this.autopilot.forceRoll;
			this.verticalRoll = this.autopilot.verticalRoll;
			this.turnRoll = this.autopilot.turnRoll;
			this.autodeploySolarPanels = this.autopilot.autodeploySolarPanels;
			this._autostage = this.autopilot._autostage;
		}

		override public void writeModuleConfiguration()
		{
			this.autopilot.ascentPath = this.ascentPath;
			this.autopilot.desiredOrbitAltitude.val = this.desiredOrbitAltitude;
			this.autopilot.desiredInclination = this.desiredInclination;
			this.autopilot.autoThrottle = this.autoThrottle;
			this.autopilot.correctiveSteering = this.correctiveSteering;
			this.autopilot.forceRoll = this.forceRoll;
			this.autopilot.verticalRoll = this.verticalRoll;
			this.autopilot.turnRoll = this.turnRoll;
			this.autopilot.autodeploySolarPanels = this.autodeploySolarPanels;
			this.autopilot._autostage = this._autostage;
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			if (actionType == 1)
			{
				this.launchingToRendezvous = true;
			}
			else {
				this.launchingToRendezvous = false;
			}
			if (!this.launchingToRendezvous) {
				GUILayout.Label ("ASCENT to " + (this.desiredOrbitAltitude / 1000.0) + "km");
			} else {
				GUILayout.Label ("Launching to rendezvous");
			}

			if (this.autopilot != null)
			{
				if (this.isExecuted() && this.autopilot.status.CompareTo ("Off") == 0)
				{
					GUILayout.Label ("Finished Ascend");
				}
				else
				{
					GUILayout.Label (this.autopilot.status);
				}
			}
			else
			{
				GUIStyle s = new GUIStyle(GUI.skin.label);
				s.normal.textColor = Color.yellow;
				GUILayout.Label ("-- ERROR --", s);
			}
			base.postWindowGUI(windowID);
		}

		override public void afterOnFixedUpdate()
		{
			if (this.autopilot != null)
			{
				if (this.isStarted() && !this.isExecuted() && this.autopilot.status.CompareTo("Off") == 0)
				{
					this.endAction();
				}
			}
		}

		override public void activateAction()
		{
			base.activateAction();
			this.writeModuleConfiguration();
			if (this.launchingToRendezvous)
			{
				this.autopilot.StartCountdown(this.scriptModule.vesselState.time +
					LaunchTiming.TimeToPhaseAngle(this.autopilot.launchPhaseAngle,
						mainBody, this.scriptModule.vesselState.longitude, targetOrbit));
			}
			this.autopilot.users.Add (this.ascentModule);
		}

		override public void endAction()
		{
			base.endAction();
			this.autopilot.users.Remove(this.ascentModule);
		}

		override public void onAbord()
		{
			this.ascentModule.launchingToInterplanetary = this.ascentModule.launchingToPlane = this.ascentModule.launchingToRendezvous = this.autopilot.timedLaunch = false;
			base.onAbord();
		}
	}
}
