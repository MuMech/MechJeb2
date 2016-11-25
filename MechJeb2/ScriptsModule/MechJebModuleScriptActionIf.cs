using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech
{
	public class MechJebModuleScriptActionIf : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
	{
		public static String NAME = "If";
		private MechJebModuleScriptActionsList actionsThen;
		private MechJebModuleScriptActionsList actionsElse;
		MechJebModuleScriptCondition condition;
		private bool conditionVerified = false;

		public MechJebModuleScriptActionIf (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actionsThen = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			actionsElse = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			condition = new MechJebModuleScriptCondition(scriptModule, core, this);
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (condition.checkCondition())
			{
				this.actionsThen.start();
				this.conditionVerified = true;
			}
			else
			{
				this.actionsElse.start();
				this.conditionVerified = false;
			}
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
			GUILayout.Label("If", s);
			condition.WindowGUI(windowID);
			GUILayout.Label("Then", s);
			base.postWindowGUI(windowID);
			actionsThen.actionsWindowGui(windowID);
			base.preWindowGUI(windowID);
			GUILayout.Label("Else", s);
			base.postWindowGUI(windowID);
			actionsElse.actionsWindowGui(windowID);
		}

		public int getRecursiveCount()
		{
			return this.actionsThen.getRecursiveCount() + this.actionsElse.getRecursiveCount();
		}

		public List<MechJebModuleScriptAction> getRecursiveActionsList()
		{
			List<MechJebModuleScriptAction> actionsRes = new List<MechJebModuleScriptAction>();
			actionsRes.AddRange(this.actionsThen.getRecursiveActionsList());
			actionsRes.AddRange(this.actionsElse.getRecursiveActionsList());
			return actionsRes;
		}

		public void notifyEndActionsList()
		{
			this.endAction();
		}

		override public void afterOnFixedUpdate() {
			if (this.conditionVerified)
			{
				actionsThen.OnFixedUpdate();
			}
			else
			{
				actionsElse.OnFixedUpdate();
			}
		}

		override public void postLoad(ConfigNode node) {
			ConfigNode.LoadObjectFromConfig(condition, node.GetNode("Condition"));
			ConfigNode nodeListThen = node.GetNode("ActionsListThen");
			if (nodeListThen != null)
			{
				actionsThen.LoadConfig(nodeListThen);
			}
			ConfigNode nodeListElse = node.GetNode("ActionsListElse");
			if (nodeListElse != null)
			{
				actionsElse.LoadConfig(nodeListElse);
			}
		}

		override public void postSave(ConfigNode node) {
			ConfigNode conditionNode = ConfigNode.CreateConfigFromObject(this.condition, (int)Pass.Type, null);
			conditionNode.CopyTo(node.AddNode("Condition"));
			ConfigNode nodeListThen = new ConfigNode("ActionsListThen");
			actionsThen.SaveConfig(nodeListThen);
			node.AddNode(nodeListThen);
			ConfigNode nodeListElse = new ConfigNode("ActionsListElse");
			actionsElse.SaveConfig(nodeListElse);
			node.AddNode(nodeListElse);
		}
	}
}

