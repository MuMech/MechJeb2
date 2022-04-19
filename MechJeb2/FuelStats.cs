using System;

namespace MuMech
{
   public partial class FuelFlowSimulation
    {
        //A FuelStats struct describes the result of the simulation over a certain interval of time (e.g., one stage)
        public struct FuelStats
        {
            public double StartMass;
            public double EndMass;
            public double StartThrust;
            public double EndThrust;
            public double MaxAccel;
            public double DeltaTime;
            public double DeltaV;
            public double SpoolUpTime;

            public double ResourceMass;
            public double Isp;
            public double StagedMass;
            public double MaxThrust;

            public double StartTWR(double geeASL)
            {
                return StartMass > 0 ? StartThrust / (9.80665 * geeASL * StartMass) : 0;
            }

            public double MaxTWR(double geeASL)
            {
                return MaxAccel / (9.80665 * geeASL);
            }

            //Computes the deltaV from the other fields. Only valid when the thrust is constant over the time interval represented.
            public void ComputeTimeStepDeltaV()
            {
                if (DeltaTime > 0 && StartMass > EndMass && StartMass > 0 && EndMass > 0)
                    DeltaV = StartThrust * DeltaTime / (StartMass - EndMass) * Math.Log(StartMass / EndMass);
                else
                    DeltaV = 0;
            }

            //Append joins two FuelStats describing adjacent intervals of time into one describing the combined interval
            public FuelStats Append(FuelStats s)
            {
                double sDeltaTime = s.DeltaTime < float.MaxValue && !double.IsInfinity(s.DeltaTime) ? s.DeltaTime : 0;
                double sumDT = sDeltaTime + DeltaTime;

                return new FuelStats
                {
                    // we integrate to the time-averaged maxthrust to accomodate weirdness like ullage motors that burnout and tiny segments
                    // that turn off the engines for some reason.  this is so PVG has a number which is closest to reality.
                    MaxThrust    = sumDT > 1e-10 ? (MaxThrust * DeltaTime + s.MaxThrust * sDeltaTime ) / ( DeltaTime + sDeltaTime) : MaxThrust,
                    StartMass    = StartMass,
                    EndMass      = s.EndMass,
                    ResourceMass = StartMass - s.EndMass,
                    StartThrust  = StartThrust,
                    EndThrust    = s.EndThrust,
                    SpoolUpTime  = Math.Max(SpoolUpTime, s.SpoolUpTime),
                    MaxAccel     = Math.Max(MaxAccel, s.MaxAccel),
                    DeltaTime    = sumDT,
                    DeltaV       = DeltaV + s.DeltaV,
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Isp          = StartMass == s.EndMass ? 0 : (DeltaV + s.DeltaV) / (9.80665f * Math.Log(StartMass / s.EndMass))
                };
            }
        }
    }
}
