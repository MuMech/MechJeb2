#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>
    /// A choice's effect shown before it is picked (ui-sdk's createPreview), without Unity: the entry the pointer rests on in an
    /// open list, or the one the keyboard highlights, previews its effect (the maneuver planner's ghost orbit), and leaving the
    /// list without a pick shows the committed state again. The model decides only when a preview starts and ends: its host
    /// computes and draws the preview on <see cref="Previewed"/> and removes it on <see cref="Reverted"/>. Time is passed in
    /// (seconds, any steady clock) so a test needs no clock.
    /// <list type="bullet">
    /// <item>A preview never sets the value: the model takes no setter, and <see cref="Commit"/> only ends the preview (the
    /// list's own pick sets the value). <see cref="Committed"/> reads the real value.</item>
    /// <item>Hovering an entry (the pointer, or the keyboard's highlight) waits <see cref="Debounce"/> seconds; the preview starts
    /// at the first <see cref="Tick"/> at or after it, if that entry is still hovered, so sweeping the list computes nothing.</item>
    /// <item>One preview at a time: hovering another entry ends the shown one at once and waits again.</item>
    /// <item>An entry that is not previewable (the host's predicate: an operation that did not opt in) previews nothing, and
    /// ends the shown one.</item>
    /// <item>The pointer leaving the hovered entry, <see cref="Revert"/> (Escape, the list closing, its owner disabled or gone, a
    /// vessel or scene change) and <see cref="Commit"/> end the preview and drop the wait: the committed state shows again.</item>
    /// <item>A pointer leave of an entry that is not the hovered one (the keyboard moved the highlight since) changes nothing.</item>
    /// <item>Off (<see cref="Enabled"/> false, the setting's default) nothing previews; turning it off ends the shown one.</item>
    /// <item><see cref="Invalidate"/> (the preview's inputs changed: the orbit, the time, the target) previews the shown entry
    /// again at once, for the host to compute it again.</item>
    /// </list>
    /// </summary>
    [NotAWidget("behaviour: when a list's hovered entry previews its effect; the list itself (ComboModel) describes the rows")]
    public sealed class PreviewModel<T>
    {
        readonly Func<T> committed;
        readonly Func<T, bool> previewable;
        readonly IEqualityComparer<T> same = EqualityComparer<T>.Default;

        T hovered, previewing;
        bool hasHovered, isPreviewing, enabled;
        double previewAt;

        /// <param name="committed">Reads the real value (the entry picked); the model never writes it.</param>
        /// <param name="previewable">Whether an entry has a preview (null: every entry).</param>
        public PreviewModel(Func<T> committed, Func<T, bool> previewable = null)
        {
            this.committed = committed;
            this.previewable = previewable;
        }

        /// <summary>Seconds an entry stays hovered before its preview starts (about 150 ms).</summary>
        public double Debounce { get; set; } = 0.15;

        /// <summary>Whether previews are on (the Settings toggle; off by default). Turning it off ends the shown preview.</summary>
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value) return;
                enabled = value;
                if (!value) Revert();
            }
        }

        /// <summary>The real value: the entry picked.</summary>
        public T Committed => committed();

        /// <summary>Who made the last pick (<see cref="Commit"/>); <see cref="ChangeSource.Program"/> before any.</summary>
        public ChangeSource CommittedSource { get; private set; } = ChangeSource.Program;

        /// <summary>Whether an entry is hovered (its preview waits or is shown).</summary>
        public bool IsHovering => hasHovered;

        /// <summary>The hovered entry (valid while <see cref="IsHovering"/>).</summary>
        public T Hovered => hovered;

        /// <summary>Who hovered it: the pointer or the keyboard's highlight.</summary>
        public ChangeSource HoveredSource { get; private set; } = ChangeSource.Program;

        /// <summary>Whether a preview is shown.</summary>
        public bool IsPreviewing => isPreviewing;

        /// <summary>The entry whose preview is shown (valid while <see cref="IsPreviewing"/>).</summary>
        public T Previewing => previewing;

        /// <summary>Whether a preview waits for the debounce (an entry with a preview is hovered, none shown yet).</summary>
        public bool Waiting => hasHovered && !isPreviewing && enabled && CanPreview(hovered);

        /// <summary>When the waiting preview starts (valid while <see cref="Waiting"/>).</summary>
        public double PreviewAt => previewAt;

        /// <summary>How many previews were started (each one a computation for the host).</summary>
        public int Previews { get; private set; }

        /// <summary>A preview starts (or starts again, after <see cref="Invalidate"/>): the host computes and draws the entry's effect.</summary>
        public event Action<T> Previewed;

        /// <summary>The shown preview ends: the host removes it, and the committed state shows.</summary>
        public event Action Reverted;

        bool CanPreview(T item) => previewable == null || previewable(item);

        /// <summary>The pointer enters an entry, or the keyboard highlights one, at <paramref name="now"/>.</summary>
        public void Hover(T item, ChangeSource source, double now)
        {
            if (!enabled) return;
            if (hasHovered && same.Equals(hovered, item))
            {
                HoveredSource = source;
                return;
            }
            EndPreview();
            hovered = item;
            hasHovered = true;
            HoveredSource = source;
            previewAt = now + Debounce;
        }

        /// <summary>The pointer leaves an entry: the preview ends if that entry is the hovered one.</summary>
        public void Leave(T item)
        {
            if (!hasHovered || !same.Equals(hovered, item)) return;
            Revert();
        }

        /// <summary>Starts the waiting preview once its debounce is over. True when one started.</summary>
        public bool Tick(double now)
        {
            if (!Waiting || now < previewAt) return false;
            Start();
            return true;
        }

        /// <summary>The preview's inputs changed: the shown preview starts again (the host computes it again). True when one was shown.</summary>
        public bool Invalidate()
        {
            if (!isPreviewing) return false;
            Start();
            return true;
        }

        /// <summary>The list picked an entry (it sets the value itself): the preview ends.</summary>
        public void Commit(ChangeSource source)
        {
            CommittedSource = source;
            Revert();
        }

        /// <summary>Ends the preview and drops the hover (the pointer left, Escape, the list closed, its owner went away).</summary>
        public void Revert()
        {
            EndPreview();
            hasHovered = false;
            hovered = default(T);
        }

        void Start()
        {
            previewing = hovered;
            isPreviewing = true;
            Previews++;
            Previewed?.Invoke(previewing);
        }

        void EndPreview()
        {
            if (!isPreviewing) return;
            isPreviewing = false;
            previewing = default(T);
            Reverted?.Invoke();
        }
    }
}
