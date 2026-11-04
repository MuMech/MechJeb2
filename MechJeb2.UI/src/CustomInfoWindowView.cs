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

            // "E": edit this window, "C": copy its sharing string; the modern layout draws them as icons beside the close button
            void TitleAction(UnityEngine.Sprite icon, string letter, string tooltipKey, System.Action click)
            {
                if (KodeWindow.ModernLayout) w.TitleIcon(icon, tooltipKey, click);
                else Tooltips.Add(w.TitleButton(letter, click).gameObject, tooltipKey); // localized when shown
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
                window.refreshRate.Text = s;
                if (window.refreshRate < 1) window.refreshRate.Val = 1;
                window.UpdateRefreshRate();
            }, 60);
            KodeWindow.Label(rate, "Hz");
            w.Visible(rate.gameObject, () => GuiUtils.ShowAdvancedWindowSettings);

            // the bottom-right corner resizes the window, and fixes its width
            rootSize = w.Root.GetComponent<LayoutElement>();
            shownWidth = -1;
            grip = AddGrip(w);

            widgets.Refresh();
        }

        static Sprite gripSprite;

        // three diagonal stripes in the bottom-right corner, the usual resize grip (KodeUI's sprites have none)
        static Sprite GripSprite
        {
            get
            {
                if (gripSprite != null) return gripSprite;
                const int n = 12;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int d = n - 1 - x + y; // 0 in the bottom-right corner
                    tex.SetPixel(x, y, d < n && d % 4 >= 2 ? Color.white : Color.clear);
                }
                tex.Apply();
                return gripSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            }
        }

        ResizeGrip AddGrip(KodeWindow w)
        {
            var go = new GameObject("ResizeGrip", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(ResizeGrip));
            go.transform.SetParent(w.Root.transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(12, 12);
            var img = go.GetComponent<Image>();
            img.sprite = GripSprite;
            KodeWindow.Styled(go, th => img.color = new Color(th.Text.r, th.Text.g, th.Text.b, 0.45f));
            var g = go.GetComponent<ResizeGrip>();
            g.Root = w.Root.rectTransform;
            g.Resized = width => window.FixedWidth = Mathf.RoundToInt(width);
            g.Done = () => window.WidthFixed = true;
            return g;
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

        /// <summary>Drags the window's right edge: the new width, in the window's own units, as the pointer moves.</summary>
        sealed class ResizeGrip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public RectTransform Root;
            public System.Action<float> Resized;
            public System.Action Done;
            public bool Dragging { get; private set; }
            float offset;

            bool Local(PointerEventData e, out float x)
            {
                bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, e.position, e.pressEventCamera, out Vector2 p);
                x = p.x + Root.pivot.x * Root.rect.width; // from the window's left edge
                return ok;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                if (!Local(e, out float x)) return;
                offset = Root.rect.width - x;
                Dragging = true;
                Resized(Root.rect.width);
            }

            public void OnDrag(PointerEventData e)
            {
                if (Dragging && Local(e, out float x)) Resized(x + offset);
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (!Dragging) return;
                Dragging = false;
                Done();
            }
        }
    }
}
