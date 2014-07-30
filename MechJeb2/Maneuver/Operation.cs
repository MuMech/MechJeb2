using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    public abstract class Operation
    {
        public abstract string getName();
        protected string errorMessage = "";
        public string getErrorMessage() { return errorMessage;}

        public abstract void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target);

        public ManeuverParameters MakeNode(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            errorMessage = "";
            try
            {
                return MakeNodeImpl(o, universalTime, target);
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return null;
            }
        }

        public abstract ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target);

        private static List<Type> operations = new List<Type>();

        static Operation()
        {
            var baseType = typeof(Operation);
            Assembly assembly = Assembly.GetAssembly(baseType);
            operations = assembly.GetTypes().Where(
                t => !t.IsAbstract
                && baseType.IsAssignableFrom(t)
                && t.GetConstructor(Type.EmptyTypes) != null
            ).ToList();

        }

        public static Operation[] getAvailableOperations()
        {
            var res = operations.ConvertAll(t => (Operation)t.GetConstructor(Type.EmptyTypes).Invoke(null));
            res.Sort((x,y) => x.getName().CompareTo(y.getName()));
            return res.ToArray();
        }

    }
}

