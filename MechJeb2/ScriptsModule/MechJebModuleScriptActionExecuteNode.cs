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
		private List<String> actionTypes = new List<String>();
		private int startNodeCount = 0;

		public MechJebModuleScriptActionExecuteNode (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			actionTypes.Add("Next node");
			actionTypes.Add("All nodes");
		}

		override public void activateAction(int actionIndex) {
			base.activateAction(actionIndex);
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
		override public  void endAction() {
			base.endAction();
		}

		override public void readModuleConfiguration() {
		}

		override public void writeModuleConfiguration() {
		}

		override public void WindowGUI(int windowID)
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

