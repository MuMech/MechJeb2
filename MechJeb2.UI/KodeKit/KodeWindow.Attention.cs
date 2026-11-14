using System;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    // Settings that need the player's attention: an orange frame round a field, its label in the caution colour, and a badge on
    // the button that opens the window they are in.
    public partial class KodeWindow
    {
        /// <summary>
        /// Marks a row while <paramref name="needs"/> is true: its edit field gets a frame and its label the caution colour. The label is
        /// the row's first text outside the field. Call it after the row is built, like ToneText.
        /// </summary>
        public void Attention(Layout row, Func<bool> needs)
        {
            TMP_InputField field = row.GetComponentInChildren<TMP_InputField>(true);
            if (field != null) Attention(field, needs);

            foreach (TMP_Text t in row.GetComponentsInChildren<TMP_Text>(true))
            {
                if (field != null && t.transform.IsChildOf(field.transform)) continue;
                ToneText(t, () => needs() ? Tone.Caution : Tone.Normal);
                break;
            }
        }

        /// <summary>
        /// A frame in the caution colour round <paramref name="field"/> while <paramref name="needs"/> is true. The keyboard focus frame takes
        /// its place while the field is being edited.
        /// </summary>
        public void Attention(TMP_InputField field, Func<bool> needs)
        {
            Image frame = Overlay(field.gameObject, "Attention", Sprites.RoundedBox(3, 2, new Color(1, 1, 1, 0), Color.white));
            Styled(frame.gameObject, th => frame.color = th.Caution);
            bool shown = false;
            frame.enabled = false;
            syncs.Add(() =>
            {
                bool v = needs() && !field.isFocused;
                if (v == shown) return;
                shown = v;
                frame.enabled = v;
            });
        }

        /// <summary>
        /// A "!" in a caution-coloured circle on the top right corner of <paramref name="button"/> while <paramref name="show"/> is true: one
        /// of the settings behind the button needs a look. It stays visible whatever the button's role (a window button drawn selected).
        /// </summary>
        public void Badge(UIButton button, Func<bool> show)
        {
            const float size = 15;
            var go = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = button.gameObject.layer };
            go.transform.SetParent(button.transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(-3, -3); // just inside the corner, so a clipping parent can't cut it
            Image dot = go.GetComponent<Image>();
            dot.sprite = Sprites.Circle;
            dot.raycastTarget = false; // clicks reach the button

            var textGo = new GameObject("Mark", typeof(RectTransform), typeof(TextMeshProUGUI)) { layer = go.layer };
            textGo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var mark = textGo.GetComponent<TextMeshProUGUI>();
            mark.raycastTarget = false;
            mark.alignment = TextAlignmentOptions.Center;
            mark.text = "!";

            Styled(go, th =>
            {
                dot.color = th.Caution;
                th.ApplyText(mark, new Color(0.1f, 0.06f, 0.02f));
                mark.fontSize = 11;
                mark.fontStyle = FontStyles.Bold;
            });

            bool shown = show();
            go.SetActive(shown);
            syncs.Add(() =>
            {
                bool v = show();
                if (v == shown) return;
                shown = v;
                go.SetActive(v);
            });
        }

        // an image stretched over parent, drawn above its other children and ignored by its layout and by clicks
        static Image Overlay(GameObject parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = parent.layer };
            go.transform.SetParent(parent.transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }
    }
}
