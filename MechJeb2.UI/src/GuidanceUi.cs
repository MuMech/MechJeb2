using System;
using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
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

        /// <summary>The rendezvous windows' state: 0 = no target, 1 = target in another sphere of influence, 2 = usable target.</summary>
        public static int RendezvousMode(MechJebCore core, Orbit orbit)
        {
            if (core.Target == null || !core.Target.NormalTargetExists) return 0;
            return core.Target.TargetOrbit.referenceBody != orbit.referenceBody ? 1 : 2;
        }

        /// <summary>The rendezvous windows' notes for modes 0 (no target) and 1 (other sphere of influence); returns the panel shown in mode 2.</summary>
        public static Layout RendezvousGate(KodeWindow w, UIObject r, Func<int> mode, string noneKey, string otherKey)
        {
            Layout none = KodeWindow.Panel(r);
            Wrapped(none, noneKey);
            w.Visible(none.gameObject, () => mode() == 0);

            Layout other = KodeWindow.Panel(r);
            Wrapped(other, otherKey);
            w.Visible(other.gameObject, () => mode() == 1);

            Layout main = KodeWindow.Panel(r);
            w.Visible(main.gameObject, () => mode() == 2);
            return main;
        }

        /// <summary>A wrapping text in the theme's warning colour (GuiUtils.YellowLabel).</summary>
        public static UIText Warning(KodeWindow w, UIObject parent, string textOrKey)
        {
            UIText t = Wrapped(parent, textOrKey);
            KodeWindow.ToneText(t, KodeWindow.Tone.Warn);
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
            // GuiUtils.SimpleLabel: the value flush right (the cell is as wide as the template, often wider than the value)
            UIText value = Live(w, row, template, false, fill, active);
            value.tmpText.alignment = TextAlignmentOptions.Right;
            return value;
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

        /// <summary>
        /// The modern layout's "AUTO": while <paramref name="auto"/> holds, <paramref name="body"/> stays in view but greyed
        /// and inert, and a status bar under it says which module is in control (<paramref name="controller"/>).
        /// </summary>
        public static Layout AutoBar(KodeWindow w, UIObject parent, GameObject body, string autoKey, Func<bool> auto, Func<object> controller)
        {
            Layout bar = w.StatusBar(parent, () => KodeWindow.Tone.Warn);
            Layout row = KodeWindow.Row(bar);
            w.Badge(row, autoKey, KodeWindow.Tone.Warn);
            object shownUser = null;
            string text = Localizer.Format("#MechJeb_NewUI_OtherInControl"); //Another autopilot is in control
            w.BoundLabel(row, () =>
            {
                object u = controller();
                if (ReferenceEquals(u, shownUser)) return text;
                shownUser = u;
                string name = UserName(u);
                text = name != null ? Localizer.Format("#MechJeb_NewUI_InControl", name) : Localizer.Format("#MechJeb_NewUI_OtherInControl"); //<<1>> is in control
                return text;
            }, true);
            w.Visible(bar.gameObject, auto);

            CanvasGroup group = body.GetComponent<CanvasGroup>() ?? body.AddComponent<CanvasGroup>();
            bool shown = false;
            w.AddSync(() =>
            {
                bool a = auto();
                if (a == shown) return;
                shown = a;
                group.alpha = a ? 0.45f : 1;
                group.interactable = !a;
                group.blocksRaycasts = !a;
            });
            return bar;
        }

        /// <summary>A module's name for the player: its window title, or its type name in words ("Node Executor").</summary>
        public static string UserName(object user)
        {
            if (user is DisplayModule d) return d.GetName();
            if (!(user is ComputerModule)) return null;
            string type = user.GetType().Name.Replace("MechJebModule", "");
            var sb = new System.Text.StringBuilder(type.Length + 4);
            for (int i = 0; i < type.Length; i++)
            {
                if (i > 0 && char.IsUpper(type[i]) && !char.IsUpper(type[i - 1])) sb.Append(' ');
                sb.Append(type[i]);
            }
            return sb.ToString();
        }

        /// <summary>The first user of a controller other than <paramref name="self"/> (null: none).</summary>
        public static object OtherUser(UserPool users, object self)
        {
            if (users == null) return null;
            foreach (object u in users)
                if (!ReferenceEquals(u, self)) return u;
            return null;
        }

        public static int HoursPerDay => KSPUtil.dateTimeFormatter.Day / 3600;
        public static int DaysPerYear => KSPUtil.dateTimeFormatter.Year / KSPUtil.dateTimeFormatter.Day;
    }
}
