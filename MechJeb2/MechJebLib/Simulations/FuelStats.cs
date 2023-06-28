using System;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Simulations
{
    public class FuelStats
    {
        public double DeltaTime;
        public double DeltaV;
        public double EndMass;
        public double ISP;
        public int    KSPStage;
        public double StagedMass;
        public double StartMass;
        public double StartTime;
        public double Thrust;

        public FuelStats()
        {
        }

        public FuelStats(FuelStats from)
        {
            DeltaTime  = from.DeltaTime;
            DeltaV     = from.DeltaV;
            EndMass    = from.EndMass;
            ISP        = from.ISP;
            KSPStage   = from.KSPStage;
            StagedMass = from.StagedMass;
            StartMass  = from.StartMass;
            StartTime  = from.StartTime;
            Thrust     = from.Thrust;
        }

        public void ComputeStats()
        {
            DeltaV = StartMass > EndMass ? Thrust * DeltaTime / (StartMass - EndMass) * Math.Log(StartMass / EndMass) : 0;
            ISP    = StartMass > EndMass ? DeltaV / (G0 * Math.Log(StartMass / EndMass)) : 0;
        }

        public override string ToString()
        {
            return
                $"KSP Stage: {KSPStage.ToString()} Thrust: {Thrust.ToString()} Time: {DeltaTime.ToString()} StartMass: {StartMass.ToString()} EndMass: {EndMass.ToString()} DeltaV: {DeltaV.ToString()} ISP: {ISP.ToString()}";
        }
    }
}
