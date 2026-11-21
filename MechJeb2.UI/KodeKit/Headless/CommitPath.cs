#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>
    /// Who made a change, whatever the device: the player (a click and a key are the same person), an agent or a test, or the
    /// program. Rules about people (an undo by source, the redo a new edit drops, a conflict) go by actor, not by source.
    /// </summary>
    public enum ChangeActor { User, Agent, Program }

    /// <summary>Sources and their actors.</summary>
    public static class ChangeSources
    {
        /// <summary>The actor behind a source: Pointer and Keyboard are the player; any other source is the program.</summary>
        public static ChangeActor Actor(ChangeSource source)
        {
            switch (source)
            {
                case ChangeSource.Pointer:
                case ChangeSource.Keyboard:
                    return ChangeActor.User;
                case ChangeSource.Agent:
                    return ChangeActor.Agent;
                default:
                    return ChangeActor.Program;
            }
        }
    }

    /// <summary>
    /// One change a control made to its value: which control, the value before and after (read from the control's getter), what
    /// was asked of the setter, and who asked (ui-sdk's <c>createCommit</c>). A grouped gesture (a slider drag, a held repeat
    /// button, a field's typing) is one commit whose <see cref="New"/> and <see cref="Count"/> follow the gesture.
    /// <list type="bullet">
    /// <item>While <see cref="Open"/>, the commit is live: each change of its gesture updates it. Once settled
    /// (<see cref="CommitPath.Settled"/>), none of its fields change again.</item>
    /// <item>An observer that keeps what it saw (a change feed, an input history) copies the fields, or keeps settled commits only:
    /// a kept commit keeps its target, and through it the window's controls, alive.</item>
    /// </list>
    /// </summary>
    [NotAWidget("data: one change on a commit path")]
    public sealed class Commit
    {
        /// <summary>The control that made the change.</summary>
        public CommitTarget Target { get; internal set; }

        /// <summary>The control's value before the change (before the first change of a group).</summary>
        public object Old { get; internal set; }

        /// <summary>The control's value after the change, read back from its getter: what the model kept (a field's normalised text).</summary>
        public object New { get; internal set; }

        /// <summary>What was given to the setter: the value, a typed text, or a stepper's delta.</summary>
        public object Asked { get; internal set; }

        /// <summary>The device or origin of the change (Pointer, Keyboard, Agent, Program).</summary>
        public ChangeSource Source { get; internal set; }

        /// <summary>Who made the change: the actor of <see cref="Source"/>; for an undo's commit, who undid (the player, an agent).</summary>
        public ChangeActor Actor { get; internal set; }

        /// <summary>The number of setter calls in this commit: 1, more for a grouped gesture.</summary>
        public int Count { get; internal set; }

        /// <summary>The path's tick (the frame) when the commit was made.</summary>
        public int Tick { get; internal set; }

        /// <summary>The commit's place in its path: 1 for the path's first.</summary>
        public long Seq { get; internal set; }

        /// <summary>True while the target's group is open and further changes join this commit; false once it is settled.</summary>
        public bool Open { get; internal set; }

        /// <summary>
        /// The target's <see cref="CommitTarget.State"/> before the change (before the first change of a group), when it has one
        /// (null otherwise): what an undo restores.
        /// </summary>
        public object Before { get; internal set; }

        /// <summary>The target's <see cref="CommitTarget.State"/> after the change (after the latest change of a group), or null.</summary>
        public object After { get; internal set; }

        /// <summary>
        /// The target's previous commit, which this one carries on (a slider's track click, made before its drag's group opened): this
        /// commit's <see cref="Old"/> and <see cref="Before"/> are that one's, and that one stays as it was. Null for most commits.
        /// </summary>
        public Commit Replaces { get; internal set; }

        /// <summary>
        /// Made while another target's setter on the same path was running: a side effect of that change (a setter that writes another
        /// control through its target). Observers see it, before the outer commit (its <see cref="Seq"/> is lower); an undo stack records
        /// only the outer change, whose undo runs the outer setter again.
        /// </summary>
        public bool Nested { get; internal set; }

        /// <summary>
        /// False when the setter left the value as it was (a pick of the entry already selected), and the target's state too: a
        /// button that moves a node changes its state, not its field's text.
        /// </summary>
        public bool Changed => !Equals(Old, New) || !Equals(Before, After);

        public override string ToString() => $"{Target?.Key}: {Old} -> {New} ({Source}, x{Count})";
    }

    /// <summary>
    /// What an undo restores for a target whose value is not the whole of its change (<see cref="CommitTarget.State"/>): a node
    /// editor's field writes a text, and the edit is the node's burn and time.
    /// </summary>
    public interface IUndoState
    {
        /// <summary>
        /// The state now, taken before and after each change and before an undo (for its redo). States are compared with
        /// <c>Equals</c> (<see cref="Commit.Changed"/>): give them value equality.
        /// </summary>
        object Take();

        /// <summary>Puts a state back. False, with nothing changed, when it can't be (the node it described was executed or deleted).</summary>
        bool Restore(object state);
    }

    /// <summary>
    /// A control on a <see cref="CommitPath"/>: its kind, a key (kind and order in this window instance), an optional
    /// label, the drawn object, and how to read and write its value.
    /// <list type="bullet">
    /// <item>A group (<see cref="BeginGroup"/>/<see cref="EndGroup"/>, nestable) makes the changes made while it is open one commit,
    /// as long as their source stays the same.</item>
    /// <item><see cref="BeginGroup"/> with <c>adoptSameTick</c> also carries on the target's last commit when it is the path's last and
    /// was made in the same tick: a press that changed the value before the group's handler heard of it (a slider's track click).
    /// The group's commit starts from that commit's old value and names it in <see cref="Commit.Replaces"/>; the earlier commit,
    /// already settled, is not changed.</item>
    /// <item><see cref="ContinueGroup"/> carries on a given commit the same way: the same field's, in a window rebuilt during the edit.</item>
    /// <item>The group's commit settles when the group's last <see cref="EndGroup"/> runs (or its source changes).</item>
    /// <item><see cref="Write"/> sets the value through the path, as a click would: an undo or an agent records a commit too.</item>
    /// </list>
    /// </summary>
    public abstract class CommitTarget
    {
        internal int holds;
        internal Commit open;
        internal Commit last;
        // the commit a group opened with adoptSameTick carries on
        internal Commit adopted;

        protected CommitTarget(CommitPath path, string kind, int index, string label, bool persist)
        {
            Path = path;
            Kind = kind ?? "";
            Index = index;
            Label = label;
            Persist = persist;
            Undoable = persist;
        }

        public CommitPath Path { get; }

        /// <summary>The widget kind ("Toggle", "Segmented", "Field").</summary>
        public string Kind { get; }

        /// <summary>The target's order among the path's targets of the same kind.</summary>
        public int Index { get; }

        /// <summary>
        /// The control's label as its factory got it: a '#key' (localized where it is shown) or a text; null when the widget has none.
        /// The model's <see cref="IDescribeParts.Label"/> is the same label as shown.
        /// </summary>
        public string Label { get; set; }

        string description;

        /// <summary>
        /// What the control does, for an agent or an accessibility tree: the one a view set, else the path's
        /// <see cref="CommitPath.Describer"/> (KodeKit: the control's tooltip, in the language now); null when it has none. Read when
        /// asked: nothing is looked up while the window is built.
        /// </summary>
        public string Description
        {
            get => description ?? Path.Describer?.Invoke(this);
            set => description = value;
        }

        /// <summary>
        /// A name for the control that a rebuilt window gives the same control (unlike <see cref="Key"/>): by default its label (a
        /// localization key or a text) where no other target of its scope has it, after the lazy panel's scope it was built in
        /// (<see cref="CommitPath.NameScope"/>), or one the view sets. Null: none. With the
        /// path's <see cref="CommitPath.Name"/> (the window), it names the control for an observer of several windows.
        /// </summary>
        public string Id { get; set; }

        /// <summary>A view's mark on the target (the planner tags the node executor's settings), for a guard or a host; null: none.</summary>
        public string Tag { get; set; }

        /// <summary>True once the path let the target go (<see cref="CommitPath.Remove"/>): its control is gone, and writes are refused.</summary>
        public bool Removed { get; internal set; }

        /// <summary>The drawn control (a uGUI component), for a host that maps controls to targets.</summary>
        public object Control { get; set; }

        /// <summary>
        /// The headless model that draws the control (a <see cref="ChoiceModel"/>, a <see cref="StepperModel"/>), for a host that
        /// describes controls (<see cref="IDescribeParts"/>: a test, an agent); null: the widget has none.
        /// </summary>
        public object Model { get; set; }

        /// <summary>When true, each change calls the path's <see cref="CommitPath.Edited"/> (the window's owner saves its settings).</summary>
        public bool Persist { get; }

        /// <summary>
        /// Whether an undo stack records this target's commits. True for a target that persists (a setting), false for one that only
        /// navigates (a master-detail's selected row, a colour picker's preview).
        /// </summary>
        public bool Undoable { get; set; }

        /// <summary>
        /// What an undo restores, when the control's value is not the whole of what it changes: a node editor's Δv field writes
        /// a text, and the edit is the node's burn. Null: an undo writes the old value back through <see cref="Write"/>.
        /// </summary>
        public IUndoState State { get; set; }

        /// <summary>
        /// Puts back a state <see cref="State"/> took, through the path (a commit with <paramref name="source"/>). False, with
        /// nothing changed, when the target has no state or the state refuses (what it described is gone).
        /// </summary>
        public bool Restore(object state, ChangeSource source)
        {
            if (State == null || Removed) return false;
            object before = State.Take();
            object old = Read();
            Path.setting++;
            try
            {
                if (!State.Restore(state)) return false;
            }
            finally
            {
                Path.setting--;
            }
            Path.Record(this, old, Read(), state, source, before, State.Take());
            return true;
        }

        /// <summary>
        /// "Kind#Index", unique in the path. Keys belong to one window instance: a lazily built panel (LazyPanel, Section)
        /// numbers its widgets in the order it is first shown, so a rebuilt window can give the same widget another key.
        /// </summary>
        public string Key => Kind + "#" + Index;

        /// <summary>The target's latest commit (null before the first).</summary>
        public Commit Last => last;

        public bool Grouping => holds > 0;

        /// <summary>The value now, boxed.</summary>
        public abstract object Read();

        /// <summary>
        /// Sets the value through the path (an undo, an agent). False when the value is not of the target's type, or the target was
        /// removed.
        /// </summary>
        public abstract bool Write(object value, ChangeSource source);

        /// <summary>Opens a group: the changes until the matching <see cref="EndGroup"/> make one commit.</summary>
        public void BeginGroup(bool adoptSameTick = false)
        {
            if (holds++ > 0) return;
            adopted = adoptSameTick && last != null && last == Path.Last && last.Tick == Path.Now ? last : null;
        }

        /// <summary>
        /// Opens a group that carries on <paramref name="from"/>, the same control's commit in a window that was rebuilt while it was
        /// being edited (<see cref="FocusRecovery"/>): the group's commit starts from that commit's old value and names it in
        /// <see cref="Commit.Replaces"/>, as <c>adoptSameTick</c> does, whatever its tick or path. Null, or a group already open: a
        /// plain <see cref="BeginGroup"/>.
        /// </summary>
        public void ContinueGroup(Commit from)
        {
            BeginGroup();
            if (holds == 1 && from != null && from.Target != this) adopted = from;
        }

        /// <summary>Closes a group (extra calls do nothing); its commit settles on the last one.</summary>
        public void EndGroup()
        {
            if (holds == 0) return;
            if (--holds > 0) return;
            adopted = null;
            Commit c = open;
            open = null;
            if (c != null) Path.Settle(c);
        }
    }

    /// <summary>A target whose value is a <typeparamref name="T"/>, read and written through the widget's delegates.</summary>
    [NotAWidget("a control's entry on its window's commit path; the widget's model (CommitTarget.Model) describes the control")]
    public sealed class CommitTarget<T> : CommitTarget
    {
        readonly Func<T> get;
        readonly Action<T> set;

        internal CommitTarget(CommitPath path, string kind, int index, Func<T> get, Action<T> set, string label, bool persist)
            : base(path, kind, index, label, persist)
        {
            this.get = get ?? throw new ArgumentNullException(nameof(get));
            this.set = set ?? throw new ArgumentNullException(nameof(set));
        }

        public T Value => get();

        public override object Read() => get();

        /// <summary>Calls the setter with <paramref name="value"/> and records the commit.</summary>
        public void Set(T value, ChangeSource source)
        {
            object before = State?.Take();
            T old = get();
            Path.setting++;
            try
            {
                set(value);
            }
            finally
            {
                Path.setting--;
            }
            Path.Record(this, old, get(), value, source, before, State?.Take());
        }

        /// <summary>
        /// Runs <paramref name="act"/> with <paramref name="asked"/> (a change that is not a plain set: a stepper's delta) and records
        /// the value before and after.
        /// </summary>
        public void Apply<TA>(TA asked, Action<TA> act, ChangeSource source)
        {
            object before = State?.Take();
            T old = get();
            Path.setting++;
            try
            {
                act(asked);
            }
            finally
            {
                Path.setting--;
            }
            Path.Record(this, old, get(), asked, source, before, State?.Take());
        }

        /// <inheritdoc />
        /// <remarks>Null is a value for a reference type (a field whose getter returned null can be written back).</remarks>
        public override bool Write(object value, ChangeSource source)
        {
            if (Removed) return false;
            if (value is T v)
            {
                Set(v, source);
                return true;
            }
            if (value != null || default(T) != null) return false;
            Set(default(T), source);
            return true;
        }
    }

    /// <summary>
    /// The one path every edit of a window's controls takes (ui-sdk's commit with a change source): the widget's setter, then the
    /// owner's <see cref="Edited"/>, then a <see cref="Commit"/> for the observers (the agent bridge's change feed, an undo stack).
    /// <list type="bullet">
    /// <item>Every setter call is a commit, also one that leaves the value as it was (<see cref="Commit.Changed"/> is false): some
    /// setters act when called (the Translatron's OFF picked again).</item>
    /// <item>A change inside an open group joins the group's commit: <see cref="Committed"/> fires again with the same commit, its
    /// <see cref="Commit.New"/> and <see cref="Commit.Count"/> updated. An observer that keeps commits tells a new one from a
    /// continued one by reference.</item>
    /// <item>The source comes from the widget (a click: Pointer, typing: Keyboard), unless a host runs the widget's own handler inside
    /// <see cref="As"/> (an agent clicking through the bridge): <see cref="Resolve"/> gives that source then.</item>
    /// <item>The path keeps the last <see cref="Capacity"/> commits in <see cref="Log"/>.</item>
    /// <item>A view that destroys a control (a row of a list) removes its target (<see cref="Remove"/>): the path keeps no closure of
    /// a destroyed control, and observers (an undo stack) drop what they hold for it.</item>
    /// <item>A setter that throws records nothing and leaves the owner's <see cref="Edited"/> uncalled, as before the path.</item>
    /// <item>An observer that throws doesn't stop the others or the widget: it goes to <see cref="ObserverFailed"/>.</item>
    /// <item>Observers get the commits in the order they were made (<see cref="Commit.Seq"/>): a commit made by an observer while
    /// another is being reported waits until every observer has seen the first. A commit made inside another target's setter is
    /// <see cref="Commit.Nested"/>.</item>
    /// </list>
    /// </summary>
    [NotAWidget("a window's record of its widgets' changes, not a widget")]
    public sealed class CommitPath
    {
        readonly Func<int> clock;
        readonly List<CommitTarget> targets = new List<CommitTarget>();
        readonly Dictionary<string, int> kindCounts = new Dictionary<string, int>();
        readonly List<Commit> log = new List<Commit>();
        // the reports waiting for their observers while an observer runs, so every observer sees them in order
        readonly Queue<Report> pending = new Queue<Report>();

        struct Report
        {
            public Commit Commit;
            public bool Settled;
        }
        bool delivering;
        long seq;

        // the setters running now (a commit made inside one is nested)
        internal int setting;

        // the actor an undo stack writes for (the actor of its commits, whose source is Undo)
        internal ChangeActor undoActor = ChangeActor.Program;

        // the tag given to the targets made now (Tagging)
        string tagging;

        /// <param name="name">The window's name, part of a target's full key for an observer of several windows.</param>
        /// <param name="clock">The tick (the frame number) stamped on commits; a counter of commits when null.</param>
        /// <param name="capacity">How many commits <see cref="Log"/> keeps.</param>
        public CommitPath(string name, Func<int> clock = null, int capacity = 32)
        {
            Name = name ?? "";
            this.clock = clock;
            Capacity = Math.Max(0, capacity);
        }

        public string Name { get; }

        public int Capacity { get; }

        /// <summary>Called after each change of a target that persists, before the commit is reported (the owner marks its settings dirty).</summary>
        public Action Edited;

        /// <summary>
        /// The description of a target that has none of its own (<see cref="CommitTarget.Description"/>), looked up when asked: KodeKit
        /// gives the control's tooltip. Null: targets have only the descriptions views set.
        /// </summary>
        public Func<CommitTarget, string> Describer;

        /// <summary>
        /// A target's label as shown (a '#key' localized, the trailing colon dropped), for a host that reads a control with no model
        /// (<see cref="ControlInfo"/>): KodeKit sets it. Null: the label as the factory got it.
        /// </summary>
        public Func<string, string> ShowLabel;

        // the observers, replaced (not changed) on each add or remove, so an observer may unsubscribe while being called
        Action<Commit>[] observers = new Action<Commit>[0];

        /// <summary>
        /// Each change: a new commit, or a grouped one continued. An observer that throws is reported to
        /// <see cref="ObserverFailed"/> and the others are still called: the setter has run, so the widget's own handler goes on.
        /// </summary>
        public event Action<Commit> Committed
        {
            add => observers = Added(observers, value);
            remove => observers = Removed(observers, value);
        }

        /// <summary>An observer of <see cref="Committed"/> or <see cref="Settled"/> threw (the host logs it). Null: the exception is dropped.</summary>
        public event Action<Commit, Exception> ObserverFailed;

        /// <summary>
        /// A commit is final: an ungrouped one right after its <see cref="Committed"/>, a gesture's when its group ends (the value a
        /// field's edit or a drag ended with). Once settled, a commit never changes.
        /// </summary>
        public event Action<Commit> Settled
        {
            add => settledObservers = Added(settledObservers, value);
            remove => settledObservers = Removed(settledObservers, value);
        }

        Action<Commit>[] settledObservers = new Action<Commit>[0];

        // a new array on each change, so an observer may unsubscribe while being called
        static Action<Commit>[] Added(Action<Commit>[] list, Action<Commit> value)
        {
            if (value == null) return list;
            var a = new Action<Commit>[list.Length + 1];
            Array.Copy(list, a, list.Length);
            a[list.Length] = value;
            return a;
        }

        static Action<Commit>[] Removed(Action<Commit>[] list, Action<Commit> value)
        {
            int i = Array.LastIndexOf(list, value);
            if (i < 0) return list;
            var a = new Action<Commit>[list.Length - 1];
            Array.Copy(list, a, i);
            Array.Copy(list, i + 1, a, i, list.Length - i - 1);
            return a;
        }

        public IReadOnlyList<CommitTarget> Targets => targets;

        /// <summary>The kept commits, oldest first.</summary>
        public IReadOnlyList<Commit> Log => log;

        /// <summary>The latest commit (null before the first).</summary>
        public Commit Last { get; private set; }

        /// <summary>A target was removed (its control destroyed): an observer drops what it holds for it.</summary>
        public event Action<CommitTarget> TargetRemoved;

        /// <summary>The number of commits made (a group counts once).</summary>
        public long Total => seq;

        internal int Now => clock != null ? clock() : (int)seq;

        /// <summary>A control on this path. <paramref name="persist"/>: its changes call <see cref="Edited"/>.</summary>
        public CommitTarget<T> Target<T>(string kind, Func<T> get, Action<T> set, string label = null, object control = null, bool persist = true)
        {
            kind = kind ?? "";
            kindCounts.TryGetValue(kind, out int n);
            kindCounts[kind] = n + 1;
            var t = new CommitTarget<T>(this, kind, n, get, set, label, persist) { Control = control, Tag = tagging };
            targets.Add(t);
            if (label != null) NameTarget(t, label);
            return t;
        }

        /// <summary>
        /// The scope the targets made now are named in (<see cref="NameTarget"/>): "" for the window's own build, else the lazy panel or
        /// section being built ("p3/"), which the window numbers when it makes the panel, not when the panel is first shown. Ids made
        /// in a scope begin with it, so a label repeated across panels (the planner's operations) names one control per panel whichever
        /// panel the player opens first.
        /// </summary>
        public string NameScope { get; set; } = "";

        /// <summary>
        /// Gives <paramref name="t"/> an <see cref="CommitTarget.Id"/> from <paramref name="label"/> unless it has one: the
        /// <see cref="NameScope"/> and the label, or the scope, the label and "/Kind" when another target of the scope has the label (a
        /// switch and its field on one row). None when both are taken.
        /// </summary>
        public void NameTarget(CommitTarget t, string label)
        {
            if (t == null || t.Id != null || string.IsNullOrEmpty(label)) return;
            string named = (NameScope ?? "") + label;
            if (FindId(named) == null) t.Id = named;
            else
            {
                string id = named + "/" + t.Kind;
                if (FindId(id) == null) t.Id = id;
            }
        }

        /// <summary>
        /// Lets a target go: its group ends (its commit settles), it leaves <see cref="Targets"/>, its writes are refused and
        /// <see cref="TargetRemoved"/> fires. A view calls it when it destroys the control (a list's row). Keys are not reused.
        /// </summary>
        public bool Remove(CommitTarget t)
        {
            if (t == null || t.Path != this || t.Removed) return false;
            if (t.holds > 0)
            {
                t.holds = 1;
                t.EndGroup();
            }
            targets.Remove(t);
            t.Removed = true;
            t.Control = null;
            TargetRemoved?.Invoke(t);
            return true;
        }

        /// <summary>The target named <paramref name="id"/> (<see cref="CommitTarget.Id"/>; null: none).</summary>
        public CommitTarget FindId(string id)
        {
            if (id == null) return null;
            for (int i = 0; i < targets.Count; i++)
                if (targets[i].Id == id) return targets[i];
            return null;
        }

        /// <summary>
        /// While the returned scope is open, the targets made get <paramref name="tag"/> (<see cref="CommitTarget.Tag"/>): a view marks
        /// the controls one part of it builds. Lazily built panels make their targets later, outside the scope: tag those where they
        /// are built.
        /// </summary>
        public TagScope Tagging(string tag)
        {
            var scope = new TagScope(this, tagging);
            tagging = tag;
            return scope;
        }

        /// <summary>The scope of <see cref="Tagging"/>; disposing it restores the previous tag.</summary>
        public struct TagScope : IDisposable
        {
            readonly CommitPath path;
            readonly string previous;

            internal TagScope(CommitPath path, string previous)
            {
                this.path = path;
                this.previous = previous;
            }

            public void Dispose()
            {
                if (path != null) path.tagging = previous;
            }
        }

        /// <summary>The target drawn by <paramref name="control"/> (null: none).</summary>
        public CommitTarget Find(object control)
        {
            if (control == null) return null;
            for (int i = 0; i < targets.Count; i++)
                if (ReferenceEquals(targets[i].Control, control)) return targets[i];
            return null;
        }

        /// <summary>The target with this key ("Kind#Index"; null: none).</summary>
        public CommitTarget FindKey(string key)
        {
            int hash = key?.LastIndexOf('#') ?? -1;
            if (hash < 0 || !int.TryParse(key.Substring(hash + 1), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out int index)) return null;
            string kind = key.Substring(0, hash);
            for (int i = 0; i < targets.Count; i++)
                if (targets[i].Index == index && targets[i].Kind == kind) return targets[i];
            return null;
        }

        /// <summary>Forgets the kept commits (the targets stay).</summary>
        public void ClearLog()
        {
            log.Clear();
            Last = null;
        }

        internal void Record<TV, TA>(CommitTarget t, TV old, TV now, TA asked, ChangeSource source, object before, object after)
        {
            if (t.Persist) Edited?.Invoke();
            Commit c = t.open;
            if (c != null && c.Source == source)
            {
                c.New = now;
                c.After = after;
                c.Asked = asked;
                c.Count++;
            }
            else
            {
                if (c != null)
                {
                    // another source inside the group: the group's commit so far is final
                    t.open = null;
                    Settle(c);
                }
                c = new Commit
                {
                    Target = t,
                    Old = old,
                    New = now,
                    Asked = asked,
                    Source = source,
                    Actor = source == ChangeSource.Undo ? undoActor : ChangeSources.Actor(source),
                    Count = 1,
                    Seq = ++seq,
                    Open = t.holds > 0,
                    Before = before,
                    After = after,
                    Nested = setting > 0
                };
                c.Tick = Now;
                Commit adopted = t.adopted;
                t.adopted = null;
                if (adopted != null && c.Open && adopted.Source == source)
                {
                    c.Old = adopted.Old;
                    c.Before = adopted.Before;
                    c.Count = adopted.Count + 1;
                    c.Replaces = adopted;
                }
                t.open = c.Open ? c : null;
                t.last = c;
                Last = c;
                if (Capacity > 0)
                {
                    if (log.Count >= Capacity) log.RemoveAt(0);
                    log.Add(c);
                }
            }
            Deliver(c, false);
            if (!c.Open) Deliver(c, true);
        }

        internal void Settle(Commit c)
        {
            c.Open = false;
            Deliver(c, true);
        }

        // reports a commit to the observers, after the reports already waiting (an observer that commits while being called)
        void Deliver(Commit c, bool settled)
        {
            pending.Enqueue(new Report { Commit = c, Settled = settled });
            if (delivering) return;
            delivering = true;
            try
            {
                while (pending.Count > 0)
                {
                    Report next = pending.Dequeue();
                    Call(next.Settled ? settledObservers : observers, next.Commit);
                }
            }
            finally
            {
                delivering = false;
                pending.Clear();
            }
        }

        void Call(Action<Commit>[] called, Commit c)
        {
            for (int i = 0; i < called.Length; i++)
            {
                try
                {
                    called[i](c);
                }
                catch (Exception e)
                {
                    ObserverFailed?.Invoke(c, e);
                }
            }
        }

        // ---- the source a host imposes ----

        // per thread: a scope opened on another thread (an HTTP listener's) has no effect on the main thread's widgets
        [ThreadStatic] static int overrides;
        [ThreadStatic] static ChangeSource overrideSource;

        /// <summary>
        /// While the returned scope is open, widgets report <paramref name="source"/> instead of their own (a host that runs a
        /// widget's handler for an agent, or keys that pick through a click handler).
        /// <list type="bullet">
        /// <item>The outermost scope wins: a widget that opens its own <c>As(Keyboard)</c> inside a host's <c>As(Agent)</c> still
        /// reports Agent.</item>
        /// <item>Scopes are per thread: open one on the main thread, around the widget's handler.</item>
        /// <item>Dispose it once, with <c>using</c>: a copy disposed again, or a scope disposed while an inner one is still open, does
        /// nothing. A host checks <see cref="OpenScopes"/> once a frame and calls <see cref="ResetScopes"/> when one was left open.</item>
        /// </list>
        /// </summary>
        public static SourceScope As(ChangeSource source)
        {
            if (overrides == 0) overrideSource = source;
            overrides++;
            return new SourceScope(overrides);
        }

        /// <summary>The source a widget reports: the host's (<see cref="As"/>) when one is set, else <paramref name="own"/>.</summary>
        public static ChangeSource Resolve(ChangeSource own) => overrides > 0 ? overrideSource : own;

        /// <summary>The scopes of <see cref="As"/> open on this thread (0 between frames, unless one was never disposed).</summary>
        public static int OpenScopes => overrides;

        /// <summary>
        /// Closes the scopes left open on this thread: one never disposed would give its source to every later edit of every window.
        /// True when there were some (the host logs it).
        /// </summary>
        public static bool ResetScopes()
        {
            if (overrides == 0) return false;
            overrides = 0;
            return true;
        }

        /// <summary>The scope of <see cref="As"/>; a struct, so opening one allocates nothing.</summary>
        public struct SourceScope : IDisposable
        {
            readonly int depth;

            internal SourceScope(int depth) => this.depth = depth;

            /// <summary>Closes the scope when it is the innermost open one; otherwise (already closed, a copy) does nothing.</summary>
            public void Dispose()
            {
                if (depth == 0 || overrides != depth) return;
                overrides--;
            }
        }
    }
}
