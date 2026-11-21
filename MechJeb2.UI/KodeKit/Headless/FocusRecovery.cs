#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>A text field as <see cref="FocusRecovery"/> sees it: a name, whether it can take the keyboard, and its edit.</summary>
    public interface IFocusField
    {
        /// <summary>
        /// The name the same field has in a rebuilt window. KodeKit: its stable name where it has one (a view's name for it, such as
        /// a custom window item's id, or its label's localization key), else its kind and its place in the window's build (the lazy
        /// panel or section it is in and its order there, which don't change with the language or with the order panels are first
        /// shown in). Null: none, and the field is not remembered.
        /// </summary>
        string FocusKey { get; }

        /// <summary>
        /// Something of the field besides its name that the same field has in the rebuilt window (KodeKit: its value's text, for a
        /// field named by its place): a field found by the name whose check differs is another field at that place, and the edit is
        /// not handed to it. Null: nothing to compare, the name alone decides (a stable name).
        /// </summary>
        string FocusCheck { get; }

        /// <summary>Whether the field can take the keyboard now (shown and interactable).</summary>
        bool CanFocus { get; }

        /// <summary>
        /// The field's edit is remembered: it holds back the actions of an edit's end (a view's commit on focus loss) until
        /// <see cref="Release"/> says whether the edit goes on elsewhere or ended. Its window is destroyed after this, and the field may
        /// end its edit then: the held actions don't run.
        /// </summary>
        void Hold();

        /// <summary>Takes the keyboard and carries on <paramref name="edit"/>: its text, caret and selection, its text at focus, its commit.</summary>
        void TakeOver(FocusSnapshot edit);

        /// <summary>What becomes of the actions <see cref="Hold"/> held back (called once for each hold).</summary>
        void Release(FocusRelease how);
    }

    /// <summary>What a held field does with the actions of its edit's end.</summary>
    public enum FocusRelease
    {
        /// <summary>The edit goes on in the rebuilt window's field: they are dropped (that field runs its own when the edit ends).</summary>
        HandedOver,

        /// <summary>The edit ended (its field is gone, or its window wasn't rebuilt): they run now, as at a focus loss.</summary>
        Ended,

        /// <summary>The field is still there with the keyboard (its window wasn't rebuilt): they are put back.</summary>
        Kept
    }

    /// <summary>A field's edit, taken before its window is rebuilt.</summary>
    [NotAWidget("data: a field's edit kept across a rebuild")]
    public sealed class FocusSnapshot
    {
        /// <summary>The window, as the host names it across rebuilds (KodeKit: the window's owner, its MechJeb module).</summary>
        public object Window { get; internal set; }

        /// <summary>The field's <see cref="IFocusField.FocusKey"/>.</summary>
        public string Field { get; internal set; }

        /// <summary>The field's <see cref="IFocusField.FocusCheck"/> when its edit was taken (null: none).</summary>
        public string Check { get; internal set; }

        /// <summary>The text being edited.</summary>
        public string Text { get; internal set; }

        /// <summary>The text the field had when it took the keyboard (what Escape puts back); null: <see cref="Text"/>.</summary>
        public string Original { get; internal set; }

        /// <summary>The selection's fixed end and the caret (its moving end), in characters, within the text.</summary>
        public int Anchor { get; internal set; }

        public int Caret { get; internal set; }

        /// <summary>The edit's commit, still open (its group); null when the edit had changed nothing yet.</summary>
        public Commit Session { get; internal set; }

        /// <summary>The field the edit was taken from.</summary>
        public IFocusField From { get; internal set; }

        /// <summary>The tick it was taken in.</summary>
        public int Tick { get; internal set; }
    }

    /// <summary>What <see cref="FocusRecovery.Recover"/> did.</summary>
    public enum FocusOutcome
    {
        /// <summary>Nothing remembered for this window.</summary>
        None,

        /// <summary>The same field took the keyboard with the edit.</summary>
        Recovered,

        /// <summary>The window has no such field, or it can't take the keyboard: the edit ended and nothing has the keyboard.</summary>
        Gone,

        /// <summary>Two fields have the name: none is guessed, as for <see cref="Gone"/>.</summary>
        Ambiguous,

        /// <summary>
        /// The field of that name is another one (its <see cref="IFocusField.FocusCheck"/> differs: a field added or removed before
        /// it moved the places): no field gets the keyboard, as for <see cref="Gone"/>.
        /// </summary>
        Mismatched,

        /// <summary>The window wasn't rebuilt in time (closed, its vessel switched): the edit was dropped.</summary>
        Expired
    }

    /// <summary>
    /// A text field being edited keeps the keyboard when its window is rebuilt (ui-sdk's focus recovery): the window remembers the
    /// field before it is destroyed and the rebuilt window gives the keyboard back to the same field, with its text, caret and
    /// selection, and its edit's commit.
    /// <list type="bullet">
    /// <item>One edit at a time: only one field has the keyboard, so <see cref="Remember"/> replaces what was remembered.</item>
    /// <item>A field without a name (<see cref="IFocusField.FocusKey"/> null) is not remembered: it can't be found again safely.</item>
    /// <item>The edit belongs to its window (compared with <c>Equals</c>): another window's rebuild leaves it remembered.</item>
    /// <item>The rebuilt window's field with the same name takes the keyboard, the text (as typed, also when the model holds
    /// another), the caret and selection (kept within the text), and the text at focus, so Escape still puts back the text from
    /// before the edit.</item>
    /// <item>A remembered field holds back the actions of its edit's end (<see cref="IFocusField.Hold"/>) while its window is
    /// destroyed and rebuilt, and is released once (<see cref="IFocusField.Release"/>): handed over when the edit goes on in the
    /// rebuilt field (the actions then run once, when the edit really ends there: Enter, Escape, focus loss), ended when it doesn't
    /// (the actions run then, as at a focus loss), kept when the field is still there.</item>
    /// <item>The edit's commit goes on: the new field's group carries on the old commit (<see cref="CommitTarget.ContinueGroup"/>),
    /// so the edit is one commit from its focus to its end, whose old value is the value from before the edit.</item>
    /// <item>A field found by its name whose check is neither the edited field's (<see cref="IFocusField.FocusCheck"/>) nor its text at
    /// focus (a view that keeps the typed text apart from its model until the edit ends) is another field (another value's field now
    /// at that place, a field added before it): <see cref="FocusOutcome.Mismatched"/>, released as ended.</item>
    /// <item>No field of that name, two of them, or one that can't take the keyboard (hidden, disabled): no field gets the keyboard.
    /// Focus doesn't move to a neighbour: a field with the keyboard holds KSP's keyboard lock, and a player who didn't click it
    /// would find the flight keys gone. The old field's edit ends as a focus loss does (released as ended).</item>
    /// <item>An edit not recovered within <see cref="MaxAge"/> ticks is dropped (released as ended): its window was closed rather
    /// than rebuilt. So is one replaced by another field's, or forgotten.</item>
    /// <item>The same field, still there (its window not rebuilt), is left as it is.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: gives a rebuilt window's field the keyboard back")]
    public sealed class FocusRecovery
    {
        readonly Func<int> clock;

        /// <param name="clock">The tick (KodeKit: the frame number).</param>
        /// <param name="maxAge">How many ticks an edit waits for its window: a rebuild builds the window in the tick it destroyed it.</param>
        public FocusRecovery(Func<int> clock, int maxAge = 1)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            MaxAge = Math.Max(0, maxAge);
        }

        public int MaxAge { get; }

        /// <summary>The edit waiting for its window (null: none).</summary>
        public FocusSnapshot Pending { get; private set; }

        /// <summary>What the last <see cref="Recover"/> that found the edit's window did (for tests and logs).</summary>
        public FocusOutcome LastOutcome { get; private set; }

        /// <summary>
        /// Remembers <paramref name="field"/>'s edit before <paramref name="window"/> is rebuilt. False (and nothing remembered) without a
        /// window or a field name. <paramref name="session"/> is kept only while it is open.
        /// </summary>
        public bool Remember(object window, IFocusField field, string text, string original, int anchor, int caret, Commit session)
        {
            // the same field remembered again (its window destroyed twice) is held already
            bool held = Pending != null && field != null && ReferenceEquals(Pending.From, field);
            string key = field?.FocusKey;
            if (window == null || string.IsNullOrEmpty(key))
            {
                Drop(FocusRelease.Ended);
                return false;
            }
            if (!held)
            {
                Drop(FocusRelease.Ended);
                field.Hold();
            }
            text = text ?? "";
            Pending = new FocusSnapshot
            {
                Window = window,
                Field = key,
                Check = field.FocusCheck,
                Text = text,
                Original = original,
                Anchor = Clamp(anchor, text.Length),
                Caret = Clamp(caret, text.Length),
                Session = session != null && session.Open ? session : null,
                From = field,
                Tick = clock()
            };
            return true;
        }

        static int Clamp(int i, int length) => i < 0 ? 0 : i > length ? length : i;

        // the field found by the name shows what the edited one showed: the edit's check, or the text at focus (a view that keeps
        // the typed text apart from its model until the edit ends: its rebuilt field shows the model, still the text at focus)
        static bool SameField(IFocusField found, FocusSnapshot s)
        {
            if (s.Check == null) return true;
            string check = found.FocusCheck;
            return check == s.Check || (s.Original != null && check == s.Original);
        }

        // lets the remembered edit go, its field released as said
        void Drop(FocusRelease how)
        {
            FocusSnapshot s = Pending;
            Pending = null;
            s?.From?.Release(how);
        }

        /// <summary>Whether an edit waits for <paramref name="window"/> (cheap: a build checks this before it looks for fields).</summary>
        public bool Waits(object window) => Pending != null && Equals(Pending.Window, window);

        /// <summary>Drops an edit that waited too long. True when one was dropped.</summary>
        public bool Expire()
        {
            if (Pending == null || clock() - Pending.Tick <= MaxAge) return false;
            Drop(FocusRelease.Ended);
            LastOutcome = FocusOutcome.Expired;
            return true;
        }

        /// <summary>Forgets the edit (the host goes away): its field is released as ended.</summary>
        public void Forget() => Drop(FocusRelease.Ended);

        /// <summary>
        /// <paramref name="window"/> was built: gives the keyboard back to its field of the remembered name, if the edit is this window's.
        /// </summary>
        public FocusOutcome Recover(object window, IReadOnlyList<IFocusField> fields)
        {
            if (Expire()) return FocusOutcome.Expired;
            if (!Waits(window)) return FocusOutcome.None;
            FocusSnapshot s = Pending;
            Pending = null;
            IFocusField found = null;
            int matches = 0;
            if (fields != null)
                for (int i = 0; i < fields.Count; i++)
                {
                    IFocusField f = fields[i];
                    if (f == null || f.FocusKey != s.Field) continue;
                    matches++;
                    found = f;
                }
            FocusOutcome outcome;
            if (matches > 1) outcome = FocusOutcome.Ambiguous;
            else if (found == null || !found.CanFocus) outcome = FocusOutcome.Gone;
            else if (!ReferenceEquals(found, s.From) && !SameField(found, s)) outcome = FocusOutcome.Mismatched;
            else outcome = FocusOutcome.Recovered;
            if (outcome != FocusOutcome.Recovered) s.From?.Release(FocusRelease.Ended);
            else if (ReferenceEquals(found, s.From)) found.Release(FocusRelease.Kept);
            else
            {
                s.From?.Release(FocusRelease.HandedOver);
                found.TakeOver(s);
            }
            LastOutcome = outcome;
            return outcome;
        }
    }
}
