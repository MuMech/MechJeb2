using System;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>Small private helpers shared by the Translatron, Warp Helper, RCS Balancer, SmartRcs, Flight Recorder and Menu views.</summary>
    internal static class MiscViewKit
    {
        public static Color ToneColor(KodeWindow.Tone tone)
        {
            Theme th = Theme.Current ?? Theme.KodeDefault();
            return tone == KodeWindow.Tone.Good ? th.Good : tone == KodeWindow.Tone.Warn ? th.Warn : tone == KodeWindow.Tone.Bad ? th.Bad : th.Button.TextColor;
        }

        /// <summary>A button whose label colour follows a state (IMGUI: a GUIStyle with a coloured text).</summary>
        public static UIButton ToneButton(KodeWindow w, UIObject parent, string text, Action click, Func<KodeWindow.Tone> tone, bool flexible = false)
        {
            UIButton b = KodeWindow.Button(parent, text, click, flexible);
            Tone(w, b, tone);
            return b;
        }

        /// <summary>A button whose text changes (cached strings: the getter is called every frame) and optionally follows a state.</summary>
        public static UIButton BoundButton(KodeWindow w, UIObject parent, Func<string> text, Action click, Func<KodeWindow.Tone> tone = null, bool flexible = false)
        {
            UIButton b = KodeWindow.Button(parent, text(), click, flexible);
            string last = text();
            w.AddSync(() =>
            {
                string s = text();
                if (s == last) return;
                last = s;
                b.Text(s);
            });
            if (tone != null) Tone(w, b, tone);
            return b;
        }

        static void Tone(KodeWindow w, UIButton b, Func<KodeWindow.Tone> tone)
        {
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            KodeWindow.Tone shown = KodeWindow.Tone.Normal;
            // added after the button's own restyle, so it runs later and wins
            w.Behaviour.AddRestyle(() => label.color = ToneColor(shown));
            w.AddSync(() =>
            {
                KodeWindow.Tone t = tone();
                if (t == shown) return;
                shown = t;
                label.color = ToneColor(t);
            });
            shown = tone();
            label.color = ToneColor(shown);
        }

        /// <summary>A label that changes: a fixed-size cell sized from the template, text pushed when the string changes.</summary>
        public static UIText LiveBound(KodeWindow w, UIObject parent, string template, Func<string> value, bool flexible = false)
        {
            UIText t = KodeWindow.LiveLabel(parent, template, flexible);
            string last = value();
            t.Text(last);
            w.AddSync(() =>
            {
                string s = value();
                if (s == last) return;
                last = s;
                t.Text(s);
            });
            return t;
        }

        /// <summary>The label of a toggle made by KodeWindow.Toggle.</summary>
        public static TMP_Text ToggleLabel(UnityEngine.UI.Toggle toggle) =>
            toggle.transform.parent.GetComponentInChildren<ToggleLabelClick>().GetComponent<TMP_Text>();

    }
}
