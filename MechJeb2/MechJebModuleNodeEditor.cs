using System;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleNodeEditor : DisplayModule
    {
        private EditableDouble prograde   = 0;
        private EditableDouble radialPlus = 0;
        private EditableDouble normalPlus = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        private EditableDouble progradeDelta = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        private EditableDouble radialPlusDelta = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        private EditableDouble normalPlusDelta = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        private readonly EditableTime timeOffset = 0;

        private ManeuverNode  node;
        private ManeuverGizmo gizmo;

        private enum Snap { PERIAPSIS, APOAPSIS, REL_ASCENDING, REL_DESCENDING, EQ_ASCENDING, EQ_DESCENDING }

        private static readonly int  numSnaps = Enum.GetNames(typeof(Snap)).Length;
        private                 Snap snap     = Snap.PERIAPSIS;

        private readonly string[] snapStrings =
        {
            Localizer.Format("#MechJeb_NodeEd_Snap1"), Localizer.Format("#MechJeb_NodeEd_Snap2"), Localizer.Format("#MechJeb_NodeEd_Snap3"),
            Localizer.Format("#MechJeb_NodeEd_Snap4"), Localizer.Format("#MechJeb_NodeEd_Snap5"), Localizer.Format("#MechJeb_NodeEd_Snap6")
        }; //"periapsis""apoapsis""AN with target""DN with target""equatorial AN""equatorial DN"

        private void GizmoUpdateHandler(Vector3d dV, double UT)
        {
            prograde   = dV.z;
            radialPlus = dV.x;
            normalPlus = dV.y;
        }

        private void MergeNext(int index)
        {
            ManeuverNode cur = Vessel.patchedConicSolver.maneuverNodes[index];
            ManeuverNode next = Vessel.patchedConicSolver.maneuverNodes[index + 1];

            double newUT = (cur.UT + next.UT) / 2;
            cur.UpdateNode(cur.patch.DeltaVToManeuverNodeCoordinates(newUT, cur.WorldDeltaV() + next.WorldDeltaV()), newUT);
            next.RemoveSelf();
        }

        protected override void WindowGUI(int windowID)
        {
            if (Vessel.patchedConicSolver.maneuverNodes.Count == 0)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label1")); //"No maneuver nodes to edit."
                RelativityModeSelectUI();
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            ManeuverNode oldNode = node;

            if (Vessel.patchedConicSolver.maneuverNodes.Count == 1)
            {
                node = Vessel.patchedConicSolver.maneuverNodes[0];
            }
            else
            {
                if (!Vessel.patchedConicSolver.maneuverNodes.Contains(node)) node = Vessel.patchedConicSolver.maneuverNodes[0];

                int nodeIndex = Vessel.patchedConicSolver.maneuverNodes.IndexOf(node);
                int numNodes = Vessel.patchedConicSolver.maneuverNodes.Count;

                nodeIndex = GuiUtils.ArrowSelector(nodeIndex, numNodes, "Maneuver node #" + (nodeIndex + 1));

                node = Vessel.patchedConicSolver.maneuverNodes[nodeIndex];
                if (nodeIndex < numNodes - 1 && GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button1")))
                    MergeNext(nodeIndex); //"Merge next node"
            }

            if (node != oldNode)
            {
                prograde   = node.DeltaV.z;
                radialPlus = node.DeltaV.x;
                normalPlus = node.DeltaV.y;
            }

            if (gizmo != node.attachedGizmo)
            {
                if (gizmo != null) gizmo.OnGizmoUpdated -= GizmoUpdateHandler;
                gizmo = node.attachedGizmo;
                if (gizmo != null) gizmo.OnGizmoUpdated += GizmoUpdateHandler;
            }


            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_NodeEd_Label2"), prograde, "m/s", 60); //"Prograde:"
            if (LimitedRepeatButtoon("-"))
            {
                prograde -= progradeDelta;
                node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }

            progradeDelta.text = GUILayout.TextField(progradeDelta.text, GUILayout.Width(50));
            if (LimitedRepeatButtoon("+"))
            {
                prograde += progradeDelta;
                node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }

            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_NodeEd_Label3"), radialPlus, "m/s", 60); //"Radial+:"
            if (LimitedRepeatButtoon("-"))
            {
                radialPlus -= radialPlusDelta;
                node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }

            radialPlusDelta.text = GUILayout.TextField(radialPlusDelta.text, GUILayout.Width(50));
            if (LimitedRepeatButtoon("+"))
            {
                radialPlus += radialPlusDelta;
                node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }

            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_NodeEd_Label4"), normalPlus, "m/s", 60); //"Normal+:"
            if (LimitedRepeatButtoon("-"))
            {
                normalPlus -= normalPlusDelta;
                node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }

            normalPlusDelta.text = GUILayout.TextField(normalPlusDelta.text, GUILayout.Width(50));
            if (LimitedRepeatButtoon("+"))
            {
                normalPlus += normalPlusDelta;
                node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }

            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label5"), GUILayout.ExpandWidth(true)); //"Set delta to:"
            if (GUILayout.Button("0.01", GUILayout.ExpandWidth(true)))
                progradeDelta = radialPlusDelta = normalPlusDelta = 0.01;
            if (GUILayout.Button("0.1", GUILayout.ExpandWidth(true)))
                progradeDelta = radialPlusDelta = normalPlusDelta = 0.1;
            if (GUILayout.Button("1", GUILayout.ExpandWidth(true)))
                progradeDelta = radialPlusDelta = normalPlusDelta = 1;
            if (GUILayout.Button("10", GUILayout.ExpandWidth(true)))
                progradeDelta = radialPlusDelta = normalPlusDelta = 10;
            if (GUILayout.Button("100", GUILayout.ExpandWidth(true)))
                progradeDelta = radialPlusDelta = normalPlusDelta = 100;
            GUILayout.EndHorizontal();

            if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button2")))
                node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT); //"Update"

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label6"), GUILayout.ExpandWidth(true)); //"Shift time"
            if (GUILayout.Button("-o", GUILayout.ExpandWidth(false)))
            {
                node.UpdateNode(node.DeltaV, node.UT - node.patch.period);
            }

            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                node.UpdateNode(node.DeltaV, node.UT - timeOffset);
            }

            timeOffset.text = GUILayout.TextField(timeOffset.text, GUILayout.Width(100));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                node.UpdateNode(node.DeltaV, node.UT + timeOffset);
            }

            if (GUILayout.Button("+o", GUILayout.ExpandWidth(false)))
            {
                node.UpdateNode(node.DeltaV, node.UT + node.patch.period);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button3"), GUILayout.ExpandWidth(true))) //"Snap node to"
            {
                Orbit o = node.patch;
                double UT = node.UT;
                switch (snap)
                {
                    case Snap.PERIAPSIS:
                        UT = o.NextPeriapsisTime(o.eccentricity < 1 ? UT - o.period / 2 : UT);
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
                        if (Core.Target.NormalTargetExists && Core.Target.TargetOrbit.referenceBody == o.referenceBody)
                        {
                            if (o.AscendingNodeExists(Core.Target.TargetOrbit))
                                UT = o.TimeOfAscendingNode(Core.Target.TargetOrbit, UT - o.period / 2);
                        }

                        break;

                    case Snap.REL_DESCENDING:
                        if (Core.Target.NormalTargetExists && Core.Target.TargetOrbit.referenceBody == o.referenceBody)
                        {
                            if (o.DescendingNodeExists(Core.Target.TargetOrbit))
                                UT = o.TimeOfDescendingNode(Core.Target.TargetOrbit, UT - o.period / 2);
                        }

                        break;
                }

                node.UpdateNode(node.DeltaV, UT);
            }

            snap = (Snap)GuiUtils.ArrowSelector((int)snap, numSnaps, snapStrings[(int)snap]);

            GUILayout.EndHorizontal();

            RelativityModeSelectUI();


            if (Core.Node != null)
            {
                if (Vessel.patchedConicSolver.maneuverNodes.Count > 0 && !Core.Node.Enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button4"))) //"Execute next node"
                    {
                        Core.Node.ExecuteOneNode(this);
                    }

                    if (VesselState.isLoadedPrincipia && GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button7"))) //Execute next Principia node
                    {
                        Core.Node.ExecuteOnePNode(this);
                    }

                    if (Vessel.patchedConicSolver.maneuverNodes.Count > 1)
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button5"))) //"Execute all nodes"
                        {
                            Core.Node.ExecuteAllNodes(this);
                        }
                    }
                }
                else if (Core.Node.Enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button6"))) //"Abort node execution"
                    {
                        Core.Node.Abort();
                    }
                }

                GUILayout.BeginHorizontal();
                Core.Node.Autowarp =
                    GUILayout.Toggle(Core.Node.Autowarp, Localizer.Format("#MechJeb_NodeEd_checkbox1"), GUILayout.ExpandWidth(true)); //"Auto-warp"
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private static float nextClick;

        private static bool LimitedRepeatButtoon(string text)
        {
            if (GUILayout.RepeatButton(text, GUILayout.ExpandWidth(false)) && nextClick < Time.time)
            {
                nextClick = Time.time + 0.2f;
                return true;
            }

            return false;
        }

        private static readonly string[] relativityModeStrings = { "0", "1", "2", "3", "4" };

        private void RelativityModeSelectUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label8"), GUILayout.ExpandWidth(false)); //"Conics mode:"
            int newRelativityMode = GUILayout.SelectionGrid((int)Vessel.patchedConicRenderer.relativityMode, relativityModeStrings, 5);
            Vessel.patchedConicRenderer.relativityMode = (PatchRendering.RelativityMode)newRelativityMode;
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label9",
                Vessel.patchedConicRenderer.relativityMode.ToString())); //"Current mode: <<1>>"

            GUILayout.EndVertical();
        }

        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(150) };

        public override string GetName() => Localizer.Format("#MechJeb_NodeEd_title"); //"Maneuver Node Editor"

        public override string IconName() => "Maneuver Node Editor";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();

        public MechJebModuleNodeEditor(MechJebCore core) : base(core) { }
    }
}
