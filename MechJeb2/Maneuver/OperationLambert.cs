using KSP.Localization;
using System.Collections.Generic;
namespace MuMech
{
    public class OperationLambert : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_intercept_title");}//intercept target at chosen time

        [Persistent(pass = (int)Pass.Global)]
        public EditableTime interceptInterval = 3600;
        private TimeSelector timeSelector;

        public OperationLambert ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_intercept_label"), interceptInterval);//Time after burn to intercept target:
            timeSelector.DoChooseTimeGUI();
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_intercept_Exception1"));//must select a target to intercept.
            if (o.referenceBody != target.TargetOrbit.referenceBody)
                throw new OperationException(Localizer.Format("#MechJeb_intercept_Exception2"));//target must be in the same sphere of influence.

            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            var dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(o, UT, target.TargetOrbit, UT + interceptInterval);

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add(new ManeuverParameters(dV, UT));
            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
