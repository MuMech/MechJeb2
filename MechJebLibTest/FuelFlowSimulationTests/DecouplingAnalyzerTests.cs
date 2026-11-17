/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.FuelFlowSimulation.PartModules;
using MechJebLib.Primitives;
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

        // the analysis starts from the root part, wherever it is in the list.  starting from the lower Mainsail, the first part
        // here, the decoupler would hold on to the lower stage and drop the upper one.
        [Fact]
        public void RootPartAnywhereInTheList()
        {
            SimVessel expected = SimVesselFixtures.MainsailTinCan2Stage();
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage();
            SimVesselFixtures.MovePartToFront(v, v.Parts.Find(p => p.PersistentId == 296107038));

            Assert.False(v.Parts[0].IsRoot);
            foreach (SimPart p in v.Parts)
                Assert.Equal(expected.Parts.Find(e => e.PersistentId == p.PersistentId).DecoupledInStage, p.DecoupledInStage);
            for (int stage = 0; stage <= expected.CurrentStage; stage++)
                Assert.Equal(Ids(expected, stage), Ids(v, stage));
        }

        // a decoupler which a decoupler above it has already dropped does nothing when it fires: it is just another part of
        // what was dropped.  here decoupler A drops the tank, decoupler B and the nose cone in stage 1, and B would drop the
        // nose cone in stage 0, which is too late.  firing B kept B and the nose cone on the vessel through stage 1.
        [Fact]
        public void DecouplerAlreadyDroppedIsJustAnotherPart()
        {
            SimVessel v = SimVesselFixtures.NewVessel(2);

            SimPart probe = PartFixtures.ProbeStackLarge(v, 1);
            SimPart decouplerA = PartFixtures.Decoupler2(v, 2, 1, probe);
            SimPart tank = PartFixtures.Rockomax64BW(v, 3);
            SimPart decouplerB = PartFixtures.Decoupler2(v, 4, 0, tank);
            SimPart noseCone = PartFixtures.RocketNoseConeV3(v, 5);

            SimVesselFixtures.MakeRoot(probe);

            SimVesselFixtures.Link(probe, decouplerA);
            SimVesselFixtures.Link(decouplerA, tank);
            SimVesselFixtures.Link(tank, decouplerB);
            SimVesselFixtures.Link(decouplerB, noseCone);

            SimVesselFixtures.Finish(v);

            Assert.Equal(-1, probe.DecoupledInStage);
            Assert.Equal(1, decouplerA.DecoupledInStage);
            Assert.Equal(1, tank.DecoupledInStage);
            Assert.Equal(1, decouplerB.DecoupledInStage);
            Assert.Equal(1, noseCone.DecoupledInStage);

            Assert.Equal(new uint[] { 1, 2, 3, 4, 5 }, Ids(v, 2).OrderBy(id => id));
            Assert.Equal(new uint[] { 1 }, Ids(v, 1));
            Assert.Equal(new uint[] { 1 }, Ids(v, 0));
        }

        // MechJeb2 issue #321: a decoupler which has already fired, or has its staging disabled, or sits on a part kept out of
        // the staging, decouples nothing when its stage fires, and the tank under it stays on the vessel.  otherwise the tank
        // is dropped in stage 0, and the decoupler stays with the probe core above it.
        [Theory]
        [InlineData(false, true, true, 0)]
        [InlineData(true, true, true, -1)]
        [InlineData(false, false, true, -1)]
        [InlineData(false, true, false, -1)]
        public void DecouplerThatCannotFireDropsNothing(bool isDecoupled, bool stagingEnabled, bool stagingOn, int tankDecoupledInStage)
        {
            SimVessel v = SimVesselFixtures.NewVessel(1);

            SimPart probe = PartFixtures.ProbeStackLarge(v, 1);
            SimPart tank = PartFixtures.Rockomax64BW(v, 3, 0);
            SimPart decoupler = PartFixtures.Decoupler2(v, 2, 0, tank);
            var decouple = (SimModuleDecouple)decoupler.Modules[0];
            decouple.IsDecoupled = isDecoupled;
            decouple.StagingEnabled = stagingEnabled;
            decoupler.StagingOn = stagingOn;

            SimVesselFixtures.MakeRoot(probe);

            SimVesselFixtures.Link(probe, decoupler);
            SimVesselFixtures.Link(decoupler, tank);

            SimVesselFixtures.Finish(v);

            Assert.Equal(-1, probe.DecoupledInStage);
            Assert.Equal(-1, decoupler.DecoupledInStage);
            Assert.Equal(tankDecoupledInStage, tank.DecoupledInStage);
        }

        // a separator decouples both of its nodes, so it is dropped in its stage along with everything on the far side of it
        // from the root part: here the TS-25 Stack Separator and the tank under it
        [Fact]
        public void OmniDecouplerDropsItselfAndWhatIsBelow()
        {
            SimVessel v = SimVesselFixtures.NewVessel(1);

            SimPart probe = PartFixtures.ProbeStackLarge(v, 1);
            SimPart tank = PartFixtures.Rockomax64BW(v, 3, 0);
            SimPart separator = PartFixtures.NewDecoupler(v, "Separator.2", 2, 0, 0.21f, tank);
            ((SimModuleDecouple)separator.Modules[0]).IsOmniDecoupler = true;

            SimVesselFixtures.MakeRoot(probe);

            SimVesselFixtures.Link(probe, separator);
            SimVesselFixtures.Link(separator, tank);

            SimVesselFixtures.Finish(v);

            Assert.Equal(-1, probe.DecoupledInStage);
            Assert.Equal(0, separator.DecoupledInStage);
            Assert.Equal(0, tank.DecoupledInStage);
            Assert.Equal(new uint[] { 1 }, Ids(v, 0));
        }

        // as in KSP's ModuleDockingNode, a docking port only decouples when it is staged and its staging is enabled.  here two
        // ports docked face to face hold a tank under a Mainsail, and the ports are in the Mainsail's stage 0 (as a port which
        // isn't staged takes the stage of the staged part above it).  staged, the lower port and the tank are dropped in stage
        // 0.  unstaged, nothing is: the stock ports which aren't staged have their staging disabled too, but a mod's port
        // need not.  this dropped the lower port and the tank in stage 0 all the same.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DockingPortDecouplesOnlyWhenStaged(bool staged)
        {
            SimVessel v = SimVesselFixtures.NewVessel(1);

            SimPart probe = PartFixtures.ProbeStackLarge(v, 1);
            SimPart upperTank = PartFixtures.Rockomax64BW(v, 2);
            SimPart engine = PartFixtures.LiquidEngineMainsailV2(v, 3, 0, new V3(1, 0, 0));
            SimPart upperPort = PartFixtures.DockingPort2(v, 4, 0, null, staged, true);
            SimPart lowerPort = PartFixtures.DockingPort2(v, 5, 0, upperPort, staged, true);
            SimPart lowerTank = PartFixtures.Rockomax64BW(v, 6, 0);
            ((SimModuleDockingNode)upperPort.Modules[0]).AttachedPart = lowerPort;

            SimVesselFixtures.MakeRoot(probe);

            SimVesselFixtures.Link(probe, upperTank);
            SimVesselFixtures.Link(upperTank, engine);
            SimVesselFixtures.Link(engine, upperPort);
            SimVesselFixtures.Link(upperPort, lowerPort);
            SimVesselFixtures.Link(lowerPort, lowerTank);

            SimVesselFixtures.Finish(v);

            int dropped = staged ? 0 : -1;
            Assert.Equal(-1, engine.DecoupledInStage);
            Assert.Equal(-1, upperPort.DecoupledInStage);
            Assert.Equal(dropped, lowerPort.DecoupledInStage);
            Assert.Equal(dropped, lowerTank.DecoupledInStage);
        }

        // a new, empty craft in the editor has no root part
        [Fact]
        public void EmptyVessel()
        {
            SimVessel v = SimVesselFixtures.NewVessel(0);

            DecouplingAnalyzer.Analyze(v);

            Assert.Equal(0, v.CurrentStage);
            Assert.Empty(v.PartsRemainingInStage[0]);
        }
    }
}
