using System.Collections.Generic;
using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleCustomInfoWindow (every custom info window: "Vessel Info", "Delta-V Stats", users' own...) in KodeKit.
    /// Built once from the window's item list; rebuilt when the list changes in the window editor. Values refresh at the
    /// window's refresh rate.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleCustomInfoWindow))]
    public class CustomInfoWindowView : KodeView
    {
        readonly MechJebModuleCustomInfoWindow window;
        InfoItemWidgets widgets;
        InfoItem[] built;
        float nextRefresh;
        MechJebModuleCustomInfoWindow.CompactMode builtCompact;
        bool builtFixed;
        LayoutElement rootSize;
        ResizeGrip grip;
        int shownWidth = -1;
        Theme windowTheme;
        int windowThemeVersion = -1;

        // the window's own "Compact", on or off whatever the new UI's global one is; null when it follows that, or the two agree
        Theme WindowTheme()
        {
            Theme current = Theme.Current;
            MechJebModuleCustomInfoWindow.CompactMode mode = window.Compact;
            bool compact = mode == MechJebModuleCustomInfoWindow.CompactMode.On;
            if (current == null || mode == MechJebModuleCustomInfoWindow.CompactMode.Follow || compact == current.Name.StartsWith("Compact"))
                return null;
            if (windowThemeVersion != Theme.Version)
            {
                MechJebModuleSettings settings = window.Core.Settings;
                windowTheme = KodeHost.ThemeFor((MechJebModuleSettings.UiSkinType)settings.UiSkin, compact, settings.UiOpacity);
                windowThemeVersion = Theme.Version;
            }
            return windowTheme;
        }

        public CustomInfoWindowView(MechJebModuleCustomInfoWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 250;

        protected override Color OverlayBackground => window.backgroundColor;

        protected override bool ClickThrough => window.ClickThrough;

        protected override void BuildContents(KodeWindow w)
        {
            built = window.items.ToArray();
            UIObject r = w.Root;

            builtCompact = window.Compact;
            w.Behaviour.ThemeOverride = WindowTheme;
            w.Behaviour.Restyle(); // the frame and title bar were styled before the override existed

            // the title follows renames from the editor
            TMP_Text title = w.TitleRow.GetComponentInChildren<TMP_Text>();
            string shownTitle = window.title;
            w.AddSync(() =>
            {
                if (window.title == shownTitle) return;
                shownTitle = window.title;
                title.text = shownTitle;
            });

            // edit this window, copy its sharing string: icons beside the close button
            if (window.Core.GetComputerModule<MechJebModuleCustomWindowEditor>() is { TechLocked: false } editor)
                w.TitleIcon(Sprites.Edit, "#MechJeb_NewUI_EditWindow_tooltip", () => //Edit this window
                {
                    editor.Enabled = true;
                    editor.editedWindow = window;
                });
            w.TitleIcon(Sprites.Copy, "#MechJeb_NewUI_CopyWindow_tooltip", () => //Copy this window's configuration
            {
                MuUtils.SystemClipboard = window.ToSharingString();
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg1", window.GetName()), 3.0f,
                    ScreenMessageStyle.UPPER_RIGHT); //Configuration of <<1>> window copied to clipboard.
            });

            // IMGUI's window is 250 wide and wraps long values: same width for the rows (minus the window's side padding)
            builtFixed = window.WidthFixed;
            widgets = new InfoItemWidgets(w)
            {
                TextColor = () => window.text, RowWidth = MinWidth - 2 * w.Behaviour.Theme.Padding, FixedWidth = builtFixed,
                ValueTooltips = !window.IsOverlay // the view rebuilds when that changes
            };
            foreach (InfoItem item in built)
            {
                if (HighLogic.LoadedSceneIsEditor ? item.showInEditor : item.showInFlight)
                    widgets.Add(r, item);
                else
                    widgets.AddUnavailable(r, item);
            }

            if (built.Length == 0)
                KodeWindow.Paragraph(r, CachedLocalizer.Instance.MechJebWindowEdCustomInfoWindowLabel1); //Add items to this window with the custom window editor.

            // RefreshRateGUI
            Layout rate = KodeWindow.Row(r);
            KodeWindow.Label(rate, "Update Interval", true);
            EditFields.Field(w, rate, () => window.refreshRate.Text, s =>
            {
                int old = window.refreshRate;
                window.refreshRate.Text = s;
                if (window.refreshRate < 1) window.refreshRate.Val = 1;
                if (window.refreshRate != old) window.Dirty = true;
            }, 60);
            KodeWindow.Label(rate, "Hz");
            w.Visible(rate.gameObject, () => GuiUtils.ShowAdvancedWindowSettings);

            // the bottom-right corner resizes the window, and fixes its width
            rootSize = w.Root.GetComponent<LayoutElement>();
            shownWidth = -1;
            grip = ResizeGrip.Add(w, (width, _) => window.FixedWidth = Mathf.RoundToInt(width), () => window.WidthFixed = true);
            // a Locked window keeps its size as well as its place: no grip, as no title bar drag
            w.Visible(grip.gameObject, () => !window.WindowLocked);

            widgets.Refresh();
        }

        // the window's width: its values' (Auto), or the fixed one, also while the grip is dragged in Auto
        void ApplyWidth()
        {
            if (!builtFixed && !grip.Dragging)
            {
                if (shownWidth == 0) return;
                shownWidth = 0;
                rootSize.minWidth = MinWidth;
                rootSize.preferredWidth = -1;
                return;
            }
            if (window.FixedWidth == 0) window.FixedWidth = Mathf.RoundToInt(Window.Root.rectTransform.rect.width);
            if (window.FixedWidth == shownWidth) return;
            shownWidth = window.FixedWidth;
            rootSize.minWidth = rootSize.preferredWidth = shownWidth;
        }

        protected override void BeforeSync()
        {
            if (Changed() || window.Compact != builtCompact || window.WidthFixed != builtFixed && !grip.Dragging)
            {
                RebuildRequested = true;
                return;
            }
            ApplyWidth();

            if (Time.realtimeSinceStartup < nextRefresh) return;
            nextRefresh = Time.realtimeSinceStartup + 1f / Mathf.Max(1, window.refreshRate);
            widgets.Refresh();
        }

        // the editor adds, removes and moves items in place
        bool Changed()
        {
            List<InfoItem> items = window.items;
            if (items.Count != built.Length) return true;
            for (int i = 0; i < built.Length; i++)
                if (items[i] != built[i]) return true;
            return false;
        }
    }
}
