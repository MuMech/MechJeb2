#if !CSHARP_7_3
#nullable disable
#endif
using System;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest.
namespace KodeKit.Headless
{
    /// <summary>The side of its anchor a popup opens on.</summary>
    public enum PopupSide
    {
        Below,
        Above,
        Right,
        Left
    }

    /// <summary>
    /// How a popup lines up with its anchor along the side it opens on: Start is the anchor's left edge (below or above) or its top
    /// edge (right or left), End the other edge, Centre the middle.
    /// </summary>
    public enum PopupAlign
    {
        Start,
        Centre,
        End
    }

    /// <summary>A corner of the screen (the toast stack's place).</summary>
    public enum PopupCorner
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }

    /// <summary>Where <see cref="PopupPlacer"/> put a popup.</summary>
    public struct PopupPlacement
    {
        /// <summary>The popup's box on the canvas, at its scale (canvas units, y up).</summary>
        public FrameBox Box;

        /// <summary>The side it opened on (the preferred one, or the opposite one when it flipped).</summary>
        public PopupSide Side;

        /// <summary>Whether it opened on the side opposite the preferred one.</summary>
        public bool Flipped;

        /// <summary>Whether the clamp moved it (it would have run off the screen on the side or the alignment asked for).</summary>
        public bool Clamped;

        /// <summary>Whether it fits on the screen whole (false: larger than the screen, its top-left corner kept on it).</summary>
        public bool Fits;

        public override string ToString() => $"{Box} {Side}{(Flipped ? " flipped" : "")}{(Clamped ? " clamped" : "")}{(Fits ? "" : " too big")}";
    }

    /// <summary>
    /// Where a popup goes, without Unity: KodeKit's drop-down lists, dialogs, toast stack and tooltips all place themselves here.
    /// Everything is in the canvas's local units with y up (<see cref="FrameBox"/>); the host converts its RectTransforms' corners
    /// and puts the popup at the result. A popup's size is its own (unscaled) size; it is drawn at <c>scale</c> (MechJeb's UI scale,
    /// about its top-left corner), so its box on the canvas is the size times the scale.
    /// <list type="bullet">
    /// <item>A popup opens on its preferred side of the anchor (a rect; a point is a rect of no size), <c>offset</c> units away from it,
    /// lined up by <see cref="PopupAlign"/>.</item>
    /// <item>It flips to the opposite side when the preferred side has no room for it and the opposite side has room. When neither
    /// side has room, it takes the side with more room.</item>
    /// <item>Then it is clamped on the screen, <c>margin</c> units in from each edge: it slides along both axes until it is whole on
    /// the screen. A popup larger than the screen keeps its top-left corner on it (the start of a list, a title bar).</item>
    /// <item>A dialog is centred on the screen (<see cref="Centre"/>), on whole units. A toast stack sits in a corner, <c>margin</c>
    /// units in (<see cref="Corner"/>).</item>
    /// <item>A tooltip keeps its pointer placement: only the clamp is shared (<see cref="Clamp"/>).</item>
    /// <item>The tallest list that fits (<see cref="MaxHeight"/>) is the room below or above the anchor, whichever is larger, less the
    /// margin, and never less than a floor (a list on a tiny screen still shows some rows). The room on a side is never more than the
    /// screen's height: an anchor off the bottom edge doesn't make a list taller than the screen.</item>
    /// </list>
    /// </summary>
    public static class PopupPlacer
    {
        /// <summary>
        /// Places a popup of unscaled size <paramref name="width"/> x <paramref name="height"/>, drawn at <paramref name="scale"/>, by
        /// <paramref name="anchor"/>, on <paramref name="screen"/>.
        /// </summary>
        public static PopupPlacement Place(FrameBox anchor, float width, float height, FrameBox screen, float scale = 1,
            PopupSide side = PopupSide.Below, PopupAlign align = PopupAlign.Start, float offset = 0, float margin = 0)
        {
            if (!(scale > 0)) scale = 1;
            float w = width * scale, h = height * scale;
            bool vertical = side == PopupSide.Below || side == PopupSide.Above;
            float need = vertical ? h : w;

            PopupSide chosen = side;
            float room = Room(anchor, screen, side, offset, margin);
            if (room < need)
            {
                PopupSide other = Opposite(side);
                float otherRoom = Room(anchor, screen, other, offset, margin);
                if (otherRoom >= need || otherRoom > room) chosen = other;
            }

            FrameBox box = OnSide(anchor, w, h, chosen, align, offset);
            FrameBox clamped = Clamp(box, screen, margin);
            return new PopupPlacement
            {
                Box     = clamped,
                Side    = chosen,
                Flipped = chosen != side,
                Clamped = clamped.Left != box.Left || clamped.Top != box.Top,
                Fits    = w <= screen.Width - 2 * margin && h <= screen.Height - 2 * margin
            };
        }

        /// <summary>The room between the anchor and the screen's edge on <paramref name="side"/>, less the offset and the margin.</summary>
        public static float Room(FrameBox anchor, FrameBox screen, PopupSide side, float offset = 0, float margin = 0)
        {
            float r;
            switch (side)
            {
                case PopupSide.Below:
                    r = anchor.Bottom - screen.Bottom;
                    break;
                case PopupSide.Above:
                    r = screen.Top - anchor.Top;
                    break;
                case PopupSide.Right:
                    r = screen.Right - anchor.Right;
                    break;
                default:
                    r = anchor.Left - screen.Left;
                    break;
            }

            return r - offset - margin;
        }

        /// <summary>The side opposite <paramref name="side"/>.</summary>
        public static PopupSide Opposite(PopupSide side)
        {
            switch (side)
            {
                case PopupSide.Below: return PopupSide.Above;
                case PopupSide.Above: return PopupSide.Below;
                case PopupSide.Right: return PopupSide.Left;
                default:              return PopupSide.Right;
            }
        }

        // the box of a w x h popup against the anchor on that side, before the clamp
        private static FrameBox OnSide(FrameBox a, float w, float h, PopupSide side, PopupAlign align, float offset)
        {
            float left, top;
            switch (side)
            {
                case PopupSide.Below:
                    left = Along(a.Left, a.Right, w, align);
                    top  = a.Bottom - offset;
                    break;
                case PopupSide.Above:
                    left = Along(a.Left, a.Right, w, align);
                    top  = a.Top + offset + h;
                    break;
                case PopupSide.Right:
                    left = a.Right + offset;
                    top  = AlongDown(a.Top, a.Bottom, h, align);
                    break;
                default:
                    left = a.Left - offset - w;
                    top  = AlongDown(a.Top, a.Bottom, h, align);
                    break;
            }

            return new FrameBox(left, top, left + w, top - h);
        }

        // the left edge of a span of length w lined up on [lo, hi]
        private static float Along(float lo, float hi, float w, PopupAlign align)
        {
            if (align == PopupAlign.Start) return lo;
            if (align == PopupAlign.End) return hi - w;
            return (lo + hi - w) / 2;
        }

        // the top edge of a span of height h lined up on [bottom, top], Start being the top
        private static float AlongDown(float top, float bottom, float h, PopupAlign align)
        {
            if (align == PopupAlign.Start) return top;
            if (align == PopupAlign.End) return bottom + h;
            return (top + bottom + h) / 2;
        }

        /// <summary>
        /// <paramref name="box"/> slid onto <paramref name="screen"/>, <paramref name="margin"/> units in from each edge, its size kept.
        /// A box wider or taller than the room keeps its left or top edge on it.
        /// </summary>
        public static FrameBox Clamp(FrameBox box, FrameBox screen, float margin = 0)
        {
            float w = box.Width, h = box.Height;
            float left = box.Left, top = box.Top;
            float minLeft = screen.Left + margin, maxLeft = screen.Right - margin - w;
            float maxTop = screen.Top - margin, minTop = screen.Bottom + margin + h;
            if (left > maxLeft) left = maxLeft;
            if (left < minLeft) left = minLeft;
            if (top < minTop) top = minTop;
            if (top > maxTop) top = maxTop;
            return new FrameBox(left, top, left + w, top - h);
        }

        /// <summary>
        /// A popup of unscaled size <paramref name="width"/> x <paramref name="height"/> at <paramref name="scale"/>, centred on the
        /// screen on whole units, then clamped on it.
        /// </summary>
        public static PopupPlacement Centre(float width, float height, FrameBox screen, float scale = 1)
        {
            if (!(scale > 0)) scale = 1;
            float w = width * scale, h = height * scale;
            float left = (float)Math.Round(screen.Left + (screen.Width - w) / 2);
            float top  = (float)Math.Round(screen.Top - (screen.Height - h) / 2);
            var box = new FrameBox(left, top, left + w, top - h);
            FrameBox clamped = Clamp(box, screen);
            return new PopupPlacement
            {
                Box = clamped, Side = PopupSide.Below, Clamped = clamped.Left != box.Left || clamped.Top != box.Top,
                Fits = w <= screen.Width && h <= screen.Height
            };
        }

        /// <summary>
        /// A popup of unscaled size <paramref name="width"/> x <paramref name="height"/> at <paramref name="scale"/> in a corner of the
        /// screen, <paramref name="margin"/> units in from its two edges, then clamped on it.
        /// </summary>
        public static PopupPlacement Corner(float width, float height, FrameBox screen, PopupCorner corner, float margin = 0, float scale = 1)
        {
            if (!(scale > 0)) scale = 1;
            float w = width * scale, h = height * scale;
            bool right = corner == PopupCorner.BottomRight || corner == PopupCorner.TopRight;
            bool bottom = corner == PopupCorner.BottomLeft || corner == PopupCorner.BottomRight;
            float left = right ? screen.Right - margin - w : screen.Left + margin;
            float top = bottom ? screen.Bottom + margin + h : screen.Top - margin;
            var box = new FrameBox(left, top, left + w, top - h);
            FrameBox clamped = Clamp(box, screen);
            return new PopupPlacement
            {
                Box = clamped, Side = bottom ? PopupSide.Above : PopupSide.Below, Clamped = clamped.Left != box.Left || clamped.Top != box.Top,
                Fits = w <= screen.Width && h <= screen.Height
            };
        }

        /// <summary>
        /// The tallest popup (canvas units, at its scale) that fits below or above <paramref name="anchor"/>, whichever has more room,
        /// less <paramref name="margin"/>, a side's room being at most the screen's height; never less than <paramref name="floor"/> or the
        /// screen's height, whichever is smaller.
        /// </summary>
        public static float MaxHeight(FrameBox anchor, FrameBox screen, float margin, float floor)
        {
            // an anchor partly off the screen has no more room than the screen itself on either side
            float below = Math.Min(Room(anchor, screen, PopupSide.Below), screen.Height);
            float above = Math.Min(Room(anchor, screen, PopupSide.Above), screen.Height);
            return Math.Max(Math.Max(below, above) - margin, Math.Min(floor, screen.Height));
        }
    }
}
