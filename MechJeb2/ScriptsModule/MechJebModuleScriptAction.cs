using System;
using UnityEngine;

namespace MuMech
{
	public abstract class MechJebModuleScriptAction
	{
		private String name = "DEFAULT";
		protected bool started = false;
		protected bool executed = false;
		protected MechJebModuleScript scriptModule;
		protected MechJebCore core;
		protected int actionIndex;
		protected MechJebModuleScriptActionsList actionsList;

		public MechJebModuleScriptAction (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList, String name)
		{
			this.scriptModule = scriptModule;
			this.core = core;
			this.name = name;
			this.actionsList = actionsList;
		}

		public void setActionIndex(int actionIndex)
		{
			this.actionIndex = actionIndex;
		}

		public int getActionIndex()
		{
			return this.actionIndex;
		}

		public virtual void activateAction()
		{
			this.started = true;
		}

		public void markActionDone() //Used when we reload a previously saved script to mark past actions as resolved
		{
			this.started = false;
			this.executed = true;
		}

		public virtual void endAction()
		{
			this.markActionDone();
			this.actionsList.notifyEndAction();
		}

		public bool isStarted()
		{
			return this.started;
		}

		public bool isExecuted()
		{
			return this.executed;
		}

		public void deleteAction()
		{
			this.actionsList.removeAction(this);
		}

		public virtual void readModuleConfiguration() { }
		public virtual void writeModuleConfiguration() { }
		public virtual void afterOnFixedUpdate() { }
		public virtual void postLoad(ConfigNode node) {}
		public virtual void postSave(ConfigNode node) {}

		public virtual void onAbord()
		{
			this.endAction();
		}

		public String getName()
		{
			return this.name;
		}

		public void preWindowGUI(int windowID)
		{
			GUILayout.BeginHorizontal();
		}

		virtual public void WindowGUI(int windowID)
		{
			if (this.isStarted())
			{
				GUILayout.Label (scriptModule.imageRed, new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) });
			}
			else
			{
				if (this.executed)
				{
					GUILayout.Label (scriptModule.imageGreen, new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) });
				}
				else
				{
					GUILayout.Label (scriptModule.imageGray, new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) });
				}
			}
			if (!this.scriptModule.isStarted())
			{
				if (GUILayout.Button("✖", new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
				{
					this.deleteAction();
				}
				if (GUILayout.Button("↑", new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
				{
					this.actionsList.moveActionUp(this);
				}
				/*if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/delete", true), new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
				{
					this.deleteAction();
				}
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/up", true), new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
				{
					this.actionsList.moveActionUp(this);
				}*/
			}
		}

		public void postWindowGUI(int windowID)
		{
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		public void resetStatus()
		{
			this.started = false;
			this.executed = false;
		}
	}
}

