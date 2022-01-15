using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionRendezvous : MechJebModuleScriptAction
	{
		public static string NAME = "Rendezvous";
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		private readonly List<string> actionTypes = new List<string>();
		[Persistent(pass = (int)Pass.Type)]
		private readonly EditableDoubleMult phasingOrbitAltitude = new EditableDoubleMult(200000, 1000);

		public MechJebModuleScriptActionRendezvous (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actionTypes.Add("Align Planes");
			actionTypes.Add("Establish new orbit at");
			actionTypes.Add("Intercept with Hohmann transfer");
			actionTypes.Add("Match velocities at closest approach");
			actionTypes.Add("Get closer");

		}

		public override void activateAction()
		{
			base.activateAction();
			var vessel = this.scriptModule.vessel;
			var vesselState = this.scriptModule.vesselState;
			var orbit = this.scriptModule.orbit;
			var mainBody = this.scriptModule.mainBody;

			const double leadTime = 30;
			var closestApproachTime = orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time);

			if (actionType == 0) //Align planes
			{
				double UT;
				Vector3d dV;
				if (orbit.AscendingNodeExists(core.target.TargetOrbit))
				{
					dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, core.target.TargetOrbit, vesselState.time, out UT);
				}
				else
				{
					dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, core.target.TargetOrbit, vesselState.time, out UT);
				}
				vessel.RemoveAllManeuverNodes();
				vessel.PlaceManeuverNode(this.scriptModule.orbit, dV, UT);
			}
			else if (actionType == 1) //Establish new orbit
			{
				var phasingOrbitRadius = phasingOrbitAltitude + mainBody.Radius;

				vessel.RemoveAllManeuverNodes();
				if (orbit.ApR < phasingOrbitRadius)
				{
					var UT1 = vesselState.time + leadTime;
					var dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, UT1, phasingOrbitRadius);
					vessel.PlaceManeuverNode(orbit, dV1, UT1);
					var transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
					var UT2 = transferOrbit.NextApoapsisTime(UT1);
					var dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
					vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
				}
				else if (orbit.PeR > phasingOrbitRadius)
				{
					var UT1 = vesselState.time + leadTime;
					var dV1 = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, UT1, phasingOrbitRadius);
					vessel.PlaceManeuverNode(orbit, dV1, UT1);
					var transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
					var UT2 = transferOrbit.NextPeriapsisTime(UT1);
					var dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
					vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
				}
				else
				{
					var UT = orbit.NextTimeOfRadius(vesselState.time, phasingOrbitRadius);
					var dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
					vessel.PlaceManeuverNode(orbit, dV, UT);
				}
			}
			else if (actionType == 2) //Intercept with Hohmann transfer
			{
                var dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.target.TargetOrbit, vesselState.time, out var UT);
                vessel.RemoveAllManeuverNodes();
				vessel.PlaceManeuverNode(orbit, dV, UT);
			}
			else if (actionType == 3) //Match velocities at closest approach
			{
				var UT = closestApproachTime;
				var dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.TargetOrbit);
				vessel.RemoveAllManeuverNodes();
				vessel.PlaceManeuverNode(orbit, dV, UT);
			}
			else if (actionType == 4) //Get closer
			{
				var UT = vesselState.time;
				var interceptUT = UT + 100;
				var dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.TargetOrbit, interceptUT, 10);
				vessel.RemoveAllManeuverNodes();
				vessel.PlaceManeuverNode(orbit, dV, UT);
			}

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
			GUILayout.Label ("Rendezvous");
			actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			if (actionType == 1) //Establish new orbit
			{
				GUILayout.Label("at");
				phasingOrbitAltitude.text = GUILayout.TextField(phasingOrbitAltitude.text, GUILayout.Width(70));
				GUILayout.Label("km", GUILayout.ExpandWidth(false));
			}
			base.postWindowGUI(windowID);
		}
	}
}

