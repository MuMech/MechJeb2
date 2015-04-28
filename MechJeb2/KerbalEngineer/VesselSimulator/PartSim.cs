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

using KerbalEngineer.Extensions;
using UnityEngine;

#endregion

namespace KerbalEngineer.VesselSimulator
{
    using CompoundParts;

    public class PartSim
    {
        private static readonly Pool<PartSim> pool = new Pool<PartSim>(Create, Reset);

        private readonly List<AttachNodeSim> attachNodes = new List<AttachNodeSim>();

        public Vector3d centerOfMass;
        public double baseMass = 0d;
        public double cost;
        public int decoupledInStage;
        public bool fuelCrossFeed;
        public List<PartSim> fuelTargets = new List<PartSim>();
        public bool hasModuleEngines;
        public bool hasModuleEnginesFX;
        public bool hasMultiModeEngine;

        public bool hasVessel;
        public String initialVesselName;
        public int inverseStage;
        public bool isDecoupler;   // never assigned - remove ?
        public bool isEngine;
        public bool isFuelLine;
        public bool isFuelTank;
        public bool isLanded;
        public bool isNoPhysics;
        public bool isSepratron;
        public bool localCorrectThrust;    // not used - remove ?
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
            partSim.baseMass = 0d;
            partSim.startMass = 0d;

            // Something did not get reset properly so I got overboard with the default
            // will find the actual usefull line later

            partSim.centerOfMass = Vector3d.zero;
            partSim.cost = 0;
            partSim.decoupledInStage = 0;
            partSim.fuelCrossFeed = false;
            partSim.hasModuleEngines= false;
            partSim.hasModuleEnginesFX = false;
            partSim.hasMultiModeEngine = false;
            
            partSim.hasVessel= false;
            partSim.initialVesselName = null;
            partSim.inverseStage = 0;
            partSim.isDecoupler = false; ;   // never assigned 
            partSim.isEngine = false;  ;
            partSim.isFuelLine = false; ;
            partSim.isFuelTank = false; ;
            partSim.isLanded = false; ;
            partSim.isNoPhysics = false; ;
            partSim.isSepratron = false; ;
            partSim.localCorrectThrust = false; ;    // not use
            partSim.name = null;
            partSim.noCrossFeedNodeKey = null;
            partSim.parent = null;
            partSim.parentAttach = AttachModes.SRF_ATTACH;
            partSim.part = null; // This is only set while t
            partSim.partId = 0;
            partSim.vesselName = null;
            partSim.vesselType = VesselType.Base;
        }

        public void Release()
        {
            pool.Release(this);
        }

        public static PartSim New(Part thePart, int id, double atmosphere, LogMsg log)
        {
            PartSim partSim = pool.Borrow();

            Reset(partSim);

            partSim.part = thePart;
            partSim.centerOfMass = thePart.transform.TransformPoint(thePart.CoMOffset);
            partSim.partId = id;
            partSim.name = partSim.part.partInfo.name;

            if (log != null)
            {
                log.buf.AppendLine("Create PartSim for " + partSim.name);
            }

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

            partSim.cost = partSim.part.GetCostWet();

            // Work out if the part should have no physical significance
            partSim.isNoPhysics = partSim.part.HasModule<LaunchClamp>() ||
                               partSim.part.physicalSignificance == Part.PhysicalSignificance.NONE ||
                               partSim.part.PhysicsSignificance == 1;

            if (!partSim.isNoPhysics)
            {
                partSim.baseMass = partSim.part.mass + partSim.part.GetPhysicslessChildMass();
            }

            if (SimManager.logOutput)
            {
                MonoBehaviour.print((partSim.isNoPhysics ? "Ignoring" : "Using") + " part.mass of " + partSim.part.mass);
            }

            for (int i = 0; i < partSim.part.Resources.Count; i++)
            {
                PartResource resource = partSim.part.Resources[i];

                // Make sure it isn't NaN as this messes up the part mass and hence most of the values
                // This can happen if a resource capacity is 0 and tweakable
                if (!Double.IsNaN(resource.amount))
                {
                    if (SimManager.logOutput)
                    {
                        MonoBehaviour.print(resource.resourceName + " = " + resource.amount);
                    }

                    partSim.resources.Add(resource.info.id, resource.amount);
                    partSim.resourceFlowStates.Add(resource.info.id, resource.flowState ? 1 : 0);
                }
                else
                {
                    MonoBehaviour.print(resource.resourceName + " is NaN. Skipping.");
                }
            }

            partSim.startMass = partSim.GetMass();

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

            if (SimManager.logOutput)
            {
                MonoBehaviour.print("Created " + partSim.name + ". Decoupled in stage " + partSim.decoupledInStage);
            }
            return partSim;
        }

        public ResourceContainer Resources
        {
            get { return this.resources; }
        }

        public ResourceContainer ResourceDrains
        {
            get { return this.resourceDrains; }
        }

        public void CreateEngineSims(List<EngineSim> allEngines, double atmosphere, double mach, bool vectoredThrust, bool fullThrust, LogMsg log)
        {
            bool correctThrust = SimManager.DoesEngineUseCorrectedThrust(this.part);
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

            if (this.hasMultiModeEngine)
            {
                // A multi-mode engine has multiple ModuleEnginesFX but only one is active at any point
                // The mode of the engine is the engineID of the ModuleEnginesFX that is active
                string mode = this.part.GetModule<MultiModeEngine>().mode;

                List<ModuleEnginesFX> enginesFx = this.part.GetModules<ModuleEnginesFX>();
                for (int i = 0; i < enginesFx.Count; i++)
                {
                    ModuleEnginesFX engine = enginesFx[i];
                    if (engine.engineID == mode)
                    {
                        if (log != null)
                        {
                            log.buf.AppendLine("Module: " + engine.moduleName);
                        }

                        Vector3 thrustvec = this.CalculateThrustVector(vectoredThrust ? engine.thrustTransforms : null, log);

                        EngineSim engineSim = EngineSim.New(
                            this,
                            atmosphere,
                            mach,
                            engine.maxFuelFlow,
                            engine.minFuelFlow,
                            engine.thrustPercentage,
                            thrustvec,
                            engine.atmosphereCurve,
                            engine.atmChangeFlow,
                            engine.useAtmCurve ? engine.atmCurve : null,
                            engine.useVelCurve ? engine.velCurve : null,
                            engine.currentThrottle,
                            engine.g,
                            engine.throttleLocked || fullThrust,
                            engine.propellants,
                            engine.isOperational,
                            correctThrust,
                            engine.thrustTransforms);
                        allEngines.Add(engineSim);
                    }
                }
            }
            else
            {
                if (this.hasModuleEngines)
                {
                    List<ModuleEngines> engines = this.part.GetModules<ModuleEngines>();  // only place that still allocate some memory
                    for (int i = 0; i < engines.Count; i++)
                    {
                        ModuleEngines engine = engines[i];
                        if (log != null)
                        {
                            log.buf.AppendLine("Module: " + engine.moduleName);
                        }
                        
                        Vector3 thrustvec = this.CalculateThrustVector(vectoredThrust ? engine.thrustTransforms : null, log);

                        EngineSim engineSim = EngineSim.New(
                            this,
                            atmosphere,
                            mach,
                            engine.maxFuelFlow,
                            engine.minFuelFlow,
                            engine.thrustPercentage,
                            thrustvec,
                            engine.atmosphereCurve,
                            engine.atmChangeFlow,
                            engine.useAtmCurve ? engine.atmCurve : null,
                            engine.useVelCurve ? engine.velCurve : null,
                            engine.currentThrottle,
                            engine.g,
                            engine.throttleLocked || fullThrust,
                            engine.propellants,
                            engine.isOperational,
                            correctThrust,
                            engine.thrustTransforms);
                        allEngines.Add(engineSim);
                    }
                }
            }

            if (log != null)
            {
                log.Flush();
            }
        }

        private Vector3 CalculateThrustVector(List<Transform> thrustTransforms, LogMsg log)
        {
            if (thrustTransforms == null)
            {
                return Vector3.forward;
            }

            Vector3 thrustvec = Vector3.zero;
            for (int i = 0; i < thrustTransforms.Count; i++)
            {
                Transform trans = thrustTransforms[i];
                if (log != null)
                {
                    log.buf.AppendFormat(
                        "Transform = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n",
                        trans.forward.x,
                        trans.forward.y,
                        trans.forward.z,
                        trans.forward.magnitude);
                }

                thrustvec -= trans.forward;
            }

            if (log != null)
            {
                log.buf.AppendFormat("ThrustVec  = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", thrustvec.x, thrustvec.y, thrustvec.z, thrustvec.magnitude);
            }

            thrustvec.Normalize();

            if (log != null)
            {
                log.buf.AppendFormat("ThrustVecN = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", thrustvec.x, thrustvec.y, thrustvec.z, thrustvec.magnitude);
            }

            return thrustvec;
        }

        public void SetupParent(Dictionary<Part, PartSim> partSimLookup, LogMsg log)
        {
            if (this.part.parent != null)
            {
                this.parent = null;
                if (partSimLookup.TryGetValue(this.part.parent, out this.parent))
                {
                    if (log != null)
                    {
                        log.buf.AppendLine("Parent part is " + this.parent.name + ":" + this.parent.partId);
                    }
                }
                else
                {
                    if (log != null)
                    {
                        log.buf.AppendLine("No PartSim for parent part (" + this.part.parent.partInfo.name + ")");
                    }
                }
            }
        }

        public void SetupAttachNodes(Dictionary<Part, PartSim> partSimLookup, LogMsg log)
        {
            if (log != null)
            {
                log.buf.AppendLine("SetupAttachNodes for " + this.name + ":" + this.partId + "");
            }

            this.attachNodes.Clear();
            for (int i = 0; i < this.part.attachNodes.Count; i++)
            {
                AttachNode attachNode = this.part.attachNodes[i];
                if (log != null)
                {
                    log.buf.AppendLine(
                        "AttachNode " + attachNode.id + " = " +
                        (attachNode.attachedPart != null ? attachNode.attachedPart.partInfo.name : "null"));
                }

                if (attachNode.attachedPart != null && attachNode.id != "Strut")
                {
                    PartSim attachedSim;
                    if (partSimLookup.TryGetValue(attachNode.attachedPart, out attachedSim))
                    {
                        if (log != null)
                        {
                            log.buf.AppendLine("Adding attached node " + attachedSim.name + ":" + attachedSim.partId + "");
                        }

                        AttachNodeSim attachnode = AttachNodeSim.New(attachedSim, attachNode.id, attachNode.nodeType);
                        this.attachNodes.Add(attachnode);
                    }
                    else
                    {
                        if (log != null)
                        {
                            log.buf.AppendLine("No PartSim for attached part (" + attachNode.attachedPart.partInfo.name + ")");
                        }
                    }
                }
            }

            for (int i = 0; i < this.part.fuelLookupTargets.Count; i++)
            {
                Part p = this.part.fuelLookupTargets[i];
                if (p != null)
                {
                    PartSim targetSim;
                    if (partSimLookup.TryGetValue(p, out targetSim))
                    {
                        if (log != null)
                        {
                            log.buf.AppendLine("Fuel target: " + targetSim.name + ":" + targetSim.partId);
                        }

                        this.fuelTargets.Add(targetSim);
                    }
                    else
                    {
                        if (log != null)
                        {
                            log.buf.AppendLine("No PartSim for fuel target (" + p.name + ")");
                        }
                    }
                }
            }
        }

        private int DecoupledInStage(Part thePart, int stage = -1)
        {
            if (this.IsDecoupler(thePart))
            {
                if (thePart.inverseStage > stage)
                {
                    stage = thePart.inverseStage;
                }
            }

            if (thePart.parent != null)
            {
                stage = this.DecoupledInStage(thePart.parent, stage);
            }

            return stage;
        }

        private bool IsDecoupler(Part thePart)
        {
            return thePart.HasModule<ModuleDecouple>() ||
                   thePart.HasModule<ModuleAnchoredDecoupler>();
        }

        private bool IsActiveDecoupler(Part thePart)
        {
            return thePart.FindModulesImplementing<ModuleDecouple>().Any(mod => !mod.isDecoupled) ||
                   thePart.FindModulesImplementing<ModuleAnchoredDecoupler>().Any(mod => !mod.isDecoupled);
        }

        private bool IsSepratron()
        {
            if (!this.part.ActivatesEvenIfDisconnected)
            {
                return false;
            }

            if (this.part is SolidRocket)
            {
                return true;
            }

            if (!this.part.IsEngine())
            {
                return false;
            }


            return this.part.IsSolidRocket();
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
                log.buf.AppendLine(indent + "GetSourceSet(" + ResourceContainer.GetResourceName(type) + ") for " + this.name + ":" + this.partId);
                indent += "  ";
            }

            // Rule 1: Each part can be only visited once, If it is visited for second time in particular search it returns as is.
            if (visited.Contains(this))
            {
                if (log != null)
                {
                    log.buf.AppendLine(indent + "Returning empty set, already visited (" + this.name + ":" + this.partId + ")");
                }

                return;
            }

            //if (log != null)
            //    log.buf.AppendLine(indent + "Adding this to visited");

            visited.Add(this);

            // Rule 2: Part performs scan on start of every fuel pipe ending in it. This scan is done in order in which pipes were installed.
            // Then it makes an union of fuel tank sets each pipe scan returned. If the resulting list is not empty, it is returned as result.
            //MonoBehaviour.print("foreach fuel line");

            int lastCount = allSources.Count;

            for (int i = 0; i < this.fuelTargets.Count; i++)
            {
                PartSim partSim = this.fuelTargets[i];
                if (visited.Contains(partSim))
                {
                    //if (log != null)
                    //    log.buf.AppendLine(indent + "Fuel target already visited, skipping (" + partSim.name + ":" + partSim.partId + ")");
                }
                else
                {
                    //if (log != null)
                    //    log.buf.AppendLine(indent + "Adding fuel target as source (" + partSim.name + ":" + partSim.partId + ")");

                    partSim.GetSourceSet(type, allParts, visited, allSources, log, indent);
                }
            }

            if (allSources.Count > lastCount)
            {
                if (log != null)
                {
                    log.buf.AppendLine(indent + "Returning " + (allSources.Count - lastCount) + " fuel target sources (" + this.name + ":" + this.partId + ")");
                }

                return;
            }


            // Rule 3: This rule has been removed and merged with rules 4 and 7 to fix issue with fuel tanks with disabled crossfeed

            // Rule 4: Part performs scan on each of its axially mounted neighbors. 
            //  Couplers (bicoupler, tricoupler, ...) are an exception, they only scan one attach point on the single attachment side,
            //  skip the points on the side where multiple points are. [Experiment]
            //  Again, the part creates union of scan lists from each of its neighbor and if it is not empty, returns this list. 
            //  The order in which mount points of a part are scanned appears to be fixed and defined by the part specification file. [Experiment]
            if (this.fuelCrossFeed)
            {
                lastCount = allSources.Count;
                //MonoBehaviour.print("foreach attach node");
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
                                    //if (log != null)
                                    //    log.buf.AppendLine(indent + "Attached part already visited, skipping (" + attachSim.attachedPartSim.name + ":" + attachSim.attachedPartSim.partId + ")");
                                }
                                else
                                {
                                    //if (log != null)
                                    //    log.buf.AppendLine(indent + "Adding attached part as source (" + attachSim.attachedPartSim.name + ":" + attachSim.attachedPartSim.partId + ")");

                                    attachSim.attachedPartSim.GetSourceSet(type, allParts, visited, allSources, log, indent);
                                }
                            }
                        }
                    }
                }

                if (allSources.Count > lastCount)
                {
                    if (log != null)
                    {
                        log.buf.AppendLine(indent + "Returning " + (allSources.Count - lastCount) + " attached sources (" + this.name + ":" + this.partId + ")");
                    }

                    return;
                }
            }

            // Rule 5: If the part is fuel container for searched type of fuel (i.e. it has capability to contain that type of fuel and the fuel 
            // type was not disabled [Experiment]) and it contains fuel, it returns itself.
            // Rule 6: If the part is fuel container for searched type of fuel (i.e. it has capability to contain that type of fuel and the fuel 
            // type was not disabled) but it does not contain the requested fuel, it returns empty list. [Experiment]
            if (this.resources.HasType(type) && this.resourceFlowStates[type] != 0)
            {
                if (this.resources[type] > SimManager.RESOURCE_MIN)
                {
                    allSources.Add(this);

                    if (log != null)
                    {
                        log.buf.AppendLine(indent + "Returning enabled tank as only source (" + this.name + ":" + this.partId + ")");
                    }
                }

                return;
            }

            // Rule 7: If the part is radially attached to another part and it is child of that part in the ship's tree structure, it scans its 
            // parent and returns whatever the parent scan returned. [Experiment] [Experiment]
            if (this.parent != null && this.parentAttach == AttachModes.SRF_ATTACH)
            {
                if (this.fuelCrossFeed)
                {
                    if (visited.Contains(this.parent))
                    {
                        //if (log != null)
                        //    log.buf.AppendLine(indent + "Parent part already visited, skipping (" + parent.name + ":" + parent.partId + ")");
                    }
                    else
                    {
                        lastCount = allSources.Count;
                        this.parent.GetSourceSet(type, allParts, visited, allSources, log, indent);
                        if (allSources.Count > lastCount)
                        {
                            if (log != null)
                            {
                                log.buf.AppendLine(indent + "Returning " + (allSources.Count  - lastCount) + " parent sources (" + this.name + ":" + this.partId + ")");
                            }

                            return;
                        }
                    }
                }
            }

            // Rule 8: If all preceding rules failed, part returns empty list.
            //if (log != null)
            //    log.buf.AppendLine(indent + "Returning empty set, no sources found (" + name + ":" + partId + ")");

            return;
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
        }

        public void DrainResources(double time)
        {
            //MonoBehaviour.print("DrainResources(" + name + ":" + partId + ", " + time + ")");
            for (int i = 0; i < this.resourceDrains.Types.Count; i++)
            {
                int type = this.resourceDrains.Types[i];
                //MonoBehaviour.print("draining " + (time * resourceDrains[type]) + " " + ResourceContainer.GetResourceName(type));
                this.resources.Add(type, -time * this.resourceDrains[type]);
                //MonoBehaviour.print(ResourceContainer.GetResourceName(type) + " left = " + resources[type]);
            }
        }

        public double TimeToDrainResource()
        {
            //MonoBehaviour.print("TimeToDrainResource(" + name + ":" + partId + ")");
            double time = double.MaxValue;

            for (int i = 0; i < this.resourceDrains.Types.Count; i++)
            {
                int type = this.resourceDrains.Types[i];
                if (this.resourceDrains[type] > 0)
                {
                    time = Math.Min(time, this.resources[type] / this.resourceDrains[type]);
                    //MonoBehaviour.print("type = " + ResourceContainer.GetResourceName(type) + "  amount = " + resources[type] + "  rate = " + resourceDrains[type] + "  time = " + time);
                }
            }

            //if (time < double.MaxValue)
            //    MonoBehaviour.print("TimeToDrainResource(" + name + ":" + partId + ") = " + time);
            return time;
        }

        public bool EmptyOf(HashSet<int> types)
        {
            foreach (int type in types)
            {
                if (this.resources.HasType(type) && this.resourceFlowStates[type] != 0 && (double)this.resources[type] > SimManager.RESOURCE_MIN)
                {
                    return false;
                }
            }

            return true;
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

        public double GetStartMass()
        {
            return this.startMass;
        }

        public double GetMass()
        {
            double mass = this.baseMass;

            for (int i = 0; i < this.resources.Types.Count; i++)
            {
                int type = this.resources.Types[i];
                mass += this.resources.GetResourceMass(type);
            }

            return mass;
        }

        public String DumpPartAndParentsToBuffer(StringBuilder buffer, String prefix)
        {
            if (this.parent != null)
            {
                prefix = this.parent.DumpPartAndParentsToBuffer(buffer, prefix) + " ";
            }

            this.DumpPartToBuffer(buffer, prefix);

            return prefix;
        }

        public void DumpPartToBuffer(StringBuilder buffer, String prefix, List<PartSim> allParts = null)
        {
            buffer.Append(prefix);
            buffer.Append(this.name);
            buffer.AppendFormat(":[id = {0:d}, decouple = {1:d}, invstage = {2:d}", this.partId, this.decoupledInStage, this.inverseStage);

            buffer.AppendFormat(", vesselName = '{0}'", this.vesselName);
            buffer.AppendFormat(", vesselType = {0}", SimManager.GetVesselTypeString(this.vesselType));
            buffer.AppendFormat(", initialVesselName = '{0}'", this.initialVesselName);

            buffer.AppendFormat(", fuelCF = {0}", this.fuelCrossFeed);
            buffer.AppendFormat(", noCFNKey = '{0}'", this.noCrossFeedNodeKey);

            buffer.AppendFormat(", isSep = {0}", this.isSepratron);

            for (int i = 0; i < this.resources.Types.Count; i++)
            {
                int type = this.resources.Types[i];
                buffer.AppendFormat(", {0} = {1:g6}", ResourceContainer.GetResourceName(type), this.resources[type]);
            }

            if (this.attachNodes.Count > 0)
            {
                buffer.Append(", attached = <");
                this.attachNodes[0].DumpToBuffer(buffer);
                for (int i = 1; i < this.attachNodes.Count; i++)
                {
                    buffer.Append(", ");
                    this.attachNodes[i].DumpToBuffer(buffer);
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
                    {
                        partSim.DumpPartToBuffer(buffer, newPrefix, allParts);
                    }
                }
            }
        }
    }
}