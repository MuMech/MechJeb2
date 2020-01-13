using UnityEngine;
using KSP.Localization;
using System.Collections.Generic;

namespace MuMech
{
    public class OperationMoonReturn : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_return_title");}//return from a moon

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult moonReturnAltitude = new EditableDoubleMult(100000, 1000);

        public OperationMoonReturn ()
        {
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_return_label1"), moonReturnAltitude, "km");//Approximate final periapsis:
            GUILayout.Label(Localizer.Format("#MechJeb_return_label2"));//Schedule the burn at the next return window.
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (o.eccentricity > 0.2)
            {
                errorMessage = Localizer.Format("#MechJeb_return_errormsg", o.eccentricity.ToString("F2"));//"Warning: Recommend starting moon returns from a near-circular orbit (eccentricity < 0.2). Planned return is starting from an orbit with eccentricity "" and so may not be accurate."
            }

            if (o.referenceBody.referenceBody == null)
            {
                throw new OperationException(Localizer.Format("#MechJeb_return_Exception",o.referenceBody.displayName));//<<1>> is not orbiting another body you could return to.
            }

            double UT;
            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, universalTime, o.referenceBody.referenceBody.Radius + moonReturnAltitude, out UT);

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add(new ManeuverParameters(dV, UT));
            return NodeList;
        }
    }
}
