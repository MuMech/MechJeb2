using System.Collections.Generic;
using KodeUI;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    // the names and descriptions of a window's controls: what the agent bridge, the pattern checks and an accessibility tree read
    public partial class KodeWindow
    {
        // the targets whose model took its label from the target's '#key': relabelled on a language switch
        readonly List<Headless.CommitTarget> keyLabelled = new List<Headless.CommitTarget>();

        /// <summary>
        /// The label a control about to be added to <paramref name="parent"/> takes when its factory is given none: the text right before
        /// it in its row (a row's "Lead time:" before its stepper), as the '#key' it was set from where there was one (so the label follows
        /// a language switch and finds its tooltip), else as shown without a trailing colon; null when the last child is no label.
        /// </summary>
        internal static string LabelBefore(UIObject parent)
        {
            Transform p = parent != null ? parent.transform : null;
            if (p == null || p.childCount == 0) return null;
            UIText t = p.GetChild(p.childCount - 1).GetComponent<UIText>();
            if (t == null || t.tmpText == null) return null;
            WindowBehaviour wb = p.GetComponentInParent<WindowBehaviour>();
            if (wb != null && wb.TextKeys.TryGetValue(t.tmpText, out string key)) return key;
            string s = t.tmpText.text;
            return string.IsNullOrEmpty(s) ? null : s.TrimEnd(':', ' ', ' ');
        }

        /// <summary>A label as a model shows it: localized when a '#key', without a trailing colon; null for none.</summary>
        public static string ShownLabel(string label)
        {
            string s = Loc(label);
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Trim().TrimEnd(':', ' ', ' ');
            return s.Length > 0 ? s : null;
        }

        /// <summary>
        /// Gives <paramref name="target"/> its label (a '#key' or a text) unless it has one, an id from it, and its model the label as
        /// shown unless the model has one. A null or empty label does nothing.
        /// </summary>
        internal void LabelTarget(Headless.CommitTarget target, string label)
        {
            if (target == null || string.IsNullOrEmpty(label)) return;
            if (target.Label == null) target.Label = label;
            Commits.NameTarget(target, label);
            if (!(target.Model is Headless.IDescribeParts m) || !string.IsNullOrEmpty(m.Label)) return;
            m.Label = ShownLabel(label);
            if (label.Length > 1 && label[0] == '#' && target.Label == label) keyLabelled.Add(target);
        }

        /// <summary>
        /// Gives <paramref name="part"/>, a model a widget is made of (a stepper's field), the label of the widget's target, as shown; it
        /// follows a language switch with it.
        /// </summary>
        internal void LabelPart(Headless.IDescribeParts part, Headless.CommitTarget owner)
        {
            if (part == null || owner == null || string.IsNullOrEmpty(owner.Label)) return;
            part.Label = ShownLabel(owner.Label);
            if (owner.Label.Length > 1 && owner.Label[0] == '#') keyParts.Add(new KeyValuePair<Headless.IDescribeParts, Headless.CommitTarget>(part, owner));
        }

        readonly List<KeyValuePair<Headless.IDescribeParts, Headless.CommitTarget>> keyParts = new List<KeyValuePair<Headless.IDescribeParts, Headless.CommitTarget>>();

        // a language switch: the models labelled from a '#key' show it in the new language
        void Relabel()
        {
            for (int i = 0; i < keyLabelled.Count; i++)
            {
                Headless.CommitTarget t = keyLabelled[i];
                if (!t.Removed && t.Model is Headless.IDescribeParts m) m.Label = ShownLabel(t.Label);
            }
            for (int i = 0; i < keyParts.Count; i++)
                if (!keyParts[i].Value.Removed) keyParts[i].Key.Label = ShownLabel(keyParts[i].Value.Label);
        }

        // the path's describer: what a control does, in the language now (CommitTarget.Description)
        static readonly System.Func<Headless.CommitTarget, string> describeTarget = DescribeTarget;

        // the path's label as shown, for a control with no model (ControlInfo)
        static readonly System.Func<string, string> showLabel = ShownLabel;

        /// <summary>
        /// What a control does: the tooltip of its label's '#key' (<see cref="TooltipFor"/>), else the tooltip a view put on the control
        /// or next to it (<see cref="TooltipNear"/>); null when it has none.
        /// </summary>
        public static string DescribeTarget(Headless.CommitTarget t)
        {
            if (t == null) return null;
            string l = t.Label;
            if (l != null && l.Length > 1 && l[0] == '#')
            {
                string d = TooltipFor?.Invoke(l);
                if (!string.IsNullOrEmpty(d)) return d;
            }
            return TooltipNear(t);
        }

        static readonly List<TooltipTrigger> triggers = new List<TooltipTrigger>();

        /// <summary>
        /// The text of the tooltip a view put on a control (<see cref="Tooltips.Add(GameObject, string)"/>): on the control or inside it
        /// (not on one of its items' buttons: a segmented bar's item has its own), else on the row around it and up to two levels more,
        /// as long as that row holds no other control of the window and the tooltip is on no other button there (a row's Reset button
        /// has its own). Localized; null when there is none.
        /// </summary>
        public static string TooltipNear(Headless.CommitTarget t)
        {
            if (t == null || !(t.Control is Component c) || c == null) return null;
            Transform own = c.transform;
            WindowBehaviour wb = own.GetComponentInParent<WindowBehaviour>();
            Transform stop = wb != null ? wb.transform : null;
            Transform at = own;
            for (int level = 0; level < 4 && at != null && at != stop; level++, at = at.parent)
            {
                if (level > 0 && HoldsAnother(t, own, at)) break;
                at.GetComponentsInChildren(true, triggers);
                for (int i = 0; i < triggers.Count; i++)
                {
                    TooltipTrigger tr = triggers[i];
                    if (tr.transform.IsChildOf(own) ? OnAnItem(tr.transform, own) : OnAnotherButton(tr.transform, at, own)) continue;
                    string s = tr.Text?.Invoke();
                    if (!string.IsNullOrEmpty(s))
                    {
                        triggers.Clear();
                        return Loc(s);
                    }
                }
                triggers.Clear();
            }
            return null;
        }

        // another control of the path is under row (not the target's own parts)
        static bool HoldsAnother(Headless.CommitTarget t, Transform own, Transform row)
        {
            IReadOnlyList<Headless.CommitTarget> all = t.Path.Targets;
            for (int i = 0; i < all.Count; i++)
            {
                if (ReferenceEquals(all[i], t) || !(all[i].Control is Component o) || o == null) continue;
                Transform ot = o.transform;
                if (ot.IsChildOf(row) && !ot.IsChildOf(own) && !own.IsChildOf(ot)) return true;
            }
            return false;
        }

        // the tooltip is inside the control, on (or under) one of its items' buttons: a Selectable between it and the control
        static bool OnAnItem(Transform tip, Transform own)
        {
            if (own.GetComponent<Selectable>() != null) return false; // the control is the button: what is inside it is its own
            for (Transform x = tip; x != null && x != own; x = x.parent)
                if (x.GetComponent<Selectable>() != null) return true;
            return false;
        }

        // the tooltip is on a button (any Selectable) between it and the row that isn't the control's own
        static bool OnAnotherButton(Transform tip, Transform row, Transform own)
        {
            for (Transform x = tip; x != null && x != row; x = x.parent)
                if (x.GetComponent<Selectable>() != null && !x.IsChildOf(own)) return true;
            return false;
        }
    }
}
