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
        bool builtCompact;
        Theme compact;
        int compactVersion = -1;

        Theme CompactTheme()
        {
            Theme current = Theme.Current;
            if (!window.isCompact || current == null || current.Name.StartsWith("Compact")) return null;
            if (compactVersion != Theme.Version)
            {
                compact = Theme.Compact(current);
                compactVersion = Theme.Version;
            }
            return compact;
        }

        public CustomInfoWindowView(MechJebModuleCustomInfoWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 250;

        protected override Color OverlayBackground => window.backgroundColor;

        protected override void BuildContents(KodeWindow w)
        {
            built = window.items.ToArray();
            UIObject r = w.Root;

            // the window's own "Compact" (IMGUI draws it with the compact skin whatever MechJeb's skin is)
            builtCompact = window.isCompact;
            w.Behaviour.ThemeOverride = CompactTheme;
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

            // "E": edit this window, "C": copy its sharing string
            if (window.Core.GetComputerModule<MechJebModuleCustomWindowEditor>() is { TechLocked: false } editor)
                Tooltips.Add(w.TitleButton("E", () =>
                {
                    editor.Enabled = true;
                    editor.editedWindow = window;
                }).gameObject, Localizer.Format("#MechJeb_NewUI_EditWindow_tooltip")); //Edit this window
            Tooltips.Add(w.TitleButton("C", () =>
            {
                MuUtils.SystemClipboard = window.ToSharingString();
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg1", window.GetName()), 3.0f,
                    ScreenMessageStyle.UPPER_RIGHT); //Configuration of <<1>> window copied to clipboard.
            }).gameObject, Localizer.Format("#MechJeb_NewUI_CopyWindow_tooltip")); //Copy this window's configuration

            // IMGUI's window is 250 wide and wraps long values: same width for the rows (minus the window's side padding)
            widgets = new InfoItemWidgets(w) { TextColor = () => window.text, RowWidth = MinWidth - 2 * Theme.Current.Padding };
            foreach (InfoItem item in built)
            {
                if (HighLogic.LoadedSceneIsEditor ? item.showInEditor : item.showInFlight)
                    widgets.Add(r, item);
                else
                    KodeWindow.Label(r, item.localizedName); // not available in this scene: its name only, like IMGUI
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
            if (Changed() || window.isCompact != builtCompact)
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
