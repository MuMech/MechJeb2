using System;
using System.Collections.Generic;
using System.Reflection;
using KodeKit.Headless;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KodeKit
{
    /// <summary>
    /// The keyboard shortcuts (<see cref="ShortcutMap"/>) in game: one shared map over one shared action registry (the command palette
    /// reuses <see cref="Actions"/>), KSP's keys read at run time, and a driver that polls only the bound chords.
    /// <list type="bullet">
    /// <item>KSP's keys are every <c>KeyBinding</c> of <c>GameSettings</c> (its statics, the custom axes' keys) plus the keys its code
    /// reads (<see cref="ShortcutMap.KspHardCoded"/>, with MODIFIER_KEY's live keys); read again on
    /// <c>GameEvents.OnGameSettingsApplied</c>, so a KSP key rebound onto a MechJeb chord shows as a conflict and stops it firing.</item>
    /// <item>The keyboard is busy (nothing fires) while a KodeKit keyboard scope holds its lock, while the game is paused
    /// (<c>FlightDriver.Pause</c>: the pause menu, KSP's settings and key capture opened from it, the space centre's menu), while KSP's whole keyboard lock is
    /// taken (<c>(LockMask &amp; KEYBOARDINPUT) == KEYBOARDINPUT</c>, never <c>IsLocked</c>, which any partial lock such as Alt+L's
    /// stage lock would trip), while uGUI's selected object is a text field, or while an IMGUI control has the keyboard.</item>
    /// <item>A popup layer (a drop-down's list, a context menu, a dialog) pauses the shortcuts.</item>
    /// <item>The driver runs only while a chord is bound or a capture is on. A capture holds a keyboard scope (KSP's keyboard lock),
    /// so the key pressed to try a chord doesn't also fly the vessel; the lock stays until every key is up.</item>
    /// </list>
    /// </summary>
    public static class Shortcuts
    {
        /// <summary>The actions the player can run by id (window toggles), shared with the command palette.</summary>
        public static readonly ActionRegistry Actions = new ActionRegistry();

        /// <summary>The bindings. Public for tests and the agent bridge.</summary>
        public static readonly ShortcutMap Map = MakeMap();

        /// <summary>The last frame's poll (None on a frame with no bound chord pressed).</summary>
        public static ShortcutResult Last => Map.Last;

        /// <summary>What the last pressed bound chord did (fired, or why not), for tests and the agent bridge.</summary>
        public static ShortcutResult LastPress { get; private set; } = ShortcutResult.Nothing;

        /// <summary>The frame of the last poll that found a bound chord pressed (its outcome is <see cref="Last"/>).</summary>
        public static int LastPressFrame { get; private set; } = -1;

        /// <summary>Called whenever a pressed chord was handled (fired or blocked).</summary>
        public static event Action<ShortcutResult> Pressed;

        static ShortcutMap MakeMap()
        {
            var m = new ShortcutMap(Actions, KeyboardBusy, () => Popups.Layers.Count > 0);
            m.Changed += Apply;
            return m;
        }

        // ---- the conditions ----

        const ulong FullKeyboardLock = (ulong)ControlTypes.KEYBOARDINPUT;

        /// <summary>Whether something else has the keyboard now (the rules above).</summary>
        public static bool KeyboardBusy()
        {
            if (KeyboardScope.Scopes.LockWanted) return true;
            // the pause menu and the screens opened from it (KSP's settings, its key capture): its lock leaves PAUSE out of the full mask
            if (FlightDriver.Pause) return true;
            if ((InputLockManager.LockMask & FullKeyboardLock) == FullKeyboardLock) return true;
            if (GUIUtility.keyboardControl != 0) return true;
            GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return sel != null && (sel.GetComponent<TMP_InputField>() != null || sel.GetComponent<UnityEngine.UI.InputField>() != null);
        }

        // ---- KSP's keys ----

        static bool hooked;

        /// <summary>Reads KSP's keys now and hooks their re-reading on every settings apply (once).</summary>
        public static void Start()
        {
            if (!hooked)
            {
                hooked = true;
                // a lambda, not the static method: KSP's EventVoid reads its delegate's Target, null for a static method
                GameEvents.OnGameSettingsApplied.Add(() => RefreshReserved());
            }
            RefreshReserved();
        }

        /// <summary>Reads KSP's bindings and hard-coded keys again (KSP's settings were applied).</summary>
        public static void RefreshReserved()
        {
            layoutMap = null;
            layoutRead = false;
            Map.SetReserved(ReadKsp());
        }

        static List<ReservedKeys> ReadKsp()
        {
            var r = new List<ReservedKeys>();
            foreach (FieldInfo f in typeof(GameSettings).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                object v;
                try { v = f.GetValue(null); }
                catch (Exception) { continue; }
                if (v is KeyBinding kb) AddBinding(r, f.Name, kb);
                else if (v is AxisKeyBindingList list)
                    for (int i = 0; i < list.Length; i++) AddAxisKeys(r, f.Name + "[" + i + "]", list[i]);
                else if (v is AxisKeyBinding ak) AddAxisKeys(r, f.Name, ak);
            }
            var mod = new List<int>();
            KeyBinding modifier = GameSettings.MODIFIER_KEY;
            if (modifier != null)
            {
                if (modifier.primary != null && modifier.primary.code != KeyCode.None) mod.Add((int)modifier.primary.code);
                if (modifier.secondary != null && modifier.secondary.code != KeyCode.None) mod.Add((int)modifier.secondary.code);
            }
            r.AddRange(ShortcutMap.KspHardCoded(mod));
            return r;
        }

        static void AddAxisKeys(List<ReservedKeys> r, string name, AxisKeyBinding ak)
        {
            if (ak == null) return;
            AddBinding(r, name + "+", ak.plusKeyBinding);
            AddBinding(r, name + "-", ak.minusKeyBinding);
        }

        static void AddBinding(List<ReservedKeys> r, string name, KeyBinding kb)
        {
            if (kb == null) return;
            if (kb.primary != null && kb.primary.code != KeyCode.None) r.Add(new ReservedKeys(name, (int)kb.primary.code));
            if (kb.secondary != null && kb.secondary.code != KeyCode.None) r.Add(new ReservedKeys(name, (int)kb.secondary.code));
        }

        // ---- key names ----

        static ConfigNode layoutMap;
        static bool layoutRead;

        /// <summary>A key as KSP's settings screen names it: the layout's KEY_MAP label, or the KeyCode's name.</summary>
        public static string KeyName(int key)
        {
            string code = ((KeyCode)key).ToString();
            if (layoutMap == null && !layoutRead)
            {
                layoutRead = true;
                try
                {
                    // as KSP's settings screen does (SettingsScreen.Start): the chosen layout, else the detected one
                    string layout = GameSettings.CURRENT_LAYOUT_SETTINGS;
                    if (string.IsNullOrEmpty(layout)) layout = GameSettings.DetectKeyboardLayout();
                    if (!string.IsNullOrEmpty(layout) && GameSettings.KeyboardLayouts != null &&
                        GameSettings.KeyboardLayouts.TryGetValue(layout, out ConfigNode n))
                        layoutMap = n?.GetNode("KEY_MAP");
                }
                catch (Exception) { layoutMap = null; }
            }
            string shown = layoutMap != null && layoutMap.HasValue(code) ? layoutMap.GetValue(code) : null;
            return string.IsNullOrEmpty(shown) ? code : shown;
        }

        /// <summary>A chord as shown (<c>RightControl+Y</c>), empty for none.</summary>
        public static string Show(Chord c) => c.Format(KeyName);

        /// <summary>A key's KeyCode name, as the bindings are saved (the same on every layout).</summary>
        public static string CodeName(int key) => ((KeyCode)key).ToString();

        /// <summary>A KeyCode from its name; -1 when unknown.</summary>
        public static int CodeOf(string name)
        {
            try { return string.IsNullOrEmpty(name) ? -1 : (int)(KeyCode)Enum.Parse(typeof(KeyCode), name); }
            catch (ArgumentException) { return -1; }
        }

        /// <summary>The bindings as saved text (KeyCode names).</summary>
        public static string Save() => Map.Save(CodeName);

        /// <summary>Replaces the bindings with saved text.</summary>
        public static void Load(string text) => Map.Load(text, CodeOf);

        /// <summary>
        /// An action's label, or for a binding whose action isn't registered (a custom window renamed or deleted) "missing window
        /// &lt;name&gt;", the name read from its id (<c>window:custom:Foo</c>, <c>window:MechJebModuleFoo</c>).
        /// </summary>
        public static string ActionName(string id)
        {
            UiAction a = Actions.Find(id);
            if (a != null) return a.Label;
            string name = id ?? "";
            if (name.StartsWith("window:custom:")) name = name.Substring("window:custom:".Length);
            else if (name.StartsWith("window:")) name = name.Substring("window:".Length);
            return Localizer.Format("#MechJeb_NewUI_ShortcutMissing", name);
        }

        /// <summary>Why a chord can't be bound, or doesn't fire, in words (empty when it is fine).</summary>
        public static string Describe(ChordCheck k, Chord c)
        {
            switch (k.Problem)
            {
                case ChordProblem.None: return "";
                case ChordProblem.NoKey: return Localizer.Format("#MechJeb_NewUI_ShortcutNoKey");
                case ChordProblem.NotAKey: return Localizer.Format("#MechJeb_NewUI_ShortcutNotAKey", KeyName(k.Key != 0 ? k.Key : c.Key));
                case ChordProblem.UiKey: return Localizer.Format("#MechJeb_NewUI_ShortcutUiKey", KeyName(k.Key));
                case ChordProblem.AltChord: return Localizer.Format("#MechJeb_NewUI_ShortcutAlt");
                case ChordProblem.SystemChord: return Localizer.Format("#MechJeb_NewUI_ShortcutSystem");
                case ChordProblem.Taken:
                    return Localizer.Format("#MechJeb_NewUI_ShortcutTaken", Show(c), ActionName(k.Other));
                case ChordProblem.Reserved:
                    // KSP's own name for the binding when it has one (its localization), else "a key KSP uses": a GameSettings field
                    // name or a file and line of KSP's code is for the log, not the player
                    string named = BindingName(k.Source);
                    return named != null
                        ? Localizer.Format("#MechJeb_NewUI_ShortcutKspBinding", KeyName(k.Key), named)
                        : Localizer.Format("#MechJeb_NewUI_ShortcutKspKey", KeyName(k.Key));
                default: return k.Problem.ToString();
            }
        }

        // GameSettings' bindings and KSP's own localization keys for their names, as KSP 1.12.5's InputSettings dialog lists them
        static readonly Dictionary<string, string> bindingNames = new Dictionary<string, string>
        {
            ["THROTTLE_UP"] = "#autoLOC_900771", ["THROTTLE_DOWN"] = "#autoLOC_900772", ["THROTTLE_FULL"] = "#autoLOC_900791",
            ["THROTTLE_CUTOFF"] = "#autoLOC_900790", ["CAMERA_MODE"] = "#autoLOC_900845", ["CAMERA_NEXT"] = "#autoLOC_900846",
            ["PITCH_UP"] = "#autoLOC_900758", ["PITCH_DOWN"] = "#autoLOC_900757", ["ROLL_LEFT"] = "#autoLOC_900761",
            ["ROLL_RIGHT"] = "#autoLOC_900762", ["YAW_LEFT"] = "#autoLOC_900759", ["YAW_RIGHT"] = "#autoLOC_900760",
            ["SAS_HOLD"] = "#autoLOC_900787", ["SAS_TOGGLE"] = "#autoLOC_900788", ["LAUNCH_STAGES"] = "#autoLOC_148071",
            ["RCS_TOGGLE"] = "#autoLOC_900789", ["TRANSLATE_FWD"] = "#autoLOC_900768", ["TRANSLATE_BACK"] = "#autoLOC_900769",
            ["TRANSLATE_DOWN"] = "#autoLOC_900765", ["TRANSLATE_LEFT"] = "#autoLOC_900766", ["TRANSLATE_UP"] = "#autoLOC_900764",
            ["TRANSLATE_RIGHT"] = "#autoLOC_900767", ["NAVBALL_TOGGLE"] = "#autoLOC_148942", ["TAKE_SCREENSHOT"] = "#autoLOC_900837",
            ["TOGGLE_UI"] = "#autoLOC_148195", ["TOGGLE_STATUS_SCREEN"] = "#autoLOC_148199", ["TOGGLE_LABELS"] = "#autoLOC_148198",
            ["QUICKSAVE"] = "#autoLOC_900840", ["QUICKLOAD"] = "#autoLOC_900842", ["MAP_VIEW_TOGGLE"] = "#autoLOC_900831",
            ["CAMERA_RESET"] = "#autoLOC_900847", ["TIME_WARP_INCREASE"] = "#autoLOC_900832", ["TIME_WARP_DECREASE"] = "#autoLOC_900833"
        };

        /// <summary>KSP's localized name of a binding (null: KSP names it nowhere, or its language has no text for it).</summary>
        public static string BindingName(string source)
        {
            if (source == null || !bindingNames.TryGetValue(source, out string key)) return null;
            string s = Localizer.Format(key);
            return string.IsNullOrEmpty(s) || s.StartsWith("#autoLOC") ? null : s;
        }

        // ---- the driver ----

        static GameObject driverObject;
        static Driver driver;
        static KeyboardScope captureScope;

        static void Apply()
        {
            // a capture that ended keeps the keyboard lock until every key is up: the key it took, still held, would fly the vessel
            if (Map.Capturing == null && captureScope != null && captureScope.Scope.IsOpen) releasing = true;
            bool busy = Map.Count > 0 || Map.Capturing != null || releasing;
            if (driver == null && busy)
            {
                driverObject = new GameObject("KodeKitShortcuts");
                UnityEngine.Object.DontDestroyOnLoad(driverObject);
                driver = driverObject.AddComponent<Driver>();
                captureScope = driverObject.AddComponent<KeyboardScope>();
                captureScope.Configure(ScopeKeys.None, false);
            }
            if (driver != null && driver.enabled != busy) driver.enabled = busy;
            if (captureScope == null) return;
            if (Map.Capturing != null && !captureScope.Locked)
            {
                releasing = false;
                captureScope.Lock();
            }
        }

        // a capture ended, its lock held until every key is up
        static bool releasing;

        static void ReleaseWhenKeysUp()
        {
            if (Input.anyKey) return;
            releasing = false;
            if (captureScope != null && captureScope.Scope.IsOpen) captureScope.UnlockAfterKeys();
            Apply();
        }

        /// <summary>Polls the bound chords each frame, or reads the next key while a capture is on.</summary>
        sealed class Driver : MonoBehaviour
        {
            static readonly UnityKeys keys = new UnityKeys();

            void Update()
            {
                if (Map.Capturing != null)
                {
                    Capture();
                    return;
                }
                if (releasing)
                {
                    ReleaseWhenKeysUp();
                    return;
                }
                ShortcutResult r = Map.Poll(keys);
                if (r.Outcome == ShortcutOutcome.None) return;
                LastPressFrame = Time.frameCount;
                LastPress = r;
                Pressed?.Invoke(r);
            }

            // the first key down this frame (a modifier alone waits for the key), with the modifiers held
            static void Capture()
            {
                if (!Input.anyKeyDown) return;
                ShortcutMods held = ShortcutMap.HeldMods(keys);
                for (int k = 1; k < ShortcutKeys.Mouse0; k++)
                {
                    if (!Input.GetKeyDown((KeyCode)k)) continue;
                    if (Map.CaptureKey(k, held)) return;
                }
                // a click anywhere cancels; a joystick button is refused with its words
                for (int k = ShortcutKeys.Mouse0; k <= (int)KeyCode.Joystick8Button19; k++)
                {
                    if (!Input.GetKeyDown((KeyCode)k)) continue;
                    if (k <= (int)KeyCode.Mouse6) Map.CancelCapture();
                    else Map.CaptureKey(k, held);
                    return;
                }
            }

            sealed class UnityKeys : IShortcutKeys
            {
                public bool Down(int key) => Input.GetKeyDown((KeyCode)key);
                public bool Held(int key) => Input.GetKey((KeyCode)key);
            }
        }
    }
}
