using System;
using System.Reflection;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A field's keyboard scope (<see cref="KeyboardScope"/>): it holds KSP's keyboard lock while its input field has focus, so typing
    /// "1" in a MechJeb field doesn't also fire action group 1. The field reads its own text keys; the scope routes the keys its
    /// owners ask for with <see cref="Route"/> (a stepper's Up, Down, PageUp and PageDown, the input history's Up and Down, a prompt's
    /// Tab), so they come from the stack: only while the field's scope is on top (not under a dialog), and never a key held as the top
    /// changed. It leaves uGUI's navigation on; covering the scopes under it keeps their keys from them while it is on top. Opened on
    /// select, closed on deselect and end of edit (its lock goes once Escape and Enter are up), dropped on disable and destroy.
    /// </summary>
    public class FieldKeyboardLock : KeyboardScope
    {
        protected override string LockPrefix => "KodeKitField_";

        /// <summary>Routes <paramref name="keys"/> to <paramref name="handler"/> while the field's scope is on top.</summary>
        public void Route(Headless.ScopeKeys keys, Action<Headless.ScopeKeys> handler)
        {
            Headless.KeyScope s = Scope;
            s.Routes |= keys;
            s.Key += handler;
        }

        /// <summary>Called when the field is disabled (hidden with its panel): its edit session is over.</summary>
        internal Action Disabled;

        protected override void OnDisable()
        {
            Disabled?.Invoke();
            base.OnDisable();
        }
    }

    /// <summary>A KodeKit field's model, and its way to show a text the model already holds without handing it back to the model.</summary>
    public class FieldText : MonoBehaviour
    {
        internal Action<string> Show;

        /// <summary>The field's behaviour (its edit, its text, its validity): what a click and the agent bridge both drive.</summary>
        public Headless.TextFieldModel Model { get; internal set; }
    }

    /// <summary>
    /// A KodeKit field as focus recovery sees it (<see cref="Headless.FocusRecovery"/>, <see cref="KodeWindow.RememberFocus"/>): its
    /// name in a rebuilt window, its edit, how it takes one over, and its end-of-edit actions held back meanwhile.
    /// </summary>
    public class FieldFocus : MonoBehaviour, Headless.IFocusField
    {
        internal TMP_InputField Input;
        internal Headless.CommitTarget Target;

        // the field's edit (its text at focus, which Escape returns to)
        internal Headless.TextFieldModel Model;

        // the field's check that its text parses (null: none), for its input history
        internal Func<bool> Valid;

        // the field's place in its window's build (KodeWindow.NextFieldPlace): with its kind, its name in a rebuilt window when it
        // has no stable name
        internal string Place;

        // the view's name for the field (EditFields.Named: a custom window item's id), the same in every build and language
        internal string ViewName;

        // the edit taken over: its commit, for the group the selection opens, then its caret and text at focus, applied once the field
        // has the keyboard (TMP takes it a frame after the selection, and selects the whole text then)
        internal Headless.Commit Carry;
        internal Headless.FocusSnapshot Restoring;

        /// <summary>
        /// The field's stable name in its window, "Kind=name", where it has one: the view's name for it (<see cref="EditFields.Named"/>,
        /// also its target's <c>Id</c>); else its kind and its place in the window's build, "Kind:place" (the place is numbered in the
        /// lazy panel or section it is in, so it doesn't depend on the order panels are first shown). A target's Id taken from its label
        /// is no name for focus: two fields of one kind and label in one panel share it (the second gets none), and a place is checked
        /// against the field's value (<see cref="FocusCheck"/>).
        /// </summary>
        public string FocusKey
        {
            get
            {
                string kind = Target?.Kind ?? "Field";
                string id = StableId;
                if (id != null) return kind + "=" + id;
                return Place != null ? kind + ":" + Place : null;
            }
        }

        string StableId
        {
            get
            {
                return ViewName;
            }
        }

        /// <summary>
        /// The value's text (its target's value), for a field not named by the view: a field found again by its place
        /// that shows another value is another field, and gets no edit. Null for a field the view named.
        /// </summary>
        public string FocusCheck => ViewName != null ? null : Target?.Read() as string;

        /// <inheritdoc />
        public bool CanFocus => this != null && isActiveAndEnabled && Input != null && Input.isActiveAndEnabled && Input.IsInteractable();

        internal bool Remember(object owner)
        {
            if (Input == null) return false;
            string original = Model != null && Model.Editing ? Model.Original : null;
            return KodeWindow.Focus.Remember(owner, this, Input.text, original, Input.selectionAnchorPosition, Input.selectionFocusPosition, Target?.Last);
        }

        /// <inheritdoc />
        public void TakeOver(Headless.FocusSnapshot edit)
        {
            EventSystem events = EventSystem.current;
            if (events == null || edit == null) return;
            Restoring = edit;
            Carry = edit.Session;
            // the selection takes the keyboard lock and opens the edit's group, carrying on the old field's commit
            events.SetSelectedGameObject(gameObject);
            Carry = null;
            // the text as typed (the model may hold another: a view that keeps the typed text until the edit ends), inside the group
            if (Input.text != edit.Text) Input.text = edit.Text;
        }

        // the field's end-of-edit actions (its own and its view's: a typed value applied on focus loss), held back while its edit is
        // remembered, and the text they would have had
        TMP_InputField.SubmitEvent held;
        string heldText;

        /// <summary>Whether focus recovery holds the field's end-of-edit actions (its edit goes on in a rebuilt window, or ends later).</summary>
        internal bool Holding => held != null;

        /// <inheritdoc />
        public void Hold()
        {
            if (Input == null || held != null) return;
            // the field's input history waits too: the commit its deselection settles is half an edit if the edit goes on
            GetComponent<FieldHistory>()?.Hold();
            heldText = Input.text;
            held = Input.onEndEdit;
            // the field's own end of edit (its group, its lock) also runs from its deselection and from its disabling
            Input.onEndEdit = new TMP_InputField.SubmitEvent();
        }

        /// <inheritdoc />
        public void Release(Headless.FocusRelease how)
        {
            TMP_InputField.SubmitEvent e = held;
            held = null;
            if (e == null) return;
            if (this != null) GetComponent<FieldHistory>()?.Release(how);
            switch (how)
            {
                case Headless.FocusRelease.Kept:
                    if (Input != null) Input.onEndEdit = e;
                    break;
                case Headless.FocusRelease.Ended:
                    // as at the focus loss the window's destruction made: its view's actions see the text the field had
                    e.Invoke(heldText);
                    break;
                case Headless.FocusRelease.HandedOver:
                    // the edit goes on in the rebuilt field: this field's ends with nothing written (its deselection, which comes with
                    // the take-over, would otherwise end it as a focus loss and write the draft)
                    Model?.Drop();
                    break;
            }
        }

        /// <summary>Puts the taken-over caret, selection and text at focus in place: the field has the keyboard now.</summary>
        internal bool ApplyRestore()
        {
            Headless.FocusSnapshot s = Restoring;
            Restoring = null;
            if (s == null) return false;
            int length = Input.text.Length;
            int anchor = Math.Min(s.Anchor, length), caret = Math.Min(s.Caret, length);
            Input.selectionStringAnchorPosition = anchor;
            Input.selectionStringFocusPosition = caret;
            Input.selectionAnchorPosition = anchor;
            Input.selectionFocusPosition = caret;
            if (s.Original != null) Model?.Resume(s.Original);
            Input.ForceLabelUpdate();
            return true;
        }
    }


    /// <summary>
    /// What a field holds, which sets where its text lines up (<see cref="EditFields.Align"/>): a number on the right, like the read-outs
    /// of a value column, so the digits of fields under one another line up; a text (a name, a title, a filter) on the left.
    /// </summary>
    public enum FieldKind
    {
        /// <summary>A number, on the right: the default, as most of MechJeb's fields hold numbers.</summary>
        Number,
        /// <summary>A text (a name, a title, a filter, a prompt), on the left.</summary>
        Text,
        /// <summary>A number in the middle of a small box (the stepper's field, between its - and + buttons).</summary>
        Centred,
    }

    public static partial class EditFields
    {
        /// <summary>
        /// A text field bound to a MechJeb-style IEditable (get/set of the text), written as typed (<see cref="Headless.TextFieldModel"/>,
        /// <see cref="Headless.TextCommit.AsTyped"/>). The setter may normalise the text (MechJeb strips junk characters with a regex);
        /// the field then shows the normalised text. <paramref name="valid"/> colours the text red while it doesn't parse. The field's
        /// model is its commit target's <c>Model</c> and its <see cref="FieldText.Model"/>.
        /// </summary>
        /// <param name="label">The field's name (a '#key' or a text; null: the label right before it in its row, <see cref="KodeWindow.LabelBefore"/>).</param>
        /// <param name="kind">What it holds: <see cref="FieldKind.Number"/> lines its text up on the right (a text alone can't tell).</param>
        public static TMP_InputField Field(KodeWindow w, UIObject parent, Func<string> get, Action<string> set, float width,
            Func<bool> valid = null, float fontSize = 14, string label = null, FieldKind kind = FieldKind.Number)
        {
            label = label ?? KodeWindow.LabelBefore(parent);
            Headless.CommitTarget<string> target = w.Target("Field", get, set);
            TMP_InputField f = Field(w, parent, target, width, kind, (begin, end) =>
                new Headless.TextFieldModel(get, target.Set, Headless.TextCommit.AsTyped, null, valid, null, null, begin, end));
            w.LabelTarget(target, label);
            return f;
        }

        /// <summary>
        /// The field on a commit target of its owner (a stepper's, an inline edit's), drawing the model <paramref name="make"/> builds
        /// with the field's session hooks (focus to the end of the edit is one commit of <paramref name="target"/>). Typing goes to the
        /// model with the Keyboard source; Enter, Escape and a focus loss end its edit. <paramref name="ended"/> hears every end.
        /// <paramref name="kind"/> lines its text up (<see cref="Align"/>).
        /// </summary>
        internal static TMP_InputField Field(KodeWindow w, UIObject parent, Headless.CommitTarget<string> target, float width, FieldKind kind,
            Func<Action, Action, Headless.TextFieldModel> make, Action<Headless.TextEditEnd> ended = null)
        {
            UIInputField f = parent.Add<UIInputField>();
            f.Finish();
            f.PreferredSize(width, 22);

            Image bg = f.GetComponent<Image>();
            TMP_InputField input = f.GetComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.onFocusSelectAll = true; // click a field and type to replace its value
            input.selectionColor = new Color(0.3f, 0.55f, 1f, 0.55f); // a visible selection on the dark field
            input.caretWidth = 2; // TMP's 1 px is easy to miss, and under MSAA it falls across two screen pixels and turns grey
            // Escape is the model's (TextFieldModel.Cancel): it knows the text the edit began with
            input.restoreOriginalTextOnEscape = false;

            var area = (RectTransform)f.transform.Find("Text Area");
            area.offsetMin = new Vector2(4, 1);
            area.offsetMax = new Vector2(-4, -1);

            ((TextMeshProUGUI)input.placeholder).text = "";
            Align(input, kind);
            TMP_Text[] texts = f.GetComponentsInChildren<TextMeshProUGUI>(true);
            bool shownValid = true;
            // the field only grows, like the value cells: a value that doesn't fit widens it once (IMGUI's field shows
            // "0.562" in 35 px; this one, with its rounded inner margins, would cut the last digit)
            float fieldWidth = width;
            Theme theme = null; // the window's (a custom window can have its own), set by the restyle below
            // a number wider than its field even widened: out of an edit it shows its start (its sign and its leading digits) rather than
            // its end, so it is lined up on the left then; in an edit TMP scrolls the text to the caret, in either alignment
            bool overflowing = false;
            void AlignShown() => Align(input, kind != FieldKind.Text && overflowing && !input.isFocused ? FieldKind.Text : kind);
            void Fit(string value)
            {
                float wanted = string.IsNullOrEmpty(value) ? 0 : TextMeasure.Measure(value, theme).x + 10; // the text area's 4 px margins, and 2 to spare
                // numbers need a few pixels more at most; free text (names) scrolls inside the field instead of widening the window
                float fitted = Mathf.Min(wanted, width * 1.6f);
                if (fitted > fieldWidth)
                {
                    fieldWidth = fitted;
                    f.PreferredSize(fieldWidth, theme.RowHeight);
                }
                bool over = wanted > fieldWidth;
                if (over == overflowing) return;
                overflowing = over;
                AlignShown();
            }
            KodeWindow.Styled(f.gameObject, th =>
            {
                theme = th;
                th.Field.ApplySelectable(input, bg);
                foreach (var t in texts) th.ApplyText(t, th.Field.TextColor);
                if (!shownValid) input.textComponent.color = th.Bad;
                fieldWidth = width; // a theme change measures again, in the new font size
                f.PreferredSize(width, th.RowHeight);
                Fit(input.text);
            });

            // a frame in the accent while the field has the keyboard. Made on the first focus: most fields never get one. Not a sprite of
            // the field's states: TMP_InputField shows a focused field as highlighted, its hover look
            StyledPart frame = null;
            void ShowFocus(bool on)
            {
                if (frame == null)
                {
                    if (!on) return;
                    frame = StyledPart.FocusFrame(f.transform, 0);
                }
                frame.Set(Headless.StyleFlags.Focus, on);
            }

            if (target.Control == null) target.Control = input;

            var keyLock = f.gameObject.AddComponent<FieldKeyboardLock>();
            // focus recovery: the field's name in a rebuilt window, and the edit it takes over from the field it replaces
            var focus = f.gameObject.AddComponent<FieldFocus>();
            focus.Input = input;
            focus.Target = target;
            focus.Place = w.NextFieldPlace();

            // the behaviour: an edit session (focus to Enter, Escape or focus loss) is one commit, carrying on a taken-over edit's
            Headless.TextFieldModel model = make(() => target.ContinueGroup(focus.Carry), target.EndGroup);
            if (target.Model == null) target.Model = model;
            focus.Model = model;
            focus.Valid = model.ParsesNow;
            var shown = f.gameObject.AddComponent<FieldText>();
            shown.Model = model;

            bool pushing = false;
            // shows a text the model holds without handing it back to the model
            void Show(string s)
            {
                if (input.text == s) return;
                pushing = true;
                input.text = s;
                pushing = false;
            }
            // a host that changed the value (a stepper's Up/Down) shows its text: in an open edit it is the draft
            shown.Show = t =>
            {
                Show(t);
                model.Shows(t);
            };

            // the edit is over: the keyboard lock goes once Enter and Escape are up, the field lets go of its selection (Enter and
            // Escape leave it selected: it would keep its focused look, and the next click on it would select nothing new, so no onSelect
            // and no keyboard lock), and it shows the value
            void Finished(Headless.TextEditEnd how)
            {
                // an edit held back by focus recovery may end after its window was destroyed (FieldFocus.Release): Unity's null checks
                if (keyLock != null) keyLock.UnlockAfterKeys();
                EventSystem events = EventSystem.current;
                if (f != null && events != null && !events.alreadySelecting && events.currentSelectedGameObject == f.gameObject) events.SetSelectedGameObject(null);
                if (input != null) Show(model.Shown);
                ended?.Invoke(how);
            }

            keyLock.Disabled = () =>
            {
                // hidden before TMP gave it the keyboard (no end of edit then): the taken-over caret is not for a later click, and the
                // edit ends with nothing written. TMP's activation waiting for its next update goes too, and the selection: shown again,
                // the field would select itself and take KSP's keyboard lock unasked, or keep a selection a click can't renew
                focus.Restoring = null;
                // an edit focus recovery holds ends when it is released (its window rebuilt or destroyed for good)
                if (!model.Editing || focus.Holding) return;
                // a field that had the keyboard ended its edit from TMP's own OnDisable
                if (input.isFocused) return;
                // the end first: a null-conditional call would skip evaluating it
                Headless.TextEditEnd how = model.Drop();
                EndEdit(input);
                ended?.Invoke(how);
            };
            input.onSelect.AddListener(_ =>
            {
                keyLock.Lock();
                // the field took the keyboard: the edit opens (a click, focus recovery's take-over, a host's Open); one open already (an
                // Enter refused) goes on
                if (!model.Editing) model.Begin();
            });
            input.onDeselect.AddListener(_ =>
            {
                focus.Restoring = null;
                keyLock.UnlockAfterKeys();
                // deselected before it had the keyboard (TMP sends no end of edit then); not an edit focus recovery holds, which goes on
                // in the rebuilt window or ends when released
                if (model.Editing && !focus.Holding) Finished(model.Leave(KodeWindow.Source(Headless.ChangeSource.Keyboard)));
            });
            // Enter came before the end of the edit (TMP sends its submit, then ends the edit); KSP's TMP sends it for Escape too, which
            // the field's wasCanceled tells
            bool submitted = false;
            input.onSubmit.AddListener(_ => submitted = true);
            input.onEndEdit.AddListener(_ =>
            {
                bool enter = submitted;
                submitted = false;
                Headless.TextEditEnd how = Headless.TextEditEnd.None;
                if (model.Editing)
                {
                    // the field may be gone (focus recovery runs a destroyed window's held end of edit, as a focus loss)
                    bool alive = input != null;
                    Headless.ChangeSource source = KodeWindow.Source(Headless.ChangeSource.Keyboard);
                    if (alive && input.wasCanceled) how = model.Cancel(source);
                    else if (enter) how = model.Submit(source);
                    else how = model.Leave(source);
                    if (model.Editing)
                    {
                        // Enter on a text that doesn't parse: the edit stays open, the field keeps its selection and its lock, and takes
                        // the keyboard again in its next update
                        if (alive) input.ActivateInputField();
                        ended?.Invoke(how);
                        return;
                    }
                }
                Finished(how);
            });

            input.text = model.Shown;
            Fit(input.text);
            input.onValueChanged.AddListener(s =>
            {
                Fit(s);
                if (pushing) return;
                model.Type(s, KodeWindow.Source(Headless.ChangeSource.Keyboard));
                string kept = model.Shown;
                if (kept == s) return;
                // junk was stripped: show what the model kept, keep the caret where it makes sense
                int caret = Math.Min(input.caretPosition, kept.Length);
                Show(kept);
                input.caretPosition = caret;
            });

            TMP_Text text = input.textComponent;
            bool wasFocused = false;
            w.AddSync(() =>
            {
                // select the whole value when the field gains focus, so typing replaces it
                // (KSP's TMP ignores onFocusSelectAll; this runs after the click has placed the caret)
                bool focused = input.isFocused;
                if (focused != wasFocused)
                {
                    ShowFocus(focused);
                    if (overflowing) AlignShown();
                }
                // focus lost with neither an end of edit nor a deselect: the edit is over
                if (wasFocused && !focused && model.Editing && !Selected(f.gameObject))
                    Finished(model.Leave(KodeWindow.Source(Headless.ChangeSource.Keyboard)));
                // an edit taken over gets its caret and selection back instead
                if (focused && !wasFocused && !focus.ApplyRestore())
                {
                    input.selectionAnchorPosition = 0;
                    input.selectionFocusPosition = input.text.Length;
                    input.ForceLabelUpdate();
                }
                wasFocused = focused;

                bool editing = model.Editing;
                if (model.Sync())
                {
                    if (editing && !model.Editing) EndEdit(input); // read-only now: the edit ended without writing
                    else if (model.Mode == Headless.TextCommit.AtEnd && model.Editing && focused && input.text != model.Draft)
                    {
                        // at the end: a draft not typed in yet follows the value: the field shows it, all selected, and Escape returns to it. The
                        // string positions too: typing replaces the selection TMP keeps in them
                        Show(model.Draft);
                        int n = input.text.Length;
                        input.selectionStringAnchorPosition = 0;
                        input.selectionStringFocusPosition = n;
                        input.selectionAnchorPosition = 0;
                        input.selectionFocusPosition = n;
                        input.ForceLabelUpdate();
                    }
                }
                // not focused: the field shows the model (the value; the draft of an edit opened or taken over, not yet focused)
                if (!focused) Show(model.Shown);
                bool v = model.Valid;
                if (v == shownValid) return;
                shownValid = v;
                text.color = v ? theme.Field.TextColor : theme.Bad;
            });
            return input;
        }

        /// <summary>
        /// Lines the field's text and its placeholder up for what it holds, on the right for a number and on the left for a text, and
        /// keeps their vertical alignment. Set once, on the text components (a restyle sets their font, size and colour only); the
        /// caret, the selection and the scrolling of a text wider than the field are TMP's, which follow any alignment.
        /// </summary>
        public static void Align(TMP_InputField input, FieldKind kind)
        {
            int horizontal = (int)(kind == FieldKind.Number ? TextAlignmentOptions.Right : kind == FieldKind.Centred ? TextAlignmentOptions.Center : TextAlignmentOptions.Left) & HorizontalBits;
            Align(input.textComponent, horizontal);
            Align(input.placeholder as TMP_Text, horizontal);
        }

        // TextAlignmentOptions: the horizontal flags in the low byte, the vertical ones above it
        const int HorizontalBits = 0xFF;

        static void Align(TMP_Text text, int horizontal)
        {
            if (text == null) return;
            SetAlignment(text, (TextAlignmentOptions)(((int)text.alignment & ~HorizontalBits) | horizontal));
        }

        /// <summary>
        /// Sets a text's alignment so that it stays when the text wakes later. A TMP text made under an inactive object (a window or a
        /// panel built hidden) wakes on its first activation, and TMP then reads its alignment as a value of TMP's old enum
        /// (<c>TMP_Text.LoadDefaultSettings</c>): every value set before comes out TopLeft. Marked as converted already, it keeps the one set.
        /// </summary>
        internal static void SetAlignment(TMP_Text text, TextAlignmentOptions alignment)
        {
            alignmentConverted?.SetValue(text, True);
            text.alignment = alignment;
        }

        // TMP_Text's "alignment already in the new enum" flag, protected in TMP; boxed once
        static readonly FieldInfo alignmentConverted =
            typeof(TMP_Text).GetField("m_isAlignmentEnumConverted", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly object True = true;

        static bool Selected(GameObject go)
        {
            EventSystem events = EventSystem.current;
            return events != null && events.currentSelectedGameObject == go;
        }

        /// <summary>
        /// Ends the field's edit from the host's side (the model already ended it, or is about to): the field lets go of the keyboard,
        /// as at Enter. A field selected but not focused yet also drops TMP's pending activation.
        /// </summary>
        internal static void EndEdit(TMP_InputField input)
        {
            if (input == null) return;
            if (input.isFocused) input.DeactivateInputField();
            // an edit ended in the frame it opened: TMP gives a selected field the keyboard in its next LateUpdate, and would select
            // itself again then (a new edit, and the keyboard lock taken)
            else shouldActivate?.SetValue(input, false);
            EventSystem events = EventSystem.current;
            if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == input.gameObject) events.SetSelectedGameObject(null);
            FieldText shown = input.GetComponent<FieldText>();
            if (shown != null) shown.Show(shown.Model.Shown);
        }

        // TMP_InputField's activation waiting for its next LateUpdate; private in TMP, and no public member clears it
        static readonly FieldInfo shouldActivate =
            typeof(TMP_InputField).GetField("m_ShouldActivateNextUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
