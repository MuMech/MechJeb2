using System;
using KodeKit;
using KodeUI;
using KSP.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// Small helpers shared by the landing, rendezvous and docking views (kept private to those views, in this assembly only).
    /// </summary>
    internal static class GuidanceUi
    {
        /// <summary>A static text that wraps to the window width instead of widening the window.</summary>
        public static UIText Wrapped(UIObject parent, string textOrKey)
        {
            UIText t = KodeWindow.Paragraph(parent, textOrKey);
            var le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 0;
            le.preferredWidth = 0;
            le.flexibleWidth = 1;
            return t;
        }

        /// <summary>A wrapping text in the theme's warning colour (GuiUtils.YellowLabel).</summary>
        public static UIText Warning(KodeWindow w, UIObject parent, string textOrKey)
        {
            UIText t = Wrapped(parent, textOrKey);
            t.tmpText.color = Theme.Current.Warn;
            w.Behaviour.AddRestyle(() => t.tmpText.color = Theme.Current.Warn);
            return t;
        }

        /// <summary>
        /// A fixed-size label sized from <paramref name="template"/>, refilled every frame by <paramref name="fill"/> without
        /// allocating (CharText). Nothing runs while <paramref name="active"/> is false, and a throwing fill keeps the last text.
        /// </summary>
        public static UIText Live(KodeWindow w, UIObject parent, string template, bool flexible, Action<CharText> fill, Func<bool> active = null)
        {
            UIText t = KodeWindow.LiveLabel(parent, template, flexible);
            var text = new CharText(t.tmpText);
            w.AddSync(() =>
            {
                if (active != null && !active()) return;
                try
                {
                    text.Clear();
                    fill(text);
                    text.Commit();
                }
                catch (Exception)
                {
                    // no vessel, no target, no prediction yet: keep what is shown
                }
            });
            return t;
        }

        /// <summary>GuiUtils.SimpleLabel: a label on the left, a live value on the right.</summary>
        public static UIText Pair(KodeWindow w, UIObject parent, string key, string template, Action<CharText> fill, Func<bool> active = null)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, key, true);
            return Live(w, row, template, false, fill, active);
        }

        /// <summary>The text before and after the &lt;&lt;1&gt;&gt; of a localized string, so a number can be appended without Localizer.Format.</summary>
        public sealed class Parts
        {
            readonly string key;
            string pre, post;

            public Parts(string key) => this.key = key;

            void Load()
            {
                if (pre != null) return;
                string s = Localizer.Format(key, "\u0001");
                int i = s.IndexOf('\u0001');
                pre = i < 0 ? s : s.Substring(0, i);
                post = i < 0 ? "" : s.Substring(i + 1);
            }

            public string Pre
            {
                get
                {
                    Load();
                    return pre;
                }
            }

            public string Post
            {
                get
                {
                    Load();
                    return post;
                }
            }
        }

        /// <summary>Localizer.Format(key, arg) rebuilt only when the argument changes.</summary>
        public static Func<string> Format1(string key, Func<string> arg)
        {
            string lastArg = null, last = null;
            return () =>
            {
                string a = arg() ?? "";
                if (last == null || a != lastArg)
                {
                    lastArg = a;
                    last = Localizer.Format(key, a);
                }

                return last;
            };
        }

        /// <summary>Enable or disable a button (GUI.enabled = ...) following a condition.</summary>
        public static void Enable(KodeWindow w, UIButton b, Func<bool> enabled)
        {
            var button = b.GetComponent<UnityEngine.UI.Button>();
            button.interactable = enabled();
            w.AddSync(() =>
            {
                bool e = enabled();
                if (button.interactable != e) button.interactable = e;
            });
        }

        /// <summary>Make a button at least this wide (GUILayout.Width on a button).</summary>
        public static void MinWidth(UIButton b, float width)
        {
            var le = b.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
            le.minWidth = width;
        }

        public static int HoursPerDay => KSPUtil.dateTimeFormatter.Day / 3600;
        public static int DaysPerYear => KSPUtil.dateTimeFormatter.Year / KSPUtil.dateTimeFormatter.Day;
    }
}
