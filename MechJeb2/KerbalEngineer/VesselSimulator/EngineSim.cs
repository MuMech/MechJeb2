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

    public class EngineSim
    {
        private static readonly Pool<EngineSim> pool = new Pool<EngineSim>(Create, Reset);

        private readonly ResourceContainer resourceConsumptions = new ResourceContainer();
        private readonly ResourceContainer resourceFlowModes = new ResourceContainer();

        public double actualThrust = 0;
        public bool isActive = false;
        public double isp = 0;
        public PartSim partSim;
        public List<AppliedForce> appliedForces = new List<AppliedForce>();
        public float maxMach;

        public double thrust = 0;

        // Add thrust vector to account for directional losses
        public Vector3 thrustVec;

        private static EngineSim Create()
        {
            return new EngineSim();
        }

        private static void Reset(EngineSim engineSim)
        {
            engineSim.resourceConsumptions.Reset();
            engineSim.resourceFlowModes.Reset();
            engineSim.actualThrust = 0;
            engineSim.isActive = false;
            engineSim.isp = 0;
            for (int i = 0; i < engineSim.appliedForces.Count; i++)
            {
                engineSim.appliedForces[i].Release();
            }
            engineSim.appliedForces.Clear();
            engineSim.thrust = 0;
            engineSim.maxMach = 0f;
        }

        public void Release()
        {
            pool.Release(this);
        }

        public static EngineSim New(PartSim theEngine,
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
                         float IspG,
                         bool throttleLocked,
                         List<Propellant> propellants,
                         bool active,
                         float resultingThrust,
                         List<Transform> thrustTransforms,
                        LogMsg log)
        {
            EngineSim engineSim = pool.Borrow();

            engineSim.isp = 0.0;
            engineSim.maxMach = 0.0f;
            engineSim.actualThrust = 0.0;
            engineSim.partSim = theEngine;
            engineSim.isActive = active;
            engineSim.thrustVec = vecThrust;
            engineSim.resourceConsumptions.Reset();
            engineSim.resourceFlowModes.Reset();
            engineSim.appliedForces.Clear();

            double flowRate = 0.0;
            if (engineSim.partSim.hasVessel)
            {
                if (log != null) log.buf.AppendLine("hasVessel is true"); 

                float flowModifier = GetFlowModifier(atmChangeFlow, atmCurve, engineSim.partSim.part.atmDensity, velCurve, machNumber, ref engineSim.maxMach);
                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(Mathf.Lerp(minFuelFlow, maxFuelFlow, GetThrustPercent(thrustPercentage)) * flowModifier, engineSim.isp);
                engineSim.actualThrust = engineSim.isActive ? resultingThrust : 0.0;
                if (log != null)
                {
                    log.buf.AppendFormat("flowMod = {0:g6}\n", flowModifier);
                    log.buf.AppendFormat("isp     = {0:g6}\n", engineSim.isp);
                    log.buf.AppendFormat("thrust  = {0:g6}\n", engineSim.thrust);
                    log.buf.AppendFormat("actual  = {0:g6}\n", engineSim.actualThrust);
                }

                if (throttleLocked)
                {
                    if (log != null) log.buf.AppendLine("throttleLocked is true, using thrust for flowRate");
                    flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
                }
                else
                {
                    if (currentThrottle > 0.0f && engineSim.partSim.isLanded == false)
                    {
                        if (log != null) log.buf.AppendLine("throttled up and not landed, using actualThrust for flowRate");
                        flowRate = GetFlowRate(engineSim.actualThrust, engineSim.isp);
                    }
                    else
                    {
                        if (log != null) log.buf.AppendLine("throttled down or landed, using thrust for flowRate");
                        flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
                    }
                }
            }
            else
            {
                if (log != null) log.buf.AppendLine("hasVessel is false");
                float flowModifier = GetFlowModifier(atmChangeFlow, atmCurve, SimManager.Body.GetDensity(FlightGlobals.getStaticPressure(0, SimManager.Body), FlightGlobals.getExternalTemperature(0, SimManager.Body)), velCurve, machNumber, ref engineSim.maxMach);
                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(Mathf.Lerp(minFuelFlow, maxFuelFlow, GetThrustPercent(thrustPercentage)) * flowModifier, engineSim.isp);
                engineSim.actualThrust = 0d;
                if (log != null)
                {
                    log.buf.AppendFormat("flowMod = {0:g6}\n", flowModifier);
                    log.buf.AppendFormat("isp     = {0:g6}\n", engineSim.isp);
                    log.buf.AppendFormat("thrust  = {0:g6}\n", engineSim.thrust);
                    log.buf.AppendFormat("actual  = {0:g6}\n", engineSim.actualThrust);
                }

                if (log != null) log.buf.AppendLine("no vessel, using thrust for flowRate");
                flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
            }

            if (log != null) log.buf.AppendFormat("flowRate = {0:g6}\n", flowRate);

            float flowMass = 0f;
            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];
                if (!propellant.ignoreForIsp)
                    flowMass += propellant.ratio * ResourceContainer.GetResourceDensity(propellant.id);
            }

            if (log != null) log.buf.AppendFormat("flowMass = {0:g6}\n", flowMass);

            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];

                if (propellant.name == "ElectricCharge" || propellant.name == "IntakeAir")
                {
                    continue;
                }

                double consumptionRate = propellant.ratio * flowRate / flowMass;
                if (log != null) log.buf.AppendFormat(
                        "Add consumption({0}, {1}:{2:d}) = {3:g6}\n",
                        ResourceContainer.GetResourceName(propellant.id),
                        theEngine.name,
                        theEngine.partId,
                        consumptionRate);
                engineSim.resourceConsumptions.Add(propellant.id, consumptionRate);
                engineSim.resourceFlowModes.Add(propellant.id, (double)propellant.GetFlowMode());
            }

            double thrustPerThrustTransform = engineSim.thrust / thrustTransforms.Count;
            for (int i = 0; i < thrustTransforms.Count; i++)
            {
                Transform thrustTransform = thrustTransforms[i];
                Vector3d direction = thrustTransform.forward.normalized;
                Vector3d position = thrustTransform.position;

                AppliedForce appliedForce = AppliedForce.New(direction * thrustPerThrustTransform, position);
                engineSim.appliedForces.Add(appliedForce);
            }

            return engineSim;
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

        // A dictionary to hold a set of parts for each resource
        Dictionary<int, HashSet<PartSim>> sourcePartSets = new Dictionary<int, HashSet<PartSim>>();

        Dictionary<int, HashSet<PartSim>> stagePartSets = new Dictionary<int, HashSet<PartSim>>();

        HashSet<PartSim> visited = new HashSet<PartSim>();

        public bool SetResourceDrains(List<PartSim> allParts, List<PartSim> allFuelLines, HashSet<PartSim> drainingParts)
        {
            LogMsg log = null;
            
            foreach (HashSet<PartSim> sourcePartSet in sourcePartSets.Values)
            {
                sourcePartSet.Clear();
            }

            for (int index = 0; index < this.resourceConsumptions.Types.Count; index++)
            {
                int type = this.resourceConsumptions.Types[index];

                HashSet<PartSim> sourcePartSet;
                if (!sourcePartSets.TryGetValue(type, out sourcePartSet))
                {
                    sourcePartSet = new HashSet<PartSim>();
                    sourcePartSets.Add(type, sourcePartSet);
                }

                switch ((ResourceFlowMode)this.resourceFlowModes[type])
                {
                    case ResourceFlowMode.NO_FLOW:
                        if (partSim.resources[type] > SimManager.RESOURCE_MIN && partSim.resourceFlowStates[type] != 0)
                        {
                            //sourcePartSet = new HashSet<PartSim>();
                            //MonoBehaviour.print("SetResourceDrains(" + name + ":" + partId + ") setting sources to just this");
                            sourcePartSet.Add(partSim);
                        }
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                        for (int i = 0; i < allParts.Count; i++)
                        {
                            PartSim aPartSim = allParts[i];
                            if (aPartSim.resources[type] > SimManager.RESOURCE_MIN && aPartSim.resourceFlowStates[type] != 0)
                            {
                                sourcePartSet.Add(aPartSim);
                            }
                        }
                        break;

                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:

                        foreach (HashSet<PartSim> stagePartSet in stagePartSets.Values)
                        {
                            stagePartSet.Clear();
                        }
                        var maxStage = -1;

                        //Logger.Log(type);
                        for (int i = 0; i < allParts.Count; i++)
                        {
                            var aPartSim = allParts[i];
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
                        visited.Clear();

                        if (SimManager.logOutput)
                        {
                            log = new LogMsg();
                            log.buf.AppendLine("Find " + ResourceContainer.GetResourceName(type) + " sources for " + partSim.name + ":" + partSim.partId);
                        }
                        partSim.GetSourceSet(type, allParts, visited, sourcePartSet, log, "");
                        if (SimManager.logOutput)
                        {
                            MonoBehaviour.print(log.buf);
                        }
                        break;

                    default:
                        MonoBehaviour.print("SetResourceDrains(" + partSim.name + ":" + partSim.partId + ") Unexpected flow type for " + ResourceContainer.GetResourceName(type) + ")");
                        break;
                }


                if (sourcePartSet.Count > 0)
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
            for (int i = 0; i < this.resourceConsumptions.Types.Count; i++)
            {
                int type = this.resourceConsumptions.Types[i];
                HashSet<PartSim> sourcePartSet; 
                if (!sourcePartSets.TryGetValue(type, out sourcePartSet) || sourcePartSet.Count == 0)
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
            for (int i = 0; i < this.resourceConsumptions.Types.Count; i++)
            {
                int type = this.resourceConsumptions.Types[i];
                HashSet<PartSim> sourcePartSet = sourcePartSets[type];
                // Loop through the members of the set 
                double amount = resourceConsumptions[type] / sourcePartSet.Count;
                foreach (PartSim partSim in sourcePartSet)
                {
                    if (SimManager.logOutput)
                    {
                        MonoBehaviour.print(
                            "Adding drain of " + amount + " " + ResourceContainer.GetResourceName(type) + " to " + partSim.name + ":" +
                            partSim.partId);
                    }

                    partSim.resourceDrains.Add(type, amount);
                    drainingParts.Add(partSim);
                }
            }
            return true;
        }
    }
}