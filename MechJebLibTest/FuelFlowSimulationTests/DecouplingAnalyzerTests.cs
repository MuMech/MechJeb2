/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using MechJebLib.FuelFlowSimulation;
using Xunit;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class DecouplingAnalyzerTests
    {
        private static uint[] Ids(SimVessel v, int stage) => v.PartsRemainingInStage[stage].Select(p => p.PersistentId).ToArray();

        // with the CurrentStage one too low, as the editor can give it, the per-stage lists come out the same as with the
        // right one, part for part and in the same order
        [Fact]
        public void CurrentStageRaisedDuringTheTraversal()
        {
            SimVessel right = SimVesselFixtures.MainsailTinCanAtStage(1);
            SimVessel low = SimVesselFixtures.MainsailTinCanAtStage(0);

            Assert.Equal(1, low.CurrentStage);

            // stage 1 holds every part, stage 0 all but the two launch clamps
            Assert.Equal(right.Parts.Count, Ids(right, 1).Length);
            Assert.Equal(right.Parts.Count - 2, Ids(right, 0).Length);

            Assert.Equal(Ids(right, 1), Ids(low, 1));
            Assert.Equal(Ids(right, 0), Ids(low, 0));
            Assert.Equal(right.AvionicsRemainingInStage[1].Count, low.AvionicsRemainingInStage[1].Count);
            Assert.Equal(right.AvionicsRemainingInStage[0].Count, low.AvionicsRemainingInStage[0].Count);
        }
    }
}
