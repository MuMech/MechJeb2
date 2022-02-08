using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech
{
	public class MechJebModuleScriptActionWhile : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
	{
		public static String NAME = "While";
		private readonly MechJebModuleScriptActionsList actions;
		private readonly MechJebModuleScriptCondition condition;
		private readonly GUIStyle sBorder;

		public MechJebModuleScriptActionWhile (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actions = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			condition = new MechJebModuleScriptCondition(scriptModule, core, this);
			sBorder = new GUIStyle();
			sBorder.border = new RectOffset(1, 1, 1, 1);
			Texture2D background = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			for (int x = 0; x < background.width; x++)
			{
				for (int y = 0; y < background.height; y++)
				{
					if (x == 0 || x == 15 || y == 0 || y == 15)
					{
						background.SetPixel(x, y, Color.yellow);
					}
					else
					{
						background.SetPixel(x, y, Color.clear);
					}
				}
			}
			background.Apply();
			sBorder.normal.background = background;
			sBorder.onNormal.background = background;
			sBorder.padding = new RectOffset(1, 1, 1, 1);
		}

		public override void activateAction()
		{
			base.activateAction();
			this.actions.start();
		}

		public override void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			GUILayout.BeginVertical(sBorder);
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("While",GuiUtils.yellowLabel, GUILayout.ExpandWidth(false));
			condition.WindowGUI(windowID);
			base.postWindowGUI(windowID);
			GUILayout.BeginHorizontal();
			GUILayout.Space(50);
			GUILayout.BeginVertical();
			actions.actionsWindowGui(windowID);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
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

		public List<MechJebModuleScriptActionsList> getActionsListsObjects()
		{
			List<MechJebModuleScriptActionsList> lists = new List<MechJebModuleScriptActionsList>();
			lists.Add(this.actions);
			return lists;
		}

		public override void afterOnFixedUpdate() {
			actions.OnFixedUpdate();
		}

		public override void postLoad(ConfigNode node) {
			ConfigNode.LoadObjectFromConfig(condition, node.GetNode("Condition"));
			ConfigNode nodeList = node.GetNode("ActionsList");
			if (nodeList != null)
			{
				actions.LoadConfig(nodeList);
			}
		}

		public override void postSave(ConfigNode node) {
			ConfigNode conditionNode = ConfigNode.CreateConfigFromObject(this.condition, (int)Pass.Type, null);
			conditionNode.CopyTo(node.AddNode("Condition"));
			ConfigNode nodeListThen = new ConfigNode("ActionsList");
			actions.SaveConfig(nodeListThen);
			node.AddNode(nodeListThen);
		}
	}
}

