#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>How a dialog ended (None: it hasn't).</summary>
    public enum DialogResult { None, Confirmed, Cancelled, Other, Dismissed }

    /// <summary>The places the keyboard focus can be in a dialog: its text input (a prompt) and its buttons.</summary>
    public enum DialogStop { Input, Confirm, Other, Cancel }

    /// <summary>
    /// A modal dialog without Unity: a question with a confirm button, a cancel button and an optional third one, or a prompt with a
    /// text input (a rename). KodeKit draws it (<c>KodeKit.Dialogs</c>) over every window, on its own keyboard scope; a
    /// <see cref="DialogQueue"/> shows one at a time.
    /// <list type="bullet">
    /// <item>A dialog ends once: the first of <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Press"/> on the third button or
    /// <see cref="Dismiss"/> sets <see cref="Result"/> and <see cref="ResultSource"/>, calls its action (<see cref="OnConfirm"/>,
    /// <see cref="OnCancel"/>, <see cref="OnOther"/>; a dismissed dialog calls none), then <see cref="Closed"/>, then lets its queue show
    /// the next dialog (also when the action threw); any later call does nothing and returns false. A dialog shown from its action
    /// waits for it.</item>
    /// <item>Dismissed is the dialog ending without an answer: its owner went (<see cref="OwnerAlive"/> false at a <see cref="Tick"/>), the
    /// scene changed, or the program closed it. The action is not taken.</item>
    /// <item>The buttons in order are Confirm, the third (when <see cref="OtherText"/> is set), Cancel; a prompt's input comes first.
    /// These are the focus stops (<see cref="Stops"/>). Tab and Shift+Tab move the focus through them and wrap (the focus stays in the
    /// dialog); Left and Right move between the buttons only.</item>
    /// <item>The focus starts on the input for a prompt, else on <see cref="Default"/>: Confirm, or Cancel for a destructive dialog unless
    /// the default is set, so a stray Enter doesn't destroy anything.</item>
    /// <item>Enter presses the focused stop; Enter on the input confirms. Escape cancels, whatever has the focus. The host keeps the
    /// input's keyboard focus with the model's: the field is focused while <see cref="Focused"/> is the input, and Tab in it moves on.</item>
    /// <item>An exception thrown by the action goes to <see cref="ActionFailed"/> when it is set (KodeKit logs it with the title), else on
    /// to the caller; the dialog has ended either way.</item>
    /// <item>A prompt can't be confirmed while its input is not valid (<see cref="Valid"/>): Confirm then returns false and the dialog
    /// stays open.</item>
    /// <item><see cref="Version"/> changes whenever something drawn changes (the focus, the input, its validity, the result).</item>
    /// </list>
    /// </summary>
    public sealed class DialogModel : IDescribeParts
    {
        string label;

        /// <summary>What the dialog is called, for <see cref="IDescribeParts"/>: its title unless set.</summary>
        public string Label
        {
            get => label ?? Title;
            set => label = value;
        }

        static readonly PartKey[] InputKeys = { PartKey.Enter, PartKey.Escape, PartKey.Tab };
        static readonly PartKey[] ButtonKeys = { PartKey.Enter, PartKey.Escape, PartKey.Tab, PartKey.Left, PartKey.Right };

        /// <summary>The dialog, its prompt's text box (invalid while its text isn't), and its buttons in focus order (the focused stop focused; Confirm disabled while it can't confirm).</summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription { Name = "dialog", Role = PartRole.Dialog, Label = Label });
            IReadOnlyList<DialogStop> all = Stops;
            for (int i = 0; i < all.Count; i++)
            {
                DialogStop stop = all[i];
                PartFlags f = stop == Focused ? PartFlags.Focus : PartFlags.None;
                if (stop == DialogStop.Input)
                {
                    if (!Valid) f |= PartFlags.Invalid;
                    into.Add(new PartDescription
                    {
                        Name = "input", Role = PartRole.TextBox, Label = string.IsNullOrEmpty(Message) ? Label : Message, Flags = f, Focusable = true,
                        Keys = InputKeys
                    });
                    continue;
                }
                if (stop == DialogStop.Confirm && !CanConfirm) f |= PartFlags.Disabled;
                string text = stop == DialogStop.Confirm ? ConfirmText : stop == DialogStop.Other ? OtherText : CancelText;
                into.Add(new PartDescription
                {
                    Name = stop == DialogStop.Confirm ? "confirm" : stop == DialogStop.Other ? "other" : "cancel", Role = PartRole.Button, Label = text,
                    Flags = f, Focusable = true, Keys = ButtonKeys
                });
            }
        }

        public DialogModel(string title, string message)
        {
            Title   = title;
            Message = message;
        }

        public string Title   { get; }
        public string Message { get; }

        /// <summary>The confirm button's text (KodeKit draws a '#key' localized).</summary>
        public string ConfirmText = "OK";

        public string CancelText = "Cancel";

        string otherText;

        /// <summary>The third button's text; null: no third button.</summary>
        public string OtherText
        {
            get => otherText;
            set
            {
                otherText = value;
                stops     = null;
                Version++;
            }
        }

        /// <summary>The confirm button destroys something: drawn in the danger colour, and Cancel is the default.</summary>
        public bool Destructive;

        DialogStop? defaultStop;

        /// <summary>The button the focus starts on (the input first for a prompt): Confirm, or Cancel for a destructive dialog.</summary>
        public DialogStop Default
        {
            get => defaultStop ?? (Destructive ? DialogStop.Cancel : DialogStop.Confirm);
            set => defaultStop = value;
        }

        /// <summary>Who opened the dialog (a window's button); with <see cref="Title"/> what tells two dialogs apart.</summary>
        public object Owner;

        /// <summary>Whether the owner is still there (null: always); a dialog whose owner went is dismissed.</summary>
        public Func<bool> OwnerAlive;

        public Action OnConfirm;
        public Action OnCancel;
        public Action OnOther;

        /// <summary>After the dialog ended, however it did (after its action).</summary>
        public event Action<DialogModel> Closed;

        /// <summary>
        /// Gets an exception thrown by the action, which is then not thrown on (null: it is thrown on, after the cleanup). Either way the
        /// dialog has ended, <see cref="Closed"/> runs and the queue moves on.
        /// </summary>
        public event Action<DialogModel, Exception> ActionFailed;

        // ---- the prompt ----

        Func<string, bool> valid;
        string input;

        /// <summary>Whether the dialog has a text input (<see cref="MakePrompt"/>).</summary>
        public bool HasInput { get; private set; }

        /// <summary>Makes the dialog a prompt: a text input starting at <paramref name="initial"/>, valid when <paramref name="isValid"/> says so (null: always).</summary>
        public DialogModel MakePrompt(string initial, Func<string, bool> isValid = null)
        {
            HasInput = true;
            input    = initial ?? "";
            valid    = isValid;
            stops    = null;
            focused  = DialogStop.Input;
            Version++;
            return this;
        }

        /// <summary>The prompt's text.</summary>
        public string Input
        {
            get => input;
            set
            {
                value = value ?? "";
                if (value == input) return;
                input = value;
                Version++;
            }
        }

        /// <summary>Whether the prompt's text is valid (true for a dialog without input).</summary>
        public bool Valid => !HasInput || valid == null || valid(input);

        /// <summary>Whether <see cref="Confirm"/> would end the dialog now.</summary>
        public bool CanConfirm => Result == DialogResult.None && Valid;

        // ---- focus ----

        List<DialogStop> stops;
        DialogStop?      focused;

        /// <summary>The focus stops in order: the input (a prompt), Confirm, the third button, Cancel.</summary>
        public IReadOnlyList<DialogStop> Stops
        {
            get
            {
                if (stops != null) return stops;
                stops = new List<DialogStop>(4);
                if (HasInput) stops.Add(DialogStop.Input);
                stops.Add(DialogStop.Confirm);
                if (OtherText != null) stops.Add(DialogStop.Other);
                stops.Add(DialogStop.Cancel);
                return stops;
            }
        }

        /// <summary>The stop with the keyboard focus.</summary>
        public DialogStop Focused
        {
            get
            {
                if (focused == null || !Has(focused.Value)) focused = HasInput ? DialogStop.Input : Has(Default) ? Default : DialogStop.Confirm;
                return focused.Value;
            }
        }

        /// <summary>Puts the focus on <paramref name="stop"/>, when the dialog has it.</summary>
        public bool Focus(DialogStop stop)
        {
            if (!Has(stop)) return false;
            if (Focused != stop)
            {
                focused = stop;
                Version++;
            }
            return true;
        }

        /// <summary>Moves the focus <paramref name="by"/> stops, wrapping; <paramref name="buttonsOnly"/> skips the input (Left and Right).</summary>
        public void MoveFocus(int by, bool buttonsOnly = false)
        {
            IReadOnlyList<DialogStop> all = Stops;
            int n = all.Count;
            int i = IndexOf(all, Focused);
            int step = by < 0 ? -1 : 1;
            for (int moved = 0; moved < Math.Abs(by); moved++)
            {
                for (int k = 0; k < n; k++)
                {
                    i = ((i + step) % n + n) % n;
                    if (!buttonsOnly || all[i] != DialogStop.Input) break;
                }
            }
            Focus(all[i]);
        }

        /// <summary>Whether the dialog has the stop (a third button, an input).</summary>
        public bool Has(DialogStop stop) => IndexOf(Stops, stop, -1) >= 0;

        static int IndexOf(IReadOnlyList<DialogStop> all, DialogStop s, int none = 0)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i] == s)
                    return i;
            return none;
        }

        // ---- the answer ----

        /// <summary>How the dialog ended; None while it is open or waiting.</summary>
        public DialogResult Result { get; private set; }

        /// <summary>Who ended it (a click, a key, an agent, the program).</summary>
        public ChangeSource ResultSource { get; private set; }

        /// <summary>A number that changes whenever something drawn changes.</summary>
        public int Version { get; private set; }

        /// <summary>Whether a <see cref="DialogQueue"/> shows it now.</summary>
        public bool IsShown { get; internal set; }

        internal DialogQueue Queue;

        /// <summary>Confirms (the action runs), unless the dialog ended or the prompt's input is not valid.</summary>
        public bool Confirm(ChangeSource source = ChangeSource.Program) => CanConfirm && End(DialogResult.Confirmed, source);

        public bool Cancel(ChangeSource source = ChangeSource.Program) => End(DialogResult.Cancelled, source);

        /// <summary>Ends the dialog without an answer: no action runs.</summary>
        public bool Dismiss(ChangeSource source = ChangeSource.Program) => End(DialogResult.Dismissed, source);

        /// <summary>Presses a stop: a button's answer, or Confirm for the input (Enter in it).</summary>
        public bool Press(DialogStop stop, ChangeSource source)
        {
            if (!Has(stop)) return false;
            switch (stop)
            {
                case DialogStop.Input:
                case DialogStop.Confirm:
                    return Confirm(source);
                case DialogStop.Other:
                    return End(DialogResult.Other, source);
                default:
                    return Cancel(source);
            }
        }

        /// <summary>A key while the dialog has the keyboard: Enter, Escape, Tab (Shift: back), Left, Right. Returns whether it acted.</summary>
        public bool Key(ScopeKeys key, bool shift = false, ChangeSource source = ChangeSource.Keyboard)
        {
            if (Result != DialogResult.None) return false;
            switch (key)
            {
                case ScopeKeys.Enter:
                    return Press(Focused, source);
                case ScopeKeys.Escape:
                    return Cancel(source);
                case ScopeKeys.Tab:
                    MoveFocus(shift ? -1 : 1);
                    return true;
                case ScopeKeys.Left:
                    MoveFocus(-1, true);
                    return true;
                case ScopeKeys.Right:
                    MoveFocus(1, true);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Once a frame while shown or waiting: a dialog whose owner went is dismissed. Returns whether it was.</summary>
        public bool Tick() => Result == DialogResult.None && OwnerAlive != null && !OwnerAlive() && Dismiss();

        bool End(DialogResult result, ChangeSource source)
        {
            if (Result != DialogResult.None) return false;
            Result       = result;
            ResultSource = source;
            Version++;
            try
            {
                switch (result)
                {
                    case DialogResult.Confirmed:
                        OnConfirm?.Invoke();
                        break;
                    case DialogResult.Cancelled:
                        OnCancel?.Invoke();
                        break;
                    case DialogResult.Other:
                        OnOther?.Invoke();
                        break;
                }
            }
            catch (Exception e) when (ActionFailed != null)
            {
                // reported once, here; the dialog has ended and the cleanup below still runs
                ActionFailed(this, e);
            }
            finally
            {
                // the queue moves on even when the action or Closed threw
                try
                {
                    Closed?.Invoke(this);
                }
                finally
                {
                    Queue?.Ended(this);
                }
            }
            return true;
        }

        /// <summary>Whether two dialogs ask the same thing: the same owner and title (a second click on the same button).</summary>
        public bool SameAs(DialogModel other) => other != null && Equals(Owner, other.Owner) && Title == other.Title;

        public override string ToString() => $"dialog \"{Title}\" ({Result})";
    }

    /// <summary>
    /// The dialogs of a UI, one shown at a time.
    /// <list type="bullet">
    /// <item><see cref="Show"/> shows a dialog at once when none is shown, else it waits in order (at most <see cref="MaxPending"/>).</item>
    /// <item>A dialog that asks what one shown or waiting already asks (<see cref="DialogModel.SameAs"/>), or one already shown or ended, is
    /// refused: <see cref="Show"/> returns false and nothing changes.</item>
    /// <item>When the shown dialog ends, the next waiting one is shown; one that ended while it waited (dismissed, its owner gone) is
    /// skipped.</item>
    /// <item><see cref="Tick"/> dismisses the dialogs, shown or waiting, whose owner went; <see cref="DismissAll"/> ends every one (a scene
    /// change), and refuses any dialog shown while it runs (from a <see cref="DialogModel.Closed"/> callback).</item>
    /// <item><see cref="Opened"/> is called when a dialog is shown, <see cref="Hidden"/> when the shown one ended (before the next opens).</item>
    /// </list>
    /// </summary>
    [NotAWidget("a queue that shows one DialogModel (which describes its parts) at a time")]
    public sealed class DialogQueue
    {
        public const int MaxPending = 8;

        readonly List<DialogModel> pending = new List<DialogModel>();

        /// <summary>The dialog shown (null: none).</summary>
        public DialogModel Current { get; private set; }

        /// <summary>The dialogs waiting, first to show first.</summary>
        public IReadOnlyList<DialogModel> Pending => pending;

        public event Action<DialogModel> Opened;
        public event Action<DialogModel> Hidden;

        /// <summary>Shows <paramref name="d"/> now or after the dialogs before it. False: refused (see the rules).</summary>
        public bool Show(DialogModel d)
        {
            if (dismissing || d == null || d.Result != DialogResult.None || d.Queue != null) return false;
            if (d.SameAs(Current)) return false;
            for (int i = 0; i < pending.Count; i++)
                if (d.SameAs(pending[i]))
                    return false;
            if (Current != null && pending.Count >= MaxPending) return false;
            d.Queue = this;
            if (Current == null) Open(d);
            else pending.Add(d);
            return true;
        }

        void Open(DialogModel d)
        {
            Current   = d;
            d.IsShown = true;
            Opened?.Invoke(d);
        }

        internal void Ended(DialogModel d)
        {
            if (d != Current)
            {
                pending.Remove(d);
                return;
            }
            d.IsShown = false;
            Current   = null;
            Hidden?.Invoke(d);
            while (pending.Count > 0)
            {
                DialogModel next = pending[0];
                pending.RemoveAt(0);
                if (next.Result != DialogResult.None) continue;
                // its owner may have gone while it waited
                if (next.Tick()) continue;
                Open(next);
                return;
            }
        }

        /// <summary>Once a frame while any dialog is shown or waiting: dismisses those whose owner went.</summary>
        public void Tick()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
                if (i < pending.Count)
                    pending[i].Tick();
            Current?.Tick();
        }

        /// <summary>Whether a dialog is shown or waiting.</summary>
        public bool Busy => Current != null || pending.Count > 0;

        /// <summary>Dismisses every dialog, the waiting ones first (none of them is shown next).</summary>
        public void DismissAll(ChangeSource source = ChangeSource.Program)
        {
            // a Closed callback can't show a new dialog meanwhile: it would outlive the dismissal
            dismissing = true;
            try
            {
                while (pending.Count > 0) pending[pending.Count - 1].Dismiss(source);
                Current?.Dismiss(source);
            }
            finally
            {
                dismissing = false;
            }
        }

        bool dismissing;
    }
}
