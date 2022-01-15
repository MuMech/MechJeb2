using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech
{
	public class MechJebModuleScriptActionIf : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
	{
		public static string NAME = "If";
		private readonly MechJebModuleScriptActionsList actionsThen;
		private readonly MechJebModuleScriptActionsList actionsElse;
		private readonly MechJebModuleScriptCondition condition;
		private readonly GUIStyle sBorderY;
		private readonly GUIStyle sBorderG;
		private readonly GUIStyle sBorderR;

		public MechJebModuleScriptActionIf (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actionsThen = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			actionsElse = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			condition = new MechJebModuleScriptCondition(scriptModule, core, this);
            sBorderY = new GUIStyle
            {
                border = new RectOffset(1, 1, 1, 1)
            };
            sBorderG = new GUIStyle
            {
                border = new RectOffset(1, 1, 1, 1)
            };
            sBorderR = new GUIStyle
            {
                border = new RectOffset(1, 1, 1, 1)
            };
            var backgroundY = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			var backgroundG = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			var backgroundR = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			for (var x = 0; x < backgroundY.width; x++)
			{
				for (var y = 0; y < backgroundY.height; y++)
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

		public override void activateAction()
		{
			base.activateAction();
			if (condition.checkCondition())
			{
				this.actionsThen.start();
			}
			else
			{
				this.actionsElse.start();
			}
		}

		public override void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			var s = new GUIStyle(GUI.skin.label);
			s.normal.textColor = Color.yellow;
			GUILayout.BeginVertical(sBorderY);
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("If", s, GUILayout.ExpandWidth(false));
			condition.WindowGUI(windowID);
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
			var actionsRes = new List<MechJebModuleScriptAction>();
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
			var lists = new List<MechJebModuleScriptActionsList>();
			lists.Add(this.actionsThen);
			lists.Add(this.actionsElse);
			return lists;
		}

		public override void afterOnFixedUpdate() {
			if (this.condition.getConditionVerified())
			{
				actionsThen.OnFixedUpdate();
			}
			else
			{
				actionsElse.OnFixedUpdate();
			}
		}

		public override void postLoad(ConfigNode node) {
			ConfigNode.LoadObjectFromConfig(condition, node.GetNode("Condition"));
			var nodeListThen = node.GetNode("ActionsListThen");
			if (nodeListThen != null)
			{
				actionsThen.LoadConfig(nodeListThen);
			}
			var nodeListElse = node.GetNode("ActionsListElse");
			if (nodeListElse != null)
			{
				actionsElse.LoadConfig(nodeListElse);
			}
		}

		public override void postSave(ConfigNode node) {
			var conditionNode = ConfigNode.CreateConfigFromObject(this.condition, (int)Pass.Type, null);
			conditionNode.CopyTo(node.AddNode("Condition"));
			var nodeListThen = new ConfigNode("ActionsListThen");
			actionsThen.SaveConfig(nodeListThen);
			node.AddNode(nodeListThen);
			var nodeListElse = new ConfigNode("ActionsListElse");
			actionsElse.SaveConfig(nodeListElse);
			node.AddNode(nodeListElse);
		}
	}
}

