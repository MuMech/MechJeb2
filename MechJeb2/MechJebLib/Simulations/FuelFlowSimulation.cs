#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MechJebLib.Simulations.PartModules;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Simulations
{
    public class FuelFlowSimulation : BackgroundJob<bool>
    {
        private const int MAXSTEPS = 100;

        public readonly  List<FuelStats>  Segments = new List<FuelStats>();
        private          FuelStats        _currentSegment;
        private          double           _time;
        public           bool             DVLinearThrust           = true; // include cos losses
        private readonly HashSet<SimPart> _partsWithResourceDrains = new HashSet<SimPart>();
        private readonly HashSet<SimPart> _partsWithRCSDrains      = new HashSet<SimPart>();
        private readonly HashSet<SimPart> _partsWithRCSDrains2     = new HashSet<SimPart>();
        private          bool             _allocatedFirstSegment;

        protected override bool Run(object? o)
        {
            var vessel = (SimVessel)o!;

            _allocatedFirstSegment = false;
            _time                  = 0;
            Segments.Clear();
            vessel.MainThrottle = 1.0;

            vessel.ActivateEnginesAndRCS();

            while (vessel.CurrentStage >= 0) // FIXME: should stop mutating vessel.CurrentStage
            {
                SimulateStage(vessel);
                ClearResiduals();
                ComputeRcsMaxValues(vessel);
                FinishSegment(vessel);
                vessel.Stage();
            }

            Segments.Reverse();

            _partsWithResourceDrains.Clear();

            return true; // we pull results off the object not off the return value
        }

        private void SimulateRCS(SimVessel vessel, bool max)
        {
            vessel.SaveRcsStatus();
            _partsWithRCSDrains2.Clear();

            double lastmass = vessel.Mass;

            int steps = MAXSTEPS;

            while (true)
            {
                if (steps-- == 0)
                    throw new Exception("FuelFlowSimulation hit max steps of " + MAXSTEPS + " steps in rcs calculations");

                vessel.UpdateRcsStats();
                vessel.UpdateActiveRcs();
                if (vessel.ActiveRcs.Count == 0)
                    break;

                UpdateRcsDrains(vessel);
                double dt = MinimumRcsTimeStep();

                ApplyRcsDrains(dt);
                vessel.UpdateMass();
                FinishRcsSegment(max, dt, lastmass, vessel.Mass, vessel.RcsThrust);
                lastmass = vessel.Mass;
            }

            UnapplyRcsDrains();
            vessel.ResetRcsStatus();
            vessel.UpdateMass();
        }

        private void UnapplyRcsDrains()
        {
            foreach (SimPart p in _partsWithRCSDrains2)
                p.UnapplyRCSDrains();
        }

        private void ComputeRcsMinValues(SimVessel vessel) => SimulateRCS(vessel, false);

        private void ComputeRcsMaxValues(SimVessel vessel) => SimulateRCS(vessel, true);

        private void SimulateStage(SimVessel vessel)
        {
            vessel.UpdateMass();
            vessel.UpdateEngineStats();
            vessel.UpdateActiveEngines();

            GetNextSegment(vessel);
            ComputeRcsMinValues(vessel);

            UpdateResourceDrainsAndResiduals(vessel);
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
                    ClearResiduals();
                    ComputeRcsMaxValues(vessel);
                    FinishSegment(vessel);
                    GetNextSegment(vessel);
                    currentThrust = vessel.ThrustMagnitude;
                }

                _time += dt;
                ApplyResourceDrains(dt);

                vessel.UpdateMass();
                vessel.UpdateEngineStats();
                vessel.UpdateActiveEngines();
                UpdateResourceDrainsAndResiduals(vessel);
            }

            throw new Exception("FuelFlowSimulation hit max steps of " + MAXSTEPS + " steps");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyRcsDrains(double dt)
        {
            foreach (SimPart part in _partsWithRCSDrains)
                part.ApplyRCSDrains(dt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyResourceDrains(double dt)
        {
            foreach (SimPart part in _partsWithResourceDrains)
                part.ApplyResourceDrains(dt);
        }

        private void UpdateRcsDrains(SimVessel vessel)
        {
            foreach (SimPart part in _partsWithRCSDrains)
                part.ClearRCSDrains();

            _partsWithRCSDrains.Clear();

            for (int i = 0; i < vessel.ActiveRcs.Count; i++)
            {
                SimModuleRCS e = vessel.ActiveRcs[i];
                foreach (int resourceId in e.PropellantFlowModes.Keys)
                {
                    switch (e.PropellantFlowModes[resourceId])
                    {
                        case SimFlowMode.NO_FLOW:
                            UpdateRCSDrainsInPart(e.Part, e.ResourceConsumptions[resourceId], resourceId);
                            break;
                        case SimFlowMode.ALL_VESSEL:
                        case SimFlowMode.ALL_VESSEL_BALANCE:
                            UpdateRCSDrainsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage], e.ResourceConsumptions[resourceId],
                                resourceId, false);
                            break;
                        case SimFlowMode.STAGE_PRIORITY_FLOW:
                        case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            UpdateRCSDrainsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage], e.ResourceConsumptions[resourceId],
                                resourceId, true);
                            break;
                        case SimFlowMode.STAGE_STACK_FLOW:
                        case SimFlowMode.STAGE_STACK_FLOW_BALANCE:
                        case SimFlowMode.STACK_PRIORITY_SEARCH:
                            UpdateRCSDrainsInParts(e.Part.CrossFeedPartSet, e.ResourceConsumptions[resourceId], resourceId, true);
                            break;
                        case SimFlowMode.NULL:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private readonly List<SimPart> _sourcesRCS = new List<SimPart>();

        private void UpdateRCSDrainsInParts(IList<SimPart> parts, double resourceConsumption, int resourceId, bool usePriority)
        {
            int maxPriority = int.MinValue;

            _sourcesRCS.Clear();

            for (int i = 0; i < parts.Count; i++)
            {
                SimPart p = parts[i];

                if (!p.TryGetResource(resourceId, out SimResource resource))
                    continue;

                if (resource.Free)
                    continue;

                if (resource.Amount <= p.ResourceRequestRemainingThreshold)
                    continue;

                if (usePriority)
                {
                    if (p.ResourcePriority < maxPriority)
                        continue;

                    if (p.ResourcePriority > maxPriority)
                    {
                        _sourcesRCS.Clear();
                        maxPriority = p.ResourcePriority;
                    }
                }

                _sourcesRCS.Add(p);
            }

            for (int i = 0; i < _sourcesRCS.Count; i++)
                UpdateRCSDrainsInPart(_sourcesRCS[i], resourceConsumption / _sourcesRCS.Count, resourceId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateRCSDrainsInPart(SimPart p, double resourceConsumption, int resourceId)
        {
            _partsWithRCSDrains.Add(p);
            _partsWithRCSDrains2.Add(p);
            p.AddRCSDrain(resourceId, resourceConsumption);
        }

        private void ClearResiduals()
        {
            foreach (SimPart part in _partsWithResourceDrains)
                part.ClearResiduals();
        }

        private void UpdateResourceDrainsAndResiduals(SimVessel vessel)
        {
            foreach (SimPart part in _partsWithResourceDrains)
            {
                part.ClearResourceDrains();
                part.ClearResiduals();
            }

            _partsWithResourceDrains.Clear();

            for (int i = 0; i < vessel.ActiveEngines.Count; i++)
            {
                SimModuleEngines e = vessel.ActiveEngines[i];
                foreach (int resourceId in e.PropellantFlowModes.Keys)
                    switch (e.PropellantFlowModes[resourceId])
                    {
                        case SimFlowMode.NO_FLOW:
                            UpdateResourceDrainsAndResidualsInPart(e.Part, e.ResourceConsumptions[resourceId], resourceId, e.ModuleResiduals);
                            break;
                        case SimFlowMode.ALL_VESSEL:
                        case SimFlowMode.ALL_VESSEL_BALANCE:
                            UpdateResourceDrainsAndResidualsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage],
                                e.ResourceConsumptions[resourceId],
                                resourceId, false, e.ModuleResiduals);
                            break;
                        case SimFlowMode.STAGE_PRIORITY_FLOW:
                        case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                            UpdateResourceDrainsAndResidualsInParts(vessel.PartsRemainingInStage[vessel.CurrentStage],
                                e.ResourceConsumptions[resourceId],
                                resourceId, true, e.ModuleResiduals);
                            break;
                        case SimFlowMode.STAGE_STACK_FLOW:
                        case SimFlowMode.STAGE_STACK_FLOW_BALANCE:
                        case SimFlowMode.STACK_PRIORITY_SEARCH:
                            UpdateResourceDrainsAndResidualsInParts(e.Part.CrossFeedPartSet, e.ResourceConsumptions[resourceId], resourceId, true,
                                e.ModuleResiduals);
                            break;
                        case SimFlowMode.NULL:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
            }
        }

        private readonly List<SimPart> _sources = new List<SimPart>();

        private void UpdateResourceDrainsAndResidualsInParts(IList<SimPart> parts, double resourceConsumption, int resourceId, bool usePriority,
            double residual)
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

                if (resource.Amount <= residual * resource.MaxAmount + p.ResourceRequestRemainingThreshold)
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
                UpdateResourceDrainsAndResidualsInPart(_sources[i], resourceConsumption / _sources.Count, resourceId, residual);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateResourceDrainsAndResidualsInPart(SimPart p, double resourceConsumption, int resourceId, double residual)
        {
            _partsWithResourceDrains.Add(p);
            p.AddResourceDrain(resourceId, resourceConsumption);
            p.UpdateResourceResidual(residual, resourceId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double MinimumRcsTimeStep()
        {
            double maxTime = RCSMaxTime();

            return maxTime < double.MaxValue && maxTime >= 0 ? maxTime : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double RCSMaxTime()
        {
            double maxTime = double.MaxValue;

            foreach (SimPart part in _partsWithRCSDrains)
                maxTime = Math.Min(part.RCSMaxTime(), maxTime);

            return maxTime;
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
        private void FinishRcsSegment(bool max, double deltaTime, double startMass, double endMass, double rcsThrust)
        {
            double rcsDeltaV = rcsThrust * deltaTime / (startMass - endMass) * Math.Log(startMass / endMass);
            double rcsISP = rcsDeltaV / (G0 * Math.Log(startMass / endMass));

            if (_currentSegment.RcsISP == 0)
                _currentSegment.RcsISP = rcsISP;
            if (_currentSegment.RcsThrust == 0)
                _currentSegment.RcsThrust = rcsThrust;

            if (max)
            {
                _currentSegment.MaxRcsDeltaV += rcsDeltaV;
                if (_currentSegment.RcsStartTMR == 0)
                    _currentSegment.RcsStartTMR = rcsThrust / startMass;
            }
            else
            {
                _currentSegment.MinRcsDeltaV += rcsDeltaV;
                _currentSegment.RcsEndTMR    =  _currentSegment.RcsThrust / endMass;
                _currentSegment.RcsMass      += startMass - endMass;
                _currentSegment.RcsDeltaTime += deltaTime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishSegment(SimVessel vessel)
        {
            if (!_allocatedFirstSegment)
                return;

            double startMass = _currentSegment.StartMass;
            double thrust = _currentSegment.Thrust;
            double endMass = vessel.Mass;
            double deltaTime = _time - _currentSegment.StartTime;
            double deltaV = startMass > endMass ? thrust * deltaTime / (startMass - endMass) * Math.Log(startMass / endMass) : 0;
            double isp = startMass > endMass ? deltaV / (G0 * Math.Log(startMass / endMass)) : 0;

            _currentSegment.DeltaTime = deltaTime;
            _currentSegment.EndMass   = endMass;
            _currentSegment.DeltaV    = deltaV;
            _currentSegment.Isp       = isp;

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
