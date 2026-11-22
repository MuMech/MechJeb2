using System;
using System.Collections.Generic;
using KodeKit.Headless;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Opens a context menu (<see cref="ContextMenus"/>) on a right-click: a press and release of the right button without a drag.
    /// A right drag stays the camera's (KSP orbits it on a right drag over a window, rule 57) and opens nothing. The object, or a
    /// child, must catch the pointer (a <see cref="RaycastArea"/> or a raycast-target text). Disabled: no menu.
    /// </summary>
    public sealed class ContextMenuTrigger : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>The menu's entries, built at each open (null or no action: no menu).</summary>
        public Func<List<MenuEntry>> Entries;

        /// <summary>What the menu is about (its first caption and the model's label), or null.</summary>
        public Func<string> Label;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Right || !IsClick(e)) return;
            ContextMenus.Open(this, e.position);
        }

        /// <summary>
        /// Whether a release is a click, not the end of a drag: uGUI sends the click after a drag when the pressed object is also the
        /// one dragged (a title bar), or when nothing under it takes drags, so the press point and the drag flag are checked here.
        /// </summary>
        public static bool IsClick(PointerEventData e)
        {
            if (e.dragging) return false;
            EventSystem es = EventSystem.current;
            float t = es != null ? es.pixelDragThreshold : 10;
            return (e.position - e.pressPosition).sqrMagnitude <= t * t;
        }

        void OnDisable() => ContextMenus.OwnerGone(this);
    }

    /// <summary>
    /// The one open context menu, drawn from <see cref="Model"/> on its own overlay canvas (above the drop-downs, under the toasts and
    /// dialogs), placed at the pointer by <see cref="PopupPlacer"/> at the scale of the window it was opened on, in that window's theme,
    /// and a layer of the popup stack (<see cref="Popups.Layers"/>: a scene change or its owner closing closes it). Pointer-only: it
    /// takes no keyboard lock and reads no key. Each frame it tells the model where the pointer is, whether a button went down outside
    /// it (that press still reaches what is under it: there is no blocker) and whether any key went down (the key still does what it
    /// does: Escape pauses KSP). No entry is ever the EventSystem's selected object (navigation off), so uGUI's arrow keys can't move
    /// to one. A window hovered by the pointer takes the usual click-through lock (the panel's <see cref="WindowBehaviour"/>, or the
    /// host's: <see cref="Hovered"/>), which keeps the flight keys.
    /// </summary>
    public static class ContextMenus
    {
        /// <summary>The menu: its entries, its close rules, its last close reason (for tests and the agent bridge).</summary>
        public static readonly MenuModel Model = new MenuModel();

        /// <summary>The trigger the open menu belongs to (null: none open).</summary>
        public static ContextMenuTrigger Trigger { get; private set; }

        /// <summary>The open menu's panel (null: none open): its box, its scale.</summary>
        public static RectTransform Panel { get; private set; }

        /// <summary>The open menu's layer on the popup stack (null: none open).</summary>
        public static PopupLayer Layer { get; private set; }

        /// <summary>Where the open menu was placed (canvas units of its overlay).</summary>
        public static PopupPlacement Placement { get; private set; }

        /// <summary>The open menu's row of each entry, by entry index (null for a separator; a caption's is its label's object).</summary>
        public static IReadOnlyList<GameObject> Rows => rows;

        /// <summary>The open menu's button of each entry, by entry index (null where the entry is no action).</summary>
        public static IReadOnlyList<Button> Buttons => buttons;

        /// <summary>The overlay canvas's rect, the space of <see cref="Placement"/> (null: none open).</summary>
        public static RectTransform CanvasRect { get; private set; }

        /// <summary>Whether the pointer is over the open menu (a host that owns the click-through lock reads it).</summary>
        public static bool Hovered => panelBehaviour != null && panelBehaviour.Hovered;

        /// <summary>For tests: the pointer's screen position, instead of the mouse's, for the leave and press rules (null: the mouse).</summary>
        public static Vector2? PointerOverride;

        /// <summary>How many menus opened since the game started (for tests).</summary>
        public static int OpenCount { get; private set; }

        static readonly List<GameObject> rows = new List<GameObject>();
        static readonly List<Button> buttons = new List<Button>();
        static GameObject popup;
        static WindowBehaviour panelBehaviour;
        static int openedFrame = -1;
        static bool hooked;

        static double Now => Time.unscaledTime;

        /// <summary>Opens <paramref name="t"/>'s menu at screen point <paramref name="screen"/>; false when it has no action.</summary>
        public static bool Open(ContextMenuTrigger t, Vector2 screen)
        {
            if (t == null || !t.isActiveAndEnabled) return false;
            if (!hooked)
            {
                hooked = true;
                Model.Closed += _ => Teardown();
                var go = new GameObject("KodeKit.ContextMenus");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
            List<MenuEntry> entries;
            try
            {
                entries = t.Entries?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            string label = t.Label?.Invoke();
            Model.Label = label;
            if (!Model.Open(t, entries, Now)) return false;
            OpenCount++;
            Trigger = t;
            openedFrame = Time.frameCount;
            Build(t, screen);
            return true;
        }

        /// <summary>Closes the open menu (its host's close: <see cref="MenuClose.Program"/>).</summary>
        public static void Close() => Model.Close(MenuClose.Program);

        /// <summary>Picks entry <paramref name="index"/> of the open menu as a click on it does (for tests and the agent bridge).</summary>
        public static bool Pick(int index) => Model.Pick(index, KodeWindow.Pointer);

        internal static void OwnerGone(ContextMenuTrigger t)
        {
            if (t == null || t != Trigger || !Model.IsOpen) return;
            if (Layer != null && Layer.IsOpen) Layer.Dismiss(PopupDismiss.OwnerClosed);
            else Model.Close(MenuClose.OwnerClosed);
        }

        static void Build(ContextMenuTrigger t, Vector2 screen)
        {
            Canvas root = Overlay.Root;
            var canvasRt = (RectTransform)root.transform;
            // on the UI layer: UIMainCamera culls the others (raycasts would still work, so that fails silently)
            popup = new GameObject("ContextMenu", typeof(RectTransform)) { layer = root.gameObject.layer };
            var rt = (RectTransform)popup.transform;
            rt.SetParent(canvasRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Overlay.Setup(popup.AddComponent<Canvas>(), root, Overlay.MenuOrder);
            popup.AddComponent<GraphicRaycaster>();
            CanvasRect = rt;

            // the window's look, its theme (a custom window's own Compact) and its scale
            WindowBehaviour owner = t.GetComponentInParent<WindowBehaviour>();
            LayoutPanel panel = UIKit.CreateUI<LayoutPanel>(rt, "Menu");
            panel.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(0).Padding(3);
            panel.PreferredSizeFitter(true, true).Anchor(AnchorPresets.TopLeft).Pivot(PivotPresets.TopLeft);
            panel.Finish();
            panelBehaviour = panel.gameObject.AddComponent<WindowBehaviour>(); // the click-through lock while hovered
            if (owner != null) panelBehaviour.ThemeOverride = () => owner.Theme;
            // opaque whatever the windows' opacity and the skin's sprite (its entries sit over a window's values): the panel's own
            // image is a dark opaque backing of the look's shape, and the window's look is drawn over it by a child, under the entries
            Theme theme = KodeWindow.ThemeOf(panel.gameObject);
            var lookGo = new GameObject("Look", typeof(RectTransform)) { layer = root.gameObject.layer };
            var look = (RectTransform)lookGo.transform;
            look.SetParent(panel.transform, false);
            look.anchorMin = Vector2.zero;
            look.anchorMax = Vector2.one;
            look.offsetMin = look.offsetMax = Vector2.zero;
            lookGo.AddComponent<LayoutElement>().ignoreLayout = true;
            var lookImage = lookGo.AddComponent<Image>();
            lookImage.raycastTarget = false;
            theme.Window.ApplyImage(lookImage);
            theme.Window.ApplyImage(panel.BackGround);
            panel.BackGround.color = new Color(0.06f, 0.07f, 0.08f, 1);
            Panel = panel.rectTransform;
            panel.gameObject.AddComponent<LayoutElement>().minWidth = 120;

            rows.Clear();
            buttons.Clear();
            IReadOnlyList<MenuEntry> entries = Model.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                MenuEntry e = entries[i];
                switch (e.Kind)
                {
                    case MenuEntryKind.Separator:
                        rows.Add(KodeWindow.Rule(panel).gameObject);
                        buttons.Add(null);
                        break;
                    case MenuEntryKind.Caption:
                        UIText caption = KodeWindow.Header(panel, e.Label, true);
                        caption.tmpText.raycastTarget = false;
                        rows.Add(caption.gameObject);
                        buttons.Add(null);
                        break;
                    default:
                        int k = i;
                        UIButton b = KodeWindow.ListRow(panel, e.Label, () => Model.Pick(k, KodeWindow.Pointer));
                        var button = b.GetComponent<Button>();
                        // never the EventSystem's selected object: uGUI's navigation would move it with the arrow keys (rule 54)
                        button.navigation = new Navigation { mode = Navigation.Mode.None };
                        if (!e.IsEnabled())
                        {
                            button.interactable = false;
                            b.gameObject.AddComponent<CanvasGroup>().alpha = 0.4f;
                        }
                        if (e.IsChecked()) KodeWindow.SetSelected(b, true);
                        b.GetComponentInChildren<TMP_Text>().raycastTarget = false;
                        rows.Add(b.gameObject);
                        buttons.Add(button);
                        break;
                }
            }

            float scale = ScaleOf(t);
            Panel.localScale = new Vector3(scale, scale, 1);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Panel);

            Vector2 local = Local(screen);
            anchor = new FrameBox(local.x, local.y, local.x, local.y);
            Layer = Popups.Push(t.gameObject);
            Place();
            PopupLayer layer = Layer;
            Layer.Dismissed += d =>
            {
                if (Layer == layer) Model.Close(MenuModel.FromDismiss(d));
            };
        }

        static FrameBox anchor;

        // below-right of the pointer (a point anchor), flipped above or left when there is no room, kept on screen
        static void Place()
        {
            Vector2 size = Panel.rect.size;
            Placement = PopupPlacer.Place(anchor, size.x, size.y, Popups.Box(CanvasRect.rect), Panel.localScale.x, PopupSide.Below, PopupAlign.Start, 0, 4);
            Popups.Put(Panel, Placement.Box);
            Model.Place(Placement.Box);
            Layer?.Place(Placement.Box);
        }

        // the menu is drawn at its trigger's window's scale (the overlay canvas is at scale 1 while a window can be at 1.5)
        static float ScaleOf(ContextMenuTrigger t)
        {
            float scale = t.transform.lossyScale.x / CanvasRect.lossyScale.x;
            return scale > 0 ? scale : 1;
        }

        static Vector2 Local(Vector2 screen)
        {
            Camera cam = Overlay.Root.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasRect, screen, cam, out Vector2 local);
            return local;
        }

        static void Teardown()
        {
            PopupLayer layer = Layer;
            Layer = null;
            if (layer != null && layer.IsOpen) layer.Dismiss(PopupDismiss.Program);
            if (popup != null) UnityEngine.Object.Destroy(popup);
            popup = null;
            Panel = null;
            CanvasRect = null;
            panelBehaviour = null;
            Trigger = null;
            rows.Clear();
            buttons.Clear();
        }

        static bool AnyMouseDown()
        {
            for (int b = 0; b < 7; b++)
                if (Input.GetMouseButtonDown(b)) return true;
            return false;
        }

        /// <summary>Feeds the open menu the pointer, the presses and the keys, once a frame.</summary>
        sealed class Driver : MonoBehaviour
        {
            void Update()
            {
                if (!Model.IsOpen || CanvasRect == null) return;
                if (Trigger == null || !Trigger.isActiveAndEnabled)
                {
                    OwnerGone(Trigger);
                    if (Model.IsOpen) Model.Close(MenuClose.OwnerClosed);
                    return;
                }
                // the right-click that opened it went down frames ago; nothing of this frame is about the new menu yet
                if (Time.frameCount == openedFrame) return;
                // its window was rescaled (MechJeb's UI scale): drawn at the new scale, placed again at the same point
                float scale = ScaleOf(Trigger);
                if (scale != Panel.localScale.x)
                {
                    Panel.localScale = new Vector3(scale, scale, 1);
                    Place();
                }

                Vector2 local = Local(PointerOverride ?? (Vector2)Input.mousePosition);
                bool mouse = AnyMouseDown();
                // a press outside: the popup stack closes the menu (the top layer); the press goes on to what is under it
                if (mouse && !Layer.Contains(local.x, local.y))
                {
                    Popups.Layers.OutsidePress(local.x, local.y);
                    if (!Model.IsOpen) return;
                    Model.Press(local.x, local.y);
                    return;
                }
                // a key (any but a mouse button): the menu closes, and the key is left to whatever reads it
                if (Input.anyKeyDown && !mouse)
                {
                    Model.KeyDown();
                    return;
                }
                Model.PointerAt(local.x, local.y, Now);
            }
        }
    }
}
