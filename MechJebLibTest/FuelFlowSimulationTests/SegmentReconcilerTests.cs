/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Linq;
using MechJebLib.FuelFlowSimulation;
using Xunit;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class SegmentReconcilerTests
    {
        private const int JET1 = 1, JET2 = 2, AERO = 3, BOOSTER = 4, CORE = 5;

        private static FuelStats Seg(int kspStage, double dv, double startMass, double endMass, double startTime, double deltaTime,
            params int[] engines)
        {
            var s = new FuelStats
            {
                KSPStage  = kspStage, DeltaV = dv, StartMass = startMass, EndMass = endMass, StartTime = startTime,
                DeltaTime = deltaTime, Thrust = engines.Length > 0 ? 100 : 0, ControllableMass = 1
            };
            foreach (int e in engines)
                s.AddEngineSignature(new EngineSignature((uint)e, 0));
            s.SortEngineSignatures();
            return s;
        }

        // the simulation's lists hold the segments latest first: these helpers take them in burn order
        private static List<FuelStats> LatestFirst(params FuelStats[] burnOrder) => burnOrder.Reverse().ToList();

        private static void AssertSameStages(List<FuelStats> atmo, List<FuelStats> vac)
        {
            Assert.Equal(atmo.Count, vac.Count);
            for (int k = 0; k < atmo.Count; k++)
                Assert.Equal(atmo[k].KSPStage, vac[k].KSPStage);
        }

        private static bool IsBookkeeping(FuelStats s) => s.DeltaV == 0 && s.DeltaTime == 0 && s.EngineSignatures.Count == 0;

        // the Aeris 4A with every engine lit: in the atmosphere the aerospike runs out first and the jets go on alone, in vacuum
        // the jets don't run. The jets' segment gets a bookkeeping partner in vacuum, after the aerospike's.
        [Fact]
        public void JetsAloneInTheAtmosphereGetAVacuumBookkeepingSegment()
        {
            List<FuelStats> atmo = LatestFirst(
                Seg(1, 0, 20, 19, 0, 0),
                Seg(0, 1403, 19, 15, 0, 60, JET1, JET2, AERO),
                Seg(0, 3946, 15, 10, 60, 300, JET1, JET2));
            List<FuelStats> vac = LatestFirst(
                Seg(1, 0, 20, 19, 0, 0),
                Seg(0, 1761, 19, 14, 0, 75, AERO));

            new SegmentReconciler().Reconcile(atmo, vac);

            AssertSameStages(atmo, vac);
            Assert.Equal(3, vac.Count);

            // latest first: index 0 is the jets alone, index 1 the jets with the aerospike, index 2 stage 1
            Assert.Equal(3946, atmo[0].DeltaV);
            Assert.True(IsBookkeeping(vac[0]));
            Assert.Equal(1403, atmo[1].DeltaV);
            Assert.Equal(1761, vac[1].DeltaV);
            Assert.Equal(1, atmo[2].KSPStage);

            // the bookkeeping segment sits at the mass its run has after the aerospike's burn
            Assert.Equal(14, vac[0].StartMass);
            Assert.Equal(14, vac[0].EndMass);
            Assert.Equal(75, vac[0].StartTime);
        }

        // the stage after the split lines up too: before, a stage with one more segment in the atmosphere moved every later
        // index of the vacuum list by one
        [Fact]
        public void LaterStagesStayAligned()
        {
            List<FuelStats> atmo = LatestFirst(
                Seg(2, 500, 30, 25, 0, 30, JET1, AERO),
                Seg(2, 400, 25, 22, 30, 40, JET1),
                Seg(1, 0, 22, 20, 70, 0),
                Seg(0, 900, 20, 12, 70, 50, CORE));
            List<FuelStats> vac = LatestFirst(
                Seg(2, 600, 30, 24, 0, 35, AERO),
                Seg(1, 0, 24, 22, 35, 0),
                Seg(0, 1000, 22, 13, 35, 50, CORE));

            new SegmentReconciler().Reconcile(atmo, vac);

            AssertSameStages(atmo, vac);
            Assert.Equal(new[] { 0, 1, 2, 2 }, atmo.Select(s => s.KSPStage));
            Assert.Equal(900, atmo[0].DeltaV);
            Assert.Equal(1000, vac[0].DeltaV);
            Assert.True(IsBookkeeping(vac[2]));
            Assert.Equal(600, vac[3].DeltaV);
        }

        // the jets run out first in the atmosphere: the vacuum's rocket pairs with the first segment, whose engines hold it,
        // and the rocket alone gets the bookkeeping partner
        [Fact]
        public void RocketOutlastingTheJetsPairsWithTheFirstSegment()
        {
            List<FuelStats> atmo = LatestFirst(
                Seg(0, 300, 20, 18, 0, 20, JET1, CORE),
                Seg(0, 700, 18, 12, 20, 40, CORE));
            List<FuelStats> vac = LatestFirst(
                Seg(0, 1100, 20, 11, 0, 60, CORE));

            new SegmentReconciler().Reconcile(atmo, vac);

            AssertSameStages(atmo, vac);
            Assert.Equal(300, atmo[1].DeltaV);
            Assert.Equal(1100, vac[1].DeltaV);
            Assert.Equal(700, atmo[0].DeltaV);
            Assert.True(IsBookkeeping(vac[0]));
        }

        // a segment that pairs with nothing on the other side gets its bookkeeping partner, in either list
        [Fact]
        public void BookkeepingInTheAtmosphereList()
        {
            List<FuelStats> atmo = LatestFirst(
                Seg(0, 800, 20, 12, 0, 50, BOOSTER, CORE));
            List<FuelStats> vac = LatestFirst(
                Seg(0, 500, 20, 15, 0, 30, BOOSTER, CORE),
                Seg(0, 400, 15, 12, 30, 20, CORE));

            new SegmentReconciler().Reconcile(atmo, vac);

            AssertSameStages(atmo, vac);
            Assert.Equal(800, atmo[1].DeltaV);
            Assert.True(IsBookkeeping(atmo[0]));
            Assert.Equal(12, atmo[0].StartMass);
            Assert.Equal(400, vac[0].DeltaV);
        }

        // a segment that pairs with nothing yet but with the other run's next one: the bookkeeping partner goes on the other
        // side, so the next ones still pair
        [Fact]
        public void BookkeepingWhereTheOtherRunPairsLater()
        {
            List<FuelStats> atmo = LatestFirst(
                Seg(0, 300, 20, 17, 0, 20, BOOSTER, CORE),
                Seg(0, 700, 17, 10, 20, 40, CORE));
            List<FuelStats> vac = LatestFirst(
                Seg(0, 350, 20, 17, 0, 20, BOOSTER, CORE),
                Seg(0, 100, 17, 16, 20, 5, BOOSTER),
                Seg(0, 800, 16, 9, 25, 40, CORE));

            new SegmentReconciler().Reconcile(atmo, vac);

            // burn order: both first segments, then the vacuum's booster with a bookkeeping partner, then the core in both
            AssertSameStages(atmo, vac);
            Assert.Equal(3, atmo.Count);
            Assert.Equal(300, atmo[2].DeltaV);
            Assert.Equal(350, vac[2].DeltaV);
            Assert.True(IsBookkeeping(atmo[1]));
            Assert.Equal(17, atmo[1].StartMass);
            Assert.Equal(100, vac[1].DeltaV);
            Assert.Equal(700, atmo[0].DeltaV);
            Assert.Equal(800, vac[0].DeltaV);
        }

        // a stage of jets alone: nothing runs in vacuum, and the two first segments stay side by side
        [Fact]
        public void JetOnlyStagePairsWithItsEmptyVacuumSegment()
        {
            List<FuelStats> atmo = LatestFirst(Seg(0, 5000, 10, 8, 0, 600, JET1));
            List<FuelStats> vac  = LatestFirst(Seg(0, 0, 10, 10, 0, 0));

            new SegmentReconciler().Reconcile(atmo, vac);

            Assert.Single(atmo);
            Assert.Single(vac);
            Assert.Equal(5000, atmo[0].DeltaV);
        }

        // nothing is merged or dropped: each list keeps its segments in order, and its Δv and burn time add up as before
        [Fact]
        public void SumsAndOrderUnchanged()
        {
            List<FuelStats> atmo = LatestFirst(
                Seg(3, 0, 50, 45, 0, 0),
                Seg(2, 500, 45, 40, 0, 30, JET1, JET2, AERO),
                Seg(2, 200, 40, 38, 30, 30, JET1, JET2),
                Seg(2, 100, 38, 37, 60, 20, JET2),
                Seg(1, 0, 37, 30, 80, 0),
                Seg(0, 900, 30, 20, 80, 60, CORE));
            List<FuelStats> vac = LatestFirst(
                Seg(3, 0, 50, 45, 0, 0),
                Seg(2, 650, 45, 39, 0, 40, AERO),
                Seg(1, 0, 39, 32, 40, 0),
                Seg(0, 1000, 32, 21, 40, 60, CORE));

            var atmoBefore = atmo.ToList();
            var vacBefore  = vac.ToList();

            new SegmentReconciler().Reconcile(atmo, vac);

            AssertSameStages(atmo, vac);
            Assert.Equal(atmoBefore.Sum(s => s.DeltaV), atmo.Sum(s => s.DeltaV));
            Assert.Equal(vacBefore.Sum(s => s.DeltaV), vac.Sum(s => s.DeltaV));
            Assert.Equal(atmoBefore.Sum(s => s.DeltaTime), atmo.Sum(s => s.DeltaTime));
            Assert.Equal(vacBefore.Sum(s => s.DeltaTime), vac.Sum(s => s.DeltaTime));
            Assert.Equal(atmoBefore, atmo.Where(s => atmoBefore.Contains(s)));
            Assert.Equal(vacBefore, vac.Where(s => vacBefore.Contains(s)));
        }

        // lists that already match are left as they are
        [Fact]
        public void MatchingListsUnchanged()
        {
            List<FuelStats> atmo = LatestFirst(Seg(1, 0, 20, 18, 0, 0), Seg(0, 900, 18, 10, 0, 60, CORE));
            List<FuelStats> vac  = LatestFirst(Seg(1, 0, 20, 18, 0, 0), Seg(0, 1000, 18, 9, 0, 60, CORE));
            var atmoBefore = atmo.ToList();
            var vacBefore  = vac.ToList();

            new SegmentReconciler().Reconcile(atmo, vac);

            Assert.Equal(atmoBefore, atmo);
            Assert.Equal(vacBefore, vac);
        }

        [Fact]
        public void EmptyLists()
        {
            var atmo = new List<FuelStats>();
            var vac  = new List<FuelStats>();

            new SegmentReconciler().Reconcile(atmo, vac);

            Assert.Empty(atmo);
            Assert.Empty(vac);
        }
    }
}
