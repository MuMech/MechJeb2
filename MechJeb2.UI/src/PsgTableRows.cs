using System.Collections.Generic;
using MechJebLib.PSG;

// No Unity here: MechJebLibTest compiles this file too (PsgTableRowsTests), next to MechJebLib.
namespace MuMech.UI
{
    /// <summary>A line of the PSG Stages table as drawn: a stage row of <see cref="PlanEditor.Rows"/>, or the plan's coast beside it.</summary>
    public struct PsgTableRow
    {
        /// <summary>The index in <see cref="PlanEditor.Rows"/> of the stage row, or of the row the coast is drawn beside.</summary>
        public int Row;

        /// <summary>The line is the plan's coast (<see cref="PlanEditor.Coast"/>), not the stage row.</summary>
        public bool Coast;

        public override string ToString() => Coast ? "coast@" + Row : "row" + Row;
    }

    /// <summary>
    /// The lines of the PSG Stages table, top-down as <c>PSGStagesTableView</c> draws them (its table model's rows, so Copy and Copy as
    /// Markdown write what is drawn).
    /// <list type="bullet">
    /// <item>Stage rows go top-down: the last row (the stage that burns last) first.</item>
    /// <item>A coast flown after a burn (AFTER) or during it (DURING) is drawn above that burn's row; one flown before it (BEFORE)
    /// below.</item>
    /// <item>A dedicated coast is drawn in place of its engine-less row; any other engine-less row is not drawn.</item>
    /// </list>
    /// </summary>
    public static class PsgTableRows
    {
        public static void TopDown(PlanEditor ed, List<PsgTableRow> into)
        {
            into.Clear();
            PlanEditor.CoastSlot? coast = ed.Coast;
            for (int i = ed.Rows.Count - 1; i >= 0; i--)
            {
                PlanEditor.Row row = ed.Rows[i];
                bool here = coast is { } c && c.VacIndex == row.VacIndex;
                PlanEditor.CoastKind kind = coast is { } k && here ? k.Kind : PlanEditor.CoastKind.BEFORE;
                if (here && (kind == PlanEditor.CoastKind.AFTER || kind == PlanEditor.CoastKind.DURING))
                    into.Add(new PsgTableRow { Row = i, Coast = true });
                if (here && kind == PlanEditor.CoastKind.DEDICATED)
                    into.Add(new PsgTableRow { Row = i, Coast = true });
                else if (row.Kind != PlanEditor.RowKind.NO_ENGINE)
                    into.Add(new PsgTableRow { Row = i });
                if (here && kind == PlanEditor.CoastKind.BEFORE)
                    into.Add(new PsgTableRow { Row = i, Coast = true });
            }
        }

        /// <summary>A coast's line, as the table draws it in the Stage to TWR cells.</summary>
        public static string CoastText(PlanEditor.CoastKind kind, int kspStage) =>
            kind switch
            {
                PlanEditor.CoastKind.BEFORE => $"Coast, then burn stage {kspStage}",
                PlanEditor.CoastKind.DURING => $"Coast during stage {kspStage} (splits its burn)",
                PlanEditor.CoastKind.AFTER  => $"Coast after stage {kspStage}, before staging",
                _                           => $"Coast at stage {kspStage} (no engines)"
            };
    }
}
