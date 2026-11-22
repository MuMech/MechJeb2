#if !CSHARP_7_3
#nullable disable
#endif
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>What a part is, as an assistive tool or an agent would name it (ui-sdk's roles, after ARIA's).</summary>
    public enum PartRole
    {
        /// <summary>Parts that belong together (a segmented bar, a drop-down's group header, the toasts).</summary>
        Group,
        Radio,
        Button,

        /// <summary>A value with a decrease and an increase (a stepper's field).</summary>
        SpinButton,

        /// <summary>A modal question (KodeKit's dialogs).</summary>
        Dialog,

        /// <summary>A text input (a prompt's field, a drop-down's filter, an inline edit while it is edited).</summary>
        TextBox,

        /// <summary>A drop-down's button: what is chosen, and the list it opens.</summary>
        ComboBox,

        /// <summary>A list of options of which at most one is chosen (an open drop-down's list, a timeline).</summary>
        ListBox,

        /// <summary>An entry of a list box (On: chosen; Focus: highlighted).</summary>
        Option,

        Tree,

        /// <summary>A row of a tree (Expanded: open; On or Mixed: its check).</summary>
        TreeItem,

        /// <summary>A value along an axis that a drag moves (a graph's scroll, a range's ends and points).</summary>
        Slider,

        /// <summary>A movable window.</summary>
        Window,

        /// <summary>A handle that resizes what is next to it (a window's grip).</summary>
        Separator,

        /// <summary>A notice that needs no answer (a toast).</summary>
        Status,

        /// <summary>A drawing with no parts of its own to pick (a graph's plot).</summary>
        Figure,

        /// <summary>An on/off value drawn as a switch (On: on).</summary>
        Switch,

        /// <summary>An on/off value drawn as a check box (On: checked).</summary>
        CheckBox,

        /// <summary>A tab of a tab list: one of its group's tabs is on, like a radio group's items (On: its page is shown).</summary>
        Tab,

        /// <summary>Rows of cells under column headers (Delta-V Stats, PSG Stages).</summary>
        Table,

        /// <summary>A column's header (On: the column sorts the table; a header that hides its column on a click is a button).</summary>
        ColumnHeader,

        /// <summary>A table's row (On: selected; Current: the row of now, as the stage that burns).</summary>
        Row,

        /// <summary>A table's cell (On: in the selected range).</summary>
        Cell,

        /// <summary>A menu of actions (a context menu; Expanded: open).</summary>
        Menu,

        /// <summary>An action of a menu (Disabled: can't be picked now; On: checked).</summary>
        MenuItem,

        /// <summary>A value on a known scale, drawn as a filled bar (ARIA's meter: a gauge; Invalid: no value to show).</summary>
        Meter
    }

    /// <summary>The keys a part can handle, whichever model it belongs to.</summary>
    public enum PartKey { Left, Right, Up, Down, Home, End, PageUp, PageDown, Enter, Space, Escape, Tab }

    /// <summary>The state flags a part reports.</summary>
    [System.Flags]
    public enum PartFlags
    {
        None = 0,
        On = 1,
        Focus = 2,
        Disabled = 4,
        Invalid = 8,

        /// <summary>Open (a drop-down showing its list, a tree's open branch).</summary>
        Expanded = 16,

        /// <summary>Partly on (a tree branch with some of its leaves checked).</summary>
        Mixed = 32,

        /// <summary>Held by a drag now (a window being moved, a range's end).</summary>
        Grabbed = 64,

        /// <summary>The item that stands for now (ARIA's aria-current): a table's row of the stage that burns.</summary>
        Current = 128
    }

    /// <summary>
    /// Marks a class of this folder that is not a widget's model, so it has no parts to describe: a helper a widget's model uses
    /// (a matcher, a key buffer, a list's geometry, a drag gesture), a queue or stack that holds models, or data a model hands out.
    /// The pattern checks' test requires every class here to describe its parts or to carry this, with the reason.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)]
    public sealed class NotAWidgetAttribute : System.Attribute
    {
        public NotAWidgetAttribute(string why) => Why = why;

        /// <summary>Why the class has no parts (what it is, and which widget it serves).</summary>
        public string Why { get; }
    }

    /// <summary>One part of a model as it is now: its name, role, label, flags, whether it takes focus, and the keys it handles.</summary>
    [NotAWidget("data: one part of a description")]
    public sealed class PartDescription
    {
        public string Name;
        public PartRole Role;
        public string Label;
        public PartFlags Flags;
        public bool Focusable;
        /// <summary>Read-only: models hand out shared arrays here.</summary>
        public IReadOnlyList<PartKey> Keys = NoKeys;

        static readonly PartKey[] NoKeys = new PartKey[0];

        public bool Has(PartFlags f) => (Flags & f) != 0;
    }

    /// <summary>
    /// A model that can report its parts (ui-sdk's anatomy). Every widget model in this folder implements it (the other classes
    /// carry <see cref="NotAWidgetAttribute"/>), and <see cref="Patterns.Check"/> runs on what it reports. A description
    /// allocates: it is for tests, an agent or an accessibility tree, never for a frame.
    /// </summary>
    public interface IDescribeParts
    {
        /// <summary>
        /// What the control is called, as shown (null: its factory gave no label). KodeKit's factories set it from the text they
        /// have: a labelled row's label, a dialog's title, a window's title, a graph's title.
        /// </summary>
        string Label { get; set; }

        /// <summary>Appends the model's parts, as they are now, to <paramref name="into"/>.</summary>
        void Describe(List<PartDescription> into);
    }

    /// <summary>
    /// ui-sdk's pattern checks over a model's description.
    /// <list type="bullet">
    /// <item>The control has a label, and so has every focusable part and every part a player or an agent picks by its name
    /// (<see cref="NeedsLabel"/>: groups, buttons, dialogs, text boxes, drop-downs, list boxes and their options, trees and their
    /// items, sliders, windows, menus and their items, meters).</item>
    /// <item>A focusable part handles at least one key.</item>
    /// <item>A radio group (or a tab list) has exactly one item on and at most one focused.</item>
    /// <item>A list box has at most one option on and at most one highlighted (Focus).</item>
    /// <item>Part names are unique.</item>
    /// </list>
    /// </summary>
    public static class Patterns
    {
        /// <summary>Whether a part of <paramref name="role"/> needs a label even when it doesn't take the keyboard.</summary>
        public static bool NeedsLabel(PartRole role)
        {
            switch (role)
            {
                case PartRole.Group:
                case PartRole.Button:
                case PartRole.Dialog:
                case PartRole.TextBox:
                case PartRole.ComboBox:
                case PartRole.ListBox:
                case PartRole.Option:
                case PartRole.Tree:
                case PartRole.TreeItem:
                case PartRole.Slider:
                case PartRole.Window:
                case PartRole.Switch:
                case PartRole.CheckBox:
                case PartRole.Tab:
                case PartRole.Table:
                case PartRole.ColumnHeader:
                case PartRole.Menu:
                case PartRole.MenuItem:
                case PartRole.Meter:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Every rule the model breaks, one line each; empty when it passes.</summary>
        public static List<string> Check(IDescribeParts model)
        {
            var failures = new List<string>();
            var parts = new List<PartDescription>();
            model.Describe(parts);
            string control = string.IsNullOrEmpty(model.Label) ? "(no label)" : model.Label;
            if (string.IsNullOrEmpty(model.Label)) failures.Add("the control has no label");
            int radios = 0, on = 0, focusedRadios = 0, optionsOn = 0, optionsFocused = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                PartDescription p = parts[i];
                for (int j = 0; j < i; j++)
                    if (parts[j].Name == p.Name)
                    {
                        failures.Add($"part {p.Name}: the name is used twice");
                        break;
                    }
                bool needsLabel = p.Focusable || NeedsLabel(p.Role);
                if (needsLabel && string.IsNullOrEmpty(p.Label)) failures.Add($"part {p.Name}: {(p.Focusable ? "focusable" : p.Role.ToString().ToLowerInvariant())} without a label");
                if (p.Focusable && p.Keys.Count == 0) failures.Add($"part {p.Name}: focusable but handles no key");
                if (p.Role == PartRole.Option)
                {
                    if (p.Has(PartFlags.On)) optionsOn++;
                    if (p.Has(PartFlags.Focus)) optionsFocused++;
                }
                if (p.Role != PartRole.Radio && p.Role != PartRole.Tab) continue;
                radios++;
                if (p.Has(PartFlags.On)) on++;
                if (p.Has(PartFlags.Focus)) focusedRadios++;
            }
            if (radios > 0 && on != 1) failures.Add($"radio group \"{control}\": {on} items on, not 1");
            if (focusedRadios > 1) failures.Add($"radio group \"{control}\": {focusedRadios} items focused");
            if (optionsOn > 1) failures.Add($"list \"{control}\": {optionsOn} options on, not at most 1");
            if (optionsFocused > 1) failures.Add($"list \"{control}\": {optionsFocused} options highlighted");
            return failures;
        }

        /// <summary>
        /// The labels two controls of one kind share among <paramref name="shown"/> (the targets of one window on screen together), one
        /// line each; empty when every label names one control of its kind. An agent or an assistive tool that names a control by its
        /// label can't tell such two apart (a twin stage's two "Guided (stage 3)" check boxes). Labels compare as shown, ignoring case.
        /// </summary>
        public static List<string> CheckLabelsUnique(IEnumerable<CommitTarget> shown)
        {
            var failures = new List<string>();
            var seen = new Dictionary<string, int>();
            foreach (CommitTarget t in shown)
            {
                if (t == null) continue;
                ControlInfo c = ControlInfo.Of(t);
                if (string.IsNullOrEmpty(c.Label)) continue;
                string key = c.Kind + "\n" + c.Label.Trim().ToLowerInvariant();
                seen.TryGetValue(key, out int n);
                seen[key] = n + 1;
                if (n == 1) failures.Add($"{c.Kind} \"{c.Label.Trim()}\": two controls of the window have the label");
            }
            return failures;
        }
    }
}
