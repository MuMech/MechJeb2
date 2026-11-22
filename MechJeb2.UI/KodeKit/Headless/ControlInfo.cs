#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Globalization;

namespace KodeKit.Headless
{
    /// <summary>
    /// One control of a window as an agent, a test or an accessibility tree sees it, read from its commit target and the model that
    /// draws it (<see cref="CommitTarget.Model"/>): what kind of control it is, its label and description, its value, its entries and
    /// the actions it takes.
    /// <list type="bullet">
    /// <item>The kind comes from the model when there is one (a <see cref="ToggleModel"/> is a switch or a check box, a
    /// <see cref="ChoiceModel"/> a choice, tabs or a list), else from the target's widget kind.</item>
    /// <item>The label is the model's (as shown), else the target's own, shown through the path's <see cref="CommitPath.ShowLabel"/>;
    /// the description is the target's
    /// (<see cref="CommitTarget.Description"/>, the control's tooltip in KodeKit).</item>
    /// <item>The actions are the model's verbs: flip and set for a switch, select for one of several, nudge and set for a stepper, set
    /// for a field or a slider, next and previous for an arrow selector.</item>
    /// <item>Made when asked: it allocates, so it is never made per frame.</item>
    /// </list>
    /// </summary>
    [NotAWidget("data: one control as an agent sees it, read from its target and model")]
    public sealed class ControlInfo
    {
        /// <summary>switch, checkbox, choice, tabs, list, switch3, stepper, text, inline, combo, slider, cycle, grid, chip, items, colour, or the widget kind in lower case.</summary>
        public string Kind;

        /// <summary>The control's label as shown (null: none).</summary>
        public string Label;

        /// <summary>What the control does (null: no tooltip).</summary>
        public string Description;

        /// <summary>The value as text (a choice: its entry's text; a switch: true or false).</summary>
        public string Value;

        /// <summary>The entries of a choice, a drop-down or a three-way switch (null for the others).</summary>
        public string[] Items;

        /// <summary>The rows a list's add takes from (its registry), for a list (null for the others).</summary>
        public string[] Sources;

        /// <summary>The actions the control takes.</summary>
        public string[] Actions;

        /// <summary>False while the model refuses a change (a disabled control).</summary>
        public bool Enabled = true;

        /// <summary>The target's widget kind (<see cref="CommitTarget.Kind"/>).</summary>
        public string Widget;

        /// <summary>The target's id (<see cref="CommitTarget.Id"/>; null: none) and its key in this window instance.</summary>
        public string Id, Key;

        static readonly string[] SwitchActions = { "flip", "set" };
        static readonly string[] SelectActions = { "select" };
        static readonly string[] CycleActions = { "select", "next", "previous" };
        static readonly string[] StepperActions = { "nudge", "set" };
        static readonly string[] SetActions = { "set" };
        static readonly string[] ListActions = { "select", "move", "add" };
        static readonly string[] NoActions = new string[0];

        /// <summary>The control behind <paramref name="t"/>.</summary>
        public static ControlInfo Of(CommitTarget t)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            var c = new ControlInfo { Widget = t.Kind, Id = t.Id, Key = t.Key, Description = t.Description };
            object v = t.Read();
            switch (t.Model)
            {
                case ToggleModel m:
                    c.Kind = m.Kind == ToggleKind.CheckBox ? "checkbox" : "switch";
                    c.Label = m.Label;
                    c.Value = m.On ? "true" : "false";
                    c.Actions = SwitchActions;
                    c.Enabled = m.Value.Enabled;
                    break;
                case Switch3Model m:
                    c.Kind = "switch3";
                    c.Label = m.Label;
                    c.Items = m.Names;
                    c.Value = m.ShownName;
                    c.Actions = SelectActions;
                    c.Enabled = m.Value.Enabled;
                    break;
                case ChoiceModel m:
                    c.Kind = m.ItemRole == PartRole.Tab ? "tabs" : m.ItemRole == PartRole.Option ? "list" : "choice";
                    c.Label = m.Label;
                    c.Items = m.ItemLabels;
                    c.Value = ItemText(m.ItemLabels, m.Shown);
                    c.Actions = SelectActions;
                    break;
                case StepperModel m:
                    c.Kind = "stepper";
                    c.Label = m.Label;
                    c.Value = m.Text;
                    c.Actions = StepperActions;
                    break;
                case InlineEditModel m:
                    c.Kind = "inline";
                    c.Label = m.Label;
                    c.Value = m.Value;
                    c.Actions = SetActions;
                    c.Enabled = m.Enabled;
                    break;
                case TextFieldModel m:
                    c.Kind = "text";
                    c.Label = m.Label;
                    c.Value = m.Value;
                    c.Actions = SetActions;
                    c.Enabled = m.Enabled;
                    break;
                case ComboModel m:
                    c.Kind = "combo";
                    c.Label = m.Label;
                    c.Items = m.Entries;
                    c.Value = v is int i ? ItemText(c.Items, i) : Text(v);
                    c.Actions = SelectActions;
                    break;
                case SliderModel m:
                    c.Kind = "slider";
                    c.Label = m.Label;
                    c.Value = m.Shown.ToString("G4", CultureInfo.InvariantCulture);
                    c.Actions = SetActions;
                    c.Enabled = m.Value.Enabled;
                    break;
                case ListEdits e:
                    c.Kind = "items";
                    c.Items = e.Rows();
                    c.Sources = e.SourceRows();
                    int sel = e.Selected?.Invoke() ?? -1;
                    c.Value = c.Items != null && sel >= 0 && sel < c.Items.Length ? c.Items[sel] : "";
                    c.Actions = ListActions;
                    break;
                default:
                    c.Kind = KindOf(t.Kind);
                    c.Value = Text(v);
                    c.Actions = ActionsOf(c.Kind);
                    break;
            }
            // a widget with no model (a chip, an arrow selector, a selection grid, a list): the target's label, as shown
            if (string.IsNullOrEmpty(c.Label)) c.Label = t.Label != null && t.Path.ShowLabel != null ? t.Path.ShowLabel(t.Label) ?? t.Label : t.Label;
            return c;
        }

        /// <summary>
        /// The kind <see cref="Of"/> gives <paramref name="t"/>, read from its model alone: nothing is formatted or listed (a change
        /// feed records it on every commit, a slider's drag included).
        /// </summary>
        public static string KindOf(CommitTarget t)
        {
            if (t == null) return "";
            switch (t.Model)
            {
                case ToggleModel m: return m.Kind == ToggleKind.CheckBox ? "checkbox" : "switch";
                case Switch3Model _: return "switch3";
                case ChoiceModel m: return m.ItemRole == PartRole.Tab ? "tabs" : m.ItemRole == PartRole.Option ? "list" : "choice";
                case StepperModel _: return "stepper";
                case InlineEditModel _: return "inline";
                case TextFieldModel _: return "text";
                case ComboModel _: return "combo";
                case SliderModel _: return "slider";
                case ListEdits _: return "items";
                default: return KindOf(t.Kind);
            }
        }

        static string KindOf(string widget)
        {
            switch (widget)
            {
                case "ArrowSelector": return "cycle";
                case "SelectionGrid": return "grid";
                case "Chip": return "chip";
                case "ItemList": return "items";
                case "ColorPicker": return "colour";
                default: return (widget ?? "").ToLowerInvariant();
            }
        }

        static string[] ActionsOf(string kind)
        {
            switch (kind)
            {
                case "chip": return SwitchActions;
                case "cycle": return CycleActions;
                case "grid": return SelectActions;
                case "colour": return NoActions;
                default: return NoActions;
            }
        }

        static string ItemText(string[] items, int i) =>
            items != null && i >= 0 && i < items.Length ? items[i] : i.ToString(CultureInfo.InvariantCulture);

        static string Text(object v)
        {
            if (v == null) return "";
            if (v is bool b) return b ? "true" : "false";
            if (v is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);
            return v.ToString();
        }
    }
}
