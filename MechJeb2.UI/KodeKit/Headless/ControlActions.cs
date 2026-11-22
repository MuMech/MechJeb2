#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Globalization;

namespace KodeKit.Headless
{
    /// <summary>
    /// The edits of a list a view hands its host on the list's commit target (<see cref="CommitTarget.Model"/>): its rows' labels, the
    /// row selected (what the view's buttons act on), its reorder, and its transfer from another list with that list's labels.
    /// </summary>
    [NotAWidget("a holder of a list's rows, selection, reorder and transfer for a host (the agent bridge); the list's rows are the widgets")]
    public sealed class ListEdits
    {
        /// <summary>The rows' count and labels (null: not listed).</summary>
        public Func<int> Count;
        public Func<int, string> Label;

        /// <summary>The row selected (-1: none) and how to select one (a click on it: no commit, the selection is view state).</summary>
        public Func<int> Selected;
        public Action<int> Select;

        /// <summary>The rows <see cref="Transfer"/> adds from (the registry), by label.</summary>
        public Func<int> SourceCount;
        public Func<int, string> SourceLabel;

        /// <summary>The rows' labels now (null when the view gave none).</summary>
        public string[] Rows() => Labels(Count, Label);

        /// <summary>The source rows' labels now (null when the view gave none).</summary>
        public string[] SourceRows() => Labels(SourceCount, SourceLabel);

        static string[] Labels(Func<int> count, Func<int, string> label)
        {
            if (count == null || label == null) return null;
            var a = new string[Math.Max(0, count())];
            for (int i = 0; i < a.Length; i++) a[i] = label(i) ?? "";
            return a;
        }

        /// <summary>Moves the list's rows (null: none).</summary>
        public ReorderModel Reorder;

        /// <summary>Adds rows from another list (null: none).</summary>
        public TransferModel Transfer;
    }

    /// <summary>
    /// The number of entries of a choice drawn without a model (an arrow selector, a selection grid), on its commit target
    /// (<see cref="CommitTarget.Model"/>): a host's select by "#n" is refused past it.
    /// </summary>
    [NotAWidget("the entry count of a model-less choice (arrow selector, selection grid) for a host; the widget's buttons are the parts")]
    public sealed class IndexChoice
    {
        readonly Func<int> count;

        public IndexChoice(Func<int> count) => this.count = count;

        /// <summary>The entries now.</summary>
        public int Count => count?.Invoke() ?? 0;
    }

    /// <summary>
    /// What an agent does to a control, through the model that draws it, as the agent (<see cref="ChangeSource.Agent"/>, also as the
    /// imposed source of <see cref="CommitPath.As"/>): the actions <see cref="ControlInfo.Actions"/> lists.
    /// <list type="bullet">
    /// <item>flip, set true|false: a switch, a check box or a chip; set to the value it has changes nothing.</item>
    /// <item>select item: a choice, tabs, a list, a three-way switch, a drop-down, a selection grid: the item by its text (exact,
    /// case-insensitive, else the one item it begins) or "#n" (0-based). A choice picked again still calls its setter, as a click
    /// does.</item>
    /// <item>nudge [+n|-n] [big]: a stepper's − or + (n times, each a commit; big: ×10). set text: a stepper's, a field's or an inline
    /// edit's text, as one edit (begin, type, submit: one commit); a text the field doesn't take is an error, the edit cancelled.</item>
    /// <item>set number: a slider, constrained to its range.</item>
    /// <item>select row, move row to, add source-row [at]: a list's rows (<see cref="ListEdits"/>), a row by its label, "#n" or its
    /// number; add takes a row of the list it adds from (the registry) and its place (default: the end). A select is no commit.</item>
    /// <item>A nudge takes at most <see cref="MaxNudges"/> steps: a count past it is an error (each step is a commit on the main thread).</item>
    /// <item>A disabled control, a removed target, an unknown action or a wrong argument is an error message; null is success.</item>
    /// </list>
    /// </summary>
    public static class ControlActions
    {
        /// <summary>Runs <paramref name="action"/> on <paramref name="t"/>. Null on success, else what went wrong.</summary>
        /// <summary>The most steps one nudge takes.</summary>
        public const int MaxNudges = 1000;

        public static string Run(CommitTarget t, string action, string[] args)
        {
            if (t == null) return "no control";
            if (t.Removed) return "the control is gone";
            args = args ?? new string[0];
            ControlInfo info = ControlInfo.Of(t);
            if (!info.Enabled) return "the control is disabled";
            using (CommitPath.As(ChangeSource.Agent))
            {
                switch (action)
                {
                    case "flip":
                    case "toggle":
                        return Flip(t);
                    case "set":
                        return Set(t, info, args);
                    case "select":
                        if (t.Model is ListEdits le) return SelectRow(le, args);
                        // a ref skips the bridge's action filter: a kind with no select (a chip, a field) refuses it here
                        if (Array.IndexOf(info.Actions, "select") < 0) return $"no action \"select\" on {info.Kind} \"{info.Label}\"; it has {string.Join(", ", info.Actions)}";
                        return Select(t, info, args);
                    case "nudge":
                    case "increment":
                    case "decrement":
                        return Nudge(t, action, args);
                    case "move":
                    case "add":
                        return ListEdit(t, action, args);
                    default:
                        return $"no action \"{action}\" on {info.Kind} \"{info.Label}\"; it has {string.Join(", ", info.Actions)}";
                }
            }
        }

        const ChangeSource Agent = ChangeSource.Agent;

        static string Flip(CommitTarget t)
        {
            switch (t.Model)
            {
                case ToggleModel m:
                    return m.Flip(Agent) ? null : "refused";
                default:
                    return t.Read() is bool b ? Written(t.Write(!b, Agent)) : NotFor(t, "flip");
            }
        }

        static string Set(CommitTarget t, ControlInfo info, string[] args)
        {
            if (args.Length < 1) return "set takes a value";
            string a = args[0] ?? "";
            switch (t.Model)
            {
                case ToggleModel m:
                    if (!bool.TryParse(a, out bool on)) return "set takes true or false";
                    return m.On == on || m.Set(on, Agent) ? null : "refused";
                case StepperModel m:
                    m.Type(a, Agent);
                    return m.Valid ? null : $"the field does not accept \"{a}\"";
                case TextFieldModel m:
                    return SetText(m, a);
                case SliderModel m:
                    if (!double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return "set takes a number";
                    return m.Set((float)d, Agent) || Math.Abs(m.Shown - m.Constrain((float)d)) < 1e-6f ? null : "refused";
                default:
                    if (t.Read() is bool && bool.TryParse(a, out bool b)) return Equals(t.Read(), b) ? null : Written(t.Write(b, Agent));
                    return NotFor(t, "set");
            }
        }

        // one edit: open, the text, Enter; a text it doesn't take ends the edit as Escape would
        static string SetText(TextFieldModel m, string s)
        {
            bool opened = m.Begin();
            m.Type(s, Agent);
            TextEditEnd end = m.Submit(Agent);
            if (m.Editing)
            {
                m.Cancel(Agent);
                return $"the field does not accept \"{s}\"";
            }
            if (!opened && end == TextEditEnd.None && !m.Enabled) return "the field is read-only";
            return m.ParsesNow() ? null : $"the field does not accept \"{s}\"";
        }

        static string Select(CommitTarget t, ControlInfo info, string[] args)
        {
            if (args.Length < 1) return "select takes an item";
            string[] items = info.Items;
            int k;
            string error;
            if (items == null)
            {
                // no entries known (an arrow selector): only "#n", within the count it has
                if (!TryIndex(args[0], out k)) return "select takes \"#n\" here (the entries are not known)";
                if (t.Model is IndexChoice ic && k >= ic.Count) return "no item " + args[0] + " (" + ic.Count + " items)";
            }
            else if ((k = IndexOf(items, args[0], out error)) < 0) return error;
            switch (t.Model)
            {
                case ChoiceModel m:
                    return m.Select(k, Agent) ? null : "refused";
                case Switch3Model m:
                    return m.Shown == k || m.Pick(k, Agent) ? null : "refused";
                default:
                    return Written(t.Write(k, Agent));
            }
        }

        static string Nudge(CommitTarget t, string action, string[] args)
        {
            if (!(t.Model is StepperModel m)) return NotFor(t, action);
            int n = action == "decrement" ? -1 : 1;
            bool big = false;
            foreach (string a in args)
            {
                if (a == "big") big = true;
                else if (int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c)) n = action == "decrement" ? -Math.Abs(c) : c;
                else if (a == "up" || a == "+") n = Math.Abs(n);
                else if (a == "down" || a == "-") n = -Math.Abs(n);
                else return "nudge takes a count (+3, -1) and \"big\"";
            }
            if (n == 0) return "nudge by 0";
            if (Math.Abs((long)n) > MaxNudges) return "nudge takes at most " + MaxNudges + " steps";
            for (int i = 0; i < Math.Abs(n); i++)
                if (!m.Nudge(Math.Sign(n), big, Agent)) return "refused";
            return null;
        }

        static string ListEdit(CommitTarget t, string action, string[] args)
        {
            if (!(t.Model is ListEdits e)) return NotFor(t, action);
            if (action == "move")
            {
                if (e.Reorder == null) return NotFor(t, action);
                if (args.Length < 2) return "move takes a row and its new place";
                int from = Row(e.Rows(), args[0], out string error);
                if (from < 0) return error;
                if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int to) && !TryIndex(args[1], out to))
                    return "move's place is a number";
                return e.Reorder.Move(from, to, Agent) ? null : "nothing moved";
            }
            if (e.Transfer == null) return NotFor(t, action);
            if (args.Length < 1) return "add takes a source row (its label or #n) and a place";
            int src = Row(e.SourceRows(), args[0], out string err);
            if (src < 0) return err;
            int at = int.MaxValue;
            if (args.Length > 1 && !int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out at)) return "add's place is a number";
            return e.Transfer.Transfer(src, at, Agent).Done ? null : "refused";
        }

        // a list's row: its label, "#n", or a plain number
        static int Row(string[] rows, string s, out string error)
        {
            error = null;
            if (rows == null)
            {
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || TryIndex(s, out n)) return n;
                error = "the rows are not known: give #n";
                return -1;
            }
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int k))
            {
                if (k >= 0 && k < rows.Length) return k;
                error = "no row " + s + " (" + rows.Length + " rows)";
                return -1;
            }
            return IndexOf(rows, s, out error);
        }

        static string SelectRow(ListEdits e, string[] args)
        {
            if (e.Select == null) return "the list has no selection";
            if (args.Length < 1) return "select takes a row";
            int k = Row(e.Rows(), args[0], out string error);
            if (k < 0) return error;
            e.Select(k);
            return null;
        }

        static string Written(bool ok) => ok ? null : "the value is not of the control's type";

        static string NotFor(CommitTarget t, string action) => $"{ControlInfo.Of(t).Kind} has no action \"{action}\"";

        static bool TryIndex(string s, out int k)
        {
            k = -1;
            return s != null && s.Length > 1 && s[0] == '#' && int.TryParse(s.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out k) && k >= 0;
        }

        /// <summary>
        /// The item by its text: exact (case-insensitive), else the one item it begins; "#n" picks the n-th item (0-based). A text that
        /// begins several items is an error listing them: "10" in [100, 1000] picks neither.
        /// </summary>
        public static int IndexOf(string[] items, string s, out string error)
        {
            error = null;
            s = s ?? "";
            if (TryIndex(s, out int k))
            {
                if (k < items.Length) return k;
                error = "no item " + s + " (" + items.Length + " items)";
                return -1;
            }
            for (int i = 0; i < items.Length; i++)
                if (string.Equals(items[i], s, StringComparison.OrdinalIgnoreCase)) return i;
            int found = -1;
            var begun = new List<string>();
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null && items[i].StartsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    found = i;
                    begun.Add(items[i]);
                }
            if (begun.Count == 1) return found;
            error = begun.Count == 0
                ? "no item \"" + s + "\" (items: " + string.Join(", ", items) + ")"
                : "\"" + s + "\" begins several items: " + string.Join(", ", begun);
            return -1;
        }
    }
}
