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
		private MechJebModuleScriptCondition condition;
		private bool conditionVerified = false;
		private GUIStyle sBorderY;
		private GUIStyle sBorderG;
		private GUIStyle sBorderR;

		public MechJebModuleScriptActionIf (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actionsThen = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			actionsElse = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			condition = new MechJebModuleScriptCondition(scriptModule, core, this);
			sBorderY = new GUIStyle();
			sBorderY.border = new RectOffset(1, 1, 1, 1);
			sBorderG = new GUIStyle();
			sBorderG.border = new RectOffset(1, 1, 1, 1);
			sBorderR = new GUIStyle();
			sBorderR.border = new RectOffset(1, 1, 1, 1);
			Texture2D backgroundY = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			Texture2D backgroundG = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			Texture2D backgroundR = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			for (int x = 0; x < backgroundY.width; x++)
			{
				for (int y = 0; y < backgroundY.height; y++)
				{
					if (x == 0 || x == 15 || y == 0 || y == 15)
					{
						backgroundY.SetPixel(x, y, Color.yellow);
						backgroundG.SetPixel(x, y, Color.green);
						backgroundR.SetPixel(x, y, Color.red);
					}
					else
					{
						backgroundY.SetPixel(x, y, Color.clear);
						backgroundG.SetPixel(x, y, Color.clear);
						backgroundR.SetPixel(x, y, Color.clear);
					}
				}
			}
			backgroundY.Apply();
			backgroundG.Apply();
			backgroundR.Apply();
			sBorderY.normal.background = backgroundY;
			sBorderY.onNormal.background = backgroundY;
			sBorderG.normal.background = backgroundG;
			sBorderG.onNormal.background = backgroundG;
			sBorderR.normal.background = backgroundR;
			sBorderR.onNormal.background = backgroundR;
			sBorderY.padding = new RectOffset(1, 1, 1, 1);
			sBorderG.padding = new RectOffset(1, 1, 1, 1);
			sBorderR.padding = new RectOffset(1, 1, 1, 1);
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
			GUILayout.BeginVertical(sBorderY);
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("If", s, GUILayout.ExpandWidth(false));
			condition.WindowGUI(windowID);
			if (this.isStarted() || this.executed)
			{
				if (this.conditionVerified)
				{
					s = new GUIStyle(GUI.skin.label);
					s.normal.textColor = Color.red;
					GUILayout.Label("(Verified)", s, GUILayout.ExpandWidth(false));
				}
				else
				{
					s = new GUIStyle(GUI.skin.label);
					s.normal.textColor = Color.red;
					GUILayout.Label("(NOT Verified)", s, GUILayout.ExpandWidth(false));
				}
			}
			s.normal.textColor = Color.yellow;
			GUILayout.Label("Then", s, GUILayout.ExpandWidth(false));
			base.postWindowGUI(windowID);

			GUILayout.BeginHorizontal();
			GUILayout.Space(50);
			GUILayout.BeginVertical(sBorderG);
			actionsThen.actionsWindowGui(windowID);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Space(50);
			GUILayout.BeginVertical();
			GUILayout.Label("Else", s, GUILayout.ExpandWidth(false));
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Space(50);
			GUILayout.BeginVertical(sBorderR);
			actionsElse.actionsWindowGui(windowID);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
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

		public List<MechJebModuleScriptActionsList> getActionsListsObjects()
		{
			List<MechJebModuleScriptActionsList> lists = new List<MechJebModuleScriptActionsList>();
			lists.Add(this.actionsThen);
			lists.Add(this.actionsElse);
			return lists;
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

