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
    using System.Linq;
    using System.Text;
    using CompoundParts;
    using Extensions;
    using UnityEngine;

    public class PartSim
    {
        private static readonly Pool<PartSim> pool = new Pool<PartSim>(Create, Reset);

        private readonly List<AttachNodeSim> attachNodes = new List<AttachNodeSim>();

        public double realMass;
        public double baseMass;
        public double baseMassForCoM;
        public Vector3d centerOfMass;
        public double baseCost;
        public int decoupledInStage;
        public bool fuelCrossFeed;
        public List<PartSim> fuelTargets = new List<PartSim>();
        public bool hasModuleEngines;
        public bool hasModuleEnginesFX;
        public bool hasMultiModeEngine;

        public bool hasVessel;
        public String initialVesselName;
        public int inverseStage;
        public bool isDecoupler;
        public bool isEngine;
        public bool isFuelLine;
        public bool isFuelTank;
        public bool isLanded;
        public bool isNoPhysics;
        public bool isSepratron;
        public bool isFairing;
        public bool localCorrectThrust;
        public float moduleMass;
        public int stageIndex;
        public String name;
        public String noCrossFeedNodeKey;
        public PartSim parent;
        public AttachModes parentAttach;
        public Part part; // This is only set while the data structures are being initialised
        public int partId = 0;
        public ResourceContainer resourceDrains = new ResourceContainer();
        public ResourceContainer resourceFlowStates = new ResourceContainer();
        public ResourceContainer resources = new ResourceContainer();
        public double startMass = 0d;
        public String vesselName;
        public VesselType vesselType;
        

        private static PartSim Create()
        {
            return new PartSim();
        }

        private static void Reset(PartSim partSim)
        {
            for (int i = 0; i < partSim.attachNodes.Count; i++)
            {
                partSim.attachNodes[i].Release();
            }
            partSim.attachNodes.Clear();
            partSim.fuelTargets.Clear();
            partSim.resourceDrains.Reset();
            partSim.resourceFlowStates.Reset();
            partSim.resources.Reset();
            partSim.baseCost = 0d;
            partSim.baseMass = 0d;
            partSim.baseMassForCoM = 0d;
            partSim.startMass = 0d;
        }

        public void Release()
        {
            pool.Release(this);
        }

        public static PartSim New(Part thePart, int id, double atmosphere, LogMsg log)
        {
            PartSim partSim = pool.Borrow();

            partSim.part = thePart;
            partSim.centerOfMass = thePart.transform.TransformPoint(thePart.CoMOffset);
            partSim.partId = id;
            partSim.name = partSim.part.partInfo.name;

            if (log != null) log.buf.AppendLine("Create PartSim for " + partSim.name);

            partSim.parent = null;
            partSim.parentAttach = partSim.part.attachMode;
            partSim.fuelCrossFeed = partSim.part.fuelCrossFeed;
            partSim.noCrossFeedNodeKey = partSim.part.NoCrossFeedNodeKey;
            partSim.decoupledInStage = partSim.DecoupledInStage(partSim.part);
            partSim.isFuelLine = partSim.part.HasModule<CModuleFuelLine>();
            partSim.isFuelTank = partSim.part is FuelTank;
            partSim.isSepratron = partSim.IsSepratron();
            partSim.inverseStage = partSim.part.inverseStage;
            //MonoBehaviour.print("inverseStage = " + inverseStage);

            partSim.baseCost = partSim.part.GetCostDry();

            if (log != null)
            {
                log.buf.AppendLine("Parent part = " + (partSim.part.parent == null ? "null" : partSim.part.parent.partInfo.name));
                log.buf.AppendLine("physicalSignificance = " + partSim.part.physicalSignificance);
                log.buf.AppendLine("PhysicsSignificance = " + partSim.part.PhysicsSignificance);
            }

            // Work out if the part should have no physical significance
            // The root part is never "no physics"
            partSim.isNoPhysics = partSim.part.physicalSignificance == Part.PhysicalSignificance.NONE ||
                                    partSim.part.PhysicsSignificance == 1;

            if (partSim.part.HasModule<LaunchClamp>())
            {
                partSim.realMass = 0d;
                if (log != null) log.buf.AppendLine("Ignoring mass of launch clamp");
            }
            else
            {
                partSim.realMass = partSim.part.mass;
                if (log != null) log.buf.AppendLine("Using part.mass of " + partSim.part.mass);
            }

            partSim.moduleMass = partSim.part.GetModuleMass((float)partSim.realMass);

            for (int i = 0; i < partSim.part.Resources.Count; i++)
            {
                PartResource resource = partSim.part.Resources[i];

                // Make sure it isn't NaN as this messes up the part mass and hence most of the values
                // This can happen if a resource capacity is 0 and tweakable
                if (!Double.IsNaN(resource.amount))
                {
                    if (log != null)
                        log.buf.AppendLine(resource.resourceName + " = " + resource.amount);

                    partSim.resources.Add(resource.info.id, resource.amount);
                    partSim.resourceFlowStates.Add(resource.info.id, resource.flowState ? 1 : 0);
                }
                else
                {
                    if (log != null) log.buf.AppendLine(resource.resourceName + " is NaN. Skipping.");
                }
            }

            partSim.hasVessel = (partSim.part.vessel != null);
            partSim.isLanded = partSim.hasVessel && partSim.part.vessel.Landed;
            if (partSim.hasVessel)
            {
                partSim.vesselName = partSim.part.vessel.vesselName;
                partSim.vesselType = partSim.part.vesselType;
            }
            partSim.initialVesselName = partSim.part.initialVesselName;

            partSim.hasMultiModeEngine = partSim.part.HasModule<MultiModeEngine>();
            partSim.hasModuleEnginesFX = partSim.part.HasModule<ModuleEnginesFX>();
            partSim.hasModuleEngines = partSim.part.HasModule<ModuleEngines>();

            partSim.isEngine = partSim.hasMultiModeEngine || partSim.hasModuleEnginesFX || partSim.hasModuleEngines;

            if (log != null) log.buf.AppendLine("Created " + partSim.name + ". Decoupled in stage " + partSim.decoupledInStage);

            return partSim;
        }

        public ResourceContainer ResourceDrains
        {
            get
            {
                return resourceDrains;
            }
        }

        public ResourceContainer Resources
        {
            get
            {
                return resources;
            }
        }

        public void CreateEngineSims(List<EngineSim> allEngines, double atmosphere, double mach, bool vectoredThrust, bool fullThrust, LogMsg log)
        {
            bool correctThrust = SimManager.DoesEngineUseCorrectedThrust(part);
            if (log != null)
            {
                log.buf.AppendLine("CreateEngineSims for " + this.name);
                for (int i = 0; i < this.part.Modules.Count; i++)
                {
                    PartModule partMod = this.part.Modules[i];
                    log.buf.AppendLine("Module: " + partMod.moduleName);
                }

                log.buf.AppendLine("correctThrust = " + correctThrust);
            }

            if (hasMultiModeEngine)
            {
                // A multi-mode engine has multiple ModuleEnginesFX but only one is active at any point
                // The mode of the engine is the engineID of the ModuleEnginesFX that is active
                string mode = part.GetModule<MultiModeEngine>().mode;

                List<ModuleEnginesFX> engines = part.GetModules<ModuleEnginesFX>();
                for (int i = 0; i < engines.Count; ++i)
                {
                    ModuleEnginesFX engine = engines[i];
                    if (engine.engineID == mode)
                    {
                        if (log != null) log.buf.AppendLine("Module: " + engine.moduleName);

                        Vector3 thrustvec = this.CalculateThrustVector(vectoredThrust ? engine.thrustTransforms : null, log);

                        EngineSim engineSim = EngineSim.New(
                            this,
                            atmosphere,
                            (float)mach,
                            engine.maxFuelFlow,
                            engine.minFuelFlow,
                            engine.thrustPercentage,
                            thrustvec,
                            engine.atmosphereCurve,
                            engine.atmChangeFlow,
                            engine.useAtmCurve ? engine.atmCurve : null,
                            engine.useVelCurve ? engine.velCurve : null,
                            fullThrust ? 0.0f : engine.currentThrottle,
                            engine.g,
                            engine.throttleLocked,
                            engine.propellants,
                            engine.isOperational,
                            engine.resultingThrust,
                            engine.thrustTransforms,
                            log);
                        allEngines.Add(engineSim);
                    }
                }
            }
            else
            {
                if (hasModuleEngines)
                {
                    List<ModuleEngines> engines = part.GetModules<ModuleEngines>();
                    for (int i = 0; i < engines.Count; ++i)
                    {
                        ModuleEngines engine = engines[i];
                        if (log != null) log.buf.AppendLine("Module: " + engine.moduleName);

                        Vector3 thrustvec = this.CalculateThrustVector(vectoredThrust ? engine.thrustTransforms : null, log);

                        EngineSim engineSim = EngineSim.New(
                            this,
                            atmosphere,
                            (float)mach,
                            engine.maxFuelFlow,
                            engine.minFuelFlow,
                            engine.thrustPercentage,
                            thrustvec,
                            engine.atmosphereCurve,
                            engine.atmChangeFlow,
                            engine.useAtmCurve ? engine.atmCurve : null,
                            engine.useVelCurve ? engine.velCurve : null,
                            fullThrust ? 0.0f : engine.currentThrottle,
                            engine.g,
                            engine.throttleLocked,
                            engine.propellants,
                            engine.isOperational,
                            engine.resultingThrust,
                            engine.thrustTransforms,
                            log);
                        allEngines.Add(engineSim);
                    }
                }
            }

            if (log != null)
            {
                log.Flush();
            }
        }

        public int DecouplerCount()
        {
            int count = 0;
            PartSim partSim = this;
            while (partSim != null)
            {
                if (partSim.isDecoupler)
                {
                    count++;
                }

                partSim = partSim.parent;
            }
            return count;
        }

        public void DrainResources(double time)
        {
            //MonoBehaviour.print("DrainResources(" + name + ":" + partId + ", " + time + ")");
            for (int i = 0; i < resourceDrains.Types.Count; ++i)
            {
                int type = resourceDrains.Types[i];

                //MonoBehaviour.print("draining " + (time * resourceDrains[type]) + " " + ResourceContainer.GetResourceName(type));
                resources.Add(type, -time * resourceDrains[type]);
                //MonoBehaviour.print(ResourceContainer.GetResourceName(type) + " left = " + resources[type]);
            }
        }

        public String DumpPartAndParentsToBuffer(StringBuilder buffer, String prefix)
        {
            if (parent != null)
            {
                prefix = parent.DumpPartAndParentsToBuffer(buffer, prefix) + " ";
            }

            DumpPartToBuffer(buffer, prefix);

            return prefix;
        }

        public void DumpPartToBuffer(StringBuilder buffer, String prefix, List<PartSim> allParts = null)
        {
            buffer.Append(prefix);
            buffer.Append(name);
            buffer.AppendFormat(":[id = {0:d}, decouple = {1:d}, invstage = {2:d}", partId, decoupledInStage, inverseStage);

            //buffer.AppendFormat(", vesselName = '{0}'", vesselName);
            //buffer.AppendFormat(", vesselType = {0}", SimManager.GetVesselTypeString(vesselType));
            //buffer.AppendFormat(", initialVesselName = '{0}'", initialVesselName);

            buffer.AppendFormat(", isNoPhys = {0}", isNoPhysics);
            buffer.AppendFormat(", baseMass = {0}", baseMass);
            buffer.AppendFormat(", baseMassForCoM = {0}", baseMassForCoM);

            buffer.AppendFormat(", fuelCF = {0}", fuelCrossFeed);
            buffer.AppendFormat(", noCFNKey = '{0}'", noCrossFeedNodeKey);

            buffer.AppendFormat(", isSep = {0}", isSepratron);

            for (int i = 0; i < resources.Types.Count; i++)
            {
                int type = resources.Types[i];
                buffer.AppendFormat(", {0} = {1:g6}", ResourceContainer.GetResourceName(type), resources[type]);
            }

            if (attachNodes.Count > 0)
            {
                buffer.Append(", attached = <");
                attachNodes[0].DumpToBuffer(buffer);
                for (int i = 1; i < attachNodes.Count; i++)
                {
                    buffer.Append(", ");
                    attachNodes[i].DumpToBuffer(buffer);
                }
                buffer.Append(">");
            }

            // Add more info here

            buffer.Append("]\n");

            if (allParts != null)
            {
                String newPrefix = prefix + " ";
                for (int i = 0; i < allParts.Count; i++)
                {
                    PartSim partSim = allParts[i];
                    if (partSim.parent == this)
                        partSim.DumpPartToBuffer(buffer, newPrefix, allParts);
                }
            }
        }

        public bool EmptyOf(HashSet<int> types)
        {
            foreach (int type in types)
            {
                if (resources.HasType(type) && resourceFlowStates[type] != 0 && resources[type] > SimManager.RESOURCE_PART_EMPTY_THRESH)
                    return false;
            }

            return true;
        }

        public double GetMass(int currentStage, bool forCoM = false)
        {
            if (decoupledInStage >= currentStage)
                return 0d;

            double mass = forCoM ? baseMassForCoM : baseMass;

            for (int i = 0; i < resources.Types.Count; ++i)
            {
                mass += resources.GetResourceMass(resources.Types[i]);
            }

            if (hasVessel == false && isFairing && inverseStage < currentStage)
            {
                mass = mass + moduleMass;
            }

            return mass;
        }

        public double GetCost(int currentStage)
        {
            if (decoupledInStage >= currentStage)
                return 0d;

            double cost = baseCost;

            for (int i = 0; i < resources.Types.Count; ++i)
            {
                cost += resources.GetResourceCost(resources.Types[i]);
            }

            return cost;
        }

        public void ReleasePart()
        {
            this.part = null;
        }

        // All functions below this point must not rely on the part member (it may be null)
        //

        public void GetSourceSet(int type, List<PartSim> allParts, HashSet<PartSim> visited, HashSet<PartSim> allSources, LogMsg log, String indent)
        {
            if (log != null)
            {
                log.buf.AppendLine(indent + "GetSourceSet(" + ResourceContainer.GetResourceName(type) + ") for " + name + ":" + partId);
                indent += "  ";
            }

            // Rule 1: Each part can be only visited once, If it is visited for second time in particular search it returns as is.
            if (visited.Contains(this))
            {
                if (log != null) log.buf.AppendLine(indent + "Returning empty set, already visited (" + name + ":" + partId + ")");
                return;
            }

            if (log != null) log.buf.AppendLine(indent + "Adding this to visited");

            visited.Add(this);

            // Rule 2: Part performs scan on start of every fuel pipe ending in it. This scan is done in order in which pipes were installed.
            // Then it makes an union of fuel tank sets each pipe scan returned. If the resulting list is not empty, it is returned as result.
            //MonoBehaviour.print("for each fuel line");

            int lastCount = allSources.Count;

            for (int i = 0; i < this.fuelTargets.Count; i++)
            {
                PartSim partSim = this.fuelTargets[i];
                if (partSim != null)
                {
                    if (visited.Contains(partSim))
                    {
                        if (log != null) log.buf.AppendLine(indent + "Fuel target already visited, skipping (" + partSim.name + ":" + partSim.partId + ")");
                    }
                    else
                    {
                        if (log != null) log.buf.AppendLine(indent + "Adding fuel target as source (" + partSim.name + ":" + partSim.partId + ")");

                        partSim.GetSourceSet(type, allParts, visited, allSources, log, indent);
                    }
                }
            }

            if (allSources.Count > lastCount)
            {
                if (log != null) log.buf.AppendLine(indent + "Returning " + (allSources.Count - lastCount) + " fuel target sources (" + this.name + ":" + this.partId + ")");
                return;
            }


            // Rule 3: This rule has been removed and merged with rules 4 and 7 to fix issue with fuel tanks with disabled crossfeed

            // Rule 4: Part performs scan on each of its axially mounted neighbors. 
            //  Couplers (bicoupler, tricoupler, ...) are an exception, they only scan one attach point on the single attachment side,
            //  skip the points on the side where multiple points are. [Experiment]
            //  Again, the part creates union of scan lists from each of its neighbor and if it is not empty, returns this list. 
            //  The order in which mount points of a part are scanned appears to be fixed and defined by the part specification file. [Experiment]
            if (fuelCrossFeed)
            {
                lastCount = allSources.Count;
                //MonoBehaviour.print("for each attach node");
                for (int i = 0; i < this.attachNodes.Count; i++)
                {
                    AttachNodeSim attachSim = this.attachNodes[i];
                    if (attachSim.attachedPartSim != null)
                    {
                        if (attachSim.nodeType == AttachNode.NodeType.Stack)
                        {
                            if (
                                !(this.noCrossFeedNodeKey != null && this.noCrossFeedNodeKey.Length > 0 &&
                                  attachSim.id.Contains(this.noCrossFeedNodeKey)))
                            {
                                if (visited.Contains(attachSim.attachedPartSim))
                                {
                                    if (log != null) log.buf.AppendLine(indent + "Attached part already visited, skipping (" + attachSim.attachedPartSim.name + ":" + attachSim.attachedPartSim.partId + ")");
                                }
                                else
                                {
                                    if (log != null) log.buf.AppendLine(indent + "Adding attached part as source (" + attachSim.attachedPartSim.name + ":" + attachSim.attachedPartSim.partId + ")");

                                    attachSim.attachedPartSim.GetSourceSet(type, allParts, visited, allSources, log, indent);
                                }
                            }
                        }
                    }
                }

                if (allSources.Count > lastCount)
                {
                    if (log != null) log.buf.AppendLine(indent + "Returning " + (allSources.Count - lastCount) + " attached sources (" + this.name + ":" + this.partId + ")");
                    return;
                }
            }

            // Rule 5: If the part is fuel container for searched type of fuel (i.e. it has capability to contain that type of fuel and the fuel 
            // type was not disabled [Experiment]) and it contains fuel, it returns itself.
            // Rule 6: If the part is fuel container for searched type of fuel (i.e. it has capability to contain that type of fuel and the fuel 
            // type was not disabled) but it does not contain the requested fuel, it returns empty list. [Experiment]
            if (resources.HasType(type) && resourceFlowStates[type] != 0)
            {
                if (resources[type] > SimManager.RESOURCE_MIN)
                {
                    allSources.Add(this);

                    if (log != null) log.buf.AppendLine(indent + "Returning enabled tank as only source (" + name + ":" + partId + ")");
                }

                return;
            }
            else
            {
                if (log != null) log.buf.AppendLine(indent + "Not fuel tank or disabled. HasType = " + resources.HasType(type) + "  FlowState = " + resourceFlowStates[type]);
            }

            // Rule 7: If the part is radially attached to another part and it is child of that part in the ship's tree structure, it scans its 
            // parent and returns whatever the parent scan returned. [Experiment] [Experiment]
            if (parent != null && parentAttach == AttachModes.SRF_ATTACH)
            {
                if (fuelCrossFeed)
                {
                    if (visited.Contains(parent))
                    {
                        if (log != null) log.buf.AppendLine(indent + "Parent part already visited, skipping (" + parent.name + ":" + parent.partId + ")");
                    }
                    else
                    {
                        lastCount = allSources.Count;
                        this.parent.GetSourceSet(type, allParts, visited, allSources, log, indent);
                        if (allSources.Count > lastCount)
                        {
                            if (log != null) log.buf.AppendLine(indent + "Returning " + (allSources.Count  - lastCount) + " parent sources (" + this.name + ":" + this.partId + ")");
                            return;
                        }
                    }
                }
            }

            // Rule 8: If all preceding rules failed, part returns empty list.
            if (log != null) log.buf.AppendLine(indent + "Returning empty set, no sources found (" + name + ":" + partId + ")");

            return;
        }

        public double GetStartMass()
        {
            return startMass;
        }

        public void RemoveAttachedParts(HashSet<PartSim> partSims)
        {
            // Loop through the attached parts
            for (int i = 0; i < this.attachNodes.Count; i++)
            {
                AttachNodeSim attachSim = this.attachNodes[i];
                // If the part is in the set then "remove" it by clearing the PartSim reference
                if (partSims.Contains(attachSim.attachedPartSim))
                {
                    attachSim.attachedPartSim = null;
                }
            }

            // Loop through the fuel targets (fuel line sources)
            for (int i = 0; i < this.fuelTargets.Count; i++)
            {
                PartSim fuelTargetSim = this.fuelTargets[i];
                // If the part is in the set then "remove" it by clearing the PartSim reference
                if (fuelTargetSim != null && partSims.Contains(fuelTargetSim))
                {
                    this.fuelTargets[i] = null;
                }
            }
        }

        public void SetupAttachNodes(Dictionary<Part, PartSim> partSimLookup, LogMsg log)
        {
            if (log != null) log.buf.AppendLine("SetupAttachNodes for " + name + ":" + partId + "");

            attachNodes.Clear();

            for (int i = 0; i < part.attachNodes.Count; ++i)
            {
                AttachNode attachNode = part.attachNodes[i];

                if (log != null) log.buf.AppendLine("AttachNode " + attachNode.id + " = " + (attachNode.attachedPart != null ? attachNode.attachedPart.partInfo.name : "null"));

                if (attachNode.attachedPart != null && attachNode.id != "Strut")
                {
                    PartSim attachedSim;
                    if (partSimLookup.TryGetValue(attachNode.attachedPart, out attachedSim))
                    {
                        if (log != null) log.buf.AppendLine("Adding attached node " + attachedSim.name + ":" + attachedSim.partId + "");

                        attachNodes.Add(AttachNodeSim.New(attachedSim, attachNode.id, attachNode.nodeType));
                    }
                    else
                    {
                        if (log != null) log.buf.AppendLine("No PartSim for attached part (" + attachNode.attachedPart.partInfo.name + ")");
                    }
                }
            }

            for (int i = 0; i < part.fuelLookupTargets.Count; ++i)
            {
                Part p = part.fuelLookupTargets[i];

                if (p != null)
                {
                    PartSim targetSim;
                    if (partSimLookup.TryGetValue(p, out targetSim))
                    {
                        if (log != null) log.buf.AppendLine("Fuel target: " + targetSim.name + ":" + targetSim.partId);

                        fuelTargets.Add(targetSim);
                    }
                    else
                    {
                        if (log != null) log.buf.AppendLine("No PartSim for fuel target (" + p.name + ")");
                    }
                }
            }
        }

        public void SetupParent(Dictionary<Part, PartSim> partSimLookup, LogMsg log)
        {
            if (part.parent != null)
            {
                parent = null;
                if (partSimLookup.TryGetValue(part.parent, out parent))
                {
                    if (log != null) log.buf.AppendLine("Parent part is " + parent.name + ":" + parent.partId);
                }
                else
                {
                    if (log != null) log.buf.AppendLine("No PartSim for parent part (" + part.parent.partInfo.name + ")");
                }
            }
        }

        public double TimeToDrainResource()
        {
            //MonoBehaviour.print("TimeToDrainResource(" + name + ":" + partId + ")");
            double time = double.MaxValue;

            for (int i = 0; i < resourceDrains.Types.Count; ++i)
            {
                int type = resourceDrains.Types[i];

                if (resourceDrains[type] > 0)
                {
                    time = Math.Min(time, resources[type] / resourceDrains[type]);
                    //MonoBehaviour.print("type = " + ResourceContainer.GetResourceName(type) + "  amount = " + resources[type] + "  rate = " + resourceDrains[type] + "  time = " + time);
                }
            }

            //if (time < double.MaxValue)
            //    MonoBehaviour.print("TimeToDrainResource(" + name + ":" + partId + ") = " + time);
            return time;
        }

        private Vector3 CalculateThrustVector(List<Transform> thrustTransforms, LogMsg log)
        {
            if (thrustTransforms == null)
            {
                return Vector3.forward;
            }

            Vector3 thrustvec = Vector3.zero;
            for (int i = 0; i < thrustTransforms.Count; ++i)
            {
                Transform trans = thrustTransforms[i];

                if (log != null) log.buf.AppendFormat("Transform = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", trans.forward.x, trans.forward.y, trans.forward.z, trans.forward.magnitude);

                thrustvec -= trans.forward;
            }

            if (log != null) log.buf.AppendFormat("ThrustVec  = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", thrustvec.x, thrustvec.y, thrustvec.z, thrustvec.magnitude);

            thrustvec.Normalize();

            if (log != null) log.buf.AppendFormat("ThrustVecN = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", thrustvec.x, thrustvec.y, thrustvec.z, thrustvec.magnitude);

            return thrustvec;
        }

        private int DecoupledInStage(Part thePart, int stage = -1)
        {
            if (IsDecoupler(thePart) && thePart.inverseStage > stage)
                stage = thePart.inverseStage;

            if (thePart.parent != null)
                stage = DecoupledInStage(thePart.parent, stage);

            return stage;
        }

        private bool IsActiveDecoupler(Part thePart)
        {
            return thePart.FindModulesImplementing<ModuleDecouple>().Any(mod => !mod.isDecoupled) ||
                   thePart.FindModulesImplementing<ModuleAnchoredDecoupler>().Any(mod => !mod.isDecoupled);
        }

        private bool IsDecoupler(Part thePart)
        {
            return thePart.HasModule<ModuleDecouple>() ||
                   thePart.HasModule<ModuleAnchoredDecoupler>();
        }

        private bool IsFairing(Part thePart)
        {
            return thePart.HasModule<ModuleProceduralFairing>();
        }

        private bool IsSepratron()
        {
            if (!part.ActivatesEvenIfDisconnected)
            {
                return false;
            }

            if (part is SolidRocket)
            {
                return true;
            }

            IEnumerable<ModuleEngines> modList = part.Modules.OfType<ModuleEngines>();
            if (modList.Count() == 0)
            {
                return false;
            }

            if (modList.First().throttleLocked)
            {
                return true;
            }

            return false;
        }
    }
}