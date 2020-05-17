using System;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleNodeEditor : DisplayModule
    {
        EditableDouble prograde = 0;
        EditableDouble radialPlus = 0;
        EditableDouble normalPlus = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableDouble progradeDelta = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableDouble radialPlusDelta = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableDouble normalPlusDelta = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableTime timeOffset = 0;

        ManeuverNode node;
        ManeuverGizmo gizmo;

        enum Snap { PERIAPSIS, APOAPSIS, REL_ASCENDING, REL_DESCENDING, EQ_ASCENDING, EQ_DESCENDING };
        static int numSnaps = Enum.GetNames(typeof(Snap)).Length;
        Snap snap = Snap.PERIAPSIS;
        string[] snapStrings = new string[] { Localizer.Format("#MechJeb_NodeEd_Snap1"), Localizer.Format("#MechJeb_NodeEd_Snap2"), Localizer.Format("#MechJeb_NodeEd_Snap3"), Localizer.Format("#MechJeb_NodeEd_Snap4"), Localizer.Format("#MechJeb_NodeEd_Snap5"), Localizer.Format("#MechJeb_NodeEd_Snap6") };//"periapsis""apoapsis""AN with target""DN with target""equatorial AN""equatorial DN"

        void GizmoUpdateHandler(Vector3d dV, double UT)
        {
            prograde = dV.z;
            radialPlus = dV.x;
            normalPlus = dV.y;
        }

        void MergeNext(int index)
        {
            ManeuverNode cur = vessel.patchedConicSolver.maneuverNodes[index];
            ManeuverNode next = vessel.patchedConicSolver.maneuverNodes[index+1];

            double newUT = (cur.UT + next.UT) / 2;
            cur.UpdateNode(cur.patch.DeltaVToManeuverNodeCoordinates(newUT, cur.WorldDeltaV() + next.WorldDeltaV()), newUT);
            next.RemoveSelf();
        }

        protected override void WindowGUI(int windowID)
        {
            if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label1"));//"No maneuver nodes to edit."
                RelativityModeSelectUI();
                base.WindowGUI(windowID);
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
                if (!vessel.patchedConicSolver.maneuverNodes.Contains(node)) node = vessel.patchedConicSolver.maneuverNodes[0];

                int nodeIndex = vessel.patchedConicSolver.maneuverNodes.IndexOf(node);
                int numNodes = vessel.patchedConicSolver.maneuverNodes.Count;

                nodeIndex = GuiUtils.ArrowSelector(nodeIndex, numNodes, "Maneuver node #" + (nodeIndex + 1));

                node = vessel.patchedConicSolver.maneuverNodes[nodeIndex];
                if (nodeIndex < (numNodes-1) && GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button1"))) MergeNext(nodeIndex);//"Merge next node"
            }

            if (node != oldNode)
            {
                prograde = node.DeltaV.z;
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
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_NodeEd_Label2"), prograde, "m/s", 60);//"Prograde:"
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
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_NodeEd_Label3"), radialPlus, "m/s", 60);//"Radial+:"
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
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_NodeEd_Label4"), normalPlus, "m/s", 60);//"Normal+:"
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
            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label5"), GUILayout.ExpandWidth(true));//"Set delta to:"
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

            if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button2"))) node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);//"Update"

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label6"), GUILayout.ExpandWidth(true));//"Shift time"
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
            if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button3"), GUILayout.ExpandWidth(true)))//"Snap node to"
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
                        if (core.target.NormalTargetExists && core.target.TargetOrbit.referenceBody == o.referenceBody)
                        {
                            if (o.AscendingNodeExists(core.target.TargetOrbit)) UT = o.TimeOfAscendingNode(core.target.TargetOrbit, UT - o.period / 2);
                        }
                        break;

                    case Snap.REL_DESCENDING:
                        if (core.target.NormalTargetExists && core.target.TargetOrbit.referenceBody == o.referenceBody)
                        {
                            if (o.DescendingNodeExists(core.target.TargetOrbit)) UT = o.TimeOfDescendingNode(core.target.TargetOrbit, UT - o.period / 2);
                        }
                        break;
                }
                node.UpdateNode(node.DeltaV, UT);
            }

            snap = (Snap)GuiUtils.ArrowSelector((int)snap, numSnaps, snapStrings[(int)snap]);

            GUILayout.EndHorizontal();

            RelativityModeSelectUI();


            if (core.node != null)
            {
                if (vessel.patchedConicSolver.maneuverNodes.Count > 0 && !core.node.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button4")))//"Execute next node"
                    {
                        core.node.ExecuteOneNode(this);
                    }

                    if (MechJebModuleGuidanceController.isLoadedPrincipia && GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button7")))//Execute next Principia node
                    {
                        core.node.ExecuteOnePNode(this);
                    }

                    if (vessel.patchedConicSolver.maneuverNodes.Count > 1)
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button5")))//"Execute all nodes"
                        {
                            core.node.ExecuteAllNodes(this);
                        }
                    }
                }
                else if (core.node.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button6")))//"Abort node execution"
                    {
                        core.node.Abort();
                    }
                }

                GUILayout.BeginHorizontal();
                core.node.autowarp = GUILayout.Toggle(core.node.autowarp, Localizer.Format("#MechJeb_NodeEd_checkbox1"), GUILayout.ExpandWidth(true));//"Auto-warp"
                GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label7"), GUILayout.ExpandWidth(false));//"Tolerance:"
                core.node.tolerance.text = GUILayout.TextField(core.node.tolerance.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
                GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private static float nextClick = 0;

        private static bool LimitedRepeatButtoon(string text)
        {
            if (GUILayout.RepeatButton(text, GUILayout.ExpandWidth(false)) && nextClick < Time.time)
            {
                nextClick = Time.time + 0.2f;
                return true;
            }
            return false;
        }

        static readonly string[] relativityModeStrings = { "0", "1", "2", "3", "4" };
        private void RelativityModeSelectUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label8"), GUILayout.ExpandWidth(false));//"Conics mode:"
            int newRelativityMode = GUILayout.SelectionGrid((int)vessel.patchedConicRenderer.relativityMode, relativityModeStrings, 5);
            vessel.patchedConicRenderer.relativityMode = (PatchRendering.RelativityMode)newRelativityMode;
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format("#MechJeb_NodeEd_Label9", vessel.patchedConicRenderer.relativityMode.ToString()));//"Current mode: <<1>>"

            GUILayout.EndVertical();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_NodeEd_title");//"Maneuver Node Editor"
        }

        public override string IconName()
        {
            return "Maneuver Node Editor";
        }

        public override bool IsSpaceCenterUpgradeUnlocked()
        {
            return vessel.patchedConicsUnlocked();
        }

        public MechJebModuleNodeEditor(MechJebCore core) : base(core) { }
    }
}
