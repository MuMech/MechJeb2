using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using KodeKit;
using KodeKit.Headless;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJeb's side of KodeKit's keyboard shortcuts (<see cref="Shortcuts"/>): every window of the menu as a toggle action in the
    /// shared registry, and the bindings kept in MechJeb's global settings (<see cref="MechJebModuleSettings.Shortcuts"/>). Opt-in: no
    /// chord is bound by default. Only window toggles: no action engages an autopilot.
    /// <code>
    /// UiShortcuts { SHORTCUT { action = window:MechJebModuleAscentMenu  chord = RightControl+Y } }
    /// </code>
    /// </summary>
    internal static class WindowShortcuts
    {
        const string Node = "SHORTCUT", ActionKey = "action", ChordKey = "chord";

        /// <summary>The core the actions act on (the active vessel's, or the editor ship's); null between scenes.</summary>
        static MechJebCore core;

        /// <summary>
        /// A window's action id: its module type, or a custom window's name. Two custom windows with one name share one id, and the
        /// registry keeps the first: the chord toggles that one, and the other can't be bound until it is renamed.
        /// </summary>
        public static string IdOf(DisplayModule m) =>
            m is MechJebModuleCustomInfoWindow ? "window:custom:" + m.GetName() : "window:" + m.GetType().Name;

        /// <summary>Whether a window is offered in the menu (and so to the shortcuts).</summary>
        public static bool InMenu(DisplayModule m) => m.ShowInMenu && !m.TechLocked && m.ShowInCurrentScene &&
                                                     !(m is MechJebModulePSGSolutionGraph && m.Core.AscentSettings.AscentType != AscentType.PSG);

        static DisplayModule Find(string id)
        {
            if (core == null) return null;
            List<DisplayModule> modules = core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance);
            for (int i = 0; i < modules.Count; i++)
                if (IdOf(modules[i]) == id) return modules[i];
            return null;
        }

        static bool Shown =>
            core != null && core.ShowGui && (HighLogic.LoadedSceneIsEditor || core.vessel == FlightGlobals.ActiveVessel && core.part.State != PartStates.DEAD);

        static UiAction Toggle(string id, string label) =>
            new UiAction(id, UiActionKind.ToggleWindow, () =>
            {
                DisplayModule m = Find(id);
                return m != null ? m.GetName() : label;
            }, () =>
            {
                DisplayModule m = Find(id);
                if (m != null) m.Enabled = !m.Enabled;
            }, () =>
            {
                if (!Shown) return false;
                DisplayModule m = Find(id);
                return m != null && InMenu(m);
            });

        // ---- per frame, from the host ----

        static int builtSignature;
        static float nextCheck;
        static int loadsRead = -1;
        static bool loading;
        static string savedText;
        static MechJebModuleSettings settings;

        /// <summary>Once, at the host's start: KSP's keys read, the bindings saved as they change.</summary>
        public static void Start()
        {
            Shortcuts.Start();
            Shortcuts.Map.Changed -= OnMapChanged;
            Shortcuts.Map.Changed += OnMapChanged;
        }

        /// <summary>The host is gone: no core held across the scene change.</summary>
        public static void Stop()
        {
            core = null;
            settings = null;
        }

        /// <summary>Each frame: follows the core (the window list) and the settings' loads (the bindings).</summary>
        public static void Follow(MechJebCore c, MechJebModuleSettings s)
        {
            settings = s;
            if (MechJebModuleSettings.ShortcutsLoads != loadsRead)
            {
                loadsRead = MechJebModuleSettings.ShortcutsLoads;
                Import(MechJebModuleSettings.Shortcuts);
            }
            if (!ReferenceEquals(c, core))
            {
                core = c;
                builtSignature = 0;
                nextCheck = 0;
            }
            if (core == null || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 1f;
            int sig = Signature();
            if (sig == builtSignature) return;
            builtSignature = sig;
            Register();
        }

        static int Signature()
        {
            unchecked
            {
                int h = 17;
                List<DisplayModule> modules = core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance);
                for (int i = 0; i < modules.Count; i++)
                {
                    DisplayModule m = modules[i];
                    if (!m.ShowInMenu) continue;
                    h = h * 31 + RuntimeHelpers.GetHashCode(m.GetType());
                    if (m is MechJebModuleCustomInfoWindow) h = h * 31 + (m.GetName()?.GetHashCode() ?? 0);
                }
                return h;
            }
        }

        /// <summary>Every window of the menu as a toggle, in the menu's order (a window that went keeps its binding).</summary>
        static void Register()
        {
            Shortcuts.Actions.RemoveAll(UiActionKind.ToggleWindow);
            foreach (DisplayModule m in core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance))
                if (m.ShowInMenu)
                    Shortcuts.Actions.Add(Toggle(IdOf(m), m.GetName()));
        }

        // ---- the saved bindings ----

        static void Import(ConfigNode node)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            if (node != null)
                foreach (ConfigNode n in node.GetNodes(Node))
                    pairs.Add(new KeyValuePair<string, string>(Unescape(n.GetValue(ActionKey)), n.GetValue(ChordKey)));
            loading = true;
            try { Shortcuts.Map.Load(pairs, Shortcuts.CodeOf); }
            finally { loading = false; }
            savedText = Shortcuts.Save();
        }

        // a binding made, changed or cleared goes to the settings (the conflicts and captures change nothing saved)
        static void OnMapChanged()
        {
            if (loading) return;
            string text = Shortcuts.Save();
            if (text == savedText) return;
            savedText = text;
            var node = new ConfigNode(MechJebModuleSettings.ShortcutsNodeName);
            foreach (ShortcutBinding b in Shortcuts.Map.Bindings)
            {
                ConfigNode n = node.AddNode(Node);
                n.AddValue(ActionKey, Escape(b.ActionId));
                n.AddValue(ChordKey, b.Chord.Format(Shortcuts.CodeName));
            }
            MechJebModuleSettings.SetShortcuts(node);
            if (settings != null) settings.Dirty = true;
        }

        // a custom window's name as a config value: ConfigNode reads { } = and // as syntax, ends a value at a newline and trims
        // its ends, so those are written as %XX (and % itself)
        static string Escape(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            var sb = new StringBuilder(id.Length + 8);
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                bool edge = (i == 0 || i == id.Length - 1) && char.IsWhiteSpace(c);
                if (edge || c == '%' || c == '{' || c == '}' || c == '=' || c == '/' || c == '\n' || c == '\r' || c == '\t')
                    sb.Append('%').Append(((int)c).ToString("X2"));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        static string Unescape(string v)
        {
            if (string.IsNullOrEmpty(v) || v.IndexOf('%') < 0) return v;
            var sb = new StringBuilder(v.Length);
            for (int i = 0; i < v.Length; i++)
            {
                if (v[i] == '%' && i + 2 < v.Length && int.TryParse(v.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                {
                    sb.Append((char)code);
                    i += 2;
                }
                else sb.Append(v[i]);
            }
            return sb.ToString();
        }
    }
}
