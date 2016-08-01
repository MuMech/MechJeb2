﻿namespace MuMech
{
    public class OperationPlane : Operation
    {
        public override string getName() { return "match planes with target";}

        private TimeSelector timeSelector;

        public OperationPlane ()
        {
            timeSelector = new TimeSelector(new TimeReference[]
            {
                TimeReference.REL_HIGHEST_AD, TimeReference.REL_NEAREST_AD,
                TimeReference.REL_ASCENDING, TimeReference.REL_DESCENDING
            });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (!target.NormalTargetExists)
            {
                throw new OperationException("must select a target to match planes with.");
            }
            else if (o.referenceBody != target.TargetOrbit.referenceBody)
            {
                throw new OperationException("can only match planes with an object in the same sphere of influence.");
            }

            var anExists = o.AscendingNodeExists(target.TargetOrbit);
            var dnExists = o.DescendingNodeExists(target.TargetOrbit);
            double anTime = 0;
            double dnTime = 0;
            var anDeltaV = anExists ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(o, target.TargetOrbit, UT, out anTime) : Vector3d.zero;
            var dnDeltaV = anExists ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(o, target.TargetOrbit, UT, out dnTime) : Vector3d.zero;
            Vector3d dV;

            if(timeSelector.timeReference == TimeReference.REL_ASCENDING)
            {
                if(!anExists)
                {
                    throw new OperationException("ascending node with target doesn't exist.");
                }
                UT = anTime;
                dV = anDeltaV;
            }
            else if(timeSelector.timeReference == TimeReference.REL_DESCENDING)
            {
                if(!dnExists)
                {
                    throw new OperationException("descending node with target doesn't exist.");
                }
                UT = dnTime;
                dV = dnDeltaV;
            }
            else if(timeSelector.timeReference == TimeReference.REL_NEAREST_AD)
            {
                if(!anExists && !dnExists)
                {
                    throw new OperationException("neither ascending nor descending node with target exists.");
                }
                if(!dnExists || anTime <= dnTime)
                {
                    UT = anTime;
                    dV = anDeltaV;
                }
                else
                {
                    UT = dnTime;
                    dV = dnDeltaV;
                }
            }
            else if(timeSelector.timeReference == TimeReference.REL_HIGHEST_AD)
            {
                if(!anExists && !dnExists)
                {
                    throw new OperationException("neither ascending nor descending node with target exists.");
                }
                if(!dnExists || anDeltaV.magnitude <= dnDeltaV.magnitude)
                {
                    UT = anTime;
                    dV = anDeltaV;
                }
                else
                {
                    UT = dnTime;
                    dV = dnDeltaV;
                }
            }
            else
            {
                throw new OperationException("wrong time reference.");
            }

            return new ManeuverParameters(dV, UT);
        }
    }
}

