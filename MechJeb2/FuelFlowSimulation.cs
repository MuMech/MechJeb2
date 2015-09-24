using System;
using System.Collections.Generic;
using System.Linq;
using CompoundParts;
using UnityEngine;

namespace MuMech
{
    public class FuelFlowSimulation
    {
        public int simStage; //the simulated rocket's current stage
        readonly List<FuelNode> nodes; //a list of FuelNodes representing all the parts of the ship
        public float t;

        //Takes a list of parts so that the simulation can be run in the editor as well as the flight scene
        public FuelFlowSimulation(List<Part> parts, bool dVLinearThrust)
        {
            // Create FuelNodes corresponding to each Part
            nodes = new List<FuelNode>();
            Dictionary<Part, FuelNode> nodeLookup = parts.ToDictionary(p => p, p => new FuelNode(p, dVLinearThrust));
            nodes = nodeLookup.Values.ToList();

            // Determine when each part will be decoupled
            Part rootPart = parts[0]; // hopefully always correct
            nodeLookup[rootPart].AssignDecoupledInStage(rootPart, nodeLookup, -1);

            // Set up the fuel flow graph
            if (HighLogic.LoadedSceneIsFlight)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    Part p = parts[i];
                    nodeLookup[p].SetupFuelLineSourcesFlight(p, nodeLookup);
                }
            }
            else
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    Part p = parts[i];
                    nodeLookup[p].SetupFuelLineSourcesFlight(p, nodeLookup);
                    nodeLookup[p].SetupFuelLineSourcesEditor(p, nodeLookup);
                }
            }
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                nodeLookup[p].SetupRegularSources(p, nodeLookup);
            }


            simStage = Staging.lastStage + 1;

            // Add a fake stage if we are beyond the first one
            // Mostly usefull for the Node Executor who use the last stage info
            // and fail to get proper info when the ship was never staged and
            // some engine were activated manually
            if (Staging.CurrentStage > Staging.lastStage)
                simStage++;

            t = 0;
        }

        //Simulate the activation and execution of each stage of the rocket,
        //and return stats for each stage
        public Stats[] SimulateAllStages(float throttle, double staticPressure, double atmDensity, double machNumber)
        {
            Stats[] stages = new Stats[simStage];

            int maxStages = simStage - 1;

            //print("SimulateAllStages starting from stage " + simStage + "; ticks from start = " + (Environment.TickCount - startTick));
            SimulateStageActivation();

            while (simStage >= 0)
            {
                //print("Simulating stage " + simStage + "(vessel mass = " + VesselMass() + ")");
                stages[simStage] = SimulateStage(throttle, staticPressure, atmDensity, machNumber);
                if (simStage != maxStages)
                    stages[simStage].stagedMass = stages[simStage + 1].endMass - stages[simStage].startMass;
                //print("Staging at t = " + t);
                SimulateStageActivation();
            }

            //print("SimulateAllStages ended");

            return stages;
        }

        public static void print(object message)
        {
            MonoBehaviour.print("[MechJeb2] " + message);
        }

        //Simulate (the rest of) the current stage of the simulated rocket,
        //and return stats for the stage
        public Stats SimulateStage(float throttle, double staticPressure, double atmDensity, double machNumber)
        {
            //need to set initial consumption rates for VesselThrust and AllowedToStage to work right
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].SetConsumptionRates(throttle, atmDensity, machNumber);
            }

            Stats stats = new Stats();
            stats.startMass = VesselMass(simStage);
            stats.startThrust = VesselThrust(throttle, staticPressure, atmDensity, machNumber);
            stats.endMass = stats.startMass;
            stats.resourceMass = 0;
            stats.maxAccel = stats.endMass > 0 ? stats.startThrust / stats.endMass : 0;
            stats.deltaTime = 0;
            stats.deltaV = 0;

            const int maxSteps = 100;
            int step;
            for (step = 0; step < maxSteps; step++)
            {
                if (AllowedToStage()) break;
                float dt;
                stats = stats.Append(SimulateTimeStep(float.MaxValue, throttle, staticPressure, atmDensity, machNumber, out dt));
            }

            //print("Finished stage " + simStage + " after " + step + " steps");
            if (step == maxSteps) throw new Exception("FuelFlowSimulation.SimulateStage reached max step count of " + maxSteps);

            return stats;
        }

        //Simulate a single time step, and return stats for the time step.
        // - desiredDt is the requested time step size. Often the actual time step size
        //   with be less than this. The actual step size is reported in dt.
        public Stats SimulateTimeStep(float desiredDt, float throttle, double staticPressure, double atmDensity, double machNumber, out float dt)
        {
            Stats stats = new Stats();

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].ResetDrainRates();
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].SetConsumptionRates(throttle, atmDensity, machNumber);
            }

            stats.startMass = VesselMass(simStage);
            stats.startThrust = VesselThrust(throttle, staticPressure, atmDensity, machNumber); // NK

            List<FuelNode> engines = FindActiveEngines();

            if (engines.Count > 0)
            {
                for (int i = 0; i < engines.Count; i++)
                {
                    engines[i].AssignResourceDrainRates(nodes);
                }
                //foreach (FuelNode n in nodes) n.DebugDrainRates();

                float maxDt = nodes.Min(n => n.MaxTimeStep());
                dt = Mathf.Min(desiredDt, maxDt);

                //print("Simulating time step of " + dt);

                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].DrainResources(dt);
                }
            }
            else
            {
                dt = 0;
            }

            stats.deltaTime = dt;
            stats.endMass = VesselMass(simStage);
            stats.resourceMass = stats.startMass - stats.endMass;
            stats.maxAccel = stats.endMass > 0 ? stats.startThrust / stats.endMass : 0;
            stats.ComputeTimeStepDeltaV();
            stats.isp = stats.startMass > stats.endMass ? stats.deltaV / (9.80665f * Mathf.Log(stats.startMass / stats.endMass)) : 0;

            t += dt;

            return stats;
        }

        //Active the next stage of the simulated rocket and remove all nodes that get decoupled by the new stage
        public void SimulateStageActivation()
        {
            simStage--;

            List<FuelNode> decoupledNodes = nodes.Where(n => n.decoupledInStage == simStage).ToList();

            for (int i = 0; i < decoupledNodes.Count; i++)
            {
                nodes.Remove(decoupledNodes[i]); //remove the decoupled nodes from the simulated ship
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < decoupledNodes.Count; j++)
                {
                    nodes[i].RemoveSourceNode(decoupledNodes[j]); //remove the decoupled nodes from the remaining nodes' source lists
                }
            }
        }

        //Whether we've used up the current stage
        public bool AllowedToStage()
        {
            //print("Checking whether allowed to stage at t = " + t);

            List<FuelNode> activeEngines = FindActiveEngines();

            //print("  activeEngines.Count = " + activeEngines.Count);

            //if no engines are active, we can always stage
            if (activeEngines.Count == 0)
            {
                //print("Allowed to stage because no active engines");
                return true;
            }

            List<int> burnedResources = activeEngines.SelectMany(eng => eng.BurnedResources()).Distinct().ToList();

            //if staging would decouple an active engine or non-empty fuel tank, we're not allowed to stage
            for (int i = 0; i < nodes.Count; i++)
            {
                FuelNode n = nodes[i];
//print(n.partName + " is sepratron? " + n.isSepratron);
                if (n.decoupledInStage == (simStage - 1) && !n.isSepratron)
                {
                    if (activeEngines.Contains(n) || n.ContainsResources(burnedResources))
                    {
                        //print("Not allowed to stage because " + n.partName + " either contains resources or is an active engine");
                        return false;
                    }
                }
            }

            // We are not allowed to stage if the stage does not decouple anything, and there is an active engine that still has access to resources
            {
                bool activeEnginesWorking = false;
                bool partDecoupledInNextStage = false;

                for (int i = 0; i < nodes.Count; i++)
                {
                    FuelNode n = nodes[i];
                    if (activeEngines.Contains(n))
                    {
                        if (n.CanDrawNeededResources(nodes))
                        {
                            //print("Part " + n.partName + " is an active engine that still has resources to draw on.");
                            activeEnginesWorking = true;
                        }
                    }

                    if (n.decoupledInStage == (simStage - 1))
                    {
                        //print("Part " + n.partName + " is decoupled in the next stage.");

                        partDecoupledInNextStage = true;
                    }
                }

                if (!partDecoupledInNextStage && activeEnginesWorking)
                {
                    //print("Not allowed to stage because nothing is decoupled in the enst stage, and there are already other engines active.");
                    return false;
                }
            }

            //if this isn't the last stage, we're allowed to stage because doing so wouldn't drop anything important
            if (simStage > 0)
            {
                //print("Allowed to stage because this isn't the last stage");
                return true;
            }

            //print("Not allowed to stage because there are active engines and this is the last stage");

            //if this is the last stage, we're not allowed to stage while there are still active engines
            return false;
        }

        public float VesselMass(int simStage)
        {
            float sum = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                sum += nodes[i].Mass(simStage);
            }
            return sum;
        }

        public float VesselThrust(float throttle, double staticPressure, double atmDensity, double machNumber)
        {
            return FindActiveEngines().Sum(eng => eng.EngineThrust(throttle, staticPressure, atmDensity, machNumber));
        }

        //Returns a list of engines that fire during the current simulated stage.
        public List<FuelNode> FindActiveEngines()
        {
            //print("Finding active engines: excluding resource considerations, there are " + nodes.Count(n => n.isEngine && n.inverseStage >= simStage));
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

            public float resourceMass;
            public float isp;
            public float stagedMass;

            public double StartTWR(double geeASL) { return startMass > 0 ? startThrust / (9.80665 * geeASL * startMass) : 0; }
            public double MaxTWR(double geeASL) { return maxAccel / (9.80665 * geeASL); }

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
                    startMass = this.startMass,
                    endMass = s.endMass,
                    resourceMass = startMass - s.endMass,
                    startThrust = this.startThrust,
                    maxAccel = Mathf.Max(this.maxAccel, s.maxAccel),
                    deltaTime = this.deltaTime + s.deltaTime,
                    deltaV = this.deltaV + s.deltaV,
                    isp = this.startMass == s.endMass ? 0 : (this.deltaV + s.deltaV) / (9.80665f * Mathf.Log(this.startMass / s.endMass))
                };
            }
        }
    }

    //A FuelNode is a compact summary of a Part, containing only the information needed to run the fuel flow simulation.
    public class FuelNode
    {
        readonly DefaultableDictionary<int, float> resources = new DefaultableDictionary<int, float>(0);       //the resources contained in the part
        Dictionary<int, float> resourceConsumptions = new Dictionary<int, float>();                   //the resources this part consumes per unit time when active at full throttle
        readonly DefaultableDictionary<int, float> resourceDrains = new DefaultableDictionary<int, float>(0);  //the resources being drained from this part per unit time at the current simulation time

        // if a resource amount falls below this amount we say that the resource has been drained
        // set to the smallest amount that the user can see is non-zero in the resource tab or by
        // right-clicking.
        static readonly float DRAINED = 0.005f;

        readonly FloatCurve atmosphereCurve;  //the function that gives Isp as a function of atmospheric pressure for this part, if it's an engine
        readonly bool atmChangeFlow;
        readonly bool useAtmCurve;
        readonly FloatCurve atmCurve;
        readonly bool useVelCurve;
        readonly FloatCurve velCurve;

        readonly Dictionary<int, float> propellantRatios; //ratios of propellants used by this engine
        readonly float propellantSumRatioTimesDensity;    //a number used in computing propellant consumption rates

        readonly List<FuelNode> fuelLineSources = new List<FuelNode>();
        readonly List<FuelNode> stackNodeSources = new List<FuelNode>();
        FuelNode surfaceMountParent = null;

        readonly float maxFuelFlow = 0;     //max fuel flow of this part
        readonly float minFuelFlow = 0;     //min fuel flow of this part

        readonly float thrustPercentage = 0;

        readonly float fwdThrustRatio = 1; // % of thrust moving the ship forwad
        readonly float g;                  // value of g used for engine flow rate / isp

        public int decoupledInStage;    //the stage in which this part will be decoupled from the rocket
        public int inverseStage;        //stage in which this part is activated
        public bool isSepratron;        //whether this part is a sepratron
        public bool isEngine = false;   //whether this part is an engine

        readonly float dryMass = 0; //the mass of this part, not counting resource mass
        readonly float fairingMass = 0; //the mass of the fairing of this part

        public string partName; //for debugging

        public float moduleMass; // for debugging

        public FuelNode(Part part, bool dVLinearThrust)
        {
            if (!part.IsLaunchClamp())
            {
                //print(part.partInfo.name.PadRight(25) + " " + part.mass.ToString("F4") + " " + part.GetPhysicslessChildMass().ToString("F4") + " " + part.GetModuleMass(part.partInfo.partPrefab.mass).ToString("F4"));
                dryMass = part.mass; // Intentionally ignore the physic flag.

                moduleMass = part.GetModuleMass(part.partInfo.partPrefab.mass);
                if (part.HasModule<ModuleProceduralFairing>())
                {
                    fairingMass = moduleMass;
                }
            }

            inverseStage = part.inverseStage;
            partName = part.partInfo.name;

            //note which resources this part has stored
            for (int i = 0; i < part.Resources.Count; i++)
            {
                PartResource r = part.Resources[i];
                if (r.info.density > 0 && r.info.name != "IntakeAir")
                {
                    if (r.flowState)
                    {
                        resources[r.info.id] = (float) r.amount;
                    }
                    else
                    {
                        dryMass += (float) (r.amount*r.info.density); // disabled resources are just dead weight
                    }
                }
            }

            // TODO : handle the multiple active ModuleEngine case ( SXT engines with integrated vernier )

            //record relevant engine stats
            ModuleEngines engine = part.Modules.OfType<ModuleEngines>().FirstOrDefault(e => e.isEnabled);
            if (engine != null)
            {
                //Only count engines that either are ignited or will ignite in the future:
                if ((HighLogic.LoadedSceneIsEditor || inverseStage < Staging.CurrentStage || engine.getIgnitionState) && (engine.thrustPercentage > 0 || engine.minThrust > 0))
                {
                    //if an engine has been activated early, pretend it is in the current stage:
                    if (engine.getIgnitionState && inverseStage < Staging.CurrentStage)
                        inverseStage = Staging.CurrentStage;

                    isEngine = true;

                    g = engine.g;

                    // If we take into account the engine rotation
                    if (dVLinearThrust)
                    {
                        Vector3 thrust = Vector3d.zero;
                        for (int i = 0; i < engine.thrustTransforms.Count; i++)
                        {
                            thrust -= engine.thrustTransforms[i].forward/engine.thrustTransforms.Count;
                        }

                        Vector3d fwd = HighLogic.LoadedScene == GameScenes.EDITOR ? EditorLogic.VesselRotation * Vector3d.up : engine.part.vessel.GetTransform().up;
                        fwdThrustRatio = Vector3.Dot(fwd, thrust);
                    }

                    thrustPercentage = engine.thrustPercentage;

                    minFuelFlow = engine.minFuelFlow;
                    maxFuelFlow = engine.maxFuelFlow;

                    atmosphereCurve = new FloatCurve(engine.atmosphereCurve.Curve.keys);
                    atmChangeFlow = engine.atmChangeFlow;
                    useAtmCurve = engine.useAtmCurve;
                    if (useAtmCurve)
                        atmCurve = new FloatCurve(engine.atmCurve.Curve.keys);
                    useVelCurve = engine.useVelCurve;
                    if (useAtmCurve)
                        velCurve = new FloatCurve(engine.velCurve.Curve.keys);

                    propellantSumRatioTimesDensity = engine.propellants.Where(prop => !prop.ignoreForIsp).Sum(prop => prop.ratio * MuUtils.ResourceDensity(prop.id));
                    propellantRatios = engine.propellants.Where(prop => MuUtils.ResourceDensity(prop.id) > 0 && !prop.ignoreForIsp ).ToDictionary(prop => prop.id, prop => prop.ratio);
                }
            }
        }

        // Determine when this part will be decoupled given when its parent will be decoupled.
        // Then recurse to all of this part's children.
        public void AssignDecoupledInStage(Part p, Dictionary<Part, FuelNode> nodeLookup, int parentDecoupledInStage)
        {
            if (p.IsUnfiredDecoupler() || p.IsLaunchClamp())
            {
                decoupledInStage = p.inverseStage > parentDecoupledInStage ? p.inverseStage : parentDecoupledInStage;
            }
            else
            {
                decoupledInStage = parentDecoupledInStage;
            }

            isSepratron = isEngine && (inverseStage == decoupledInStage);

            for (int i = 0; i < p.children.Count; i++)
            {
                Part child = p.children[i];
                nodeLookup[child].AssignDecoupledInStage(child, nodeLookup, decoupledInStage);
            }
        }

        public static void print(object message)
        {
            MonoBehaviour.print("[MechJeb2] " + message);
        }

        public void SetConsumptionRates(float throttle, double atmDensity, double machNumber)
        {
            if (isEngine)
            {
                float flowModifier = GetFlowModifier(atmDensity, machNumber);

                float massFlowRate = Mathf.Lerp(minFuelFlow, maxFuelFlow, throttle * 0.01f * thrustPercentage) * flowModifier;

                //propellant consumption rate = ratio * massFlowRate / sum(ratio * density)
                resourceConsumptions = propellantRatios.Keys.ToDictionary(id => id, id => propellantRatios[id] * massFlowRate / propellantSumRatioTimesDensity);
            }
        }

        public void SetupFuelLineSourcesFlight(Part part, Dictionary<Part, FuelNode> nodeLookup)
        {
            // In the flight scene, each part knows which fuel lines point to it.
            // (actually, fuelLookupTargets also includes attached docking nodes that can
            // transfer fuel to us).
            for (int i = 0; i < part.fuelLookupTargets.Count; i++)
            {
                FuelNode nodeSource;
                if (nodeLookup.TryGetValue(part.fuelLookupTargets[i], out nodeSource))
                {
                    fuelLineSources.Add(nodeSource);
                }
            }
        }

        public void SetupFuelLineSourcesEditor(Part part, Dictionary<Part, FuelNode> nodeLookup)
        {
            // In the editor scene, fuel lines have to inform their targets that they
            // are valid fuel sources (and in the editor docking nodes attach via regular stack nodes,
            // so they need no special treatment).
            CModuleFuelLine fuelLine = part.GetModule<CModuleFuelLine>();
            if (fuelLine != null && fuelLine.target != null)
            {
                FuelNode targetNode;
                if (nodeLookup.TryGetValue(fuelLine.target, out targetNode))
                    targetNode.fuelLineSources.Add(this);
            }
        }

        // Find the set of nodes from which we can draw resources according to the STACK_PRIORITY_SEARCH flow scheme.
        // This gets called after all the FuelNodes have been constructed in order to set up the fuel flow graph.
        // Note that fuel flow through fuel lines and docked docking nodes is set up separately in
        // SetupFuelLineSources*()
        public void SetupRegularSources(Part part, Dictionary<Part, FuelNode> nodeLookup)
        {
            // When fuelCrossFeed is enabled we can draw fuel through stack and surface attachment
            if (part.fuelCrossFeed)
            {
                // Stack nodes:
                for (int i = 0; i < part.attachNodes.Count; i++)
                {
                    AttachNode attachNode = part.attachNodes[i];
                    if (attachNode.attachedPart != null)
                    {
                        // For stack nodes, we can draw fuel unless this node is specifically
                        // labeled as having crossfeed disabled (Kashua rule #4)
                        FuelNode fuelnode;
                        if (attachNode.id != "Strut"
                            && attachNode.ResourceXFeed
                            && !(part.NoCrossFeedNodeKey.Length > 0
                                 && attachNode.id.Contains(part.NoCrossFeedNodeKey))
                            && nodeLookup.TryGetValue(attachNode.attachedPart, out fuelnode))
                        {
                            stackNodeSources.Add(fuelnode);
                        }
                    }
                }

                // If we are surface-mounted to our parent we can draw fuel from it (Kashua rule #7)
                if (part.attachMode == AttachModes.SRF_ATTACH && part.parent != null)
                {
                    surfaceMountParent = nodeLookup[part.parent];
                }
            }
        }

        //call this when a node no longer exists, so that this node knows that it's no longer a valid source
        public void RemoveSourceNode(FuelNode n)
        {
            if (fuelLineSources.Contains(n)) fuelLineSources.Remove(n);
            if (stackNodeSources.Contains(n)) stackNodeSources.Remove(n);
            if (surfaceMountParent == n) surfaceMountParent = null;
        }


        //return the mass of the simulated FuelNode. This is not the same as the mass of the Part,
        //because the simulated node may have lost resources, and thus mass, during the simulation.
        public float Mass(int simStage)
        {
            //print("\n(" + simStage + ") " + partName.PadRight(25) + " dryMass " + dryMass.ToString("F3")
            //          + " ResMass " + (resources.Keys.Sum(id => resources[id] * MuUtils.ResourceDensity(id))).ToString("F3")
            //          + " Fairing Mass " + (inverseStage < simStage ? fairingMass : 0).ToString("F3")
            //          + " (" + fairingMass.ToString("F3") + ")"
            //          + " ModuleMass " + moduleMass.ToString("F3")
            //          );

            return dryMass + resources.Keys.Sum(id => resources[id] * MuUtils.ResourceDensity(id)) +
                   (inverseStage < simStage ? fairingMass : 0);
        }

        public float EngineThrust(float throttle, double atmospheres, double atmDensity, double machNumber)
        {
            float Isp = atmosphereCurve.Evaluate((float)atmospheres);

            float flowModifier = GetFlowModifier(atmDensity, machNumber);

            float thrust = Mathf.Lerp(minFuelFlow, maxFuelFlow, throttle * 0.01f * thrustPercentage) * flowModifier * Isp * g;

            return thrust * fwdThrustRatio;
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
            if (!resourceDrains.Keys.Any(id => resources[id] > DRAINED)) return float.MaxValue;
            return resourceDrains.Keys.Where(id => resources[id] > DRAINED).Min(id => resources[id] / resourceDrains[id]);
        }

        //Returns an enumeration of the resources this part burns
        public IEnumerable<int> BurnedResources()
        {
            return resourceConsumptions.Keys;
        }

        //returns whether this part contains any of the given resources
        public bool ContainsResources(IEnumerable<int> whichResources)
        {
            return whichResources.Any(id => resources[id] > DRAINED);
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

                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                    case ResourceFlowMode.ALL_VESSEL:
                        //check if any part contains the needed resource:
                        if (!vessel.Any(n => n.resources[type] > DRAINED)) return false;
                        break;

                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        // check if we can get any of the needed resources
                        if (!FindFuelSourcesStackPriority(type).Any()) return false;
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
                print(partName + "'s drain rate of " + PartResourceLibrary.Instance.GetDefinition(type).name + " is " + resourceDrains[type]);
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

                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                    case ResourceFlowMode.ALL_VESSEL:
                        AssignFuelDrainRateStagePriorityFlow(type, amount, vessel);
                        break;

                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        AssignFuelDrainRateStackPriority(type, amount);
                        break;

                    default:
                        //do nothing. there's an EVEN_FLOW scheme but nothing seems to use it
                        break;
                }
            }
        }

        void AssignFuelDrainRateStagePriorityFlow(int type, float amount, List<FuelNode> vessel)
        {
            int maxInverseStage = -1;
            List<FuelNode> sources = new List<FuelNode>();
            for (int i = 0; i < vessel.Count; i++)
            {
                FuelNode n = vessel[i];
                if (n.resources[type] > DRAINED)
                {
                    if (n.inverseStage > maxInverseStage)
                    {
                        maxInverseStage = n.inverseStage;
                        sources.Clear();
                        sources.Add(n);
                    }
                    else if (n.inverseStage == maxInverseStage)
                    {
                        sources.Add(n);
                    }
                }
            }
            for (int i = 0; i < sources.Count; i++)
            {
                sources[i].resourceDrains[type] += amount/sources.Count;
            }
        }


        void AssignFuelDrainRateStackPriority(int type, float amount)
        {
            HashSet<FuelNode> sources = FindFuelSourcesStackPriority(type);
            float amountPerSource = amount / sources.Count();
            foreach (FuelNode source in sources) source.resourceDrains[type] += amountPerSource;
        }

        static int nextFuelLookupID = 0;
        int lastSeenFuelLookupID = -1;

        HashSet<FuelNode> FindFuelSourcesStackPriority(int type)
        {
            int fuelLookupID = nextFuelLookupID++;
            HashSet<FuelNode> sources = new HashSet<FuelNode>();
            bool success = FindFuelSourcesStackPriorityRecursive(type, sources, fuelLookupID, 0);
            return sources;
        }

        bool FindFuelSourcesStackPriorityRecursive(int type, HashSet<FuelNode> sources, int fuelLookupID, int level)
        {
            // The fuel flow rules for STACK_PRIORITY_SEARCH are nicely explained in detail by Kashua at
            // http://forum.kerbalspaceprogram.com/threads/64362-Fuel-Flow-Rules-%280-23-5%29

            // recursive search cannot visit same node twice (Kashua rule #1)
            if (fuelLookupID == lastSeenFuelLookupID)
            {
                return false;
            }
            lastSeenFuelLookupID = fuelLookupID;

            // First try to draw fuel through incoming fuel lines (Kashua rule #2)
            bool success = false;
            for (int i = 0; i < fuelLineSources.Count; i++)
            {
                success |= fuelLineSources[i].FindFuelSourcesStackPriorityRecursive(type, sources, fuelLookupID, level + 1);
            }
            if (success)
            {
                return true;
            }

            // Then try to draw fuel through stack nodes (Kashua rule #4 (there is no rule #3))
            // TODO: only do this search if crossfeed capable!!!
            for (int i = 0; i < stackNodeSources.Count; i++)
            {
                success |= stackNodeSources[i].FindFuelSourcesStackPriorityRecursive(type, sources, fuelLookupID, level + 1);
            }
            if (success)
            {
                return true;
            }

            // If we are a container for this resource (and it hasn't been disabled by the right-click menu)...
            if (resources.Keys.Contains(type))
            {
                // If we have some of the resource, return ourselves (Kashua rule #5)
                // Otherwise return failure (Kashua rule #6)
                if (resources[type] > DRAINED)
                {
                    sources.Add(this);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // If we are fuel crossfeed capable and surface-mounted to our parent,
            // try to draw fuel from our parent (Kashua rule #7)
            if (surfaceMountParent != null)
            {
                return surfaceMountParent.FindFuelSourcesStackPriorityRecursive(type, sources, fuelLookupID, level+1);
            }

            // If all that fails, give up (Kashua rule #8)
            return false;
        }

        private float GetFlowModifier(double atmDensity, double machNumber)
        {
            float flowModifier = 1.0f;
            if (atmChangeFlow)
            {
                flowModifier = (float)(atmDensity / 1.225);
                if (useAtmCurve)
                {
                    flowModifier = atmCurve.Evaluate(flowModifier);
                }
            }
            if (useVelCurve)
            {
                flowModifier = flowModifier * velCurve.Evaluate((float)machNumber);
            }
            if (flowModifier < 1E-05f)
            {
                flowModifier = 1E-05f;
            }
            return flowModifier;
        }

    }
}
