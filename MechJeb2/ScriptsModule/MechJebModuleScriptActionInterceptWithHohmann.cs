using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionInterceptWithHohmann : MechJebModuleScriptAction
	{
		public static String NAME = "InterceptWithHohmann";

		public MechJebModuleScriptActionInterceptWithHohmann (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
		}

		override public void activateAction(int actionIndex) {
			base.activateAction(actionIndex);
			double UT;
			Vessel vessel = FlightGlobals.ActiveVessel;
			Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(this.scriptModule.orbit, core.target.TargetOrbit, this.scriptModule.vesselState.time, out UT);
			vessel.RemoveAllManeuverNodes();
			vessel.PlaceManeuverNode(this.scriptModule.orbit, dV, UT);
			this.endAction();
		}
		override public  void endAction() {
			base.endAction();
		}

		override public void readModuleConfiguration() {
		}

		override public void writeModuleConfiguration() {
		}

		override public void WindowGUI(int windowID) {
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Intercept target with Hohmann transfer");
			base.postWindowGUI(windowID);
		}
	}
}

