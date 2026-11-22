#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>How an on/off value is drawn.</summary>
    public enum ToggleKind { Switch, CheckBox }

    /// <summary>
    /// An on/off value, without Unity: the behaviour of KodeKit's <c>Switch</c> (also in <c>SwitchRow</c>, <c>Toggle</c> and a property
    /// grid's switch rows) and <c>CheckBox</c>, on a <see cref="ValueModel{T}"/>.
    /// <list type="bullet">
    /// <item><see cref="Set"/> (the drawn toggle's change) and <see cref="Flip"/> (an agent, a key) call the setter through the value
    /// model with their source: one setter call, one commit, per change. A click and an agent's flip run the same code.</item>
    /// <item><see cref="Flip"/> turns the value as it is now (the getter), not as last drawn: a value changed since the last sync
    /// still flips to the other state.</item>
    /// <item>Refused (read-only, or <see cref="ValueModel{T}.Allow"/>) changes nothing and calls nothing. The host draws
    /// <see cref="On"/> again after every set, which puts back a toggle uGUI already moved when the set was refused or the setter
    /// kept the old value (no <see cref="ValueModel{T}.ShownChanged"/> then).</item>
    /// <item>What is drawn is <see cref="On"/>, the value as last read: a setter that keeps the old state shows it at once.</item>
    /// <item>Space and Enter flip it (<see cref="Key"/>), for a host that routes keys through a keyboard scope; KodeKit routes none
    /// (a selected toggle doesn't take KSP's keyboard).</item>
    /// </list>
    /// </summary>
    public sealed class ToggleModel : IDescribeParts
    {
        /// <summary>A toggle on <paramref name="get"/> and <paramref name="set"/>, drawn as <paramref name="kind"/>.</summary>
        public ToggleModel(ToggleKind kind, Func<bool> get, Action<bool, ChangeSource> set, Func<bool> enabled = null)
        {
            Kind = kind;
            Value = new ValueModel<bool>(get, set, enabled);
        }

        /// <summary>The value, its source and its refusals.</summary>
        public ValueModel<bool> Value { get; }

        /// <summary>How the value is drawn (its role in <see cref="Describe"/>).</summary>
        public ToggleKind Kind { get; }

        /// <summary>What the toggle is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The state drawn.</summary>
        public bool On => Value.Shown;

        /// <summary>Sets the state (the drawn toggle changed). False when refused.</summary>
        public bool Set(bool on, ChangeSource source) => Value.Set(on, source);

        /// <summary>Turns the value to the other state. False when refused.</summary>
        public bool Flip(ChangeSource source) => Value.Set(!Value.Value, source);

        /// <summary>A key on the toggle: Space and Enter flip it. True when handled.</summary>
        public bool Key(PartKey key)
        {
            if (key != PartKey.Space && key != PartKey.Enter) return false;
            Flip(ChangeSource.Keyboard);
            return true;
        }

        /// <summary>Pulls the value once a frame. True when the drawn state changed.</summary>
        public bool Sync() => Value.Sync();

        /// <summary>The switch or check box: On when on, Disabled while read-only.</summary>
        public void Describe(List<PartDescription> into)
        {
            PartFlags f = (On ? PartFlags.On : PartFlags.None) | (Value.Enabled ? PartFlags.None : PartFlags.Disabled);
            into.Add(new PartDescription { Name = Kind == ToggleKind.Switch ? "switch" : "checkbox", Role = Kind == ToggleKind.Switch ? PartRole.Switch : PartRole.CheckBox, Label = Label, Flags = f });
        }
    }

    /// <summary>
    /// A switch with a few positions (three in KodeKit's <c>Switch3</c>: left, middle, right), without Unity, on a
    /// <see cref="ValueModel{T}"/> of the position's index.
    /// <list type="bullet">
    /// <item><see cref="Pick"/> (a click on a third of the track, an agent) sets the position through the value model: one setter call
    /// and one commit. Picking the position already shown calls nothing (a click on the knob's own third does nothing).</item>
    /// <item>A position outside the range is refused.</item>
    /// <item><see cref="Shown"/> is what the knob shows, the value as last read; a value outside the positions has no position on
    /// (<see cref="ShownName"/> is empty).</item>
    /// <item>Left and Right move by one (no wrap), Home and End jump (<see cref="Key"/>), for a host that routes keys through a
    /// keyboard scope; KodeKit routes none.</item>
    /// </list>
    /// </summary>
    public sealed class Switch3Model : IDescribeParts
    {
        /// <summary>A switch of <paramref name="names"/>.Length positions on <paramref name="get"/> and <paramref name="set"/>.</summary>
        public Switch3Model(string[] names, Func<int> get, Action<int, ChangeSource> set, Func<bool> enabled = null)
        {
            Names = names ?? throw new ArgumentNullException(nameof(names));
            if (names.Length < 2) throw new ArgumentOutOfRangeException(nameof(names));
            Value = new ValueModel<int>(get, set, enabled);
        }

        /// <summary>The value, its source and its refusals.</summary>
        public ValueModel<int> Value { get; }

        /// <summary>The positions' names (shown beside the switch).</summary>
        public string[] Names { get; }

        /// <summary>The number of positions.</summary>
        public int Count => Names.Length;

        /// <summary>What the switch is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The position drawn, the value as last read (it may be outside the positions).</summary>
        public int Shown => Value.Shown;

        /// <summary>The name of the position drawn ("" outside the positions).</summary>
        public string ShownName => Shown >= 0 && Shown < Count ? Names[Shown] : "";

        /// <summary>Moves to position <paramref name="i"/>. False when it is already shown, outside the positions, or refused.</summary>
        public bool Pick(int i, ChangeSource source)
        {
            if (i < 0 || i >= Count || i == Value.Shown) return false;
            return Value.Set(i, source);
        }

        /// <summary>A key: Left/Right move by one, Home/End jump. True when handled.</summary>
        public bool Key(PartKey key)
        {
            int at = Math.Max(0, Math.Min(Count - 1, Shown));
            switch (key)
            {
                case PartKey.Left: return Pick(at - 1, ChangeSource.Keyboard) || true;
                case PartKey.Right: return Pick(at + 1, ChangeSource.Keyboard) || true;
                case PartKey.Home: return Pick(0, ChangeSource.Keyboard) || true;
                case PartKey.End: return Pick(Count - 1, ChangeSource.Keyboard) || true;
                default: return false;
            }
        }

        /// <summary>Pulls the value once a frame. True when the drawn position changed.</summary>
        public bool Sync() => Value.Sync();

        /// <summary>The group and one radio item per position (On: the position shown).</summary>
        public void Describe(List<PartDescription> into)
        {
            PartFlags disabled = Value.Enabled ? PartFlags.None : PartFlags.Disabled;
            into.Add(new PartDescription { Name = "group", Role = PartRole.Group, Label = Label, Flags = disabled });
            for (int i = 0; i < Count; i++)
                into.Add(new PartDescription { Name = "position." + i, Role = PartRole.Radio, Label = Names[i], Flags = disabled | (i == Shown ? PartFlags.On : PartFlags.None) });
        }
    }
}
