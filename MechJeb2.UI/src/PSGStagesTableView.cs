using System;
using System.Text;
using KodeKit;
using KodeUI;
using MechJebLib.PSG;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>
    /// The PSG Stages window, mockup A: the stage table. The vessel's stages top-down like KSP's staging list (the first burn at
    /// the bottom), one row per stage stats row: a planned burn has its ΔV, burn time and TWR, Guided and Full burn switches and
    /// an "End here" button; a burn above the plan's end is dimmed with "Plan up to here"; a stage under Min ΔV or with no
    /// engines is a dimmed line. The coast is a row of its own where it falls, with up and down arrows to move it through the places a coast
    /// can go, a Guided switch and a cross to remove it; without one, "Add coast". Every change edits the plan through PlanEditor.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAscentPSGStagesMenu))]
    public class PSGStagesTableView : KodeView
    {
        readonly MechJebModuleAscentPSGStagesMenu m;

        public PSGStagesTableView(MechJebModuleAscentPSGStagesMenu m) : base(m) => this.m = m;

        protected override float MinWidth => 470;

        PlanEditor ed;
        string builtShape;
        float nextRead;

        // what the table's rows depend on: a change rebuilds the window
        static string Shape(PlanEditor e, bool canEdit, bool edited)
        {
            var sb = new StringBuilder();
            sb.Append(canEdit).Append(edited).Append(e.Fits).Append(e.PlannedCount).Append(e.Coast?.ToString() ?? "-").Append('|');
            foreach (PlanEditor.Row r in e.Rows) sb.Append(r.VacIndex).Append((int)r.Kind).Append(',');
            return sb.ToString();
        }

        protected override void BeforeSync()
        {
            if (Time.unscaledTime < nextRead) return;
            nextRead = Time.unscaledTime + 0.5f;
            ed = m.Read();
            if (Shape(ed, m.CanEdit, m.PlanEdited) != builtShape) RebuildRequested = true;
        }

        // an edit: write it, read the plan back, and rebuild if the table's shape changed
        void Edit(Action<PlanEditor> change)
        {
            if (!m.CanEdit) return;
            change(ed);
            m.Apply(ed);
            nextRead = 0;
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            ed = m.Read();
            builtShape = Shape(ed, m.CanEdit, m.PlanEdited);

            Layout top = KodeWindow.Row(r);
            UIText state = KodeWindow.Label(top,
                !m.CanEdit ? (m.VacStats.Count == 0 ? "No stages yet" : "The ascent is engaged: the plan is frozen for the flight")
                : m.PlanEdited ? "Plan edited here" : "Plan from the stage settings: an edit here takes over", true);
            KodeWindow.ToneText(state, m.PlanEdited || !m.CanEdit ? Tone.Normal : Tone.Dim);
            if (m.PlanEdited && m.CanEdit) KodeWindow.Button(top, "Reset to settings", () => { m.ResetToSettings(); nextRead = 0; });

            if (!ed.Fits)
            {
                UIText fit = KodeWindow.Label(r, "The saved plan doesn't fit this vessel any more: pick where it ends to re-fit it.", true);
                KodeWindow.ToneText(fit, Tone.Caution);
            }

            Layout table = KodeWindow.Panel(r);
            Layout head = KodeWindow.Row(table);
            KodeWindow.ToneText(Cell(head, "Stage", NameWidth), Tone.Dim);
            KodeWindow.ToneText(Cell(head, "ΔV", NumberWidth), Tone.Dim);
            KodeWindow.ToneText(Cell(head, "Time", NumberWidth), Tone.Dim);
            KodeWindow.ToneText(Cell(head, "TWR", NumberWidth), Tone.Dim);
            KodeWindow.FlexibleSpace(head);

            // top-down: the last row in flight order first
            for (int i = ed.Rows.Count - 1; i >= 0; i--)
            {
                PlanEditor.Row row = ed.Rows[i];
                CoastAbove(w, table, row);
                StageRow(w, table, row);
                CoastBelow(w, table, row);
            }

            if (!ed.Coast.HasValue && m.CanEdit && ed.Slots.Count > 0)
                KodeWindow.Button(KodeWindow.Row(r), "Add coast", () => Edit(e => e.AddCoast()));
        }

        // a coast flown after this burn (AFTER, or DURING it) is drawn above its row; one flown before it (BEFORE) below
        void CoastAbove(KodeWindow w, UIObject parent, PlanEditor.Row row)
        {
            if (ed.Coast is { } c && c.VacIndex == row.VacIndex && (c.Kind == PlanEditor.CoastKind.AFTER || c.Kind == PlanEditor.CoastKind.DURING))
                CoastRow(w, parent, c, row);
        }

        void CoastBelow(KodeWindow w, UIObject parent, PlanEditor.Row row)
        {
            if (ed.Coast is { } c && c.VacIndex == row.VacIndex && c.Kind == PlanEditor.CoastKind.BEFORE)
                CoastRow(w, parent, c, row);
        }

        void StageRow(KodeWindow w, UIObject parent, PlanEditor.Row row)
        {
            Layout line = KodeWindow.Row(parent);
            int burn = row.BurnIndex;

            // a dedicated coast takes the place of its engine-less row
            if (ed.Coast is { Kind: PlanEditor.CoastKind.DEDICATED } d && d.VacIndex == row.VacIndex)
            {
                UnityEngine.Object.Destroy(line.gameObject);
                CoastRow(w, parent, d, row);
                return;
            }

            // an engine-less stage is only a place for a dedicated coast: listed only when the coast is there
            if (row.Kind == PlanEditor.RowKind.NO_ENGINE)
            {
                UnityEngine.Object.Destroy(line.gameObject);
                return;
            }

            if (row.Kind != PlanEditor.RowKind.BURN)
            {
                string why = row.Kind == PlanEditor.RowKind.NO_ENGINE ? "no engines" : $"{row.Stats.DeltaV:F0} m/s, under Min ΔV: skipped";
                KodeWindow.ToneText(KodeWindow.Label(line, $"Stage {row.KSPStage}   {why}", true), Tone.Dim);
                return;
            }

            UIText name = Cell(line, $"Stage {row.KSPStage}" + (Twin(row) ? $" ({Part(row)})" : ""), NameWidth);
            Cell(line, $"{row.Stats.DeltaV:F0} m/s", NumberWidth);
            Cell(line, $"{row.Stats.DeltaTime:F0} s", NumberWidth);
            Cell(line, $"{row.Stats.StartTWR(1):F2}", NumberWidth);
            KodeWindow.FlexibleSpace(line);

            if (!row.Planned)
            {
                foreach (UIText t in line.GetComponentsInChildren<UIText>()) KodeWindow.ToneText(t, Tone.Dim);
                if (m.CanEdit) KodeWindow.Button(line, "Plan up to here", () => Edit(e => e.EndAt(burn)));
                else KodeWindow.Space(line, 210);
                return;
            }

            w.Toggle(line, "Guided", () => ed.BurnRows.Count > burn && ed.BurnRows[burn].Guided, v => Edit(e => e.SetGuided(burn, v)));
            w.Toggle(line, "Full burn", () => ed.BurnRows.Count > burn && ed.BurnRows[burn].FullBurn, v => Edit(e => e.SetFullBurn(burn, v)));
            bool isEnd = burn == ed.PlannedCount - 1;
            UIButton end = KodeWindow.Button(line, isEnd ? "Last" : "End here", () => Edit(e => e.EndAt(burn)));
            if (isEnd) KodeWindow.ToneText(end.GetComponentInChildren<UIText>(), Tone.Dim);
            KodeWindow.ToneText(name, Tone.Normal);
        }

        void CoastRow(KodeWindow w, UIObject parent, PlanEditor.CoastSlot c, PlanEditor.Row row)
        {
            Layout line = KodeWindow.Row(parent);
            string where = c.Kind switch
            {
                PlanEditor.CoastKind.BEFORE    => $"Coast, then burn stage {row.KSPStage}",
                PlanEditor.CoastKind.DURING    => $"Coast during stage {row.KSPStage} (splits its burn)",
                PlanEditor.CoastKind.AFTER     => $"Coast after stage {row.KSPStage}, before staging",
                _                              => $"Coast at stage {row.KSPStage} (no engines)"
            };
            UIText label = KodeWindow.Label(line, "     " + where, true);
            KodeWindow.ToneText(label, Tone.Caution);
            if (!m.CanEdit) return;

            int at = ed.SlotIndex(c);
            // the table is top-down: up is later in the flight
            UIButton up = KodeWindow.IconButton(line, Sprites.Arrow(Sprites.Direction.Up), () => Edit(e => e.MoveCoast(+1)));
            up.GetComponent<UnityEngine.UI.Button>().interactable = at < ed.Slots.Count - 1;
            UIButton down = KodeWindow.IconButton(line, Sprites.Arrow(Sprites.Direction.Down), () => Edit(e => e.MoveCoast(-1)));
            down.GetComponent<UnityEngine.UI.Button>().interactable = at > 0;
            w.Toggle(line, "Guided", () => ed.CoastGuided, v => Edit(e => e.SetCoastGuided(v)));
            KodeWindow.IconButton(line, Sprites.Cross, () => Edit(e => e.RemoveCoast()));
        }

        const float NameWidth = 120, NumberWidth = 64;

        // a label of a fixed width, so the rows' numbers line up in columns
        static UIText Cell(UIObject parent, string text, float width)
        {
            UIText t = KodeWindow.Label(parent, text);
            // the width goes on the label's ancestor that is the row's own child: the one the row's layout sizes
            Transform cell = t.transform;
            while (cell.parent != null && cell.parent != parent.transform) cell = cell.parent;
            var le = cell.GetComponent<UnityEngine.UI.LayoutElement>() ?? cell.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.flexibleWidth = 0;
            return t;
        }

        // a KSP stage with two burn rows (boosters, then the core alone): the rows say which part they are
        bool Twin(PlanEditor.Row row)
        {
            int n = 0;
            foreach (PlanEditor.Row x in ed.BurnRows)
                if (x.KSPStage == row.KSPStage) n++;
            return n > 1;
        }

        string Part(PlanEditor.Row row)
        {
            int k = 0;
            foreach (PlanEditor.Row x in ed.BurnRows)
            {
                if (x.KSPStage != row.KSPStage) continue;
                k++;
                if (x == row) break;
            }

            return k == 1 ? "first part" : "second part";
        }
    }
}
