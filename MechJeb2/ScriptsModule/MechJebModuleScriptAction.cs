using System;
using UnityEngine;

namespace MuMech
{
	public abstract class MechJebModuleScriptAction
	{
		private String name = "DEFAULT";
		private bool started = false;
		private bool executed = false;
		protected MechJebModuleScript scriptModule;
		protected MechJebCore core;
		protected int actionIndex;

		public MechJebModuleScriptAction (MechJebModuleScript scriptModule, MechJebCore core, String name)
		{
			this.scriptModule = scriptModule;
			this.core = core;
			this.name = name;
		}

		public virtual void activateAction(int actionIndex)
		{
			this.started = true;
			this.actionIndex = actionIndex;
		}

		public virtual void endAction()
		{
			this.started = false;
			this.executed = true;
			this.scriptModule.notifyEndAction(actionIndex);
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
			this.scriptModule.removeAction(this);
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
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/delete", true), new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
				{
					this.deleteAction();
				}
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/up", true), new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
				{
					this.scriptModule.moveActionUp(this);
				}
			}
		}

		public void postWindowGUI(int windowID)
		{
			GUILayout.EndHorizontal();
		}
	}
}

