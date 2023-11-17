/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.FuelFlowSimulation.PartModules
{
    public enum SimFlowMode
    {
        NO_FLOW,
        ALL_VESSEL,
        STAGE_PRIORITY_FLOW,
        STACK_PRIORITY_SEARCH,
        ALL_VESSEL_BALANCE,
        STAGE_PRIORITY_FLOW_BALANCE,
        STAGE_STACK_FLOW,
        STAGE_STACK_FLOW_BALANCE,
        NULL
    }

    public class SimModuleEngines : SimPartModule
    {
        private static readonly ObjectPool<SimModuleEngines> _pool = new ObjectPool<SimModuleEngines>(New, Clear);

        public readonly List<SimPropellant>          Propellants                = new List<SimPropellant>();
        public readonly Dictionary<int, SimFlowMode> PropellantFlowModes        = new Dictionary<int, SimFlowMode>();
        public readonly Dictionary<int, double>      ResourceConsumptions       = new Dictionary<int, double>();
        public readonly List<double>                 ThrustTransformMultipliers = new List<double>();
        public readonly List<V3>                     ThrustDirectionVectors     = new List<V3>();

        public bool   IsOperational;
        public double FlowMultiplier;
        public V3     ThrustCurrent;
        public V3     ThrustMax;
        public V3     ThrustMin;
        public double MassFlowRate;
        public double ISP;
        public float  G;
        public float  MaxFuelFlow;
        public float  MaxThrust;
        public float  MinFuelFlow;
        public float  MinThrust;
        public float  MultIsp;
        public float  Clamp;
        public float  FlowMultCap;
        public float  FlowMultCapSharpness;
        public bool   ThrottleLocked;
        public float  ThrottleLimiter;
        public bool   AtmChangeFlow;
        public bool   UseAtmCurve;
        public bool   UseAtmCurveIsp;
        public bool   UseThrottleIspCurve;
        public bool   UseThrustCurve;
        public bool   UseVelCurve;
        public bool   UseVelCurveIsp;
        public double ModuleResiduals;
        public double ModuleSpoolupTime;
        public bool   NoPropellants;
        public bool   isModuleEnginesRF;

        public readonly H1 ThrustCurve                 = H1.Get(true);
        public readonly H1 ThrottleIspCurve            = H1.Get(true);
        public readonly H1 ThrottleIspCurveAtmStrength = H1.Get(true);
        public readonly H1 VelCurve                    = H1.Get(true);
        public readonly H1 VelCurveIsp                 = H1.Get(true);
        public readonly H1 ATMCurve                    = H1.Get(true);
        public readonly H1 ATMCurveIsp                 = H1.Get(true);
        public readonly H1 AtmosphereCurve             = H1.Get(true);

        private double _throttle    => Part.Vessel.MainThrottle;
        private double _atmPressure => Part.Vessel.ATMPressure;
        private double _atmDensity  => Part.Vessel.ATMDensity;
        private double _machNumber  => Part.Vessel.MachNumber;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Activate() => IsOperational = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateEngineStatus()
        {
            if (CanDrawResources())
                return;

            IsOperational = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanDrawResources()
        {
            if (NoPropellants)
                return false;

            foreach (int resourceId in ResourceConsumptions.Keys)
                switch (PropellantFlowModes[resourceId])
                {
                    case SimFlowMode.NO_FLOW:
                        if (!PartHasResource(Part, resourceId))
                            return false;
                        break;
                    case SimFlowMode.ALL_VESSEL:
                    case SimFlowMode.ALL_VESSEL_BALANCE:
                    case SimFlowMode.STAGE_PRIORITY_FLOW:
                    case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                        if (!PartsHaveResource(Part.Vessel.Parts, resourceId))
                            return false;
                        break;
                    case SimFlowMode.STAGE_STACK_FLOW:
                    case SimFlowMode.STAGE_STACK_FLOW_BALANCE:
                    case SimFlowMode.STACK_PRIORITY_SEARCH:
                        if (!PartsHaveResource(Part.CrossFeedPartSet, resourceId))
                            return false;
                        break;
                    case SimFlowMode.NULL:
                        return false;
                    default:
                        return false;
                }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PartHasResource(SimPart part, int resourceId)
        {
            if (part.TryGetResource(resourceId, out SimResource resource))
                return resource.Amount > part.Resources[resourceId].MaxAmount * ModuleResiduals + part.ResourceRequestRemainingThreshold;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PartsHaveResource(IReadOnlyList<SimPart> parts, int resourceId)
        {
            for (int i = 0; i < parts.Count; i++)
                if (PartHasResource(parts[i], resourceId))
                    return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Dispose() => _pool.Release(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SimModuleEngines Borrow(SimPart part)
        {
            SimModuleEngines engine = _pool.Borrow();
            engine.Part = part;
            return engine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SimModuleEngines New() => new SimModuleEngines();

        private static void Clear(SimModuleEngines m)
        {
            m.ThrustDirectionVectors.Clear();
            m.PropellantFlowModes.Clear();
            m.ResourceConsumptions.Clear();
            m.Propellants.Clear();
            m.ThrustTransformMultipliers.Clear();
            m.ThrustCurve.Clear();
            m.ThrottleIspCurve.Clear();
            m.ThrottleIspCurveAtmStrength.Clear();
            m.VelCurve.Clear();
            m.VelCurveIsp.Clear();
            m.ATMCurve.Clear();
            m.ATMCurveIsp.Clear();
            m.AtmosphereCurve.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DrawingFuelFromPartDroppedInStage(SimPart p, int resourceId, int stageNum) =>
            PartHasResource(p, resourceId) && p.DecoupledInStage == stageNum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DrawingFuelFromPartsDroppedInStage(IList<SimPart> parts, int resourceId, int stageNum)
        {
            for (int i = 0; i < parts.Count; i++)
                if (PartHasResource(parts[i], resourceId) && parts[i].DecoupledInStage == stageNum)
                    return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WouldDropAccessibleFuelTank(int stageNum)
        {
            foreach (int resourceId in ResourceConsumptions.Keys)
                switch (PropellantFlowModes[resourceId])
                {
                    case SimFlowMode.NO_FLOW:
                        if (DrawingFuelFromPartDroppedInStage(Part, resourceId, stageNum))
                            return true;
                        break;
                    case SimFlowMode.ALL_VESSEL:
                    case SimFlowMode.ALL_VESSEL_BALANCE:
                    case SimFlowMode.STAGE_PRIORITY_FLOW:
                    case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                        if (DrawingFuelFromPartsDroppedInStage(Part.Vessel.Parts, resourceId, stageNum))
                            return true;
                        break;
                    case SimFlowMode.STAGE_STACK_FLOW:
                    case SimFlowMode.STAGE_STACK_FLOW_BALANCE:
                    case SimFlowMode.STACK_PRIORITY_SEARCH:
                        if (DrawingFuelFromPartsDroppedInStage(Part.CrossFeedPartSet, resourceId, stageNum))
                            return true;
                        break;
                }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            ISP            = ISPAtConditions();
            FlowMultiplier = FlowMultiplierAtConditions();
            MassFlowRate   = FlowRateAtConditions();
            RefreshThrust();
            SetConsumptionRates();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double FlowRateAtConditions()
        {
            double minFuelFlow = MinFuelFlow;
            double maxFuelFlow = MaxFuelFlow;

            if (minFuelFlow == 0 && MinThrust > 0) minFuelFlow = MinThrust / (AtmosphereCurve.Evaluate(0f) * G);
            if (maxFuelFlow == 0 && MaxThrust > 0) maxFuelFlow = MaxThrust / (AtmosphereCurve.Evaluate(0f) * G);

            return Lerp(minFuelFlow, maxFuelFlow, _throttle * 0.01f * ThrottleLimiter) * FlowMultiplier;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RefreshThrust()
        {
            ThrustCurrent = V3.zero;
            ThrustMax     = V3.zero;
            ThrustMin     = V3.zero;

            double thrustLimiter = ThrottleLimiter / 100f;

            double maxThrust = MaxFuelFlow * FlowMultiplier * ISP * G * MultIsp;
            double minThrust = MinFuelFlow * FlowMultiplier * ISP * G * MultIsp;

            double eMaxThrust = minThrust + (maxThrust - minThrust) * thrustLimiter;
            double eMinThrust = ThrottleLocked ? eMaxThrust : minThrust;
            double eCurrentThrust = MassFlowRate * ISP * G * MultIsp;

            for (int i = 0; i < ThrustDirectionVectors.Count; i++)
            {
                V3 thrustDirectionVector = ThrustDirectionVectors[i];

                double thrustTransformMultiplier = ThrustTransformMultipliers[i];
                double tCurrentThrust = eCurrentThrust * thrustTransformMultiplier;

                ThrustCurrent += tCurrentThrust * thrustDirectionVector;
                ThrustMax     += eMaxThrust * thrustDirectionVector * thrustTransformMultiplier;
                ThrustMin     += eMinThrust * thrustDirectionVector * thrustTransformMultiplier;
            }
        }

        // for a single EngineModule, determine its flowMultiplier, subject to atmDensity + machNumber
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double FlowMultiplierAtConditions()
        {
            double flowMultiplier = 1;

            if (AtmChangeFlow)
            {
                if (UseAtmCurve)
                    flowMultiplier = ATMCurve.Evaluate(_atmDensity * 40 / 49);
                else
                    flowMultiplier = _atmDensity * 40 / 49;
            }

            if (UseThrustCurve)
                // MJ doesn't support thrust curves.  And Engines where "1.0" values don't give reasonable numbers
                // will not work with MJ.  I don't think this will be much of a problem, but I don't care if it is.
                //flowMultiplier *= ThrustCurve.Evaluate(0.5);
                flowMultiplier *= 1.0;

            if (UseVelCurve)
                flowMultiplier *= VelCurve.Evaluate(_machNumber);

            if (flowMultiplier > FlowMultCap)
            {
                double excess = flowMultiplier - FlowMultCap;
                flowMultiplier = FlowMultCap + excess / (FlowMultCapSharpness + excess / FlowMultCap);
            }

            // some engines have e.Clamp set to float.MaxValue so we have to have the e.Clamp < 1 sanity check here
            if (flowMultiplier < Clamp && Clamp < 1)
                flowMultiplier = Clamp;

            return flowMultiplier;
        }

        // for a single EngineModule, evaluate its ISP, subject to all the different possible curves
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ISPAtConditions()
        {
            double isp = AtmosphereCurve.Evaluate(_atmPressure);
            if (UseThrottleIspCurve)
                isp *= Lerp(1f, ThrottleIspCurve.Evaluate(_throttle), ThrottleIspCurveAtmStrength.Evaluate(_atmPressure));
            if (UseAtmCurveIsp)
                isp *= ATMCurveIsp.Evaluate(_atmDensity * 40 / 49);
            if (UseVelCurveIsp)
                isp *= VelCurveIsp.Evaluate(_machNumber);
            return isp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetConsumptionRates()
        {
            ResourceConsumptions.Clear();
            PropellantFlowModes.Clear();

            double totalDensity = 0;

            for (int j = 0; j < Propellants.Count; j++)
            {
                SimPropellant p = Propellants[j];
                double density = p.density;

                // skip zero density (eC, air intakes, etc) assuming those are available and infinite
                if (density <= 0)
                    continue;

                PropellantFlowModes.TryAdd(p.id, p.FlowMode);

                // ignoreForIsp fuels are not part of the total density
                if (p.ignoreForIsp)
                    continue;

                totalDensity += p.ratio * density;
            }

            // this is also the volume flow rate of the non-ignoreForIsp fuels.  although this is a bit janky since the p.ratios in most
            // stock engines sum up to 2, not 1 (1.1 + 0.9), so this is not per-liter but per-summed-ratios (the massflowrate you get out
            // of the atmosphere curves (above) are also similarly adjusted by these ratios -- it is a bit of a horror show).
            double volumeFlowRate = MassFlowRate / totalDensity;

            for (int j = 0; j < Propellants.Count; j++)
            {
                SimPropellant p = Propellants[j];
                double density = p.density;

                // this is the individual propellant volume rate.  we are including the ignoreForIsp fuels in this loop and this will
                // correctly calculate the volume rates of all the propellants, in L/sec.  if you sum these it'll be larger than the
                // volumeFlowRate by including both the ignoreForIsp fuels and if the ratios sum up to more than one.
                double propVolumeRate = p.ratio * volumeFlowRate;

                // skip zero density here as well
                if (density <= 0)
                    continue;

                if (ResourceConsumptions.ContainsKey(p.id))
                    ResourceConsumptions[p.id] += propVolumeRate;
                else
                    ResourceConsumptions.Add(p.id, propVolumeRate);
            }
        }
    }
}
