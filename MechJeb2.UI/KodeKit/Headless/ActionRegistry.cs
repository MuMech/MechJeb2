#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>What an action does, for the lists that offer it (the shortcuts' Settings row, later the command palette).</summary>
    public enum UiActionKind
    {
        /// <summary>Shows or hides a window.</summary>
        ToggleWindow,

        /// <summary>Runs a command that is no window toggle (opens the command palette).</summary>
        Command
    }

    /// <summary>
    /// One thing the player can run by id: a window toggle. Its id is stable across sessions (bindings are saved by it); its label is
    /// read when shown (it follows a language switch); whether it can run now is asked each time.
    /// </summary>
    [NotAWidget("data: one action of the action registry, run by a shortcut or the command palette")]
    public sealed class UiAction
    {
        private readonly Func<string> label;
        private readonly Func<bool>   available;
        private readonly Action       run;

        /// <param name="id">The stable id (<c>window:MechJebModuleAscentMenu</c>).</param>
        /// <param name="label">The label as shown now.</param>
        /// <param name="run">What it does.</param>
        /// <param name="available">Whether it can run now (null: always).</param>
        public UiAction(string id, UiActionKind kind, Func<string> label, Action run, Func<bool> available = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("an action needs an id", nameof(id));
            Id             = id;
            Kind           = kind;
            this.label     = label;
            this.run       = run;
            this.available = available;
        }

        public string       Id   { get; }
        public UiActionKind Kind { get; }

        /// <summary>The label as shown now (the id when it has none).</summary>
        public string Label
        {
            get
            {
                string s = label?.Invoke();
                return string.IsNullOrEmpty(s) ? Id : s;
            }
        }

        /// <summary>Whether it can run now.</summary>
        public bool Available => available == null || available();

        /// <summary>Runs it when it is available; false when it isn't.</summary>
        public bool Run()
        {
            if (!Available) return false;
            run?.Invoke();
            return true;
        }
    }

    /// <summary>
    /// The actions the player can run by id, shared by the keyboard shortcuts (<see cref="ShortcutMap"/>) and the command palette.
    /// <list type="bullet">
    /// <item>An id is unique: adding an action with an id already there replaces it (a window's module recreated keeps its
    /// bindings).</item>
    /// <item>The order is the order of adding (the lists that show the actions show them in it).</item>
    /// <item>Removing or replacing an action changes <see cref="Version"/> and raises <see cref="Changed"/>; finding one allocates
    /// nothing.</item>
    /// </list>
    /// </summary>
    [NotAWidget("the registry of actions the shortcuts and the command palette run, not a widget")]
    public sealed class ActionRegistry
    {
        private readonly List<UiAction>               list = new List<UiAction>();
        private readonly Dictionary<string, UiAction> byId = new Dictionary<string, UiAction>(StringComparer.Ordinal);

        /// <summary>Called after an action is added, replaced or removed.</summary>
        public event Action Changed;

        /// <summary>Grows by one at every change.</summary>
        public int Version { get; private set; }

        /// <summary>The actions, in the order they were added.</summary>
        public IReadOnlyList<UiAction> All => list;

        public int Count => list.Count;

        /// <summary>Adds <paramref name="a"/>, or replaces the action with its id (in its place).</summary>
        public void Add(UiAction a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (byId.TryGetValue(a.Id, out UiAction old))
                list[list.IndexOf(old)] = a;
            else
                list.Add(a);
            byId[a.Id] = a;
            Touch();
        }

        /// <summary>Removes the action with this id; false when there is none.</summary>
        public bool Remove(string id)
        {
            if (id == null || !byId.TryGetValue(id, out UiAction a)) return false;
            byId.Remove(id);
            list.Remove(a);
            Touch();
            return true;
        }

        /// <summary>Removes every action of <paramref name="kind"/>.</summary>
        public void RemoveAll(UiActionKind kind)
        {
            bool any = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Kind != kind) continue;
                byId.Remove(list[i].Id);
                list.RemoveAt(i);
                any = true;
            }
            if (any) Touch();
        }

        /// <summary>The action with this id (null: none).</summary>
        public UiAction Find(string id) => id != null && byId.TryGetValue(id, out UiAction a) ? a : null;

        private void Touch()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
