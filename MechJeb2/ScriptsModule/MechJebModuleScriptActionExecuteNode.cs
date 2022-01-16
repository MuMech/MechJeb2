using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionExecuteNode : MechJebModuleScriptAction
	{
		public static String NAME = "ExecuteNode";
		[Persistent(pass = (int)Pass.Type)]
		private bool autowarp = true;
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		private readonly List<String> actionTypes = new List<String>();
		private int startNodeCount = 0;

		public MechJebModuleScriptActionExecuteNode (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actionTypes.Add("Next node");
			actionTypes.Add("All nodes");
		}

		public override void activateAction() {
			base.activateAction();
			if (core.node != null)
			{
				if (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0 && !core.node.enabled)
				{
					startNodeCount = FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count;
					if (actionType == 0)
					{
						core.node.ExecuteOneNode(this);
					}
					else {
						core.node.ExecuteAllNodes(this);
					}
				}
				core.node.autowarp = autowarp;
			}
		}
		public override  void endAction() {
			base.endAction();
		}

		public override void readModuleConfiguration() {
		}

		public override void writeModuleConfiguration() {
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			//Check if we have less nodes than when we start. If yes, it means the node is executed.
			if (this.isStarted() && !this.isExecuted() && ((FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count < startNodeCount && actionType == 0) || FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count == 0))
			{
				this.endAction();
			}
			base.WindowGUI(windowID);
			GUILayout.Label("Execute");
			actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			autowarp = GUILayout.Toggle(autowarp, "autowarp");
			base.postWindowGUI(windowID);
		}
	}
}

