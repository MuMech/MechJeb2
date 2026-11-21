#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>
    /// The values the player committed in some fields, kept to be offered again (ui-sdk's <c>createInputHistory</c>): per field key,
    /// a short list, the most recent first. A field walks its list with an <see cref="InputHistoryBrowser"/>.
    /// <list type="bullet">
    /// <item>A key names what a field holds, across windows, rebuilds, languages and game sessions: the view that opts a field in
    /// chooses it ("ascent.inclination"), and fields that hold the same quantity may share one. It is neither the label (localized)
    /// nor the field's place in its window (which moves when a window's layout changes, and the lists are saved).</item>
    /// <item>Only a player's finished edit is recorded (<see cref="Recordable"/>): a settled commit of the player that changed the
    /// value, not an agent's, the program's or an undo's.</item>
    /// <item>A value is trimmed. An empty one, one longer than <see cref="MaxLength"/> or one with a line break is not recorded.</item>
    /// <item>A value already in the list moves to the front: a list has no duplicates (compared as text).</item>
    /// <item>A list keeps at most <see cref="PerKey"/> values: the oldest goes.</item>
    /// <item>At most <see cref="MaxKeys"/> keys: a new key past the cap drops the key recorded into least recently. The whole is
    /// bounded, whatever the views ask.</item>
    /// <item><see cref="Changed"/> fires after a record that changed the lists (not for a value already first in the key recorded
    /// into last), a removal or a clear;
    /// <see cref="Load"/>, which puts back saved lists, doesn't fire it.</item>
    /// </list>
    /// </summary>
    [NotAWidget("the fields' recent values by key, not a widget")]
    public sealed class InputHistory
    {
        sealed class Entry
        {
            public string Key;
            public readonly List<string> Values = new List<string>();
        }

        static readonly string[] none = new string[0];

        // the keys, the one recorded into most recently first
        readonly List<Entry> entries = new List<Entry>();

        public InputHistory(int perKey = 8, int maxKeys = 64, int maxLength = 40)
        {
            PerKey = Math.Max(1, perKey);
            MaxKeys = Math.Max(1, maxKeys);
            MaxLength = Math.Max(1, maxLength);
        }

        /// <summary>The most values a key keeps.</summary>
        public int PerKey { get; }

        /// <summary>The most keys kept.</summary>
        public int MaxKeys { get; }

        /// <summary>The longest value kept, in characters.</summary>
        public int MaxLength { get; }

        /// <summary>A list changed (a record, a removal, a clear): a host saves the lists.</summary>
        public event Action Changed;

        /// <summary>The keys, the one recorded into most recently first.</summary>
        public int Count => entries.Count;

        /// <summary>The key at <paramref name="index"/> (0: the one recorded into most recently).</summary>
        public string KeyAt(int index) => entries[index].Key;

        /// <summary>
        /// <paramref name="key"/>'s values, the most recent first; an empty list for a key with none. The list is the live one: it
        /// changes with later records.
        /// </summary>
        public IReadOnlyList<string> Values(string key)
        {
            Entry e = Find(key);
            return e != null ? (IReadOnlyList<string>)e.Values : none;
        }

        Entry Find(string key)
        {
            if (key == null) return null;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Key == key) return entries[i];
            return null;
        }

        /// <summary>
        /// Whether <paramref name="c"/> is a value the player committed: settled, made by the player (not an agent, the program or an
        /// undo), not a side effect of another control's setter, a text, and a change of the value.
        /// </summary>
        public static bool Recordable(Commit c) =>
            c != null && !c.Open && c.Actor == ChangeActor.User && c.Source != ChangeSource.Undo && !c.Nested && c.New is string &&
            !Equals(c.Old, c.New);

        /// <summary>Records <paramref name="c"/>'s new value under <paramref name="key"/> when it is <see cref="Recordable"/>.</summary>
        public bool Record(string key, Commit c) => Recordable(c) && Record(key, (string)c.New);

        /// <summary>
        /// Puts <paramref name="value"/> first in <paramref name="key"/>'s list. False when nothing changed: no key, a value refused
        /// (empty, too long, a line break) or already first.
        /// </summary>
        public bool Record(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || !Accept(ref value)) return false;
            if (!Put(key, value, true)) return false;
            Changed?.Invoke();
            return true;
        }

        bool Accept(ref string value)
        {
            if (value == null) return false;
            value = value.Trim();
            return value.Length > 0 && value.Length <= MaxLength && value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0;
        }

        // puts value first (front) or last in key's list; false when it changed nothing
        bool Put(string key, string value, bool front)
        {
            Entry e = Find(key);
            if (e == null)
            {
                if (entries.Count >= MaxKeys)
                {
                    // a saved list past the cap is dropped; a new record drops the key used least recently
                    if (!front) return false;
                    entries.RemoveAt(entries.Count - 1);
                }
                e = new Entry { Key = key };
                if (front) entries.Insert(0, e);
                else entries.Add(e);
            }
            else if (front && entries[0] != e)
            {
                entries.Remove(e);
                entries.Insert(0, e);
            }
            else if (front && e.Values.Count > 0 && e.Values[0] == value) return false;

            int at = e.Values.IndexOf(value);
            if (!front)
            {
                if (at >= 0 || e.Values.Count >= PerKey) return false;
                e.Values.Add(value);
                return true;
            }
            if (at >= 0) e.Values.RemoveAt(at);
            e.Values.Insert(0, value);
            if (e.Values.Count > PerKey) e.Values.RemoveAt(e.Values.Count - 1);
            return true;
        }

        /// <summary>
        /// Puts back a saved list: <paramref name="values"/>, the most recent first, after what <paramref name="key"/> holds, under
        /// the same rules (trimmed, refused values left out, no duplicates, the caps). Doesn't fire <see cref="Changed"/>.
        /// </summary>
        public void Load(string key, IEnumerable<string> values)
        {
            if (string.IsNullOrEmpty(key) || values == null) return;
            foreach (string v in values)
            {
                string value = v;
                if (Accept(ref value)) Put(key, value, false);
            }
        }

        /// <summary>Forgets <paramref name="key"/>'s values.</summary>
        public bool Remove(string key)
        {
            Entry e = Find(key);
            if (e == null) return false;
            entries.Remove(e);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Forgets every list. <paramref name="notify"/>: fire <see cref="Changed"/> (false when the saved lists are reloaded).</summary>
        public void Clear(bool notify = true)
        {
            if (entries.Count == 0) return;
            entries.Clear();
            if (notify) Changed?.Invoke();
        }
    }

    /// <summary>
    /// One field's walk through its key's list during an edit (KodeKit: Up shows an older value, Down a newer one).
    /// <list type="bullet">
    /// <item><see cref="Older"/> first keeps the field's text as the typed text and shows the most recent value; each next one shows
    /// the next older value. At the oldest it shows nothing new (null).</item>
    /// <item>Values equal to the typed text or to the field's text are passed over: the field shows that text already.</item>
    /// <item><see cref="Newer"/> walks back toward the most recent; past it, the typed text comes back. Not walking: null.</item>
    /// <item>A text other than the one shown last (the player typed, or Escape put the old text back) ends the walk: the next
    /// <see cref="Older"/> starts from that text.</item>
    /// <item><see cref="Reset"/> ends the walk (the edit ended).</item>
    /// <item>The walk reads the live list: when it changes during the walk, the walk goes on from the same place in it.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: a field's walk through its recent values (the field is the widget)")]
    public sealed class InputHistoryBrowser
    {
        readonly InputHistory history;
        string shown;

        public InputHistoryBrowser(InputHistory history, string key)
        {
            this.history = history ?? throw new ArgumentNullException(nameof(history));
            Key = key;
        }

        public string Key { get; }

        /// <summary>The place in the list of the value shown (-1: the typed text, not walking).</summary>
        public int Index { get; private set; } = -1;

        /// <summary>The text the walk started from, which <see cref="Newer"/> brings back past the most recent value.</summary>
        public string Typed { get; private set; }

        public bool Browsing => Index >= 0;

        // a text other than the one the walk showed: the player edited it, the walk is over
        void Follow(string current)
        {
            if (Index >= 0 && current != shown) Reset();
        }

        /// <summary>The next older value than the one shown (the most recent when not walking), or null when there is none.</summary>
        public string Older(string current)
        {
            current = current ?? "";
            Follow(current);
            string from = Index >= 0 ? Typed : current;
            IReadOnlyList<string> list = history.Values(Key);
            for (int i = Index + 1; i < list.Count; i++)
            {
                if (list[i] == from || list[i] == current) continue;
                Typed = from;
                Index = i;
                shown = list[i];
                return shown;
            }
            return null;
        }

        /// <summary>The next newer value than the one shown, the typed text past the most recent, or null when not walking.</summary>
        public string Newer(string current)
        {
            current = current ?? "";
            Follow(current);
            if (Index < 0) return null;
            IReadOnlyList<string> list = history.Values(Key);
            for (int i = Math.Min(Index, list.Count) - 1; i >= 0; i--)
            {
                if (list[i] == Typed || list[i] == current) continue;
                Index = i;
                shown = list[i];
                return shown;
            }
            string typed = Typed;
            Reset();
            return typed;
        }

        /// <summary>Ends the walk.</summary>
        public void Reset()
        {
            Index = -1;
            shown = null;
            Typed = null;
        }
    }
}
