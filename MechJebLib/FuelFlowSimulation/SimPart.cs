/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Text;
using MechJebLib.Utils;
using static System.Math;

namespace MechJebLib.FuelFlowSimulation
{
    public class SimPart
    {
        private static readonly ObjectPool<SimPart> _pool = new ObjectPool<SimPart>(New, Clear);

        public readonly  List<SimPartModule>          Modules          = new List<SimPartModule>();
        public readonly  List<SimPart>                CrossFeedPartSet = new List<SimPart>();
        public readonly  List<SimPart>                Links            = new List<SimPart>();
        public readonly  Dictionary<int, SimResource> Resources        = new Dictionary<int, SimResource>();
        private readonly Dictionary<int, double>      _resourceDrains  = new Dictionary<int, double>();
        private readonly Dictionary<int, double>      _rcsDrains       = new Dictionary<int, double>();

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
        public double DisabledResourcesMass;
        public double EngineResiduals;

        public bool IsRoot;
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

            Mass =  DryMass + CrewMass + DisabledResourcesMass;
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

        private static SimPart New() => new SimPart();

        private static void Clear(SimPart p)
        {
            p.Modules.Clear();
            p.Links.Clear();
            p.CrossFeedPartSet.Clear();
            p.Resources.Clear();
            p._resourceDrains.Clear();
            p._rcsDrains.Clear();

            p.Vessel           = null!;
            p.IsLaunchClamp    = false;
            p.IsEngine         = false;
            p.IsThrottleLocked = false;
        }

        public bool TryGetResource(int resourceId, out SimResource resource) => Resources.TryGetValue(resourceId, out resource);

        public void ApplyResourceDrains(double dt)
        {
            foreach (int id in _resourceDrains.Keys)
                Resources[id] = Resources[id].Drain(dt * _resourceDrains[id]);
        }

        public void ApplyRCSDrains(double dt)
        {
            foreach (int id in _rcsDrains.Keys)
                Resources[id] = Resources[id].RCSDrain(dt * _rcsDrains[id]);
        }

        private readonly List<int> _resourceKeys = new List<int>();

        public void UnapplyRCSDrains()
        {
            _resourceKeys.Clear();
            foreach (int id in Resources.Keys)
                _resourceKeys.Add(id);

            foreach (int id in _resourceKeys)
                Resources[id] = Resources[id].ResetRCS();
        }

        public void UpdateResourceResidual(double residual, int resourceId)
        {
            if (!Resources.TryGetValue(resourceId, out SimResource resource))
                return;

            resource.Residual     = Max(resource.Residual, residual);
            Resources[resourceId] = resource;
        }

        public void ClearResiduals()
        {
            _resourceKeys.Clear();
            foreach (int id in Resources.Keys)
                _resourceKeys.Add(id);

            foreach (int id in _resourceKeys)
            {
                SimResource resource = Resources[id];
                resource.Residual = 0;
                Resources[id]     = resource;
            }
        }

        public double ResidualThreshold(int resourceId) => Resources[resourceId].ResidualThreshold + ResourceRequestRemainingThreshold;

        public void ClearResourceDrains() => _resourceDrains.Clear();

        public void ClearRCSDrains() => _rcsDrains.Clear();

        public void AddResourceDrain(int resourceId, double resourceConsumption)
        {
            if (_resourceDrains.TryGetValue(resourceId, out double resourceDrain))
                _resourceDrains[resourceId] = resourceDrain + resourceConsumption;
            else
                _resourceDrains.Add(resourceId, resourceConsumption);
        }

        public void AddRCSDrain(int resourceId, double resourceConsumption)
        {
            if (_rcsDrains.TryGetValue(resourceId, out double resourceDrain))
                _rcsDrains[resourceId] = resourceDrain + resourceConsumption;
            else
                _rcsDrains.Add(resourceId, resourceConsumption);
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

                double dt = (resource.Amount - resource.ResidualThreshold) / resourceDrain;

                maxTime = Min(maxTime, dt);
            }

            return maxTime;
        }

        public double RCSMaxTime()
        {
            double maxTime = double.MaxValue;

            foreach (SimResource resource in Resources.Values)
            {
                if (resource.Free)
                    continue;

                if (resource.Amount <= ResourceRequestRemainingThreshold)
                    continue;

                if (!_rcsDrains.TryGetValue(resource.Id, out double resourceDrain))
                    continue;

                double dt = (resource.Amount - resource.ResidualThreshold) / resourceDrain;

                maxTime = Min(maxTime, dt);
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
