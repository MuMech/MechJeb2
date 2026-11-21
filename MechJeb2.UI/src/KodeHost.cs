using System;
using System.Collections.Generic;
using System.Linq;
using KodeKit;
using KodeUI;
using KSP.Localization;
using KSP.UI;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// Draws MechJeb's windows with KodeKit.
    /// Registers as MechJeb's IMechJebUiHost: MechJebCore's single click-through lock also covers the windows drawn here
    /// (KodeKit's own per-window locks are switched off).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class KodeHost : MonoBehaviour, IMechJebUiHost
    {
        /// <summary>Ported windows: DisplayModule type → view factory, from every KodeView marked [KodeViewFor].</summary>
        static readonly Dictionary<Type, Func<DisplayModule, KodeView>> factories = FindViews();

        static Dictionary<Type, Func<DisplayModule, KodeView>> FindViews()
        {
            var found = new Dictionary<Type, Func<DisplayModule, KodeView>>();
            foreach (Type t in typeof(KodeHost).Assembly.GetTypes())
            {
                if (t.IsAbstract || !typeof(KodeView).IsAssignableFrom(t)) continue;
                foreach (KodeViewForAttribute a in t.GetCustomAttributes(typeof(KodeViewForAttribute), false))
                {
                    Type view = t;
                    found[a.Module] = m => (KodeView)Activator.CreateInstance(view, m);
                }
            }
            return found;
        }

        /// <summary>The DisplayModule types that have a KodeKit view.</summary>
        public static IEnumerable<Type> PortedTypes => factories.Keys;

        readonly List<DisplayModule> stale = new List<DisplayModule>();

        readonly Dictionary<DisplayModule, KodeView> views = new Dictionary<DisplayModule, KodeView>();

        // the modules seen on last frame: one that turns on is a window opened, which comes above the others
        readonly HashSet<DisplayModule> wasEnabled = new HashSet<DisplayModule>();

        // views whose per-frame update threw, logged once each; the other windows still update
        readonly HashSet<KodeView> failedSync = new HashSet<KodeView>();

        MechJebCore core;
        MechJebModuleSettings settings;
        string language;

        void Start()
        {
            WindowBehaviour.ManageLocks = false;
            ComboBox.UseArrowSelector = () => GuiUtils.DontUseDropDownMenu;
            KodeWindow.TooltipFor = TooltipTexts.For;
            KodeWindow.TitleBarOnly = () => settings != null && settings.UseTitlebarDragging;
            MechJebUiHost.Register(this);
            // the agent bridge, for tests and full control: off unless KSP is launched with -mechjeb-ui-agent <port>
            // (or MECHJEB_UI_AGENT_PORT is set)
            KodeKit.Agent.AgentServer.StartFromLaunch();
            MechJebModuleSettings.Changed += OnSettingsChanged;
            KodeWindow.InputHistory.Changed += OnInputHistoryChanged;
            // the keyboard shortcuts (opt-in window toggles): KSP's keys read, the bindings followed in the settings
            WindowShortcuts.Start();
            // the command palette's "open" action, which a shortcut can be bound to (none by default)
            PaletteCommands.Start();
            // the font is searched again in each scene when the last search found none
            arialMetricSearched = false;
            Debug.Log("[MechJeb2.UI] KodeHost registered");
        }

        void OnDestroy()
        {
            // the agent bridge's requests waiting for this scene are dropped (503), and new ones answered 503 until the next host pumps
            KodeKit.Agent.AgentServer.HostGone();
            MechJebUiHost.Unregister(this);
            MechJebModuleSettings.Changed -= OnSettingsChanged;
            KodeWindow.InputHistory.Changed -= OnInputHistoryChanged;
            DestroyViews();
            KodeWindow.Focus.Forget();
            WindowShortcuts.Stop();
            PaletteCommands.Stop();
            // this closure would keep the host, and through it the vessel's MechJeb, in memory until the next scene's host
            KodeWindow.TitleBarOnly = () => false;
        }

        // a field's recent values changed: saved with the global settings
        void OnInputHistoryChanged() => InputHistorySettings.Save(settings);

        // ---- IMechJebUiHost ----

        /// <summary>The live view of a module, if it has one (for tests and screenshots).</summary>
        public KodeView ViewOf(DisplayModule module) => views.TryGetValue(module, out KodeView v) ? v : null;

        public bool MouseOverWindow()
        {
            // a dialog's blocker covers the whole screen, and so does the command palette's
            if (CommandPalette.IsOpen) return true;
            if (Dialogs.IsOpen) return true;
            // a toast takes the click that dismisses it: the part or the map under it mustn't get it too
            if (Toasts.PointerOver) return true;
            // an open context menu under the pointer: its click is the menu's
            if (ContextMenus.Hovered) return true;
            // overlays (custom windows as a HUD) don't lock the game's controls, as in GuiUtils.MouseIsOverWindow
            foreach (KodeView v in views.Values)
                if (v.Visible && !v.Module.IsOverlay && v.Window.Behaviour.Hovered) return true;
            // an open combo popup or its list
            return ComboBox.Open != null && ComboBox.Open.PopupList != null &&
                ComboBox.Open.PopupList.GetComponent<WindowBehaviour>()?.Hovered == true;
        }

        // ---- per frame ----

        void Update()
        {
            // a source scope (KodeKit's CommitPath.As) left open by a handler that threw would give its source to every later edit
            if (KodeKit.Headless.CommitPath.ResetScopes()) Debug.LogWarning("[MechJeb] a KodeKit source scope was left open; closed");
            // the agent bridge's requests, answered on the main thread (nothing to do while it is off)
            if (KodeKit.Agent.AgentServer.Running) KodeKit.Agent.AgentServer.Pump();
            // an edit remembered for a window that wasn't built again (closed, or its vessel switched) lets go of the old window
            KodeWindow.Focus.Expire();
            // the fields' recent values, read again when MechJeb loaded its global settings
            InputHistorySettings.Follow();
            MechJebCore c = FindCore();
            // Unity's == counts a destroyed core (its pod deleted in the editor, or destroyed in flight) as null: compare the
            // references, so the views of a destroyed core are let go
            if (c == null) c = null;
            if (!ReferenceEquals(c, core))
            {
                DestroyViews();
                core = c;
            }
            // a new core, or "Restore factory default settings", which recreates every module of the same core (this one too)
            MechJebModuleSettings s = core?.Settings;
            if (s != settings)
            {
                settings = s;
                positionScale = GuiUtils.Scale;
                Toasts.Scale = GuiUtils.Scale;
                CommandPalette.Scale = GuiUtils.Scale;
                ApplyTheme();
            }
            // a language switch: the views keep many texts made when they were built (tooltips, button captions, cached
            // readouts), so they are made again, as on a layout change
            string lang = Localizer.CurrentLanguage;
            if (lang != language)
            {
                if (language != null) DestroyViews(true);
                language = lang;
            }
            // the windows as shortcut actions, the bindings as saved
            WindowShortcuts.Follow(core, settings);
            PaletteCommands.Follow(core);
            if (core == null)
            {
                Tooltips.FollowPointer(null);
                return;
            }

            // the conditions MechJebCore.Update shows the windows under
            bool show = core.ShowGui &&
                (HighLogic.LoadedSceneIsEditor || core.vessel == FlightGlobals.ActiveVessel && core.part.State != PartStates.DEAD);

            // picking a target position on the map: its coordinates and biome next to the pointer
            Tooltips.FollowPointer(show ? core.Target?.PickLabel : null);

            List<ComputerModule> modules = core.GetComputerModules<DisplayModule>();

            // "Restore factory default settings" recreates the modules: drop views of the old ones
            stale.Clear();
            foreach (DisplayModule m in views.Keys)
                if (!modules.Contains(m)) stale.Add(m);
            foreach (DisplayModule m in stale)
            {
                views[m].Destroy();
                failedSync.Remove(views[m]);
                views.Remove(m);
                wasEnabled.Remove(m);
            }

            foreach (ComputerModule cm in modules)
            {
                var m = (DisplayModule)cm;
                if (!factories.TryGetValue(m.GetType(), out Func<DisplayModule, KodeView> factory)) continue;
                bool want = show && m.Enabled && m.ShowInCurrentScene;
                views.TryGetValue(m, out KodeView v);
                if (want && (v == null || v.RebuildRequested))
                {
                    // first shown, or its structure changed (e.g. items added to a custom window): build it (again, in place)
                    if (v != null)
                    {
                        v.Destroy(true);
                        failedSync.Remove(v);
                    }
                    v = Build(m, factory);
                }
                if (v == null) continue;
                // opened since it was last shown (the module turned on): above the other windows; shown again only, it keeps its place
                bool opened = m.Enabled && !wasEnabled.Contains(m);
                // seen only once shown: a window enabled while the UI is hidden is raised when the UI is shown
                if (!m.Enabled) wasEnabled.Remove(m);
                else if (want) wasEnabled.Add(m);
                v.SetVisible(want, opened);
                if (!want) continue;
                try
                {
                    v.Sync();
                }
                catch (Exception e)
                {
                    if (!failedSync.Add(v)) continue;
                    Debug.LogError($"[MechJeb2.UI] the update of the \"{m.GetName()}\" window threw (logged once, the other windows still update):");
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Build a module's view. When the build throws, the half-built window is removed and the player told, instead of a
        /// new half-built window every frame.
        /// </summary>
        KodeView Build(DisplayModule m, Func<DisplayModule, KodeView> factory)
        {
            KodeView v = null;
            try
            {
                v = factory(m);
                v.Build((RectTransform)Overlay.Root.transform);
                views[m] = v;
                return v;
            }
            catch (Exception e)
            {
                v?.Destroy();
                views.Remove(m);
                // the window is closed, so the menu shows it closed, and it is built again only when the player opens it again
                m.Enabled = false;
                Debug.LogError($"[MechJeb2.UI] the \"{m.GetName()}\" window could not be built:");
                Debug.LogException(e);
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_NewUI_WindowFailed", m.GetName()), 10, ScreenMessageStyle.UPPER_CENTER);
                return null;
            }
        }

        /// <summary>The master MechJeb of the active vessel in flight, or of the ship being built in the editor.</summary>
        static MechJebCore FindCore()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return EditorLogic.fetch != null && EditorLogic.fetch.ship != null ? ((Vessel)null).GetMasterMechJeb() : null;
            return FlightGlobals.ready && FlightGlobals.ActiveVessel != null ? FlightGlobals.ActiveVessel.GetMasterMechJeb() : null;
        }

        void OnSettingsChanged(MechJebModuleSettings s)
        {
            if (s != settings) return;
            ApplyTheme();
            Toasts.Scale = GuiUtils.Scale;
            CommandPalette.Scale = GuiUtils.Scale;
            if (GuiUtils.Scale.Equals(positionScale)) return;
            KeepPositionsOnScreen(positionScale, GuiUtils.Scale);
            positionScale = GuiUtils.Scale;
            foreach (KodeView v in views.Values) v.Rescale();
        }

        // the UI scale the stored window positions are in units of (IMGUI units: screen pixels / scale)
        float positionScale = 1;

        /// <summary>
        /// A UI scale change keeps every window of this UI where it is on screen: the stored positions, in IMGUI units, are
        /// converted to the new scale's units for every ported module, in both scenes, whether its window is shown, hidden or
        /// not built yet.
        /// </summary>
        void KeepPositionsOnScreen(float from, float to)
        {
            if (core == null) return;
            float k = from / to;
            foreach (ComputerModule cm in core.GetComputerModules<DisplayModule>())
            {
                var m = (DisplayModule)cm;
                if (!factories.ContainsKey(m.GetType())) continue;
                m.WindowVector = new Vector4(m.WindowVector.x * k, m.WindowVector.y * k, m.WindowVector.z, m.WindowVector.w);
                m.WindowVectorEditor = new Vector4(m.WindowVectorEditor.x * k, m.WindowVectorEditor.y * k, m.WindowVectorEditor.z,
                    m.WindowVectorEditor.w);
                m.Dirty = true;
            }
        }

        bool? layoutKey;

        /// <summary>
        /// The new UI's layout settings → KodeKit's switches. Views read them when they build, so a change rebuilds every
        /// view (the next Update builds the open ones again).
        /// </summary>
        void ApplyLayout()
        {
            KodeWindow.ShowHeaders = settings.UiSectionHeaders;
            bool key = KodeWindow.ShowHeaders;
            if (layoutKey != null && key != layoutKey) DestroyViews(true);
            layoutKey = key;
        }

        // canvas units within which a dragged window's edge snaps to another edge, with Snap windows on
        const float SnapDistance = 8;

        /// <summary>
        /// The new UI's settings → KodeKit theme: KSP's stock sprites at MechJeb's text size, the new UI's own skin on top (Classic,
        /// Flat, Glass, ZTheme), tighter with its Compact setting.
        /// </summary>
        void ApplyTheme()
        {
            if (settings == null) return;
            ApplyLayout();
            WindowBehaviour.SnapDistance = settings.UiSnapWindows ? SnapDistance : 0;
            // every settings change comes here (a slider drag sends one per step): a theme is built only for another skin or
            // spacing, and an opacity change alone goes to the windows' backgrounds
            var skin = (MechJebModuleSettings.UiSkinType)settings.UiSkin;
            bool compact = settings.UiCompact;
            float opacity = Mathf.Clamp(settings.UiOpacity, 0.3f, 1f);
            bool sameTheme = themeApplied && Theme.Current != null && skin == themeSkin && compact == themeCompact;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (sameTheme && opacity == themeOpacity) return;
            themeApplied = true;
            themeSkin = skin;
            themeCompact = compact;
            themeOpacity = opacity;
            // ThemeFor puts the opacity in the window tint's alpha, for every skin
            if (sameTheme) SetOpacity(opacity);
            else Theme.Set(ThemeFor(skin, compact, opacity));
        }

        // what this host last applied: until then Theme.Current is another scene's theme, or KodeKit's default, and is replaced
        bool themeApplied;
        MechJebModuleSettings.UiSkinType themeSkin;
        bool themeCompact;
        float themeOpacity;

        /// <summary>
        /// The opacity slider: only the window backgrounds' alpha changes, so it is written to them and to the themes they
        /// use, instead of a new theme that restyles every widget of every window at each drag event.
        /// </summary>
        void SetOpacity(float alpha)
        {
            SetWindowAlpha(Theme.Current, alpha);
            foreach (KodeView v in views.Values)
            {
                KodeRootWindow root = v.Window?.Root;
                if (root == null) continue;
                // a custom window can have its own theme (its Compact setting): ThemeFor gave it the same alpha
                Theme th = KodeWindow.ThemeOf(root.gameObject);
                SetWindowAlpha(th, alpha);
                if (root.BackGround != null) root.BackGround.color = th.Window.Tint;
            }
        }

        static void SetWindowAlpha(Theme t, float alpha)
        {
            Color c = t.Window.Tint;
            c.a = alpha;
            t.Window.Tint = c;
        }

        /// <summary>The theme of a new UI skin, at MechJeb's COMPACT or normal spacing and a window opacity (also for tests).</summary>
        public static Theme ThemeFor(MechJebModuleSettings.UiSkinType skin, bool compact, float opacity)
        {
            Theme t = Theme.KspStock();
            // MechJeb's skins are Unity's built-in IMGUI skin: near-white Arial 13 px. LiberationSans is metric-compatible
            // with Arial (same advance widths), so at 13 px the text has the same size as MechJeb's IMGUI text.
            t.Text = new Color(0.9f, 0.9f, 0.9f);
            if (ArialMetricFont != null) t.Font = ArialMetricFont;
            t.Field.TextColor = t.Text; // KSP's field text is yellow; MechJeb's is white
            t.FontSize = 13;
            // IMGUI's label rows: 15 px of text plus padding, 21 px in MechJeb's default skin, 19 px in Compact (measured)
            t.LineHeight = 21;
            // real checkboxes: KSP's toggle sprite is a small dot whose on/off states look alike
            t.Toggle = new ElementStyle { Normal = Sprites.CheckBox, Highlight = Sprites.CheckBoxHover, Active = Sprites.CheckBoxHover };
            t.Check = new ElementStyle { Normal = Sprites.Tick, Tint = new Color(0.55f, 1f, 0.55f) };
            // darker windows than KSP's grey sprite, at the user's opacity (Settings → "New UI opacity")
            opacity = Mathf.Clamp(opacity, 0.3f, 1f);
            t.Window.Tint = new Color(0.5f, 0.5f, 0.5f, opacity);
            if (skin == MechJebModuleSettings.UiSkinType.Classic) t = Theme.Classic(t);
            else if (skin == MechJebModuleSettings.UiSkinType.Flat) t = Theme.Flat(t);
            else if (skin == MechJebModuleSettings.UiSkinType.Glass) t = Theme.Glass(t);
            else if (skin == MechJebModuleSettings.UiSkinType.ZTheme) t = Theme.ZTheme(t);
            // the drawn skins carry their colour in their sprites: the opacity goes in the tint's alpha only
            if (skin == MechJebModuleSettings.UiSkinType.Flat || skin == MechJebModuleSettings.UiSkinType.Glass || skin == MechJebModuleSettings.UiSkinType.ZTheme)
                t.Window.Tint = new Color(1, 1, 1, opacity);
            return compact ? Theme.Compact(t) : t;
        }

        static TMP_FontAsset arialMetric;
        static bool arialMetricSearched;

        /// <summary>KSP's LiberationSans SDF, with KSP's NotoSans as fallback for the symbols it lacks (Δ, Σ...).</summary>
        static TMP_FontAsset ArialMetricFont
        {
            get
            {
                // a missing font is searched once per scene (KodeHost.Start clears the flag), not at each theme build
                if (arialMetric != null || arialMetricSearched) return arialMetric;
                arialMetricSearched = true;
                arialMetric = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "LiberationSans SDF");
                TMP_FontAsset noto = UISkinManager.TMPFont;
                if (arialMetric != null && noto != null && arialMetric.fallbackFontAssets != null && !arialMetric.fallbackFontAssets.Contains(noto))
                    arialMetric.fallbackFontAssets.Add(noto);
                return arialMetric;
            }
        }

        // rebuilding: the open views are built again (a language or layout switch), so an edit in progress waits for its window
        void DestroyViews(bool rebuilding = false)
        {
            foreach (KodeView v in views.Values) v.Destroy(rebuilding);
            views.Clear();
            failedSync.Clear();
        }
    }
}
