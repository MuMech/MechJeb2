using UnityEngine;
using KSP.Localization;
using System.Collections.Generic;
namespace MuMech
{
    public class OperationGeneric : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_Hohm_title");}//bi-impulsive (Hohmann) transfer to target

        public bool intercept_only = false;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble periodOffset = 0;
        [Persistent(pass = (int)Pass.Global)]
        public EditableTime MinDepartureUT = 0;
        [Persistent(pass = (int)Pass.Global)]
        public EditableTime MaxDepartureUT = 0;
        [Persistent(pass = (int)Pass.Global)]
        public bool simpleTransfer = false;

        private TimeSelector timeSelector;

        public OperationGeneric ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.COMPUTED, TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE, TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING, TimeReference.REL_NEAREST_AD, TimeReference.REL_ASCENDING, TimeReference.REL_DESCENDING, TimeReference.CLOSEST_APPROACH });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            intercept_only = GUILayout.Toggle(intercept_only, Localizer.Format("#MechJeb_Hohm_intercept_only"));//intercept only, no capture burn (impact/flyby)
            simpleTransfer = GUILayout.Toggle(simpleTransfer, Localizer.Format("#MechJeb_Hohm_simpleTransfer"));//simple coplanar Hohmann transfer
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Hohm_Label1"), periodOffset);//fractional target period offset
            if (!simpleTransfer)
            {
                timeSelector.DoChooseTimeGUI();
            }
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = 0;

            if (!target.NormalTargetExists)
            {
                throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception1"));//must select a target for the bi-impulsive transfer.
            }
            else if (o.referenceBody != target.TargetOrbit.referenceBody)
            {
                throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception2"));//target for bi-impulsive transfer must be in the same sphere of influence.
            }

            Vector3d dV;

            Orbit targetOrbit = target.TargetOrbit;

            if ( periodOffset != 0 )
            {
                targetOrbit = target.TargetOrbit.Clone();
                targetOrbit.MutatedOrbit(periodOffset: periodOffset);
            }

            if (simpleTransfer)
            {
                dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targetOrbit, universalTime, out UT);
            }
            else
            {
                if (timeSelector.timeReference == TimeReference.COMPUTED)
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(o, targetOrbit, universalTime, out UT, intercept_only: intercept_only);
                }
                else
                {
                    var anExists = o.AscendingNodeExists(target.TargetOrbit);
                    var dnExists = o.DescendingNodeExists(target.TargetOrbit);
                    double anTime = o.TimeOfAscendingNode(target.TargetOrbit, universalTime);
                    double dnTime = o.TimeOfDescendingNode(target.TargetOrbit, universalTime);

                    if(timeSelector.timeReference == TimeReference.REL_ASCENDING)
                    {
                        if(!anExists)
                        {
                            throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception3"));//ascending node with target doesn't exist.
                        }
                        UT = anTime;
                    }
                    else if(timeSelector.timeReference == TimeReference.REL_DESCENDING)
                    {
                        if(!dnExists)
                        {
                            throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception4"));//descending node with target doesn't exist.
                        }
                        UT = dnTime;
                    }
                    else if(timeSelector.timeReference == TimeReference.REL_NEAREST_AD)
                    {
                        if(!anExists && !dnExists)
                        {
                            throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception5"));//neither ascending nor descending node with target exists.
                        }
                        if(!dnExists || anTime <= dnTime)
                        {
                            UT = anTime;
                        }
                        else
                        {
                            UT = dnTime;
                        }
                    }

                    UT = timeSelector.ComputeManeuverTime(o, universalTime, target);
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(o, targetOrbit, UT, out UT, intercept_only: intercept_only, fixed_ut: true);
                }
            }

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add( new ManeuverParameters(dV, UT) );
            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
