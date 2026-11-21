/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Linq;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.PSG;
using MuMech.UI;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    // the PSG Stages table's lines as drawn, which its Copy and Copy as Markdown write
    public class PsgTableRowsTests
    {
        private static FuelStats Row(int kspStage, double deltaV, params uint[] engines)
        {
            var stats = new FuelStats { KSPStage = kspStage, DeltaV = deltaV, Isp = 300, StartMass = 10, EndMass = 5, MaxThrust = 100, MinThrust = 100 };
            foreach (uint engine in engines)
                stats.AddEngineSignature(new EngineSignature(engine, 0));
            stats.SortEngineSignatures();
            return stats;
        }

        // KSP stage 1 has no engines: a place for a dedicated coast
        private static PlanEditor Editor() =>
            new PlanEditor(new LogicalPlan(), new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 3000, 3), Row(3, 1500, 4) }, 40);

        private static string Lines(PlanEditor e)
        {
            var into = new List<PsgTableRow>();
            PsgTableRows.TopDown(e, into);
            return string.Join(" ", into.Select(l => (l.Coast ? "coast@" : "") + e.Rows[l.Row].KSPStage));
        }

        [Fact]
        public void WithoutACoastTheBurnsGoTopDownAndAnEngineLessStageIsNotDrawn()
        {
            Assert.Equal("0 2 3", Lines(Editor())); // KSP stage 0 (the last burn) on top, as the staging list
        }

        [Fact]
        public void EveryCoastSlotIsOneLineInItsDrawnPlace()
        {
            PlanEditor e = Editor();
            e.AddCoast();
            Assert.NotNull(e.Coast);
            // from the first slot of the flight to the last
            while (e.MoveCoast(-1)) { }
            var seen = new List<string>();
            do
            {
                PlanEditor.CoastSlot c = e.Coast!.Value;
                var into = new List<PsgTableRow>();
                PsgTableRows.TopDown(e, into);
                Assert.Equal(1, into.Count(l => l.Coast));
                int at = into.FindIndex(l => l.Coast);
                int row = e.Rows.FindIndex(r => r.VacIndex == c.VacIndex);
                Assert.Equal(row, into[at].Row);
                int stageLine = into.FindIndex(l => !l.Coast && l.Row == row);
                switch (c.Kind)
                {
                    case PlanEditor.CoastKind.AFTER:
                    case PlanEditor.CoastKind.DURING:
                        Assert.Equal(at + 1, stageLine); // drawn above its burn
                        break;
                    case PlanEditor.CoastKind.BEFORE:
                        Assert.Equal(at - 1, stageLine); // drawn below its burn
                        break;
                    default:
                        Assert.Equal(-1, stageLine); // in place of its engine-less row
                        break;
                }
                seen.Add(c.Kind.ToString());
            } while (e.MoveCoast(+1));
            Assert.Contains("DEDICATED", seen);
            Assert.Contains("BEFORE", seen);
        }

        [Fact]
        public void ADedicatedCoastKeepsItsEngineLessRowsPlace()
        {
            PlanEditor e = Editor();
            e.AddCoast();
            PlanEditor.CoastSlot dedicated = e.Slots.First(s => s.Kind == PlanEditor.CoastKind.DEDICATED);
            Assert.True(e.SetCoast(dedicated));
            Assert.Equal("0 coast@1 2 3", Lines(e));
        }

        [Fact]
        public void TheCoastTextIsTheOneTheTableDraws()
        {
            Assert.Equal("Coast, then burn stage 2", PsgTableRows.CoastText(PlanEditor.CoastKind.BEFORE, 2));
            Assert.Equal("Coast at stage 1 (no engines)", PsgTableRows.CoastText(PlanEditor.CoastKind.DEDICATED, 1));
        }
    }
}
