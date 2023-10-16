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
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core) { }

        // Keep all Operation objects so parameters are saved
        private static readonly Operation[] _operation = Operation.GetAvailableOperations();

        private readonly string[] _operationNames =
            new List<Operation>(_operation).ConvertAll(x => x.GetName()).ToArray();

        [Persistent(pass = (int)Pass.GLOBAL)]
        private int _operationId;

        // Creation or replacement mode
        private bool _createNode = true;

        protected override void WindowGUI(int windowID)
        {
            _operationId = Mathf.Clamp(_operationId, 0, _operation.Length - 1);

            GUILayout.BeginVertical();

            List<ManeuverNode> maneuverNodes = GetManeuverNodes();
            bool anyNodeExists = GetManeuverNodes().Any();

            if (anyNodeExists)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_createNode
                        ? Localizer.Format("#MechJeb_Maneu_createNodeBtn01")
                        : Localizer.Format("#MechJeb_Maneu_createNodeBtn02"))) //"Create a new":"Change the last"
                {
                    _createNode = !_createNode;
                }

                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_createlab1")); //"maneuver node to:"
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_createlab2")); //"Create a new maneuver node to:"
                _createNode = true;
            }

            _operationId = GuiUtils.ComboBox.Box(_operationId, _operationNames, this);

            // Compute orbit and universal time parameters for next maneuver
            double UT = VesselState.time;
            Orbit o = Orbit;
            if (anyNodeExists)
            {
                if (_createNode)
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
                _operation[_operationId].DoParametersGUI(o, UT, Core.Target);
            }
            catch (Exception) { } // TODO: Would be better to fix the problem but this will do for now

            if (anyNodeExists)
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_createlab3")); //"after the last maneuver node."

            bool makingNode = false;
            bool executingNode = false;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button1"))) //"Create node"
            {
                makingNode = true;
            }

            if (Core.Node != null && GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button2"))) //"Create and execute"
            {
                makingNode    = true;
                executingNode = true;
            }

            GUILayout.EndHorizontal();

            if (makingNode)
            {
                List<ManeuverParameters> nodeList = _operation[_operationId].MakeNodes(o, UT, Core.Target);
                if (nodeList != null)
                {
                    if (!_createNode)
                        maneuverNodes.Last().RemoveSelf();
                    for (int i = 0; i < nodeList.Count; i++)
                    {
                        Vessel.PlaceManeuverNode(o, nodeList[i].dV, nodeList[i].UT);
                    }
                }

                if (executingNode && Core.Node != null)
                    Core.Node.ExecuteOneNode(this);
            }

            if (_operation[_operationId].GetErrorMessage().Length > 0)
            {
                GUILayout.Label(_operation[_operationId].GetErrorMessage(), GuiUtils.yellowLabel);
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button3"))) //Remove ALL nodes
            {
                Vessel.RemoveAllManeuverNodes();
            }

            if (Core.Node != null)
            {
                if (anyNodeExists && !Core.Node.Enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button4"))) //Execute next node
                    {
                        Core.Node.ExecuteOneNode(this);
                    }

                    if (VesselState.isLoadedPrincipia && GUILayout.Button(Localizer.Format("#MechJeb_NodeEd_button7"))) //Execute next Principia node
                    {
                        Core.Node.ExecuteOnePNode(this);
                    }

                    if (Vessel.patchedConicSolver.maneuverNodes.Count > 1)
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button5"))) //Execute all nodes
                        {
                            Core.Node.ExecuteAllNodes(this);
                        }
                    }
                }
                else if (Core.Node.Enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Maneu_button6"))) //Abort node execution
                    {
                        Core.Node.Abort();
                    }
                }

                GUILayout.BeginHorizontal();
                Core.Node.autowarp =
                    GUILayout.Toggle(Core.Node.autowarp, Localizer.Format("#MechJeb_Maneu_Autowarp"), GUILayout.ExpandWidth(true)); //"Auto-warp"

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_Tolerance"), GUILayout.ExpandWidth(false)); //"Tolerance:"
                Core.Node.tolerance.text = GUILayout.TextField(Core.Node.tolerance.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    Core.Node.tolerance.val += 0.1;
                }

                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    Core.Node.tolerance.val -= Core.Node.tolerance.val > 0.1 ? 0.1 : 0.0;
                }

                if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
                {
                    Core.Node.tolerance.val = 0.1;
                }

                GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#MechJeb_Maneu_Lead_time"), GUILayout.ExpandWidth(false)); //Lead time:
                Core.Node.leadTime.text = GUILayout.TextField(Core.Node.leadTime.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    Core.Node.leadTime.val += 1;
                }

                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    Core.Node.leadTime.val -= 1;
                }

                if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
                {
                    Core.Node.leadTime.val = 3;
                }

                GUILayout.Label("s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID, _operation[_operationId].Draggable);
        }

        public List<ManeuverNode> GetManeuverNodes()
        {
            MechJebModuleLandingPredictions predictor = Core.GetComputerModule<MechJebModuleLandingPredictions>();
            if (predictor == null) return Vessel.patchedConicSolver.maneuverNodes;
            return Vessel.patchedConicSolver.maneuverNodes.Where(n => n != predictor.aerobrakeNode).ToList();
        }

        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(150) };

        public override string GetName() => Localizer.Format("#MechJeb_Maneuver_Planner_title"); //Maneuver Planner

        public override string IconName() => "Maneuver Planner";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
