using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionManoeuver : MechJebModuleScriptAction
    {
        public static String NAME = "Manoeuver";

        private List<Operation> operation;
        private List<String> operationNames;
        private int operationId = 0;
        [Persistent(pass = (int)Pass.Type)]
        private String operationName;


        public MechJebModuleScriptActionManoeuver (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME) {
            Operation[] op_list = Operation.getAvailableOperations();
            operation = new List<Operation>();
            operationNames = new List<String>();
            //Only add the "tested" and "supported" operations
            for (int i = 0; i < op_list.Length; i++)
            {
                if (!op_list[i].getName().ToLower().Contains("advanced transfer") && !op_list[i].getName().ToLower().Contains("transfer to another planet"))
                {
                    operation.Add(op_list[i]);
                    operationNames.Add(op_list[i].getName());
                }
            }
            //operationNames = new List<Operation>(operation).ConvertAll(x => x.getName()).ToArray();
        }

        override public void activateAction() {
            base.activateAction();
            Vessel vessel = FlightGlobals.ActiveVessel;
            double UT = this.scriptModule.vesselState.time;
            Orbit o = this.scriptModule.orbit;
            var nodeList = operation[operationId].MakeNodes(o, UT, core.target);
            if (nodeList != null)
            {
                for (var i = 0; i < nodeList.Count; i++)
                    vessel.PlaceManeuverNode(o, nodeList[i].dV, nodeList[i].UT);
            }

            //if (core.node != null)
            //	core.node.ExecuteOneNode(this);
            this.endAction();
        }
        override public  void endAction() {
            base.endAction();
        }

        override public void postLoad(ConfigNode node)
        {
            for (int i = 0; i < operation.Count; i++) //We select the operation ID based on the operation name. This will help when future operations will be added in the list
            {
                if (operation[i].getName().CompareTo(operationName) == 0)
                {
                    this.operationId = i;
                }
            }
            //Load basic operation config
            ConfigNode.LoadObjectFromConfig(operation[operationId], node.GetNode("Operation"));
            //Load Timeselector
            if (operation[operationId].getName().ToLower().Contains("circularize"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationCircularize)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("apoapsis"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationApoapsis)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("periapsis"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationPeriapsis)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("Pe and Ap"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationEllipticize)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("inclination"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationInclination)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("match velocities"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationKillRelVel)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("intercept target"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationLambert)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("change longitude"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationLan)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("change surface longitude"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationLongitude)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("match planes"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationPlane)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("resonant orbit"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationResonantOrbit)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("semi-major"))
            {
                ConfigNode.LoadObjectFromConfig(((OperationSemiMajor)operation[operationId]).getTimeSelector(), node.GetNode("OperationConfig"));
            }
        }

        override public void postSave(ConfigNode node)
        {
            ConfigNode operationBaseNode = ConfigNode.CreateConfigFromObject(operation[operationId], (int)Pass.Global, null);
            operationBaseNode.CopyTo(node.AddNode("Operation"));
            if (operation[operationId].getName().ToLower().Contains("circularize"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationCircularize)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("apoapsis"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationApoapsis)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("periapsis"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationPeriapsis)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("Pe and Ap"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationEllipticize)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("inclination"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationInclination)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("match velocities"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationKillRelVel)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("intercept target"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationLambert)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("change longitude"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationLan)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("change surface longitude"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationLongitude)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("match planes"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationPlane)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("resonant orbit"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationResonantOrbit)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
            else if (operation[operationId].getName().ToLower().Contains("semi-major"))
            {
                ConfigNode operationNode = ConfigNode.CreateConfigFromObject(((OperationSemiMajor)operation[operationId]).getTimeSelector(), (int)Pass.Global, null);
                operationNode.CopyTo(node.AddNode("OperationConfig"));
            }
        }

        override public void WindowGUI(int windowID) {
            base.preWindowGUI(windowID);
            base.WindowGUI(windowID);
            operationId = GuiUtils.ComboBox.Box(operationId, operationNames.ToArray(), operationNames);
            operationName = operation[operationId].getName(); //Used to save the operation based on the name
            // Compute orbit and universal time parameters for next maneuver
            double UT = this.scriptModule.vesselState.time;
            Orbit o = this.scriptModule.orbit;
            try
            {
                operation[operationId].DoParametersGUI(o, UT, core.target);
            }
            catch (Exception) { } // TODO: Would be better to fix the problem but this will do for now
            base.postWindowGUI(windowID);
            if (operation[operationId].getErrorMessage().Length > 0)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label(operation[operationId].getErrorMessage(), s);
            }
        }
    }
}
