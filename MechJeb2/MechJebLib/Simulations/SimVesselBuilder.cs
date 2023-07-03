#nullable enable

using System.Collections.Generic;
using KSP.UI.Screens;
using MechJebLib.Simulations.PartModules;

namespace MechJebLib.Simulations
{
    public partial class SimVesselManager
    {
        public class SimVesselBuilder
        {
            private Dictionary<Part, SimPart>             _partMapping              => _manager._partMapping;
            private Dictionary<SimPart, Part>             _inversePartMapping       => _manager._inversePartMapping;
            private Dictionary<SimPartModule, PartModule> _inversePartModuleMapping => _manager._inversePartModuleMapping;

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

            private readonly SimVesselManager _manager;

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

                part.DecoupledInStage = int.MinValue;

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
                    ModuleEngines kspEngine => BuildModuleEngines(part, kspEngine),
                    LaunchClamp _           => BuildLaunchClamp(part),
                    ModuleDecouplerBase _   => BuildModuleDecouple(part),
                    ModuleDockingNode _     => BuildDockingNode(part),
                    ModuleRCS kspModuleRCS  => BuildModuleRCS(part, kspModuleRCS),
                    _                       => null
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

            private SimPartModule BuildModuleDecouple(SimPart part)
            {
                var decoupler = SimModuleDecouple.Borrow(part);

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

                _vessel.EnginesActivatedInStage[kspEngine.part.inverseStage].Add(engine);

                part.IsEngine = true;

                return engine;
            }

            private SimPartModule BuildModuleRCS(SimPart part, ModuleRCS kspModuleRCS)
            {
                var rcs = SimModuleRCS.Borrow(part);

                _vessel.RCSActivatedInStage[kspModuleRCS.part.inverseStage].Add(rcs);

                return rcs;
            }

            private SimProceduralFairingDecoupler BuildProceduralFairingDecoupler(SimPart part)
            {
                return SimProceduralFairingDecoupler.Borrow(part);
            }
        }
    }
}
