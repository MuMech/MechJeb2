// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

namespace KerbalEngineer.VesselSimulator
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    //using Editor;
    using Helpers;
    using UnityEngine;

    public class EngineSim : Pool<EngineSim>
    {
        public double actualThrust = 0.0;
        public List<AppliedForce> appliedForces = new List<AppliedForce>();
        public bool isActive = false;
        public double isp = 0;
        public PartSim partSim;
        public float maxMach;

        public double thrust = 0;

        // Add thrust vector to account for directional losses
        public Vector3 thrustVec;
        private readonly ResourceContainer resourceConsumptions = new ResourceContainer();

        public EngineSim Initialise(PartSim theEngine,
            double atmosphere,
            float machNumber,
            float maxFuelFlow,
            float minFuelFlow,
            float thrustPercentage,
            Vector3 vecThrust,
            FloatCurve atmosphereCurve,
            bool atmChangeFlow,
            FloatCurve atmCurve,
            FloatCurve velCurve,
            float currentThrottle,
            bool throttleLocked,
            List<Propellant> propellants,
            bool active,
            float resultingThrust,
            List<Transform> thrustTransforms)
        {
            StringBuilder buffer = null;

            isp = 0.0;
            maxMach = 0.0f;
            actualThrust = 0.0;
            partSim = theEngine;
            isActive = active;
            thrustVec = vecThrust;
            resourceConsumptions.Reset();
            appliedForces.Clear();

            double flowRate = 0.0;
            if (partSim.hasVessel)
            {
                float flowModifier = GetFlowModifier(atmChangeFlow, atmCurve, partSim.part.atmDensity, velCurve, machNumber, ref maxMach);
                isp = atmosphereCurve.Evaluate((float)atmosphere);
                thrust = GetThrust(Mathf.Lerp(minFuelFlow, maxFuelFlow, GetThrustPercent(thrustPercentage)) * flowModifier, isp);
                actualThrust = isActive ? resultingThrust : 0.0;

                if (throttleLocked)
                {
                    flowRate = GetFlowRate(thrust, isp);
                }
                else
                {
                    if (currentThrottle > 0.0f && partSim.isLanded == false)
                    {
                        flowRate = GetFlowRate(actualThrust, isp);
                    }
                    else
                    {
                        flowRate = GetFlowRate(thrust, isp);
                    }
                }
            }
            else
            {
                float flowModifier = GetFlowModifier(atmChangeFlow, atmCurve, SimManager.Body.GetDensity(FlightGlobals.getStaticPressure(0, SimManager.Body), FlightGlobals.getExternalTemperature(0, SimManager.Body)), velCurve, machNumber, ref maxMach);
                isp = atmosphereCurve.Evaluate((float)atmosphere);
                thrust = GetThrust(Mathf.Lerp(minFuelFlow, maxFuelFlow, GetThrustPercent(thrustPercentage)) * flowModifier, isp);
                flowRate = GetFlowRate(thrust, isp);
            }

            if (SimManager.logOutput)
            {
                buffer = new StringBuilder(1024);
                buffer.AppendFormat("flowRate = {0:g6}\n", flowRate);
            }

            float flowMass = 0f;
            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];
                flowMass += propellant.ratio * ResourceContainer.GetResourceDensity(propellant.id);
            }

            if (SimManager.logOutput)
            {
                buffer.AppendFormat("flowMass = {0:g6}\n", flowMass);
            }

            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];

                if (propellant.name == "ElectricCharge" || propellant.name == "IntakeAir")
                {
                    continue;
                }

                double consumptionRate = propellant.ratio * flowRate / flowMass;
                if (SimManager.logOutput)
                {
                    buffer.AppendFormat("Add consumption({0}, {1}:{2:d}) = {3:g6}\n", ResourceContainer.GetResourceName(propellant.id), theEngine.name, theEngine.partId, consumptionRate);
                }
                resourceConsumptions.Add(propellant.id, consumptionRate);
            }

            if (SimManager.logOutput)
            {
                MonoBehaviour.print(buffer);
            }

            appliedForces.Clear();
            double thrustPerThrustTransform = thrust / thrustTransforms.Count;
            for (int i = 0; i < thrustTransforms.Count; ++i)
            {
                Transform thrustTransform = thrustTransforms[i];
                Vector3d direction = thrustTransform.forward.normalized;
                Vector3d position = thrustTransform.position;
                appliedForces.Add(new AppliedForce(direction * thrustPerThrustTransform, position));
            }

            return this;
        }

        public ResourceContainer ResourceConsumptions
        {
            get
            {
                return resourceConsumptions;
            }
        }

        public static double GetExhaustVelocity(double isp)
        {
            return isp * Units.GRAVITY;
        }

        public static float GetFlowModifier(bool atmChangeFlow, FloatCurve atmCurve, double atmDensity, FloatCurve velCurve, float machNumber, ref float maxMach)
        {
            float flowModifier = 1.0f;
            if (atmChangeFlow)
            {
                flowModifier = (float)(atmDensity / 1.225);
                if (atmCurve != null)
                {
                    flowModifier = atmCurve.Evaluate(flowModifier);
                }
            }
            if (velCurve != null)
            {
                flowModifier = flowModifier * velCurve.Evaluate(machNumber);
                maxMach = velCurve.maxTime;
            }
            if (flowModifier < float.Epsilon)
            {
                flowModifier = float.Epsilon;
            }
            return flowModifier;
        }

        public static double GetFlowRate(double thrust, double isp)
        {
            return thrust / GetExhaustVelocity(isp);
        }

        public static float GetThrottlePercent(float currentThrottle, float thrustPercentage)
        {
            return currentThrottle * GetThrustPercent(thrustPercentage);
        }

        public static double GetThrust(double flowRate, double isp)
        {
            return flowRate * GetExhaustVelocity(isp);
        }

        public static float GetThrustPercent(float thrustPercentage)
        {
            return thrustPercentage * 0.01f;
        }

        public void DumpEngineToBuffer(StringBuilder buffer, String prefix)
        {
            buffer.Append(prefix);
            buffer.AppendFormat("[thrust = {0:g6}, actual = {1:g6}, isp = {2:g6}\n", thrust, actualThrust, isp);
        }

        public bool SetResourceDrains(List<PartSim> allParts, List<PartSim> allFuelLines, HashSet<PartSim> drainingParts)
        {
            LogMsg log = null;

            // A dictionary to hold a set of parts for each resource
            Dictionary<int, HashSet<PartSim>> sourcePartSets = new Dictionary<int, HashSet<PartSim>>();

            for (int i = 0; i < resourceConsumptions.Types.Count; ++i)
            {
                int type = resourceConsumptions.Types[i];

                HashSet<PartSim> sourcePartSet = null;
                switch (ResourceContainer.GetResourceFlowMode(type))
                {
                    case ResourceFlowMode.NO_FLOW:
                        if (partSim.resources[type] > SimManager.RESOURCE_MIN && partSim.resourceFlowStates[type] != 0)
                        {
                            sourcePartSet = new HashSet<PartSim>();
                            //MonoBehaviour.print("SetResourceDrains(" + name + ":" + partId + ") setting sources to just this");
                            sourcePartSet.Add(partSim);
                        }
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                        for (int j = 0; j < allParts.Count; ++j)
                        {
                            PartSim aPartSim = allParts[j];
                            if (aPartSim.resources[type] > SimManager.RESOURCE_MIN && aPartSim.resourceFlowStates[type] != 0)
                            {
                                if (sourcePartSet == null)
                                {
                                    sourcePartSet = new HashSet<PartSim>();
                                }

                                sourcePartSet.Add(aPartSim);
                            }
                        }
                        break;

                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                        Dictionary<int, HashSet<PartSim>> stagePartSets = new Dictionary<int, HashSet<PartSim>>();
                        int maxStage = -1;

                        //Logger.Log(type);
                        for (int j = 0; j < allParts.Count; ++j)
                        {
                            PartSim aPartSim = allParts[j];
                            if (aPartSim.resources[type] <= SimManager.RESOURCE_MIN || aPartSim.resourceFlowStates[type] == 0)
                            {
                                continue;
                            }

                            int stage = aPartSim.DecouplerCount();
                            if (stage > maxStage)
                            {
                                maxStage = stage;
                            }

                            if (!stagePartSets.TryGetValue(stage, out sourcePartSet))
                            {
                                sourcePartSet = new HashSet<PartSim>();
                                stagePartSets.Add(stage, sourcePartSet);
                            }
                            sourcePartSet.Add(aPartSim);
                        }

                        for (int j = 0; j <= maxStage; j++)
                        {
                            HashSet<PartSim> stagePartSet;
                            if (stagePartSets.TryGetValue(j, out stagePartSet) && stagePartSet.Count > 0)
                            {
                                sourcePartSet = stagePartSet;
                            }
                        }
                        break;

                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        HashSet<PartSim> visited = new HashSet<PartSim>();

                        if (SimManager.logOutput)
                        {
                            log = new LogMsg();
                            log.buf.AppendLine("Find " + ResourceContainer.GetResourceName(type) + " sources for " + partSim.name + ":" + partSim.partId);
                        }
                        sourcePartSet = partSim.GetSourceSet(type, allParts, visited, log, "");
                        if (SimManager.logOutput)
                        {
                            MonoBehaviour.print(log.buf);
                        }
                        break;

                    default:
                        MonoBehaviour.print("SetResourceDrains(" + partSim.name + ":" + partSim.partId + ") Unexpected flow type for " + ResourceContainer.GetResourceName(type) + ")");
                        break;
                }

                if (sourcePartSet != null && sourcePartSet.Count > 0)
                {
                    sourcePartSets[type] = sourcePartSet;
                    if (SimManager.logOutput)
                    {
                        log = new LogMsg();
                        log.buf.AppendLine("Source parts for " + ResourceContainer.GetResourceName(type) + ":");
                        foreach (PartSim partSim in sourcePartSet)
                        {
                            log.buf.AppendLine(partSim.name + ":" + partSim.partId);
                        }
                        MonoBehaviour.print(log.buf);
                    }
                }
            }

            // If we don't have sources for all the needed resources then return false without setting up any drains
            for (int i = 0; i < resourceConsumptions.Types.Count; ++i)
            {
                int type = resourceConsumptions.Types[i];

                if (!sourcePartSets.ContainsKey(type))
                {
                    if (SimManager.logOutput)
                    {
                        MonoBehaviour.print("No source of " + ResourceContainer.GetResourceName(type));
                    }

                    isActive = false;
                    return false;
                }
            }

            // Now we set the drains on the members of the sets and update the draining parts set
            for (int i = 0; i < resourceConsumptions.Types.Count; ++i)
            {
                int type = resourceConsumptions.Types[i];

                HashSet<PartSim> sourcePartSet = sourcePartSets[type];
                // Loop through the members of the set 
                double amount = resourceConsumptions[type] / sourcePartSet.Count;
                foreach (PartSim partSim in sourcePartSet)
                {
                    if (SimManager.logOutput)
                    {
                        MonoBehaviour.print("Adding drain of " + amount + " " + ResourceContainer.GetResourceName(type) + " to " + partSim.name + ":" + partSim.partId);
                    }

                    partSim.resourceDrains.Add(type, amount);
                    drainingParts.Add(partSim);
                }
            }

            return true;
        }
    }
}