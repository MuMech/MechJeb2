using System;
using System.Linq;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionWarp : MechJebModuleScriptAction
	{
		public static String NAME = "Warp";

		public enum WarpTarget { Periapsis, Apoapsis, Node, SoI, Time, PhaseAngleT, SuicideBurn, AtmosphericEntry }
		static string[] warpTargetStrings = new string[] { "periapsis", "apoapsis", "maneuver node", "SoI transition", "Time", "Phase angle", "suicide burn", "atmospheric entry" };
		[Persistent(pass = (int)Pass.Type)]
		public WarpTarget warpTarget = WarpTarget.Periapsis;
		[Persistent(pass = (int)Pass.Type)]
		EditableDouble phaseAngle = 0;
		[Persistent(pass = (int)Pass.Type)]
		public EditableTime leadTime = 0;
		[Persistent(pass = (int)Pass.Type)]
		EditableTime timeOffset = 0;
		double targetUT = 0;
		private bool warping;
		private int spendTime = 0;
		private int initTime = 5; //Add a 5s timer after the action to allow time for physics to update before next action
		private float startTime = 0f;

		public MechJebModuleScriptActionWarp (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			warping = true;
			Orbit orbit = this.scriptModule.orbit;
			VesselState vesselState = this.scriptModule.vesselState;
			Vessel vessel = FlightGlobals.ActiveVessel;

			switch (warpTarget)
			{
				case WarpTarget.Periapsis:
					targetUT = orbit.NextPeriapsisTime(vesselState.time);
					break;

				case WarpTarget.Apoapsis:
					if (orbit.eccentricity < 1) targetUT = orbit.NextApoapsisTime(vesselState.time);
					break;

				case WarpTarget.SoI:
					if (orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) targetUT = orbit.EndUT;
					break;

				case WarpTarget.Node:
					if (vessel.patchedConicsUnlocked() && vessel.patchedConicSolver.maneuverNodes.Any()) targetUT = vessel.patchedConicSolver.maneuverNodes[0].UT;
					break;

				case WarpTarget.Time:
					targetUT = vesselState.time + timeOffset;
					break;

				case WarpTarget.PhaseAngleT:
					if (core.target.NormalTargetExists)
					{
						Orbit reference;
						if (core.target.TargetOrbit.referenceBody == orbit.referenceBody)
							reference = orbit; // we orbit arround the same body
						else
							reference = orbit.referenceBody.orbit;
						// From Kerbal Alarm Clock
						double angleChangePerSec = (360 / core.target.TargetOrbit.period) - (360 / reference.period);
						double currentAngle = reference.PhaseAngle(core.target.TargetOrbit, vesselState.time);
						double angleDigff = currentAngle - phaseAngle;
						if (angleDigff > 0 && angleChangePerSec > 0)
							angleDigff -= 360;
						if (angleDigff < 0 && angleChangePerSec < 0)
							angleDigff += 360;
						double TimeToTarget = Math.Floor(Math.Abs(angleDigff / angleChangePerSec));
						targetUT = vesselState.time + TimeToTarget;
					}
					break;

				case WarpTarget.AtmosphericEntry:
					try
					{
						targetUT = OrbitExtensions.NextTimeOfRadius(vessel.orbit, vesselState.time, vesselState.mainBody.Radius + vesselState.mainBody.RealMaxAtmosphereAltitude());
					}
					catch
					{
						warping = false;
					}
					break;

				case WarpTarget.SuicideBurn:
					try
					{
						targetUT = OrbitExtensions.SuicideBurnCountdown(orbit, vesselState, vessel) + vesselState.time;
					}
					catch
					{
						warping = false;
					}
					break;

				default:
					targetUT = vesselState.time;
					break;
			}
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);

			GUILayout.Label("Warp to: ", GUILayout.ExpandWidth(false));
			warpTarget = (WarpTarget)GuiUtils.ComboBox.Box((int)warpTarget, warpTargetStrings, warpTargetStrings);

			if (warpTarget == WarpTarget.Time)
			{
				GUILayout.Label("Warp for: ", GUILayout.ExpandWidth(true));
				timeOffset.text = GUILayout.TextField(timeOffset.text, GUILayout.Width(100));
			}
			else if (warpTarget == WarpTarget.PhaseAngleT)
			{
				// I wonder if I should check for target that don't make sense
				if (!core.target.NormalTargetExists)
					GUILayout.Label("You need a target");
				else
					GuiUtils.SimpleTextBox("Phase Angle:", phaseAngle, "º", 60);
			}

			if (!warping)
			{
				GuiUtils.SimpleTextBox("Lead time: ", leadTime, "");
			}

			if (warping)
			{
				if (GUILayout.Button("Abort"))
				{
					this.onAbord();
				}
			}

			if (warping) GUILayout.Label("Warping to " + (leadTime > 0 ? GuiUtils.TimeToDHMS(leadTime) + " before " : "") + warpTargetStrings[(int)warpTarget] + ".");

			if (this.isStarted() && !this.isExecuted() && this.startTime > 0)
			{
				GUILayout.Label(" waiting " + this.spendTime + "s");
			}

			base.postWindowGUI(windowID);
		}

		override public void afterOnFixedUpdate()
		{
			//Check the end of the action
			if (this.isStarted() && !this.isExecuted() && !warping && startTime == 0f)
			{
				startTime = Time.time;
			}
			if (this.isStarted() && !this.isExecuted() && startTime > 0)
			{
				this.spendTime = initTime - (int)(Math.Round(Time.time - startTime)); //Add the end action timer
				if (this.spendTime <= 0)
				{
					this.endAction();
				}
			}

			if (!warping) return;

			if (warpTarget == WarpTarget.SuicideBurn)
			{
				try
				{
					targetUT = OrbitExtensions.SuicideBurnCountdown(this.scriptModule.orbit, this.scriptModule.vesselState, this.scriptModule.vessel) + this.scriptModule.vesselState.time;
				}
				catch
				{
					warping = false;
				}
			}

			double target = targetUT - leadTime;

			if (target < this.scriptModule.vesselState.time + 1)
			{
				core.warp.MinimumWarp(true);
				warping = false;
			}
			else
			{
				core.warp.WarpToUT(target);
			}
		}

		override public void onAbord()
		{
			warping = false;
			core.warp.MinimumWarp(true);
			base.onAbord();
		}
	}
}

