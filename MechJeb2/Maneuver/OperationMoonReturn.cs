using System;
using UnityEngine;

namespace MuMech
{
    public class OperationMoonReturn : Operation
    {
        public override string getName() { return "return from a moon";}

        [Persistent(pass = (int)Pass.Global)]
            public EditableDoubleMult moonReturnAltitude = new EditableDoubleMult(100000, 1000);

        public OperationMoonReturn ()
        {
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox("Approximate final periapsis:", moonReturnAltitude, "km");
            GUILayout.Label("Schedule the burn at the next return window.");
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (o.eccentricity > 0.2)
            {
                errorMessage = "Warning: Recommend starting moon returns from a near-circular orbit (eccentricity < 0.2). Planned return is starting from an orbit with eccentricity " + o.eccentricity.ToString("F2") + " and so may not be accurate.";
            }

            if (o.referenceBody.referenceBody == null)
            {
                throw new OperationException(o.referenceBody.theName + " is not orbiting another body you could return to.");
            }

            double UT;
            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, universalTime, o.referenceBody.referenceBody.Radius + moonReturnAltitude, out UT);

            return new ManeuverParameters(dV, UT);
        }
    }
}

