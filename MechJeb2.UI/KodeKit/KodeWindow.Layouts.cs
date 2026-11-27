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
    /// The layout widgets (LAYOUTS.md): switches, segmented choices, steppers, chips, badges, section headers, status
    /// bars, meters, action bars, uniform grids, the compass, flow wrap, property grids, tabs and master-detail. Like the other
    /// widgets, all of them follow the theme and make no garbage once built.
    /// </summary>
    public partial class KodeWindow
    {
        /// <summary>The section headers the views add (read when a window is built).</summary>
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
        /// A selectable painted from the skin's rules as <paramref name="part"/> (<see cref="Headless.StyleParts"/>): its sprite, fill
        /// and state tints, and the colour of <paramref name="texts"/> and <paramref name="icons"/> (<see cref="StyledPart"/>). The
        /// widget sets the part's On flag from its model; the pointer, the selection and <c>interactable</c> set the others.
        /// </summary>
        static StyledPart Part(Selectable s, Image img, string part, TMP_Text[] texts = null, Image[] icons = null) =>
            StyledPart.Add(s.gameObject, part, img, s, texts, icons);

        static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

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

        /// <summary>Gives a button a fixed role (primary, danger).</summary>
        public static UIButton Role(UIButton b, ButtonRole role)
        {
            if (role != ButtonRole.Normal) RolePart(b, role);
            return b;
        }

        static string RolePartName(ButtonRole role) =>
            role == ButtonRole.Primary ? Headless.StyleParts.ButtonPrimary : role == ButtonRole.Danger ? Headless.StyleParts.ButtonDanger
            : role == ButtonRole.Selected ? Headless.StyleParts.ButtonSelected : Headless.StyleParts.ButtonNormal;

        // the skin's button by role, its labels and the icons it has now in the role's text colour
        static StyledPart RolePart(UIButton b, ButtonRole role)
        {
            var icons = new System.Collections.Generic.List<Image>();
            foreach (Transform c in b.Content.transform)
                if (c.name == "Icon")
                    icons.Add(c.GetComponent<Image>());
            return Part(b.GetComponent<Button>(), b.GetComponent<Image>(), RolePartName(role), b.GetComponentsInChildren<TMP_Text>(true), icons.ToArray());
        }

        /// <summary>A button whose role follows a state (a state button that turns "selected" while its mode is on).</summary>
        public void BindRole(UIButton b, Func<ButtonRole> role)
        {
            ButtonRole shown = role();
            StyledPart part = RolePart(b, shown);
            syncs.Add(() =>
            {
                ButtonRole r = role();
                if (r == shown) return;
                shown = r;
                part.Part = RolePartName(r);
            });
        }

        // ---- switch ----

        /// <summary>
        /// An on/off switch: a pill track in the accent colour when on, with a knob that moves across. <paramref name="label"/> names it
        /// (a '#key' or a text; null: the label right before it in its row, <see cref="LabelBefore"/>).
        /// </summary>
        public Toggle Switch(UIObject parent, Func<bool> get, Action<bool> set, string label = null)
        {
            label = label ?? LabelBefore(parent);
            GameObject go = Child(parent.transform, "Switch", typeof(Image), typeof(Toggle), typeof(LayoutElement));
            var le = go.GetComponent<LayoutElement>();
            var track = go.GetComponent<Image>();
            var toggle = NoSelect(go.GetComponent<Toggle>());
            GameObject knobGo = Child(go.transform, "Knob", typeof(Image));
            var knob = knobGo.GetComponent<Image>();
            knob.sprite = Sprites.Circle;
            knob.raycastTarget = false;
            var krt = (RectTransform)knobGo.transform;
            toggle.graphic = null;

            Headless.CommitTarget<bool> target = Target("Switch", get, set);
            target.Control = toggle;
            // the behaviour: a click (uGUI's toggle) and an agent's flip both set the model, which calls the setter once
            var model = new Headless.ToggleModel(Headless.ToggleKind.Switch, get, target.Set);
            target.Model = model;
            LabelTarget(target, label);
            toggle.SetIsOnWithoutNotify(model.On);
            StyledPart trackPart = Part(toggle, track, Headless.StyleParts.Switch);
            trackPart.Follower(knobGo, Headless.StyleParts.SwitchKnob, knob);

            void Look(Theme th)
            {
                bool on = model.On;
                trackPart.Set(Headless.StyleFlags.On, on);
                krt.anchorMin = krt.anchorMax = krt.pivot = new Vector2(on ? 1 : 0, 0.5f);
                krt.anchoredPosition = new Vector2(on ? -2 : 2, 0);
            }

            // what the model holds now: a refused or kept value puts back the toggle uGUI moved
            void Show()
            {
                if (toggle.isOn != model.On) toggle.SetIsOnWithoutNotify(model.On);
                Look(Th);
            }
            toggle.onValueChanged.AddListener(v =>
            {
                // drawn again after every set: a refused set, or a setter that keeps the old value (the model's Shown doesn't change
                // then, so no ShownChanged), puts back the control uGUI moved
                model.Set(v, Pointer);
                Show();
            });
            model.Value.ShownChanged += Show;
            Styled(go, th =>
            {
                float h = Mathf.Round(th.FontSize + 1);
                le.minWidth = le.preferredWidth = Mathf.Round(h * 1.8f);
                le.minHeight = le.preferredHeight = h;
                krt.sizeDelta = new Vector2(h - 4, h - 4);
                Look(th);
            });
            syncs.Add(() => model.Sync());
            return toggle;
        }

        /// <summary>
        /// A switch with three positions: a pill track with a knob at the left, middle or right, and the position's name beside it.
        /// A click on a third of the track moves the knob there. The right position is "on", in the accent colour.
        /// </summary>
        /// <param name="label">Its name (a '#key' or a text; null: the label right before it in its row).</param>
        public Button Switch3(UIObject parent, string[] names, Func<int> get, Action<int> set, string label = null)
        {
            label = label ?? LabelBefore(parent);
            GameObject go = Child(parent.transform, "Switch3", typeof(Image), typeof(Button), typeof(LayoutElement), typeof(ThirdsClick));
            var le = go.GetComponent<LayoutElement>();
            var track = go.GetComponent<Image>();
            var button = NoSelect(go.GetComponent<Button>());
            GameObject knobGo = Child(go.transform, "Knob", typeof(Image));
            var knob = knobGo.GetComponent<Image>();
            knob.sprite = Sprites.Circle;
            knob.raycastTarget = false;
            var krt = (RectTransform)knobGo.transform;
            Headless.CommitTarget<int> target = Target("Switch3", get, set);
            target.Control = button;
            // the behaviour: a click on a third and an agent's pick both go through the model (the knob's own third does nothing)
            var model = new Headless.Switch3Model(names, get, target.Set);
            target.Model = model;
            LabelTarget(target, label);
            StyledPart trackPart = Part(button, track, Headless.StyleParts.Switch);
            trackPart.Follower(knobGo, Headless.StyleParts.SwitchKnob, knob);

            void Look(Theme th)
            {
                int shown = model.Shown;
                bool on = shown == 2;
                trackPart.Set(Headless.StyleFlags.On, on);
                krt.anchorMin = krt.anchorMax = krt.pivot = new Vector2(shown * 0.5f, 0.5f);
                krt.anchoredPosition = new Vector2(shown == 0 ? 2 : on ? -2 : 0, 0);
            }

            go.GetComponent<ThirdsClick>().Clicked = i => model.Pick(i, Pointer);
            model.Value.ShownChanged += () => Look(Th);
            Styled(go, th =>
            {
                float h = Mathf.Round(th.FontSize + 1);
                le.minWidth = le.preferredWidth = Mathf.Round(h * 2.7f);
                le.minHeight = le.preferredHeight = h;
                krt.sizeDelta = new Vector2(h - 4, h - 4);
                Look(th);
            });
            syncs.Add(() => model.Sync());
            BoundLabel(parent, () => model.ShownName);
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
        /// <see cref="Toggle(UIObject, string, Func{bool}, Action{bool}, Func{bool})"/>: a switch and its label, green while
        /// <paramref name="green"/> holds. The label flips it through ToggleLabelClick, as the checkbox's does, so code that
        /// looks up a toggle's label works with both.
        /// </summary>
        Toggle SwitchToggle(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<bool> green)
        {
            Layout row = Row(parent);
            Toggle t = Switch(row, get, set);
            UIText label = Label(row, text, true);
            LabelFlips(label, t);
            Labelled(t, text);
            if (green != null) ToneText(label, () => green() ? Tone.Good : Tone.Normal);
            return t;
        }

        // the label of the control's commit target, its id from the label, for an observer that names controls, and the label of
        // the model that draws it (as shown), for its description
        void Labelled(object control, string label) => LabelTarget(Commits.Find(control), label);

        /// <summary>
        /// Names <paramref name="control"/> (a KodeKit widget on this window's commit path) for its description and an observer, as a
        /// labelled row does: its target's label and id, and its model's label. For a widget that has no label of its own (a
        /// drop-down or a segmented bar after a text). Returns the control.
        /// </summary>
        public T Named<T>(T control, string label) where T : class
        {
            Labelled(control, label);
            return control;
        }

        static void LabelFlips(UIText label, Toggle t)
        {
            label.tmpText.raycastTarget = true;
            label.gameObject.AddComponent<ToggleLabelClick>().Toggle = t;
        }

        // ---- folding groups ----

        /// <summary>
        /// A folding group the views add to long windows: a header with a chevron that shows or hides the panel it
        /// returns, remembered per window and title for the session. With the "Section headers" setting off there is no
        /// header, and the panel always shows.
        /// </summary>
        public Layout Group(UIObject parent, string title, bool open = true) =>
            ShowHeaders ? FoldSection(parent, title, open, Root.name + "/" + title, false) : Panel(parent);

        /// <summary>The look of <see cref="Section"/> and <see cref="Group"/>: a chevron (down: open, right: folded) and a header.</summary>
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
                ReportFold(header, title, o);
            }

            Styled(chevGo, Look);
            // for a host that folds sections (the agent bridge): view state, no commit
            Disclosure d = header.gameObject.AddComponent<Disclosure>();
            d.Title = title;
            d.Content = content.gameObject;
            d.Flip = Flip;
            if (!open && !lazy) FoldInFirstSync(content, () => Look(Th));
            label.tmpText.raycastTarget = true;
            label.gameObject.AddComponent<ClickHandler>().Click = Flip;
            chevGo.AddComponent<ClickHandler>().Click = Flip;
            return content;
        }

        /// <summary>A folding header's handle for a host: its title, whether it is open, and the click that flips it.</summary>
        public sealed class Disclosure : MonoBehaviour
        {
            /// <summary>The header's text or '#key'.</summary>
            public string Title;

            /// <summary>The panel it shows and hides.</summary>
            public GameObject Content;

            /// <summary>Folds or unfolds, as a click on the header.</summary>
            public Action Flip;

            public bool Open => Content != null && Content.activeSelf;
        }

        // ---- segmented choices ----

        /// <summary>
        /// One choice out of several, drawn as one control: a joined bar, underlined tabs, or pills that wrap to the
        /// window's width. MechJeb's rule: <see cref="ChoiceStyle.Tabs"/> for navigation (a choice that only changes what
        /// the window shows, like SmartASS's mode or Waypoint Help's topics), <see cref="ChoiceStyle.Joined"/> for the
        /// rest (modes, settings, values).
        /// </summary>
        /// <param name="label">The choice's name (a '#key' or a text; null: the label right before it in its row, <see cref="LabelBefore"/>).</param>
        public Layout Segmented(UIObject parent, string[] items, Func<int> get, Action<int> set, ChoiceStyle style = ChoiceStyle.Joined, string label = null) =>
            Segmented(parent, items, get, set, style, out _, label);

        /// <summary>A <see cref="Segmented"/> that also hands out the model it draws (Tabs shows its pages from it).</summary>
        public Layout Segmented(UIObject parent, string[] items, Func<int> get, Action<int> set, ChoiceStyle style, out Headless.ChoiceModel model,
            string label = null)
        {
            string rowLabel = label ?? LabelBefore(parent);
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

            // the behaviour (what is selected, what a click does) is Headless.ChoiceModel, tested without KSP; this draws it
            // every pick is a commit on the window's path, with the source the model records
            Headless.CommitTarget<int> target = Target("Segmented", get, set);
            target.Control = box;
            var choice = new Headless.ChoiceModel(items.Length, get, target.Set)
            {
                ItemLabels = new string[items.Length], ItemRole = s == ChoiceStyle.Tabs ? Headless.PartRole.Tab : Headless.PartRole.Radio
            };
            model = choice;
            void ItemLabels()
            {
                for (int i = 0; i < items.Length; i++) choice.ItemLabels[i] = ShownLabel(items[i]) ?? "";
            }
            ItemLabels();
            target.Model = choice;
            LabelTarget(target, rowLabel);
            // the items follow a language switch, like their buttons
            Behaviour.AddRelocalize(ItemLabels);
            var parts = new StyledPart[items.Length];
            var bars = new Image[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                int k = i;
                UIButton b = Button(cells, items[i], () => choice.Select(k, Pointer));
                if (s == ChoiceStyle.Pills) b.Content.Padding(9, 9, 1, 1);
                if (s == ChoiceStyle.Tabs)
                {
                    b.Content.Padding(1, 1, 2, 4);
                    bars[i] = Overlay(b, "Underline", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2));
                }
                // a choice needs no flash: its new fill already says it happened
                UnityEngine.Object.Destroy(b.GetComponent<ClickFlash>());
                string part = s == ChoiceStyle.Joined ? Headless.StyleParts.Segment : s == ChoiceStyle.Tabs ? Headless.StyleParts.Tab : Headless.StyleParts.Pill;
                parts[i] = Part(b.GetComponent<Button>(), b.GetComponent<Image>(), part, new[] { b.GetComponentInChildren<TMP_Text>() });
                if (bars[i] != null) parts[i].Follower(bars[i].gameObject, Headless.StyleParts.TabLine, bars[i]);
            }

            // the chosen cell is On: the skin's rules have its look (the selected role, the accent pill, the bold tab and its underline)
            void Look()
            {
                for (int i = 0; i < parts.Length; i++) parts[i].Set(Headless.StyleFlags.On, choice.IsOn(i));
            }

            Look();
            // a pick (which syncs the model itself) and a change from outside both repaint, at once
            choice.ShownChanged += Look;
            syncs.Add(() => choice.Sync());
            return box;
        }

        // ---- stepper ----

        /// <summary>
        /// A value with its nudges as one control: [−][value][+]. − and + call <paramref name="add"/> with ∓step()
        /// (×10 with Right Shift or Left Alt held: Left Shift is KSP's throttle up, AltGr sends Left Ctrl, its throttle down); with <paramref name="repeat"/> they repeat while held. The step is a separate choice, made
        /// once (a <see cref="Segmented"/> "Step" row). <paramref name="label"/> names it (a '#key' or a text; null: the label right before
        /// it in its row, <see cref="LabelBefore"/>).
        /// </summary>
        public Layout Stepper(UIObject parent, Func<string> text, Action<string> setText, Action<double> add, Func<double> step, float width,
            Func<bool> valid = null, bool repeat = false, string label = null)
        {
            string rowLabel = label ?? LabelBefore(parent);
            LayoutPanel box = parent.Add<LayoutPanel>("Stepper");
            box.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, true).Spacing(0).Padding(1);
            box.Finish();

            // the behaviour (what a nudge or a key adds) is Headless.StepperModel, tested without KSP; this draws it
            // one commit target for the nudges and the typing: its value is the text, a nudge's asked value the delta
            Headless.CommitTarget<string> target = Target("Stepper", text, setText);
            target.Control = box;
            var model = new Headless.StepperModel(text, target.Set, (d, source) => target.Apply(d, add, source), step, valid);
            target.Model = model;
            LabelTarget(target, rowLabel);
            StepButton(box, "-", () => model.Press(-1, Modifiers(), Pointer), repeat, target);
            // its field is a text field written as typed, through the stepper's rules (Allow, the source)
            Headless.TextFieldModel fieldModel = null;
            TMP_InputField field = EditFields.Field(this, box, target, width, FieldKind.Centred, (begin, end) => fieldModel =
                new Headless.TextFieldModel(text, (s, source) => model.Type(s, source), Headless.TextCommit.AsTyped, null, valid, null, null, begin, end));
            // the field is the stepper's: it has the stepper's name
            LabelPart(fieldModel, target);
            // a number centred between - and + (FieldKind.Centred), in the middle of the stepper's box, which is taller than the text
            EditFields.SetAlignment(field.textComponent, TextAlignmentOptions.Center);
            EditFields.SetAlignment((TMP_Text)field.placeholder, TextAlignmentOptions.Center);
            StepButton(box, "+", () => model.Press(1, Modifiers(), Pointer), repeat, target);
            // while the field has the keyboard (and KodeKit's keyboard lock: these keys don't reach KSP), Up/Down nudge
            field.gameObject.AddComponent<StepperKeys>().Init(model, field, text);

            Image fieldBg = field.GetComponent<Image>();
            Styled(box.gameObject, th => th.Field.ApplyImage(box.BackGround));
            // the field draws on the stepper's background
            Part(field, fieldBg, Headless.StyleParts.StepperField);
            return box;
        }

        // the modifiers held; Headless.StepperModel decides which of them make a ×10 step on a button or a key. Either Alt counts,
        // but Left Alt is the one to name: on Windows, AltGr (the right Alt of AZERTY or QWERTZ layouts) also sends Left Ctrl,
        // KSP's throttle down, which a pointer over a window leaves live
        static Headless.StepperModifiers Modifiers()
        {
            Headless.StepperModifiers m = Headless.StepperModifiers.None;
            if (Input.GetKey(KeyCode.LeftShift)) m |= Headless.StepperModifiers.LeftShift;
            if (Input.GetKey(KeyCode.RightShift)) m |= Headless.StepperModifiers.RightShift;
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)) m |= Headless.StepperModifiers.Alt;
            return m;
        }

        /// <summary>
        /// Up/Down (×10 with either Shift or Alt: the field's keyboard lock keeps Left Shift from KSP's throttle) and PageUp/PageDown
        /// (×10) on a stepper's focused field, routed by the field's keyboard scope (nothing runs per frame). The field shows the new
        /// value at once: a focused field isn't refreshed by its sync.
        /// </summary>
        sealed class StepperKeys : MonoBehaviour
        {
            Headless.StepperModel model;
            TMP_InputField field;
            FieldText shown;
            Func<string> text;

            /// <summary>The model the keys nudge (for tests).</summary>
            public Headless.StepperModel Model => model;

            public void Init(Headless.StepperModel m, TMP_InputField f, Func<string> t)
            {
                model = m;
                field = f;
                shown = f.GetComponent<FieldText>();
                text = t;
                f.GetComponent<FieldKeyboardLock>().Route(Headless.ScopeKeys.Up | Headless.ScopeKeys.Down | Headless.ScopeKeys.PageUp | Headless.ScopeKeys.PageDown, Key);
            }

            void Key(Headless.ScopeKeys k)
            {
                if (this == null || field == null || !field.isFocused) return;
                bool handled = k == Headless.ScopeKeys.Up ? model.Key(Headless.StepperKey.Up, Modifiers())
                    : k == Headless.ScopeKeys.Down ? model.Key(Headless.StepperKey.Down, Modifiers())
                    : k == Headless.ScopeKeys.PageUp ? model.Key(Headless.StepperKey.PageUp, false)
                    : k == Headless.ScopeKeys.PageDown && model.Key(Headless.StepperKey.PageDown, false);
                if (!handled) return;
                // the nudge set the model: the field shows its text without typing it again (one setter call per key)
                string s = text();
                shown.Show(s);
                field.caretPosition = s.Length;
            }
        }

        // a flat − or + at least a row high and wide: the 15 px "-" buttons of IMGUI are too small to hit
        static UIButton StepButton(UIObject parent, string text, Action onClick, bool repeat, Headless.CommitTarget target)
        {
            UIButton b = repeat ? RepeatButton(parent, text, onClick) : Button(parent, text, onClick);
            // a hold is one commit
            if (repeat) b.GetComponent<RepeatTrigger>().Group = target;
            b.Content.Padding(4, 4, 0, 0);
            var le = b.GetComponent<LayoutElement>();
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            Styled(b.gameObject, th => le.minWidth = th.RowHeight);
            Part(b.GetComponent<Button>(), b.GetComponent<Image>(), Headless.StyleParts.StepperButton, new[] { label });
            return b;
        }

        // ---- chips and badges ----

        /// <summary>
        /// An on/off chip: a pill with an optional colour dot and a label, filled when on, outlined and dim when off
        /// (recorder series, Delta-V options). Put several in a <see cref="FlowWrap"/>.
        /// </summary>
        public UIButton Chip(UIObject parent, string text, Func<bool> get, Action<bool> set, Color? dot = null)
        {
            Headless.CommitTarget<bool> target = Target("Chip", get, set, text);
            UIButton b = Button(parent, text, () => target.Set(!get(), Pointer));
            target.Control = b;
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
            bool shown = get();
            StyledPart part = Part(b.GetComponent<Button>(), b.GetComponent<Image>(), Headless.StyleParts.Chip, new[] { label });

            void Look(Theme th)
            {
                part.Set(Headless.StyleFlags.On, shown);
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

        // ---- meter ----

        /// <summary>A meter's Unity side: its model and what it draws, for the agent bridge and tests.</summary>
        public sealed class MeterHandle : MonoBehaviour
        {
            public Headless.GaugeModel Model;

            /// <summary>The label as given (a '#key' or a text).</summary>
            public string Label;

            /// <summary>The fill: its anchorMax.x is the fraction drawn.</summary>
            public RectTransform Fill;

            /// <summary>The marks of the low and high bounds (inactive when that bound isn't set).</summary>
            public RectTransform LowMark, HighMark;

            /// <summary>The value's text (pushed as a char array: read it with GetParsedText).</summary>
            public TMP_Text Text;

            /// <summary>The meter's sentence in the current language (allocates).</summary>
            public string Describe() => Model.Describe(MeterWords, ShownLabel(Label));
        }

        static Headless.GaugeWords meterWords;
        static string meterWordsLanguage;

        /// <summary>
        /// The words of the meters' sentences and of their bars, from the localization keys (<c>#MechJeb_NewUI_Meter_*</c>), read again
        /// after a language switch; a key that is missing keeps its English word.
        /// </summary>
        public static Headless.GaugeWords MeterWords
        {
            get
            {
                string language = KSP.Localization.Localizer.CurrentLanguage;
                if (meterWords != null && language == meterWordsLanguage) return meterWords;
                Headless.GaugeWords d = Headless.GaugeModel.DefaultWords;
                meterWords = new Headless.GaugeWords
                {
                    Reading = Tag("Reading", d.Reading), Scale = Tag("Scale", d.Scale), LowBound = Tag("LowBound", d.LowBound),
                    HighBound = Tag("HighBound", d.HighBound), ZoneRated = Tag("ZoneRated", d.ZoneRated), OffScale = Tag("OffScale", d.OffScale),
                    NotAvailable = Tag("NotAvailable", d.NotAvailable), Missing = Tag("Missing", d.Missing), Low = Tag("Low", d.Low),
                    Mid = Tag("Mid", d.Mid), High = Tag("High", d.High), Good = Tag("Good", d.Good), Fair = Tag("Fair", d.Fair), Poor = Tag("Poor", d.Poor)
                };
                meterWordsLanguage = language;
                return meterWords;
            }
        }

        // the raw text of a key, its <<1>> placeholders kept for GaugeWords.Fill
        static string Tag(string name, string fallback) =>
            KSP.Localization.Localizer.TryGetStringByTag("#MechJeb_NewUI_Meter_" + name, out string s) && !string.IsNullOrEmpty(s) ? s : fallback;

        /// <summary>
        /// label [bar] value: a value on a scale (<see cref="Headless.GaugeModel"/>, HTML's meter) as a filled bar, with marks at its
        /// low and high bounds and the value as text. The fill's colour is the rating's (good green, fair yellow, poor red, a plain bar
        /// the accent), and a fair or poor value also says its zone in words ("low", "high"), so colour is never the only sign. With
        /// no value or no scale the bar is empty and the text says so. The tooltip is the model's sentence. The model is read every
        /// frame while the meter is shown (a hidden one is not read), and the bar, marks and text are written only when what they show
        /// changed: no garbage. The meter owns the model's <see cref="Headless.GaugeModel.Sync"/>: others read it as drawn.
        /// <paramref name="template"/> is the widest value text expected, which sizes its cell. Several meters one under the other
        /// line up their bars in a grid: <see cref="PropertyGrid.Meter"/>.
        /// </summary>
        public MeterHandle Meter(UIObject parent, string label, Headless.GaugeModel model, string template = "000 % high")
        {
            Layout row = parent.Add<Layout>("Meter");
            row.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(6).ChildAlignment(TextAnchor.MiddleLeft);
            row.FlexibleLayout(true, false);
            row.Finish();
            return MeterIn(row, label, model, template);
        }

        // the meter's cells (label, bar, value) in a row of its own or a grid's
        MeterHandle MeterIn(Layout row, string label, Headless.GaugeModel model, string template)
        {
            if (model.Label == null) model.Label = label;
            Label(row, label);

            UIEmpty bar = row.Add<UIEmpty>("Bar");
            bar.Finish();
            LayoutElement barLe = bar.gameObject.GetComponent<LayoutElement>() ?? bar.gameObject.AddComponent<LayoutElement>();
            barLe.flexibleWidth = 1;
            barLe.minWidth = 60;
            Image track = Overlay(bar, "Track", Vector2.zero, Vector2.one, Vector2.zero);
            track.raycastTarget = true; // the tooltip's target
            Image fill = Overlay(bar, "Fill", Vector2.zero, new Vector2(0, 1), Vector2.zero);
            Image lowMark = Overlay(bar, "LowMark", Vector2.zero, new Vector2(0, 1), new Vector2(2, 0));
            Image highMark = Overlay(bar, "HighMark", Vector2.zero, new Vector2(0, 1), new Vector2(2, 0));

            UIEmpty cell = row.Add<UIEmpty>("MeterValue");
            UIText t = cell.Add<UIText>();
            t.Text("").Anchor(AnchorPresets.StretchAll);
            t.Finish();
            t.tmpText.enableWordWrapping = false;
            t.tmpText.alignment = TextAlignmentOptions.Right;
            t.tmpText.overflowMode = TextOverflowModes.Ellipsis;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            LayoutElement cellLe = cell.gameObject.GetComponent<LayoutElement>() ?? cell.gameObject.AddComponent<LayoutElement>();
            cell.Finish();
            var text = new CharText(t.tmpText);

            MeterHandle h = row.gameObject.AddComponent<MeterHandle>();
            h.Model = model;
            h.Label = label;
            h.Fill = fill.rectTransform;
            h.LowMark = lowMark.rectTransform;
            h.HighMark = highMark.rectTransform;
            h.Text = t.tmpText;
            Tooltips.Add(track.gameObject, h.Describe);

            Headless.GaugeRating shownRating = model.Rating;
            bool shownAvailable = model.Available;

            void Look(Theme th)
            {
                th.ApplyText(t.tmpText);
                Vector2 size = TextMeasure.Measure(template, th);
                cellLe.preferredWidth = size.x + 8;
                cellLe.preferredHeight = Mathf.Max(size.y, th.LineHeight);
                barLe.preferredHeight = barLe.minHeight = Mathf.Max(6, Mathf.Round(th.FontSize * 0.6f));
                track.sprite = fill.sprite = Sprites.Rounded;
                track.type = fill.type = Image.Type.Sliced;
                track.color = Alpha(th.Text, 0.12f);
                Color c = RatingColor(th, shownRating);
                fill.color = c;
                lowMark.color = highMark.color = Alpha(th.Text, 0.7f);
                t.tmpText.color = !shownAvailable ? th.Dim : shownRating == Headless.GaugeRating.Fair || shownRating == Headless.GaugeRating.Poor ? c : th.Text;
            }

            void Draw()
            {
                fill.rectTransform.anchorMax = new Vector2((float)model.Fraction, 1);
                Mark(lowMark, model.Available && model.Zoned && model.HasLow, model.FractionOf(model.Low));
                Mark(highMark, model.Available && model.Zoned && model.HasHigh, model.FractionOf(model.High));
                Headless.GaugeWords words = MeterWords;
                model.FormatValue(text.Chars);
                if (!model.Available) text.Append(words.Missing);
                else if (model.Rating == Headless.GaugeRating.Fair || model.Rating == Headless.GaugeRating.Poor) text.Append(' ').Append(words.Of(model.Zone));
                text.Commit();
                if (model.Rating == shownRating && model.Available == shownAvailable) return;
                shownRating = model.Rating;
                shownAvailable = model.Available;
                Look(Th);
            }

            Styled(row.gameObject, Look);
            Draw();
            GameObject go = row.gameObject;
            syncs.Add(() =>
            {
                // hidden (its setting off, a folded section): not read
                if (!go.activeInHierarchy) return;
                if (model.Sync()) Draw();
            });
            return h;
        }

        static void Mark(Image mark, bool shown, double at)
        {
            if (mark.gameObject.activeSelf != shown) mark.gameObject.SetActive(shown);
            if (!shown) return;
            RectTransform rt = mark.rectTransform;
            var a = new Vector2((float)at, 0);
            if (rt.anchorMin == a) return;
            rt.anchorMin = a;
            rt.anchorMax = new Vector2((float)at, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        static Color RatingColor(Theme th, Headless.GaugeRating r) =>
            r == Headless.GaugeRating.Good ? th.Good : r == Headless.GaugeRating.Fair ? th.Warn : r == Headless.GaugeRating.Poor ? th.Bad : th.Accent;

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
            /// limiter that is acting now). <paramref name="suffix"/>: a short word at the right end of the label cell, next to
            /// the field column ("Lo" of a Lo/Hi pair); it flips the switch and takes the tone like the label, and is not part of
            /// the switch's name.
            /// </summary>
            public Layout Switch(string label, Func<bool> get, Action<bool> set, Func<string> text = null, Action<string> setText = null,
                float width = 60, string unit = null, Func<bool> valid = null, Func<Tone> tone = null, FieldKind kind = FieldKind.Number,
                string suffix = null)
            {
                Layout row = Row();
                Toggle t = w.Switch(row, get, set);
                UIText l;
                if (suffix == null) l = Label(row, label);
                else
                {
                    // one cell: the label takes the space, the suffix keeps its width at the right
                    Layout cell = KodeWindow.Row(row);
                    l = Label(cell, label, true);
                    UIText s = Label(cell, suffix);
                    LabelFlips(s, t);
                    if (tone != null) w.ToneText(s, tone);
                }
                LabelFlips(l, t);
                w.Labelled(t, label);
                if (tone != null) w.ToneText(l, tone);
                if (text == null) return row;
                TMP_InputField f = EditFields.Field(w, row, text, setText, width, valid, kind: kind);
                w.Labelled(f, label);
                UIText u = Label(row, unit ?? "");
                Dim(f.gameObject, u.gameObject, () => !get());
                return row;
            }

            /// <summary>
            /// label [bar] value: a meter (<see cref="KodeWindow.Meter"/>) in a grid of its own, so the bars of the meters under one
            /// another start at one column (make the grid with <see cref="FlexibleColumn"/> 1, the bar).
            /// </summary>
            public MeterHandle Meter(string label, Headless.GaugeModel model, string template = "000 % high") => w.MeterIn(Row(), label, model, template);

            /// <summary>A row of one cell across the grid: a sub-heading, a note, a button.</summary>
            public UIText Note(string text)
            {
                Layout row = Row();
                UIText t = Label(row, text);
                Styled(t.gameObject, th => th.ApplyText(t.tmpText, th.Dim));
                return t;
            }

            /// <summary>label [field] unit, no switch. <paramref name="kind"/>: what the field holds (<see cref="EditFields.Align"/>).</summary>
            public Layout Field(string label, Func<string> text, Action<string> setText, float width = 60, string unit = null, Func<bool> valid = null,
                FieldKind kind = FieldKind.Number)
            {
                Layout row = Row();
                Empty(row);
                Label(row, label);
                w.Labelled(EditFields.Field(w, row, text, setText, width, valid, kind: kind), label);
                Label(row, unit ?? "");
                return row;
            }

            /// <summary>
            /// label [read-out] unit: the value as text in the field column, edited in place when clicked
            /// (<see cref="EditFields.InlineEdit"/>). Its cells are a field row's.
            /// </summary>
            public Layout InlineEdit(string label, Func<string> text, Action<string> setText, float width = 60, string unit = null,
                Func<string, bool> parses = null, Func<bool> enabled = null, Func<Color?> readOutColor = null, Func<string, string> normalise = null,
                FieldKind kind = FieldKind.Number)
            {
                Layout row = Row();
                Empty(row);
                Label(row, label);
                w.Labelled(EditFields.InlineEdit(w, row, text, setText, width, parses, enabled, readOutColor, normalise, kind: kind), label);
                Label(row, unit ?? "");
                return row;
            }

            /// <summary>
            /// label value [unit]: a read-out in the label and field columns, its unit in the unit column as a field's. The value's
            /// cell is sized from <paramref name="template"/> (its longest text) when given, else from the value when the row is made.
            /// </summary>
            public Layout Value(string label, Func<string> value, string unit = null, string template = null)
            {
                Layout row = Row();
                Empty(row);
                Label(row, label);
                UIText v = template != null ? w.BoundLabel(row, template, value) : w.BoundLabel(row, value);
                v.tmpText.alignment = TextAlignmentOptions.Right;
                if (unit != null) Label(row, unit);
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
        /// pages. Without get/set the window keeps the selection itself. The tabs are a <see cref="Headless.ChoiceModel"/> (tab
        /// items); the pages follow its shown entry, so a pick shows its page in the same frame.
        /// </summary>
        public Layout[] Tabs(UIObject parent, string[] titles, Func<int> get = null, Action<int> set = null, string label = null) =>
            Tabs(parent, titles, get, set, out _, label);

        /// <summary>A <see cref="Tabs"/> that also hands out its model.</summary>
        public Layout[] Tabs(UIObject parent, string[] titles, Func<int> get, Action<int> set, out Headless.ChoiceModel model, string label = null)
        {
            int current = 0;
            get ??= () => current;
            set ??= i => current = i;
            Segmented(parent, titles, get, set, ChoiceStyle.Tabs, out Headless.ChoiceModel choice, label);
            model = choice;
            var pages = new Layout[titles.Length];
            for (int i = 0; i < titles.Length; i++) pages[i] = Panel(parent);
            void Pages()
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    bool on = choice.IsOn(i);
                    if (pages[i].gameObject.activeSelf != on) pages[i].gameObject.SetActive(on);
                }
            }
            Pages();
            choice.ShownChanged += Pages;
            return pages;
        }

        /// <summary>
        /// A list on the left, the selected item's details on the right (the planner's operations). Consecutive items
        /// with the same group are listed under its header. Returns the details panel. The list is a
        /// <see cref="Headless.ChoiceModel"/> of options (nothing selected is allowed): a click picks through it, and its shown
        /// entry marks the row.
        /// </summary>
        public Layout MasterDetail(UIObject parent, float listWidth, float height, int count, Func<int, string> name, Func<int, string> group,
            Func<int> get, Action<int> set) => MasterDetail(parent, listWidth, height, count, name, group, get, set, out _);

        /// <summary>A <see cref="MasterDetail"/> that also hands out its list's model.</summary>
        public Layout MasterDetail(UIObject parent, float listWidth, float height, int count, Func<int, string> name, Func<int, string> group,
            Func<int> get, Action<int> set, out Headless.ChoiceModel model)
        {
            // the list's name: the text before it, else its first group's header, else the window's title
            string label = LabelBefore(parent) ?? (count > 0 ? group?.Invoke(0) : null) ?? Behaviour.Frame.Label;
            Layout row = Row(parent);
            row.ChildAlignment(TextAnchor.UpperLeft);
            ScrollView sv = ScrollList(row, height, out Layout list);
            var sle = sv.GetComponent<LayoutElement>();
            sle.flexibleWidth = 0;
            sle.minWidth = sle.preferredWidth = listWidth;

            // a selection rather than a setting: a commit, but not an edit for the owner to save
            Headless.CommitTarget<int> target = Target("MasterDetail", get, set, null, false);
            target.Control = list;
            var choice = new Headless.ChoiceModel(count, get, target.Set) { ItemLabels = new string[count], ItemRole = Headless.PartRole.Option };
            target.Model = choice;
            LabelTarget(target, label);
            model = choice;
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
                string text = name(i);
                choice.ItemLabels[i] = Loc(text);
                rows[i] = ListRow(list, text, () => choice.Select(k, Pointer));
            }
            void Mark()
            {
                for (int i = 0; i < rows.Length; i++) SetSelected(rows[i], choice.IsOn(i));
            }
            Mark();
            // a pick (which syncs the model itself) and a change from outside both mark the row, at once
            choice.ShownChanged += Mark;
            syncs.Add(() => choice.Sync());

            Layout detail = Panel(row);
            detail.FlexibleLayout(true, false);
            return detail;
        }

        // ---- title bar ----

        /// <summary>
        /// A flat icon button in the title bar, before the close button (the custom windows' edit and copy). The tooltip
        /// is a text or a '#key', and names the icon's entry in the title's context menu.
        /// </summary>
        public UIButton TitleIcon(Sprite icon, string tooltip, Action onClick)
        {
            UIButton b = IconButton(TitleRow, icon, onClick);
            FlatIcon(b, false);
            b.transform.SetSiblingIndex(CloseButton.transform.GetSiblingIndex());
            if (!string.IsNullOrEmpty(tooltip)) Tooltips.Add(b.gameObject, tooltip); // a '#key' is localized when shown
            NameButtonByKey(b, tooltip);
            AddTitleMenuEntry(b, tooltip);
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
                if (icon == null) return;
                var ile = icon.GetComponent<LayoutElement>();
                ile.minWidth = ile.preferredWidth = ile.minHeight = ile.preferredHeight = Mathf.Round(th.FontSize * 0.75f);
            });
            // nothing at rest, a light box under the pointer (red for the close button); the icon or text dimmed, bold
            Part(b.GetComponent<Button>(), b.GetComponent<Image>(), close ? Headless.StyleParts.TitleClose : Headless.StyleParts.TitleIcon,
                text != null ? new[] { text } : null, icon != null ? new[] { icon } : null);
        }
    }
}
