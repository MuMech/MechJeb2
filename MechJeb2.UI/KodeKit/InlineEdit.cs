using System;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// An inline edit (<see cref="Headless.InlineEditModel"/>, made with <see cref="EditFields.InlineEdit"/>): a read-out that is a
    /// KodeKit field drawn as text. A click (or anything that gives the field the keyboard: focus recovery, a host's
    /// <see cref="Open"/>) opens the edit in place, in the same rect, so nothing moves; Enter or a focus loss writes the typed text
    /// once through the field's commit target, Escape drops it.
    /// <list type="bullet">
    /// <item>Read-out: the field's box is hidden, a thin rule under the value says it can be edited, and hovering shows the box.</item>
    /// <item>Editing: the field's box and its focus frame; the text is red while it doesn't parse, and Enter then keeps the edit
    /// open. Nothing reaches the value while typing.</item>
    /// <item>The field holds KSP's keyboard lock while it has the keyboard and lets it go once Enter or Escape is up
    /// (<see cref="FieldKeyboardLock"/>): Escape cancels the edit and doesn't open KSP's pause menu.</item>
    /// <item>A read-only value (the model's enabled check) is shown dimmed and takes no click; one that becomes read-only during an
    /// edit ends it without writing.</item>
    /// </list>
    /// </summary>
    public class InlineEdit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>The edit's behaviour.</summary>
        public Headless.InlineEditModel Model { get; internal set; }

        /// <summary>The field the read-out turns into.</summary>
        public TMP_InputField Input { get; internal set; }

        /// <summary>The commit target the edit writes through (its window's).</summary>
        public Headless.CommitTarget<string> Target { get; internal set; }

        internal Action Refresh;
        internal bool Hovered;

        /// <summary>
        /// Opens the edit as a click on the read-out does: the field takes the keyboard (and KSP's keyboard lock). For a host without a
        /// pointer (the agent bridge, a test). False when the value is read-only or the edit is open already.
        /// </summary>
        public bool Open()
        {
            EventSystem events = EventSystem.current;
            if (events == null || Input == null || Model.Editing || !Model.Enabled || !Input.IsInteractable()) return false;
            events.SetSelectedGameObject(Input.gameObject);
            return Model.Editing;
        }

        /// <summary>Ends the edit as Enter does (for a host without keys). False when no edit was open.</summary>
        public bool Submit()
        {
            if (Input == null || !Model.Editing) return false;
            // as Enter: TMP's submit, then the end of the edit (selected, not yet focused: TMP gives it the keyboard a frame later)
            Input.onSubmit.Invoke(Input.text);
            if (Input.isFocused) Input.DeactivateInputField();
            else Input.onEndEdit.Invoke(Input.text);
            return true;
        }

        /// <summary>Ends the edit as Escape does (for a host without keys): nothing is written.</summary>
        public bool Cancel()
        {
            if (Input == null || !Model.Editing) return false;
            Model.Cancel();
            EditFields.EndEdit(Input);
            Refresh?.Invoke();
            return true;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            Hovered = true;
            Refresh?.Invoke();
        }

        public void OnPointerExit(PointerEventData e)
        {
            Hovered = false;
            Refresh?.Invoke();
        }

        void OnDisable()
        {
            Hovered = false;
        }
    }

    public static partial class EditFields
    {
        /// <summary>
        /// A read-out of a text value that turns into a field when clicked, in place (<see cref="InlineEdit"/>). <paramref name="get"/>
        /// reads the value's text; <paramref name="set"/> writes it, once per edit, through the window's commit path (a target of kind
        /// "InlineEdit": the window's undo stack records it when the window opts in, and the field can take an input history key with
        /// <see cref="History"/>). <paramref name="parses"/> checks a typed text without writing it (null: any text);
        /// <paramref name="enabled"/> makes the value read-only while false; <paramref name="readOutColor"/> colours the read-out (a
        /// custom window's text colour; null: the theme's); <paramref name="normalise"/> cleans each typed text as the value would keep it
        /// (the field then shows the cleaned draft). In MechJeb, made on an <c>IEditable</c> only (<c>EditUi.Inline</c>).
        /// </summary>
        /// <param name="label">Its name (a '#key' or a text; null: the label right before it in its row, <see cref="KodeWindow.LabelBefore"/>).</param>
        public static TMP_InputField InlineEdit(KodeWindow w, UIObject parent, Func<string> get, Action<string> set, float width,
            Func<string, bool> parses = null, Func<bool> enabled = null, Func<Color?> readOutColor = null, Func<string, string> normalise = null,
            string label = null, FieldKind kind = FieldKind.Number)
        {
            label = label ?? KodeWindow.LabelBefore(parent);
            Headless.CommitTarget<string> target = w.Target("InlineEdit", get, set);
            InlineEdit inline = null;
            Headless.InlineEditModel model = null;
            // the field draws the model (written at the end): Enter or a focus loss writes the draft once, Escape drops it; the field
            // shows the draft, so a typed text the normaliser changed (junk thrown away) is shown cleaned, the caret clamped
            TMP_InputField input = Field(w, parent, target, width, kind, (begin, end) =>
                model = new Headless.InlineEditModel(get, target.Set, parses, enabled, normalise, begin, end), how => Ended(inline, how));
            GameObject go = input.gameObject;
            var box = (Image)input.targetGraphic;
            target.Model = model;
            w.LabelTarget(target, label);
            inline = go.AddComponent<InlineEdit>();
            inline.Model = model;
            inline.Input = input;
            inline.Target = target;

            // clicks: the box is hidden in the read-out, a clear graphic over the field takes them (a click on it reaches the field)
            var hit = new GameObject("Hit", typeof(RectTransform), typeof(Image)) { layer = go.layer };
            hit.transform.SetParent(go.transform, false);
            hit.transform.SetAsFirstSibling();
            Stretch((RectTransform)hit.transform);
            var hitImage = hit.GetComponent<Image>();
            hitImage.color = Color.clear;

            // the read-out's rule under the value: it can be edited
            var rule = new GameObject("Rule", typeof(RectTransform), typeof(Image)) { layer = go.layer };
            rule.transform.SetParent(go.transform, false);
            var rrt = (RectTransform)rule.transform;
            rrt.anchorMin = new Vector2(0, 0);
            rrt.anchorMax = new Vector2(1, 0);
            rrt.pivot = new Vector2(0.5f, 0);
            rrt.offsetMin = new Vector2(3, 1);
            rrt.offsetMax = new Vector2(-3, 2);
            var ruleImage = rule.GetComponent<Image>();
            ruleImage.raycastTarget = false;

            Theme theme = null;
            TMP_Text text = input.textComponent;
            // what is shown now, to restyle only on a change
            int look = -1;
            Color? colour = null;
            bool editable = model.Enabled;
            void Look()
            {
                if (theme == null) return;
                bool editing = model.Editing;
                int now = editing ? (model.Valid ? 1 : 2) : inline.Hovered && editable ? 3 : editable ? 4 : 5;
                Color? c = editing ? null : readOutColor?.Invoke();
                if (now == look && c == colour) return;
                look = now;
                colour = c;
                box.enabled = editing || now == 3;
                rule.SetActive(!editing && editable);
                ruleImage.color = new Color(theme.Dim.r, theme.Dim.g, theme.Dim.b, 0.6f);
                Color read = c ?? theme.Text;
                text.color = editing ? (model.Valid ? theme.Field.TextColor : theme.Bad) : editable ? read : new Color(read.r, read.g, read.b, read.a * 0.5f);
            }
            inline.Refresh = Look;
            KodeWindow.Styled(go, th =>
            {
                theme = th;
                look = -1; // the field's restyle put its box and text colour back
                Look();
            });

            input.interactable = editable;
            hitImage.raycastTarget = editable;
            input.onSelect.AddListener(_ =>
            {
                // the field's own listener opened the edit (a click, focus recovery's take-over, a host's Open), unless the value is
                // read-only
                if (!model.Editing)
                {
                    // read-only: no edit
                    EventSystem events = EventSystem.current;
                    if (events != null && !events.alreadySelecting) events.SetSelectedGameObject(null);
                }
                Look();
            });
            w.AddSync(() =>
            {
                // the field's own sync ran first: a value read-only now ended the edit without writing, before the field is made
                // non-interactable (which deselects it, and a focus loss would write the draft)
                bool e = model.Enabled;
                if (e != editable)
                {
                    editable = e;
                    input.interactable = e;
                    hitImage.raycastTarget = e;
                }
                Look();
            });
            return input;
        }

        // an end of the edit: refused (Enter on a text that doesn't parse, the edit open still) shows the invalid look; any other end
        // shows the read-out
        static void Ended(InlineEdit inline, Headless.TextEditEnd how)
        {
            if (inline == null) return;
            if (how == Headless.TextEditEnd.Cancelled && inline.Input != null && !inline.Input.isFocused) EditFields.EndEdit(inline.Input);
            inline.Refresh?.Invoke();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
