#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>How an edit of a text field ended, or why it didn't (<see cref="TextFieldModel.LastEnd"/>).</summary>
    public enum TextEditEnd
    {
        /// <summary>Nothing ended: no edit was open.</summary>
        None,

        /// <summary>The typed text was written to the value (at the end, once; or as typed).</summary>
        Committed,

        /// <summary>The text was the value's: nothing written.</summary>
        Unchanged,

        /// <summary>Escape (or the host) dropped the typed text: the value is what it was when the edit began.</summary>
        Cancelled,

        /// <summary>The typed text doesn't parse: nothing written. Enter keeps the edit open; a focus loss ends it.</summary>
        Refused,

        /// <summary>The value became read-only during the edit: the edit ended, nothing written.</summary>
        Disabled
    }

    /// <summary>When a text field's typing reaches its value.</summary>
    public enum TextCommit
    {
        /// <summary>Each typed text, at once (KodeKit's plain fields and the stepper's: MechJeb's editables parse as they are typed).</summary>
        AsTyped,

        /// <summary>Once, at Enter or a focus loss (the inline edit).</summary>
        AtEnd
    }

    /// <summary>
    /// A text field's edit, without Unity: the behaviour of KodeKit's fields (<c>EditFields.Field</c>, the stepper's field, the inline
    /// edit, a dialog's prompt), on a <see cref="ValueModel{T}"/> of the value's text. The value (a MechJeb editable) owns its text and
    /// its parsing; this holds the edit: when it opens, what is typed, when it reaches the value and how it ends.
    /// <list type="bullet">
    /// <item><see cref="Begin"/> (the field selected: a click, focus recovery's take-over, an agent) opens an edit whose
    /// <see cref="Draft"/> and <see cref="Original"/> start as the value's text, and the host's session (KodeKit's commit group: the
    /// edit is one commit). A read-only value opens no edit; an open edit is kept.</item>
    /// <item>Typing (<see cref="Type"/>) with <see cref="TextCommit.AsTyped"/> writes each typed text through the value model at once,
    /// in the session, and the draft becomes what the value kept (a setter that strips junk shows the cleaned text). Typing with no
    /// edit open (a host setting the text) is written too, as a commit of its own. With <see cref="TextCommit.AtEnd"/> only the draft
    /// changes (cleaned by the normaliser), and nothing is written while the edit is open.</item>
    /// <item><see cref="Submit"/> (Enter) and <see cref="Leave"/> (focus loss) end the edit. At the end, a valid, changed draft is
    /// written once; one equal to the value writes nothing (<see cref="TextEditEnd.Unchanged"/>); one that doesn't parse is
    /// <see cref="TextEditEnd.Refused"/>: Enter keeps the edit open, a focus loss ends it with the value as it was. As typed, both
    /// just end the edit: the value has the text already.</item>
    /// <item><see cref="Cancel"/> (Escape) ends the edit with the value as it was at <see cref="Begin"/>: at the end, nothing is
    /// written; as typed, the original text is written back once when the draft (typed, or shown by the host with
    /// <see cref="Shows"/>: a stepper's nudge) differs from it (in the session, so the edit's commit is
    /// from the original to the original). TMP's own Escape restore is not used: the model is the one that reverts.</item>
    /// <item>At the end, a write the value refuses (<see cref="ValueModel{T}.Allow"/>) ends the edit as
    /// <see cref="TextEditEnd.Refused"/>, with nothing written.</item>
    /// <item><see cref="Drop"/> ends an edit without writing anything (the field hidden before it had the keyboard).</item>
    /// <item><see cref="Sync"/>, once a frame: a value read-only now ends the edit (<see cref="TextEditEnd.Disabled"/>). At the end,
    /// a value that changed under a draft not typed in yet becomes the draft (the edit shows it, and Escape returns to it); under a
    /// typed draft it is kept and wins at Enter, and <see cref="Stale"/> says so. As typed, the open edit keeps the text typed and
    /// <see cref="Stale"/> says the value moved. With no edit open the shown text follows the value.</item>
    /// <item>Validity: at the end, the draft's parse, checked when the draft changes; as typed, the value's own check, read once a
    /// frame (the value parses what it was given).</item>
    /// <item><see cref="Shown"/> is what the field draws: the draft while an edit is open, else the value's text.</item>
    /// </list>
    /// </summary>
    public class TextFieldModel : IDescribeParts
    {
        /// <summary>What the value is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        static readonly PartKey[] EditKeys = { PartKey.Enter, PartKey.Escape };

        /// <summary>The text box: focusable (Enter and Escape), Focus while an edit is open, Invalid, Disabled.</summary>
        public virtual void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription
            {
                Name = "field", Role = PartRole.TextBox, Label = Label, Focusable = true, Keys = EditKeys,
                Flags = (Editing ? PartFlags.Focus : PartFlags.None) | (Valid ? PartFlags.None : PartFlags.Invalid) |
                        (Enabled ? PartFlags.None : PartFlags.Disabled)
            });
        }

        readonly Func<string, bool> parses;
        readonly Func<bool> valueValid;
        readonly Func<string, string> normalise;
        readonly Action beginSession, endSession;

        /// <summary>
        /// A field on the value read by <paramref name="text"/> and written by <paramref name="commit"/> (KodeKit's commit target),
        /// written <paramref name="mode"/>. Validity: <paramref name="parses"/> checks a draft at the end; <paramref name="valid"/> is the
        /// value's own check as typed (null: always valid). <paramref name="enabled"/>: editable while it holds (always when null).
        /// <paramref name="normalise"/> cleans a draft at the end as the value would keep it. <paramref name="beginSession"/> and
        /// <paramref name="endSession"/> open and close the host's group around an edit.
        /// </summary>
        public TextFieldModel(Func<string> text, Action<string, ChangeSource> commit, TextCommit mode, Func<string, bool> parses = null,
            Func<bool> valid = null, Func<bool> enabled = null, Func<string, string> normalise = null, Action beginSession = null,
            Action endSession = null)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (commit == null) throw new ArgumentNullException(nameof(commit));
            Mode = mode;
            Store = new ValueModel<string>(() => text() ?? "", commit, enabled);
            this.parses = parses;
            valueValid = valid;
            this.normalise = normalise;
            this.beginSession = beginSession;
            this.endSession = endSession;
            if (mode == TextCommit.AsTyped) Valid = valid == null || valid();
        }

        /// <summary>The value's text, its source and its refusals (<see cref="ValueModel{T}.Allow"/> refuses a write).</summary>
        public ValueModel<string> Store { get; }

        /// <summary>When the typing reaches the value.</summary>
        public TextCommit Mode { get; }

        /// <summary>Whether an edit is open.</summary>
        public bool Editing { get; private set; }

        /// <summary>The text being typed (null while no edit is open).</summary>
        public string Draft { get; private set; }

        /// <summary>The value's text the edit started from (or last followed): what Escape returns to.</summary>
        public string Original { get; private set; }

        /// <summary>Whether the text parses: the draft's (at the end) or the value's (as typed).</summary>
        public bool Valid { get; private set; } = true;

        /// <summary>Whether the text parses now: the value's own check called (as typed), or the draft's last check (at the end).</summary>
        public bool ParsesNow() => Mode == TextCommit.AsTyped ? valueValid == null || valueValid() : Valid;

        /// <summary>Whether the player typed during this edit (also when the draft is back to the text it started from).</summary>
        public bool Typed { get; private set; }

        /// <summary>Whether the value changed underneath a typed draft.</summary>
        public bool Stale { get; private set; }

        /// <summary>Whether the value can be edited now.</summary>
        public bool Enabled => Store.Enabled;

        /// <summary>The value's text now.</summary>
        public string Value => Store.Value;

        /// <summary>What the widget shows: the draft while editing, else the value's text.</summary>
        public string Shown => Editing ? Draft : Value;

        /// <summary>How the last edit ended.</summary>
        public TextEditEnd LastEnd { get; private set; }

        /// <summary>The source of the last write.</summary>
        public ChangeSource LastSource => Store.LastSource;

        /// <summary>Opens an edit with the value's text as the draft. False when one is open already (its draft is kept) or the value is read-only.</summary>
        public bool Begin()
        {
            if (Editing || !Enabled) return false;
            Editing = true;
            Stale = Typed = false;
            Original = Draft = Value;
            if (Mode == TextCommit.AtEnd) Valid = Check(Draft);
            beginSession?.Invoke();
            return true;
        }

        /// <summary>
        /// The text Escape returns to in the open edit (focus recovery: the text the edit had at focus in the window it came from).
        /// False when no edit is open.
        /// </summary>
        public bool Resume(string original)
        {
            if (!Editing || original == null) return false;
            Original = original;
            return true;
        }

        /// <summary>
        /// Text typed with <paramref name="source"/>: as typed, written at once (also with no edit open) and the draft becomes what the
        /// value kept; at the end, the new draft, cleaned by the normaliser. The host shows <see cref="Shown"/> when it differs from the
        /// text typed. False when nothing took the text (at the end with no edit open, or a write refused).
        /// </summary>
        public bool Type(string s, ChangeSource source)
        {
            s = s ?? "";
            if (Mode == TextCommit.AsTyped)
            {
                bool written = Store.Set(s, source);
                if (!Editing) return written;
                if (written) Typed = true;
                Draft = Value;
                return written;
            }
            if (!Editing) return false;
            if (normalise != null) s = normalise(s) ?? "";
            if (s == Draft) return true;
            Draft = s;
            Typed = true;
            Valid = Check(s);
            return true;
        }

        /// <summary>
        /// The host shows <paramref name="s"/> in the open edit as the value's text, without typing it (a stepper's Up/Down set the value
        /// and show its text): as typed, it becomes the draft, so Escape writes the original back. Nothing is written. False when no
        /// edit is open or the edit is written at the end (its draft is the player's).
        /// </summary>
        public bool Shows(string s)
        {
            if (!Editing || Mode != TextCommit.AsTyped) return false;
            Draft = s ?? "";
            Stale = false;
            return true;
        }

        /// <summary>Enter: ends the edit; at the end, writes a valid, changed draft once, and refuses an invalid one (the edit stays open).</summary>
        public TextEditEnd Submit(ChangeSource source) => Finish(source, true);

        /// <summary>Focus loss: ends the edit in every case; at the end, writes a valid, changed draft once.</summary>
        public TextEditEnd Leave(ChangeSource source) => Finish(source, false);

        /// <summary>Escape: ends the edit with the value as it began (as typed, the original written back once if typing changed it).</summary>
        public TextEditEnd Cancel(ChangeSource source)
        {
            if (!Editing) return TextEditEnd.None;
            if (Mode == TextCommit.AsTyped && Draft != Original) Store.Set(Original, source);
            return End(TextEditEnd.Cancelled);
        }

        /// <summary>Ends the edit without writing anything (the field hidden before it had the keyboard).</summary>
        public TextEditEnd Drop() => Editing ? End(TextEditEnd.Cancelled) : TextEditEnd.None;

        /// <summary>
        /// Once a frame: a value read-only now ends the edit (<see cref="TextEditEnd.Disabled"/>); at the end, a value that changed under
        /// a draft not typed yet becomes the draft; as typed, the value's check is read. True when the draft, the validity or the state
        /// changed (the host shows it), or with no edit open when the value's text changed.
        /// </summary>
        public bool Sync()
        {
            bool changed = Store.Sync();
            if (Mode == TextCommit.AsTyped)
            {
                bool v = valueValid == null || valueValid();
                if (v != Valid)
                {
                    Valid = v;
                    changed = true;
                }
            }
            if (!Editing) return changed;
            if (!Enabled)
            {
                End(TextEditEnd.Disabled);
                return true;
            }
            string now = Value;
            if (Mode == TextCommit.AsTyped)
            {
                if (now != Draft) Stale = true;
                return changed;
            }
            if (now == Original) return false;
            if (Typed)
            {
                Stale = true;
                return false;
            }
            Original = Draft = now;
            Valid = Check(now);
            return true;
        }

        TextEditEnd Finish(ChangeSource source, bool stayIfInvalid)
        {
            if (!Editing) return TextEditEnd.None;
            if (Mode == TextCommit.AsTyped) return End(Value != Original ? TextEditEnd.Committed : TextEditEnd.Unchanged);
            if (!Valid)
            {
                if (stayIfInvalid)
                {
                    LastEnd = TextEditEnd.Refused;
                    return TextEditEnd.Refused;
                }
                return End(TextEditEnd.Refused);
            }
            string draft = Draft;
            if (draft == Value) return End(TextEditEnd.Unchanged);
            // the edit is over before the write: a host that reads the shown text in the commit's observers sees the value. The
            // session closes after the write, so the write is the session's commit
            EndState(TextEditEnd.Committed);
            // refused by the value (read-only now, or its Allow): the edit is over with nothing written
            if (!Store.Set(draft, source)) LastEnd = TextEditEnd.Refused;
            endSession?.Invoke();
            return LastEnd;
        }

        TextEditEnd End(TextEditEnd how)
        {
            EndState(how);
            endSession?.Invoke();
            return how;
        }

        void EndState(TextEditEnd how)
        {
            Editing = false;
            Draft = null;
            Stale = Typed = false;
            if (Mode == TextCommit.AtEnd) Valid = true;
            LastEnd = how;
        }

        bool Check(string s) => parses == null || parses(s);
    }

    /// <summary>
    /// A read-out that turns into a text field when clicked, without Unity: the behaviour of KodeKit's <c>InlineEdit</c> (ui-sdk's
    /// <c>createInlineEdit</c>). It is a <see cref="TextFieldModel"/> written at the end (<see cref="TextCommit.AtEnd"/>): the same
    /// rules as every KodeKit field, with the read-out's state between edits.
    /// <list type="bullet">
    /// <item>Two states: the read-out shows the value's text; <see cref="TextFieldModel.Begin"/> (a click, the field taking the
    /// keyboard) opens an edit. A read-only value (<c>enabled</c> false) opens no edit.</item>
    /// <item>Typing changes the draft only; Enter and a focus loss write it once, Escape drops it, an invalid draft is refused
    /// (<see cref="TextFieldModel"/>).</item>
    /// <item>The value becoming read-only during the edit ends it without writing (<see cref="TextEditEnd.Disabled"/>).</item>
    /// <item>KodeKit draws it only on MechJeb's <c>IEditable</c>s.</item>
    /// </list>
    /// </summary>
    public sealed class InlineEditModel : TextFieldModel
    {
        /// <summary>
        /// An inline edit of the value read by <paramref name="text"/>, written by <paramref name="commit"/> (KodeKit's commit target),
        /// valid when <paramref name="parses"/> accepts the text (always when null), editable while <paramref name="enabled"/> holds
        /// (always when null). <paramref name="normalise"/> cleans a typed text as the value would keep it (MechJeb's editables throw
        /// junk characters away): the draft is the cleaned text, so the field shows what Enter would write.
        /// </summary>
        public InlineEditModel(Func<string> text, Action<string, ChangeSource> commit, Func<string, bool> parses = null, Func<bool> enabled = null,
            Func<string, string> normalise = null, Action beginSession = null, Action endSession = null)
            : base(text, commit, TextCommit.AtEnd, parses, null, enabled, normalise, beginSession, endSession)
        {
        }

        /// <summary>Text typed while the edit is open (the source is the write's, given at the end). False when no edit is open.</summary>
        public bool Type(string s) => Type(s, ChangeSource.Keyboard);

        /// <summary>Escape: ends the edit and drops the draft; nothing is written.</summary>
        public TextEditEnd Cancel() => Cancel(ChangeSource.Keyboard);

        static readonly PartKey[] ReadKeys = { PartKey.Enter, PartKey.Escape };

        /// <summary>The read-out (a button that opens the edit; disabled when read-only), or while editing the text box (invalid while the draft doesn't parse).</summary>
        public override void Describe(List<PartDescription> into)
        {
            if (!Editing)
            {
                into.Add(new PartDescription { Name = "readout", Role = PartRole.Button, Label = Label, Flags = Enabled ? PartFlags.None : PartFlags.Disabled });
                return;
            }
            into.Add(new PartDescription
            {
                Name = "field", Role = PartRole.TextBox, Label = Label, Focusable = true, Keys = ReadKeys,
                Flags = PartFlags.Focus | (Valid ? PartFlags.None : PartFlags.Invalid)
            });
        }
    }
}
