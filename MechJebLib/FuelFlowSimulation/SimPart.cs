/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Text;
using MechJebLib.Utils;
using static System.Math;
using static System.FormattableString;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.FuelFlowSimulation
{
    public class SimPart
    {
        private static readonly ObjectPool<SimPart> _pool = new ObjectPool<SimPart>(New, Clear);

        public readonly List<SimPartModule> Modules = new List<SimPartModule>();
        public readonly List<SimPart> CrossFeedPartSet = new List<SimPart>();
        public readonly List<SimPart> SymmetryCounterParts = new List<SimPart>();
        public readonly List<SimPart> Links = new List<SimPart>();
        public readonly Dictionary<int, SimResource> Resources = new Dictionary<int, SimResource>();
        private readonly Dictionary<int, double> _resourceDrains = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _rcsDrains = new Dictionary<int, double>();

        public int DecoupledInStage = -1;
        public bool StagingOn = true;
        public int InverseStage = -1;
        public SimVessel Vessel;
        public string Name;
        public uint PersistentId;

        public string Ident => Invariant($"{Name}-{PersistentId}");

        public bool ActivatesEvenIfDisconnected = true;
        public bool IsThrottleLocked = false;
        public int ResourcePriority = 0;
        public double ResourceRequestRemainingThreshold = 1E-12;
        public bool IsEnabled = false;

        public double Mass;
        public double DryMass;
        public double CrewMass = 0;
        public double ModulesStagedMass = 0;
        public double ModulesUnstagedMass = 0;
        public double DisabledResourcesMass = 0;
        public double EngineResiduals = 0;

        public bool IsRoot = false;
        public bool IsLaunchClamp = false;
        public bool IsEngine = false;
        public bool IsSepratron => IsEngine && IsThrottleLocked && ActivatesEvenIfDisconnected && InverseStage == DecoupledInStage;

        private SimPart()
        {
            // Always set in Borrow()
            Vessel = null!;
            Name = null!;
        }

        public void UpdateMass()
        {
            if (IsLaunchClamp)
            {
                Mass = 0;
                return;
            }

            Mass = DryMass + CrewMass + DisabledResourcesMass;
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
            part.Name = name;
            return part;
        }

        private static SimPart New() => new SimPart();

        private static void Clear(SimPart p)
        {
            p.Modules.Clear();
            p.Links.Clear();
            p.CrossFeedPartSet.Clear();
            p.SymmetryCounterParts.Clear();
            p.Resources.Clear();
            p._resourceDrains.Clear();
            p._rcsDrains.Clear();

            p.Vessel = null!;
            p.IsLaunchClamp = false;
            p.IsEngine = false;
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

            resource.Residual = Max(resource.Residual, residual);
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
                Resources[id] = resource;
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
            sb.AppendLine(Invariant($"SimPart '{Ident}':"));

            // only emit fields that differ from their declared default, so the dump focuses on what a fixture needs to set
            var fields = new List<string>();

            void B(string name, bool val, bool def)
            {
                if (val != def) fields.Add(Invariant($"{name}={val}"));
            }

            void I(string name, int val, int def)
            {
                if (val != def) fields.Add(Invariant($"{name}={val}"));
            }

            void D(string name, double val, double def)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (val != def) fields.Add(Invariant($"{name}={val}"));
            }

            I("InverseStage", InverseStage, -1);
            I("DecoupledInStage", DecoupledInStage, -1);
            B("StagingOn", StagingOn, true);
            B("IsRoot", IsRoot, false);
            B("IsEngine", IsEngine, false);
            B("IsLaunchClamp", IsLaunchClamp, false);
            B("IsThrottleLocked", IsThrottleLocked, false);
            B("ActivatesEvenIfDisconnected", ActivatesEvenIfDisconnected, true);
            B("IsEnabled", IsEnabled, false);
            I("ResourcePriority", ResourcePriority, 0);
            D("ResourceRequestRemainingThreshold", ResourceRequestRemainingThreshold, 1E-12);
            D("Mass", Mass, 0);
            D("DryMass", DryMass, 0);
            D("CrewMass", CrewMass, 0);
            D("ModulesStagedMass", ModulesStagedMass, 0);
            D("ModulesUnstagedMass", ModulesUnstagedMass, 0);
            D("DisabledResourcesMass", DisabledResourcesMass, 0);
            D("EngineResiduals", EngineResiduals, 0);

            if (fields.Count > 0)
                sb.AppendLine("  " + string.Join(" ", fields));

            AppendParts(sb, "Links", Links);
            AppendParts(sb, "SymmetryCounterParts", SymmetryCounterParts);
            AppendParts(sb, "CrossFeedPartSet", CrossFeedPartSet);

            if (Resources.Count > 0)
            {
                sb.Append("  Resources:");
                foreach (SimResource r in Resources.Values)
                    sb.Append(Invariant(
                        $" [id={r.Id} amount={r.Amount} maxAmount={r.MaxAmount} density={r.Density} free={r.Free} residual={r.Residual}]"));
                sb.AppendLine();
            }

            if (Modules.Count > 0)
            {
                sb.AppendLine(Invariant($"  Modules ({Modules.Count}):"));
                foreach (SimPartModule m in Modules)
                    sb.AppendLine(m.ToString().Indent(4));
            }

            return sb.ToString().TrimEnd();
        }

        private static void AppendParts(StringBuilder sb, string name, List<SimPart> parts)
        {
            if (parts.Count == 0)
                return;

            sb.Append(Invariant($"  {name}:"));
            foreach (SimPart p in parts)
                sb.Append(Invariant($" {p.Ident}"));
            sb.AppendLine();
        }
    }
}
