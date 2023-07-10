#nullable enable

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using KSP.UI.Screens;
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

            private static readonly FieldInfo? _pfDecoupled;

            static SimVesselUpdater()
            {
                if (!ReflectionUtils.isAssemblyLoaded("ProceduralFairings")) return;

                _pfDecoupled =
                    ReflectionUtils.getFieldByReflection("ProceduralFairings", "ProceduralFairings.ProceduralFairingDecoupler", "decoupled");
                if (_pfDecoupled == null)
                {
                    Debug.Log(
                        "MechJeb BUG: ProceduralFairings loaded, but ProceduralFairings.ProceduralFairingDecoupler has no decoupled field");
                }
            }

            public SimVesselUpdater(SimVesselManager manager)
            {
                _manager = manager;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Update()
            {
                UpdateParts();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdateParts()
            {
                _vessel.CurrentStage = StageManager.CurrentStage;

                // FIXME: could track only parts that matter to the sim (engines+tanks) and only loop over them here
                foreach (SimPart part in _vessel.Parts)
                {
                    UpdatePart(part);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdatePart(SimPart part)
            {
                Part kspPart = _inversePartMapping[part];

                part.EngineResiduals = 0;

                UpdateResources(part, kspPart);

                UpdateModules(part);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void UpdateResources(SimPart part, Part kspPart)
            {
                part.Resources.Clear();
                part.DisabledResourcesMass = 0;

                for (int i = 0; i < kspPart.Resources.Count; i++)
                {
                    PartResource kspResource = kspPart.Resources[i];

                    if (!kspResource.flowState)
                    {
                        // disabled resources are dead weight and cannot be enabled by staging
                        part.DisabledResourcesMass += kspResource.amount * kspResource.info.density;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdateModules(SimPart part)
            {
                foreach (SimPartModule module in part.Modules)
                {
                    UpdateModule(part, module);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdateModule(SimPart part, SimPartModule m)
            {
                PartModule kspModule = _inversePartModuleMapping[m];

                m.ModuleIsEnabled = kspModule.moduleIsEnabled;
                m.StagingEnabled  = kspModule.stagingEnabled;

                switch (m)
                {
                    case SimModuleEngines engine:
                        UpdateModuleEngines(engine, kspModule as ModuleEngines);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void UpdateModuleEngines(SimModuleEngines engine, ModuleEngines? kspEngine)
            {
                if (kspEngine is null)
                {
                    Log("Internal FuelFlowSimluation Error: SimModuleEngines mapped to wrong type");
                    return;
                }

                engine.IsEnabled        = kspEngine.isEnabled;
                engine.IsOperational    = kspEngine.isOperational;
                engine.ThrustPercentage = kspEngine.thrustPercentage;
                engine.MultIsp          = kspEngine.multIsp;
                engine.NoPropellants    = kspEngine is { flameout: true, statusL2: "No propellants" };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdateModuleDecouplerBase(SimModuleDecouple decoupler, ModuleDecouplerBase? kspModuleDecouple)
            {
                if (kspModuleDecouple is null)
                {
                    Log("Internal FuelFlowSimluation Error: SimModuleDecouple mapped to wrong type");
                    return;
                }

                decoupler.IsDecoupled = kspModuleDecouple.isDecoupled;
                decoupler.Staged      = kspModuleDecouple.staged;

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void UpdateModuleRCS(SimModuleRCS rcs, ModuleRCS? kspModuleRCS)
            {
                if (kspModuleRCS is null)
                {
                    Log("Internal FuelFlowSimluation Error: SimModuleRCS mapped to wrong type");
                    return;
                }

                rcs.Isp        = kspModuleRCS.atmosphereCurve.Evaluate(0) * kspModuleRCS.ispMult;
                rcs.Thrust     = kspModuleRCS.flowMult * kspModuleRCS.maxFuelFlow * rcs.Isp * rcs.G;
                rcs.RcsEnabled = kspModuleRCS.rcsEnabled;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void UpdateProceduralFairingDecoupler(SimProceduralFairingDecoupler decoupler, PartModule kspPartModule)
            {
                if (_pfDecoupled == null) return;

                object obj;
                if ((obj = _pfDecoupled.GetValue(kspPartModule)) == null)
                    decoupler.IsDecoupled = false;
                else
                    decoupler.IsDecoupled = (bool)obj;
            }
        }
    }
}
