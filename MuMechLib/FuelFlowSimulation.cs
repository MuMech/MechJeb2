using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{




    public class FuelFlowSimulation
    {
        public int simStage; //the simulated rocket's current stage
        List<FuelNode> nodes; //a list of FuelNodes representing all the parts of the ship
        public float t;

        //Takes a list of parts so that the simulation can be run in the editor as well as the flight scene
        public FuelFlowSimulation(List<Part> parts)
        {
            //Initialize the simulation
            nodes = new List<FuelNode>();
            Dictionary<Part, FuelNode> nodeLookup = parts.ToDictionary(p => p, p => new FuelNode(p));
            nodes = nodeLookup.Values.ToList();

            foreach (Part p in parts) nodeLookup[p].FindSourceNodes(p, nodeLookup);

            simStage = Staging.lastStage;

            t = 0;
        }

        //Simulate the activation and execution of each stage of the rocket,
        //and return stats for each stage
        public Stats[] SimulateAllStages(float throttle, float atmospheres)
        {
            Stats[] stages = new Stats[simStage + 1];

            Debug.Log("SimulteAllStages");

            while (simStage >= 0)
            {
                Debug.Log("Simulating stage " + simStage);
                stages[simStage] = SimulateStage(throttle, atmospheres);
                SimulateStageActivation();
            }

            return stages;
        }

        //Simulate (the rest of) the current stage of the simulated rocket,
        //and return stats for the stage
        public Stats SimulateStage(float throttle, float atmospheres)
        {
            Stats stats = new Stats();
            stats.startMass = VesselMass();
            stats.startThrust = VesselThrust(throttle);

            const int maxSteps = 100;
            int step;
            for (step = 0; step < maxSteps; step++)
            {
                if (AllowedToStage()) break;
                float dt;
                stats = stats.Append(SimulateTimeStep(float.MaxValue, throttle, atmospheres, out dt));
            }

            Debug.Log("Finished stage " + simStage + " after " + step + " steps");
            if (step == maxSteps) throw new Exception("FuelFlowSimulation.SimulateStage reached max step count of " + maxSteps);

            return stats;
        }

        //Simulate a single time step, and return stats for the time step. 
        // - desiredDt is the requested time step size. Often the actual time step size
        //   with be less than this. The actual step size is reported in dt.
        public Stats SimulateTimeStep(float desiredDt, float throttle, float atmospheres, out float dt)
        {
            Stats stats = new Stats();

            stats.startMass = VesselMass();
            stats.startThrust = VesselThrust(throttle);

            foreach (FuelNode n in nodes) n.ResetDrainRates();
            
            List<FuelNode> engines = FindActiveEngines();
            foreach (FuelNode n in engines) n.SetConsumptionRates(throttle, atmospheres);
            foreach (FuelNode n in engines) n.AssignResourceDrainRates(nodes);
            foreach (FuelNode n in nodes) n.DebugDrainRates();

            float maxDt = nodes.Min(n => n.MaxTimeStep());
            dt = Mathf.Min(desiredDt, maxDt);

            Debug.Log("Simulating time step of " + dt);

            foreach (FuelNode n in nodes) n.DrainResources(dt);

            stats.deltaTime = dt;
            stats.endMass = VesselMass();
            //stats.endThrust = VesselThrust(throttle);
            stats.maxAccel = stats.startThrust / stats.endMass;
            stats.ComputeTimeStepDeltaV();

            t += dt;

            return stats;
        }

        //Active the next stage of the simulated rocket and remove all nodes that get decoupled by the new stage
        public void SimulateStageActivation()
        {
            simStage--;

            Debug.Log("Simulating activation of stage " + simStage);

            List<FuelNode> decoupledNodes = nodes.Where(n => n.decoupledInStage == simStage).ToList();

            foreach (FuelNode d in decoupledNodes) nodes.Remove(d); //remove the decoupled nodes from the simulated ship

            foreach (FuelNode n in nodes)
            {
                foreach (FuelNode d in decoupledNodes) n.RemoveSourceNode(d); //remove the decoupled nodes from the remaining nodes' source lists
            }
        }

        //Whether we've used up the current stage
        public bool AllowedToStage()
        {
            List<FuelNode> activeEngines = FindActiveEngines();

            //if no engines are active, we can always stage
            if (activeEngines.Count == 0)
            {
                Debug.Log("Allowed to stage because no active engines");
                return true;
            }

            //if staging would decouple an active engine or non-empty fuel tank, we're not allowed to stage
            foreach (FuelNode n in nodes)
            {
                if (n.decoupledInStage == (simStage - 1))
                {
                    if (n.ContainsResources() || (activeEngines.Contains(n) && !n.isSepratron))
                    {
                        Debug.Log("Not allowed to stage because " + n.partName + " either contains resources or is an active engine");
                        return false;
                    }
                }
            }

            //if this isn't the last stage, we're allowed to stage because doing so wouldn't drop anything important
            if (simStage > 0)
            {
                Debug.Log("Allowed to stage because this isn't the last stage");
                return true;
            }

            Debug.Log("Not allowed to stage because there are active engines and this is the last stage");

            //if this is the last stage, we're not allowed to stage while there are still active engines
            return false;
        }

        public float VesselMass()
        {
            return nodes.Sum(n => n.Mass);
        }

        public float VesselThrust(float throttle)
        {
            return throttle * FindActiveEngines().Sum(eng => eng.maxThrust);
        }

        //Returns a list of engines that fire during the current simulated stage.
        public List<FuelNode> FindActiveEngines()
        {
            return nodes.Where(n => n.isEngine && n.inverseStage >= simStage && n.CanDrawNeededResources(nodes)).ToList();
        }




        //A Stats struct describes the result of the simulation over a certain interval of time (e.g., one stage)
        public struct Stats
        {
            public float startMass;
            public float endMass;
            public float startThrust;
            public float maxAccel;
            public float deltaTime;
            public float deltaV;

            public double StartTWR(CelestialBody body) { return startThrust / (9.81 * body.GeeASL * startMass); }
            public double MaxTWR(CelestialBody body) { return maxAccel / (9.81 * body.GeeASL); }

            //Computes the deltaV from the other fields. Only valid when the thrust is constant over the time interval represented.
            public void ComputeTimeStepDeltaV()
            {
                if (deltaTime > 0 && startMass > endMass && startMass > 0 && endMass > 0)
                {
                    deltaV = startThrust * deltaTime / (startMass - endMass) * Mathf.Log(startMass / endMass);
                }
                else
                {
                    deltaV = 0;
                }
            }

            //Append joins two Stats describing adjacent intervals of time into one describing the combined interval
            public Stats Append(Stats s) 
            {
                return new Stats 
                { 
                    startMass = this.startMass, endMass = s.endMass, 
                    startThrust = this.startThrust,
                    maxAccel = Mathf.Max(this.maxAccel, s.maxAccel),
                    deltaTime = this.deltaTime + s.deltaTime, deltaV = this.deltaV + s.deltaV
                };
            }
        }
    }


    //A FuelNode is a compact summary of a Part, containing only the information needed to run the fuel flow simulation. 
    public class FuelNode
    {
        DefaultableDictionary<int, float> resources = new DefaultableDictionary<int, float>(0);       //the resources contained in the part
        Dictionary<int, float> resourceConsumptions = new Dictionary<int, float>();                   //the resources this part consumes per unit time when active at full throttle
        DefaultableDictionary<int, float> resourceDrains = new DefaultableDictionary<int, float>(0);  //the resources being drained from this part per unit time at the current simulation time

        const float DRAINED = 1.0f; //if a resource amount falls below this amount we say that the resource has been drained

        FloatCurve ispCurve;                     //the function that gives Isp as a function of atmospheric pressure for this part, if it's an engine
        Dictionary<int, float> propellantRatios; //ratios of propellants used by this engine
        float propellantSumRatioTimesDensity;    //a number used in computing propellant consumption rates

        HashSet<FuelNode> sourceNodes = new HashSet<FuelNode>();  //a set of FuelNodes that this node could draw fuel or resources from (for resources that use the ResourceFlowMode STACK_PRIORITY_SEARCH).
        FuelNode parent;
        List<int> resourcesUnobtainableFromParent = new List<int>();
        bool surfaceMounted;

        public float maxThrust = 0;     //max thrust of this part
        public int decoupledInStage;    //the stage in which this part will be decoupled from the rocket
        public int inverseStage;        //stage in which this part is activated
        public bool isSepratron;        //whether this part is a sepratron
        public bool isEngine = false;   //whether this part is an engine

        bool isFuelLine; //whether this part is a fuel line

        float dryMass = 0; //the mass of this part, not counting resource mass

        public string partName; //for debugging

        public FuelNode(Part part)
        {
            if(part.physicalSignificance != Part.PhysicalSignificance.NONE) dryMass = part.mass;
            inverseStage = part.inverseStage;
            isFuelLine = (part is FuelLine);
            isSepratron = part.IsSepratron();
            partName = part.partInfo.name;

            //note which resources this part has stored
            foreach (PartResource r in part.Resources)
            {
                if(r.info.name != "ElectricCharge") resources[r.info.id] = (float)r.amount;
                resourcesUnobtainableFromParent.Add(r.info.id);
            }

            //record relevant engine stats
            if(part.HasModule<ModuleEngines>()) 
            {
                isEngine = true;
                ModuleEngines engine = part.Modules.OfType<ModuleEngines>().First();

                maxThrust = engine.maxThrust;
                ispCurve = engine.atmosphereCurve;

                propellantSumRatioTimesDensity = engine.propellants.Sum(prop => prop.ratio * MuUtils.ResourceDensity(prop.id));
                propellantRatios = engine.propellants.Where(prop => prop.name != "ElectricCharge").ToDictionary(prop => prop.id, prop => prop.ratio);
            }

            //figure out when this part gets decoupled. We do this by looking through this part and all this part's ancestors
            //and noting which one gets decoupled earliest (i.e., has the highest inverseStage). Some parts never get decoupled
            //and these are assigned decoupledInStage = -1.
            decoupledInStage = -1;
            Part p = part;
            while (true)
            {
                if (p.IsDecoupler())
                {
                    if (p.inverseStage > decoupledInStage) decoupledInStage = p.inverseStage;
                }
                if (p.parent == null) break;
                else p = p.parent;
            }
        }

        public void SetConsumptionRates(float throttle, float atmospheres)
        {
            if (isEngine)
            {
                float Isp = ispCurve.Evaluate(atmospheres);
                float massFlowRate = (throttle * maxThrust) / (Isp * 9.81f);

                //propellant consumption rate = ratio * massFlowRate / sum(ratio * density)
                resourceConsumptions = propellantRatios.Keys.ToDictionary(id => id, id => propellantRatios[id] * massFlowRate / propellantSumRatioTimesDensity);
            }
        }

        //Find the set of nodes from which we can draw resources according to the STACK_PRIORITY_SEARCH flow scheme.
        //This gets called after all the FuelNodes have been constructed in order to set up the fuel flow graph
        public void FindSourceNodes(Part part, Dictionary<Part, FuelNode> nodeLookup)
        {
            //we can draw fuel from any fuel lines that point to this part
            foreach (Part p in nodeLookup.Keys)
            {
                if (p is FuelLine && ((FuelLine)p).target == part)
                {
                    sourceNodes.Add(nodeLookup[p]);
                }
            }

            surfaceMounted = true;
            if (part.parent != null) this.parent = nodeLookup[part.parent];

            //we can (sometimes) draw fuel from stacked parts
            foreach (AttachNode attachNode in part.attachNodes)
            {
                //decide if it's possible to draw fuel through this node:
                if (attachNode.attachedPart != null                            //if there is a part attached here            
                    && attachNode.nodeType == AttachNode.NodeType.Stack        //and the attached part is stacked (rather than surface mounted)
                    && attachNode.attachedPart.fuelCrossFeed                   //and the attached part allows fuel flow
                    && !(part.NoCrossFeedNodeKey.Length > 0                    //and this part does not forbid fuel flow
                         && attachNode.id.Contains(part.NoCrossFeedNodeKey)))  //    through this particular node
                {
                    sourceNodes.Add(nodeLookup[attachNode.attachedPart]);
                    if (attachNode.attachedPart == part.parent) surfaceMounted = false;
                }
            }

            //Parts can draw resources from their parents
            //(exception: surface mounted fuel tanks cannot)
            if (part.parent != null && part.parent.fuelCrossFeed) sourceNodes.Add(nodeLookup[part.parent]);
        }

        //call this when a node no longer exists, so that this node knows that it's no longer a valid source
        public void RemoveSourceNode(FuelNode n)
        {
            if (sourceNodes.Contains(n)) sourceNodes.Remove(n);
        }


        //return the mass of the simulated FuelNode. This is not the same as the mass of the Part,
        //because the simulated node may have lost resources, and thus mass, during the simulation.
        public float Mass
        {
            get
            {
                return dryMass + resources.Keys.Sum(id => resources[id] * MuUtils.ResourceDensity(id));
            }
        }


        public void ResetDrainRates()
        {
            resourceDrains.Clear();
        }

        public void DrainResources(float dt)
        {
            foreach (int type in resourceDrains.Keys) resources[type] -= dt * resourceDrains[type];
        }
        
        public float MaxTimeStep()
        {
            if (resourceDrains.Keys.Where(id => resources[id] > DRAINED).Count() == 0) return float.MaxValue;
            Debug.Log("resourceDrains.Keys.Where(id => resources[id] > DRAINED).Count() = " + resourceDrains.Keys.Where(id => resources[id] > DRAINED).Count());
            return resourceDrains.Keys.Where(id => resources[id] > DRAINED).Min(id => resources[id] / resourceDrains[id]);
        }

        public bool ContainsResources()
        {
            //Todo: need to fix this to ignore unimportant resources like monopropellant
            return resources.Values.Any(amount => amount > DRAINED);
        }

        public bool CanDrawNeededResources(List<FuelNode> vessel)
        {
            foreach (int type in resourceConsumptions.Keys)
            {
                switch (PartResourceLibrary.Instance.GetDefinition(type).resourceFlowMode)
                {
                    case ResourceFlowMode.NO_FLOW:
                        //check if we contain the needed resource:
                        if (resources[type] < DRAINED) return false;
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                        //check if any part contains the needed resource:
                        if (!vessel.Any(n => n.resources[type] > DRAINED)) return false;
                        break;

                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        if (!this.CanSupplyResourceRecursive(type, new List<FuelNode>())) return false;
                        break;

                    default:
                        //do nothing. there's an EVEN_FLOW scheme but nothing seems to use it
                        return false;
                }
            }
            return true; //we didn't find ourselves lacking for any resource
        }

        public void DebugDrainRates()
        {
            foreach (int type in resourceDrains.Keys)
            {
                Debug.Log(partName + "'s drain rate of " + PartResourceLibrary.Instance.GetDefinition(type).name + " is " + resourceDrains[type]);
            }
        }

        public void AssignResourceDrainRates(List<FuelNode> vessel)
        {
            foreach (int type in resourceConsumptions.Keys)
            {
                float amount = resourceConsumptions[type];

                switch (PartResourceLibrary.Instance.GetDefinition(type).resourceFlowMode)
                {
                    case ResourceFlowMode.NO_FLOW:
                        resourceDrains[type] += amount;
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                        AssignFuelDrainRateAllVessel(type, amount, vessel);
                        break;

                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        Debug.Log(partName + " starting to assign drain of " + amount + " of " + PartResourceLibrary.Instance.GetDefinition(type).name);
                        AssignFuelDrainRateRecursive(type, amount, new List<FuelNode>());
                        break;

                    default:
                        //do nothing. there's an EVEN_FLOW scheme but nothing seems to use it
                        break;
                }
            }
        }


        void AssignFuelDrainRateAllVessel(int type, float amount, List<FuelNode> vessel)
        {
            //I don't know how this flow scheme actually works but I'm going to assume
            //that it drains from the part with the highest possible inverseStage
            FuelNode source = null;
            foreach (FuelNode n in vessel)
            {
                if (n.resources[type] > DRAINED)
                {
                    if (source == null || n.inverseStage > source.inverseStage) source = n;
                }
            }
            if (source != null) source.resourceDrains[type] += amount;
        }



        //We need to drain <totalDrainRate> of resource <type> per second from somewhere.
        //We're not allowed to drain it through any of the nodes in <visited>.
        //Decide whether to drain it from this node, or pass the recursive buck
        //and drain it from some subset of the sources of this node.
        void AssignFuelDrainRateRecursive(int type, float amount, List<FuelNode> visited)
        {
            Debug.Log(partName + ".AssignFuelDrainRateRecursive(" + (PartResourceLibrary.Instance.GetDefinition(type).name) + ", " + amount + ") (visited.Count = " + visited.Count + ")");

            //if we drain from our sources, newVisted is the set of nodes that those sources
            //aren't allowed to drain from. We add this node to that list to prevent loops.
            List<FuelNode> newVisited = new List<FuelNode>(visited);
            newVisited.Add(this);

            //First see if we can drain fuel through fuel lines. If we can, drain equally through
            //all active fuel lines that point to this part. 
            List<FuelNode> fuelLines = new List<FuelNode>();
            foreach (FuelNode n in sourceNodes)
            {
                if (n.isFuelLine && !visited.Contains(n))
                {
                    if (n.CanSupplyResourceRecursive(type, newVisited))
                    {
                        fuelLines.Add(n);
                    }
                    else
                    {
                        Debug.Log("--fuel line can't supply me");
                    }
                }
            }
            if (fuelLines.Count > 0)
            {
                Debug.Log("--draining from " + fuelLines.Count + " fuel lines");
                foreach (FuelNode fuelLine in fuelLines)
                {
                    fuelLine.AssignFuelDrainRateRecursive(type, amount / fuelLines.Count, newVisited);
                }
                return;
            }

            //If there are no incoming fuel lines, try other sources.
            //I think there may actually be more structure to the fuel source priority system here. 
            //For instance, can't a fuel tank drain fuel simultaneously from its top and bottom stack nodes?
            foreach (FuelNode n in sourceNodes)
            {
                if (!visited.Contains(n))
                {
                    //Fuel tanks cannot draw from their parents if they are surface mounted:
                    if (!(surfaceMounted && n == parent && resourcesUnobtainableFromParent.Contains(type)))
                    {
                        if (n.CanSupplyResourceRecursive(type, newVisited))
                        {
                            Debug.Log("--passing the recursive buck to " + n.partName);
                            n.AssignFuelDrainRateRecursive(type, amount, newVisited);
                            return;
                        }
                        else
                        {
                            Debug.Log("--" + n.partName + " can't supply me");
                        }
                    }
                    else
                    {
                        Debug.Log("--can't drain from " + n.partName + " because I'm a surface mount fuel tank");
                    }
                }
            }

            //in the final extremity, drain the resource from this part
            if (this.resources[type] > DRAINED)
            {
                Debug.Log("--draining from self");
                this.resourceDrains[type] += amount;
            }
        }



        //determine if this FuelNode can supply fuel itself, or can supply fuel by drawing
        //from other sources, without drawing through any node in <visited>
        bool CanSupplyResourceRecursive(int type, List<FuelNode> visited)
        {
            if (resources[type] > DRAINED) return true;

            //if we drain from our sources, newVisted is the set of nodes that those sources
            //aren't allowed to drain from. We add this node to that list to prevent loops.
            List<FuelNode> newVisited = new List<FuelNode>(visited);
            newVisited.Add(this);

            foreach (FuelNode n in sourceNodes)
            {
                if (!visited.Contains(n))
                {
                    //Fuel tanks cannot draw from their parents if they are surface mounted:
                    if (!(surfaceMounted && n == parent && resourcesUnobtainableFromParent.Contains(type)))
                    {
                        if (n.CanSupplyResourceRecursive(type, newVisited)) return true;
                    }
                }
            }

            return false;
        }
    }

}
