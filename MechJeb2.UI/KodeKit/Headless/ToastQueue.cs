#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>How serious a toast is: it sets the colour of the toast and how long it stays.</summary>
    public enum ToastSeverity { Info, Success, Warning, Error }

    /// <summary>Why a toast left the screen.</summary>
    public enum ToastEnd { Expired, Dismissed, Dropped }

    /// <summary>One notice. The queue owns it; a host only reads it.</summary>
    [NotAWidget("data: one notice of a ToastQueue")]
    public sealed class Toast
    {
        internal Toast(int id, string text, ToastSeverity severity, float duration)
        {
            Id = id;
            Text = text;
            Severity = severity;
            Duration = duration;
            Remaining = duration;
        }

        public int Id { get; }
        public string Text { get; }
        public ToastSeverity Severity { get; }

        /// <summary>How many times this text was shown while the toast was on screen or waiting (1: once).</summary>
        public int Count { get; internal set; } = 1;

        /// <summary>Seconds on screen (0 or less: until dismissed).</summary>
        public float Duration { get; }

        /// <summary>Seconds left on screen. It starts when the toast becomes visible and stops while the stack is hovered.</summary>
        public float Remaining { get; internal set; }

        public bool Sticky => Duration <= 0;
    }

    /// <summary>
    /// The queue behind KodeKit's toast stack, without Unity: short notices, a few on screen at a time. Time is passed in
    /// (seconds, any steady clock) so a test needs no clock.
    /// <list type="bullet">
    /// <item>At most <see cref="MaxVisible"/> toasts are visible; the rest wait in order and take a free place as one leaves.
    /// At most <see cref="MaxPending"/> wait: beyond that the oldest waiting toast is dropped.</item>
    /// <item>The same text and severity shown again while a toast with it is visible or waiting is counted
    /// (<see cref="Toast.Count"/>), not stacked; a visible one gets its full time again.</item>
    /// <item>Each severity has its own duration (<see cref="SetDuration"/>); 0 or less keeps the toast until it is dismissed.
    /// A toast's time starts when it becomes visible, not when it was posted.</item>
    /// <item>While the stack is hovered (<see cref="SetHovered"/>) no visible toast loses time; leaving resumes with the
    /// time that was left.</item>
    /// <item><see cref="Dismiss"/> removes a visible or waiting toast at once. Empty text posts nothing.</item>
    /// <item><see cref="Version"/> changes whenever what a host draws changes (a toast added, counted again or removed).</item>
    /// </list>
    /// </summary>
    public sealed class ToastQueue : IDescribeParts
    {
        /// <summary>What the toast stack is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The stack (a group) and each visible toast, by id (a status: its text as its label; a click dismisses it).</summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription { Name = "toasts", Role = PartRole.Group, Label = Label });
            for (int i = 0; i < visible.Count; i++)
                into.Add(new PartDescription { Name = "toast." + visible[i].Id, Role = PartRole.Status, Label = visible[i].Text });
        }

        readonly List<Toast> visible = new List<Toast>();
        readonly List<Toast> pending = new List<Toast>();
        readonly float[] durations = { 4f, 3f, 6f, 8f };
        int nextId = 1;
        double last = double.NaN;

        public ToastQueue(int maxVisible = 4, int maxPending = 32)
        {
            if (maxVisible < 1) throw new ArgumentOutOfRangeException(nameof(maxVisible));
            if (maxPending < 0) throw new ArgumentOutOfRangeException(nameof(maxPending));
            MaxVisible = maxVisible;
            MaxPending = maxPending;
        }

        public int MaxVisible { get; }
        public int MaxPending { get; }

        /// <summary>The toasts on screen, oldest first.</summary>
        public IReadOnlyList<Toast> Visible => visible;

        public int PendingCount => pending.Count;

        /// <summary>No toast on screen or waiting: a host can stop its per-frame update until the next <see cref="Posted"/>.</summary>
        public bool Empty => visible.Count == 0 && pending.Count == 0;

        /// <summary>True while the stack is hovered and the timers are stopped.</summary>
        public bool Hovered { get; private set; }

        /// <summary>Changes whenever what a host draws changes.</summary>
        public int Version { get; private set; }

        /// <summary>Waiting toasts that were dropped for lack of room.</summary>
        public int Dropped { get; private set; }

        /// <summary>A text was posted: the toast, and whether it only counted a toast already there. For a log or an agent.</summary>
        public event Action<Toast, bool> Posted;

        /// <summary>A toast left (expired, dismissed or dropped).</summary>
        public event Action<Toast, ToastEnd> Closed;

        public float GetDuration(ToastSeverity severity) => durations[(int)severity];

        public void SetDuration(ToastSeverity severity, float seconds) => durations[(int)severity] = seconds;

        /// <summary>Posts a notice. Null when the text is empty.</summary>
        public Toast Show(string text, ToastSeverity severity, double now)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            Advance(now);

            Toast t = Find(visible, text, severity);
            bool repeat = t != null;
            if (repeat)
                t.Remaining = t.Duration;
            else
            {
                t = Find(pending, text, severity);
                repeat = t != null;
            }

            if (repeat)
                t.Count++;
            else
            {
                t = new Toast(nextId++, text, severity, durations[(int)severity]);
                if (visible.Count < MaxVisible && pending.Count == 0)
                    visible.Add(t);
                else
                {
                    pending.Add(t);
                    if (pending.Count > MaxPending)
                    {
                        Toast gone = pending[0];
                        pending.RemoveAt(0);
                        Dropped++;
                        Closed?.Invoke(gone, ToastEnd.Dropped);
                    }
                }
            }

            Version++;
            Posted?.Invoke(t, repeat);
            return t;
        }

        /// <summary>Moves the clock: visible toasts lose the time that passed unless hovered, expired ones leave, waiting ones come in. True when something changed.</summary>
        public bool Update(double now)
        {
            int before = Version;
            Advance(now);
            return Version != before;
        }

        /// <summary>The pointer is over the stack (true) or left it. Time up to <paramref name="now"/> counts first.</summary>
        public void SetHovered(bool hovered, double now)
        {
            Advance(now);
            Hovered = hovered;
        }

        /// <summary>Removes a visible or waiting toast. True when there was one.</summary>
        public bool Dismiss(int id, ChangeSource source, double now)
        {
            Advance(now);
            for (int i = 0; i < visible.Count; i++)
                if (visible[i].Id == id)
                {
                    Toast t = visible[i];
                    visible.RemoveAt(i);
                    Left(t, ToastEnd.Dismissed);
                    return true;
                }
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Id == id)
                {
                    Toast t = pending[i];
                    pending.RemoveAt(i);
                    Left(t, ToastEnd.Dismissed);
                    return true;
                }
            return false;
        }

        /// <summary>Removes every toast, visible and waiting.</summary>
        public void DismissAll(double now)
        {
            Advance(now);
            while (visible.Count > 0) Dismiss(visible[0].Id, ChangeSource.Program, now);
            while (pending.Count > 0) Dismiss(pending[0].Id, ChangeSource.Program, now);
        }

        public Toast Find(int id)
        {
            for (int i = 0; i < visible.Count; i++)
                if (visible[i].Id == id) return visible[i];
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Id == id) return pending[i];
            return null;
        }

        static Toast Find(List<Toast> list, string text, ToastSeverity severity)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Severity == severity && list[i].Text == text) return list[i];
            return null;
        }

        void Left(Toast t, ToastEnd why)
        {
            Version++;
            Closed?.Invoke(t, why);
            Promote();
        }

        void Promote()
        {
            while (visible.Count < MaxVisible && pending.Count > 0)
            {
                visible.Add(pending[0]);
                pending.RemoveAt(0);
                Version++;
            }
        }

        void Advance(double now)
        {
            double dt = double.IsNaN(last) ? 0 : Math.Max(0, now - last);
            if (double.IsNaN(last) || now > last) last = now;
            if (dt <= 0 || Hovered) return;

            // a toast that expires frees a place for a waiting one, which starts with its full time (dt is not charged to it)
            for (int i = visible.Count - 1; i >= 0; i--)
            {
                Toast t = visible[i];
                if (t.Sticky) continue;
                t.Remaining -= (float)dt;
                if (t.Remaining > 0) continue;
                visible.RemoveAt(i);
                Version++;
                Closed?.Invoke(t, ToastEnd.Expired);
            }
            Promote();
        }
    }
}
