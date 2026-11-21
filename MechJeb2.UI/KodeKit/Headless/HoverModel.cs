#if !CSHARP_7_3
#nullable disable
#endif

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>What a call to <see cref="HoverModel"/> changed for its host.</summary>
    public enum HoverChange
    {
        /// <summary>Nothing to draw or hide.</summary>
        None,

        /// <summary>The card of <see cref="HoverModel.Shown"/> opens: the host builds and shows it.</summary>
        Opened,

        /// <summary>The card that was shown closes: the host hides it.</summary>
        Closed,

        /// <summary>The shown card is replaced by <see cref="HoverModel.Shown"/>'s at once (the pointer moved to another target while it closed).</summary>
        Switched
    }

    /// <summary>
    /// When a tooltip or a hover card opens and closes, without Unity: the pointer's enters and leaves, its presses and the time.
    /// Time is passed in (seconds, any steady clock) so a test needs no clock. A target is any object (a control, or a key that
    /// several controls share, such as a table's row); it is compared by reference. The card is a read-only display: the pointer
    /// never moves onto it (it catches no ray), so there is no rule for a pointer over the card.
    /// <list type="bullet">
    /// <item>The pointer entering a target waits its open delay; the card opens at the first <see cref="Tick"/> at or after it,
    /// if the pointer is still there.</item>
    /// <item>Leaving a target counts after its close delay: entering the same target again before then is as if the pointer had
    /// never left (the wait goes on, or the card stays). With no close delay the leave counts at once, and a card shown closes.</item>
    /// <item>Entering another target while the shown card is closing (left, within its close delay) switches to that target's card
    /// at once, with no second wait. Entering another target while a card is shown and not closing (a target inside another)
    /// closes that card and waits again.</item>
    /// <item>A press (any button) closes the shown card and drops the wait: nothing opens again until the pointer leaves the
    /// target and its leave counts, then enters a target.</item>
    /// <item>A target that goes away (disabled, destroyed) is forgotten at once: its card closes and its wait is dropped
    /// (<see cref="Cancel"/>).</item>
    /// <item>A host with nothing to show for a card that opened (an empty text) declines it (<see cref="Decline"/>): it is tried
    /// again at the next tick, while the pointer stays.</item>
    /// </list>
    /// </summary>
    [NotAWidget("behaviour: when a tooltip or a hover card opens and closes; the card is a read-only display that catches no pointer and has no part to pick")]
    public sealed class HoverModel
    {
        /// <summary>Seconds the pointer rests on a target before its card opens (KodeKit's tooltips: 1, as GuiUtils).</summary>
        public double OpenDelay { get; set; } = 1;

        /// <summary>Seconds after a leave before it counts (0: at once, the plain tooltip's rule).</summary>
        public double CloseDelay { get; set; }

        object hovered;
        double openAt, leftAt, closeDelay;
        bool leaving, pressed;

        /// <summary>The target the pointer is on (or left, within its close delay); null when none.</summary>
        public object Hovered => hovered;

        /// <summary>The target whose card is shown; null when none.</summary>
        public object Shown { get; private set; }

        /// <summary>Whether a card is shown.</summary>
        public bool IsShown => Shown != null;

        /// <summary>Whether a card waits to open (the pointer rests on a target, no press since it entered).</summary>
        public bool Waiting => hovered != null && Shown == null && !pressed && !leaving;

        /// <summary>Whether the shown card (or the wait) ends unless the pointer comes back within the close delay.</summary>
        public bool Closing => leaving;

        /// <summary>When the waiting card opens (valid while <see cref="Waiting"/>).</summary>
        public double OpensAt => openAt;

        /// <summary>The pointer enters <paramref name="target"/>, with the model's delays.</summary>
        public HoverChange Enter(object target, double now) => Enter(target, now, OpenDelay, CloseDelay);

        /// <summary>The pointer enters <paramref name="target"/>, with its own delays (a hover card's close delay, a tooltip's none).</summary>
        public HoverChange Enter(object target, double now, double openDelay, double closeDelay)
        {
            if (target == null) return HoverChange.None;
            if (ReferenceEquals(target, hovered) && leaving)
            {
                // back within the close delay: as if it never left
                leaving = false;
                return HoverChange.None;
            }
            if (ReferenceEquals(target, hovered)) return HoverChange.None;

            if (Shown != null && leaving)
            {
                // the shown card was closing: the new target's card replaces it at once
                hovered = target;
                Shown = target;
                this.closeDelay = closeDelay;
                leaving = false;
                pressed = false;
                return HoverChange.Switched;
            }

            HoverChange change = Shown != null ? HoverChange.Closed : HoverChange.None;
            Shown = null;
            hovered = target;
            this.closeDelay = closeDelay;
            openAt = now + openDelay;
            leaving = false;
            pressed = false;
            return change;
        }

        /// <summary>The pointer leaves <paramref name="target"/> (another target's leave changes nothing).</summary>
        public HoverChange Leave(object target, double now)
        {
            if (target == null || !ReferenceEquals(target, hovered)) return HoverChange.None;
            if (closeDelay <= 0) return Forget();
            if (!leaving)
            {
                leaving = true;
                leftAt = now;
            }
            return HoverChange.None;
        }

        /// <summary>A press of any button: the shown card closes, the wait is dropped until the pointer leaves and enters again.</summary>
        public HoverChange Press(double now)
        {
            if (hovered == null) return HoverChange.None;
            pressed = true;
            if (Shown == null) return HoverChange.None;
            Shown = null;
            return HoverChange.Closed;
        }

        /// <summary><paramref name="target"/> went away: its card closes at once and its wait is dropped.</summary>
        public HoverChange Cancel(object target)
        {
            if (target == null || !ReferenceEquals(target, hovered) && !ReferenceEquals(target, Shown)) return HoverChange.None;
            return Forget();
        }

        /// <summary>The host had nothing to show for the card that opened: it is hidden and tried again at the next tick.</summary>
        public void Decline()
        {
            Shown = null;
        }

        /// <summary>Closes everything (the host's own reset): the card closes, nothing waits.</summary>
        public HoverChange Reset() => Forget();

        /// <summary>The time passes: a leave counts once its close delay is over, a wait opens its card once its open delay is over.</summary>
        public HoverChange Tick(double now)
        {
            if (leaving && now >= leftAt + closeDelay) return Forget();
            if (hovered != null && Shown == null && !pressed && !leaving && now >= openAt)
            {
                Shown = hovered;
                return HoverChange.Opened;
            }
            return HoverChange.None;
        }

        HoverChange Forget()
        {
            HoverChange change = Shown != null ? HoverChange.Closed : HoverChange.None;
            hovered = null;
            Shown = null;
            leaving = false;
            pressed = false;
            return change;
        }
    }
}
