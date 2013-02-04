using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleNodeEditor : DisplayModule
    {
        EditableDouble prograde = 0;
        EditableDouble radialPlus = 0;
        EditableDouble normalPlus = 0;
        EditableDouble progradeDelta = 0;
        EditableDouble radialPlusDelta = 0;
        EditableDouble normalPlusDelta = 0;
        EditableTime timeOffset = 0;

        ManeuverNode node;
        ManeuverGizmo gizmo;

        enum Snap { PERIAPSIS, APOAPSIS, REL_ASCENDING, REL_DESCENDING, EQ_ASCENDING, EQ_DESCENDING };
        static int numSnaps = Enum.GetNames(typeof(Snap)).Length;
        Snap snap = Snap.PERIAPSIS;
        string[] snapStrings = new string[] { "periapsis", "apoapsis", "AN with target", "DN with target", "equatorial AN", "equatorial DN" };

        void GizmoUpdateHandler(Vector3d dV, double UT)
        {
            prograde = dV.z;
            radialPlus = dV.x;
            normalPlus = dV.y;
        }

        protected override void FlightWindowGUI(int windowID)
        {
            if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
            {
                GUILayout.Label("No maneuver nodes to edit.");
                GUI.DragWindow();
                return;
            }

            GUILayout.BeginVertical();

            ManeuverNode oldNode = node;

            if (vessel.patchedConicSolver.maneuverNodes.Count == 1)
            {
                node = vessel.patchedConicSolver.maneuverNodes[0];
            }
            else
            {
                if(!vessel.patchedConicSolver.maneuverNodes.Contains(node)) node = vessel.patchedConicSolver.maneuverNodes[0];

                int nodeIndex = vessel.patchedConicSolver.maneuverNodes.IndexOf(node);
                int numNodes = vessel.patchedConicSolver.maneuverNodes.Count;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("◀", GUILayout.ExpandWidth(false))) nodeIndex = (nodeIndex - 1 + numNodes) % numNodes;
                GUILayout.Label("Maneuver node #" + (nodeIndex + 1));
                if (GUILayout.Button("▶", GUILayout.ExpandWidth(false))) nodeIndex = (nodeIndex + 1) % numNodes;
                GUILayout.EndHorizontal();

                node = vessel.patchedConicSolver.maneuverNodes[nodeIndex];
            }

            if (node != oldNode)
            {
                prograde = node.DeltaV.z;
                radialPlus = node.DeltaV.x;
                normalPlus = -node.DeltaV.y;
            }

            if (gizmo != node.attachedGizmo)
            {
                if (gizmo != null) gizmo.OnGizmoUpdated -= GizmoUpdateHandler;
                gizmo = node.attachedGizmo;
                if (gizmo != null) gizmo.OnGizmoUpdated += GizmoUpdateHandler;
            }

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Prograde:", prograde, "m/s", 60);
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
            {
                prograde += progradeDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            progradeDelta.text = GUILayout.TextField(progradeDelta.text, GUILayout.Width(50));
            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Radial+:", radialPlus, "m/s", 60);
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
            {
                radialPlus += radialPlusDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            radialPlusDelta.text = GUILayout.TextField(radialPlusDelta.text, GUILayout.Width(50));
            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Normal+:", normalPlus, "m/s", 60);
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
            {
                normalPlus += normalPlusDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            normalPlusDelta.text = GUILayout.TextField(normalPlusDelta.text, GUILayout.Width(50));
            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Update")) node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Shift time by", GUILayout.ExpandWidth(true)))
            {
                node.OnGizmoUpdated(node.DeltaV, node.UT + timeOffset);
            }
            timeOffset.text = GUILayout.TextField(timeOffset.text, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Snap node to", GUILayout.ExpandWidth(true)))
            {
                Orbit o = node.patch;
                double UT = node.UT;
                switch (snap)
                {
                    case Snap.PERIAPSIS:
                        UT = o.NextPeriapsisTime(UT - o.period / 2); //period is who-knows-what for e > 1, but this should still work
                        break;

                    case Snap.APOAPSIS:
                        if (o.eccentricity < 1) UT = o.NextApoapsisTime(UT - o.period / 2);
                        break;

                    case Snap.EQ_ASCENDING:
                        if (o.AscendingNodeEquatorialExists()) UT = o.TimeOfAscendingNodeEquatorial(UT - o.period / 2);
                        break;

                    case Snap.EQ_DESCENDING:
                        if (o.DescendingNodeEquatorialExists()) UT = o.TimeOfDescendingNodeEquatorial(UT - o.period / 2);
                        break;

                    case Snap.REL_ASCENDING:
                        if (core.target.NormalTargetExists && core.target.Orbit.referenceBody == o.referenceBody)
                        {
                            if (o.AscendingNodeExists(core.target.Orbit)) UT = o.TimeOfAscendingNode(core.target.Orbit, UT - o.period / 2);
                        }
                        break;

                    case Snap.REL_DESCENDING:
                        if (core.target.NormalTargetExists && core.target.Orbit.referenceBody == o.referenceBody)
                        {
                            if (o.DescendingNodeExists(core.target.Orbit)) UT = o.TimeOfDescendingNode(core.target.Orbit, UT - o.period / 2);
                        }
                        break;
                }
                node.OnGizmoUpdated(node.DeltaV, UT);
            }

            if (GUILayout.Button("◀", GUILayout.ExpandWidth(false))) snap = (Snap)(((int)snap - 1 + numSnaps) % numSnaps);
            GUILayout.Label(snapStrings[(int)snap], GUILayout.Width(100));
            if (GUILayout.Button("▶", GUILayout.ExpandWidth(false))) snap = (Snap)(((int)snap + 1) % numSnaps);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Maneuver Node Editor";
        }

        public MechJebModuleNodeEditor(MechJebCore core) : base(core) { }
    }
}
