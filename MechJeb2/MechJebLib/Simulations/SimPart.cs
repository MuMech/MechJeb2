#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Simulations
{
    public class SimPart
    {
        private static readonly ObjectPool<SimPart> _pool = new ObjectPool<SimPart>(New, Clear);

        public readonly  List<SimPartModule>          Modules          = new List<SimPartModule>();
        public readonly  List<SimPart>                CrossFeedPartSet = new List<SimPart>();
        public readonly  List<SimPart>                Links            = new List<SimPart>();
        public readonly  Dictionary<int, SimResource> Resources        = new Dictionary<int, SimResource>();
        private readonly Dictionary<int, double>      _resourceDrains  = new Dictionary<int, double>();

        public int       DecoupledInStage;
        public bool      StagingOn;
        public int       InverseStage;
        public SimVessel Vessel;
        public string    Name;

        public bool   ActivatesEvenIfDisconnected;
        public bool   IsThrottleLocked;
        public int    ResourcePriority;
        public double ResourceRequestRemainingThreshold;
        public bool   IsEnabled;

        public double Mass;
        public double DryMass;
        public double CrewMass;
        public double ModulesStagedMass;
        public double ModulesUnstagedMass;
        public double EngineResiduals;

        public bool IsLaunchClamp;
        public bool IsEngine;
        public bool IsSepratron => IsEngine && IsThrottleLocked && ActivatesEvenIfDisconnected && InverseStage == DecoupledInStage;

        private SimPart()
        {
            // Always set in Borrow()
            Vessel = null!;
            Name   = null!;
        }

        public void UpdateMass()
        {
            if (IsLaunchClamp)
            {
                Mass = 0;
                return;
            }

            Mass =  DryMass + CrewMass;
            Mass += Vessel.CurrentStage <= InverseStage ? ModulesStagedMass : ModulesUnstagedMass;
            //ModulesCurrentMass =  Mass;
            foreach (SimResource resource in Resources.Values)
                Mass += resource.Amount * resource.Density;
        }

        public void Dispose()
        {
            foreach (SimPartModule m in Modules)
                m.Dispose();
            _pool.Release(this);
        }

        public static SimPart Borrow(SimVessel vessel, string name)
        {
            SimPart part = _pool.Borrow();
            part.Vessel = vessel;
            part.Name   = name;
            return part;
        }

        private static SimPart New()
        {
            return new SimPart();
        }

        private static void Clear(SimPart p)
        {
            p.Modules.Clear();
            p.Links.Clear();
            p.CrossFeedPartSet.Clear();
            p.Resources.Clear();
            p._resourceDrains.Clear();
            p.Vessel           = null!;
            p.IsLaunchClamp    = false;
            p.IsEngine         = false;
            p.IsThrottleLocked = false;
        }

        public bool TryGetResource(int resourceId, out SimResource resource)
        {
            return Resources.TryGetValue(resourceId, out resource);
        }

        public void ApplyResourceDrains(double dt)
        {
            foreach (int id in _resourceDrains.Keys)
                Resources[id] = Resources[id].Drain(dt * _resourceDrains[id]);
        }

        public void UpdateResourceResidual(double residual, int resourceId)
        {
            if (!Resources.TryGetValue(resourceId, out SimResource resource))
                return;

            resource.Residual     = Math.Max(resource.Residual, residual);
            Resources[resourceId] = resource;
        }

        public double ResidualThreshold(int resourceId)
        {
            return Resources[resourceId].ResidualThreshold + ResourceRequestRemainingThreshold;
        }

        public void ClearResourceDrains()
        {
            _resourceDrains.Clear();
        }

        public void AddResourceDrain(int resourceId, double resourceConsumption)
        {
            if (_resourceDrains.TryGetValue(resourceId, out double resourceDrain))
                _resourceDrains[resourceId] = resourceDrain + resourceConsumption;
            else
                _resourceDrains.Add(resourceId, resourceConsumption);
        }

        public double ResourceMaxTime()
        {
            double maxTime = double.MaxValue;

            foreach (SimResource resource in Resources.Values)
            {
                if (resource.Free)
                    continue;

                if (resource.Amount <= ResourceRequestRemainingThreshold)
                    continue;

                if (!_resourceDrains.TryGetValue(resource.Id, out double resourceDrain))
                    continue;

                Log($"    id: {resource.Id} drain: {resourceDrain} excess: {resource.Amount - resource.ResidualThreshold}");

                double dt = (resource.Amount - resource.ResidualThreshold) / resourceDrain;

                maxTime = Math.Min(maxTime, dt);
            }

            return maxTime;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Name}: ");
            sb.Append("  Neighbors:");
            for (int i = 0; i < Links.Count; i++)
                sb.Append($" {Links[i].Name}");
            sb.AppendLine();
            //sb.Append("  CrossFeedPartSet:");
            //for (int i = 0; i < CrossFeedPartSet.Count; i++)
            //    sb.Append($" {CrossFeedPartSet[i].Name}");
            //sb.AppendLine();
            sb.Append("  Resources:");
            foreach (SimResource resource in Resources.Values)
                sb.Append($" {resource.Id}={resource.Amount}*{resource.Density}");
            sb.AppendLine();
            sb.Append($"  DecoupledInStage: {DecoupledInStage} InverseStage: {InverseStage}");
            sb.AppendLine();


            return sb.ToString();
        }
    }
}
