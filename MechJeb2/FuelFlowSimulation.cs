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
        private readonly List<FuelNode> nodes = new List<FuelNode>(); //a list of FuelNodes representing all the parts of the ship
        private readonly Dictionary<Part, FuelNode> nodeLookup = new Dictionary<Part, FuelNode>();
        private readonly Dictionary<FuelNode, Part> partLookup = new Dictionary<FuelNode, Part>();

        private double KpaToAtmospheres;

        //Takes a list of parts so that the simulation can be run in the editor as well as the flight scene
        public void Init(List<Part> parts, bool dVLinearThrust)
        {
            //print("==================================================");
            //print("Init Start");
            KpaToAtmospheres = PhysicsGlobals.KpaToAtmospheres;

            // Create FuelNodes corresponding to each Part
            nodes.Clear();
            nodeLookup.Clear();
            partLookup.Clear();

            Part negOnePart = parts[0];

            for (int index = 0; index < parts.Count; index++)
            {
                Part part = parts[index];
                FuelNode node = FuelNode.Borrow(part, dVLinearThrust);
                nodeLookup[part] = node;
                partLookup[node] = part;
                nodes.Add(node);
                if ( part.inverseStage < 0 && negOnePart == null )
                    negOnePart = part;
            }

            if ( negOnePart == null )
                negOnePart = parts[0];

            // Determine when each part will be decoupled
            nodeLookup[negOnePart].AssignDecoupledInStage(negOnePart, null, nodeLookup, -1);

            simStage = StageManager.LastStage;

            // Set up the fuel flow graph
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                FuelNode node = nodeLookup[p];
                node.AddCrossfeedSouces(p.crossfeedPartSet.GetParts(), nodeLookup);
                if (node.decoupledInStage >= simStage) simStage = node.decoupledInStage + 1;
            }

            // Add a fake stage if we are beyond the first one
            // Mostly useful for the Node Executor who use the last stage info
            // and fail to get proper info when the ship was never staged and
            // some engine were activated manually
            if (StageManager.CurrentStage > StageManager.LastStage)
                simStage++;
            //print("Init End");
        }

        //Simulate the activation and execution of each stage of the rocket,
        //and return stats for each stage
        public Stats[] SimulateAllStages(float throttle, double staticPressureKpa, double atmDensity, double machNumber)
        {
            Stats[] stages = new Stats[simStage + 1];

            int maxStages = simStage - 1;

            double staticPressure = staticPressureKpa * KpaToAtmospheres;

            //print("**************************************************");
            //print("SimulateAllStages starting from stage " + simStage + " throttle=" + throttle + " staticPressureKpa=" + staticPressureKpa + " atmDensity=" + atmDensity + " machNumber=" + machNumber);

            while (simStage >= 0)
            {
                //print("Simulating stage " + simStage + "(vessel mass = " + VesselMass(simStage) + ")");
                stages[simStage] = SimulateStage(throttle, staticPressure, atmDensity, machNumber);
                if (simStage + 1 < stages.Length)
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
                nodes[i].SetConsumptionRates(throttle, staticPressure, atmDensity, machNumber);
            }

            Stats stats = new Stats();
            stats.startMass = VesselMass(simStage);
            stats.startThrust = VesselThrust();
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
                stats = stats.Append(SimulateTimeStep(double.MaxValue, throttle, staticPressure, atmDensity, machNumber, out dt));
                //print("Stage " + simStage + " step " + step + " dt " + dt);
                // BS engine detected. Bail out.
                if (dt == double.MaxValue || double.IsInfinity(dt))
                {
                    //print("BS engine detected. Bail out.");
                    break;
                }
            }

            //print("Finished stage " + simStage + " after " + step + " steps");
            if (step == maxSteps) throw new Exception("FuelFlowSimulation.SimulateStage reached max step count of " + maxSteps);

            //Debug.Log("thrust = " + stats.startThrust + " ISP = " + stats.isp + " FuelFlow = " + ( stats.startMass - stats.endMass ) / stats.deltaTime * 1000 + " num = " + FindActiveEngines(true).value.Count );

            return stats;
        }

        //Simulate a single time step, and return stats for the time step.
        // - desiredDt is the requested time step size. Often the actual time step size
        //   with be less than this. The actual step size is reported in dt.
        private Stats SimulateTimeStep(double desiredDt, float throttle, double staticPressure, double atmDensity, double machNumber, out double dt)
        {
            Stats stats = new Stats();

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].ResetDrainRates();
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].SetConsumptionRates(throttle, staticPressure, atmDensity, machNumber);
            }

            stats.startMass = VesselMass(simStage);
            stats.startThrust = VesselThrust();

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
                nodes.Slinq().Where((n, stage) => n.decoupledInStage >= stage, simStage).AddTo(decoupledNodes);

                for (int i = 0; i < decoupledNodes.value.Count; i++)
                {
                    FuelNode decoupledNode = decoupledNodes.value[i];
                    nodes.Remove(decoupledNode); //remove the decoupled nodes from the simulated ship
                    //print("Decoupling: " + decoupledNode.partName + " decoupledInStage=" + decoupledNode.decoupledInStage);
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
            //print("Checking whether allowed to stage");

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
                        //print("Not allowed to stage because nothing is decoupled in the next stage, and there are already other engines active.");
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

        private double VesselIsp()
        {
            double sumThrust = 0;
            double sumThrustOverIsp = 0;
            using (var activeEngines = FindActiveEngines())
            {
                for(int i = 0; i < activeEngines.value.Count; i++)
                {
                    sumThrust += activeEngines.value[i].partThrust;
                    sumThrustOverIsp += activeEngines.value[i].partThrust;
                }
            }
            return sumThrust / sumThrustOverIsp;
        }

        private double VesselThrust()
        {
            double sumThrust = 0;
            using (var activeEngines = FindActiveEngines())
            {
                for(int i = 0; i < activeEngines.value.Count; i++)
                {
                    sumThrust += activeEngines.value[i].partThrust;
                }
            }
            return sumThrust;
        }

        //Returns a list of engines that fire during the current simulated stage.
        private Disposable<List<FuelNode>> FindActiveEngines()
        {
            var param = new Smooth.Algebraics.Tuple<int, List<FuelNode>>(simStage, nodes);
            var activeEngines = ListPool<FuelNode>.Instance.BorrowDisposable();
            //print("Finding engines in " + nodes.Count + " parts, there are " + nodes.Slinq().Where(n => n.isEngine).Count());
            //nodes.Slinq().Where(n => n.isEngine).ForEach(node => print("  (" + node.partName + " " + node.inverseStage + ">=" + simStage + " " + (node.inverseStage >= simStage) + ")"));
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
        private readonly struct EngineInfo
        {
            public readonly ModuleEngines engineModule;
            public readonly Vector3d thrustVector;

            public EngineInfo(ModuleEngines engineModule)
            {
                this.engineModule = engineModule;

                thrustVector = Vector3d.zero;

                for (int i = 0; i < engineModule.thrustTransforms.Count; i++)
                    thrustVector -= engineModule.thrustTransforms[i].forward * engineModule.thrustTransformMultipliers[i];
            }
        }

        readonly DefaultableDictionary<int, double> resources = new DefaultableDictionary<int, double>(0);       //the resources contained in the part
        readonly KeyableDictionary<int, double> resourceConsumptions = new KeyableDictionary<int, double>();     //the resources this part consumes per unit time when active at full throttle
        readonly DefaultableDictionary<int, double> resourceDrains = new DefaultableDictionary<int, double>(0);  //the resources being drained from this part per unit time at the current simulation time
        readonly DefaultableDictionary<int, bool> freeResources = new DefaultableDictionary<int, bool>(false);  //the resources that are "free" and assumed to be infinite like IntakeAir

        // if a resource amount falls below this amount we say that the resource has been drained
        // set to the smallest amount that the user can see is non-zero in the resource tab or by
        // right-clicking.
        const double DRAINED = 1E-4;

        KeyableDictionary<int, ResourceFlowMode> propellantFlows = new KeyableDictionary<int, ResourceFlowMode>();  //flow modes of propellants since the engine can override them

        readonly List<FuelNode> crossfeedSources = new List<FuelNode>();

        public int decoupledInStage;    //the stage in which this part will be decoupled from the rocket
        public int inverseStage;        //stage in which this part is activated
        public bool isLaunchClamp;        //whether this part is a launch clamp
        public bool isSepratron;        //whether this part is a sepratron
        public bool isEngine = false;   //whether this part is an engine
        public bool isthrottleLocked = false;
        public bool activatesEvenIfDisconnected = false;
        public bool isDrawingResources = true; // Is the engine actually using any resources

        private double resourceRequestRemainingThreshold;
        private int resourcePriority;

        double dryMass = 0; //the mass of this part, not counting resource mass
        float modulesUnstagedMass;   // the mass of the modules of this part before staging
        float modulesStagedMass = 0; // the mass of the modules of this part after staging

        public string partName; //for debugging

        Part part;
        bool dVLinearThrust;
        Vector3d vesselOrientation;

        readonly List<EngineInfo> engineInfos = new List<EngineInfo>();

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
            this.part = part;
            this.dVLinearThrust = dVLinearThrust;
            resources.Clear();
            resourceConsumptions.Clear();
            resourceDrains.Clear();
            freeResources.Clear();

            crossfeedSources.Clear();

            isEngine = false;
            isthrottleLocked = false;
            activatesEvenIfDisconnected = part.ActivatesEvenIfDisconnected;
            isLaunchClamp = part.IsLaunchClamp();

            dryMass = 0;
            modulesStagedMass = 0;

            decoupledInStage = int.MinValue;

            vesselOrientation = HighLogic.LoadedScene == GameScenes.EDITOR ? EditorLogic.VesselRotation * Vector3d.up : part.vessel.GetTransform().up;

            modulesUnstagedMass = 0;
            if (!isLaunchClamp)
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
                else
                {
                    freeResources[r.info.id] = true;
                }
                // Including the ressource in the CRP.
                if (r.info.name == "IntakeAir" || r.info.name == "IntakeLqd" || r.info.name == "IntakeAtm")
                    freeResources[r.info.id] = true;
            }

            engineInfos.Clear();

            // determine if we've got at least one useful ModuleEngine
            // we only do these test for the first ModuleEngines in the Part, could any other ones actually differ?
            for (int i = 0; i < part.Modules.Count; i++)
            {
                if (!(part.Modules[i] is ModuleEngines e) || !e.isEnabled) continue;

                // Only count engines that either are ignited or will ignite in the future:
                if (!isEngine && (HighLogic.LoadedSceneIsEditor || inverseStage < StageManager.CurrentStage || e.getIgnitionState) && (e.thrustPercentage > 0 || e.minThrust > 0))
                {
                    // if an engine has been activated early, pretend it is in the current stage:
                    if (e.getIgnitionState && inverseStage < StageManager.CurrentStage)
                        inverseStage = StageManager.CurrentStage;

                    isEngine = true;
                    isthrottleLocked = e.throttleLocked;
                }

                engineInfos.Add(new EngineInfo(e));
            }
        }

        // We are not necessarily traversing from the root part but from any interior part, so that p.parent is just another potential child node
        // in our traversal.  This is a helper to loop over all the children in our traversal.
        private void AssignChildrenDecoupledInStage(Part p, Part traversalParent, Dictionary<Part, FuelNode> nodeLookup, int parentDecoupledInStage)
        {
            for (int i = 0; i < p.children.Count; i++)
            {
                Part child = p.children[i];
                if ( child != null && child != traversalParent )
                    nodeLookup[child].AssignDecoupledInStage(child, p, nodeLookup, parentDecoupledInStage);
            }
            if ( p.parent != null && p.parent != traversalParent )
                nodeLookup[p.parent].AssignDecoupledInStage(p.parent, p, nodeLookup, parentDecoupledInStage);
        }

        // Determine when this part will be decoupled given when its parent will be decoupled.
        // Then recurse to all of this part's children.
        public void AssignDecoupledInStage(Part p, Part traversalParent, Dictionary<Part, FuelNode> nodeLookup, int parentDecoupledInStage)
        {
            // Already processed (this gets used where we assign the attached part, then loop over all the children and expect the
            // one we already hit to be skipped by this)
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
                            // We are decoupling our parent. The part and its children are not part of the ship when we decouple
                            isDecoupler = true;
                            decoupledInStage = p.inverseStage;
                            AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                        }
                        else
                        {
                            AttachNode attach;
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                if (mDecouple.explosiveNodeID != "srf")
                                    attach = p.FindAttachNode(mDecouple.explosiveNodeID);
                                else
                                    attach = p.srfAttachNode;
                            }
                            else
                            {
                                attach = mDecouple.ExplosiveNode;
                            }

                            if (attach != null && attach.attachedPart != null)
                            {
                                if (attach.attachedPart == traversalParent && mDecouple.staged)
                                {
                                    // We are decoupling our parent. The part and its children are not part of the ship when we decouple
                                    isDecoupler = true;
                                    decoupledInStage = p.inverseStage;
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                }
                                else
                                {
                                    // We are still attached to our parent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the parent.
                                    isDecoupler = true;
                                    decoupledInStage = parentDecoupledInStage;
                                    nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, p, nodeLookup, p.inverseStage);
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
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
                        if (HighLogic.LoadedSceneIsEditor)
                        {
                            if (mAnchoredDecoupler.explosiveNodeID != "srf")
                                attach = p.FindAttachNode(mAnchoredDecoupler.explosiveNodeID);
                            else
                                attach = p.srfAttachNode;
                        }
                        else
                        {
                            attach = mAnchoredDecoupler.ExplosiveNode;
                        }
                        if (attach != null && attach.attachedPart != null)
                        {
                            if (attach.attachedPart == traversalParent && mAnchoredDecoupler.staged)
                            {
                                // We are decoupling our parent. The part and its children are not part of the ship when we decouple
                                isDecoupler = true;
                                decoupledInStage = p.inverseStage;
                                AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                            }
                            else
                            {
                                // We are still attached to our parent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the parent.
                                isDecoupler = true;
                                decoupledInStage = parentDecoupledInStage;
                                nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, p, nodeLookup, p.inverseStage);
                                AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
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
                            if (attachedPart == traversalParent)
                            {
                                // We are decoupling our parent. The part and its children are not part of the ship when we decouple
                                isDecoupler = true;
                                decoupledInStage = p.inverseStage;
                                AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                isDecoupler = true;
                            }
                            else
                            {
                                // We are still attached to our parent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the parent.
                                isDecoupler = true;
                                decoupledInStage = parentDecoupledInStage;
                                nodeLookup[attachedPart].AssignDecoupledInStage(attachedPart, p, nodeLookup, p.inverseStage);
                                AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                            }
                        }
                    }
                    break;
                }

                if (m.moduleName == "ProceduralFairingDecoupler")
                {
                    if (!m.Fields["decoupled"].GetValue<bool>(m) && m.stagingEnabled && p.stagingOn)
                    {
                        // We are decoupling our parent. The part and its children are not part of the ship when we decouple
                        isDecoupler = true;
                        decoupledInStage = p.inverseStage;
                        AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                        isDecoupler = true;
                        break;
                    }
                }
            }

            if (isLaunchClamp)
            {
                decoupledInStage = p.inverseStage > parentDecoupledInStage ? p.inverseStage : parentDecoupledInStage;
            }
            else if (!isDecoupler)
            {
                decoupledInStage = parentDecoupledInStage;
            }

            isSepratron = isEngine && isthrottleLocked && activatesEvenIfDisconnected && (inverseStage == decoupledInStage);

            AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
        }

        public static void print(object message)
        {
            Dispatcher.InvokeAsync(() => MonoBehaviour.print("[MechJeb2] " + message));
        }

        public double partThrust;

        public void SetConsumptionRates(float throttle, double atmospheres, double atmDensity, double machNumber)
        {
            if (isEngine)
            {
                resourceConsumptions.Clear();
                propellantFlows.Clear();

                //double sumThrustOverIsp = 0;
                partThrust = 0;

                isDrawingResources = false;

                foreach (EngineInfo engineInfo in engineInfos)
                {
                    ModuleEngines e = engineInfo.engineModule;
                    // thrust is correct.
                    Vector3d thrust;
                    // note that isp and massFlowRate do not include ignoreForIsp fuels like HTP and so need to be fixed for effective isp and the
                    // actual mdot of the rocket needs to be fixed to include HTP.
                    //
                    // IMHO: using ignoreForIsp is just wrong.  full stop.  engines should never set this and should set the correct effective isp in
                    // the config for the engine.  makes everything simpler and fixes the UI to show the correct ISP.  going this direction we are going
                    // to get bug reports that e.g. mechjeb is reporting 299s for an RD-108 when RF and KSP are displaying 308s everywhere.  which is not
                    // going to be a bug at all.  as long as the thrust is correct, the effective isp is correct, and the fuel fractions are correct then
                    // the rocket equation just works and HTP should not be "ignored".   keeping it out of the atmosphereCurve just makes this annoying
                    // here and screws up the KSP API display of ISP.
                    //
                    double isp, massFlowRate;
                    EngineValuesAtConditions(engineInfo, throttle, atmospheres, atmDensity, machNumber, out thrust, out isp, out massFlowRate, cosLoss: dVLinearThrust);
                    partThrust += thrust.magnitude;

                    if ( massFlowRate > 0 )
                        isDrawingResources = true;

                    double totalDensity = 0;

                    for(int j = 0; j < e.propellants.Count; j++)
                    {
                        var p = e.propellants[j];
                        double density = MuUtils.ResourceDensity(p.id);

                        // zero density draws (eC, air intakes, etc) are skipped, we have to assume you open your solar panels or
                        // air intakes or whatever it is you need for them to function.  they don't affect the mass of the vehicle
                        // so they do not affect the rocket equation.  they are assumed to be "renewable" or effectively infinite.
                        // (we keep them out of the propellantFlows dict here so they're just ignored by the sim later).
                        //
                        if (density > 0)
                        {
                            // hopefully different EngineModules in the same part don't have different flow modes for the same propellant
                            if (!propellantFlows.ContainsKey(p.id))
                                propellantFlows.Add(p.id, p.GetFlowMode());
                        }

                        // have to ignore ignoreForIsp fuels here since we're dealing with the massflowrate of the other fuels
                        if (!p.ignoreForIsp)
                        {
                            totalDensity += p.ratio * density;
                        }
                    }

                    // this is also the volume flow rate of the non-ignoreForIsp fuels.  although this is a bit janky since the p.ratios in most
                    // stock engines sum up to 2, not 1 (1.1 + 0.9), so this is not per-liter but per-summed-ratios (the massflowrate you get out
                    // of the atmosphere curves (above) are also similarly adjusted by these ratios -- it is a bit of a horror show).
                    double volumeFlowRate = massFlowRate / totalDensity;

                    for(int j = 0; j < e.propellants.Count; j++)
                    {
                        var p = e.propellants[j];
                        double density = MuUtils.ResourceDensity(p.id);

                        // this is the individual propellant volume rate.  we are including the ignoreForIsp fuels in this loop and this will
                        // correctly calculate the volume rates of all the propellants, in L/sec.  if you sum these it'll be larger than the
                        // volumeFlowRate by including both the ignoreForIsp fuels and if the ratios sum up to more than one.
                        double propVolumeRate = p.ratio * volumeFlowRate;

                        // same density check here as above to keep massless propellants out of the ResourceConsumptions dict as well
                        if (density > 0)
                        {
                            if (resourceConsumptions.ContainsKey(p.id))
                                resourceConsumptions[p.id] += propVolumeRate;
                            else
                                resourceConsumptions.Add(p.id, propVolumeRate);
                        }
                    }
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

        public void DebugDrainRates()
        {
            foreach (int type in resourceDrains.Keys)
            {
                print(partName + "'s drain rate of " + PartResourceLibrary.Instance.GetDefinition(type).name + "(" + type  + ") is " + resourceDrains[type] + " free=" + freeResources[type]);
            }
        }

        public double MaxTimeStep()
        {
            //DebugDrainRates();
            var param = new Smooth.Algebraics.Tuple<DefaultableDictionary<int, double>, double, DefaultableDictionary<int, double>, DefaultableDictionary<int, bool>>(resources, resourceRequestRemainingThreshold, resourceDrains, freeResources);
            if (!resourceDrains.KeysList.Slinq().Any((id, p) => !p.Item4[id] && p.Item1[id] > p.Item2, param)) return double.MaxValue;
            return resourceDrains.KeysList.Slinq().Where((id, p) => !p.Item4[id] && p.Item1[id] > p.Item2, param).Select((id, p) => p.Item1[id] / p.Item3[id], param).Min();
        }

        //Returns an enumeration of the resources this part burns
        public List<int> BurnedResources()
        {
            return resourceConsumptions.KeysList;
        }

        //returns whether this part contains any of the given resources
        public bool ContainsResources(List<int> whichResources)
        {
            var param = new Smooth.Algebraics.Tuple<DefaultableDictionary<int, double>, double>(resources, resourceRequestRemainingThreshold);
            //return whichResources.Any(id => resources[id] > DRAINED);
            return whichResources.Slinq().Any((id, r) => r.Item1[id] > r.Item2, param);
        }

        public bool CanDrawNeededResources(List<FuelNode> vessel)
        {
            // XXX: this fix is intended to fix SRBs which have burned out but which
            // still have an amount of fuel over the resourceRequestRemainingThreshold, which
            // can happen in RealismOverhaul.  this targets specifically "No propellants" because
            // we do not want flamed out jet engines to trigger this code if they just don't have
            // enough intake air, and any other causes.
            ModuleEngines e = part.Modules[0] as ModuleEngines;
            if (e != null && e.flameout && e.statusL2 == "No propellants")
                return false;

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

        // for a single EngineModule, get thrust + isp + massFlowRate
        private void EngineValuesAtConditions(EngineInfo engineInfo, double throttle, double atmPressure, double atmDensity, double machNumber, out Vector3d thrust, out double isp, out double massFlowRate, bool cosLoss = true)
        {
            isp = engineInfo.engineModule.ISPAtConditions(throttle, atmPressure, atmDensity, machNumber);
            double flowMultiplier = engineInfo.engineModule.FlowMultiplierAtConditions(atmDensity, machNumber);
            massFlowRate = engineInfo.engineModule.FlowRateAtConditions(throttle, flowMultiplier);
            thrust = ThrustAtConditions(engineInfo, massFlowRate, isp, cosLoss);
            //Debug.Log("thrust = " + thrust + " isp = " + isp + " massFlowRate = " + massFlowRate);
        }

        // for a single EngineModule, get its thrust vector (use EngineModuleFlowMultiplier and EngineModuleISP below)
        private Vector3d ThrustAtConditions(EngineInfo engineInfo, double massFlowRate, double isp, bool cosLoss = true)
        {
            if (massFlowRate <= 0)
                return Vector3d.zero;

            Vector3d thrustVector = engineInfo.thrustVector;

            if (cosLoss)
            {
                thrustVector = Vector3.Dot(vesselOrientation, thrustVector) * thrustVector.normalized;
            }

            return thrustVector * massFlowRate * engineInfo.engineModule.g * engineInfo.engineModule.multIsp * isp;
        }
    }
}
