﻿using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech
{
	public class MechJebModuleScriptActionParallel : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
	{
		public static String NAME = "Parallel";
		private MechJebModuleScriptActionsList actions1;
		private MechJebModuleScriptActionsList actions2;
		private GUIStyle sBorderY;
		private GUIStyle sBorderB;
		private int countEndList = 0;
		private bool panel1Hidden = false;
		private bool panel2Hidden = false;

		public MechJebModuleScriptActionParallel (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actions1 = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			actions2 = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
			sBorderB = new GUIStyle();
			sBorderB.border = new RectOffset(1, 1, 1, 1);
			Texture2D backgroundB = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			sBorderY = new GUIStyle();
			sBorderY.border = new RectOffset(1, 1, 1, 1);
			Texture2D backgroundY = new Texture2D(16, 16, TextureFormat.RGBA32, false);
			for (int x = 0; x < backgroundB.width; x++)
			{
				for (int y = 0; y < backgroundB.height; y++)
				{
					if (x == 0 || x == 15 || y == 0 || y == 15)
					{
						backgroundB.SetPixel(x, y, Color.blue);
						backgroundY.SetPixel(x, y, Color.yellow);
					}
					else
					{
						backgroundB.SetPixel(x, y, Color.clear);
						backgroundY.SetPixel(x, y, Color.clear);
					}
				}
			}
			backgroundB.Apply();
			backgroundY.Apply();

			sBorderB.normal.background = backgroundB;
			sBorderB.onNormal.background = backgroundB;
			sBorderB.padding = new RectOffset(1, 1, 1, 1);
			sBorderY.normal.background = backgroundY;
			sBorderY.onNormal.background = backgroundY;
			sBorderY.padding = new RectOffset(1, 1, 1, 1);
		}

		override public void activateAction()
		{
			base.activateAction();
			this.actions1.start();
			this.actions2.start();
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
			GUILayout.Label("Parallel Execution", s, GUILayout.ExpandWidth(false));
			if (this.panel1Hidden)
			{
				if (GUILayout.Button("<<Show Panel 1", GUILayout.ExpandWidth(false)))
				{
					this.panel1Hidden = false;
				}
			}
			else if (!this.panel2Hidden)
			{
				if (GUILayout.Button(">>Hide Panel 1", GUILayout.ExpandWidth(false)))
				{
					this.panel1Hidden = true;
				}
			}
			else
			{
				if (GUILayout.Button(">>Hide Panel 1 and <<Show Panel 2", GUILayout.ExpandWidth(false)))
				{
					this.panel1Hidden = true;
					this.panel2Hidden = false;
				}
			}
			if (this.panel2Hidden)
			{
				if (GUILayout.Button("<<Show Panel 2", GUILayout.ExpandWidth(false)))
				{
					this.panel2Hidden = false;
				}
			}
			else if (!this.panel1Hidden)
			{
				if (GUILayout.Button(">>Hide Panel 2", GUILayout.ExpandWidth(false)))
				{
					this.panel2Hidden = true;
				}
			}
			else
			{
				if (GUILayout.Button(">>Hide Panel 2 and <<Show Panel 1", GUILayout.ExpandWidth(false)))
				{
					this.panel2Hidden = true;
					this.panel1Hidden = false;
				}
			}
			base.postWindowGUI(windowID);
			GUILayout.BeginHorizontal();
			if (!this.panel1Hidden)
			{
				GUILayout.BeginVertical(sBorderB);
				actions1.actionsWindowGui(windowID);
				GUILayout.EndVertical();
			}
			if (!this.panel2Hidden)
			{
				GUILayout.BeginVertical(sBorderB);
				actions2.actionsWindowGui(windowID);
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		public int getRecursiveCount()
		{
			return this.actions1.getRecursiveCount() + this.actions2.getRecursiveCount();
		}

		public List<MechJebModuleScriptAction> getRecursiveActionsList()
		{
			List<MechJebModuleScriptAction> actionsRes = new List<MechJebModuleScriptAction>();
			actionsRes.AddRange(this.actions1.getRecursiveActionsList());
			actionsRes.AddRange(this.actions2.getRecursiveActionsList());
			return actionsRes;
		}

		public void notifyEndActionsList()
		{
			countEndList++;
			if (countEndList == 2)
			{
				this.endAction();
			}
		}

		public List<MechJebModuleScriptActionsList> getActionsListsObjects()
		{
			List<MechJebModuleScriptActionsList> lists = new List<MechJebModuleScriptActionsList>();
			lists.Add(this.actions1);
			lists.Add(this.actions2);
			return lists;
		}

		override public void afterOnFixedUpdate() {
			if (actions1.isStarted() && !actions1.isExecuted())
			{
				actions1.OnFixedUpdate();
			}
			if (actions2.isStarted() && !actions2.isExecuted())
			{
				actions2.OnFixedUpdate();
			}
		}

		override public void postLoad(ConfigNode node) {
			ConfigNode nodeList1 = node.GetNode("ActionsList1");
			if (nodeList1 != null)
			{
				actions1.LoadConfig(nodeList1);
			}
			ConfigNode nodeList2 = node.GetNode("ActionsList2");
			if (nodeList2 != null)
			{
				actions2.LoadConfig(nodeList2);
			}
		}

		override public void postSave(ConfigNode node) {
			ConfigNode nodeList1 = new ConfigNode("ActionsList1");
			actions1.SaveConfig(nodeList1);
			node.AddNode(nodeList1);
			ConfigNode nodeList2 = new ConfigNode("ActionsList2");
			actions2.SaveConfig(nodeList2);
			node.AddNode(nodeList2);
		}
	}
}

