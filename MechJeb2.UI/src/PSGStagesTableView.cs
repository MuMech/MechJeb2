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
    /// the bottom), one row per stage stats row, in fixed-width columns: ΔV, burn time and TWR, then a checkbox column for each
    /// option (Guided, Full burn, and the coast's Spin), then the row's button: "End here" on a planned burn, "Plan up to here"
    /// on one above the plan's end (dimmed). A stage under Min ΔV is a dimmed line. The coast is a row of its own where it
    /// falls, its Guided in the Guided column and its spin-up in the Spin column, with up and down arrows to move it through
    /// the places a coast can go and a cross to remove it; without one, "Add coast". Every plan change goes through PlanEditor.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAscentPSGStagesMenu))]
    public class PSGStagesTableView : KodeView
    {
        readonly MechJebModuleAscentPSGStagesMenu m;

        public PSGStagesTableView(MechJebModuleAscentPSGStagesMenu m) : base(m) => this.m = m;

        protected override float MinWidth => 470;

        PlanEditor ed;
        string builtShape;
        bool spinColumn;
        float nextRead;

        // what the table's rows depend on: a change rebuilds the window
        string Shape(PlanEditor e, bool canEdit, bool edited)
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
                !m.CanEdit ? (m.VacStats.Count == 0 ? "No stages yet" : "The plan is locked for the flight (the ascent is engaged, or the vessel has left the ground)")
                : m.PlanEdited ? "Plan edited here" : "The default plan: every burn guided, no coast", true);
            KodeWindow.ToneText(state, m.PlanEdited || !m.CanEdit ? Tone.Normal : Tone.Dim);
            if (m.PlanEdited && m.CanEdit)
            {
                UIButton reset = KodeWindow.Button(top, "Reset plan", () => { m.ResetPlan(); nextRead = 0; });
                Tooltips.Add(reset.gameObject, ResetTooltip);
            }

            // only while the plan can be edited, on the ground: in flight the stages already flown are gone from the stage stats,
            // so the locked plan stops mapping onto them
            if (!ed.Fits && m.CanEdit)
            {
                UIText fit = KodeWindow.Label(r, "The saved plan doesn't fit this vessel any more: pick where it ends to re-fit it.", true);
                KodeWindow.ToneText(fit, Tone.Caution);
            }

            Layout table = KodeWindow.Panel(r);
            Layout head = KodeWindow.Row(table);
            Header(head, "Stage", NameWidth, "The KSP stage, top-down like the staging list: the first burn is at the bottom. A stage " +
                "that burns in two parts (boosters with the core, then the core alone) has a row for each part.");
            Header(head, "ΔV", NumberWidth, "The burn's vacuum Δv, as the stage stats count it.");
            Header(head, "Time", NumberWidth, "How long the burn lasts at full thrust.");
            Header(head, "TWR", NumberWidth, "Thrust-to-weight ratio at the start of the burn, at 1 g (9.81 m/s²).");
            Header(head, "Guided", OptionWidth, "PSG steers the burn. Off (unguided): the burn holds one fixed inertial attitude, " +
                "which PSG picks, e.g. for a spin-stabilised stage. The coast row has its own: off, the coast holds its attitude too.");
            Header(head, "Full", OptionWidth, "Full burn: the stage burns until it's empty, a fixed burn time, for solids or engines " +
                "that can't be shut down. Off: PSG may cut it off early to reach the orbit.");
            // the Spin column is the coast's only: without a coast (the default plan) it would stand empty
            spinColumn = ed.Coast.HasValue;
            if (spinColumn)
                Header(head, "Spin", OptionWidth, "On the coast: spin the vessel up while it coasts, at the spin-up rate (ω under Plan " +
                "options), and wait for the spin before staging, for spin-stabilised upper stages. Only on a guided coast.");
            Cell(head, "", ActionWidth);
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

            BuildPlanOptions(w, r);
        }

        static Layout Tip(Layout row, string key)
        {
            Tooltips.Add(row.gameObject, key);
            return row;
        }

        public const string ResetTooltip = "Forget the edits: the default plan, every burn guided, with no coast.";

        const string RelightNote = "An engine of the coast stage can't light again after the coast (no restart, or no ignitions left).";

        // the numbers that belong with the table: which burns it keeps, the coast's bounds and ullage, the spin rate
        void BuildPlanOptions(KodeWindow w, UIObject r)
        {
            MechJebCore core = m.Core;
            MechJebModulePSGSimulation psgs = core.PSGSimulation;
            KodeWindow.Header(r, "#MechJeb_NewUI_PlanOptions"); //Plan options
            KodeWindow.PropertyGrid g = w.Properties(r);
            Tip(OptionRows.Field(g, "#MechJeb_NewUI_PsgMinDv", psgs.MinDeltaV, "m/s", 50), "#MechJeb_NewUI_PsgMinDv_tooltip"); //Min ΔV
            w.Attention(Tip(OptionRows.Field(g, "#MechJeb_NewUI_PsgMinCoast", psgs.MinCoast, "s", 50), "#MechJeb_NewUI_PsgCoast_tooltip"), //Min coast
                () => PsgAttention.CoastTimesInverted(core));
            w.Attention(Tip(OptionRows.Field(g, "#MechJeb_NewUI_PsgMaxCoast", psgs.MaxCoast, "s", 50), "#MechJeb_NewUI_PsgCoast_tooltip"), //Max coast
                () => PsgAttention.CoastTimesInverted(core));
            Tip(OptionRows.Field(g, "#MechJeb_NewUI_PsgUllageLead", core.PSGAutopilot.UllageLeadTime, "s", 50), "#MechJeb_NewUI_PsgUllageLead_tooltip"); //Ullage lead time
            Tip(OptionRows.Field(g, "ω", core.AscentSettings.SpinupAngularVelocity, "rpm", 50), "#MechJeb_NewUI_PsgSpin_tooltip");
            UIText relight = g.Note(RelightNote);
            KodeWindow.ToneText(relight, Tone.Caution);
            KodeWindow.WrapToWidth(relight);
            w.Visible(relight.transform.parent.gameObject, () => PsgAttention.CoastNoRelight(core));
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

            if (!row.Planned)
            {
                Blank(line, spinColumn ? 3 : 2);
                foreach (UIText t in line.GetComponentsInChildren<UIText>()) KodeWindow.ToneText(t, Tone.Dim);
                Layout planHere = Action(line);
                if (m.CanEdit) KodeWindow.Button(planHere, "Plan up to here", () => Edit(e => e.EndAt(burn)));
                KodeWindow.FlexibleSpace(line);
                return;
            }

            Option(w, line, () => ed.BurnRows.Count > burn && ed.BurnRows[burn].Guided, v => Edit(e => e.SetGuided(burn, v)));
            Option(w, line, () => ed.BurnRows.Count > burn && ed.BurnRows[burn].FullBurn, v => Edit(e => e.SetFullBurn(burn, v)));
            if (spinColumn) Blank(line, 1);

            // the plan's last burn says so in plain text; the others can become the last
            if (burn == ed.PlannedCount - 1)
                KodeWindow.ToneText(KodeWindow.Label(Action(line), "last burn"), Tone.Dim);
            else
                KodeWindow.Button(Action(line), "End here", () => Edit(e => e.EndAt(burn)));
            KodeWindow.FlexibleSpace(line);
            KodeWindow.ToneText(name, Tone.Normal);
        }

        // empty option cells, so the next columns stay in place
        static void Blank(UIObject line, int n)
        {
            for (int i = 0; i < n; i++) Cell(line, "", OptionWidth);
        }

        // the last column: the row's buttons, left-aligned in a cell of a fixed width
        static Layout Action(UIObject line)
        {
            Layout cell = KodeWindow.Row(line);
            FixWidth(cell.transform, line, ActionWidth);
            return cell;
        }

        const float OptionWidth = 52, ActionWidth = 116;

        // a column's header: its name, dimmed, with what the column means as a tooltip
        static void Header(UIObject head, string text, float width, string tooltip)
        {
            UIText t = Cell(head, text, width);
            KodeWindow.ToneText(t, Tone.Dim);
            Tooltips.Add(t.gameObject, tooltip);
        }

        // a bare checkbox in an option column: the header names it
        static UnityEngine.UI.Toggle Option(KodeWindow w, UIObject line, Func<bool> get, Action<bool> set)
        {
            UnityEngine.UI.Toggle t = w.CheckBox(line, "", get, set);
            FixWidth(KodeWindow.RowOf(t).transform, line, OptionWidth);
            return t;
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
            // the description runs over the Stage to TWR cells (the same four cells as a stage row, so the next columns line up),
            // and the coast's Guided sits in the Guided column
            UIText label = Cell(line, "   " + where, NameWidth);
            label.tmpText.overflowMode = TMPro.TextOverflowModes.Overflow;
            KodeWindow.ToneText(label, Tone.Caution);
            for (int i = 0; i < 3; i++) Cell(line, "", NumberWidth);
            if (!m.CanEdit)
            {
                KodeWindow.FlexibleSpace(line);
                return;
            }

            Option(w, line, () => ed.CoastGuided, v => Edit(e => e.SetCoastGuided(v)));
            Blank(line, 1);
            UnityEngine.UI.Toggle spin = Option(w, line, () => ed.CoastSpinup, v => Edit(e => e.SetCoastSpinup(v)));
            Tooltips.Add(KodeWindow.RowOf(spin), $"Spin the vessel up during the coast, in stage {row.KSPStage}: staging waits until it " +
                "spins at the spin-up rate (ω under Plan options). Turning it on makes the coast guided.");

            int at = ed.SlotIndex(c);
            Layout buttons = Action(line);
            // the table is top-down: up is later in the flight
            UIButton up = KodeWindow.IconButton(buttons, Sprites.Arrow(Sprites.Direction.Up), () => Edit(e => e.MoveCoast(+1)));
            up.GetComponent<UnityEngine.UI.Button>().interactable = at < ed.Slots.Count - 1;
            UIButton down = KodeWindow.IconButton(buttons, Sprites.Arrow(Sprites.Direction.Down), () => Edit(e => e.MoveCoast(-1)));
            down.GetComponent<UnityEngine.UI.Button>().interactable = at > 0;
            KodeWindow.IconButton(buttons, Sprites.Cross, () => Edit(e => e.RemoveCoast()));
            KodeWindow.FlexibleSpace(line);
        }

        const float NameWidth = 120, NumberWidth = 64;

        // a label of a fixed width, so the rows' numbers line up in columns
        static UIText Cell(UIObject parent, string text, float width)
        {
            UIText t = KodeWindow.Label(parent, text);
            FixWidth(t.transform, parent, width);
            return t;
        }

        // the width goes on the element's ancestor that is the row's own child: the one the row's layout sizes
        static void FixWidth(Transform t, UIObject parent, float width)
        {
            Transform cell = t;
            while (cell.parent != null && cell.parent != parent.transform) cell = cell.parent;
            var le = cell.GetComponent<UnityEngine.UI.LayoutElement>() ?? cell.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.flexibleWidth = 0;
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
