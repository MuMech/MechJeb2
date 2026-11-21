#if !CSHARP_7_3
#nullable disable
#endif
using System;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>
    /// Scrolls a list while a drag holds the pointer near its top or bottom edge, without Unity (ui-sdk's <c>createAutoScroll</c>).
    /// The host calls <see cref="Tick"/> once a frame during a drag with the pointer's height in the viewport and the time passed.
    /// <list type="bullet">
    /// <item>The pointer within <see cref="Edge"/> of the viewport's top scrolls up, of its bottom scrolls down; elsewhere nothing.
    /// Past the edge (above the top or below the bottom) counts as on it: the fastest.</item>
    /// <item>The speed grows from <see cref="MinSpeed"/> at the zone's inner side to <see cref="MaxSpeed"/> at the edge, with the
    /// square of the depth into the zone, so a pointer that just enters scrolls slowly.</item>
    /// <item>A zone held for less than <see cref="Delay"/> doesn't scroll: a drag that only crosses the edge leaves the list where it
    /// is. Leaving the zone (or <see cref="Reset"/>) starts that wait again.</item>
    /// <item>The step is the speed times the time passed, which is capped at <see cref="MaxStep"/> so a long frame doesn't jump.</item>
    /// <item>A viewport smaller than both zones splits it at its middle.</item>
    /// </list>
    /// Units are the list's (the viewport's height, the edge) and seconds.
    /// </summary>
    [NotAWidget("helper: a list's scroll speed near its edge during a drag (ListDrag); the list and its rows have no part description yet (roadmap item 32)")]
    public sealed class AutoScrollModel
    {
        /// <summary>The height of each edge's zone.</summary>
        public float Edge = 24;

        /// <summary>The speed at the zone's inner side and at the edge, in list units a second.</summary>
        public float MinSpeed = 60, MaxSpeed = 600;

        /// <summary>How long the pointer stays in a zone before the list scrolls, in seconds.</summary>
        public float Delay = 0.15f;

        /// <summary>The longest time one step counts, in seconds.</summary>
        public float MaxStep = 0.1f;

        float held;
        int heldDirection;

        /// <summary>Time spent in the current zone so far.</summary>
        public float Held => held;

        /// <summary>
        /// The scroll speed for a pointer <paramref name="y"/> below the viewport's top (negative: above it) in a viewport of
        /// <paramref name="viewportHeight"/>: negative scrolls up, positive down, 0 outside both zones.
        /// </summary>
        public float Speed(float y, float viewportHeight)
        {
            if (viewportHeight <= 0 || Edge <= 0) return 0;
            float zone = Math.Min(Edge, viewportHeight / 2);
            float fromTop = y, fromBottom = viewportHeight - y;
            if (fromTop < zone && fromTop <= fromBottom) return -SpeedAt(fromTop, zone);
            if (fromBottom < zone) return SpeedAt(fromBottom, zone);
            return 0;
        }

        float SpeedAt(float distance, float zone)
        {
            float depth = Math.Max(0, Math.Min(1, (zone - distance) / zone));
            return MinSpeed + (MaxSpeed - MinSpeed) * depth * depth;
        }

        /// <summary>
        /// The scroll for this frame: <see cref="Speed"/> times <paramref name="dt"/> (capped), once the zone has been held for
        /// <see cref="Delay"/>; 0 otherwise.
        /// </summary>
        public float Step(float y, float viewportHeight, float dt)
        {
            float speed = Speed(y, viewportHeight);
            int direction = Math.Sign(speed);
            if (direction == 0 || direction != heldDirection)
            {
                heldDirection = direction;
                held = 0;
                if (direction == 0) return 0;
            }
            dt = Math.Max(0, Math.Min(MaxStep, dt));
            float before = held;
            held += dt;
            if (held < Delay) return 0;
            // only the part of this frame past the delay scrolls
            float counted = before >= Delay ? dt : held - Delay;
            return speed * counted;
        }

        /// <summary>Scrolls <paramref name="list"/> by this frame's <see cref="Step"/>; true when its offset changed.</summary>
        public bool Tick(VirtualListModel list, float y, float dt, ChangeSource source = ChangeSource.Pointer)
        {
            float step = Step(y, list.ViewportHeight, dt);
            return step != 0 && list.SetOffset(list.Offset + step, source);
        }

        /// <summary>The drag ended or left the list: the next zone waits for <see cref="Delay"/> again.</summary>
        public void Reset()
        {
            held = 0;
            heldDirection = 0;
        }
    }
}
