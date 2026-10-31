using System;
using System.Collections.Generic;
using System.Linq;
using KodeKit;
using KSP.UI;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// Draws the ported MechJeb windows with KodeKit when Settings → "Use the new UI" is on.
    /// Registers as MechJeb's IMechJebUiHost: MechJebCore.OnGUI then skips the windows drawn here, and its
    /// single click-through lock also covers them (KodeKit's own per-window locks are switched off).
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

        // modules whose view threw while it was built: IMGUI draws their windows (OwnsWindow says no) until the views are
        // made again (another vessel, a layout change)
        readonly HashSet<DisplayModule> broken = new HashSet<DisplayModule>();

        // views whose per-frame update threw, logged once each; the other windows still update
        readonly HashSet<KodeView> failedSync = new HashSet<KodeView>();

        MechJebCore core;
        MechJebModuleSettings settings;

        bool Kode => settings != null && settings.Backend == MechJebModuleSettings.UiBackendType.Kode;

        void Start()
        {
            WindowBehaviour.ManageLocks = false;
            ComboBox.UseArrowSelector = () => GuiUtils.DontUseDropDownMenu;
            KodeWindow.TooltipFor = TooltipTexts.For;
            KodeWindow.TitleBarOnly = () => settings != null && settings.UseTitlebarDragging;
            MechJebUiHost.Register(this);
            MechJebModuleSettings.Changed += OnSettingsChanged;
            Debug.Log("[MechJeb2.UI] KodeHost registered");
        }

        void OnDestroy()
        {
            MechJebUiHost.Unregister(this);
            MechJebModuleSettings.Changed -= OnSettingsChanged;
            DestroyViews();
            // this closure would keep the host, and through it the vessel's MechJeb, in memory until the next scene's host
            KodeWindow.TitleBarOnly = () => false;
        }

        // ---- IMechJebUiHost ----

        public bool OwnsWindow(DisplayModule module) => Kode && module.Core == core && factories.ContainsKey(module.GetType()) && !broken.Contains(module);

        /// <summary>The live view of a module, if it has one (for tests and screenshots).</summary>
        public KodeView ViewOf(DisplayModule module) => views.TryGetValue(module, out KodeView v) ? v : null;

        public bool MouseOverWindow()
        {
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
            MechJebCore c = FindCore();
            if (c != core)
            {
                DestroyViews();
                core = c;
            }
            // a new core, or "Restore factory default settings", which recreates every module of the same core (this one too)
            MechJebModuleSettings s = core?.Settings;
            if (s != settings)
            {
                settings = s;
                broken.Clear(); // the broken modules were recreated as well
                ApplyTheme();
            }
            if (core == null) return;

            ImguiBlockHost.Skin = GuiUtils.Skin; // unported IMGUI bodies hosted in views use MechJeb's skin
            // MechJebCore.OnGUI's conditions
            bool show = Kode && core.ShowGui &&
                (HighLogic.LoadedSceneIsEditor || core.vessel == FlightGlobals.ActiveVessel && core.part.State != PartStates.DEAD);

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
            }

            foreach (ComputerModule cm in modules)
            {
                var m = (DisplayModule)cm;
                if (!factories.TryGetValue(m.GetType(), out Func<DisplayModule, KodeView> factory) || broken.Contains(m)) continue;
                bool want = show && m.Enabled && m.ShowInCurrentScene;
                views.TryGetValue(m, out KodeView v);
                if (want && (v == null || v.RebuildRequested))
                {
                    // first shown, or its structure changed (e.g. items added to a custom window): build it (again, in place)
                    if (v != null)
                    {
                        v.Destroy();
                        failedSync.Remove(v);
                    }
                    v = Build(m, factory);
                }
                if (v == null) continue;
                v.SetVisible(want);
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
        /// Build a module's view. When the build throws, the half-built window is removed, the error logged, and the module
        /// left to IMGUI, instead of a new half-built window every frame.
        /// </summary>
        KodeView Build(DisplayModule m, Func<DisplayModule, KodeView> factory)
        {
            KodeView v = null;
            try
            {
                v = factory(m);
                v.Build(AppCanvas);
                views[m] = v;
                return v;
            }
            catch (Exception e)
            {
                v?.Destroy();
                views.Remove(m);
                broken.Add(m);
                Debug.LogError($"[MechJeb2.UI] the \"{m.GetName()}\" window could not be built; IMGUI draws it instead:");
                Debug.LogException(e);
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

        static RectTransform AppCanvas => (RectTransform)UIMasterController.Instance.appCanvas.transform;

        void OnSettingsChanged(MechJebModuleSettings s)
        {
            if (s != settings) return;
            ApplyTheme();
            foreach (KodeView v in views.Values) v.ApplyScale();
        }

        string layoutKey;

        /// <summary>
        /// The new UI's layout settings → KodeKit's switches. Views read them when they build, so a change rebuilds every
        /// view (the next Update builds the open ones again).
        /// </summary>
        void ApplyLayout()
        {
            KodeWindow.ModernLayout = settings.UiModernLayout;
            KodeWindow.ShowHeaders = settings.UiSectionHeaders;
            string key = KodeWindow.ModernLayout + "|" + KodeWindow.ShowHeaders;
            if (layoutKey != null && key != layoutKey) DestroyViews();
            layoutKey = key;
        }

        /// <summary>
        /// The new UI's settings → KodeKit theme: KSP's stock sprites at MechJeb's text size, the new UI's own skin on top (Classic,
        /// Flat, Glass, ZTheme), tighter with its Compact setting.
        /// </summary>
        void ApplyTheme()
        {
            if (settings == null) return;
            ApplyLayout();
            Theme t = ThemeFor((MechJebModuleSettings.UiSkinType)settings.UiSkin, settings.UiCompact, settings.UiOpacity);
            string key = t.Name + "|" + Mathf.Clamp(settings.UiOpacity, 0.3f, 1f).ToString("F2");
            if (key == themeKey) return;
            themeKey = key;
            Theme.Set(t);
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

        string themeKey;

        static TMP_FontAsset arialMetric;

        /// <summary>KSP's LiberationSans SDF, with KSP's NotoSans as fallback for the symbols it lacks (Δ, Σ...).</summary>
        static TMP_FontAsset ArialMetricFont
        {
            get
            {
                if (arialMetric != null) return arialMetric;
                arialMetric = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "LiberationSans SDF");
                TMP_FontAsset noto = UISkinManager.TMPFont;
                if (arialMetric != null && noto != null && arialMetric.fallbackFontAssets != null && !arialMetric.fallbackFontAssets.Contains(noto))
                    arialMetric.fallbackFontAssets.Add(noto);
                return arialMetric;
            }
        }

        void DestroyViews()
        {
            foreach (KodeView v in views.Values) v.Destroy();
            views.Clear();
            broken.Clear();
            failedSync.Clear();
        }
    }
}
