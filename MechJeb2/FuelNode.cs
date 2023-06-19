using System;
using System.Collections.Generic;
using System.Reflection;
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
            // RealFuels fields for residuals and engine spoolup time
            private static FieldInfo _rfPredictedMaximumResiduals;
            private static FieldInfo _rfSpoolUpTime;

            public static void DoReflection()
            {
                if (!ReflectionUtils.isAssemblyLoaded("RealFuels")) return;

                _rfPredictedMaximumResiduals =
                    ReflectionUtils.getFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "predictedMaximumResiduals");
                if (_rfPredictedMaximumResiduals == null)
                {
                    Debug.Log(
                        "MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no predictedMaximumResiduals field, disabling residuals");
                }

                _rfSpoolUpTime = ReflectionUtils.getFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "effectiveSpoolUpTime");
                if (_rfSpoolUpTime == null)
                {
                    Debug.Log(
                        "MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no effectiveSpoolUpTime field, disabling spoolup");
                }
            }

            private readonly struct EngineInfo
            {
                public readonly ModuleEngines EngineModule;
                public readonly Vector3d      ThrustVector;
                public readonly double        ModuleResiduals;
                public readonly double        ModuleSpoolupTime;
                public readonly double        MaxThrust;

                public EngineInfo(ModuleEngines engineModule)
                {
                    this.EngineModule = engineModule;

                    MaxThrust = engineModule.maxThrust;

                    ThrustVector = Vector3d.zero;

                    for (int i = 0; i < engineModule.thrustTransforms.Count; i++)
                        ThrustVector -= engineModule.thrustTransforms[i].forward * engineModule.thrustTransformMultipliers[i];

                    double? temp = 0;

                    if (_rfPredictedMaximumResiduals != null)
                    {
                        try
                        {
                            temp = _rfPredictedMaximumResiduals.GetValue(engineModule) as double?;
                        }
                        catch (ArgumentException)
                        {
                            temp = 0;
                        }
                    }

                    ModuleResiduals = temp ?? 0;

                    float? temp2 = 0;
                    if (_rfSpoolUpTime != null)
                    {
                        try
                        {
                            temp2 = _rfSpoolUpTime.GetValue(engineModule) as float?;
                        }
                        catch (ArgumentException)
                        {
                            //FuelFlowSimulation.Print("For engine " + engineModule.part.partName + " failed to find spoolup time field!");
                            temp2 = 0;
                        }
                    }

                    ModuleSpoolupTime = temp2 ?? 0;
                }

                public EngineInfo(EngineInfo engineInfo)
                {
                    EngineModule      = engineInfo.EngineModule;
                    ThrustVector      = engineInfo.ThrustVector;
                    ModuleResiduals   = engineInfo.ModuleResiduals;
                    MaxThrust         = engineInfo.MaxThrust;
                    ModuleSpoolupTime = engineInfo.ModuleSpoolupTime;
                }
            }

            //the resources contained in the part
            private readonly DefaultableDictionary<int, double> _resources = new DefaultableDictionary<int, double>(0);
            //the resources the part has when full
            private readonly DefaultableDictionary<int, double> _resourcesFull = new DefaultableDictionary<int, double>(0);
            //the resources this part consumes per unit time when active at full throttle
            private readonly KeyableDictionary<int, double> _resourceConsumptions = new KeyableDictionary<int, double>();
            //the resources being drained from this part per unit time at the current simulation
            private readonly DefaultableDictionary<int, double> _resourceDrains = new DefaultableDictionary<int, double>(0);
            // the fraction of the resource which will be residual
            private readonly DefaultableDictionary<int, double> _resourceResidual = new DefaultableDictionary<int, double>(0);
            //the resources that are "free" and assumed to be infinite like IntakeAir
            private readonly DefaultableDictionary<int, bool> _freeResources = new DefaultableDictionary<int, bool>(false);

            // if a resource amount falls below this amount we say that the resource has been drained
            // set to the smallest amount that the user can see is non-zero in the resource tab or by
            // right-clicking.
            private const double DRAINED = 1E-4;

            //flow modes of propellants since the engine can override them
            private readonly KeyableDictionary<int, ResourceFlowMode> _propellantFlows  = new KeyableDictionary<int, ResourceFlowMode>();
            private readonly List<FuelNode>                           _crossfeedSources = new List<FuelNode>();

            public int    DecoupledInStage; //the stage in which this part will be decoupled from the rocket
            public int    InverseStage;     //stage in which this part is activated
            public bool   IsLaunchClamp;    //whether this part is a launch clamp
            public bool   IsSepratron;      //whether this part is a sepratron
            public bool   IsEngine;         //whether this part is an engine
            public bool   IsThrottleLocked;
            public bool   ActivatesEvenIfDisconnected;
            public bool   IsDrawingResources = true; // Is the engine actually using any resources
            public bool   HasResources;
            public double MaxThrust;

            private double _resourceRequestRemainingThreshold;
            private int    _resourcePriority;

            private double _dryMass;             //the mass of this part, not counting resource mass
            private double _crewMass;            //the mass of this part crew
            private float  _modulesUnstagedMass; // the mass of the modules of this part before staging
            private float  _modulesStagedMass;   // the mass of the modules of this part after staging

            private double _maxEngineResiduals; // fractional amount of residuals from RealFuels/ModuleEnginesRF

            public string PartName; //for debugging

            public  Part     Part;
            private bool     _dVLinearThrust;
            private Vector3d _vesselOrientation;

            private readonly List<EngineInfo> _engineInfos = new List<EngineInfo>();

            private static readonly Pool<FuelNode> _pool = new Pool<FuelNode>(Create, Reset);

            private delegate double CrewMass(ProtoCrewMember crew);

            private static readonly CrewMass _crewMassDelegate;

            static FuelNode()
            {
                if (Versioning.version_major == 1 && Versioning.version_minor < 11)
                    _crewMassDelegate = CrewMassOld;
                else
                    _crewMassDelegate = CrewMassNew;
            }

            private static double CrewMassOld(ProtoCrewMember crew)
            {
                return PhysicsGlobals.KerbalCrewMass;
            }

            private static double CrewMassNew(ProtoCrewMember crew)
            {
                return PhysicsGlobals.KerbalCrewMass + crew.ResourceMass() + crew.InventoryMass();
            }

            public static int PoolSize => _pool.Size;

            private static FuelNode Create()
            {
                return new FuelNode();
            }

            public void Release()
            {
                _pool.Release(this);
            }

            private static void Reset(FuelNode obj)
            {
            }

            public static FuelNode Borrow(Part part, bool dVLinearThrust)
            {
                FuelNode node = _pool.Borrow();
                node.Init(part, dVLinearThrust);
                return node;
            }

            public static FuelNode BorrowAndCopyFrom(FuelNode n)
            {
                FuelNode node = _pool.Borrow();
                node.Part                              = n.Part;
                node._dVLinearThrust                    = n._dVLinearThrust;
                node.IsEngine                          = n.IsEngine;
                node.IsThrottleLocked                  = n.IsThrottleLocked;
                node.ActivatesEvenIfDisconnected       = n.ActivatesEvenIfDisconnected;
                node.MaxThrust                         = n.MaxThrust;
                node.IsLaunchClamp                     = n.IsLaunchClamp;
                node.IsSepratron                       = n.IsSepratron;
                node._dryMass                           = n._dryMass;
                node._crewMass                          = n._crewMass;
                node._modulesStagedMass                 = n._modulesStagedMass;
                node.DecoupledInStage                  = n.DecoupledInStage;
                node._maxEngineResiduals                = n._maxEngineResiduals;
                node._vesselOrientation                 = n._vesselOrientation;
                node._modulesUnstagedMass               = n._modulesUnstagedMass;
                node.InverseStage                      = n.InverseStage;
                node.PartName                          = n.PartName;
                node._resourceRequestRemainingThreshold = n._resourceRequestRemainingThreshold;
                node._resourcePriority                  = n._resourcePriority;
                node.HasResources                      = n.HasResources;

                node._resources.Clear();
                node._resourcesFull.Clear();
                node._resourceConsumptions.Clear();
                node._resourceDrains.Clear();
                node._freeResources.Clear();
                node._resourceResidual.Clear();

                node._crossfeedSources.Clear();

                node._engineInfos.Clear();

                node._resources.Clear();
                foreach (int key in n._resources.KeysList)
                {
                    node._resources.Add(key, n._resources[key]);
                }

                foreach (int key in n._resourcesFull.KeysList)
                {
                    node._resourcesFull.Add(key, n._resourcesFull[key]);
                }

                foreach (int key in n._resourceConsumptions.KeysList)
                {
                    node._resourceConsumptions.Add(key, n._resourceConsumptions[key]);
                }

                foreach (int key in n._resourceDrains.KeysList)
                {
                    node._resourceDrains.Add(key, n._resourceDrains[key]);
                }

                foreach (int key in n._freeResources.KeysList)
                {
                    node._freeResources.Add(key, n._freeResources[key]);
                }

                foreach (int key in n._resourceResidual.KeysList)
                {
                    node._resourceResidual.Add(key, n._resourceResidual[key]);
                }

                foreach (EngineInfo e in n._engineInfos)
                {
                    node._engineInfos.Add(new EngineInfo(e));
                }

                // Note: Can't copy crossfeedSources yet. This needs to be done in a separate iteration after all the FuelNodes have been collected.

                return node;
            }

            private void Init(Part part, bool dVLinearThrust)
            {
                this.Part           = part;
                this._dVLinearThrust = dVLinearThrust;
                _resources.Clear();
                _resourcesFull.Clear();
                _resourceConsumptions.Clear();
                _resourceDrains.Clear();
                _freeResources.Clear();
                _resourceResidual.Clear();

                _crossfeedSources.Clear();

                IsEngine                    = false;
                IsThrottleLocked            = false;
                ActivatesEvenIfDisconnected = part.ActivatesEvenIfDisconnected;
                IsLaunchClamp               = part.IsLaunchClamp();

                _dryMass           = 0;
                _crewMass          = 0;
                _modulesStagedMass = 0;

                DecoupledInStage = int.MinValue;

                _maxEngineResiduals = 0.0;

                _vesselOrientation = HighLogic.LoadedScene == GameScenes.EDITOR
                    ? EditorLogic.VesselRotation * Vector3d.up
                    : part.vessel.GetTransform().up;

                _modulesUnstagedMass = 0;
                if (!IsLaunchClamp)
                {
                    _dryMass = part.prefabMass; // Intentionally ignore the physic flag.

                    if (HighLogic.LoadedSceneIsFlight && part.protoModuleCrew != null)
                        for (int i = 0; i < part.protoModuleCrew.Count; i++)
                        {
                            ProtoCrewMember crewMember = part.protoModuleCrew[i];
                            _crewMass += _crewMassDelegate(crewMember);
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
                                    _crewMass += _crewMassDelegate(crewMember);
                                }
                            }
                        }

                    _modulesUnstagedMass = part.GetModuleMassNoAlloc((float)_dryMass, ModifierStagingSituation.UNSTAGED);

                    _modulesStagedMass = part.GetModuleMassNoAlloc((float)_dryMass, ModifierStagingSituation.STAGED);

                    float currentModulesMass = part.GetModuleMassNoAlloc((float)_dryMass, ModifierStagingSituation.CURRENT);

                    // if it was manually staged
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (currentModulesMass == _modulesStagedMass) _modulesUnstagedMass = _modulesStagedMass;

                    //Print(part.partInfo.name.PadRight(25) + " " + part.mass.ToString("F4") + " " + part.GetPhysicslessChildMass().ToString("F4") + " " + modulesUnstagedMass.ToString("F4") + " " + modulesStagedMass.ToString("F4"));
                }

                InverseStage = part.inverseStage;
                PartName     = part.partInfo.name;

                _resourceRequestRemainingThreshold = Math.Max(part.resourceRequestRemainingThreshold, DRAINED);
                _resourcePriority                  = part.GetResourcePriority();

                //note which resources this part has stored
                for (int i = 0; i < part.Resources.Count; i++)
                {
                    PartResource r = part.Resources[i];
                    if (r.info.density > 0)
                    {
                        if (r.flowState)
                        {
                            _resources[r.info.id]     = r.amount;
                            _resourcesFull[r.info.id] = r.maxAmount;
                        }
                        else
                            _dryMass += r.amount * r.info.density; // disabled resources are just dead weight
                    }
                    else
                    {
                        _freeResources[r.info.id] = true;
                    }

                    // Including the ressource in the CRP.
                    if (r.info.name == "IntakeAir" || r.info.name == "IntakeLqd" || r.info.name == "IntakeAtm")
                        _freeResources[r.info.id] = true;
                }

                HasResources = _resources.Count > 0 || _freeResources.Count > 0;
                _engineInfos.Clear();

                // determine if we've got at least one useful ModuleEngine
                // we only do these test for the first ModuleEngines in the Part, could any other ones actually differ?
                for (int i = 0; i < part.Modules.Count; i++)
                {
                    if (!(part.Modules[i] is ModuleEngines e) || !e.isEnabled) continue;

                    // Only count engines that either are ignited or will ignite in the future:
                    if (!IsEngine && (HighLogic.LoadedSceneIsEditor || InverseStage < StageManager.CurrentStage || e.getIgnitionState) &&
                        (e.thrustPercentage > 0 || e.minThrust > 0))
                    {
                        // if an engine has been activated early, pretend it is in the current stage:
                        if (e.getIgnitionState && InverseStage < StageManager.CurrentStage)
                            InverseStage = StageManager.CurrentStage;

                        IsEngine         = true;
                        IsThrottleLocked = e.throttleLocked;
                    }

                    _engineInfos.Add(new EngineInfo(e));
                }

                MaxThrust = 0;
                // find our max maxEngineResiduals for this engine part from all the modules
                foreach (EngineInfo e in _engineInfos)
                {
                    _maxEngineResiduals =  Math.Max(_maxEngineResiduals, e.ModuleResiduals);
                    MaxThrust          += e.MaxThrust;
                }
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
                if (DecoupledInStage != int.MinValue)
                    return;

                bool isDecoupler = false;
                DecoupledInStage = parentDecoupledInStage;

                for (int i = 0; i < p.Modules.Count; i++)
                {
                    PartModule m = p.Modules[i];

                    if (m is ModuleDecouple mDecouple)
                        if (!mDecouple.isDecoupled && mDecouple.stagingEnabled && p.stagingOn)
                        {
                            if (mDecouple.isOmniDecoupler)
                            {
                                // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                isDecoupler      = true;
                                DecoupledInStage = p.inverseStage;
                                AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
                            }
                            else
                            {
                                AttachNode attach;
                                if (HighLogic.LoadedSceneIsEditor)
                                {
                                    attach = mDecouple.explosiveNodeID != "srf" ? p.FindAttachNode(mDecouple.explosiveNodeID) : p.srfAttachNode;
                                }
                                else
                                {
                                    attach = mDecouple.ExplosiveNode;
                                }

                                if (attach is { attachedPart: { } })
                                {
                                    if (attach.attachedPart == traversalParent && mDecouple.staged)
                                    {
                                        // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                        isDecoupler      = true;
                                        DecoupledInStage = p.inverseStage;
                                        AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
                                    }
                                    else
                                    {
                                        // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the traversalParent.
                                        isDecoupler      = true;
                                        DecoupledInStage = parentDecoupledInStage;
                                        nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, p, nodeLookup, p.inverseStage);
                                        AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
                                    }
                                }
                            }

                            break; // Hopefully no one made part with multiple decoupler modules ?
                        }

                    if (m is ModuleAnchoredDecoupler mAnchoredDecoupler)
                    {
                        if (!mAnchoredDecoupler.isDecoupled && mAnchoredDecoupler.stagingEnabled && p.stagingOn)
                        {
                            AttachNode attach;
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                attach = mAnchoredDecoupler.explosiveNodeID != "srf"
                                    ? p.FindAttachNode(mAnchoredDecoupler.explosiveNodeID)
                                    : p.srfAttachNode;
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
                                    DecoupledInStage = p.inverseStage;
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
                                }
                                else
                                {
                                    // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the traversalParent.
                                    isDecoupler      = true;
                                    DecoupledInStage = parentDecoupledInStage;
                                    nodeLookup[attach.attachedPart].AssignDecoupledInStage(attach.attachedPart, p, nodeLookup, p.inverseStage);
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
                                }
                            }

                            break;
                        }
                    }

                    if (m is ModuleDockingNode mDockingNode)
                    {
                        if (mDockingNode.staged && mDockingNode.stagingEnabled && p.stagingOn)
                        {
                            Part attachedPart = mDockingNode.referenceNode.attachedPart;
                            if (!(attachedPart is null))
                            {
                                if (attachedPart == traversalParent)
                                {
                                    // We are decoupling our traversalParent. The part and its children are not part of the ship when we decouple
                                    isDecoupler      = true;
                                    DecoupledInStage = p.inverseStage;
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
                                }
                                else
                                {
                                    // We are still attached to our traversalParent.  The part we decouple is dropped when we decouple.  The part and other children are dropped with the traversalParent.
                                    isDecoupler      = true;
                                    DecoupledInStage = parentDecoupledInStage;
                                    nodeLookup[attachedPart].AssignDecoupledInStage(attachedPart, p, nodeLookup, p.inverseStage);
                                    AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
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
                            DecoupledInStage = p.inverseStage;
                            AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
                            break;
                        }
                }

                if (IsLaunchClamp)
                    DecoupledInStage                    = p.inverseStage > parentDecoupledInStage ? p.inverseStage : parentDecoupledInStage;
                else if (!isDecoupler) DecoupledInStage = parentDecoupledInStage;

                IsSepratron = IsEngine && IsThrottleLocked && ActivatesEvenIfDisconnected && InverseStage == DecoupledInStage;

                AssignChildrenDecoupledInStage(p, traversalParent, nodeLookup, DecoupledInStage);
            }

            private static void Print(object message)
            {
                Dispatcher.InvokeAsync(() => MonoBehaviour.print("[MechJeb2] " + message));
            }

            public double PartThrust;
            public double PartSpoolupTime;

            public void SetConsumptionRates(float throttle, double atmospheres, double atmDensity, double machNumber)
            {
                if (IsEngine)
                {
                    _resourceConsumptions.Clear();
                    _propellantFlows.Clear();

                    //double sumThrustOverIsp = 0;
                    PartThrust      = 0;
                    PartSpoolupTime = 0;

                    IsDrawingResources = false;

                    foreach (EngineInfo engineInfo in _engineInfos)
                    {
                        ModuleEngines e = engineInfo.EngineModule;
                        // thrust is correct.
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
                        EngineValuesAtConditions(engineInfo, throttle, atmospheres, atmDensity, machNumber, out Vector3d thrust, out double massFlowRate,
                            _dVLinearThrust);
                        //Print($"EngineValuesAtConditions thrust:{thrust} isp:{isp}, massFlowRate:{massFlowRate}");
                        double thrMagnitude = thrust.magnitude;
                        PartThrust      += thrMagnitude;
                        PartSpoolupTime += thrMagnitude * engineInfo.ModuleSpoolupTime;

                        if (massFlowRate > 0)
                            IsDrawingResources = true;

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
                            {
                                // hopefully different EngineModules in the same part don't have different flow modes for the same propellant
                                if (!_propellantFlows.ContainsKey(p.id))
                                {
                                    _propellantFlows.Add(p.id, p.GetFlowMode());
                                }
                            }

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
                            if (density <= 0) continue;

                            if (_resourceConsumptions.ContainsKey(p.id))
                                _resourceConsumptions[p.id] += propVolumeRate;
                            else
                                _resourceConsumptions.Add(p.id, propVolumeRate);
                        }
                    }

                    if (PartThrust > 0)
                        PartSpoolupTime /= PartThrust;

                    //Print("For all engines, found spoolup time " + partSpoolupTime + " (with total thrust " + partThrust);
                }
            }

            public void AddCrossfeedSources(HashSet<Part> parts, Dictionary<Part, FuelNode> nodeLookup)
            {
                using HashSet<Part>.Enumerator it = parts.GetEnumerator();

                while (it.MoveNext())
                {
                    if (nodeLookup.TryGetValue(it.Current!, out FuelNode fuelnode) && fuelnode.HasResources)
                        _crossfeedSources.Add(fuelnode);
                }
            }

            //call this when a node no longer exists, so that this node knows that it's no longer a valid source
            public void RemoveSourceNode(FuelNode n)
            {
                _crossfeedSources.Remove(n);
            }

            //return the mass of the simulated FuelNode. This is not the same as the mass of the Part,
            //because the simulated node may have lost resources, and thus mass, during the simulation.
            public double Mass(int simStage)
            {
                //Print("\n(" + simStage + ") " + partName.PadRight(25) + " dryMass " + dryMass.ToString("F3")
                //          + " ResMass " + (resources.Keys.Sum(id => resources[id] * MuUtils.ResourceDensity(id))).ToString("F3")
                //          + " Fairing Mass " + (inverseStage < simStage ? fairingMass : 0).ToString("F3")
                //          + " (" + fairingMass.ToString("F3") + ")"
                //          + " ModuleMass " + moduleMass.ToString("F3")
                //          );

                //return dryMass + resources.Keys.Sum(id => resources[id] * MuUtils.ResourceDensity(id)) +
                double resMass = _resources.KeysList.Slinq().Select((r, rs) => rs[r] * MuUtils.ResourceDensity(r), _resources).Sum();
                return _dryMass + _crewMass + resMass +
                       (InverseStage < simStage ? _modulesUnstagedMass : _modulesStagedMass);
            }

            public void ResetDrainRates()
            {
                _resourceDrains.Clear();
            }

            public void DrainResources(double dt)
            {
                foreach (int type in _resourceDrains.KeysList)
                    if (!_freeResources[type])
                        _resources[type] -= dt * _resourceDrains[type];
            }

            public void DebugResources()
            {
                foreach (KeyValuePair<int, double> type in _resources)
                    Print(PartName + " " + PartResourceLibrary.Instance.GetDefinition(type.Key).name + " is " + type.Value);
            }

            public void DebugDrainRates()
            {
                foreach (int type in _resourceDrains.Keys)
                    Print(PartName + "'s drain rate of " + PartResourceLibrary.Instance.GetDefinition(type).name + "(" + type + ") is " +
                          _resourceDrains[type] + " free=" + _freeResources[type]);
            }

            public double MaxTimeStep()
            {
                double minDT = double.MaxValue;

                foreach (int id in _resourceDrains.KeysList)
                    if (!_freeResources[id] && _resources[id] > ResidualThreshold(id))
                        minDT = Math.Min(minDT, (_resources[id] - _resourceResidual[id] * _resourcesFull[id]) / _resourceDrains[id]);

                return minDT;
            }

            //Returns an enumeration of the resources this part burns
            public List<int> BurnedResources()
            {
                return _resourceConsumptions.KeysList;
            }

            //returns whether this part contains any of the given resources
            public bool ContainsResources(List<int> whichResources)
            {
                foreach (int id in whichResources)
                    if (_resources[id] > ResidualThreshold(id))
                        return true;

                return false;
            }

            public bool ContainsResource(int id)
            {
                return _resources[id] > ResidualThreshold(id);
            }

            public bool CanDrawNeededResources(List<FuelNode> vessel)
            {
                // XXX: this fix is intended to fix SRBs which have burned out but which
                // still have an amount of fuel over the ResidualThreshold[id], which
                // can happen in RealismOverhaul.  this targets specifically "No propellants" because
                // we do not want flamed out jet engines to trigger this code if they just don't have
                // enough intake air, and any other causes.
                // BIG FIXME: we're doing this in the thread and touching the KSP part object.

                if (Part.Modules[0] is ModuleEngines { flameout: true, statusL2: "No propellants" })
                    return false;

                foreach (int type in _resourceConsumptions.KeysList)
                {
                    ResourceFlowMode resourceFlowMode = _propellantFlows[type];
                    switch (resourceFlowMode)
                    {
                        case ResourceFlowMode.NO_FLOW:
                            //check if we contain the needed resource:
                            if (_resources[type] <= ResidualThreshold(type)) return false;
                            break;

                        case ResourceFlowMode.ALL_VESSEL:
                        case ResourceFlowMode.ALL_VESSEL_BALANCE:
                        case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                        case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            //check if any part contains the needed resource:
                            if (!vessel.Slinq().Any((n, t) => n._resources[t] > n.ResidualThreshold(type), type)) return false;
                            break;

                        case ResourceFlowMode.STAGE_STACK_FLOW:
                        case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                        case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                            // check if we can get any of the needed resources
                            if (!_crossfeedSources.Slinq().Any((n, t) => n._resources[t] > n.ResidualThreshold(type), type)) return false;
                            break;

                        case ResourceFlowMode.NULL:
                        default:
                            return false;
                    }
                }

                return true; //we didn't find ourselves lacking for any resource
            }

            public bool CanDrawResourceFrom(int type, FuelNode node)
            {
                ResourceFlowMode resourceFlowMode = _propellantFlows[type];

                switch (resourceFlowMode)
                {
                    case ResourceFlowMode.NO_FLOW:
                        return node == this;

                    case ResourceFlowMode.ALL_VESSEL:
                    case ResourceFlowMode.ALL_VESSEL_BALANCE:
                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                    case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                        return true;

                    case ResourceFlowMode.STAGE_STACK_FLOW:
                    case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                    case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                        return _crossfeedSources.Contains(node);

                    case ResourceFlowMode.NULL:
                    default:
                        return false;
                }
            }

            public void AssignResourceDrainRates(List<FuelNode> vessel)
            {
                foreach (int type in _resourceConsumptions.KeysList)
                {
                    if (_freeResources[type])
                        continue;

                    double amount = _resourceConsumptions[type];

                    ResourceFlowMode resourceFlowMode = _propellantFlows[type];
                    switch (resourceFlowMode)
                    {
                        case ResourceFlowMode.NO_FLOW:
                            //Print("NO_FLOW for " + partName + " searching for " + amount + " of " + PartResourceLibrary.Instance.GetDefinition(type).name);
                            _resourceResidual[type] =  _maxEngineResiduals;
                            _resourceDrains[type]   += amount;
                            break;

                        case ResourceFlowMode.ALL_VESSEL:
                        case ResourceFlowMode.ALL_VESSEL_BALANCE:
                            AssignMaxResiduals(type, _maxEngineResiduals, vessel);
                            AssignFuelDrainRateStagePriorityFlow(type, amount, false, vessel);
                            break;

                        case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                        case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            AssignMaxResiduals(type, _maxEngineResiduals, vessel);
                            AssignFuelDrainRateStagePriorityFlow(type, amount, true, vessel);
                            break;

                        case ResourceFlowMode.STAGE_STACK_FLOW:
                        case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                        case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                            //AssignFuelDrainRateStackPriority(type, true, amount);
                            AssignMaxResiduals(type, _maxEngineResiduals, _crossfeedSources);
                            AssignFuelDrainRateStagePriorityFlow(type, amount, true, _crossfeedSources);
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
                    n._resourceResidual[type] = Math.Max(n._resourceResidual[type], maxEngineResiduals);
                }
            }

            private void AssignFuelDrainRateStagePriorityFlow(int type, double amount, bool usePrio, List<FuelNode> vessel)
            {
                int maxPrio = int.MinValue;
                using Disposable<List<FuelNode>> dispoSources = ListPool<FuelNode>.Instance.BorrowDisposable();

                List<FuelNode> sources = dispoSources.value;
                //Print("AssignFuelDrainRateStagePriorityFlow for " + partName + " searching for " + amount + " of " + PartResourceLibrary.Instance.GetDefinition(type).name + " in " + vessel.Count + " parts ");
                for (int i = 0; i < vessel.Count; i++)
                {
                    FuelNode n = vessel[i];
                    if (n._resources[type] > n.ResidualThreshold(type))
                    {
                        if (usePrio)
                        {
                            if (n._resourcePriority > maxPrio)
                            {
                                maxPrio = n._resourcePriority;
                                sources.Clear();
                                sources.Add(n);
                            }
                            else if (n._resourcePriority == maxPrio)
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

                //Print(partName + " drains resource from " + sources.Count + " parts ");
                for (int i = 0; i < sources.Count; i++)
                    if (!_freeResources[type])
                        sources[i]._resourceDrains[type] += amount / sources.Count;
            }

            // for a single EngineModule, get thrust + isp + massFlowRate
            private void EngineValuesAtConditions(EngineInfo engineInfo, double throttle, double atmPressure, double atmDensity, double machNumber,
                out Vector3d thrust, out double massFlowRate, bool cosLoss = true)
            {
                double isp = engineInfo.EngineModule.ISPAtConditions(throttle, atmPressure, atmDensity, machNumber);
                double flowMultiplier = engineInfo.EngineModule.FlowMultiplierAtConditions(atmDensity, machNumber);
                massFlowRate = engineInfo.EngineModule.FlowRateAtConditions(throttle, flowMultiplier);
                thrust       = ThrustAtConditions(engineInfo, massFlowRate, isp, cosLoss);
                //Debug.Log("thrust = " + thrust + " isp = " + isp + " massFlowRate = " + massFlowRate);
            }

            // for a single EngineModule, get its thrust vector (use EngineModuleFlowMultiplier and EngineModuleISP below)
            private Vector3d ThrustAtConditions(EngineInfo engineInfo, double massFlowRate, double isp, bool cosLoss = true)
            {
                if (massFlowRate <= 0)
                    return Vector3d.zero;

                Vector3d thrustVector = engineInfo.ThrustVector;

                if (cosLoss) thrustVector = Vector3.Dot(_vesselOrientation, thrustVector) * thrustVector.normalized;

                return thrustVector * massFlowRate * engineInfo.EngineModule.g * engineInfo.EngineModule.multIsp * isp;
            }

            private double ResidualThreshold(int resourceId)
            {
                return Math.Max(_resourceRequestRemainingThreshold, _resourceResidual[resourceId] * _resourcesFull[resourceId]);
            }
        }
    }
}
