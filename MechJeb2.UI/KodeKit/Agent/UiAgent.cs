using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using KodeKit.Headless;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit.Agent
{
    /// <summary>
    /// One control on screen as the bridge lists it: a value control (a commit target and the model that draws it), a button, a
    /// folding header, a read-out, a meter, a table or the open dialog. Made fresh for each request: nothing is registered while windows are built.
    /// </summary>
    public sealed class Control
    {
        /// <summary>
        /// "w3.Switch#2" (a commit target: its window and key), "w3.b4" (a button), "w3.d1", "w3.r7", "w3.m2", "w3.t0", or "dialog". A ref stays
        /// with its control for the window's life and is never given to another (<see cref="StableRefs"/>).
        /// </summary>
        public string Ref;

        /// <summary>The ControlInfo kinds, and button, disclosure, readout, meter, table, dialog, palette.</summary>
        public string Kind;

        /// <summary>
        /// The control's name in every language, given as a step's "id": a commit target's <see cref="CommitTarget.Id"/> (its label's
        /// '#key'), or a button's (<see cref="KodeWindow.NameButton"/>: its text's or its tooltip's '#key', numbered "/2" when the
        /// window has another). Null: none (a button with a plain text or only an icon).
        /// </summary>
        public string Id;

        /// <summary>Its window's <see cref="KodeWindow.ModuleName"/> (null for the dialog, the palette, or a window without one).</summary>
        public string Module;

        public string Label, Description, Value;
        public string[] Items, Sources;
        public string[] Actions;
        public bool Enabled = true, Invalid;
        public string Range;

        public KodeWindow Window;
        public CommitTarget Target;
        public Button Button;
        public KodeWindow.Disclosure Disclosure;
        public TableGrid Table;
        public DialogModel Dialog;
        public GameObject Anchor;
    }

    /// <summary>
    /// The agent bridge (ui-sdk's createAgent), for tests and full control of MechJeb: <see cref="Snapshot"/> lists the open windows
    /// and their controls, <see cref="Query"/> finds controls by text, <see cref="Act"/> runs steps on them, <see cref="Changes"/>
    /// reports the commits made since an earlier call, <see cref="Undo"/> undoes by source.
    /// <list type="bullet">
    /// <item>Value controls are the windows' commit targets: their kind, label, description and actions come from the target and the
    /// model that draws it (<see cref="ControlInfo"/>), and an action calls that model (<see cref="ControlActions"/>) inside
    /// <c>CommitPath.As(ChangeSource.Agent)</c>: each is a commit with the source Agent, on the path a click takes. A text is typed into
    /// its field as one edit (select, type, Enter), so the view's own end-of-edit listeners run as for a person.</item>
    /// <item>Buttons are pressed through uGUI's event system (down, up, click) inside the same scope; folding headers flip as a click;
    /// tables select rows and hide columns through their model (view state: no commit); the open dialog is answered through its model
    /// (its answer is no commit; the action it runs is).</item>
    /// <item>Where it acts as a person can: a hidden control is absent; a disabled one has no actions and refuses; while a dialog is
    /// open, only the dialog takes a step (its blocker takes every click), and undo is refused. Where it differs, on purpose: it acts on
    /// a control covered by another window or scrolled out of its list, runs up to <see cref="MaxSteps"/> steps in one frame, and undoes
    /// over every window's history, closed ones included.</item>
    /// <item>The change feed subscribes to <see cref="KodeWindow.AnyCommit"/> and <see cref="KodeWindow.AnySettled"/> while the bridge
    /// runs: one record per commit or continued gesture, and one when it settles, with its source (pointer, keyboard, agent, program,
    /// undo). Nothing is polled per frame.</item>
    /// <item>No confirmation step: the bridge is for tests and full control (<see cref="Allow"/> stays an optional hook).</item>
    /// <item>Main thread only; <see cref="AgentServer"/> brings HTTP requests here.</item>
    /// </list>
    /// </summary>
    public static class UiAgent
    {
        /// <summary>Asked before each step with the control, the action and its arguments; false refuses it. Default: everything.</summary>
        public static Func<Control, string, string[], bool> Allow = (c, action, args) => true;

        const ChangeSource Agent = ChangeSource.Agent;

        /// <summary>
        /// The protocol's version, in every answer: 2 (this bridge: ids, stable refs, the feed's types and session, 503 when no host
        /// pumps); #324's experiment had none. A client that needs a rule of a later version checks it.
        /// </summary>
        public const int Version = 2;

        // ---- windows and controls ----

        /// <summary>The windows on screen, oldest first.</summary>
        public static List<KodeWindow> OpenWindows()
        {
            var list = new List<KodeWindow>();
            IReadOnlyList<WindowBehaviour> shown = WindowBehaviour.Shown;
            for (int i = 0; i < shown.Count; i++)
            {
                WindowBehaviour b = shown[i];
                if (b != null && b.Window != null && b.isActiveAndEnabled && !list.Contains(b.Window)) list.Add(b.Window);
            }
            list.Sort((a, b) => a.Serial.CompareTo(b.Serial));
            // the refs of windows gone are forgotten (a window's serial is never reused)
            if (refs.Windows > list.Count)
            {
                var open = new HashSet<int>();
                foreach (KodeWindow w in list) open.Add(w.Serial);
                refs.KeepOnly(open);
            }
            return list;
        }

        public static string RefOf(KodeWindow w) => "w" + w.Serial;

        public static string TitleOf(KodeWindow w) => w.TitleText != null ? Clean(w.TitleText.tmpText.text) : RefOf(w);

        static readonly List<Transform> owned = new List<Transform>();

        // the numbers of the windows' buttons, headers, read-outs and tables: an old ref names its control or none, never another
        static readonly StableRefs refs = new StableRefs();

        /// <summary>The controls of <paramref name="w"/> on screen, in build order: its targets, then its buttons, headers, tables, read-outs and meters.</summary>
        public static List<Control> Collect(KodeWindow w)
        {
            var list = new List<Control>();
            string wref = RefOf(w);
            owned.Clear();
            foreach (CommitTarget t in w.Commits.Targets)
            {
                if (!(t.Control is Component c) || c == null) continue;
                owned.Add(c.transform);
                if (!c.gameObject.activeInHierarchy) continue;
                list.Add(FromTarget(w, wref, t, c));
            }
            Transform root = w.Root.transform;
            NameButtons(w);
            foreach (Button b in root.GetComponentsInChildren<Button>(false))
            {
                if (Owned(b.transform)) continue;
                Control c = FromButton(w, refs.Of(w.Serial, 'b', b.GetInstanceID()), b);
                c.Id = IdOf(b);
                list.Add(c);
            }
            foreach (KodeWindow.Disclosure d in root.GetComponentsInChildren<KodeWindow.Disclosure>(false))
                list.Add(new Control
                {
                    Ref = refs.Of(w.Serial, 'd', d.GetInstanceID()), Kind = "disclosure", Window = w, Disclosure = d, Anchor = d.gameObject,
                    Label = Clean(KodeWindow.ShownLabel(d.Title)), Value = d.Open ? "open" : "closed",
                    Actions = new[] { "expand", "collapse", "toggle" }
                });
            foreach (TableGridHandle h in root.GetComponentsInChildren<TableGridHandle>(false))
                if (h.Grid != null) list.Add(FromTable(w, refs.Of(w.Serial, 't', h.GetInstanceID()), h.Grid));
            foreach (Transform cell in root.GetComponentsInChildren<Transform>(false))
            {
                if (cell.name != "LiveCell" || Owned(cell)) continue;
                TMP_Text tx = cell.GetComponentInChildren<TMP_Text>();
                if (tx == null) continue;
                list.Add(new Control
                {
                    Ref = refs.Of(w.Serial, 'r', cell.GetInstanceID()), Kind = "readout", Window = w, Anchor = cell.gameObject, Label = LabelNear(cell),
                    // what TMP shows: a CharText value (SetCharArray) never reaches .text
                    Value = Clean(tx.GetParsedText()), Actions = new string[0]
                });
            }
            // a meter (KodeWindow.Meter): its sentence as the value (the label, the value, the scale, the bounds, the zone), read-only
            foreach (KodeWindow.MeterHandle m in root.GetComponentsInChildren<KodeWindow.MeterHandle>(false))
                if (m.Model != null) list.Add(FromMeter(w, refs.Of(w.Serial, 'm', m.GetInstanceID()), m));
            foreach (Control c in list) c.Module = w.ModuleName;
            return list;
        }

        // the ids of the last window named (NameButtons): every button of the window that isn't a commit target's, hidden ones too
        static readonly List<Button> namedButtons = new List<Button>();
        static readonly List<string> buttonNames = new List<string>();
        static string[] buttonIds = new string[0];

        /// <summary>
        /// The ids of <paramref name="w"/>'s buttons (<see cref="ControlMatch.ButtonIds"/>), from the names they were built with
        /// (<see cref="ButtonName"/>), else their text's '#key' (a text set after the build): in hierarchy order, hidden buttons
        /// included, so a fold or a hidden row changes no id. Needs <see cref="owned"/> filled for <paramref name="w"/>.
        /// </summary>
        static void NameButtons(KodeWindow w)
        {
            namedButtons.Clear();
            buttonNames.Clear();
            foreach (Button b in w.Root.transform.GetComponentsInChildren<Button>(true))
            {
                if (Owned(b.transform)) continue;
                namedButtons.Add(b);
                buttonNames.Add(NameOf(w, b));
            }
            if (buttonIds.Length < buttonNames.Count) buttonIds = new string[buttonNames.Count * 2];
            CommitPath path = w.Commits;
            ControlMatch.ButtonIds(buttonNames, id => path.FindId(id) != null, buttonIds);
        }

        static string IdOf(Button b)
        {
            int i = namedButtons.IndexOf(b);
            return i >= 0 ? buttonIds[i] : null;
        }

        static string NameOf(KodeWindow w, Button b)
        {
            ButtonName n = b.GetComponent<ButtonName>();
            if (n != null) return n.Name;
            TMP_Text tx = b.GetComponentInChildren<TMP_Text>(true);
            return tx != null && w.Behaviour.TextKeys.TryGetValue(tx, out string key) ? key : null;
        }

        /// <summary>The id of <paramref name="b"/> in <paramref name="w"/> (<see cref="Control.Id"/>; null: none).</summary>
        public static string ButtonId(KodeWindow w, Button b)
        {
            FillOwned(w);
            NameButtons(w);
            return IdOf(b);
        }

        static void FillOwned(KodeWindow w)
        {
            owned.Clear();
            foreach (CommitTarget t in w.Commits.Targets)
                if (t.Control is Component c && c != null)
                    owned.Add(c.transform);
        }

        /// <summary>Whether <paramref name="within"/> names <paramref name="w"/> (<see cref="ControlMatch.InWindow"/>: its ref, its module, a part of its title).</summary>
        public static bool InWindow(KodeWindow w, string within) => within == null || ControlMatch.InWindow(within, TitleOf(w), RefOf(w), w.ModuleName);

        static Control FromMeter(KodeWindow w, string r, KodeWindow.MeterHandle m)
        {
            // as drawn: the meter reads its model each frame (a Sync here would hide a change from its next draw)
            GaugeModel g = m.Model;
            string range = null;
            if (g.HasScale)
            {
                var b = new CharBuffer();
                g.Format(b, g.Min);
                b.Append("..");
                g.Format(b, g.Max);
                range = b.ToString();
            }
            return new Control
            {
                Ref = r, Kind = "meter", Label = Clean(KodeWindow.ShownLabel(m.Label)) ?? g.Label, Window = w, Anchor = m.gameObject,
                Value = Clean(m.Describe()), Range = range, Invalid = !g.Available, Actions = new string[0]
            };
        }

        static bool Owned(Transform t)
        {
            for (int i = 0; i < owned.Count; i++)
                if (owned[i] != null && t.IsChildOf(owned[i])) return true;
            return false;
        }

        static Control FromTarget(KodeWindow w, string wref, CommitTarget t, Component c)
        {
            ControlInfo info = ControlInfo.Of(t);
            var ctl = new Control
            {
                Ref = wref + "." + t.Key, Kind = info.Kind, Label = Clean(KodeWindow.ShownLabel(info.Label)), Description = info.Description,
                Value = info.Value, Items = info.Items, Sources = info.Sources, Id = t.Id, Window = w, Target = t, Anchor = c.gameObject, Actions = info.Actions
            };
            Selectable s = c as Selectable ?? c.GetComponent<Selectable>();
            ctl.Enabled = info.Enabled && (s != null ? s.IsInteractable() : GroupsAllow(c.transform));
            switch (t.Model)
            {
                case StepperModel m:
                    ctl.Invalid = !m.Valid;
                    break;
                case TextFieldModel m:
                    ctl.Invalid = !m.ParsesNow();
                    break;
                case SliderModel m:
                    ctl.Range = m.Min.ToString("G4", CultureInfo.InvariantCulture) + ".." + m.Max.ToString("G4", CultureInfo.InvariantCulture);
                    break;
            }
            return ctl;
        }

        static Control FromButton(KodeWindow w, string r, Button b)
        {
            string label = null;
            if (w.CloseButton != null && b.gameObject == w.CloseButton.gameObject) label = "Close";
            else if (w.UndoIcon != null && b.gameObject == w.UndoIcon.gameObject) label = "Undo";
            else if (w.RedoIcon != null && b.gameObject == w.RedoIcon.gameObject) label = "Redo";
            else
            {
                TMP_Text tx = b.GetComponentInChildren<TMP_Text>();
                if (tx != null) label = Clean(tx.GetParsedText());
            }
            string tip = TooltipOf(b.gameObject);
            if (string.IsNullOrEmpty(label)) label = tip ?? LabelNear(b.transform);
            return new Control
            {
                Ref = r, Kind = "button", Label = label, Description = tip, Window = w, Button = b, Anchor = b.gameObject,
                Enabled = b.IsInteractable(), Actions = new[] { "press" }
            };
        }

        static Control FromTable(KodeWindow w, string r, TableGrid g)
        {
            TableModel m = g.Model;
            var cols = new List<string>();
            for (int i = 0; i < m.Columns.Count; i++)
                if (m.Columns[i].Visible) cols.Add(m.Columns[i].Header ?? m.Columns[i].Name ?? "");
            var rows = new StringBuilder();
            for (int row = 0; row < m.RowCount && rows.Length < 4000; row++)
            {
                if (row > 0) rows.Append('\n');
                bool first = true;
                for (int col = 0; col < m.Columns.Count; col++)
                {
                    if (!m.Columns[col].Visible) continue;
                    if (!first) rows.Append('\t');
                    first = false;
                    string cell = m.Cell(row, col).ToString();
                    if (!string.IsNullOrEmpty(m.UnitMarkup)) cell = cell.Replace(m.UnitMarkup, " ");
                    rows.Append(Clean(cell));
                }
                if (m.Selection.IsRowSelected(row)) rows.Append("\t(selected)");
            }
            return new Control
            {
                Ref = r, Kind = "table", Label = m.Label, Window = w, Table = g, Anchor = g.Root.gameObject, Items = cols.ToArray(),
                Value = rows.ToString(), Actions = new[] { "select", "hide" }
            };
        }

        // a dialog's text as it shows: its '#key' localized, rich text out
        static string Shown(string s) => s == null ? null : Clean(KodeWindow.Loc(s));

        static Control DialogControl()
        {
            DialogModel d = Dialogs.Queue.Current;
            if (d == null) return null;
            var actions = new List<string> { "confirm", "cancel" };
            if (d.OtherText != null) actions.Add("other");
            if (d.HasInput) actions.Add("type");
            return new Control
            {
                Ref = "dialog", Kind = "dialog", Label = Shown(d.Title), Description = Shown(d.Message), Value = d.HasInput ? d.Input : null, Dialog = d,
                Items = d.OtherText != null ? new[] { Shown(d.ConfirmText), Shown(d.OtherText), Shown(d.CancelText) } : new[] { Shown(d.ConfirmText), Shown(d.CancelText) },
                Actions = actions.ToArray(), Enabled = true
            };
        }

        // the open command palette: its query as the value, its results as the items ("(highlighted)", "(can't run: why)"), its status line
        static Control PaletteControl()
        {
            PaletteModel m = CommandPalette.Model;
            if (!m.IsOpen) return null;
            var items = new string[m.ResultCount];
            for (int r = 0; r < items.Length; r++)
            {
                PaletteCommand c = m.ResultAt(r);
                string s = Clean(c.Label) + " [" + c.Id + "]";
                if (r == m.Highlight) s += " (highlighted)";
                if (!m.ResultAvailable(r)) s += " (can't run: " + Clean(c.Reason ?? "") + ")";
                else if (c == m.Armed) s += " (armed: enter again runs it)";
                items[r] = s;
            }
            return new Control
            {
                Ref = "palette", Kind = "palette", Label = Clean(m.Label), Value = m.Query, Items = items,
                Description = CommandPalette.StatusText != null ? Clean(CommandPalette.StatusText.text) : m.Status.ToString(),
                Actions = new[] { "type", "enter", "pick", "up", "down", "close" }, Enabled = true
            };
        }

        // the palette's steps: the same calls its field, its keys and its rows make, with the source Agent; a command that changes the
        // flight runs on the second enter (or pick) only, as for a person
        static string Palette(string action, string[] args)
        {
            PaletteModel m = CommandPalette.Model;
            switch (action)
            {
                case "type":
                    CommandPalette.Type(args.Length > 0 ? args[0] : "");
                    return null;
                case "enter":
                    return Entered(m.Enter(Agent), m);
                case "pick":
                    if (m.ResultCount == 0) return "no results: type another query";
                    if (args.Length < 1 || !int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) || r < 0 || r >= m.ResultCount)
                        return "pick takes a result number (0 to " + (m.ResultCount - 1) + ")";
                    return Entered(m.Pick(r, Agent), m);
                case "up":
                case "down":
                    m.Move(action == "up" ? -1 : 1);
                    return null;
                case "close":
                    m.Close(PaletteClose.Escape);
                    return null;
                default:
                    return "the palette takes type, enter, pick, up, down or close";
            }
        }

        static string Entered(PaletteEnter e, PaletteModel m)
        {
            switch (e)
            {
                case PaletteEnter.Nothing: return "no command is highlighted";
                case PaletteEnter.Unavailable: return "\"" + Clean(m.StatusCommand?.Label) + "\" can't run now: " + Clean(m.StatusCommand?.Reason ?? "");
                default: return null; // ran, or armed (it waits for a second enter)
            }
        }

        static readonly List<CanvasGroup> groups = new List<CanvasGroup>();

        // a composite with no Selectable of its own (a segmented bar): the CanvasGroups above it (a view dims a locked choice's group)
        static bool GroupsAllow(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                t.GetComponents(groups);
                bool stop = false;
                foreach (CanvasGroup g in groups)
                {
                    if (!g.enabled) continue;
                    if (!g.interactable) return false;
                    if (g.ignoreParentGroups) stop = true;
                }
                if (stop) break;
            }
            return true;
        }

        static string TooltipOf(GameObject go)
        {
            TooltipTrigger tr = go.GetComponent<TooltipTrigger>();
            string s = tr?.Text?.Invoke();
            return string.IsNullOrEmpty(s) ? null : Clean(KodeWindow.Loc(s));
        }

        /// <summary>A read-out's or an icon button's label: the text before it in its row, else after it; in a column, the text right above.</summary>
        static string LabelNear(Transform anchor)
        {
            Transform row = anchor.parent;
            if (row == null) return "";
            int at = anchor.GetSiblingIndex();
            if (row.GetComponent<HorizontalLayoutGroup>() != null || row.GetComponent<KodeUI.TableRow>() != null)
            {
                for (int i = at - 1; i >= 0; i--)
                {
                    string s = TextOf(row.GetChild(i));
                    if (s != null) return s;
                }
                for (int i = at + 1; i < row.childCount; i++)
                {
                    string s = TextOf(row.GetChild(i));
                    if (s != null) return s;
                }
            }
            else if (at > 0) return TextOf(row.GetChild(at - 1)) ?? "";
            return "";
        }

        // a plain label's text: not another control's (a live value, a field)
        static string TextOf(Transform t)
        {
            if (t.name == "LiveCell" || t.GetComponent<Selectable>() != null) return null;
            TMP_Text tx = t.GetComponentInChildren<TMP_Text>(true);
            if (tx == null || tx.GetComponentInParent<Selectable>() != null) return null;
            string s = Clean(tx.text);
            return s.Length > 0 ? s : null;
        }

        /// <summary>Rich-text tags and padding out: an agent matches labels as a person reads them.</summary>
        public static string Clean(string s)
        {
            if (s == null) return "";
            s = s.Replace("​", "");
            if (s.IndexOf('<') < 0) return s.Trim();
            var sb = new StringBuilder(s.Length);
            bool tag = false;
            foreach (char c in s)
            {
                if (c == '<') tag = true;
                else if (c == '>') tag = false;
                else if (!tag) sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        /// <summary>The controls on screen, with their windows (the open dialog first, without a window).</summary>
        public static List<KeyValuePair<KodeWindow, Control>> Present(string within = null, string kind = null)
        {
            var all = new List<KeyValuePair<KodeWindow, Control>>();
            Control dialog = DialogControl();
            if (dialog != null && (kind == null || kind == "dialog")) all.Add(new KeyValuePair<KodeWindow, Control>(null, dialog));
            Control palette = PaletteControl();
            if (palette != null && (kind == null || kind == "palette")) all.Add(new KeyValuePair<KodeWindow, Control>(null, palette));
            foreach (KodeWindow w in OpenWindows())
            {
                if (!InWindow(w, within)) continue;
                foreach (Control c in Collect(w))
                    if (kind == null || c.Kind == kind)
                        all.Add(new KeyValuePair<KodeWindow, Control>(w, c));
            }
            return all;
        }

        // ---- snapshot and query ----

        public static string Snapshot(string within = null, bool readouts = true)
        {
            var j = new JsonWriter();
            j.Open().Prop("version", Version).Prop("session", Session);
            Control dialog = DialogControl();
            if (dialog != null)
            {
                j.Open("dialog");
                Fields(j, dialog, null);
                j.Close();
            }
            Control palette = PaletteControl();
            if (palette != null)
            {
                j.Open("palette");
                Fields(j, palette, null);
                j.Close();
            }
            j.OpenArray("windows");
            foreach (KodeWindow w in OpenWindows())
            {
                if (!InWindow(w, within)) continue;
                j.Open().Prop("ref", RefOf(w)).Prop("title", TitleOf(w));
                // its name in every language, which "within" takes: the module, also as the window's id
                if (w.ModuleName != null) j.Prop("module", w.ModuleName).Prop("id", w.ModuleName);
                // behind the open dialog: its controls refuse until it is answered
                if (dialog != null) j.Prop("blocked", true);
                j.OpenArray("controls");
                foreach (Control c in Collect(w))
                {
                    if (!readouts && c.Kind == "readout") continue;
                    j.Open();
                    Fields(j, c, null);
                    j.Close();
                }
                j.CloseArray().Close();
            }
            return j.CloseArray().Close().ToString();
        }

        public static string Query(string text, string kind = null, string within = null)
        {
            var j = new JsonWriter();
            j.Open().Prop("version", Version).OpenArray("matches");
            int n = 0;
            foreach (KeyValuePair<KodeWindow, Control> p in Present(within, kind))
            {
                Control c = p.Value;
                if (text != null && !Contains(c.Label, text) && !Contains(c.Value, text) && !Contains(c.Description, text)) continue;
                j.Open();
                Fields(j, c, p.Key != null ? TitleOf(p.Key) : null);
                j.Close();
                if (++n == 100) break;
            }
            return j.CloseArray().Close().ToString();
        }

        static bool Contains(string s, string text) => s != null && s.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;

        static void Fields(JsonWriter j, Control c, string window)
        {
            j.Prop("ref", c.Ref).Prop("kind", c.Kind).Prop("label", c.Label ?? "");
            if (window != null) j.Prop("window", window);
            if (window != null && c.Module != null) j.Prop("module", c.Module);
            if (!string.IsNullOrEmpty(c.Id)) j.Prop("id", c.Id);
            if (!string.IsNullOrEmpty(c.Description)) j.Prop("description", c.Description);
            if (c.Value != null) j.Prop("value", c.Value);
            if (c.Items != null) j.Strings("items", c.Items);
            if (c.Sources != null) j.Strings("sources", c.Sources);
            if (c.Range != null) j.Prop("range", c.Range);
            if (c.Invalid) j.Prop("invalid", true);
            if (!c.Enabled) j.Prop("disabled", true);
            // a disabled control has no action a person could take
            else if (c.Actions != null && c.Actions.Length > 0) j.Strings("actions", c.Actions);
        }

        // ---- act ----

        public struct Step
        {
            /// <summary>The control: a ref, or a label (<see cref="Resolve"/>); <see cref="Id"/> names it by its id instead.</summary>
            public string Ref, Action, Within;

            /// <summary>The control's id (<see cref="CommitTarget.Id"/>), given as an id: only an id matches it.</summary>
            public string Id;

            public string[] Args;
        }

        /// <summary>
        /// A step's control (<see cref="ControlMatch"/>): an id given as an id; else a ref; else, among the controls with the action, a
        /// label or an id equal to the target (case-insensitive for the label); else the one label it begins; else the one label holding
        /// it as whole words. Never a plain substring: "Engage" must not find "Disengage". More than one candidate at the first rule that
        /// finds any is an error listing them, also when one of them matched by its id and another by its label.
        /// </summary>
        public static Control Resolve(string target, string within, string action, out string error) => Resolve(target, null, within, action, out error);

        public static Control Resolve(string target, string id, string within, string action, out string error)
        {
            error = null;
            List<KeyValuePair<KodeWindow, Control>> present = Present(within);
            var names = new ControlName[present.Count];
            for (int i = 0; i < present.Count; i++)
            {
                Control c = present[i].Value;
                names[i] = new ControlName
                {
                    Ref = c.Ref, Id = c.Id, Label = c.Label,
                    TakesAction = action == null || c.Actions != null && Array.IndexOf(c.Actions, action) >= 0 || Alias(c, action)
                };
            }
            var candidates = new List<int>();
            int k = ControlMatch.Resolve(names, target, id, candidates);
            if (k >= 0) return present[k].Value;
            string what = id != null ? "id \"" + id + "\"" : "\"" + target + "\"";
            if (k == -1)
            {
                error = "no control " + (id != null ? "with " : "") + what + (action != null ? " with action " + action : "") + (within != null ? " in \"" + within + "\"" : "");
                return null;
            }
            var listed = new List<string>();
            foreach (int i in candidates)
            {
                Control c = present[i].Value;
                listed.Add(c.Ref + " (" + c.Kind + " \"" + c.Label + "\"" + (c.Id != null ? ", id \"" + c.Id + "\"" : "") + ", " + (c.Window != null ? TitleOf(c.Window) : "dialog") + ")");
            }
            error = what + " is ambiguous: " + string.Join("; ", listed);
            return null;
        }

        // the action names of #324's bridge, still taken
        static bool Alias(Control c, string action)
        {
            switch (c.Kind)
            {
                case "switch":
                case "checkbox":
                case "chip":
                    return action == "toggle";
                case "stepper":
                    return action == "increment" || action == "decrement";
                default:
                    return false;
            }
        }

        /// <summary>Runs the steps in order and stops at the first failure (no rollback, as in ui-sdk's act).</summary>
        public static string Act(IList<Step> steps)
        {
            var j = new JsonWriter();
            j.Open().Prop("version", Version);
            var acted = new List<string>();
            string error = null;
            int failed = -1;
            for (int i = 0; i < steps.Count && error == null; i++)
            {
                Step s = steps[i];
                string[] args = s.Args ?? new string[0];
                Control c = Resolve(s.Ref, s.Id, s.Within, s.Action, out error);
                if (c == null)
                {
                    failed = i;
                    break;
                }
                if (!c.Enabled) error = c.Ref + " (" + c.Label + ") is disabled";
                else if (c.Kind != "dialog" && Dialogs.Queue.Current is DialogModel open)
                    error = "the dialog \"" + Shown(open.Title) + "\" is open: answer it first (ref \"dialog\": confirm, cancel" + (open.OtherText != null ? ", other" : "") + ")";
                // the palette's blocker covers the windows, as a dialog's does
                else if (c.Kind != "palette" && c.Kind != "dialog" && CommandPalette.IsOpen)
                    error = "the command palette is open: close it first (ref \"palette\": close)";
                else if (!Allow(c, s.Action, args)) error = c.Ref + ": " + s.Action + " was not allowed";
                else
                {
                    try
                    {
                        error = Run(c, s.Action ?? "", args);
                    }
                    catch (Exception e)
                    {
                        error = e.GetType().Name + ": " + e.Message;
                    }
                    if (!acted.Contains(c.Ref)) acted.Add(c.Ref);
                    // the window shows the change at once, as after a click
                    c.Window?.Sync();
                }
                if (error != null) failed = i;
            }
            j.Prop("ok", error == null);
            if (error != null) j.Open("error").Prop("step", failed).Prop("message", error).Close();
            // each acted control as it is now: what the steps resolved to
            j.OpenArray("values");
            List<KeyValuePair<KodeWindow, Control>> now = Present();
            foreach (string r in acted)
                foreach (KeyValuePair<KodeWindow, Control> p in now)
                    if (p.Value.Ref == r)
                    {
                        j.Open();
                        Fields(j, p.Value, p.Key != null ? TitleOf(p.Key) : null);
                        j.Close();
                    }
            j.CloseArray();
            return j.Close().ToString();
        }

        /// <summary>One action on one control, as the agent. Null on success, else what went wrong.</summary>
        public static string Run(Control c, string action, string[] args)
        {
            using (CommitPath.As(Agent))
            {
                switch (c.Kind)
                {
                    case "button":
                        if (action != "press") return "a button takes press";
                        Click(c.Button.gameObject);
                        return null;
                    case "disclosure":
                        if (action == "toggle" || action == "expand" && !c.Disclosure.Open || action == "collapse" && c.Disclosure.Open) c.Disclosure.Flip();
                        else if (action != "expand" && action != "collapse") return "a header takes expand, collapse or toggle";
                        return null;
                    case "readout":
                        return "a read-out has no action";
                    case "meter":
                        return "a meter has no action";
                    case "table":
                        return Table(c.Table.Model, action, args);
                    case "dialog":
                        return Dialog(c.Dialog, action, args);
                    case "palette":
                        return Palette(action, args);
                    case "cycle" when action == "next" || action == "previous":
                        return Cycle(c, action == "next");
                    case "stepper" when action == "set":
                    case "text" when action == "set":
                        return TypeInto(c, args);
                    default:
                        return ControlActions.Run(c.Target, action, args);
                }
            }
        }

        /// <summary>A click through uGUI's event system, as the pointer: down, up, click (a repeat button acts on down).</summary>
        public static void Click(GameObject go)
        {
            var e = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 1 };
            ExecuteEvents.Execute(go, e, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(go, e, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(go, e, ExecuteEvents.pointerClickHandler);
        }

        // an arrow selector's [<] or [>]: the first or the last button of its row
        static string Cycle(Control c, bool next)
        {
            Button[] buttons = c.Anchor.GetComponentsInChildren<Button>(false);
            if (buttons.Length < 2) return "the arrow selector has no buttons";
            Click((next ? buttons[buttons.Length - 1] : buttons[0]).gameObject);
            return null;
        }

        // TMP's "the edit ended with Escape": set by Escape, cleared only when the field is activated (a click), which the agent's edit
        // never does. Left set, the field's end-of-edit listener would cancel every agent edit after a person's Escape
        static readonly FieldInfo wasCanceled = typeof(TMP_InputField).GetField("m_WasCanceled", BindingFlags.NonPublic | BindingFlags.Instance);

        // a text typed into its field as one edit, as a person: select, the text, Enter, focus off; the view's own end-of-edit runs.
        // An edit the field didn't take (cancelled, or the value left as it was) is an error
        static string TypeInto(Control c, string[] args)
        {
            if (args.Length < 1) return "set takes a text";
            TMP_InputField input = c.Anchor.GetComponentInChildren<TMP_InputField>(false);
            if (input == null) return ControlActions.Run(c.Target, "set", args);
            if (!input.IsInteractable()) return "the field is disabled";
            string s = args[0] ?? "";
            string before = input.text;
            string valueBefore = Text(c.Target.Read());
            // a new edit, as a click's activation begins one: not cancelled
            wasCanceled?.SetValue(input, false);
            input.onSelect.Invoke(input.text);
            input.text = s;
            bool valid = c.Target.Model is StepperModel sm ? sm.Valid : !(c.Target.Model is TextFieldModel tm) || tm.ParsesNow();
            if (!valid)
            {
                // refused: the edit ends as Escape would (the text it began with written back), like ControlActions' set
                input.text = before;
                input.onEndEdit.Invoke(input.text);
                input.onDeselect.Invoke(input.text);
                return "the field does not accept \"" + s + "\"";
            }
            input.onSubmit.Invoke(input.text);
            input.onEndEdit.Invoke(input.text);
            input.onDeselect.Invoke(input.text);
            // read back: a text other than the value had must have changed it (the setter may normalise the text, so not compared to s)
            string valueAfter = Text(c.Target.Read());
            if (valueAfter == valueBefore && s != before && s != valueBefore)
                return "the field kept \"" + valueAfter + "\": the edit of \"" + s + "\" did not take";
            return null;
        }

        static string Table(TableModel m, string action, string[] args)
        {
            if (args.Length < 1) return action + " takes a " + (action == "hide" ? "column's header" : "row number");
            if (action == "select")
            {
                if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int row) || row < 0 || row >= m.RowCount)
                    return "no row " + args[0] + " (" + m.RowCount + " rows)";
                // selects (adds the row to the selection); a row already selected stays selected
                m.Selection.SelectRows(row, row, Agent);
                return null;
            }
            if (action == "hide")
            {
                for (int i = 0; i < m.Columns.Count; i++)
                    if (m.Columns[i].Visible && string.Equals(m.Columns[i].Header ?? m.Columns[i].Name, args[0], StringComparison.OrdinalIgnoreCase))
                        return m.Hide(i, Agent) ? null : "the column can't be hidden";
                return "no visible column \"" + args[0] + "\"";
            }
            return "a table takes select or hide";
        }

        static string Dialog(DialogModel d, string action, string[] args)
        {
            switch (action)
            {
                case "confirm":
                    return d.Press(DialogStop.Confirm, Agent) ? null : "the dialog can't be confirmed now" + (d.HasInput ? " (its input is not valid)" : "");
                case "cancel":
                    return d.Press(DialogStop.Cancel, Agent) ? null : "the dialog can't be cancelled";
                case "other":
                    return d.Press(DialogStop.Other, Agent) ? null : "the dialog has no third answer";
                case "type":
                    if (!d.HasInput) return "the dialog has no input";
                    d.Input = args.Length > 0 ? args[0] ?? "" : "";
                    return null;
                default:
                    return "a dialog takes confirm, cancel, other or type";
            }
        }

        // ---- undo by source ----

        static bool ParseSource(string s, out ChangeSource? source)
        {
            source = null;
            if (string.IsNullOrEmpty(s) || s == "any") return true;
            if (Enum.TryParse(s, true, out ChangeSource v))
            {
                source = v;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Undoes (or redoes) the most recent change of <paramref name="source"/> (agent, pointer, keyboard, program; null or "any": any)
        /// over every window (<see cref="KodeWindow.History"/>), as the agent. Only windows whose view opted into undo record changes.
        /// </summary>
        public static string Undo(string source, bool redo)
        {
            if (!ParseSource(source, out ChangeSource? s)) return Error("source is agent, pointer, keyboard, program or any");
            // a person can't reach a window's undo behind a dialog either
            if (Dialogs.Queue.Current is DialogModel open) return Error("the dialog \"" + Shown(open.Title) + "\" is open: answer it first");
            int before = KodeWindow.History.Count(s);
            UndoResult r;
            using (CommitPath.As(Agent))
                r = redo ? KodeWindow.History.Redo(s) : KodeWindow.History.Undo(s);
            return new JsonWriter().Open().Prop("version", Version).Prop("ok", r == UndoResult.Done).Prop("result", r.ToString())
                .Prop("undoable", KodeWindow.History.Count(s)).Prop("undoableBefore", before).Close().ToString();
        }

        // ---- the change feed ----

        struct Change
        {
            public long Seq, Commit;
            public string Type, Window, Module, Ref, Id, Label, Kind, Previous, Value, Source, Actor;
            public int Count;
            public bool Settled, Changed;
        }

        const int FeedSize = 500;
        static readonly Change[] feed = new Change[FeedSize];
        static long seq;
        static bool listening;

        /// <summary>This launch of the game: a client that kept a "since" from another launch sees the session change.</summary>
        public static readonly string Session = NewSession();

        static string NewSession()
        {
            var b = new byte[6];
            new System.Random().NextBytes(b);
            return DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>True while the change feed records (the server runs).</summary>
        public static bool Listening => listening;

        /// <summary>
        /// Starts the change feed (the server does when it starts): a record per commit, button press, fold, column hidden, dialog
        /// shown or answered, and window shown or hidden, while the bridge runs.
        /// </summary>
        public static void Listen()
        {
            if (listening) return;
            listening = true;
            KodeWindow.AnyCommit += OnCommit;
            KodeWindow.AnySettled += OnSettled;
            KodeWindow.AnyAction += OnAction;
            WindowBehaviour.AnyShown += OnShown;
            TableModel.AnyHidden += OnHidden;
            Dialogs.Queue.Opened += OnDialog;
        }

        public static void StopListening()
        {
            if (!listening) return;
            listening = false;
            KodeWindow.AnyCommit -= OnCommit;
            KodeWindow.AnySettled -= OnSettled;
            KodeWindow.AnyAction -= OnAction;
            WindowBehaviour.AnyShown -= OnShown;
            TableModel.AnyHidden -= OnHidden;
            Dialogs.Queue.Opened -= OnDialog;
        }

        static void OnCommit(KodeWindow w, Commit c) => Record(w, c, false);

        static void OnSettled(KodeWindow w, Commit c) => Record(w, c, true);

        static string Lower(ChangeSource s) => s.ToString().ToLowerInvariant();

        // the fields copied: a kept commit would keep its window's controls alive
        static void Record(KodeWindow w, Commit c, bool settled)
        {
            CommitTarget t = c.Target;
            seq++;
            feed[seq % FeedSize] = new Change
            {
                Seq = seq, Commit = c.Seq, Type = "commit", Window = TitleOf(w), Module = w.ModuleName, Ref = RefOf(w) + "." + t?.Key, Id = t?.Id,
                Label = t?.Model is IDescribeParts m && !string.IsNullOrEmpty(m.Label) ? m.Label : KodeWindow.ShownLabel(t?.Label),
                Kind = ControlInfo.KindOf(t), Previous = Text(c.Old), Value = Text(c.New),
                Source = Lower(c.Source), Actor = c.Actor.ToString().ToLowerInvariant(), Count = c.Count,
                Settled = settled, Changed = c.Changed
            };
        }

        // an action that is no commit: one record, settled at once
        static void Record(string type, string window, string r, string kind, string label, string value, ChangeSource source, KodeWindow w = null, string id = null)
        {
            seq++;
            feed[seq % FeedSize] = new Change
            {
                Seq = seq, Type = type, Window = window ?? "", Module = w?.ModuleName, Ref = r ?? "", Id = id, Kind = kind, Label = label ?? "", Previous = "",
                Value = value ?? "",
                Source = Lower(source), Actor = ChangeSources.Actor(source).ToString().ToLowerInvariant(), Count = 1, Settled = true, Changed = true
            };
        }

        static void OnAction(KodeWindow w, ViewAction a)
        {
            if (a.Type == "press")
            {
                var b = a.Control as Button ?? (a.Control != null ? a.Control.GetComponent<Button>() : null);
                if (b == null) return;
                Control c = FromButton(w, refs.Of(w.Serial, 'b', b.GetInstanceID()), b);
                Record("press", TitleOf(w), c.Ref, "button", c.Label, null, a.Source, w, ButtonId(w, b));
            }
            else if (a.Type == "fold")
            {
                KodeWindow.Disclosure d = a.Control != null ? a.Control.GetComponent<KodeWindow.Disclosure>() : null;
                Record("fold", TitleOf(w), d != null ? refs.Of(w.Serial, 'd', d.GetInstanceID()) : null, "disclosure", a.Label, a.Value, a.Source, w);
            }
        }

        static void OnShown(WindowBehaviour b, bool shown)
        {
            KodeWindow w = b.Window;
            string title;
            try
            {
                title = TitleOf(w);
            }
            catch (Exception)
            {
                title = RefOf(w); // torn down with its scene
            }
            Record("window", title, RefOf(w), "window", title, shown ? "open" : "closed", CommitPath.Resolve(ChangeSource.Program), w, w.ModuleName);
        }

        static void OnHidden(TableModel m, TableColumn column, ChangeSource source)
        {
            // the table's window: looked up only when a column is hidden
            string window = null, r = null;
            foreach (KodeWindow w in OpenWindows())
                foreach (TableGridHandle h in w.Root.GetComponentsInChildren<TableGridHandle>(false))
                    if (h.Grid != null && h.Grid.Model == m)
                    {
                        window = TitleOf(w);
                        r = refs.Of(w.Serial, 't', h.GetInstanceID());
                    }
            Record("hide", window, r, "table", m.Label, column.Header ?? column.Name, source);
        }

        static void OnDialog(DialogModel d)
        {
            Record("dialog", null, "dialog", "dialog", Shown(d.Title), "open", ChangeSource.Program);
            d.Closed += OnDialogClosed;
        }

        static void OnDialogClosed(DialogModel d)
        {
            d.Closed -= OnDialogClosed;
            if (listening) Record("dialog", null, "dialog", "dialog", Shown(d.Title), d.Result.ToString().ToLowerInvariant(), d.ResultSource);
        }

        static string Text(object v)
        {
            if (v == null) return "";
            if (v is bool b) return b ? "true" : "false";
            if (v is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);
            return v.ToString();
        }

        /// <summary>
        /// The records after <paramref name="since"/> (the "seq" of an earlier answer; 0 the first time), each with its "type": a commit
        /// (one per commit, again for each change a gesture adds to it, the same "commit" number, its count up, and one when it
        /// settles), a press (a button), a fold (a header), a hide (a table's column), a dialog (shown, then its answer) or a window (shown
        /// or hidden). The answer's "session" names the launch; "oldest" is the first record kept, and "dropped" says records after
        /// <paramref name="since"/> were overwritten before this call (more than 500 since); "reset" says <paramref name="since"/> is past
        /// this launch's last record (a "since" kept from another launch).
        /// </summary>
        public static string Changes(long since)
        {
            var j = new JsonWriter();
            long oldest = Math.Max(1, seq - FeedSize + 1);
            j.Open().Prop("version", Version).Prop("session", Session).Prop("seq", seq).Prop("oldest", oldest).Prop("listening", listening);
            if (since > 0 && since + 1 < oldest) j.Prop("dropped", true);
            if (since > seq) j.Prop("reset", true);
            j.OpenArray("changes");
            for (long s = Math.Max(oldest, since + 1); s <= seq; s++)
            {
                Change c = feed[s % FeedSize];
                if (c.Seq != s) continue;
                j.Open().Prop("seq", c.Seq).Prop("type", c.Type).Prop("commit", c.Commit).Prop("window", c.Window).Prop("ref", c.Ref);
                if (c.Module != null) j.Prop("module", c.Module);
                if (c.Id != null) j.Prop("id", c.Id);
                j.Prop("label", c.Label ?? "").Prop("kind", c.Kind).Prop("previous", c.Previous).Prop("value", c.Value)
                    .Prop("source", c.Source).Prop("actor", c.Actor).Prop("count", c.Count).Prop("settled", c.Settled).Prop("changed", c.Changed).Close();
            }
            j.CloseArray();
            return j.Close().ToString();
        }

        // ---- the wire protocol (HTTP or a test) ----

        /// <summary>
        /// GET / (the version, the session, the paths), GET /snapshot[?within=&amp;readouts=false], GET /query?text=&amp;kind=&amp;within=, GET /changes?since=n,
        /// POST /act {"steps":[{"ref" or "id","action","args","within"}]}, POST /undo {"source": "agent", "redo": false}. Returns JSON; an
        /// error is {"error": "..."}.
        /// </summary>
        public static string Handle(string path, IDictionary<string, string> query, string body)
        {
            try
            {
                string Q(string k) => query != null && query.TryGetValue(k, out string v) && v.Length > 0 ? v : null;
                switch (path.TrimEnd('/'))
                {
                    case "":
                    case "/version":
                        return About();
                    case "/snapshot":
                        return Snapshot(Q("within"), Q("readouts") != "false");
                    case "/query":
                        return Query(Q("text"), Q("kind"), Q("within"));
                    case "/changes":
                        return Changes(long.TryParse(Q("since"), out long since) ? since : 0);
                    case "/act":
                        return Act(ParseSteps(body));
                    case "/undo":
                        Dictionary<string, object> u = JsonReader.Parse(string.IsNullOrEmpty(body) ? "{}" : body) as Dictionary<string, object>
                                                       ?? throw new FormatException("undo takes {\"source\": \"agent\", \"redo\": false}");
                        return Undo(u.TryGetValue("source", out object so) && so != null ? so.ToString() : "agent", u.TryGetValue("redo", out object ro) && ro is bool rb && rb);
                    default:
                        return Error("unknown path " + path + " (/, snapshot, query, changes, act, undo)");
                }
            }
            catch (FormatException e)
            {
                // the client's mistake: answered, not logged (an exception in the log opens KSP's console on the development player)
                return Error(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Error(e.GetType().Name + ": " + e.Message);
            }
        }

        static string Error(string m) => new JsonWriter().Open().Prop("version", Version).Prop("error", m).Close().ToString();

        /// <summary>GET / : the protocol's version, the launch's session and the paths.</summary>
        public static string About() =>
            new JsonWriter().Open().Prop("version", Version).Prop("session", Session).Prop("listening", listening)
                .Strings("paths", new[] { "/snapshot", "/query", "/changes", "/act", "/undo" }).Close().ToString();

        /// <summary>The most steps one act takes: each resolves its control over every open window, on the main thread.</summary>
        public const int MaxSteps = 100;

        public static List<Step> ParseSteps(string body)
        {
            var steps = new List<Step>();
            if (!(JsonReader.Parse(body ?? "") is Dictionary<string, object> root) || !(root.TryGetValue("steps", out object o) && o is List<object> list))
                throw new FormatException("act takes {\"steps\": [...]}");
            if (list.Count > MaxSteps) throw new FormatException("an act takes at most " + MaxSteps + " steps (" + list.Count + " given)");
            foreach (object e in list)
            {
                if (!(e is Dictionary<string, object> d)) throw new FormatException("a step is an object");
                var s = new Step { Ref = Str(d, "ref"), Id = Str(d, "id"), Action = Str(d, "action"), Within = Str(d, "within") };
                if (d.TryGetValue("args", out object a) && a is List<object> al)
                {
                    s.Args = new string[al.Count];
                    for (int i = 0; i < al.Count; i++)
                        s.Args[i] = al[i] is double dv ? dv.ToString("R", CultureInfo.InvariantCulture) : al[i] is bool b ? (b ? "true" : "false") : al[i]?.ToString();
                }
                if (s.Ref == null && s.Id == null || s.Action == null) throw new FormatException("a step needs ref (or id) and action");
                steps.Add(s);
            }
            return steps;
        }

        static string Str(Dictionary<string, object> d, string k) => d.TryGetValue(k, out object v) ? v?.ToString() : null;
    }
}
