#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MechJebLib.Simulations.PartModules;

namespace MechJebLib.Simulations
{
    // TODO:
    //   - add threading
    //   - throttle running in VAB
    public class FuelFlowSimulation
    {
        private const int MAXSTEPS = 100;

        public readonly  List<FuelStats>  Segments = new List<FuelStats>();
        private          FuelStats        _currentSegment;
        private          double           _time;
        public           bool             DVLinearThrust           = true; // include cos losses
        private readonly HashSet<SimPart> _partsWithResourceDrains = new HashSet<SimPart>();
        private          bool             _allocatedFirstSegment;

        public void Run(SimVessel vessel)
        {
            _allocatedFirstSegment = false;
            _time                  = 0;
            Segments.Clear();
            vessel.MainThrottle = 1.0;

            vessel.ActivateEngines();

            while (vessel.CurrentStage >= 0) // FIXME: should stop mutating vessel.CurrentStage
            {
                SimulateStage(vessel);
                FinishSegment(vessel);
                vessel.Stage();
            }

            Segments.Reverse();

            _partsWithResourceDrains.Clear();
        }

        private void SimulateStage(SimVessel vessel)
        {
            vessel.UpdateMass();
            UpdateEngineStats(vessel);
            UpdateActiveEngines(vessel);
            UpdateResourceDrainsAndResiduals(vessel);

            GetNextSegment(vessel);
            double currentThrust = vessel.ThrustMagnitude;

            for (int steps = MAXSTEPS; steps > 0; steps--)
            {
                if (AllowedToStage(vessel))
                    return;

                double dt = MinimumTimeStep();

                // FIXME: if we have constructed a segment which is > 0 dV, but less than 0.02s, and there's a
                // prior > 0dV segment in the same kspStage we should add those together to reduce clutter.
                if (Math.Abs(vessel.ThrustMagnitude - currentThrust) > 1e-12)
                {
                    FinishSegment(vessel);
                    GetNextSegment(vessel);
                    currentThrust = vessel.ThrustMagnitude;
                }

                _time += dt;
                ApplyResourceDrains(dt);

                vessel.UpdateMass();
                UpdateEngineStats(vessel);
                UpdateActiveEngines(vessel);
                UpdateResourceDrainsAndResiduals(vessel);
            }

            throw new Exception("FuelFlowSimulation hit max steps of " + MAXSTEPS + " steps");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateEngineStats(SimVessel vessel)
        {
            vessel.UpdateEngineStats();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyResourceDrains(double dt)
        {
            foreach (SimPart part in _partsWithResourceDrains)
                part.ApplyResourceDrains(dt);
        }

        private void UpdateResourceDrainsAndResiduals(SimVessel vessel)
        {
            foreach (SimPart part in _partsWithResourceDrains)
                part.ClearResourceDrains();

            _partsWithResourceDrains.Clear();

            for (int i = 0; i < vessel.ActiveEngines.Count; i++)
            {
                SimModuleEngines e = vessel.ActiveEngines[i];
                foreach (int resourceId in e.PropellantFlowModes.Keys)
                    switch (e.PropellantFlowModes[resourceId])
                    {
                        case SimFlowMode.NO_FLOW:
                            UpdateResourceDrainsInPart(e.Part, e.ResourceConsumptions[resourceId], resourceId);
                            e.Part.UpdateResourceResidual(e.ModuleResiduals, resourceId);
                            break;
                        case SimFlowMode.ALL_VESSEL:
                        case SimFlowMode.ALL_VESSEL_BALANCE:
                            UpdateResourceDrainsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage], e.ResourceConsumptions[resourceId],
                                resourceId, false);
                            UpdateResourceResidualsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage], e.ModuleResiduals, resourceId);
                            break;
                        case SimFlowMode.STAGE_PRIORITY_FLOW:
                        case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            UpdateResourceDrainsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage], e.ResourceConsumptions[resourceId],
                                resourceId, true);
                            UpdateResourceResidualsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage], e.ModuleResiduals, resourceId);
                            break;
                        case SimFlowMode.STAGE_STACK_FLOW:
                        case SimFlowMode.STAGE_STACK_FLOW_BALANCE:
                        case SimFlowMode.STACK_PRIORITY_SEARCH:
                            UpdateResourceDrainsInParts(e.Part.CrossFeedPartSet, e.ResourceConsumptions[resourceId], resourceId, true);
                            UpdateResourceResidualsInParts(e.Part.CrossFeedPartSet, e.ModuleResiduals, resourceId);
                            break;
                        case SimFlowMode.NULL:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
            }
        }

        private readonly List<SimPart> _sources = new List<SimPart>();

        private void UpdateResourceDrainsInParts(IList<SimPart> parts, double resourceConsumption, int resourceId, bool usePriority)
        {
            int maxPriority = int.MinValue;

            _sources.Clear();

            for (int i = 0; i < parts.Count; i++)
            {
                SimPart p = parts[i];

                if (!p.TryGetResource(resourceId, out SimResource resource))
                    continue;

                if (resource.Free)
                    continue;

                if (resource.Amount <= p.Resources[resourceId].ResidualThreshold)
                    continue;

                if (usePriority)
                {
                    if (p.ResourcePriority < maxPriority)
                        continue;

                    if (p.ResourcePriority > maxPriority)
                    {
                        _sources.Clear();
                        maxPriority = p.ResourcePriority;
                    }
                }

                _sources.Add(p);
            }

            for (int i = 0; i < _sources.Count; i++)
                UpdateResourceDrainsInPart(_sources[i], resourceConsumption / _sources.Count, resourceId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateResourceDrainsInPart(SimPart p, double resourceConsumption, int resourceId)
        {
            _partsWithResourceDrains.Add(p);
            p.AddResourceDrain(resourceId, resourceConsumption);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateResourceResidualsInParts(IList<SimPart> parts, double residual, int resourceId)
        {
            for (int i = 0; i < parts.Count; i++)
                parts[i].UpdateResourceResidual(residual, resourceId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double MinimumTimeStep()
        {
            double maxTime = ResourceMaxTime();

            return maxTime < double.MaxValue && maxTime >= 0 ? maxTime : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ResourceMaxTime()
        {
            double maxTime = double.MaxValue;

            foreach (SimPart part in _partsWithResourceDrains)
                maxTime = Math.Min(part.ResourceMaxTime(), maxTime);

            return maxTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateActiveEngines(SimVessel vessel)
        {
            vessel.UpdateActiveEngines();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishSegment(SimVessel vessel)
        {
            if (!_allocatedFirstSegment)
                return;

            _currentSegment.DeltaTime = _time - _currentSegment.StartTime;
            _currentSegment.EndMass   = vessel.Mass;

            _currentSegment.ComputeStats();

            Segments.Add(_currentSegment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetNextSegment(SimVessel vessel)
        {
            double stagedMass = 0;
            if (_allocatedFirstSegment)
                stagedMass = _currentSegment.EndMass - vessel.Mass;
            else
                _allocatedFirstSegment = true;

            _currentSegment = new FuelStats
            {
                KSPStage    = vessel.CurrentStage,
                Thrust      = DVLinearThrust ? vessel.ThrustMagnitude : vessel.ThrustNoCosLoss,
                StartTime   = _time,
                StartMass   = vessel.Mass,
                SpoolUpTime = vessel.SpoolupCurrent,
                StagedMass  = stagedMass
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllowedToStage(SimVessel vessel)
        {
            // always stage if all the engines are burned out
            if (vessel.ActiveEngines.Count == 0)
                return true;

            for (int i = 0; i < vessel.ActiveEngines.Count; i++)
            {
                SimModuleEngines e = vessel.ActiveEngines[i];

                if (e.Part.IsSepratron)
                    continue;

                // never stage an active engine
                if (e.Part.DecoupledInStage >= vessel.CurrentStage - 1)
                    return false;

                // never drop fuel that could be used
                if (e.WouldDropAccessibleFuelTank(vessel.CurrentStage - 1))
                    return false;
            }

            // do not trigger a stage that doesn't decouple anything -- until the engines burn out
            if (vessel.PartsRemainingInStage[vessel.CurrentStage - 1].Count == vessel.PartsRemainingInStage[vessel.CurrentStage].Count)
                return false;

            return vessel.CurrentStage > 0;
        }
    }
}
