using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionManoeuver : MechJebModuleScriptAction
    {
        public static string NAME = "Manoeuver";

        private readonly List<Operation> operation;
        private readonly List<string>    operationNames;
        private          int             operationId;

        [Persistent(pass = (int)Pass.Type)]
        private string operationName;

        public MechJebModuleScriptActionManoeuver(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
            Operation[] op_list = Operation.GetAvailableOperations();
            operation      = new List<Operation>();
            operationNames = new List<string>();
            //Only add the "tested" and "supported" operations
            for (int i = 0; i < op_list.Length; i++)
            {
                if (!op_list[i].GetName().ToLower().Contains("advanced transfer") &&
                    !op_list[i].GetName().ToLower().Contains("transfer to another planet"))
                {
                    operation.Add(op_list[i]);
                    operationNames.Add(op_list[i].GetName());
                }
            }
            //operationNames = new List<Operation>(operation).ConvertAll(x => x.getName()).ToArray();
        }

        public override void activateAction()
        {
            base.activateAction();
            Vessel vessel = FlightGlobals.ActiveVessel;
            double UT = scriptModule.vesselState.time;
            Orbit o = scriptModule.orbit;
            List<ManeuverParameters> nodeList = operation[operationId].MakeNodes(o, UT, core.target);
            if (nodeList != null)
            {
                for (int i = 0; i < nodeList.Count; i++)
                    vessel.PlaceManeuverNode(o, nodeList[i].dV, nodeList[i].UT);
            }

            //if (core.node != null)
            //	core.node.ExecuteOneNode(this);
            endAction();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void postLoad(ConfigNode node)
        {
            for (int i = 0;
                 i < operation.Count;
                 i++) //We select the operation ID based on the operation name. This will help when future operations will be added in the list
            {
                if (operation[i].GetName().CompareTo(operationName) == 0)
                {
                    operationId = i;
                }
            }

            //Load basic operation config
            ConfigNode.LoadObjectFromConfig(operation[operationId], node.GetNode("Operation"));
            //Load Timeselector
            if (operation[operationId].GetName().ToLower().Contains("circularize"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationCircularize)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("apoapsis"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationApoapsis)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("periapsis"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationPeriapsis)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("Pe and Ap"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationEllipticize)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("inclination"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationInclination)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("match velocities"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationKillRelVel)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("intercept target"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationLambert)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("change longitude"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationLan)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("change surface longitude"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationLongitude)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("match planes"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationPlane)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("resonant orbit"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationResonantOrbit)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("semi-major"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationSemiMajor)operation[operationId]).GetTimeSelector(), node.GetNode("OperationConfig"));
            }
        }

        public override void postSave(ConfigNode node)
        {
            var operationBaseNode = ConfigNode.CreateConfigFromObject(operation[operationId], (int)Pass.Global, null);
            operationBaseNode.CopyTo(node.AddNode("Operation"));
            if (operation[operationId].GetName().ToLower().Contains("circularize"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationCircularize)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("apoapsis"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationApoapsis)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("periapsis"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationPeriapsis)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("Pe and Ap"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationEllipticize)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("inclination"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationInclination)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("match velocities"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationKillRelVel)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("intercept target"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationLambert)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("change longitude"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationLan)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("change surface longitude"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationLongitude)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("match planes"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationPlane)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("resonant orbit"))
            {
                var operationNode = ConfigNode.CreateConfigFromObject(((OperationResonantOrbit)operation[operationId]).GetTimeSelector(),
                    (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].GetName().ToLower().Contains("semi-major"))
            {
                var operationNode =
                    ConfigNode.CreateConfigFromObject(((OperationSemiMajor)operation[operationId]).GetTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            operationId   = GuiUtils.ComboBox.Box(operationId, operationNames.ToArray(), operationNames);
            operationName = operation[operationId].GetName(); //Used to save the operation based on the name
            // Compute orbit and universal time parameters for next maneuver
            double UT = scriptModule.vesselState.time;
            Orbit o = scriptModule.orbit;
            try
            {
                operation[operationId].DoParametersGUI(o, UT, core.target);
            }
            catch (Exception) { } // TODO: Would be better to fix the problem but this will do for now

            postWindowGUI(windowID);
            if (operation[operationId].GetErrorMessage().Length > 0)
            {
                GUILayout.Label(operation[operationId].GetErrorMessage(), GuiUtils.yellowLabel);
            }
        }
    }
}
