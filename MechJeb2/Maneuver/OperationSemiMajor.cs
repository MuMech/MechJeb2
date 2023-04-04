using KSP.Localization;
using System.Collections.Generic;
using JetBrains.Annotations;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationSemiMajor : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_Sa_title");}//change semi-major axis

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newSMA = new EditableDoubleMult(800000, 1000);
        private readonly TimeSelector timeSelector;

        public OperationSemiMajor ()
        {
            timeSelector = new TimeSelector(new TimeReference[] {TimeReference.X_FROM_NOW, TimeReference.APOAPSIS, TimeReference.PERIAPSIS});
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox (Localizer.Format("#MechJeb_Sa_label"), newSMA, "km");//New Semi-Major Axis:
            timeSelector.DoChooseTimeGUI();
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (2*newSMA > o.Radius(UT) + o.referenceBody.sphereOfInfluence)
            {
                errorMessage = Localizer.Format("#MechJeb_Sa_errormsg");//Warning: new Semi-Major Axis is very large, and may result in a hyberbolic orbit
            }

            if(o.Radius(UT) > 2*newSMA)
            {
                throw new OperationException(Localizer.Format("#MechJeb_Sa_Exception",o.referenceBody.displayName.LocalizeRemoveGender()) +  "(" + o.referenceBody.Radius.ToSI(3) + "m)");//cannot make Semi-Major Axis less than twice the burn altitude plus the radius of <<1>>
            }

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add(new ManeuverParameters(OrbitalManeuverCalculator.DeltaVForSemiMajorAxis (o, UT, newSMA), UT));

            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
