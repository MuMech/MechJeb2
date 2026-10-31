using System;
using System.Collections.Generic;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>A short highlight after a click, so an action with no visible result still shows it happened.</summary>
    public class ClickFlash : MonoBehaviour, IPointerClickHandler
    {
        public Image Target;
        public static Color FlashColor = new Color(0.55f, 1f, 0.55f, 1f);
        const float Duration = 0.35f;
        float start = -1;
        Color baseColor, written;

        public void OnPointerClick(PointerEventData e)
        {
            if (Target == null) return;
            if (start < 0) written = baseColor = Target.color;
            start = Time.unscaledTime;
        }

        void Update()
        {
            if (start < 0) return;
            // repainted since the last frame (a selection turning green, a role change): fade back to the new colour
            if (Target.color != written) baseColor = Target.color;
            float t = (Time.unscaledTime - start) / Duration;
            if (t >= 1)
            {
                Target.color = baseColor;
                start = -1;
                return;
            }
            Target.color = written = Color.Lerp(FlashColor, baseColor, t * t);
        }

        void OnDisable()
        {
            if (start < 0 || Target == null) return;
            Target.color = baseColor;
            start = -1;
        }
    }

    /// <summary>
    /// KodeUI's Window moves itself when its empty background is dragged, bypassing KodeKit's clamp, window lock and
    /// position save (and doubling the movement when a DragHandle is on the same object). KodeKit windows drag through
    /// DragHandle only.
    /// </summary>
    public class KodeRootWindow : Window
    {
        public override void OnBeginDrag(PointerEventData eventData) { }
        public override void OnDrag(PointerEventData eventData) { }
        public override void OnEndDrag(PointerEventData eventData) { }
    }

    /// <summary>Calls Click when the object is clicked (labels that act like buttons).</summary>
    public class ClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public Action Click;
        public void OnPointerClick(PointerEventData e) => Click?.Invoke();
    }

    /// <summary>Hold-to-repeat behaviour for GUILayout.RepeatButton parity (fires at once, then every 0.2 s).</summary>
    public class RepeatTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action Action;
        bool held;
        float next;

        public void OnPointerDown(PointerEventData e) { held = true; next = 0; }
        public void OnPointerUp(PointerEventData e) => held = false;
        public void OnPointerExit(PointerEventData e) => held = false;

        void Update()
        {
            if (!held || Time.time < next) return;
            next = Time.time + 0.2f;
            Action?.Invoke();
        }
    }

    /// <summary>
    /// A retained window plus the bindings that push model changes into it. MechJeb-style widgets
    /// (toggled text box, numeric field, arrow selector, selection grid) are built on KodeUI here,
    /// because KodeUI has none of them. Every widget follows Theme.Current and re-localizes on a
    /// language switch; text starting with '#' is a localization key.
    /// </summary>
    public partial class KodeWindow
    {
        public Window Root;
        readonly List<Action> syncs = new List<Action>();

        static Theme T => Theme.Current ?? SetDefaultTheme();

        static Theme SetDefaultTheme()
        {
            Theme.Set(Theme.KodeDefault());
            return Theme.Current;
        }

        /// <summary>Register a per-frame update for a custom widget.</summary>
        public void AddSync(Action sync) => syncs.Add(sync);

        HashSet<Action> failed; // the syncs that threw, so each is logged once

        /// <summary>
        /// Runs the per-frame updates. One that throws is logged the first time and the others still run, so a broken
        /// readout does not freeze the rest of the window. It is still called every frame: it recovers when its cause
        /// goes (a target that comes back).
        /// </summary>
        public void Sync()
        {
            for (int i = 0; i < syncs.Count; i++)
            {
                try
                {
                    syncs[i]();
                }
                catch (Exception e)
                {
                    if (failed == null) failed = new HashSet<Action>();
                    if (!failed.Add(syncs[i])) continue;
                    Debug.LogError($"[KodeKit] an update of window \"{(TitleText != null ? TitleText.tmpText.text : Root.name)}\" threw (logged once, the others still run):");
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>Give each window its own nested Canvas so a dirty text only re-batches that window.</summary>
        public static bool CanvasPerWindow;

        public KodeWindow(RectTransform canvas, string title, float minWidth, Vector2 pos)
        {
            Root = UIKit.CreateUI<KodeRootWindow>(canvas, title);
            if (CanvasPerWindow)
            {
                Root.gameObject.AddComponent<Canvas>();
                Root.gameObject.AddComponent<GraphicRaycaster>();
            }
            Root.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false);
            Root.PreferredSizeFitter(true, true).Anchor(AnchorPresets.TopLeft).Pivot(PivotPresets.TopLeft);
            Root.Finish();
            Root.gameObject.AddComponent<LayoutElement>().minWidth = minWidth;
            Root.rectTransform.anchoredPosition = new Vector2(pos.x, -pos.y);

            Behaviour = Root.gameObject.AddComponent<WindowBehaviour>();
            Window root = Root;
            Styled(Root.gameObject, t =>
            {
                t.Window.ApplyImage(root.BackGround);
                root.Padding(t.Padding, t.Padding, 2, t.Padding).Spacing(t.Spacing);
            });

            Layout titleRow = Row(Root);
            TitleRow = titleRow;
            titleRow.gameObject.AddComponent<DragHandle>().Target = Behaviour;
            // the rest of the window drags too (GUI.DragWindow), unless MechJeb's "titlebar only" setting is on
            BodyDrag = Root.gameObject.AddComponent<DragHandle>();
            BodyDrag.Target = Behaviour;
            BodyDrag.Enabled = () => !TitleBarOnly();
            UIText titleText = Label(titleRow, title, true);
            // the modern layout puts the title on the left, where the eye starts, and draws the title-bar buttons as icons
            titleText.tmpText.alignment = ModernLayout ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            titleText.tmpText.raycastTarget = true; // so the title bar catches drags
            TitleText = titleText;
            Action close = () => (OnClose ?? (() => Root.gameObject.SetActive(false)))();
            if (ModernLayout)
            {
                CloseButton = IconButton(titleRow, Sprites.Cross, close);
                FlatIcon(CloseButton, true);
            }
            else
            {
                CloseButton = Button(titleRow, "x", close);
                SquareButton(CloseButton, true);
            }
        }

        /// <summary>The title label (a window can show something other than its module's name, e.g. the menu's version).</summary>
        public UIText TitleText { get; }

        /// <summary>The title bar's close button.</summary>
        public UIButton CloseButton { get; }

        /// <summary>
        /// A small square title-bar button: an outlined frame instead of a full button; the close button turns red on hover.
        /// </summary>
        static void SquareButton(UIButton b, bool close)
        {
            var button = b.GetComponent<Button>();
            var img = b.GetComponent<Image>();
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            b.Padding(0);
            var le = b.gameObject.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
            Styled(b.gameObject, th =>
            {
                float size = th.FontSize + 3;
                le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = size;
                img.sprite = Sprites.CheckBox;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                button.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.normalColor = new Color(1, 1, 1, 0.8f);
                cb.highlightedColor = close ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 1f, 0.7f);
                cb.pressedColor = cb.highlightedColor * 0.8f;
                cb.selectedColor = cb.normalColor;
                button.colors = cb;
                th.ApplyText(label, th.Text);
                label.fontSize = th.FontSize - 2;
            });
        }

        public WindowBehaviour Behaviour { get; }

        /// <summary>What the close button does; hides the window when null.</summary>
        public Action OnClose;

        /// <summary>The drag handle on the window body (dragging anywhere but a control moves the window).</summary>
        public DragHandle BodyDrag { get; }

        /// <summary>True when only the title bar drags windows (MechJeb's "Use only the titlebar for window dragging").</summary>
        public static Func<bool> TitleBarOnly = () => false;

        /// <summary>The title bar (title text and close button); also the drag handle.</summary>
        public Layout TitleRow { get; }

        /// <summary>
        /// A small button at the left of the title bar (like the custom windows' "E" and "C"); in the modern layout a flat one
        /// on the right, before the close button, like the title-bar icons.
        /// </summary>
        public UIButton TitleButton(string text, Action onClick)
        {
            UIButton b = Button(TitleRow, text, onClick);
            if (ModernLayout)
            {
                FlatIcon(b, false);
                b.transform.SetSiblingIndex(CloseButton.transform.GetSiblingIndex());
                return b;
            }
            SquareButton(b, false);
            b.transform.SetSiblingIndex(0);
            return b;
        }

        /// <summary>GUILayout.HorizontalSlider: a flexible-width slider bound to a value.</summary>
        public Slider Slider(UIObject parent, Func<float> get, Action<float> set, float min = 0, float max = 1)
        {
            GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.transform.SetParent(parent.transform, false);
            // DefaultControls builds on layer 0, which UIMainCamera culls (FINDINGS rule 9)
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = parent.gameObject.layer;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minWidth = 40;
            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(get());
            slider.onValueChanged.AddListener(v => set(v));
            // thin track with a round handle: DefaultControls' 10 px bar and 20 px square handle are too heavy
            Image[] images = go.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                var rt = img.rectTransform;
                switch (img.gameObject.name)
                {
                    case "Background":
                        rt.anchorMin = new Vector2(0, 0.5f);
                        rt.anchorMax = new Vector2(1, 0.5f);
                        rt.sizeDelta = new Vector2(0, 3);
                        break;
                    case "Fill":
                        rt.parent.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
                        rt.parent.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
                        rt.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(-12, 3);
                        rt.sizeDelta = new Vector2(6, 0);
                        break;
                    case "Handle":
                        img.sprite = Sprites.Circle;
                        break;
                }
                if (img.gameObject.name != "Handle") img.sprite = null;
            }
            Styled(go, th =>
            {
                le.preferredHeight = th.FontSize;
                foreach (Image img in images)
                {
                    if (img.gameObject.name == "Handle")
                    {
                        img.color = th.Text;
                        img.rectTransform.sizeDelta = new Vector2(th.FontSize - 2, 0);
                        img.rectTransform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(-(th.FontSize - 2), 0);
                    }
                    else img.color = img.gameObject.name == "Fill" ? th.Good * 0.8f : new Color(1, 1, 1, 0.25f);
                }
            });
            syncs.Add(() =>
            {
                float v = get();
                if (!Mathf.Approximately(slider.value, v)) slider.SetValueWithoutNotify(v);
            });
            return slider;
        }

        // ---- panels and lists ----

        /// <summary>A vertical group that fills the width (GUILayout.BeginVertical): rows stacked, each as wide as the panel.</summary>
        public static Layout Panel(UIObject parent)
        {
            Layout l = parent.Add<Layout>("Panel");
            l.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false);
            l.FlexibleLayout(true, false);
            l.Finish();
            return l;
        }

        /// <summary>
        /// A vertical scroll list of a fixed height (GUILayout.BeginScrollView) on the dark list backdrop, with a scrollbar
        /// shown when needed. Rows go in <paramref name="content"/>; text changes inside relayout only the list.
        /// </summary>
        public static ScrollView ScrollList(UIObject parent, float height, out Layout content, int spacing = 0)
        {
            ScrollView sv = parent.Add<ScrollView>("Scroll");
            sv.PreferredHeight(height);
            sv.FlexibleLayout(true, false);
            sv.Horizontal(false);
            sv.Vertical(true);

            sv.BackgroundColor(ListBackground);
            ScrollRect sr = sv.ScrollRect;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25;
            sr.inertia = false;

            content = sv.Content;
            content.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(spacing);
            content.PreferredSizeFitter(true, false);
            RectTransform c = content.rectTransform;
            c.anchorMin = new Vector2(0, 1);
            c.anchorMax = new Vector2(1, 1);
            c.pivot = new Vector2(0.5f, 1);
            c.sizeDelta = Vector2.zero;
            c.anchoredPosition = Vector2.zero;

            // the viewport (which masks the rows) leaves room for the scrollbar on the right
            sv.Viewport.rectTransform.offsetMin = Vector2.zero;
            sv.Viewport.rectTransform.offsetMax = new Vector2(-12, 0);

            UIScrollbar bar = sv.Add<UIScrollbar>("Bar");
            bar.Direction(Scrollbar.Direction.BottomToTop);
            RectTransform br = bar.rectTransform;
            br.anchorMin = new Vector2(1, 0);
            br.anchorMax = new Vector2(1, 1);
            br.pivot = new Vector2(1, 1);
            br.sizeDelta = new Vector2(10, 0);
            br.anchoredPosition = Vector2.zero;
            sv.VerticalScrollbar = bar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            Image barImage = bar.GetComponent<Image>();
            Image handleImage = bar.scrollbar.handleRect.GetComponent<Image>();
            Styled(bar.gameObject, th =>
            {
                barImage.color = new Color(0, 0, 0, 0.35f);
                handleImage.color = new Color(th.Text.r, th.Text.g, th.Text.b, 0.6f);
            });
            return sv;
        }

        // ---- lists ----

        /// <summary>A dark backdrop for a vertical list of rows (ListRow), like IMGUI's scroll views.</summary>
        public static Layout ListBox(UIObject parent)
        {
            LayoutPanel box = parent.Add<LayoutPanel>("List");
            box.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(0).Padding(0);
            box.FlexibleLayout(true, false);
            box.Finish();
            box.BackGround.sprite = null;
            box.BackGround.color = ListBackground;
            return box;
        }

        public static Color ListBackground = new Color(0.07f, 0.08f, 0.09f, 0.85f);

        /// <summary>
        /// A flat, left-aligned clickable row for lists (item pickers, help topics): no button frame, a hover highlight
        /// and a hairline separator. SetSelected marks the selected row (yellow text and a bar on the left).
        /// </summary>
        public static UIButton ListRow(UIObject parent, string text, Action onClick)
        {
            UIButton b = parent.Add<UIButton>("ListRow");
            b.Text("").OnClick(() => onClick());
            b.FlexibleLayout(true, false);
            b.Finish();
            b.Padding(8, 8, 2, 2);
            b.ChildAlignment(TextAnchor.MiddleLeft);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Left;
            SetText(label, text);
            var button = b.GetComponent<Button>();
            var img = b.GetComponent<Image>();

            // separator and selection bar: drawn over the row, outside its layout
            Image line = Overlay(b, "Separator", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1));
            Image bar = Overlay(b, "SelectBar", new Vector2(0, 0), new Vector2(0, 1), new Vector2(3, 0));
            bar.color = new Color(0.95f, 0.82f, 0.35f);
            bar.gameObject.SetActive(false);

            Styled(b.gameObject, th =>
            {
                img.sprite = null;
                img.color = Color.white;
                button.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.normalColor = new Color(1, 1, 1, 0);
                cb.highlightedColor = new Color(1, 1, 1, 0.12f);
                cb.pressedColor = new Color(1, 1, 1, 0.2f);
                cb.selectedColor = cb.normalColor;
                button.colors = cb;
                th.ApplyText(label, bar.gameObject.activeSelf ? bar.color : th.Text);
                line.color = new Color(1, 1, 1, 0.07f);
            });
            return b;
        }

        /// <summary>Mark a ListRow as selected or not.</summary>
        public static void SetSelected(UIButton row, bool selected)
        {
            Transform bar = row.transform.Find("SelectBar");
            if (bar == null || bar.gameObject.activeSelf == selected) return;
            bar.gameObject.SetActive(selected);
            row.GetComponentInChildren<TMP_Text>().color = selected ? bar.GetComponent<Image>().color : ThemeOf(row.gameObject).Text;
        }

        /// <summary>A flat clickable column header (stage stats): dim text that brightens on hover, no button frame.</summary>
        public static UIButton HeaderButton(UIObject parent, string text, Action onClick)
        {
            UIButton b = ListRow(parent, text, onClick);
            b.FlexibleLayout(false, false);
            b.Padding(2, 2, 1, 1);
            b.ChildAlignment(TextAnchor.MiddleRight);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Right;
            Styled(b.gameObject, th =>
            {
                th.ApplyText(label, th.Dim);
                b.transform.Find("Separator").GetComponent<Image>().color = new Color(1, 1, 1, 0.25f);
            });
            return b;
        }

        static Image Overlay(UIObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent.transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        static readonly Dictionary<string, bool> sectionState = new Dictionary<string, bool>();

        /// <summary>
        /// A panel shown while <paramref name="visible"/> holds, its contents made by <paramref name="build"/> the first time
        /// it shows rather than with the window: a window with many alternative panels (the planner's operations, the
        /// attitude controllers) then builds only the one in view when it opens.
        /// </summary>
        public Layout LazyPanel(UIObject parent, Func<bool> visible, Action<Layout> build)
        {
            Layout panel = Panel(parent);
            Visible(panel.gameObject, visible);
            BuildWhenShown(panel, build);
            return panel;
        }

        /// <summary>A collapsible section whose contents are made the first time it is open (a folded section costs nothing until then).</summary>
        public Layout Section(UIObject parent, string title, bool open, Action<Layout> build, string key = null)
        {
            Layout content = SectionCore(parent, title, open, key ?? title, true);
            BuildWhenShown(content, build);
            return content;
        }

        // a folded section's content is filled while active (TMP needs an active parent, FINDINGS rule 4) and folded in
        // the window's first sync, which runs before the window is first drawn
        void FoldInFirstSync(Layout content, Action folded)
        {
            bool done = false;
            syncs.Add(() =>
            {
                if (done) return;
                done = true;
                content.gameObject.SetActive(false);
                folded?.Invoke();
            });
        }

        // builds now if the panel is on screen, else in the first sync where it is (TMP needs active parents to set up,
        // FINDINGS rule 4): a lazy panel inside a folded section waits for the section too
        void BuildWhenShown(Layout panel, Action<Layout> build)
        {
            if (panel.gameObject.activeInHierarchy)
            {
                build(panel);
                return;
            }
            bool built = false;
            syncs.Add(() =>
            {
                if (built || !panel.gameObject.activeInHierarchy) return;
                built = true;
                build(panel);
            });
        }

        /// <summary>
        /// A collapsible section: a header row ("-"/"+" and the title) and a content panel it shows or hides. Returns the
        /// content panel. The open/closed state is remembered per key for the session (key defaults to the title).
        /// </summary>
        public Layout Section(UIObject parent, string title, bool open = true, string key = null) => SectionCore(parent, title, open, key ?? title, false);

        // lazy: a folded section starts inactive and its contents are made when it opens; else they are made now, open
        Layout SectionCore(UIObject parent, string title, bool open, string key, bool lazy)
        {
            if (ModernLayout) return FoldSection(parent, title, open, key, lazy);
            if (sectionState.TryGetValue(key, out bool saved)) open = saved;

            Layout header = Row(parent);
            UIButton toggle = Button(header, open ? "-" : "+", () => { });
            UIText label = Label(header, title, true);
            label.tmpText.fontStyle = FontStyles.Bold;

            Layout content = parent.Add<Layout>("Section");
            content.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Padding(10, 0, 0, 2);
            content.FlexibleLayout(true, false);
            content.Finish();
            content.gameObject.SetActive(open || !lazy);
            if (!open && !lazy) FoldInFirstSync(content, null);

            void Flip()
            {
                bool o = !content.gameObject.activeSelf;
                content.gameObject.SetActive(o);
                sectionState[key] = o;
                toggle.Text(o ? "-" : "+");
            }

            toggle.GetComponent<Button>().onClick.AddListener(Flip);
            label.tmpText.raycastTarget = true;
            label.gameObject.AddComponent<ClickHandler>().Click = Flip;
            return content;
        }

        /// <summary>A static text that wraps to the window width (help texts, long descriptions).</summary>
        public static UIText Paragraph(UIObject parent, string text)
        {
            UIText t = Label(parent, text, true);
            t.tmpText.enableWordWrapping = true;
            return t;
        }

        // ---- theme and localization plumbing ----

        /// <summary>Apply a theme-dependent look now and again on every theme change (via the owning window).</summary>
        public static void Styled(GameObject go, Action<Theme> apply)
        {
            WindowBehaviour wb = go.GetComponentInParent<WindowBehaviour>();
            apply(wb != null ? wb.Theme ?? T : T);
            wb?.AddRestyle(() => apply(wb.Theme ?? T));
        }

        /// <summary>Text, or the localized string when it is a '#key'.</summary>
        public static string Loc(string text) => KeepLastLine(text != null && text.Length > 1 && text[0] == '#' ? Localizer.Format(text) : text);

        // TMP's preferred height ignores a trailing empty line, IMGUI shows it ("\nRestore factory default settings\n" is
        // a 3-line button): a space after the last newline keeps the line
        static string KeepLastLine(string s) => s != null && s.Length > 0 && s[s.Length - 1] == '\n' ? s + " " : s;

        /// <summary>
        /// Tooltip text for a localization key (null: none). Every label, toggle and button whose text is a '#key'
        /// gets that tooltip automatically, so tooltips can be added for a whole UI from one table.
        /// </summary>
        public static Func<string, string> TooltipFor;

        /// <summary>Set a text from a literal or a '#key'; keys are re-applied on a language switch.</summary>
        public static void SetText(TMP_Text t, string textOrKey)
        {
            t.text = Loc(textOrKey);
            if (textOrKey == null || textOrKey.Length < 2 || textOrKey[0] != '#') return;
            string tip = TooltipFor?.Invoke(textOrKey);
            if (tip != null && t.GetComponent<TooltipTrigger>() == null)
            {
                t.raycastTarget = true;
                Tooltips.Add(t.gameObject, tip);
            }
            t.GetComponentInParent<WindowBehaviour>()?.AddRelocalize(() => t.text = Loc(textOrKey));
        }

        // ---- layout ----

        public static Layout Row(UIObject parent)
        {
            Layout row = parent.Add<Layout>("Row");
            row.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(4).ChildAlignment(TextAnchor.MiddleLeft);
            row.FlexibleLayout(true, false);
            row.Finish();
            return row;
        }

        public static LayoutPanel Box(UIObject parent)
        {
            LayoutPanel box = parent.Add<LayoutPanel>("Box");
            box.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(2).Padding(4);
            box.FlexibleLayout(true, false);
            box.Finish();
            Styled(box.gameObject, t => t.Box.ApplyImage(box.BackGround));
            return box;
        }

        public static void FlexibleSpace(UIObject parent) => parent.Add<UIEmpty>().FlexibleLayout(true, false).Finish();

        public static void Space(UIObject parent, float w) => parent.Add<UIEmpty>().PreferredWidth(w).Finish();

        // ---- widgets ----

        public static UIText Label(UIObject parent, string text, bool flexible = false)
        {
            UIText t = parent.Add<UIText>();
            if (flexible) t.FlexibleLayout(true, false);
            t.Finish();
            t.tmpText.enableWordWrapping = false;
            t.tmpText.alignment = TextAlignmentOptions.Left; // middle-left: rows are taller than the text, keep it centred like IMGUI
            SetText(t.tmpText, text);
            var lineLe = t.gameObject.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            Styled(t.gameObject, th =>
            {
                th.ApplyText(t.tmpText);
                lineLe.minHeight = th.LineHeight; // IMGUI pads labels to a fixed row height
            });
            return t;
        }

        /// <summary>
        /// Put live labels and input fields in fixed-size boxes with no layout group, so a text change
        /// stops LayoutRebuilder.MarkLayoutForRebuild at the widget instead of rebuilding the whole window.
        /// </summary>
        public static bool IsolateLayout = true;

        /// <summary>
        /// A layout cell of fixed height (and fixed or flexible width) whose child text does not drive layout.
        /// Size it from a template string as wide as the widest expected value; it re-sizes on theme change.
        /// </summary>
        public static UIText LiveLabel(UIObject parent, string template, bool flexible) => IsolatedLabel(parent, template, flexible);

        const string LiveCellName = "LiveCell";

        /// <summary>The object that holds a label's place in its row: the fixed-size cell of a live or bound label, else the label.</summary>
        public static GameObject CellOf(UIText t)
        {
            Transform p = t.transform.parent;
            return p != null && p.name == LiveCellName ? p.gameObject : t.gameObject;
        }

        /// <summary>The row of a toggle made by Toggle, CheckBox or SwitchRow: its box or switch and its label.</summary>
        public static GameObject RowOf(Toggle t) => t.transform.parent.gameObject;

        static UIText IsolatedLabel(UIObject parent, string text, bool flexible)
        {
            UIEmpty cell = parent.Add<UIEmpty>(LiveCellName);
            UIText t = cell.Add<UIText>();
            t.Text(text).Anchor(AnchorPresets.StretchAll);
            t.Finish();
            t.tmpText.enableWordWrapping = false;
            t.tmpText.alignment = TextAlignmentOptions.Left; // middle-left
            t.tmpText.overflowMode = TextOverflowModes.Ellipsis;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;

            var le = cell.gameObject.AddComponent<LayoutElement>();
            if (flexible) le.flexibleWidth = 1;
            cell.Finish();
            Styled(cell.gameObject, th =>
            {
                th.ApplyText(t.tmpText);
                // measure the template on a copy of the settings, not on the live text (KSP's TMP overwrites the buffer)
                Vector2 size = TextMeasure.Measure(text, th);
                le.preferredHeight = Mathf.Max(size.y, th.LineHeight);
                if (!flexible) le.preferredWidth = size.x + 24;
            });
            return t;
        }

        /// <summary>
        /// A label that changes, in a cell sized from <paramref name="template"/> (the longest text it shows), so the text
        /// never drives the layout. The getter is called every frame: return cached strings.
        /// </summary>
        public UIText BoundLabel(UIObject parent, string template, Func<string> value, bool flexible = false)
        {
            UIText t = LiveLabel(parent, template, flexible);
            string last = value();
            t.Text(last);
            syncs.Add(() =>
            {
                string s = value();
                if (s == last) return;
                last = s;
                t.Text(s);
            });
            return t;
        }

        public UIText BoundLabel(UIObject parent, Func<string> value, bool flexible = false)
        {
            UIText t = IsolateLayout ? IsolatedLabel(parent, value(), flexible) : Label(parent, value(), flexible);
            string last = t.tmpText.text;
            syncs.Add(() =>
            {
                string s = value();
                if (s == last) return;
                last = s;
                t.Text(s);
            });
            return t;
        }

        /// <summary>
        /// Let a flexible BoundLabel wrap onto more lines when its text is wider than its cell, as IMGUI's labels do (an
        /// autopilot's status), instead of ending in an ellipsis. The cell grows in height. The text is measured at the
        /// cell's width only when the text or the width changes, and the layout is touched only when the line count does,
        /// so a value that keeps its lines costs no layout. For string texts (BoundLabel), not CharText ones.
        /// </summary>
        public UIText Wrap(UIText t)
        {
            GameObject cell = CellOf(t);
            var le = cell.GetComponent<LayoutElement>() ?? cell.AddComponent<LayoutElement>();
            var rt = (RectTransform)cell.transform;
            t.tmpText.enableWordWrapping = true;
            t.tmpText.overflowMode = TextOverflowModes.Overflow;
            string measured = null;
            float measuredWidth = -1;
            // a theme change sizes the cell from its first text again: measure again
            Behaviour.AddRestyle(() => measured = null);
            syncs.Add(() =>
            {
                float width = rt.rect.width;
                string s = t.tmpText.text;
                if (width <= 0 || s == measured && width == measuredWidth) return;
                measured = s;
                measuredWidth = width;
                Theme th = ThemeOf(cell);
                float height = Mathf.Max(TextMeasure.Measure(s ?? "", th, width).y, th.LineHeight);
                if (Mathf.Abs(height - le.preferredHeight) > 0.5f) le.preferredHeight = height;
            });
            return t;
        }

        public static UIButton Button(UIObject parent, string text, Action onClick, bool flexible = false)
        {
            UIButton b = parent.Add<UIButton>();
            b.Text("").OnClick(() => onClick());
            if (flexible) b.FlexibleLayout(true, false);
            b.Finish();
            b.Padding(6, 6, 1, 1);
            // KodeUI's button lays its text out at preferred width from the left: centre it, so a wide button centres its label
            // (not ChildForceExpand: that makes the layout group flexible and every button would stretch)
            b.ChildAlignment(TextAnchor.MiddleCenter);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Center; // like GUILayout.Button
            SetText(label, text);
            var button = b.GetComponent<Button>();
            var img = b.GetComponent<Image>();
            b.gameObject.AddComponent<ClickFlash>().Target = img;
            var buttonLe = b.gameObject.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
            Styled(b.gameObject, th =>
            {
                th.Button.ApplySelectable(button, img);
                th.ApplyText(label, th.Button.TextColor);
                buttonLe.minHeight = th.LineHeight;
            });
            return b;
        }

        /// <summary>
        /// IMGUI's big state buttons: a GUIStyle with padding 8 on every side (SmartASS, SmartRcs, Translatron), so the
        /// button is the text's height plus 16.
        /// </summary>
        /// <summary>A button showing only a sprite (e.g. <see cref="Sprites.Arrow"/>), sized like a one-character button.</summary>
        public static UIButton IconButton(UIObject parent, Sprite icon, Action onClick)
        {
            UIButton b = Button(parent, "", onClick);
            b.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            AddIcon(b, icon, false);
            return b;
        }

        /// <summary>
        /// A sprite inside a button, before or after its label, tinted with the button's text colour and as high as a
        /// capital letter (themes re-apply both).
        /// </summary>
        public static Image AddIcon(UIButton b, Sprite icon, bool before)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = b.gameObject.layer };
            go.transform.SetParent(b.transform, false);
            if (before) go.transform.SetSiblingIndex(0);
            var img = go.GetComponent<Image>();
            img.sprite = icon;
            img.raycastTarget = false;
            img.preserveAspect = true;
            var le = go.GetComponent<LayoutElement>();
            Styled(go, th =>
            {
                float size = Mathf.Round(th.FontSize * 0.8f);
                le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = size;
                img.color = th.Button.TextColor;
            });
            return img;
        }

        public static UIButton Padded(UIButton b, int padding = 8)
        {
            b.Padding(padding, padding, padding, padding);
            return b;
        }

        /// <summary>The row's children take the height of the tallest one (GUILayout.ExpandHeight in a horizontal group).</summary>
        public static Layout EqualHeights(Layout row)
        {
            row.ChildForceExpand(false, true);
            return row;
        }

        public static UIButton RepeatButton(UIObject parent, string text, Action action)
        {
            UIButton b = Button(parent, text, () => { });
            b.gameObject.AddComponent<RepeatTrigger>().Action = action;
            return b;
        }

        /// <summary>A state shown by a text colour (Theme.ToneColor): Normal, green, yellow, red, dimmed, or orange (Caution).</summary>
        public enum Tone { Normal, Good, Warn, Bad, Dim, Caution }

        /// <summary>A toggle whose label colour follows a state (MechJeb's Green/Yellow/RedToggle styles).</summary>
        public Toggle Toggle(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<Tone> tone) =>
            ToneLabel(Toggle(parent, text, get, set), tone);

        /// <summary>A checkbox whose label colour follows a state, in either layout.</summary>
        public Toggle CheckBox(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<Tone> tone) =>
            ToneLabel(CheckBox(parent, text, get, set), tone);

        Toggle ToneLabel(Toggle toggle, Func<Tone> tone)
        {
            ToneText(LabelOf(toggle), tone);
            return toggle;
        }

        /// <summary>The label of a toggle made by Toggle, CheckBox or SwitchToggle (the text that flips it).</summary>
        public static TMP_Text LabelOf(Toggle toggle) => toggle.transform.parent.GetComponentInChildren<ToggleLabelClick>().GetComponent<TMP_Text>();

        /// <summary>A checkbox and its label; a switch in the modern layout. The label flips it too.</summary>
        public Toggle Toggle(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<bool> green = null) =>
            ModernLayout ? SwitchToggle(parent, text, get, set, green) : CheckBox(parent, text, get, set, green);

        /// <summary>A checkbox and its label in either layout (the menu's window list keeps checkboxes in the modern layout).</summary>
        public Toggle CheckBox(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<bool> green = null)
        {
            Layout row = Row(parent);
            UIToggle t = row.Add<UIToggle>();
            t.PreferredSize(16, 16).Finish();
            UIText label = Label(row, text, true);

            Toggle toggle = t.GetComponent<Toggle>();
            label.gameObject.AddComponent<ToggleLabelClick>().Toggle = toggle;
            toggle.SetIsOnWithoutNotify(get());
            toggle.onValueChanged.AddListener(v => set(v));

            var box = t.GetComponent<Image>();
            var check = (Image)toggle.graphic;
            Styled(t.gameObject, th =>
            {
                th.Toggle.ApplySelectable(toggle, box);
                th.Check.ApplyImage(check);
                t.PreferredSize(th.FontSize + 2, th.FontSize + 2);
            });

            syncs.Add(() =>
            {
                bool v = get();
                if (toggle.isOn != v) toggle.SetIsOnWithoutNotify(v);
            });
            if (green != null) ToneText(label, () => green() ? Tone.Good : Tone.Normal);
            return toggle;
        }

        public TMP_InputField Field(UIObject parent, Ed ed, float width) =>
            EditFields.Field(this, parent, () => ed.Text, s => ed.Text = s, width);

        /// <summary>GuiUtils.ToggledTextBox: [x] label [field] unit</summary>
        public void ToggledTextBox(UIObject parent, string text, Func<bool> get, Action<bool> set, Ed ed, string unit, float width, Func<bool> green = null)
        {
            Layout row = Row(parent);
            Toggle(row, text, get, set, green);
            Field(row, ed, width);
            if (!string.IsNullOrEmpty(unit)) Label(row, unit);
        }

        /// <summary>GuiUtils.SimpleTextBox: label [field] unit</summary>
        public void SimpleTextBox(UIObject parent, string left, Ed ed, string unit, float width, bool horizontalFraming = true)
        {
            UIObject row = horizontalFraming ? Row(parent) : parent;
            if (!string.IsNullOrEmpty(left)) Label(row, left);
            Field(row, ed, width);
            if (!string.IsNullOrEmpty(unit)) Label(row, unit);
        }

        /// <summary>GuiUtils.ArrowSelector: [&lt;] label [&gt;]</summary>
        public void ArrowSelector(UIObject parent, Func<int> get, Action<int> set, int count, Func<string> label) =>
            ArrowSelector(parent, get, set, () => count, label);

        /// <summary>ArrowSelector over a list whose length changes (e.g. maneuver nodes).</summary>
        public void ArrowSelector(UIObject parent, Func<int> get, Action<int> set, Func<int> count, Func<string> label)
        {
            Layout row = Row(parent);
            Button(row, "<", () =>
            {
                int n = count();
                if (n > 0) set((Mathf.Clamp(get(), 0, n - 1) - 1 + n) % n);
            });
            BoundLabel(row, label, true).tmpText.alignment = TextAlignmentOptions.Center;
            Button(row, ">", () =>
            {
                int n = count();
                if (n > 0) set((Mathf.Clamp(get(), 0, n - 1) + 1) % n);
            });
        }

        /// <summary>GUILayout.SelectionGrid with one row.</summary>
        /// <param name="padding">0: normal buttons; else like <see cref="Padded"/>.</param>
        public void SelectionGrid(UIObject parent, string[] items, Func<int> get, Action<int> set, int padding = 0)
        {
            if (ModernLayout)
            {
                Segmented(parent, items, get, set);
                return;
            }
            var images = new Image[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                int k = i;
                UIButton b = Button(parent, items[i], () => set(k), true);
                if (padding > 0) Padded(b, padding);
                images[i] = b.GetComponent<Image>();
            }

            int shown = -1;
            Action paint = () =>
            {
                for (int i = 0; i < images.Length; i++)
                    images[i].color = i == shown ? new Color(0.5f, 1f, 0.5f) : T.Button.Tint;
            };
            Behaviour.AddRestyle(paint);
            syncs.Add(() =>
            {
                int v = get();
                if (v == shown) return;
                shown = v;
                paint();
            });
        }

        /// <summary>Shows a live or bound label's cell (or a plain label) while <paramref name="visible"/> holds.</summary>
        public void Visible(UIText t, Func<bool> visible) => Visible(CellOf(t), visible);

        /// <summary>Shows a toggle's whole row while <paramref name="visible"/> holds.</summary>
        public void Visible(Toggle t, Func<bool> visible) => Visible(RowOf(t), visible);

        public void Visible(GameObject go, Func<bool> visible)
        {
            bool shown = visible();
            go.SetActive(shown);
            syncs.Add(() =>
            {
                bool v = visible();
                if (v == shown) return;
                shown = v;
                go.SetActive(v);
            });
        }
    }

    /// <summary>
    /// Measures text with a hidden TMP object: KSP's TMP overwrites an object's own text buffer in
    /// GetPreferredValues(string), so visible texts must never be used for measuring.
    /// </summary>
    public static class TextMeasure
    {
        static TextMeshProUGUI measurer;

        public static Vector2 Measure(string text, Theme theme)
        {
            if (measurer == null)
            {
                var go = new GameObject("KodeKit.TextMeasure", typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(go);
                measurer = go.AddComponent<TextMeshProUGUI>();
                measurer.enableWordWrapping = false;
                measurer.enabled = false; // never drawn
            }
            theme.ApplyText(measurer);
            return measurer.GetPreferredValues(text);
        }

        /// <summary>The size of text word-wrapped at maxWidth.</summary>
        public static Vector2 Measure(string text, Theme theme, float maxWidth)
        {
            Measure("", theme);
            measurer.enableWordWrapping = true;
            Vector2 size = measurer.GetPreferredValues(text, maxWidth, 0);
            measurer.enableWordWrapping = false;
            return size;
        }
    }
}
