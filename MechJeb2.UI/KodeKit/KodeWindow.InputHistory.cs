using System;
using TMPro;
using UnityEngine;

namespace KodeKit
{
    /// <summary>
    /// A field's input history (<see cref="Headless.InputHistory"/>, opted in with <see cref="EditFields.History"/>): while the field
    /// has the keyboard, Up shows an older value of its key and Down a newer one, then the text typed; the value shown is typed into
    /// the field as a key would, so Enter commits it and Escape puts back the text from before the edit. The player's finished edits
    /// of the field are recorded under its key.
    /// <list type="bullet">
    /// <item>Only while the field has the keyboard, which holds KSP's keyboard lock: Up and Down never reach KSP's camera then, and
    /// without a focused field they are KSP's. The keys come from the field's keyboard scope (only while it is on top: not under a
    /// dialog). The component is enabled (its caret fix-up) only during the field's edit: no per-frame cost otherwise.</item>
    /// <item>While walking, the caret stays at the end of the value shown, also when a key has nothing new to show.</item>
    /// <item>Plain Up and Down: with Shift they keep TMP's selection keys, with Ctrl or Alt they do nothing.</item>
    /// <item>Not on a stepper's field, whose Up and Down nudge the value.</item>
    /// <item>Recorded: the field's settled commit of the player that changed the value (<see cref="Headless.InputHistory.Recordable"/>)
    /// and parses (the field's valid check).</item>
    /// </list>
    /// </summary>
    public class FieldHistory : MonoBehaviour
    {
        TMP_InputField input;
        Headless.CommitTarget target;
        Func<bool> valid;
        Headless.InputHistoryBrowser browser;
        bool placeCaret;

        /// <summary>The field's key in <see cref="KodeWindow.InputHistory"/>.</summary>
        public string Key { get; private set; }

        /// <summary>The field's walk through its list (for tests).</summary>
        public Headless.InputHistoryBrowser Browser => browser;

        internal void Init(TMP_InputField field, Headless.CommitTarget t, Func<bool> isValid, string key)
        {
            input = field;
            target = t;
            valid = isValid;
            Key = key;
            browser = new Headless.InputHistoryBrowser(KodeWindow.InputHistory, key);
            enabled = false;
            target.Path.Settled += OnSettled;
            field.GetComponent<FieldKeyboardLock>().Route(Headless.ScopeKeys.Up | Headless.ScopeKeys.Down, Walk);
            field.onSelect.AddListener(_ => Begin());
            field.onDeselect.AddListener(_ => End());
            field.onEndEdit.AddListener(_ => End());
        }

        void Begin()
        {
            browser.Reset();
            enabled = true;
        }

        void End()
        {
            browser.Reset();
            placeCaret = false;
            // a field destroyed with its window may end its edit afterwards (focus recovery's held actions)
            if (this != null) enabled = false;
        }

        // While focus recovery holds the field's edit (its window rebuilt), a commit it settles is kept back. Handed over, the edit
        // goes on in the rebuilt field, which records its real end: this field records nothing more (its deselection, after the
        // hand-over, settles the text typed so far). Ended, the kept commit is the edit's end and is recorded.
        bool holding, handedOver;
        Headless.Commit heldCommit;

        internal void Hold()
        {
            holding = true;
            heldCommit = null;
        }

        internal void Release(Headless.FocusRelease how)
        {
            Headless.Commit c = heldCommit;
            holding = false;
            heldCommit = null;
            if (how == Headless.FocusRelease.HandedOver) handedOver = true;
            else if (how == Headless.FocusRelease.Ended && c != null) KodeWindow.InputHistory.Record(Key, c);
        }

        void OnSettled(Headless.Commit c)
        {
            if (c.Target != target || handedOver || !Headless.InputHistory.Recordable(c) || (valid != null && !valid())) return;
            if (holding)
            {
                heldCommit = c;
                return;
            }
            KodeWindow.InputHistory.Record(Key, c);
        }

        void OnDestroy()
        {
            if (target != null) target.Path.Settled -= OnSettled;
        }

        // Up or Down, routed by the field's keyboard scope
        void Walk(Headless.ScopeKeys k)
        {
            if (this == null || input == null || !input.isFocused) return;
            bool up = k == Headless.ScopeKeys.Up, down = k == Headless.ScopeKeys.Down;
            if (!up && !down) return;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                return;
            // at the end of the list the key shows nothing new, and the caret stays at the end of the value shown (TMP would move it)
            if (!Step(up) && browser.Browsing) placeCaret = true;
        }

        /// <summary>
        /// What Up (<paramref name="older"/>) or Down does in the focused field (a host with no keys, such as the agent bridge, calls
        /// it): shows the next value. False, with nothing changed, when the field doesn't have the keyboard or there is no such value.
        /// The value goes through the field's own change handler, whose source is the player's keyboard unless a host's scope says
        /// otherwise: a host acting for an agent calls it inside <c>CommitPath.As(ChangeSource.Agent)</c>, or the edit it commits is
        /// recorded as the player's.
        /// </summary>
        public bool Step(bool older)
        {
            if (input == null || !input.isFocused) return false;
            string s = older ? browser.Older(input.text) : browser.Newer(input.text);
            if (s == null) return false;
            // typed as a key would: the field's own change handler gives it to the model, in the edit's commit, as Keyboard
            input.text = s;
            placeCaret = true;
            return true;
        }

        // TMP moves the caret on Up (to the start) and Down (to the end) in its own update, before or after this one's: the caret goes
        // to the end of the value shown once both ran
        void LateUpdate()
        {
            if (!placeCaret || input == null) return;
            placeCaret = false;
            int n = input.text.Length;
            input.selectionStringAnchorPosition = input.selectionStringFocusPosition = n;
            input.selectionAnchorPosition = input.selectionFocusPosition = n;
            input.ForceLabelUpdate();
        }
    }

    public partial class KodeWindow
    {
        /// <summary>
        /// The recent values of the fields opted in with <see cref="EditFields.History"/>, by key, for the whole game: a rebuilt window,
        /// another window or another scene offers the same lists. A host saves them (<see cref="Headless.InputHistory.Changed"/>).
        /// </summary>
        public static readonly Headless.InputHistory InputHistory = new Headless.InputHistory();
    }

    public static partial class EditFields
    {
        /// <summary>
        /// Gives <paramref name="field"/> (a KodeKit field, an inline edit's included) a name for its window that the view gives the
        /// same field in every build and language (a custom window item's id): its focus recovery finds it by that name wherever it
        /// moved in the build, and its target's <c>Id</c> is that name, for an observer. Returns the field.
        /// </summary>
        public static TMP_InputField Named(this TMP_InputField field, string name)
        {
            if (field == null || string.IsNullOrEmpty(name)) return field;
            var focus = field.GetComponent<FieldFocus>();
            if (focus == null) return field;
            focus.ViewName = name;
            if (focus.Target != null) focus.Target.Id = name;
            return field;
        }

        /// <summary>
        /// Opts <paramref name="field"/> (a KodeKit field) into the input history under <paramref name="key"/>: Up and Down while it
        /// has the keyboard show the recent values the player committed in fields of that key (<see cref="FieldHistory"/>). The key
        /// names what the field holds and is the same in every language and build ("planner.apoapsis"); fields of one quantity may
        /// share it. Not for a stepper's field (its Up and Down nudge): that field is returned unchanged. Returns the field.
        /// </summary>
        public static TMP_InputField History(this TMP_InputField field, string key)
        {
            if (field == null || string.IsNullOrEmpty(key) || field.GetComponent<FieldHistory>() != null) return field;
            var focus = field.GetComponent<FieldFocus>();
            if (focus == null || focus.Target == null || focus.Target.Kind == "Stepper") return field;
            field.gameObject.AddComponent<FieldHistory>().Init(field, focus.Target, focus.Valid, key);
            return field;
        }
    }
}
