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
        }

        // ---- IMechJebUiHost ----

        public bool OwnsWindow(DisplayModule module) => Kode && module.Core == core && factories.ContainsKey(module.GetType());

        /// <summary>The live view of a module, if it has one (for tests and screenshots).</summary>
        public KodeView ViewOf(DisplayModule module) => views.TryGetValue(module, out KodeView v) ? v : null;

        public bool MouseOverWindow()
        {
            foreach (KodeView v in views.Values)
                if (v.Visible && v.Window.Behaviour.Hovered) return true;
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
                settings = core?.GetComputerModule<MechJebModuleSettings>();
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
                views.Remove(m);
                if (m is MechJebModuleSettings) settings = core.GetComputerModule<MechJebModuleSettings>();
            }

            foreach (ComputerModule cm in modules)
            {
                var m = (DisplayModule)cm;
                if (!factories.TryGetValue(m.GetType(), out Func<DisplayModule, KodeView> factory)) continue;
                bool want = show && m.Enabled && m.ShowInCurrentScene;
                views.TryGetValue(m, out KodeView v);
                if (want && v == null)
                {
                    v = factory(m);
                    v.Build(AppCanvas);
                    views[m] = v;
                }
                if (v == null) continue;
                if (want && v.RebuildRequested)
                {
                    // the window's structure changed (e.g. items added to a custom window): build it again in place
                    v.Destroy();
                    v = factory(m);
                    v.Build(AppCanvas);
                    views[m] = v;
                }
                v.SetVisible(want);
                if (want) v.Sync();
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

        /// <summary>MechJeb skin → KodeKit theme. KSP stock look for DEFAULT and MECH_JEB1 until a MechJeb theme exists.</summary>
        void ApplyTheme()
        {
            if (settings == null) return;
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
            float opacity = Mathf.Clamp(settings.UiOpacity, 0.3f, 1f);
            t.Window.Tint = new Color(0.5f, 0.5f, 0.5f, opacity);
            if ((GuiUtils.SkinType)settings.SkinId == GuiUtils.SkinType.COMPACT) t = Theme.Compact(t);
            string key = t.Name + "|" + opacity.ToString("F2");
            if (key == themeKey) return;
            themeKey = key;
            Theme.Set(t);
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
        }
    }
}
