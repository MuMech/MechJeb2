#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using KSP.UI;
using KSP.UI.Screens;
using MechJebLib.Simulations.PartModules;
using MuMech;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Simulations
{
    public partial class SimVesselManager
    {
        public class SimVesselBuilder
        {
            private Dictionary<Part, SimPart>             _partMapping              => _manager._partMapping;
            private Dictionary<SimPart, Part>             _inversePartMapping       => _manager._inversePartMapping;
            private Dictionary<SimPartModule, PartModule> _inversePartModuleMapping => _manager._inversePartModuleMapping;

            private static readonly FieldInfo? _rfSpoolUpTime;

            private delegate double CrewMass(ProtoCrewMember crew);

            private static readonly CrewMass _crewMassDelegate;

            private SimVessel _vessel
            {
                get => _manager._vessel;
                set => _manager._vessel = value;
            }

            private IShipconstruct _kspVessel
            {
                get => _manager._kspVessel;
                set => _manager._kspVessel = value;
            }

            private readonly        SimVesselManager _manager;
            private static readonly Type?            _rfType;

            static SimVesselBuilder()
            {
                _crewMassDelegate = Versioning.version_major == 1 && Versioning.version_minor < 11 ? (CrewMass)CrewMassOld : CrewMassNew;

                if (ReflectionUtils.isAssemblyLoaded("RealFuels"))
                {
                    _rfSpoolUpTime = ReflectionUtils.getFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "effectiveSpoolUpTime");
                    if (_rfSpoolUpTime == null)
                        Debug.Log(
                            "MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no effectiveSpoolUpTime field, disabling spoolup");

                    _rfType = Type.GetType("RealFuels.ModuleEnginesRF, RealFuels");
                }
            }

            private static double CrewMassOld(ProtoCrewMember crew) => PhysicsGlobals.KerbalCrewMass;

            private static double CrewMassNew(ProtoCrewMember crew) => PhysicsGlobals.KerbalCrewMass + crew.ResourceMass() + crew.InventoryMass();

            public SimVesselBuilder(SimVesselManager manager)
            {
                _manager = manager;
            }

            internal void UpdateEngineSet()
            {
                foreach (SimPart part in _vessel.Parts)
                {
                    foreach (SimPartModule m in part.Modules)
                    {
                        if (m is SimModuleEngines e)
                            _vessel.EnginesDroppedInStage[part.DecoupledInStage].Add(e);
                        if (m is SimModuleRCS r)
                            _vessel.RCSDroppedInStage[part.DecoupledInStage].Add(r);
                    }
                }
            }

            internal void UpdateLinks()
            {
                foreach (Part kspPart in _kspVessel.Parts)
                {
                    SimPart p = _partMapping[kspPart];
                    if (!(kspPart.parent is null))
                        p.Links.Add(_partMapping[kspPart.parent]);

                    foreach (Part child in kspPart.children)
                        p.Links.Add(_partMapping[child]);
                }
            }

            internal void UpdateCrossFeedSet()
            {
                foreach (Part kspPart in _kspVessel.Parts)
                {
                    SimPart p = _partMapping[kspPart];

                    foreach (Part pset in kspPart.crossfeedPartSet.GetParts())
                    {
                        p.CrossFeedPartSet.Add(_partMapping[pset]);
                    }
                }
            }

            internal void BuildVessel(IShipconstruct kspVessel)
            {
                _vessel.Dispose();
                _vessel    = SimVessel.Borrow();
                _kspVessel = kspVessel;
            }

            internal void BuildParts()
            {
                _vessel.CurrentStage = StageManager.CurrentStage;

                foreach (Part kspPart in _kspVessel.Parts)
                {
                    SimPart part = BuildPart(kspPart);

                    _vessel.Parts.Add(part);
                    _partMapping.Add(kspPart, part);
                    _inversePartMapping.Add(part, kspPart);
                }
            }

            private SimPart BuildPart(Part kspPart)
            {
                var part = SimPart.Borrow(_vessel, kspPart.partName);

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
                part.DecoupledInStage                  = int.MinValue;

                HandleCrewMass(part, kspPart);

                BuildModules(part, kspPart);

                return part;
            }

            private void BuildModules(SimPart part, Part kspPart)
            {
                foreach (PartModule kspModule in kspPart.Modules)
                {
                    if (!TryBuildModule(part, kspModule, out SimPartModule? m))
                        continue;

                    part.Modules.Add(m!);
                    _inversePartModuleMapping.Add(m!, kspModule);
                }
            }

            // FIXME: needs [NotNullWhen(returnValue: true)]
            private bool TryBuildModule(SimPart part, PartModule kspModule, out SimPartModule? m)
            {
                m = kspModule switch
                {
                    ModuleEngines kspEngine               => BuildModuleEngines(part, kspEngine),
                    LaunchClamp _                         => BuildLaunchClamp(part),
                    ModuleDecouplerBase kspModuleDecouple => BuildModuleDecouple(part, kspModuleDecouple),
                    ModuleDockingNode _                   => BuildDockingNode(part),
                    ModuleRCS kspModuleRCS                => BuildModuleRCS(part, kspModuleRCS),
                    _                                     => null
                } ?? kspModule.moduleName switch
                {
                    "ProceduralFairingDecoupler" => BuildProceduralFairingDecoupler(part),
                    _                            => null
                };

                return m != null;
            }

            private SimPartModule BuildDockingNode(SimPart part)
            {
                var decoupler = SimModuleDockingNode.Borrow(part);

                return decoupler;
            }

            private SimPartModule BuildModuleDecouple(SimPart part, ModuleDecouplerBase kspModuleDecouple)
            {
                var decoupler = SimModuleDecouple.Borrow(part);

                decoupler.IsOmniDecoupler = kspModuleDecouple.isOmniDecoupler;

                return decoupler;
            }

            private SimLaunchClamp BuildLaunchClamp(SimPart part)
            {
                part.IsLaunchClamp = true;
                return SimLaunchClamp.Borrow(part);
            }

            private SimModuleEngines BuildModuleEngines(SimPart part, ModuleEngines kspEngine)
            {
                var engine = SimModuleEngines.Borrow(part);

                engine.ThrottleLocked       = kspEngine.throttleLocked;
                engine.MaxFuelFlow          = kspEngine.maxFuelFlow;
                engine.MinFuelFlow          = kspEngine.minFuelFlow;
                engine.FlowMultiplier       = kspEngine.flowMultiplier;
                engine.G                    = kspEngine.g;
                engine.MaxThrust            = kspEngine.maxThrust;
                engine.MinThrust            = kspEngine.minThrust;
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

                _vessel.EnginesActivatedInStage[kspEngine.part.inverseStage].Add(engine);

                part.IsThrottleLocked = kspEngine.throttleLocked;
                part.IsEngine         = true;

                engine.ModuleSpoolupTime = 0;
                engine.isModuleEnginesRF = false;

                if (ReflectionUtils.isAssemblyLoaded("RealFuels"))
                {
                    engine.isModuleEnginesRF = _rfType!.IsInstanceOfType(kspEngine);

                    if (engine.isModuleEnginesRF && _rfSpoolUpTime!.GetValue(kspEngine) is float floatVal)
                        engine.ModuleSpoolupTime = floatVal;
                }

                return engine;
            }

            private SimPartModule BuildModuleRCS(SimPart part, ModuleRCS kspModuleRCS)
            {
                var rcs = SimModuleRCS.Borrow(part);

                rcs.G                = kspModuleRCS.G;
                rcs.ISPMult          = kspModuleRCS.ispMult;
                rcs.ThrustPercentage = kspModuleRCS.thrustPercentage;
                rcs.MaxFuelFlow      = kspModuleRCS.maxFuelFlow;

                rcs.AtmosphereCurve.LoadH1(kspModuleRCS.atmosphereCurve);

                rcs.Propellants.Clear();

                foreach (Propellant p in kspModuleRCS.propellants)
                    rcs.Propellants.Add(new SimPropellant(p.id, p.ignoreForIsp, p.ratio, (SimFlowMode)p.GetFlowMode(),
                        PartResourceLibrary.Instance.GetDefinition(p.id).density));

                _vessel.RCSActivatedInStage[kspModuleRCS.part.inverseStage].Add(rcs);

                return rcs;
            }

            private SimProceduralFairingDecoupler BuildProceduralFairingDecoupler(SimPart part) => SimProceduralFairingDecoupler.Borrow(part);

            private static double GetModuleMass(Part kspPart, float defaultMass, ModifierStagingSituation sit)
            {
                double mass = 0;

                for (int i = 0; i < kspPart.Modules.Count; i++)
                    if (kspPart.Modules[i] is IPartMassModifier m)
                        mass += m.GetModuleMass(defaultMass, sit);

                return mass;
            }

            private void HandleCrewMass(SimPart part, Part kspPart)
            {
                part.CrewMass = 0;

                if (HighLogic.LoadedSceneIsFlight && kspPart.protoModuleCrew != null)
                {
                    for (int i = 0; i < kspPart.protoModuleCrew.Count; i++)
                    {
                        ProtoCrewMember crewMember = kspPart.protoModuleCrew[i];
                        part.CrewMass += _crewMassDelegate(crewMember);
                    }
                }
                else if (HighLogic.LoadedSceneIsEditor)
                {
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
            }
        }
    }
}
