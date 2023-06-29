using System;
using System.Collections.Generic;
using System.Text;
using MechJebLib.Primitives;
using MechJebLib.Simulations.PartModules;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Simulations
{
    public class SimVessel : IDisposable
    {
        private static readonly ObjectPool<SimVessel> _pool = new ObjectPool<SimVessel>(New, Clear);

        public readonly List<SimPart>                      Parts                   = new List<SimPart>(30);
        public readonly DictOfLists<int, SimPart>          PartsDroppedInStage     = new DictOfLists<int, SimPart>(10);
        public readonly DictOfLists<int, SimModuleEngines> EnginesDroppedInStage   = new DictOfLists<int, SimModuleEngines>(10);
        public readonly DictOfLists<int, SimModuleEngines> EnginesActivatedInStage = new DictOfLists<int, SimModuleEngines>(10);
        public readonly DictOfLists<int, SimModuleRCS> RCSActivatedInStage     = new DictOfLists<int, SimModuleRCS>(10);
        public readonly List<SimModuleEngines>             ActiveEngines           = new List<SimModuleEngines>(10);

        public int    CurrentStage; // FIXME: restorable
        public double MainThrottle = 1.0;
        public double Mass;
        public V3     ThrustCurrent;
        public double ThrustMagnitude;
        public double ThrustNoCosLoss;
        public double SpoolupCurrent;
        public double ATMPressure;
        public double ATMDensity;
        public double MachNumber;

        public void SetConditions(double atmDensity, double atmPressure, double machNumber)
        {
            ATMDensity  = atmDensity;
            ATMPressure = atmPressure;
            MachNumber  = machNumber;
        }

        public void UpdateMass()
        {
            Mass = 0;
            for (int i = -1; i < CurrentStage; i++)
            {
                foreach (SimPart part in PartsDroppedInStage[i])
                {
                    part.UpdateMass();
                    Mass += part.Mass;
                }
            }
        }

        public void Stage()
        {
            if (CurrentStage < 0)
                return;

            CurrentStage--;

            Log($"{EnginesActivatedInStage[CurrentStage].Count} engines activated in stage");

            foreach (SimModuleEngines e in EnginesActivatedInStage[CurrentStage])
                if (e.IsEnabled)
                    e.Activate();

            foreach (SimModuleRCS r in RCSActivatedInStage[CurrentStage])
                if (r.IsEnabled)
                    r.Activate();

            UpdateMass();
        }

        public void UpdateActiveEngines()
        {
            ActiveEngines.Clear();

            for (int i = -1; i < CurrentStage; i++)
            {
                Log($"found {EnginesDroppedInStage[i].Count} engines in stage");
                foreach (SimModuleEngines e in EnginesDroppedInStage[i])
                {
                    if (e.MassFlowRate <= 0) continue;

                    Log("mass flow was not zero");

                    e.UpdateFlameout();

                    if (e.IsOperational)
                    {
                        Log("engine is operational");
                        ActiveEngines.Add(e);
                    }
                }
            }

            Log($"found {ActiveEngines.Count} active engines");

            ComputeThrustAndSpoolup();
        }

        private void ComputeThrustAndSpoolup()
        {
            ThrustCurrent   = V3.zero;
            ThrustMagnitude = 0;
            ThrustNoCosLoss = 0;

            for (int i = 0; i < ActiveEngines.Count; i++)
            {
                SimModuleEngines e = ActiveEngines[i];
                if (!e.IsOperational) continue;

                SpoolupCurrent += e.ThrustCurrent.magnitude * e.ModuleSpoolupTime;

                e.Update();
                ThrustCurrent   += e.ThrustCurrent;
                ThrustNoCosLoss += e.ThrustCurrent.magnitude;
            }

            ThrustMagnitude =  ThrustCurrent.magnitude;
            SpoolupCurrent  /= ThrustCurrent.magnitude;
        }

        public double ResourceMaxTime()
        {
            double maxTime = double.MaxValue;

            for (int i = 0; i < Parts.Count; i++)
                maxTime = Math.Min(Parts[i].ResourceMaxTime(), maxTime);

            return maxTime;
        }

        public void Dispose()
        {
            foreach (SimPart p in Parts)
                p.Dispose();
            _pool.Release(this);
        }

        public static SimVessel Borrow()
        {
            return _pool.Borrow();
        }

        private static SimVessel New()
        {
            return new SimVessel();
        }

        private static void Clear(SimVessel v)
        {
            v.Parts.Clear();
            v.PartsDroppedInStage.Clear();
            v.EnginesActivatedInStage.Clear();
            v.RCSActivatedInStage.Clear();
            v.ActiveEngines.Clear();
        }

        public void UpdateEngineStats()
        {
            for (int i = -1; i < CurrentStage; i++)
                foreach (SimModuleEngines e in EnginesDroppedInStage[i])
                    e.Update();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Parts.Count; i++)
                sb.Append(Parts[i]);

            for (int i = -1; i <= CurrentStage; i++)
            {
                sb.Append($"Stage {i}: ");
                foreach (SimPart part in PartsDroppedInStage[i])
                    sb.Append($" {part.Name}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
