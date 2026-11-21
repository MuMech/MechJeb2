using System;
using System.Collections.Generic;
using KodeKit.Headless;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// The command palette (<see cref="PaletteModel"/>) as drawn: a window on its own overlay canvas, near the top of the screen, over a
    /// full-screen blocker, with a search field, a fixed number of result rows and a status line. Its host gives it the commands.
    /// <list type="bullet">
    /// <item>While it is open it owns the keyboard through its <see cref="KeyboardScope"/>: KSP's keyboard lock is held (released once
    /// Escape and Enter are up), so a letter typed there doesn't fly the vessel, Space doesn't stage and Escape doesn't pause. The
    /// scope routes Up, Down, Page Up, Page Down, Enter and Escape to the model; the field types the text.</item>
    /// <item>A click on a row is an Enter on it; a second click on an armed command runs it only half a second or more after the first
    /// (<see cref="PaletteModel.ClickConfirmDelay"/>), so a double-click arms and runs nothing. A click outside closes it (the popup
    /// stack's outside press), and a scene change too.</item>
    /// <item>It is a layer of the popup stack, so the keyboard shortcuts are paused while it is open.</item>
    /// <item>Nothing runs per frame while it is closed: its driver is enabled only while it is open. Open, it reads which commands can
    /// run twice a second (<see cref="PaletteModel.Refresh"/>) and rebinds its rows only when the model says something changed.</item>
    /// </list>
    /// </summary>
    public static class CommandPalette
    {
        /// <summary>The palette's behaviour: its commands, query, results, highlight, armed command. Public for tests and the agent bridge.</summary>
        public static readonly PaletteModel Model = new PaletteModel();

        /// <summary>The result rows shown at once.</summary>
        public const int MaxRows = 10;

        /// <summary>The palette's scale (MechJeb's UI scale), set by the host.</summary>
        public static float Scale = 1;

        /// <summary>The open palette's window (null: closed).</summary>
        public static KodeWindow Window { get; private set; }

        /// <summary>The open palette's search field (null: closed).</summary>
        public static TMP_InputField Field { get; private set; }

        /// <summary>The open palette's result rows (MaxRows of them; the unused ones hidden). Row i shows result <see cref="FirstRow"/> + i.</summary>
        public static IReadOnlyList<UIButton> Rows => rows;

        /// <summary>The result shown by the first row.</summary>
        public static int FirstRow { get; private set; }

        /// <summary>The status line (null: closed).</summary>
        public static TMP_Text StatusText { get; private set; }

        /// <summary>The open palette's layer on the popup stack (null: closed).</summary>
        public static PopupLayer Layer { get; private set; }

        /// <summary>The full-screen blocker behind the window (null: closed).</summary>
        public static GameObject Blocker { get; private set; }

        public static bool IsOpen => Model.IsOpen;

        /// <summary>How many palettes opened since the game started (for tests).</summary>
        public static int OpenCount { get; private set; }

        static readonly List<UIButton> rows = new List<UIButton>();
        static readonly List<TMP_Text> labels = new List<TMP_Text>();
        static readonly List<TMP_Text> tags = new List<TMP_Text>();
        static readonly List<CanvasGroup> dims = new List<CanvasGroup>();
        static GameObject overlay;
        static GameObject host;
        static Driver driver;
        static KeyboardScope keys;
        static bool hooked;
        static float shownScale;
        static float placedWidth = -1;

        /// <summary>Opens the palette on <paramref name="commands"/> (closing one already open), its field focused.</summary>
        public static void Open(IEnumerable<PaletteCommand> commands)
        {
            Hook();
            if (Model.IsOpen) Model.Close(PaletteClose.Program);
            Build();
            keys.Lock();
            Layer = Popups.Push(host);
            PopupLayer layer = Layer;
            Layer.Dismissed += d =>
            {
                if (Layer == layer) Model.Close(PaletteModel.FromDismiss(d));
            };
            Model.Label = Localizer.Format("#MechJeb_NewUI_Palette");
            Model.QueryLabel = Localizer.Format("#MechJeb_NewUI_PaletteSearch");
            Model.ResultsLabel = Localizer.Format("#MechJeb_NewUI_PaletteCommands");
            Model.Open(commands);
            OpenCount++;
            driver.enabled = true;
            nextRefresh = Time.unscaledTime + RefreshEvery;
            LayoutRebuilder.ForceRebuildLayoutImmediate(Window.Root.rectTransform);
            Place();
            EventSystem events = EventSystem.current;
            if (events != null) events.SetSelectedGameObject(Field.gameObject);
            Field.ActivateInputField();
        }

        /// <summary>Closes the open palette, running nothing (its host's close).</summary>
        public static void Close() => Model.Close(PaletteClose.Program);

        /// <summary>Types <paramref name="text"/> as the query, as the field does (for tests and the agent bridge).</summary>
        public static void Type(string text)
        {
            if (!Model.IsOpen) return;
            if (Field != null) Field.text = text ?? "";
            else Model.SetQuery(text);
        }

        static void Hook()
        {
            if (hooked) return;
            hooked = true;
            host = new GameObject("KodeKit.CommandPalette");
            UnityEngine.Object.DontDestroyOnLoad(host);
            driver = host.AddComponent<Driver>();
            driver.enabled = false;
            keys = host.AddComponent<KeyboardScope>();
            KeyScope scope = keys.Configure(ScopeKeys.Navigation & ~(ScopeKeys.Home | ScopeKeys.End) | ScopeKeys.Closing | ScopeKeys.Text, true);
            scope.Key += Key;
            scope.Typed += Typed;
            // Escape closes the scope, and the palette's layer with it: nothing runs, an armed command included
            scope.Dismissed += () =>
            {
                if (Layer != null && Layer.IsOpen) Layer.Dismiss(PopupDismiss.Escape);
                else Model.Close(PaletteClose.Escape);
            };
            Model.Clock = () => Time.unscaledTime;
            Model.ResultsChanged += BindRows;
            Model.HighlightChanged += BindRows;
            Model.StatusChanged += BindStatus;
            Model.Closed += _ => Teardown();
        }

        // ---- the popup ----

        static void Build()
        {
            Canvas root = Overlay.Root;
            var canvasRt = (RectTransform)root.transform;
            int layer = root.gameObject.layer; // UIMainCamera culls the other layers
            overlay = new GameObject("KodeKit.CommandPaletteOverlay", typeof(RectTransform)) { layer = layer };
            var rt = (RectTransform)overlay.transform;
            rt.SetParent(canvasRt, false);
            Stretch(rt);
            Overlay.Setup(overlay.AddComponent<Canvas>(), root, Overlay.PaletteOrder);
            overlay.AddComponent<GraphicRaycaster>();

            // a click outside the window closes the palette and runs nothing; the scene under it gets no click
            Blocker = new GameObject("Blocker", typeof(RectTransform)) { layer = layer };
            var brt = (RectTransform)Blocker.transform;
            brt.SetParent(rt, false);
            Stretch(brt);
            Blocker.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            KodeWindow.NoSelect(Blocker.AddComponent<Button>()).transition = Selectable.Transition.None;
            Blocker.AddComponent<Popups.OutsidePressCatcher>();

            var w = new KodeWindow(rt, "#MechJeb_NewUI_Palette", 460, Vector2.zero);
            Window = w;
            w.Root.name = "CommandPalette";
            w.TitleRow.GetComponent<DragHandle>().Enabled = () => false;
            w.BodyDrag.Enabled = () => false;
            // no close button and no title menu: Escape and a click outside are the ways out
            w.CloseButton.gameObject.SetActive(false);
            w.TitleMenu.enabled = false;
            Theme theme = KodeWindow.ThemeOf(w.Root.gameObject);

            Field = MakeField(w.Root, theme);
            Field.onValueChanged.AddListener(s => Model.SetQuery(s));

            Layout list = w.Root.Add<Layout>("Results");
            list.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(0);
            list.FlexibleLayout(true, false);
            list.Finish();
            rows.Clear();
            labels.Clear();
            tags.Clear();
            dims.Clear();
            for (int i = 0; i < MaxRows; i++)
            {
                int k = i;
                UIButton b = KodeWindow.ListRow(list, "", () => Model.Pick(FirstRow + k, KodeWindow.Pointer));
                b.gameObject.name = "PaletteRow" + i;
                // never the EventSystem's selected object: the field keeps the keyboard, uGUI's arrows can't move to a row (rule 54)
                b.GetComponent<Button>().navigation = new Navigation { mode = Navigation.Mode.None };
                TMP_Text label = b.GetComponentInChildren<TMP_Text>();
                label.raycastTarget = false;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.GetComponent<UIObject>()?.FlexibleLayout(true, false);
                UIText tag = KodeWindow.Label(b.Content, "");
                tag.tmpText.alignment = TextAlignmentOptions.Right;
                tag.tmpText.raycastTarget = false;
                KodeWindow.ToneText(tag, KodeWindow.Tone.Dim);
                rows.Add(b);
                labels.Add(label);
                tags.Add(tag.tmpText);
                dims.Add(b.gameObject.AddComponent<CanvasGroup>());
                b.gameObject.SetActive(false);
            }

            UIText status = KodeWindow.Paragraph(w.Root, " ");
            status.name = "PaletteStatus";
            StatusText = status.tmpText;

            shownScale = Scale > 0 ? Scale : 1;
            w.Behaviour.Scale = shownScale;
            w.Behaviour.Restyle();
            FirstRow = 0;
            placedWidth = -1;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static TMP_InputField MakeField(UIObject parent, Theme theme)
        {
            UIInputField f = parent.Add<UIInputField>("PaletteField");
            f.Finish();
            f.PreferredSize(-1, theme.RowHeight + 4);
            f.FlexibleLayout(true, false);
            Image bg = f.GetComponent<Image>();
            TMP_InputField input = f.GetComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.selectionColor = new Color(0.3f, 0.55f, 1f, 0.55f);
            input.caretWidth = 2;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onFocusSelectAll = false; // the focus taken back (after an Enter that armed a command) keeps the query
            var area = (RectTransform)f.transform.Find("Text Area");
            area.offsetMin = new Vector2(4, 1);
            area.offsetMax = new Vector2(-4, -1);
            var hint = (TextMeshProUGUI)input.placeholder;
            TMP_Text[] texts = f.GetComponentsInChildren<TextMeshProUGUI>(true);
            KodeWindow.Styled(f.gameObject, th =>
            {
                th.Field.ApplySelectable(input, bg);
                foreach (TMP_Text t in texts) th.ApplyText(t, th.Field.TextColor);
                hint.text = KodeWindow.Loc("#MechJeb_NewUI_PaletteFieldHint");
                hint.fontStyle = FontStyles.Italic;
                Color c = th.Field.TextColor;
                hint.color = new Color(c.r, c.g, c.b, 0.45f);
            });
            return input;
        }

        // near the top of the screen, centred across; placed again when the window's width or the scale changes
        static void Place()
        {
            if (Window == null) return;
            RectTransform rt = Window.Root.rectTransform;
            var parent = (RectTransform)rt.parent;
            float width = rt.rect.width;
            placedWidth = width;
            Rect r = parent.rect;
            float s = Window.Behaviour.Scale;
            float left = Mathf.Round(r.center.x - width * s / 2);
            float top = Mathf.Round(r.yMax - r.height * 0.15f);
            Popups.Put(rt, new FrameBox(left, top, left + width * s, top - rt.rect.height * s));
            Layer?.Place(new FrameBox(left, top, left + width * s, top - rt.rect.height * s));
        }

        // ---- the model, drawn ----

        static void BindRows()
        {
            if (Window == null || !Model.IsOpen) return;
            int count = Model.ResultCount, h = Model.Highlight;
            // the highlight in view: the rows scroll by as little as they must
            if (h >= 0)
            {
                if (h < FirstRow) FirstRow = h;
                else if (h >= FirstRow + MaxRows) FirstRow = h - MaxRows + 1;
            }
            if (FirstRow > Math.Max(0, count - MaxRows)) FirstRow = Math.Max(0, count - MaxRows);
            for (int i = 0; i < rows.Count; i++)
            {
                int r = FirstRow + i;
                UIButton row = rows[i];
                bool shown = r < count;
                if (row.gameObject.activeSelf != shown) row.gameObject.SetActive(shown);
                if (!shown) continue;
                PaletteCommand c = Model.ResultAt(r);
                labels[i].text = Model.ResultLabel(r); // read once per list: a command's own Label asks its owner (and allocates) each time
                string tag = c.Tag ?? "";
                tags[i].text = tag;
                dims[i].alpha = Model.ResultAvailable(r) ? 1 : 0.45f;
                KodeWindow.SetSelected(row, r == h);
            }
        }

        static void BindStatus()
        {
            if (StatusText == null || !Model.IsOpen) return;
            Theme th = KodeWindow.ThemeOf(StatusText.gameObject);
            PaletteCommand c = Model.StatusCommand;
            string text;
            KodeWindow.Tone tone;
            switch (Model.Status)
            {
                case PaletteStatus.Confirm:
                    text = Localizer.Format("#MechJeb_NewUI_PaletteConfirm", c?.Preview ?? c?.Label);
                    tone = KodeWindow.Tone.Warn;
                    break;
                case PaletteStatus.Unavailable:
                    string why = c?.Reason;
                    text = Localizer.Format("#MechJeb_NewUI_PaletteUnavailable", c?.Label, string.IsNullOrEmpty(why) ? Localizer.Format("#MechJeb_NewUI_PaletteCantRun") : why);
                    tone = KodeWindow.Tone.Bad;
                    break;
                case PaletteStatus.NoMatch:
                    text = Localizer.Format("#MechJeb_NewUI_PaletteNoMatch");
                    tone = KodeWindow.Tone.Dim;
                    break;
                default:
                    text = Localizer.Format("#MechJeb_NewUI_PaletteKeys");
                    tone = KodeWindow.Tone.Dim;
                    break;
            }
            StatusText.text = text;
            StatusText.color = th.ToneColor(tone, th.Text);
        }

        // ---- the keyboard ----

        static bool caretToEnd;

        static void Key(ScopeKeys k)
        {
            if (!Model.IsOpen) return;
            if ((k & (ScopeKeys.Up | ScopeKeys.Down | ScopeKeys.PageUp | ScopeKeys.PageDown)) != 0) caretToEnd = true;
            Model.Handle(k, KodeWindow.Source(ChangeSource.Keyboard));
        }

        // a letter typed while the field hasn't the keyboard (after a click on a row) goes in too; the scopes give none in the frame
        // it opened, so the chord that opened it doesn't type into it
        static void Typed(char c)
        {
            if (!Model.IsOpen || Field == null || Field.isFocused) return;
            Field.text += c;
            KeepFieldFocus();
        }

        /// <summary>The field keeps the keyboard while the palette is open (an Enter that armed a command ends TMP's edit).</summary>
        static void KeepFieldFocus()
        {
            if (Field == null || Field.isFocused || !keys.Scope.OnTop) return;
            EventSystem events = EventSystem.current;
            if (events == null || events.alreadySelecting) return;
            GameObject selected = events.currentSelectedGameObject;
            if (selected != null && selected != Field.gameObject && !selected.transform.IsChildOf(overlay.transform)) return;
            if (selected != Field.gameObject) events.SetSelectedGameObject(Field.gameObject);
            Field.ActivateInputField();
            caretToEnd = true;
        }

        static void CaretToEnd()
        {
            if (!caretToEnd || Field == null || !Field.isFocused) return;
            caretToEnd = false;
            int end = Field.text.Length;
            Field.caretPosition = end;
            Field.selectionAnchorPosition = Field.selectionFocusPosition = end;
            Field.ForceLabelUpdate();
        }

        static void Teardown()
        {
            if (keys != null) keys.UnlockAfterKeys();
            PopupLayer layer = Layer;
            Layer = null;
            if (layer != null && layer.IsOpen) layer.Dismiss(PopupDismiss.Program);
            if (overlay != null) UnityEngine.Object.Destroy(overlay);
            overlay = null;
            Window = null;
            Field = null;
            StatusText = null;
            Blocker = null;
            rows.Clear();
            labels.Clear();
            tags.Clear();
            dims.Clear();
            if (driver != null) driver.enabled = false;
        }

        const float RefreshEvery = 0.5f;
        static float nextRefresh;

        /// <summary>Runs only while the palette is open: the window's syncs, the field's focus, the scale, a refresh twice a second.</summary>
        sealed class Driver : MonoBehaviour
        {
            void Update()
            {
                if (!Model.IsOpen || Window == null)
                {
                    enabled = false;
                    return;
                }
                Window.Sync();
                KeepFieldFocus();
                float s = Scale > 0 ? Scale : 1;
                if (s != shownScale)
                {
                    shownScale = s;
                    Window.Behaviour.Scale = s;
                    placedWidth = -1;
                }
                if (Time.unscaledTime >= nextRefresh)
                {
                    nextRefresh = Time.unscaledTime + RefreshEvery;
                    Model.Refresh();
                }
                if (Window != null && Window.Root.rectTransform.rect.width != placedWidth) Place();
            }

            // TMP moves the caret on Up (to the start) and Down (to the end) in its own update
            void LateUpdate() => CaretToEnd();
        }
    }
}
