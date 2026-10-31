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
        /// <summary>A button whose label colour follows a state (IMGUI: a GUIStyle with a coloured text).</summary>
        public static UIButton ToneButton(KodeWindow w, UIObject parent, string text, Action click, Func<KodeWindow.Tone> tone, bool flexible = false)
        {
            UIButton b = KodeWindow.Button(parent, text, click, flexible);
            w.ToneText(b.GetComponentInChildren<TMP_Text>(), tone, true);
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
            if (tone != null) w.ToneText(b.GetComponentInChildren<TMP_Text>(), tone, true);
            return b;
        }


    }
}
