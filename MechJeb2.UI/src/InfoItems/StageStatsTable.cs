using System.Collections.Generic;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StageData = MuMech.MechJebStageStatsHelper.StageData;

namespace MuMech.UI
{
    /// <summary>
    /// The "Stage stats (all)" info item (MechJebStageStatsHelper.AllStageStats) as a retained grid: one column per
    /// StageData built once, a pool of fixed-height cells per column, columns shown by the helper's visibility, rows by
    /// the stage count. Numbers are formatted into char buffers (no strings); column widths only grow.
    /// State and actions stay in the helper, shared with the IMGUI item.
    /// </summary>
    public class StageStatsTable
    {
        class Column
        {
            public StageData Id;
            public Layout Root;
            public UIButton Header;
            public TMP_Text HeaderText;
            public LayoutElement Size;
            public string HeaderShown;
            public int MaxChars;
            public readonly List<UIText> Cells = new List<UIText>();
            public readonly List<CharText> Chars = new List<CharText>();
        }

        readonly KodeWindow w;
        readonly MechJebStageStatsHelper h;
        readonly Layout grid;
        readonly List<Column> columns = new List<Column>();
        int builtRows = -1;
        long builtShape = -1;

        public StageStatsTable(KodeWindow w, UIObject parent, MechJebStageStatsHelper helper)
        {
            this.w = w;
            h = helper;

            if (KodeWindow.ModernLayout) ModernToolbar(parent);
            else ClassicToolbar(parent);

            grid = KodeWindow.Row(parent);
            // columns at the top: a row's default centres them, so one shorter than the others would sit half a row lower
            grid.Spacing(0).ChildAlignment(TextAnchor.UpperLeft);
            foreach (StageData d in MechJebStageStatsHelper.Columns)
                columns.Add(BuildColumn(d));

            Refresh();
        }

        // modern layout: the on/off options as switches side by side, the display set as one choice
        void ModernToolbar(UIObject parent)
        {
            KodeWindow.Header(parent, "#MechJeb_InfoItems_label1"); //"Stage stats"
            Layout switches = KodeWindow.FlowWrap(parent, 10);
            Switch(switches, "#MechJeb_InfoItems_timeSeconds", () => h.TimeSeconds, h.ToggleTimeSeconds); //"Seconds"
            Switch(switches, "#MechJeb_InfoItems_hideEmpty", () => !h.ShowEmpty, h.ToggleShowEmpty);   //"Hide 0"
            Switch(switches, "#MechJeb_InfoItems_rcs", () => h.ShowRcs, h.ToggleRcs);                  //"RCS"
            w.Segmented(parent, MechJebStageStatsHelper.DisplayStateNames, () => h.DisplayState, i =>
            {
                // the helper only cycles: step to the chosen set
                for (int n = 0; n < MechJebStageStatsHelper.DisplayStateNames.Length && h.DisplayState != i; n++) h.CycleDisplayState();
                Refresh();
            });
            if (HighLogic.LoadedSceneIsEditor) EditorEnvironment(parent);
        }

        void Switch(UIObject parent, string text, System.Func<bool> get, System.Action toggle) =>
            w.SwitchRow(parent, text, get, v =>
            {
                if (v != get()) toggle();
                Refresh();
            });

        void ClassicToolbar(UIObject parent)
        {
            Layout bar = KodeWindow.Row(parent);
            KodeWindow.Label(bar, "#MechJeb_InfoItems_label1", true); //"Stage stats"
            ToolbarButton(bar, () => h.TimeSeconds ? "s" : "dhms", h.ToggleTimeSeconds);
            ToolbarButton(bar, () => h.ShowEmpty ? CachedLocalizer.Instance.MechJebInfoItemsShowEmpty : CachedLocalizer.Instance.MechJebInfoItemsHideEmpty,
                h.ToggleShowEmpty);
            ToolbarButton(bar, () => MechJebStageStatsHelper.DisplayStateNames[h.DisplayState], h.CycleDisplayState);
            ToolbarButton(bar, () => h.ShowRcs ? CachedLocalizer.Instance.MechJebInfoItemsRcs : CachedLocalizer.Instance.MechJebInfoItemsEngine, h.ToggleRcs);
            if (HighLogic.LoadedSceneIsEditor) EditorEnvironment(parent);
        }

        void ToolbarButton(UIObject parent, System.Func<string> text, System.Action onClick)
        {
            UIButton b = KodeWindow.Button(parent, text(), () =>
            {
                onClick();
                Refresh();
            });
            string shown = text();
            w.AddSync(() =>
            {
                string s = text();
                if ((object)s == shown) return; // constant strings: a reference check is enough
                shown = s;
                b.Text(s);
            });
        }

        // body combo + altitude and mach sliders (editor only)
        void EditorEnvironment(UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            new ComboBox(w, row, () => h.TwrBody, i =>
            {
                h.TwrBody = i;
                h.ApplyEnvironment();
            }, () => h.Bodies, false);

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

        Column BuildColumn(StageData d)
        {
            var c = new Column { Id = d };
            c.Root = grid.Add<Layout>("Col_" + d);
            c.Root.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(0);
            c.Root.Finish();
            c.Size = c.Root.gameObject.AddComponent<LayoutElement>();
            c.Header = KodeWindow.HeaderButton(c.Root, h.Header(d), () =>
            {
                h.HideColumn(d);
                Refresh();
            });
            c.HeaderText = c.Header.GetComponentInChildren<TMP_Text>();
            c.HeaderText.alignment = TextAlignmentOptions.Right;
            c.HeaderText.enableWordWrapping = false; // one line, so the rows of every column line up
            c.HeaderShown = h.Header(d);
            return c;
        }

        void EnsureCells(Column c, int rows)
        {
            while (c.Cells.Count < rows)
            {
                UIText t = KodeWindow.LiveLabel(c.Root, "0", true);
                t.tmpText.alignment = TextAlignmentOptions.Right;
                c.Cells.Add(t);
                c.Chars.Add(new CharText(t.tmpText));
            }
        }

        // what decides which cells exist: visible columns, rows, headers (RCS swaps two of them)
        long Shape()
        {
            long s = h.ShowRcs ? 1 : 0;
            // by index: a foreach over the IReadOnlyList would box its enumerator at each refresh
            IReadOnlyList<StageData> all = MechJebStageStatsHelper.Columns;
            for (int i = 0; i < all.Count; i++)
                s = s * 2 + (h.IsVisible(all[i]) ? 1 : 0);
            return s;
        }

        /// <summary>UpdateStageStats plus what AllStageStats draws, at the owning window's refresh rate.</summary>
        public void Refresh()
        {
            h.ApplyEnvironment();
            h.UpdateStagesOnly();
            List<int> stages = h.Stages;
            int rows = stages.Count;

            long shape = Shape();
            if (shape != builtShape || rows != builtRows)
            {
                // TMP set up under an inactive parent misses Awake: build the cells once the window is shown
                if (!grid.gameObject.activeInHierarchy) return;
                foreach (Column c in columns)
                {
                    bool visible = c.Id == StageData.KSPStage || h.IsVisible(c.Id);
                    // create cells while the column is active: TMP set up under an inactive parent misses Awake
                    if (visible && !c.Root.gameObject.activeSelf) c.Root.gameObject.SetActive(true);
                    if (visible) EnsureCells(c, rows);
                    if (c.Root.gameObject.activeSelf != visible) c.Root.gameObject.SetActive(visible);
                    string header = h.Header(c.Id);
                    if (header != c.HeaderShown)
                    {
                        c.HeaderShown = header;
                        c.Header.Text(header);
                        c.MaxChars = 0; // re-fit to the new header
                    }
                    for (int i = 0; i < c.Cells.Count; i++)
                    {
                        bool on = i < rows;
                        if (c.Cells[i].gameObject.activeSelf != on) c.Cells[i].gameObject.SetActive(on);
                    }
                }
                // only once every column has its cells: a rebuild that threw half way is tried again at the next refresh
                builtShape = shape;
                builtRows = rows;
            }

            // the modern layout highlights the stage that burns now and dims the units
            bool modern = KodeWindow.ModernLayout;
            int current = modern ? CurrentRow(stages) : -1;
            Theme th = Theme.Current;
            double gee = h.GeeASL;
            foreach (Column c in columns)
            {
                if (!c.Root.gameObject.activeSelf) continue;
                for (int row = 0; row < rows; row++)
                {
                    double v = h.Value(c.Id, row, gee, out int decimals, out string unit);
                    CharText ct = c.Chars[row].Clear();
                    if (decimals >= 0) ct.Append(v, decimals);
                    else if (decimals == MechJebStageStatsHelper.SI) ct.AppendSI(v);
                    else if (h.TimeSeconds) ct.Append(v, 2).Append('s');
                    else
                    {
                        long day = KSPUtil.dateTimeFormatter.Day, year = KSPUtil.dateTimeFormatter.Year;
                        ct.AppendDHMS(v, 1, (int)(day / 3600), (int)(year / day));
                    }
                    int length = ct.Length + (unit?.Length ?? 0);
                    if (modern && !string.IsNullOrEmpty(unit)) ct.Append("<alpha=#80>").Append(unit);
                    else ct.Append(unit);
                    FitWidth(c, length);
                    ct.Commit();
                    if (!modern) continue;
                    Color want = row == current ? th.Accent : th.Text;
                    if (c.Cells[row].tmpText.color != want) c.Cells[row].tmpText.color = want;
                }
            }
        }

        /// <summary>The row of the stage that burns now: the highest stage at or below the vessel's current stage (none in the editor).</summary>
        static int CurrentRow(List<int> stages)
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (HighLogic.LoadedSceneIsEditor || v == null) return -1;
            int best = -1;
            for (int i = 0; i < stages.Count; i++)
                if (stages[i] <= v.currentStage && (best < 0 || stages[i] > stages[best])) best = i;
            return best;
        }

        /// <summary>Column width = max(header, widest value so far); only grows, so changing values never re-lay out.</summary>
        void FitWidth(Column c, int chars)
        {
            if (chars <= c.MaxChars) return;
            c.MaxChars = chars;
            float value = TextMeasure.Measure(new string('0', chars), Theme.Current).x;
            float header = TextMeasure.Measure(c.HeaderShown, Theme.Current).x + 12; // + button padding
            c.Size.preferredWidth = Mathf.Max(value, header);
        }

        // ---- for tests ----

        public int VisibleColumns
        {
            get
            {
                int n = 0;
                foreach (Column c in columns)
                    if (c.Root.gameObject.activeSelf) n++;
                return n;
            }
        }

        public int Rows => builtRows;
    }
}
