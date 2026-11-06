/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using Xunit;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class StageSumsTests
    {
        private const int PRECISION = 12;

        // stage 1 burns A then B (an engine flames out during it: no staging, a new segment), then stage 0 burns C. The
        // simulation lists them latest first, as it publishes them
        private static readonly FuelStats _a = new FuelStats
        {
            KSPStage = 1, StartTime = 0, StartMass = 100, EndMass = 80, Thrust = 2000, Isp = 300, DeltaV = 650, DeltaTime = 30,
            StagedMass = 5, ControllableMass = 10,
            // the RCS burn at the stage's full mass is only in its first segment
            MinRcsDeltaV = 40, RcsISP = 240, RcsThrust = 4, RcsMass = 1, RcsDeltaTime = 60, RcsEndTMR = 0.041, RcsUllageTime = 2,
            MaxRcsDeltaV = 45, RcsStartTMR = 0.05
        };

        private static readonly FuelStats _b = new FuelStats
        {
            KSPStage = 1, StartTime = 30, StartMass = 80, EndMass = 60, Thrust = 1000, Isp = 320, DeltaV = 900, DeltaTime = 40,
            StagedMass = 0, ControllableMass = 10, RcsISP = 240, RcsThrust = 4, MaxRcsDeltaV = 60, RcsStartTMR = 0.067
        };

        private static readonly FuelStats _c = new FuelStats
        {
            KSPStage = 0, StartTime = 70, StartMass = 50, EndMass = 30, Thrust = 500, Isp = 340, DeltaV = 1700, DeltaTime = 120,
            StagedMass = 10, ControllableMass = 2, MinRcsDeltaV = 70, RcsISP = 240, RcsThrust = 4, RcsMass = 1, RcsDeltaTime = 40,
            RcsEndTMR = 0.08, MaxRcsDeltaV = 110, RcsStartTMR = 0.13
        };

        private static List<KSPStageSums> Build(List<FuelStats> vac, List<FuelStats> atmo)
        {
            var stages = new List<KSPStageSums>();
            KSPStageSums.Build(vac, atmo, stages, new List<KSPStageSums>());
            return stages;
        }

        [Fact]
        public void AStageWithTwoSegmentsSumsThemInTheOrderTheyBurn()
        {
            var list = new List<FuelStats> { _c, _b, _a };
            List<KSPStageSums> stages = Build(list, list);

            Assert.Equal(2, stages.Count);
            Assert.Equal(0, stages[0].KSPStage);
            Assert.Equal(1, stages[1].KSPStage);

            StageSums s = stages[1].Atmo;
            Assert.True(s.Present);
            // the start of the burn
            Assert.Equal(100, s.StartMass, PRECISION);
            Assert.Equal(2000, s.Thrust, PRECISION);
            Assert.Equal(300, s.Isp, PRECISION);
            Assert.Equal(10, s.ControllableMass, PRECISION);
            // its end
            Assert.Equal(60, s.EndMass, PRECISION);
            Assert.Equal(40, s.ResourceMass, PRECISION);
            // the sums, and the highest acceleration (A's, 2000 / 80)
            Assert.Equal(1550, s.DeltaV, PRECISION);
            Assert.Equal(70, s.DeltaTime, PRECISION);
            Assert.Equal(5, s.StagedMass, PRECISION);
            Assert.Equal(25, s.MaxAccel, PRECISION);
            // the RCS burn at the stage's full mass: the first segment's
            Assert.Equal(40, s.MinRcsDeltaV, PRECISION);
            Assert.Equal(240, s.RcsISP, PRECISION);
            Assert.Equal(4, s.RcsThrust, PRECISION);
            Assert.Equal(1, s.RcsMass, PRECISION);
            Assert.Equal(60, s.RcsDeltaTime, PRECISION);
            Assert.Equal(0.041, s.RcsEndTMR, PRECISION);
            Assert.Equal(2, s.RcsUllageTime, PRECISION);
            // the RCS burn at the mass left at the end of the stage: the last segment's
            Assert.Equal(60, s.MaxRcsDeltaV, PRECISION);
            Assert.Equal(0.067, s.RcsStartTMR, PRECISION);

            // a stage of one segment is that segment
            StageSums c = stages[0].Vac;
            Assert.Equal(_c.StartMass, c.StartMass, PRECISION);
            Assert.Equal(_c.EndMass, c.EndMass, PRECISION);
            Assert.Equal(_c.DeltaV, c.DeltaV, PRECISION);
            Assert.Equal(_c.MaxAccel, c.MaxAccel, PRECISION);
            Assert.Equal(_c.MaxRcsDeltaV, c.MaxRcsDeltaV, PRECISION);
            Assert.Equal(_c.RcsEndTMR, c.RcsEndTMR, PRECISION);
        }

        // jets don't run in vacuum: there, stage 1 is one segment, where the atmosphere has two
        [Fact]
        public void TheListsArePairedByStageNotByPosition()
        {
            var vac1 = new FuelStats { KSPStage = 1, StartMass = 100, EndMass = 90, Thrust = 800, Isp = 330, DeltaV = 350, DeltaTime = 12 };
            List<KSPStageSums> stages = Build(new List<FuelStats> { _c, vac1 }, new List<FuelStats> { _c, _b, _a });

            Assert.Equal(2, stages.Count);
            Assert.Equal(1, stages[1].KSPStage);
            Assert.Equal(350, stages[1].Vac.DeltaV, PRECISION);
            Assert.Equal(1550, stages[1].Atmo.DeltaV, PRECISION);
            Assert.Equal(_c.DeltaV, stages[0].Vac.DeltaV, PRECISION);
            Assert.Equal(_c.DeltaV, stages[0].Atmo.DeltaV, PRECISION);
            Assert.Same(stages[1].Atmo, stages[1].Masses);
        }

        // a segment with nothing burning (the launch clamps) can start a stage: the thrust and Isp are the burn's
        [Fact]
        public void TheThrustAndIspAreThoseOfTheFirstSegmentThatBurns()
        {
            var held = new FuelStats { KSPStage = 1, StartMass = 100, EndMass = 100, Thrust = 0, Isp = 0, DeltaV = 0, DeltaTime = 0 };
            var list = new List<FuelStats> { _b, held };
            StageSums s = Build(list, list)[0].Atmo;

            Assert.Equal(100, s.StartMass, PRECISION);
            Assert.Equal(_b.Thrust, s.Thrust, PRECISION);
            Assert.Equal(_b.Isp, s.Isp, PRECISION);
            Assert.Equal(_b.EndMass, s.EndMass, PRECISION);
        }

        [Fact]
        public void AStageInOneListOnlyGetsItsEntryInStageOrder()
        {
            var vac2 = new FuelStats { KSPStage = 2, StartMass = 120, EndMass = 120 };
            List<KSPStageSums> stages = Build(new List<FuelStats> { _c, _b, _a, vac2 }, new List<FuelStats> { _c });

            Assert.Equal(new[] { 0, 1, 2 }, stages.ConvertAll(s => s.KSPStage));
            Assert.False(stages[1].Atmo.Present);
            Assert.True(stages[1].Vac.Present);
            Assert.Same(stages[1].Vac, stages[1].Masses);
            Assert.Equal(100, stages[1].Masses.StartMass, PRECISION);
            Assert.True(stages[2].Vac.Present);
        }

        [Fact]
        public void NoSegmentsGiveNoStages()
        {
            var list = new List<FuelStats> { _c, _b, _a };
            var stages = new List<KSPStageSums>();
            var pool = new List<KSPStageSums>();
            KSPStageSums.Build(list, list, stages, pool);
            KSPStageSums.Build(new List<FuelStats>(), new List<FuelStats>(), stages, pool);

            Assert.Empty(stages);
            Assert.Equal(2, pool.Count);
        }

        // the stats are rebuilt with every simulation run, ten times a second in flight
        [Fact]
        public void ARebuildAllocatesNothing()
        {
            var vac = new List<FuelStats> { _c, _a };
            var atmo = new List<FuelStats> { _c, _b, _a };
            var stages = new List<KSPStageSums>();
            var pool = new List<KSPStageSums>();
            KSPStageSums.Build(vac, atmo, stages, pool);

            AssertionExtensions.ShouldNotAllocate(() => KSPStageSums.Build(vac, atmo, stages, pool));

            Assert.Equal(2, stages.Count);
            Assert.Equal(1550, stages[1].Atmo.DeltaV, PRECISION);
        }

        // KSPStageSums.Build relies on the order the simulation publishes its segments in: latest first
        [Fact]
        public void TheSimulationListsItsSegmentsLatestFirst()
        {
            var sim = new FuelFlowSimulation();
            sim.Run(SimVesselFixtures.MainsailTinCanAsparagus());
            List<FuelStats> segments = sim.Segments;

            Assert.True(segments.Count > 2);
            for (int i = 0; i + 1 < segments.Count; i++)
            {
                Assert.True(segments[i].StartTime >= segments[i + 1].StartTime);
                Assert.True(segments[i].KSPStage <= segments[i + 1].KSPStage);
            }

            // one segment per stage there: each stage's sums are its segment
            List<KSPStageSums> stages = Build(segments, segments);
            Assert.Equal(segments.Count, stages.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                Assert.Equal(segments[i].KSPStage, stages[i].KSPStage);
                Assert.Equal(segments[i].DeltaV, stages[i].Vac.DeltaV, PRECISION);
                Assert.Equal(segments[i].StartMass, stages[i].Atmo.StartMass, PRECISION);
                Assert.Equal(segments[i].EndMass, stages[i].Atmo.EndMass, PRECISION);
            }
        }
    }
}
