#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>
    /// One command of the command palette: a stable id, a label read when the palette opens, an English name it is also found by,
    /// what it does, and when it can run.
    /// <list type="bullet">
    /// <item>A command that changes the flight (an autopilot, a maneuver node) asks for a second Enter: <see cref="Confirm"/> gives
    /// it, with the text that says what it will do.</item>
    /// <item>A command can be unavailable now (<see cref="When"/>): it is still listed, greyed, with the reason, and never runs.</item>
    /// <item>A command can be left out of the list now (<see cref="ListedWhen"/>): "Disengage" while nothing is engaged. A command not
    /// listed can't run either, even before the palette's next refresh drops it.</item>
    /// </list>
    /// </summary>
    [NotAWidget("data: one command of the command palette; the palette describes its parts")]
    public sealed class PaletteCommand
    {
        private readonly Func<string>         label;
        private readonly Action<ChangeSource> run;
        private          Func<bool>           available;
        private          Func<string>         reason;
        private          Func<string>         preview;
        private          Func<string>         tag;
        private          Func<bool>           listed;

        /// <param name="id">The stable id (<c>window:MechJebModuleAscentMenu</c>, <c>node:execute-one</c>); unique in a palette.</param>
        /// <param name="label">The label as shown now (localized).</param>
        /// <param name="key">The name it is also found by, language-independent (English); null: none.</param>
        /// <param name="run">What it does, with the source of the Enter or the click that ran it.</param>
        public PaletteCommand(string id, Func<string> label, string key, Action<ChangeSource> run)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("a command needs an id", nameof(id));
            Id         = id;
            this.label = label;
            Key        = key;
            this.run   = run;
        }

        public string Id  { get; }
        public string Key { get; }

        /// <summary>Whether it changes the flight: the palette runs it only on a second Enter (or a second click).</summary>
        public bool ChangesFlight => preview != null;

        /// <summary>The label as shown now (the id when it has none).</summary>
        public string Label
        {
            get
            {
                string s = label?.Invoke();
                return string.IsNullOrEmpty(s) ? Id : s;
            }
        }

        /// <summary>
        /// Whether it can run now: it has a run, it is listed now (<see cref="ListedWhen"/>: a command that lost its meaning between a
        /// refresh and an Enter never runs), and its <see cref="When"/> condition holds (asked only while it is listed).
        /// </summary>
        public bool Available => run != null && Listed && (available == null || available());

        /// <summary>Why it can't run now (null when it can, when it is not listed, or when it gives no reason).</summary>
        public string Reason => Available || !Listed ? null : reason?.Invoke();

        /// <summary>What it will do, shown on the first Enter (null for a command that doesn't change the flight).</summary>
        public string Preview => preview?.Invoke();

        /// <summary>The short text after the label (its kind or state: "Window", "Open", "Autopilot"); null: none.</summary>
        public string Tag => tag?.Invoke();

        /// <summary>Whether it is in the list now.</summary>
        public bool Listed => listed == null || listed();

        /// <summary>It changes the flight: a second Enter runs it, the first shows <paramref name="whatItDoes"/>.</summary>
        public PaletteCommand Confirm(Func<string> whatItDoes)
        {
            preview = whatItDoes ?? throw new ArgumentNullException(nameof(whatItDoes));
            return this;
        }

        /// <summary>It runs only while <paramref name="can"/> holds; <paramref name="why"/> says why not.</summary>
        public PaletteCommand When(Func<bool> can, Func<string> why)
        {
            available = can;
            reason    = why;
            return this;
        }

        /// <summary>The short text after the label.</summary>
        public PaletteCommand Tagged(Func<string> text)
        {
            tag = text;
            return this;
        }

        /// <summary>It is listed only while <paramref name="show"/> holds.</summary>
        public PaletteCommand ListedWhen(Func<bool> show)
        {
            listed = show;
            return this;
        }

        internal void Run(ChangeSource source) => run?.Invoke(source);
    }

    /// <summary>Why a <see cref="PaletteModel"/> closed.</summary>
    public enum PaletteClose
    {
        /// <summary>A command ran (it runs after the palette closed).</summary>
        Ran,

        /// <summary>Escape: nothing ran, a command waiting for its second Enter included.</summary>
        Escape,

        /// <summary>A press outside the palette.</summary>
        OutsidePress,

        /// <summary>What it was opened on went away (the vessel, MechJeb's UI hidden).</summary>
        OwnerClosed,

        /// <summary>The game is changing scene.</summary>
        SceneChange,

        /// <summary>Its host closed it.</summary>
        Program
    }

    /// <summary>What the palette's status line says.</summary>
    public enum PaletteStatus
    {
        /// <summary>The keys: Enter runs, Escape closes.</summary>
        Hint,

        /// <summary>No command matches the query.</summary>
        NoMatch,

        /// <summary>Enter was pressed on a command that can't run: its reason (<see cref="PaletteModel.StatusCommand"/>).</summary>
        Unavailable,

        /// <summary>A command that changes the flight waits for its second Enter: what it will do (<see cref="PaletteModel.Armed"/>).</summary>
        Confirm
    }

    /// <summary>What an Enter (or a click on a row) did.</summary>
    public enum PaletteEnter
    {
        /// <summary>Nothing: no row, or the palette is closed.</summary>
        Nothing,

        /// <summary>The command ran, and the palette closed.</summary>
        Ran,

        /// <summary>The command changes the flight: it waits for a second Enter.</summary>
        Armed,

        /// <summary>The command can't run now: the status says why.</summary>
        Unavailable
    }

    /// <summary>
    /// The command palette, without Unity: a query typed in a field, the commands that match it ranked by <see cref="FuzzyFilter"/>,
    /// a highlight, and the two-step run of the commands that change the flight. Its host draws it, gives it the keys of its keyboard
    /// scope and the clicks on its rows, and closes it from the popup stack.
    /// <list type="bullet">
    /// <item>Opening lists the commands listed now, each id once (a later duplicate is left out), and starts with an empty query.</item>
    /// <item>A query is matched against each command's label (as shown, localized) and its English key; an empty query lists every
    /// command in its order. The ranking is the filter's (score, then the shorter text, then the order), except that every command
    /// that can run comes before every command that can't.</item>
    /// <item>A command that can't run is listed with its reason and never runs: Enter on it shows the reason
    /// (<see cref="PaletteStatus.Unavailable"/>) and the palette stays open.</item>
    /// <item>The highlight starts on the first result after every query; Up and Down move it by one, Page Up and Page Down by
    /// <see cref="Page"/>, clamped at the ends (no wrap).</item>
    /// <item>Enter on a command that doesn't change the flight closes the palette (<see cref="PaletteClose.Ran"/>), then runs it with
    /// the Enter's source, inside <see cref="CommitPath.As"/>, so the edits it makes carry that source.</item>
    /// <item>Enter on a command that changes the flight arms it (<see cref="Armed"/>, <see cref="PaletteStatus.Confirm"/>) and runs
    /// nothing; a second Enter on it runs it, if it can still run (else it is disarmed and the reason shown).</item>
    /// <item>Typing (a new query), moving the highlight, a click on another row, and closing the palette disarm: nothing runs without
    /// the second Enter on the same command. Escape closes the palette (<see cref="PaletteClose.Escape"/>) and runs nothing.</item>
    /// <item>A click on a row is an Enter on it: it highlights the row, then runs, arms or explains it. A pointer click on the armed
    /// row runs it only <see cref="ClickConfirmDelay"/> seconds or more after it was armed (by <see cref="Clock"/>): the two halves of
    /// a double-click arm, never run. Enter and the agent's pick have no delay.</item>
    /// <item><see cref="Refresh"/> reads which commands are listed and can run again (a node made, an autopilot engaged), keeps the
    /// highlighted command highlighted, and disarms a command that can no longer run. It allocates nothing when nothing changed.</item>
    /// <item>Each palette closes once, with its reason (<see cref="LastClose"/>, <see cref="Closed"/>). A closed palette holds no
    /// command (nor what their delegates reach): only the last run's id and label are kept.</item>
    /// </list>
    /// </summary>
    public sealed class PaletteModel : IDescribeParts
    {
        /// <summary>The rows Page Up and Page Down move by.</summary>
        public const int Page = 8;

        /// <summary>
        /// The seconds a pointer click on the armed row waits after the arming before it runs the command: about the system's
        /// double-click time, so a double-click only arms.
        /// </summary>
        public const double ClickConfirmDelay = 0.5;

        /// <summary>The time in seconds, for <see cref="ClickConfirmDelay"/> (null: a click on the armed row never runs it; Enter does).</summary>
        public Func<double> Clock { get; set; }

        private double armedAt;

        private static readonly PartKey[] queryKeys = { PartKey.Up, PartKey.Down, PartKey.PageUp, PartKey.PageDown, PartKey.Enter, PartKey.Escape };

        private readonly List<PaletteCommand> all     = new List<PaletteCommand>();
        private readonly List<PaletteCommand> shown   = new List<PaletteCommand>();
        private readonly List<int>            results = new List<int>();
        private readonly HashSet<string>      ids     = new HashSet<string>(StringComparer.Ordinal);
        private          bool[]               can     = new bool[0];
        private          string[]             labels  = new string[0];
        private          FuzzyFilter          filter;
        private          string               query = "";

        /// <summary>What the palette is called (its title), as shown.</summary>
        public string Label { get; set; }

        /// <summary>What its search field is called, as shown.</summary>
        public string QueryLabel { get; set; } = "Search";

        /// <summary>What its result list is called, as shown.</summary>
        public string ResultsLabel { get; set; } = "Commands";

        public bool IsOpen { get; private set; }

        /// <summary>The query as typed.</summary>
        public string Query => query;

        /// <summary>The commands listed now (open palette), in their order; results index this list.</summary>
        public IReadOnlyList<PaletteCommand> Listed => shown;

        /// <summary>The results, best first, as indices into <see cref="Listed"/>. Owned by the model.</summary>
        public IReadOnlyList<int> Results => results;

        public int ResultCount => results.Count;

        /// <summary>The command of result <paramref name="r"/> (null out of range).</summary>
        public PaletteCommand ResultAt(int r) => r >= 0 && r < results.Count ? shown[results[r]] : null;

        /// <summary>
        /// The label of result <paramref name="r"/> as read when the list was last made (the one the query matched; null out of range).
        /// Reading it allocates nothing, unlike a command's <see cref="PaletteCommand.Label"/>, which asks its owner each time.
        /// </summary>
        public string ResultLabel(int r) => r >= 0 && r < results.Count ? labels[results[r]] : null;

        /// <summary>Whether the command of result <paramref name="r"/> could run when the list was last ranked.</summary>
        public bool ResultAvailable(int r) => r >= 0 && r < results.Count && can[results[r]];

        /// <summary>The highlighted result (-1: none).</summary>
        public int Highlight { get; private set; } = -1;

        /// <summary>The highlighted command (null: none).</summary>
        public PaletteCommand Highlighted => ResultAt(Highlight);

        /// <summary>The command waiting for its second Enter (null: none).</summary>
        public PaletteCommand Armed { get; private set; }

        /// <summary>What the status line says.</summary>
        public PaletteStatus Status { get; private set; }

        /// <summary>The command the status is about: the armed one, or the one that couldn't run (null for a hint).</summary>
        public PaletteCommand StatusCommand { get; private set; }

        /// <summary>Why the last palette closed (valid once one closed).</summary>
        public PaletteClose LastClose { get; private set; }

        /// <summary>The id of the last command run (null: none yet), its label as listed, and the source it ran with.</summary>
        public string LastRunId { get; private set; }

        public string LastRunLabel { get; private set; }

        public ChangeSource LastSource { get; private set; }

        /// <summary>Called when a palette opens.</summary>
        public event Action Opened;

        /// <summary>Called once when the open palette closes, with the reason; the host tears its popup down here.</summary>
        public event Action<PaletteClose> Closed;

        /// <summary>Called after the results changed (a query, a refresh).</summary>
        public event Action ResultsChanged;

        /// <summary>Called after the highlight moved.</summary>
        public event Action HighlightChanged;

        /// <summary>Called after the status (or the command it is about) changed.</summary>
        public event Action StatusChanged;

        /// <summary>Called after a command ran (the palette is closed by then).</summary>
        public event Action<PaletteCommand, ChangeSource> Ran;

        /// <summary>Opens the palette on <paramref name="commands"/>, with an empty query. Opening an open palette starts it again.</summary>
        public void Open(IEnumerable<PaletteCommand> commands)
        {
            all.Clear();
            ids.Clear();
            if (commands != null)
                foreach (PaletteCommand c in commands)
                    if (c != null && ids.Add(c.Id))
                        all.Add(c);
            IsOpen        = true;
            query         = "";
            Armed         = null;
            Highlight     = -1;
            StatusCommand = null;
            Status        = PaletteStatus.Hint;
            ReadListed();
            RebuildFilter();
            ReadAvailable();
            Rank(null);
            Status = results.Count == 0 ? PaletteStatus.NoMatch : PaletteStatus.Hint;
            Opened?.Invoke();
            ResultsChanged?.Invoke();
            HighlightChanged?.Invoke();
            StatusChanged?.Invoke();
        }

        /// <summary>The query typed: the results ranked again, the highlight on the first, an armed command disarmed.</summary>
        public void SetQuery(string q)
        {
            q = q ?? "";
            if (!IsOpen || string.Equals(q, query, StringComparison.Ordinal)) return;
            query = q;
            filter.Apply(query);
            Rank(null);
            SetStatus(results.Count == 0 ? PaletteStatus.NoMatch : PaletteStatus.Hint, null);
            ResultsChanged?.Invoke();
            HighlightChanged?.Invoke();
        }

        /// <summary>Moves the highlight by <paramref name="delta"/>, clamped; disarms. True when it moved.</summary>
        public bool Move(int delta)
        {
            if (!IsOpen || results.Count == 0) return false;
            int to = Highlight < 0 ? 0 : Highlight + delta;
            if (to < 0) to = 0;
            if (to > results.Count - 1) to = results.Count - 1;
            if (Armed != null) SetStatus(PaletteStatus.Hint, null);
            if (to == Highlight) return false;
            Highlight = to;
            if (Status == PaletteStatus.Unavailable) SetStatus(PaletteStatus.Hint, null);
            HighlightChanged?.Invoke();
            return true;
        }

        /// <summary>A key of the palette's scope: Up, Down, Page Up, Page Down, Enter, Escape. True when it did something.</summary>
        public bool Handle(ScopeKeys k, ChangeSource source)
        {
            if (!IsOpen) return false;
            switch (k)
            {
                case ScopeKeys.Up: return Move(-1);
                case ScopeKeys.Down: return Move(1);
                case ScopeKeys.PageUp: return Move(-Page);
                case ScopeKeys.PageDown: return Move(Page);
                case ScopeKeys.Enter: return Enter(source) != PaletteEnter.Nothing;
                case ScopeKeys.Escape:
                    Close(PaletteClose.Escape);
                    return true;
                default: return false;
            }
        }

        /// <summary>Enter on the highlighted command: runs it, arms it, or says why it can't run.</summary>
        public PaletteEnter Enter(ChangeSource source)
        {
            PaletteCommand c = Highlighted;
            if (!IsOpen || c == null) return PaletteEnter.Nothing;
            if (!c.Available)
            {
                SetStatus(PaletteStatus.Unavailable, c);
                return PaletteEnter.Unavailable;
            }
            if (c.ChangesFlight && Armed != c)
            {
                Armed   = c;
                armedAt = Clock != null ? Clock() : 0;
                SetStatus(PaletteStatus.Confirm, c);
                return PaletteEnter.Armed;
            }
            string label = Highlight >= 0 && Highlight < results.Count ? labels[results[Highlight]] : c.Label;
            Close(PaletteClose.Ran);
            LastRunId    = c.Id;
            LastRunLabel = label;
            LastSource   = source;
            using (CommitPath.As(source)) c.Run(source);
            Ran?.Invoke(c, source);
            return PaletteEnter.Ran;
        }

        /// <summary>
        /// A click on result <paramref name="r"/>: it is highlighted, then entered. A second click on an armed command confirms it,
        /// with the source <see cref="ChangeSource.Pointer"/> only once <see cref="ClickConfirmDelay"/> has passed since the arming
        /// (before that it does nothing and the command stays armed).
        /// </summary>
        public PaletteEnter Pick(int r, ChangeSource source)
        {
            if (!IsOpen || r < 0 || r >= results.Count) return PaletteEnter.Nothing;
            if (source == ChangeSource.Pointer && Armed != null && shown[results[r]] == Armed && r == Highlight &&
                (Clock == null || Clock() - armedAt < ClickConfirmDelay))
                return PaletteEnter.Armed;
            if (r != Highlight)
            {
                Highlight = r;
                if (Status != PaletteStatus.Hint && Status != PaletteStatus.NoMatch) SetStatus(PaletteStatus.Hint, null);
                HighlightChanged?.Invoke();
            }
            return Enter(source);
        }

        /// <summary>
        /// Reads again which commands are listed and can run. The highlighted command stays highlighted while it is listed; an armed
        /// command that can't run any more is disarmed, with its reason. True when the results changed.
        /// </summary>
        public bool Refresh()
        {
            if (!IsOpen) return false;
            bool listChanged = ReadListed();
            if (listChanged) RebuildFilter();
            bool availableChanged = ReadAvailable();
            if (Armed != null && !Armed.Available) SetStatus(PaletteStatus.Unavailable, Armed);
            else if (Armed != null && !shown.Contains(Armed)) SetStatus(PaletteStatus.Hint, null);
            if (!listChanged && !availableChanged) return false;
            PaletteCommand kept = Highlighted;
            int before = Highlight;
            Rank(kept);
            if (results.Count == 0 && Status == PaletteStatus.Hint) SetStatus(PaletteStatus.NoMatch, null);
            else if (results.Count > 0 && Status == PaletteStatus.NoMatch) SetStatus(PaletteStatus.Hint, null);
            ResultsChanged?.Invoke();
            if (Highlight != before || Highlighted != kept) HighlightChanged?.Invoke();
            return true;
        }

        /// <summary>Closes the open palette with <paramref name="reason"/> (an armed command is dropped); nothing happens when closed.</summary>
        public void Close(PaletteClose reason)
        {
            if (!IsOpen) return;
            IsOpen        = false;
            LastClose     = reason;
            Armed         = null;
            StatusCommand = null;
            Status        = PaletteStatus.Hint;
            Closed?.Invoke(reason);
            // nothing of the commands (their delegates reach the game's modules and vessel) kept while closed
            if (IsOpen) return;
            all.Clear();
            shown.Clear();
            results.Clear();
            ids.Clear();
            labels    = new string[0];
            can       = new bool[0];
            Highlight = -1;
            filter?.SetItems(labels, labels);
        }

        /// <summary>The palette's reason for a popup layer's dismissal.</summary>
        public static PaletteClose FromDismiss(PopupDismiss d)
        {
            switch (d)
            {
                case PopupDismiss.OutsidePress: return PaletteClose.OutsidePress;
                case PopupDismiss.Escape: return PaletteClose.Escape;
                case PopupDismiss.OwnerClosed:
                case PopupDismiss.ParentClosed:
                    return PaletteClose.OwnerClosed;
                case PopupDismiss.SceneChange: return PaletteClose.SceneChange;
                default: return PaletteClose.Program;
            }
        }

        // ---- inside ----

        private void SetStatus(PaletteStatus s, PaletteCommand about)
        {
            // only Confirm keeps a command armed
            if (s != PaletteStatus.Confirm) Armed = null;
            if (s == Status && about == StatusCommand) return;
            Status        = s;
            StatusCommand = about;
            StatusChanged?.Invoke();
        }

        // the commands listed now, in their order; true when the list changed
        private bool ReadListed()
        {
            int n = 0;
            bool changed = false;
            for (int i = 0; i < all.Count; i++)
            {
                PaletteCommand c = all[i];
                if (!c.Listed) continue;
                if (n < shown.Count)
                {
                    if (shown[n] != c)
                    {
                        shown[n] = c;
                        changed = true;
                    }
                }
                else
                {
                    shown.Add(c);
                    changed = true;
                }
                n++;
            }
            if (shown.Count > n)
            {
                shown.RemoveRange(n, shown.Count - n);
                changed = true;
            }
            return changed;
        }

        private void RebuildFilter()
        {
            labels = new string[shown.Count];
            var keys = new string[shown.Count];
            for (int i = 0; i < shown.Count; i++)
            {
                labels[i] = shown[i].Label;
                keys[i]   = shown[i].Key;
            }
            if (filter == null) filter = new FuzzyFilter(labels, keys);
            else filter.SetItems(labels, keys);
            filter.Apply(query);
            if (can.Length != shown.Count) can = new bool[shown.Count];
        }

        // whether each listed command can run; true when one changed
        private bool ReadAvailable()
        {
            bool changed = false;
            for (int i = 0; i < shown.Count; i++)
            {
                bool a = shown[i].Available;
                if (a == can[i]) continue;
                can[i]  = a;
                changed = true;
            }
            return changed;
        }

        // the filter's matches, those that can run first; the highlight on `keep` when it is still there, else on the first
        private void Rank(PaletteCommand keep)
        {
            results.Clear();
            List<FuzzyResult> m = filter.Results;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < m.Count; i++)
                    if (can[m[i].Index] == (pass == 0))
                        results.Add(m[i].Index);
            int h = results.Count > 0 ? 0 : -1;
            if (keep != null)
                for (int r = 0; r < results.Count; r++)
                    if (shown[results[r]] == keep)
                    {
                        h = r;
                        break;
                    }
            Highlight = h;
        }

        /// <summary>
        /// The palette (a Dialog, Expanded while open), its search field (a TextBox that takes the keyboard: Up, Down, Page Up, Page
        /// Down, Enter, Escape), the result list (a ListBox) and one Option per result (Focus: highlighted; Disabled: can't run now;
        /// named by its command's id), then the status line while it says something about a command (a Status; Invalid when the
        /// command can't run).
        /// </summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription { Name = "palette", Role = PartRole.Dialog, Label = Label, Flags = IsOpen ? PartFlags.Expanded : PartFlags.None });
            into.Add(new PartDescription { Name = "query", Role = PartRole.TextBox, Label = QueryLabel, Focusable = true, Keys = queryKeys });
            into.Add(new PartDescription { Name = "results", Role = PartRole.ListBox, Label = ResultsLabel });
            for (int r = 0; r < results.Count; r++)
            {
                PaletteCommand c = shown[results[r]];
                PartFlags f = PartFlags.None;
                if (r == Highlight) f |= PartFlags.Focus;
                if (!can[results[r]]) f |= PartFlags.Disabled;
                into.Add(new PartDescription { Name = c.Id, Role = PartRole.Option, Label = labels[results[r]], Flags = f });
            }
            if (StatusCommand != null)
                into.Add(new PartDescription
                {
                    Name = "status", Role = PartRole.Status, Label = StatusCommand.Label,
                    Flags = Status == PaletteStatus.Unavailable ? PartFlags.Invalid : PartFlags.None
                });
        }
    }
}
