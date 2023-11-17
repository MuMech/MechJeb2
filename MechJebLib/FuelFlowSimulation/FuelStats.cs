/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

namespace MechJebLib.FuelFlowSimulation
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
        public double MaxRcsDeltaV;
        public double MinRcsDeltaV;
        public double RcsISP;
        public double RcsDeltaTime;
        public double RcsThrust;
        public double RcsMass;
        public double RcsStartTMR;
        public double RcsEndTMR;

        public double MaxAccel     => EndMass > 0 ? Thrust / EndMass : 0;
        public double ResourceMass => StartMass - EndMass;

        public double RcsStartTWR(double geeASL) => RcsStartTMR / (9.80665 * geeASL);
        public double RcsMaxTWR(double geeASL)   => RcsEndTMR / (9.80665 * geeASL);

        public double StartTWR(double geeASL) => StartMass > 0 ? Thrust / (9.80665 * geeASL * StartMass) : 0;

        public double MaxTWR(double geeASL) => MaxAccel / (9.80665 * geeASL);

        public override string ToString() =>
            $"KSP Stage: {KSPStage.ToString()} Thrust: {Thrust.ToString()} Time: {DeltaTime.ToString()} StartMass: {StartMass.ToString()} EndMass: {EndMass.ToString()} DeltaV: {DeltaV.ToString()} ISP: {Isp.ToString()}";
    }
}
