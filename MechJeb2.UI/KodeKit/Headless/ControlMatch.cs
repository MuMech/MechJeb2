#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KodeKit.Headless
{
    /// <summary>A control as <see cref="ControlMatch"/> sees it: its ref, its id, its label, and whether it takes the step's action.</summary>
    public struct ControlName
    {
        public string Ref, Id, Label;
        public bool TakesAction;
    }

    /// <summary>
    /// How an agent's step names its control (the agent bridge's <c>Resolve</c>), without Unity.
    /// <list type="bullet">
    /// <item>An id given as an id (the step's "id"): the controls whose <see cref="CommitTarget.Id"/> it is, among those that take the
    /// action. Ids are unique in a window; two windows with the id are ambiguous. None: the controls whose id is it after a build
    /// scope (<see cref="Unscoped"/>: "#MechJeb_Maneu_button1" names "p0/#MechJeb_Maneu_button1"), as only the shown panel's controls
    /// are listed; two are ambiguous.</item>
    /// <item>Else a ref (the step's "ref" equal to a control's ref): that control, whatever its actions (the action's own error tells).</item>
    /// <item>Else, among the controls that take the action: a label equal to the target (case-insensitive) or an id equal to it; else the
    /// one label it begins; else the one label holding it as whole words. Never a plain substring: "Engage" must not find "Disengage".
    /// An id never wins over a label: a text that is one control's id and another's label is ambiguous.</item>
    /// <item>More than one candidate at the first rule that finds any is ambiguous: the result lists them.</item>
    /// </list>
    /// </summary>
    public static class ControlMatch
    {
        /// <summary>The index of the control the step names; -1 when none does, -2 when several do (<paramref name="candidates"/>).</summary>
        public static int Resolve(IList<ControlName> controls, string target, string id, List<int> candidates)
        {
            candidates.Clear();
            if (id != null)
            {
                for (int i = 0; i < controls.Count; i++)
                    if (controls[i].TakesAction && controls[i].Id == id)
                        candidates.Add(i);
                if (candidates.Count == 0)
                    for (int i = 0; i < controls.Count; i++)
                        if (controls[i].TakesAction && controls[i].Id != null && controls[i].Id != id && Unscoped(controls[i].Id) == id)
                            candidates.Add(i);
                return Picked(candidates);
            }
            if (target == null) return -1;
            for (int i = 0; i < controls.Count; i++)
                if (controls[i].Ref == target)
                    return i;
            var prefix = new List<int>();
            var words = new List<int>();
            Regex whole = null;
            for (int i = 0; i < controls.Count; i++)
            {
                ControlName c = controls[i];
                if (!c.TakesAction) continue;
                string l = c.Label ?? "";
                if (c.Id == target || string.Equals(l, target, StringComparison.OrdinalIgnoreCase)) candidates.Add(i);
                else if (l.StartsWith(target, StringComparison.OrdinalIgnoreCase)) prefix.Add(i);
                else
                {
                    whole = whole ?? new Regex(@"(?<![\w])" + Regex.Escape(target) + @"(?![\w])", RegexOptions.IgnoreCase);
                    if (whole.IsMatch(l)) words.Add(i);
                }
            }
            if (candidates.Count == 0) candidates.AddRange(prefix.Count > 0 ? prefix : words);
            return Picked(candidates);
        }

        /// <summary>
        /// An id without its build scope: the leading "p&lt;n&gt;/" parts a lazy panel or section gives the ids made in it
        /// (<see cref="CommitPath.NameScope"/>): "p0/p2/#MechJeb_Maneu_STB" is "#MechJeb_Maneu_STB".
        /// </summary>
        public static string Unscoped(string id)
        {
            if (id == null) return null;
            int at = 0;
            while (at + 2 < id.Length && id[at] == 'p' && char.IsDigit(id[at + 1]))
            {
                int k = at + 1;
                while (k < id.Length && char.IsDigit(id[k])) k++;
                if (k >= id.Length || id[k] != '/') break;
                at = k + 1;
            }
            return at == 0 ? id : id.Substring(at);
        }

        static int Picked(List<int> candidates) => candidates.Count == 1 ? candidates[0] : candidates.Count == 0 ? -1 : -2;

        /// <summary>
        /// Whether a step's or a request's <paramref name="within"/> names a window (the agent bridge's <c>within</c>):
        /// <list type="bullet">
        /// <item>null: every window;</item>
        /// <item>its session ref ("w3"), exactly;</item>
        /// <item>its module (<paramref name="module"/>: a display module's type name, "MechJebModuleManeuverPlanner", or
        /// "CustomInfoWindow:&lt;its saved id&gt;"), whole and case-insensitive: "MechJebModuleAscent" does not name
        /// "MechJebModuleAscentSettingsMenu". The same in every language;</item>
        /// <item>a part of its shown title, case-insensitive (the title is in the language now).</item>
        /// </list>
        /// </summary>
        public static bool InWindow(string within, string title, string windowRef, string module) =>
            within == null || windowRef == within || module != null && string.Equals(module, within, StringComparison.OrdinalIgnoreCase) ||
            title != null && title.IndexOf(within, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// The ids of a window's named buttons, in the window's hierarchy order (hidden ones included, so a fold or a hidden row moves
        /// no id): <paramref name="names"/>[i] is the name the i-th button was built with (its build scope and its text's '#key';
        /// null: none) and <paramref name="ids"/>[i] gets its id (null for none).
        /// <list type="bullet">
        /// <item>The first button of a name has the name as its id, the n-th the name and "/n" ("#MechJeb_Maneu_button5/2").</item>
        /// <item>An id that a commit target of the window has (<paramref name="targetHas"/>) takes "/button": an id names one control.</item>
        /// <item>Language-independent (keys, not texts) and stable across rebuilds (the build order is the same).</item>
        /// </list>
        /// </summary>
        public static void ButtonIds(IList<string> names, Func<string, bool> targetHas, string[] ids)
        {
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                if (string.IsNullOrEmpty(name))
                {
                    ids[i] = null;
                    continue;
                }
                seen.TryGetValue(name, out int n);
                seen[name] = ++n;
                string id = n == 1 ? name : name + "/" + n;
                if (targetHas != null && targetHas(id)) id += "/button";
                ids[i] = id;
            }
        }
    }

    /// <summary>
    /// Refs that stay with their objects: the n-th button, header, read-out or table a window showed keeps its number for the
    /// window's life, a new one gets the next number, and a number is never given again. An old ref names its object or nothing,
    /// never the control that took its place after a section folded or an autopilot swapped its buttons.
    /// <list type="bullet">
    /// <item><see cref="Of"/>: "w3.b4" for the window 3's object of kind 'b' with that id (Unity's instance id, unique for the game).</item>
    /// <item><see cref="KeepOnly"/>: the windows gone are forgotten (their serials are never reused either).</item>
    /// </list>
    /// </summary>
    [NotAWidget("the agent bridge's numbering of a window's buttons, headers, read-outs and tables")]
    public sealed class StableRefs
    {
        sealed class Numbers
        {
            public readonly Dictionary<long, int> ByObject = new Dictionary<long, int>();
            public readonly Dictionary<char, int> Next = new Dictionary<char, int>();
        }

        readonly Dictionary<int, Numbers> windows = new Dictionary<int, Numbers>();

        /// <summary>The ref of the object <paramref name="objectId"/> of <paramref name="kind"/> ('b', 'd', 'r', 't') in the window <paramref name="window"/>.</summary>
        public string Of(int window, char kind, int objectId)
        {
            if (!windows.TryGetValue(window, out Numbers w)) windows[window] = w = new Numbers();
            long key = ((long)kind << 32) | (uint)objectId;
            if (!w.ByObject.TryGetValue(key, out int n))
            {
                w.Next.TryGetValue(kind, out n);
                w.Next[kind] = n + 1;
                w.ByObject[key] = n;
            }
            return "w" + window + "." + kind + n;
        }

        /// <summary>Forgets every window but <paramref name="open"/>.</summary>
        public void KeepOnly(ICollection<int> open)
        {
            List<int> gone = null;
            foreach (int w in windows.Keys)
                if (!open.Contains(w))
                    (gone = gone ?? new List<int>()).Add(w);
            if (gone == null) return;
            foreach (int w in gone) windows.Remove(w);
        }

        /// <summary>The windows remembered.</summary>
        public int Windows => windows.Count;
    }
}
