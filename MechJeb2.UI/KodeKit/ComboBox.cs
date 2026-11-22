using System;
using System.Collections.Generic;
using KodeUI;
using KSP.UI;
using TMPro;
using KodeKit.Headless;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A drop-down: a button showing the current entry; clicking it opens a popup list
    /// above every window, at the window's scale, placed by <see cref="Headless.PopupPlacer"/>: under the button, flipped above it when
    /// there is no room below, clamped on screen.
    /// A click on an entry selects it; a click anywhere else closes the popup without a change. The open list is a layer on the shared
    /// popup stack (<see cref="Popups.Layers"/>): an outside press, Escape (through its keyboard scope), its window closing or hiding and a
    /// scene change close it there.
    /// While the list is open it owns the keyboard through its <see cref="KeyboardScope"/>: KSP's keyboard lock is held (released only once Escape and Enter are up,
    /// FINDINGS rule 43), uGUI's own navigation events are off (they would move an unseen selection and submit it), and
    /// Up/Down/Home/End/PageUp/PageDown move a highlight, letters typed within a second make a prefix that moves the highlight (a letter that matches nothing is dropped), Enter picks the
    /// highlighted entry and Escape closes (FINDINGS rule 54).
    /// The behaviour is <see cref="Headless.ComboModel"/>'s (<see cref="Model"/>): the rows, the highlight, the typeahead, the pick.
    /// A drop-down with <see cref="Filter"/> set and at least <see cref="FilterMinEntries"/> entries is typeable: its popup has a filter
    /// field at the top (letters go into it; the entries are ranked by <see cref="Headless.FuzzyFilter"/> over their labels and
    /// <see cref="Keys"/>), and its rows are a <see cref="VirtualList{TRow}"/> that builds only the rows in view (a planet pack's
    /// bodies, MuMech/MechJeb2#1583). Other drop-downs are a plain popup of buttons.
    /// The open list reports the entry the pointer rests on and the one a key highlights (<see cref="EntryHovered"/>,
    /// <see cref="EntryLeft"/>), and a pick (<see cref="Picked"/>), for a host that previews an entry before it is picked
    /// (<see cref="Headless.PreviewModel{T}"/>); the highlight the list opens with is the selected entry, and is not reported.
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
        // the entries array the open popup's rows were made from: a list that changes comes as a new array, which closes it
        string[] popupEntries;
        // the open popup's rows (the built path) and the highlighted one (-1: none), with its own tint
        UIButton[] rows;
        int highlight = -1;
        Color highlightWas;
        // the typeable path: the filter field and the virtual rows
        TMP_InputField filterField;
        VirtualList<Row> virtualRows;
        float entryHeight, headerHeight;

        /// <summary>The drop-down's behaviour: its open list's rows, highlight, filter and typeahead.</summary>
        public readonly Headless.ComboModel Model;
        // the open list's keyboard scope: the lock, uGUI's navigation off, the keys
        readonly KeyboardScope keys;

        /// <summary>The drop-down's target on its window's commit path.</summary>
        public Headless.CommitTarget<int> Target { get; }

        /// <summary>
        /// The open list's entry (an index into the entries) the pointer entered, or the keyboard highlighted (a key, a typed letter,
        /// the filter), with its source. Not raised for the highlight the list opens with.
        /// </summary>
        public event Action<int, ChangeSource> EntryHovered;

        /// <summary>The pointer left the open list's entry (an index into the entries).</summary>
        public event Action<int> EntryLeft;

        /// <summary>An entry was picked (after the list closed), with its source; not raised for a pick of the selected entry.</summary>
        public event Action<int, ChangeSource> Picked;

        /// <summary>
        /// An open list's entry's hover card (null: none): fills the card of the entry (an index into the entries) when it opens
        /// beside the list. A builder that adds nothing shows no card.
        /// </summary>
        public Action<int, HoverCard> EntryCard;

        // the open list's row the pointer is on (its entry is reported again if the row is bound to another), and the open in progress
        GameObject pointerRow;
        int pointerItem = -1;
        bool opening;

        public bool IsOpen => popup != null;
        public RectTransform PopupList => list;
        public GameObject Blocker { get; private set; }

        /// <summary>The open list's layer on <see cref="Popups.Layers"/> (null: closed).</summary>
        public Headless.PopupLayer Layer { get; private set; }

        /// <summary>Where the open list was last placed (its box on the popup canvas, the side, whether it flipped or was clamped).</summary>
        public Headless.PopupPlacement Placement { get; private set; }

        /// <summary>The open list's scroll view when it is too tall for the screen (null otherwise).</summary>
        public ScrollRect Scroll { get; private set; }

        static string Arrow(TMP_Text t) => t.font != null && t.font.HasCharacter('↓') ? "↓" : "v";

        /// <param name="entries">
        /// The entries, read every frame: the same array while the list is the same, a new array when it changes (an open popup
        /// closes on a new array).
        /// </param>
        /// <param name="dots">Each entry's colour, as a dot before its text in the button and the popup (the arrow selector shows the text alone).</param>
        /// <param name="name">The drop-down's name (a '#key' or a text; null: the label right before it in its row, <see cref="KodeWindow.LabelBefore"/>).</param>
        public ComboBox(KodeWindow w, UIObject parent, Func<int> get, Action<int> set, Func<string[]> entries, bool flexible = true, Func<int, Color> dots = null,
            string name = null)
        {
            this.get = get;
            string rowLabel = name ?? KodeWindow.LabelBefore(parent);
            Headless.CommitTarget<int> target = w.Target("ComboBox", get, set);
            Target = target;
            this.set = i =>
            {
                if (IsEnabled) target.Set(i, KodeWindow.Pointer);
            };
            this.entries = entries;
            this.dots = dots;
            Model = new Headless.ComboModel(get, (i, source) =>
            {
                if (!IsEnabled) return;
                target.Set(i, source);
                Picked?.Invoke(i, source);
            }, 1.0, entries);
            Model.HighlightChanged += ShowHighlight;
            Model.RowsChanged += RowsChanged;
            Model.Closed += Teardown;
            Button = KodeWindow.Button(parent, "", Toggle, flexible);
            target.Control = Button;
            target.Model = Model;
            w.LabelTarget(target, rowLabel);
            label = Button.GetComponentInChildren<TMP_Text>();
            // on the button, which outlives the popup: the lock waits for the closing key's release, and a closing window drops it
            keys = Button.gameObject.AddComponent<KeyboardScope>();
            Headless.KeyScope scope = keys.Configure(Headless.ScopeKeys.All, true);
            scope.Key += Key;
            scope.Typed += Typeahead;
            // Escape closes the scope, and the list's layer with it
            scope.Dismissed += () => Dismiss(Headless.PopupDismiss.Escape);

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

            // a locked selector is dimmed: the field look has no disabled state of its own
            CanvasGroup dim = Button.gameObject.AddComponent<CanvasGroup>();
            string shown = null;
            w.AddSync(() =>
            {
                bool on = IsEnabled;
                if (Button.interactable != on)
                {
                    Button.interactable = on;
                    dim.alpha = on ? 1 : LockedAlpha;
                }
                if (!on && IsOpen) Close();

                string[] e = this.entries();
                if (IsOpen && e != popupEntries) Close();
                if (Model.IsOpen) Model.SetItems(e, Keys?.Invoke());
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
            UIButton previous = KodeWindow.Button(arrows, "<", () =>
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
            UIButton next = KodeWindow.Button(arrows, ">", () =>
            {
                int n = this.entries().Length;
                if (n > 0) this.set((Mathf.Clamp(this.get(), 0, n - 1) + 1) % n);
            });
            CanvasGroup dimArrows = arrows.gameObject.AddComponent<CanvasGroup>();
            bool useArrows = !UseArrowSelector();
            w.AddSync(() =>
            {
                bool on = IsEnabled;
                if (previous.interactable != on)
                {
                    previous.interactable = next.interactable = on;
                    dimArrows.alpha = on ? 1 : LockedAlpha;
                }

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

        /// <summary>
        /// Opt in to the typeable popup: a filter field over the entries when the list has at least <see cref="FilterMinEntries"/> of
        /// them (a short list stays a plain drop-down). Read at each open.
        /// </summary>
        public bool Filter;

        /// <summary>The entries' language-independent names (English), matched by the filter as well as the labels. Same array rule as the entries.</summary>
        public Func<string[]> Keys;

        /// <summary>The fewest entries for which a drop-down with <see cref="Filter"/> shows its filter field.</summary>
        public static int FilterMinEntries = 12;

        /// <summary>The open popup's filter field (null when the list is not typeable or closed).</summary>
        public TMP_InputField FilterField => filterField;

        /// <summary>The open typeable popup's rows (null otherwise): only the rows in view are built.</summary>
        public VirtualList<Row> VirtualRows => virtualRows;

        /// <summary>A row of the typeable popup: an entry button, or a group header (Header is null when the list has no groups).</summary>
        public sealed class Row
        {
            public Layout Root;
            public UIButton Entry;
            public UIText Header;
            internal Image Dot;
            internal Color DotColor = Color.white;
            internal bool Lit;
            internal Color Unlit;
        }

        /// <summary>
        /// When set and false, the selection is fixed: the button and the arrows are greyed out, and an open list closes
        /// (checked every frame).
        /// </summary>
        public Func<bool> Enabled;

        bool IsEnabled => Enabled == null || Enabled();

        /// <summary>The opacity of a control that is shown but locked.</summary>
        public const float LockedAlpha = 0.45f;

        /// <summary>When set and true, combos show as [&lt;] entry [&gt;] arrow selectors instead (checked every frame).</summary>
        public static Func<bool> UseArrowSelector;

        class CloseOnDisable : MonoBehaviour
        {
            public ComboBox Combo;
            void OnDisable() => Combo?.Dismiss(Headless.PopupDismiss.OwnerClosed);
        }

        /// <summary>An open list's row: reports the pointer entering and leaving it, with the entry it shows then.</summary>
        sealed class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            internal ComboBox Combo;
            internal Func<int> Item;
            public void OnPointerEnter(PointerEventData e) => Combo?.PointerEntered(gameObject, Item());
            public void OnPointerExit(PointerEventData e) => Combo?.PointerLeft(gameObject);
        }

        static void ReportHover(GameObject row, ComboBox combo, Func<int> item)
        {
            RowHover h = row.AddComponent<RowHover>();
            h.Combo = combo;
            h.Item = item;
            if (combo.EntryCard == null) return;
            Tooltips.Add(row, card =>
            {
                int i = item();
                if (i >= 0) combo.EntryCard?.Invoke(i, card);
            }, null, Tooltips.BesideWindow((RectTransform)row.transform));
        }

        void PointerEntered(GameObject row, int item)
        {
            if (!IsOpen || item < 0) return;
            if (pointerRow != null && pointerRow != row && pointerItem >= 0) EntryLeft?.Invoke(pointerItem);
            pointerRow = row;
            pointerItem = item;
            EntryHovered?.Invoke(item, KodeWindow.Pointer);
        }

        void PointerLeft(GameObject row)
        {
            if (row != pointerRow) return;
            int item = pointerItem;
            pointerRow = null;
            pointerItem = -1;
            if (item >= 0) EntryLeft?.Invoke(item);
        }

        // a virtual row bound to another entry while the pointer is on it (a scroll, a filter): the pointer is on that entry now
        void PointerRowBound(GameObject row, int item)
        {
            if (row != pointerRow || item == pointerItem) return;
            // its card was the entry it showed before
            TooltipTrigger t = row.GetComponent<TooltipTrigger>();
            if (t != null && Tooltips.CardTrigger == t) Tooltips.Close(t);
            if (pointerItem >= 0) EntryLeft?.Invoke(pointerItem);
            pointerItem = item;
            if (item >= 0) EntryHovered?.Invoke(item, KodeWindow.Pointer);
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
            keys.Lock();
            Layer = Popups.Push(Button.gameObject);
            Layer.Dismissed += _ => Close();

            Canvas root = Overlay.Root;
            var canvasRt = (RectTransform)root.transform;

            // must be on the UI layer: UIMainCamera culls other layers (raycasts still work, so this fails silently)
            popup = new GameObject("ComboPopup", typeof(RectTransform)) { layer = root.gameObject.layer };
            var rt = (RectTransform)popup.transform;
            rt.SetParent(canvasRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Overlay.Setup(popup.AddComponent<Canvas>(), root, Overlay.PopupOrder);
            popup.AddComponent<GraphicRaycaster>();

            // full-screen transparent blocker: a click outside the list closes without a change
            Blocker = new GameObject("Blocker", typeof(RectTransform)) { layer = root.gameObject.layer };
            var brt = (RectTransform)Blocker.transform;
            brt.SetParent(rt, false);
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            Blocker.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            // a Button, so a press there takes the keyboard off the field that had it (the input module deselects whatever a press
            // isn't on), with no selection of its own; the press is the stack's
            KodeWindow.NoSelect(Blocker.AddComponent<Button>()).transition = Selectable.Transition.None;
            Blocker.AddComponent<Popups.OutsidePressCatcher>();

            // the list, as a KodeUI panel so it gets the window look, a lock while hovered and the window's scale
            LayoutPanel panel = UIKit.CreateUI<LayoutPanel>(rt, "List");
            panel.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(1).Padding(3);
            panel.PreferredSizeFitter(true, true).Anchor(AnchorPresets.TopLeft).Pivot(PivotPresets.TopLeft);
            panel.Finish();
            KodeWindow.ThemeOf(Button.gameObject).Window.ApplyImage(panel.BackGround);
            list = panel.rectTransform;
            panel.gameObject.AddComponent<WindowBehaviour>(); // click-through lock while hovered

            string[] e = entries();
            popupEntries = e;
            bool typeable = Filter && e.Length >= FilterMinEntries;
            Model.Filterable = typeable;
            Model.Group = typeable ? Group : null;
            Model.SetItems(e, Keys?.Invoke());
            highlight = -1;

            // same scale as the window the button lives in (MechJeb UI scale)
            float scale = Button.transform.lossyScale.x / canvasRt.lossyScale.x;
            list.localScale = new Vector3(scale, scale, 1);

            if (typeable)
            {
                opening = true;
                Model.Open();
                ShowTypeable(panel, scale);
                opening = false;
                return;
            }

            var rows = new UIButton[e.Length];
            string lastGroup = null;
            for (int i = 0; i < e.Length; i++)
            {
                string g = Group?.Invoke(i);
                if (g != null && g != lastGroup) KodeWindow.Header(panel, g, true);
                lastGroup = g;
                int k = i;
                UIButton b = rows[i] = KodeWindow.Button(panel, e[i], () => Model.PickRow(k, KodeWindow.Pointer), true);
                ReportHover(b.gameObject, this, () => k);
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
            this.rows = rows;

            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            float maxHeight = MaxListHeight() / scale;
            if (list.rect.height > maxHeight) ScrollRows(panel, maxHeight, rows);
            PlaceUnderButton();
            // the model highlights the selected entry: its rows are the entries, one for one
            opening = true;
            Model.Open();
            opening = false;
        }

        // ---- the typeable popup: a filter field over a virtual list ----

        /// <summary>Runs the popup's syncs (its virtual list's) once a frame, and keeps the keyboard in the filter field.</summary>
        sealed class PopupDriver : MonoBehaviour
        {
            internal readonly List<Action> Syncs = new List<Action>();
            internal ComboBox Combo;

            void Update()
            {
                for (int i = 0; i < Syncs.Count; i++) Syncs[i]();
                Combo?.KeepFilterFocus();
            }

            // TMP moves the caret on Up (to the start) and Down (to the end) in its own update, and a field taking the keyboard back
            // puts it at the start: the next letter would go in front of the filter
            void LateUpdate() => Combo?.CaretToEnd();
        }

        bool caretToEnd;

        void CaretToEnd()
        {
            if (!caretToEnd || filterField == null || !filterField.isFocused) return;
            caretToEnd = false;
            int end = filterField.text.Length;
            filterField.caretPosition = end;
            filterField.selectionAnchorPosition = filterField.selectionFocusPosition = end;
            filterField.ForceLabelUpdate();
        }

        void ShowTypeable(LayoutPanel panel, float scale)
        {
            Theme theme = KodeWindow.ThemeOf(panel.gameObject);
            var driver = popup.AddComponent<PopupDriver>();
            driver.Combo = this;

            filterField = FilterInput(panel, theme);

            // the rows' heights: an entry and a header, measured on a row made for it and dropped
            Row probe = MakeRow(panel);
            BindEntry(probe, "Xg");
            LayoutRebuilder.ForceRebuildLayoutImmediate(probe.Root.rectTransform);
            entryHeight = LayoutUtility.GetPreferredHeight(probe.Root.rectTransform);
            headerHeight = entryHeight;
            if (probe.Header != null)
            {
                BindHeader(probe, "Xg");
                LayoutRebuilder.ForceRebuildLayoutImmediate(probe.Root.rectTransform);
                headerHeight = LayoutUtility.GetPreferredHeight(probe.Root.rectTransform);
            }
            probe.Root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(probe.Root.gameObject);

            // as wide as the widest entry (measured once per entries array), at least the button's width
            float buttonWidth = ((RectTransform)Button.transform).rect.width;
            float width = Mathf.Max(buttonWidth, WidestEntry(theme) + (Group != null ? 20 : 12) + (dots != null ? 12 : 0) + 12 + 6);
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            float fieldRow = list.rect.height;
            listRoom = MaxListHeight() / scale - fieldRow - 1;
            float height = RowsHeight();

            shownHeight = height;
            var v = new VirtualList<Row>(driver.Syncs.Add, panel, height, Model.RowCount, MakeRow, r => r.Root.rectTransform, BindRow, 1);
            v.View.BackgroundColor(Color.clear); // the panel already has the window look
            var le = v.View.GetComponent<LayoutElement>() ?? v.View.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            v.SetRowHeights((r, w) => Model.IsHeader(r) ? headerHeight : entryHeight);
            v.EmptyLabel("#MechJeb_NewUI_FilterNone");
            virtualRows = v;
            Scroll = v.ScrollRect;

            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            PlaceUnderButton();
            for (int i = 0; i < driver.Syncs.Count; i++) driver.Syncs[i]();
            if (Model.Highlight >= 0) v.ScrollTo(Model.Highlight, ScrollAlign.Center);

            EventSystem events = EventSystem.current;
            if (events != null) events.SetSelectedGameObject(filterField.gameObject);
            filterField.ActivateInputField();
        }

        // the widest entry of an entries array, kept while the array and the theme are the same
        string[] widestOf;
        Theme widestTheme;
        float widest;

        float WidestEntry(Theme theme)
        {
            string[] e = Model.Items;
            if (e == widestOf && theme == widestTheme) return widest;
            widestOf = e;
            widestTheme = theme;
            widest = 0;
            for (int i = 0; i < e.Length; i++) widest = Mathf.Max(widest, TextMeasure.Measure(e[i] ?? "", theme).x);
            if (Group != null)
                for (int i = 0; i < e.Length; i++)
                    widest = Mathf.Max(widest, TextMeasure.Measure(Group(i) ?? "", theme).x * 1.2f - 8);
            return widest;
        }

        TMP_InputField FilterInput(LayoutPanel panel, Theme theme)
        {
            UIInputField f = panel.Add<UIInputField>("Filter");
            f.Finish();
            f.PreferredSize(-1, theme.RowHeight);
            f.FlexibleLayout(true, false);
            Image bg = f.GetComponent<Image>();
            TMP_InputField input = f.GetComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.selectionColor = new Color(0.3f, 0.55f, 1f, 0.55f);
            input.caretWidth = 2;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onFocusSelectAll = false; // the focus taken back (after Enter with no match) keeps the typed filter
            var area = (RectTransform)f.transform.Find("Text Area");
            area.offsetMin = new Vector2(4, 1);
            area.offsetMax = new Vector2(-4, -1);
            var hint = (TextMeshProUGUI)input.placeholder;
            TMP_Text[] texts = f.GetComponentsInChildren<TextMeshProUGUI>(true);
            KodeWindow.Styled(f.gameObject, th =>
            {
                th.Field.ApplySelectable(input, bg);
                foreach (TMP_Text t in texts) th.ApplyText(t, th.Field.TextColor);
                hint.text = KodeWindow.Loc("#MechJeb_NewUI_ComboFilterHint");
                hint.fontStyle = FontStyles.Italic;
                Color c = th.Field.TextColor;
                hint.color = new Color(c.r, c.g, c.b, 0.45f);
            });
            input.onValueChanged.AddListener(s => Model.SetFilter(s));
            return input;
        }

        /// <summary>
        /// The filter field keeps the keyboard while the popup is open: Enter with no match, or a click on the scrollbar, ends its edit
        /// (TMP), and the next letter would go nowhere. Only while the popup's scope is on top: a dialog opened over the open list
        /// takes the keyboard, and the filter under its blocker must not take it back.
        /// <para>The filter is a field of the popup's scope, not a KodeKit field: it has no scope or lock of its own (the popup's holds
        /// KSP's lock and routes every key, the field types its letters), no commit target and no focus recovery.</para>
        /// </summary>
        void KeepFilterFocus()
        {
            if (filterField == null || filterField.isFocused || !keys.Scope.OnTop) return;
            EventSystem events = EventSystem.current;
            if (events == null || events.alreadySelecting) return;
            GameObject selected = events.currentSelectedGameObject;
            if (selected != null && selected != filterField.gameObject && !selected.transform.IsChildOf(popup.transform)) return;
            if (selected != filterField.gameObject) events.SetSelectedGameObject(filterField.gameObject);
            filterField.ActivateInputField();
            caretToEnd = true;
        }

        Row MakeRow(UIObject parent)
        {
            var row = new Row();
            Layout root = row.Root = parent.Add<Layout>("Row");
            root.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false);
            root.Finish();
            // a header only where the list has groups: most lists have none, and each is a text more to build
            if (Group != null) row.Header = KodeWindow.Header(root, "", true);
            row.Entry = KodeWindow.Button(root, "", () =>
            {
                // the list as it is at the click: the rows made in its constructor were made before it was assigned
                int r = virtualRows != null ? virtualRows.IndexOf(row) : -1;
                if (r >= 0) Model.PickRow(r, KodeWindow.Pointer);
            }, true);
            ReportHover(row.Entry.gameObject, this, () =>
            {
                int r = virtualRows != null ? virtualRows.IndexOf(row) : -1;
                return r >= 0 && r < Model.RowCount && !Model.IsHeader(r) ? Model.ItemAt(r) : -1;
            });
            row.Entry.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
            row.Entry.Content.ChildAlignment(TextAnchor.MiddleLeft);
            if (Group != null) row.Entry.Content.Padding(14, 6, 1, 1);
            if (dots != null) row.Dot = Dot(row.Entry, () => row.DotColor);
            row.Unlit = row.Entry.GetComponent<Image>().color;
            return row;
        }

        static void BindEntry(Row row, string text)
        {
            if (row.Header != null && row.Header.gameObject.activeSelf) row.Header.gameObject.SetActive(false);
            if (!row.Entry.gameObject.activeSelf) row.Entry.gameObject.SetActive(true);
            row.Entry.Text(text);
        }

        static void BindHeader(Row row, string text)
        {
            if (row.Entry.gameObject.activeSelf) row.Entry.gameObject.SetActive(false);
            if (!row.Header.gameObject.activeSelf) row.Header.gameObject.SetActive(true);
            row.Header.tmpText.text = (text ?? "").TrimEnd(':', ' ', ' ');
        }

        void BindRow(Row row, int r)
        {
            if (Model.IsHeader(r))
            {
                Light(row, false);
                BindHeader(row, Model.HeaderAt(r));
                PointerRowBound(row.Entry.gameObject, -1);
                return;
            }
            int item = Model.ItemAt(r);
            BindEntry(row, Model.ItemLabel(item));
            PointerRowBound(row.Entry.gameObject, item);
            if (row.Dot != null)
            {
                row.DotColor = dots(item);
                row.Dot.color = row.DotColor;
            }
            Light(row, r == Model.Highlight);
        }

        void Light(Row row, bool on)
        {
            if (row.Lit == on) return;
            row.Lit = on;
            var img = row.Entry.GetComponent<Image>();
            if (on)
            {
                row.Unlit = img.color;
                img.color = row.Unlit * Color.Lerp(Color.white, KodeWindow.ThemeOf(Button.gameObject).Accent, 0.6f);
            }
            else img.color = row.Unlit;
        }

        // a new filter: the virtual list shows the model's rows from the top (or around the selected entry, for a cleared filter)
        void RowsChanged()
        {
            if (virtualRows == null || !Model.IsOpen) return;
            // as tall as the rows shown, up to the room on screen: the popup shrinks with the matches
            float height = RowsHeight();
            if (Math.Abs(height - shownHeight) > 0.5f)
            {
                virtualRows.View.PreferredHeight(height);
                shownHeight = height;
                LayoutRebuilder.ForceRebuildLayoutImmediate(list);
                PlaceUnderButton();
            }
            virtualRows.SetCount(Model.RowCount);
            virtualRows.Rebind();
            if (Model.Highlight > 0) virtualRows.ScrollTo(Model.Highlight, ScrollAlign.Center);
            else virtualRows.SetOffset(0);
        }

        // the room for the rows under the filter field, and the height the list has now
        float listRoom, shownHeight;

        float RowsHeight()
        {
            float all = 0;
            for (int r = 0; r < Model.RowCount; r++) all += (Model.IsHeader(r) ? headerHeight : entryHeight) + 1;
            // at least one row: the "no match" note
            return Mathf.Max(entryHeight + 4, Mathf.Min(all + 2, listRoom));
        }

        // ---- the keyboard, while the list is open ----

        /// <summary>A key the list's scope routed to it (Escape closes the scope, and the list with it: <see cref="Close"/>).</summary>
        void Key(ScopeKeys k)
        {
            if (!IsOpen || !Model.IsOpen || k == ScopeKeys.Escape) return;
            if (filterField != null && (k & (ScopeKeys.Up | ScopeKeys.Down | ScopeKeys.PageUp | ScopeKeys.PageDown)) != 0) caretToEnd = true;
            Model.Handle(k, KodeWindow.Source(ChangeSource.Keyboard));
        }

        /// <summary>A letter: the typeahead (<see cref="TypeaheadModel"/>), or the filter field's when the list is typeable.</summary>
        void Typeahead(char c)
        {
            if (!IsOpen || !Model.IsOpen) return;
            if (filterField == null)
            {
                Model.Type(c, Time.unscaledTime);
                return;
            }
            // the field types its own letters; one typed while it hasn't the keyboard (the frame it opens, a click elsewhere) goes in too
            if (filterField.isFocused) return;
            filterField.text += c;
            KeepFilterFocus();
        }

        // the model's highlight, painted and kept in view, and reported (a key moved it; not the one the list opens with)
        void ShowHighlight()
        {
            if (!opening && Model.IsOpen && Model.HighlightedItem >= 0) EntryHovered?.Invoke(Model.HighlightedItem, KodeWindow.Source(ChangeSource.Keyboard));
            if (virtualRows != null)
            {
                VirtualListModel vm = virtualRows.Model;
                for (int r = vm.RenderFirst; r <= vm.RenderLast && r < vm.Count; r++)
                {
                    Row row = virtualRows.RowAt(r);
                    if (row != null && !Model.IsHeader(r)) Light(row, r == Model.Highlight);
                }
                if (Model.Highlight >= 0) virtualRows.ScrollTo(Model.Highlight, ScrollAlign.Nearest, KodeWindow.Source(ChangeSource.Keyboard));
                return;
            }
            Highlight(Model.Highlight);
        }

        void Highlight(int i)
        {
            if (rows == null || i < 0 || i >= rows.Length || i == highlight) return;
            if (highlight >= 0 && rows[highlight] != null) rows[highlight].GetComponent<Image>().color = highlightWas;
            highlight = i;
            var img = rows[i].GetComponent<Image>();
            highlightWas = img.color;
            img.color = highlightWas * Color.Lerp(Color.white, KodeWindow.ThemeOf(Button.gameObject).Accent, 0.6f);
            if (Scroll == null) return;
            // keep the highlighted row in view
            var row = (RectTransform)rows[i].transform;
            RectTransform c = Scroll.content;
            float viewport = Scroll.viewport.rect.height;
            float top = -(row.localPosition.y + row.rect.yMax);
            float y = c.anchoredPosition.y;
            if (top < y) y = top;
            else if (top + row.rect.height > y + viewport) y = top + row.rect.height - viewport;
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, Mathf.Clamp(y, 0, Mathf.Max(0, c.rect.height - viewport)));
        }

        /// <summary>
        /// The tallest list that fits on screen, in canvas units: the room below the button or above it, whichever is larger
        /// (PlaceUnderButton flips the list above when it does not fit below).
        /// </summary>
        float MaxListHeight()
        {
            var parent = (RectTransform)list.parent;
            return Headless.PopupPlacer.MaxHeight(Popups.BoxIn((RectTransform)Button.transform, parent), Popups.Box(parent.rect), 8, 200);
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

        /// <summary>
        /// Places the open list by its button (<see cref="Headless.PopupPlacer.Place"/>: below, left edges lined up, flipped above when
        /// there is no room below, clamped on the screen) and gives its layer the box.
        /// </summary>
        public void PlaceUnderButton()
        {
            if (list == null) return;
            var parent = (RectTransform)list.parent;
            Headless.FrameBox anchor = Popups.BoxIn((RectTransform)Button.transform, parent);
            Vector2 size = list.rect.size;
            Headless.PopupPlacement p = Headless.PopupPlacer.Place(anchor, size.x, size.y, Popups.Box(parent.rect), list.localScale.x);
            Placement = p;
            Popups.Put(list, p.Box);
            Layer?.Place(p.Box);
        }

        // the list's layer closes (Escape, the owner, a scene change), or the list closes without one (not open: only the teardown)
        void Dismiss(Headless.PopupDismiss reason)
        {
            if (Layer != null && Layer.IsOpen) Layer.Dismiss(reason);
            else Close();
        }

        /// <summary>Closes the list without a change.</summary>
        public void Close()
        {
            // the model's Closed tears the popup down; with the model closed already, it is done here
            if (Model.IsOpen) Model.Close();
            else Teardown();
        }

        void Teardown()
        {
            keys.UnlockAfterKeys();
            // closed by its host (a pick, the button, another list opening): its layer goes too
            Headless.PopupLayer layer = Layer;
            Layer = null;
            if (layer != null && layer.IsOpen) layer.Dismiss(Headless.PopupDismiss.Program);
            rows = null;
            highlight = -1;
            pointerRow = null;
            pointerItem = -1;
            if (popup != null) UnityEngine.Object.Destroy(popup);
            popup = null;
            popupEntries = null;
            list = null;
            Blocker = null;
            Scroll = null;
            filterField = null;
            virtualRows = null;
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
