using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleManeuverPlanner : DisplayModule
    {
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core)
        {
            operationNames = new List<Operation>(operation).ConvertAll(x => x.GetName()).ToArray();
        }

        // Keep all Operation objects so parameters are saved
        private readonly Operation[] operation = Operation.GetAvailableOperations();
        private readonly string[]    operationNames;

        [Persistent(pass = (int)Pass.Global)]
        private int operationId;

        // Creation or replacement mode
        private bool createNode = true;

        protected override void WindowGUI(int windowID)
        {
            operationId = Mathf.Clamp(operationId, 0, operation.Length - 1);

            GUILayout.BeginVertical();

            List<ManeuverNode> maneuverNodes = GetManeuverNodes();
            bool anyNodeExists = GetManeuverNodes().Any();

            if (anyNodeExists)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(createNode
                        ? Localizer.Format("#MechJeb_Maneu_createNodeBtn01")
                        : Localizer.Format("#MechJeb_Maneu_createNodeBtn02"))) //"Create a new":"Change the last"
                {
                    createNode = !createNode;
                }

                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_createlab1")); //"maneuver node to:"
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_createlab2")); //"Create a new maneuver node to:"
                createNode = true;
            }

            operationId = GuiUtils.ComboBox.Box(operationId, operationNames, this);

            // Compute orbit and universal time parameters for next maneuver
            double UT = vesselState.time;
            Orbit o = orbit;
            if (anyNodeExists)
            {
                if (createNode)
                {
                    ManeuverNode last = maneuverNodes.Last();
                    UT = last.UT;
                    o  = last.nextPatch;
                }
                else if (maneuverNodes.Count > 1)
                {
                    ManeuverNode last = maneuverNodes[maneuverNodes.Count - 1];
                    UT = last.UT;
                    o  = last.nextPatch;
                }
            }

            try
            {
                operation[operationId].DoParametersGUI(o, UT, core.Target);
            }
            catch (Exception) { } // TODO: Would be better to fix the problem but this will do for now

            if (anyNodeExists)
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_createlab3")); //"after the last maneuver node."

            bool makingNode = false;
            bool executingNode = false;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button1"))) //"Create node"
            {
                makingNode    = true;
                executingNode = false;
            }

            if (core.Node != null && GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button2"))) //"Create and execute"
            {
                makingNode    = true;
                executingNode = true;
            }

            GUILayout.EndHorizontal();

            if (makingNode)
            {
                List<ManeuverParameters> nodeList = operation[operationId].MakeNodes(o, UT, core.Target);
                if (nodeList != null)
                {
                    if (!createNode)
                        maneuverNodes.Last().RemoveSelf();
                    for (int i = 0; i < nodeList.Count; i++)
                    {
                        vessel.PlaceManeuverNode(o, nodeList[i].dV, nodeList[i].UT);
                    }
                }

                if (executingNode && core.Node != null)
                    core.Node.ExecuteOneNode(this);
            }

            if (operation[operationId].GetErrorMessage().Length > 0)
            {
                GUILayout.Label(operation[operationId].GetErrorMessage(), GuiUtils.yellowLabel);
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button3"))) //Remove ALL nodes
            {
                vessel.RemoveAllManeuverNodes();
            }

            if (core.Node != null)
            {
                if (anyNodeExists && !core.Node.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button4"))) //Execute next node
                    {
                        core.Node.ExecuteOneNode(this);
                    }

                    if (VesselState.isLoadedPrincipia && GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button7"))) //Execute next Principia node
                    {
                        core.Node.ExecuteOnePNode(this);
                    }

                    if (vessel.patchedConicSolver.maneuverNodes.Count > 1)
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button5"))) //Execute all nodes
                        {
                            core.Node.ExecuteAllNodes(this);
                        }
                    }
                }
                else if (core.Node.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button6"))) //Abort node execution
                    {
                        core.Node.Abort();
                    }
                }

                GUILayout.BeginHorizontal();
                core.Node.autowarp =
                    GUILayout.Toggle(core.Node.autowarp, Localizer.Format("#MechJeb_Maneu_Autowarp"), GUILayout.ExpandWidth(true)); //"Auto-warp"

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_Tolerance"), GUILayout.ExpandWidth(false)); //"Tolerance:"
                core.Node.tolerance.text = GUILayout.TextField(core.Node.tolerance.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    core.Node.tolerance.val += 0.1;
                }

                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    core.Node.tolerance.val -= core.Node.tolerance.val > 0.1 ? 0.1 : 0.0;
                }

                if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
                {
                    core.Node.tolerance.val = 0.1;
                }

                GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_Lead_time"), GUILayout.ExpandWidth(false)); //Lead time:
                core.Node.leadTime.text = GUILayout.TextField(core.Node.leadTime.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    core.Node.leadTime.val += 1;
                }

                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    core.Node.leadTime.val -= 1;
                }

                if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
                {
                    core.Node.leadTime.val = 3;
                }

                GUILayout.Label("s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID, operation[operationId].Draggable);
        }

        public List<ManeuverNode> GetManeuverNodes()
        {
            MechJebModuleLandingPredictions predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
            if (predictor == null) return vessel.patchedConicSolver.maneuverNodes;
            return vessel.patchedConicSolver.maneuverNodes.Where(n => n != predictor.aerobrakeNode).ToList();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_Maneuver_Planner_title"); //Maneuver Planner
        }

        public override string IconName()
        {
            return "Maneuver Planner";
        }

        public override bool IsSpaceCenterUpgradeUnlocked()
        {
            return vessel.patchedConicsUnlocked();
        }
    }
}
