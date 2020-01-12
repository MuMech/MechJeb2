using UnityEngine;
using KSP.Localization;
using System.Collections.Generic;

namespace MuMech
{
    public class OperationInterplanetaryTransfer : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_transfer_title");}//transfer to another planet

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        private bool waitForPhaseAngle = true;

        public OperationInterplanetaryTransfer ()
        {
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GUILayout.Label(Localizer.Format("#MechJeb_transfer_Label1"));//Schedule the burn:
            waitForPhaseAngle = GUILayout.Toggle(waitForPhaseAngle, Localizer.Format("#MechJeb_transfer_Label2"));//at the next transfer window.
            waitForPhaseAngle = !GUILayout.Toggle(!waitForPhaseAngle,Localizer.Format ("#MechJeb_transfer_Label3"));//as soon as possible

            if (!waitForPhaseAngle)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
                GUILayout.Label(Localizer.Format("#MechJeb_transfer_Label4"), s);//Using this mode voids your warranty
            }
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double UT, MechJebModuleTargetController target)
        {

            // Check preconditions
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_transfer_Exception1"));//"must select a target for the interplanetary transfer."

            if (o.referenceBody.referenceBody == null)
                throw new OperationException(Localizer.Format("#MechJeb_transfer_Exception2",o.referenceBody.displayName));//doesn't make sense to plot an interplanetary transfer from an orbit around <<1>>

            if (o.referenceBody.referenceBody != target.TargetOrbit.referenceBody)
            {
                if (o.referenceBody == target.TargetOrbit.referenceBody)
                    throw new OperationException(Localizer.Format("#MechJeb_transfer_Exception3", o.referenceBody.displayName));//use regular Hohmann transfer function to intercept another body orbiting <<1>>
                throw new OperationException(Localizer.Format("#MechJeb_transfer_Exception4", o.referenceBody.displayName, o.referenceBody.displayName, o.referenceBody.referenceBody.displayName));//"an interplanetary transfer from within "<<1>>"'s sphere of influence must target a body that orbits "<<2>>"'s parent, "<<3>>.
            }

            // Simple warnings
            if (o.referenceBody.orbit.RelativeInclination(target.TargetOrbit) > 30)
            {
                errorMessage = Localizer.Format("#MechJeb_transfer_errormsg1", o.RelativeInclination(target.TargetOrbit).ToString("F0"), o.referenceBody.displayName);//"Warning: target's orbital plane is at a"<<1>>"º angle to "<<2>>"'s orbital plane (recommend at most 30º). Planned interplanetary transfer may not intercept target properly."
            }
            else
            {
                double relativeInclination = Vector3d.Angle(o.SwappedOrbitNormal(), o.referenceBody.orbit.SwappedOrbitNormal());
                if (relativeInclination > 10)
                {
                    errorMessage = Localizer.Format("#MechJeb_transfer_errormsg2", o.referenceBody.displayName, o.referenceBody.displayName, o.referenceBody.referenceBody.displayName, o.referenceBody.displayName, relativeInclination.ToString("F1"), o.referenceBody.displayName, o.referenceBody.referenceBody.displayName);//Warning: Recommend starting interplanetary transfers from  <<1>> from an orbit in the same plane as "<<2>>"'s orbit around "<<3>>". Starting orbit around "<<4>>" is inclined "<<5>>"º with respect to "<<6>>"'s orbit around "<<7>> " (recommend < 10º). Planned transfer may not intercept target properly."
                }
                else if (o.eccentricity > 0.2)
                {
                    errorMessage = Localizer.Format("#MechJeb_transfer_errormsg3",o.eccentricity.ToString("F2"));//Warning: Recommend starting interplanetary transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity <<1>> and so may not intercept target properly.
                }
            }

            var dV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(o, UT, target.TargetOrbit, waitForPhaseAngle, out UT);
            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add( new ManeuverParameters(dV, UT) );
            return NodeList;
        }
    }
}
