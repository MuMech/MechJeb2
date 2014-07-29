using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleManeuverPlanner : DisplayModule
    {
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core)
        {
            operationNames = new List<Operation>(operation).ConvertAll(x => x.getName()).ToArray();
        }

        // Keep all Operation objects so parameters are saved
        Operation[] operation = Operation.getAvailableOperations();
        string[] operationNames;
        int operationId = 0;
        bool operationPopupDisplay;

        // Creation or replacement mode
        bool createNode = true;

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            List<ManeuverNode> maneuverNodes = GetManeuverNodes();
            bool anyNodeExists = GetManeuverNodes().Any();

            if (anyNodeExists)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(createNode ? "Create a new" : "Change the last"))
                {
                    createNode = !createNode;
                }
                GUILayout.Label("maneuver node to:");
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Create a new maneuver node to:");
                createNode = true;
            }

            operationId = GuiUtils.ComboBox(operationId, ref operationPopupDisplay, operationNames);

            if (!operationPopupDisplay)
            {
                // Compute orbit and universal time parameters for next maneuver
                double UT = vesselState.time;
                Orbit o = orbit;
                if (anyNodeExists)
                {
                    if (createNode)
                    {
                        ManeuverNode last = maneuverNodes.Last();
                        UT = last.UT;
                        o = last.nextPatch;
                    }
                    else if (maneuverNodes.Count() > 1)
                    {
                        ManeuverNode last = maneuverNodes[maneuverNodes.Count() - 1];
                        UT = last.UT;
                        o = last.nextPatch;
                    }
                }

                operation[operationId].DoParametersGUI(o, UT, core.target);

                if (anyNodeExists)
                    GUILayout.Label("after the last maneuver node.");

                bool makingNode = false;
                bool executingNode = false;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create node"))
                {
                    makingNode = true;
                    executingNode = false;
                }
                if (core.node != null && GUILayout.Button("Create and execute"))
                {
                    makingNode = true;
                    executingNode = true;
                }
                GUILayout.EndHorizontal();

                if (makingNode)
                {
                    
                    var computedNode = operation[operationId].MakeNode(o, UT, core.target);
                    if (computedNode != null)
                    {
                        if (!createNode)
                            vessel.patchedConicSolver.RemoveManeuverNode(maneuverNodes.Last());
                        vessel.PlaceManeuverNode(o, computedNode.dV, computedNode.UT);
                    }

                    if (executingNode && core.node != null)
                        core.node.ExecuteOneNode(this);
                }

                if (operation[operationId].getErrorMessage().Length > 0)
                {
                    GUIStyle s = new GUIStyle(GUI.skin.label);
                    s.normal.textColor = Color.yellow;
                    GUILayout.Label(operation[operationId].getErrorMessage(), s);
                }

                if (GUILayout.Button("Remove ALL nodes"))
                {
                    vessel.RemoveAllManeuverNodes();
                }

                if (core.node != null)
                {
                    if (anyNodeExists && !core.node.enabled)
                    {
                        if (GUILayout.Button("Execute next node"))
                        {
                            core.node.ExecuteOneNode(this);
                        }

                        if (vessel.patchedConicSolver.maneuverNodes.Count > 1)
                        {
                            if (GUILayout.Button("Execute all nodes"))
                            {
                                core.node.ExecuteAllNodes(this);
                            }
                        }
                    }
                    else if (core.node.enabled)
                    {
                        if (GUILayout.Button("Abort node execution"))
                        {
                            core.node.Abort();
                        }
                    }

                    GUILayout.BeginHorizontal();
                    core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto-warp", GUILayout.ExpandWidth(true));
                    GUILayout.Label("Tolerance:", GUILayout.ExpandWidth(false));
                    core.node.tolerance.text = GUILayout.TextField(core.node.tolerance.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
                    GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public List<ManeuverNode> GetManeuverNodes()
        {
            MechJebModuleLandingPredictions predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
            if (predictor == null) return vessel.patchedConicSolver.maneuverNodes;
            else return vessel.patchedConicSolver.maneuverNodes.Where(n => n != predictor.aerobrakeNode).ToList();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Maneuver Planner";
        }
    }
}
