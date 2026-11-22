using System;
using UnityEngine;

namespace KodeKit
{
    /// <summary>
    /// What a person (or the agent) did in a window that is no commit: a button pressed, a section folded. With its source, for an
    /// observer of the whole UI (the agent bridge's change feed).
    /// </summary>
    public struct ViewAction
    {
        /// <summary>"press" or "fold".</summary>
        public string Type;

        /// <summary>The header's title as shown (a fold); null for a press (the observer reads the button).</summary>
        public string Label;

        /// <summary>"open" or "closed" for a fold; null for a press.</summary>
        public string Value;

        /// <summary>The button or the header.</summary>
        public Component Control;

        public Headless.ChangeSource Source;
    }

    public partial class KodeWindow
    {
        // AnyAction's observers, replaced (not changed) on each add or remove, as AnyCommit's
        static Action<KodeWindow, ViewAction>[] anyAction = new Action<KodeWindow, ViewAction>[0];

        /// <summary>
        /// Every press of a KodeKit button (<see cref="Button(KodeUI.UIObject,string,Action,bool)"/>, <see cref="ListRow"/>) and every
        /// fold of a section, in any window, before the button's action runs: what a watcher of the player needs besides the commits.
        /// Nothing is looked up while no one observes.
        /// </summary>
        public static event Action<KodeWindow, ViewAction> AnyAction
        {
            add
            {
                if (value == null) return;
                var a = new Action<KodeWindow, ViewAction>[anyAction.Length + 1];
                Array.Copy(anyAction, a, anyAction.Length);
                a[anyAction.Length] = value;
                anyAction = a;
            }
            remove
            {
                int i = Array.LastIndexOf(anyAction, value);
                if (i < 0) return;
                var a = new Action<KodeWindow, ViewAction>[anyAction.Length - 1];
                Array.Copy(anyAction, a, i);
                Array.Copy(anyAction, i + 1, a, i, anyAction.Length - i - 1);
                anyAction = a;
            }
        }

        static void ReportAction(KodeWindow w, ViewAction a)
        {
            Action<KodeWindow, ViewAction>[] called = anyAction;
            for (int i = 0; i < called.Length; i++)
            {
                try
                {
                    called[i](w, a);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // a button of a window was pressed (a click, or the agent's): its window found only when someone observes
        static void ReportPress(Component button)
        {
            if (anyAction.Length == 0 || button == null) return;
            KodeWindow w = button.GetComponentInParent<WindowBehaviour>()?.Window;
            if (w == null) return; // a dialog's or a popup's button: the dialog's answer is reported by its model
            ReportAction(w, new ViewAction { Type = "press", Control = button, Source = Pointer });
        }

        void ReportFold(Component header, string title, bool open)
        {
            if (anyAction.Length == 0) return;
            ReportAction(this, new ViewAction { Type = "fold", Label = ShownLabel(title), Value = open ? "open" : "closed", Control = header, Source = Pointer });
        }
    }
}
