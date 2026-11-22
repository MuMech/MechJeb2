#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>The keys a stepper understands, as its host reports them.</summary>
    public enum StepperKey { Up, Down, PageUp, PageDown }

    /// <summary>The modifier keys held, as the stepper's host reports them (either Alt is <see cref="Alt"/>).</summary>
    [Flags]
    public enum StepperModifiers { None = 0, LeftShift = 1, RightShift = 2, Alt = 4 }

    /// <summary>
    /// A value with − and + and a text field, without Unity: the behaviour of KodeKit's <c>Stepper</c>. The model (a MechJeb
    /// Editable) owns the value, its text and its parsing; this decides what a nudge or a key does.
    /// <list type="bullet">
    /// <item>A nudge adds ±step, read when it happens (the step can be a separate choice, the node editor's step bar); ×10
    /// with the big modifier.</item>
    /// <item>On − and +, the big modifier is Right Shift or Alt. Left Shift is not: it is KSP's throttle up, and a pointer over
    /// a window leaves flight keys live, so a ×10 on Left Shift would raise the throttle too.</item>
    /// <item>Up/Down nudge by one step, ×10 with either Shift or Alt: the field has the keyboard then, and its lock keeps the
    /// keys from KSP. PageUp/PageDown nudge by ten whatever is held.</item>
    /// <item>Typed text goes to the model as it is: the model parses it, and <see cref="Valid"/> tells whether it did.</item>
    /// <item><see cref="Allow"/> can refuse a change; each change that reaches the model records its source, and setters that take
    /// a source get the same one (the commit agrees with <see cref="LastSource"/>).</item>
    /// </list>
    /// </summary>
    public sealed class StepperModel : IDescribeParts
    {
        readonly Func<string> text;
        readonly Action<string, ChangeSource> setText;
        readonly Action<double, ChangeSource> add;
        readonly Func<double> step;
        readonly Func<bool> valid;

        public StepperModel(Func<string> text, Action<string> setText, Action<double> add, Func<double> step, Func<bool> valid = null)
            : this(text, Plain(setText, nameof(setText)), Plain(add, nameof(add)), step, valid)
        {
        }

        /// <summary>A stepper whose setters also take the change's source (KodeKit passes its commit target's).</summary>
        public StepperModel(Func<string> text, Action<string, ChangeSource> setText, Action<double, ChangeSource> add, Func<double> step,
            Func<bool> valid = null)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.setText = setText ?? throw new ArgumentNullException(nameof(setText));
            this.add = add ?? throw new ArgumentNullException(nameof(add));
            this.step = step ?? throw new ArgumentNullException(nameof(step));
            this.valid = valid;
        }

        static Action<TV, ChangeSource> Plain<TV>(Action<TV> set, string name)
        {
            if (set == null) throw new ArgumentNullException(name);
            return (v, source) => set(v);
        }

        /// <summary>When set, asked before each change with the delta (NaN for typed text) and the source; false refuses it.</summary>
        public Func<double, ChangeSource, bool> Allow;

        /// <summary>The source of the last change that reached the model.</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>What the value is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        public string Text => text();

        /// <summary>Whether the model parsed its text (true when the model can't tell).</summary>
        public bool Valid => valid == null || valid();

        /// <summary>Whether the modifiers held make a press of − or + a ×10 step: Right Shift or Alt, never Left Shift alone.</summary>
        public static bool BigOnButton(StepperModifiers held) => (held & (StepperModifiers.RightShift | StepperModifiers.Alt)) != 0;

        /// <summary>Whether the modifiers held make Up/Down in the focused field a ×10 step: either Shift or Alt.</summary>
        public static bool BigOnKey(StepperModifiers held) => held != StepperModifiers.None;

        /// <summary>A press of − (<paramref name="sign"/> &lt; 0) or + with the modifiers held. True when the model was changed.</summary>
        public bool Press(int sign, StepperModifiers held, ChangeSource source) => Nudge(sign, BigOnButton(held), source);

        /// <summary>A key on the focused field with the modifiers held. True when the stepper handled it.</summary>
        public bool Key(StepperKey key, StepperModifiers held) => Key(key, BigOnKey(held));

        /// <summary>The delta a nudge applies now: ±step, ×10 when <paramref name="big"/>.</summary>
        public double Delta(int sign, bool big) => Math.Sign(sign) * step() * (big ? 10 : 1);

        /// <summary>Adds ±step (×10 when <paramref name="big"/>). True when the model was changed.</summary>
        public bool Nudge(int sign, bool big, ChangeSource source)
        {
            if (sign == 0) return false;
            double d = Delta(sign, big);
            if (d == 0 || double.IsNaN(d) || double.IsInfinity(d)) return false;
            if (Allow != null && !Allow(d, source)) return false;
            LastSource = source;
            add(d, source);
            return true;
        }

        /// <summary>Text typed into the field: given to the model to parse. True when the model took it.</summary>
        public bool Type(string s, ChangeSource source)
        {
            if (Allow != null && !Allow(double.NaN, source)) return false;
            LastSource = source;
            setText(s ?? "", source);
            return true;
        }

        /// <summary>A key on the field. True when the stepper handled it.</summary>
        public bool Key(StepperKey key, bool big)
        {
            switch (key)
            {
                case StepperKey.Up: return Nudge(1, big, ChangeSource.Keyboard) || true;
                case StepperKey.Down: return Nudge(-1, big, ChangeSource.Keyboard) || true;
                case StepperKey.PageUp: return Nudge(1, true, ChangeSource.Keyboard) || true;
                case StepperKey.PageDown: return Nudge(-1, true, ChangeSource.Keyboard) || true;
                default: return false;
            }
        }

        static readonly PartKey[] FieldKeys = { PartKey.Up, PartKey.Down, PartKey.PageUp, PartKey.PageDown };

        /// <summary>The − button, the field and the + button.</summary>
        public void Describe(List<PartDescription> into)
        {
            bool named = !string.IsNullOrEmpty(Label);
            into.Add(new PartDescription { Name = "dec", Role = PartRole.Button, Label = named ? "decrease " + Label : null });
            into.Add(new PartDescription
            {
                Name = "field",
                Role = PartRole.SpinButton,
                Label = Label,
                Flags = Valid ? PartFlags.None : PartFlags.Invalid,
                Focusable = true,
                Keys = FieldKeys
            });
            into.Add(new PartDescription { Name = "inc", Role = PartRole.Button, Label = named ? "increase " + Label : null });
        }
    }
}
