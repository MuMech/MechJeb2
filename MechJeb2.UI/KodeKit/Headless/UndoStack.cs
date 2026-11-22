#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>What an undo or a redo did.</summary>
    public enum UndoResult
    {
        /// <summary>The value (or state) was put back.</summary>
        Done,

        /// <summary>No entry to undo or redo (of that source).</summary>
        Nothing,

        /// <summary>The stack's <see cref="UndoStack.Guard"/> refused the write now (an autopilot owns the setting): the entry stays.</summary>
        Blocked,

        /// <summary>
        /// The value changed since the entry was recorded by a way the stack didn't see, or the state it described is gone (an executed
        /// node): nothing was written and the entry was dropped.
        /// </summary>
        Stale,

        /// <summary>A later entry of another source changed the same control: undoing this one would undo that too. The entry stays.</summary>
        Conflict,

        /// <summary>
        /// An undo or a redo is running already (asked by an observer or a setter of the one being written): refused, nothing changed.
        /// </summary>
        Busy,

        /// <summary>The write threw (a setter or a state's restore): the entry stays, and the exception went to <see cref="UndoStack.Failed"/>.</summary>
        Failed
    }

    /// <summary>One undoable change: a commit of a control (a gesture counts once), with its source and its place in the history.</summary>
    [NotAWidget("data: one entry of an UndoStack")]
    public sealed class UndoEntry
    {
        // a state target's state after this entry's last undo or redo, which the next redo or undo expects (moved: set)
        internal object state;
        internal bool moved;

        internal UndoEntry(Commit commit, long order)
        {
            Commit = commit;
            Order = order;
        }

        public Commit Commit { get; }

        public CommitTarget Target => Commit.Target;

        public ChangeSource Source => Commit.Source;

        /// <summary>Who made the change (a click and a key are the same player): the filters and the redo rule go by it.</summary>
        public ChangeActor Actor => Commit.Actor;

        /// <summary>The entry's place among every stack of a history: higher is more recent (an undo or a redo stamps it again).</summary>
        public long Order { get; internal set; }

        public override string ToString() => $"{Commit.Target?.Key}: {Commit.Old} -> {Commit.New} ({Commit.Source})";
    }

    /// <summary>
    /// The undo and redo of one window's edits (ui-sdk's <c>createUndoStack</c>), fed by its <see cref="CommitPath"/>.
    /// <list type="bullet">
    /// <item>Each commit is one entry: a grouped gesture (a slider drag, a held repeat button, a field's edit session) is undone at
    /// once. A commit that leaves the value as it was is not an entry (for a target with a <see cref="CommitTarget.State"/>: one
    /// that leaves the state as it was, so typing in the node editor's fields is no step until the typed value reaches the node), nor is a <see cref="Commit.Nested"/> one (a side effect of
    /// another target's setter). A commit that carries on another (<see cref="Commit.Replaces"/>: a slider's press and its drag)
    /// takes that one's entry.</item>
    /// <item>An undo writes the old value back through the target (<see cref="CommitTarget.Write"/>), or restores the target's
    /// <see cref="CommitTarget.State"/>, with the source <see cref="ChangeSource.Undo"/>: the path records that commit and its
    /// observers see it, and this stack records no entry for it, so a redo stays possible.</item>
    /// <item>A target that isn't <see cref="CommitTarget.Undoable"/> (a selected row) is not recorded.</item>
    /// <item>Undo and redo can be filtered by who made the change (a source's <see cref="ChangeActor"/>: Pointer and Keyboard are both
    /// the player): an agent or a test undoes its own changes and leaves the player's. An entry
    /// whose control a later entry of another source changed is not undone (<see cref="UndoResult.Conflict"/>).</item>
    /// <item>Before writing, the stack checks the control still shows the entry's value (its old value for a redo). When it
    /// doesn't (the module changed it, the player edited it in another window), nothing is written and the entry is dropped
    /// (<see cref="UndoResult.Stale"/>). A state that refuses its restore is stale the same way.</item>
    /// <item>A target with a <see cref="CommitTarget.State"/> is checked by its state: the undo restores
    /// <see cref="Commit.Before"/> only while the state is still <see cref="Commit.After"/> (a gizmo drag or a stock node edit since
    /// makes it stale), and the redo restores <see cref="Commit.After"/> only while the state is still the one the undo left. Every
    /// change of the state must therefore be a commit of the target (the node editor applies a typed Δv as one).</item>
    /// <item><see cref="Guard"/> can hold a write back while the window's owner forbids it (an engaged autopilot reads the
    /// setting): the entry stays, and the undo is possible again once the guard allows it.</item>
    /// <item>A new edit drops the redo entries of its actor (a click drops the player's undone typing), and those of anyone on the
    /// same control.</item>
    /// <item>A target removed from the path (<see cref="CommitPath.Remove"/>: its row destroyed) takes its entries with it.</item>
    /// <item>The stack keeps <see cref="Capacity"/> entries, dropping the oldest. <see cref="Clear"/> forgets them all, and
    /// <see cref="Detach"/> stops recording (the window was destroyed).</item>
    /// <item>An undo or a redo asked while another one writes (by an observer, by a setter) is refused (<see cref="UndoResult.Busy"/>).
    /// A write that throws keeps its entry (<see cref="UndoResult.Failed"/>, the exception to <see cref="Failed"/>).</item>
    /// <item>The commit an undo writes has the source <see cref="ChangeSource.Undo"/> and, as its actor, who undid: the scope a host
    /// opened (<see cref="CommitPath.As"/>: the player's click, an agent), else the actor the filter names, else the program.</item>
    /// <item>A stack records nothing until its owner turns <see cref="Recording"/> on: an undo writes setters back, and a window
    /// whose setters engage a flight mode or an autopilot (the Translatron's mode, SmartASS's) must not be undone through the
    /// history. A window opts in once it has set its <see cref="Guard"/> (KodeKit's <c>UndoButtons()</c>).</item>
    /// </list>
    /// </summary>
    [NotAWidget("a window's undo history over its commit path (its buttons are the window's own widgets)")]
    public sealed class UndoStack
    {
        readonly List<UndoEntry> done = new List<UndoEntry>();
        readonly List<UndoEntry> undone = new List<UndoEntry>();
        readonly UndoHistory history;
        long order;
        bool attached;

        /// <param name="path">The window's commit path, whose commits become entries.</param>
        /// <param name="capacity">How many entries the stack keeps.</param>
        /// <param name="history">The history every window's stack belongs to (an undo by source across windows), or null.</param>
        /// <param name="name">The window's name, for a host that lists the stacks.</param>
        public UndoStack(CommitPath path, int capacity = 50, UndoHistory history = null, string name = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Capacity = Math.Max(1, capacity);
            Name = name ?? path.Name;
            this.history = history;
            path.Committed += Observe;
            path.TargetRemoved += Forget;
            attached = true;
            history?.Add(this);
        }

        public CommitPath Path { get; }

        public string Name { get; }

        public int Capacity { get; }

        /// <summary>
        /// Whether an undo or a redo may write this control now (null: always). The window's owner refuses a setting that an
        /// engaged autopilot reads; the entry waits.
        /// </summary>
        public Func<CommitTarget, bool> Guard;

        /// <summary>The entries that can be undone, oldest first.</summary>
        public IReadOnlyList<UndoEntry> Done => done;

        /// <summary>The entries that can be redone, the last undone last.</summary>
        public IReadOnlyList<UndoEntry> Undone => undone;

        /// <summary>Whether the stack still records (false once <see cref="Detach"/>ed).</summary>
        public bool Attached => attached;

        bool recording;

        /// <summary>
        /// Whether the window's owner opted in: off by default, so a window that never asked for undo has no entries, and
        /// <see cref="UndoHistory"/> can't write its setters back. Turning it off forgets the entries.
        /// </summary>
        public bool Recording
        {
            get => recording;
            set
            {
                if (recording == value) return;
                recording = value;
                if (!value) Clear();
            }
        }

        /// <summary>Counts every change of the stack (an entry recorded, undone, redone or dropped), for a view that redraws on change.</summary>
        public int Version { get; private set; }

        internal long NextOrder() => history != null ? history.NextOrder() : ++order;

        void Observe(Commit c)
        {
            // a nested commit is a side effect of the outer one, whose undo runs the outer setter again
            if (!attached || !recording || c.Source == ChangeSource.Undo || c.Nested || c.Target == null || !c.Target.Undoable) return;
            // a grouped gesture continued: the entry already holds the commit, whose New follows the gesture
            for (int i = done.Count - 1; i >= 0; i--)
                if (ReferenceEquals(done[i].Commit, c)) return;
            // recorded once it changed something (a gesture that hasn't yet is recorded by its next change that does)
            if (!Effective(c)) return;

            // the entries that ended up changing nothing (a field edited back to its value) are not undo steps
            for (int i = done.Count - 1; i >= 0; i--)
                if (!done[i].Commit.Open && !Effective(done[i].Commit)) done.RemoveAt(i);
            for (int i = undone.Count - 1; i >= 0; i--)
                if (undone[i].Actor == c.Actor || SameControl(undone[i].Target, c.Target)) undone.RemoveAt(i);
            // a drag that carries on its press's commit (a slider's track click): one entry for both
            if (c.Replaces != null)
                for (int i = done.Count - 1; i >= 0; i--)
                    if (ReferenceEquals(done[i].Commit, c.Replaces)) done.RemoveAt(i);

            done.Add(new UndoEntry(c, NextOrder()));
            while (done.Count > Capacity) done.RemoveAt(0);
            Version++;
        }

        // a removed target (its row destroyed): its entries can't be written any more
        void Forget(CommitTarget t)
        {
            int before = done.Count + undone.Count;
            for (int i = done.Count - 1; i >= 0; i--)
                if (done[i].Target == t) done.RemoveAt(i);
            for (int i = undone.Count - 1; i >= 0; i--)
                if (undone[i].Target == t) undone.RemoveAt(i);
            if (done.Count + undone.Count != before) Version++;
        }

        /// <summary>
        /// Whether a commit is an undo step: it changed the value, or, for a target with a <see cref="CommitTarget.State"/>, the
        /// state (a typed text that hasn't reached the node is no step).
        /// </summary>
        public static bool Effective(Commit c) => c.Target?.State != null ? !Equals(c.Before, c.After) : !Equals(c.Old, c.New);

        // two targets change the same thing: the same control, or controls sharing one state (a node's Δv and time fields)
        static bool SameControl(CommitTarget a, CommitTarget b) =>
            ReferenceEquals(a, b) || a.State != null && ReferenceEquals(a.State, b.State);

        // a filter by source is a filter by its actor: Undo(Pointer) also takes the player's typed edits
        internal static bool Matches(UndoEntry e, ChangeSource? source) => source == null || e.Actor == ChangeSources.Actor(source.Value);

        /// <summary>The entry <see cref="Undo"/> would take (of <paramref name="source"/>; null: any), or null.</summary>
        public UndoEntry Next(ChangeSource? source = null)
        {
            for (int i = done.Count - 1; i >= 0; i--)
                if (Effective(done[i].Commit) && Matches(done[i], source)) return done[i];
            return null;
        }

        /// <summary>The entry <see cref="Redo"/> would take (of <paramref name="source"/>; null: any), or null.</summary>
        public UndoEntry NextRedo(ChangeSource? source = null)
        {
            for (int i = undone.Count - 1; i >= 0; i--)
                if (Matches(undone[i], source)) return undone[i];
            return null;
        }

        /// <summary>Whether an undo (of <paramref name="source"/>) has an entry and the guard allows it now.</summary>
        public bool CanUndo(ChangeSource? source = null)
        {
            UndoEntry e = Next(source);
            return e != null && Allowed(e);
        }

        /// <summary>Whether a redo (of <paramref name="source"/>) has an entry and the guard allows it now.</summary>
        public bool CanRedo(ChangeSource? source = null)
        {
            UndoEntry e = NextRedo(source);
            return e != null && Allowed(e);
        }

        bool Allowed(UndoEntry e) => Guard == null || Guard(e.Target);

        /// <summary>The write of an undo or a redo threw (the host logs it): the entry stayed. Null: the exception is dropped.</summary>
        public event Action<UndoEntry, Exception> Failed;

        // an undo or a redo writing now, in any stack (one asked from inside another's write is refused)
        [ThreadStatic] static bool writing;

        // who undoes: a host's scope (the player's click on the undo icon, an agent through the bridge), else the actor the filter
        // names (an agent undoing its own changes), else the program
        static ChangeActor Undoer(ChangeSource? filter) =>
            CommitPath.OpenScopes > 0 ? ChangeSources.Actor(CommitPath.Resolve(ChangeSource.Program))
            : filter != null ? ChangeSources.Actor(filter.Value) : ChangeActor.Program;

        /// <summary>Undoes the latest entry (of <paramref name="source"/>'s actor; null: the latest of anyone).</summary>
        public UndoResult Undo(ChangeSource? source = null)
        {
            if (writing) return UndoResult.Busy;
            UndoEntry e = Next(source);
            if (e == null) return UndoResult.Nothing;
            int at = done.IndexOf(e);
            for (int i = at + 1; i < done.Count; i++)
                if (Effective(done[i].Commit) && SameControl(done[i].Target, e.Target)) return UndoResult.Conflict;
            if (!Allowed(e)) return UndoResult.Blocked;

            CommitTarget t = e.Target;
            bool written;
            writing = true;
            Path.undoActor = Undoer(source);
            try
            {
                if (t.State != null)
                {
                    // the state must still be the one the change (or the last redo) left: an edit outside the path since would be lost
                    written = Equals(t.State.Take(), e.moved ? e.state : e.Commit.After) && t.Restore(e.Commit.Before, ChangeSource.Undo);
                    if (written)
                    {
                        e.state = t.State.Take();
                        e.moved = true;
                    }
                }
                else written = Equals(t.Read(), e.Commit.New) && t.Write(e.Commit.Old, ChangeSource.Undo);
            }
            catch (Exception x)
            {
                Failed?.Invoke(e, x);
                return UndoResult.Failed;
            }
            finally
            {
                writing = false;
            }
            done.Remove(e);
            Version++;
            if (!written) return UndoResult.Stale;
            e.Order = NextOrder();
            // a write that rebuilt the window detached this stack: the entry goes with it
            if (attached) undone.Add(e);
            return UndoResult.Done;
        }

        /// <summary>Redoes the last undone entry (of <paramref name="source"/>'s actor; null: of anyone).</summary>
        public UndoResult Redo(ChangeSource? source = null)
        {
            if (writing) return UndoResult.Busy;
            UndoEntry e = NextRedo(source);
            if (e == null) return UndoResult.Nothing;
            int at = undone.IndexOf(e);
            for (int i = at + 1; i < undone.Count; i++)
                if (SameControl(undone[i].Target, e.Target)) return UndoResult.Conflict;
            if (!Allowed(e)) return UndoResult.Blocked;

            CommitTarget t = e.Target;
            bool written;
            writing = true;
            Path.undoActor = Undoer(source);
            try
            {
                if (t.State != null)
                {
                    written = Equals(t.State.Take(), e.state) && t.Restore(e.Commit.After, ChangeSource.Undo);
                    if (written) e.state = t.State.Take();
                }
                else written = Equals(t.Read(), e.Commit.Old) && t.Write(e.Commit.New, ChangeSource.Undo);
            }
            catch (Exception x)
            {
                Failed?.Invoke(e, x);
                return UndoResult.Failed;
            }
            finally
            {
                writing = false;
            }
            undone.Remove(e);
            Version++;
            if (!written) return UndoResult.Stale;
            e.Order = NextOrder();
            if (attached) done.Add(e);
            return UndoResult.Done;
        }

        /// <summary>Forgets every entry (the stack still records).</summary>
        public void Clear()
        {
            if (done.Count == 0 && undone.Count == 0) return;
            done.Clear();
            undone.Clear();
            Version++;
        }

        /// <summary>Stops recording and leaves the history, with the entries forgotten: the window is gone.</summary>
        public void Detach()
        {
            if (!attached) return;
            attached = false;
            Path.Committed -= Observe;
            Path.TargetRemoved -= Forget;
            history?.Remove(this);
            Clear();
        }
    }

    /// <summary>
    /// Every window's undo stack, ordered (ui-sdk's <c>createHistory</c>): an undo by source across windows, for an agent or a
    /// test that reverts exactly its own changes, wherever it made them.
    /// <list type="bullet">
    /// <item><see cref="Undo"/> takes the most recent entry of the source over all stacks; <see cref="Redo"/> the most recently
    /// undone one. Each stack's rules apply (its guard, the stale and conflict checks).</item>
    /// <item>A stack leaves the history when its window is destroyed (<see cref="UndoStack.Detach"/>).</item>
    /// </list>
    /// </summary>
    [NotAWidget("every window's undo stack in order, not a widget")]
    public sealed class UndoHistory
    {
        readonly List<UndoStack> stacks = new List<UndoStack>();
        long order;

        public IReadOnlyList<UndoStack> Stacks => stacks;

        internal long NextOrder() => ++order;

        internal void Add(UndoStack s) => stacks.Add(s);

        internal void Remove(UndoStack s) => stacks.Remove(s);

        /// <summary>The stack holding the most recent entry of <paramref name="source"/> (null: any), or null.</summary>
        public UndoStack Latest(ChangeSource? source = null)
        {
            UndoStack best = null;
            long bestOrder = long.MinValue;
            for (int i = 0; i < stacks.Count; i++)
            {
                UndoEntry e = stacks[i].Next(source);
                if (e != null && e.Order > bestOrder)
                {
                    best = stacks[i];
                    bestOrder = e.Order;
                }
            }
            return best;
        }

        /// <summary>The stack holding the most recently undone entry of <paramref name="source"/> (null: any), or null.</summary>
        public UndoStack LatestUndone(ChangeSource? source = null)
        {
            UndoStack best = null;
            long bestOrder = long.MinValue;
            for (int i = 0; i < stacks.Count; i++)
            {
                UndoEntry e = stacks[i].NextRedo(source);
                if (e != null && e.Order > bestOrder)
                {
                    best = stacks[i];
                    bestOrder = e.Order;
                }
            }
            return best;
        }

        /// <summary>Undoes the most recent entry of <paramref name="source"/> (null: any) over every window.</summary>
        public UndoResult Undo(ChangeSource? source = null)
        {
            UndoStack s = Latest(source);
            return s == null ? UndoResult.Nothing : s.Undo(source);
        }

        /// <summary>Redoes the most recently undone entry of <paramref name="source"/> (null: any) over every window.</summary>
        public UndoResult Redo(ChangeSource? source = null)
        {
            UndoStack s = LatestUndone(source);
            return s == null ? UndoResult.Nothing : s.Redo(source);
        }

        /// <summary>How many entries of <paramref name="source"/> (null: any) can be undone over every window.</summary>
        public int Count(ChangeSource? source = null)
        {
            int n = 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                IReadOnlyList<UndoEntry> d = stacks[i].Done;
                for (int k = 0; k < d.Count; k++)
                    if (UndoStack.Effective(d[k].Commit) && UndoStack.Matches(d[k], source)) n++;
            }
            return n;
        }

        /// <summary>Forgets every stack's entries.</summary>
        public void Clear()
        {
            for (int i = 0; i < stacks.Count; i++) stacks[i].Clear();
        }
    }
}
