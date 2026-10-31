using System;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>How a one-of-many choice is drawn: a joined bar, underlined tabs, or pills that wrap.</summary>
    public enum ChoiceStyle { Joined, Tabs, Pills }

    /// <summary>The eight places around the centre of a <see cref="KodeWindow.Compass"/>, and the centre.</summary>
    public enum Dir { NW, N, NE, W, C, E, SW, S, SE }

    /// <summary>
    /// The modern layout's widgets and layouts (LAYOUTS.md): switches, segmented choices, steppers, chips, badges,
    /// section headers, status bars, action bars, uniform grids, the compass, flow wrap, property grids, tabs and
    /// master-detail. Views use them only when <see cref="ModernLayout"/> is on; the IMGUI-faithful widgets stay the
    /// default. Like the other widgets, all of them follow the theme and make no garbage once built.
    /// </summary>
    public partial class KodeWindow
    {
        /// <summary>Views build the modern layout instead of IMGUI's (set before the windows are built; a change rebuilds them).</summary>
        public static bool ModernLayout;

        /// <summary>The section headers the modern layout adds (read when a window is built).</summary>
        public static bool ShowHeaders = true;

        Theme Th => Behaviour.Theme ?? T;

        /// <summary>The theme a widget is drawn with: its window's (a custom window's own Compact setting) or Theme.Current.</summary>
        public static Theme ThemeOf(GameObject go) => go.GetComponentInParent<WindowBehaviour>()?.Theme ?? T;

        /// <summary>A bare child object on the UI layer (KodeUI's own objects are UIObjects; these are plain graphics).</summary>
        static GameObject Child(Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            foreach (Type t in components) go.AddComponent(t);
            return go;
        }

        /// <summary>
        /// A selectable drawn by hand: its sprite and colour, then a colour tint per state over them. Used where a widget
        /// switches looks (selected or not): sprite swap would leave the old tint on the renderer.
        /// </summary>
        static void Paint(Selectable s, Image img, Sprite sprite, Color color, Color normal, Color hover, Color press)
        {
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            s.targetGraphic = img;
            s.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.normalColor = cb.selectedColor = normal;
            cb.highlightedColor = hover;
            cb.pressedColor = press;
            cb.fadeDuration = 0.05f;
            s.colors = cb;
        }

        static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        static readonly Color Clear = new Color(1, 1, 1, 0), HoverWash = new Color(1, 1, 1, 0.1f), PressWash = new Color(1, 1, 1, 0.18f);

        // tints at rest, so hover can brighten to white (a tint above 1 does nothing)
        static readonly Color Dimmed = new Color(0.8f, 0.8f, 0.8f, 1), Pressed = new Color(0.7f, 0.7f, 0.7f, 1);

        // ---- headers and rules ----

        /// <summary>
        /// A section header: small, upper case, in the theme's header colour. Hidden when <see cref="ShowHeaders"/> is off
        /// (the setting "Section headers"), unless <paramref name="always"/>.
        /// </summary>
        public static UIText Header(UIObject parent, string text, bool always = false)
        {
            // labels that become headers often end in a colon ("Mode:"); a header doesn't need it
            UIText t = Label(parent, text, true);
            string Trimmed() => Loc(text)?.TrimEnd(':', ' ', '\u00A0');
            t.tmpText.text = Trimmed();
            if (text != null && text.Length > 1 && text[0] == '#') t.GetComponentInParent<WindowBehaviour>()?.AddRelocalize(() => t.tmpText.text = Trimmed());
            t.tmpText.margin = new Vector4(0, 5, 0, 1);
            Styled(t.gameObject, th =>
            {
                th.ApplyText(t.tmpText, th.Header);
                t.tmpText.fontSize = Mathf.Round(th.FontSize * th.HeaderScale);
                t.tmpText.fontStyle = th.HeaderUpper ? FontStyles.Bold | FontStyles.UpperCase : FontStyles.Bold;
            });
            t.gameObject.SetActive(always || ShowHeaders);
            return t;
        }

        /// <summary>A hairline across the parent, in the theme's rule colour.</summary>
        public static Image Rule(UIObject parent)
        {
            GameObject go = Child(parent.transform, "Rule", typeof(Image), typeof(LayoutElement));
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 1;
            le.flexibleWidth = 1;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            Styled(go, th => img.color = th.Rule);
            return img;
        }

        // ---- buttons with a role ----

        /// <summary>A button whose look says what it does: the main action, a destructive one, or a selected choice.</summary>
        public static UIButton Button(UIObject parent, string text, Action onClick, ButtonRole role, bool flexible = false) =>
            Role(Button(parent, text, onClick, flexible), role);

        /// <summary>Gives a button a fixed role (primary, danger) in the modern layout; the classic layout keeps IMGUI's plain buttons.</summary>
        public static UIButton Role(UIButton b, ButtonRole role)
        {
            if (ModernLayout && role != ButtonRole.Normal) Styled(b.gameObject, th => PaintRole(th, b, role));
            return b;
        }

        static void PaintRole(Theme th, UIButton b, ButtonRole role)
        {
            ElementStyle st = th.Role(role);
            var img = b.GetComponent<Image>();
            Paint(b.GetComponent<Button>(), img, st.Normal, st.Tint, Color.white, new Color(0.92f, 0.92f, 0.92f), new Color(0.78f, 0.78f, 0.78f));
            foreach (TMP_Text label in b.GetComponentsInChildren<TMP_Text>(true)) th.ApplyText(label, st.TextColor);
            foreach (Transform c in b.Content.transform)
                if (c.name == "Icon") c.GetComponent<Image>().color = st.TextColor;
        }

        /// <summary>A button whose role follows a state (a state button that turns "selected" while its mode is on).</summary>
        public void BindRole(UIButton b, Func<ButtonRole> role)
        {
            ButtonRole shown = role();
            Styled(b.gameObject, th => PaintRole(th, b, shown));
            syncs.Add(() =>
            {
                ButtonRole r = role();
                if (r == shown) return;
                shown = r;
                PaintRole(Th, b, r);
            });
        }

        // ---- switch ----

        /// <summary>An on/off switch: a pill track in the accent colour when on, with a knob that moves across.</summary>
        public Toggle Switch(UIObject parent, Func<bool> get, Action<bool> set)
        {
            GameObject go = Child(parent.transform, "Switch", typeof(Image), typeof(Toggle), typeof(LayoutElement));
            var le = go.GetComponent<LayoutElement>();
            var track = go.GetComponent<Image>();
            var toggle = go.GetComponent<Toggle>();
            GameObject knobGo = Child(go.transform, "Knob", typeof(Image));
            var knob = knobGo.GetComponent<Image>();
            knob.sprite = Sprites.Circle;
            knob.raycastTarget = false;
            var krt = (RectTransform)knobGo.transform;
            toggle.graphic = null;
            toggle.SetIsOnWithoutNotify(get());

            void Look(Theme th)
            {
                bool on = toggle.isOn;
                Paint(toggle, track, Sprites.Pill, on ? th.Accent : Alpha(th.Dim, 0.35f), Dimmed, Color.white, Pressed);
                knob.color = on ? Color.white : Alpha(th.Text, 0.9f);
                krt.anchorMin = krt.anchorMax = krt.pivot = new Vector2(on ? 1 : 0, 0.5f);
                krt.anchoredPosition = new Vector2(on ? -2 : 2, 0);
            }

            set = Edits(set);
            toggle.onValueChanged.AddListener(v =>
            {
                set(v);
                Look(Th);
            });
            Styled(go, th =>
            {
                float h = Mathf.Round(th.FontSize + 1);
                le.minWidth = le.preferredWidth = Mathf.Round(h * 1.8f);
                le.minHeight = le.preferredHeight = h;
                krt.sizeDelta = new Vector2(h - 4, h - 4);
                Look(th);
            });
            syncs.Add(() =>
            {
                bool v = get();
                if (toggle.isOn == v) return;
                toggle.SetIsOnWithoutNotify(v);
                Look(Th);
            });
            return toggle;
        }

        /// <summary>
        /// A switch with three positions: a pill track with a knob at the left, middle or right, and the position's name beside it.
        /// A click on a third of the track moves the knob there. The right position is "on", in the accent colour.
        /// </summary>
        public Button Switch3(UIObject parent, string[] names, Func<int> get, Action<int> set)
        {
            GameObject go = Child(parent.transform, "Switch3", typeof(Image), typeof(Button), typeof(LayoutElement), typeof(ThirdsClick));
            var le = go.GetComponent<LayoutElement>();
            var track = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            GameObject knobGo = Child(go.transform, "Knob", typeof(Image));
            var knob = knobGo.GetComponent<Image>();
            knob.sprite = Sprites.Circle;
            knob.raycastTarget = false;
            var krt = (RectTransform)knobGo.transform;
            int shown = get();

            void Look(Theme th)
            {
                bool on = shown == 2;
                Paint(button, track, Sprites.Pill, on ? th.Accent : Alpha(th.Dim, 0.35f), Dimmed, Color.white, Pressed);
                knob.color = on ? Color.white : Alpha(th.Text, 0.9f);
                krt.anchorMin = krt.anchorMax = krt.pivot = new Vector2(shown * 0.5f, 0.5f);
                krt.anchoredPosition = new Vector2(shown == 0 ? 2 : on ? -2 : 0, 0);
            }

            set = Edits(set);
            go.GetComponent<ThirdsClick>().Clicked = i =>
            {
                if (i == shown) return;
                shown = i;
                set(i);
                Look(Th);
            };
            Styled(go, th =>
            {
                float h = Mathf.Round(th.FontSize + 1);
                le.minWidth = le.preferredWidth = Mathf.Round(h * 2.7f);
                le.minHeight = le.preferredHeight = h;
                krt.sizeDelta = new Vector2(h - 4, h - 4);
                Look(th);
            });
            syncs.Add(() =>
            {
                int v = get();
                if (v == shown) return;
                shown = v;
                Look(Th);
            });
            BoundLabel(parent, () => names[shown]);
            return button;
        }

        /// <summary>Tells which third of a control a click landed on, from the left.</summary>
        sealed class ThirdsClick : MonoBehaviour, IPointerClickHandler
        {
            public Action<int> Clicked;

            public void OnPointerClick(PointerEventData e)
            {
                var rt = (RectTransform)transform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 p))
                    Clicked?.Invoke(Mathf.Clamp(Mathf.FloorToInt(3 * (p.x / rt.rect.width + rt.pivot.x)), 0, 2));
            }
        }

        /// <summary>A switch on the left and its label (a click on the label flips it too).</summary>
        public Toggle SwitchRow(UIObject parent, string text, Func<bool> get, Action<bool> set) => SwitchToggle(parent, text, get, set, null);

        /// <summary>
        /// The modern layout's <see cref="Toggle(UIObject, string, Func{bool}, Action{bool}, Func{bool})"/>: a switch and its
        /// label, green while <paramref name="green"/> holds. The label flips it through ToggleLabelClick, as the checkbox's
        /// does, so code that looks up a toggle's label works with both.
        /// </summary>
        Toggle SwitchToggle(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<bool> green)
        {
            Layout row = Row(parent);
            Toggle t = Switch(row, get, set);
            UIText label = Label(row, text, true);
            LabelFlips(label, t);
            if (green != null) ToneText(label, () => green() ? Tone.Good : Tone.Normal);
            return t;
        }

        static void LabelFlips(UIText label, Toggle t)
        {
            label.tmpText.raycastTarget = true;
            label.gameObject.AddComponent<ToggleLabelClick>().Toggle = t;
        }

        // ---- folding groups ----

        /// <summary>
        /// A folding group the modern layout adds to long windows: a header with a chevron that shows or hides the panel it
        /// returns, remembered per window and title for the session. With the "Section headers" setting off there is no
        /// header, and the panel always shows.
        /// </summary>
        public Layout Group(UIObject parent, string title, bool open = true) =>
            ShowHeaders ? FoldSection(parent, title, open, Root.name + "/" + title, false) : Panel(parent);

        /// <summary>The modern look of <see cref="Section"/> and <see cref="Group"/>: a chevron (down: open, right: folded) and a header.</summary>
        Layout FoldSection(UIObject parent, string title, bool open, string key, bool lazy)
        {
            if (sectionState.TryGetValue(key, out bool saved)) open = saved;
            Layout header = Row(parent);
            GameObject chevGo = Child(header.transform, "Chevron", typeof(Image), typeof(LayoutElement));
            var chevron = chevGo.GetComponent<Image>();
            chevron.sprite = Sprites.ChevronDown;
            var cle = chevGo.GetComponent<LayoutElement>();
            UIText label = Header(header, title, true);
            Layout content = Panel(parent);
            content.gameObject.SetActive(open || !lazy);

            void Look(Theme th)
            {
                cle.minWidth = cle.preferredWidth = cle.minHeight = cle.preferredHeight = Mathf.Round(th.FontSize * 0.75f);
                chevron.color = th.Header;
                chevGo.transform.localRotation = Quaternion.Euler(0, 0, content.gameObject.activeSelf ? 0 : 90);
            }

            void Flip()
            {
                bool o = !content.gameObject.activeSelf;
                content.gameObject.SetActive(o);
                sectionState[key] = o;
                Look(Th);
            }

            Styled(chevGo, Look);
            if (!open && !lazy) FoldInFirstSync(content, () => Look(Th));
            label.tmpText.raycastTarget = true;
            label.gameObject.AddComponent<ClickHandler>().Click = Flip;
            chevGo.AddComponent<ClickHandler>().Click = Flip;
            return content;
        }

        // ---- segmented choices ----

        /// <summary>
        /// One choice out of several, drawn as one control: a joined bar, underlined tabs, or pills that wrap to the
        /// window's width. MechJeb's rule: <see cref="ChoiceStyle.Tabs"/> for navigation (a choice that only changes what
        /// the window shows, like SmartASS's mode or Waypoint Help's topics), <see cref="ChoiceStyle.Joined"/> for the
        /// rest (modes, settings, values).
        /// </summary>
        public Layout Segmented(UIObject parent, string[] items, Func<int> get, Action<int> set, ChoiceStyle style = ChoiceStyle.Joined)
        {
            ChoiceStyle s = style;
            Layout box;
            UIObject cells;
            if (s == ChoiceStyle.Joined)
            {
                LayoutPanel bar = parent.Add<LayoutPanel>("Segmented");
                var grid = bar.gameObject.AddComponent<UniformGridLayout>();
                grid.Columns = items.Length;
                grid.Spacing = 1;
                grid.padding = new RectOffset(1, 1, 1, 1);
                bar.FlexibleLayout(true, false);
                bar.Finish();
                Styled(bar.gameObject, th => th.Field.ApplyImage(bar.BackGround));
                box = bar;
                cells = bar;
            }
            else if (s == ChoiceStyle.Tabs)
            {
                box = Panel(parent);
                box.Spacing(0);
                Layout row = Row(box);
                row.Spacing(14);
                Rule(box);
                cells = row;
            }
            else
            {
                box = FlowWrap(parent, 4);
                cells = box;
            }

            set = Edits(set);
            var buttons = new UIButton[items.Length];
            var bars = new Image[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                int k = i;
                UIButton b = Button(cells, items[i], () => set(k));
                if (s == ChoiceStyle.Pills) b.Content.Padding(9, 9, 1, 1);
                if (s == ChoiceStyle.Tabs)
                {
                    b.Content.Padding(1, 1, 2, 4);
                    bars[i] = Overlay(b, "Underline", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2));
                }
                // a choice needs no flash: its new fill already says it happened
                UnityEngine.Object.Destroy(b.GetComponent<ClickFlash>());
                buttons[i] = b;
            }

            int shown = get();
            void Look(Theme th)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    bool on = i == shown;
                    var btn = buttons[i].GetComponent<Button>();
                    var img = buttons[i].GetComponent<Image>();
                    TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>();
                    switch (s)
                    {
                        case ChoiceStyle.Joined:
                            ElementStyle sel = th.Role(ButtonRole.Selected);
                            if (on) Paint(btn, img, sel.Normal, sel.Tint, Color.white, new Color(0.92f, 0.92f, 0.92f), new Color(0.8f, 0.8f, 0.8f));
                            else Paint(btn, img, null, Color.white, Clear, HoverWash, PressWash);
                            th.ApplyText(label, on ? sel.TextColor : th.Text);
                            break;
                        case ChoiceStyle.Tabs:
                            Paint(btn, img, null, Color.white, Clear, Clear, Clear);
                            th.ApplyText(label, on ? th.Text : th.Dim);
                            label.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                            bars[i].color = th.Accent;
                            bars[i].gameObject.SetActive(on);
                            break;
                        default:
                            if (on) Paint(btn, img, Sprites.Pill, th.Accent, Color.white, new Color(0.92f, 0.92f, 0.92f), new Color(0.8f, 0.8f, 0.8f));
                            else Paint(btn, img, Sprites.PillOutline, Alpha(th.Dim, 0.8f), Dimmed, Color.white, Color.white);
                            th.ApplyText(label, on ? th.AccentText : th.Text);
                            break;
                    }
                }
            }

            Styled(box.gameObject, Look);
            syncs.Add(() =>
            {
                int v = get();
                if (v == shown) return;
                shown = v;
                Look(Th);
            });
            return box;
        }

        // ---- stepper ----

        /// <summary>
        /// A value with its nudges as one control: [−][value][+]. − and + call <paramref name="add"/> with ∓step()
        /// (×10 with Shift held); with <paramref name="repeat"/> they repeat while held. The step is a separate choice, made
        /// once (a <see cref="Segmented"/> "Step" row).
        /// </summary>
        public Layout Stepper(UIObject parent, Func<string> text, Action<string> setText, Action<double> add, Func<double> step, float width,
            Func<bool> valid = null, bool repeat = false)
        {
            LayoutPanel box = parent.Add<LayoutPanel>("Stepper");
            box.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, true).Spacing(0).Padding(1);
            box.Finish();

            add = Edits(add);
            double Step() => step() * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 10 : 1);
            StepButton(box, "-", () => add(-Step()), repeat);
            TMP_InputField field = EditFields.Field(this, box, text, setText, width, valid);
            field.textComponent.alignment = TextAlignmentOptions.Center;
            StepButton(box, "+", () => add(Step()), repeat);

            Image fieldBg = field.GetComponent<Image>();
            Styled(box.gameObject, th =>
            {
                th.Field.ApplyImage(box.BackGround);
                // the field draws on the stepper's background
                Paint(field, fieldBg, null, Color.white, Clear, HoverWash, Clear);
            });
            return box;
        }

        // a flat − or + at least a row high and wide: the 15 px "-" buttons of IMGUI are too small to hit
        static UIButton StepButton(UIObject parent, string text, Action onClick, bool repeat)
        {
            UIButton b = repeat ? RepeatButton(parent, text, onClick) : Button(parent, text, onClick);
            b.Content.Padding(4, 4, 0, 0);
            var le = b.GetComponent<LayoutElement>();
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            Styled(b.gameObject, th =>
            {
                Paint(b.GetComponent<Button>(), b.GetComponent<Image>(), null, Color.white, Clear, HoverWash, PressWash);
                th.ApplyText(label, th.Text);
                label.fontStyle = FontStyles.Bold;
                le.minWidth = th.RowHeight;
            });
            return b;
        }

        // ---- chips and badges ----

        /// <summary>
        /// An on/off chip: a pill with an optional colour dot and a label, filled when on, outlined and dim when off
        /// (recorder series, Delta-V options). Put several in a <see cref="FlowWrap"/>.
        /// </summary>
        public UIButton Chip(UIObject parent, string text, Func<bool> get, Action<bool> set, Color? dot = null)
        {
            set = Edits(set);
            UIButton b = Button(parent, text, () => set(!get()));
            b.Content.Padding(dot.HasValue ? 6 : 9, 9, 1, 1);
            b.Content.Spacing(4);
            UnityEngine.Object.Destroy(b.GetComponent<ClickFlash>());
            Image dotImg = null;
            if (dot.HasValue)
            {
                dotImg = AddIcon(b, Sprites.Circle, true);
                dotImg.name = "Dot";
            }
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            var btn = b.GetComponent<Button>();
            var img = b.GetComponent<Image>();
            bool shown = get();

            void Look(Theme th)
            {
                if (shown) Paint(btn, img, Sprites.Pill, Alpha(th.Text, 0.2f), Dimmed, Color.white, Color.white);
                else Paint(btn, img, Sprites.PillOutline, Alpha(th.Dim, 0.55f), Dimmed, Color.white, Color.white);
                th.ApplyText(label, shown ? th.Text : th.Dim);
                if (dotImg == null) return;
                dotImg.color = Alpha(dot.Value, shown ? 1 : 0.35f);
                var le = dotImg.GetComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = Mathf.Round(th.FontSize * 0.6f);
            }

            Styled(b.gameObject, Look);
            syncs.Add(() =>
            {
                bool v = get();
                if (v == shown) return;
                shown = v;
                Look(Th);
            });
            return b;
        }

        /// <summary>
        /// A text coloured by a state (MechJeb's green, yellow and red labels). Normal is the plain text colour, or a
        /// button's text colour with <paramref name="button"/>. Call it after the widget is built: its restyle runs later and wins.
        /// </summary>
        public void ToneText(TMP_Text t, Func<Tone> tone, bool button = false)
        {
            Tone shown = tone();
            void Look(Theme th) => t.color = th.ToneColor(shown, button ? th.Button.TextColor : th.Text);
            Styled(t.gameObject, Look);
            syncs.Add(() =>
            {
                Tone v = tone();
                if (v == shown) return;
                shown = v;
                Look(Th);
            });
        }

        public void ToneText(UIText t, Func<Tone> tone) => ToneText(t.tmpText, tone);

        /// <summary>A text in a fixed state colour (warnings, dim notes): restyled with the theme, no per-frame binding.</summary>
        public static void ToneText(TMP_Text t, Tone tone) => Styled(t.gameObject, th => t.color = th.ToneColor(tone, th.Text));

        public static void ToneText(UIText t, Tone tone) => ToneText(t.tmpText, tone);

        /// <summary>A small upper-case pill of text in a tone's colour ("LIMITING", "ON"). Show it with <see cref="Visible"/>.</summary>
        public LayoutPanel Badge(UIObject parent, string text, Tone tone)
        {
            LayoutPanel p = parent.Add<LayoutPanel>("Badge");
            p.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, false).Padding(6, 6, 0, 0);
            p.Finish();
            UIText t = Label(p, text);
            Styled(p.gameObject, th =>
            {
                Color c = th.ToneColor(tone, th.Accent);
                p.BackGround.sprite = Sprites.Pill;
                p.BackGround.type = Image.Type.Sliced;
                p.BackGround.color = Alpha(c, 0.2f);
                p.BackGround.enabled = true;
                th.ApplyText(t.tmpText, c);
                t.tmpText.fontSize = Mathf.Round(th.FontSize * 0.8f);
                t.tmpText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                t.GetComponent<LayoutElement>().minHeight = 0;
            });
            return p;
        }

        // ---- status bar ----

        /// <summary>
        /// A strip for state, across the window: a coloured edge on the left, a tint of the same colour behind, and the
        /// rows added to it. At the bottom (added last) it says who is in control; at the top it lists what is wrong.
        /// The tone is read every frame.
        /// </summary>
        public Layout StatusBar(UIObject parent, Func<Tone> tone)
        {
            LayoutPanel p = parent.Add<LayoutPanel>("StatusBar");
            p.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(1).Padding(9, 6, 3, 3);
            p.FlexibleLayout(true, false);
            p.Finish();
            Image edge = Overlay(p, "Edge", new Vector2(0, 0), new Vector2(0, 1), new Vector2(3, 0));
            Tone shown = tone();

            void Look(Theme th)
            {
                Color c = th.ToneColor(shown, th.Accent);
                p.BackGround.sprite = Sprites.Rounded;
                p.BackGround.type = Image.Type.Sliced;
                p.BackGround.color = Alpha(c, 0.14f);
                p.BackGround.enabled = true;
                edge.color = c;
            }

            Styled(p.gameObject, Look);
            syncs.Add(() =>
            {
                Tone t = tone();
                if (t == shown) return;
                shown = t;
                Look(Th);
            });
            return p;
        }

        // ---- action bar ----

        /// <summary>The bottom of a window: destructive actions on the left, the others on the right, the primary last.</summary>
        public class ActionBarRow
        {
            public readonly Layout Left, Right;

            internal ActionBarRow(Layout left, Layout right)
            {
                Left = left;
                Right = right;
            }

            public UIButton Danger(string text, Action onClick) => Button(Left, text, onClick, ButtonRole.Danger);
            public UIButton Normal(string text, Action onClick) => Button(Right, text, onClick, ButtonRole.Normal);
            public UIButton Primary(string text, Action onClick) => Button(Right, text, onClick, ButtonRole.Primary);
        }

        public static ActionBarRow ActionBar(UIObject parent)
        {
            Rule(parent);
            Layout row = Row(parent);
            Layout left = Row(row), right = Row(row);
            left.FlexibleLayout(false, false);
            FlexibleSpace(row);
            right.transform.SetAsLastSibling();
            right.FlexibleLayout(false, false);
            return new ActionBarRow(left, right);
        }

        // ---- grids ----

        /// <summary>Rows of <paramref name="columns"/> cells, all the same size (<see cref="UniformGridLayout"/>).</summary>
        public static Layout UniformGrid(UIObject parent, int columns, float spacing = 2)
        {
            Layout g = parent.Add<Layout>("UniformGrid");
            var lg = g.gameObject.AddComponent<UniformGridLayout>();
            lg.Columns = columns;
            lg.Spacing = spacing;
            g.FlexibleLayout(true, false);
            g.Finish();
            return g;
        }

        /// <summary>Children left to right, wrapping at the window's width (<see cref="FlowLayout"/>).</summary>
        public static Layout FlowWrap(UIObject parent, float spacing = 4)
        {
            Layout g = parent.Add<Layout>("FlowWrap");
            var lg = g.gameObject.AddComponent<FlowLayout>();
            lg.Spacing = spacing;
            lg.LineSpacing = spacing;
            g.FlexibleLayout(true, false);
            g.Finish();
            return g;
        }

        /// <summary>A 3×3 grid of slots around a centre; put a widget in a slot, leave the others empty.</summary>
        public class CompassGrid
        {
            readonly Layout[] slots;
            public readonly Layout Grid;

            internal CompassGrid(Layout grid, Layout[] slots)
            {
                Grid = grid;
                this.slots = slots;
            }

            public Layout this[Dir d] => slots[(int)d];
        }

        /// <summary>
        /// Up to eight cells around a centre, all the same size (the SmartASS navball cross, the landing nudge pad).
        /// <paramref name="cell"/>: the smallest cell size (0: the widgets' own).
        /// </summary>
        public static CompassGrid Compass(UIObject parent, float cell = 0)
        {
            Layout grid = UniformGrid(parent, 3);
            var slots = new Layout[9];
            for (int i = 0; i < 9; i++)
            {
                Layout slot = grid.Add<Layout>("Slot" + (Dir)i);
                slot.Vertical().ControlChildSize(true, true).ChildForceExpand(true, true);
                slot.Finish();
                if (cell > 0)
                {
                    var le = slot.gameObject.AddComponent<LayoutElement>();
                    le.minWidth = le.minHeight = cell;
                }
                slots[i] = slot;
            }
            return new CompassGrid(grid, slots);
        }

        // ---- property grid ----

        /// <summary>
        /// Option rows in shared columns, [switch] label [field] unit, so the fields of a block line up (Utilities,
        /// Ascent Settings). The label column takes the extra width. A row whose switch is off dims its field and unit.
        /// </summary>
        public class PropertyGrid
        {
            readonly KodeWindow w;
            public readonly Layout Table;

            internal PropertyGrid(KodeWindow w, Layout table)
            {
                this.w = w;
                Table = table;
            }

            /// <summary>The column that takes the extra width: 1, the label, for [switch] label [field] unit rows; 0 for rows without a switch.</summary>
            public int FlexibleColumn
            {
                get => Table.GetComponent<TableLayout>().FlexibleColumn;
                set => Table.GetComponent<TableLayout>().FlexibleColumn = value;
            }

            /// <summary>An empty row: add its cells in column order (switch, label, field, unit).</summary>
            public Layout Row()
            {
                Layout row = Table.Add<Layout>("GridRow");
                row.gameObject.AddComponent<TableRow>();
                row.Finish();
                return row;
            }

            static void Empty(UIObject row) => row.Add<UIEmpty>("Empty").Finish();

            /// <summary>
            /// [switch] label [field] unit; without a field, [switch] label. <paramref name="tone"/> colours the label (a
            /// limiter that is acting now).
            /// </summary>
            public Layout Switch(string label, Func<bool> get, Action<bool> set, Func<string> text = null, Action<string> setText = null,
                float width = 60, string unit = null, Func<bool> valid = null, Func<Tone> tone = null)
            {
                Layout row = Row();
                Toggle t = w.Switch(row, get, set);
                UIText l = Label(row, label);
                LabelFlips(l, t);
                if (tone != null) w.ToneText(l, tone);
                if (text == null) return row;
                TMP_InputField f = EditFields.Field(w, row, text, setText, width, valid);
                UIText u = Label(row, unit ?? "");
                Dim(f.gameObject, u.gameObject, () => !get());
                return row;
            }

            /// <summary>A row of one cell across the grid: a sub-heading, a note, a button.</summary>
            public UIText Note(string text)
            {
                Layout row = Row();
                UIText t = Label(row, text);
                Styled(t.gameObject, th => th.ApplyText(t.tmpText, th.Dim));
                return t;
            }

            /// <summary>label [field] unit, no switch.</summary>
            public Layout Field(string label, Func<string> text, Action<string> setText, float width = 60, string unit = null, Func<bool> valid = null)
            {
                Layout row = Row();
                Empty(row);
                Label(row, label);
                EditFields.Field(w, row, text, setText, width, valid);
                Label(row, unit ?? "");
                return row;
            }

            /// <summary>label value: a read-out in the label and field columns.</summary>
            public Layout Value(string label, Func<string> value)
            {
                Layout row = Row();
                Empty(row);
                Label(row, label);
                w.BoundLabel(row, value).tmpText.alignment = TextAlignmentOptions.Right;
                return row;
            }

            /// <summary>Fade some cells while a condition holds (the field of a switched-off option).</summary>
            public void Dim(GameObject a, GameObject b, Func<bool> dim)
            {
                CanvasGroup ga = a.GetComponent<CanvasGroup>() ?? a.AddComponent<CanvasGroup>();
                CanvasGroup gb = b == null ? null : b.GetComponent<CanvasGroup>() ?? b.AddComponent<CanvasGroup>();
                bool shown = !dim();
                void Apply()
                {
                    ga.alpha = shown ? 1 : 0.4f;
                    if (gb != null) gb.alpha = ga.alpha;
                }
                Apply();
                w.AddSync(() =>
                {
                    bool v = !dim();
                    if (v == shown) return;
                    shown = v;
                    Apply();
                });
            }
        }

        public PropertyGrid Properties(UIObject parent)
        {
            Layout table = parent.Add<Layout>("PropertyGrid");
            var lg = table.gameObject.AddComponent<TableLayout>();
            lg.FlexibleColumn = 1;
            lg.ColumnSpacing = 6;
            table.FlexibleLayout(true, false);
            table.Finish();
            Styled(table.gameObject, th => lg.RowSpacing = Mathf.Max(1, th.Spacing));
            return new PropertyGrid(this, table);
        }

        // ---- tabs and master-detail ----

        /// <summary>
        /// One window, several pages: underlined tabs over one panel per page, only the selected one shown. Returns the
        /// pages. Without get/set the window keeps the selection itself.
        /// </summary>
        public Layout[] Tabs(UIObject parent, string[] titles, Func<int> get = null, Action<int> set = null)
        {
            int current = 0;
            get ??= () => current;
            set ??= i => current = i;
            Segmented(parent, titles, get, set, ChoiceStyle.Tabs);
            var pages = new Layout[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                int k = i;
                pages[i] = Panel(parent);
                Visible(pages[i].gameObject, () => get() == k);
            }
            return pages;
        }

        /// <summary>
        /// A list on the left, the selected item's details on the right (the planner's operations). Consecutive items
        /// with the same group are listed under its header. Returns the details panel.
        /// </summary>
        public Layout MasterDetail(UIObject parent, float listWidth, float height, int count, Func<int, string> name, Func<int, string> group,
            Func<int> get, Action<int> set)
        {
            Layout row = Row(parent);
            row.ChildAlignment(TextAnchor.UpperLeft);
            ScrollView sv = ScrollList(row, height, out Layout list);
            var sle = sv.GetComponent<LayoutElement>();
            sle.flexibleWidth = 0;
            sle.minWidth = sle.preferredWidth = listWidth;

            var rows = new UIButton[count];
            string lastGroup = null;
            for (int i = 0; i < count; i++)
            {
                string g = group?.Invoke(i);
                if (g != null && g != lastGroup)
                {
                    UIText h = Header(list, g, true);
                    h.tmpText.margin = new Vector4(6, 5, 0, 1);
                }
                lastGroup = g;
                int k = i;
                rows[i] = ListRow(list, name(i), () => set(k));
            }
            int shown = -1;
            void Mark()
            {
                for (int i = 0; i < rows.Length; i++) SetSelected(rows[i], i == shown);
            }
            syncs.Add(() =>
            {
                int v = get();
                if (v == shown) return;
                shown = v;
                Mark();
            });

            Layout detail = Panel(row);
            detail.FlexibleLayout(true, false);
            return detail;
        }

        // ---- title bar ----

        /// <summary>
        /// A flat icon button in the title bar, before the close button (the modern layout's edit and copy). The tooltip
        /// is a text or a '#key'.
        /// </summary>
        public UIButton TitleIcon(Sprite icon, string tooltip, Action onClick)
        {
            UIButton b = IconButton(TitleRow, icon, onClick);
            FlatIcon(b, false);
            b.transform.SetSiblingIndex(CloseButton.transform.GetSiblingIndex());
            if (!string.IsNullOrEmpty(tooltip)) Tooltips.Add(b.gameObject, Loc(tooltip));
            return b;
        }

        /// <summary>A borderless square title-bar button showing an icon or a short text ("?"); the close button turns red on hover.</summary>
        static void FlatIcon(UIButton b, bool close)
        {
            UnityEngine.Object.Destroy(b.GetComponent<ClickFlash>());
            b.Content.Padding(2);
            var le = b.gameObject.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
            Transform iconT = b.Content.transform.Find("Icon");
            Image icon = iconT != null ? iconT.GetComponent<Image>() : null;
            TMP_Text text = icon == null ? b.GetComponentInChildren<TMP_Text>() : null;
            Styled(b.gameObject, th =>
            {
                float size = th.FontSize + 3;
                le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = size;
                Color hover = close ? new Color(0.85f, 0.25f, 0.2f, 0.85f) : Alpha(th.Text, 0.15f);
                Paint(b.GetComponent<Button>(), b.GetComponent<Image>(), Sprites.Rounded, Color.white,
                    Clear, hover, hover);
                if (icon != null)
                {
                    icon.color = Alpha(th.Text, 0.85f);
                    var ile = icon.GetComponent<LayoutElement>();
                    ile.minWidth = ile.preferredWidth = ile.minHeight = ile.preferredHeight = Mathf.Round(th.FontSize * 0.75f);
                }
                else if (text != null)
                {
                    th.ApplyText(text, Alpha(th.Text, 0.85f));
                    text.fontStyle = FontStyles.Bold;
                }
            });
        }
    }
}
