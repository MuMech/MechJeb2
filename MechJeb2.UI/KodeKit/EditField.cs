using System;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Takes a KSP keyboard lock while its input field has focus, so typing "1" in a MechJeb field
    /// doesn't also fire action group 1. Released on deselect, end of edit, disable and destroy.
    /// </summary>
    public class FieldKeyboardLock : MonoBehaviour
    {
        string lockId;
        public bool Locked { get; private set; }
        public string LockId => lockId ??= "KodeKitField_" + GetInstanceID();

        public void Lock()
        {
            if (Locked) return;
            InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, LockId);
            Locked = true;
        }

        public void Unlock()
        {
            if (!Locked) return;
            InputLockManager.RemoveControlLock(LockId);
            Locked = false;
        }

        void OnDisable() => Unlock();
        void OnDestroy() => Unlock();
    }

    public static class EditFields
    {
        /// <summary>
        /// A text field bound to a MechJeb-style IEditable (get/set of the text). The setter may normalise the
        /// text (MechJeb strips junk characters with a regex); the field then shows the normalised text.
        /// <paramref name="valid"/> colours the text red while it doesn't parse.
        /// </summary>
        public static TMP_InputField Field(KodeWindow w, UIObject parent, Func<string> get, Action<string> set, float width,
            Func<bool> valid = null, float fontSize = 14)
        {
            UIInputField f = parent.Add<UIInputField>();
            f.Finish();
            f.PreferredSize(width, 22);

            Image bg = f.GetComponent<Image>();
            TMP_InputField input = f.GetComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.onFocusSelectAll = true; // click a field and type to replace its value
            input.selectionColor = new Color(0.3f, 0.55f, 1f, 0.55f); // a visible selection on the dark field
            input.caretWidth = 2; // TMP's 1 px usually falls across two screen pixels, and shows as a faint grey line

            var area = (RectTransform)f.transform.Find("Text Area");
            area.offsetMin = new Vector2(4, 1);
            area.offsetMax = new Vector2(-4, -1);

            ((TextMeshProUGUI)input.placeholder).text = "";
            TMP_Text[] texts = f.GetComponentsInChildren<TextMeshProUGUI>(true);
            bool shownValid = true;
            // the field only grows, like the value cells: a value that doesn't fit widens it once (IMGUI's field shows
            // "0.562" in 35 px; this one, with its rounded inner margins, would cut the last digit)
            float fieldWidth = width;
            Theme theme = null; // the window's (a custom window can have its own), set by the restyle below
            void Fit(string value)
            {
                if (string.IsNullOrEmpty(value)) return;
                float wanted = TextMeasure.Measure(value, theme).x + 10; // the text area's 4 px margins, and 2 to spare
                // numbers need a few pixels more at most; free text (names) scrolls inside the field instead of widening the window
                wanted = Mathf.Min(wanted, width * 1.6f);
                if (wanted <= fieldWidth) return;
                fieldWidth = wanted;
                f.PreferredSize(fieldWidth, theme.RowHeight);
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
            Image frame = null;
            void ShowFocus(bool on)
            {
                if (frame == null)
                {
                    if (!on) return;
                    var go = new GameObject("Focus", typeof(RectTransform), typeof(Image)) { layer = f.gameObject.layer };
                    go.transform.SetParent(f.transform, false);
                    var rt = (RectTransform)go.transform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    frame = go.GetComponent<Image>();
                    frame.sprite = Sprites.RoundedBox(3, 2, new Color(1, 1, 1, 0), Color.white);
                    frame.type = Image.Type.Sliced;
                    frame.raycastTarget = false;
                }
                frame.color = theme.Accent;
                frame.enabled = on;
            }

            var keyLock = f.gameObject.AddComponent<FieldKeyboardLock>();
            input.onSelect.AddListener(_ => keyLock.Lock());
            input.onDeselect.AddListener(_ => keyLock.Unlock());
            input.onEndEdit.AddListener(_ =>
            {
                keyLock.Unlock();
                // Enter and Escape end the edit but leave the field selected: it would keep its focused look, and the next
                // click on it would select nothing new, so no onSelect and no keyboard lock
                EventSystem events = EventSystem.current;
                if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == f.gameObject) events.SetSelectedGameObject(null);
            });

            set = w.Edits(set);
            bool pushing = false;
            input.text = get();
            Fit(input.text);
            input.onValueChanged.AddListener(s =>
            {
                Fit(s);
                if (pushing) return;
                set(s);
                string normalised = get();
                if (normalised == s) return;
                // junk was stripped: show what the model kept, keep the caret where it makes sense
                pushing = true;
                int caret = Math.Min(input.caretPosition, normalised.Length);
                input.text = normalised;
                input.caretPosition = caret;
                pushing = false;
            });

            TMP_Text text = input.textComponent;
            bool wasFocused = false;
            w.AddSync(() =>
            {
                // select the whole value when the field gains focus, so typing replaces it
                // (KSP's TMP ignores onFocusSelectAll; this runs after the click has placed the caret)
                bool focused = input.isFocused;
                if (focused != wasFocused) ShowFocus(focused);
                if (focused && !wasFocused)
                {
                    input.selectionAnchorPosition = 0;
                    input.selectionFocusPosition = input.text.Length;
                    input.ForceLabelUpdate();
                }
                wasFocused = focused;

                if (!focused)
                {
                    string s = get();
                    if (input.text != s)
                    {
                        pushing = true;
                        input.text = s;
                        pushing = false;
                    }
                }
                if (valid == null) return;
                bool v = valid();
                if (v == shownValid) return;
                shownValid = v;
                text.color = v ? theme.Field.TextColor : theme.Bad;
            });
            return input;
        }
    }
}
