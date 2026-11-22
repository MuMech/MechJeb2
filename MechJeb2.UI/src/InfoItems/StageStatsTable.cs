using System.Collections.Generic;
using KodeKit;
using KodeKit.Headless;
using KSP.Localization;
using KodeUI;
using UnityEngine;
using UnityEngine.UI;
using StageData = MuMech.MechJebStageStatsHelper.StageData;

namespace MuMech.UI
{
    /// <summary>
    /// The "Stage stats (all)" info item (MechJebStageStatsHelper.AllStageStats) on a <see cref="KodeKit.Headless.TableModel"/>
    /// drawn by <see cref="TableGrid"/>: one column per StageData (shown by the helper's visibility, a click on a header hides
    /// it), one row per stage, the stage that burns now highlighted, units dimmed. Numbers are formatted into the model's cell
    /// buffers (no strings); column widths only grow. State and actions stay in the helper, shared with the IMGUI item.
    /// </summary>
    public class StageStatsTable
    {
        readonly KodeWindow w;
        readonly MechJebStageStatsHelper h;
        readonly TableModel model;
        readonly TableGrid grid;
        double gee;

        public StageStatsTable(KodeWindow w, UIObject parent, MechJebStageStatsHelper helper)
        {
            this.w = w;
            h = helper;

            Toolbar(parent);

            model = new TableModel(() => h.Stages.Count, Format) { Label = Localizer.Format("#MechJeb_InfoItems_label1"), UnitMarkup = "<alpha=#80>" }
               .WithHighlight(CurrentRow)
               .WithColumnState(key => key == (int)StageData.KSPStage || h.IsVisible((StageData)key), key => h.Header((StageData)key))
               .WithHide((key, source) => h.HideColumn((StageData)key));
            // by index: a foreach over the IReadOnlyList would box its enumerator
            IReadOnlyList<StageData> all = MechJebStageStatsHelper.Columns;
            for (int i = 0; i < all.Count; i++)
            {
                TableColumn c = model.AddColumn((int)all[i], h.Header(all[i]));
                c.Name = all[i].ToString(); // Col_<StageData>, as before
                c.Hideable = all[i] != StageData.KSPStage;
            }
            grid = new TableGrid(parent, model);
            grid.RowCards(StageCard);

            Refresh();
        }

        // a stage row's hover card: the stage, its engines (by part, counted), its Isp in the atmosphere and in vacuum and its burn
        // time, both live (the table's own simulation, which the window runs anyway)
        void StageCard(int row, HoverCard card)
        {
            if (!h.HasRow(row)) return;
            card.Title(Localizer.Format("#MechJeb_NewUI_Card_Stage", h.KspStageOf(row))); //Stage <<1>>
            card.Text("#MechJeb_NewUI_Card_Engines", true); //Engines
            card.Text(EngineList(h.EnginesOf(row)));
            // with RCS shown, the RCS Isp (as the table's Isp column and the burn time)
            card.Value(h.ShowRcs ? "#MechJeb_NewUI_Card_RcsIsp" : "#MechJeb_NewUI_Card_Isp", ct => //RCS Isp / Isp (atm / vac)
            {
                if (!h.HasRow(row)) return;
                ct.Append(h.AtmoIspOf(row), 1).Append(" s / ").Append(h.VacIspOf(row), 1).Append(" s");
            });
            card.Value("#MechJeb_NewUI_Card_BurnTime", ct => //Burn time
            {
                if (!h.HasRow(row)) return;
                double t = h.BurnTimeOf(row);
                if (h.TimeSeconds) ct.Append(t, 2).Append('s');
                else ct.AppendDHMS(t, 1);
            });
        }

        /// <summary>The stage's engines as "2 × title" lines, in the order of their parts (made when the card opens).</summary>
        internal static string EngineList(IReadOnlyList<MechJebLib.FuelFlowSimulation.EngineSignature> engines)
        {
            if (engines == null || engines.Count == 0) return Localizer.Format("#MechJeb_NewUI_Card_NoEngine"); //none
            List<Part> parts = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch?.ship?.parts : FlightGlobals.ActiveVessel?.parts;
            var titles = new List<string>();
            var counts = new List<int>();
            var seen = new HashSet<uint>();
            for (int i = 0; i < engines.Count; i++)
            {
                // one line per part: a multi-mode engine's modules are one engine
                if (!seen.Add(engines[i].PartPersistentID)) continue;
                string title = TitleOf(parts, engines[i].PartPersistentID) ?? "?";
                int at = titles.IndexOf(title);
                if (at < 0)
                {
                    titles.Add(title);
                    counts.Add(1);
                }
                else counts[at]++;
            }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < titles.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(counts[i]).Append(" × ").Append(titles[i]);
            }
            return sb.ToString();
        }

        static string TitleOf(List<Part> parts, uint id)
        {
            if (parts == null) return null;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] != null && parts[i].persistentId == id) return parts[i].partInfo?.title;
            return null;
        }

        // a cell: the helper's value, formatted as UpdateStageDisplayInfo formats it, and its unit
        void Format(int row, int key, CharBuffer into, ref string unit)
        {
            double v = h.Value((StageData)key, row, gee, out int decimals, out unit);
            if (decimals >= 0) into.Append(v, decimals);
            else if (decimals == MechJebStageStatsHelper.SI) into.AppendSI(v);
            else if (h.TimeSeconds) into.Append(v, 2).Append('s');
            else into.AppendDHMS(v, 1);
        }

        // the on/off options as switches side by side, the display set as one choice
        void Toolbar(UIObject parent)
        {
            KodeWindow.Header(parent, "#MechJeb_InfoItems_label1"); //"Stage stats"
            Layout switches = KodeWindow.FlowWrap(parent, 10);
            Switch(switches, "#MechJeb_InfoItems_timeSeconds", () => h.TimeSeconds, h.ToggleTimeSeconds, "#MechJeb_NewUI_StatsSeconds_tooltip"); //"Seconds"
            Switch(switches, "#MechJeb_InfoItems_hideEmpty", () => !h.ShowEmpty, h.ToggleShowEmpty, "#MechJeb_NewUI_StatsHideEmpty_tooltip");   //"Hide 0"
            Switch(switches, "#MechJeb_InfoItems_rcs", () => h.ShowRcs, h.ToggleRcs, "#MechJeb_NewUI_StatsRcs_tooltip");                  //"RCS"
            CopyButtons(switches, () => model); // the model is made after the toolbar
            w.Segmented(parent, MechJebStageStatsHelper.DisplayStateNames, () => h.DisplayState, i =>
            {
                // the helper only cycles: step to the chosen set
                for (int n = 0; n < MechJebStageStatsHelper.DisplayStateNames.Length && h.DisplayState != i; n++) h.CycleDisplayState();
                Refresh();
            }, label: "#MechJeb_NewUI_Label_StatsShown"); //Stats shown
            if (HighLogic.LoadedSceneIsEditor) EditorEnvironment(parent);
        }

        /// <summary>
        /// Copy and Copy table as Markdown: every row, the shown columns, as the table shows them, to
        /// the clipboard; a toast says how many rows. Two buttons rather than a format choice: each is one click, and neither hides a
        /// state that changes what the other does.
        /// </summary>
        internal static void CopyButtons(UIObject parent, System.Func<TableModel> model)
        {
            Tooltips.Add(KodeWindow.Button(parent, "#MechJeb_NewUI_CopyRows", () => CopyTable(model(), false)).gameObject, "#MechJeb_NewUI_CopyRows_tooltip"); //Copy
            Tooltips.Add(KodeWindow.Button(parent, "#MechJeb_NewUI_CopyTableMarkdown", () => CopyTable(model(), true)).gameObject, "#MechJeb_NewUI_CopyMarkdown_tooltip"); //Copy table as Markdown
        }

        /// <summary>The table's rows (its selected ones when a host or an agent selected some) to the clipboard, as tab-separated text or a Markdown table.</summary>
        internal static int CopyTable(TableModel model, bool markdown)
        {
            var sb = new System.Text.StringBuilder();
            int rows = markdown ? model.CopyMarkdown(sb) : model.CopyRows(sb);
            MuUtils.SystemClipboard = sb.ToString();
            Toasts.Show(Localizer.Format(markdown ? "#MechJeb_NewUI_Toast_RowsCopiedMarkdown" : "#MechJeb_NewUI_Toast_RowsCopied", rows)); //<<1>> rows copied (as Markdown)
            return rows;
        }

        void Switch(UIObject parent, string text, System.Func<bool> get, System.Action toggle, string tooltip) =>
            Tooltips.Add(SwitchToggle(parent, text, get, toggle).transform.parent.gameObject, tooltip);

        UnityEngine.UI.Toggle SwitchToggle(UIObject parent, string text, System.Func<bool> get, System.Action toggle) =>
            w.SwitchRow(parent, text, get, v =>
            {
                if (v != get()) toggle();
                Refresh();
            });

        // body combo + altitude and mach sliders (editor only)
        void EditorEnvironment(UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            new ComboBox(w, row, () => h.TwrBody, i =>
            {
                h.TwrBody = i;
                h.ApplyEnvironment();
            }, () => h.Bodies, false) { Filter = true }; // typeable once long: a planet pack's bodies

            Layout sliders = row.Add<Layout>("Sliders");
            sliders.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false);
            sliders.FlexibleLayout(true, false);
            sliders.Finish();

            Layout alt = KodeWindow.Row(sliders);
            w.Slider(alt, () => h.AltSLTScale, v =>
            {
                h.AltSLTScale = v;
                h.ApplyEnvironment();
            });
            float altShown = -1;
            string altText = "";
            UIText altLabel = w.BoundLabel(alt, () =>
            {
                if (h.AltSLTScale != altShown)
                {
                    altShown = h.AltSLTScale;
                    altText = MechJebLib.Utils.Statics.ToSI(h.AltSLT) + "m";
                }
                return altText;
            });
            altLabel.transform.parent.GetComponent<LayoutElement>().preferredWidth = 80;

            Layout mach = KodeWindow.Row(sliders);
            w.Slider(mach, () => h.MachScale, v =>
            {
                h.MachScale = v;
                h.ApplyEnvironment();
            });
            float machShown = -1;
            string machText = "";
            UIText machLabel = w.BoundLabel(mach, () =>
            {
                if (h.MachScale != machShown)
                {
                    machShown = h.MachScale;
                    machText = h.Mach.ToString("F1") + " M";
                }
                return machText;
            });
            machLabel.transform.parent.GetComponent<LayoutElement>().preferredWidth = 80;
        }

        /// <summary>UpdateStageStats plus what AllStageStats draws, at the owning window's refresh rate.</summary>
        public void Refresh()
        {
            h.ApplyEnvironment();
            h.UpdateStagesOnly();
            gee = h.GeeASL;
            model.Sync();
            grid.Draw();
        }

        /// <summary>The row of the stage that burns now: the highest stage at or below the vessel's current stage (none in the editor).</summary>
        int CurrentRow()
        {
            List<int> stages = h.Stages;
            Vessel v = FlightGlobals.ActiveVessel;
            if (HighLogic.LoadedSceneIsEditor || v == null) return -1;
            int best = -1;
            for (int i = 0; i < stages.Count; i++)
                if (stages[i] <= v.currentStage && (best < 0 || stages[i] > stages[best])) best = i;
            return best;
        }

        // ---- for tests ----

        public int VisibleColumns => grid.VisibleColumns;

        public int Rows => model.RowCount;

        /// <summary>The table's model (columns, rows, the highlighted stage, the selection).</summary>
        public TableModel Model => model;

        public TableGrid Grid => grid;
    }
}
