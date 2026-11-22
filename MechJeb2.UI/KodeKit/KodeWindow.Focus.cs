using System;
using KodeUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KodeKit
{
    // focus recovery: a field being edited keeps the keyboard when its window is rebuilt (Headless.FocusRecovery)
    public partial class KodeWindow
    {
        /// <summary>
        /// The edit a window remembers before it is destroyed and gives back once rebuilt (<see cref="RememberFocus"/>,
        /// <see cref="RecoverFocus"/>). One for every window: only one field has the keyboard.
        /// </summary>
        public static readonly Headless.FocusRecovery Focus = new Headless.FocusRecovery(() => Time.frameCount);

        // a field's place in the build, the name it has in a rebuilt window when its target has no Id: the lazy panel or section it
        // is in (numbered when the window makes it, which doesn't depend on when it is first shown) and its order there
        string buildScope = "";
        int scopeFields, scopePanels;

        internal string NextFieldPlace() => buildScope + "f" + scopeFields++;

        string NextPanelScope() => buildScope + "p" + scopePanels++ + "/";

        void BuildInScope(string scope, Action<Layout> build, Layout panel)
        {
            string outerScope = buildScope;
            int outerFields = scopeFields, outerPanels = scopePanels;
            buildScope = scope;
            scopeFields = scopePanels = 0;
            // the targets' label-derived Ids too: a label repeated across panels is one Id per panel, whatever the order they show in
            Commits.NameScope = scope;
            try
            {
                build(panel);
            }
            finally
            {
                Commits.NameScope = outerScope;
                buildScope = outerScope;
                scopeFields = outerFields;
                scopePanels = outerPanels;
            }
        }

        /// <summary>
        /// Before this window is destroyed to be rebuilt: remembers the edit of its field that has the keyboard, for the rebuilt window
        /// of the same <paramref name="owner"/> (a KodeView's module). Nothing when none of its fields has the keyboard. Allocates only
        /// when one has.
        /// </summary>
        public bool RememberFocus(object owner)
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;
            if (selected == null || Root == null || !selected.transform.IsChildOf(Root.transform)) return false;
            var field = selected.GetComponent<FieldFocus>();
            return field != null && field.Remember(owner);
        }

        /// <summary>
        /// Before this window is destroyed for good (closed, its vessel switched, its module recreated): ends the edit of its field
        /// that has the keyboard now, as a focus loss does, while the field's view and module are still there. Nothing when none
        /// of its fields has the keyboard.
        /// </summary>
        public bool EndFocusedEdit()
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;
            if (selected == null || Root == null || !selected.transform.IsChildOf(Root.transform) || events.alreadySelecting) return false;
            events.SetSelectedGameObject(null);
            return true;
        }

        /// <summary>
        /// After this window was built for <paramref name="owner"/>: gives the keyboard back to the field whose edit was remembered
        /// for it, with the edit. Costs one check when nothing waits for this window.
        /// </summary>
        public Headless.FocusOutcome RecoverFocus(object owner)
        {
            if (!Focus.Waits(owner))
            {
                Focus.Expire();
                return Headless.FocusOutcome.None;
            }
            return Focus.Recover(owner, Root.GetComponentsInChildren<FieldFocus>(true));
        }
    }
}
