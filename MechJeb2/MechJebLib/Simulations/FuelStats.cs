using System;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Simulations
{
    public struct FuelStats
    {
        public double DeltaTime;
        public double DeltaV;
        public double EndMass;
        public double Isp;
        public int    KSPStage;
        public double StagedMass;
        public double StartMass;
        public double StartTime;
        public double Thrust;
        public double SpoolUpTime;
        public double MaxAccel     => EndMass > 0 ? Thrust / EndMass : 0;
        public double ResourceMass => StartMass - EndMass;

        public double StartTWR(double geeASL)
        {
            return StartMass > 0 ? Thrust / (9.80665 * geeASL * StartMass) : 0;
        }

        public double MaxTWR(double geeASL)
        {
            return MaxAccel / (9.80665 * geeASL);
        }

        public void ComputeStats()
        {
            DeltaV = StartMass > EndMass ? Thrust * DeltaTime / (StartMass - EndMass) * Math.Log(StartMass / EndMass) : 0;
            Isp    = StartMass > EndMass ? DeltaV / (G0 * Math.Log(StartMass / EndMass)) : 0;
        }

        public override string ToString()
        {
            return
                $"KSP Stage: {KSPStage.ToString()} Thrust: {Thrust.ToString()} Time: {DeltaTime.ToString()} StartMass: {StartMass.ToString()} EndMass: {EndMass.ToString()} DeltaV: {DeltaV.ToString()} ISP: {Isp.ToString()}";
        }
    }
}
