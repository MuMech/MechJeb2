using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MuMech
{

    public class ManeuverParameters
    {
        public Vector3d dV;
        public double UT;
        public ManeuverParameters(Vector3d dV, double UT)
        {
            this.dV = dV;
            this.UT = UT;
        }

    }

    public class OperationException : Exception
    {
        public OperationException(string message) : base(message) {}
    }

    public abstract class Operation
    {
        protected string errorMessage = "";
        public string getErrorMessage() { return errorMessage;}

        // Methods that need to be implemented for new operations:

        // Description that will be displayed on the GUI
        public abstract string getName();
        // Draw the parameter part of the Operation (ask for time, altitudes etc)
        // Input parameters are orbit and time parameters after the last maneuver and current target
        public abstract void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target);
        // Function called when create node is pressed; input parameters are orbit and time parameters after the last maneuver and current target
        // ManeuverParameters contain a single time and dV describing the node that should be executed
        // In case of error you can throw an OperationException, the message will be displayed and no node will be created.
        public abstract ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target);

        public ManeuverParameters MakeNode(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            errorMessage = "";
            try
            {
                return MakeNodeImpl(o, universalTime, target);
            }
            catch (OperationException e)
            {
                errorMessage = e.Message;
                return null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                errorMessage = "An error occurred while creating the node.";
                return null;
            }
        }

        private static List<Type> operations = new List<Type>();

        private static void addTypes(Type[] types)
        {
            foreach(var t in types)
            {
                if (t != null &&
                    !t.IsAbstract
                    && typeof(Operation).IsAssignableFrom(t)
                    && t.GetConstructor(Type.EmptyTypes) != null)
                    operations.Add(t);
            }
        }

        public static Operation[] getAvailableOperations()
        {
            // Use reflection to discover all classes that inherit from Operation and have a defalt constructor
            if (operations.Count == 0)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        addTypes(assembly.GetTypes());
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        addTypes(e.Types);
                    }
                }
                Debug.Log("ManeuverPlanner initialization: found " + operations.Count + " maneuvers");
            }

            var res = operations.ConvertAll(t => (Operation)t.GetConstructor(Type.EmptyTypes).Invoke(null));
            res.Sort((x,y) => x.getName().CompareTo(y.getName()));
            return res.ToArray();
        }

		public virtual bool draggable { get { return true;}}
    }
}

