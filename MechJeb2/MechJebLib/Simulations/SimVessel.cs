#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MechJebLib.Primitives;
using MechJebLib.Simulations.PartModules;
using MechJebLib.Utils;

namespace MechJebLib.Simulations
{
    public class SimVessel : IDisposable
    {
        private static readonly ObjectPool<SimVessel> _pool = new ObjectPool<SimVessel>(New, Clear);

        public readonly List<SimPart>                      Parts                   = new List<SimPart>(30);
        public readonly DictOfLists<int, SimPart>          PartsRemainingInStage   = new DictOfLists<int, SimPart>(10);
        public readonly DictOfLists<int, SimModuleEngines> EnginesDroppedInStage   = new DictOfLists<int, SimModuleEngines>(10);
        public readonly DictOfLists<int, SimModuleEngines> EnginesActivatedInStage = new DictOfLists<int, SimModuleEngines>(10);
        public readonly DictOfLists<int, SimModuleRCS>     RCSActivatedInStage     = new DictOfLists<int, SimModuleRCS>(10);
        public readonly List<SimModuleEngines>             ActiveEngines           = new List<SimModuleEngines>(10);

        public int    CurrentStage;
        public double MainThrottle = 1.0;
        public double Mass;
        public V3     ThrustCurrent;
        public double ThrustMagnitude;
        public double ThrustNoCosLoss;
        public double SpoolupCurrent;
        public double ATMPressure;
        public double ATMDensity;
        public double MachNumber;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetConditions(double atmDensity, double atmPressure, double machNumber)
        {
            ATMDensity  = atmDensity;
            ATMPressure = atmPressure;
            MachNumber  = machNumber;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateMass()
        {
            Mass = 0;

            foreach (SimPart part in PartsRemainingInStage[CurrentStage])
            {
                part.UpdateMass();
                Mass += part.Mass;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stage()
        {
            if (CurrentStage < 0)
                return;

            CurrentStage--;

            ActivateEngines();

            UpdateMass();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ActivateEngines()
        {
            foreach (SimModuleEngines e in EnginesActivatedInStage[CurrentStage])
                if (e.IsEnabled)
                    e.Activate();

            foreach (SimModuleRCS r in RCSActivatedInStage[CurrentStage])
                if (r.IsEnabled)
                    r.Activate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateActiveEngines()
        {
            ActiveEngines.Clear();

            for (int i = -1; i < CurrentStage; i++)
            {
                foreach (SimModuleEngines e in EnginesDroppedInStage[i])
                {
                    if (e.MassFlowRate <= 0) continue;

                    e.UpdateEngineStatus();

                    if (!e.IsOperational)
                        continue;

                    ActiveEngines.Add(e);
                }
            }

            ComputeThrustAndSpoolup();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeThrustAndSpoolup()
        {
            ThrustCurrent   = V3.zero;
            ThrustMagnitude = 0;
            ThrustNoCosLoss = 0;
            SpoolupCurrent  = 0;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            foreach (SimPart p in Parts)
                p.Dispose();
            _pool.Release(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SimVessel Borrow()
        {
            return _pool.Borrow();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SimVessel New()
        {
            return new SimVessel();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Clear(SimVessel v)
        {
            v.Parts.Clear();
            v.PartsRemainingInStage.Clear();
            v.EnginesDroppedInStage.Clear();
            v.EnginesActivatedInStage.Clear();
            v.RCSActivatedInStage.Clear();
            v.ActiveEngines.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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


            sb.Append("Parts: ");
            foreach (SimPart part in PartsRemainingInStage[CurrentStage])
                sb.Append($" {part.Name}");
            sb.AppendLine();


            return sb.ToString();
        }
    }
}
