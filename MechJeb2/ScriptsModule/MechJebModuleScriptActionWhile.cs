using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech
{
	public class MechJebModuleScriptActionWhile : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
	{
		public static String NAME = "While";
		private MechJebModuleScriptActionsList actions;
		MechJebModuleScriptCondition condition;

		public MechJebModuleScriptActionWhile (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actions = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			condition = new MechJebModuleScriptCondition(scriptModule, core, this);
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			this.actions.start();
		}

		override public void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			GUIStyle s = new GUIStyle(GUI.skin.label);
			s.normal.textColor = Color.yellow;
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("While", s);
			condition.WindowGUI(windowID);
			base.postWindowGUI(windowID);
			actionsList.actionsWindowGui(windowID);
		}

		public int getRecursiveCount()
		{
			return this.actions.getRecursiveCount();
		}

		public List<MechJebModuleScriptAction> getRecursiveActionsList()
		{
			return this.actions.getRecursiveActionsList();
		}

		public void notifyEndActionsList()
		{
			if (condition.checkCondition())
			{
				this.endAction();
			}
		}

		override public void afterOnFixedUpdate() {
			actions.OnFixedUpdate();
		}

		override public void postLoad(ConfigNode node) {
			ConfigNode.LoadObjectFromConfig(condition, node.GetNode("Condition"));
			ConfigNode nodeList = node.GetNode("ActionsList");
			if (nodeList != null)
			{
				actions.LoadConfig(nodeList);
			}
		}

		override public void postSave(ConfigNode node) {
			ConfigNode conditionNode = ConfigNode.CreateConfigFromObject(this.condition, (int)Pass.Type, null);
			conditionNode.CopyTo(node.AddNode("Condition"));
			ConfigNode nodeListThen = new ConfigNode("ActionsList");
			actions.SaveConfig(nodeListThen);
			node.AddNode(nodeListThen);
		}
	}
}

