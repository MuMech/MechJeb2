#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using KSP.UI;
using MechJebLib.Simulations.PartModules;
using MuMech;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Simulations
{
    public partial class SimVesselManager
    {
        public class SimVesselUpdater
        {
            private readonly SimVesselManager _manager;

            private Dictionary<Part, SimPart>             _partMapping              => _manager._partMapping;
            private Dictionary<SimPart, Part>             _inversePartMapping       => _manager._inversePartMapping;
            private Dictionary<SimPartModule, PartModule> _inversePartModuleMapping => _manager._inversePartModuleMapping;
            private SimVessel                             _vessel                   => _manager._vessel;

            private delegate double CrewMass(ProtoCrewMember crew);

            private static readonly CrewMass _crewMassDelegate;

            private static readonly FieldInfo? _rfPredictedMaximumResiduals;
            private static readonly FieldInfo? _rfSpoolUpTime;

            public SimVesselUpdater(SimVesselManager manager)
            {
                _manager = manager;
            }

            static SimVesselUpdater()
            {
                _crewMassDelegate = Versioning.version_major == 1 && Versioning.version_minor < 11 ? (CrewMass)CrewMassOld : CrewMassNew;

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

            private static double CrewMassOld(ProtoCrewMember crew)
            {
                return PhysicsGlobals.KerbalCrewMass;
            }

            private static double CrewMassNew(ProtoCrewMember crew)
            {
                return PhysicsGlobals.KerbalCrewMass + crew.ResourceMass() + crew.InventoryMass();
            }

            internal void Update()
            {
                UpdateParts();
            }

            private void UpdateParts()
            {
                foreach (SimPart part in _vessel.Parts)
                {
                    UpdatePart(part);
                }
            }

            private void UpdatePart(SimPart part)
            {
                Part kspPart = _inversePartMapping[part];

                part.InverseStage                      = kspPart.inverseStage;
                part.ActivatesEvenIfDisconnected       = kspPart.ActivatesEvenIfDisconnected;
                part.StagingOn                         = kspPart.stagingOn;
                part.ResourcePriority                  = kspPart.GetResourcePriority();
                part.ResourceRequestRemainingThreshold = kspPart.resourceRequestRemainingThreshold;
                part.Mass                              = kspPart.mass;
                part.Name                              = kspPart.name;
                part.DryMass                           = kspPart.prefabMass;
                part.ModulesStagedMass                 = GetModuleMass(kspPart, kspPart.prefabMass, ModifierStagingSituation.STAGED);
                part.ModulesUnstagedMass               = GetModuleMass(kspPart, kspPart.prefabMass, ModifierStagingSituation.UNSTAGED);
                part.StagingOn                         = kspPart.stagingOn;
                part.EngineResiduals                   = 0;

                HandleCrewMass(part, kspPart);

                UpdateResources(part, kspPart);

                UpdateModules(part);
            }

            private void HandleCrewMass(SimPart part, Part kspPart)
            {
                part.CrewMass = 0;

                if (HighLogic.LoadedSceneIsFlight && kspPart.protoModuleCrew != null)
                    for (int i = 0; i < kspPart.protoModuleCrew.Count; i++)
                    {
                        ProtoCrewMember crewMember = kspPart.protoModuleCrew[i];
                        part.CrewMass += _crewMassDelegate(crewMember);
                    }
                else if (HighLogic.LoadedSceneIsEditor)
                    if (!(CrewAssignmentDialog.Instance is null) && CrewAssignmentDialog.Instance.CurrentManifestUnsafe != null)
                    {
                        PartCrewManifest partCrewManifest = CrewAssignmentDialog.Instance.CurrentManifestUnsafe.GetPartCrewManifest(kspPart.craftID);
                        if (partCrewManifest != null)
                        {
                            ProtoCrewMember?[]? partCrew = null;
                            partCrewManifest.GetPartCrew(ref partCrew);

                            for (int i = 0; i < partCrew.Length; i++)
                            {
                                ProtoCrewMember? crewMember = partCrew[i];
                                if (crewMember == null) continue;
                                part.CrewMass += _crewMassDelegate(crewMember);
                            }
                        }
                    }
            }

            private void UpdateResources(SimPart part, Part kspPart)
            {
                part.Resources.Clear();

                for (int i = 0; i < kspPart.Resources.Count; i++)
                {
                    PartResource kspResource = kspPart.Resources[i];

                    if (!kspResource.flowState)
                    {
                        // disabled resources are dead weight and cannot be enabled by staging
                        part.DryMass += kspResource.amount + kspResource.info.density;
                        continue;
                    }

                    var resource = new SimResource
                    {
                        Amount    = kspResource.amount,
                        MaxAmount = kspResource.maxAmount,
                        Id        = kspResource.info.id,
                        Free      = kspResource.info.density == 0,
                        Density   = kspResource.info.density,
                        Residual  = 0
                    };

                    part.Resources[resource.Id] = resource;
                }
            }

            private void UpdateModules(SimPart part)
            {
                foreach (SimPartModule module in part.Modules)
                {
                    UpdateModule(part, module);
                }
            }

            private void UpdateModule(SimPart part, SimPartModule m)
            {
                PartModule kspModule = _inversePartModuleMapping[m];

                m.ModuleIsEnabled = kspModule.moduleIsEnabled;
                m.StagingEnabled  = kspModule.stagingEnabled;

                switch (m)
                {
                    case SimModuleEngines engine:
                        UpdateModuleEngines(part, engine, kspModule as ModuleEngines);
                        break;
                    case SimLaunchClamp _:
                        // intentionally left blank
                        break;
                    case SimModuleDecouple decoupler:
                        UpdateModuleDecouplerBase(decoupler, kspModule as ModuleDecouplerBase);
                        break;
                    case SimModuleDockingNode decoupler:
                        UpdateDockingNode(decoupler, kspModule as ModuleDockingNode);
                        break;
                    case SimModuleRCS rcs:
                        UpdateModuleRCS(rcs, kspModule as ModuleRCS);
                        break;
                    case SimProceduralFairingDecoupler decoupler:
                        UpdateProceduralFairingDecoupler(decoupler, kspModule);
                        break;
                }
            }

            private void UpdateModuleEngines(SimPart part, SimModuleEngines engine, ModuleEngines? kspEngine)
            {
                if (kspEngine is null)
                {
                    Log("Internal FuelFlowSimluation Error: SimModuleEngines mapped to wrong type");
                    return;
                }

                engine.IsEnabled = kspEngine.isEnabled;

                engine.ThrottleLocked       = kspEngine.throttleLocked;
                engine.IsOperational        = kspEngine.isOperational;
                engine.ThrustPercentage     = kspEngine.thrustPercentage;
                engine.MaxFuelFlow          = kspEngine.maxFuelFlow;
                engine.MinFuelFlow          = kspEngine.minFuelFlow;
                engine.FlowMultiplier       = kspEngine.flowMultiplier;
                engine.G                    = kspEngine.g;
                engine.MaxThrust            = kspEngine.maxThrust;
                engine.MinThrust            = kspEngine.minThrust;
                engine.MultIsp              = kspEngine.multIsp;
                engine.Clamp                = kspEngine.CLAMP;
                engine.FlowMultCap          = kspEngine.flowMultCap;
                engine.FlowMultCapSharpness = kspEngine.flowMultCapSharpness;
                engine.AtmChangeFlow        = kspEngine.atmChangeFlow;
                engine.UseAtmCurve          = kspEngine.useAtmCurve;
                engine.UseAtmCurveIsp       = kspEngine.useAtmCurveIsp;
                engine.UseThrottleIspCurve  = kspEngine.useThrottleIspCurve;
                engine.UseThrustCurve       = kspEngine.useThrustCurve;
                engine.UseVelCurve          = kspEngine.useVelCurve;
                engine.UseVelCurveIsp       = kspEngine.useVelCurveIsp;

                engine.ThrustCurve.LoadH1(kspEngine.thrustCurve);
                engine.ThrottleIspCurve.LoadH1(kspEngine.throttleIspCurve);
                engine.ThrottleIspCurveAtmStrength.LoadH1(kspEngine.throttleIspCurveAtmStrength);
                engine.VelCurve.LoadH1(kspEngine.velCurve);
                engine.VelCurveIsp.LoadH1(kspEngine.velCurveIsp);
                engine.ATMCurve.LoadH1(kspEngine.atmCurve);
                engine.ATMCurveIsp.LoadH1(kspEngine.atmCurveIsp);
                engine.AtmosphereCurve.LoadH1(kspEngine.atmosphereCurve);

                engine.NoPropellants = kspEngine is { flameout: true, statusL2: "No propellants" };

                engine.ThrustTransformMultipliers.Clear();
                foreach (double multiplier in kspEngine.thrustTransformMultipliers)
                    engine.ThrustTransformMultipliers.Add(multiplier);

                engine.ThrustDirectionVectors.Clear();
                foreach (Transform transform in kspEngine.thrustTransforms)
                    // thrust transforms point at the flamey end and we want to point at the pointy end
                    engine.ThrustDirectionVectors.Add(MathExtensions.WorldToV3Rotated(-transform.forward));

                engine.Propellants.Clear();
                foreach (Propellant p in kspEngine.propellants)
                    engine.Propellants.Add(new SimPropellant(p.id, p.ignoreForIsp, p.ratio, (SimFlowMode)p.GetFlowMode(),
                        PartResourceLibrary.Instance.GetDefinition(p.id).density));

                double? temp = 0;

                if (_rfPredictedMaximumResiduals != null)
                {
                    try
                    {
                        temp = _rfPredictedMaximumResiduals.GetValue(kspEngine) as double?;
                    }
                    catch (ArgumentException)
                    {
                        temp = 0;
                    }
                }

                engine.ModuleResiduals = temp ?? 0;

                part.EngineResiduals = Math.Max(part.EngineResiduals, engine.ModuleResiduals);

                float? temp2 = 0;
                if (_rfSpoolUpTime != null)
                {
                    try
                    {
                        temp2 = _rfSpoolUpTime.GetValue(kspEngine) as float?;
                    }
                    catch (ArgumentException)
                    {
                        temp2 = 0;
                    }
                }

                engine.ModuleSpoolupTime = temp2 ?? 0;

                part.IsThrottleLocked = kspEngine.throttleLocked;
            }

            private void UpdateDockingNode(SimModuleDockingNode decoupler, ModuleDockingNode? kspModuleDockingNode)
            {
                if (kspModuleDockingNode is null)
                {
                    Log("Internal FuelFlowSimluation Error: SimModuleDockingNode mapped to wrong type");
                    return;
                }

                decoupler.Staged = kspModuleDockingNode.staged;

                if (!(kspModuleDockingNode.referenceNode.attachedPart is null))
                    decoupler.AttachedPart = _partMapping[kspModuleDockingNode.referenceNode.attachedPart];
            }

            private void UpdateModuleDecouplerBase(SimModuleDecouple decoupler, ModuleDecouplerBase? kspModuleDecouple)
            {
                if (kspModuleDecouple is null)
                {
                    Log("Internal FuelFlowSimluation Error: SimModuleDecouple mapped to wrong type");
                    return;
                }

                decoupler.IsDecoupled     = kspModuleDecouple.isDecoupled;
                decoupler.IsOmniDecoupler = kspModuleDecouple.isOmniDecoupler;
                decoupler.Staged          = kspModuleDecouple.staged;

                Part kspPart = kspModuleDecouple.part;

                AttachNode attach;
                if (HighLogic.LoadedSceneIsEditor)
                {
                    attach = kspModuleDecouple.explosiveNodeID != "srf"
                        ? kspPart.FindAttachNode(kspModuleDecouple.explosiveNodeID)
                        : kspPart.srfAttachNode;
                }
                else
                {
                    attach = kspModuleDecouple.ExplosiveNode;
                }

                if (!(attach.attachedPart is null))
                    decoupler.AttachedPart = _partMapping[attach.attachedPart];
            }

            private void UpdateModuleRCS(SimModuleRCS rcs, ModuleRCS? kspModuleRCS)
            {
                if (kspModuleRCS is null)
                {
                    Log("Internal FuelFlowSimluation Error: SimModuleRCS mapped to wrong type");
                    return;
                }

                rcs.Isp        = kspModuleRCS.atmosphereCurve.Evaluate(0) * kspModuleRCS.ispMult;
                rcs.G          = kspModuleRCS.G;
                rcs.Thrust     = kspModuleRCS.flowMult * kspModuleRCS.maxFuelFlow * rcs.Isp * rcs.G;
                rcs.RcsEnabled = kspModuleRCS.rcsEnabled;

                rcs.Propellants.Clear();

                foreach (Propellant p in kspModuleRCS.propellants)
                    rcs.Propellants.Add(new SimPropellant(p.id, p.ignoreForIsp, p.ratio, (SimFlowMode)p.GetFlowMode(),
                        PartResourceLibrary.Instance.GetDefinition(p.id).density));
            }

            private void UpdateProceduralFairingDecoupler(SimProceduralFairingDecoupler decoupler, PartModule kspPartModule)
            {
                decoupler.IsDecoupled = kspPartModule.Fields["decoupled"].GetValue<bool>(kspPartModule);
            }

            private static double GetModuleMass(Part kspPart, float defaultMass, ModifierStagingSituation sit)
            {
                double mass = 0;

                for (int i = 0; i < kspPart.Modules.Count; i++)
                    if (kspPart.Modules[i] is IPartMassModifier m)
                        mass += m.GetModuleMass(defaultMass, sit);

                return mass;
            }
        }
    }
}
