using System;
using System.Collections.Generic;
using MechJebLib.Simulations.PartModules;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Simulations
{
    // TODO:
    //   - residuals
    //   - spoolup time
    //   - the NO_FLOW ablator fix
    //   - the srb residual fuel issue
    //   - isSepratron check in AllowedToStage()
    //   - crew mass
    //   - cosLoss
    //   - "free" resources like air intake
    //   - git rid of the annoying extra stage
    //   - use objectpooling
    //   - use vessel modification callbacks
    public class FuelFlowSimulation
    {
        private const int MAXSTEPS = 100;

        public readonly List<FuelStats> Segments = new List<FuelStats>();

        private readonly SimVessel     _vessel;
        private readonly List<SimPart> _sources = new List<SimPart>();
        private          FuelStats?    _currentSegment;
        private          double        _time;

        public FuelFlowSimulation(SimVessel vessel)
        {
            _vessel = vessel;
        }

        public void Run()
        {
            _time           = 0;
            _currentSegment = null;
            Segments.Clear();
            _vessel.MainThrottle = 1.0;

            Log($"starting stage: {_vessel.CurrentStage}");

            while (_vessel.CurrentStage >= 0)
            {
                Log($"current stage: {_vessel.CurrentStage}");
                SimulateStage();
                FinishSegment();
                _vessel.Stage();
            }
        }

        private void SimulateStage()
        {
            UpdateEngineStats();
            UpdateActiveEngines();
            UpdateResourceDrainsAndResiduals();
            GetNextSegment();
            double currentThrust = _vessel.ThrustCurrent.magnitude;

            Log("starting stage");

            for (int steps = MAXSTEPS; steps > 0; steps--)
            {
                if (AllowedToStage())
                {
                    Log("allowed to stage");
                    return;
                }

                double dt = MinimumTimeStep();

                if (Math.Abs(_vessel.ThrustCurrent.magnitude - currentThrust) / currentThrust > 1e-4 && dt > 0.1)
                {
                    FinishSegment();
                    GetNextSegment();
                    currentThrust = _vessel.ThrustCurrent.magnitude;
                }

                _time += dt;
                ApplyResourceDrains(dt);
                UpdateEngineStats();
                UpdateActiveEngines();
                UpdateResourceDrainsAndResiduals();

                Log($"took timestep of {dt}");
            }

            throw new Exception("FuelFlowSimulation hit max steps of " + MAXSTEPS + " steps");
        }

        private void UpdateEngineStats()
        {
            _vessel.UpdateEngineStats();
        }

        private void ApplyResourceDrains(double dt)
        {
            for (int i = 0; i < _vessel.Parts.Count; i++)
            {
                SimPart p = _vessel.Parts[i];
                p.ApplyResourceDrains(dt);
            }

            _vessel.UpdateMass();
        }

        private void UpdateResourceDrainsAndResiduals()
        {
            for (int i = 0; i < _vessel.Parts.Count; i++)
            {
                SimPart p = _vessel.Parts[i];
                p.ClearResourceDrains();
            }

            for (int i = 0; i < _vessel.ActiveEngines.Count; i++)
            {
                SimModuleEngines e = _vessel.ActiveEngines[i];
                foreach (int resourceId in e.PropellantFlowModes.Keys)
                    switch (e.PropellantFlowModes[resourceId])
                    {
                        case SimFlowMode.NO_FLOW:
                            UpdateResourceDrainsInPart(e.Part, e.ResourceConsumptions[resourceId], resourceId);
                            e.Part.UpdateResourceResidual(e.ModuleResiduals, resourceId);
                            break;
                        case SimFlowMode.ALL_VESSEL:
                        case SimFlowMode.ALL_VESSEL_BALANCE:
                            UpdateResourceDrainsInParts(_vessel.Parts, e.ResourceConsumptions[resourceId], resourceId, false);
                            UpdateResourceResidualsInParts(_vessel.Parts, e.ModuleResiduals, resourceId);
                            break;
                        case SimFlowMode.STAGE_PRIORITY_FLOW:
                        case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            UpdateResourceDrainsInParts(_vessel.Parts, e.ResourceConsumptions[resourceId], resourceId, true);
                            UpdateResourceResidualsInParts(_vessel.Parts, e.ModuleResiduals, resourceId);
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
                SimResource? resource = p.GetResource(resourceId);

                if (resource == null)
                    continue;

                if (resource.Free)
                    continue;

                if (resource.Amount < p.ResourceRequestRemainingThreshold) continue;

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

        private double MinimumTimeStep()
        {
            double maxTime = _vessel.ResourceMaxTime();

            return maxTime < double.MaxValue ? maxTime : 0;
        }

        private void UpdateActiveEngines()
        {
            _vessel.UpdateActiveEngines();
        }

        private void FinishSegment()
        {
            Log("FinishSegment() called");
            if (_currentSegment is null)
                return;

            _currentSegment.DeltaTime = _time - _currentSegment.StartTime;
            _currentSegment.EndMass   = _vessel.Mass;

            _currentSegment.ComputeStats();

            Segments.Add(_currentSegment);
        }

        private void GetNextSegment()
        {
            Log("GetNextSegment() called");
            if (!(_currentSegment is null))
                _currentSegment.StagedMass = _currentSegment.EndMass - _vessel.Mass;

            _currentSegment = new FuelStats
            {
                KSPStage = _vessel.CurrentStage, Thrust = _vessel.ThrustCurrent.magnitude, StartTime = _time, StartMass = _vessel.Mass
            };
        }

        // FIXME: This needs to check isSepratron, which is not implemented
        private bool AllowedToStage()
        {
            // always stage if all the engines are burned out
            if (_vessel.ActiveEngines.Count == 0) return true;

            for (int i = 0; i < _vessel.ActiveEngines.Count; i++)
            {
                SimModuleEngines e = _vessel.ActiveEngines[i];

                // never stage an active engine
                if (e.Part.DecoupledInStage >= _vessel.CurrentStage - 1)
                {
                    return false;
                }

                if (e.WouldDropAccessibleFuelTank(_vessel.CurrentStage - 1))
                {
                    return false;
                }
            }

            // do not trigger a stage that doesn't decouple anything until the engines burn out
            bool dropsParts = false;

            for (int i = 0; i < _vessel.Parts.Count; i++)
            {
                SimPart p = _vessel.Parts[i];
                if (p.DecoupledInStage >= _vessel.CurrentStage - 1)
                    dropsParts = true;
            }

            if (!dropsParts) return false;

            // we always stage at this point, except for the stage zero which we burn down until the engines go out
            return _vessel.CurrentStage > 0;
        }
    }
}
