using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleWarpHelper : DisplayModule
    {
        public enum WarpTarget { Periapsis, Apoapsis, Node, SoI, Time, PhaseAngleT, SuicideBurn, AtmosphericEntry }
        static string[] warpTargetStrings = new string[] { "periapsis", "apoapsis", "maneuver node", "SoI transition", "Time", "Phase angle", "suicide burn", "atmospheric entry" };
        [Persistent(pass = (int)Pass.Global)]
        public WarpTarget warpTarget = WarpTarget.Periapsis;

        [Persistent(pass = (int)Pass.Global)]
        public EditableTime leadTime = 0;

        public bool warping = false;

        EditableTime timeOffset = 0;

        double targetUT = 0;

        [Persistent(pass = (int)(Pass.Local|Pass.Type|Pass.Global))]
        EditableDouble phaseAngle = 0;

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Warp to: ", GUILayout.ExpandWidth(false));
            warpTarget = (WarpTarget)GuiUtils.ComboBox.Box((int)warpTarget, warpTargetStrings, this);
            GUILayout.EndHorizontal();

            if (warpTarget == WarpTarget.Time)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Warp for: ", GUILayout.ExpandWidth(true));
                timeOffset.text = GUILayout.TextField(timeOffset.text, GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }
            else if (warpTarget == WarpTarget.PhaseAngleT)
            {
                // I wonder if I should check for target that don't make sense
                if (!core.target.NormalTargetExists)
                    GUILayout.Label("You need a target");
                else
                    GuiUtils.SimpleTextBox("Phase Angle:", phaseAngle, "º", 60);
            }

            GUILayout.BeginHorizontal();

            GuiUtils.SimpleTextBox("Lead time: ", leadTime, "");

            if (warping)
            {
                if (GUILayout.Button("Abort"))
                {
                    warping = false;
                    core.warp.MinimumWarp(true);
                }
            }
            else
            {
                if (GUILayout.Button("Warp")) 
                {
                    warping = true;

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
            }

            GUILayout.EndHorizontal();

            if (warping) GUILayout.Label("Warping to " + (leadTime > 0 ? GuiUtils.TimeToDHMS(leadTime) + " before " : "") + warpTargetStrings[(int)warpTarget] + ".");

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override void OnFixedUpdate()
        {
            if (!warping) return;

            if (warpTarget == WarpTarget.SuicideBurn)
            {
                try
                {
                    targetUT = OrbitExtensions.SuicideBurnCountdown(orbit, vesselState, vessel) + vesselState.time;
                }
                catch
                {
                    warping = false;
                }
            }

            double target = targetUT - leadTime;

            if (target < vesselState.time + 1)
            {
                core.warp.MinimumWarp(true);
                warping = false;
            }
            else
            {
                core.warp.WarpToUT(target);
            }
        }

        public override UnityEngine.GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(50) };
        }

        public override bool isActive()
        {
            return warping;            
        }

        public override string GetName()
        {
            return "Warp Helper";
        }

        public MechJebModuleWarpHelper(MechJebCore core) : base(core) { }
    }
}
