using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech
{
	public class MechJebModuleScriptActionFor : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
	{
		public static String NAME = "For";
		private MechJebModuleScriptActionsList actions;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt times = 0;

		public MechJebModuleScriptActionFor (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actions = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth()+1);
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			this.actionsList.start();
		}

		override public void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GuiUtils.SimpleTextBox("Repeat", times, "times", 30);
			base.postWindowGUI(windowID);
			actions.actionsWindowGui(windowID);
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
			this.endAction();
		}

		override public void afterOnFixedUpdate() {
			actionsList.OnFixedUpdate();
		}

		override public void postLoad(ConfigNode node) {
			ConfigNode nodeList = node.GetNode("ActionsList");
			if (nodeList != null)
			{
				actions.LoadConfig(nodeList);
			}
		}

		override public void postSave(ConfigNode node) {
			ConfigNode nodeList = new ConfigNode("ActionsList");
			actions.SaveConfig(nodeList);
			node.AddNode(nodeList);
		}
	}
}

