using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using Smooth.Algebraics;
using Smooth.Dispose;
using Smooth.Pools;
using Smooth.Slinq;
using UnityEngine;
using UnityToolbag;

namespace MuMech
{
    public class FuelFlowSimulation
    {
        public int simStage; //the simulated rocket's current stage
        readonly List<FuelNode> nodes = new List<FuelNode>(); //a list of FuelNodes representing all the parts of the ship
        readonly Dictionary<Part, FuelNode> nodeLookup = new Dictionary<Part, FuelNode>();
        readonly Dictionary<FuelNode, Part> partLookup = new Dictionary<FuelNode, Part>();

        private double KpaToAtmospheres;

        //Takes a list of parts so that the simulation can be run in the editor as well as the flight scene
        public void Init(List<Part> parts, bool dVLinearThrust)
        {
            KpaToAtmospheres = PhysicsGlobals.KpaToAtmospheres;

            // Create FuelNodes corresponding to each Part
            nodes.Clear();
            nodeLookup.Clear();
            partLookup.Clear();

            for (int index = 0; index < parts.Count; index++)
            {
                Part part = parts[index];
                FuelNode node = FuelNode.Borrow(part, dVLinearThrust);
                nodeLookup[part] = node;
                partLookup[node] = part;
                nodes.Add(node);
            }
            // Determine when each part will be decoupled
            Part rootPart = parts[0]; // hopefully always correct
            nodeLookup[rootPart].AssignDecoupledInStage(rootPart, nodeLookup, -1);

            // Set up the fuel flow graph
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                nodeLookup[p].AddCrossfeedSouces(p.crossfeedPartSet.GetParts(), nodeLookup);
            }

            simStage = StageManager.LastStage + 1;

            // Add a fake stage if we are beyond the first one
            // Mostly useful for the Node Executor who use the last stage info
            // and fail to get proper info when the ship was never staged and
            // some engine were activated manually
            if (StageManager.CurrentStage > StageManager.LastStage)
                simStage++;
        }

        //Simulate the activation and execution of each stage of the rocket,
        //and return stats for each stage
        public Stats[] SimulateAllStages(float throttle, double staticPressureKpa, double atmDensity, double machNumber)
        {
            Stats[] stages = new Stats[simStage];

            int maxStages = simStage - 1;

            double staticPressure = staticPressureKpa * KpaToAtmospheres;

            //print("**************************************************");
            //print("SimulateAllStages starting from stage " + simStage + " throttle=" + throttle + " staticPressureKpa=" + staticPressureKpa + " atmDensity=" + atmDensity + " machNumber=" + machNumber);
            SimulateStageActivation();

            while (simStage >= 0)
            {
                //print("Simulating stage " + simStage + "(vessel mass = " + VesselMass(simStage) + ")");
                stages[simStage] = SimulateStage(throttle, staticPressure, atmDensity, machNumber);
                if (simStage != maxStages)
                    stages[simStage].stagedMass = stages[simStage + 1].endMass - stages[simStage].startMass;
                //print("Staging at t = " + t);
                SimulateStageActivation();
            }

            //print("SimulateAllStages ended");

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Release();
            }
            return stages;
        }

        public static void print(object message)
        {
            Dispatcher.InvokeAsync(() => MonoBehaviour.print("[MechJeb2] " + message));
        }

        //Simulate (the rest of) the current stage of the simulated rocket,
        //and return stats for the stage
        private Stats SimulateStage(float throttle, double staticPressure, double atmDensity, double machNumber)
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

            // track active engines to "fingerprint" this stage
            // (could improve by adding fuel tanks being drained and thereby support drop-tanks)
            stats.parts = new List<Part>();
            var engines = FindActiveEngines().value;
            for(int i = 0; i < engines.Count; i++) {
                stats.parts.Add(partLookup[engines[i]]);
            }

            const int maxSteps = 100;
            int step;
            for (step = 0; step < maxSteps; step++)
            {
                //print("Stage " + simStage + " step " + step + " endMass " + stats.endMass.ToString("F3"));
                if (AllowedToStage()) break;
                double dt;
                stats = stats.Append(SimulateTimeStep(float.MaxValue, throttle, staticPressure, atmDensity, machNumber, out dt));
                //print("Stage " + simStage + " step " + step + " dt " + dt);
                // BS engine detected. Bail out.
                if (dt == double.MaxValue || double.IsInfinity(dt))
                {
                    break;
                }
            }

            //print("Finished stage " + simStage + " after " + step + " steps");
            if (step == maxSteps) throw new Exception("FuelFlowSimulation.SimulateStage reached max step count of " + maxSteps);

            return stats;
        }

        //Simulate a single time step, and return stats for the time step.
        // - desiredDt is the requested time step size. Often the actual time step size
        //   with be less than this. The actual step size is reported in dt.
        private Stats SimulateTimeStep(float desiredDt, float throttle, double staticPressure, double atmDensity, double machNumber, out double dt)
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

            using (var engines = FindActiveEngines())
            {
                if (engines.value.Count > 0)
                {
                    for (int i = 0; i < engines.value.Count; i++)
                    {
                        engines.value[i].AssignResourceDrainRates(nodes);
                    }
                    //foreach (FuelNode n in nodes) n.DebugDrainRates();

                    double maxDt = nodes.Slinq().Select(n => n.MaxTimeStep()).Min();
                    dt = Math.Min(desiredDt, maxDt);

                    //print("Simulating time step of " + dt);

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        nodes[i].DrainResources(dt);
                        //nodes[i].DebugResources();
                    }
                }
                else
                {
                    dt = 0;
                }
            }

            stats.deltaTime = dt;
            stats.endMass = VesselMass(simStage);
            stats.resourceMass = stats.startMass - stats.endMass;
            stats.maxAccel = stats.endMass > 0 ? stats.startThrust / stats.endMass : 0;
            stats.ComputeTimeStepDeltaV();
            stats.isp = stats.startMass > stats.endMass ? stats.deltaV / (9.80665f * Math.Log(stats.startMass / stats.endMass)) : 0;

            return stats;
        }

        //Active the next stage of the simulated rocket and remove all nodes that get decoupled by the new stage
        private void SimulateStageActivation()
        {
            simStage--;

            using (Disposable<List<FuelNode>> decoupledNodes = ListPool<FuelNode>.Instance.BorrowDisposable())
            {
                nodes.Slinq().Where((n, stage) => n.decoupledInStage == stage, simStage).AddTo(decoupledNodes);

                for (int i = 0; i < decoupledNodes.value.Count; i++)
                {
                    nodes.Remove(decoupledNodes.value[i]); //remove the decoupled nodes from the simulated ship
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    for (int j = 0; j < decoupledNodes.value.Count; j++)
                    {
                        nodes[i].RemoveSourceNode(decoupledNodes.value[j]); //remove the decoupled nodes from the remaining nodes' source lists
                    }
                }

                for (int i = 0; i < decoupledNodes.value.Count; i++)
                {
                    decoupledNodes.value[i].Release(); // We can now return them to the pool
                }
            }
        }

        //Whether we've used up the current stage
        private bool AllowedToStage()
        {
            //print("Checking whether allowed to stage at t = " + t);

            using (var activeEngines = FindActiveEngines())
            {
                //print("  activeEngines.Count = " + activeEngines.value.Count);

                //if no engines are active, we can always stage
                if (activeEngines.value.Count == 0)
                {
                    //print("Allowed to stage because no active engines");
                    return true;
                }

                using (Disposable<List<int>> burnedResources = ListPool<int>.Instance.BorrowDisposable())
                {
                    activeEngines.value.Slinq().SelectMany(eng => eng.BurnedResources().Slinq()).Distinct().AddTo(burnedResources);

                    //if staging would decouple an active engine or non-empty fuel tank, we're not allowed to stage
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        FuelNode n = nodes[i];
                        //print(n.partName + " is sepratron? " + n.isSepratron);
                        if (n.decoupledInStage == (simStage - 1) && !n.isSepratron)
                        {
                            if (activeEngines.value.Contains(n))
                            {
                                //print("Not allowed to stage because " + n.partName + " is an active engine (" + activeEngines.value.Contains(n) +")");
                                //n.DebugResources();
                                return false;
                            }

                            if (n.ContainsResources(burnedResources.value))
                            {
                                int activeEnginesCount = activeEngines.value.Count;
                                for (int j = 0; j < activeEnginesCount; j++)
                                {
                                    FuelNode engine = activeEngines.value[j];
                                    if ( engine.CanDrawFrom(n))
                                    {
                                        //print("Not allowed to stage because " + n.partName + " contains resources (" + n.ContainsResources(burnedResources.value) + ") reachable by an active engine");
                                        //n.DebugResources();
                                        return false;
                                    }
                                }
                            }


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
                        if (activeEngines.value.Contains(n))
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

        private double VesselMass(int stage)
        {
            double sum = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                sum += nodes[i].Mass(stage);
            }
            return sum;
        }

        private double VesselThrust(float throttle, double staticPressure, double atmDensity, double machNumber)
        {
            var param = new Tuple<float, double, double, double>(throttle, staticPressure, atmDensity, machNumber);

            using (var activeEngines = FindActiveEngines())
            {
                return activeEngines.value.Slinq().Select((eng, t) => eng.EngineThrust(t.Item1, t.Item2, t.Item3, t.Item4), param).Sum();
            }
            //return FindActiveEngines().Sum(eng => eng.EngineThrust(throttle, staticPressure, atmDensity, machNumber));
        }

        //Returns a list of engines that fire during the current simulated stage.
        private Disposable<List<FuelNode>> FindActiveEngines()
        {
            var param = new Tuple<int, List<FuelNode>>(simStage, nodes);
            var activeEngines = ListPool<FuelNode>.Instance.BorrowDisposable();
            //print("Finding active engines: excluding resource considerations, there are " + nodes.Slinq().Where(n => n.isEngine && n.inverseStage >= simStage).Count());
            nodes.Slinq().Where((n, p) => n.isEngine && n.inverseStage >= p.Item1 && n.isDrawingResources && n.CanDrawNeededResources(p.Item2), param).AddTo(activeEngines.value);
            //print("Finding active engines: including resource considerations, there are " + activeEngines.value.Count);
            return activeEngines;
        }

        //A Stats struct describes the result of the simulation over a certain interval of time (e.g., one stage)
        public struct Stats
        {
            public double startMass;
            public double endMass;
            public double startThrust;
            public double maxAccel;
            public double deltaTime;
            public double deltaV;

            public double resourceMass;
            public double isp;
            public double stagedMass;

            public double StartTWR(double geeASL) { return startMass > 0 ? startThrust / (9.80665 * geeASL * startMass) : 0; }
            public double MaxTWR(double geeASL) { return maxAccel / (9.80665 * geeASL); }

            public List<Part> parts;

            //Computes the deltaV from the other fields. Only valid when the thrust is constant over the time interval represented.
            public void ComputeTimeStepDeltaV()
            {
                if (deltaTime > 0 && startMass > endMass && startMass > 0 && endMass > 0)
                {
                    deltaV = startThrust * deltaTime / (startMass - endMass) * Math.Log(startMass / endMass);
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
                    maxAccel = Math.Max(this.maxAccel, s.maxAccel),
                    deltaTime = this.deltaTime + (s.deltaTime < float.MaxValue && !double.IsInfinity(s.deltaTime) ? s.deltaTime : 0),
                    deltaV = this.deltaV + s.deltaV,
                    parts = this.parts,
                    isp = this.startMass == s.endMass ? 0 : (this.deltaV + s.deltaV) / (9.80665f * Math.Log(this.startMass / s.endMass))
                };
            }
        }
    }

    //A FuelNode is a compact summary of a Part, containing only the information needed to run the fuel flow simulation.
    public class FuelNode
    {
        readonly DefaultableDictionary<int, double> resources = new DefaultableDictionary<int, double>(0);       //the resources contained in the part
        readonly KeyableDictionary<int, double> resourceConsumptions = new KeyableDictionary<int, double>();     //the resources this part consumes per unit time when active at full throttle
        readonly DefaultableDictionary<int, double> resourceDrains = new DefaultableDictionary<int, double>(0);  //the resources being drained from this part per unit time at the current simulation time
        readonly DefaultableDictionary<int, bool> freeResources = new DefaultableDictionary<int, bool>(false);  //the resources that are "free" and assumed to be infinite like IntakeAir

        // if a resource amount falls below this amount we say that the resource has been drained
        // set to the smallest amount that the user can see is non-zero in the resource tab or by
        // right-clicking.
        const double DRAINED = 1E-4;


        FloatCurve atmosphereCurve;  //the function that gives Isp as a function of atmospheric pressure for this part, if it's an engine
        bool atmChangeFlow;
        bool useAtmCurve;
        FloatCurve atmCurve;
        bool useVelCurve;
        FloatCurve velCurve;

        KeyableDictionary<int, float> propellantRatios = new KeyableDictionary<int, float>(); //ratios of propellants used by this engine
        KeyableDictionary<int, ResourceFlowMode> propellantFlows = new KeyableDictionary<int, ResourceFlowMode>();  //flow modes of propellants since the engine can override them
        float propellantSumRatioTimesDensity;    //a number used in computing propellant consumption rates

        readonly List<FuelNode> crossfeedSources = new List<FuelNode>();

        float maxFuelFlow = 0;     //max fuel flow of this part
        float minFuelFlow = 0;     //min fuel flow of this part

        float thrustPercentage = 0;

        float fwdThrustRatio = 1; // % of thrust moving the ship forwad
        float g;                  // value of g used for engine flow rate / isp

        public int decoupledInStage;    //the stage in which this part will be decoupled from the rocket
        public int inverseStage;        //stage in which this part is activated
        public bool isSepratron;        //whether this part is a sepratron
        public bool isEngine = false;   //whether this part is an engine
        public bool isDrawingResources = true; // Is the engine actually using any resources

        private double resourceRequestRemainingThreshold;
        private int resourcePriority;

        double dryMass = 0; //the mass of this part, not counting resource mass
        float modulesUnstagedMass;   // the mass of the modules of this part before staging
        float modulesStagedMass = 0; // the mass of the modules of this part after staging

        public string partName; //for debugging

        private static readonly Pool<FuelNode> pool = new Pool<FuelNode>(Create, Reset);

        public static int PoolSize
        {
            get { return pool.Size; }
        }

        private static FuelNode Create()
        {
            return new FuelNode();
        }

        public void Release()
        {
            pool.Release(this);
        }

        private static void Reset(FuelNode obj)
        {
        }

        public static FuelNode Borrow(Part part, bool dVLinearThrust)
        {
            FuelNode node = pool.Borrow();
            node.Init(part, dVLinearThrust);
            return node;
        }

        private void Init(Part part, bool dVLinearThrust)
        {
            resources.Clear();
            resourceConsumptions.Clear();
            resourceDrains.Clear();
            freeResources.Clear();

            propellantRatios.Clear();
            propellantFlows.Clear();

            crossfeedSources.Clear();

            isEngine = false;

            dryMass = 0;
            modulesStagedMass = 0;

            decoupledInStage = int.MinValue;

            modulesUnstagedMass = 0;
            if (!part.IsLaunchClamp())
            {
                dryMass = part.prefabMass; // Intentionally ignore the physic flag.

                modulesUnstagedMass = part.GetModuleMassNoAlloc((float) dryMass, ModifierStagingSituation.UNSTAGED);

                modulesStagedMass = part.GetModuleMassNoAlloc((float) dryMass, ModifierStagingSituation.STAGED);

                float currentModulesMass = part.GetModuleMassNoAlloc((float) dryMass, ModifierStagingSituation.CURRENT);

                // if it was manually staged
                if (currentModulesMass == modulesStagedMass)
                {
                    modulesUnstagedMass = modulesStagedMass;
                }

                //print(part.partInfo.name.PadRight(25) + " " + part.mass.ToString("F4") + " " + part.GetPhysicslessChildMass().ToString("F4") + " " + modulesUnstagedMass.ToString("F4") + " " + modulesStagedMass.ToString("F4"));
            }

            inverseStage = part.inverseStage;
            partName = part.partInfo.name;

            resourceRequestRemainingThreshold = Math.Max(part.resourceRequestRemainingThreshold, DRAINED);
            resourcePriority = part.GetResourcePriority();

            //note which resources this part has stored
            for (int i = 0; i < part.Resources.Count; i++)
            {
                PartResource r = part.Resources[i];
                if (r.info.density > 0)
                {
                    if (r.flowState)
                    {
                        resources[r.info.id] = r.amount;
                    }
                    else
                    {
                        dryMass += (r.amount * r.info.density); // disabled resources are just dead weight
                    }
                }
                if (r.info.name == "IntakeAir")
                    freeResources[PartResourceLibrary.Instance.GetDefinition("IntakeAir").id] = true;
                // Those two are in the CRP.
                if (r.info.name == "IntakeLqd")
                    freeResources[PartResourceLibrary.Instance.GetDefinition("IntakeLqd").id] = true;
                if (r.info.name == "IntakeAtm")
                    freeResources[PartResourceLibrary.Instance.GetDefinition("IntakeAtm").id] = true;
            }

            // TODO : handle the multiple active ModuleEngine case ( SXT engines with integrated vernier )

            //record relevant engine stats
            //ModuleEngines engine = part.Modules.OfType<ModuleEngines>().FirstOrDefault(e => e.isEnabled);
            ModuleEngines engine = null;
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule pm = part.Modules[i];
                ModuleEngines e = pm as ModuleEngines;
                if (e != null && e.isEnabled)
                {
                    engine = e;
                    break;
                }
            }

            if (engine != null)
            {
                //Only count engines that either are ignited or will ignite in the future:
                if ((HighLogic.LoadedSceneIsEditor || inverseStage < StageManager.CurrentStage || engine.getIgnitionState) && (engine.thrustPercentage > 0 || engine.minThrust > 0))
                {
                    //if an engine has been activated early, pretend it is in the current stage:
                    if (engine.getIgnitionState && inverseStage < StageManager.CurrentStage)
                        inverseStage = StageManager.CurrentStage;

                    isEngine = true;

                    g = engine.g;

                    // If we take into account the engine rotation
                    if (dVLinearThrust)
                    {
                        Vector3 thrust = Vector3.zero;
                        for (int i = 0; i < engine.thrustTransforms.Count; i++)
                        {
                            thrust -= engine.thrustTransforms[i].forward * engine.thrustTransformMultipliers[i];
                        }

                        Vector3d fwd = HighLogic.LoadedScene == GameScenes.EDITOR ? EditorLogic.VesselRotation * Vector3d.up : engine.part.vessel.GetTransform().up;
                        fwdThrustRatio = Vector3.Dot(fwd, thrust);
                    }
                    else
                    {
                        fwdThrustRatio = 1;
                    }

                    thrustPercentage = engine.thrustPercentage;

                    minFuelFlow = engine.minFuelFlow;
                    maxFuelFlow = engine.maxFuelFlow;

                    // Some brilliant engine mod seems to consider that FuelFlow is not something they should properly initialize
                    if (minFuelFlow == 0 && engine.minThrust > 0)
                    {
                        maxFuelFlow = engine.minThrust / (engine.atmosphereCurve.Evaluate(0f) * engine.g);
                    }
                    if (maxFuelFlow == 0 && engine.maxThrust > 0)
                    {
                        maxFuelFlow = engine.maxThrust / (engine.atmosphereCurve.Evaluate(0f) * engine.g);
                    }

                    atmosphereCurve = new FloatCurve(engine.atmosphereCurve.Curve.keys);
                    atmChangeFlow = engine.atmChangeFlow;
                    useAtmCurve = engine.useAtmCurve;
                    if (useAtmCurve)
                        atmCurve = new FloatCurve(engine.atmCurve.Curve.keys);
                    useVelCurve = engine.useVelCurve;
                    if (useVelCurve)
                        velCurve = new FloatCurve(engine.velCurve.Curve.keys);

                    propellantSumRatioTimesDensity = engine.propellants.Slinq().Where(prop => !prop.ignoreForIsp).Select(prop => prop.ratio * MuUtils.ResourceDensity(prop.id)).Sum();
                    propellantRatios.Clear();
                    propellantFlows.Clear();
                    var dics = new Tuple<KeyableDictionary<int, float>, KeyableDictionary<int, ResourceFlowMode>>(propellantRatios, propellantFlows);
                    engine.propellants.Slinq()
                        .Where(prop => MuUtils.ResourceDensity(prop.id) > 0 && !prop.ignoreForIsp)
                        .ForEach((p, dic) =>
                        {
                            dic.Item1.Add(p.id, p.ratio);
                            dic.Item2.Add(p.id, p.GetFlowMode());
                        }, dics);
                }
            }
        }

        // Determine when this part will be decoupled given when its parent will be decoupled.
        // Then recurse to all of this part's children.
        public void AssignDecoupledInStage(Part p, Dictionary<Part, FuelNode> nodeLookup, int parentDecoupledInStage)
        {
            // Already processed
            if (decoupledInStage != int.MinValue)
                return;

            bool isDecoupler = false;
            decoupledInStage = parentDecoupledInStage;

            for (int i = 0; i < p.Modules.Count; i++)
            {
                PartModule m = p.Modules[i];

                ModuleDecouple mDecouple = m as ModuleDecouple;
                if (mDecouple != null)
                {
                    if (!mDecouple.isDecoupled && mDecouple.stagingEnabled && p.stagingOn)
                    {
                        if (mDecouple.isOmniDecoupler)
                        {
                            isDecoupler = true;
                            // We are decoupling our parent
                            // The part and its children are not part of the ship when we decouple
                            decoupledInStage = p.inverseStage;

                            // The parent should already have its info assigned at this point
                            //nodeLookup[p.parent].AssignDecoupledInStage(p.parent, nodeLookup, p.inverseStage);

                            // The part children are decoupled when we decouple
                            foreach (Part child in p.children)
                            {
                                nodeLookup[child].AssignDecoupledInStage(child, nodeLookup, p.inverseStage);
                            }
                        }
                        else
                        {
                            AttachNode attach;
                            if (mDecouple.explosiveNodeID != "srf")
                            {
                                attach = p.FindAttachNode(mDecouple.explosiveNodeID);
                            }
                            else
                            {
                                attach = p.srfAttachNode;
                            }

                            if (attach != null && attach.attachedPart != null)
                            {
                                if (attach.attachedPart == p.parent)
                                {
                                    isDecoupler = true;
                                    // We are decoupling our parent
                                    // The part and its children are not part of the ship when we decouple
                                    decoupledInStage = p.inverseStage;
                                    //print("AssignDecoupledInStage ModuleDecouple          " + p.partInfo.name + "(" + p.inverseStage + ") decoupling " + attach.attachedPart + "(" + attach.attachedPart.inverseStage + "). parent " + decoupledInStage);

                                    // The parent should already have its info assigned at this point
                                    //nodeLookup[p.parent].AssignDecoupledInStage(p.parent, nodeLookup, p.inverseStage);
                                }
                                else
                                {
                                    isDecoupler = true;
                                    // We are still attached to our parent
                                    // The part and it's children are dropped when the parent is
                                    decoupledInStage = parentDecoupledInStage;

                                    //print("AssignDecoupledInStage ModuleDecouple          " + p.partInfo.name + "(" + p.inverseStage + ") decoupling " + attach.attachedPart + "(" + attach.attachedPart.inverseStage + "). not the parent " + decoupledInStage);
                                    // The part we decouple is dropped when we decouple
                                    nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, nodeLookup, p.inverseStage);

                                }
                            }
                        }
                        break; // Hopefully no one made part with multiple decoupler modules ?
                    }
                }

                ModuleAnchoredDecoupler mAnchoredDecoupler = m as ModuleAnchoredDecoupler;
                if (mAnchoredDecoupler != null)
                {
                    if (!mAnchoredDecoupler.isDecoupled && mAnchoredDecoupler.stagingEnabled && p.stagingOn)
                    {
                        AttachNode attach;
                        if (mAnchoredDecoupler.explosiveNodeID != "srf")
                        {
                            attach = p.FindAttachNode(mAnchoredDecoupler.explosiveNodeID);
                        }
                        else
                        {
                            attach = p.srfAttachNode;
                        }

                        if (attach != null && attach.attachedPart != null)
                        {
                            if (attach.attachedPart == p.parent)
                            {
                                isDecoupler = true;
                                // We are decoupling our parent
                                // The part and its children are not part of the ship when we decouple
                                decoupledInStage = p.inverseStage;
                                //print("AssignDecoupledInStage ModuleAnchoredDecoupler " + p.partInfo.name + "(" + p.inverseStage + ") decoupling " + attach.attachedPart + "(" + attach.attachedPart.inverseStage + "). parent " + decoupledInStage);

                                // The parent should already have its info assigned at this point
                                //nodeLookup[p.parent].AssignDecoupledInStage(p.parent, nodeLookup, p.inverseStage);
                            }
                            else
                            {
                                isDecoupler = true;
                                // We are still attached to our parent
                                // The part and it's children are dropped when the parent is
                                decoupledInStage = parentDecoupledInStage;

                                //print("AssignDecoupledInStage ModuleAnchoredDecoupler " + p.partInfo.name + "(" + p.inverseStage + ") decoupling " + attach.attachedPart + "(" + attach.attachedPart.inverseStage + "). not the parent " + decoupledInStage);
                                // The part we decouple is dropped when we decouple
                                nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, nodeLookup, p.inverseStage);
                            }
                        }
                        break;
                    }
                }

                ModuleDockingNode mDockingNode = m as ModuleDockingNode;
                if (mDockingNode != null)
                {
                    if (mDockingNode.staged && mDockingNode.stagingEnabled && p.stagingOn)
                    {
                        Part attachedPart = mDockingNode.referenceNode.attachedPart;
                        if (attachedPart != null)
                        {
                            if (attachedPart == p.parent)
                            {
                                isDecoupler = true;
                                // We are decoupling our parent
                                // The part and its children are not part of the ship when we decouple
                                decoupledInStage = p.inverseStage;

                                // The parent should already have its info assigned at this point
                                //nodeLookup[p.parent].AssignDecoupledInStage(p.parent, nodeLookup, p.inverseStage);
                            }
                            else
                            {
                                isDecoupler = true;
                                decoupledInStage = parentDecoupledInStage;
                                //childDecoupledInStage = parentDecoupledInStage;
                                nodeLookup[attachedPart].AssignDecoupledInStage(attachedPart, nodeLookup, p.inverseStage);
                            }
                        }
                    }
                    break;
                }

                if (m.moduleName == "ProceduralFairingDecoupler")
                {
                    if (!m.Fields["decoupled"].GetValue<bool>(m) && m.stagingEnabled && p.stagingOn)
                    {
                        isDecoupler = true;
                        // We are decoupling our parent
                        // The part and its children are not part of the ship when we decouple
                        decoupledInStage = p.inverseStage;
                        break;
                    }
                }
            }

            if (p.IsLaunchClamp())
            {
                decoupledInStage = p.inverseStage > parentDecoupledInStage ? p.inverseStage : parentDecoupledInStage;
                //print("AssignDecoupledInStage D " + p.partInfo.name + " " + parentDecoupledInStage);

            }
            else if (!isDecoupler)
            {
                decoupledInStage = parentDecoupledInStage;
                //print("AssignDecoupledInStage                         " + p.partInfo.name + "(" + p.inverseStage + ")" + decoupledInStage);
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
            Dispatcher.InvokeAsync(() => MonoBehaviour.print("[MechJeb2] " + message));
        }

        public void SetConsumptionRates(float throttle, double atmDensity, double machNumber)
        {
            if (isEngine)
            {
                double flowModifier = GetFlowModifier(atmDensity, machNumber);

                double massFlowRate = Mathf.Lerp(minFuelFlow, maxFuelFlow, throttle * 0.01f * thrustPercentage) * flowModifier;

                isDrawingResources = massFlowRate > 0;

                //propellant consumption rate = ratio * massFlowRate / sum(ratio * density)
                //resourceConsumptions = propellantRatios.Keys.ToDictionary(id => id, id => propellantRatios[id] * massFlowRate / propellantSumRatioTimesDensity);
                resourceConsumptions.Clear();
                for (int i = 0; i < propellantRatios.KeysList.Count; i++)
                {
                    int id = propellantRatios.KeysList[i];
                    double rate = propellantRatios[id] * massFlowRate / propellantSumRatioTimesDensity;
                    //print(partName + " SetConsumptionRates for " + PartResourceLibrary.Instance.GetDefinition(id).name + " is " + rate + " flowModifier=" + flowModifier + " massFlowRate="+ massFlowRate);
                    resourceConsumptions.Add(id, rate);
                }
            }
        }

        public void AddCrossfeedSouces(HashSet<Part> parts, Dictionary<Part, FuelNode> nodeLookup)
        {
            using (var it = parts.GetEnumerator())
            {
                while (it.MoveNext())
                {
                    FuelNode fuelnode;
                    if (nodeLookup.TryGetValue(it.Current, out fuelnode))
                        crossfeedSources.Add(fuelnode);
                }
            }
        }

        //call this when a node no longer exists, so that this node knows that it's no longer a valid source
        public void RemoveSourceNode(FuelNode n)
        {
            crossfeedSources.Remove(n);
        }

        //return the mass of the simulated FuelNode. This is not the same as the mass of the Part,
        //because the simulated node may have lost resources, and thus mass, during the simulation.
        public double Mass(int simStage)
        {
            //print("\n(" + simStage + ") " + partName.PadRight(25) + " dryMass " + dryMass.ToString("F3")
            //          + " ResMass " + (resources.Keys.Sum(id => resources[id] * MuUtils.ResourceDensity(id))).ToString("F3")
            //          + " Fairing Mass " + (inverseStage < simStage ? fairingMass : 0).ToString("F3")
            //          + " (" + fairingMass.ToString("F3") + ")"
            //          + " ModuleMass " + moduleMass.ToString("F3")
            //          );

            //return dryMass + resources.Keys.Sum(id => resources[id] * MuUtils.ResourceDensity(id)) +
            double resMass = resources.KeysList.Slinq().Select((r, rs) => rs[r] * MuUtils.ResourceDensity(r), resources).Sum();
            return dryMass + resMass +
                   (inverseStage < simStage ? modulesUnstagedMass : modulesStagedMass);
        }

        public double EngineThrust(float throttle, double atmospheres, double atmDensity, double machNumber)
        {
            float Isp = atmosphereCurve.Evaluate((float)atmospheres);

            double flowModifier = GetFlowModifier(atmDensity, machNumber);

            double thrust = Mathf.Lerp(minFuelFlow, maxFuelFlow, throttle * 0.01f * thrustPercentage) * flowModifier * Isp * g;

            return thrust * fwdThrustRatio;
        }

        public void ResetDrainRates()
        {
            resourceDrains.Clear();
        }

        public void DrainResources(double dt)
        {
            foreach (int type in resourceDrains.KeysList)
                if (!freeResources[type])
                    resources[type] -= dt * resourceDrains[type];
        }


        public void DebugResources()
        {
            foreach (KeyValuePair<int, double> type in resources)
                print(partName + " " + PartResourceLibrary.Instance.GetDefinition(type.Key).name + " is " + type.Value);
        }

        public double MaxTimeStep()
        {
            var param = new Tuple<DefaultableDictionary<int, double>, double, DefaultableDictionary<int, double>>(resources, resourceRequestRemainingThreshold, resourceDrains);
            if (!resourceDrains.KeysList.Slinq().Any((id, p) => p.Item1[id] > p.Item2, param)) return double.MaxValue;
            return resourceDrains.KeysList.Slinq().Where((id, p) => p.Item1[id] > p.Item2, param).Select((id, p) => p.Item1[id] / p.Item3[id], param).Min();
        }

        //Returns an enumeration of the resources this part burns
        public List<int> BurnedResources()
        {
            return resourceConsumptions.KeysList;
        }

        //returns whether this part contains any of the given resources
        public bool ContainsResources(List<int> whichResources)
        {
            var param = new Tuple<DefaultableDictionary<int, double>, double>(resources, resourceRequestRemainingThreshold);
            //return whichResources.Any(id => resources[id] > DRAINED);
            return whichResources.Slinq().Any((id, r) => r.Item1[id] > r.Item2, param);
        }

        public bool CanDrawNeededResources(List<FuelNode> vessel)
        {
            foreach (int type in resourceConsumptions.KeysList)
            {
                var resourceFlowMode = propellantFlows[type];
                switch (resourceFlowMode)
                {
                    case ResourceFlowMode.NO_FLOW:
                        //check if we contain the needed resource:
                        if (resources[type] < resourceRequestRemainingThreshold) return false;
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                    case ResourceFlowMode.ALL_VESSEL_BALANCE:
                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                    case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                        //check if any part contains the needed resource:
                        if (!vessel.Slinq().Any((n, t) => n.resources[t] > n.resourceRequestRemainingThreshold, type)) return false;
                        break;

                    case ResourceFlowMode.STAGE_STACK_FLOW:
                    case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        // check if we can get any of the needed resources
                        if (!crossfeedSources.Slinq().Any((n, t) => n.resources[t] > n.resourceRequestRemainingThreshold, type)) return false;
                        break;

                    default: // and NULL
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

        public bool CanDrawFrom(FuelNode node)
        {
            return crossfeedSources.Contains(node);
        }

        public void AssignResourceDrainRates(List<FuelNode> vessel)
        {
            foreach (int type in resourceConsumptions.KeysList)
            {
                if (freeResources[type])
                    continue;

                double amount = resourceConsumptions[type];

                var resourceFlowMode = propellantFlows[type];
                switch (resourceFlowMode)
                {
                    case ResourceFlowMode.NO_FLOW:
                        resourceDrains[type] += amount;
                        break;

                    case ResourceFlowMode.ALL_VESSEL:
                    case ResourceFlowMode.ALL_VESSEL_BALANCE:
                        AssignFuelDrainRateStagePriorityFlow(type, amount, false, vessel);
                        break;

                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                    case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                        AssignFuelDrainRateStagePriorityFlow(type, amount, true, vessel);
                        break;

                    case ResourceFlowMode.STAGE_STACK_FLOW:
                    case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        //AssignFuelDrainRateStackPriority(type, true, amount);
                        AssignFuelDrainRateStagePriorityFlow(type, amount, true, crossfeedSources);
                        break;

                    default:
                        //do nothing.
                        break;
                }
            }
        }

        void AssignFuelDrainRateStagePriorityFlow(int type, double amount, bool usePrio, List<FuelNode> vessel)
        {
            int maxPrio = int.MinValue;
            using (var dispoSources = ListPool<FuelNode>.Instance.BorrowDisposable())
            {
                var sources = dispoSources.value;
                //print("AssignFuelDrainRateStagePriorityFlow for " + partName + " searching for " + amount + " of " + PartResourceLibrary.Instance.GetDefinition(type).name + " in " + vessel.Count + " parts ");
                for (int i = 0; i < vessel.Count; i++)
                {
                    FuelNode n = vessel[i];
                    if (n.resources[type] > n.resourceRequestRemainingThreshold)
                    {
                        if (usePrio)
                        {
                            if (n.resourcePriority > maxPrio)
                            {
                                maxPrio = n.resourcePriority;
                                sources.Clear();
                                sources.Add(n);
                            }
                            else if (n.resourcePriority == maxPrio)
                            {
                                sources.Add(n);
                            }
                        }
                        else
                        {
                            sources.Add(n);
                        }
                    }
                }
                //print(partName + " drains resource from " + sources.Count + " parts ");
                for (int i = 0; i < sources.Count; i++)
                {
                    if (!freeResources[type])
                        sources[i].resourceDrains[type] += amount / sources.Count;
                }
            }
        }

        private double GetFlowModifier(double atmDensity, double machNumber)
        {
            double flowModifier = 1.0f;
            if (atmChangeFlow)
            {
                flowModifier = atmDensity * (1d / 1.225);
                if (useAtmCurve)
                {
                    flowModifier = atmCurve.Evaluate((float) flowModifier);
                }
            }
            if (useVelCurve)
            {
                flowModifier = flowModifier * velCurve.Evaluate((float)machNumber);
            }
            return flowModifier;
        }

    }
}
