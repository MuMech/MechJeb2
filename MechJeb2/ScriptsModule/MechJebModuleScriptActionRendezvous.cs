﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionRendezvous : MechJebModuleScriptAction
	{
		public static String NAME = "Rendezvous";
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		private List<String> actionTypes = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private EditableDoubleMult phasingOrbitAltitude = new EditableDoubleMult(200000, 1000);

		public MechJebModuleScriptActionRendezvous (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			actionTypes.Add("Align Planes");
			actionTypes.Add("Establish new orbit at");
			actionTypes.Add("Intercept with Hohmann transfer");
			actionTypes.Add("Match velocities at closest approach");
			actionTypes.Add("Get closer");

		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			Vessel vessel = this.scriptModule.vessel;
			VesselState vesselState = this.scriptModule.vesselState;
			Orbit orbit = this.scriptModule.orbit;
			CelestialBody mainBody = this.scriptModule.mainBody;

			const double leadTime = 30;
			double closestApproachTime = orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time);

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
				double phasingOrbitRadius = phasingOrbitAltitude + mainBody.Radius;

				vessel.RemoveAllManeuverNodes();
				if (orbit.ApR < phasingOrbitRadius)
				{
					double UT1 = vesselState.time + leadTime;
					Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, UT1, phasingOrbitRadius);
					vessel.PlaceManeuverNode(orbit, dV1, UT1);
					Orbit transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
					double UT2 = transferOrbit.NextApoapsisTime(UT1);
					Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
					vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
				}
				else if (orbit.PeR > phasingOrbitRadius)
				{
					double UT1 = vesselState.time + leadTime;
					Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, UT1, phasingOrbitRadius);
					vessel.PlaceManeuverNode(orbit, dV1, UT1);
					Orbit transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
					double UT2 = transferOrbit.NextPeriapsisTime(UT1);
					Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
					vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
				}
				else
				{
					double UT = orbit.NextTimeOfRadius(vesselState.time, phasingOrbitRadius);
					Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
					vessel.PlaceManeuverNode(orbit, dV, UT);
				}
			}
			else if (actionType == 2) //Intercept with Hohmann transfer
			{
				double UT;
				Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.target.TargetOrbit, vesselState.time, out UT);
				vessel.RemoveAllManeuverNodes();
				vessel.PlaceManeuverNode(orbit, dV, UT);
			}
			else if (actionType == 3) //Match velocities at closest approach
			{
				double UT = closestApproachTime;
				Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.TargetOrbit);
				vessel.RemoveAllManeuverNodes();
				vessel.PlaceManeuverNode(orbit, dV, UT);
			}
			else if (actionType == 4) //Get closer
			{
				double UT = vesselState.time;
				double interceptUT = UT + 100;
				Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.TargetOrbit, interceptUT, 10);
				vessel.RemoveAllManeuverNodes();
				vessel.PlaceManeuverNode(orbit, dV, UT);
			}

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

