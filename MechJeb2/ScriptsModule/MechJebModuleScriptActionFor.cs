using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech
{
	public class MechJebModuleScriptActionFor : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
	{
		public static String NAME = "For";
		private readonly MechJebModuleScriptActionsList actions;
		[Persistent(pass = (int)Pass.Type)]
		private readonly EditableInt times = 2;
		private int executedTimes = 0;
		private readonly GUIStyle sBorder;

		public MechJebModuleScriptActionFor (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actions = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
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
			this.executedTimes = 0;
			this.actions.start();
		}

		public override void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			GUIStyle s = new GUIStyle(GUI.skin.label);
			s.normal.textColor = Color.yellow;
			GUILayout.BeginVertical(sBorder);
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("Repeat", s, GUILayout.ExpandWidth(false));
			if (this.isStarted() && !this.isExecuted())
			{
				s = new GUIStyle(GUI.skin.label);
				s.normal.textColor = Color.red;
				GUILayout.Label(times.val + " times. Executed", GUILayout.ExpandWidth(false));
				GUILayout.Label(this.executedTimes + "", s, GUILayout.ExpandWidth(false));
				GUILayout.Label("/" + times.val, GUILayout.ExpandWidth(false));
			}
			else
			{
				GuiUtils.SimpleTextBox("", times, "times", 30);
			}
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
			executedTimes++;
			if (executedTimes >= times)
			{
				this.endAction();
			}
			else
			{
				//Reset the list status
				this.actions.recursiveResetStatus();
				this.actions.start();
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
			ConfigNode nodeList = node.GetNode("ActionsList");
			if (nodeList != null)
			{
				actions.LoadConfig(nodeList);
			}
		}

		public override void postSave(ConfigNode node) {
			ConfigNode nodeList = new ConfigNode("ActionsList");
			actions.SaveConfig(nodeList);
			node.AddNode(nodeList);
		}
	}
}

