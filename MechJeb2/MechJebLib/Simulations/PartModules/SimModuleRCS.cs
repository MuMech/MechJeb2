#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Statics;

namespace MechJebLib.Simulations.PartModules
{
    public class SimModuleRCS : SimPartModule
    {
        private static readonly ObjectPool<SimModuleRCS>     _pool                = new ObjectPool<SimModuleRCS>(New, Clear);
        public readonly         List<SimPropellant>          Propellants          = new List<SimPropellant>();
        public readonly         Dictionary<int, SimFlowMode> PropellantFlowModes  = new Dictionary<int, SimFlowMode>();
        public readonly         Dictionary<int, double>      ResourceConsumptions = new Dictionary<int, double>();

        public readonly H1 AtmosphereCurve = H1.Get(true);

        public  double G;
        public  double Isp;
        public  double Thrust;
        public  bool   RcsEnabled;
        private bool   _savedRcsEnabled;
        public  double ISPMult;
        public  double ThrustPercentage;
        public  double MaxFuelFlow;
        public  double MassFlowRate;

        private double _atmPressure => Part.Vessel.ATMPressure;

        public override void Dispose() => _pool.Release(this);

        public static SimModuleRCS Borrow(SimPart part)
        {
            SimModuleRCS clamp = _pool.Borrow();
            clamp.Part = part;
            return clamp;
        }

        private static SimModuleRCS New() => new SimModuleRCS();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateRCSStatus()
        {
            if (CanDrawResources())
                return;

            RcsEnabled = false;
        }

        public void SaveStatus() => _savedRcsEnabled = RcsEnabled;

        public void ResetStatus() => RcsEnabled = _savedRcsEnabled;

        // FIXME: aggressively duplicates ModuleEngines code and should be moved to SimPart
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanDrawResources()
        {
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

        // FIXME: aggressively duplicates ModuleEngines code and should be moved to SimPart
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PartHasResource(SimPart part, int resourceId)
        {
            if (part.TryGetResource(resourceId, out SimResource resource))
                return resource.Amount > part.ResourceRequestRemainingThreshold;
            return false;
        }

        // FIXME: aggressively duplicates ModuleEngines code and should be moved to SimPart
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PartsHaveResource(IReadOnlyList<SimPart> parts, int resourceId)
        {
            for (int i = 0; i < parts.Count; i++)
                if (PartHasResource(parts[i], resourceId))
                    return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            Isp = AtmosphereCurve.Evaluate(_atmPressure);
            double exhaustVel = Isp * G * ISPMult;
            MassFlowRate = MaxFuelFlow * (ThrustPercentage * 0.01);
            Thrust       = exhaustVel * MassFlowRate;
            SetConsumptionRates();
        }

        public void Activate() => RcsEnabled = true;

        private static void Clear(SimModuleRCS m)
        {
            m.Propellants.Clear();
            m.PropellantFlowModes.Clear();
            m.ResourceConsumptions.Clear();
            m.AtmosphereCurve.Clear();
        }

        // FIXME: direct copypasta from SimModuleEngines
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
