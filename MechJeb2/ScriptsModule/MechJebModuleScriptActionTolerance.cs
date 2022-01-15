using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionTolerance : MechJebModuleScriptAction
	{
		public static string NAME = "Tolerance";
		[Persistent(pass = (int)Pass.Type)]
		private readonly EditableDouble tolerance;
		[Persistent(pass = (int)Pass.Type)]
		private readonly EditableDouble leadTime;

		public MechJebModuleScriptActionTolerance (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			tolerance = new EditableDouble(core.node.tolerance.val);
			leadTime = new EditableDouble(core.node.leadTime.val);
		}

		public override void activateAction()
		{
			base.activateAction();
			core.node.tolerance = tolerance;
			core.node.leadTime = leadTime;
			this.endAction();
		}

		public override  void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);

			base.WindowGUI(windowID);
			GUILayout.Label("Tolerance:", GUILayout.ExpandWidth(false));
			tolerance.text = GUILayout.TextField(tolerance.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
			if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
			{
				tolerance.val += 0.1;
			}
			if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
			{
				tolerance.val -= tolerance.val > 0.1 ? 0.1 : 0.0;
			}
			if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
			{
				tolerance.val = 0.1;
			}
			GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
			GUILayout.Label("Lead time:", GUILayout.ExpandWidth(false));
			leadTime.text = GUILayout.TextField(leadTime.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
			GUILayout.Label("s", GUILayout.ExpandWidth(false));

			base.postWindowGUI(windowID);
		}
	}
}

