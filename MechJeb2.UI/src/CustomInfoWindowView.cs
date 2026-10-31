using System.Collections.Generic;
using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleCustomInfoWindow (every custom info window: "Vessel Info", "Delta-V Stats", users' own...) in KodeKit.
    /// Built once from the window's item list; rebuilt when the list changes in the window editor. Values refresh at the
    /// window's refresh rate like UpdateWindowItems.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleCustomInfoWindow))]
    public class CustomInfoWindowView : KodeView
    {
        readonly MechJebModuleCustomInfoWindow window;
        InfoItemWidgets widgets;
        InfoItem[] built;
        float nextRefresh;
        MechJebModuleCustomInfoWindow.CompactMode builtCompact;
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

            // "E": edit this window, "C": copy its sharing string; the modern layout draws them as icons beside the close button
            void TitleAction(UnityEngine.Sprite icon, string letter, string tooltipKey, System.Action click)
            {
                if (KodeWindow.ModernLayout) w.TitleIcon(icon, tooltipKey, click);
                else Tooltips.Add(w.TitleButton(letter, click).gameObject, Localizer.Format(tooltipKey));
            }

            if (window.Core.GetComputerModule<MechJebModuleCustomWindowEditor>() is { TechLocked: false } editor)
                TitleAction(Sprites.Edit, "E", "#MechJeb_NewUI_EditWindow_tooltip", () => //Edit this window
                {
                    editor.Enabled = true;
                    editor.editedWindow = window;
                });
            TitleAction(Sprites.Copy, "C", "#MechJeb_NewUI_CopyWindow_tooltip", () => //Copy this window's configuration
            {
                MuUtils.SystemClipboard = window.ToSharingString();
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg1", window.GetName()), 3.0f,
                    ScreenMessageStyle.UPPER_RIGHT); //Configuration of <<1>> window copied to clipboard.
            });

            // IMGUI's window is 250 wide and wraps long values: same width for the rows (minus the window's side padding)
            widgets = new InfoItemWidgets(w) { TextColor = () => window.text, RowWidth = MinWidth - 2 * w.Behaviour.Theme.Padding };
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
                window.refreshRate.Text = s;
                if (window.refreshRate < 1) window.refreshRate.Val = 1;
                window.UpdateRefreshRate();
            }, 60);
            KodeWindow.Label(rate, "Hz");
            w.Visible(rate.gameObject, () => GuiUtils.ShowAdvancedWindowSettings);

            widgets.Refresh();
        }

        protected override void BeforeSync()
        {
            if (Changed() || window.Compact != builtCompact)
            {
                RebuildRequested = true;
                return;
            }

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
