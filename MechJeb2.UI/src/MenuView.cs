using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>A right press on the menu button starts moving it (MechJebModuleMenu.OnMenuUpdate follows the mouse while it is held).</summary>
    class MenuButtonRightPress : MonoBehaviour, IPointerDownHandler
    {
        public MechJebModuleMenu Menu;

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right && !Menu.movingButton) Menu.movingButton = true;
        }
    }

    /// <summary>
    /// MechJebModuleMenu in KodeKit: the "MechJeb" button on a screen edge and the panel that slides out of it,
    /// with one toggle per window (green while the window's module is active).
    /// There is no title bar. The panel is the KodeWindow itself, placed every frame by BeforeSync at the position
    /// MechJebModuleMenu.displayedPos gives (so the slide animation, side, column count and hPos/vPos all work as in IMGUI);
    /// while hidden it sits below the screen. The button is a child of the window that ignores its layout and is placed by
    /// MechJebModuleMenu.buttonPos (rotated 90 degrees on the left and right edges). When the window list or the column count
    /// changes the view asks the host for a rebuild (checked twice a second).
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleMenu))]
    public class MenuView : KodeView
    {
        readonly MechJebModuleMenu menu;

        public MenuView(MechJebModuleMenu menu) : base(menu) => this.menu = menu;

        protected override float MinWidth => 200; // colWidth

        const float ButtonWidth = 100, ButtonHeight = 25;

        MechJebCore Core => menu.Core;

        // ---- structure ----

        static bool InMenu(DisplayModule m) => m.ShowInMenu && !m.TechLocked && m.ShowInCurrentScene;

        int builtSignature;
        float nextCheck;

        /// <summary>Changes when the toggles to show or the column count change.</summary>
        int Signature()
        {
            unchecked
            {
                int h = menu.columns;
                h = h * 31 + (KSP.Localization.Localizer.CurrentLanguage?.GetHashCode() ?? 0);
                List<DisplayModule> modules = Core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance);
                for (int i = 0; i < modules.Count; i++)
                {
                    DisplayModule m = modules[i];
                    if (!InMenu(m)) continue;
                    h = h * 31 + RuntimeHelpers.GetHashCode(m);
                    // custom windows can be renamed
                    if (m is MechJebModuleCustomInfoWindow) h = h * 31 + (m.GetName()?.GetHashCode() ?? 0);
                }

                return h;
            }
        }

        static KodeWindow.Tone ModuleTone(DisplayModule m)
        {
            try { return m.IsActive() ? KodeWindow.Tone.Good : KodeWindow.Tone.Normal; }
            catch (Exception) { return KodeWindow.Tone.Normal; }
        }

        UIButton button;
        RectTransform buttonRect;
        string upText = "▲ MechJeb ▲", downText = "▼ MechJeb ▼", shownText;

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            w.TitleRow.gameObject.SetActive(false);
            builtSignature = Signature();
            nextCheck = Time.unscaledTime + 0.5f;

            // the small button at the top left of the panel that closes it when the big button is hidden
            UIButton close = KodeWindow.Button(r, "", menu.ShowHideWindow);
            close.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform cr = close.rectTransform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0, 1);
            cr.anchoredPosition = new Vector2(2, -2);
            cr.sizeDelta = new Vector2(16, 16);
            w.Visible(close.gameObject, () => menu.HideMenuButton);

            // columns of toggles, the online manual button ends the last one
            Layout columns = r.Add<Layout>("Columns");
            columns.Horizontal().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(4).ChildAlignment(TextAnchor.UpperLeft);
            columns.FlexibleLayout(true, false);
            columns.Finish();

            List<DisplayModule> modules = Core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance);
            int count = 0;
            for (int i = 0; i < modules.Count; i++)
                if (InMenu(modules[i])) count++;
            int step = Mathf.CeilToInt((float)(count + 1) / menu.columns);

            Layout column = KodeWindow.Panel(columns);
            int inColumn = 0;
            for (int i = 0; i < modules.Count; i++)
            {
                DisplayModule module = modules[i];
                if (!InMenu(module)) continue;
                if (inColumn == step)
                {
                    column = KodeWindow.Panel(columns);
                    inColumn = 0;
                }

                w.Toggle(column, module.GetName(), () => module.Enabled, v => module.Enabled = v, () => ModuleTone(module));
                inColumn++;
            }

            KodeWindow.Button(column, "#MechJeb_OnlineManualbutton", () => Application.OpenURL("https://github.com/MuMech/MechJeb2/wiki")); //"Online Manual"

            UIText locked = KodeWindow.Paragraph(r, "#MechJeb_ModuleAreLocked"); //"Some modules are disabled until you unlock the proper node in the R&D tree or upgrade the tracking station."
            w.Visible(locked.gameObject, () => Core.someModuleAreLocked);

            // the "MechJeb" button on the edge
            button = KodeWindow.Button(r, upText, menu.ShowHideWindow);
            button.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            button.gameObject.AddComponent<MenuButtonRightPress>().Menu = menu;
            buttonRect = button.rectTransform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label.font != null && !label.font.HasCharacter('▲'))
            {
                upText = "^ MechJeb ^";
                downText = "v MechJeb v";
            }

            shownText = null;
            w.Visible(button.gameObject, () => !menu.HideMenuButton);
        }

        // ---- per frame ----

        MechJebModuleMenu.WindowStat lastStat;
        Camera cam;
        bool haveCam;

        protected override void BeforeSync()
        {
            // what DrawGUI did before drawing
            menu.AdvanceAnimation();

            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 0.5f;
                if (Signature() != builtSignature) RebuildRequested = true;
            }

            // a freshly opening menu goes above the other windows (IMGUI: GUI.depth = -99)
            if (menu.windowStat == MechJebModuleMenu.WindowStat.OPENING && lastStat != MechJebModuleMenu.WindowStat.OPENING)
                Window.Behaviour.BringToFront();
            lastStat = menu.windowStat;

            RectTransform root = Window.Root.rectTransform;
            var canvas = (RectTransform)root.parent;
            if (!haveCam)
            {
                Canvas c = root.GetComponentInParent<Canvas>().rootCanvas;
                cam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
                haveCam = true;
            }

            float scale = GuiUtils.Scale;
            // IMGUI units (screen pixels / MechJeb scale) per local unit
            float k = root.lossyScale.x / scale;
            menu.SetMeasuredSize(root.rect.width * k, root.rect.height * k);

            // the panel: displayedPos, or just below the screen while hidden (like IMGUI's "so the mouse can't hover it")
            Rect pos = menu.windowStat == MechJebModuleMenu.WindowStat.HIDDEN
                ? new Rect(GuiUtils.ScaledScreenWidth, GuiUtils.ScaledScreenHeight, 0, 0)
                : menu.displayedPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, new Vector2(pos.x * scale, Screen.height - pos.y * scale), cam, out Vector2 local);
            root.localPosition = new Vector3(local.x, local.y, root.localPosition.z);

            // the button: buttonPos is in the rotated space of the left and right edges (GUI.matrix = rotate -90 degrees),
            // where a point (x, y) is at the screen position (y, -x)
            Rect bp = menu.buttonPos;
            bool vertical = menu.windowSide == MechJebModuleMenu.WindowSide.LEFT || menu.windowSide == MechJebModuleMenu.WindowSide.RIGHT;
            Rect box = vertical ? new Rect(bp.y, -bp.x - bp.width, bp.height, bp.width) : bp;
            var center = new Vector2((box.x + box.width * 0.5f) * scale, Screen.height - (box.y + box.height * 0.5f) * scale);
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas, center, cam, out Vector3 world);
            buttonRect.sizeDelta = new Vector2(ButtonWidth / k, ButtonHeight / k);
            buttonRect.position = world;
            buttonRect.localRotation = Quaternion.Euler(0, 0, vertical ? 90 : 0);

            string text = (menu.windowStat == MechJebModuleMenu.WindowStat.HIDDEN) ^
                (menu.windowSide == MechJebModuleMenu.WindowSide.TOP || menu.windowSide == MechJebModuleMenu.WindowSide.LEFT)
                    ? upText
                    : downText;
            if (text != shownText)
            {
                shownText = text;
                button.Text(text);
            }
        }

        // ---- the Menu Position item of the Settings window ----

        static readonly string[] sideNames = Enum.GetNames(typeof(MechJebModuleMenu.WindowSide));
        static readonly Dictionary<int, string> numbers = new Dictionary<int, string>();

        /// <summary>
        /// MechJebModuleMenu's "Menu Position" general info item (MenuPosition) for a KodeKit window: [side] [-] columns [+].
        /// SettingsView can call this instead of hosting the IMGUI item.
        /// </summary>
        public static void BuildMenuPosition(KodeWindow w, UIObject parent, MechJebModuleMenu menu)
        {
            Layout row = KodeWindow.Row(parent);
            MiscViewKit.BoundButton(w, row, () => sideNames[(int)menu.windowSide], menu.CycleSide, null, true);
            KodeWindow.Button(row, "-", () => menu.columns = Math.Max(1, menu.columns - 1));
            MiscViewKit.LiveBound(w, row, "99", () =>
            {
                int c = menu.columns;
                if (!numbers.TryGetValue(c, out string s)) numbers[c] = s = c.ToString();
                return s;
            });
            KodeWindow.Button(row, "+", () => menu.columns++);
        }
    }
}
