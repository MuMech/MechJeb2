using System;
using System.Collections.Generic;
using MechJebLib.Simulations.PartModules;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Simulations
{
    // TODO:
    //   - isn't running in the VAB
    //   - add threading
    //   - debug remaining uses of vacStats and mjPhase vs kspStage issues
    //   - fix stage display to show kspStages again
    //   - wire up cosLoss properly in the GUI
    //   - the tiny-partial-stage FIXME below
    public class FuelFlowSimulation
    {
        private const int MAXSTEPS = 100;

        public readonly  List<FuelStats> Segments = new List<FuelStats>();
        private readonly List<SimPart>   _sources = new List<SimPart>();
        private          FuelStats?      _currentSegment;
        private          double          _time;

        public void Run(SimVessel vessel)
        {
            _time           = 0;
            _currentSegment = null;
            Segments.Clear();
            vessel.MainThrottle = 1.0;

            Log($"starting stage: {vessel.CurrentStage}");

            while (vessel.CurrentStage >= 0)
            {
                vessel.UpdateMass();
                Log($"current stage: {vessel.CurrentStage}");
                SimulateStage(vessel);
                FinishSegment(vessel);
                vessel.Stage();
            }

            Segments.Reverse();
        }

        private void SimulateStage(SimVessel vessel)
        {
            UpdateEngineStats(vessel);
            UpdateActiveEngines(vessel);
            UpdateResourceDrainsAndResiduals(vessel);
            GetNextSegment(vessel);
            double currentThrust = vessel.ThrustMagnitude;

            Log("starting stage");

            for (int steps = MAXSTEPS; steps > 0; steps--)
            {
                if (AllowedToStage(vessel))
                {
                    Log("allowed to stage");
                    return;
                }

                double dt = MinimumTimeStep(vessel);

                // FIXME: if we have constructed a segment which is > 0 dV, but less than 0.02s, and there's a
                // prior > 0dV segment in the same kspStage we should add those together to reduce clutter.
                if (Math.Abs(vessel.ThrustMagnitude - currentThrust) > 1e-12)
                {
                    FinishSegment(vessel);
                    GetNextSegment(vessel);
                    currentThrust = vessel.ThrustMagnitude;
                }

                _time += dt;
                ApplyResourceDrains(vessel, dt);
                vessel.UpdateMass();
                UpdateEngineStats(vessel);
                UpdateActiveEngines(vessel);
                UpdateResourceDrainsAndResiduals(vessel);

                Log($"took timestep of {dt}");
            }

            throw new Exception("FuelFlowSimulation hit max steps of " + MAXSTEPS + " steps");
        }

        private void UpdateEngineStats(SimVessel vessel)
        {
            vessel.UpdateEngineStats();
        }

        private void ApplyResourceDrains(SimVessel vessel, double dt)
        {
            for (int i = 0; i < vessel.Parts.Count; i++)
            {
                SimPart p = vessel.Parts[i];
                p.ApplyResourceDrains(dt);
            }
        }

        private void UpdateResourceDrainsAndResiduals(SimVessel vessel)
        {
            for (int i = 0; i < vessel.Parts.Count; i++)
            {
                SimPart p = vessel.Parts[i];
                p.ClearResourceDrains();
            }

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
                            UpdateResourceDrainsInParts(vessel.Parts, e.ResourceConsumptions[resourceId], resourceId, false);
                            UpdateResourceResidualsInParts(vessel.Parts, e.ModuleResiduals, resourceId);
                            break;
                        case SimFlowMode.STAGE_PRIORITY_FLOW:
                        case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            UpdateResourceDrainsInParts(vessel.Parts, e.ResourceConsumptions[resourceId], resourceId, true);
                            UpdateResourceResidualsInParts(vessel.Parts, e.ModuleResiduals, resourceId);
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

        private void UpdateResourceDrainsInPart(SimPart p, double resourceConsumption, int resourceId)
        {
            p.AddResourceDrain(resourceId, resourceConsumption);
        }

        private void UpdateResourceResidualsInParts(IList<SimPart> parts, double residual, int resourceId)
        {
            foreach (SimPart part in parts)
                part.UpdateResourceResidual(residual, resourceId);
        }

        private double MinimumTimeStep(SimVessel vessel)
        {
            double maxTime = vessel.ResourceMaxTime();

            return maxTime < double.MaxValue && maxTime >= 0 ? maxTime : 0;
        }

        private void UpdateActiveEngines(SimVessel vessel)
        {
            vessel.UpdateActiveEngines();
        }

        private void FinishSegment(SimVessel vessel)
        {
            Log("FinishSegment() called");
            if (_currentSegment is null)
                return;

            _currentSegment.DeltaTime = _time - _currentSegment.StartTime;
            _currentSegment.EndMass   = vessel.Mass;

            _currentSegment.ComputeStats();

            Segments.Add(_currentSegment);
        }

        private void GetNextSegment(SimVessel vessel)
        {
            Log("GetNextSegment() called");
            double stagedMass = 0;
            if (!(_currentSegment is null))
                stagedMass = _currentSegment.EndMass - vessel.Mass;

            _currentSegment = new FuelStats
            {
                KSPStage        = vessel.CurrentStage,
                Thrust          = vessel.ThrustMagnitude,
                ThrustNoCosLoss = vessel.ThrustNoCosLoss,
                StartTime       = _time,
                StartMass       = vessel.Mass,
                SpoolUpTime     = vessel.SpoolupCurrent,
                StagedMass      = stagedMass
            };
        }

        private bool AllowedToStage(SimVessel vessel)
        {
            // always stage if all the engines are burned out
            if (vessel.ActiveEngines.Count == 0)
            {
                Log("staging because engines are burned out");
                return true;
            }

            for (int i = 0; i < vessel.ActiveEngines.Count; i++)
            {
                SimModuleEngines e = vessel.ActiveEngines[i];

                if (e.Part.IsSepratron)
                    continue;

                Log($"found an active engine DecoupledInStage {e.Part.DecoupledInStage}");

                // never stage an active engine
                if (e.Part.DecoupledInStage >= vessel.CurrentStage - 1)
                {
                    Log("not staging because of active engines");
                    return false;
                }

                // never drop fuel that could be used
                if (e.WouldDropAccessibleFuelTank(vessel.CurrentStage - 1))
                {
                    Log("not staging because would drop fuel tank");
                    return false;
                }
            }

            // do not trigger a stage that doesn't decouple anything until the engines burn out
            if (vessel.PartsDroppedInStage.Count(vessel.CurrentStage - 1) == 0)
            {
                Log("not staging because no parts would drop");
                return false;
            }

            // we always stage at this point, except for the stage zero which we burn down until the engines go out
            if (vessel.CurrentStage > 0)
                Log("staging because we have parts to drop that aren't engines or fuel");

            return vessel.CurrentStage > 0;
        }
    }
}
