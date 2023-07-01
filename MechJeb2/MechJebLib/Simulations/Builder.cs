#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using KSP.UI;
using KSP.UI.Screens;
using MechJebLib.Simulations.PartModules;
using MuMech;
using UnityEngine;

namespace MechJebLib.Simulations
{
    public class Builder
    {
        private readonly Dictionary<Part, SimPart> _partMapping = new Dictionary<Part, SimPart>();

        private delegate double CrewMass(ProtoCrewMember crew);

        private static readonly CrewMass _crewMassDelegate;

        static Builder()
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

        public SimVessel Build(IShipconstruct kspVessel)
        {
            var vessel = SimVessel.Borrow();

            vessel.CurrentStage = StageManager.CurrentStage + 1;

            BuildParts(kspVessel, vessel);

            UpdateLinks(kspVessel);

            UpdateCrossFeedSet(kspVessel);

            DecouplingAnalyzer.Analyze(vessel);

            UpdateEngineSet(vessel);

            return vessel;
        }

        private void UpdateEngineSet(SimVessel vessel)
        {
            foreach (SimPart part in vessel.Parts)
            {
                foreach (SimPartModule m in part.Modules)
                {
                    if (m is SimModuleEngines e)
                        vessel.EnginesDroppedInStage[part.DecoupledInStage].Add(e);
                }
            }
        }

        private void UpdateLinks(IShipconstruct kspVessel)
        {
            foreach (Part kspPart in kspVessel.Parts)
            {
                SimPart p = _partMapping[kspPart];
                if (!(kspPart.parent is null))
                    p.Links.Add(_partMapping[kspPart.parent]);

                foreach (Part child in kspPart.children)
                    p.Links.Add(_partMapping[child]);
            }
        }

        private void UpdateCrossFeedSet(IShipconstruct kspVessel)
        {
            foreach (Part kspPart in kspVessel.Parts)
            {
                SimPart p = _partMapping[kspPart];

                foreach (Part pset in kspPart.crossfeedPartSet.GetParts())
                {
                    p.CrossFeedPartSet.Add(_partMapping[pset]);
                }
            }
        }

        private void BuildParts(IShipconstruct kspVessel, SimVessel vessel)
        {
            foreach (Part kspPart in kspVessel.Parts)
            {
                SimPart newPart = BuildPart(vessel, kspPart);

                vessel.Parts.Add(newPart);
                _partMapping.Add(kspPart, newPart);
            }
        }

        private SimPart BuildPart(SimVessel vessel, Part kspPart)
        {
            var part = SimPart.Borrow(vessel, kspPart.partName);

            part.DecoupledInStage                  = int.MinValue;
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

            BuildModules(vessel, part, kspPart);

            BuildResources(part, kspPart);

            return part;
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

        private void BuildResources(SimPart part, Part kspPart)
        {
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

        private void BuildModules(SimVessel vessel, SimPart part, Part kspPart)
        {
            foreach (PartModule kspModule in kspPart.Modules)
            {
                if (TryBuildModule(vessel, part, kspModule, out SimPartModule? m))
                    part.Modules.Add(m!);
            }
        }

        // FIXME: needs [NotNullWhen(returnValue: true)]
        private bool TryBuildModule(SimVessel vessel, SimPart part, PartModule kspModule, out SimPartModule? m)
        {
            m = kspModule switch
            {
                ModuleEngines kspEngine                      => BuildModuleEngines(vessel, part, kspEngine),
                LaunchClamp _                                => BuildLaunchClamp(part),
                ModuleAnchoredDecoupler kspAnchoredDecoupler => BuildModuleAnchoredDecoupler(part, kspAnchoredDecoupler),
                ModuleDecouple kspModuleDecouple             => BuildModuleDecouple(part, kspModuleDecouple),
                ModuleDockingNode kspModuleDockingNode       => BuildDockingNode(part, kspModuleDockingNode),
                ModuleRCS kspModuleRCS                       => BuildModuleRCS(vessel, part, kspModuleRCS),
                _                                            => null
            } ?? kspModule.moduleName switch
            {
                "ProceduralFairingDecoupler" => BuildProceduralFairingDecoupler(part, kspModule),
                _                            => null
            };

            if (m == null)
                return false;

            m.ModuleIsEnabled = kspModule.moduleIsEnabled;
            m.StagingEnabled  = kspModule.stagingEnabled;

            return true;
        }

        private SimPartModule BuildDockingNode(SimPart part, ModuleDockingNode kspModuleDockingNode)
        {
            var decoupler = SimModuleDockingNode.Borrow(part);
            decoupler.Staged = kspModuleDockingNode.staged;

            if (!(kspModuleDockingNode.referenceNode.attachedPart is null))
                decoupler.AttachedPart = _partMapping[kspModuleDockingNode.referenceNode.attachedPart];
            return decoupler;
        }

        private SimPartModule BuildModuleDecouple(SimPart part, ModuleDecouple kspModuleDecouple)
        {
            var decoupler = SimModuleDecouple.Borrow(part);
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

            return decoupler;
        }

        private SimPartModule BuildModuleAnchoredDecoupler(SimPart part, ModuleAnchoredDecoupler kspAnchoredDecoupler)
        {
            var decoupler = SimModuleAnchoredDecoupler.Borrow(part);
            decoupler.IsDecoupled = kspAnchoredDecoupler.isDecoupled;
            decoupler.Staged      = kspAnchoredDecoupler.staged;

            Part kspPart = kspAnchoredDecoupler.part;

            AttachNode attach;
            if (HighLogic.LoadedSceneIsEditor)
            {
                attach = kspAnchoredDecoupler.explosiveNodeID != "srf"
                    ? kspPart.FindAttachNode(kspAnchoredDecoupler.explosiveNodeID)
                    : kspPart.srfAttachNode;
            }
            else
            {
                attach = kspAnchoredDecoupler.ExplosiveNode;
            }

            if (!(attach.attachedPart is null))
                decoupler.AttachedPart = _partMapping[attach.attachedPart];

            return decoupler;
        }

        private SimLaunchClamp BuildLaunchClamp(SimPart part)
        {
            var clamp = SimLaunchClamp.Borrow(part);
            part.IsLaunchClamp = true;
            return clamp;
        }

        private SimModuleEngines BuildModuleEngines(SimVessel vessel, SimPart part, ModuleEngines kspEngine)
        {
            var engine = SimModuleEngines.Borrow(part);
            engine.IsEnabled = kspEngine.isEnabled;
            vessel.EnginesActivatedInStage[kspEngine.part.inverseStage].Add(engine);

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

            engine.NoPropellants = kspEngine.flameout && kspEngine.statusL2 == "No propellants";

            foreach (double multiplier in kspEngine.thrustTransformMultipliers)
                engine.ThrustTransformMultipliers.Add(multiplier);

            foreach (Transform transform in kspEngine.thrustTransforms)
                // thrust transforms point at the flamey end and we want to point at the pointy end
                engine.ThrustDirectionVectors.Add(MathExtensions.WorldToV3Rotated(-transform.forward));

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

            part.IsEngine         = true;
            part.IsThrottleLocked = kspEngine.throttleLocked;

            return engine;
        }

        private SimPartModule BuildModuleRCS(SimVessel vessel, SimPart part, ModuleRCS kspModuleRCS)
        {
            var rcs = SimModuleRCS.Borrow(part);
            rcs.Isp        = kspModuleRCS.atmosphereCurve.Evaluate(0) * kspModuleRCS.ispMult;
            rcs.G          = kspModuleRCS.G;
            rcs.Thrust     = kspModuleRCS.flowMult * kspModuleRCS.maxFuelFlow * rcs.Isp * rcs.G;
            rcs.rcsEnabled = kspModuleRCS.rcsEnabled;

            foreach (Propellant p in kspModuleRCS.propellants)
                rcs.Propellants.Add(new SimPropellant(p.id, p.ignoreForIsp, p.ratio, (SimFlowMode)p.GetFlowMode(),
                    PartResourceLibrary.Instance.GetDefinition(p.id).density));

            vessel.RCSActivatedInStage[kspModuleRCS.part.inverseStage].Add(rcs);

            return rcs;
        }

        private SimProceduralFairingDecoupler BuildProceduralFairingDecoupler(SimPart part, PartModule kspPartModule)
        {
            var decoupler = SimProceduralFairingDecoupler.Borrow(part);
            decoupler.IsDecoupled = kspPartModule.Fields["decoupled"].GetValue<bool>(kspPartModule);
            return decoupler;
        }

        private static double GetModuleMass(Part kspPart, float defaultMass, ModifierStagingSituation sit)
        {
            double mass = 0;

            for (int i = 0; i < kspPart.Modules.Count; i++)
                if (kspPart.Modules[i] is IPartMassModifier m)
                    mass += m.GetModuleMass(defaultMass, sit);

            return mass;
        }

        private static readonly FieldInfo? _rfPredictedMaximumResiduals;
        private static readonly FieldInfo? _rfSpoolUpTime;
    }
}
