#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>
    /// The value side every input widget's model shares, without Unity: a value read by a getter and written by a setter that takes
    /// the change's source (KodeKit passes its commit target's <see cref="CommitTarget{T}.Set"/>), what the widget shows of it, and who
    /// may change it. The widget's own model (<see cref="ToggleModel"/>, <see cref="Switch3Model"/>, <see cref="SliderModel"/>,
    /// <see cref="TextFieldModel"/>) adds its thin rules on top and describes the control.
    /// <list type="bullet">
    /// <item><see cref="Set"/> calls the setter every time it is allowed, also with the value it already has: some setters act when
    /// called. It is refused, with nothing called, while the value is read-only (<see cref="Enabled"/> false) or when
    /// <see cref="Allow"/> says no.</item>
    /// <item><see cref="Shown"/> is what the widget draws: the value as last read, by <see cref="Sync"/> (once a frame) or right after
    /// a set. A setter that keeps another value (clamped, refused by the module) shows that value at once. A set whose setter keeps
    /// the value as it was returns true with <see cref="Shown"/> and <see cref="Version"/> unchanged and no
    /// <see cref="ShownChanged"/>: a host whose control already moved (uGUI's toggle or slider) draws it again after every set.</item>
    /// <item><see cref="Version"/> counts the changes of <see cref="Shown"/>, and <see cref="ShownChanged"/> fires on each, whoever
    /// made it: the host repaints on it, so a click shows at once and a change from outside on the next sync.</item>
    /// <item><see cref="LastSource"/> is the source of the last set that reached the setter: the commit it made has the same one.</item>
    /// <item>Nothing allocates: a sync is a getter call and a compare.</item>
    /// </list>
    /// </summary>
    [NotAWidget("the value side the input widgets' models share; each widget's model describes the control")]
    public sealed class ValueModel<T>
    {
        readonly Func<T> get;
        readonly Action<T, ChangeSource> set;
        readonly Func<bool> enabled;
        readonly IEqualityComparer<T> comparer;

        /// <summary>
        /// The value read by <paramref name="get"/> and written by <paramref name="set"/>, editable while <paramref name="enabled"/>
        /// holds (always when null), compared with <paramref name="comparer"/> (the type's default when null).
        /// </summary>
        public ValueModel(Func<T> get, Action<T, ChangeSource> set, Func<bool> enabled = null, IEqualityComparer<T> comparer = null)
        {
            this.get = get ?? throw new ArgumentNullException(nameof(get));
            this.set = set ?? throw new ArgumentNullException(nameof(set));
            this.enabled = enabled;
            this.comparer = comparer ?? EqualityComparer<T>.Default;
            Shown = get();
        }

        /// <summary>A value whose setter takes no source.</summary>
        public ValueModel(Func<T> get, Action<T> set, Func<bool> enabled = null, IEqualityComparer<T> comparer = null)
            : this(get, Plain(set), enabled, comparer)
        {
        }

        static Action<T, ChangeSource> Plain(Action<T> set)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            return (v, source) => set(v);
        }

        /// <summary>When set, asked before each set with the value and the source; false refuses it.</summary>
        public Func<T, ChangeSource, bool> Allow;

        /// <summary>The value now (a getter call).</summary>
        public T Value => get();

        /// <summary>What the widget draws: the value as last read.</summary>
        public T Shown { get; private set; }

        /// <summary>The number of changes of <see cref="Shown"/> since the model was made.</summary>
        public int Version { get; private set; }

        /// <summary>Whether the value can be changed now.</summary>
        public bool Enabled => enabled == null || enabled();

        /// <summary>The source of the last set that reached the setter.</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>The number of setter calls this model made.</summary>
        public int Writes { get; private set; }

        /// <summary><see cref="Shown"/> changed (by a set or by <see cref="Sync"/>): the host repaints.</summary>
        public event Action ShownChanged;

        /// <summary>Whether <paramref name="v"/> could be set now with <paramref name="source"/>, without setting it.</summary>
        public bool CanSet(T v, ChangeSource source) => Enabled && (Allow == null || Allow(v, source));

        /// <summary>
        /// Calls the setter with <paramref name="v"/> and shows what the value is then. False, with nothing called, while read-only or
        /// when <see cref="Allow"/> refuses.
        /// </summary>
        public bool Set(T v, ChangeSource source)
        {
            if (!CanSet(v, source)) return false;
            LastSource = source;
            Writes++;
            set(v, source);
            Sync();
            return true;
        }

        /// <summary>Reads the value. True when <see cref="Shown"/> changed (<see cref="ShownChanged"/> fired).</summary>
        public bool Sync()
        {
            T v = get();
            if (comparer.Equals(v, Shown)) return false;
            Shown = v;
            Version++;
            ShownChanged?.Invoke();
            return true;
        }
    }
}
