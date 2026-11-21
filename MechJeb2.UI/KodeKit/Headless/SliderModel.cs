#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>
    /// A value along a range, moved by a drag, without Unity: the behaviour of KodeKit's <c>Slider</c>, on a <see cref="ValueModel{T}"/>.
    /// <list type="bullet">
    /// <item><see cref="Set"/> clamps the value to <see cref="Min"/>..<see cref="Max"/> and, with a <see cref="Step"/>, snaps it to the
    /// step grid from <see cref="Min"/>, then calls the setter through the value model: one setter call per move, with its source.
    /// A value that is not a number is refused.</item>
    /// <item>A drag is one gesture: <see cref="BeginDrag"/> (the press) opens the host's group (KodeKit's commit target, which takes in
    /// the move the press itself made), each move is a <see cref="Set"/>, <see cref="EndDrag"/> (the release, or the slider hidden)
    /// closes it, so the whole drag is one commit. Extra begins and ends do nothing. An agent drags the same way.</item>
    /// <item>Keys (<see cref="Key"/>) move by one step (a hundredth of the range without a step), ten with PageUp/PageDown, and jump
    /// with Home/End, each key one commit with the Keyboard source; only for a host that routes them through a keyboard scope:
    /// KodeKit routes none, and its slider takes no arrow keys from uGUI's navigation (arrows are KSP's camera keys).</item>
    /// <item><see cref="Shown"/> is what the handle shows, the value as last read.</item>
    /// </list>
    /// </summary>
    public sealed class SliderModel : IDescribeParts
    {
        readonly Action beginGroup, endGroup;

        /// <summary>
        /// A slider from <paramref name="min"/> to <paramref name="max"/> (swapped when given the wrong way round) on <paramref name="get"/>
        /// and <paramref name="set"/>, snapped to <paramref name="step"/> when it is above 0. <paramref name="beginGroup"/> and
        /// <paramref name="endGroup"/> make a drag one commit (null: none).
        /// </summary>
        public SliderModel(Func<float> get, Action<float, ChangeSource> set, float min, float max, float step = 0, Action beginGroup = null,
            Action endGroup = null, Func<bool> enabled = null)
        {
            if (float.IsNaN(min) || float.IsNaN(max)) throw new ArgumentOutOfRangeException(nameof(min));
            Min = Math.Min(min, max);
            Max = Math.Max(min, max);
            Step = step > 0 && !float.IsInfinity(step) ? step : 0;
            this.beginGroup = beginGroup;
            this.endGroup = endGroup;
            Value = new ValueModel<float>(get, set, enabled);
        }

        /// <summary>The value, its source and its refusals.</summary>
        public ValueModel<float> Value { get; }

        public float Min { get; }
        public float Max { get; }

        /// <summary>The step grid (0: none).</summary>
        public float Step { get; }

        /// <summary>What the slider is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The value drawn, as last read.</summary>
        public float Shown => Value.Shown;

        /// <summary>Whether a drag is open.</summary>
        public bool Dragging { get; private set; }

        /// <summary>The value <paramref name="v"/> becomes: clamped to the range and snapped to the step grid.</summary>
        public float Constrain(float v)
        {
            if (Step > 0)
            {
                double n = Math.Round((v - (double)Min) / Step, MidpointRounding.AwayFromZero);
                v = (float)(Min + n * Step);
            }
            return v < Min ? Min : v > Max ? Max : v;
        }

        /// <summary>Moves to <paramref name="v"/>, constrained. False when it is not a number or refused.</summary>
        public bool Set(float v, ChangeSource source)
        {
            if (float.IsNaN(v)) return false;
            return Value.Set(Constrain(v), source);
        }

        /// <summary>A drag begins (the press): the moves until <see cref="EndDrag"/> are one commit. False when one is open already.</summary>
        public bool BeginDrag()
        {
            if (Dragging) return false;
            Dragging = true;
            beginGroup?.Invoke();
            return true;
        }

        /// <summary>The drag ends (the release, the slider hidden). False when none was open.</summary>
        public bool EndDrag()
        {
            if (!Dragging) return false;
            Dragging = false;
            endGroup?.Invoke();
            return true;
        }

        /// <summary>A key on the slider: one step, ten with PageUp/PageDown, or an end. True when handled.</summary>
        public bool Key(PartKey key)
        {
            float unit = Step > 0 ? Step : (Max - Min) / 100f;
            float at = Value.Value;
            switch (key)
            {
                case PartKey.Left:
                case PartKey.Down: return Set(at - unit, ChangeSource.Keyboard) || true;
                case PartKey.Right:
                case PartKey.Up: return Set(at + unit, ChangeSource.Keyboard) || true;
                case PartKey.PageDown: return Set(at - 10 * unit, ChangeSource.Keyboard) || true;
                case PartKey.PageUp: return Set(at + 10 * unit, ChangeSource.Keyboard) || true;
                case PartKey.Home: return Set(Min, ChangeSource.Keyboard) || true;
                case PartKey.End: return Set(Max, ChangeSource.Keyboard) || true;
                default: return false;
            }
        }

        /// <summary>Pulls the value once a frame. True when the drawn value changed.</summary>
        public bool Sync() => Value.Sync();

        /// <summary>The slider: Grabbed while dragged, Disabled while read-only.</summary>
        public void Describe(List<PartDescription> into)
        {
            PartFlags f = (Dragging ? PartFlags.Grabbed : PartFlags.None) | (Value.Enabled ? PartFlags.None : PartFlags.Disabled);
            into.Add(new PartDescription { Name = "slider", Role = PartRole.Slider, Label = Label, Flags = f });
        }
    }
}
