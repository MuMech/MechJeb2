using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Simulations.PartModules
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

        public bool   IsOperational; // FIXME: resettable
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
        public float  ThrustPercentage; // FIXME: resettable
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

        public readonly H1 ThrustCurve                 = H1.Get();
        public readonly H1 ThrottleIspCurve            = H1.Get();
        public readonly H1 ThrottleIspCurveAtmStrength = H1.Get();
        public readonly H1 VelCurve                    = H1.Get();
        public readonly H1 VelCurveIsp                 = H1.Get();
        public readonly H1 ATMCurve                    = H1.Get();
        public readonly H1 ATMCurveIsp                 = H1.Get();
        public readonly H1 AtmosphereCurve             = H1.Get();

        private double _throttle    => Part.Vessel.MainThrottle;
        private double _atmPressure => Part.Vessel.ATMPressure;
        private double _atmDensity  => Part.Vessel.ATMDensity;
        private double _machNumber  => Part.Vessel.MachNumber;

        public void Activate()
        {
            Log("igniting engine");
            IsOperational    = true;
            ThrustPercentage = 100f;
        }

        public void UpdateFlameout()
        {
            if (CanDrawResources())
                return;

            Log("engine cannot draw resources");

            IsOperational = false;
        }

        private bool CanDrawResources()
        {
            if (NoPropellants)
            {
                Log("  no propellants");
                return false;
            }

            foreach (int resourceId in ResourceConsumptions.Keys)
                switch (PropellantFlowModes[resourceId])
                {
                    case SimFlowMode.NO_FLOW:
                        if (!PartHasResource(Part, resourceId))
                        {
                            Log($"  cannot draw resources NO_FLOW on {resourceId} ");
                            return false;
                        }
                        break;
                    case SimFlowMode.ALL_VESSEL:
                    case SimFlowMode.ALL_VESSEL_BALANCE:
                    case SimFlowMode.STAGE_PRIORITY_FLOW:
                    case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                        if (!PartsHaveResource(Part.Vessel.Parts, resourceId))
                        {
                            Log($"  cannot draw resources ALL_VESSEL on {resourceId} ");
                            return false;
                        }
                        break;
                    case SimFlowMode.STAGE_STACK_FLOW:
                    case SimFlowMode.STAGE_STACK_FLOW_BALANCE:
                    case SimFlowMode.STACK_PRIORITY_SEARCH:
                        if (!PartsHaveResource(Part.CrossFeedPartSet, resourceId))
                        {
                            Log($"  cannot draw resources STACK_FLOW on {resourceId} ");
                            return false;
                        }
                        break;
                    case SimFlowMode.NULL:
                        return false;
                    default:
                        return false;
                }

            return true;
        }

        private bool PartHasResource(SimPart part, int resourceId)
        {
            SimResource? resource = part.GetResource(resourceId);
            if (resource != null && resource.Amount > part.ResidualThreshold(resourceId))
                Log($"  id: {resource.Id} amount: {resource.Amount} threshold: {part.ResidualThreshold(resourceId)}");
            return resource != null && resource.Amount > part.ResidualThreshold(resourceId);
        }

        private bool PartsHaveResource(IReadOnlyList<SimPart> parts, int resourceId)
        {
            for (int i = 0; i < parts.Count; i++)
                if (PartHasResource(parts[i], resourceId))
                    return true;
            return false;
        }

        public override void Dispose()
        {
            _pool.Release(this);
        }

        public static SimModuleEngines Borrow(SimPart part)
        {
            SimModuleEngines engine = _pool.Borrow();
            engine.Part = part;
            return engine;
        }

        private static SimModuleEngines New()
        {
            return new SimModuleEngines();
        }

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

        private bool DrawingFuelFromPartDroppedInStage(SimPart p, int resourceId, int stageNum)
        {
            return PartHasResource(p, resourceId) && p.DecoupledInStage == stageNum;
        }

        private bool DrawingFuelFromPartsDroppedInStage(IList<SimPart> parts, int resourceId, int stageNum)
        {
            for (int i = 0; i < parts.Count; i++)
                if (PartHasResource(parts[i], resourceId) && parts[i].DecoupledInStage == stageNum)
                    return true;
            return false;
        }

        public bool WouldDropAccessibleFuelTank(int stageNum)
        {
            foreach (int resourceId in ResourceConsumptions.Keys)
                switch (PropellantFlowModes[resourceId])
                {
                    case SimFlowMode.NO_FLOW:
                        if (DrawingFuelFromPartDroppedInStage(Part, resourceId, stageNum))
                        {
                            Log("NO_FLOW returning true");
                            return true;
                        }
                        break;
                    case SimFlowMode.ALL_VESSEL:
                    case SimFlowMode.ALL_VESSEL_BALANCE:
                    case SimFlowMode.STAGE_PRIORITY_FLOW:
                    case SimFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                        if (DrawingFuelFromPartsDroppedInStage(Part.Vessel.Parts, resourceId, stageNum))
                        {
                            Log("ALL_VESSEL returning true");
                            return true;
                        }
                        break;
                    case SimFlowMode.STAGE_STACK_FLOW:
                    case SimFlowMode.STAGE_STACK_FLOW_BALANCE:
                    case SimFlowMode.STACK_PRIORITY_SEARCH:
                        if (DrawingFuelFromPartsDroppedInStage(Part.CrossFeedPartSet, resourceId, stageNum))
                        {
                            Log("STACK_FLOW returning true");
                            return true;
                        }
                        break;
                }

            return false;
        }

        public void Update()
        {
            ISP            = ISPAtConditions();
            FlowMultiplier = FlowMultiplierAtConditions();
            MassFlowRate   = FlowRateAtConditions();
            RefreshThrust();
            SetConsumptionRates();
        }

        private double FlowRateAtConditions()
        {
            double minFuelFlow = MinFuelFlow;
            double maxFuelFlow = MaxFuelFlow;

            if (minFuelFlow == 0 && MinThrust > 0) minFuelFlow = MinThrust / (AtmosphereCurve.Evaluate(0f) * G);
            if (maxFuelFlow == 0 && MaxThrust > 0) maxFuelFlow = MaxThrust / (AtmosphereCurve.Evaluate(0f) * G);

            return Lerp(minFuelFlow, maxFuelFlow, _throttle * 0.01f * ThrustPercentage) * FlowMultiplier;
        }

        private void RefreshThrust()
        {
            ThrustCurrent = V3.zero;
            ThrustMax     = V3.zero;
            ThrustMin     = V3.zero;

            double thrustLimiter = ThrustPercentage / 100f;

            double maxThrust = MaxFuelFlow * FlowMultiplier * ISP * G * MultIsp;
            double minThrust = MinFuelFlow * FlowMultiplier * ISP * G * MultIsp;

            double eMaxThrust = minThrust + (maxThrust - minThrust) * thrustLimiter;
            double eMinThrust = ThrottleLocked ? eMaxThrust : minThrust;
            double eCurrentThrust = MassFlowRate * FlowMultiplier * ISP * G * MultIsp;

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
                flowMultiplier *= ThrustCurve.Evaluate(0.5);

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
                double density = PartResourceLibrary.Instance.GetDefinition(p.id).density;

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
