using System;
using System.Collections.Generic;
using KodeUI;
using KSP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// GuiUtils.ComboBox for uGUI: a button showing the current entry; clicking it opens a popup list
    /// above every window, positioned under the button, clamped on screen, at the window's scale.
    /// A click on an entry selects it; a click anywhere else closes the popup without a change.
    /// </summary>
    public class ComboBox
    {
        /// <summary>The popup currently open (only one at a time, like MechJeb's static ComboBox).</summary>
        public static ComboBox Open { get; private set; }

        readonly Func<int> get;
        readonly Action<int> set;
        readonly Func<string[]> entries;
        readonly Func<int, Color> dots;
        public readonly UIButton Button;
        readonly TMP_Text label;
        GameObject popup;
        RectTransform list;

        public bool IsOpen => popup != null;
        public RectTransform PopupList => list;
        public GameObject Blocker { get; private set; }

        /// <summary>The open list's scroll view when it is too tall for the screen (null otherwise).</summary>
        public ScrollRect Scroll { get; private set; }

        static string Arrow(TMP_Text t) => t.font != null && t.font.HasCharacter('↓') ? "↓" : "v";

        /// <param name="dots">Each entry's colour, as a dot before its text in the button and the popup (the arrow selector shows the text alone).</param>
        public ComboBox(KodeWindow w, UIObject parent, Func<int> get, Action<int> set, Func<string[]> entries, bool flexible = true, Func<int, Color> dots = null)
        {
            this.get = get;
            this.set = w.Edits(set);
            this.entries = entries;
            this.dots = dots;
            Button = KodeWindow.Button(parent, "", Toggle, flexible);
            label = Button.GetComponentInChildren<TMP_Text>();

            // a dropdown field: dark field background, entry on the left, a caret on the right
            Button.Content.ChildAlignment(TextAnchor.MiddleLeft);
            Button.Content.Padding(6, 18, 1, 1);
            label.alignment = TextAlignmentOptions.Left;
            var caretGo = new GameObject("Caret", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
            caretGo.layer = Button.gameObject.layer;
            caretGo.transform.SetParent(Button.transform, false);
            caretGo.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            var caretRt = (RectTransform)caretGo.transform;
            caretRt.anchorMin = caretRt.anchorMax = caretRt.pivot = new Vector2(1, 0.5f);
            caretRt.anchoredPosition = new Vector2(-5, 0);
            var caret = caretGo.GetComponent<UnityEngine.UI.Image>();
            caret.sprite = Sprites.Caret;
            caret.raycastTarget = false;
            var bg = Button.GetComponent<UnityEngine.UI.Image>();
            var button = Button.GetComponent<UnityEngine.UI.Button>();
            KodeWindow.Styled(Button.gameObject, th =>
            {
                th.Field.ApplySelectable(button, bg);
                th.ApplyText(label, th.Text);
                label.alignment = TextAlignmentOptions.Left;
                caretRt.sizeDelta = new Vector2(th.FontSize * 0.7f, th.FontSize * 0.7f);
                caret.color = th.Text;
            });

            Color dotColor = Color.white;
            Image dot = dots != null ? Dot(Button, () => dotColor) : null;

            string shown = null;
            w.AddSync(() =>
            {
                string[] e = this.entries();
                int i = Mathf.Clamp(this.get(), 0, Math.Max(0, e.Length - 1));
                string s = e.Length == 0 ? "" : e[i];
                if (s == shown) return;
                shown = s;
                Button.Text(s);
                if (dot == null || e.Length == 0) return;
                dotColor = dots(i);
                dot.color = dotColor;
            });

            // close with the owning window
            var closer = Button.gameObject.AddComponent<CloseOnDisable>();
            closer.Combo = this;

            // the [<] entry [>] alternative (MechJeb's "Replace drop down menu with arrow selector"), switched live
            if (UseArrowSelector == null) return;
            Layout arrows = KodeWindow.Row(parent);
            if (!flexible) arrows.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 0;
            arrows.transform.SetSiblingIndex(Button.transform.GetSiblingIndex() + 1);
            KodeWindow.Button(arrows, "<", () =>
            {
                int n = this.entries().Length;
                if (n > 0) this.set((Mathf.Clamp(this.get(), 0, n - 1) - 1 + n) % n);
            });
            UIText current = w.BoundLabel(arrows, () =>
            {
                string[] e = this.entries();
                return e.Length == 0 ? " " : e[Mathf.Clamp(this.get(), 0, e.Length - 1)];
            }, true);
            current.tmpText.alignment = TextAlignmentOptions.Center;
            if (!flexible)
            {
                // a flexible label in a row that doesn't stretch gets no width at all: size it for the widest entry
                GameObject cell = KodeWindow.CellOf(current);
                var le = cell.GetComponent<UnityEngine.UI.LayoutElement>() ?? cell.AddComponent<UnityEngine.UI.LayoutElement>();
                string[] fitted = null;
                void FitWidest()
                {
                    fitted = this.entries();
                    float widest = 0;
                    foreach (string e in fitted)
                        widest = Mathf.Max(widest, TextMeasure.Measure(e ?? "", KodeWindow.ThemeOf(arrows.gameObject)).x);
                    le.minWidth = le.preferredWidth = widest + 8;
                }
                FitWidest();
                w.Behaviour.AddRestyle(FitWidest);
                // a list that changes comes as a new array
                w.AddSync(() =>
                {
                    if (this.entries() != fitted) FitWidest();
                });
            }
            KodeWindow.Button(arrows, ">", () =>
            {
                int n = this.entries().Length;
                if (n > 0) this.set((Mathf.Clamp(this.get(), 0, n - 1) + 1) % n);
            });
            bool useArrows = !UseArrowSelector();
            w.AddSync(() =>
            {
                bool a = UseArrowSelector();
                if (a == useArrows) return;
                useArrows = a;
                Button.gameObject.SetActive(!a);
                arrows.gameObject.SetActive(a);
            });
        }

        /// <summary>
        /// The group of each entry (null: none). The popup lists consecutive entries of a group under its header, so a
        /// long list (the planner's operations) reads as a few short ones.
        /// </summary>
        public Func<int, string> Group;

        /// <summary>When set and true, combos show as [&lt;] entry [&gt;] arrow selectors instead (checked every frame).</summary>
        public static Func<bool> UseArrowSelector;

        class CloseOnDisable : MonoBehaviour
        {
            public ComboBox Combo;
            void OnDisable() => Combo?.Close();
        }

        // the chips' colour dot; the restyle that repaints icons reads its colour again
        static Image Dot(UIButton b, Func<Color> color)
        {
            Image dot = KodeWindow.AddIcon(b, Sprites.Circle, true);
            dot.name = "Dot";
            var le = dot.GetComponent<LayoutElement>();
            b.Content.Spacing(4);
            KodeWindow.Styled(dot.gameObject, th =>
            {
                le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = Mathf.Round(th.FontSize * 0.6f);
                dot.color = color();
            });
            return dot;
        }

        void Toggle()
        {
            if (IsOpen) Close();
            else Show();
        }

        public void Show()
        {
            Open?.Close();
            Open = this;

            Canvas root = Overlay.Root;
            var canvasRt = (RectTransform)root.transform;

            // must be on the UI layer: UIMainCamera culls other layers (raycasts still work, so this fails silently)
            popup = new GameObject("ComboPopup", typeof(RectTransform)) { layer = root.gameObject.layer };
            var rt = (RectTransform)popup.transform;
            rt.SetParent(canvasRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Overlay.Setup(popup.AddComponent<Canvas>(), root, 100);
            popup.AddComponent<GraphicRaycaster>();

            // full-screen transparent blocker: a click outside the list closes without a change
            Blocker = new GameObject("Blocker", typeof(RectTransform)) { layer = root.gameObject.layer };
            var brt = (RectTransform)Blocker.transform;
            brt.SetParent(rt, false);
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            Blocker.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            Blocker.AddComponent<Button>().onClick.AddListener(Close);

            // the list, as a KodeUI panel so it gets the window look, a lock while hovered and the window's scale
            LayoutPanel panel = UIKit.CreateUI<LayoutPanel>(rt, "List");
            panel.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(1).Padding(3);
            panel.PreferredSizeFitter(true, true).Anchor(AnchorPresets.TopLeft).Pivot(PivotPresets.TopLeft);
            panel.Finish();
            KodeWindow.ThemeOf(Button.gameObject).Window.ApplyImage(panel.BackGround);
            list = panel.rectTransform;
            panel.gameObject.AddComponent<WindowBehaviour>(); // click-through lock while hovered

            string[] e = entries();
            var rows = new UIButton[e.Length];
            string lastGroup = null;
            for (int i = 0; i < e.Length; i++)
            {
                string g = Group?.Invoke(i);
                if (g != null && g != lastGroup) KodeWindow.Header(panel, g, true);
                lastGroup = g;
                int k = i;
                UIButton b = rows[i] = KodeWindow.Button(panel, e[i], () => Select(k), true);
                b.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
                if (Group != null) b.Content.Padding(14, 6, 1, 1);
                if (dots != null)
                {
                    Color color = dots(i);
                    Dot(b, () => color);
                    b.Content.ChildAlignment(TextAnchor.MiddleLeft);
                }
            }
            panel.gameObject.AddComponent<LayoutElement>().minWidth = ((RectTransform)Button.transform).rect.width;

            // same scale as the window the button lives in (MechJeb UI scale)
            float scale = Button.transform.lossyScale.x / canvasRt.lossyScale.x;
            list.localScale = new Vector3(scale, scale, 1);

            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            float maxHeight = MaxListHeight() / scale;
            if (list.rect.height > maxHeight) ScrollRows(panel, maxHeight, rows);
            PlaceUnderButton();
        }

        /// <summary>
        /// The tallest list that fits on screen, in canvas units: the room below the button or above it, whichever is larger
        /// (PlaceUnderButton flips the list above when it does not fit below).
        /// </summary>
        float MaxListHeight()
        {
            var corners = new Vector3[4];
            ((RectTransform)Button.transform).GetWorldCorners(corners); // 0 = bottom-left, 1 = top-left
            var parent = (RectTransform)list.parent;
            Rect p = parent.rect;
            float below = parent.InverseTransformPoint(corners[0]).y - p.yMin;
            float above = p.yMax - parent.InverseTransformPoint(corners[1]).y;
            const float margin = 8;
            return Mathf.Max(Mathf.Max(below, above) - margin, Mathf.Min(200, p.height));
        }

        /// <summary>
        /// A list too tall for the screen (a planet pack's bodies, #1583): its rows move into a scroll view of the given
        /// height, as wide as the rows were, scrolled to the selected entry.
        /// </summary>
        void ScrollRows(LayoutPanel panel, float height, UIButton[] rows)
        {
            float width = list.rect.width;
            var children = new List<Transform>();
            foreach (Transform t in panel.transform) children.Add(t);

            ScrollView sv = KodeWindow.ScrollList(panel, height, out Layout content, 1);
            sv.BackgroundColor(Color.clear); // the panel already has the window look
            var le = sv.GetComponent<LayoutElement>() ?? sv.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width + 12; // ScrollRect reports no width of its own; 12 = the scrollbar's room
            foreach (Transform t in children) t.SetParent(content.transform, false);
            Scroll = sv.ScrollRect;
            // the content first: the list's rebuild stops at the viewport, which is no layout element, so the rows would
            // have no positions yet and the content no height
            LayoutRebuilder.ForceRebuildLayoutImmediate(content.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);

            int selected = get();
            if (selected < 0 || selected >= rows.Length) return;
            var row = (RectTransform)rows[selected].transform;
            RectTransform c = content.rectTransform;
            float viewport = sv.ScrollRect.viewport.rect.height;
            float top = -(row.localPosition.y + row.rect.yMax); // from the content's top, which is its pivot
            float y = Mathf.Clamp(top - (viewport - row.rect.height) / 2, 0, Mathf.Max(0, c.rect.height - viewport));
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, y);
        }

        void PlaceUnderButton()
        {
            var corners = new Vector3[4];
            ((RectTransform)Button.transform).GetWorldCorners(corners); // 0 = bottom-left
            var parent = (RectTransform)list.parent;
            Vector3 local = parent.InverseTransformPoint(corners[0]);
            list.localPosition = new Vector3(local.x, local.y, 0);

            // clamp fully on screen (flip above the button if there is no room below)
            Rect p = parent.rect;
            Vector2 size = list.rect.size * list.localScale.x;
            Vector3 pos = list.localPosition;
            if (pos.y - size.y < p.yMin)
            {
                Vector3 top = parent.InverseTransformPoint(corners[1]);
                pos.y = Mathf.Min(top.y + size.y, p.yMax);
            }
            pos.x = Mathf.Clamp(pos.x, p.xMin, p.xMax - size.x);
            list.localPosition = pos;
        }

        void Select(int i)
        {
            Close();
            if (i != get()) set(i);
        }

        public void Close()
        {
            if (popup != null) UnityEngine.Object.Destroy(popup);
            popup = null;
            list = null;
            Blocker = null;
            Scroll = null;
            if (Open == this) Open = null;
        }

        /// <summary>For tests: the entry buttons in the open popup.</summary>
        public List<Button> EntryButtons()
        {
            var r = new List<Button>();
            if (list != null) r.AddRange(list.GetComponentsInChildren<Button>());
            return r;
        }
    }
}
