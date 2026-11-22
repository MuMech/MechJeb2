#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>
    /// Who asked for a change (ui-sdk's change reason): a click, a key, an agent or test, the program, or an undo stack writing
    /// back a value it recorded (an undo or a redo: <see cref="UndoStack"/> records no entry for it).
    /// </summary>
    public enum ChangeSource { Pointer, Keyboard, Agent, Program, Undo }

    /// <summary>The keys a choice understands, as its host reports them.</summary>
    public enum ChoiceKey { Left, Right, Home, End, Enter, Space, Up, Down }

    /// <summary>
    /// One choice out of several, without Unity: the behaviour of KodeKit's <c>Segmented</c> (joined bar, tabs, pills). The
    /// model owns the value; this pulls it with <see cref="Sync"/> (once a frame) and pushes the user's picks with
    /// <see cref="Select"/>.
    /// <list type="bullet">
    /// <item>A pick of the entry already selected still calls the setter, as a click always did: some setters act on it
    /// (the Translatron's OFF releases the throttle when picked again).</item>
    /// <item><see cref="Allow"/> can refuse a pick (an autopilot that owns the setting); a refused pick changes nothing.</item>
    /// <item>A model value outside the entries selects nothing (<see cref="Shown"/> is -1).</item>
    /// <item>The setter can take the pick's source: the commit it records then has the source <see cref="LastSource"/> shows.</item>
    /// <item><see cref="ShownChanged"/> fires whenever the shown entry changes, by a pick or by <see cref="Sync"/>: the host
    /// repaints on it, so a pick shows at once.</item>
    /// <item>Keys follow a radio group: Left/Right (Up/Down for a list's options) move and wrap, Home/End jump, each move
    /// selects; Enter/Space pick the focused entry. The focus starts on the shown entry.</item>
    /// <item>It is drawn as a radio group (<c>Segmented</c>), a tab list (<c>Tabs</c>: <see cref="PartRole.Tab"/>) or a list
    /// box (<c>MasterDetail</c>'s list: <see cref="PartRole.Option"/>, where nothing chosen is allowed): <see cref="ItemRole"/>
    /// says which, for <see cref="IDescribeParts"/>.</item>
    /// </list>
    /// </summary>
    public sealed class ChoiceModel : IDescribeParts
    {
        readonly Func<int> get;
        readonly Action<int, ChangeSource> set;

        public ChoiceModel(int count, Func<int> get, Action<int> set)
            : this(count, get, Plain(set))
        {
        }

        /// <summary>A choice whose setter also takes the pick's source (KodeKit passes a <see cref="CommitTarget{T}.Set"/>).</summary>
        public ChoiceModel(int count, Func<int> get, Action<int, ChangeSource> set)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            this.get = get ?? throw new ArgumentNullException(nameof(get));
            this.set = set ?? throw new ArgumentNullException(nameof(set));
            Sync();
        }

        static Action<int, ChangeSource> Plain(Action<int> set)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            return (i, source) => set(i);
        }

        public int Count { get; }

        /// <summary>What the choice is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The entries' texts, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string[] ItemLabels { get; set; }

        /// <summary>
        /// How the entries are drawn, for <see cref="Describe"/>: <see cref="PartRole.Radio"/> (a segmented bar, the default),
        /// <see cref="PartRole.Tab"/> (a tab list) or <see cref="PartRole.Option"/> (a list box: its group is a
        /// <see cref="PartRole.ListBox"/>).
        /// </summary>
        public PartRole ItemRole { get; set; } = PartRole.Radio;

        /// <summary>The entry drawn as selected, from the model's last value: -1 when that value is not an entry.</summary>
        public int Shown { get; private set; } = -1;

        /// <summary>The entry the keys act on: the shown one until a key moves it (-1: none).</summary>
        public int Focus { get; private set; } = -1;

        /// <summary>When set, asked before each pick; false refuses it.</summary>
        public Func<int, ChangeSource, bool> Allow;

        /// <summary>The source of the last pick that reached the setter (for an observer, ui-sdk's change source).</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>
        /// The shown entry changed, by a pick or by <see cref="Sync"/> (not raised by the constructor's first sync): the host
        /// repaints the entries.
        /// </summary>
        public event Action ShownChanged;

        public bool IsOn(int i) => i == Shown;

        /// <summary>Pulls the model's value. True when the shown entry changed (<see cref="ShownChanged"/> fired).</summary>
        public bool Sync()
        {
            int v = get();
            int s = v >= 0 && v < Count ? v : -1;
            if (s == Shown) return false;
            Shown = s;
            if (Focus < 0 || Focus >= Count) Focus = s;
            ShownChanged?.Invoke();
            return true;
        }

        /// <summary>Picks entry <paramref name="i"/>. True when the setter ran (also for the entry already selected).</summary>
        public bool Select(int i, ChangeSource source)
        {
            if (i < 0 || i >= Count) return false;
            if (Allow != null && !Allow(i, source)) return false;
            Focus = i;
            LastSource = source;
            set(i, source);
            Sync();
            return true;
        }

        /// <summary>A key on the choice. True when the choice handled it.</summary>
        public bool Key(ChoiceKey key)
        {
            if (Count == 0) return false;
            int at = Focus >= 0 ? Focus : Math.Max(Shown, 0);
            switch (key)
            {
                case ChoiceKey.Left:
                case ChoiceKey.Up:
                    return Move((at - 1 + Count) % Count);
                case ChoiceKey.Right:
                case ChoiceKey.Down:
                    return Move((at + 1) % Count);
                case ChoiceKey.Home: return Move(0);
                case ChoiceKey.End: return Move(Count - 1);
                case ChoiceKey.Enter:
                case ChoiceKey.Space:
                    Select(at, ChangeSource.Keyboard);
                    return true;
                default: return false;
            }
        }

        bool Move(int i)
        {
            Focus = i;
            Select(i, ChangeSource.Keyboard);
            return true;
        }

        static readonly PartKey[] ItemKeys = { PartKey.Left, PartKey.Right, PartKey.Home, PartKey.End, PartKey.Enter, PartKey.Space };
        static readonly PartKey[] OptionKeys = { PartKey.Up, PartKey.Down, PartKey.Home, PartKey.End, PartKey.Enter, PartKey.Space };

        /// <summary>The group (a list box for options) and one item per entry, in <see cref="ItemRole"/>.</summary>
        public void Describe(List<PartDescription> into)
        {
            bool list = ItemRole == PartRole.Option;
            into.Add(new PartDescription { Name = "group", Role = list ? PartRole.ListBox : PartRole.Group, Label = Label });
            for (int i = 0; i < Count; i++)
                into.Add(new PartDescription
                {
                    Name = "item." + i,
                    Role = ItemRole,
                    Label = ItemLabels != null && i < ItemLabels.Length ? ItemLabels[i] : null,
                    Flags = (IsOn(i) ? PartFlags.On : PartFlags.None) | (i == Focus ? PartFlags.Focus : PartFlags.None),
                    Focusable = true,
                    Keys = list ? OptionKeys : ItemKeys
                });
        }
    }
}
