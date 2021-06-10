using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using KSP.UI;
using KSP.UI.Screens;
using Smooth.Dispose;
using Smooth.Pools;
using Smooth.Slinq;
using UnityEngine;
using UnityToolbag;

namespace MuMech
{
    //A FuelNode is a compact summary of a Part, containing only the information needed to run the fuel flow simulation.
    public partial class FuelFlowSimulation
    {
        public class FuelNode
        {
            // RealFuels.ModuleEngineRF ullage field to call via reflection
            private static FieldInfo RFpredictedMaximumResiduals;

            public static void DoReflection()
            {
                if (ReflectionUtils.isAssemblyLoaded("RealFuels"))
                {
                    RFpredictedMaximumResiduals = ReflectionUtils.getFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "predictedMaximumResiduals");
                    if (RFpredictedMaximumResiduals == null)
                    {
                        Debug.Log("MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no predictedMaximumResiduals field, disabling residuals");
                    }
                }
            }

            private readonly struct EngineInfo
            {
                public readonly ModuleEngines engineModule;
                public readonly Vector3d      thrustVector;
                public readonly double        moduleResiduals;

                public EngineInfo(ModuleEngines engineModule)
                {
                    this.engineModule = engineModule;

                    thrustVector = Vector3d.zero;

                    for (int i = 0; i < engineModule.thrustTransforms.Count; i++)
                        thrustVector -= engineModule.thrustTransforms[i].forward * engineModule.thrustTransformMultipliers[i];

                    double? temp = 0;

                    if (RFpredictedMaximumResiduals != null)
                    {
                        try
                        {
                            temp = RFpredictedMaximumResiduals.GetValue(engineModule) as double?;
                        }
                        catch (ArgumentException)
                        {
                            temp = 0;
                        }
                    }

                    moduleResiduals = temp ?? 0;
                }
            }

            private readonly DefaultableDictionary<int, double> resources = new DefaultableDictionary<int, double>(0); //the resources contained in the part
            private readonly DefaultableDictionary<int, double> resourcesFull = new DefaultableDictionary<int, double>(0);   //the resources the part has when full
            private readonly KeyableDictionary<int, double> resourceConsumptions = new KeyableDictionary<int, double>(); //the resources this part consumes per unit time when active at full throttle
            private readonly DefaultableDictionary<int, double> resourceDrains = new DefaultableDictionary<int, double>(0); //the resources being drained from this part per unit time at the current simulation
            private readonly DefaultableDictionary<int, double> resourceResidual = new DefaultableDictionary<int, double>(0); // the fraction of the resource which will be residual
            private readonly DefaultableDictionary<int, bool> freeResources = new DefaultableDictionary<int, bool>(false); //the resources that are "free" and assumed to be infinite like IntakeAir

            // if a resource amount falls below this amount we say that the resource has been drained
            // set to the smallest amount that the user can see is non-zero in the resource tab or by
            // right-clicking.
            private const double DRAINED = 1E-4;

            private readonly KeyableDictionary<int, ResourceFlowMode> propellantFlows = new KeyableDictionary<int, ResourceFlowMode>(); //flow modes of propellants since the engine can override them

            private readonly List<FuelNode> crossfeedSources = new List<FuelNode>();

            public int  decoupledInStage; //the stage in which this part will be decoupled from the rocket
            public int  inverseStage;     //stage in which this part is activated
            public bool isLaunchClamp;    //whether this part is a launch clamp
            public bool isSepratron;      //whether this part is a sepratron
            public bool isEngine;         //whether this part is an engine
            public bool isthrottleLocked;
            public bool activatesEvenIfDisconnected;
            public bool isDrawingResources = true; // Is the engine actually using any resources

            private double resourceRequestRemainingThreshold;
            private int    resourcePriority;

            private double dryMass;             //the mass of this part, not counting resource mass
            private double crewMass;            //the mass of this part crew
            private float  modulesUnstagedMass; // the mass of the modules of this part before staging
            private float  modulesStagedMass;   // the mass of the modules of this part after staging

            private double maxEngineResiduals; // fractional amount of residuals from RealFuels/ModuleEnginesRF

            public string partName; //for debugging

            private Part     part;
            private bool     dVLinearThrust;
            private Vector3d vesselOrientation;

            private readonly List<EngineInfo> engineInfos = new List<EngineInfo>();

            private static readonly Pool<FuelNode> pool = new Pool<FuelNode>(Create, Reset);

            private delegate double CrewMass(ProtoCrewMember crew);

            private static readonly CrewMass CrewMassDelegate;

            static FuelNode()
            {
                if (Versioning.version_major == 1 && Versioning.version_minor < 11)
                    CrewMassDelegate = CrewMassOld;
                else
                    CrewMassDelegate = CrewMassNew;
            }

            private static double CrewMassOld(ProtoCrewMember crew)
            {
                return PhysicsGlobals.KerbalCrewMass;
            }

            private static double CrewMassNew(ProtoCrewMember crew)
            {
                return PhysicsGlobals.KerbalCrewMass + crew.ResourceMass() + crew.InventoryMass();
            }

            public static int PoolSize => pool.Size;

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
                this.part           = part;
                this.dVLinearThrust = dVLinearThrust;
                resources.Clear();
                resourcesFull.Clear();
                resourceConsumptions.Clear();
                resourceDrains.Clear();
                freeResources.Clear();
                resourceResidual.Clear();

                crossfeedSources.Clear();

                isEngine                    = false;
                isthrottleLocked            = false;
                activatesEvenIfDisconnected = part.ActivatesEvenIfDisconnected;
                isLaunchClamp               = part.IsLaunchClamp();

                dryMass           = 0;
                crewMass          = 0;
                modulesStagedMass = 0;

                decoupledInStage = int.MinValue;

                maxEngineResiduals = 0.0;

                vesselOrientation = HighLogic.LoadedScene == GameScenes.EDITOR
                    ? EditorLogic.VesselRotation * Vector3d.up
                    : part.vessel.GetTransform().up;

                modulesUnstagedMass = 0;
                if (!isLaunchClamp)
                {
                    dryMass = part.prefabMass; // Intentionally ignore the physic flag.

                    if (HighLogic.LoadedSceneIsFlight && part.protoModuleCrew != null)
                        for (int i = 0; i < part.protoModuleCrew.Count; i++)
                        {
                            ProtoCrewMember crewMember = part.protoModuleCrew[i];
                            crewMass += CrewMassDelegate(crewMember);
                        }
                    else if (HighLogic.LoadedSceneIsEditor)
                        if (CrewAssignmentDialog.Instance != null && CrewAssignmentDialog.Instance.CurrentManifestUnsafe != null)
                        {
                            PartCrewManifest partCrewManifest = CrewAssignmentDialog.Instance.CurrentManifestUnsafe.GetPartCrewManifest(part.craftID);
                            if (partCrewManifest != null)
                            {
                                ProtoCrewMember[] partCrew = null;
                                partCrewManifest.GetPartCrew(ref partCrew);

                                for (int i = 0; i < partCrew.Length; i++)
                                {
                                    ProtoCrewMember crewMember = partCrew[i];
                                    if (crewMember == null) continue;
                                    crewMass += CrewMassDelegate(crewMember);
                                }
                            }
                        }

                    modulesUnstagedMass = part.GetModuleMassNoAlloc((float) dryMass, ModifierStagingSituation.UNSTAGED);

                    modulesStagedMass = part.GetModuleMassNoAlloc((float) dryMass, ModifierStagingSituation.STAGED);

                    float currentModulesMass = part.GetModuleMassNoAlloc((float) dryMass, ModifierStagingSituation.CURRENT);

                    // if it was manually staged
                    if (currentModulesMass == modulesStagedMass) modulesUnstagedMass = modulesStagedMass;

                    //print(part.partInfo.name.PadRight(25) + " " + part.mass.ToString("F4") + " " + part.GetPhysicslessChildMass().ToString("F4") + " " + modulesUnstagedMass.ToString("F4") + " " + modulesStagedMass.ToString("F4"));
                }

                inverseStage = part.inverseStage;
                partName     = part.partInfo.name;

                resourceRequestRemainingThreshold = Math.Max(part.resourceRequestRemainingThreshold, DRAINED);
                resourcePriority                  = part.GetResourcePriority();

                //note which resources this part has stored
                for (int i = 0; i < part.Resources.Count; i++)
                {
                    PartResource r = part.Resources[i];
                    if (r.info.density > 0)
                    {
                        if (r.flowState)
                        {
                            resources[r.info.id]     = r.amount;
                            resourcesFull[r.info.id] = r.maxAmount;
                        }
                        else
                            dryMass += r.amount * r.info.density; // disabled resources are just dead weight
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
                    if (!isEngine && (HighLogic.LoadedSceneIsEditor || inverseStage < StageManager.CurrentStage || e.getIgnitionState) &&
                        (e.thrustPercentage > 0 || e.minThrust > 0))
                    {
                        // if an engine has been activated early, pretend it is in the current stage:
                        if (e.getIgnitionState && inverseStage < StageManager.CurrentStage)
                            inverseStage = StageManager.CurrentStage;

                        isEngine         = true;
                        isthrottleLocked = e.throttleLocked;
                    }

                    engineInfos.Add(new EngineInfo(e));
                }

                // find our max maxEngineResiduals for this engine part from all the modules
                foreach (EngineInfo e in engineInfos)
                    maxEngineResiduals = Math.Max(maxEngineResiduals, e.moduleResiduals);
            }

            // We are not necessarily traversing from the root part but from any interior part, so that p.parent is just another potential child node
            // in our traversal.  This is a helper to loop over all the children (including the "p.parent") in our traversal.
            private void AssignChildrenDecoupledInStage(Part p, Part traversalParent, Dictionary<Part, FuelNode> nodeLookup,
                int parentDecoupledInStage)
            {
                for (int i = 0; i < p.children.Count; i++)
                {
                    Part child = p.children[i];
                    if (child != null && child != traversalParent)
                        nodeLookup[child].AssignDecoupledInStage(child, p, nodeLookup, parentDecoupledInStage);
                }

                if (p.parent != null && p.parent != traversalParent)
                    nodeLookup[p.parent].AssignDecoupledInStage(p.parent, p, nodeLookup, parentDecoupledInStage);
            }

            // Determine when this part will be decoupled given when its traversal-order parent will be decoupled.
            // Then recurse to all of this part's "children" (including the p.parent if the traversalParent is coming from a child).
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

                    var mDecouple = m as ModuleDecouple;
                    if (mDecouple != null)
                        if (!mDecouple.isDecoupled && mDecouple.stagingEnabled && p.stagingOn)
                        {
                            if (mDecouple.isOmniDecoupler)
                            {
                                // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                isDecoupler      = true;
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
                                        // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                        isDecoupler      = true;
                                        decoupledInStage = p.inverseStage;
                                        AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                    }
                                    else
                                    {
                                        // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the traversalParent.
                                        isDecoupler      = true;
                                        decoupledInStage = parentDecoupledInStage;
                                        nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, p, nodeLookup, p.inverseStage);
                                        AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                    }
                                }
                            }

                            break; // Hopefully no one made part with multiple decoupler modules ?
                        }

                    var mAnchoredDecoupler = m as ModuleAnchoredDecoupler;
                    if (mAnchoredDecoupler != null)
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
                                    // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                    isDecoupler      = true;
                                    decoupledInStage = p.inverseStage;
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                }
                                else
                                {
                                    // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the traversalParent.
                                    isDecoupler      = true;
                                    decoupledInStage = parentDecoupledInStage;
                                    nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, p, nodeLookup, p.inverseStage);
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                }
                            }

                            break;
                        }

                    var mDockingNode = m as ModuleDockingNode;
                    if (mDockingNode != null)
                    {
                        if (mDockingNode.staged && mDockingNode.stagingEnabled && p.stagingOn)
                        {
                            Part attachedPart = mDockingNode.referenceNode.attachedPart;
                            if (attachedPart != null)
                            {
                                if (attachedPart == traversalParent)
                                {
                                    // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                    isDecoupler      = true;
                                    decoupledInStage = p.inverseStage;
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                    isDecoupler = true;
                                }
                                else
                                {
                                    // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the traversalParent.
                                    isDecoupler      = true;
                                    decoupledInStage = parentDecoupledInStage;
                                    nodeLookup[attachedPart].AssignDecoupledInStage(attachedPart, p, nodeLookup, p.inverseStage);
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                                }
                            }
                        }

                        break;
                    }

                    if (m.moduleName == "ProceduralFairingDecoupler")
                        if (!m.Fields["decoupled"].GetValue<bool>(m) && m.stagingEnabled && p.stagingOn)
                        {
                            // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                            isDecoupler      = true;
                            decoupledInStage = p.inverseStage;
                            AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, decoupledInStage);
                            isDecoupler = true;
                            break;
                        }
                }

                if (isLaunchClamp)
                    decoupledInStage                    = p.inverseStage > parentDecoupledInStage ? p.inverseStage : parentDecoupledInStage;
                else if (!isDecoupler) decoupledInStage = parentDecoupledInStage;

                isSepratron = isEngine && isthrottleLocked && activatesEvenIfDisconnected && inverseStage == decoupledInStage;

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
                        EngineValuesAtConditions(engineInfo, throttle, atmospheres, atmDensity, machNumber, out thrust, out isp, out massFlowRate,
                            dVLinearThrust);
                        partThrust += thrust.magnitude;

                        if (massFlowRate > 0)
                            isDrawingResources = true;

                        double totalDensity = 0;

                        for (int j = 0; j < e.propellants.Count; j++)
                        {
                            Propellant p = e.propellants[j];
                            double density = MuUtils.ResourceDensity(p.id);

                            // zero density draws (eC, air intakes, etc) are skipped, we have to assume you open your solar panels or
                            // air intakes or whatever it is you need for them to function.  they don't affect the mass of the vehicle
                            // so they do not affect the rocket equation.  they are assumed to be "renewable" or effectively infinite.
                            // (we keep them out of the propellantFlows dict here so they're just ignored by the sim later).
                            //
                            if (density > 0)
                                // hopefully different EngineModules in the same part don't have different flow modes for the same propellant
                                if (!propellantFlows.ContainsKey(p.id))
                                    propellantFlows.Add(p.id, p.GetFlowMode());

                            // have to ignore ignoreForIsp fuels here since we're dealing with the massflowrate of the other fuels
                            if (!p.ignoreForIsp) totalDensity += p.ratio * density;
                        }

                        // this is also the volume flow rate of the non-ignoreForIsp fuels.  although this is a bit janky since the p.ratios in most
                        // stock engines sum up to 2, not 1 (1.1 + 0.9), so this is not per-liter but per-summed-ratios (the massflowrate you get out
                        // of the atmosphere curves (above) are also similarly adjusted by these ratios -- it is a bit of a horror show).
                        double volumeFlowRate = massFlowRate / totalDensity;

                        for (int j = 0; j < e.propellants.Count; j++)
                        {
                            Propellant p = e.propellants[j];
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
                using (HashSet<Part>.Enumerator it = parts.GetEnumerator())
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
                return dryMass + crewMass + resMass +
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
                    print(partName + "'s drain rate of " + PartResourceLibrary.Instance.GetDefinition(type).name + "(" + type + ") is " +
                          resourceDrains[type] + " free=" + freeResources[type]);
            }

            public double MaxTimeStep()
            {
                double minDT = double.MaxValue;

                foreach (int id in resourceDrains.KeysList)
                    if (!freeResources[id] && resources[id] > ResidualThreshold(id))
                        minDT = Math.Min(minDT, ( resources[id] - resourceResidual[id] * resourcesFull[id] ) / resourceDrains[id]);

                return minDT;
            }

            //Returns an enumeration of the resources this part burns
            public List<int> BurnedResources()
            {
                return resourceConsumptions.KeysList;
            }

            //returns whether this part contains any of the given resources
            public bool ContainsResources(List<int> whichResources)
            {
                foreach (int id in whichResources)
                    if (resources[id] > ResidualThreshold(id))
                        return true;

                return false;
            }

            public bool CanDrawNeededResources(List<FuelNode> vessel)
            {
                // XXX: this fix is intended to fix SRBs which have burned out but which
                // still have an amount of fuel over the ResidualThreshold[id], which
                // can happen in RealismOverhaul.  this targets specifically "No propellants" because
                // we do not want flamed out jet engines to trigger this code if they just don't have
                // enough intake air, and any other causes.
                // BIG FIXME: we're doing this in the thread and touching the KSP part object.

                if (part.Modules[0] is ModuleEngines {flameout: true, statusL2: "No propellants"})
                    return false;

                foreach (int type in resourceConsumptions.KeysList)
                {
                    ResourceFlowMode resourceFlowMode = propellantFlows[type];
                    switch (resourceFlowMode)
                    {
                        case ResourceFlowMode.NO_FLOW:
                            //check if we contain the needed resource:
                            if (resources[type] <= ResidualThreshold(type)) return false;
                            break;

                        case ResourceFlowMode.ALL_VESSEL:
                        case ResourceFlowMode.ALL_VESSEL_BALANCE:
                        case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                        case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            //check if any part contains the needed resource:
                            if (!vessel.Slinq().Any((n, t) => n.resources[t] > n.ResidualThreshold(type), type)) return false;
                            break;

                        case ResourceFlowMode.STAGE_STACK_FLOW:
                        case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                        case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                            // check if we can get any of the needed resources
                            if (!crossfeedSources.Slinq().Any((n, t) => n.resources[t] > n.ResidualThreshold(type), type)) return false;
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

                    ResourceFlowMode resourceFlowMode = propellantFlows[type];
                    switch (resourceFlowMode)
                    {
                        case ResourceFlowMode.NO_FLOW:
                            resourceResidual[type] =  maxEngineResiduals;
                            resourceDrains[type]   += amount;
                            break;

                        case ResourceFlowMode.ALL_VESSEL:
                        case ResourceFlowMode.ALL_VESSEL_BALANCE:
                            AssignMaxResiduals(type, maxEngineResiduals, vessel);
                            AssignFuelDrainRateStagePriorityFlow(type, amount, false, vessel);
                            break;

                        case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                        case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            AssignMaxResiduals(type, maxEngineResiduals, vessel);
                            AssignFuelDrainRateStagePriorityFlow(type, amount, true, vessel);
                            break;

                        case ResourceFlowMode.STAGE_STACK_FLOW:
                        case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                        case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                            //AssignFuelDrainRateStackPriority(type, true, amount);
                            AssignMaxResiduals(type, maxEngineResiduals, crossfeedSources);
                            AssignFuelDrainRateStagePriorityFlow(type, amount, true, crossfeedSources);
                            break;
                    }
                }
            }

            // this assigns the maxResidauls from the engine to all the parts it is drawing from
            private void AssignMaxResiduals(int type, double maxEngineResiduals, List<FuelNode> vessel)
            {
                for (int i = 0; i < vessel.Count; i++)
                {
                    FuelNode n = vessel[i];
                    n.resourceResidual[type] = Math.Max(n.resourceResidual[type], maxEngineResiduals);
                }
            }

            private void AssignFuelDrainRateStagePriorityFlow(int type, double amount, bool usePrio, List<FuelNode> vessel)
            {
                int maxPrio = int.MinValue;
                using Disposable<List<FuelNode>> dispoSources = ListPool<FuelNode>.Instance.BorrowDisposable();

                List<FuelNode> sources = dispoSources.value;
                //print("AssignFuelDrainRateStagePriorityFlow for " + partName + " searching for " + amount + " of " + PartResourceLibrary.Instance.GetDefinition(type).name + " in " + vessel.Count + " parts ");
                for (int i = 0; i < vessel.Count; i++)
                {
                    FuelNode n = vessel[i];
                    if (n.resources[type] > n.ResidualThreshold(type))
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
                    if (!freeResources[type])
                        sources[i].resourceDrains[type] += amount / sources.Count;
            }

            // for a single EngineModule, get thrust + isp + massFlowRate
            private void EngineValuesAtConditions(EngineInfo engineInfo, double throttle, double atmPressure, double atmDensity, double machNumber,
                out Vector3d thrust, out double isp, out double massFlowRate, bool cosLoss = true)
            {
                isp = engineInfo.engineModule.ISPAtConditions(throttle, atmPressure, atmDensity, machNumber);
                double flowMultiplier = engineInfo.engineModule.FlowMultiplierAtConditions(atmDensity, machNumber);
                massFlowRate = engineInfo.engineModule.FlowRateAtConditions(throttle, flowMultiplier);
                thrust       = ThrustAtConditions(engineInfo, massFlowRate, isp, cosLoss);
                //Debug.Log("thrust = " + thrust + " isp = " + isp + " massFlowRate = " + massFlowRate);
            }

            // for a single EngineModule, get its thrust vector (use EngineModuleFlowMultiplier and EngineModuleISP below)
            private Vector3d ThrustAtConditions(EngineInfo engineInfo, double massFlowRate, double isp, bool cosLoss = true)
            {
                if (massFlowRate <= 0)
                    return Vector3d.zero;

                Vector3d thrustVector = engineInfo.thrustVector;

                if (cosLoss) thrustVector = Vector3.Dot(vesselOrientation, thrustVector) * thrustVector.normalized;

                return thrustVector * massFlowRate * engineInfo.engineModule.g * engineInfo.engineModule.multIsp * isp;
            }

            private double ResidualThreshold(int resourceId)
            {
                return Math.Max(resourceRequestRemainingThreshold, resourceResidual[resourceId] * resourcesFull[resourceId]);
            }
        }
    }
}
