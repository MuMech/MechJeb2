#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Text;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>The modifier keys of a chord, each side its own key (Left Shift is KSP's throttle up by default, Right Shift is free).</summary>
    [Flags]
    public enum ShortcutMods
    {
        None         = 0,
        LeftShift    = 1 << 0,
        RightShift   = 1 << 1,
        LeftControl  = 1 << 2,
        RightControl = 1 << 3,
        LeftAlt      = 1 << 4,
        RightAlt     = 1 << 5,
        AltGr        = 1 << 6,
        LeftCommand  = 1 << 7,
        RightCommand = 1 << 8,
        LeftWindows  = 1 << 9,
        RightWindows = 1 << 10,

        Alt    = LeftAlt | RightAlt | AltGr,
        System = LeftCommand | RightCommand | LeftWindows | RightWindows
    }

    /// <summary>The key codes the shortcuts know by number: Unity's <c>KeyCode</c> values, without Unity.</summary>
    public static class ShortcutKeys
    {
        public const int None = 0, Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32, Insert = 277, Delete = 127,
            BackQuote = 96, Alpha0 = 48, A = 97, C = 99, D = 100, V = 118, PageUp = 280, PageDown = 281, L = 108, S = 115, W = 119, X = 120, Y = 121, Z = 122, KeypadEnter = 271,
            UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276, F1 = 282, F8 = 289, F12 = 293, F15 = 296,
            NumLock = 300, CapsLock = 301, ScrollLock = 302, RightShift = 303, LeftShift = 304, RightControl = 305, LeftControl = 306,
            RightAlt = 307, LeftAlt = 308, RightCommand = 309, LeftCommand = 310, LeftWindows = 311, RightWindows = 312, AltGr = 313,
            Mouse0 = 323;

        /// <summary>The modifier keys, in the order of <see cref="ShortcutMods"/>'s flags.</summary>
        public static readonly int[] Modifiers =
        {
            LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, AltGr, LeftCommand, RightCommand, LeftWindows, RightWindows
        };

        /// <summary>The flag of a modifier key (None: not a modifier).</summary>
        public static ShortcutMods ModOf(int key)
        {
            for (int i = 0; i < Modifiers.Length; i++)
                if (Modifiers[i] == key) return (ShortcutMods)(1 << i);
            return ShortcutMods.None;
        }

        /// <summary>A keyboard key a chord can end on: not a modifier, a lock key, a mouse or joystick button.</summary>
        public static bool IsMainKey(int key) => key > 0 && key < NumLock;
    }

    /// <summary>A key and the modifier keys held with it. <see cref="Key"/> is a Unity <c>KeyCode</c> value.</summary>
    public struct Chord : IEquatable<Chord>
    {
        public readonly int          Key;
        public readonly ShortcutMods Mods;

        public Chord(int key, ShortcutMods mods = ShortcutMods.None)
        {
            Key  = key;
            Mods = mods;
        }

        public static readonly Chord Empty = new Chord(ShortcutKeys.None);

        public bool IsEmpty => Key == ShortcutKeys.None;

        /// <summary>Whether pressing the chord presses <paramref name="key"/> (its key or one of its modifiers).</summary>
        public bool Presses(int key) => key == Key || (Mods & ShortcutKeys.ModOf(key)) != 0;

        /// <summary>The keys pressed, modifiers first (in <see cref="ShortcutMods"/>' order), then the key.</summary>
        public List<int> Keys()
        {
            var r = new List<int>();
            for (int i = 0; i < ShortcutKeys.Modifiers.Length; i++)
                if ((Mods & (ShortcutMods)(1 << i)) != 0) r.Add(ShortcutKeys.Modifiers[i]);
            if (!IsEmpty) r.Add(Key);
            return r;
        }

        /// <summary>The keys' names joined by '+', modifiers first (<c>RightControl+Y</c>); empty for no chord.</summary>
        public string Format(Func<int, string> keyName)
        {
            if (IsEmpty) return "";
            var sb = new StringBuilder();
            List<int> keys = Keys();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0) sb.Append('+');
                sb.Append(keyName(keys[i]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// A chord from <see cref="Format"/>'s text: the last name is the key, the others modifiers. Empty when a name is unknown
        /// (<paramref name="keyCode"/> returns a negative number) or a modifier name isn't a modifier.
        /// </summary>
        public static Chord Parse(string text, Func<string, int> keyCode)
        {
            if (string.IsNullOrEmpty(text)) return Empty;
            string[] parts = text.Split('+');
            ShortcutMods mods = ShortcutMods.None;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                ShortcutMods m = ShortcutKeys.ModOf(keyCode(parts[i].Trim()));
                if (m == ShortcutMods.None) return Empty;
                mods |= m;
            }
            int key = keyCode(parts[parts.Length - 1].Trim());
            return key <= 0 ? Empty : new Chord(key, mods);
        }

        public bool Equals(Chord o) => Key == o.Key && Mods == o.Mods;
        public override bool Equals(object o) => o is Chord c && Equals(c);
        public override int GetHashCode() => Key * 4099 + (int)Mods;
        public static bool operator ==(Chord a, Chord b) => a.Equals(b);
        public static bool operator !=(Chord a, Chord b) => !a.Equals(b);
        public override string ToString() => Format(k => k.ToString());
    }

    /// <summary>
    /// Keys another part of the game reads: a KSP key binding (one key), or a key KSP reads in its code, alone or with others
    /// (<c>LeftControl+Z</c> in the editor). A chord that presses all of <see cref="Keys"/> would also do what they do.
    /// </summary>
    [NotAWidget("data: keys another part of the game reads, for the shortcuts' conflict check")]
    public sealed class ReservedKeys
    {
        public ReservedKeys(string source, params int[] keys)
        {
            Source = source;
            Keys   = keys;
        }

        /// <summary>What reads them: a binding's name (<c>PITCH_DOWN</c>) or a file and line of KSP's code.</summary>
        public string Source { get; }

        /// <summary>The keys, all held together (one for a binding).</summary>
        public IReadOnlyList<int> Keys { get; }

        /// <summary>Whether pressing <paramref name="c"/> presses every one of <see cref="Keys"/>.</summary>
        public bool HitBy(Chord c)
        {
            if (Keys.Count == 0) return false;
            for (int i = 0; i < Keys.Count; i++)
                if (Keys[i] <= 0 || !c.Presses(Keys[i])) return false;
            return true;
        }
    }

    /// <summary>Why a chord can't be bound or doesn't fire.</summary>
    public enum ChordProblem
    {
        None,

        /// <summary>No key.</summary>
        NoKey,

        /// <summary>A mouse or joystick button, a lock key, or a modifier on its own.</summary>
        NotAKey,

        /// <summary>A key the windows and KSP's dialogs read for themselves (Escape, Enter, Tab).</summary>
        UiKey,

        /// <summary>A chord with Alt: KSP's MODIFIER_KEY (Alt+L, Alt+X, the trim keys, Alt+F12).</summary>
        AltChord,

        /// <summary>A chord with Command or Windows: the operating system's.</summary>
        SystemChord,

        /// <summary>A key KSP reads (a binding or its code): <see cref="ChordCheck.Source"/> says which.</summary>
        Reserved,

        /// <summary>Another action has the chord: <see cref="ChordCheck.Other"/>.</summary>
        Taken
    }

    /// <summary>The result of checking a chord: its problem, and what it collides with.</summary>
    public struct ChordCheck
    {
        public readonly ChordProblem Problem;

        /// <summary>The binding or code that reads the key (<see cref="ChordProblem.Reserved"/>).</summary>
        public readonly string Source;

        /// <summary>The key of the chord that KSP reads (<see cref="ChordProblem.Reserved"/>; its first key for a set of keys).</summary>
        public readonly int Key;

        /// <summary>The action that has the chord (<see cref="ChordProblem.Taken"/>).</summary>
        public readonly string Other;

        public ChordCheck(ChordProblem problem, string source = null, int key = 0, string other = null)
        {
            Problem = problem;
            Source  = source;
            Key     = key;
            Other   = other;
        }

        public bool Ok => Problem == ChordProblem.None;

        public static readonly ChordCheck Fine = new ChordCheck(ChordProblem.None);
    }

    /// <summary>What a frame's keys did.</summary>
    public enum ShortcutOutcome
    {
        /// <summary>No bound chord was pressed.</summary>
        None,

        /// <summary>A bound chord ran its action.</summary>
        Fired,

        /// <summary>A bound chord was pressed while a keyboard scope (a field, a drop-down, another mod's text field) had the keys.</summary>
        KeyboardBusy,

        /// <summary>A bound chord was pressed under a blocking layer (a dialog, a popup).</summary>
        Layer,

        /// <summary>A bound chord was pressed, but KSP now reads one of its keys (rebound after it was bound).</summary>
        Conflict,

        /// <summary>A bound chord was pressed, but its action is gone or can't run now.</summary>
        Unavailable
    }

    /// <summary>What a poll found: the outcome and the action it is about.</summary>
    public struct ShortcutResult
    {
        public readonly ShortcutOutcome Outcome;
        public readonly string          ActionId;

        public ShortcutResult(ShortcutOutcome outcome, string actionId)
        {
            Outcome  = outcome;
            ActionId = actionId;
        }

        public static readonly ShortcutResult Nothing = new ShortcutResult(ShortcutOutcome.None, null);
    }

    /// <summary>The keyboard as the shortcuts read it.</summary>
    public interface IShortcutKeys
    {
        /// <summary>Whether <paramref name="key"/> went down this frame.</summary>
        bool Down(int key);

        /// <summary>Whether <paramref name="key"/> is held.</summary>
        bool Held(int key);
    }

    /// <summary>One action's chord, and whether it collides now.</summary>
    [NotAWidget("data: one binding of the shortcut map")]
    public sealed class ShortcutBinding
    {
        internal ShortcutBinding(string actionId, Chord chord)
        {
            ActionId = actionId;
            Chord    = chord;
        }

        public string ActionId { get; }
        public Chord  Chord    { get; internal set; }

        /// <summary>What it collides with now (KSP's bindings as last given): a binding with a problem doesn't fire.</summary>
        public ChordCheck Live { get; internal set; }
    }

    /// <summary>
    /// The keyboard shortcuts: chords bound to the actions of an <see cref="ActionRegistry"/>, opt-in (none bound by default), and the
    /// rules that keep them off KSP's keys. KodeKit's <c>Shortcuts</c> host polls it each frame and gives it KSP's live bindings.
    /// <list type="bullet">
    /// <item>A chord is a key, alone or with Shift or Control held, either side (a bare F8 is a chord). The key is a keyboard key: no modifier alone, no lock key, no
    /// mouse or joystick button; not Escape, Enter or Tab, which the windows and KSP's dialogs read for themselves.</item>
    /// <item>A chord with Alt is refused (KSP's MODIFIER_KEY: Alt+L, Alt+X, the trim keys, Alt+F12), and one with Command or Windows
    /// (the operating system's).</item>
    /// <item>Every key a chord presses, the modifiers included, is tested against the keys KSP reads (<see cref="SetReserved"/>):
    /// KSP's bindings read at run time and the keys its code reads (<see cref="KspHardCoded"/>). So Left Shift and Left Control
    /// (throttle by default) are refused and the right-hand ones are not, unless the player bound them.</item>
    /// <item>Two actions on one chord is an error at bind time (<see cref="ChordProblem.Taken"/>); an action has one chord, binding
    /// another replaces it.</item>
    /// <item>A chord bound before KSP's bindings changed keeps its binding but shows its conflict (<see cref="ShortcutBinding.Live"/>)
    /// and doesn't fire until the conflict is gone.</item>
    /// <item>A chord fires on its key going down with exactly its modifiers held (none other).</item>
    /// <item>Nothing fires while a keyboard scope has the keys (a field, an open drop-down, another mod's text field), under a
    /// blocking layer (a dialog, a popup), or while a chord is being captured; an action that can't run now doesn't run.</item>
    /// <item>At most one action runs per frame.</item>
    /// <item>A poll with no binding reads no key; a poll reads only the bound chords' keys, and the modifiers only when one of them
    /// went down. Polling allocates nothing.</item>
    /// </list>
    /// </summary>
    [NotAWidget("the shortcuts' bindings and rules, polled by a host; the Settings row that edits it is made of plain widgets")]
    public sealed class ShortcutMap
    {
        private readonly ActionRegistry        actions;
        private readonly Func<bool>            keyboardBusy;
        private readonly Func<bool>            layerOpen;
        private readonly List<ShortcutBinding> bindings  = new List<ShortcutBinding>();
        private readonly List<ReservedKeys>    reserved  = new List<ReservedKeys>();

        /// <param name="keyboardBusy">Whether a keyboard scope has the keys now.</param>
        /// <param name="layerOpen">Whether a blocking layer is open now.</param>
        public ShortcutMap(ActionRegistry actions, Func<bool> keyboardBusy, Func<bool> layerOpen)
        {
            this.actions      = actions ?? throw new ArgumentNullException(nameof(actions));
            this.keyboardBusy = keyboardBusy;
            this.layerOpen    = layerOpen;
        }

        public ActionRegistry Actions => actions;

        /// <summary>Called after a binding is added, changed or cleared, the live conflicts change, or a capture starts or ends.</summary>
        public event Action Changed;

        /// <summary>Grows by one at every <see cref="Changed"/>.</summary>
        public int Version { get; private set; }

        /// <summary>The bindings, in the order they were made.</summary>
        public IReadOnlyList<ShortcutBinding> Bindings => bindings;

        public int Count => bindings.Count;

        /// <summary>The keys KSP reads, as last given.</summary>
        public IReadOnlyList<ReservedKeys> Reserved => reserved;

        /// <summary>The last poll's result.</summary>
        public ShortcutResult Last { get; private set; } = ShortcutResult.Nothing;

        private void Touch()
        {
            Version++;
            Changed?.Invoke();
        }

        // ---- KSP's keys ----

        /// <summary>
        /// The keys KSP reads now (its bindings and its code's keys), replacing the last list: every binding's conflict is checked
        /// again. A host calls it at the start and whenever KSP's settings are applied.
        /// </summary>
        public void SetReserved(IEnumerable<ReservedKeys> keys)
        {
            reserved.Clear();
            if (keys != null) reserved.AddRange(keys);
            for (int i = 0; i < bindings.Count; i++) bindings[i].Live = CheckKeys(bindings[i].Chord);
            Touch();
        }

        /// <summary>
        /// The keys KSP 1.12.5's code reads for itself, outside its bindings, that a chord could press (the file and line in KSP's
        /// Assembly-CSharp). <paramref name="modifierKeys"/> are the keys bound to MODIFIER_KEY now (Alt by default; Alt+L and Alt+X
        /// go with whatever it is).
        /// </summary>
        public static List<ReservedKeys> KspHardCoded(IEnumerable<int> modifierKeys)
        {
            var r = new List<ReservedKeys>
            {
                new ReservedKeys("PlanetariumCamera.cs:250 (Tab: map focus)", ShortcutKeys.Tab),
                new ReservedKeys("EditorLogic.cs:2756 (LeftControl+Z: undo)", ShortcutKeys.LeftControl, ShortcutKeys.Z),
                new ReservedKeys("EditorLogic.cs:2785 (LeftControl+Y: redo)", ShortcutKeys.LeftControl, ShortcutKeys.Y),
                new ReservedKeys("EditorLogic.cs:2750 (LeftCommand+Z: undo)", ShortcutKeys.LeftCommand, ShortcutKeys.Z),
                new ReservedKeys("EditorLogic.cs:2777 (LeftCommand+Y: redo)", ShortcutKeys.LeftCommand, ShortcutKeys.Y),
                new ReservedKeys("EditorLogic.cs:3178 (Delete: delete part)", ShortcutKeys.Delete),
                new ReservedKeys("EditorLogic.cs:1245 (Insert)", ShortcutKeys.Insert),
                new ReservedKeys("CraftBrowserDialog.cs:430 (BackQuote)", ShortcutKeys.BackQuote),
                new ReservedKeys("KSPWheelController.cs:269 (W A S D Space: wheels)", ShortcutKeys.W),
                new ReservedKeys("KSPWheelController.cs:243 (W A S D Space: wheels)", ShortcutKeys.A),
                new ReservedKeys("KSPWheelController.cs:263 (W A S D Space: wheels)", ShortcutKeys.S),
                new ReservedKeys("KSPWheelController.cs:249 (W A S D Space: wheels)", ShortcutKeys.D),
                new ReservedKeys("KSPWheelController.cs:275 (W A S D Space: wheels)", ShortcutKeys.Space),
                new ReservedKeys("MenuNavigation.cs:143 (arrows: menus)", ShortcutKeys.UpArrow),
                new ReservedKeys("MenuNavigation.cs:147 (arrows: menus)", ShortcutKeys.DownArrow),
                new ReservedKeys("MenuNavigation.cs:151 (arrows: menus)", ShortcutKeys.LeftArrow),
                new ReservedKeys("MenuNavigation.cs:155 (arrows: menus)", ShortcutKeys.RightArrow),
                // the space centre's camera
                new ReservedKeys("SpaceCenterCamera2.cs:199 (Alpha0: KSC camera)", ShortcutKeys.Alpha0),
                new ReservedKeys("SpaceCenterCamera2.cs:203 (Alpha1: KSC camera)", ShortcutKeys.Alpha0 + 1),
                new ReservedKeys("SpaceCenterCamera2.cs:207 (Alpha2: KSC camera)", ShortcutKeys.Alpha0 + 2),
                new ReservedKeys("SpaceCenterCamera2.cs:211 (Alpha3: KSC camera)", ShortcutKeys.Alpha0 + 3),
                new ReservedKeys("SpaceCenterCamera2.cs:235 (PageUp: KSC camera)", ShortcutKeys.PageUp),
                new ReservedKeys("SpaceCenterCamera2.cs:235 (C: KSC camera)", ShortcutKeys.C),
                new ReservedKeys("SpaceCenterCamera2.cs:239 (PageDown: KSC camera)", ShortcutKeys.PageDown),
                // the Mission Builder
                new ReservedKeys("MissionEditorLogic.cs:1986 (Backspace: delete)", ShortcutKeys.Backspace),
                new ReservedKeys("MissionEditorLogic.cs:2115 (LeftControl+C: copy)", ShortcutKeys.LeftControl, ShortcutKeys.C),
                new ReservedKeys("MissionEditorLogic.cs:2115 (RightControl+C: copy)", ShortcutKeys.RightControl, ShortcutKeys.C),
                new ReservedKeys("MissionEditorLogic.cs:2119 (LeftControl+V: paste)", ShortcutKeys.LeftControl, ShortcutKeys.V),
                new ReservedKeys("MissionEditorLogic.cs:2119 (RightControl+V: paste)", ShortcutKeys.RightControl, ShortcutKeys.V)
            };
            if (modifierKeys != null)
                foreach (int m in modifierKeys)
                {
                    if (m <= 0) continue;
                    r.Add(new ReservedKeys("FlightInputHandler.cs:196 (MODIFIER_KEY+L: stage lock)", m, ShortcutKeys.L));
                    r.Add(new ReservedKeys("FlightInputHandler.cs:645 (MODIFIER_KEY+X: trim reset)", m, ShortcutKeys.X));
                    r.Add(new ReservedKeys("DebugToolbar.cs:276 (MODIFIER_KEY+F12: debug toolbar)", m, ShortcutKeys.F12));
                }
            return r;
        }

        // ---- the rules ----

        /// <summary>Whether <paramref name="c"/> can be bound to <paramref name="actionId"/> now, and if not, why.</summary>
        public ChordCheck Check(Chord c, string actionId)
        {
            ChordCheck k = CheckKeys(c);
            if (!k.Ok) return k;
            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i].Chord == c && bindings[i].ActionId != actionId)
                    return new ChordCheck(ChordProblem.Taken, other: bindings[i].ActionId);
            return ChordCheck.Fine;
        }

        // the rules that don't depend on the other bindings
        private ChordCheck CheckKeys(Chord c)
        {
            if (c.IsEmpty) return new ChordCheck(ChordProblem.NoKey);
            if (!ShortcutKeys.IsMainKey(c.Key)) return new ChordCheck(ChordProblem.NotAKey, key: c.Key);
            if (c.Key == ShortcutKeys.Escape || c.Key == ShortcutKeys.Return || c.Key == ShortcutKeys.KeypadEnter || c.Key == ShortcutKeys.Tab)
                return new ChordCheck(ChordProblem.UiKey, key: c.Key);
            if ((c.Mods & ShortcutMods.Alt) != 0) return new ChordCheck(ChordProblem.AltChord);
            if ((c.Mods & ShortcutMods.System) != 0) return new ChordCheck(ChordProblem.SystemChord);
            for (int i = 0; i < reserved.Count; i++)
                if (reserved[i].HitBy(c))
                    return new ChordCheck(ChordProblem.Reserved, reserved[i].Source, reserved[i].Keys[0]);
            return ChordCheck.Fine;
        }

        // ---- the bindings ----

        /// <summary>The binding of an action (null: none).</summary>
        public ShortcutBinding Find(string actionId)
        {
            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i].ActionId == actionId) return bindings[i];
            return null;
        }

        /// <summary>
        /// Whether a binding's action isn't registered (a custom window renamed or deleted, a window of another mod gone): it can't
        /// fire, still holds its chord, and is cleared like any other.
        /// </summary>
        public bool IsOrphan(ShortcutBinding b) => b != null && actions.Find(b.ActionId) == null;

        /// <summary>The chord of an action (empty: none).</summary>
        public Chord ChordOf(string actionId)
        {
            ShortcutBinding b = Find(actionId);
            return b?.Chord ?? Chord.Empty;
        }

        /// <summary>Binds <paramref name="c"/> to the action, replacing its chord, when <see cref="Check"/> passes; else nothing changes.</summary>
        public ChordCheck Bind(string actionId, Chord c)
        {
            if (string.IsNullOrEmpty(actionId)) return new ChordCheck(ChordProblem.NoKey);
            ChordCheck k = Check(c, actionId);
            if (!k.Ok) return k;
            ShortcutBinding b = Find(actionId);
            if (b == null) bindings.Add(b = new ShortcutBinding(actionId, c));
            else b.Chord = c;
            b.Live = ChordCheck.Fine;
            Touch();
            return k;
        }

        /// <summary>Removes the action's chord; false when it had none.</summary>
        public bool Clear(string actionId)
        {
            ShortcutBinding b = Find(actionId);
            if (b == null) return false;
            bindings.Remove(b);
            Touch();
            return true;
        }

        // ---- capturing the next key ----

        /// <summary>The action whose chord is being captured (null: none).</summary>
        public string Capturing { get; private set; }

        /// <summary>The last capture's result.</summary>
        public ChordCheck LastCapture { get; private set; } = ChordCheck.Fine;

        /// <summary>The chord the last capture tried (empty when it was cancelled).</summary>
        public Chord LastCaptured { get; private set; }

        /// <summary>Starts capturing the next chord for <paramref name="actionId"/>; nothing fires meanwhile.</summary>
        public void BeginCapture(string actionId)
        {
            Capturing    = actionId;
            LastCapture  = ChordCheck.Fine;
            LastCaptured = Chord.Empty;
            Touch();
        }

        /// <summary>Stops capturing with nothing bound.</summary>
        public void CancelCapture()
        {
            if (Capturing == null) return;
            Capturing = null;
            Touch();
        }

        /// <summary>
        /// A key went down while capturing, with <paramref name="held"/> modifiers held. A modifier alone waits for the key; Escape
        /// cancels; any other key ends the capture and is bound when the rules allow it (<see cref="LastCapture"/> says why not).
        /// True when the capture ended.
        /// </summary>
        public bool CaptureKey(int key, ShortcutMods held)
        {
            if (Capturing == null || ShortcutKeys.ModOf(key) != ShortcutMods.None) return false;
            string id = Capturing;
            Capturing = null;
            if (key == ShortcutKeys.Escape && held == ShortcutMods.None)
            {
                Touch();
                return true;
            }
            LastCaptured = new Chord(key, held);
            LastCapture  = Bind(id, LastCaptured);
            if (!LastCapture.Ok) Touch();
            return true;
        }

        // ---- per frame ----

        /// <summary>The modifiers held now.</summary>
        public static ShortcutMods HeldMods(IShortcutKeys keys)
        {
            ShortcutMods m = ShortcutMods.None;
            for (int i = 0; i < ShortcutKeys.Modifiers.Length; i++)
                if (keys.Held(ShortcutKeys.Modifiers[i])) m |= (ShortcutMods)(1 << i);
            return m;
        }

        /// <summary>
        /// Reads this frame's keys and runs at most one action (the rules above). Reads no key with no binding or while capturing.
        /// </summary>
        public ShortcutResult Poll(IShortcutKeys keys)
        {
            ShortcutResult r = ShortcutResult.Nothing;
            if (bindings.Count > 0 && Capturing == null)
            {
                bool modsRead = false;
                ShortcutMods held = ShortcutMods.None;
                for (int i = 0; i < bindings.Count; i++)
                {
                    ShortcutBinding b = bindings[i];
                    if (!keys.Down(b.Chord.Key)) continue;
                    if (!modsRead)
                    {
                        held     = HeldMods(keys);
                        modsRead = true;
                    }
                    if (held != b.Chord.Mods) continue;
                    r = Resolve(b);
                    break;
                }
            }
            Last = r;
            return r;
        }

        private ShortcutResult Resolve(ShortcutBinding b)
        {
            if (keyboardBusy != null && keyboardBusy()) return new ShortcutResult(ShortcutOutcome.KeyboardBusy, b.ActionId);
            if (layerOpen != null && layerOpen()) return new ShortcutResult(ShortcutOutcome.Layer, b.ActionId);
            if (!b.Live.Ok) return new ShortcutResult(ShortcutOutcome.Conflict, b.ActionId);
            UiAction a = actions.Find(b.ActionId);
            if (a == null || !a.Run()) return new ShortcutResult(ShortcutOutcome.Unavailable, b.ActionId);
            return new ShortcutResult(ShortcutOutcome.Fired, b.ActionId);
        }

        // ---- saved text ----

        /// <summary>The bindings as one line: <c>id=chord;id=chord</c>, the chords by <paramref name="keyName"/>.</summary>
        public string Save(Func<int, string> keyName)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bindings.Count; i++)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(Escape(bindings[i].ActionId)).Append('=').Append(bindings[i].Chord.Format(keyName));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Replaces the bindings with <see cref="Save"/>'s text. An entry whose chord doesn't parse or that a fixed rule refuses
        /// (Alt, a UI key, two actions on one chord: the first stays) is dropped; one that KSP reads now is kept and shown as
        /// conflicting; entries are kept even when their action isn't registered yet.
        /// </summary>
        public void Load(string text, Func<string, int> keyCode)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(text))
                foreach (string entry in text.Split(';'))
                {
                    int eq = entry.LastIndexOf('=');
                    if (eq > 0) pairs.Add(new KeyValuePair<string, string>(Unescape(entry.Substring(0, eq)), entry.Substring(eq + 1)));
                }
            Load(pairs, keyCode);
        }

        /// <summary>Replaces the bindings with (action id, chord text) pairs, by the rules of <see cref="Load(string, Func{string, int})"/>.</summary>
        public void Load(IEnumerable<KeyValuePair<string, string>> pairs, Func<string, int> keyCode)
        {
            bindings.Clear();
            if (pairs != null)
                foreach (KeyValuePair<string, string> p in pairs)
                {
                    string id = p.Key;
                    if (string.IsNullOrEmpty(id)) continue;
                    Chord c = Chord.Parse(p.Value, keyCode);
                    ChordCheck k = Check(c, id);
                    // a chord KSP reads now stays bound, shown as conflicting (the player may move KSP's key away again)
                    if (!k.Ok && k.Problem != ChordProblem.Reserved || Find(id) != null) continue;
                    bindings.Add(new ShortcutBinding(id, c) { Live = CheckKeys(c) });
                }
            Touch();
        }

        private static string Escape(string s) => s.Replace("%", "%25").Replace(";", "%3B").Replace("=", "%3D");

        private static string Unescape(string s) => s.Replace("%3D", "=").Replace("%3B", ";").Replace("%25", "%");
    }
}
