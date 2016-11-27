using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionThrottle : MechJebModuleScriptAction //TODO: Experimental script module. Not working for the moment
	{
		public static String NAME = "Throttle";

		[Persistent(pass = (int)Pass.Type)]
		private EditableInt burnIndex = 0;
		private List<String> burnRates = new List<String>();

		public MechJebModuleScriptActionThrottle (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			burnRates.Add("0%");
			burnRates.Add("10%");
			burnRates.Add("20%");
			burnRates.Add("30%");
			burnRates.Add("40%");
			burnRates.Add("50%");
			burnRates.Add("60%");
			burnRates.Add("70%");
			burnRates.Add("80%");
			burnRates.Add("90%");
			burnRates.Add("100%");
		}

		override public void activateAction()
		{
			base.activateAction();

			float throttle = (float)burnIndex*0.1f;
			//core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
			Vessel vessel = FlightGlobals.ActiveVessel;
			if (core.rcs.enabled)
			{
				Vector3d target = vessel.ReferenceTransform.InverseTransformDirection(new Vector3d(vessel.up.x*throttle, vessel.up.y*throttle, vessel.up.z*throttle));
				core.rcs.SetTargetRelative(target);
			}
			else {
				core.thrust.targetThrottle = throttle;
			}
			//core.thrust.Drive(FlightGlobals.ActiveVessel.ctrlState);
			/*Vessel vessel = FlightGlobals.ActiveVessel;
			if (vessel == null || vessel.ctrlState == null)
				return;
			if (vessel != null)
			{
				vessel.ctrlState.mainThrottle = throttle;
				FlightInputHandler.state.mainThrottle = throttle;
			}*/
			this.endAction();
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("Throttle at ");
			burnIndex = GuiUtils.ComboBox.Box(burnIndex, burnRates.ToArray(), burnRates);
			base.postWindowGUI(windowID);
		}
	}
}

