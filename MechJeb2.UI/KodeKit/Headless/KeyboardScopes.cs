#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>The keys a keyboard scope can be given, as flags (a scope's routes, the keys down or held in a frame).</summary>
    [Flags]
    public enum ScopeKeys
    {
        None     = 0,
        Up       = 1 << 0,
        Down     = 1 << 1,
        PageUp   = 1 << 2,
        PageDown = 1 << 3,
        Home     = 1 << 4,
        End      = 1 << 5,

        /// <summary>Return or the keypad's Enter.</summary>
        Enter = 1 << 6,

        Escape = 1 << 7,

        /// <summary>Typed characters (letters and digits).</summary>
        Text = 1 << 8,

        Left  = 1 << 9,
        Right = 1 << 10,

        /// <summary>Tab (with or without Shift: the scope's owner reads Shift itself).</summary>
        Tab = 1 << 11,

        /// <summary>The keys that move a highlight.</summary>
        Navigation = Up | Down | PageUp | PageDown | Home | End,

        /// <summary>The keys that end a scope: its lock is held until they are up (KSP pauses on Escape's key-up).</summary>
        Closing = Enter | Escape,

        All = Navigation | Closing | Text | Left | Right | Tab
    }

    /// <summary>The keyboard as the scopes read it: the frame, and the keys held now.</summary>
    public interface IScopeKeyboard
    {
        /// <summary>A number that grows by one each frame.</summary>
        int Frame { get; }

        /// <summary>The keys of <see cref="ScopeKeys"/> held now (Text is never reported held).</summary>
        ScopeKeys Held { get; }
    }

    /// <summary>
    /// The stack of keyboard scopes: whatever owns the keyboard (a focused field, an open drop-down, a dialog, the command palette) opens
    /// a scope, and the scopes decide who gets the keys and how long KSP's keyboard lock is held. KodeKit draws it as
    /// <c>KodeKit.KeyboardScope</c> components on one shared stack.
    /// <list type="bullet">
    /// <item>A scope opened goes on top; the top scope alone gets the keys, and only the keys it routes. A key the top scope doesn't
    /// route goes to no scope (the scopes below are covered).</item>
    /// <item>Escape closes the top scope only, and only when it routes Escape: its owner is told (<see cref="KeyScope.Dismissed"/>), and
    /// the scope below gets no key that frame.</item>
    /// <item>A key held when the top scope changes (opened or closed) reaches the new top scope only after it has come up: the Escape
    /// that closed a field doesn't also close the drop-down under it. Text typed in the frame a scope became the top is not given to
    /// it: the chord that opened the command palette (a plain P, Right Shift+P) doesn't land in its query.</item>
    /// <item>Keys are given out once per frame, however many hosts ask. The keys kept from a top are forgotten once no scope holds a
    /// lock (nobody reads the keys then, and the next scope keeps only what is held as it opens).</item>
    /// <item>The lock is wanted while any scope is open. A scope that closes keeps its lock until the frame after it closed and until
    /// Escape and Enter are up (KSP opens its pause menu on Escape's key-up, FINDINGS rule 43). A scope opened again while it waits
    /// keeps its lock.</item>
    /// <item>A scope dropped (its owner hidden or destroyed) lets its lock go at once, and a scope whose owner is gone
    /// (<see cref="KeyScope.Alive"/> false) is removed at the next key dispatch or tick: no scope stays on the stack for ever.</item>
    /// <item>uGUI's navigation is wanted off while any open scope takes it (a drop-down: its arrows and Enter would move and submit an
    /// unseen selection, rule 54); a field leaves it on (the field consumes its own keys).</item>
    /// <item>A scope can close while it isn't on top (the window under a dialog closing): the stack keeps its order.</item>
    /// </list>
    /// </summary>
    [NotAWidget("the stack that decides which widget gets the keyboard, not a widget")]
    public sealed class KeyboardScopes
    {
        private readonly IScopeKeyboard     keyboard;
        private readonly List<KeyScope>     open      = new List<KeyScope>();
        private readonly List<KeyScope>     releasing = new List<KeyScope>();
        private          ScopeKeys          swallowed;
        private          int                dispatched = int.MinValue;
        private          int                topFrame   = int.MinValue;

        public KeyboardScopes(IScopeKeyboard keyboard) => this.keyboard = keyboard;

        /// <summary>Called after any change to the stack, a lock or the navigation wanted.</summary>
        public event Action Changed;

        /// <summary>The open scopes, bottom first.</summary>
        public IReadOnlyList<KeyScope> OpenScopes => open;

        /// <summary>The scope that gets the keys (null: none open).</summary>
        public KeyScope Top => open.Count > 0 ? open[open.Count - 1] : null;

        /// <summary>Whether any scope holds its lock (open, or closed and waiting for its keys to come up).</summary>
        public bool LockWanted => open.Count > 0 || releasing.Count > 0;

        /// <summary>Whether uGUI's navigation events should be off: an open scope takes them.</summary>
        public bool NavigationOff
        {
            get
            {
                for (int i = 0; i < open.Count; i++)
                    if (open[i].TakesNavigation) return true;
                return false;
            }
        }

        /// <summary>A scope for <paramref name="owner"/>, closed. It routes <paramref name="routes"/> while on top.</summary>
        /// <param name="alive">Whether the owner still exists (null: always); a scope whose owner is gone is removed.</param>
        public KeyScope Create(object owner, ScopeKeys routes, bool takesNavigation = false, Func<bool> alive = null) =>
            new KeyScope(this, owner, routes, takesNavigation, alive);

        internal void OpenScope(KeyScope s)
        {
            if (s.IsOpen) return;
            bool wasLocked = s.Locked;
            releasing.Remove(s);
            s.Releasing = false;
            open.Add(s);
            s.IsOpen = true;
            TopChanged();
            if (!wasLocked) s.RaiseLock(true);
            Changed?.Invoke();
        }

        internal void CloseScope(KeyScope s, bool atOnce)
        {
            int i = open.IndexOf(s);
            bool wasTop = i >= 0 && i == open.Count - 1;
            if (!s.Locked) return;
            if (i >= 0)
            {
                open.RemoveAt(i);
                s.IsOpen = false;
            }
            if (atOnce)
            {
                releasing.Remove(s);
                s.Releasing = false;
                s.RaiseLock(false);
            }
            else if (!s.Releasing)
            {
                s.Releasing = true;
                s.ClosedFrame = keyboard.Frame;
                releasing.Add(s);
            }
            if (wasTop) TopChanged();
            ForgetIfIdle();
            Changed?.Invoke();
        }

        // with no scope holding a lock nobody reads the keys, and the ones kept from a top would never be seen coming up
        private void ForgetIfIdle()
        {
            if (!LockWanted) swallowed = ScopeKeys.None;
        }

        // the keys held as the top changes are kept from the new top until they come up
        private void TopChanged()
        {
            swallowed |= keyboard.Held;
            topFrame  =  keyboard.Frame;
        }

        /// <summary>Removes the scopes whose owner is gone, letting their locks go.</summary>
        private void Prune()
        {
            for (int i = open.Count - 1; i >= 0; i--)
                if (open[i].Alive != null && !open[i].Alive())
                    CloseScope(open[i], true);
            for (int i = releasing.Count - 1; i >= 0; i--)
                if (releasing[i].Alive != null && !releasing[i].Alive())
                    CloseScope(releasing[i], true);
        }

        /// <summary>
        /// Gives this frame's keys to the top scope (once per frame: a second call in the same frame does nothing).
        /// <paramref name="typed"/> is read only when the top scope routes Text; null is no text.
        /// </summary>
        public void Dispatch(ScopeKeys down, Func<string> typed = null)
        {
            int frame = keyboard.Frame;
            if (frame == dispatched) return;
            dispatched = frame;
            Prune();
            swallowed &= keyboard.Held;
            KeyScope top = Top;
            if (top == null) return;
            ScopeKeys keys = down & ~swallowed & top.Routes;
            if ((keys & ScopeKeys.Escape) != 0)
            {
                // the top scope alone, and nothing else this frame
                top.Close();
                top.RaiseDismissed();
                return;
            }
            for (int k = (int)ScopeKeys.Up; k <= (int)ScopeKeys.Tab; k <<= 1)
            {
                // Escape closed the scope above; typed text goes to Typed below
                if (k == (int)ScopeKeys.Escape || k == (int)ScopeKeys.Text || (keys & (ScopeKeys)k) == 0) continue;
                top.RaiseKey((ScopeKeys)k);
                // the key closed the scope (Enter picking an entry): the rest of the frame's keys are not for the next one
                if (Top != top) return;
            }
            // the text of the frame the top changed was typed for whatever was on top before (the key that opened this scope)
            if ((top.Routes & ScopeKeys.Text) == 0 || typed == null || !top.HasTyped || frame == topFrame) return;
            string s = typed();
            if (string.IsNullOrEmpty(s)) return;
            for (int i = 0; i < s.Length && Top == top; i++)
                if (char.IsLetterOrDigit(s[i]))
                    top.RaiseTyped(s[i]);
        }

        /// <summary>Once a frame, after every host's update: lets go of the locks of closed scopes whose keys are up.</summary>
        public void Tick()
        {
            Prune();
            if (releasing.Count == 0) return;
            int frame = keyboard.Frame;
            if ((keyboard.Held & ScopeKeys.Closing) != 0) return;
            bool any = false;
            for (int i = releasing.Count - 1; i >= 0; i--)
            {
                KeyScope s = releasing[i];
                if (frame <= s.ClosedFrame) continue;
                releasing.RemoveAt(i);
                s.Releasing = false;
                s.RaiseLock(false);
                any = true;
            }
            if (!any) return;
            ForgetIfIdle();
            Changed?.Invoke();
        }
    }

    /// <summary>One keyboard scope of a <see cref="KeyboardScopes"/> stack: its owner, the keys it routes, its lock.</summary>
    [NotAWidget("one entry of the keyboard scopes, owned by a widget")]
    public sealed class KeyScope
    {
        private readonly KeyboardScopes stack;

        internal KeyScope(KeyboardScopes stack, object owner, ScopeKeys routes, bool takesNavigation, Func<bool> alive)
        {
            this.stack      = stack;
            Owner           = owner;
            Routes          = routes;
            TakesNavigation = takesNavigation;
            Alive           = alive;
        }

        public object    Owner           { get; }

        /// <summary>
        /// The keys the scope routes to its owner while on top. A field's scope routes none by default; its owner asks for the keys
        /// it reads (a stepper's Up and Down, a prompt's Tab) by adding them, so every key a KodeKit widget reads comes through the
        /// stack: from the top scope only, and never a key held across a change of the top.
        /// </summary>
        public ScopeKeys Routes          { get; set; }
        public bool      TakesNavigation { get; }
        public Func<bool> Alive          { get; }

        /// <summary>Whether the scope is on the stack.</summary>
        public bool IsOpen { get; internal set; }

        /// <summary>Whether the scope is on top of the stack: it gets the keys.</summary>
        public bool OnTop => stack.Top == this;

        /// <summary>Closed, its lock still held until Escape and Enter are up.</summary>
        public bool Releasing { get; internal set; }

        internal int ClosedFrame;

        /// <summary>Whether the scope holds its lock: open, or releasing.</summary>
        public bool Locked => IsOpen || Releasing;

        /// <summary>A routed key went down while the scope was on top (not Escape, which closes it: <see cref="Dismissed"/>).</summary>
        public event Action<ScopeKeys> Key;

        /// <summary>A letter or digit was typed while the scope was on top (when it routes Text).</summary>
        public event Action<char> Typed;

        /// <summary>Escape closed the scope.</summary>
        public event Action Dismissed;

        /// <summary>The scope's lock is to be taken (true) or let go (false).</summary>
        public event Action<bool> LockChanged;

        internal void RaiseKey(ScopeKeys k) => Key?.Invoke(k);

        internal bool HasTyped => Typed != null;

        internal void RaiseTyped(char c) => Typed?.Invoke(c);

        internal void RaiseDismissed() => Dismissed?.Invoke();

        internal void RaiseLock(bool on) => LockChanged?.Invoke(on);

        /// <summary>Puts the scope on top (nothing if it is open). A scope waiting for its keys keeps its lock.</summary>
        public void Open() => stack.OpenScope(this);

        /// <summary>Takes the scope off the stack; its lock goes once Escape and Enter are up, a frame later at the soonest.</summary>
        public void Close() => stack.CloseScope(this, false);

        /// <summary>Takes the scope off the stack and lets its lock go now (its owner hidden or destroyed).</summary>
        public void Drop() => stack.CloseScope(this, true);
    }
}
