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

#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

#endregion

namespace KerbalEngineer.VesselSimulator
{
    public class EngineSim
    {
        private readonly ResourceContainer resourceConsumptions = new ResourceContainer();

        public double actualThrust = 0;
        public bool isActive = false;
        public double isp = 0;
        public PartSim partSim;
        public List<AppliedForce> appliedForces;

        public double thrust = 0;

        // Add thrust vector to account for directional losses
        public Vector3 thrustVec;

        public EngineSim(PartSim theEngine,
                         double atmosphere,
                         double velocity,
                         float maxThrust,
                         float minThrust,
                         float thrustPercentage,
                         float requestedThrust,
                         Vector3 vecThrust,
                         float realIsp,
                         FloatCurve atmosphereCurve,
                         FloatCurve velocityCurve,
                         bool throttleLocked,
                         List<Propellant> propellants,
                         bool active,
                         bool correctThrust,
                         List<Transform> thrustTransforms)
        {
            StringBuilder buffer = null;
            //MonoBehaviour.print("Create EngineSim for " + theEngine.name);
            //MonoBehaviour.print("maxThrust = " + maxThrust);
            //MonoBehaviour.print("minThrust = " + minThrust);
            //MonoBehaviour.print("thrustPercentage = " + thrustPercentage);
            //MonoBehaviour.print("requestedThrust = " + requestedThrust);
            //MonoBehaviour.print("velocity = " + velocity);

            this.partSim = theEngine;

            this.isActive = active;
            this.thrust = (maxThrust - minThrust) * (thrustPercentage / 100f) + minThrust;
            //MonoBehaviour.print("thrust = " + thrust);

            this.thrustVec = vecThrust;

            double flowRate = 0d;
            if (this.partSim.hasVessel)
            {
                //MonoBehaviour.print("hasVessel is true");
                this.actualThrust = isActive ? requestedThrust : 0.0;
                if (velocityCurve != null)
                {
                    this.actualThrust *= velocityCurve.Evaluate((float)velocity);
                    //MonoBehaviour.print("actualThrust at velocity = " + actualThrust);
                }

                this.isp = atmosphereCurve.Evaluate((float)this.partSim.part.staticPressureAtm);
                if (this.isp == 0d)
                {
                    MonoBehaviour.print("Isp at " + this.partSim.part.staticPressureAtm + " is zero. Flow rate will be NaN");
                }

                if (correctThrust && realIsp == 0)
                {
                    float ispsl = atmosphereCurve.Evaluate(0);
                    if (ispsl != 0)
                    {
                        this.thrust = this.thrust * this.isp / ispsl;
                    }
                    else
                    {
                        MonoBehaviour.print("Isp at sea level is zero. Unable to correct thrust.");
                    }
                    //MonoBehaviour.print("corrected thrust = " + thrust);
                }

                if (velocityCurve != null)
                {
                    this.thrust *= velocityCurve.Evaluate((float)velocity);
                    //MonoBehaviour.print("thrust at velocity = " + thrust);
                }

                if (throttleLocked)
                {
                    //MonoBehaviour.print("throttleLocked is true");
                    flowRate = this.thrust / (this.isp * 9.82);
                }
                else
                {
                    if (this.partSim.isLanded)
                    {
                        //MonoBehaviour.print("partSim.isLanded is true, mainThrottle = " + FlightInputHandler.state.mainThrottle);
                        flowRate = Math.Max(0.000001d, this.thrust * FlightInputHandler.state.mainThrottle) / (this.isp * 9.82);
                    }
                    else
                    {
                        if (requestedThrust > 0)
                        {
                            if (velocityCurve != null)
                            {
                                requestedThrust *= velocityCurve.Evaluate((float)velocity);
                                //MonoBehaviour.print("requestedThrust at velocity = " + requestedThrust);
                            }

                            //MonoBehaviour.print("requestedThrust > 0");
                            flowRate = requestedThrust / (this.isp * 9.82);
                        }
                        else
                        {
                            //MonoBehaviour.print("requestedThrust <= 0");
                            flowRate = this.thrust / (this.isp * 9.82);
                        }
                    }
                }
            }
            else
            {
                //MonoBehaviour.print("hasVessel is false");
                this.isp = atmosphereCurve.Evaluate((float)atmosphere);
                if (this.isp == 0d)
                {
                    MonoBehaviour.print("Isp at " + atmosphere + " is zero. Flow rate will be NaN");
                }
                if (correctThrust)
                {
                    float ispsl = atmosphereCurve.Evaluate(0);
                    if (ispsl != 0)
                    {
                        this.thrust = this.thrust * this.isp / ispsl;
                    }
                    else
                    {
                        MonoBehaviour.print("Isp at sea level is zero. Unable to correct thrust.");
                    }
                    //MonoBehaviour.print("corrected thrust = " + thrust);
                }

                if (velocityCurve != null)
                {
                    this.thrust *= velocityCurve.Evaluate((float)velocity);
                    //MonoBehaviour.print("thrust at velocity = " + thrust);
                }

                flowRate = this.thrust / (this.isp * 9.82);
            }

            if (SimManager.logOutput)
            {
                buffer = new StringBuilder(1024);
                buffer.AppendFormat("flowRate = {0:g6}\n", flowRate);
            }

            float flowMass = 0f;
            foreach (Propellant propellant in propellants)
            {
                flowMass += propellant.ratio * ResourceContainer.GetResourceDensity(propellant.id);
            }

            if (SimManager.logOutput)
            {
                buffer.AppendFormat("flowMass = {0:g6}\n", flowMass);
            }

            foreach (Propellant propellant in propellants)
            {
                if (propellant.name == "ElectricCharge" || propellant.name == "IntakeAir")
                {
                    continue;
                }

                double consumptionRate = propellant.ratio * flowRate / flowMass;
                if (SimManager.logOutput)
                {
                    buffer.AppendFormat("Add consumption({0}, {1}:{2:d}) = {3:g6}\n", ResourceContainer.GetResourceName(propellant.id), theEngine.name, theEngine.partId, consumptionRate);
                }
                this.resourceConsumptions.Add(propellant.id, consumptionRate);
            }

            if (SimManager.logOutput)
            {
                MonoBehaviour.print(buffer);
            }

            appliedForces = new List<AppliedForce>();
            double thrustPerThrustTransform = thrust / thrustTransforms.Count;
            foreach (Transform thrustTransform in thrustTransforms) {
                Vector3d direction = thrustTransform.forward.normalized;
                Vector3d position = thrustTransform.position;
                appliedForces.Add(new AppliedForce(direction * thrustPerThrustTransform, position));
            }
        }

        public ResourceContainer ResourceConsumptions
        {
            get { return this.resourceConsumptions; }
        }

        public bool SetResourceDrains(List<PartSim> allParts, List<PartSim> allFuelLines, HashSet<PartSim> drainingParts)
        {
            LogMsg log = null;

            // A dictionary to hold a set of parts for each resource
            Dictionary<int, HashSet<PartSim>> sourcePartSets = new Dictionary<int, HashSet<PartSim>>();

            foreach (int type in this.resourceConsumptions.Types)
            {
                HashSet<PartSim> sourcePartSet = null;
                switch (ResourceContainer.GetResourceFlowMode(type))
                {
                    case ResourceFlowMode.NO_FLOW:
                        if (this.partSim.resources[type] > SimManager.RESOURCE_MIN && this.partSim.resourceFlowStates[type] != 0)
                        {
                            sourcePartSet = new HashSet<PartSim>();
                            //MonoBehaviour.print("SetResourceDrains(" + name + ":" + partId + ") setting sources to just this");
                            sourcePartSet.Add(this.partSim);
                        }
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                        foreach (PartSim aPartSim in allParts)
                        {
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
                        var stagePartSets = new Dictionary<int, HashSet<PartSim>>();
                        var maxStage = -1;

                        //Logger.Log(type);
                        foreach (var aPartSim in allParts)
                        {
                            if (aPartSim.resources[type] <= SimManager.RESOURCE_MIN || aPartSim.resourceFlowStates[type] == 0) continue;

                            var stage = aPartSim.DecouplerCount();
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

                        for (var i = 0; i <= maxStage; i++)
                        {
                            HashSet<PartSim> stagePartSet;
                            if (stagePartSets.TryGetValue(i, out stagePartSet) && stagePartSet.Count > 0)
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
                            log.buf.AppendLine("Find " + ResourceContainer.GetResourceName(type) + " sources for " + this.partSim.name + ":" + this.partSim.partId);
                        }
                        sourcePartSet = this.partSim.GetSourceSet(type, allParts, visited, log, "");
                        if (SimManager.logOutput)
                        {
                            MonoBehaviour.print(log.buf);
                        }
                        break;

                    default:
                        MonoBehaviour.print("SetResourceDrains(" + this.partSim.name + ":" + this.partSim.partId + ") Unexpected flow type for " + ResourceContainer.GetResourceName(type) + ")");
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
            foreach (int type in this.resourceConsumptions.Types)
            {
                if (!sourcePartSets.ContainsKey(type))
                {
                    if (SimManager.logOutput)
                    {
                        MonoBehaviour.print("No source of " + ResourceContainer.GetResourceName(type));
                    }

                    this.isActive = false;
                    return false;
                }
            }

            // Now we set the drains on the members of the sets and update the draining parts set
            foreach (int type in this.resourceConsumptions.Types)
            {
                HashSet<PartSim> sourcePartSet = sourcePartSets[type];
                // Loop through the members of the set 
                double amount = this.resourceConsumptions[type] / sourcePartSet.Count;
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

        public void DumpEngineToBuffer(StringBuilder buffer, String prefix)
        {
            buffer.Append(prefix);
            buffer.AppendFormat("[thrust = {0:g6}, actual = {1:g6}, isp = {2:g6}\n", this.thrust, this.actualThrust, this.isp);
        }
    }
}