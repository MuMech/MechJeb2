using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleWarpHelper : DisplayModule
    {

        enum WarpTarget { Periapsis, Apoapsis, Node, SoI }
        string[] warpTargetStrings = new string[] { "periapsis", "apoapsis", "maneuver node", "SoI transition" };
        static readonly int numWarpTargets = Enum.GetNames(typeof(WarpTarget)).Length;
        WarpTarget warpTarget = WarpTarget.Periapsis;

        EditableTime leadTime = 0;

        bool warping = false;

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Warp to: ", GUILayout.ExpandWidth(false));
            warpTarget = (WarpTarget)GuiUtils.ArrowSelector((int)warpTarget, numWarpTargets, warpTargetStrings[(int)warpTarget]);
            GUILayout.EndHorizontal();

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
                if (GUILayout.Button("Warp")) warping = true;
            }

            GUILayout.EndHorizontal();

            if(warping) GUILayout.Label("Warping to " + (leadTime > 0 ? GuiUtils.TimeToDHMS(leadTime) + " before " : "") + warpTargetStrings[(int)warpTarget] + ".");

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public override void OnFixedUpdate()
        {
            if (!warping) return;

            double targetUT = vesselState.time;

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
                    if (vessel.patchedConicSolver.maneuverNodes.Any()) targetUT = vessel.patchedConicSolver.maneuverNodes[0].UT;
                    break;
            }

            targetUT -= leadTime;

            if (targetUT < vesselState.time + 1)
            {
                core.warp.MinimumWarp(true);
                warping = false;
            }
            else
            {
                core.warp.WarpToUT(targetUT);
            }
        }

        public override UnityEngine.GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(50) };
        }

        public override string GetName()
        {
            return "Warp Helper";
        }

        public MechJebModuleWarpHelper(MechJebCore core) : base(core) { }
    }
}
